using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;
using UnityEngine.SceneManagement; 

namespace LooseLink
{
public class ServiceEnvironment
{
    readonly List<IServiceSourceProvider> _installers = new List<IServiceSourceProvider>();

    // INSTALL & UNINSTALL SETS ---------------
    
    public bool TryInstallServiceSourceSet(ServiceSourceSet serviceSourceSet) =>
        TryInstallServiceSourceProvider(serviceSourceSet);
    
    public bool TryUninstallServiceSourceSet(ServiceSourceSet serviceSourceSet) => 
        TryUninstallServiceSourceProvider( serviceSourceSet);

    
    public void UninstallAllSourceSets()
    {
        Type[] types =TypesOfWholeEnvironment().ToArray();
        _installers.Clear();
        InvokeEnvironmentChanged(types);
    }
    
    internal bool TryInstallServiceSourceProvider(IServiceSourceProvider serviceSourceProvider)
    {
        if (serviceSourceProvider == null) return false;
        if (_installers.Contains(serviceSourceProvider)) return false;
        _installers.Add(serviceSourceProvider);
        TrySortInstallers();
        InvokeEnvironmentChangedOnInstaller(serviceSourceProvider);
        return false;
    }

    internal bool TryUninstallServiceSourceProvider(IServiceSourceProvider serviceSourceProvider)
    {
        if (serviceSourceProvider == null) return false;
        if (!_installers.Remove(serviceSourceProvider)) return false;
        TrySortInstallers();
        InvokeEnvironmentChangedOnInstaller(serviceSourceProvider);
        return false;
    }

    internal void SetAllGlobalInstallers(List<ServiceSourceSet> globalInstallers)
    {
        UninstallAllSourceSets();
        foreach (ServiceSourceSet globalInstaller in globalInstallers)
            _installers.Add(globalInstaller);
        TrySortInstallers();
    }

    // SORT INSTALLERS ---------------
    
    static readonly Comparison<IServiceSourceProvider> installerSorting = (a, b) => b.PriorityValue.CompareTo(a.PriorityValue);

    internal void SortInstallers()
    {
        if (TrySortInstallers())
            InvokeEnvironmentChangedOnWholeEnvironment();
    }

    bool TrySortInstallers()
    {
        if (_installers.Count <= 1) return true; 
        var isSorted = true;
        IServiceSourceProvider s1 = _installers[0];
        for (var index = 1; index < _installers.Count; index++)
        {
            IServiceSourceProvider s2 = _installers[index];
            if (installerSorting.Invoke(s1, s2) > 0)
            {
                isSorted = false; 
                break;
            }

            s1 = s2;
        }

        if (isSorted)
            return false;

        _installers.Sort(installerSorting);
        return true;
    }
    
    // INSTALLER & SOURCE GETTERS ---------------

    public IEnumerable<IServiceSourceProvider> GetEnabledAndActiveInstallers() => 
        GetAllInstallers().Where(installer => installer.IsEnabled);

    public IEnumerable<IServiceSourceProvider> GetAllInstallers()
    {
        if (Application.isPlaying)
            foreach (IServiceSourceProvider set in _installers)
                yield return set;
        else
            foreach (IServiceSourceProvider serviceSourceSet in FindInstallers())
                yield return serviceSourceSet;
    }
    
    static readonly List<IServiceSourceProvider> sets = new List<IServiceSourceProvider>();
    static IEnumerable<IServiceSourceProvider> FindInstallers()
    {
        sets.Clear();
        sets.AddRange(Services.FindGlobalInstallers);

        var localInstallers = new List<InstallerComponent>();
        localInstallers.AddRange(
            FindObjectsOfTypeAll<LocalServiceInstaller>());
        localInstallers.AddRange(
            FindObjectsOfTypeAll<ServerObject>().Where(serverObj => serverObj.InstallAutomatically));


        int maxPriority = sets.Count == 0 ? 0 : sets.Select(set => set.PriorityValue).Max();

        sets.AddRange(localInstallers.Where(
            inst => inst.Priority.type == InstallerPriority.Type.ConcreteValue));

        foreach (InstallerComponent localInstaller in localInstallers.Where(
            inst => inst.Priority.type == InstallerPriority.Type.HighestAtInstallation))
        {
            maxPriority++;
            localInstaller.SetInstallationValue(maxPriority);
            sets.Add(localInstaller);
        }
 
        sets.Sort(installerSorting);
        return sets;
    }

    static IEnumerable<T> FindObjectsOfTypeAll<T>() where T: Component
    {
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || !scene.IsValid()) continue;
            foreach (GameObject go in scene.GetRootGameObjects())
            foreach (T t in go.GetComponentsInChildren<T>(includeInactive: true))
                yield return t;
        }
    }

    internal IEnumerable<(IServiceSourceProvider installer, ServiceSource source)> ServiceSources
    {
        get
        { 
            foreach (IServiceSourceProvider installer in GetEnabledAndActiveInstallers())
            foreach (ServiceSource setting in installer.GetEnabledValidSourcesRecursive())
                yield return (installer, setting);
        }
    }

    public int MaxPriority => GetAllInstallers().FirstOrDefault()?.PriorityValue ?? 0;

    internal void InitServiceSources()
    {
        foreach ((IServiceSourceProvider installer, ServiceSource source) pair in ServiceSources)
            pair.source.InitDynamicIfNeeded();
    }

    // SUBSCRIPTION ---------------------

    public event Action EnvironmentChanged;
    readonly Dictionary<Type, HashSet<Action>> _subscribers = new Dictionary<Type, HashSet<Action>>();

    public void SubscribeToEnvironmentChange<T>(Action callback) =>
        SubscribeToEnvironmentChange(typeof(T), callback);

    public void UnSubscribeToEnvironmentChange<T>(Action callback) =>
        UnSubscribeToEnvironmentChange(typeof(T), callback);


    public void SubscribeToEnvironmentChange(Type type, Action callback)
    {
        if (!_subscribers.TryGetValue(type, out HashSet<Action> callbacks))
        {
            callbacks = new HashSet<Action>();
            _subscribers.Add(type, callbacks);
        }

        callbacks.Add(callback);
    }

    public void UnSubscribeToEnvironmentChange(Type type, Action callback)
    {
        if (!_subscribers.TryGetValue(type, out HashSet<Action> callbacks))
            return;
        callbacks.Remove(callback);
    }
    
    // INVOKE SUBSCRIPTION ---------------------
    
    public void InvokeEnvironmentChangedOnWholeEnvironment()
    {
        // Debug.Log("Whole Environment Change");
        InvokeEnvironmentChanged(TypesOfWholeEnvironment());
    }
    
    internal void InvokeEnvironmentChangedOnInstaller(IServiceSourceProvider provider)
    {
        // Debug.Log($"Installer Change: {provider.Name}"); 
        InvokeEnvironmentChanged(TypesOfInstaller(provider));
    }
    
    internal void InvokeEnvironmentChangedOnSources(params ServiceSource[] sources)
    {
        // string typeNames = string.Join(", ", sources.Select(t => t == null ? "-" : t.Name).ToArray());
        // Debug.Log($"Sources Change: {typeNames}"); 
        InvokeEnvironmentChanged(TypesOfSources(sources));
    }

    internal void InvokeEnvironmentChangedOnSource(ServiceSource source)
    {
        // Debug.Log($"Source Change: {source.Name}"); 
        InvokeEnvironmentChanged(TypesOfSource(source));
    }

    internal void InvokeEnvironmentChangedOnType(Type type)
    {
        // Debug.Log($"Type Change Type: {type.Name}");  
        InvokeEnvironmentChanged(TypesOfType(type));
    }
    internal void InvokeEnvironmentChangedOnTypes(params Type[] types)
    {
        // string typeNames = string.Join(", ", types.Select(t => t == null ? "-" : t.Name).ToArray());
        // Debug.Log($"Types Changed Type: {typeNames}"); 
        InvokeEnvironmentChanged(types);
    }
    
    static HashSet<Type> _tempTypes = new HashSet<Type>();
    void InvokeEnvironmentChanged(IEnumerable<Type> types)
    {
        if (!Services.AreServiceLocatorInitialized) return;
        if (Services.IsDestroying) return;
        
        _tempTypes = EnumerableToHashSet(types);
        if(_tempTypes.IsEmpty()) return;
        foreach (Type type in _tempTypes)
            if (_subscribers.TryGetValue(type, out HashSet<Action> callbacks))
            {
                foreach (Action callback in callbacks)
                    callback?.Invoke();
            }
        EnvironmentChanged?.Invoke();
    }

    static HashSet<Type> EnumerableToHashSet(IEnumerable<Type> types)
    {
        _tempTypes.Clear();
        foreach (Type type in types)
            if(type!=null) 
                _tempTypes.Add(type);
        return _tempTypes;
    }
    
    
    // TYPES OF METHODS ---------------------
    
    public IReadOnlyList<Type> GetAllInstalledServiceTypes() => EnumerableToHashSet(TypesOfWholeEnvironment()).ToList();
    IEnumerable<Type>  TypesOfWholeEnvironment()
    {  
        foreach (IServiceSourceProvider set in GetEnabledAndActiveInstallers())
        foreach (Type type in  TypesOfInstaller( set))
            yield return type;
    }

    static IEnumerable<Type> TypesOfInstaller(IServiceSourceProvider provider)
    {
        foreach (ServiceSource source in provider.GetEnabledValidSourcesRecursive())
        foreach (Type type in  TypesOfSource( source))
            yield return type;
    }

    static IEnumerable<Type> TypesOfSources(ServiceSource[] sources)
    {
        foreach (ServiceSource source in sources)
        foreach (Type type in TypesOfSource(source))
            yield return type;
    }

    static IEnumerable<Type> TypesOfSource(ServiceSource source) => source.GetServiceTypesRecursively();

    static IEnumerable<Type> TypesOfType(Type type) { yield return type; }
    
}
}
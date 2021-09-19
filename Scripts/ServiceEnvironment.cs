using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
public class ServiceEnvironment
{
    readonly List<IServiceSourceSet> _installers = new List<IServiceSourceSet>();

    // INSTALL & UNINSTALL SETS ---------------
    
    public bool TryInstallServiceSourceSet(ServiceSourceSet serviceSourceSet) =>
        TryInstallServiceSourceSet((IServiceSourceSet) serviceSourceSet);
    
    public bool TryUninstallServiceSourceSet(ServiceSourceSet serviceSourceSet) => 
        TryUninstallServiceSourceSet( (IServiceSourceSet) serviceSourceSet);

    
    public void UninstallAllSourceSets()
    {
        Type[] types =TypesOfWholeEnvironment().ToArray();
        _installers.Clear();
        InvokeEnvironmentChanged(types);
    }
    
    internal bool TryInstallServiceSourceSet(IServiceSourceSet serviceSourceSet)
    {
        if (serviceSourceSet == null) return false;
        if (_installers.Contains(serviceSourceSet)) return false;
        _installers.Add(serviceSourceSet);
        TrySortInstallers();
        InvokeEnvironmentChangedOnInstaller(serviceSourceSet);
        return false;
    }

    internal bool TryUninstallServiceSourceSet(IServiceSourceSet serviceSourceSet)
    {
        if (serviceSourceSet == null) return false;
        if (!_installers.Remove(serviceSourceSet)) return false;
        TrySortInstallers();
        InvokeEnvironmentChangedOnInstaller(serviceSourceSet);
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
    
    static readonly Comparison<IServiceSourceSet> _installerSorting = (a, b) => b.Priority.CompareTo(a.Priority);

    internal void SortInstallers()
    {
        if (TrySortInstallers())
            InvokeEnvironmentChangedOnWholeEnvironment();
    }

    bool TrySortInstallers()
    {
        if (_installers.Count <= 1) return true; 
        var isSorted = true;
        IServiceSourceSet s1 = _installers[0];
        for (var index = 1; index < _installers.Count; index++)
        {
            IServiceSourceSet s2 = _installers[index];
            if (_installerSorting.Invoke(s1, s2) > 0)
            {
                isSorted = false; 
                break;
            }

            s1 = s2;
        }

        if (isSorted)
            return false;

        _installers.Sort(_installerSorting);
        return true;
    }
    
    // INSTALLER & SOURCE GETTERS ---------------
    
    internal IEnumerable<IServiceSourceSet> GetInstallers()
    {
        if (Application.isPlaying)
        {
            foreach (IServiceSourceSet set in _installers)
                yield return set;
        }
        else
        {
            var sets = new List<IServiceSourceSet>();
            sets.AddRange(ServiceLocator.FindGlobalInstallers);
            
            List<SceneServiceInstaller> sceneInstallers =
                Object.FindObjectsOfType<SceneServiceInstaller>()
                .Where(inst => inst.isActiveAndEnabled)
                .ToList();

            sets.AddRange(sceneInstallers.Where(
                    inst => inst.PriorityType == SceneServiceInstaller.PriorityTypeEnum.ConcreteValue));


            int maxPriority = sets.Select(set => set.Priority).Max();
            foreach (SceneServiceInstaller sceneInstaller in sceneInstallers.Where(
                inst => inst.PriorityType == SceneServiceInstaller.PriorityTypeEnum.HighestAtInstallation))
            {
                maxPriority++;
                sceneInstaller.priorityAtInstallation = maxPriority;
                sets.Add(sceneInstaller);
            }
          

            sets.Sort(_installerSorting);
            foreach (IServiceSourceSet set in sets)
                yield return set;
        }
    }

    internal IEnumerable<(IServiceSourceSet installer, ServiceSource source)> ServiceSources
    {
        get
        {
            foreach (IServiceSourceSet installer in GetInstallers())
            foreach (ServiceSource setting in installer.GetEnabledValidSourcesRecursive())
                yield return (installer, setting);
        }
    }

    public int MaxPriority => GetInstallers().FirstOrDefault()?.Priority ?? 0;

    internal void InitServiceSources()
    {
        foreach ((IServiceSourceSet installer, ServiceSource source) pair in ServiceSources)
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
        Debug.Log("Whole Environment Change");
        InvokeEnvironmentChanged(TypesOfWholeEnvironment());
    }
    
    internal void InvokeEnvironmentChangedOnInstaller(IServiceSourceSet set)
    {
        Debug.Log($"Installer Change: {set.Name}"); 
        InvokeEnvironmentChanged(TypesOfInstaller(set));
    }
    
    internal void InvokeEnvironmentChangedOnSources(params ServiceSource[] sources)
    {
        string typeNames = string.Join(", ", sources.Select(t => t == null ? "-" : t.Name).ToArray());
        Debug.Log($"Sources Change: {typeNames}"); 
        InvokeEnvironmentChanged(TypesOfSources(sources));
    }

    internal void InvokeEnvironmentChangedOnSource(ServiceSource source)
    {
        Debug.Log($"Source Change: {source.Name}"); 
        InvokeEnvironmentChanged(TypesOfSource(source));
    }

    internal void InvokeEnvironmentChangedOnType(Type type)
    {
        Debug.Log($"Type Change Type: {type.Name}");  
            InvokeEnvironmentChanged(TypesOfType(type));
    }
    internal void InvokeEnvironmentChangedOnTypes(params Type[] types)
    {
        string typeNames = string.Join(", ", types.Select(t => t == null ? "-" : t.Name).ToArray());
        Debug.Log($"Types Changed Type: {typeNames}"); 
        InvokeEnvironmentChanged(types);
    }
    
    static HashSet<Type> _tempTypes = new HashSet<Type>();
    void InvokeEnvironmentChanged(IEnumerable<Type> types)
    {
        if (!Application.isPlaying) return;
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
        foreach (IServiceSourceSet set in GetInstallers())
        foreach (Type type in  TypesOfInstaller( set))
            yield return type;
    }

    static IEnumerable<Type> TypesOfInstaller(IServiceSourceSet set)
    {
        foreach (ServiceSource source in set.GetEnabledValidSourcesRecursive())
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
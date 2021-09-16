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
    readonly List<SceneServiceInstaller> _sceneInstallers = new List<SceneServiceInstaller>();
    List<ServiceSourceSet> _globalInstallers = new List<ServiceSourceSet>();
 

    internal void SetGlobalInstallers(List<ServiceSourceSet> globalInstallers)
    {
        _globalInstallers = globalInstallers;
        TrySortGlobalInstallers(); 
    }

    internal bool  TryAddGlobalInstaller(ServiceSourceSet serviceSourceSet)
    {
        if (serviceSourceSet == null) return false;
        if (_globalInstallers.Contains(serviceSourceSet)) return false;
        _globalInstallers.Add(serviceSourceSet);
        _globalInstallers.Sort(_globalInstallerSorting);
        InvokeEnvironmentChangedOnInstaller(serviceSourceSet);
        return true;
    }

    internal bool TryRemoveGlobalInstaller(ServiceSourceSet serviceSourceSet)
    {
        if (serviceSourceSet == null) return false;
        if (!_globalInstallers.Remove(serviceSourceSet)) return false; 
        _globalInstallers.Sort(_globalInstallerSorting);
        InvokeEnvironmentChangedOnInstaller(serviceSourceSet);
        return true;
    }

    internal void SortGlobalInstallers()
    {
        if (TrySortGlobalInstallers())
            InvokeEnvironmentChangedOnWholeEnvironment();
    }
    

    static readonly Comparison<ServiceSourceSet> _globalInstallerSorting = (a, b) => b.Priority.CompareTo(a.Priority);

    bool TrySortGlobalInstallers()
    {
        if (_globalInstallers.Count <= 1) return true; 
        var isSorted = true;
        ServiceSourceSet s1 = _globalInstallers[0];
        for (var index = 1; index < _globalInstallers.Count; index++)
        {
            ServiceSourceSet s2 = _globalInstallers[index];
            if (_globalInstallerSorting.Invoke(s1, s2) > 0)
            {
                isSorted = false; 
                break;
            }

            s1 = s2;
        }

        if (isSorted)
            return false;

        _globalInstallers.Sort(_globalInstallerSorting);
        return true;
    }
 

    internal IEnumerable<SceneServiceInstaller> SceneInstallers
    {
        get
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return Object.FindObjectsOfType<SceneServiceInstaller>().Where(installer => installer.enabled);
#endif

            return _sceneInstallers;
        }
    }

    internal void AddSceneContextInstaller(SceneServiceInstaller serviceInstaller)
    {
        if (_sceneInstallers.Contains(serviceInstaller)) return;
        _sceneInstallers.Insert(index: 0, serviceInstaller);
        InvokeEnvironmentChangedOnInstaller(serviceInstaller);
    }

    internal void RemoveSceneContextInstaller(SceneServiceInstaller serviceInstaller)
    {
        _sceneInstallers.Remove(serviceInstaller);
        InvokeEnvironmentChangedOnInstaller(serviceInstaller);
    }

    internal IEnumerable<IServiceSourceSet> GetInstallers()
    {
        foreach (SceneServiceInstaller sceneInstaller in SceneInstallers)
            yield return sceneInstaller;

        foreach (ServiceSourceSet globalInstaller in _globalInstallers)
            yield return globalInstaller;
    }

    internal IEnumerable<(IServiceSourceSet installer, ServiceSource source)> SceneAndGlobalContextServiceSources
    {
        get
        {
            foreach (IServiceSourceSet installer in GetInstallers())
            foreach (ServiceSource setting in installer.GetEnabledValidSourcesRecursive())
                yield return (installer, setting);
        }
    }
 

    public HashSet<Type> GetAllInstalledServiceTypes()
    {
        var types = new HashSet<Type>();
        foreach ((IServiceSourceSet _, ServiceSource source) in SceneAndGlobalContextServiceSources)
        foreach (Type type in source.GetServiceTypesRecursively())
            types.Add(type);
        return types;
    }

    public void InitServiceSources()
    {
        foreach ((IServiceSourceSet installer, ServiceSource source) pair in SceneAndGlobalContextServiceSources)
            pair.source.InitDynamicIfNeeded();
    }

    // SUBSCRIPTION

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
    internal void InvokeEnvironmentChangedOnWholeEnvironment()
    {
        Debug.Log("Whole Environment Change");
        InvokeEnvironmentChanged(EnvironmentChanged_WholeEnvironment());
    }

    IEnumerable<Type>  EnvironmentChanged_WholeEnvironment()
    { 
        foreach (SceneServiceInstaller set in _sceneInstallers)
        foreach (Type type in EnvironmentChanged_Installer(set))
            yield return type;
        foreach (ServiceSourceSet set in _globalInstallers)
        foreach (Type type in  EnvironmentChanged_Installer( set))
            yield return type;
    }
    
    
    internal void InvokeEnvironmentChangedOnInstaller(IServiceSourceSet set)
    {
        Debug.Log($"Installer Change: {set.Name}"); 
        InvokeEnvironmentChanged(EnvironmentChanged_Installer(set));
    }

    IEnumerable<Type> EnvironmentChanged_Installer(IServiceSourceSet set)
    {
        foreach (ServiceSource source in set.GetEnabledValidSourcesRecursive())
        foreach (Type type in  EnvironmentChanged_Source( source))
            yield return type;
    }
    
    internal void InvokeEnvironmentChangedOnSources(params ServiceSource[] sources)
    {
        string typeNames = string.Join(", ", sources.Select(t => t == null ? "-" : t.Name).ToArray());
        Debug.Log($"Sources Change: {typeNames}"); 
        InvokeEnvironmentChanged(EnvironmentChanged_Sources(sources));
    }
    IEnumerable<Type> EnvironmentChanged_Sources(ServiceSource[] sources) 
    {
        foreach (ServiceSource source in  sources)
        foreach (Type type in  EnvironmentChanged_Source(source))
            yield return type;
    }


    internal void InvokeEnvironmentChangedOnSource(ServiceSource source)
    {
        Debug.Log($"Source Change: {source.Name}"); 
        InvokeEnvironmentChanged(EnvironmentChanged_Source(source));
    }

    IEnumerable<Type> EnvironmentChanged_Source(ServiceSource source) => source.GetServiceTypesRecursively();

    internal void InvokeEnvironmentChangedOnType(Type type)
    {
        Debug.Log($"Type Change Type: {type.Name}"); 
        InvokeEnvironmentChanged(EnvironmentChanged_Type(type));
    }


    IEnumerable<Type> EnvironmentChanged_Type(Type type)
    {
        if (type != null) yield return type;
    }
    
    internal void InvokeEnvironmentChangedOnTypes(params Type[] types)
    {
        string typeNames = string.Join(", ", types.Select(t => t == null ? "-" : t.Name).ToArray());
        Debug.Log($"Types Changed Type: {typeNames}"); 
        InvokeEnvironmentChanged(types);
    }

    static readonly HashSet<Type> _tempTypes = new HashSet<Type>();
    void InvokeEnvironmentChanged(IEnumerable<Type> types)
    {
        // if (!Application.isPlaying) return; 
        _tempTypes.Clear();
        foreach (Type type in types)
            if(type!=null) 
                _tempTypes.Add(type);
        if(_tempTypes.IsEmpty()) return;
        foreach (Type type in _tempTypes)
            if (_subscribers.TryGetValue(type, out HashSet<Action> callbacks))
            {
                foreach (Action callback in callbacks)
                    callback?.Invoke();
            }
        EnvironmentChanged?.Invoke();
    }
}
}
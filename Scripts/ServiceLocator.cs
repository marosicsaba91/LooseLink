using System;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{

public static class ServiceLocator
{
    internal static readonly List<SceneServiceInstaller> sceneInstallers =
        new List<SceneServiceInstaller>();


    internal static IEnumerable<SceneServiceInstaller> SceneInstallers
    {
        get
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return Object.FindObjectsOfType<SceneServiceInstaller>().Where(installer => installer.enabled);
#endif

            return sceneInstallers;
        }
    }

    internal static List<ServiceSourceSet> globalInstallers =
        new List<ServiceSourceSet>();

    public static event Action InstallersChanged;
    internal static event Action LoadedInstancesChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    internal static void UpdateGlobalInstallers()
    {
        globalInstallers = FindGlobalInstallers;
    }

    static List<ServiceSourceSet> FindGlobalInstallers =>
        Resources
            .LoadAll<ServiceSourceSet>(string.Empty)
            .Where(contextInstaller => contextInstaller.useAsGlobalInstaller)
            .OrderByDescending(set => set.priority)
            .ToList();

    internal static IEnumerable<(IServiceSourceSet installer, ServiceSource source)> SceneAndGlobalContextServiceSources
    {
        get
        {
            foreach (SceneServiceInstaller sceneInstaller in SceneInstallers)
            foreach (ServiceSource setting in sceneInstaller.GetEnabledValidSourcesRecursive())
                yield return (sceneInstaller, setting);

            foreach (ServiceSourceSet globalInstaller in globalInstallers)
            foreach (ServiceSource setting in globalInstaller.GetEnabledValidSourcesRecursive())
                yield return (globalInstaller, setting);
        }
    }

    static Transform _parentObject;

    public static Transform ParentObject
    {
        get
        {
            if (_parentObject == null)
            {
                _parentObject = new GameObject("Services").transform;
                Object.DontDestroyOnLoad(_parentObject.gameObject);
            }

            return _parentObject;
        }
    }

    internal static void AddSceneContextInstaller(SceneServiceInstaller serviceInstaller)
    {
        if (sceneInstallers.Contains(serviceInstaller)) return;
        sceneInstallers.Insert(index: 0, serviceInstaller);
        InstallersChanged?.Invoke(); 
    }

    internal static void RemoveSceneContextInstaller(SceneServiceInstaller serviceInstaller)
    {
        sceneInstallers.Remove(serviceInstaller);
        InstallersChanged?.Invoke(); 
    }


    internal static void ClearAllCachedData()
    {
        foreach (var installerSourcePair in SceneAndGlobalContextServiceSources)
            installerSourcePair.source.GetDynamicServiceSource()?.ClearInstancesAndCachedTypes();
        LoadedInstancesChanged.Invoke();
    }

    public static TService Get<TService>(params object[] tags) =>
        (TService) Get(typeof(TService), tags);

    public static object Get(Type looseServiceType, params object[] tags)
    {
        if (TryGet(looseServiceType, tags, out object service))
            return service;

        throw CantFindService(looseServiceType);
    }

    public static bool TryGet<TService>(out TService service) =>
        TryGet(tags: null, out service);

    public static bool TryGet(Type looseServiceType, out object service) =>
        TryGet(looseServiceType, tags: null, out service);

    public static bool TryGet<TService>(object[] tags, out TService service)
    {
        if (TryGet(typeof(TService), tags, out object service1))
        {
            service = (TService) service1;
            return true;
        }

        service = default;
        return false;
    }

    public static bool TryGet(Type looseServiceType, object[] tags, out object service)
    {
        foreach ((IServiceSourceSet installer, ServiceSource source) in SceneAndGlobalContextServiceSources)
        {
            DynamicServiceSource dynamicSource = source?.GetDynamicServiceSource();
            if (dynamicSource == null) continue;
            bool typeEnabled = dynamicSource.GetAllAbstractTypes().Contains(looseServiceType);

            if (!typeEnabled)
                foreach (SerializableType variable in source.additionalTypes)
                {
                    if (typeEnabled) break;
                    if (variable.Type == looseServiceType) typeEnabled = true;
                }

            if (!typeEnabled) continue;

            if (TryGetServiceInSource(looseServiceType, installer, source, dynamicSource, tags, out object serv))
            {
                service = serv;
                return true;
            }
        }

        service = null;
        return false;
    }

    static bool TryGetServiceInSource(
        Type looseServiceType,
        IServiceSourceSet set,
        ServiceSource source,
        DynamicServiceSource dynamicSource,
        object[] tags, out object service)
    {
        service = null;

        if (!dynamicSource.TryGetService(looseServiceType, set, tags, source.additionalTags, out service,
            out bool newInstance))
            return false;
        if (newInstance)
            LoadedInstancesChanged?.Invoke();
        return true;
    }

    static Exception CantFindService(Type looseServiceType) =>
        new ArgumentException($"Can't find Services of this Type: {looseServiceType}");



    internal static IEnumerable<IServiceSourceSet> GetInstallers()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UpdateGlobalInstallers();
#endif

        foreach (SceneServiceInstaller sceneInstaller in SceneInstallers)
            yield return sceneInstaller;

        foreach (ServiceSourceSet globalInstaller in globalInstallers)
            yield return globalInstaller;
    }

    public static HashSet<Type> GetAllInstalledTypes()
    {
        var types = new HashSet<Type>();
        foreach ((IServiceSourceSet _, ServiceSource source) in SceneAndGlobalContextServiceSources)
        foreach (Type type in source.GetServiceTypes())
            types.Add(type);
        return types;
    }

    internal static void InvokeLoadedInstancesChanged() => LoadedInstancesChanged?.Invoke();

    /*
     // MIGHT WILL BE FUNCTIONALITY
     
    static readonly Dictionary<Type, HashSet<Action>> subscribers = new Dictionary<Type, HashSet<Action>>();

    public static void SubscribeToEnvironmentChange<T>(Action callback) =>
        SubscribeToEnvironmentChange(typeof(T), callback);

    public static void UnSubscribeToEnvironmentChange<T>( Action callback)=>
        UnSubscribeToEnvironmentChange(typeof(T), callback);


    public static void SubscribeToEnvironmentChange(Type type, Action callback)
    {
        if (!subscribers.TryGetValue(type, out HashSet<Action> callbacks))
        {
            callbacks = new HashSet<Action>();
            subscribers.Add(type,callbacks);
        }

        callbacks.Add(callback);
    }

    public static void UnSubscribeToEnvironmentChange(Type type, Action callback)
    {
        if (!subscribers.TryGetValue(type, out HashSet<Action> callbacks))
            return;
        callbacks.Remove(callback);
    }

    static readonly HashSet<Type> tempTypeSet = new HashSet<Type>();
    internal static void InvokeEnvironmentChanged(IServiceSourceSet set)
    {
        tempTypeSet.Clear();
        foreach (ServiceSource source in set.GetValidSources())
        foreach (Type type in source.GetServiceTypes())
            tempTypeSet.Add(type);

        foreach (Type type in tempTypeSet)
            InvokeEnvironmentChanged(type);
    }

    internal static void InvokeEnvironmentChanged(Type type)
    {
        if (subscribers.TryGetValue(type, out HashSet<Action> callbacks))
        {
            foreach (Action callback in callbacks)
             callback?.Invoke();
        }
    }
    */

}
}
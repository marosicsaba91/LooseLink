using System;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{

public static class Services
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

    public static event Action SceneContextInstallersChanged;
    public static event Action LoadedInstancesChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    internal static void UpdateGlobalInstallers()
    {
        globalInstallers = FindGlobalInstallers;
    }

    static List<ServiceSourceSet> FindGlobalInstallers =>
        Resources
            .LoadAll<ServiceSourceSet>(string.Empty)
            .Where(contextInstaller => contextInstaller.useAsGlobalInstaller)
            .OrderByDescending(set =>set.priority)
            .ToList();

    internal static IEnumerable<(IServiceSourceSet installer, ServiceSource source)> SceneAndGlobalContextServiceSources
    {
        get
        {
            foreach (SceneServiceInstaller sceneInstaller in SceneInstallers)
            foreach (ServiceSource setting in sceneInstaller.GetValidSources())
                yield return (sceneInstaller, setting);

            foreach (ServiceSourceSet globalInstaller in globalInstallers)
            foreach (ServiceSource setting in globalInstaller.GetValidSources())
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
                _parentObject = new GameObject("Loose Services").transform;
                Object.DontDestroyOnLoad(_parentObject.gameObject);
            }

            return _parentObject;
        }
    }

    internal static void AddSceneContextInstaller(SceneServiceInstaller serviceInstaller)
    {
        if (sceneInstallers.Contains(serviceInstaller)) return;
        sceneInstallers.Insert(index: 0, serviceInstaller);
        SceneContextInstallersChanged?.Invoke();
    }

    internal static void RemoveSceneContextInstaller(SceneServiceInstaller serviceInstaller)
    {
        sceneInstallers.Remove(serviceInstaller);
        SceneContextInstallersChanged?.Invoke();
    }


    internal static void ClearAllCachedData()
    {
        foreach (var installerSourcePair in SceneAndGlobalContextServiceSources)
            installerSourcePair.source.GetDynamicServiceSource()?.ClearInstances();

        LoadedInstancesChanged?.Invoke();
    }

    public static TService Get<TService>(params object[] tags) =>
        (TService) Get(typeof(TService), tags);

    public static object Get(Type looseServiceType, params object[] tags)
    {
        if (TryGet(looseServiceType, tags, out object service))
            return service;

        throw CantFindService(looseServiceType);
    }

    public static bool TryGet<TService>(Type looseServiceType, object[] tags, out TService service)
    {
        if (TryGet(looseServiceType, tags, out object service1))
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
            if (dynamicSource == null) break;
            bool typeEnabled = dynamicSource.GetAllAbstractTypes().Contains(looseServiceType);
            
            foreach (SerializableType variable in source.additionalTypes)
            {
                if(typeEnabled) break;
                if (variable.Type == looseServiceType) typeEnabled = true;
            }
            if (!typeEnabled) break;
            
            if (TryGetServiceInSource(looseServiceType, installer, dynamicSource, tags, out object serv))
            {
                service = serv;
                return true;
            }
        }

        service = null;
        return false;
    }

    static bool TryGetServiceInSource(Type looseServiceType, IServiceSourceSet set, DynamicServiceSource source,
        object[] tags, out object service)
    {
        service = null;

        if (!source.TryGetService(looseServiceType, set, tags, out object sys, out bool newInstance))
            return false;
        if (!newInstance)
        {
            service = sys;
            return true;
        }

        sys.TryInitialize();
        LoadedInstancesChanged?.Invoke();
        service = sys;
        return true;
    }

    static Exception CantFindService(Type looseServiceType) =>
        new ArgumentException($"Can't instantiate Services of this Type: {looseServiceType}");

 

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
}
}
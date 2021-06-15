using System;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{

public static class Services
{
    internal static readonly List<SceneServiceSet> sceneInstallers =
        new List<SceneServiceSet>();


    internal static IEnumerable<SceneServiceSet> SceneInstallers
    {
        get
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return Object.FindObjectsOfType<SceneServiceSet>().Where(installer => installer.enabled);
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
            foreach (SceneServiceSet sceneInstaller in SceneInstallers)
            foreach (ServiceSource source in ((IServiceSourceSet) sceneInstaller).GetServiceSources())
                yield return (sceneInstaller, source);

            foreach (ServiceSourceSet globalInstaller in globalInstallers)
            foreach (ServiceSource source in ((IServiceSourceSet) globalInstaller).GetServiceSources())
                yield return (globalInstaller, source);
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

    internal static void AddSceneContextInstaller(SceneServiceSet serviceSet)
    {
        if (sceneInstallers.Contains(serviceSet)) return;
        sceneInstallers.Insert(index: 0, serviceSet);
        SceneContextInstallersChanged?.Invoke();
    }

    internal static void RemoveSceneContextInstaller(SceneServiceSet serviceSet)
    {
        sceneInstallers.Remove(serviceSet);
        SceneContextInstallersChanged?.Invoke();
    }


    internal static void ClearAllCachedData()
    {
        foreach (var installerSourcePair in SceneAndGlobalContextServiceSources)
            installerSourcePair.source.ClearInstances();

        LoadedInstancesChanged?.Invoke();
    }

    public static TService Get<TService>(params object[] tags) =>
        (TService) Get(typeof(TService), tags);

    public static object Get(Type looseServiceType, params object[] tags)
    {
        if (TryGetService(looseServiceType, tags, out object service))
            return service;

        throw CantFindService(looseServiceType);
    }


    static bool TryGetService(Type looseServiceType, object[] tags, out object service)
    {
        foreach ((IServiceSourceSet installer, ServiceSource source) in SceneAndGlobalContextServiceSources)
        {  
            ErrorCheckForType(installer, looseServiceType);
            if (TryGetServiceInSource(looseServiceType, installer, source,
                tags, out object serv))
            {
                service = serv;
                return true;
            }
        }

        service = null;
        return false;
    }

    static bool TryGetServiceInSource(Type looseServiceType, IServiceSourceSet set, ServiceSource source,
        object[] tags, out object service)
    {
        service = null;
        if (!source.GetAllAbstractTypes(set).Contains(looseServiceType)) return false;

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

    static void ErrorCheckForType(IServiceSourceSet set, Type looseServiceType)
    { 
        if (!set.ServiceTypeProvider.IsServiceType(looseServiceType))
            throw new TypeLoadException(
                $"{looseServiceType} is ignored not a service Type. Mark it with [Service] attribute.");
    }

    internal static IEnumerable<IServiceSourceSet> GetInstallers()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UpdateGlobalInstallers();
#endif

        foreach (SceneServiceSet sceneInstaller in SceneInstallers)
            yield return sceneInstaller;

        foreach (ServiceSourceSet globalInstaller in globalInstallers)
            yield return globalInstaller;
    }
}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MUtility;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityServiceLocator
{

public static class ServiceLocator
{
    static readonly bool debugLogs = false;
    public static TimeSpan SetupTime { get; private set; }

    static readonly ServiceEnvironment environment = new ServiceEnvironment();
    public static ServiceEnvironment Environment => environment;
    internal static bool IsDestroying { get; private set; } 
    internal static bool AreServiceLocatorInitialized { get; private set; } 

    static ServiceLocator() => Init();
    
    static Transform _parentObject;

#if UNITY_EDITOR
    static void OnUnityPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode)
        {
            IsDestroying = true;
            ClearAllCachedData(); 
        }
    }
#endif

    internal static void Init()
    {
        if (AreServiceLocatorInitialized) return;
        DateTime start = DateTime.Now;
        
        #if UNITY_EDITOR
                EditorApplication.playModeStateChanged += OnUnityPlayModeChanged;
        #endif
        environment.SetAllGlobalInstallers(FindGlobalInstallers);
        environment.InitServiceSources();
        ServiceTypeHelper.Init();
        IsDestroying = false;
        AreServiceLocatorInitialized = true;
        
        SetupTime = DateTime.Now - start;
        if(debugLogs) 
            Debug.Log($"Init {SetupTime.TotalMilliseconds} ms");
    }

    internal static List<ServiceSourceSet> FindGlobalInstallers =>
        Resources
            .LoadAll<ServiceSourceSet>(string.Empty)
            .Where(contextInstaller => contextInstaller.automaticallyUseAsGlobalInstaller)
            .ToList();

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

    internal static void ClearAllCachedData()
    {
        foreach ((IServiceSourceProvider installer, ServiceSource source) installerSourcePair in Environment.ServiceSources)
            installerSourcePair.source?.ClearCachedInstancesAndTypes_NoEnvironmentChangeEvent();
    }

    public static TService Resolve<TService>(params object[] tags) =>
        (TService) Resolve(typeof(TService), tags);

    public static object Resolve(Type looseServiceType, params object[] tags)
    {
        if (TryResolve(looseServiceType, tags, out object service))
            return service;

        throw CantFindService(looseServiceType, tags);
    }

    public static bool TryResolve<TService>(out TService service) =>
        TryResolve(tags: null, out service);

    public static bool TryResolve(Type looseServiceType, out object service) =>
        TryResolve(looseServiceType, tags: null, out service);

    public static bool TryResolve<TService>(object[] tags, out TService service)
    {
        if (TryResolve(typeof(TService), tags, out object service1))
        {
            service = (TService) service1;
            return true;
        }

        service = default;
        return false;
    }

    public static void InitTypeMap() => ServiceTypeHelper.Init();


    public static bool TryResolve(Type looseServiceType, object[] tags, out object service)
    {
        if(debugLogs)
            Debug.Log("Resolve");

        if (!ServiceLocationSetupData.Instance.enableTags && !tags.IsNullOrEmpty())
            Debug.LogWarning("If You want to use Service Tags, enable them in Service Locator's settings menu."+
                             "(Tools / Service Locator / Open Settings in top right corner)");
        
        foreach ((IServiceSourceProvider installer, ServiceSource source) in Environment.ServiceSources)
        { 
            if (!source.IsServiceSource) continue;
            bool serviceTypeFound = source.GetDynamicServiceTypes().Contains(looseServiceType);

            if (!serviceTypeFound)
                foreach (SerializableType variable in source.additionalTypes)
                {
                    if (serviceTypeFound) break;
                    if (variable.Type == looseServiceType) serviceTypeFound = true;
                }

            if (!serviceTypeFound) continue;

            if (TryGetServiceInSource(looseServiceType, installer, source,  tags, out object serv))
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
        IServiceSourceProvider provider,
        ServiceSource source, 
        object[] tags, out object service)
    {
        service = null;

        if (!source.TryGetService(looseServiceType, provider, tags, out service))
            return false;
        
        return true;
    }

    static Exception CantFindService(Type looseServiceType, object[] tags)
    {
        if(tags.IsNullOrEmpty())
            return new ArgumentException($"Can't find Services of this Type: {looseServiceType}");
          
        var tagNames = new StringBuilder();
        for (var i = 0; i < tags.Length; i++)
        {
            object tag = tags[i];
            tagNames.Append(new Tag(tag).Name);
            if(i< tags.Length-1)
                tagNames.Append(", ");
        }

        return new ArgumentException(
            $"Can't find Services of this Type: {looseServiceType} with these tags: [{tagNames}]");
    }

    internal static IEnumerable<IServiceSourceProvider> GetAllInstallers() => 
        Environment.GetAllInstallers();
 
}
}
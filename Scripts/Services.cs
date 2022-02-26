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

namespace LooseLink
{

public static class Services
{
    static readonly bool debugLogs = false;
    public static TimeSpan SetupTime { get; private set; }

    static readonly ServiceEnvironment environment = new ServiceEnvironment();
    public static ServiceEnvironment Environment => environment;
    internal static bool IsDestroying { get; private set; } 
    internal static bool AreServiceLocatorInitialized { get; private set; } 

    static Services() => Init();
    
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

    public static TService Get<TService>(params object[] tags) =>
        (TService) Get(typeof(TService), tags);

    public static object Get(Type looseServiceType, params object[] tags)
    {
        if (TryGet(looseServiceType, tags, out object service))
            return service;

        return CantFindService(looseServiceType, tags);
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
    

    static object CantFindService(Type looseServiceType, object[] tags)
    {
        ServiceLocationSetupData.CantResolveAction action = ServiceLocationSetupData.Instance.whenServiceCantBeResolved;

        if (action == ServiceLocationSetupData.CantResolveAction.ReturnNull)
            return null;
        
        string text;
        if (tags.IsNullOrEmpty())
            text = $"Can't find Services of this Type: {looseServiceType}";
        else
        {
            var tagNames = new StringBuilder();
            for (var i = 0; i < tags.Length; i++)
            {
                object tag = tags[i];
                tagNames.Append(new Tag(tag).Name);
                if (i < tags.Length - 1)
                    tagNames.Append(", ");
            }

            text = $"Can't find Services of this Type: {looseServiceType} with these tags: [{tagNames}]";
        }
        
        if (action == ServiceLocationSetupData.CantResolveAction.ThrowException)
            throw new ArgumentException(text);
        
        Debug.LogWarning(text);
        return null;
    }

    internal static IEnumerable<IServiceSourceProvider> GetAllInstallers() => 
        Environment.GetAllInstallers();
 
}
}
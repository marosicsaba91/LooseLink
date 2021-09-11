using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
public class ServiceEnvironment
{
    readonly List<SceneServiceInstaller> sceneInstallers = new List<SceneServiceInstaller>();
    internal List<ServiceSourceSet> globalInstallers = new List<ServiceSourceSet>();
    
    public event Action InstallersChanged;
    internal event Action LoadedInstancesChanged;

    internal IEnumerable<SceneServiceInstaller> SceneInstallers
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
    
    internal void AddSceneContextInstaller(SceneServiceInstaller serviceInstaller)
    {
        if (sceneInstallers.Contains(serviceInstaller)) return;
        sceneInstallers.Insert(index: 0, serviceInstaller);
        InstallersChanged?.Invoke(); 
    }

    internal void RemoveSceneContextInstaller(SceneServiceInstaller serviceInstaller)
    {
        sceneInstallers.Remove(serviceInstaller);
        InstallersChanged?.Invoke(); 
    } 
    
    internal IEnumerable<IServiceSourceSet> GetInstallers()
    {
        foreach (SceneServiceInstaller sceneInstaller in SceneInstallers)
            yield return sceneInstaller;

        foreach (ServiceSourceSet globalInstaller in globalInstallers)
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
    
    internal void InvokeLoadedInstancesChanged() => LoadedInstancesChanged?.Invoke();

    public HashSet<Type> GetAllInstalledServiceTypes()
    {
        var types = new HashSet<Type>();
        foreach ((IServiceSourceSet _, ServiceSource source) in SceneAndGlobalContextServiceSources)
        foreach (Type type in source.GetServiceTypes())
            types.Add(type);
        return types;
    }
    
    
    
    
    
    
    
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
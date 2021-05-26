﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{

public static class Services
    {
        internal static Dictionary<Type, List<Type>> iLooseServiceToNonAbstractTypeMap;
        internal static Dictionary<Type, List<Type>> nonAbstractToILooseServiceTypeMap; 
        internal static List<ServiceSource> noInstallerSources = new List<ServiceSource>(); 

        internal static readonly List<SceneInstaller> sceneInstallers =
            new List<SceneInstaller>();
        
        
        internal static IEnumerable<SceneInstaller> SceneInstallers{
            get
            {
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                    return Object.FindObjectsOfType<SceneInstaller>().Where(installer => installer.enabled);
                #endif
                
                return sceneInstallers;
            }
        }
        
        internal static List<GlobalInstaller> globalInstallers =
            new List<GlobalInstaller>();
        
        public static event Action SceneContextInstallersChanged;
        public static event Action LoadedInstancesChanged;
 
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void UpdateGlobalInstallers()
        {
            globalInstallers = FindGlobalInstallers;
        }
          
        static List<GlobalInstaller> FindGlobalInstallers =>
            Resources
                .LoadAll<GlobalInstaller>(string.Empty)
                .Where(contextInstaller => contextInstaller.isEnabledInstaller)
                .ToList();

        internal static IEnumerable<(IServiceInstaller installer,ServiceSource source)> SceneAndGlobalContextServiceSources {
            get
            {
                InitServiceTypeMap(); 

                foreach (SceneInstaller sceneInstaller in SceneInstallers)
                foreach (ServiceSource source in ((IServiceInstaller) sceneInstaller).GetServiceSources())
                    yield return (sceneInstaller, source);

                foreach (GlobalInstaller globalInstaller in globalInstallers)
                foreach (ServiceSource source in ((IServiceInstaller) globalInstaller).GetServiceSources())
                    yield return (globalInstaller, source);
                
                foreach (ServiceSource source in noInstallerSources)
                    yield return (null, source);
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

        internal static IEnumerable<ServiceSource> GetNoInstallerSources()
        {
            InitServiceTypeMap();
            foreach (ServiceSource source in noInstallerSources)
                yield return source;
        }

        internal static void AddSceneContextInstaller(SceneInstaller installer)
        {
            if(sceneInstallers.Contains(installer)) return;
            sceneInstallers.Insert(index: 0, installer);
            SceneContextInstallersChanged?.Invoke();
        }

        internal static void RemoveSceneContextInstaller(SceneInstaller installer)
        {
            sceneInstallers.Remove(installer);
            SceneContextInstallersChanged?.Invoke();
        }
         

        internal static void ClearAllCachedData()
        {
            foreach (var installerSourcePair in SceneAndGlobalContextServiceSources) 
                installerSourcePair.source.ClearInstances();

            LoadedInstancesChanged?.Invoke();
        }
        
        public static TService Get<TService>(params object[] tags) where TService : IService => 
            (TService) Get(typeof(TService), tags);

        public static object Get(Type looseServiceType, params object[] tags)
        {
            ErrorCheckForType(looseServiceType);
            InitServiceTypeMap();

            if (TryGetService(looseServiceType, tags, out object service))
                return service;

            throw CantFindService(looseServiceType);
        }


        static bool TryGetService(Type looseServiceType, object[] tags, out object service)
        {
            foreach (var installerSourcePair in SceneAndGlobalContextServiceSources)
                if (TryGetServiceInSource(looseServiceType, installerSourcePair.installer, installerSourcePair.source,
                    tags, out object serv))
                {
                    service = serv;
                    return true;
                }

            service = null;
            return false;
        }

        static bool TryGetServiceInSource(Type looseServiceType, IServiceInstaller installer, ServiceSource source,
            object[] tags, out object service)
        {
            service = null;
            if (!source.AllAbstractTypes.Contains(looseServiceType)) return false;

            if (!source.TryGetService(looseServiceType, installer, tags, out object sys, out bool newInstance))
                return false;
            if (!newInstance)
            {
                service = sys;
                return true;
            }

            ((IService) sys).Initialize();
            LoadedInstancesChanged?.Invoke();
            service = sys;
            return true;
        }

        static Exception CantFindService(Type looseServiceType) => 
            new ArgumentException($"Can't instantiate Services of this Type: {looseServiceType}");

        static void ErrorCheckForType(Type looseServiceType)
        {
            if (looseServiceType == typeof(IService))
                throw new TypeLoadException("You can't request an instance of an ILooseServices.");

            if (looseServiceType.ContainsGenericParameters)
                throw new TypeLoadException(
                    $"The Type {looseServiceType} is generic Loose Services doesn't support generic types.");

            if (looseServiceType.IsIgnoredLooseServices())
                throw new TypeLoadException(
                    $"The Type {looseServiceType} is ignored by [LooseIgnore] attribute.");
        }

        
        internal static void InitServiceTypeMap()
        {
            if (iLooseServiceToNonAbstractTypeMap != null) return;
            iLooseServiceToNonAbstractTypeMap = new Dictionary<Type, List<Type>>();
            nonAbstractToILooseServiceTypeMap = new Dictionary<Type, List<Type>>();
             
            List<Type> allAbstractServiceTypes = (from abstractServiceType in GetAllSubclassOf(typeof(IService))
                where abstractServiceType.IsInterface ||
                      abstractServiceType.IsSubclassOf(typeof(MonoBehaviour)) ||
                      abstractServiceType.IsSubclassOf(typeof(ScriptableObject))
                select abstractServiceType).ToList();
            

            List<Type> allConcreteServiceTypes = 
                (from abstractServiceType in allAbstractServiceTypes 
                where !abstractServiceType.IsInterface 
                where !abstractServiceType.IsAbstract
                select abstractServiceType).ToList();
 
            IgnoreServiceAttributeHelper.RemoveIgnoredTypes(allAbstractServiceTypes);
            
            // Fill abstractToConcreteTypeMap
            foreach (Type abstractType in allAbstractServiceTypes)
            {
                var concreteTypes = new List<Type>();
                foreach (Type concreteType in allConcreteServiceTypes)
                {
                    if (IsSubClassOrSelf(abstractType, concreteType))
                        concreteTypes.Add(concreteType);
                }
                iLooseServiceToNonAbstractTypeMap.Add(abstractType, concreteTypes);
            }
            
            // Fill concreteToAbstractTypeMap
            foreach (Type concreteType in allConcreteServiceTypes)
            {
                var abstractTypes = new List<Type>();
                foreach (Type abstractType in allAbstractServiceTypes)
                    if (IsSubClassOrSelf(abstractType, concreteType))
                        abstractTypes.Add(abstractType);
                
                nonAbstractToILooseServiceTypeMap.Add(concreteType, abstractTypes);
                
                if(concreteType.IsSubclassOf(typeof(MonoBehaviour)))
                    noInstallerSources.Add(new ServiceSourceFromMonoBehaviourType(concreteType));
                else if(concreteType.IsSubclassOf(typeof(ScriptableObject)))
                    noInstallerSources.Add(new ServiceSourceFromScriptableObjectType(concreteType));
            }
        }

        static IEnumerable<Type> GetAllSubclassOf(Type parent)
        { 
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            foreach (Type t in a.GetTypes())
                if (t.GetInterfaces().Contains(parent)) yield return t;
        }
        
        static bool IsSubClassOrSelf(Type parent, Type child)
        {
            if (parent == child) return true;
            if (child.GetInterfaces().Contains(parent)) return true;
            if (child.IsSubclassOf(parent)) return true;

            return false;
        }

        internal static IEnumerable<IServiceInstaller> GetInstallers()
        {
            InitServiceTypeMap();

        #if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateGlobalInstallers();
        #endif

        foreach (SceneInstaller sceneInstaller in SceneInstallers)
                yield return sceneInstaller;
            
            foreach (GlobalInstaller globalInstaller in globalInstallers)
                    yield return globalInstaller;
        }
    }
}
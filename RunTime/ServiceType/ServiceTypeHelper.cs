using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LooseServices
{
public static class ServiceTypeHelper
{ 
    public static readonly List<Type> allTypes =
        AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).ToList();
    
    internal static void SetupServiceDictionaries( 
        IEnumerable<Type> abstractServiceTypes,
        out Dictionary<Type, List<Type>> serviceToNonAbstractTypeMap ,
        out Dictionary<Type, List<Type>> nonAbstractToServiceTypeMap)
    {  
        serviceToNonAbstractTypeMap = new Dictionary<Type, List<Type>>();
        nonAbstractToServiceTypeMap = new Dictionary<Type, List<Type>>();

        foreach (Type type in allTypes)
        {
            if (type.IsInterface) continue;
            if (type.IsAbstract) continue;
            if (type.ContainsGenericParameters) continue;
            foreach (Type abstractType in abstractServiceTypes)
                if (IsSubClassOrSelf(abstractType, type))
                {
                    if (!nonAbstractToServiceTypeMap.ContainsKey(type))
                        nonAbstractToServiceTypeMap.Add(type, new List<Type>());
                    nonAbstractToServiceTypeMap[type].Add(abstractType);
                }
        }

        foreach (KeyValuePair<Type, List<Type>> pair in nonAbstractToServiceTypeMap)
        {
            Type concreteTypes = pair.Key;
            foreach (Type serviceType in pair.Value)
            {
                if (!serviceToNonAbstractTypeMap.ContainsKey(serviceType))
                    serviceToNonAbstractTypeMap.Add(serviceType, new List<Type>());
                serviceToNonAbstractTypeMap[serviceType].Add(concreteTypes);
            } 
        } 
    }

    static IEnumerable<Type> GetAllSubclassOf(Type parent)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        foreach (Type t in assembly.GetTypes())
            if (t.GetInterfaces().Contains(parent))
                yield return t;
    } 
    
    
    static bool IsSubClassOrSelf(Type parent, Type child)
    {
        if (parent == child) return true;
        if (child.GetInterfaces().Contains(parent)) return true;
        if (child.IsSubclassOf(parent)) return true;

        return false;
    }
    
    // EXTENSION

    internal static bool TryInitialize(this object service)
    {
        if (service == null) return false;
        Type nonAbstractType = service.GetType();
        if (!nonAbstractType.GetInterfaces().Contains(typeof(IInitializable))) return false;

        var initialize = (IInitializable) service;
        initialize.Initialize();
        return true;
    } 

    internal static IServiceTypeProvider SelfOrDefault(this IServiceTypeProvider provider) => 
        provider ?? GlobalServiceTypeProvider.Instance;

    internal static bool IsServiceType(this IServiceTypeProvider provider, Type type) =>
        provider.ServiceTypes.Contains(type);
    internal static bool IsNonAbstractServiceType(this IServiceTypeProvider provider, Type type) => 
        provider.NonAbstractToServiceTypeMap.ContainsKey(type);
    
    internal static List<Type> AllServiceComponents( this IServiceTypeProvider provider, GameObject gameObject )
    {
        var result = new List<Type>();
        if (gameObject == null) return result;
        
        Component[] components = gameObject.GetComponents<Component>();
        foreach (Component component in components)
        {
            Type type = component.GetType();
            if(provider.IsNonAbstractServiceType(type)) 
                result.Add(type); 
        }  
        return result;
    }
   
     
    internal static IEnumerable<Type> GetServicesOfNonAbstractType(this IServiceTypeProvider provider, Type type)
    {
        if (provider.NonAbstractToServiceTypeMap.TryGetValue(type, out List<Type> serviceTypes))
            foreach (Type serviceType in serviceTypes)
                yield return serviceType;
    }
}
}
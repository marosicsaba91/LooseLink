using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LooseLink
{
public static class ServiceTypeHelper
{ 
    
    static readonly HashSet<Type> serviceTypes;
    static readonly Dictionary<Type, List<Type>> nonAbstractToServiceTypeMap;
    
    internal static void Init( ) { }
    
    static ServiceTypeHelper()
    { 

        IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies();
        Assembly[] assemblyArray = assemblies.ToArray(); 
        IEnumerable<Type> allTypes = assemblyArray.SelectMany(a => a.GetTypes()).ToArray(); 
        serviceTypes = AllGlobalServiceTypes(allTypes);

        SetupServiceDictionaries(
            allTypes,
            serviceTypes,
            out nonAbstractToServiceTypeMap);
    }


    static HashSet<Type> AllGlobalServiceTypes(IEnumerable<Type> relevantTypes)
    {
        var result = new HashSet<Type>();
        foreach (Type type in relevantTypes)
        {
            if (type.ContainsGenericParameters) continue;
            var attribute = (ServiceTypeAttribute)
                Attribute.GetCustomAttribute(type, typeof(ServiceTypeAttribute), inherit: false);
            if (attribute != null)
                result.Add(type);
        }

        return result;
    }

    internal static void SetupServiceDictionaries(
        IEnumerable<Type> allTypes ,
        IEnumerable<Type> abstractServiceTypes,
        // out Dictionary<Type, List<Type>> serviceToNonAbstractTypeMap ,
        out Dictionary<Type, List<Type>> nonAbstractToServiceTypeMap)
    {  
        // serviceToNonAbstractTypeMap = new Dictionary<Type, List<Type>>();
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

        /*
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
        */
    }

    static bool IsSubClassOrSelf(Type parent, Type child)
    {
        if (parent == child) return true;
        if (child.GetInterfaces().Contains(parent)) return true;
        if (child.IsSubclassOf(parent)) return true;

        return false;
    }
    
    // EXTENSION
 
    internal static bool IsServiceType(this Type type) => serviceTypes.Contains(type);

    internal static IEnumerable<Type> GetServicesOfNonAbstractType(Type type)
    {
        if (nonAbstractToServiceTypeMap.TryGetValue(type, out List<Type> value))
            foreach (Type serviceType in value)
                yield return serviceType;
    }  
}
}
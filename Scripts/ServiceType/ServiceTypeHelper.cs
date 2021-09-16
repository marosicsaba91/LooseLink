using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityServiceLocator
{
public static class ServiceTypeHelper
{ 
    public static readonly List<Type> allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).ToList();
    static readonly HashSet<Type> _serviceTypes;
    static readonly Dictionary<Type, List<Type>> _serviceToNonAbstractTypeMap;
    static readonly Dictionary<Type, List<Type>> _nonAbstractToServiceTypeMap; 

    static ServiceTypeHelper()
    {
        allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).ToList();
        _serviceTypes = AllGlobalServiceTypes(allTypes); 
        
        SetupServiceDictionaries(
            _serviceTypes,
            out _serviceToNonAbstractTypeMap, 
            out _nonAbstractToServiceTypeMap);
 
    }
    
    static HashSet<Type> AllGlobalServiceTypes(IEnumerable<Type> allTypes)
    {
        var result = new HashSet<Type>();
        foreach (Type type in allTypes)
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

    static bool IsSubClassOrSelf(Type parent, Type child)
    {
        if (parent == child) return true;
        if (child.GetInterfaces().Contains(parent)) return true;
        if (child.IsSubclassOf(parent)) return true;

        return false;
    }
    
    // EXTENSION

    public static Type initableType = typeof(IInitializable);
    
    internal static bool IsServiceType(this Type type) => _serviceTypes.Contains(type);

    internal static IEnumerable<Type> GetServicesOfNonAbstractType(Type type)
    {
        if (_nonAbstractToServiceTypeMap.TryGetValue(type, out List<Type> value))
            foreach (Type serviceType in value)
                yield return serviceType;
    }

    public static Type SearchTypeWithName(string name) => allTypes.FirstOrDefault(t => t.Name == name);

    public static Type SearchTypeWithFullName(string fullName) =>
        allTypes.FirstOrDefault(t => t.FullName == fullName);
}
}
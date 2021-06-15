using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
class GlobalServiceTypeProvider : IServiceTypeProvider
{
    static GlobalServiceTypeProvider _instance;

    internal static GlobalServiceTypeProvider Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GlobalServiceTypeProvider();
            }

            return _instance;
        }
    }

    GlobalServiceTypeProvider()
    { 
        List<Type> allAbstractServiceTypes = AllGlobalServiceTypes(ServiceTypeHelper.allTypes).ToList(); 
        
        ServiceTypeHelper.SetupServiceDictionaries(
            allAbstractServiceTypes,
            out Dictionary<Type, List<Type>> serviceToNonAbstractTypeMap, 
            out Dictionary<Type, List<Type>> nonAbstractToServiceTypeMap);

        ServiceToNonAbstractTypeMap = serviceToNonAbstractTypeMap;
        NonAbstractToServiceTypeMap = nonAbstractToServiceTypeMap;
    }
    
    static IEnumerable<Type> AllGlobalServiceTypes(IEnumerable<Type> allTypes)
    {
        foreach (Type type in allTypes)
        {
            if (type.ContainsGenericParameters) continue;
            var attribute = (GlobalServiceTypeAttribute)
                Attribute.GetCustomAttribute(type, typeof(GlobalServiceTypeAttribute), inherit: false);
            if (attribute == null) continue;
            if (type.IsSubclassOf(typeof(ServiceTypeProvider)))
            {
                if (type.IsAbstract || !type.IsClass) continue;
                var provider = (ServiceTypeProvider)  ScriptableObject.CreateInstance(type); 
                foreach (Type providedType in provider.LocalServiceTypes())
                    yield return providedType;
                Object.DestroyImmediate(provider);
            }
            else
                yield return type;
        }
    }

    public IEnumerable<Type> ServiceTypes
    {
        get
        {
            foreach (KeyValuePair<Type, List<Type>> pair in ServiceToNonAbstractTypeMap)
                yield return pair.Key;
        }
    }

    public Dictionary<Type, List<Type>> ServiceToNonAbstractTypeMap { get; }
    public Dictionary<Type, List<Type>> NonAbstractToServiceTypeMap { get; } 

}
}
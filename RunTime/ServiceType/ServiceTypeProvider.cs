using System;
using System.Collections.Generic;
using UnityEngine;

namespace LooseServices
{

public abstract class ServiceTypeProvider : ScriptableObject, IServiceTypeProvider
{
    Dictionary<Type, List<Type>> _serviceToNonAbstractTypeMap;
    Dictionary<Type, List<Type>> _nonAbstractToServiceTypeMap;
    public abstract IEnumerable<Type> LocalServiceTypes();

    public Dictionary<Type, List<Type>> ServiceToNonAbstractTypeMap
    {
        get
        {
            InitServiceTypeDictionaries();
            return _serviceToNonAbstractTypeMap;
        }
    }

    public Dictionary<Type, List<Type>> NonAbstractToServiceTypeMap
    {
        get
        {
            InitServiceTypeDictionaries();
            return _nonAbstractToServiceTypeMap;
        }
    }

    public IEnumerable<Type> ServiceTypes
    {
        get
        {
            InitServiceTypeDictionaries();
            foreach (Type globalServiceType in GlobalServiceTypeProvider.Instance.ServiceTypes)
                yield return globalServiceType;
            foreach (Type localServiceType in LocalServiceTypes())
                yield return localServiceType;
        }
    } 
 
    void InitServiceTypeDictionaries()
    {
        if (_serviceToNonAbstractTypeMap != null) return;
 
        ServiceTypeHelper.SetupServiceDictionaries(ServiceTypes,
            out _serviceToNonAbstractTypeMap, 
            out _nonAbstractToServiceTypeMap);

    }
}
}
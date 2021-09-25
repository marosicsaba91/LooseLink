using System;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
 
abstract class DynamicServiceSource
{
    List<Type> _allNonAbstractTypes;
    List<Type> _allAbstractTypes;
    List<Type> _possibleAdditionalTypes;
    Dictionary<Type, object> _typeToServiceOnSource; 
    ServiceSource _setting;
    bool _isDynamicTypeDataInitialized = false;
 
    public virtual Object LoadedObject { get; set; } // GameObject or ScriptableObject

    public Dictionary<Type, object> InstantiatedServices { get; private set; } =
        new Dictionary<Type, object>();

    public abstract Loadability Loadability { get; }

    public bool TryGetService(
        Type type,
        IServiceSourceSet set,
        object[] conditionTags,
        List<Tag> serializedTags,
        out object service,
        out bool newInstance)
    {
        newInstance = false;
        Loadability loadability = Loadability;
        if (loadability.type != Loadability.Type.Loadable && loadability.type!= Loadability.Type.AlwaysLoaded)
        {
            service = default;
            return false;
        }

        if (conditionTags!= null) 
        {
            var success = true;
            foreach (object tag in conditionTags)
            {
                if (tag == null) continue; 
                if (serializedTags.Any(serializedTag => serializedTag.TagObject.Equals(tag))) continue; 
                    
                success = false;
                break;
            }

            if (!success)
            {
                service = default;
                return false;
            }
        }

        if (LoadedObject == null)
        {
            Transform parentObject = null;

            if (NeedParentTransform)
            {
                parentObject = set != null && set.GetType().IsSubclassOf(typeof(Component))
                    ? ((Component) set).transform
                    : ServiceLocator.ParentObject;
            }
            
            LoadedObject = Instantiate(parentObject);
            newInstance = true;
            TryInitializeService();
        }

        if (!InstantiatedServices.ContainsKey(type))
            InstantiatedServices.Add(type, GetServiceFromServerObject(type, LoadedObject));

        service = InstantiatedServices[type];
        return true; 
    }

    void TryInitializeService()
    {
        if (LoadedObject == null) return;
        switch (LoadedObject)
        {
            case ScriptableObject so:
            {
                if (so is IInitializable initSo)
                    initSo.Initialize();
                break;
            }
            case GameObject go:
            {
                IInitializable[] initializables = go.GetComponents<IInitializable>();
                foreach (IInitializable initializable in initializables)
                    initializable.Initialize();
                break;
            }
        }
    }

    protected abstract bool NeedParentTransform { get; }

    protected abstract Object Instantiate(Transform parent);

    protected abstract object GetServiceFromServerObject(Type type, Object serverObject);
    
    public abstract string Name { get; }

    public abstract Object SourceObject { get; }

    public IReadOnlyList<Type> GetAllNonAbstractTypes()
    {
        InitDynamicTypeDataIfNeeded();
        return _allNonAbstractTypes;
    }

    public IReadOnlyList<Type> GetAllAbstractTypes()
    {
        InitDynamicTypeDataIfNeeded();
        return _allAbstractTypes;
    }

    public IReadOnlyList<Type> GetPossibleAdditionalTypes()
    {         
        InitDynamicTypeDataIfNeeded();
        return _possibleAdditionalTypes;
    }

    public object GetServiceOnSource(Type serviceType)
    { 
        InitDynamicTypeDataIfNeeded();
        return _typeToServiceOnSource[serviceType];
    } 

    void InitDynamicTypeDataIfNeeded()
    { 
        if (_isDynamicTypeDataInitialized) return;
        
        _allNonAbstractTypes = GetNonAbstractTypes();
        _allAbstractTypes = new List<Type>();
        _typeToServiceOnSource = new Dictionary<Type, object>(); 
        _possibleAdditionalTypes = new List<Type>(); 
        foreach (Type concreteType in _allNonAbstractTypes)
        { 
            object serviceInstanceOnSourceObject = GetServiceOnSourceObject(concreteType); 


            IEnumerable<Type> abstractTypes = ServiceTypeHelper.GetServicesOfNonAbstractType(concreteType)
                .Where(abstractType => !_allAbstractTypes.Contains(abstractType));

            foreach (Type abstractType in abstractTypes)
            {
                _allAbstractTypes.Add(abstractType);
                _typeToServiceOnSource.Add(abstractType, serviceInstanceOnSourceObject); 
            }
            
            
            foreach (Type subclass in AllPossibleAdditionalSubclassesOf(concreteType))
                _possibleAdditionalTypes.Add(subclass);
        }

        _isDynamicTypeDataInitialized = true;
    }

    IEnumerable<Type> AllPossibleAdditionalSubclassesOf(Type type, bool includeInterfaces = true )
    {        
        if(type == null)
            yield break;
        
        yield return type;
        if (includeInterfaces)
        {
            foreach (Type interfaceType in type.GetInterfaces())
                if (interfaceType != typeof(IInitializable))
                    yield return interfaceType;
        }

        Type baseType = type.BaseType;
        if(baseType == null ||
           baseType == typeof(ScriptableObject) ||
           baseType == typeof(Component) ||
           baseType == typeof(Behaviour) ||
           baseType == typeof(MonoBehaviour))
            yield break;
        
        foreach (Type b in AllPossibleAdditionalSubclassesOf(baseType, includeInterfaces: false ))
            yield return b;
    }

    protected abstract List<Type> GetNonAbstractTypes();
    public abstract object GetServiceOnSourceObject(Type type);
 
    public abstract ServiceSourceTypes SourceType { get; }
    public abstract IEnumerable<ServiceSourceTypes> AlternativeSourceTypes { get; }

    public void ClearInstancesAndCachedTypes()
    {
        LoadedObject = null;
        InstantiatedServices.Clear();
        ClearCachedTypes();
    }

    public void ClearCachedTypes()
    {
        if(Application.isPlaying)
            return;
        _isDynamicTypeDataInitialized = false;
    }

    public Texture Icon => FileIconHelper.GetIconOfObject(SourceObject);
}
}
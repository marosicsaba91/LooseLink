using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
[Serializable]
public class ServiceSource
{
    [SerializeField] bool enabled = true;
    [SerializeField] Object serviceSourceObject;
    [SerializeField] ServiceSourceTypes preferredSourceType;
    [SerializeField] internal List<SerializableType> additionalTypes = new List<SerializableType>();
    [FormerlySerializedAs("additionalTags")] [SerializeField] internal List<Tag> tags = new List<Tag>();
    [SerializeField] internal bool isTypesExpanded;
    [SerializeField] internal bool isTagsExpanded;

    DynamicServiceSource _dynamicSource;
    ServiceSourceSet _sourceSet;

    public ServiceSource(Object sourceObject = null, ServiceSourceTypes preferredType = ServiceSourceTypes.Non)
    {
        serviceSourceObject = sourceObject;
        preferredSourceType = preferredType;
        InitDynamicSource();
        SourceChanged();
    }

    public Object ServiceSourceObject
    {
        get => serviceSourceObject;
        set
        {
            if (serviceSourceObject == value) return;
            serviceSourceObject = value;
            InitDynamicSource();
            SourceChanged();
        }
    }
    
    public bool Enabled
    {
        get => enabled;
        set
        {
            if (enabled == value) return;
            enabled = value;
            if(enabled)
                InitDynamicSource();
            SourceChanged();
        }
    }


    public ServiceSourceTypes PreferredSourceType
    {
        get => preferredSourceType;
        set
        {
            if (preferredSourceType == value) return;
            preferredSourceType = value;
            InitDynamicSource();
            SourceChanged();
        }
    }

    public ServiceSourceTypes SourceType => GetDynamicServiceSource()?.SourceType ?? preferredSourceType;

    public bool IsServiceSource
    {
        get
        {
            InitDynamicIfNeeded();
            return _dynamicSource != null;
        }
    }

    public bool IsSourceSet
    {
        get
        {
            InitDynamicIfNeeded();
            return _sourceSet != null;
        }
    }

    public void ClearDynamicData()
    {
        if (serviceSourceObject is ServiceSourceSet set)
            set.ClearDynamicData();
        InitDynamicSource();
        SourceChanged();
    }
    
    internal void ClearDynamicData_NoSourceChange()
    {
        if (serviceSourceObject is ServiceSourceSet set)
            set.ClearDynamicData();
        InitDynamicSource();
    }

    public IReadOnlyList<Tag> GetTags() => tags;

    public bool TryAddType<T>() => TryAddType(typeof(T));

    public bool TryAddType(Type t)
    {
        if (t == null) return false;
        InitDynamicIfNeeded();

        if (_dynamicSource == null) return false;
        if (!_dynamicSource.GetPossibleAdditionalTypes().Contains(t)) return false;
        if (additionalTypes.Select(st => st.Type).Contains(t)) return false;

        additionalTypes.Add(new SerializableType {Type = t});
        ServiceLocator.Environment.InvokeEnvironmentChangedOnType(t);
        return true;
    }

    public bool RemoveType<T>() => RemoveType(typeof(T));
    public bool RemoveType(Type type)
    {
        SerializableType removable = additionalTypes.Find(st => st.Type == type);
        if (removable != null)
        {
            additionalTypes.Remove(removable); 
            ServiceLocator.Environment.InvokeEnvironmentChangedOnType(removable.Type);
        } 
        return removable != null;
    }
    
    public void AddTag(string tagString) => AddTag(new Tag(tagString));
    public void AddTag(Object tagObject) => AddTag(new Tag(tagObject));
    public void AddTag(object tagObject) => AddTag(new Tag(tagObject)); 
    
    internal void AddTag(Tag tag)
    {
        tags.Add(tag);
        SourceChanged();
    }

    public void AddTags(params object[] tagObjects)
    {
        foreach (object t in tagObjects)
        {
            Tag tag;
            if (t is string ts)
                tag = new Tag(ts);
            else if (t is Object to)
                tag = new Tag(to);
            else
                tag = new Tag(t);
            
            tags.Add(tag);
        }

        SourceChanged();
    }

     
    
    public bool RemoveTag(object tagObject)
    {
        if (tagObject == null) return false;
        Tag removable = tags.Find(st => st.TagObject.Equals(tagObject) );
        return RemoveTag(removable);
    }
    
    public bool RemoveTag(Tag removable)
    { 
        if (removable != null)
        {
            tags.Remove(removable);
            SourceChanged();
        }

        return removable != null;
    }
    
    public IEnumerable<Type> GetServiceTypesRecursively()
    {     
        if (serviceSourceObject == null) yield break; 
        InitDynamicIfNeeded();

        if ( IsServiceSource)
        {
            foreach (Type serviceTypes in _dynamicSource.GetAllAbstractTypes())
            {
                if (serviceTypes != null)
                    yield return serviceTypes;
            }

            foreach (SerializableType typeSetting in additionalTypes)
            {
                Type type = typeSetting.Type;
                if (type != null)
                    yield return type;
            }
        }else if (IsSourceSet)
        {
            foreach (ServiceSource subSource in _sourceSet.ServiceSources)
            foreach (Type type in subSource.GetServiceTypesRecursively())
                yield return type;
        }
    } 

    internal IEnumerable<ServiceTypeInfo> GetAllServicesWithName()
    {
        if (serviceSourceObject == null) yield break; 
        InitDynamicIfNeeded();
        
        if(_dynamicSource == null) yield break;
        foreach (Type serviceType in _dynamicSource.GetAllAbstractTypes())
        {
            if (serviceType != null)     
                yield return new ServiceTypeInfo{
                    type = serviceType, 
                    name = serviceType.Name, 
                    fullName = serviceType.FullName,
                    isMissing = false
                };
        }

        if (additionalTypes.IsNullOrEmpty())
            yield break;

        IReadOnlyList<Type> possibleTypes = _dynamicSource.GetPossibleAdditionalTypes();
        foreach (SerializableType typeSetting in additionalTypes)
        {
            Type type = typeSetting.Type;
            string name = type == null ? typeSetting.Name : type.Name;
            string fullName = type == null ? typeSetting.FullName : type.FullName;
            bool isMissing = type == null || !possibleTypes.Contains(type);
            yield return new ServiceTypeInfo{type = type, name = name, fullName = fullName, isMissing = isMissing};
        }
    }

    
    internal ServiceSourceSet GetServiceSourceSet ()
    { 
        if (serviceSourceObject == null) return null; 
        InitDynamicIfNeeded();
        if (_sourceSet == null || _sourceSet.automaticallyUseAsGlobalInstaller)
            return null;
        return _sourceSet;
    }

    internal string Name => GetDynamicServiceSource()?.Name;

    internal Texture Icon => GetDynamicServiceSource()?.Icon;
    internal Loadability Loadability => GetDynamicServiceSource()?.Loadability ?? Loadability.NoServiceSourceObject;
    internal DynamicServiceSource GetDynamicServiceSource()
    {
        if (serviceSourceObject == null) return null;
        InitDynamicIfNeeded();
        return _dynamicSource;
    }

    internal void LoadAllType()
    {
        DynamicServiceSource dynamicServiceSource = GetDynamicServiceSource();
        if (dynamicServiceSource == null)
            return;
        foreach (Type type in GetServiceTypesRecursively())
        {
            dynamicServiceSource.TryGetService(
                type, null,
                conditionTags: null,
                tags,
                out object _,
                out bool _); 
        }
    }
    
    DynamicServiceSource GetServiceSourceOf(Object obj, ServiceSourceTypes previousType) // NO SOURCE SET
    {
        if (obj is ScriptableObject so)
        {
            if (previousType == ServiceSourceTypes.FromScriptableObjectPrototype)
                return new DynamicServiceSourceFromScriptableObjectPrototype(so);
            return new DynamicServiceSourceFromScriptableObjectFile(so);
        }

        if (obj is GameObject gameObject)
        {
            if (gameObject.scene.name != null)
            {
                return new DynamicServiceSourceFromSceneObject(gameObject);
            }

            if (previousType == ServiceSourceTypes.FromPrefabPrototype)
                return new DynamicServiceSourceFromPrefabPrototype(gameObject);
            return new DynamicServiceSourceFromPrefabFile(gameObject); 
        }

        if (obj is MonoScript script)
        {
            Type scriptType = script.GetClass();

            if (scriptType == null) return null;
            if (scriptType.IsAbstract) return null;
            if (scriptType.IsGenericType) return null;

            if (scriptType.IsSubclassOf(typeof(ScriptableObject)))
                return new DynamicServiceSourceFromScriptableObjectType(scriptType);

            if (scriptType.IsSubclassOf(typeof(MonoBehaviour)))
                return new DynamicServiceSourceFromMonoBehaviourType(scriptType);
        }

        return null;
    }

    void SourceChanged() =>
        ServiceLocator.Environment.InvokeEnvironmentChangedOnSource(this);
    
 
        
    internal void InitDynamicIfNeeded()
    {
        bool initNeeded = _dynamicSource == null && _sourceSet == null;
        if (!initNeeded) return;
        InitDynamicSource();
    }
    
    internal void InitDynamicSource()
    { 
        _dynamicSource = null;
        _sourceSet = null;
        if (serviceSourceObject is ServiceSourceSet set)
        {
            if (!set.automaticallyUseAsGlobalInstaller)
                _sourceSet = set;
        }
        else
        { 
            _dynamicSource = GetServiceSourceOf(serviceSourceObject, preferredSourceType);
        }
    }
}
}
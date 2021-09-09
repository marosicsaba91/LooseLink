using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEditor;
using UnityEngine; 
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
[Serializable]
public class ServiceSource
{
    [SerializeField] internal bool enabled = true;
    [SerializeField] internal Object serviceSourceObject;
    [SerializeField] internal ServiceSourceTypes preferredSourceType;
    [SerializeField] internal List<SerializableType> additionalTypes = new List<SerializableType>();
    [SerializeField] internal List<SerializableTag> additionalTags = new List<SerializableTag>();
    [SerializeField] internal bool isTypesExpanded;
    [SerializeField] internal bool isTagsExpanded;

    DynamicServiceSource _dynamicSource;
    ServiceSourceSet _sourceSet;

    DateTime _dynamicContentLastGenerated = default;

    public Object ServiceSourceObject
    {
        get => serviceSourceObject;
        set
        {
            if (serviceSourceObject == value) return;
            serviceSourceObject = value;
            _dynamicSource = null;
        }
    }

    public ServiceSourceTypes PreferredSourceType
    {
        get => preferredSourceType;
        set
        {
            if (preferredSourceType == value) return;
            preferredSourceType = value;
            _dynamicSource = null;
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
        _dynamicSource = null;
        _sourceSet = null;
    }


    public IEnumerable<object> GetTags()
    {
        if (serviceSourceObject == null) yield break;
        InitDynamicIfNeeded();
        if (_dynamicSource != null)
            foreach (object serviceTypes in _dynamicSource?.GetDynamicTags())
            {
                if (serviceTypes != null)
                    yield return serviceTypes;
            }

        foreach (SerializableTag tagSetting in additionalTags)
        {
            object tag = tagSetting.TagObject;
            yield return tag;
        }
    }

    public bool TryAddType<T>() => TryAddType(typeof(T));

    public bool TryAddType(Type t)
    {
        if (t == null) return false;
        InitDynamicIfNeeded();

        if (_dynamicSource == null) return false;
        if (!_dynamicSource.GetPossibleAdditionalTypes().Contains(t)) return false;
        if (additionalTypes.Select(st => st.Type).Contains(t)) return false;

        additionalTypes.Add(new SerializableType {Type = t});
        return true;
    }

    public bool RemoveType<T>() => RemoveType(typeof(T));
    public bool RemoveType(Type type)
    {
        SerializableType removable = additionalTypes.Find(st => st.Type == type);
        if (removable != null)
            additionalTypes.Remove(removable);
        return removable != null;
    }
    
    public void AddTag(string tagString) => additionalTags.Add(new SerializableTag(tagString)); 
    public void AddTag(Object tagObject) => additionalTags.Add(new SerializableTag(tagObject));
    public void AddTag(object tagObject) => additionalTags.Add(new SerializableTag(tagObject));

    public bool RemoveTag(object tagObject)
    {
        SerializableTag removable = additionalTags.Find(st => st.TagObject.Equals(tagObject));
        if (removable != null)
            additionalTags.Remove(removable);
        return removable != null;
    }

    public IEnumerable<Type> GetServiceTypes()
    {     
        if (serviceSourceObject == null) yield break; 
        InitDynamicIfNeeded();
        
        if(_dynamicSource == null) yield break;
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
        if (_sourceSet == null || _sourceSet.useAsGlobalInstaller)
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
        foreach (Type type in GetServiceTypes())
        {
            dynamicServiceSource.TryGetService(
                type, null,
                conditionTags: null,
                additionalTags,
                out object _,
                out bool newInstance);
            
            if (newInstance) 
                ServiceLocator.InvokeLoadedInstancesChanged(); 
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

        
    void InitDynamicIfNeeded()
    {
        bool initNeeded = _dynamicSource == null && _sourceSet == null;
        if (!initNeeded) return;

        _dynamicContentLastGenerated = DateTime.Now;

        if (serviceSourceObject is ServiceSourceSet set)
        {
            if (!set.useAsGlobalInstaller)
                _sourceSet = set;
        }
        else
        { 
            _dynamicSource = GetServiceSourceOf(serviceSourceObject, preferredSourceType);
        }
    }
}
}
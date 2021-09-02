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
class ServiceSource 
{
    public bool enabled = true;
    public Object serviceSourceObject;
    public ServiceSourceTypes preferredSourceType;
    public List<SerializableType> additionalTypes = new List<SerializableType>();
    public List<SerializableTag> additionalTags = new List<SerializableTag>();

    DynamicServiceSource _dynamicSource;
    ServiceSourceSet _sourceSet;

    [SerializeField] internal bool isTypesExpanded;
    [SerializeField] internal bool isTagsExpanded;

    DateTime _dynamicContentLastGenerated = default;

    public DynamicServiceSource GetDynamicServiceSource()
    {
        if (serviceSourceObject == null) return null;
        InitIfNeeded();
        return _dynamicSource;
    }

    public bool IsSet
    {
        get
        {
            InitIfNeeded();
            return _sourceSet != null;
        }
    }

    public bool IsSource
    {
        get
        {
            InitIfNeeded();
            return _dynamicSource != null;
        }
    }

    public ServiceSourceSet GetServiceSourceSet ()
    { 
        if (serviceSourceObject == null) return null; 
        InitIfNeeded();
        if (_sourceSet == null || _sourceSet.useAsGlobalInstaller)
            return null;
        return _sourceSet;
    }

    public string Name => GetDynamicServiceSource()?.Name;

    public Texture Icon => GetDynamicServiceSource()?.Icon;
    public ServiceSourceTypes SourceType => GetDynamicServiceSource()?.SourceType ?? preferredSourceType;
    public Loadability Loadability => GetDynamicServiceSource()?.Loadability ?? Loadability.NoServiceSourceObject;

    void InitIfNeeded()
    { 
        bool initNeeded = _dynamicSource == null && _sourceSet == null;
        if(!initNeeded) return;
        
        _dynamicContentLastGenerated = DateTime.Now;

        if (serviceSourceObject is ServiceSourceSet set)
        {
            if (!set.useAsGlobalInstaller)
                _sourceSet = set;
        }
        else
            _dynamicSource = GetServiceSourceOf(serviceSourceObject, preferredSourceType);
    } 

    public void ClearDynamicData()
    {
        if (serviceSourceObject is ServiceSourceSet set)
            set.ClearDynamicData();
        _dynamicSource = null;
        _sourceSet = null; 
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
                return new DynamicServiceSourceFromSceneObject(gameObject);

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

    public IEnumerable<Type> GetServiceTypes()
    {     
        if (serviceSourceObject == null) yield break; 
        InitIfNeeded();
        
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
        InitIfNeeded();
        
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

    public IEnumerable<object> GetTags()
    {
        if (serviceSourceObject == null) yield break;
        InitIfNeeded();
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

    public void LoadAllType()
    {
        DynamicServiceSource dynamicServiceSource = GetDynamicServiceSource();
        if (dynamicServiceSource == null)
            return;
        foreach (Type type in GetServiceTypes())
        {
            dynamicServiceSource.TryGetService(
                type, null,
                conditionTags: null,
                out object service,
                out bool newInstance);
            
            if (newInstance) 
                ServiceLocator.InvokeLoadedInstancesChanged(); 
        }
    }
}
}
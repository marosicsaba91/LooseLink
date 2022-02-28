using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;
using Object = UnityEngine.Object;


namespace LooseLink
{
[Serializable]
public class ServiceSource
{
    [SerializeField] bool enabled = true;
    [SerializeField] Object serviceSourceObject;
    [SerializeField] ServiceSourceTypes preferredSourceType;
    [SerializeField] internal List<SerializableType> additionalTypes = new List<SerializableType>();

    [SerializeField] List<Tag> tags = new List<Tag>();

    [SerializeField] internal bool isTypesExpanded;
    [SerializeField] internal bool isTagsExpanded;
    [SerializeField] internal bool isConditionsExpanded;
    
    [SerializeField] SerializableType monoScriptType;

    DynamicServiceSource _dynamicSource;
    ServiceSourceSet _sourceSet;

    internal IServiceSourceProvider serviceSourceProvider;

    internal ServiceSource(
        Object sourceObject,
        IServiceSourceProvider serviceSourceProvider,
        ServiceSourceTypes preferredType = ServiceSourceTypes.Non)
    {
        ServiceSourceObject = sourceObject;
        PreferredSourceType = preferredType;
        this.serviceSourceProvider = serviceSourceProvider;
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
            monoScriptType  = new SerializableType(serviceSourceObject);
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
            if (enabled)
                InitDynamicIfNeeded();
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
            monoScriptType = new SerializableType(serviceSourceObject);
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

    internal string Name
    {
        get
        {
            DynamicServiceSource d = GetDynamicServiceSource();
            if (d != null) return d.Name;
            if (serviceSourceObject != null) return serviceSourceObject.name;
            return "Source Object Missing";
        }
    }

    internal Texture Icon => !IsSourceSet && !IsServiceSource
        ? IconHelper.ErrorIcon
        : IconHelper.GetIconOfObject(serviceSourceObject);

    internal Resolvability Resolvability => GetDynamicServiceSource()?.Resolvability ?? Resolvability.NoSourceObject;
    public IEnumerable<ServiceSourceTypes> AlternativeSourceTypes => GetDynamicServiceSource().AlternativeSourceTypes;
    public Object LoadedObject => GetDynamicServiceSource().LoadedObject;

    public List<Tag> SerializedTags =>
        !IsServiceSource ? null : 
        IsTypesAndTagsComingFromDifferentServiceSourceComponent ? GetDynamicServiceSource().ServerObject.Source.SerializedTags : 
        tags;

    public IEnumerable<object> DynamicTags =>
        !IsServiceSource ? null : GetDynamicServiceSource()?.GetDynamicTags();
    
    internal bool IsTypesAndTagsComingFromDifferentServiceSourceComponent
    {
        get
        {
            if (serviceSourceProvider == null) return false;
            ServerObject serverObject = GetDynamicServiceSource()?.ServerObject;
            if (serverObject == null) return false;
            return serverObject.gameObject != serviceSourceProvider.ProviderObject;
        }
    }

    public List<IServiceSourceCondition> Conditions => 
        IsTypesAndTagsComingFromDifferentServiceSourceComponent ? 
        GetDynamicServiceSource()?.ServerObject.Source.Conditions : 
        GetDynamicServiceSource()?.Conditions;


    public void ClearCachedTypes()
    {
        ClearCachedTypes_NoEnvironmentChangeEvent();
        SourceChanged();
    }

    internal void ClearCachedTypes_NoEnvironmentChangeEvent()
    {
        if (IsServiceSource)
            GetDynamicServiceSource().ClearCachedTypes();
        else if (IsSourceSet)
            _sourceSet.ClearDynamicData_NoEnvironmentChangeEvent();
        InitDynamicIfNeeded();
    }

    internal void ClearCachedInstancesAndTypes_NoEnvironmentChangeEvent()
    {
        if (IsServiceSource)
            GetDynamicServiceSource().ClearCachedInstancesAndTypes();
        else if (IsSourceSet)
            _sourceSet.ClearCachedInstancesAndTypes_NoEnvironmentChangeEvent();
    }


    internal bool TryAddServiceType(Type t)
    {
        if (t == null) return false;
        InitDynamicIfNeeded();

        if (_dynamicSource == null) return false;
        if (!_dynamicSource.GetPossibleAdditionalTypes().Contains(t)) return false;
        if (additionalTypes.Select(st => st.Type).Contains(t)) return false;

        additionalTypes.Add(new SerializableType(t));
        Services.Environment.InvokeEnvironmentChangedOnType(t);
        return true;
    }

    public bool RemoveServiceTypeType<T>()
    {
        return RemoveServiceTypeType(typeof(T));
    }

    public bool RemoveServiceTypeType(Type type)
    {
        SerializableType removable = additionalTypes.Find(st => st.Type == type);
        if (removable != null)
        {
            additionalTypes.Remove(removable);
            Services.Environment.InvokeEnvironmentChangedOnType(removable.Type);
        }

        return removable != null;
    }

    public void AddTag(string tagString)
    {
        AddTag(new Tag(tagString));
    }

    public void AddTag(Object tagObject)
    {
        AddTag(new Tag(tagObject));
    }

    public void AddTag(object tagObject)
    {
        AddTag(new Tag(tagObject));
    }

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
        Tag removable = tags.Find(st => st.TagObject.Equals(tagObject));
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

    internal ServiceSourceSet GetServiceSourceSet()
    {
        if (serviceSourceObject == null) return null;
        InitDynamicIfNeeded();
        if (_sourceSet == null || _sourceSet.automaticallyUseAsGlobalInstaller)
            return null;
        return _sourceSet;
    }

    DynamicServiceSource GetDynamicServiceSource()
    {
        if (serviceSourceObject == null) return null;
        InitDynamicIfNeeded();
        return _dynamicSource;
    }

    internal void ResolveAllServices()
    {
        DynamicServiceSource dynamicServiceSource = GetDynamicServiceSource();
        if (dynamicServiceSource == null)
            return;
        foreach (Type type in GetServiceTypesRecursively())
            dynamicServiceSource.TryGetService(type, provider: null, out object _);
    }

    DynamicServiceSource GetServiceSourceOf(Object sourceObject, ServiceSourceTypes previousType) // NO SOURCE SET
    {
        if (sourceObject == null) return null;
        if (sourceObject is ScriptableObject so)
        {
            if (previousType == ServiceSourceTypes.FromScriptableObjectPrototype)
                return new DynamicServiceSourceFromScriptableObjectPrototype(so);
            return new DynamicServiceSourceFromScriptableObjectFile(so);
        }

        Type type = sourceObject.GetType();
        if (type == typeof(GameObject))
        {
            var gameObject = (GameObject) sourceObject;
            if (gameObject.scene.name != null) return new DynamicServiceSourceFromSceneObject(gameObject);

            if (previousType == ServiceSourceTypes.FromPrefabFile)
                return new DynamicServiceSourceFromPrefabFile(gameObject);
            return new DynamicServiceSourceFromPrefabPrototype(gameObject);
        }

        if (monoScriptType?.Type != null)
        {
            Type scriptType = monoScriptType?.Type;

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


    public IEnumerable<Type> GetServiceTypesRecursively()
    {
        if (serviceSourceObject == null) yield break;
        InitDynamicIfNeeded();
        
        if(additionalTypes == null)
            additionalTypes = new List<SerializableType>();

        if (IsServiceSource)
        {
            if (IsTypesAndTagsComingFromDifferentServiceSourceComponent)
            {
                foreach (Type type in GetDynamicServiceSource().ServerObject.Source
                    .GetServiceTypesRecursively())
                    yield return type;
            }
            else
            {
                foreach (Type serviceTypes in _dynamicSource.GetDynamicServiceTypes())
                    if (serviceTypes != null)
                        yield return serviceTypes;

                foreach (SerializableType typeSetting in additionalTypes)
                {
                    Type type = typeSetting.Type;
                    if (type != null)
                        yield return type;
                }
            }
        }
        else if (IsSourceSet)
        {
            for (var i = 0; i < _sourceSet.SourceCount; i++)
            {
                ServiceSource subSource = _sourceSet.GetSourceAt(i);
                foreach (Type type in subSource.GetServiceTypesRecursively())
                    yield return type;
            }
        }
    }


    internal IEnumerable<ServiceTypeInfo> GetAllServiceInfos()
    {
        if (serviceSourceObject == null) yield break;
        InitDynamicIfNeeded();

        if (!IsServiceSource) yield break;

        if (IsTypesAndTagsComingFromDifferentServiceSourceComponent)
        {
            foreach (ServiceTypeInfo typeInfo in GetDynamicServiceSource().ServerObject.Source
                .GetAllServiceInfos())
                yield return typeInfo;
        }
        else
        {
            foreach (Type serviceTypes in _dynamicSource.GetDynamicServiceTypes())
                if (serviceTypes != null)
                    yield return new ServiceTypeInfo
                    {
                        type = serviceTypes,
                        name = serviceTypes.Name,
                        fullName = serviceTypes.FullName,
                        isMissing = false
                    };

            foreach (SerializableType typeSetting in additionalTypes)
            {
                Type type = typeSetting.Type;
                yield return new ServiceTypeInfo
                {
                    type = type,
                    name = typeSetting.Name,
                    fullName = typeSetting.FullName,
                    isMissing = type == null || !_dynamicSource.GetPossibleAdditionalTypes().Contains(type)
                };
            }
        }
    }

    void SourceChanged()
    {
        Services.Environment.InvokeEnvironmentChangedOnSource(this);
    }

    internal void InitDynamicIfNeeded()
    {
        if (serviceSourceObject == null)
            _dynamicSource = null;

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
            _dynamicSource = GetServiceSourceOf(serviceSourceObject, preferredSourceType);
    }

    public IReadOnlyList<Type> GetPossibleAdditionalTypes()
    {
        if (!IsServiceSource)
            return null;
        if (IsTypesAndTagsComingFromDifferentServiceSourceComponent)
            return null;
        return GetDynamicServiceSource().GetPossibleAdditionalTypes();
    }

    public IEnumerable<Type> GetDynamicServiceTypes()
    {
        if (!IsServiceSource) yield break;
        if (IsTypesAndTagsComingFromDifferentServiceSourceComponent)
            foreach (ServiceTypeInfo typeInfo in GetDynamicServiceSource().ServerObject.Source
                .GetAllServiceInfos())
                yield return typeInfo.type;
        else
            foreach (Type type in GetDynamicServiceSource().GetDynamicServiceTypes())
                yield return type;
    }

    public bool TryGetService(
        Type looseServiceType,
        IServiceSourceProvider provider,
        object[] tagConditions,
        out object service)
    {
        if (!tagConditions.IsNullOrEmpty())
        {
            var success = true;
            foreach (object conditionTag in tagConditions)
            {
                if (conditionTag == null) continue;
                if (SerializedTags.Any(serializedTag => serializedTag.TagObject.Equals(conditionTag))) continue;
                if (DynamicTags.Any(dynamicTags => dynamicTags.Equals(conditionTag))) continue;

                success = false;
                break;
            }

            if (!success)
            {
                service = default;
                return false;
            }
        }

        return GetDynamicServiceSource().TryGetService(looseServiceType, provider, out service);
    }
}
}
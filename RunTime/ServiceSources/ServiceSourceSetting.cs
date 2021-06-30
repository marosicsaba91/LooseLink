using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine; 
using Object = UnityEngine.Object;

namespace LooseServices
{
enum ServiceSourceTypes
{
    Non,
    
    FromSceneGameObject,
    FromPrefabFile,
    FromPrefabPrototype,

    FromScriptableObjectFile,
    FromScriptableObjectPrototype,

    FromMonoBehaviourType,
    FromScriptableObjectType,
}

[Serializable]
class ServiceSourceSetting 
{
    public bool enabled = true;
    public Object serviceSourceObject;
    public ServiceSourceTypes sourceType;
    public List<SerializableType> serializedTypes;
    public List<SerializableTag> serializedTags;

    ServiceSource _source;
    ServiceSourceSet _set;
    List<Type> _types;

    [SerializeField] internal bool isTypesExpanded;
    [SerializeField] internal bool isTagsExpanded;

    public ServiceSource GetServiceSource()
    { 
            if (serviceSourceObject == null) return null; 
            Init();
            return _source;
    }

    public ServiceSourceSet GetServiceSourceSet ()
    { 
        if (serviceSourceObject == null) return null; 
        Init();
        if (_set == null || _set.useAsGlobalInstaller)
            return null;
        return _set;
    }
    
    internal IEnumerable<ServiceSource> GetServiceSources(IServiceSourceSet set)
    {
        if (serviceSourceObject == null) yield break; 
        Init();

        if (_source != null)
        {
            if (_source.GetAllAbstractTypes(set).Any())
                yield return _source;
        }
        else if(_set != null)
        {
            foreach (ServiceSource source in _set.GetServiceSources())
                yield return source;
        }
    }

    void Init()
    {
        if (_source != null || _set != null) return;

        if (serviceSourceObject is ServiceSourceSet set)
        {
            if (!set.useAsGlobalInstaller)
                _set = set;
        }
        else
            _source = GetServiceSourceOf(serviceSourceObject, sourceType);
    }

    public void Clear()
    {
        if (serviceSourceObject is ServiceSourceSet set)
            set.Fresh();
        _source = null;
        _set = null;
    }

    ServiceSource GetServiceSourceOf(Object obj, ServiceSourceTypes previousType) // NO SOURCE SET
    {
        if (obj is ScriptableObject so)
        {
            if (previousType == ServiceSourceTypes.FromScriptableObjectPrototype)
                return new ServiceSourceFromScriptableObjectPrototype(so);
            return new ServiceSourceFromScriptableObjectInstance(so);
        }

        if (obj is GameObject gameObject)
        {
            if (gameObject.scene.name != null)
                return new ServiceSourceFromSceneObject(gameObject);

            if (previousType == ServiceSourceTypes.FromPrefabPrototype)
                return new ServiceSourceFromPrefabPrototype(gameObject);
            return new ServiceSourceFromPrefabFile(gameObject); 
        }

        if (obj is MonoScript script)
        {
            Type scriptType = script.GetClass();

            if (scriptType == null) return null;
            if (scriptType.IsAbstract) return null;
            if (scriptType.IsGenericType) return null;

            if (scriptType.IsSubclassOf(typeof(ScriptableObject)))
                return new ServiceSourceFromScriptableObjectType(scriptType);

            if (scriptType.IsSubclassOf(typeof(MonoBehaviour)))
                return new ServiceSourceFromMonoBehaviourType(scriptType);
        }

        return null;
    }
}
}
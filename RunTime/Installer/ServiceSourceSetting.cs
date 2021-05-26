using System;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace LooseServices
{
[Serializable]
class ServiceSourceSetting
{
    [FormerlySerializedAs("systemSourceObject")] [SerializeField] public Object serviceSourceObject;
    [SerializeField] public bool asPrototype;
     List<ServiceSource> _sources;

    internal IEnumerable<ServiceSource> GetServiceSources()
    {
        if (serviceSourceObject == null) yield break;
        _sources = _sources ?? GetServiceSourcesOf(serviceSourceObject, asPrototype).ToList();
        
        foreach (ServiceSource source in _sources)
            if(source.AllAbstractTypes.Any())
                yield return source; 
    }
 
    public void Clear()
    {
        if (serviceSourceObject is ServiceSourceSet set)
            set.Fresh();
        _sources = null;
    }
    
    IEnumerable<ServiceSource> GetServiceSourcesOf(Object obj, bool asPrototype)
    {
        if(obj is ServiceSourceSet set)
            foreach (ServiceSource source in set.GetServiceSources()) 
                yield return source;
        
        switch (obj)
        {  
            case ScriptableObject so:
                if (asPrototype)
                {   
                    yield return new ServiceSourceFromScriptableObjectPrototype
                    {
                        prototype = so, 
                        setting = this
                    };
                }
                else
                {
                    yield return new ServiceSourceFromScriptableObjectInstance
                    {
                        instance = so,
                        setting = this
                    };
                }

                break;
            case GameObject prefab when prefab.scene.name == null:
                
                if (asPrototype)
                {   
                    yield return new ServiceSourceFromPrefabPrototype
                    {
                        prototypePrefab = prefab,
                        setting = this
                    };
                }
                else
                {
                    yield return new ServiceSourceFromPrefabFile
                    {
                        prefabFile = prefab,
                        setting = this
                    };
                } 
                break;
            case GameObject goInScene :
                yield return new ServiceSourceFromSceneObject
                {
                    sceneGameObject = goInScene, 
                    setting = this
                };
                break;
        }
    }
}
}
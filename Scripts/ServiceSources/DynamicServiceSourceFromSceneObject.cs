using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
[Serializable]
class DynamicServiceSourceFromSceneObject : DynamicServiceSource
{
    public GameObject sceneGameObject;

    public DynamicServiceSourceFromSceneObject(GameObject gameObject)
    {
        sceneGameObject = gameObject;
    }
    
    public override Object LoadedObject { 
        get => sceneGameObject;
        set { }
    }

    protected override List<Type> GetNonAbstractTypes() => 
        sceneGameObject.GetComponents<Component>().Select(component => component.GetType()).ToList();

    public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromSceneGameObject;

    public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes { get { yield break; } }
     
    public override Loadability Loadability => sceneGameObject == null
        ? new Loadability(Loadability.Type.Error,  "No Scene Game Object") 
        : Loadability.AlwaysLoaded; 
    
    protected override bool NeedParentTransform => false;
    protected override Object Instantiate(Transform parent) => sceneGameObject;

    protected override object GetServiceFromServerObject(Type type, Object serverObject) => ((GameObject) serverObject).GetComponent(type);

    public override object GetServiceOnSourceObject(Type type) => sceneGameObject.GetComponent(type);
 
    public override string Name => sceneGameObject != null ? sceneGameObject.name : string.Empty;
    public override Object SourceObject => sceneGameObject;
     

}
}
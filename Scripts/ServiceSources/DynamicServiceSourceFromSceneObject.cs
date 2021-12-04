using System;
using System.Collections.Generic; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink
{

class DynamicServiceSourceFromSceneObject : DynamicServiceSourceFromGO
{ 

    public DynamicServiceSourceFromSceneObject(GameObject gameObject) : base(gameObject ) { }
    
    public override Object LoadedObject => gameObject;

    public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromSceneGameObject;

    public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes { get { yield break; } }
    
    protected override bool NeedParentTransformForLoad => false;
    protected override Object Instantiate(Transform parent) => gameObject;

    public sealed override Resolvability TypeResolvability => gameObject == null
        ? new Resolvability(Resolvability.Type.Error, "No GameObject") 
        : Resolvability.AlwaysResolved;
 
}
}
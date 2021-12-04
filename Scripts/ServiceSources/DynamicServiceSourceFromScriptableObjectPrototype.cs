using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink
{

class DynamicServiceSourceFromScriptableObjectPrototype : DynamicServiceSource
{
    public ScriptableObject prototype;
  
    protected override IEnumerable<Type> GetNonAbstractTypes()
    {
        if (prototype != null)
            yield return prototype.GetType();
    }

    internal DynamicServiceSourceFromScriptableObjectPrototype(ScriptableObject prototype)
    {
        this.prototype = prototype;
    }

    public override Resolvability TypeResolvability => prototype == null
        ? new Resolvability(Resolvability.Type.Error,  "No Prototype") 
        : Resolvability.Resolvable;

    public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromScriptableObjectPrototype;

    public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes 
    { get { yield return ServiceSourceTypes.FromScriptableObjectFile; } }
    
    protected override bool NeedParentTransformForLoad => false;
    protected override Object Instantiate(Transform parent) => Object.Instantiate(prototype);

    protected override object GetServiceFromServerObject(Type type, Object serverObject) => serverObject;
    public override object GetServiceOnSourceObject(Type type) => prototype;
 
    public override string Name => prototype != null ? prototype.name : string.Empty;
    public override Object SourceObject => prototype; 

}
}
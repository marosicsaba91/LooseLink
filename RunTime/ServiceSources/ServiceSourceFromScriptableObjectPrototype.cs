using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
[Serializable]
class ServiceSourceFromScriptableObjectPrototype : ServiceSource
{
    public ScriptableObject prototype;
 
    protected override List<Type> GetNonAbstractTypes(IServiceSourceSet set)
    { 
        var result = new List<Type>();
        if (prototype !=null)
            result.Add(prototype.GetType());

        return result;
    }

    internal ServiceSourceFromScriptableObjectPrototype(ScriptableObject prototype)
    {
        this.prototype = prototype;
    }

    public override Loadability Loadability => prototype == null
        ? new Loadability(Loadability.Type.Error,  "No Prototype") 
        : Loadability.Loadable;

    protected override void ClearService()
    {
        if (InstantiatedObject == null) return;
        if(Application.isPlaying)
            Object.Destroy(InstantiatedObject);
        else
            Object.DestroyImmediate(InstantiatedObject);
    }

    
    public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromScriptableObjectPrototype;

    public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes 
    { get { yield return ServiceSourceTypes.FromScriptableObjectFile; } }
    
    protected override bool NeedParentTransform => false;
    protected override Object Instantiate(Transform parent) => Object.Instantiate(prototype);

    protected override object GetService(Type type, Object instantiatedObject) => instantiatedObject;
    public override object GetServiceOnSourceObject(Type type) => prototype;
 
    public override string Name => prototype != null ? prototype.name : string.Empty;
    public override Object SourceObject => prototype; 

}
}
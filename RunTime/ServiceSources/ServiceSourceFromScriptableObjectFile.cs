using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{

[Serializable]
class ServiceSourceFromScriptableObjectInstance : ServiceSource
{
    public ScriptableObject instance;
    
    internal ServiceSourceFromScriptableObjectInstance(ScriptableObject instance)
    {
        this.instance = instance;
    }
 
    protected override List<Type> GetNonAbstractTypes(IServiceSourceSet set)
    { 
        var result = new List<Type>();
        if (instance != null)
            result.Add(instance.GetType());

        return result;
    }
    
    public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromScriptableObjectFile;

    public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes 
    { get { yield return ServiceSourceTypes.FromScriptableObjectPrototype; } }
    
    protected override void ClearService() { }

    public override Loadability Loadability => instance == null
        ? new Loadability(Loadability.Type.Error,  "No ScriptableObject instance") 
        : Loadability.Loadable;  
    
    protected override bool NeedParentTransform => false;
    protected override Object Instantiate(Transform parent) => instance;

    protected override object GetService(Type type, Object instantiatedObject) => instantiatedObject;

    public override object GetServiceOnSourceObject(Type type) => instance;
 
    public override string Name => instance != null ? instance.name : string.Empty;
    public override Object SourceObject => instance;
     

}
}
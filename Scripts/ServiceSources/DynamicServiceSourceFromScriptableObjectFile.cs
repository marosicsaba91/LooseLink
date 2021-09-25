using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{

[Serializable]
class DynamicServiceSourceFromScriptableObjectFile : DynamicServiceSource
{
    public ScriptableObject instance;
    
    internal DynamicServiceSourceFromScriptableObjectFile(ScriptableObject instance)
    {
        this.instance = instance;
    }
 
    public override Object LoadedObject { 
        get => instance;
        set { }
    } 
    protected override List<Type> GetNonAbstractTypes()
    { 
        var result = new List<Type>();
        if (instance != null)
            result.Add(instance.GetType());

        return result;
    }
    
    public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromScriptableObjectFile;

    public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes 
    { get { yield return ServiceSourceTypes.FromScriptableObjectPrototype; } }
     

    public override Loadability Loadability => instance == null
        ? new Loadability(Loadability.Type.Error,  "No ScriptableObject instance") 
        : Loadability.AlwaysLoaded;  
    
    protected override bool NeedParentTransform => false;
    protected override Object Instantiate(Transform parent) => instance;

    protected override object GetServiceFromServerObject(Type type, Object serverObject) => serverObject;

    public override object GetServiceOnSourceObject(Type type) => instance;
 
    public override string Name => instance != null ? instance.name : string.Empty;
    public override Object SourceObject => instance;
     

}
}
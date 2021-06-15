using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
[Serializable]
class ServiceSourceFromScriptableObjectType : ServiceSource
{
    public Type scriptableObjectType;
 
    public ServiceSourceFromScriptableObjectType(Type scriptableObjectType)
    {
        this.scriptableObjectType = scriptableObjectType;
    }

    protected override List<Type> GetNonAbstractTypes(IServiceSourceSet set)
    { 
        var result = new List<Type>();
        if (scriptableObjectType != null)
            result.Add(scriptableObjectType);

        return result;
    }
    public override Loadability Loadability => scriptableObjectType == null
        ? new Loadability(Loadability.Type.Error,  "No Type") 
        : Loadability.Loadable;

    protected override bool NeedParentTransform => false;
    protected override Object Instantiate(Transform parent)
    {
        Object obj = ScriptableObject.CreateInstance(scriptableObjectType);
        obj.name = $"New {scriptableObjectType.Name}";

        return obj;
    }
    
    public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromScriptableObjectType;

    public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes { get { yield break; } }

    protected override void ClearService()
    {
        if (InstantiatedObject == null) return;
        if(Application.isPlaying)
            Object.Destroy(InstantiatedObject);
        else
            Object.DestroyImmediate(InstantiatedObject);
    }

    protected override object GetService(Type type, Object instantiatedObject) => instantiatedObject;

    public override object GetServiceOnSourceObject(Type type) => null;
 
    public override string Name => scriptableObjectType != null ? scriptableObjectType.Name : string.Empty;
    public override Object SourceObject {
        get
        {
#if UNITY_EDITOR
            return TypeToFileHelper.GetObject(scriptableObjectType);
#else
            return null;
#endif
        }
    } 
}
}
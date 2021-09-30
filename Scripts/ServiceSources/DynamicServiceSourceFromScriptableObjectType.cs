using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{

class DynamicServiceSourceFromScriptableObjectType : DynamicServiceSource
{
    public readonly Type scriptableObjectType;
 
    public DynamicServiceSourceFromScriptableObjectType(Type scriptableObjectType)
    {
        this.scriptableObjectType = scriptableObjectType;
    }

    protected override IEnumerable<Type> GetNonAbstractTypes()
    {
        if (scriptableObjectType != null)
            yield return scriptableObjectType;
    }
    
    public override Resolvability TypeResolvability => scriptableObjectType == null
        ? new Resolvability(Resolvability.Type.Error,  "No Type") 
        : Resolvability.Resolvable;

    protected override bool NeedParentTransformForLoad => false;
    protected override Object Instantiate(Transform parent)
    {
        Object obj = ScriptableObject.CreateInstance(scriptableObjectType);
        obj.name = $"New {scriptableObjectType.Name}";

        return obj;
    }
    
    public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromScriptableObjectType;

    public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes { get { yield break; } }

    protected override object GetServiceFromServerObject(Type type, Object serverObject) => serverObject;

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
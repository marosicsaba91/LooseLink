using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink
{
class DynamicServiceSourceFromMonoBehaviourType : DynamicServiceSource
{
    public readonly Type monoBehaviourType;
 
    public DynamicServiceSourceFromMonoBehaviourType(Type monoBehaviourType)
    { 
        this.monoBehaviourType = monoBehaviourType;
    }

    protected override IEnumerable<Type> GetNonAbstractTypes()
    {
        if (monoBehaviourType != null)
            yield return monoBehaviourType;
    }

    public override Resolvability TypeResolvability
    {
        get
        {
            if (monoBehaviourType == null)
                return new Resolvability(Resolvability.Type.Error, "No Type");
            if(!Application.isPlaying)
                return new Resolvability(
                        Resolvability.Type.BlockedInEditorTime,
                    "You can't instantiate MonoBehaviour through Service Locator in Editor Time");
            return Resolvability.Resolvable;
        }
    }

    protected override bool NeedParentTransformForLoad => true;

    protected override Object Instantiate(Transform parent)
    {
        var go = new GameObject(monoBehaviourType.Name);
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.name = $"New {monoBehaviourType.Name}";
        go.AddComponent(monoBehaviourType);
        return go;
    }
    
    public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromMonoBehaviourType;

    public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes { get { yield break; } }

    /*
    protected override void ClearService()
    {
        if (LoadedObject == null) return;
        if(Application.isPlaying)
            Object.Destroy(LoadedObject);
        else
            Object.DestroyImmediate(LoadedObject);
    }
    */
 
    protected override object GetServiceFromServerObject(Type type, Object serverObject) => ((GameObject) serverObject).GetComponent(type);

    public override object GetServiceOnSourceObject(Type type) => null;
 
    public override string Name => monoBehaviourType != null ? monoBehaviourType.Name : string.Empty;
    public override Object SourceObject {
        get
        {
#if UNITY_EDITOR
            return TypeToFileHelper.GetObject(monoBehaviourType);
#else
            return null;
#endif
        }
    }
     
}
}
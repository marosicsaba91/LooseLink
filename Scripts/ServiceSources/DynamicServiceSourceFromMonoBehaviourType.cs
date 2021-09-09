using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
[Serializable]
class DynamicServiceSourceFromMonoBehaviourType : DynamicServiceSource
{
    public Type monoBehaviourType;
 
    public DynamicServiceSourceFromMonoBehaviourType(Type monoBehaviourType)
    {
        this.monoBehaviourType = monoBehaviourType;
    }

    protected override List<Type> GetNonAbstractTypes()
    { 
        var result = new List<Type>();
        if (monoBehaviourType != null)
            result.Add(monoBehaviourType);

        return result;
    }

    public override Loadability Loadability
    {
        get
        {
            if (monoBehaviourType == null)
                return new Loadability(Loadability.Type.Error, "No Type");
            if(!Application.isPlaying)
                return new Loadability(
                        Loadability.Type.Warning,
                    "You can't instantiate MonoBehaviour through Service Locator in Editor Time");
            return Loadability.Loadable;
        }
    }

    protected override bool NeedParentTransform => true;

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

    protected override void ClearService()
    {
        if (LoadedObject == null) return;
        if(Application.isPlaying)
            Object.Destroy(LoadedObject);
        else
            Object.DestroyImmediate(LoadedObject);
    }
 
    protected override object GetService(Type type, Object instantiatedObject) => ((GameObject) instantiatedObject).GetComponent(type);

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
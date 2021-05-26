using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
[Serializable]
class ServiceSourceFromMonoBehaviourType : ServiceSource
{
    public Type monoBehaviourType;
 
    public ServiceSourceFromMonoBehaviourType(Type monoBehaviourType)
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
 
    public override Loadability GetLoadability =>
        monoBehaviourType == null ? Loadability.Error
        : !Application.isPlaying ? Loadability.Warning
        : Loadability.Loadable;

    public override string NotInstantiatableReason =>  monoBehaviourType == null 
        ? "No Type"
        : "You can't instantiate MonoBehaviour through Loose Link in Editor Time";

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

    public override bool HasProtoTypeVersion => false;

    protected override void ClearService()
    {
        if (InstantiatedObject == null) return;
        if(Application.isPlaying)
            Object.Destroy(InstantiatedObject);
        else
            Object.DestroyImmediate(InstantiatedObject);
    }

    protected override object GetService(Type type, Object instantiatedObject) => ((GameObject) instantiatedObject).GetComponent(type);

    public override IService GetServiceOnSourceObject(Type type) => null;
 
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
    
    public override Texture Icon => FileIconHelper.GetIconOfSource(FileIconHelper.FileType.CsFile);
}
}
﻿using System;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
[Serializable]
class ServiceSourceFromPrefabPrototype : ServiceSource
{
    public GameObject prototypePrefab;
  
    protected override List<Type> GetNonAbstractTypes()
    {
        var result = new List<Type>();
        if (prototypePrefab == null) return result;
        
        IService[] services = prototypePrefab.GetComponents<IService>();
        result.AddRange(services.Select(service => service.GetType()));

        return result;
    }
  
    public override Loadability GetLoadability =>
        prototypePrefab == null ? Loadability.Error
        : !Application.isPlaying ? Loadability.Warning
        : Loadability.Loadable;
    
    
    public override string NotInstantiatableReason => prototypePrefab == null 
        ? "No Prefab"
        : "You can't instantiate prefab through Loose Link in Editor Time";

    public override bool HasProtoTypeVersion => true;

    protected override bool NeedParentTransform => true;

    protected override Object Instantiate(Transform parent)
    {
        GameObject go = Object.Instantiate(prototypePrefab, parent);
        go.name = prototypePrefab.name;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        return go;
    }

    protected override void ClearService()
    {
        if (InstantiatedObject == null) return;
        if(Application.isPlaying)
            Object.Destroy(InstantiatedObject);
        else
            Object.DestroyImmediate(InstantiatedObject);
    }

    protected override object GetService(Type type, Object instantiatedObject) =>
        ((GameObject) instantiatedObject).GetComponent(type);

    public override IService GetServiceOnSourceObject(Type type) =>
        (IService) prototypePrefab.GetComponent(type);
 
    public override string Name => prototypePrefab != null ? prototypePrefab.name : string.Empty;
    public override Object SourceObject => prototypePrefab;
    
    public override Texture Icon => FileIconHelper.GetIconOfSource(FileIconHelper.FileType.Prefab);

}
}
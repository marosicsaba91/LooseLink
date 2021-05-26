﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
[Serializable]
class ServiceSourceFromScriptableObjectPrototype : ServiceSource
{
    public ScriptableObject prototype;
 
    protected override List<Type> GetNonAbstractTypes()
    { 
        var result = new List<Type>();
        if (prototype !=null)
            result.Add(prototype.GetType());

        return result;
    }


    public override Loadability GetLoadability => prototype == null ? Loadability.Error : Loadability.Loadable;
    public override string NotInstantiatableReason => "No Prototype";

    protected override void ClearService()
    {
        if (InstantiatedObject == null) return;
        if(Application.isPlaying)
            Object.Destroy(InstantiatedObject);
        else
            Object.DestroyImmediate(InstantiatedObject);
    }

    public override bool HasProtoTypeVersion => true; 
    protected override bool NeedParentTransform => false;
    protected override Object Instantiate(Transform parent) => Object.Instantiate(prototype);

    protected override object GetService(Type type, Object instantiatedObject) => instantiatedObject;
    public override IService GetServiceOnSourceObject(Type type) => (IService) prototype;
 
    public override string Name => prototype != null ? prototype.name : string.Empty;
    public override Object SourceObject => prototype;
    public override Texture Icon => FileIconHelper.GetIconOfSource(FileIconHelper.FileType.ScriptableObject);

}
}
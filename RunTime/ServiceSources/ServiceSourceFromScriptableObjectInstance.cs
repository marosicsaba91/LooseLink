﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{

enum Loadability
{
    Loadable,
    Warning,
    Error
}

[Serializable]
class ServiceSourceFromScriptableObjectInstance : ServiceSource
{
    public ScriptableObject instance;
 
    protected override List<Type> GetNonAbstractTypes()
    { 
        var result = new List<Type>();
        if (instance != null)
            result.Add(instance.GetType());

        return result;
    } 
    public override bool HasProtoTypeVersion => true; 
    
    protected override void ClearService() { }

    public override Loadability GetLoadability => instance == null ? Loadability.Error : Loadability.Loadable;
    public override string NotInstantiatableReason => "No ScriptableObject instance";
    protected override bool NeedParentTransform => false;
    protected override Object Instantiate(Transform parent) => instance;

    protected override object GetService(Type type, Object instantiatedObject) => instantiatedObject;

    public override IService GetServiceOnSourceObject(Type type) => (IService) instance;
 
    public override string Name => instance != null ? instance.name : string.Empty;
    public override Object SourceObject => instance;
    
    
    public override Texture Icon => FileIconHelper.GetIconOfSource(FileIconHelper.FileType.ScriptableObject);

}
}
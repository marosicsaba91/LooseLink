﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
[Serializable]
class ServiceSourceFromPrefabFile : ServiceSource
{
    public GameObject prefabFile;

    protected override List<Type> GetNonAbstractTypes()
    {
        var result = new List<Type>();
        if (prefabFile == null) return result;
        
        IService[] services = prefabFile.GetComponents<IService>();
        result.AddRange(services.Select(service => service.GetType()));

        return result;
    }

    public override Loadability GetLoadability => prefabFile == null ? Loadability.Error : Loadability.Loadable;
    public override string NotInstantiatableReason =>"No Prefab";

    public override bool HasProtoTypeVersion => true;
    
    protected override void ClearService() { }
    protected override bool NeedParentTransform => false;

    protected override Object Instantiate(Transform parent) => prefabFile;

    protected override object GetService(Type type, Object instantiatedObject) =>
        ((GameObject) instantiatedObject).GetComponent(type);
     
    public override IService GetServiceOnSourceObject(Type type) => (IService) prefabFile.GetComponent(type);

    public override string Name => prefabFile != null ? prefabFile.name : string.Empty;
    public override Object SourceObject => prefabFile;

    public override Texture Icon => FileIconHelper.GetIconOfSource(FileIconHelper.FileType.Prefab);
    
    
}
}
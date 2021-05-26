﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
[Serializable]
class ServiceSourceFromSceneObject : ServiceSource
{
    public GameObject sceneGameObject;
  
    protected override List<Type> GetNonAbstractTypes()
    {
        var result = new List<Type>();
        if (sceneGameObject == null) return result;
        
        IService[] services = sceneGameObject.GetComponents<IService>();
        result.AddRange(services.Select(service => service.GetType()));

        return result;
    }

    public override bool HasProtoTypeVersion => false;
    
    protected override void ClearService() { }


    public override Loadability GetLoadability => sceneGameObject == null ? Loadability.Error : Loadability.Loadable;
    
    public override string NotInstantiatableReason => "No Scene Game Object";
    protected override bool NeedParentTransform => false;
    protected override Object Instantiate(Transform parent) => sceneGameObject;

    protected override object GetService(Type type, Object instantiatedObject) => ((GameObject) instantiatedObject).GetComponent(type);

    public override IService GetServiceOnSourceObject(Type type) =>
        (IService) sceneGameObject.GetComponent(type);
 
    public override string Name => sceneGameObject != null ? sceneGameObject.name : string.Empty;
    public override Object SourceObject => sceneGameObject;
    
    public override Texture Icon => FileIconHelper.GetIconOfSource(FileIconHelper.FileType.GameObject);

}
}
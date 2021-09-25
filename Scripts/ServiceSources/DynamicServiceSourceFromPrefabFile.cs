using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
[Serializable]
class DynamicServiceSourceFromPrefabFile : DynamicServiceSource
{
    public GameObject prefabFile;
    
    internal DynamicServiceSourceFromPrefabFile(GameObject prefabFile)
    {
        this.prefabFile = prefabFile;
    }
    
    public override Object LoadedObject { 
        get => prefabFile;
        set { }
    }

    protected override List<Type> GetNonAbstractTypes() =>
        prefabFile.GetComponents<Component>().Select(component => component.GetType()).ToList();
    public override Loadability Loadability => prefabFile == null
        ? new Loadability(Loadability.Type.Error, "No Prefab") 
        : Loadability.AlwaysLoaded; 

    public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromPrefabFile;

    public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes 
    { get { yield return ServiceSourceTypes.FromPrefabPrototype; } }
     
    protected override bool NeedParentTransform => false;

    protected override Object Instantiate(Transform parent) => prefabFile;

    protected override object GetServiceFromServerObject(Type type, Object serverObject) =>
        ((GameObject) serverObject).GetComponent(type);

    public override object GetServiceOnSourceObject(Type type)
    {
        Component result = prefabFile.GetComponent(type);
        return result;
    }

    public override string Name => prefabFile != null ? prefabFile.name : string.Empty;
    public override Object SourceObject => prefabFile;

    // public override Texture Icon => FileIconHelper.GetIconOfSource(FileIconHelper.FileType.Prefab); 
    
    
}
}
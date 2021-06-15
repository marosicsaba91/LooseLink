using System;
using System.Collections.Generic; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
[Serializable]
class ServiceSourceFromPrefabFile : ServiceSource
{
    public GameObject prefabFile;

    internal ServiceSourceFromPrefabFile(GameObject prefabFile)
    {
        this.prefabFile = prefabFile;
    }

    protected override List<Type> GetNonAbstractTypes(IServiceSourceSet set) => 
        set.ServiceTypeProvider.AllServiceComponents(prefabFile);

    public override Loadability Loadability => prefabFile == null
        ? new Loadability(Loadability.Type.Error, "No Prefab") 
        : Loadability.Loadable; 

    public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromPrefabFile;

    public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes 
    { get { yield return ServiceSourceTypes.FromPrefabPrototype; } }
    
    protected override void ClearService() { }
    protected override bool NeedParentTransform => false;

    protected override Object Instantiate(Transform parent) => prefabFile;

    protected override object GetService(Type type, Object instantiatedObject) =>
        ((GameObject) instantiatedObject).GetComponent(type);
     
    public override object GetServiceOnSourceObject(Type type) => prefabFile.GetComponent(type);

    public override string Name => prefabFile != null ? prefabFile.name : string.Empty;
    public override Object SourceObject => prefabFile;

    // public override Texture Icon => FileIconHelper.GetIconOfSource(FileIconHelper.FileType.Prefab); 
    
    
}
}
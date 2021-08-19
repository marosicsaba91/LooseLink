using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
[Serializable]
class DynamicServiceSourceFromPrefabPrototype : DynamicServiceSource
{
    public GameObject prototypePrefab;
  
    internal DynamicServiceSourceFromPrefabPrototype( GameObject prototypePrefab)
    {
        this.prototypePrefab = prototypePrefab;
    }
    
    protected override List<Type> GetNonAbstractTypes() => 
        prototypePrefab.GetComponents<Component>().Select(component => component.GetType()).ToList();
   
    public override Loadability Loadability
    {
        get
        {
            if (prototypePrefab == null)
                return new Loadability(Loadability.Type.Error, "No Prefab");
            if(!Application.isPlaying)
                return new Loadability(
                    Loadability.Type.Warning,
                    "You can't instantiate prefab through Loose Services in Editor Time");
            return Loadability.Loadable;
        }
    } 
    
    public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromPrefabPrototype;

    public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes 
    { get { yield return ServiceSourceTypes.FromPrefabFile; } }

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

    public override object GetServiceOnSourceObject(Type type) =>
        prototypePrefab.GetComponent(type);
 
    public override string Name => prototypePrefab != null ? prototypePrefab.name : string.Empty;
    public override Object SourceObject => prototypePrefab;
     

}
}
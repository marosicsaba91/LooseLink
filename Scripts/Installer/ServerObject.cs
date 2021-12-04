using UnityEngine;

namespace LooseLink
{ 
[DefaultExecutionOrder(order: -1000000)]
public class ServerObject : InstallerComponent
{ 
    [SerializeField, HideInInspector] ServiceSource source;
    [SerializeField] bool installAutomatically = false;

    internal ServiceSource Source {
        get
        {
            GameObject go = gameObject;
            if (source == null)
                source = new ServiceSource(go, this, ServiceSourceTypes.FromSceneGameObject);
            else
            {
                source.ServiceSourceObject = go;
                source.serviceSourceProvider = this;
                source.Enabled = true;
            }
 
            return source;
        }
    }

    public override void ClearDynamicData_NoEnvironmentChangeEvent() =>
        source.ClearCachedTypes_NoEnvironmentChangeEvent();

    public override bool InstallAutomatically => installAutomatically;
    public override bool IsSingleSourceProvider => true;
    public override int SourceCount => 1;
    public override ServiceSource GetSourceAt(int index) => index == 0 ? Source : null;
    
    public override bool ContainsSource(ServiceSource item) => item == Source;

    public override bool RemoveSource(ServiceSource item) => false;

    public override int IndexOfSource(ServiceSource item) => item == Source ? 0 : -1;
    public override void AddSource(ServiceSource item) { }

    public override void ClearSources() { }
    public override void SwapSources(int index1, int index2) { }
    
    public override void InsertSource(int index, ServiceSource item) { }
    public override void RemoveSourceAt(int index) { }
}
}
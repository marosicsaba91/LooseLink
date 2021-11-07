using System.Collections.Generic;
using MUtility;
using UnityEngine;  

namespace UnityServiceLocator
{
[DefaultExecutionOrder(order: -1000001)]
public class LocalServiceInstaller : InstallerComponent
{
    [SerializeField, HideInInspector] List<ServiceSource> serviceSources = new List<ServiceSource>();

    public override bool IsSingleSourceProvider => false;
    public override ServiceSource GetSourceAt(int index)
    {
        ServiceSource result = serviceSources[index];
        result.serviceSourceProvider = this;  
        return result;
    }
    
    public override int SourceCount => serviceSources.Count;

    public override void ClearDynamicData_NoEnvironmentChangeEvent()
    {
        foreach (ServiceSource source in serviceSources)
            source.ClearCachedTypes_NoEnvironmentChangeEvent(); 
    }

    public int ServiceSourceCount => serviceSources.Count; 
    public override void AddSource(ServiceSource item) => serviceSources.Add(item);

    public override void ClearSources() => serviceSources.Clear();

    public override bool ContainsSource(ServiceSource item) => serviceSources.Contains(item);

    public override bool RemoveSource(ServiceSource item) => serviceSources.Remove(item);

    public override int IndexOfSource(ServiceSource item) => serviceSources.IndexOf(item);

    public override void InsertSource(int index, ServiceSource item) => serviceSources.Insert(index, item);

    public override void RemoveSourceAt(int index) => serviceSources.RemoveAt(index);
    public override void SwapSources(int index1, int index2) => serviceSources.Swap(index1, index2);

}
}

using System;
using System.Collections.Generic;
using MUtility;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace LooseServices
{
abstract class LooseServiceInstallerComponent : MonoBehaviour, IServiceInstaller
{
    [SerializeField] FreshButton fresh;
    [FormerlySerializedAs("systemSources")] [SerializeField] List<ServiceSourceSetting> serviceSources = default;
    IEnumerable<ServiceSource> IServiceInstaller.GetServiceSources()
    {
        if(serviceSources == null) yield break;
        foreach (ServiceSourceSetting serviceSourceSetting in serviceSources)
        foreach (ServiceSource serviceSource in serviceSourceSetting.GetServiceSources())
            yield return serviceSource;
    }

    public string Name => gameObject != null ? name : null;
    public Object Obj => gameObject;

    void Fresh()
    {
        foreach (ServiceSourceSetting sourceSetting in serviceSources)
            sourceSetting.Clear();
    }
    
    [Serializable] class FreshButton :InspectorButton<LooseServiceInstallerComponent>
    {
        protected override void OnClick(LooseServiceInstallerComponent obj) => obj.Fresh();
        protected override string Text(LooseServiceInstallerComponent obj, string original) => "Clear Loaded Instances & Fresh";
    }
}
}
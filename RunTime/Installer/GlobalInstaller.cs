using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace LooseServices
{
[CreateAssetMenu(fileName = "Global Context Installer", menuName = "Loose Link/Global Context Installer", order = 0)]
public class GlobalInstaller : ScriptableObject, IServiceInstaller
{
    public bool isEnabledInstaller = true;
    
    [SerializeField] FreshButton fresh;
    [FormerlySerializedAs("systemSources")] [SerializeField] List<ServiceSourceSetting> serviceSources = default;
    [Space]
    [SerializeField] ResourcesWarningMessage warningMessage;
    IEnumerable<ServiceSource> IServiceInstaller.GetServiceSources()
    {
        if(serviceSources == null) yield break;
        foreach (ServiceSourceSetting serviceSourceSetting in serviceSources)
        foreach (ServiceSource serviceSource in serviceSourceSetting.GetServiceSources())
            yield return serviceSource;
    }

    public string Name => name;
    public Object Obj => this;

    void Fresh()
    {
        serviceSources = serviceSources ?? new List<ServiceSourceSetting>();
        foreach (ServiceSourceSetting sourceSetting in serviceSources)
            sourceSetting.Clear();
    }
    
    [Serializable] class FreshButton :InspectorButton<GlobalInstaller>
    {
        protected override void OnClick(GlobalInstaller obj) => obj.Fresh();
        protected override string Text(GlobalInstaller obj, string original) => "Clear Loaded Instances & Fresh";
    }
    
    [Serializable] class ResourcesWarningMessage : InspectorMessage<GlobalInstaller>
    {
        protected override IEnumerable<string> GetLines(GlobalInstaller parentObject)
        {
            if (!IsInResources(parentObject))
                yield return $"{nameof(GlobalInstaller)} files need to be in a Resources folder!";
        }

        bool IsInResources<T>(T scriptableObjectFile) where T:ScriptableObject => 
            Resources.LoadAll<T>(string.Empty).Any(so => so == scriptableObjectFile);

        protected override InspectorMessageType MessageType(GlobalInstaller parentObject) =>
            InspectorMessageType.Error;
    }
}
}
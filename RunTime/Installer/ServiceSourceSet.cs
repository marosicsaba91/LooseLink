using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
[CreateAssetMenu(fileName = "Service Source Set", menuName = "Loose Services/Service Source Set", order = 1)]
class ServiceSourceSet : ScriptableObject, IServiceSourceSet
{
    [SerializeField] ServiceTypeProvider typeProvider;
    public bool useAsGlobalInstaller = false;
     
    [SerializeField, HideInInspector] List<ServiceSourceSetting> serviceSources = default; 
    [SerializeField] ResourcesWarningMessage warningMessage;
    [HideInInspector] public int priority = 0;

    public List<ServiceSourceSetting> GetServiceSourceSettings() => serviceSources;

    public IEnumerable<ServiceSource> GetServiceSources()
    {
        if (serviceSources == null) yield break;
        foreach (ServiceSourceSetting serviceSourceSetting in serviceSources)
            if (serviceSourceSetting.enabled)
                foreach (ServiceSource serviceSource in serviceSourceSetting.GetServiceSources(this))
                    yield return serviceSource;
    }
    
    public string Name => name;
    public Object Obj => this;


    public IServiceTypeProvider ServiceTypeProvider => typeProvider.SelfOrDefault();

    public void Fresh()
    {
        serviceSources = serviceSources ?? new List<ServiceSourceSetting>();
        foreach (ServiceSourceSetting sourceSetting in serviceSources)
            sourceSetting.Clear();
    }
    
    
    [Serializable] class ResourcesWarningMessage : InspectorMessage<ServiceSourceSet>
    {
        protected override IEnumerable<string> GetLines(ServiceSourceSet parentObject)
        {
            if (parentObject.useAsGlobalInstaller && !IsInResources(parentObject))
                yield return $"{nameof(ServiceSourceSet)} files need to be in a Resources folder!";
        }

        bool IsInResources<T>(T scriptableObjectFile) where T:ScriptableObject => 
            Resources.LoadAll<T>(string.Empty).Any(so => so == scriptableObjectFile);

        protected override InspectorMessageType MessageType(ServiceSourceSet parentObject) =>
            InspectorMessageType.Error;
    }
    
    public static bool IsCircular(ServiceSourceSet set1, ServiceSourceSet set2)
    {
        if (set1.Contains(set2)) return true;
        if (set2.Contains(set1)) return true;
        return false;
    }

    bool Contains(ServiceSourceSet set)
    {
        if (this == set) return true;
		
        foreach (ServiceSourceSetting setting in serviceSources)
            if (setting.serviceSourceObject is ServiceSourceSet child)
                if (child.Contains(set))
                    return true;
		
        return false;
    }
}
}
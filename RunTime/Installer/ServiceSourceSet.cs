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
    public bool useAsGlobalInstaller = false;
     
    [SerializeField, HideInInspector] List<ServiceSource> serviceSources = default; 
    [SerializeField] ResourcesWarningMessage warningMessage;
    [HideInInspector] public int priority = 0;
    public List<ServiceSource> ServiceSources => serviceSources;

    
    public string Name => name;
    public Object Obj => this;
 

    public void ClearDynamicData()
    {
        serviceSources = serviceSources ?? new List<ServiceSource>();
        foreach (ServiceSource source in serviceSources)
            source.ClearDynamicData();
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
		
        foreach (ServiceSource setting in serviceSources)
            if (setting.serviceSourceObject is ServiceSourceSet child)
                if (child.Contains(set))
                    return true;
		
        return false;
    }
}
}
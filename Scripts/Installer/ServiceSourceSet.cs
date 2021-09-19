using System.Collections.Generic;
using System.Linq; 
using UnityEngine; 
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
[CreateAssetMenu(fileName = "New Service Source Set", menuName = "Service Source Set")]
public class ServiceSourceSet : ScriptableObject, IServiceSourceSet
{ 
    [SerializeField] internal bool automaticallyUseAsGlobalInstaller = false;
    [SerializeField] List<ServiceSource> serviceSources = new List<ServiceSource>(); 
    [SerializeField] int priority = 0; 
    
    public int Priority
    {
        get => priority;
        set
        {
            if(priority == value)
                return;
            priority = value;
            ServiceLocator.Environment.SortInstallers();
        }
    }
    
    public List<ServiceSource> ServiceSources => serviceSources;

    
    public string Name => name;
    public Object Obj => this;
 

    public void ClearDynamicData()
    {
        serviceSources = serviceSources ?? new List<ServiceSource>();
        foreach (ServiceSource source in serviceSources)
            source.ClearDynamicData_NoSourceChange();
    }
    
    internal bool IsInResources() => 
            Resources.LoadAll<ServiceSourceSet>(string.Empty).Any(so => so == this);

    public static bool IsCircular(ServiceSourceSet set1, ServiceSourceSet set2)
    {
        if (set1.ContainsSet(set2)) return true;
        if (set2.ContainsSet(set1)) return true;
        return false;
    }

    bool ContainsSet(ServiceSourceSet set)
    {
        if (this == set) return true;
		
        foreach (ServiceSource setting in serviceSources)
            if (setting.ServiceSourceObject is ServiceSourceSet child)
                if (child.ContainsSet(set))
                    return true;
		
        return false;
    }
}
}
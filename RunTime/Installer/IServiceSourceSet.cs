using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
interface IServiceSourceSet
{
    List<ServiceSource> ServiceSources { get; }
    string Name { get; }
    Object Obj { get;} 

    void ClearDynamicData();
}

static class ServiceSourceSetHelper
{
    public static IEnumerable<ServiceSource> GetValidSources(this IServiceSourceSet set) 
    {
        if (set.ServiceSources == null) yield break;
        foreach (ServiceSource serviceSource in set.ServiceSources)
            if (serviceSource.enabled)
                yield return serviceSource;
    }
}
}
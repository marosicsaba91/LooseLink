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
    public static IEnumerable<ServiceSource> GetEnabledValidSourcesRecursive(this IServiceSourceSet set)
    {
        if (set.ServiceSources == null) yield break;
        foreach (ServiceSource serviceSource in set.ServiceSources)
        {
            if (!serviceSource.enabled)
                continue;
            if (serviceSource.IsServiceSource)
                yield return serviceSource;
            else if (serviceSource.IsSourceSet)
                foreach (ServiceSource subSource in serviceSource.GetServiceSourceSet().GetEnabledValidSourcesRecursive())
                    yield return subSource;
        }
    }
    
    public static IEnumerable<ServiceSource> GetEnabledValidSources(this IServiceSourceSet set)
    {
        if (set.ServiceSources == null) yield break;
        foreach (ServiceSource serviceSource in set.ServiceSources)
        {
            if (!serviceSource.enabled)
                continue;
            if (serviceSource.IsServiceSource)
                yield return serviceSource;
            else if (serviceSource.IsSourceSet)
                yield return serviceSource;
        }
    }
}
}
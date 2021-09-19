using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
public interface IServiceSourceSet
{
    List<ServiceSource> ServiceSources { get; }
    string Name { get; }
    Object Obj { get;}
    void ClearDynamicData();
    int Priority { get; }
}

public static class ServiceSourceSetHelper
{
    internal static IEnumerable<ServiceSource> GetEnabledValidSourcesRecursive(this IServiceSourceSet set)
    {
        if (set.ServiceSources == null) yield break;
        foreach (ServiceSource serviceSource in set.ServiceSources)
        {
            if (!serviceSource.Enabled)
                continue;
            if (serviceSource.IsServiceSource)
                yield return serviceSource;
            else if (serviceSource.IsSourceSet)
            {
                ServiceSourceSet subSet = serviceSource.GetServiceSourceSet();
                if (subSet!= null && !subSet.automaticallyUseAsGlobalInstaller)
                    foreach (ServiceSource subSource in subSet.GetEnabledValidSourcesRecursive())
                        yield return subSource;
            }
        }
    }
    
    internal static IEnumerable<ServiceSource> GetEnabledValidSources(this IServiceSourceSet set)
    {
        if (set.ServiceSources == null) yield break;
        foreach (ServiceSource serviceSource in set.ServiceSources)
        {
            if (!serviceSource.Enabled)
                continue;
            if (serviceSource.IsServiceSource)
                yield return serviceSource;
            else if (serviceSource.IsSourceSet)
                yield return serviceSource;
        }
    }
    
    public static ServiceSource AddServiceSource(
        this IServiceSourceSet set,
        Object sourceObject, 
        ServiceSourceTypes preferredType = ServiceSourceTypes.Non)
    {
        var newServiceSource = new ServiceSource(sourceObject, preferredType);
        set.ServiceSources.Add(newServiceSource);
        return newServiceSource;
    }
    
    public static bool RemoveServiceSourceAt(
        this IServiceSourceSet set,
        int index)
    {
        return RemoveServiceSource(set, set.ServiceSources[index]);
    }
    
    public static bool RemoveServiceSource(this IServiceSourceSet set, ServiceSource sourceObject)
    {
        return set.ServiceSources.Remove(sourceObject);
    }
}
}
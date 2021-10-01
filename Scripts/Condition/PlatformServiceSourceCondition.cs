using System.Collections.Generic;
using MUtility;
using UnityEngine; 
namespace UnityServiceLocator
{
public class PlatformServiceSourceCondition : MonoBehaviour, IServiceSourceCondition
{
    [SerializeField] List<RuntimePlatform> platforms;

    public bool CanResolve()
    { 
        if (platforms.IsNullOrEmpty())
            return false;
 
        return platforms.Contains(Application.platform);  
    }
     
    public string GetConditionMessage()
    { 
        if (platforms.IsNullOrEmpty())
        {
            return "Source is available on NO platforms"; 
        }

        return CanResolve() ? 
            $"Current platform ({Application.platform}) is supported." :
            $"Source is only available on these platforms: { string.Join(", ", platforms)}.";
    }
}
}
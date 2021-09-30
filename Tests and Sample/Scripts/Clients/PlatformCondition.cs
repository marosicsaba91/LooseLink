using UnityEngine;
using UnityServiceLocator;

public class PlatformCondition : MonoBehaviour, IResolvingCondition
{
    [SerializeField] RuntimePlatform platform;

    public bool CanResolve(out string message)
    {
        RuntimePlatform currentPlatform = Application.platform;
        bool enable = currentPlatform == platform;

        message = enable ? null : $"Only available on {platform}";
        return enable;
    }
}

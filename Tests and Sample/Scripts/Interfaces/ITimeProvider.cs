using UnityServiceLocator;

[ServiceType]
public interface ITimeProvider
{
    float GetTime { get; } 

}
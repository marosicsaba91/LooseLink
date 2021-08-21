using UnityServiceLocator;

[ServiceType]
public interface ITimeProviderLongName
{
    float GetTime { get; }
}
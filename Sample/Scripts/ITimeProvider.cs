using UnityServiceLocator;

[ServiceType]
public interface ITimeProvider
{
    float GetTime { get; }
}

public interface ITimeProvider3
{
    float GetTime { get; }
}
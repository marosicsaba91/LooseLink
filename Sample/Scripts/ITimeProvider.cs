using UnityServiceLocator;

[ServiceType]
public interface ITimeProvider
{
    float GetTime { get; }
}

public interface ITimeProvider2
{
    float GetTime { get; }
}
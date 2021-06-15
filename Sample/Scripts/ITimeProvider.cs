using LooseServices;

[GlobalServiceType]
public interface ITimeProvider
{
    float GetTime { get; }
}
using LooseLink;

[ServiceType]
public interface ITimeProvider
{
    float GetTime { get; } 

}
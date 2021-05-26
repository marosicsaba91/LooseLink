using LooseServices;

public interface ITimeProvider : IService, ITagged
{
    float GetTime { get; }
}
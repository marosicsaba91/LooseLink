using UnityEngine;

namespace LooseServices
{
public interface ITag
{
    Color Color { get; }
    string Name { get; }

    object TagObject { get; }
}
}
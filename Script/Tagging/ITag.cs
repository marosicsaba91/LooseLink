using UnityEngine;

namespace UnityServiceLocator
{
public interface ITag
{
    Color Color { get; }
    string Name { get; }

    object TagObject { get; }
}
}
using UnityEngine;

namespace UnityServiceLocator
{
public interface ITag
{
    string Name { get; }

    object TagObject { get; }
}
}
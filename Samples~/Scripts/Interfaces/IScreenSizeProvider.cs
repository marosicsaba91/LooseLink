using System;
using UnityEngine;
using LooseLink;

[ServiceType]
public interface IScreenSizeProvider
{
    event Action<Vector2Int> ScreenSizeChanged;
    Vector2Int ScreenSize { get; }
}
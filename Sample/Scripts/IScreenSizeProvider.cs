using System;
using UnityServiceLocator;
using UnityEngine;

public interface IScreenSizeProvider
{
    event Action<Vector2Int> ScreenSizeChanged;
    Vector2Int ScreenSize { get; }
}
using System;
using System.Collections.Generic;
using LooseServices;
using UnityEngine;

[IgnoreService]
public class ScreenManager : MonoBehaviour, IScreenSizeProvider, ITagged
{
    Vector2Int _screenSize;
    
    public event Action<Vector2Int> ScreenSizeChanged;
    
    public Vector2Int ScreenSize => _screenSize;

    void Update()
    
    {
        if (_screenSize.x == Screen.width && _screenSize.y == Screen.height)
            return;
        
        _screenSize = new Vector2Int(Screen.width, Screen.height);
        ScreenSizeChanged?.Invoke(_screenSize);
    }

    public void Initialize() { }

    public IEnumerable<object> GetTags()
    {
        yield return GetType();
    }
    
}

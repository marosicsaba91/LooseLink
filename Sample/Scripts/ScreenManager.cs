using System;
using System.Collections.Generic;
using UnityServiceLocator;
using UnityEngine;

public class ScreenManager : MonoBehaviour, IScreenSizeProvider, ITagged
{
    Vector2Int _screenSize;
    [SerializeField] string id;
    [SerializeField] int age;
    public event Action<Vector2Int> ScreenSizeChanged;
    
    public Vector2Int ScreenSize => _screenSize;

    void Update()
    
    {
        if (_screenSize.x == Screen.width && _screenSize.y == Screen.height)
            return;
        
        _screenSize = new Vector2Int(Screen.width, Screen.height);
        ScreenSizeChanged?.Invoke(_screenSize);
    }
 

    public IEnumerable<object> GetTags()
    {
        yield return (nameof(id),id);
        yield return (nameof(age),age);
    }

    public bool EnableCustomTags => true;
}

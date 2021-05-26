using LooseServices;
using UnityEngine;

public class ScreenSizeUI : MonoBehaviour
{
    [SerializeField] TextMesh textMesh;
    IScreenSizeProvider _screenSizeProvider;
    
    void Awake()
    {
        _screenSizeProvider = Services.Get<IScreenSizeProvider>();
        _screenSizeProvider.ScreenSizeChanged += UpdateScreenSize;
    }

    void UpdateScreenSize(Vector2Int screenSize)
    {
        textMesh.text = $"Screen Size: {screenSize}";
    }
}

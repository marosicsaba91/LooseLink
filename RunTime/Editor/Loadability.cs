#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LooseServices
{
static class LoadabilityHelper
{
    static readonly Texture warningImage = EditorGUIUtility.IconContent("d_console.warnicon.sml").image;
    static readonly Texture errorImage = EditorGUIUtility.IconContent("d_console.erroricon.sml").image;
    
    internal static GUIContent GetGuiContent(this Loadability loadability, string text)
    {
        Texture image = loadability.ToImage();
        return new GUIContent("Can't Load", image, text);
    }

    static Texture ToImage(this Loadability loadability)
    {
        switch (loadability)
        {
            case Loadability.Warning:
                return warningImage;
            case Loadability.Error:
                return errorImage;
            default:
                return null;
        }
    }
}
}
#endif
#if UNITY_EDITOR

using System.Linq; 
using UnityEngine;

namespace UnityServiceLocator
{
static class ServicesEditorHelper
{
    
    static GUIStyle _smallLabelStyle;

    public static GUIStyle SmallLabelStyle => _smallLabelStyle = _smallLabelStyle ?? new GUIStyle
    {
        alignment = TextAnchor.MiddleCenter,
        padding = new RectOffset(left: 0, right: 0, top: 0, bottom: 0),
        normal = {textColor = GUI.skin.label.normal.textColor},
        fontSize = 10
    };

    public static string[] GenerateSearchWords(string searchText)
    {
        string[] rawKeywords = searchText.Split(',');
        return rawKeywords.Select(keyword => keyword.Trim().ToLower()).ToArray();
    }
}
}


#endif
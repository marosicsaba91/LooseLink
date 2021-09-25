#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnityServiceLocator
{

readonly struct Loadability
{
    public enum Type
    {
        AlwaysLoaded,
        Loadable,
        Warning,
        Error
    }

    public readonly Type type;
    public readonly string reason;

    public bool IsLoadable => type == Type.Loadable;
    public static Loadability AlwaysLoaded => new Loadability(Type.AlwaysLoaded);
    public static Loadability Loadable => new Loadability(Type.Loadable);
    public static Loadability NoServiceSourceObject => new Loadability(Type.Error, "No Service Source Object!");

    public Loadability(Type type)
    {
        this.type = type;
        reason = null;
    }
    
    public Loadability(Type type, string reason)
    {
        this.type = type;
        this.reason = reason;
    } 
}

static class LoadabilityHelper
{
    static readonly Texture _warningImage = EditorGUIUtility.IconContent("console.warnicon.sml").image;
    static readonly Texture _errorImage = EditorGUIUtility.IconContent("console.erroricon.sml").image;
    static readonly Texture _loadableImage = EditorGUIUtility.IconContent("FilterSelectedOnly").image;
    
    internal static GUIContent GetGuiContent(this Loadability loadability)
    {
        string text = loadability.type == Loadability.Type.Loadable ? "Loadable" : "Can't Load";
        Texture image = ToImage(loadability.type);
        
        return new GUIContent(text, image, loadability.reason);
    }

    static Texture ToImage(Loadability.Type loadabilityType)
    {
        switch (loadabilityType)
        {
            case Loadability.Type.Warning:
                return _warningImage;
            case Loadability.Type.Error:
                return _errorImage;
            case Loadability.Type.AlwaysLoaded:
            case Loadability.Type.Loadable:
                return _loadableImage;
            default:
                return null;
        }
    }
}
}
#endif
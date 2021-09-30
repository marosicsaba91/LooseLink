#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnityServiceLocator
{

readonly struct Resolvability
{
    public enum Type
    {
        AlwaysResolved,
        Resolvable,
        CantResolveNow,
        Error
    }

    public readonly Type type;
    public readonly string reason;

    public bool IsResolvable => type == Type.Resolvable;
    public static Resolvability AlwaysResolved => new Resolvability(Type.AlwaysResolved);
    public static Resolvability Resolvable => new Resolvability(Type.Resolvable); 
    public static Resolvability NoSourceObject => new Resolvability(Type.Error, "No Service Source Object!");
    public static Resolvability WrongSourcesObjectType  => new Resolvability(Type.Error, "Wrong Service Source Type!");

    public Resolvability(Type type)
    {
        this.type = type;
        reason = null;
    }
    
    public Resolvability(Type type, string reason)
    {
        this.type = type;
        this.reason = reason;
    } 
}

static class ResolvabilityHelper
{
    static readonly Texture warningImage = EditorGUIUtility.IconContent("console.warnicon.sml").image;
    static readonly Texture errorImage = EditorGUIUtility.IconContent("console.erroricon.sml").image;
    static readonly Texture resolvableImage = EditorGUIUtility.IconContent("FilterSelectedOnly").image;
    
    internal static GUIContent GetGuiContent(this Resolvability resolvability)
    {
        string text =
            resolvability.type == Resolvability.Type.Resolvable ? "Resolvable" :
            resolvability.type == Resolvability.Type.CantResolveNow ? "Can't Resolve Now" : "Can't Resolve";
        Texture image = ToImage(resolvability.type);
        
        return new GUIContent(text, image, resolvability.reason);
    }

    static Texture ToImage(Resolvability.Type resolvabilityType)
    {
        switch (resolvabilityType)
        {
            case Resolvability.Type.CantResolveNow:
                return warningImage;
            case Resolvability.Type.Error:
                return errorImage;
            case Resolvability.Type.AlwaysResolved:
            case Resolvability.Type.Resolvable:
                return resolvableImage;
            default:
                return null;
        }
    }
}
}
#endif
  using UnityEngine;

namespace LooseLink
{

readonly struct Resolvability
{
    public enum Type
    {
        AlwaysResolved,
        Resolvable,
        BlockedInEditorTime,
        BlockedByCondition,
        Error
    }

    public readonly Type type;
    public readonly string message;

    public bool IsResolvable => type == Type.Resolvable;
    public static Resolvability AlwaysResolved => new Resolvability(Type.AlwaysResolved);
    public static Resolvability Resolvable => new Resolvability(Type.Resolvable);
    public static Resolvability NoSourceObject => new Resolvability(Type.Error, "No Service Source Object!");
    public static Resolvability WrongSourcesObjectType => new Resolvability(Type.Error, "Wrong Service Source Type!");

    public Resolvability(Type type)
    {
        this.type = type;
        message = null;
    }

    public Resolvability(Type type, string message)
    {
        this.type = type;
        this.message = message;
    }

    public GUIContent GetGuiContent()
    {
        string text =
            type == Type.AlwaysResolved ? "Resolved" :
            type == Type.Resolvable ? "Resolvable" :
            type == Type.BlockedInEditorTime ? "Resolve is blocked in Editor Time" :
            type == Type.BlockedByCondition ? "Resolve is blocked by Condition" :
            type == Type.Error ? "Can't Resolve!" :
            null;
        Texture image = ToImage(type);

        return new GUIContent(text, image, message);
    }

    static Texture ToImage(Type resolvabilityType)
    {
        switch (resolvabilityType)
        {
            case Type.AlwaysResolved:
            case Type.Resolvable:
                return IconHelper.ResolvableIcon;
            case Type.BlockedByCondition: 
            case Type.BlockedInEditorTime:
                return IconHelper.BlockedIcon;
            case Type.Error:
                return IconHelper.ErrorIcon;
            default:
                return null;
        }
    }
}
}
 
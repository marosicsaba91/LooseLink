using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
static class FileIconHelper
{
    public enum FileType
    {
        Prefab,
        GameObject,
        ScriptableObject,
        CsFile,
    }

    public static Texture GetIconOfSource(FileType fileType)
    {
#if UNITY_EDITOR
        switch (fileType)
        {
            case FileType.Prefab:
                return EditorGUIUtility.IconContent("Prefab Icon").image;
            case FileType.GameObject:
                return EditorGUIUtility.IconContent("GameObject Icon").image;
            case FileType.ScriptableObject:
                return EditorGUIUtility.IconContent("ScriptableObject Icon").image;
            case FileType.CsFile:
                return EditorGUIUtility.IconContent("cs Script Icon").image;
            default:
                return null;
        }
#else
        return null;
#endif
    }
    
    public static GUIContent GetGUIContentToType(Type type)
    {
        Texture texture = GetIconOfType(type);
        if (texture == null)
            texture = GetIconOfSource(FileType.CsFile);
        string name = type.Name;
        var tooltip = $"{type.Name} ({TypeCategory(type)})";
        return new GUIContent(name, texture, tooltip); 
    }
    
    public static Texture GetIconOfObject(Object obj)
    {
#if UNITY_EDITOR
        return EditorGUIUtility.ObjectContent(obj, obj.GetType()).image;
#else
        return null;
#endif
    }
        
    public static Texture GetIconOfType(Type type)
    {
#if UNITY_EDITOR
        return EditorGUIUtility.ObjectContent(obj: null, type).image;
#else
        return null;
#endif
    }
    
    internal static string TypeCategory(Type type)
    {
        if (type.IsInterface)
            return "Interface";
        if (type.IsAbstract)
        {
            if (type.IsSubclassOf(typeof(MonoBehaviour)))
                return "Abstract MonoBehaviour class";
            if (type.IsSubclassOf(typeof(ScriptableObject)))
                return "Abstract ScriptableObject class";
            return "Abstract ScriptableObject class";
        }
        if (type.IsSubclassOf(typeof(MonoBehaviour)))
            return "MonoBehaviour class";
        if (type.IsSubclassOf(typeof(ScriptableObject)))
            return "ScriptableObject class";
        if (type.IsSubclassOf(typeof(Component)))
            return "Component class";

        if (type.IsClass)
            return "Class"; 
        
        return "Type";
    } 

}
}
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

}
}
using UnityEditor;
using UnityEngine;

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

}
}
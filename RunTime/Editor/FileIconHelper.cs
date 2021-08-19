using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
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
        var tooltip = $"{type.FullName} ({GetTypeCategory(type)})";
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
    
    internal static string GetTypeCategory(Type type)
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
    
    
    internal static string GetTooltipForISet(IServiceSourceSet iSet)
    {
        switch (iSet)
        {
            case SceneServiceInstaller _:
                return "Scene Service Installer: Service sources are available if the Component is in scene and enabled.";
            case ServiceSourceSet set when set.useAsGlobalInstaller:
                return "Global Service Installer: Service Sources are available always";
            case ServiceSourceSet _:
                return "Service Source Set";
            default:
                return unexpectedCategoryText;
        }
    }
 
    internal static string GetShortNameForServiceSource(ServiceSourceTypes sourceType)
    {
        switch (sourceType)
        {
            case ServiceSourceTypes.FromPrefabPrototype:
                return "Prefab Proto.";
            case ServiceSourceTypes.FromPrefabFile:
                return "Prefab File";
            case ServiceSourceTypes.FromScriptableObjectFile:
                return "SO. File";
            case ServiceSourceTypes.FromScriptableObjectPrototype:
                return "SO. Proto.";
            case ServiceSourceTypes.FromSceneGameObject:
                return "Scene GameObj.";
            case ServiceSourceTypes.FromScriptableObjectType:
                return "SO. script";
            case ServiceSourceTypes.FromMonoBehaviourType:
                return "MB. script"; 
            default:
                return unexpectedCategoryText;
        } 
    } 

    internal static string GetTooltipForServiceSource(ServiceSourceTypes sourceType)
    {
        switch (sourceType)
        {
            case ServiceSourceTypes.FromPrefabPrototype:
                return "Prefab Prototype: Service Creates an instance of a Prefab with Service Type component(s) in the root";
            case ServiceSourceTypes.FromPrefabFile:
                return "Prefab File: Service Gives back the Prefab File's Component";
            case ServiceSourceTypes.FromScriptableObjectFile:
                return "ScriptableObject File: ScriptableObject File instance that implements any Service Type";
            case ServiceSourceTypes.FromScriptableObjectPrototype:
                return "ScriptableObject Prototype: Creates a copy of a ScriptableObject file instance that implements any Service Type";
            case ServiceSourceTypes.FromSceneGameObject:
                return "Scene GameObject: GameObject in Scene with Service Type component(s)";
            case ServiceSourceTypes.FromScriptableObjectType:
                return "ScriptableObject script: Creates a new default instance of a Service Type ScriptableObject class";
            case ServiceSourceTypes.FromMonoBehaviourType:
                return "MonoBehaviour script: Creates a new GameObject with a MonoBehaviour class that implements a Service Type"; 
            default:
                return unexpectedCategoryText;
        } 
    } 
    
    const string unexpectedCategoryText =  "Error: Unexpected Category";

}
}
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
static class FileIconHelper
{
    static readonly Texture warningImage = EditorGUIUtility.IconContent("console.warnicon.sml").image;
    static readonly Texture errorImage = EditorGUIUtility.IconContent("console.erroricon.sml").image;
    static readonly Texture loadableImage = EditorGUIUtility.IconContent("FilterSelectedOnly").image;
    
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

    public static GUIContent GetGUIContentToType(ServiceTypeInfo typeInfo)
    { 
        Type type = typeInfo.type;
        string name = typeInfo.name;
        string fullName = typeInfo.fullName;
        bool isMissing = typeInfo.isMissing;

        if (type == null) 
            return new GUIContent(name, errorImage, $"Types \"{fullName}\" Is Missing!");
        
        Texture texture = isMissing ? errorImage : GetIconOfType(type);
        if (texture == null)
            texture = GetIconOfSource(FileType.CsFile);
        var tooltip = $"{fullName} ({GetTypeCategory(type)})";
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
                return "ScriptableObject Script: Creates a new default instance of a Service Type ScriptableObject class";
            case ServiceSourceTypes.FromMonoBehaviourType:
                return "MonoBehaviour Script: Creates a new GameObject with a MonoBehaviour class that implements a Service Type"; 
            default:
                return unexpectedCategoryText;
        } 
    }


    internal static string GetNameForServiceSourceCategory(ServiceSourceTypes sourceType)
    {
        switch (sourceType)
        {
            case ServiceSourceTypes.FromPrefabPrototype:
                return "Prefab Prototype";
            case ServiceSourceTypes.FromPrefabFile:
                return "Prefab File";
            case ServiceSourceTypes.FromScriptableObjectFile:
                return "ScriptableObj. File";
            case ServiceSourceTypes.FromScriptableObjectPrototype:
                return "ScriptableO. Proto.";
            case ServiceSourceTypes.FromSceneGameObject:
                return "Scene GameObject";
            case ServiceSourceTypes.FromScriptableObjectType:
                return "ScriptableO. Script";
            case ServiceSourceTypes.FromMonoBehaviourType:
                return "MonoBehaviour Script";
            default:
                return unexpectedCategoryText;
        }
    }

    internal static string GetShortNameForServiceSourceCategory(ServiceSourceTypes sourceType)
    {
        switch (sourceType)
        {
            case ServiceSourceTypes.FromPrefabPrototype:
                return "P. Proto.";
            case ServiceSourceTypes.FromPrefabFile:
                return "P. File";
            case ServiceSourceTypes.FromScriptableObjectFile:
                return "SO. File";
            case ServiceSourceTypes.FromScriptableObjectPrototype:
                return "SO. Proto.";
            case ServiceSourceTypes.FromSceneGameObject:
                return "Scene GO.";
            case ServiceSourceTypes.FromScriptableObjectType:
                return "SO. Script";
            case ServiceSourceTypes.FromMonoBehaviourType:
                return "MB. Script"; 
            default:
                return unexpectedCategoryText;
        } 
    } 
    
    const string unexpectedCategoryText =  "Error: Unexpected Category";

}
}
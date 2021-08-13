#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
class LooseServiceRow
{
    public enum RowCategory
    {
        Set,
        Source,
        Service
    }

    public RowCategory Category { get; }

    public IServiceSourceSet set; 
    public ServiceSource source;
    public Type type;
    public Object loadedInstance;
    public Loadability loadability;

    public LooseServiceRow(RowCategory category)
    {
        Category = category;
        set = null;
        source = null;
        type = null;
        
        loadedInstance = null;
        loadability = new Loadability(Loadability.Type.Loadable);
    }

    public Object SelectionObject
    {
        get
        {
            switch (Category)
            {
                case RowCategory.Set:
                    return set?.Obj;
                case RowCategory.Source:
                    return source.GetDynamicServiceSource()?.SourceObject;
                case RowCategory.Service:
                    return TypeToFileHelper.GetObject(type);
                default:
                    return null;
            }
        }
    }

    public override string ToString()
    {
        string i = set == null ? "-" : set.Name;
        string st = source == null ? "-" : source.GetType().ToString();
        string s = source == null ? "-" : source.Name; 
        string t = type == null ? "-" : type.Name; 
        return $"{i},{st}:{s},{t}";
    }
    
    public GUIContent GetGUIContent()
    {
        switch (Category)
        {
            case RowCategory.Set:
                return new GUIContent(set.Name,
                    FileIconHelper.GetIconOfObject(set.Obj)); 
            case RowCategory.Source:
                return new GUIContent(source.Name, source.Icon);
            case RowCategory.Service:
                Texture t = FileIconHelper.GetIconOfType(type);
                if (t == null)
                    t = FileIconHelper.GetIconOfSource(FileIconHelper.FileType.CsFile);
                return new GUIContent(type.ToString(), t);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    public GUIContent GetCategoryGUIContent()
    {
        switch (Category)
        {
            case RowCategory.Service :
                return GetCategoryGUIContentForService(type);
            case RowCategory.Source:
                return GetCategoryGUIContentForServiceSource(source);
            case RowCategory.Set:
                return GetCategoryGUIContentForInstaller(set); 
        } 
        throw new ApplicationException("Unexpected Category!");
    }


    internal static GUIContent GetCategoryGUIContentForInstaller(IServiceSourceSet iSet)
    {
        if(iSet is SceneServiceInstaller)
            return new GUIContent("Scene Inst.", installerIcon,
                "Scene Installer: Scene Context Service Installer");
        if (iSet is ServiceSourceSet set)
        {
            if (set.useAsGlobalInstaller)
                return new GUIContent("Global Inst.", installerIcon,
                    "Global Installer: Global Context Service Installer");
            return new GUIContent("Source Set", installerIcon,
                "Service Source Set");
        }

        return UnexpectedCategoryGUIContent;
    }

    internal static GUIContent GetCategoryGUIContentForService(Type type)
    {
        if (type.IsInterface)
            return new GUIContent("Interface", serviceIcon, "Service: Interface");
        if (type.IsAbstract)
        {
            if (type.IsSubclassOf(typeof(MonoBehaviour)))
                return new GUIContent("Abs. MB.", serviceIcon, "Service: Abstract MonoBehaviour class");
            if (type.IsSubclassOf(typeof(ScriptableObject)))
                return new GUIContent("Abs. SO.", serviceIcon, "Service: Abstract ScriptableObject class");
            return new GUIContent("Abs. class", serviceIcon, "Service: Abstract ScriptableObject class");
        }

        if (type.IsSubclassOf(typeof(MonoBehaviour)))
            return new GUIContent("MonoB.", serviceIcon, "Service: MonoBehaviour class");
        if (type.IsSubclassOf(typeof(ScriptableObject)))
            return new GUIContent("ScriptableObj.", serviceIcon, "Service: ScriptableObject class");
        if (type.IsSubclassOf(typeof(Component)))
            return new GUIContent("Component", serviceIcon, "Service: Component class");

        if (type.IsClass)
            return new GUIContent("Class", serviceIcon, "Service: Class"); 
        
        return UnexpectedCategoryGUIContent;
    }
    
    internal static GUIContent GetCategoryGUIContentForServiceSource(ServiceSource source, bool withIcons = true) =>
        GetCategoryGUIContentForServiceSource(source.SourceType, withIcons);

    internal static GUIContent GetCategoryGUIContentForServiceSource(ServiceSourceTypes sourceType, bool withIcons = true)
    {
        Texture image = withIcons ? serviceSourceIcon : null;
        switch (sourceType)
        {
            case ServiceSourceTypes.FromPrefabPrototype:
                return new GUIContent("Prefab Proto.", image,
                    "Service Source: Service Creates an instance of a Prefab with Service Type component(s) in the root");
            case ServiceSourceTypes.FromPrefabFile:
                return new GUIContent("Prefab File", image,
                    "Service Source: Service Gives back the Prefab File's Component");
            case ServiceSourceTypes.FromScriptableObjectFile:
                return new GUIContent("SO. File", image,
                    "Service Source: ScriptableObject File instance that implements any Service Type");
            case ServiceSourceTypes.FromScriptableObjectPrototype:
                return new GUIContent("SO. Proto.", image,
                    "Service Source: Creates a copy of a ScriptableObject file instance that implements any Service Type");
            case ServiceSourceTypes.FromSceneGameObject:
                return new GUIContent("Scene GO.", image,
                    "Service Source: GameObject in Scene with Service Type component(s)");
            case ServiceSourceTypes.FromScriptableObjectType:
                return new GUIContent("SO. script", image,
                    "Service Source: Creates a new default instance of a Service Type ScriptableObject class");
            case ServiceSourceTypes.FromMonoBehaviourType:
                return new GUIContent("MB. script", image,
                    "Service Source: Creates a new GameObject with a MonoBehaviour class that implements a Service Type");
            default:
                return UnexpectedCategoryGUIContent;
        }
    } 
    static GUIContent UnexpectedCategoryGUIContent => 
        new GUIContent("!!! Unknown !!!", image: null, "Error: Unexpected Category");

    public static readonly Texture installerIcon =EditorGUIUtility.IconContent("VerticalLayoutGroup Icon").image;
    public static readonly Texture serviceSourceIcon = EditorGUIUtility.IconContent("blendKeySelected").image;
    public static readonly Texture serviceIcon = EditorGUIUtility.IconContent("curvekeyframe").image;
} 
}
#endif
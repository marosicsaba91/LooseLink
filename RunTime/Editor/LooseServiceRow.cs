#if UNITY_EDITOR
using System; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
class LooseServiceRow
{
    public enum RowCategory
    {
        Installer,
        Source,
        Service
    }

    public RowCategory Category { get; }

    public IServiceInstaller installer; 
    public ServiceSource source;
    public Type type;
    public Object loadedInstance;
    public Loadability loadability;

    public LooseServiceRow(RowCategory category)
    {
        Category = category;
        installer = null;
        source = null;
        type = null;
        
        loadedInstance = null; 
        loadability = Loadability.Loadable; 
    }


    public Object SelectionObject
    {
        get
        {
            switch (Category)
            {
                case RowCategory.Installer:
                    return installer?.Obj;
                case RowCategory.Source:
                    return source.SourceObject;
                case RowCategory.Service:
                    return TypeToFileHelper.GetObject(type);
                default:
                    return null;
            }
        }
    }

    public override string ToString()
    {
        string i = installer == null ? "-" : installer.Name;
        string st = source == null ? "-" : source.GetType().ToString();
        string s = source == null ? "-" : source.Name;
        string hash = source?.setting == null ? "-" : source.setting.GetHashCode().ToString();
        string t = type == null ? "-" : type.Name; 
        return $"{i},{st}:{s}:{hash},{t}";
    }
    
    public GUIContent GetGUIContent()
    {
        switch (Category)
        {
            case RowCategory.Installer when installer == null:
                return new GUIContent("All Services instantiatable without installer");
            case RowCategory.Installer when installer.Obj is ScriptableObject:
                return new GUIContent(installer.Name,
                    FileIconHelper.GetIconOfSource(FileIconHelper.FileType.ScriptableObject));
            case RowCategory.Installer when installer.Obj is GameObject:
                return new GUIContent(installer.Name,
                    FileIconHelper.GetIconOfSource(FileIconHelper.FileType.GameObject));
            case RowCategory.Source:
                return new GUIContent(source.Name, source.Icon);
            case RowCategory.Service:
                return new GUIContent(type.ToString(), FileIconHelper.GetIconOfSource(FileIconHelper.FileType.CsFile));
            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    public GUIContent GetCategoryGUIContent()
    {
        switch (Category)
        {
            case RowCategory.Service when type.IsInterface:
                return new GUIContent("Interface", image: null,
                    "ILooseService: Interface");
            case RowCategory.Service when type.IsAbstract:
            {
                if (type.IsSubclassOf(typeof(MonoBehaviour)))
                    return new GUIContent("Abs. MB. class", image: null,
                        "ILooseService: Abstract MonoBehaviour class");
                if (type.IsSubclassOf(typeof(ScriptableObject)))
                    return new GUIContent("Abs. SO. class", image: null,
                        "ILooseService: Abstract ScriptableObject class");
                break;
            }
            case RowCategory.Service when type.IsSubclassOf(typeof(MonoBehaviour)):
                return new GUIContent("MB. class", image: null, "ILooseService: MonoBehaviour class");
            case RowCategory.Service when type.IsSubclassOf(typeof(ScriptableObject)):
                return new GUIContent("SO. class", image: null, "ILooseService: ScriptableObject class");
            case RowCategory.Source:
            {
                Type serviceSourceType = source.GetType();
                if (serviceSourceType == typeof(ServiceSourceFromPrefabPrototype))
                    return new GUIContent("Source: Prefab Prototype", image: null,
                        "Service Source: Creates an instance of a Prefab with ILooseService component(s) in the root");
                if (serviceSourceType == typeof(ServiceSourceFromPrefabFile))
                    return new GUIContent("Source: Prefab File", image: null,
                        "Service Source: Gives back the Prefab File's Component");
                if (serviceSourceType == typeof(ServiceSourceFromScriptableObjectInstance))
                    return new GUIContent("Source: SO. File", image: null,
                        "Service Source: File instance of a ScriptableObject that implements ILooseService");
                if (serviceSourceType == typeof(ServiceSourceFromScriptableObjectPrototype))
                    return new GUIContent("Source: SO. Proto.", image: null,
                        "Service Source: Creates a copy of a ScriptableObject file instance that implements ILooseService");
                if (serviceSourceType == typeof(ServiceSourceFromSceneObject))
                    return new GUIContent("Source: Scene GO.", image: null,
                        "Service Source: GameObject in Scene with ILooseService component(s)");
                if (serviceSourceType == typeof(ServiceSourceFromScriptableObjectType))
                    return new GUIContent("Source: SO. class", image: null,
                        "Service Source: Creates a new default instance of an ILooseService ScriptableObject class");
                if (serviceSourceType == typeof(ServiceSourceFromMonoBehaviourType))
                    return new GUIContent("Source: MB. class", image: null,
                        "Service Source: Creates a new default instance of an ILooseService MonoBehaviour class");
                break;
            } 
            case RowCategory.Installer when installer != null && installer.GetType() == typeof(SceneInstaller):
                return new GUIContent("Installer: Scene Context", image: null,
                    "Scene Context Service Installer");
            case RowCategory.Installer when installer != null && installer.GetType() == typeof(GlobalInstaller):
                return new GUIContent(text: "Installer: Global Context", image: null,
                    "Global Context Service Installer");
            default:
                return new GUIContent(text: "Installer: Non", image: null,
                    "All non abstract implementations of ILooseService, without any user defined installed Service Source");
        }

        throw new ApplicationException("Unexpected Category!");
    }

} 
}
#endif
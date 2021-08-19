#if UNITY_EDITOR
using System; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
class ServiceLocatorRow
{
    public enum RowCategory
    {
        Set,
        Source
    }

    public RowCategory Category { get; }

    public IServiceSourceSet set; 
    public ServiceSource source;
    public Type type;
    public Object loadedInstance;
    public Loadability loadability;

    public ServiceLocatorRow(RowCategory category)
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
                return new GUIContent(set.Name, FileIconHelper.GetIconOfObject(set.Obj), FileIconHelper.GetTooltipForISet(set)); 
            case RowCategory.Source:
                return new GUIContent(source.Name, source.Icon, FileIconHelper.GetTooltipForServiceSource(source.SourceType));
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
} 
}
#endif
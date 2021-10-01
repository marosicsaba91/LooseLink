#if UNITY_EDITOR 
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MUtility; 
using UnityEditor;
using UnityEngine; 

namespace UnityServiceLocator
{
class ServiceLocatorWindow : EditorWindow
{
    const string editorPrefsKey = "ServiceLocatorWindowState";
    static readonly Vector2 minimumSize = new Vector2(300, 100);

    ServiceSourceColumn _serviceSourcesColumn;
    ServiceSourceTypeColumn _typeColumn;
    ServicesColumn _servicesColumn;
    TagsColumn _tagsColumn;
    ResolveColumn _resolveColumn;
    GUITable<FoldableRow<ServiceLocatorRow>> _serviceTable; 
     
     
    [SerializeField] List<string> openedElements = new List<string>();
    public bool isSourceCategoryOpen = false; 
    public bool isTagsOpen = false; 
    public bool isServicesOpen = false; 
    public string searchTagsText = string.Empty;
    public string searchServicesText = string.Empty;
    public string searchServiceSourcesText = string.Empty;
    

    [MenuItem("Tools/Unity Service Locator")]
    static void ShowWindow()
    {
        var window = GetWindow<ServiceLocatorWindow>();
        window.minSize = minimumSize;
        window.titleContent = new GUIContent("Service Locator");
        window.Show();
    }
    
    public void OnEnable()
    {
        minSize = minimumSize;
        wantsMouseMove = true; 

        string data = EditorPrefs.GetString(
            editorPrefsKey, JsonUtility.ToJson(this, prettyPrint: false));
        JsonUtility.FromJsonOverwrite(data, this);
        Selection.selectionChanged += Repaint;
    }

    public void OnDisable()
    { 
        string data = JsonUtility.ToJson(this, prettyPrint: false);
        EditorPrefs.SetString(editorPrefsKey, data);
        Selection.selectionChanged -= Repaint;
    }

    void OnEnvironmentChanged()
    {
        if (!Application.isPlaying) return;
        Repaint();
    }

    void GenerateServiceSourceTable()
    {
        if (_serviceTable != null) return;
        
        _serviceSourcesColumn = new ServiceSourceColumn(this);
        _tagsColumn = new TagsColumn(this);
        _servicesColumn = new ServicesColumn(this);
        _typeColumn = new ServiceSourceTypeColumn(this);
        _resolveColumn = new ResolveColumn(this);
        var componentTypeColumns = new List<IColumn<FoldableRow<ServiceLocatorRow>>>
        {
            _serviceSourcesColumn, _typeColumn, _servicesColumn, _tagsColumn, _resolveColumn,
        };

        _serviceTable = new GUITable<FoldableRow<ServiceLocatorRow>>(componentTypeColumns, this)
        {
            emptyCollectionTextGetter = () => "No Service Source"
        };
    }

    void OnGUI()
    {
        EventType type = Event.current.type;
        if(type == EventType.Layout)
            return;
        GenerateServiceSourceTable(); 
         
        List<FoldableRow<ServiceLocatorRow>> rows = GenerateTreeView();
        _serviceTable.Draw(new Rect(x: 0, 0, position.width, position.height), rows);
    }
    

    List<FoldableRow<ServiceLocatorRow>> GenerateTreeView()
    {
        var roots = new List<TreeNode<ServiceLocatorRow>>();
        foreach (IServiceSourceProvider installer in ServiceLocator.GetAllInstallers())
        { 
            TreeNode<ServiceLocatorRow> installerNode = GetInstallerNode(installer, parentEnabled: true);
            if(installerNode!= null)
                roots.Add(installerNode);
        }

        return FoldableRow<ServiceLocatorRow>.GetRows(roots, openedElements, row => row.ToString());
    }

    TreeNode<ServiceLocatorRow> GetInstallerNode(IServiceSourceProvider provider, bool parentEnabled)
    {
        var installerRow = new ServiceLocatorRow(ServiceLocatorRow.RowCategory.Set)
        {
            enabled = provider.IsEnabled && parentEnabled,
            provider = provider
        };
        
        List<TreeNode<ServiceLocatorRow>> children = GetChildNodes(provider, provider.IsEnabled);
        if (provider.IsSingleSourceProvider)
            return children.FirstOrDefault();

        return new TreeNode<ServiceLocatorRow>(installerRow, children);

    }

    List<TreeNode<ServiceLocatorRow>> GetChildNodes(IServiceSourceProvider iProvider, bool enabled)
    {
        var nodes = new List<TreeNode<ServiceLocatorRow>>();
        ServiceSource[] sources = iProvider.GetSources().ToArray();

        bool noServiceSearch = _serviceSourcesColumn.NoSearch;
        bool noTypeSearch = _servicesColumn.NoSearch;
        bool noTagSearch = _tagsColumn.NoSearch;
        bool anySearch = !(noServiceSearch && noTypeSearch && noTagSearch);

        foreach (ServiceSource source in sources)
        {
            if (source.IsSourceSet)
            {
                ServiceSourceSet set = source.GetServiceSourceSet();
                if (set == null) continue;
                TreeNode<ServiceLocatorRow> installerNode = GetInstallerNode(set, enabled);
                nodes.Add(installerNode);
                
            }
            else
            {
                TreeNode<ServiceLocatorRow> sourceNode;
                if (source.IsServiceSource)
                {

                    source.ClearCachedTypes_NoEnvironmentChangeEvent();
                    var sourceRow = new ServiceLocatorRow(ServiceLocatorRow.RowCategory.Source)
                    {
                        enabled = enabled && source.Enabled,
                        provider = iProvider,
                        source = source,
                        resolvedInstance = source.LoadedObject,
                        resolvability = new Resolvability(Resolvability.Type.Resolvable)
                    };

                    var abstractTypes = new List<TreeNode<ServiceLocatorRow>>();
                    sourceNode = new TreeNode<ServiceLocatorRow>(sourceRow, abstractTypes);
                    sourceRow.resolvability = source.Resolvability;
                }
                else
                {
                    var sourceRow = new ServiceLocatorRow(ServiceLocatorRow.RowCategory.Source)
                    {
                        enabled = enabled && source.Enabled,
                        provider = null,
                        source = source,
                        resolvedInstance = null,
                        resolvability = source.ServiceSourceObject == null
                            ? Resolvability.NoSourceObject
                            : Resolvability.WrongSourcesObjectType
                    };

                    sourceNode = new TreeNode<ServiceLocatorRow>(sourceRow, children: null); 
                }

                bool serviceMatchSearch = _serviceSourcesColumn.ApplyServiceSourceSearch(source);
                bool typeMatchSearch = _servicesColumn.ApplyTypeSearchOnSource(iProvider, source);
                bool tagMatchSearch = _tagsColumn.ApplyTagSearchOnSource(iProvider, source);

                if (!anySearch)
                    nodes.Add(sourceNode);
                else if (serviceMatchSearch && tagMatchSearch && typeMatchSearch)
                    nodes.Add(sourceNode);
            }
        }
        return nodes;
    }

    public static int GetLastControlId()
    {
        FieldInfo getLastControlId = typeof (EditorGUIUtility).GetField("s_LastControlID", BindingFlags.Static | BindingFlags.NonPublic);
        if(getLastControlId != null)
            return (int)getLastControlId.GetValue(null);
        return 0;
    }
    
    public string[] GenerateSearchWords(string searchText)
    {  
        string[] rawKeywords = searchText.Split(',');
        return rawKeywords.Select(keyword => keyword.Trim().ToLower()).ToArray();
    }
}
}

#endif
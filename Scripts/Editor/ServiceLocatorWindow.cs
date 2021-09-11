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
    static readonly Vector2 minimumSize = new Vector2(500, 100);

    ServiceSourceColumn _serviceSourcesColumn;
    ServiceSourceTypeColumn _typeColumn;
    ServicesColumn _servicesColumn;
    TagsColumn _tagsColumn;
    ServiceLoadedColumn _loadedColumn;
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
        ServiceLocator.Environment.InstallersChanged += OnSceneContextInstallersChanged;
        ServiceLocator.Environment.LoadedInstancesChanged += Repaint;

        string data = EditorPrefs.GetString(
            editorPrefsKey, JsonUtility.ToJson(this, prettyPrint: false));
        JsonUtility.FromJsonOverwrite(data, this);
        Selection.selectionChanged += Repaint;
    }

    public void OnDisable()
    {
        ServiceLocator.Environment.InstallersChanged -= OnSceneContextInstallersChanged;
        ServiceLocator.Environment.LoadedInstancesChanged -= Repaint;

        string data = JsonUtility.ToJson(this, prettyPrint: false);
        EditorPrefs.SetString(editorPrefsKey, data);
        Selection.selectionChanged -= Repaint;
    }

    void OnSceneContextInstallersChanged()
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
        _loadedColumn = new ServiceLoadedColumn(this);
        var componentTypeColumns = new List<IColumn<FoldableRow<ServiceLocatorRow>>>
        {
            _serviceSourcesColumn, _typeColumn, _servicesColumn, _tagsColumn, _loadedColumn,
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
        foreach (IServiceSourceSet installer in ServiceLocator.GetInstallers())
        {
            TreeNode<ServiceLocatorRow> installerNode = GetInstallerNode(installer);
            roots.Add(installerNode);
        }

        return FoldableRow<ServiceLocatorRow>.GetRows(roots, openedElements, row => row.ToString());
    }

    TreeNode<ServiceLocatorRow> GetInstallerNode(IServiceSourceSet set)
    {
        var installerRow = new ServiceLocatorRow(ServiceLocatorRow.RowCategory.Set) {set = set};
        List<TreeNode<ServiceLocatorRow>> children = GetChildNodes(set);  
        return new TreeNode<ServiceLocatorRow>(installerRow, children); 
    }

    List<TreeNode<ServiceLocatorRow>> GetChildNodes(IServiceSourceSet iSet)
    {
        var nodes = new List<TreeNode<ServiceLocatorRow>>();
        ServiceSource[] sources = iSet.GetEnabledValidSources().ToArray();

        bool noServiceSearch = _serviceSourcesColumn.NoSearch;
        bool noTypeSearch = _servicesColumn.NoSearch;
        bool noTagSearch = _tagsColumn.NoSearch;
        bool anySearch = !(noServiceSearch && noTypeSearch && noTagSearch);
        
        foreach (ServiceSource source in sources)
        {
            if (!source.enabled) continue;
            if (source.ServiceSourceObject == null) continue;

            DynamicServiceSource dynamic = source.GetDynamicServiceSource();
            if (dynamic != null)
            {
                dynamic.ClearCachedTypes();
                var sourceRow = new ServiceLocatorRow(ServiceLocatorRow.RowCategory.Source)
                {
                    set = iSet,
                    source = source,
                    loadedInstance = dynamic.LoadedObject,
                    loadability = new Loadability(Loadability.Type.Loadable)
                };

                var abstractTypes = new List<TreeNode<ServiceLocatorRow>>();
                var sourceNode = new TreeNode<ServiceLocatorRow>(sourceRow, abstractTypes);
                sourceRow.loadability = source.Loadability;

                bool serviceMatchSearch = _serviceSourcesColumn.ApplyServiceSourceSearch(source);
                bool typeMatchSearch = _servicesColumn.ApplyTypeSearchOnSource(iSet, source);
                bool tagMatchSearch = _tagsColumn.ApplyTagSearchOnSource(iSet, source);

                if (!anySearch)
                    nodes.Add(sourceNode);
                else if (serviceMatchSearch && tagMatchSearch && typeMatchSearch)
                    nodes.Add(sourceNode);
            }
            else
            {
                ServiceSourceSet set = source.GetServiceSourceSet();
                if (set == null) continue;
                TreeNode<ServiceLocatorRow> installerNode = GetInstallerNode(set);
                nodes.Add(installerNode);
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
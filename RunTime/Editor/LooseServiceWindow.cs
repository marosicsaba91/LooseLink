#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MUtility; 
using UnityEditor;
using UnityEngine;

namespace LooseServices
{
class LooseServiceWindow : EditorWindow
{
    const string editorPrefsKey = "LooseServicesWindowState";

    LooseServiceFoldoutColumn _servicesColumn;
    LooseServiceTagsColumn _tagsColumn;
    LooseServiceLoadedColumn _loadedColumn;
    GUITable<FoldableRow<LooseServiceRow>> _serviceTable; 
     
     
    [SerializeField] List<string> openedElements = new List<string>();
    public bool isTagsOpen = false;
    public string searchTagText = string.Empty;
    public string searchServiceText = string.Empty; 

    [MenuItem("Tools/Loose Services")]
    static void ShowWindow()
    {
        var window = GetWindow<LooseServiceWindow>();
        window.titleContent = new GUIContent("Loose Services");
        window.Show();
    }
    
    public void OnEnable()
    {
        wantsMouseMove = true;
        Services.SceneContextInstallersChanged += OnSceneContextInstallersChanged;
        Services.LoadedInstancesChanged += Repaint;

        string data = EditorPrefs.GetString(
            editorPrefsKey, JsonUtility.ToJson(this, prettyPrint: false));
        JsonUtility.FromJsonOverwrite(data, this);
        Selection.selectionChanged += Repaint;
    }

    public void OnDisable()
    {
        Services.LoadedInstancesChanged -= OnSceneContextInstallersChanged;
        Services.LoadedInstancesChanged -= Repaint;

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
        
        _servicesColumn = new LooseServiceFoldoutColumn(this);
        _tagsColumn = new LooseServiceTagsColumn(this);
        _loadedColumn = new LooseServiceLoadedColumn(this);
        var componentTypeColumns = new List<IColumn<FoldableRow<LooseServiceRow>>>
        {
            _servicesColumn, _tagsColumn, _loadedColumn,
        };

        _serviceTable = new GUITable<FoldableRow<LooseServiceRow>>(componentTypeColumns, this)
        {
            emptyCollectionTextGetter = () => "No Service Source"
        };
    }

    void OnGUI()
    {
        GenerateServiceSourceTable(); 
        
        FoldoutColumn<LooseServiceRow>.inArray = false;
        List<FoldableRow<LooseServiceRow>> rows = GenerateTreeView();
        _serviceTable.Draw(new Rect(x: 0, 0, position.width, position.height), rows);
    }
    

    List<FoldableRow<LooseServiceRow>> GenerateTreeView()
    {
        var roots = new List<TreeNode<LooseServiceRow>>();
        foreach (IServiceInstaller installer in Services.GetInstallers())
        {
            var installerRow = new LooseServiceRow(LooseServiceRow.RowCategory.Installer)
            {
                installer = installer,
            };
            ServiceSource[] sources = installer.GetServiceSources().ToArray();
            if (sources.Any(source => source.AllNonAbstractTypes.Any()))
            {
                List<TreeNode<LooseServiceRow>> children = GetSourceNodes(installer, sources, hiddenElements: null);
                if (Enumerable.Any(children))
                {
                    var rootNode = new TreeNode<LooseServiceRow>(installerRow, children);
                    roots.Add(rootNode);
                }
            }
        }

        IEnumerable<ServiceSource> noSettingServiceSources = Services.GetNoInstallerSources();

        IEnumerable<ServiceSource> serviceSources = noSettingServiceSources as ServiceSource[] ?? noSettingServiceSources.ToArray();
        if (serviceSources.Any())
        {
            var noInstallerRow = new LooseServiceRow(LooseServiceRow.RowCategory.Installer);
            List<TreeNode<LooseServiceRow>> children = GetSourceNodes(installer: null, serviceSources);
            if (Enumerable.Any(children))
            {
                var noInstallerNode = new TreeNode<LooseServiceRow>(noInstallerRow, children);
                roots.Add(noInstallerNode);
            }
        }

        return FoldableRow<LooseServiceRow>.GetRows(roots, openedElements, row => row.ToString());

        List<TreeNode<LooseServiceRow>> GetSourceNodes(
            IServiceInstaller installer,
            IEnumerable<ServiceSource> nodes,
            ICollection<Type> hiddenElements = null
        )
        {
            var sources = new List<TreeNode<LooseServiceRow>>();
            foreach (ServiceSource source in nodes)
            {
                if (hiddenElements != null)
                    if (source.AllAbstractTypes.All(hiddenElements.Contains))
                        continue;

                var sourceRow = new LooseServiceRow(LooseServiceRow.RowCategory.Source)
                {
                    installer = installer,
                    source = source,
                    loadedInstance = source.InstantiatedObject,
                    loadability = Loadability.Loadable
                };
                var abstractTypes = new List<TreeNode<LooseServiceRow>>();
                var sourceNode = new TreeNode<LooseServiceRow>(sourceRow, abstractTypes); 
                sourceRow.loadability = source.GetLoadability;

                var typesShowed = 0;
                foreach (Type serviceType in source.AllAbstractTypes)
                {
                    var abstractTypeRow = new LooseServiceRow(LooseServiceRow.RowCategory.Service)
                    {
                        installer = installer,
                        source = source,
                        type = serviceType,
                    };
                    if (source.InstantiatedServices.ContainsKey(serviceType))
                        abstractTypeRow.loadedInstance = source.InstantiatedObject;


                    var abstractTypeNode = new TreeNode<LooseServiceRow>(abstractTypeRow, children: null);

                    if (!_servicesColumn.ApplyServiceSearchOnType(serviceType.ToString())) continue;
                    if (!_tagsColumn.ApplyTagSearchOnTagArray(source.GetTagsFor(serviceType))) continue;
                    abstractTypes.Add(abstractTypeNode);
                    typesShowed++; 
                }

                bool serviceMatchSearch = _servicesColumn.ApplyServiceSourceSearch(source);
                bool tagMatchSearch = _tagsColumn.ApplyTagSearchOnSource(source);
                bool noServiceSearch = _servicesColumn.NoSearch;
                bool noTagSearch =  _tagsColumn.NoSearch;
                bool anyTypesShown = typesShowed != 0;

                if (noTagSearch && noServiceSearch)
                {
                    if (anyTypesShown)
                        sources.Add(sourceNode);
                }
                else if ((noServiceSearch || serviceMatchSearch) && (noTagSearch || tagMatchSearch))
                    sources.Add(sourceNode);
            }

            return sources;
        }
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
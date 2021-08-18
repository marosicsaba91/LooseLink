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
    LooseServiceTypesColumn _typesColumn;
    LooseServiceTagsColumn _tagsColumn;
    LooseServiceLoadedColumn _loadedColumn;
    GUITable<FoldableRow<LooseServiceRow>> _serviceTable; 
     
     
    [SerializeField] List<string> openedElements = new List<string>();
    public bool isTagsOpen = false; 
    public string searchTagText = string.Empty;
    public string searchTypeText = string.Empty;
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
        ServiceLocator.SceneContextInstallersChanged += OnSceneContextInstallersChanged;
        ServiceLocator.LoadedInstancesChanged += Repaint;

        string data = EditorPrefs.GetString(
            editorPrefsKey, JsonUtility.ToJson(this, prettyPrint: false));
        JsonUtility.FromJsonOverwrite(data, this);
        Selection.selectionChanged += Repaint;
    }

    public void OnDisable()
    {
        ServiceLocator.LoadedInstancesChanged -= OnSceneContextInstallersChanged;
        ServiceLocator.LoadedInstancesChanged -= Repaint;

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
        _typesColumn = new LooseServiceTypesColumn(this);
        _loadedColumn = new LooseServiceLoadedColumn(this);
        var componentTypeColumns = new List<IColumn<FoldableRow<LooseServiceRow>>>
        {
            _servicesColumn, _typesColumn, _tagsColumn, _loadedColumn,
        };

        _serviceTable = new GUITable<FoldableRow<LooseServiceRow>>(componentTypeColumns, this)
        {
            emptyCollectionTextGetter = () => "No Service Source"
        };
    }

    void OnGUI()
    {
        GenerateServiceSourceTable(); 
         
        List<FoldableRow<LooseServiceRow>> rows = GenerateTreeView();
        _serviceTable.Draw(new Rect(x: 0, 0, position.width, position.height), rows);
    }
    

    List<FoldableRow<LooseServiceRow>> GenerateTreeView()
    {
        var roots = new List<TreeNode<LooseServiceRow>>();
        foreach (IServiceSourceSet installer in ServiceLocator.GetInstallers())
        {
            TreeNode<LooseServiceRow> installerNode = GetInstallerNode(installer);
            roots.Add(installerNode);
        }

        return FoldableRow<LooseServiceRow>.GetRows(roots, openedElements, row => row.ToString());
    }

    TreeNode<LooseServiceRow> GetInstallerNode(IServiceSourceSet set)
    {
        var installerRow = new LooseServiceRow(LooseServiceRow.RowCategory.Set) {set = set};
        List<TreeNode<LooseServiceRow>> children = GetChildNodes(set);  
        return new TreeNode<LooseServiceRow>(installerRow, children); 
    }

    List<TreeNode<LooseServiceRow>> GetChildNodes(IServiceSourceSet iSet)
    {
        var nodes = new List<TreeNode<LooseServiceRow>>();
        ServiceSource[] sources = iSet.GetValidSources().ToArray();
        foreach (ServiceSource source in sources)
        {
            if(!source.enabled) continue;
            if (source.serviceSourceObject == null) continue;
            if (source.GetDynamicServiceSource() != null)
            {
                DynamicServiceSource dynamicSource = source.GetDynamicServiceSource();
                var sourceRow = new LooseServiceRow(LooseServiceRow.RowCategory.Source)
                {
                    set = iSet,
                    source = source,
                    loadedInstance = source.GetDynamicServiceSource()?.InstantiatedObject,
                    loadability = new Loadability(Loadability.Type.Loadable)
                };

                var abstractTypes = new List<TreeNode<LooseServiceRow>>();
                var sourceNode = new TreeNode<LooseServiceRow>(sourceRow, abstractTypes);
                sourceRow.loadability = source.Loadability;

                /*
                var typesShowed = 0;
                foreach (Type serviceType in source.GetServiceTypes())
                {
                    var abstractTypeRow = new LooseServiceRow(LooseServiceRow.RowCategory.Service)
                    {
                        set = iSet,
                        source = source,
                        type = serviceType,
                    };


                    if (dynamicSource.InstantiatedServices.ContainsKey(serviceType))
                        abstractTypeRow.loadedInstance = dynamicSource.InstantiatedObject;


                    var abstractTypeNode = new TreeNode<LooseServiceRow>(abstractTypeRow, children: null);

                    if (!_servicesColumn.ApplyServiceSearchOnType(serviceType.ToString())) continue;
                    if (!_tagsColumn.ApplyTagSearchOnTagArray(source.GetTags())) continue;
                    if (!_typesColumn.ApplyTypeSearchOnTypeArray(source.GetServiceTypes())) continue;
                    abstractTypes.Add(abstractTypeNode);
                    typesShowed++;
                }
                */

                bool serviceMatchSearch = _servicesColumn.ApplyServiceSourceSearch(source);
                bool tagMatchSearch = _tagsColumn.ApplyTagSearchOnSource(iSet, source);
                bool typeMatchSearch = _typesColumn.ApplyTypeSearchOnSource(iSet, source);
                bool noServiceSearch = _servicesColumn.NoSearch;
                bool noTagSearch = _tagsColumn.NoSearch;
                bool noTypeSearch = _typesColumn.NoSearch;

                // if (typesShowed != 0)
                //    nodes.Add(sourceNode);
                // else 

                if (noServiceSearch && noTypeSearch && noTagSearch)
                    nodes.Add(sourceNode);

                else if ((noServiceSearch || serviceMatchSearch) &&
                         (noTagSearch || tagMatchSearch) &&
                         (noTypeSearch || typeMatchSearch))
                    nodes.Add(sourceNode);
            }
            else
            {
                ServiceSourceSet set = source.GetServiceSourceSet();
                if (set == null) continue;
                TreeNode<LooseServiceRow> installerNode = GetInstallerNode(set);
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
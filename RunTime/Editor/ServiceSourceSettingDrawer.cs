#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq; 
using MUtility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices.Editor
{
[CustomPropertyDrawer(typeof(ServiceSourceSetting))]
public class ServiceSourceSettingDrawer : PropertyDrawer
{
    GUITable<FoldableRow<LooseServiceRow>> _servicesTable;
    readonly List<string> _openedElements = new List<string>();
    List<FoldableRow<LooseServiceRow>> _lines;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        float space = EditorGUIUtility.standardVerticalSpacing; 
        const float foldoutW = 16;
        const float toggleW = 135;

        var sourceSetting = (ServiceSourceSetting) property.GetObjectOfProperty();
        ServiceSource[] sources = sourceSetting.GetServiceSources().ToArray();

        if (sources.All(source => !source.AllNonAbstractTypes.Any()))
            sources = new ServiceSource[0];
        
        if (_servicesTable == null)
            _servicesTable = GenerateServiceSourceTable(EditorWindow.focusedWindow);
        
        _servicesTable.window = EditorWindow.focusedWindow;

        var foldoutPos = new Rect(foldoutW + position.x, position.y, foldoutW, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutPos,property.isExpanded, GUIContent.none);
            

        float w = position.width - ( foldoutW + toggleW + space * 2);
        var objectPos = new Rect(position.x + foldoutW + space, position.y, w, EditorGUIUtility.singleLineHeight);
        Object obj = EditorGUI.ObjectField(
            objectPos,
            sourceSetting.serviceSourceObject,
            typeof(Object), 
            allowSceneObjects: true);

        bool asPrototype = sourceSetting.asPrototype;

        GUIContent categoryContent;
        switch (sources.Length)
        {
            case 0:
                categoryContent = new GUIContent("NO SERVICE SOURCE");
                break;
            case 1:
                categoryContent = GenerateRow(sources[0]).node.GetCategoryGUIContent();
                break;
            default:
                categoryContent = new GUIContent($"Source Set ({sources.Length})");
                break;
        }

        _lines = sources.Length == 1 ? GetAbstractTypeRows(sources) : GenerateTreeView(sources);

        var togglePos = new Rect(objectPos.xMax + space, position.y, toggleW, EditorGUIUtility.singleLineHeight);
        if (sources.Length == 1 && sources[0].HasProtoTypeVersion)
        {
            if (GUI.Button(togglePos, GUIContent.none))
                asPrototype = !asPrototype;
        }

        GUI.Label(togglePos, categoryContent, LooseServiceFoldoutColumn.CategoryStyle);
        
        
        // Object changed
        if (obj != sourceSetting.serviceSourceObject || asPrototype != sourceSetting.asPrototype )
        {
            Undo.RecordObject(property.serializedObject.targetObject, "Setting Object Changed");

            if (property.serializedObject.targetObject is ServiceSourceSet set1 && obj is ServiceSourceSet set2)
            {
                if (!ServiceSourceSet.IsCircular(set1, set2))
                    sourceSetting.serviceSourceObject = obj;
            }else
                sourceSetting.serviceSourceObject = obj;

            sourceSetting.asPrototype = asPrototype;
            sourceSetting.Clear();
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }

        if( !property.isExpanded) return;
        var sourcesPos = new Rect(
            position.x + foldoutW +space,
            position.y + EditorGUIUtility.singleLineHeight,
            position.width - foldoutW-space,
            position.height - EditorGUIUtility.singleLineHeight);
        
        FoldoutColumn<LooseServiceRow>.inArray = true;
        _servicesTable.Draw(sourcesPos, _lines);
         
    }

    List<FoldableRow<LooseServiceRow>> GetAbstractTypeRows(ServiceSource[] sources)
    {
        IEnumerable<TreeNode<LooseServiceRow>> a = GetAbstractTypeNodes(sources[0]);
        return FoldableRow<LooseServiceRow>.GetRows(a, _openedElements, row => row.ToString());
    }

    static GUITable<FoldableRow<LooseServiceRow>> GenerateServiceSourceTable( EditorWindow window)
    { 
        var componentTypeColumns = new List<IColumn<FoldableRow<LooseServiceRow>>>
        {
            new LooseServiceFoldoutColumn(new ColumnInfo
            {
                fixWidth = 0,
                relativeWidthWeight = 1,
            }),
        };

        return new GUITable<FoldableRow<LooseServiceRow>>(componentTypeColumns, window)
        {
            emptyCollectionTextGetter = () => "No Service Source",
            drawHeader = false
        };
    }

    List<FoldableRow<LooseServiceRow>> GenerateTreeView(IEnumerable<ServiceSource> sources)
    {
        var roots = new List<TreeNode<LooseServiceRow>>();
        foreach (ServiceSource source in sources)
            roots.Add(GenerateRow(source));

        return FoldableRow<LooseServiceRow>.GetRows(roots, _openedElements, row => row.ToString());
    }

    TreeNode<LooseServiceRow> GenerateRow(ServiceSource source)
    {
        var sourceRow = new LooseServiceRow(LooseServiceRow.RowCategory.Source)
        {
            installer = null,
            source = source,
        };
        List<TreeNode<LooseServiceRow>> abstractTypes = GetAbstractTypeNodes(source);
        var sourceNode = new TreeNode<LooseServiceRow>(sourceRow, abstractTypes); 

        return sourceNode;
    }

    static List<TreeNode<LooseServiceRow>> GetAbstractTypeNodes(ServiceSource source)
    {
        var abstractTypes = new List<TreeNode<LooseServiceRow>>(); 
        foreach (Type serviceType in source.AllAbstractTypes)
        {
            var abstractTypeRow = new LooseServiceRow(LooseServiceRow.RowCategory.Service)
            {
                source = source,
                type = serviceType,
            };
            if (source.InstantiatedServices.ContainsKey(serviceType))
                abstractTypeRow.loadedInstance = source.InstantiatedObject;


            var abstractTypeNode = new TreeNode<LooseServiceRow>(abstractTypeRow, children: null);
            abstractTypes.Add(abstractTypeNode);
        }

        return abstractTypes;
    }


    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        const float spacing = 10;
        var sourceSetting = (ServiceSourceSetting) property.GetObjectOfProperty();
        ServiceSource[] sources = sourceSetting.GetServiceSources().ToArray();
        _lines = sources.Length == 1 ? GetAbstractTypeRows(sources) : GenerateTreeView(sources);

        return EditorGUIUtility.singleLineHeight * (1 + Mathf.Max(1, _lines.Count)) + spacing;
    }
    
}
}
#endif
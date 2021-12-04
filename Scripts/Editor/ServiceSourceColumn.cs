#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using MUtility;
using Object = UnityEngine.Object;

namespace LooseLink
{
class ServiceSourceColumn : FoldoutColumn<ServiceLocatorRow>
{
    readonly ServiceLocatorWindow _serviceLocatorWindow;

    string SearchServiceText
    {
        get => _serviceLocatorWindow.searchServiceSourcesText;
        set => _serviceLocatorWindow.searchServiceSourcesText = value;
    } 

    public ServiceSourceColumn(ServiceLocatorWindow serviceLocatorWindow)
    {
        columnInfo = new ColumnInfo
        {
            customHeaderDrawer = DrawServiceSourcesHeader,
            fixWidth = 150,
            relativeWidthWeight = 0.75f,
        };
        _serviceLocatorWindow =serviceLocatorWindow;
    }

    public override void DrawContent(Rect position, FoldableRow<ServiceLocatorRow> row, GUIStyle style, Action onChanged)
    { }

    public override void DrawCell(Rect position, FoldableRow<ServiceLocatorRow> row, GUIStyle style, Action onChanged)
    {
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        position = DrawFoldout(position, row);
        EditorGUI.indentLevel = indent;
        DrawCell(position, row.element, selectElement: true);
    }
    
    static void DrawCell(Rect position, ServiceLocatorRow locatorRow, bool selectElement)
    {
        GUI.enabled = true;
        
        if (IsRowHighlighted(locatorRow))
            EditorGUI.DrawRect(position, EditorHelper.tableSelectedColor);

        GUI.color = Color.white;
 
        GUI.enabled = locatorRow.enabled;
        position.y += (position.height - 16) / 2f;
        position.height = 16;
        EditorGUI.LabelField(position, locatorRow.GetGUIContent());
 
        bool isRowSelectable = locatorRow.SelectionObject != null;
        if (isRowSelectable && position.Contains(Event.current.mousePosition))
            EditorGUI.DrawRect(position, EditorHelper.tableHoverColor);

        if (_rowButtonStyle == null)
            _rowButtonStyle = new GUIStyle(GUI.skin.label);
        
        GUI.enabled = true;
        if (GUI.Button(position, GUIContent.none, _rowButtonStyle))
            OnRowClick(locatorRow, selectElement);
        
        GUI.enabled = locatorRow.enabled;
    } 

    static void OnRowClick(ServiceLocatorRow locatorRow, bool selectElement)
    {
        Object obj = locatorRow.SelectionObject;

        if (selectElement)
        {
            if (Selection.objects.Length == 1 && Selection.objects[0] == obj)
                Selection.objects = new Object[] { };
            else
                Selection.objects = new[] {obj};
        }
        else
            EditorGUIUtility.PingObject(obj);
    }

    static bool IsRowHighlighted(ServiceLocatorRow locatorRow) =>
        locatorRow.SelectionObject != null &&
        Selection.objects.Contains(locatorRow.SelectionObject);

    void DrawServiceSourcesHeader(Rect position)
    {
        float searchTextW = Mathf.Min(200f, position.width - 110f);
        const float indent = 4;
        const float margin = 2;
        position.x += indent;
        position.width -= indent;
        GUI.Label(position, "Service Sources", LabelStyle);

        
        bool modernUI = EditorHelper.IsModernEditorUI; 
        
        var searchServicePos = new Rect(
            position.xMax - (searchTextW + margin),
            position.y + margin + 1 + (modernUI ? 0 : 1),
            searchTextW + (modernUI ? 0 : 1),
            position.height - (2 * margin));

        SearchServiceText =
            EditorGUI.TextField(searchServicePos, SearchServiceText, GUI.skin.FindStyle("ToolbarSeachTextField"));
    }

    public bool ApplyServiceSourceSearch(ServiceSource source) => ApplyServiceSearchOnType(source.Name);

    bool ApplyServiceSearchOnType(string text)
     {
         if (NoSearch) return true;  
         return text.ToLower().Contains(SearchServiceText.Trim().ToLower());
     }
     
     static GUIStyle _labelStyle;
     static GUIStyle LabelStyle => _labelStyle = _labelStyle ?? new GUIStyle(EditorStyles.label)
     {
         alignment = TextAnchor.MiddleLeft
     };
     
     protected override GUIStyle GetDefaultStyle() => LabelStyle;

     static GUIStyle _rowButtonStyle;

     public bool NoSearch => string.IsNullOrEmpty(SearchServiceText);
}

}

#endif
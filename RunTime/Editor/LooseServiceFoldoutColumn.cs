#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MUtility;
using Object = UnityEngine.Object;

namespace LooseServices
{
class LooseServiceFoldoutColumn : FoldoutColumn<LooseServiceRow>
{
    readonly LooseServiceWindow _window;

    string SearchServiceText
    {
        get => _window.searchServiceText;
        set => _window.searchServiceText = value;
    }
    public LooseServiceFoldoutColumn(ColumnInfo columnInfo) : base(columnInfo) { }

    public LooseServiceFoldoutColumn(LooseServiceWindow window)
    {
        columnInfo = new ColumnInfo {customHeaderDrawer = DrawServiceSourcesHeader, relativeWidthWeight = 2f};
        _window =window;
    }

    public override void DrawContent(Rect position, FoldableRow<LooseServiceRow> row, GUIStyle style, Action onChanged)
    { }

    public override void DrawCell(Rect position, FoldableRow<LooseServiceRow> row, GUIStyle style, Action onChanged)
    {
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        position = DrawFoldout(position, row);
        EditorGUI.indentLevel = indent;  
        DrawCell(position, row.element, selectElement: true);
    }
    
    public static void DrawCell(Rect position, LooseServiceRow row, bool selectElement)
    {
        if (IsRowHighlighted(row))
            EditorGUI.DrawRect(position, EditorHelper.tableSelectedColor);

        GUI.color = Color.white;
 
        position.y += (position.height - 16) / 2f;
        position.height = 16;
        EditorGUI.LabelField(position, row.GetGUIContent());

        const float categoryWidth = 100;
        var categoryPosition = new Rect(position.xMax-categoryWidth, position.y, categoryWidth, position.height);
        
        GUI.Label(categoryPosition, row.GetCategoryGUIContent(), CategoryStyle);
        bool isRowSelectable = row.SelectionObject != null;
        if (isRowSelectable && position.Contains(Event.current.mousePosition))
            EditorGUI.DrawRect(position, EditorHelper.tableHoverColor);

        // base.DrawCell(position, row, style, onChanged);

        if (_rowButtonStyle == null)
            _rowButtonStyle = new GUIStyle(GUI.skin.label);
        
        if (GUI.Button(position, GUIContent.none, _rowButtonStyle))
            OnRowClick(row, selectElement);
    }

    static void CreateScriptableObjectFile(Type type)
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        string selectedAssetPath = Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject));
        if (path == "")
            path = "Assets";
        else if (Path.GetExtension(path) != "")
            path = path.Replace(selectedAssetPath, "");
        path += $"/{type.Name}.asset";
        string assetsPath = Application.dataPath;
        // ReSharper disable StringIndexOfIsCultureSpecific.1
        int assetsPathLength = assetsPath.IndexOf("Assets");
        // ReSharper restore StringIndexOfIsCultureSpecific.1
        var fullPath = $"{assetsPath.Substring(0, assetsPathLength)}{path}";
        if (File.Exists(fullPath))
        {
            Debug.Log($"File   {fullPath}   already exists!");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(path);
        }
        else
        { 
            var so = ScriptableObject.CreateInstance(type);
            so.name = type.Name;
            AssetDatabase.CreateAsset(so, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = so;
        }
    }

    static void OnRowClick(LooseServiceRow row, bool selectElement)
    {
        Object obj = row.SelectionObject;

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

    static bool IsRowHighlighted(LooseServiceRow row) =>
        row.SelectionObject != null &&
        Selection.objects.Contains(row.SelectionObject);

    void DrawServiceSourcesHeader(Rect position)
    {
        const float searchTextW = 200;
        const float indent = 4;
        const float margin = 2;
        position.x += indent;
        position.width -= indent;
        GUI.Label(position, "Installers, Service Sources & Services", LabelStyle);

        
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

    public bool ApplyServiceSearchOnType(string text)
     {
         if (NoSearch) return true;  
         return text.ToLower().Contains(SearchServiceText.Trim().ToLower());
     }
     
     static GUIStyle _labelStyle;
     static GUIStyle LabelStyle => _labelStyle = _labelStyle ?? new GUIStyle(EditorStyles.label)
     {
         alignment = TextAnchor.MiddleLeft
     };
     
     static readonly GUIStyle categoryStyle = default;
     public static GUIStyle CategoryStyle => categoryStyle ?? new GUIStyle
     {
         alignment = TextAnchor.MiddleLeft,
         fontSize = 10,
         normal = {textColor = GUI.skin.label.normal.textColor},
     };

     protected override GUIStyle GetDefaultStyle() => LabelStyle;

     static GUIStyle _rowButtonStyle;

     public bool NoSearch => string.IsNullOrEmpty(SearchServiceText);
}

}

#endif
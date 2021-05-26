#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq; 
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
    {
        position.y += (position.height - 16) / 2f;
        position.height = 16;
        EditorGUI.LabelField(position, row.element.GetGUIContent(), style);
    }

    public override void DrawCell(Rect position, FoldableRow<LooseServiceRow> row, GUIStyle style, Action onChanged)
    {
        if (IsRowHighlighted(row))
            EditorGUI.DrawRect(position, EditorHelper.tableSelectedColor);


        if (row.element.Category == LooseServiceRow.RowCategory.Source)
            if (row.element.source is ServiceSourceFromScriptableObjectType source)
            {
                const float addButtonW = 18;
                var newButtonPos = new Rect(
                    position.xMax - addButtonW - EditorGUIUtility.standardVerticalSpacing,
                    position.y,
                    addButtonW,
                    position.height);

                if (GUI.Button(newButtonPos, new GUIContent("+", "Create new Instance")))
                    CreateScriptableObjectFile(source.AllNonAbstractTypes.First());
                position.width -= addButtonW + EditorGUIUtility.standardVerticalSpacing;

            }


        GUI.color = Color.white;
        GUI.Label(position, row.element.GetCategoryGUIContent(), CategoryStyle);
        bool isRowSelectable = row.element.SelectionObject != null;
        if (isRowSelectable && position.Contains(Event.current.mousePosition))
            EditorGUI.DrawRect(position, EditorHelper.tableHoverColor);

        base.DrawCell(position, row, style, onChanged);

        if (_rowButtonStyle == null)
            _rowButtonStyle = new GUIStyle(GUI.skin.label);
        
        if (GUI.Button(position, GUIContent.none, _rowButtonStyle))
            OnRowClick(row);
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
    
    static void OnRowClick(FoldableRow<LooseServiceRow> row)
    {
        Object obj = row.element.SelectionObject;
        if (Selection.objects.Length == 1 && Selection.objects[0] == obj)
            Selection.objects = new Object[] { };
        else
            Selection.objects = new[] {obj};
    }

    bool IsRowHighlighted(FoldableRow<LooseServiceRow> row) =>
        row.element.SelectionObject != null &&
        Selection.objects.Contains(row.element.SelectionObject);

    void DrawServiceSourcesHeader(Rect position)
    {
        const float searchTextW = 200;
        const float indent = 4;
        const float margin = 2;
        position.x += indent;
        position.width -= indent;
        GUI.Label(position, "Services & Sources", LabelStyle);

        
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
         alignment = TextAnchor.MiddleRight,
         fontSize = 10,
         padding = new RectOffset(left: 100, right: 5, top: 0, bottom: 0),
         normal = {textColor = GUI.skin.label.normal.textColor},
     };

     protected override GUIStyle GetDefaultStyle() => LabelStyle;

     GUIStyle _rowButtonStyle;

     public bool NoSearch => string.IsNullOrEmpty(SearchServiceText);
}

}

#endif
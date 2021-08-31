#if UNITY_EDITOR

using System.Linq;
using MUtility;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
class ServiceTypesColumn : Column<FoldableRow<ServiceLocatorRow>>
{

    readonly ServiceLocatorWindow _serviceLocatorWindow;
 
    string SearchTypeText
    {
        get => _serviceLocatorWindow.searchTypeText;
        set => _serviceLocatorWindow.searchTypeText = value;
    }

    string[] _typeSearchWords = null;
    public bool NoSearch => string.IsNullOrEmpty(SearchTypeText); 

    public ServiceTypesColumn(ServiceLocatorWindow serviceLocatorWindow)
    {
        columnInfo = new ColumnInfo
        {
            relativeWidthWeight = 1,
            fixWidth = 75,
            customHeaderDrawer = DrawHeader
        };
        _serviceLocatorWindow = serviceLocatorWindow;
    }
    
    public override void DrawCell(Rect position, FoldableRow<ServiceLocatorRow> row, GUIStyle style, Action onChanged)
    {
        if (row.element.Category != ServiceLocatorRow.RowCategory.Source)
        {
            ServicesEditorHelper.DrawLine(position);
            return;
        }
        
        List<Type> types = row.element.source.GetServiceTypes().ToList();
        
        if (types.IsNullOrEmpty())
        {
            GUI.Label(position, "-");
            return;
        }
 
        DrawTypes(position, types);
    }


    void DrawTypes(Rect position, IReadOnlyList<Type> types)
    {
        const float space = 4;
        const float iconWidth = 20;
        const float popupWidth = 25;
        if (types.Count <= 0) return;

        Rect typePosition = position;
        typePosition.y += 1;
        typePosition.height = 16;
        bool overflow = false;
        int overflowIndex = -1;
        for (var i = 0; i < types.Count; i++)
        {
            Type type = types[i];
            GUIContent content = FileIconHelper.GetGUIContentToType(types[i]);
            float w = ServicesEditorHelper.SmallLabelStyle.CalcSize(new GUIContent(content.text)).x + iconWidth;

            overflow = i == types.Count - 1
                ? typePosition.x + w > position.xMax
                : typePosition.x + w > position.xMax - popupWidth;

            if (overflow)
            {
                overflowIndex = i;
                break;
            }

            typePosition.width = w - space;
            DrawType(typePosition, content, type);
            typePosition.x += w + space;
        }

        if (!overflow) return;

        int overflowCount = types.Count - overflowIndex;
        var overflownTypes = new List<Type>(overflowCount);
        for (int i = overflowIndex; i < types.Count; i++)
            overflownTypes.Add(types[i]);

        bool allOverflown = overflowCount == types.Count;
        Rect popupRect = allOverflown
            ? position
            : new Rect(position.xMax - popupWidth, position.y, popupWidth, position.height);

        DrawTypePopup(popupRect, overflownTypes, !allOverflown);
        position.width -= popupWidth + 1;

    }

    public static void DrawType(Rect position, GUIContent content, Type type )
    {
        if (GUI.Button(position, content, ServicesEditorHelper.SmallLabelStyle))
            TryPing(type); 
    }

    void DrawTypePopup(Rect position, IReadOnlyList<Type> typesToDrawInPopup, bool drawPlus)
    {
        position.y += 1;
        position.height -= 2;
        position.x -= 1;
        var contents = new string[typesToDrawInPopup.Count];
        for (var i = 0; i < typesToDrawInPopup.Count; i++)
            contents[i] = FileIconHelper.GetGUIContentToType(typesToDrawInPopup[i]).tooltip;

        int index = EditorGUI.Popup(
            position,
            selectedIndex: -1,
            contents,
            new GUIStyle(GUI.skin.button));
 
        GUI.Label(position, $"{(drawPlus?"+":"")}{typesToDrawInPopup.Count}", ServicesEditorHelper.SmallLabelStyle);

        if (index >= -0)
            TryPing(typesToDrawInPopup[index]);
    } 
         

    public static void TryPing(Type pingable)
    {
        Object obj = TypeToFileHelper.GetObject(pingable);
        if(obj!= null)
            EditorGUIUtility.PingObject(obj);
    } 

    void DrawHeader(Rect pos)
    {
        const float labelWidth = 45; 
        var labelPos = new Rect(
            pos.x + 2,
            pos.y + 2,
            labelWidth,
            pos.height - 4);
        
        GUI.Label(labelPos, "Types");
        
        bool modernUI = EditorHelper.IsModernEditorUI; 

        float searchTextW = Mathf.Min(200f, pos.width - 52f);
        var searchTypePos = new Rect(
            pos.xMax - searchTextW - 2,
            pos.y + 3 + (modernUI ? 0 : 1),
            searchTextW,
            pos.height - 5);
        SearchTypeText = EditorGUI.TextField(searchTypePos, SearchTypeText, GUI.skin.FindStyle("ToolbarSeachTextField"));

        _typeSearchWords = ServicesEditorHelper.GenerateSearchWords(SearchTypeText);
    }
    
    public bool ApplyTypeSearchOnSource(IServiceSourceSet set, ServiceSource source) =>
        ApplyTypeSearchOnTypeArray(source.GetServiceTypes());

    public bool ApplyTypeSearchOnTypeArray(IEnumerable<Type> typesOnService)
    {
        if (string.IsNullOrEmpty(SearchTypeText)) return true;
        if (_typeSearchWords == null) return true;
        if (typesOnService == null) return false;

        Type[] types = typesOnService.ToArray();
        string[] typeTexts = types.Select(type => type.FullName.ToLower()).ToArray();

        foreach (string searchWord in _typeSearchWords)
            if (!typeTexts.Any(typeName => typeName.Contains(searchWord)))
                return false;

        return true; 
    }

    protected override GUIStyle GetDefaultStyle() => null;

    static GUIStyle _labelStyle;
    public static GUIStyle LabelStyle => _labelStyle = _labelStyle ?? new GUIStyle("Label"); 
}
}
#endif
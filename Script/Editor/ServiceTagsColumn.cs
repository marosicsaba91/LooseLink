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
class ServiceTagsColumn : Column<FoldableRow<ServiceLocatorRow>>
{
    const int minTagWidth = 25;
    const int spacing = 2;

    readonly ServiceLocatorWindow _serviceLocatorWindow;

    bool IsTagsOpen
    {
        get => _serviceLocatorWindow.isTagsOpen;
        set => _serviceLocatorWindow.isTagsOpen = value;
    }

    string SearchTagText
    {
        get => _serviceLocatorWindow.searchTagText;
        set => _serviceLocatorWindow.searchTagText = value;
    }

    string[] _tagSearchWords = null;

    public ServiceTagsColumn(ServiceLocatorWindow serviceLocatorWindow)
    {
        columnInfo = new ColumnInfo
        { 
            fixWidthGetter = GetTagWidth,
            relativeWidthWeightGetter = GetRelativeWidthWidth,
            customHeaderDrawer = DrawHeader
        };
        _serviceLocatorWindow = serviceLocatorWindow;
    }
    
    
    float GetTagWidth() => IsTagsOpen ? 75f : 55f;
    float GetRelativeWidthWidth() => IsTagsOpen ? 1f : 0f;
    
    public override void DrawCell(Rect position, FoldableRow<ServiceLocatorRow> row, GUIStyle style, Action onChanged)
    {
        if (row.element.Category != ServiceLocatorRow.RowCategory.Source)
        {
            ServicesEditorHelper.DrawLine(position);
            return;
        }
        if (row.element.source.SourceType == ServiceSourceTypes.FromScriptableObjectType) return;
        if (row.element.source.SourceType == ServiceSourceTypes.FromMonoBehaviourType) return;
        
        
        object[] tags = row.element.source.GetTags().ToArray();
        
        if (tags.IsNullOrEmpty())
        {
            GUI.Label(position, "-", ServicesEditorHelper.SmallLabelStyle);
            return;
        }

        var tagsPos = new Rect(
            position.x + spacing,
            position.y + 1,
            position.width - (2 * spacing),
            position.height - spacing);
        DrawTags(tagsPos, tags);
    }

    void DrawTags(Rect position, ICollection<object> tags)
    {
        var maxTagsToDraw = (int) ((position.width + spacing) / (minTagWidth + spacing));

        int tagCount = tags.Count;
        int tagPlacesToDraw =
            Mathf.Min(maxTagsToDraw, tagCount);
        int tagWidth = (int) ((position.width + spacing) / tagPlacesToDraw) - 2;
        int tagsToDrawInLine = tagCount > maxTagsToDraw ? maxTagsToDraw - 1 : tagPlacesToDraw;
        bool hasPopup = tags.Count > tagsToDrawInLine;
        int maxXForTags = hasPopup
            ? (int) (position.xMax - minTagWidth - spacing)
            : (int) position.xMax;

        var tagsToDrawInPopup = new List<object>();
        int startPos = (int) position.x;

        var i = 0;
        foreach (object tag in tags)
        {
            if (i < tagsToDrawInLine)
            {
                bool isLastTag = i >= tagsToDrawInLine - 1; 
                int w = isLastTag ? maxXForTags - startPos : tagWidth;
                DrawTag(new Rect(startPos, position.y, w, position.height), tag);
                startPos += tagWidth + spacing;
            }
            else
                tagsToDrawInPopup.Add(tag);

            i++;
        }

        if (tagsToDrawInPopup.Count > 0)
        {
            int w = tagsToDrawInLine <= 0 ? tagWidth : minTagWidth;
            DrawTagPopup(
                new Rect(position.xMax - w, position.y, w, position.height),
                tagsToDrawInPopup,
                tagsToDrawInLine > 1);
        }
    }

    public static void DrawTag(Rect position, object tag)
    {
        var iTag = tag.ToITag();

        Color color = iTag.Color;
        Color borderColor = EditorGUIUtility.isProSkin
            ? Color.Lerp(color, Color.white, 0.5f)
            : Color.Lerp(color, Color.black, 0.25f);
        EditorHelper.DrawBox(position, color, borderColor, borderInside: true);
        var content = new GUIContent
        {
            text = iTag.ShortText(position.width),
            tooltip = iTag.ObjectType() == null ? null : iTag.TextWithType(),
        };
        bool isColorDark = (color.a + color.g + color.b) / 3f <= 0.6;
        Color textColor = Color.Lerp(color, isColorDark ? Color.white : Color.black, t: 0.75f);
        ColoredLabelStyle.normal.textColor = textColor;
        if (GUI.Button(position, content,  ColoredLabelStyle))
            TryPing(tag);
        
    }

    void DrawTagPopup(Rect position, List<object> tagsToDrawInPopup, bool plus)
    {
        int index = EditorGUI.Popup(
            position,
            selectedIndex: -1,
            tagsToDrawInPopup.Select(tag => tag.ToITag().TextWithType()).ToArray(),
            new GUIStyle(GUI.skin.button));
        GUI.Label(position, $"{(plus ? "+" : "")}{tagsToDrawInPopup.Count}", ServicesEditorHelper.SmallLabelStyle);

        if (index >= -0)
            TryPing(tagsToDrawInPopup[index]);
    }
    
    void DrawHeader(Rect pos)
    {
        const float fullButtonWidth = 45;
        float buttonWidth = IsTagsOpen ? fullButtonWidth : pos.width - 4;
        var buttonPos = new Rect(
            pos.x + 4,
            pos.y ,
            buttonWidth,
            pos.height );
        IsTagsOpen = EditorGUI.Foldout(buttonPos, IsTagsOpen, "Tags");

        if (!IsTagsOpen)
        {
            if(_tagSearchWords == null || _tagSearchWords.Length>0)
                _tagSearchWords = new string[0];
            return;
        }
        
        bool modernUI = EditorHelper.IsModernEditorUI; 

        float searchTextW = Mathf.Min(200f, pos.width - 52f);
        var searchTagPos = new Rect(
            pos.xMax - searchTextW - 2,
            pos.y + 3 + (modernUI ? 0 : 1),
            searchTextW,
            pos.height - 5);
         
        SearchTagText = EditorGUI.TextField(searchTagPos, SearchTagText, GUI.skin.FindStyle("ToolbarSeachTextField"));

        _tagSearchWords = ServicesEditorHelper.GenerateSearchWords(SearchTagText);
    }

    public bool ApplyTagSearchOnSource(IServiceSourceSet set, ServiceSource source) =>
        ApplyTagSearchOnTagArray(source.GetTags());

    public static void TryPing(object pingable)
    {
        if (pingable is Object pingableObj)
            EditorGUIUtility.PingObject(pingableObj);
    } 
    
    public bool ApplyTagSearchOnTagArray(IEnumerable<object> tagsOnService)
    {
        if (string.IsNullOrEmpty(SearchTagText)) return true;
        if (_tagSearchWords == null) return true;
        if (tagsOnService == null) return false;

        object[] tags = tagsOnService.ToArray();
        string[] tagTexts = tags.Select(tag => tag.ToITag().TextWithType().ToLower()).ToArray();

        foreach (string searchWord in _tagSearchWords)
            if (!tagTexts.Any(tag => tag.Contains(searchWord)))
                return false;

        return true; 
    }

    public bool NoSearch => string.IsNullOrEmpty(SearchTagText);
 
    protected override GUIStyle GetDefaultStyle() => null;
    
    
    static GUIStyle _coloredLabelStyle;

    public static GUIStyle ColoredLabelStyle => _coloredLabelStyle = _coloredLabelStyle ?? new GUIStyle
    {
        alignment = TextAnchor.MiddleCenter,
        padding = new RectOffset(left: 0, right: 0, top: 0, bottom: 0),
        normal = {textColor = GUI.skin.label.normal.textColor},
        fontSize = 10
    };
}
}
#endif
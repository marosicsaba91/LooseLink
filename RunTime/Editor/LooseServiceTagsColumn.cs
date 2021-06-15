#if UNITY_EDITOR
using System.Linq;
 using MUtility;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
class LooseServiceTagsColumn : Column<FoldableRow<LooseServiceRow>>
{
    const float minTagWidth = 25;
    const float spacing = 2;

    readonly LooseServiceWindow _window;

    bool IsTagsOpen
    {
        get => _window.isTagsOpen;
        set => _window.isTagsOpen = value;
    }

    string SearchTagText
    {
        get => _window.searchTagText;
        set => _window.searchTagText = value;
    }

    string[] _tagSearchWords = null;

    public LooseServiceTagsColumn(LooseServiceWindow window)
    {
        columnInfo = new ColumnInfo
        {
            fixWidthGetter = GetTagWidth,
            customHeaderDrawer = DrawHeader
        };
        _window = window;
    }
    
    public override void DrawCell(Rect position, FoldableRow<LooseServiceRow> row, GUIStyle style, Action onChanged)
    {
        if (row.element.Category == LooseServiceRow.RowCategory.Installer) return;
        if (row.element.source is ServiceSourceFromScriptableObjectType) return;
        if (row.element.source is ServiceSourceFromMonoBehaviourType) return;

        IEnumerable<object> tagEnumerable;

        if (row.element.Category == LooseServiceRow.RowCategory.Source)
            tagEnumerable = row.element.source.GetAllTags(row.element.set);
        else
            tagEnumerable = row.element.source.GetTagsFor(row.element.type).Distinct();

        object[] tags = tagEnumerable.ToArray();

        if (tags.IsNullOrEmpty())
        {
            GUI.Label(position, "0", LabelStyle);
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
        float tagWidth = ((position.width + spacing) / tagPlacesToDraw) - 2;
        int tagsToDrawInLine = tagCount > maxTagsToDraw ? maxTagsToDraw - 1 : tagPlacesToDraw;

        var tagsToDrawInPopup = new List<object>();
        float startPos = position.x;

        var i = 0;
        foreach (object tag in tags)
        {
            if (i < tagsToDrawInLine)
            {
                DrawTag(new Rect(startPos, position.y, tagWidth, position.height), tag);
                startPos += tagWidth + spacing;
            }
            else
                tagsToDrawInPopup.Add(tag);

            i++;
        }

        if (tagsToDrawInPopup.Count > 0)
            DrawTagPopup(
                new Rect(startPos, position.y, tagWidth, position.height),
                tagsToDrawInPopup,
                tagsToDrawInLine > 1);
    }

    void DrawTag(Rect position, object tag)
    {
        var iTag = tag.ToITag();

        Color color = iTag.Color;
        Color borderColor = Color.Lerp(color, EditorGUIUtility.isProSkin ? Color.white : Color.black, t: 0.25f);
        EditorHelper.DrawBox(position, color, borderColor);
        var content = new GUIContent
        {
            text = iTag.ShortText(position.width),
            tooltip = iTag.ObjectType() == null ? null : iTag.TextWithType(),
        };
        bool isColorDark = (color.a + color.g + color.b) / 3f <= 0.6;
        Color textColor = Color.Lerp(color, isColorDark ? Color.white : Color.black, t: 0.75f);
        TagLabelStyle.normal.textColor = textColor;
        if (GUI.Button(position, content, TagLabelStyle))
            TryPing(tag);
        
    }

    void DrawTagPopup(Rect position, List<object> tagsToDrawInPopup, bool plus)
    {
        int index = EditorGUI.Popup(
            position,
            selectedIndex: -1,
            tagsToDrawInPopup.Select(tag => tag.ToITag().TextWithType()).ToArray(),
            new GUIStyle(GUI.skin.button));
        GUI.Label(position, $"{(plus ? "+" : "")}{tagsToDrawInPopup.Count}", LabelStyle);

        if (index >= -0)
            TryPing(tagsToDrawInPopup[index]);
    }

    static void TryPing(object pingable)
    {
        if (pingable is Object pingableObj)
            EditorGUIUtility.PingObject(pingableObj);
    }

    void DrawHeader(Rect pos)
    {
        const float fullButtonWidth = 40;
        float buttonWidth = IsTagsOpen ? fullButtonWidth : pos.width - 4;
        var buttonPos = new Rect(
            pos.x + pos.width - buttonWidth - 2,
            pos.y + 2,
            buttonWidth,
            pos.height - 4);
        if (GUI.Button(buttonPos, "Tag"))
            IsTagsOpen = !IsTagsOpen;

        if (!IsTagsOpen)
        {
            if(_tagSearchWords == null || _tagSearchWords.Length>0)
                _tagSearchWords = new string[0];
            return;
        }

        bool modernUI = EditorHelper.IsModernEditorUI; 

        var searchTagPos = new Rect(
            pos.x +4,
            pos.y + 3 + (modernUI ? 0 : 1),
            pos.width - buttonWidth - 7 + + (modernUI ? 0 : 1),
            pos.height - 5);
        SearchTagText = EditorGUI.TextField(searchTagPos, SearchTagText, GUI.skin.FindStyle("ToolbarSeachTextField"));

        _tagSearchWords = GenerateSearchWords(SearchTagText);
    }

    float GetTagWidth() => IsTagsOpen ? 138 : 36;
    public bool ApplyTagSearchOnSource(IServiceSourceSet set, ServiceSource source) =>
        ApplyTagSearchOnTagArray(source.GetAllTags(set));

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

    string[] GenerateSearchWords(string searchText)
    {
        string[] rawKeywords = searchText.Split(',');
        return rawKeywords.Select(keyword => keyword.Trim().ToLower()).ToArray();
    }

    protected override GUIStyle GetDefaultStyle() => null;

    static GUIStyle _labelStyle;

    static GUIStyle LabelStyle => _labelStyle = _labelStyle ?? new GUIStyle
    {
        alignment = TextAnchor.MiddleCenter,
        padding = new RectOffset(left: 0, right: 0, top: 0, bottom: 0),
        normal = {textColor = GUI.skin.label.normal.textColor},
        fontSize = 10
    };

    static GUIStyle _tagLabelStyle;

    static GUIStyle TagLabelStyle => _tagLabelStyle = _tagLabelStyle ?? new GUIStyle
    {
        alignment = TextAnchor.MiddleCenter,
        padding = new RectOffset(left: 0, right: 0, top: 0, bottom: 0),
        normal = {textColor = GUI.skin.label.normal.textColor},
        fontSize = 10
    };
}
}
#endif
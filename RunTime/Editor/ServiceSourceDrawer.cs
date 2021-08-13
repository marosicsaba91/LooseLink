﻿#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices.Editor
{
static class ServiceSourceDrawer
{
    const float padding = 4;
    const float foldoutW = 18;
    const float toggleW = 14;
    const float serviceTypeW = 100;
    const float actionButtonWidth = 20;
    const float actionBarWidth = actionButtonWidth * 3 - 2;

    static readonly float space = EditorGUIUtility.standardVerticalSpacing;
    static readonly float lineHeight = EditorGUIUtility.singleLineHeight;

    public static readonly GUIContent noServiceContent = new GUIContent("NO SERVICE",
        "Selected object should be one of these:\n" +
        "   In Scene Game Object\n" +
        "   Prefab\n" +
        "   Scriptable Object\n" +
        "   Non abstract MonoBehaviour Script\n" +
        "   Non abstract ScriptableObject Script\n" +
        "   Service Set File");

    static readonly GUIStyle categoryPopupStyle = default;

    public static GUIStyle LeftAlignedButtonStyle => categoryPopupStyle ?? new GUIStyle("Button")
    {  
        alignment = TextAnchor.MiddleLeft
    };

    static readonly GUIStyle actionButtonStyle = default;

    public static GUIStyle ActionButtonStyle => actionButtonStyle ?? new GUIStyle("Label")
    {
        fontSize = 10,
        alignment = TextAnchor.MiddleCenter,
        normal = {textColor = GUI.skin.label.normal.textColor}
    };

    static bool _anyChange;

    static ServiceSourceSet _set;
    static ServiceSource _source;
    static int _sourceIndex;
    static IList<ServiceSource> _sourceList;
    static Object _serializedObject;
    static DynamicServiceSource _dynamicSource;
    static List<Type> _nonAbstractTypes;
    static List<Type> _dynamicServiceTypes;
    static List<SerializableType> _additionalServiceTypes;
    static List<Type> _possibleAdditionalServiceTypes;
    static List<SerializableTag> _additionalTags; 
    static List<object> _dynamicTags;
    static int _typeCount;
    static int _tagCount;
    static SerializableTag _editedTag;

    public static void DrawServiceSources(
        List<ServiceSource> list,
        Object targetObject)
    {
        if (list != null)
            for (var i = 0; i < list.Count; i++)
            {
                Rect rect = EditorGUILayout.GetControlRect();
                float height = DrawServiceSource(list, i, targetObject, rect.position, rect.width);

                GUILayout.Space(height - lineHeight - 3);
            }

        GUILayout.Space(10);
        if (GUILayout.Button("Add New Services Source"))
        {
            Undo.RecordObject(targetObject, "Add new service source setting.");
            list.Add(new ServiceSource());
            EditorUtility.SetDirty(targetObject);
        }
    }

    static float DrawServiceSource(
        IList<ServiceSource> sourceList,
        int index,
        Object serializedObject,
        Vector2 startPosition,
        float width)
    {
        _serializedObject = serializedObject;
        _sourceList = sourceList;
        _source = sourceList[index];
        _sourceIndex = index;
        if (_source == null) return 0;
        _set = _source.GetServiceSourceSet();
        _dynamicSource = _source.GetDynamicServiceSource();
        _additionalServiceTypes = _source.additionalTypes;
        _additionalTags = _source.additionalTags;
        _nonAbstractTypes = _dynamicSource?.GetAllNonAbstractTypes().ToList();
        _possibleAdditionalServiceTypes = _dynamicSource?.GetPossibleAdditionalTypes().ToList();
        _dynamicServiceTypes = _dynamicSource?.GetAllAbstractTypes().ToList();
        _dynamicTags = _dynamicSource?.GetDynamicTags().ToList();
        _typeCount = (_dynamicServiceTypes?.Count ?? 0) + _additionalServiceTypes.Count;
        _tagCount = (_dynamicTags?.Count ?? 0) + _additionalTags.Count;

        float height = PixelHeightOfSource();
        var position = new Rect(startPosition, new Vector2(width, height));
        DrawSource(position); 
        GUI.enabled = true;
        return height;
    }


    static float PixelHeightOfSource()
    {
        float oneLine = lineHeight + (2 * padding);

        if (_source == null) return oneLine;

        ServiceSourceSet set = _source.GetServiceSourceSet();
        if (set != null) return oneLine;

        DynamicServiceSource source = _source.GetDynamicServiceSource();
        if (source == null) return oneLine;

        var lineCount = 3;
        if (_source.isTypesExpanded)
            lineCount += 1 + _typeCount;
        if (_source.isTagsExpanded)
            lineCount += 1+_tagCount;
        return ((lineHeight + space) * lineCount) + (2 * padding);
    }


    static void DrawSource(Rect position)
    {
        Color color = _sourceIndex % 2 != 0 ? EditorHelper.tableBackgroundColor:  EditorHelper.tableEvenLineColor;
        EditorHelper.DrawBox(position, color);

        Undo.RecordObject(_serializedObject, "Service Setting Modified");
        _anyChange = false; 
        Rect typesPos = DrawHeader(position);

        if (_dynamicSource != null)
        { 
            typesPos.x += foldoutW + space;
            typesPos.width -= foldoutW + space;

            // Draw types
            Rect tagsPosition = DrawTypes(typesPos);

            // Draw tags  
            DrawTags(tagsPosition);
        }
        
        if (_anyChange)
        {
            EditorUtility.SetDirty(_serializedObject); 
            _source.ClearDynamicData();
        } 
    }
 
    static Rect DrawHeader(Rect position)
    {
        position.x += padding;
        position.y += padding;
        position.width -= padding * 2;
        position.height -= padding * 2;

        var togglePos = new Rect(position.x + space, position.y, toggleW, lineHeight);
        GUI.enabled = true;
        bool enabled = EditorGUI.Toggle(togglePos, _source.enabled);
        GUI.enabled = _source.enabled; 
        if (enabled != _source.enabled)
        {
            _anyChange = true;
            _source.enabled = enabled;
        }

        float w = position.width - (toggleW + serviceTypeW + space * 4 + actionButtonWidth * 3);
        var objectPos = new Rect(togglePos.xMax + space * 2, position.y, w, lineHeight);
        Object obj = EditorGUI.ObjectField(
            objectPos,
            _source.serviceSourceObject,
            typeof(Object),
            allowSceneObjects: true);

        var sourceTypePos = new Rect(objectPos.xMax + space, position.y, serviceTypeW, lineHeight);

        ServiceSourceTypes sourceType = _source.preferredSourceType;
        if (_dynamicSource != null)
        {
            sourceType = _dynamicSource.SourceType;
            if (_dynamicSource.AlternativeSourceTypes.Any())
            {
                var options = new List<ServiceSourceTypes> {sourceType};
                options.AddRange(_dynamicSource.AlternativeSourceTypes);
                options.Sort();
                int currentIndex = options.IndexOf(sourceType);

                GUIContent[] guiContentOptions = new GUIContent[options.Count];
                for (var i = 0; i < options.Count; i++)
                    guiContentOptions[i] =
                        LooseServiceRow.GetCategoryGUIContentForServiceSource(options[i], withIcons: false);

                int selectedIndex = EditorGUI.Popup(sourceTypePos, currentIndex, guiContentOptions);
                sourceType = options[selectedIndex];
            }
            else
            {
                GUIContent content = LooseServiceRow.GetCategoryGUIContentForServiceSource(_source, withIcons: false);
                GUI.Label(sourceTypePos, content);
            }
        }
        else if (_set != null)
            GUI.Label(sourceTypePos,
                new GUIContent($"Source Set ({_set.GetValidSources().Count()})"));
        else
            GUI.Label(sourceTypePos, noServiceContent);

        // Object or source Type changed
        if (obj != _source.serviceSourceObject || sourceType != _source.preferredSourceType)
        {
            _anyChange = true;
            if (_serializedObject is ServiceSourceSet set1 && obj is ServiceSourceSet set2)
            {
                if (!ServiceSourceSet.IsCircular(set1, set2))
                    _source.serviceSourceObject = obj;
            }
            else
                _source.serviceSourceObject = obj;

            _source.preferredSourceType = sourceType;
        }

        // Action Buttons 
        var actionButtonPos = new Rect(sourceTypePos.xMax + space, position.y, actionBarWidth, lineHeight);
        ListAction action = DrawActionBar(actionButtonPos, _sourceList.Count, _sourceIndex);
        if (action != ListAction.Non)
        {
            if (action == ListAction.MoveUp)
                _sourceList.Swap(_sourceIndex, _sourceIndex - 1);
            if (action == ListAction.MoveDown)
               _sourceList.Swap(_sourceIndex, _sourceIndex + 1);
            if (action == ListAction.Delete)
                _sourceList.RemoveAt(_sourceIndex);
            _anyChange = true;
        }

        position.y += lineHeight + space;
        position.height = lineHeight; 
        return position;
    }


    static Rect DrawTypes(Rect position)
    {
        var title = $"Types ({_typeCount})";
        _source.isTypesExpanded = EditorGUI.Foldout(position, _source.isTypesExpanded, title);

        List<Type> usedTypes = _additionalServiceTypes
            .Select(st => st.Type)
            .Where(t => t != null)
            .Union(_dynamicServiceTypes)
            .ToList();


        position.y += lineHeight + space;
        if (!_source.isTypesExpanded) return position;

        foreach (Type type in _dynamicServiceTypes)
        {
            DrawServiceType(position, type);
            position.y += lineHeight + space;
        }
        List<Type> notUsedAdditionalTypes = 
            _possibleAdditionalServiceTypes.Where(
                t => !_dynamicServiceTypes.Contains(t)).ToList();

        if (_source.additionalTypes != null)
            for (var index = 0; index < _additionalServiceTypes.Count; index++)
            {
                notUsedAdditionalTypes.Remove(_additionalServiceTypes[index].Type);
                DrawSerializableType(position, _additionalServiceTypes, index, usedTypes);
                position.y += lineHeight + space;
            }

        
        
        bool isAnyNotUsedType = notUsedAdditionalTypes.Count > 0;
        Rect buttonPos = position;
        buttonPos.width -= actionBarWidth + space;
        if (DrawButton(buttonPos, ListAction.Add, isAnyNotUsedType))
        {
            var st = new SerializableType {Type = notUsedAdditionalTypes[0]};
            _source.additionalTypes.Add(st);
            _anyChange = true;
        }

        position.y += lineHeight + space;
        return position;
    }

    static void DrawServiceType(Rect position, Type type)
    {
        position.width -= 3 * actionButtonWidth;
        if (GUI.Button(position, type.ToString(), LeftAlignedButtonStyle))
        {
            Object file = TypeToFileHelper.GetObject(type);
            if (!(file is null))
                EditorGUIUtility.PingObject(file);
        }
    }

    static void DrawSerializableType(
        Rect position,
        IList<SerializableType> serializedTypes,
        int typeIndex,
        ICollection<Type> usedTypes)
    {
        Color guiColor = GUI.color;
        SerializableType serializableType = serializedTypes[typeIndex];
        position.width -= 3 * actionButtonWidth;
        Type type = serializableType.Type;
        bool validType = _possibleAdditionalServiceTypes.Contains(type);

        List<Type> popupTypeList =
            _possibleAdditionalServiceTypes.Where(t => t == type || !usedTypes.Contains(t)).ToList();

        int index = popupTypeList.IndexOf(type);
        int itemCount = popupTypeList.Count + (validType ? 0 : 1);
        var elementsString = new string[itemCount];
        for (var i = 0; i < popupTypeList.Count; i++)
            elementsString[i] = popupTypeList[i].ToString();
        if (!validType)
        {
            index = elementsString.Length - 1;
            elementsString[index] = serializableType.Name;
            GUI.color = EditorHelper.ErrorRedColor;
        }
        
        int newIndex = EditorGUI.Popup(position, index, elementsString);
        if (newIndex != index)
        {
            serializableType.Type = popupTypeList[newIndex];
            _anyChange = true;
        }
        GUI.color = guiColor;

        position.x = position.xMax + space;
        position.width = actionBarWidth;
        ListAction action = DrawActionBar(position, serializedTypes.Count, typeIndex);

        if (action != ListAction.Non)
        {
            if (action == ListAction.MoveUp)
                serializedTypes.Swap(typeIndex, typeIndex - 1);
            if (action == ListAction.MoveDown)
                serializedTypes.Swap(typeIndex, typeIndex + 1);
            if (action == ListAction.Delete)
                serializedTypes.RemoveAt(typeIndex);
            _anyChange = true;
        }
    }
    
    static void DrawTags(Rect position)
    {
        var title = $"Tags ({_tagCount})";
        _source.isTagsExpanded = EditorGUI.Foldout(position, _source.isTagsExpanded, title);

        position.y += lineHeight + space;
        if (!_source.isTagsExpanded) return;

        foreach (object tag in _dynamicTags)
        {
            DrawDynamicTag(position, tag);
            position.y += lineHeight + space;
        } 

        if (_source.additionalTypes != null)
            for (var index = 0; index < _additionalTags.Count; index++)
            { 
                DrawSerializableTag(position, _additionalTags, index);
                position.y += lineHeight + space;
            }

         
        Rect buttonPos = position;
        buttonPos.width -= actionBarWidth + space;
        if (DrawButton(buttonPos, ListAction.Add, enabled: true))
        { 
            _source.additionalTags.Add(new  SerializableTag());
            _anyChange = true;
        }

        position.y += lineHeight + space;
    }

    static void DrawDynamicTag(Rect position, object tag)
    {
        position.width -= 3 * actionButtonWidth;
        if (position.width <= 0) return;
        LooseServiceTagsColumn.DrawTag(position, tag);
    }

    static void DrawSerializableTag(
        Rect position,
        IList<SerializableTag> serializedTags,
        int tagIndex)
    {
        SerializableTag serializableTag = serializedTags[tagIndex];
        position.width -= 3 * actionButtonWidth;
        const float tagTypeWidth = 70;
        const float tagWidth = 50;
        const float editButtonWidth = 25;
        float tagValueWidth = position.width - tagTypeWidth - tagWidth - editButtonWidth - space;
        bool editing = _editedTag == serializableTag;

        position.width = editing ? tagWidth : position.width - editButtonWidth + space;
        if(position.width <=0) return;
        
        LooseServiceTagsColumn.DrawTag(position, serializableTag.TagObject);

        if (editing)
        {
            position.x = position.xMax + space;

            position.width = tagValueWidth;
            SerializableTag.TagType tagType = serializableTag.Type;
            switch (tagType)
            {
                case SerializableTag.TagType.String:
                    string text = serializableTag.StringTag;
                    string newText = EditorGUI.TextField(position, text);
                    if (newText != text)
                    {
                        serializableTag.StringTag = newText;
                        _anyChange = true;
                    }

                    break;
                case SerializableTag.TagType.Object:
                    Object unityObject = serializableTag.ObjectTag;
                    Object newObject =
                        EditorGUI.ObjectField(position, unityObject, typeof(Object), allowSceneObjects: true);
                    if (newObject != unityObject)
                    {
                        serializableTag.ObjectTag = newObject;
                        _anyChange = true;
                    }

                    break;
                case SerializableTag.TagType.TagFile:
                    TagFile tagFile = serializableTag.TagFile;
                    var newTagFile =
                        (TagFile) EditorGUI.ObjectField(position, tagFile, typeof(TagFile), allowSceneObjects: true);
                    if (newTagFile != tagFile)
                    {
                        serializableTag.TagFile = newTagFile;
                        _anyChange = true;
                    }

                    break;
            }

            position.x = position.xMax + space;
            position.width = tagTypeWidth;
            var newTagType = (SerializableTag.TagType) EditorGUI.EnumPopup(position, tagType);
            if (newTagType != tagType)
            {
                serializableTag.Type = newTagType;
                _anyChange = true;
            }

        }

        position.x = position.xMax + space;
        position.width = actionButtonWidth;
        if (GUI.Button(position, GUIContent.none))
            _editedTag = editing ? null : serializableTag;
        GUI.Label(position, EditorGUIUtility.IconContent("align_vertically_center"));
         
        position.x = position.xMax + space;
        position.width = actionBarWidth;
        ListAction action = DrawActionBar(position, serializedTags.Count, tagIndex);

        if (action != ListAction.Non)
        {
            if (action == ListAction.MoveUp)
                serializedTags.Swap(tagIndex, tagIndex - 1);
            if (action == ListAction.MoveDown)
                serializedTags.Swap(tagIndex, tagIndex + 1);
            if (action == ListAction.Delete)
                serializedTags.RemoveAt(tagIndex);
            _anyChange = true;
        }
    }
    

    enum ListAction
    {
        Non,
        MoveUp,
        MoveDown,
        Delete,
        Add
    }

    static ListAction DrawActionBar(Rect position, int count, int index)
    {
        position.width = actionButtonWidth;
        const float shift = actionButtonWidth - 1;

        var result = ListAction.Non;
        if (DrawButton(position, ListAction.MoveUp, index > 0))
            result = ListAction.MoveUp;

        position.x += shift;
        if (DrawButton(position, ListAction.MoveDown, index < count - 1))
            result = ListAction.MoveDown;

        position.x += shift;
        if (DrawButton(position, ListAction.Delete))
            result = ListAction.Delete;
        return result;
    }

    public static readonly GUIContent upIcon = EditorGUIUtility.IconContent("scrollup_uielements");
    public static readonly GUIContent downIcon = EditorGUIUtility.IconContent("scrolldown_uielements");
    public static readonly GUIContent deleteIcon = EditorGUIUtility.IconContent("winbtn_win_close");
    public static readonly GUIContent addNewIcon = EditorGUIUtility.IconContent("CreateAddNew"); 
    
    static bool DrawButton(Rect position, ListAction action, bool enabled = true)
    {
        GUI.enabled = enabled;
        bool result = GUI.Button(position, GUIContent.none);

        GUIContent icon =
            action == ListAction.Add ? addNewIcon :
            action == ListAction.Delete ? deleteIcon :
            action == ListAction.MoveDown ? downIcon :
            action == ListAction.MoveUp ? upIcon :
            GUIContent.none;

        if (action == ListAction.Add)
        {
            position.x += 1;
            position.y += 1;
            position.width -= 2;
            position.height -= 2;
        }

        GUI.Label(position, icon, ActionButtonStyle);
        GUI.enabled = _source.enabled; 
        return result;
    }

}
}
#endif
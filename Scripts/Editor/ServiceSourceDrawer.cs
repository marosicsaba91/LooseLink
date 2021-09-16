#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator.Editor
{
static class ServiceSourceDrawer
{
    const float padding = 4;
    const float foldoutW = 18;
    const float toggleW = 14;
    const float serviceTypeW = 100;
    const float actionButtonWidth = 20;
    const float actionBarWidth = actionButtonWidth * 3 - 2;

    static readonly float _space = EditorGUIUtility.standardVerticalSpacing;
    static readonly float _lineHeight = EditorGUIUtility.singleLineHeight;

    public static readonly GUIContent invalidObjectContent = new GUIContent("Invalid Object", typeTooltip);
    public static readonly GUIContent noObjectContent = new GUIContent("Select an Object", typeTooltip);

    const string typeTooltip =
        "Selected object should be one of these:\n" +
        "   In Scene Game Object\n" +
        "   Prefab\n" +
        "   Scriptable Object\n" +
        "   Non abstract MonoBehaviour Script\n" +
        "   Non abstract ScriptableObject Script\n" +
        "   Service Set File";
    
    static readonly GUIStyle _categoryPopupStyle = default;

    public static GUIStyle LeftAlignedButtonStyle => _categoryPopupStyle ?? new GUIStyle("Button")
    {
        alignment = TextAnchor.MiddleLeft
    };

    static readonly GUIStyle _actionButtonStyle = default;

    public static GUIStyle ActionButtonStyle => _actionButtonStyle ?? new GUIStyle("Label")
    {
        fontSize = 10,
        alignment = TextAnchor.MiddleCenter,
        normal = {textColor = GUI.skin.label.normal.textColor}
    };

    static bool _anyChange;

    static IServiceSourceSet _containingSet;
    static ServiceSourceSet _insideSet;
    static ServiceSource _source;
    static int _sourceIndex;
    static IList<ServiceSource> _sourceList;
    static Object _serializedObject;
    static DynamicServiceSource _dynamicSource;
    static List<Type> _nonAbstractTypes;
    static List<Type> _dynamicServiceTypes;
    static List<SerializableType> _additionalServiceTypes;
    static List<Type> _possibleAdditionalServiceTypes;
    static List<Tag> _additionalTags; 
    static int _typeCount; 

    public static void DrawServiceSources(
        IServiceSourceSet containingSet,
        List<ServiceSource> list,
        Object targetObject)
    {
        if (list != null)
            for (var i = 0; i < list.Count; i++)
            {
                Rect rect = EditorGUILayout.GetControlRect();
                float height = DrawServiceSource(containingSet, list, i, targetObject, rect.position, rect.width);

                GUILayout.Space(height - _lineHeight - 3);
            }

        GUILayout.Space(pixels: 10);
        if (GUILayout.Button("Add New Services Source"))
        {
            Undo.RecordObject(targetObject, "Add new service source setting.");
            list.Add(new ServiceSource());
            EditorUtility.SetDirty(targetObject);
        }
    }

    static float DrawServiceSource(
        IServiceSourceSet containingSet,
        IList<ServiceSource> sourceList,
        int index,
        Object serializedObject,
        Vector2 startPosition,
        float width)
    {
        _containingSet = containingSet;
        _serializedObject = serializedObject;
        _sourceList = sourceList;
        _source = sourceList[index];
        _sourceIndex = index;
        if (_source == null) return 0;
        _insideSet = _source.GetServiceSourceSet();
        _dynamicSource = _source.GetDynamicServiceSource();
        _dynamicSource?.ClearCachedTypes();
        _additionalServiceTypes = _source.additionalTypes;
        _additionalTags = _source.tags;
        _nonAbstractTypes = _dynamicSource?.GetAllNonAbstractTypes().ToList();
        _possibleAdditionalServiceTypes = _dynamicSource?.GetPossibleAdditionalTypes().ToList();
        _dynamicServiceTypes = _dynamicSource?.GetAllAbstractTypes().ToList(); 
        _typeCount = (_dynamicServiceTypes?.Count ?? 0) + _additionalServiceTypes.Count;

        float height = PixelHeightOfSource();
        var position = new Rect(startPosition, new Vector2(width, height));
        DrawSource(position);
        GUI.enabled = true;
        return height;
    }


    static float PixelHeightOfSource()
    {
        float oneLine = _lineHeight + (2 * padding);

        if (_source == null) return oneLine;

        ServiceSourceSet set = _source.GetServiceSourceSet();
        if (set != null) return oneLine;

        DynamicServiceSource source = _source.GetDynamicServiceSource();
        if (source == null) return oneLine;

        var lineCount = 3;
        if (_source.isTypesExpanded)
            lineCount += 1 + _typeCount;
        if (_source.isTagsExpanded)
            lineCount += 1 + _additionalTags.Count;
        return ((_lineHeight + _space) * lineCount) + (2 * padding);
    }


    static void DrawSource(Rect position)
    {
        Color color = _sourceIndex % 2 != 0 ? EditorHelper.tableBackgroundColor : EditorHelper.tableEvenLineColor;
        EditorHelper.DrawBox(position, color);
 
        Undo.RecordObject(_serializedObject, "Service Setting Modified");
        _anyChange = false;
        Rect typesPos = DrawHeader(position);

        if (_dynamicSource != null)
        {
            typesPos.x += foldoutW + _space;
            typesPos.width -= foldoutW + _space;

            // Draw types
            Rect tagsPosition = DrawServices(typesPos);

            // Draw tags  
            DrawTags(tagsPosition);
        } 

        if (_anyChange)
            EditorUtility.SetDirty(_serializedObject); 
    }

    static Rect DrawHeader(Rect position)
    {
        position.x += padding;
        position.y += padding;
        position.width -= padding * 2;
        position.height -= padding * 2;

        var togglePos = new Rect(position.x + _space, position.y, toggleW, _lineHeight);
        GUI.enabled = true;
        bool enabled = EditorGUI.Toggle(togglePos, _source.Enabled);
        GUI.enabled = IsSourceEnabled;
        if (enabled != _source.Enabled)
        {
            _anyChange = true;
            _source.Enabled = enabled; 
        }

        float w = position.width - (toggleW + serviceTypeW + _space * 4 + actionButtonWidth * 3);
        var objectPos = new Rect(togglePos.xMax + _space * 2, position.y, w, _lineHeight);
        Object obj = EditorGUI.ObjectField(
            objectPos,
            _source.ServiceSourceObject,
            typeof(Object),
            allowSceneObjects: true);

        var sourceTypePos = new Rect(objectPos.xMax + _space, position.y, serviceTypeW, _lineHeight);

        ServiceSourceTypes sourceType = _source.PreferredSourceType;
        if (_source.ServiceSourceObject== null)
            GUI.Label(sourceTypePos, noObjectContent);
        else if (_dynamicSource != null)
        {
            sourceType = _dynamicSource.SourceType;
            if (_dynamicSource.AlternativeSourceTypes.Any())
            {
                var options = new List<ServiceSourceTypes> {sourceType};
                options.AddRange(_dynamicSource.AlternativeSourceTypes);
                options.Sort();
                int currentIndex = options.IndexOf(sourceType);

                var guiContentOptions = new GUIContent[options.Count];
                for (var i = 0; i < options.Count; i++)
                {
                    ServiceSourceTypes option = options[i];
                    guiContentOptions[i] =
                        new GUIContent(FileIconHelper.GetShortNameForServiceSource(option),
                            image: null,
                            FileIconHelper.GetTooltipForServiceSource(option));
                }

                int selectedIndex = EditorGUI.Popup(sourceTypePos, currentIndex, guiContentOptions);
                sourceType = options[selectedIndex];
            }
            else
            {
                var content = new GUIContent( FileIconHelper.GetShortNameForServiceSource(_source.SourceType),
                    image: null,
                    FileIconHelper.GetTooltipForServiceSource(_source.SourceType));
                GUI.Label(sourceTypePos, content);
            }
        }
        else if (_insideSet != null)
            GUI.Label(sourceTypePos,
                new GUIContent($"Source Set ({_insideSet.GetEnabledValidSourcesRecursive().Count()})"));
        else
            GUI.Label(sourceTypePos, invalidObjectContent); 

        // Object or source Type changed
        if (obj != _source.ServiceSourceObject || sourceType != _source.PreferredSourceType)
        {
            _anyChange = true;
            if (_serializedObject is ServiceSourceSet set1 && obj is ServiceSourceSet set2)
            {
                if (!ServiceSourceSet.IsCircular(set1, set2))
                    _source.ServiceSourceObject = obj;
            }
            else
                _source.ServiceSourceObject = obj;

            _source.PreferredSourceType = sourceType;
        }

        // Action Buttons 
        var actionButtonPos = new Rect(sourceTypePos.xMax + _space, position.y, actionBarWidth, _lineHeight);
        ListAction action = DrawActionBar(actionButtonPos, _sourceList.Count, _sourceIndex);
        if (action != ListAction.Non)
        {
            if (action == ListAction.MoveUp)
            {
                _sourceList.Swap(_sourceIndex, _sourceIndex - 1);
                var source1 = _sourceList[_sourceIndex];
                var source2 = _sourceList[_sourceIndex - 1];
                ServiceLocator.Environment.InvokeEnvironmentChangedOnSources(source1,source2);
            }

            if (action == ListAction.MoveDown)
            {
                _sourceList.Swap(_sourceIndex, _sourceIndex + 1);
                var source1 = _sourceList[_sourceIndex];
                var source2 = _sourceList[_sourceIndex + 1];
                ServiceLocator.Environment.InvokeEnvironmentChangedOnSources(source1,source2);
            }

            if (action == ListAction.Delete)
            { 
                _containingSet.RemoveServiceSourceAt(_sourceIndex);
                ServiceLocator.Environment.InvokeEnvironmentChangedOnSource(_source);
            }

            _anyChange = true;
        }

        position.y += _lineHeight + _space;
        position.height = _lineHeight;
        return position;
    }

    static Rect DrawServices(Rect position)
    {
        var title = $"Services ({_typeCount})";
        _source.isTypesExpanded = EditorGUI.Foldout(position, _source.isTypesExpanded, title);

        List<Type> usedTypes = _additionalServiceTypes
            .Select(st => st.Type)
            .Where(t => t != null)
            .Union(_dynamicServiceTypes)
            .ToList();


        position.y += _lineHeight + _space;
        if (!_source.isTypesExpanded) return position;

        foreach (Type type in _dynamicServiceTypes)
        {
            DrawServiceType(position, type);
            position.y += _lineHeight + _space;
        }

        List<Type> notUsedAdditionalTypes =
            _possibleAdditionalServiceTypes.Where(
                t => !_dynamicServiceTypes.Contains(t)).ToList();

        if (_source.additionalTypes != null)
            for (var index = 0; index < _additionalServiceTypes.Count; index++)
            {
                notUsedAdditionalTypes.Remove(_additionalServiceTypes[index].Type);
                DrawSerializableType(position, _source, _additionalServiceTypes, index, usedTypes);
                position.y += _lineHeight + _space;
            }



        bool isAnyNotUsedType = notUsedAdditionalTypes.Count > 0;
        Rect buttonPos = position;
        buttonPos.width -= actionBarWidth + _space;
        if (DrawButton(buttonPos, ListAction.Add, isAnyNotUsedType))
        { 
            _source.TryAddType(notUsedAdditionalTypes[index: 0]);
            _anyChange = true;
        }

        position.y += _lineHeight + _space;
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
        ServiceSource source,
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
            GUI.color = EditorHelper.ErrorBackgroundColor;
        }

        int newIndex = EditorGUI.Popup(position, index, elementsString);
        if (newIndex != index)
        {
            Type oldType = serializableType.Type;
            serializableType.Type = popupTypeList[newIndex];
            ServiceLocator.Environment.InvokeEnvironmentChangedOnTypes(oldType, serializableType.Type);
            _anyChange = true;
        }

        GUI.color = guiColor;

        position.x = position.xMax + _space;
        position.width = actionBarWidth;
        ListAction action = DrawActionBar(position, serializedTypes.Count, typeIndex);

        if (action != ListAction.Non)
        {
            if (action == ListAction.MoveUp)
                serializedTypes.Swap(typeIndex, typeIndex - 1);
            if (action == ListAction.MoveDown)
                serializedTypes.Swap(typeIndex, typeIndex + 1);
            if (action == ListAction.Delete)
                source.RemoveType(serializedTypes[typeIndex].Type); 
            _anyChange = true;
        }
    }

    static void DrawTags(Rect position)
    {
        var title = $"Tags ({_additionalTags.Count})";
        _source.isTagsExpanded = EditorGUI.Foldout(position, _source.isTagsExpanded, title);

        position.y += _lineHeight + _space;
        if (!_source.isTagsExpanded) return;
 
        if (_source.additionalTypes != null)
            for (var index = 0; index < _additionalTags.Count; index++)
            {
                DrawTag(position, _additionalTags, index);
                position.y += _lineHeight + _space;
            }


        Rect buttonPos = position;
        buttonPos.width -= actionBarWidth + _space;
        if (DrawButton(buttonPos, ListAction.Add, enabled: true))
        {
            _source.AddTag(new Tag());
            _anyChange = true;
        }

        position.y += _lineHeight + _space;
    }
    
    static void DrawTag(
        Rect position,
        IList<Tag> serializedTags,
        int tagIndex)
    {
        Tag tag = serializedTags[tagIndex];
        
        const float tagTypeWidth = 70;
        position.width -= 3 * actionButtonWidth + tagTypeWidth + _space; 
 
        Tag.TagType tagType = tag.Type;
        switch (tagType)
        {
            case Tag.TagType.String:
                string text = tag.StringTag;
                string newText = EditorGUI.TextField(position, text);
                if (newText != text)
                {
                    tag.StringTag = newText;
                    _anyChange = true;
                    ServiceLocator.Environment.InvokeEnvironmentChangedOnSource(_source);
                }

                break;
            case Tag.TagType.Object:
                Object unityObject = tag.UnityObjectTag;
                Object newObject =
                    EditorGUI.ObjectField(position, unityObject, typeof(Object), allowSceneObjects: true);
                if (newObject != unityObject)
                {
                    tag.UnityObjectTag = newObject;
                    _anyChange = true;
                    ServiceLocator.Environment.InvokeEnvironmentChangedOnSource(_source);
                }

                break;
            case Tag.TagType.Other:
                object objectTag = tag.OtherTypeTag;
                string objectTagText = objectTag == null
                    ? "null (Accessible From Code, Not Serialized)"
                    : objectTag.ToString();
                EditorGUI.LabelField(position, objectTagText);
                break;
        }

        position.x = position.xMax + _space;
        position.width = tagTypeWidth;
        var newTagType = (Tag.TagType) EditorGUI.EnumPopup(position, tagType);
        if (newTagType != tagType)
        {
            tag.Type = newTagType;
            _anyChange = true;
            ServiceLocator.Environment.InvokeEnvironmentChangedOnSource(_source);
        }
 

        position.x = position.xMax + _space;
        position.width = actionBarWidth;
        ListAction action = DrawActionBar(position, serializedTags.Count, tagIndex);

        if (action != ListAction.Non)
        {
            if (action == ListAction.MoveUp)
                serializedTags.Swap(tagIndex, tagIndex - 1);
            if (action == ListAction.MoveDown)
                serializedTags.Swap(tagIndex, tagIndex + 1);
            if (action == ListAction.Delete)
                _source.RemoveTag(serializedTags[tagIndex]);
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
        GUI.enabled = IsSourceEnabled;
        return result;
    }

    static bool IsSourceEnabled =>
        (_containingSet == null ||
         _containingSet.GetType() != typeof(SceneServiceInstaller) ||
         ((SceneServiceInstaller) _containingSet).isActiveAndEnabled) &&
        _source.Enabled;

}
}
#endif
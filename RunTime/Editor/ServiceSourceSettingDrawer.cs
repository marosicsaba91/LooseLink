#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq; 
using MUtility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices.Editor
{
static class ServiceSourceSettingDrawer 
{
    const float padding = 4;

    static void DrawSourceSetting(
        Rect position,
        IServiceSourceSet iSet,
        Object serializedObject,
        IList<ServiceSourceSetting> sourceSettings,
        int index)
    {
        EditorHelper.DrawBox(position);
        ServiceSourceSetting sourceSetting = sourceSettings[index];

        position = new Rect(
            position.x + padding, position.y += padding,
            position.width - (padding * 2), position.height - (padding * 2));
        float space = EditorGUIUtility.standardVerticalSpacing;
        const float toggleW = 14; 
        const float serviceTypeW = 100;

        if (sourceSetting == null)
            return;

        ServiceSource source = sourceSetting.GetServiceSource();
        ServiceSourceSet set = sourceSetting.GetServiceSourceSet();

        bool nonEmptySource = source != null && source.GetAllAbstractTypes(iSet).Any();

        var togglePos = new Rect(position.x + space, position.y, toggleW, EditorGUIUtility.singleLineHeight);
        bool enabled = EditorGUI.Toggle(togglePos, sourceSetting.enabled);
        if (enabled != sourceSetting.enabled)
        {
            Undo.RecordObject(serializedObject, (enabled ? "Enables" : "Disabled") + " Service Source");
            sourceSetting.enabled = enabled;
            EditorUtility.SetDirty(serializedObject);
        }


        const float actionButtonWidth = 20;
        float w = position.width - (toggleW + serviceTypeW + space * 4 + actionButtonWidth * 3);
        var objectPos = new Rect(togglePos.xMax + space*2, position.y, w, EditorGUIUtility.singleLineHeight);
        Object obj = EditorGUI.ObjectField(
            objectPos,
            sourceSetting.serviceSourceObject,
            typeof(Object),
            allowSceneObjects: true);

        var sourceTypePos =
            new Rect(objectPos.xMax + space, position.y, serviceTypeW, EditorGUIUtility.singleLineHeight);

        ServiceSourceTypes sourceType = sourceSetting.sourceType;
        if (nonEmptySource)
        {
            sourceType = source.SourceType;
            if (source.AlternativeSourceTypes.Any())
            {
                var options = new List<ServiceSourceTypes> {sourceType};
                options.AddRange(source.AlternativeSourceTypes);
                options.Sort();
                int currentIndex = options.IndexOf(sourceType);

                GUIContent[] guiContentOptions =
                    options.Select(
                            sst => LooseServiceRow.GetCategoryGUIContentForServiceSource(sst, withIcons: false))
                        .ToArray();

                int selectedIndex = EditorGUI.Popup(sourceTypePos, currentIndex, guiContentOptions);
                sourceType = options[selectedIndex];
            }
            else
            {
                GUIContent content = LooseServiceRow.GetCategoryGUIContentForServiceSource(source, withIcons: false);
                GUI.Label(sourceTypePos, content);
            }
        }
        else if (set != null)
            GUI.Label(sourceTypePos,
                new GUIContent($"Source Set ({set.GetServiceSources().Count()})"));
        else
            GUI.Label(sourceTypePos, new GUIContent("NO SERVICE"));

        // Object or source Type changed
        if (obj != sourceSetting.serviceSourceObject || sourceType != sourceSetting.sourceType)
        {
            Undo.RecordObject(serializedObject, "Setting Object Changed");

            if (serializedObject is ServiceSourceSet set1 && obj is ServiceSourceSet set2)
            {
                if (!ServiceSourceSet.IsCircular(set1, set2))
                    sourceSetting.serviceSourceObject = obj;
            }
            else
                sourceSetting.serviceSourceObject = obj;

            sourceSetting.sourceType = sourceType;
            sourceSetting.Clear();
            EditorUtility.SetDirty(serializedObject);
        }

        // Action Buttons 
        var actionButtonPos = new Rect(sourceTypePos.xMax + space, position.y, actionButtonWidth,
            EditorGUIUtility.singleLineHeight);
        GUI.enabled = index > 0;
        if (GUI.Button(actionButtonPos, GUIContent.none))
        {
            Undo.RecordObject(serializedObject, "Service Setting Moved Up");
            sourceSettings.Swap(index, index - 1);
            EditorUtility.SetDirty(serializedObject);
        }

        GUI.Label(actionButtonPos, upIcon, ActionButtonStyle);
        actionButtonPos.x += actionButtonWidth - 1;
        GUI.enabled = index < sourceSettings.Count - 1;
        if (GUI.Button(actionButtonPos, GUIContent.none))
        {
            Undo.RecordObject(serializedObject, "Service Setting Moved Down");
            sourceSettings.Swap(index, index + 1);
            EditorUtility.SetDirty(serializedObject);
        }

        GUI.Label(actionButtonPos, downIcon, ActionButtonStyle);
        actionButtonPos.x += actionButtonWidth - 1;
        GUI.enabled = true;
        if (GUI.Button(actionButtonPos, GUIContent.none))
        {
            Undo.RecordObject(serializedObject, "Service Setting Deleted");
            sourceSettings.RemoveAt(index);
            EditorUtility.SetDirty(serializedObject);
        }

        GUI.Label(actionButtonPos, deleteIcon, ActionButtonStyle);



        const float foldoutW = 11;
        // Draw types
        var typesPos = new Rect(
            position.x + foldoutW + space,
            position.y + EditorGUIUtility.singleLineHeight + padding,
            position.width - foldoutW - space,
            EditorGUIUtility.singleLineHeight);

        if (nonEmptySource)
            sourceSetting.isTypesExpanded = EditorGUI.Foldout(typesPos, sourceSetting.isTypesExpanded, "Types");
        
        typesPos.y += EditorGUIUtility.singleLineHeight;

        if (source != null)
            foreach (SerializableType type in sourceSetting.serializedTypes)
            {
                GUI.Label(typesPos, type.Name, ActionButtonStyle);
                typesPos.y += EditorGUIUtility.singleLineHeight;
            }
        
        // Draw tags
        
        if (nonEmptySource)
            sourceSetting.isTagsExpanded = EditorGUI.Foldout(typesPos, sourceSetting.isTagsExpanded, "Tags");

    }


    static readonly GUIStyle categoryPopupStyle = default;
    public static GUIStyle CategoryPopupStyle => categoryPopupStyle ?? new GUIStyle ("Button")
    {
        fontSize = 10,
        normal = {textColor = GUI.skin.label.normal.textColor},
    };

    
    static readonly GUIStyle actionButtonStyle = default;
    public static GUIStyle ActionButtonStyle => actionButtonStyle ?? new GUIStyle ("Label")
    {
        fontSize = 10,
        alignment = TextAnchor.MiddleCenter,
        normal = {textColor = GUI.skin.label.normal.textColor}
    };
      

    static float HeightOfSourceSetting(ServiceSourceSetting sourceSetting)
    {
        float oneLine = EditorGUIUtility.singleLineHeight + (2 * padding);

        if (sourceSetting == null ) return oneLine;

        ServiceSourceSet set = sourceSetting.GetServiceSourceSet();
        if (set != null) return oneLine;

        ServiceSource source = sourceSetting.GetServiceSource();
        if (source == null) return oneLine;

        var lineCount = 1;
        lineCount += sourceSetting.isTagsExpanded ? (2 + sourceSetting.serializedTags.Count) : 1;
        lineCount += sourceSetting.isTypesExpanded ? (2 + sourceSetting.serializedTypes.Count) : 1;
         
        return (EditorGUIUtility.singleLineHeight * lineCount) + (3 * padding);
    }


    public static void DrawServiceSources(
        List<ServiceSourceSetting> list,
        Object  targetObject, IServiceSourceSet set)
    {  
        if(list!= null)
            for (var i = 0; i < list.Count; i++)
            {
                Rect rect = EditorGUILayout.GetControlRect();
                DrawServiceSourceSetting(
                    set, list, i, targetObject, rect.position, rect.width, out float height);

                GUILayout.Space(height - EditorGUIUtility.singleLineHeight - 3);
            }

        if (GUILayout.Button("Add New Services Source"))
        {
            Undo.RecordObject(targetObject, "Add new service source setting.");
            list.Add(new ServiceSourceSetting());
            EditorUtility.SetDirty(targetObject);
        } 
    }

    public static void DrawServiceSourceSetting(
        IServiceSourceSet set,
        List<ServiceSourceSetting> sourceSettings,
        int index,
        Object serializedObject,
        Vector2 startPosition,
        float width,
        out float height)
    {
        height = HeightOfSourceSetting(sourceSettings[index]);
        var position = new Rect(startPosition, new Vector2(width, height));
        DrawSourceSetting(position, set, serializedObject, sourceSettings, index);
    }

    public static readonly GUIContent upIcon = EditorGUIUtility.IconContent("scrollup_uielements");
    public static readonly GUIContent downIcon = EditorGUIUtility.IconContent("scrolldown_uielements");
    public static readonly GUIContent deleteIcon = EditorGUIUtility.IconContent("winbtn_win_close");
}
}
#endif
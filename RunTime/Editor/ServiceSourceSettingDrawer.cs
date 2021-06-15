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
static class ServiceSourceSettingDrawer 
{
    const float padding = 4;

    static void DrawSourceSetting(
        Rect position,
        IServiceSourceSet iSet,
        Object serializedObject,
        List<ServiceSourceSetting> sourceSettings,
        int index)
    {
        EditorHelper.DrawBox(position);

        ServiceSourceSetting sourceSetting = sourceSettings[index];

        position = new Rect(
            position.x + padding, position.y += padding,
            position.width - (padding * 2), position.height - (padding * 2));
        float space = EditorGUIUtility.standardVerticalSpacing;
        const float toggleW = 14;
        const float foldoutW = 11;
        const float serviceTypeW = 100;

        if (sourceSetting == null)
            return;

        ServiceSource source = sourceSetting.GetServiceSource(iSet);
        ServiceSourceSet set = sourceSetting.GetServiceSourceSet(iSet);

        var foldoutPos = new Rect(position.x + space + foldoutW, position.y, foldoutW,
            EditorGUIUtility.singleLineHeight);

        bool nonEmptySource = source != null && source.GetAllAbstractTypes(iSet).Any();
        if (nonEmptySource)
            sourceSetting.isExpanded = EditorGUI.Foldout(foldoutPos, sourceSetting.isExpanded, GUIContent.none);

        var togglePos = new Rect(foldoutPos.x + space, position.y, toggleW, EditorGUIUtility.singleLineHeight);
        bool enabled = EditorGUI.Toggle(togglePos, sourceSetting.enabled);
        if (enabled != sourceSetting.enabled)
        {
            Undo.RecordObject(serializedObject, (enabled ? "Enables" : "Disabled") + " Service Source");
            sourceSetting.enabled = enabled;
            EditorUtility.SetDirty(serializedObject);
        }


        const float actionButtonWidth = 20;
        float w = position.width - (toggleW + foldoutW + serviceTypeW + space * 4 + actionButtonWidth * 3);
        var objectPos = new Rect(togglePos.xMax + space, position.y, w, EditorGUIUtility.singleLineHeight);
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

        if (!sourceSetting.isExpanded) return;


        var sourcesPos = new Rect(
            position.x + foldoutW + space,
            position.y + EditorGUIUtility.singleLineHeight + padding,
            position.width - foldoutW - space,
            EditorGUIUtility.singleLineHeight);

        if (source != null)
            foreach (LooseServiceRow row in GetAbstractTypeRows(iSet, source))
            {
                LooseServiceFoldoutColumn.DrawCell(sourcesPos, row, selectElement: false);
                sourcesPos.y += EditorGUIUtility.singleLineHeight;
            }

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
     
    static List<LooseServiceRow> GetAbstractTypeRows(IServiceSourceSet set,ServiceSource source)
    {
        List<LooseServiceRow> result = new List<LooseServiceRow>();
        foreach (Type serviceType in source.GetAllAbstractTypes(set))
        {
            var abstractTypeRow = new LooseServiceRow(LooseServiceRow.RowCategory.Service)
            {
                source = source,
                type = serviceType,
            };
            if (source.InstantiatedServices.ContainsKey(serviceType))
                abstractTypeRow.loadedInstance = source.InstantiatedObject;

            result.Add(abstractTypeRow);
        }

        return result;
    }


    static float HeightOfSourceSetting(ServiceSourceSetting sourceSetting, IServiceSourceSet iSet)
    {
        float oneLine = EditorGUIUtility.singleLineHeight + (2 * padding);

        if (sourceSetting == null || !sourceSetting.isExpanded) return oneLine;

        ServiceSourceSet set = sourceSetting.GetServiceSourceSet(iSet);
        if (set != null) return oneLine;

        ServiceSource source = sourceSetting.GetServiceSource(iSet);
        if (source == null) return oneLine;

        List<LooseServiceRow> lines = GetAbstractTypeRows(iSet, source);
        if (lines == null || lines.Count == 0) return oneLine;

        return EditorGUIUtility.singleLineHeight * (1 + lines.Count) + (3 * padding);
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
        height = HeightOfSourceSetting(sourceSettings[index], set);
        var position = new Rect(startPosition, new Vector2(width, height));
        DrawSourceSetting(position, set, serializedObject, sourceSettings, index);
    }

    public static readonly GUIContent upIcon = EditorGUIUtility.IconContent("scrollup_uielements");
    public static readonly GUIContent downIcon = EditorGUIUtility.IconContent("scrolldown_uielements");
    public static readonly GUIContent deleteIcon = EditorGUIUtility.IconContent("winbtn_win_close");
}
}
#endif
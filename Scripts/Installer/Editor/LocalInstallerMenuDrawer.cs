#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnityServiceLocator.Editor
{
static class LocalInstallerMenuDrawer
{
    public static void DrawLocalInstallerSettingMenu(
        SerializedObject serializedObject,
        InstallerComponent installerComponent)
    { 

        GUI.enabled = !Application.isPlaying;
        bool newDontDestroyOnLoad =
            EditorGUILayout.Toggle("Don't Destroy On Load", installerComponent.AutoDontDestroyOnLoad);
        if (newDontDestroyOnLoad != installerComponent.AutoDontDestroyOnLoad)
        {
            Undo.RecordObject(serializedObject.targetObject, "Scene Installer changed.");
            installerComponent.AutoDontDestroyOnLoad = newDontDestroyOnLoad;
            EditorUtility.SetDirty(serializedObject.targetObject);
        }

        GUI.enabled = true;

        LocalInstallerPriority priority = installerComponent.Priority;
        int priorityValue = priority.priorityValueSetting; 
        var newPriorityType = (LocalInstallerPriority.Type)
            EditorGUILayout.EnumPopup("Priority Type", priority.type);

        GUI.enabled = true;
        if (priority.type == LocalInstallerPriority.Type.ConcreteValue)
            priorityValue = EditorGUILayout.IntField("Priority", priorityValue);

        if (newPriorityType != priority.type || priorityValue != priority.priorityValueSetting)
        {
            priority.type = newPriorityType;
            priority.priorityValueSetting = priorityValue;
            Undo.RecordObject(serializedObject.targetObject, "Scene Installer Priority Changed.");
            installerComponent.Priority = priority;
            EditorUtility.SetDirty(serializedObject.targetObject);
        }
    }
}
}
#endif
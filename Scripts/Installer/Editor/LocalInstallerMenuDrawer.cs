#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LooseLink.Editor
{
	static class LocalInstallerMenuDrawer
	{
		public static void DrawLocalInstallerSettingMenu(
			SerializedObject serializedObject,
			InstallerComponent installerComponent)
		{
			bool installAutomatically = installerComponent.InstallAutomatically;

			if (installAutomatically)
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

				InstallerPriority priority = installerComponent.Priority;
				int priorityValue = priority.priorityValueSetting;
				var newPriorityType = (InstallerPriority.Type)
					EditorGUILayout.EnumPopup("Priority Type", priority.type);

				GUI.enabled = true;
				if (priority.type == InstallerPriority.Type.ConcreteValue)
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
}
#endif
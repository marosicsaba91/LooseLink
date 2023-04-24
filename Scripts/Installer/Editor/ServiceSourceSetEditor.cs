#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LooseLink.Editor
{
	[CustomEditor(typeof(ServiceSourceSet))]
	public class ServiceSourceSetEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			GUI.enabled = !Application.isPlaying;
			var set = target as ServiceSourceSet;

			bool newGI = EditorGUILayout.Toggle("Use as Global Installer", set.automaticallyUseAsGlobalInstaller);
			if (newGI != set.automaticallyUseAsGlobalInstaller)
			{
				Undo.RecordObject(serializedObject.targetObject, "GlobalInstaller changed.");
				set.automaticallyUseAsGlobalInstaller = newGI;
				EditorUtility.SetDirty(serializedObject.targetObject);
			}

			GUI.enabled = true;
			if (set.automaticallyUseAsGlobalInstaller)
			{
				int priority = EditorGUILayout.IntField("Priority", set.PriorityValue);
				if (set.PriorityValue != priority)
				{
					Undo.RecordObject(serializedObject.targetObject, "GlobalInstaller priority changed.");
					set.PriorityValue = priority;
					EditorUtility.SetDirty(serializedObject.targetObject);
				}
			}

			if (set.automaticallyUseAsGlobalInstaller && !set.IsInResources())
				EditorGUILayout.HelpBox(
					$"{set.name} should be in Resources to work as Global Installer",
					MessageType.Error);

			ServiceSourceDrawer.DrawInstallerInspectorGUI(this, set);
		}
	}
}
#endif
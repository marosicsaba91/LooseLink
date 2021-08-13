#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LooseServices.Editor
{

[CustomEditor(typeof(SceneServiceInstaller))]
public class SceneInstallerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    { 
        GUI.enabled = !Application.isPlaying;
        DrawDefaultInspector();
        GUI.enabled = true;
        var sceneInstaller = target as IServiceSourceSet; 
        this.DrawInstallerInspectorGUI(sceneInstaller);
    }
}

[CustomEditor(typeof(ServiceSourceSet))]
public class ServiceSourceSetEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        GUI.enabled = !Application.isPlaying;
        DrawDefaultInspector();
        var set = target as ServiceSourceSet;
        GUI.enabled = true;
        if (set.useAsGlobalInstaller)
        {
            int priority = EditorGUILayout.IntField("Priority", set.priority);
            if (set.priority != priority)
            {
                Undo.RecordObject(serializedObject.targetObject, "GlobalInstaller priority changed.");
                set.priority = priority;
                EditorUtility.SetDirty(serializedObject.targetObject);
            }

        }
        this.DrawInstallerInspectorGUI(set);
    }
}

static class InstallerEditorHelper
{
    public static void DrawInstallerInspectorGUI(this UnityEditor.Editor editor, IServiceSourceSet set)
    {  

        ServiceSourceDrawer.DrawServiceSources(
            set.ServiceSources,
            editor.serializedObject.targetObject);
        /*
        // Button To manually clear cached data.
        GUILayout.Space(pixels: 10);
        if (GUILayout.Button("Clear Cache"))
        {
            Undo.RecordObject(editor.serializedObject.targetObject, "Add new service source setting.");
            set.ClearDynamicData();
        }
        */
    }
}
}
#endif
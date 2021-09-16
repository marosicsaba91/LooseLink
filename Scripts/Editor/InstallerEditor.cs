#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnityServiceLocator.Editor
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
        var set = target as ServiceSourceSet;

        bool newGI = EditorGUILayout.Toggle("Use as Global Installer", set.automaticallyUseAsGlobalInstaller);
        if (newGI != set.automaticallyUseAsGlobalInstaller)
        {
            Undo.RecordObject(serializedObject.targetObject, "GlobalInstaller changed.");
            set.automaticallyUseAsGlobalInstaller = newGI;
            ServiceLocator.FreshEnvironment();
            EditorUtility.SetDirty(serializedObject.targetObject);
        }

        GUI.enabled = true;
        if (set.automaticallyUseAsGlobalInstaller)
        {
            int priority = EditorGUILayout.IntField("Priority", set.Priority);
            if (set.Priority != priority)
            {
                Undo.RecordObject(serializedObject.targetObject, "GlobalInstaller priority changed.");
                set.Priority = priority;
                EditorUtility.SetDirty(serializedObject.targetObject);
            }
        }
        
        if(set.automaticallyUseAsGlobalInstaller && !set.IsInResources())
            EditorGUILayout.HelpBox(
                $"{set.name} should be in Resources to work as Global Installer",
                MessageType.Error );
        
        this.DrawInstallerInspectorGUI(set);
    }
}

static class InstallerEditorHelper
{
    public static void DrawInstallerInspectorGUI(this UnityEditor.Editor editor, IServiceSourceSet set)
    {
        ServiceSourceDrawer.DrawServiceSources(
            set,
            set.ServiceSources,
            editor.serializedObject.targetObject);
        
        if(!Application.isPlaying) return;
        
        GUILayout.Space(pixels: 10);
        if (GUILayout.Button("Clear Cached Data"))
        {
            Undo.RecordObject(editor.serializedObject.targetObject, "Add new service source setting.");
            set.ClearDynamicData();
            ServiceLocator.Environment.InvokeEnvironmentChangedOnWholeEnvironment();
        }
    }
}
}
#endif
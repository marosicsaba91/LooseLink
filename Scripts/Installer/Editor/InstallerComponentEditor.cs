#if UNITY_EDITOR
using UnityEditor;

namespace LooseLink.Editor
{
	[CustomEditor(typeof(InstallerComponent), editorForChildClasses: true)]
	public class InstallerComponentEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var sourceSet = target as IServiceSourceProvider;
			var localInstaller = target as InstallerComponent;

			LocalInstallerMenuDrawer.DrawLocalInstallerSettingMenu(serializedObject, localInstaller);
			ServiceSourceDrawer.DrawInstallerInspectorGUI(this, sourceSet);
		}
	}
}
#endif
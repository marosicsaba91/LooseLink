using System;
using UnityEngine;
using Object = UnityEngine.Object;
using EasyEditor;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LooseLink
{
	static class IconHelper
	{
		public enum FileType
		{
			Prefab,
			GameObject,
			ScriptableObject,
			CsFile,
		}

		public static Texture GetIconOfSource(FileType fileType)
		{
#if UNITY_EDITOR
			return fileType switch
			{
				FileType.Prefab => EditorGUIUtility.IconContent("Prefab Icon").image,
				FileType.GameObject => EditorGUIUtility.IconContent("GameObject Icon").image,
				FileType.ScriptableObject => EditorGUIUtility.IconContent("ScriptableObject Icon").image,
				FileType.CsFile => EditorGUIUtility.IconContent("cs Script Icon").image,
				_ => null
			};
#else
        return null;
#endif
		}

#if UNITY_EDITOR
		static readonly Texture warningImage = EditorGUIUtility.IconContent("console.warnicon.sml").image;
		static readonly Texture errorImage = EditorGUIUtility.IconContent("console.erroricon.sml").image;
		static readonly Texture successIcon = EditorGUIUtility.IconContent("TestPassed").image;
		static readonly Texture blockedIcon = EditorHelper.GetIcon(IconType.Cross);
		static readonly Texture resolvableIcon = EditorGUIUtility.IconContent("FilterSelectedOnly").image;
#endif

		internal static Texture SuccessIcon
		{
			get
			{
#if UNITY_EDITOR
				return successIcon;
#else
            return null;
#endif
			}
		}

		internal static Texture WarningIcon
		{
			get
			{
#if UNITY_EDITOR
				return warningImage;
#else
            return null;
#endif
			}
		}

		internal static Texture ErrorIcon
		{
			get
			{
#if UNITY_EDITOR
				return errorImage;
#else
            return null;
#endif
			}
		}

		internal static Texture BlockedIcon
		{
			get
			{
#if UNITY_EDITOR
				return blockedIcon;
#else
            return null;
#endif
			}
		}
		internal static Texture ResolvableIcon
		{
			get
			{
#if UNITY_EDITOR
				return resolvableIcon;
#else
            return null;
#endif
			}
		}

		public static GUIContent GetGUIContentToType(ServiceTypeInfo typeInfo)
		{
			Type type = typeInfo.type;
			string name = typeInfo.name;
			string fullName = typeInfo.fullName;
			bool isMissing = typeInfo.isMissing;

			if (type == null)
				return new GUIContent(name, ErrorIcon, $"Types \"{fullName}\" Is Missing!");

			Texture texture = isMissing ? ErrorIcon : GetIconOfType(type);
			if (texture == null)
				texture = GetIconOfSource(FileType.CsFile);
			string tooltip = $"{fullName} ({GetTypeCategory(type)})";
			return new GUIContent(name, texture, tooltip);
		}

		public static Texture GetIconOfObject(Object obj)
		{
#if UNITY_EDITOR
			return EditorGUIUtility.ObjectContent(obj, obj.GetType()).image;
#else
        return null;
#endif
		}

		public static Texture GetIconOfType(Type type)
		{
#if UNITY_EDITOR
			return EditorGUIUtility.ObjectContent(obj: null, type).image;
#else
        return null;
#endif
		}

		internal static string GetTypeCategory(Type type)
		{
			if (type.IsInterface)
				return "Interface";
			if (type.IsAbstract)
			{
				if (type.IsSubclassOf(typeof(MonoBehaviour)))
					return "Abstract MonoBehaviour class";
				if (type.IsSubclassOf(typeof(ScriptableObject)))
					return "Abstract ScriptableObject class";
				return "Abstract ScriptableObject class";
			}
			if (type.IsSubclassOf(typeof(MonoBehaviour)))
				return "MonoBehaviour class";
			if (type.IsSubclassOf(typeof(ScriptableObject)))
				return "ScriptableObject class";
			if (type.IsSubclassOf(typeof(Component)))
				return "Component class";

			if (type.IsClass)
				return "Class";

			return "Type";
		}


		internal static string GetTooltipForISet(IServiceSourceProvider iProvider)
		{
			switch (iProvider)
			{
				case LocalServiceInstaller _:
					return "Scene Service Installer: Service sources are available if the Component is in scene and enabled.";
				case ServiceSourceSet set when set.automaticallyUseAsGlobalInstaller:
					return "Global Service Installer: Service Sources are available always";
				case ServiceSourceSet _:
					return "Service Source Set";
				default:
					return unexpectedCategoryText;
			}
		}

		internal static string GetShortNameForServiceSource(ServiceSourceTypes sourceType)
		{
			switch (sourceType)
			{
				case ServiceSourceTypes.FromPrefabPrototype:
					return "Prefab Proto.";
				case ServiceSourceTypes.FromPrefabFile:
					return "Prefab File";
				case ServiceSourceTypes.FromScriptableObjectFile:
					return "SO. File";
				case ServiceSourceTypes.FromScriptableObjectPrototype:
					return "SO. Proto.";
				case ServiceSourceTypes.FromSceneGameObject:
					return "Scene GameObj.";
				case ServiceSourceTypes.FromScriptableObjectType:
					return "SO. script";
				case ServiceSourceTypes.FromMonoBehaviourType:
					return "MB. script";
				default:
					return unexpectedCategoryText;
			}
		}

		internal static string GetTooltipForServiceSource(ServiceSourceTypes sourceType)
		{
			switch (sourceType)
			{
				case ServiceSourceTypes.FromPrefabPrototype:
					return "Prefab Prototype: Service Creates an instance  the selected Prefab file.";
				case ServiceSourceTypes.FromPrefabFile:
					return "Prefab File: Service Gives back the Prefab File's Components as Services.";
				case ServiceSourceTypes.FromScriptableObjectFile:
					return "ScriptableObject File: References the selected ScriptableObject file.";
				case ServiceSourceTypes.FromScriptableObjectPrototype:
					return "ScriptableObject Prototype: Creates a copy of the selected ScriptableObject file.";
				case ServiceSourceTypes.FromSceneGameObject:
					return "Scene GameObject: GameObject in Scene with Component as Services.";
				case ServiceSourceTypes.FromScriptableObjectType:
					return "ScriptableObject Script: Creates a new default instance of the selected ScriptableObject class.";
				case ServiceSourceTypes.FromMonoBehaviourType:
					return "MonoBehaviour Script: Creates a new GameObject with a the selected Component on it.";
				default:
					return unexpectedCategoryText;
			}
		}


		internal static string GetNameForServiceSourceCategory(ServiceSourceTypes sourceType)
		{
			switch (sourceType)
			{
				case ServiceSourceTypes.FromPrefabPrototype:
					return "Prefab Prototype";
				case ServiceSourceTypes.FromPrefabFile:
					return "Prefab File";
				case ServiceSourceTypes.FromScriptableObjectFile:
					return "ScriptableObj. File";
				case ServiceSourceTypes.FromScriptableObjectPrototype:
					return "ScriptableO. Proto.";
				case ServiceSourceTypes.FromSceneGameObject:
					return "Scene GameObject";
				case ServiceSourceTypes.FromScriptableObjectType:
					return "ScriptableO. Script";
				case ServiceSourceTypes.FromMonoBehaviourType:
					return "MonoBehaviour Script";
				default:
					return unexpectedCategoryText;
			}
		}

		internal static string GetShortNameForServiceSourceCategory(ServiceSourceTypes sourceType)
		{
			switch (sourceType)
			{
				case ServiceSourceTypes.FromPrefabPrototype:
					return "P. Proto.";
				case ServiceSourceTypes.FromPrefabFile:
					return "P. File";
				case ServiceSourceTypes.FromScriptableObjectFile:
					return "SO. File";
				case ServiceSourceTypes.FromScriptableObjectPrototype:
					return "SO. Proto.";
				case ServiceSourceTypes.FromSceneGameObject:
					return "Scene GO.";
				case ServiceSourceTypes.FromScriptableObjectType:
					return "SO. Script";
				case ServiceSourceTypes.FromMonoBehaviourType:
					return "MB. Script";
				default:
					return unexpectedCategoryText;
			}
		}

		const string unexpectedCategoryText = "Error: Unexpected Category";

	}
}
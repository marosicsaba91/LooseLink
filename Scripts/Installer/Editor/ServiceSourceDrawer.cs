#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink.Editor
{
	static class ServiceSourceDrawer
	{
		const float padding = 4;
		const float foldoutW = 18;
		const float toggleW = 14;
		const float serviceTypeW = 100;
		const float actionButtonWidth = 20;
		const float actionBarWidth = actionButtonWidth * 3 - 2;

		static readonly float space = EditorGUIUtility.standardVerticalSpacing;
		static readonly float lineHeight = EditorGUIUtility.singleLineHeight;

		static readonly GUIContent invalidObjectContent = new GUIContent("Invalid Object", typeTooltip);
		static readonly GUIContent noObjectContent = new GUIContent("Select an Object", typeTooltip);

		const string typeTooltip =
			"Selected object should be one of these:\n" +
			"   In Scene Game Object\n" +
			"   Prefab\n" +
			"   Scriptable Object\n" +
			"   Non abstract MonoBehaviour Script\n" +
			"   Non abstract ScriptableObject Script\n" +
			"   Service Set File";

		static readonly GUIStyle categoryPopupStyle = default;

		static GUIStyle LeftAlignedButtonStyle => categoryPopupStyle ?? new GUIStyle("Button")
		{
			alignment = TextAnchor.MiddleLeft
		};

		static readonly GUIStyle actionButtonStyle = default;

		static GUIStyle ActionButtonStyle => actionButtonStyle ?? new GUIStyle("Label")
		{
			fontSize = 10,
			alignment = TextAnchor.MiddleCenter,
			normal = { textColor = GUI.skin.label.normal.textColor }
		};

		static bool _anyChange;

		static IServiceSourceProvider _containingProvider;
		static ServiceSourceSet _insideSet;
		static ServiceSource _source;
		static int _sourceIndex;
		static Object _serializedObject;
		static List<Type> _dynamicServiceTypes = new List<Type>();
		static List<SerializableType> _additionalServiceTypes = new List<SerializableType>();
		static List<Type> _possibleAdditionalServiceTypes = new List<Type>();
		static List<Tag> _serializedTags = new List<Tag>();
		static List<object> _dynamicTags = new List<object>();
		static List<IServiceSourceCondition> _conditions = new List<IServiceSourceCondition>();
		static int _typeCount;
		static int _tagCount;

		public static void DrawInstallerInspectorGUI(UnityEditor.Editor editor, IServiceSourceProvider provider)
		{
			DrawServiceSources(
				provider,
				editor.serializedObject.targetObject);

			if (!Application.isPlaying)
				return;

			GUILayout.Space(pixels: 10);
			if (GUILayout.Button("Clear Cached Data"))
			{
				Undo.RecordObject(editor.serializedObject.targetObject, "Add new service source setting.");
				provider.ClearDynamicData_NoEnvironmentChangeEvent();
				Services.Environment.InvokeEnvironmentChangedOnWholeEnvironment();
			}
		}

		public static void DrawServiceSources(
			IServiceSourceProvider containingProvider,
			Object serializedObject)
		{
			for (int i = 0; i < containingProvider.SourceCount; i++)
			{
				ServiceSource sourceSource = containingProvider.GetSourceAt(i);
				Rect rect = EditorGUILayout.GetControlRect();
				float height = DrawServiceSource(sourceSource, containingProvider, i, serializedObject, rect.position,
					rect.width);

				GUILayout.Space(height - lineHeight - 3);
			}

			if (containingProvider.IsSingleSourceProvider)
				return;

			GUILayout.Space(pixels: 10);
			if (GUILayout.Button("Add New Services Source"))
			{
				Undo.RecordObject(serializedObject, "Add new service source setting.");
				containingProvider.AddSource(sourceObject: null);
				EditorUtility.SetDirty(serializedObject);
			}
		}

		static float DrawServiceSource(
			ServiceSource sourceSource,
			IServiceSourceProvider containingProvider,
			int index,
			Object serializedObject,
			Vector2 startPosition,
			float width)
		{
			_containingProvider = containingProvider;
			_serializedObject = serializedObject;
			_source = sourceSource;
			_sourceIndex = index;
			if (_source == null)
				return 0;
			_insideSet = _source.GetServiceSourceSet();
			_source.ClearCachedTypes_NoEnvironmentChangeEvent();
			_additionalServiceTypes = _source.additionalTypes;
			_serializedTags = _source.SerializedTags;
			_dynamicTags = _source.DynamicTags?.ToList();
			_conditions = _source.Conditions;
			_possibleAdditionalServiceTypes = _source?.GetPossibleAdditionalTypes()?.ToList() ?? new List<Type>();
			_dynamicServiceTypes = _source?.GetDynamicServiceTypes()?.ToList();
			_typeCount = (_dynamicServiceTypes?.Count ?? 0) + (_additionalServiceTypes?.Count ?? 0);
			_tagCount = (_dynamicTags?.Count ?? 0) + (_serializedTags?.Count ?? 0);

			float height = PixelHeightOfSource();
			var position = new Rect(startPosition, new Vector2(width, height));
			DrawSource(position);
			GUI.enabled = true;
			return height;
		}

		static float PixelHeightOfSource()
		{
			float oneLine = lineHeight + (2 * padding);

			if (_source == null)
				return oneLine;

			ServiceSourceSet set = _source.GetServiceSourceSet();
			if (set != null)
				return oneLine;

			if (!_source.IsServiceSource)
				return oneLine;

			int lineCount = 2;
			if (_source.isTypesExpanded)
				lineCount += 1 + _typeCount;

			if (ServiceLocationSetupData.Instance.enableTags)
			{
				lineCount++;
				if (_source.isTagsExpanded)
					lineCount += 1 + _tagCount;
			}

			lineCount += _conditions.Count;


			return ((lineHeight + space) * lineCount) + (2 * padding);
		}


		static void DrawSource(Rect position)
		{
			Color color = _sourceIndex % 2 != 0 ? EditorHelper.tableOddLineColor : EditorHelper.tableEvenLineColor;
			EditorHelper.DrawBox(position, color);

			Undo.RecordObject(_serializedObject, "Service Setting Modified");
			_anyChange = false;
			Rect typesPos = DrawHeader(position);

			if (_source.IsServiceSource)
			{
				typesPos.x += foldoutW + space;
				typesPos.width -= foldoutW + space;

				Rect tagsPosition = DrawServices(typesPos);

				Rect conditionsPosition = DrawTags(tagsPosition);

				DrawConditions(conditionsPosition);
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

			float x = position.x;
			float withRemained = position.width;

			if (!_containingProvider.IsSingleSourceProvider)
			{
				var togglePos = new Rect(position.x + space, position.y, toggleW, lineHeight);
				bool enabled = EditorGUI.Toggle(togglePos, _source.Enabled);
				if (enabled != _source.Enabled)
				{
					_anyChange = true;
					_source.Enabled = enabled;
				}

				x = togglePos.xMax;
				withRemained -= togglePos.width;
			}

			float w = withRemained - (serviceTypeW + space * 4 + actionButtonWidth * 3);
			var objectPos = new Rect(x + space * 2, position.y, w, lineHeight);
			GUI.enabled = !_containingProvider.IsSingleSourceProvider;
			Object obj = EditorGUI.ObjectField(
				objectPos,
				_source.ServiceSourceObject,
				typeof(Object),
				allowSceneObjects: true);
			GUI.enabled = true;

			var sourceTypePos = new Rect(objectPos.xMax + space, position.y, serviceTypeW, lineHeight);

			ServiceSourceTypes sourceType = _source.PreferredSourceType;
			if (_source.ServiceSourceObject == null)
				GUI.Label(sourceTypePos, noObjectContent);
			else if (_source.IsServiceSource)
			{
				sourceType = _source.SourceType;
				if (_source.AlternativeSourceTypes.Any())
				{
					var options = new List<ServiceSourceTypes> { sourceType };
					options.AddRange(_source.AlternativeSourceTypes);
					options.Sort();
					int currentIndex = options.IndexOf(sourceType);

					var guiContentOptions = new GUIContent[options.Count];
					for (int i = 0; i < options.Count; i++)
					{
						ServiceSourceTypes option = options[i];
						guiContentOptions[i] =
							new GUIContent(IconHelper.GetShortNameForServiceSource(option),
								image: null,
								IconHelper.GetTooltipForServiceSource(option));
					}

					int selectedIndex = EditorGUI.Popup(sourceTypePos, currentIndex, guiContentOptions);
					sourceType = options[selectedIndex];
				}
				else
				{
					var content = new GUIContent(IconHelper.GetShortNameForServiceSource(_source.SourceType),
						image: null,
						IconHelper.GetTooltipForServiceSource(_source.SourceType));
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

			if (!_containingProvider.IsSingleSourceProvider)
			{
				// Action Buttons 
				var actionButtonPos = new Rect(sourceTypePos.xMax + space, position.y, actionBarWidth, lineHeight);
				ListAction action = DrawActionBar(actionButtonPos, _containingProvider.SourceCount, _sourceIndex);
				if (action != ListAction.Non)
				{
					if (action == ListAction.MoveUp)
					{
						_containingProvider.SwapSources(_sourceIndex, _sourceIndex - 1);
						ServiceSource source1 = _containingProvider.GetSourceAt(_sourceIndex);
						ServiceSource source2 = _containingProvider.GetSourceAt(_sourceIndex - 1);
						Services.Environment.InvokeEnvironmentChangedOnSources(source1, source2);
					}

					if (action == ListAction.MoveDown)
					{
						_containingProvider.SwapSources(_sourceIndex, _sourceIndex + 1);
						ServiceSource source1 = _containingProvider.GetSourceAt(_sourceIndex);
						ServiceSource source2 = _containingProvider.GetSourceAt(_sourceIndex + 1);
						Services.Environment.InvokeEnvironmentChangedOnSources(source1, source2);
					}

					if (action == ListAction.Delete)
					{
						_containingProvider.RemoveSourceAt(_sourceIndex);
						Services.Environment.InvokeEnvironmentChangedOnSource(_source);
					}

					_anyChange = true;
				}
			}

			position.y += lineHeight + space;
			position.height = lineHeight;
			return position;
		}

		static Rect DrawServices(Rect position)
		{
			string title = $"Services ({_typeCount})";
			_source.isTypesExpanded = EditorGUI.Foldout(position, _source.isTypesExpanded, title);

			List<Type> usedTypes = _additionalServiceTypes?
				.Select(st => st.Type)
				.Where(t => t != null)
				.Union(_dynamicServiceTypes)
				.ToList();


			position.y += lineHeight + space;
			if (!_source.isTypesExpanded)
				return position;

			foreach (Type type in _dynamicServiceTypes)
			{
				DrawServiceType(position, type);
				position.y += lineHeight + space;
			}

			List<Type> notUsedAdditionalTypes = _possibleAdditionalServiceTypes?.Where(
				t => !_dynamicServiceTypes.Contains(t)).ToList();

			if (_source.additionalTypes != null)
				for (int index = 0; index < _additionalServiceTypes.Count; index++)
				{
					notUsedAdditionalTypes?.Remove(_additionalServiceTypes[index].Type);
					DrawSerializableType(position, _source, _additionalServiceTypes, index, usedTypes);
					position.y += lineHeight + space;
				}



			bool isAnyNotUsedType = !notUsedAdditionalTypes.IsNullOrEmpty();
			Rect buttonPos = position;
			buttonPos.width -= actionBarWidth + space;
			if (DrawButton(buttonPos, ListAction.Add, isAnyNotUsedType))
			{
				_source.TryAddServiceType(notUsedAdditionalTypes[index: 0]);
				_anyChange = true;
			}

			position.y += lineHeight + space;
			return position;
		}

		static void DrawServiceType(Rect position, Type type)
		{
			position.width -= 3 * actionButtonWidth;
			DrawPingBox(position, TypeToFileHelper.GetObject(type), type.ToString());
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
			string[] elementsString = new string[itemCount];
			for (int i = 0; i < popupTypeList.Count; i++)
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
				Services.Environment.InvokeEnvironmentChangedOnTypes(oldType, serializableType.Type);
				_anyChange = true;
			}

			GUI.color = guiColor;

			position.x = position.xMax + space;
			position.width = actionBarWidth;
			ListAction action = DrawActionBar(position, serializedTypes.Count, typeIndex);

			if (action != ListAction.Non)
			{
				if (action == ListAction.MoveUp)
					serializedTypes.Swap(typeIndex, typeIndex - 1);
				if (action == ListAction.MoveDown)
					serializedTypes.Swap(typeIndex, typeIndex + 1);
				if (action == ListAction.Delete)
					source.RemoveServiceTypeType(serializedTypes[typeIndex].Type);
				_anyChange = true;
			}
		}

		static Rect DrawTags(Rect position)
		{
			if (_serializedTags == null || !ServiceLocationSetupData.Instance.enableTags)
			{
				position.x -= foldoutW;
				return position;
			}

			string title = $"Tags ({_tagCount})";
			_source.isTagsExpanded = EditorGUI.Foldout(position, _source.isTagsExpanded, title);

			position.y += lineHeight + space;
			if (!_source.isTagsExpanded)
			{
				position.x -= foldoutW;
				return position;
			}

			bool changeable = !_source.IsTypesAndTagsComingFromDifferentServiceSourceComponent;

			foreach (object tag in _dynamicTags)
			{
				DrawDynamicTag(position, tag);
				position.y += lineHeight + space;
			}

			if (_source.additionalTypes != null)
				for (int index = 0; index < _serializedTags.Count; index++)
				{
					DrawTag(position, _serializedTags, index, changeable);
					position.y += lineHeight + space;
				}

			Rect buttonPos = position;
			buttonPos.width -= actionBarWidth + space;
			if (DrawButton(buttonPos, ListAction.Add, changeable))
			{
				_source.AddTag(new Tag());
				_anyChange = true;
			}

			position.y += lineHeight + space;
			position.x -= foldoutW;
			return position;
		}

		static void DrawDynamicTag(Rect position, object tag)
		{
			position.width -= 3 * actionButtonWidth;
			if (position.width <= 0)
				return;
			TagsColumn.DrawTag(position, new Tag(tag), small: false, center: false);
		}

		static void DrawTag(
			Rect position,
			IList<Tag> tagList,
			int tagIndex,
			bool changeable)
		{
			Tag tag = tagList[tagIndex];
			Tag.TagType tagType = tag.Type;
			string text = string.Empty;
			Type tagTypeExact = null;

			switch (tagType)
			{
				case Tag.TagType.String:
					text = tag.StringTag;
					tagTypeExact = typeof(string);
					break;
				case Tag.TagType.Object:
					text = tag.UnityObjectTag == null ? "" : tag.UnityObjectTag.name;
					tagTypeExact = typeof(Object);
					break;
				case Tag.TagType.Other:
					text = tag.OtherTypeTag == null ? "null" : tag.OtherTypeTag.ToString();
					tagTypeExact = tag.OtherTypeTag?.GetType();
					break;
			}

			if (changeable)
			{
				const float tagTypeWidth = 70;
				position.width -= 3 * actionButtonWidth + tagTypeWidth + space;

				switch (tagType)
				{
					case Tag.TagType.String:
						string newText = EditorGUI.TextField(position, text);
						if (newText != text)
						{
							tag.StringTag = newText;
							_anyChange = true;
							Services.Environment.InvokeEnvironmentChangedOnSource(_source);
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
							Services.Environment.InvokeEnvironmentChangedOnSource(_source);
						}

						break;
					case Tag.TagType.Other:
						DrawPingBox(position, tag.TagObject,
							tag.TagObject == null ? "null" : $"{text} ({tagTypeExact})");
						break;
				}

				position.x = position.xMax + space;
				position.width = tagTypeWidth;
				var newTagType = (Tag.TagType)EditorGUI.EnumPopup(position, tagType);
				if (newTagType != tagType)
				{
					tag.Type = newTagType;
					_anyChange = true;
					Services.Environment.InvokeEnvironmentChangedOnSource(_source);
				}


				position.x = position.xMax + space;
				position.width = actionBarWidth;
				ListAction action = DrawActionBar(position, tagList.Count, tagIndex);

				if (action != ListAction.Non)
				{
					if (action == ListAction.MoveUp)
						tagList.Swap(tagIndex, tagIndex - 1);
					if (action == ListAction.MoveDown)
						tagList.Swap(tagIndex, tagIndex + 1);
					if (action == ListAction.Delete)
						_source.RemoveTag(tagList[tagIndex]);
					_anyChange = true;
				}
			}
			else
			{
				position.width -= 3 * actionButtonWidth;
				DrawPingBox(position, tag.TagObject, $"{text} ({text})");
			}
		}

		static void DrawPingBox(Rect position, object obj, string text)
		{
			if (obj is Object o)
			{
				if (GUI.Button(position, $"{text} ({text})", LeftAlignedButtonStyle))
					EditorGUIUtility.PingObject(o);
			}
			else
				EditorHelper.DrawButtonLikeBox(position, text, TextAnchor.MiddleLeft);
		}


		static void DrawConditions(Rect position)
		{
			if (_conditions == null)
				return;
			foreach (IServiceSourceCondition condition in _conditions)
			{
				bool success = condition.CanResolve();
				string message = condition.GetConditionMessage();
				var content = new GUIContent(message, success ? IconHelper.SuccessIcon : IconHelper.BlockedIcon);
				GUI.Label(position, content);
				position.y += lineHeight + space;
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

			ListAction result = ListAction.Non;
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

		static readonly GUIContent upIcon = EditorGUIUtility.IconContent("scrollup_uielements");
		static readonly GUIContent downIcon = EditorGUIUtility.IconContent("scrolldown_uielements");
		static readonly GUIContent deleteIcon = EditorGUIUtility.IconContent("winbtn_win_close");
		static readonly GUIContent addNewIcon = EditorGUIUtility.IconContent("CreateAddNew");

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
			GUI.enabled = true;
			return result;
		}

		static bool IsSourceEnabled
		{
			get
			{
				if (!_source.Enabled)
					return false;
				if (_containingProvider == null)
					return true;
				if (_containingProvider.GetType() != typeof(LocalServiceInstaller))
					return true;
				if (string.IsNullOrEmpty(((LocalServiceInstaller)_containingProvider).gameObject.scene.name))
					return true;
				if (((LocalServiceInstaller)_containingProvider).isActiveAndEnabled)
					return true;
				return false;
			}
		}
	}
}
#endif
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using EasyEditor;
using MUtility;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink.Editor
{
	[NoAutoStaticsCleanup]
	static class ServiceSourceDrawer
	{
		const float _padding = 4;
		const float _foldoutW = 18;
		const float _toggleW = 14;
		const float _serviceTypeW = 100;
		const float _actionButtonWidth = 20;
		const float _actionBarWidth = _actionButtonWidth * 3 - 2;

		static readonly float _space = EditorGUIUtility.standardVerticalSpacing;
		static readonly float _lineHeight = EditorGUIUtility.singleLineHeight;

		static readonly GUIContent _invalidObjectContent = new("Invalid Object", _typeTooltip);
		static readonly GUIContent _noObjectContent = new("Select an Object", _typeTooltip);

		const string _typeTooltip =
			"Selected object should be one of these:\n" +
			"   In Scene Game Object\n" +
			"   Prefab\n" +
			"   Scriptable Object\n" +
			"   Non abstract MonoBehaviour Script\n" +
			"   Non abstract ScriptableObject Script\n" +
			"   Service Set File";

		static GUIStyle _categoryPopupStyle;

		static GUIStyle LeftAlignedButtonStyle => _categoryPopupStyle ??= new GUIStyle("Button")
		{
			alignment = TextAnchor.MiddleLeft
		};

		static GUIStyle _actionButtonStyle;

		static GUIStyle ActionButtonStyle => _actionButtonStyle ??= new GUIStyle("Label")
		{
			fontSize = 10,
			alignment = TextAnchor.MiddleCenter,
			normal =
			{
				textColor = GUI.skin.label.normal.textColor
			}
		};

		static bool _anyChange;

		static IServiceSourceProvider _containingProvider;
		static ServiceSourceSet _insideSet;
		static ServiceSource _source;
		static int _sourceIndex;
		static Object _serializedObject;
		static List<SerializableType> _additionalServiceTypes = new();
		static readonly List<Type> _dynamicServiceTypes = new();
		static readonly List<Type> _possibleAdditionalServiceTypes = new();
		static List<IServiceSourceCondition> _conditions = new();
		static int _typeCount;

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
				Services.Environment.MarkChanged();
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

				GUILayout.Space(height - _lineHeight - 3);
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
			_conditions = _source.Conditions;
			_possibleAdditionalServiceTypes.Clear();
			_source?.CollectPossibleAdditionalTypes(_possibleAdditionalServiceTypes);
			_dynamicServiceTypes.Clear();
			_source?.CollectDynamicServiceTypes_NotSet(_dynamicServiceTypes);
			_typeCount = (_dynamicServiceTypes?.Count ?? 0) + (_additionalServiceTypes?.Count ?? 0);

			float height = PixelHeightOfSource();
			Rect position = new(startPosition, new Vector2(width, height));
			DrawSource(position);
			GUI.enabled = true;
			return height;
		}

		static float PixelHeightOfSource()
		{
			float oneLine = _lineHeight + (2 * _padding);

			if (_source == null)
				return oneLine;

			ServiceSourceSet set = _source.GetServiceSourceSet();
			if (set != null)
				return oneLine;

			if (!_source.IsSourceAndNotSet)
				return oneLine;

			int lineCount = 2;
			if (_source.isTypesExpanded)
				lineCount += 1 + _typeCount;

			lineCount += _conditions.Count;


			return ((_lineHeight + _space) * lineCount) + (2 * _padding);
		}


		static void DrawSource(Rect position)
		{
			Color color = _sourceIndex % 2 != 0 ? EditorHelper.tableOddLineColor : EditorHelper.tableEvenLineColor;
			EditorHelper.DrawBox(position, color);

			Undo.RecordObject(_serializedObject, "Service Setting Modified");
			_anyChange = false;
			Rect typesPos = DrawHeader(position);

			if (_source.IsSourceAndNotSet)
			{
				typesPos.x += _foldoutW + _space;
				typesPos.width -= _foldoutW + _space;

				Rect conditionsPosition = DrawServices(typesPos);

				DrawConditions(conditionsPosition);
			}

			if (_anyChange)
				EditorUtility.SetDirty(_serializedObject);
		}

		static readonly List<ServiceSource> _sourceCache = new();
		static Rect DrawHeader(Rect position)
		{
			position.x += _padding;
			position.y += _padding;
			position.width -= _padding * 2;
			position.height -= _padding * 2;

			float x = position.x;
			float withRemained = position.width;

			if (!_containingProvider.IsSingleSourceProvider)
			{
				Rect togglePos = new(position.x + _space, position.y, _toggleW, _lineHeight);
				bool enabled = EditorGUI.Toggle(togglePos, _source.Enabled);
				if (enabled != _source.Enabled)
				{
					_anyChange = true;
					_source.Enabled = enabled;
				}

				x = togglePos.xMax;
				withRemained -= togglePos.width;
			}

			float w = withRemained - (_serviceTypeW + _space * 4 + _actionButtonWidth * 3);
			Rect objectPos = new(x + _space * 2, position.y, w, _lineHeight);
			GUI.enabled = !_containingProvider.IsSingleSourceProvider;
			Object obj = EditorGUI.ObjectField(
				objectPos,
				_source.ServiceSourceObject,
				typeof(Object),
				allowSceneObjects: true);
			GUI.enabled = true;

			Rect sourceTypePos = new(objectPos.xMax + _space, position.y, _serviceTypeW, _lineHeight);

			ServiceSourceTypes sourceType = _source.PreferredSourceType;
			if (_source.ServiceSourceObject == null)
				GUI.Label(sourceTypePos, _noObjectContent);
			else if (_source.IsSourceAndNotSet)
			{
				sourceType = _source.SourceType;
				if (_source.AlternativeSourceTypes.Any())
				{
					List<ServiceSourceTypes> options = new()
					{
						sourceType
					};
					options.AddRange(_source.AlternativeSourceTypes);
					options.Sort();
					int currentIndex = options.IndexOf(sourceType);

					GUIContent[] guiContentOptions = new GUIContent[options.Count];
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
					GUIContent content = new(IconHelper.GetShortNameForServiceSource(_source.SourceType),
						image: null,
						IconHelper.GetTooltipForServiceSource(_source.SourceType));
					GUI.Label(sourceTypePos, content);
				}
			}
			else if (_insideSet != null)
			{
				_sourceCache.Clear();
				_insideSet.CollectAllEnabled(_sourceCache);
				GUI.Label(sourceTypePos, $"Source Set ({_sourceCache.Count})");
			}
			else
				GUI.Label(sourceTypePos, _invalidObjectContent);

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
				Rect actionButtonPos = new(sourceTypePos.xMax + _space, position.y, _actionBarWidth, _lineHeight);
				ListAction action = DrawActionBar(actionButtonPos, _containingProvider.SourceCount, _sourceIndex);
				if (action != ListAction.Non)
				{
					if (action == ListAction.MoveUp)
						_containingProvider.SwapSources(_sourceIndex, _sourceIndex - 1);

					if (action == ListAction.MoveDown)
						_containingProvider.SwapSources(_sourceIndex, _sourceIndex + 1);

					if (action == ListAction.Delete)
						_containingProvider.RemoveSourceAt(_sourceIndex);

					_anyChange = true;
				}
			}

			position.y += _lineHeight + _space;
			position.height = _lineHeight;
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


			position.y += _lineHeight + _space;
			if (!_source.isTypesExpanded)
				return position;

			foreach (Type type in _dynamicServiceTypes)
			{
				DrawServiceType(position, type);
				position.y += _lineHeight + _space;
			}

			List<Type> notUsedAdditionalTypes = _possibleAdditionalServiceTypes?.Where(
				t => !_dynamicServiceTypes.Contains(t)).ToList();

			if (_source.additionalTypes != null && _additionalServiceTypes != null)
				for (int index = 0; index < _additionalServiceTypes.Count; index++)
				{
					notUsedAdditionalTypes?.Remove(_additionalServiceTypes[index].Type);
					DrawSerializableType(position, _source, _additionalServiceTypes, index, usedTypes);
					position.y += _lineHeight + _space;
				}



			bool isAnyNotUsedType = !notUsedAdditionalTypes.IsNullOrEmpty();
			Rect buttonPos = position;
			buttonPos.width -= _actionBarWidth + _space;
			if (DrawButton(buttonPos, ListAction.Add, isAnyNotUsedType))
			{
				_source.TryAddServiceType(notUsedAdditionalTypes![index: 0]);
				_anyChange = true;
			}

			position.y += _lineHeight + _space;
			return position;
		}

		static void DrawServiceType(Rect position, Type type)
		{
			position.width -= 3 * _actionButtonWidth;
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
			position.width -= 3 * _actionButtonWidth;
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
				serializableType.Type = popupTypeList[newIndex];
				Services.Environment.MarkChanged();
				_anyChange = true;
			}

			GUI.color = guiColor;

			position.x = position.xMax + _space;
			position.width = _actionBarWidth;
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
				GUIContent content = new(message, success ? IconHelper.SuccessIcon : IconHelper.BlockedIcon);
				GUI.Label(position, content);
				position.y += _lineHeight + _space;
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
			position.width = _actionButtonWidth;
			const float shift = _actionButtonWidth - 1;

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

		static readonly GUIContent _upIcon = EditorGUIUtility.IconContent("scrollup_uielements");
		static readonly GUIContent _downIcon = EditorGUIUtility.IconContent("scrolldown_uielements");
		static readonly GUIContent _deleteIcon = new(EditorHelper.GetIcon(IconType.X));
		static readonly GUIContent _addNewIcon = EditorGUIUtility.IconContent("CreateAddNew");

		static bool DrawButton(Rect position, ListAction action, bool enabled = true)
		{
			GUI.enabled = enabled;
			bool result = GUI.Button(position, GUIContent.none);

			GUIContent icon =
				action == ListAction.Add ? _addNewIcon :
				action == ListAction.Delete ? _deleteIcon :
				action == ListAction.MoveDown ? _downIcon :
				action == ListAction.MoveUp ? _upIcon :
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
	}
}
#endif

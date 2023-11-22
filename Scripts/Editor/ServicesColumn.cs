#if UNITY_EDITOR

using System.Linq;
using MUtility;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using EasyInspector;

namespace LooseLink
{
	class ServicesColumn : Column<FoldableRow<ServiceLocatorRow>>
	{

		readonly ServiceLocatorWindow _serviceLocatorWindow;

		bool IsColumnOpen
		{
			get => _serviceLocatorWindow.isServicesOpen;
			set => _serviceLocatorWindow.isServicesOpen = value;
		}

		string SearchServicesText
		{
			get => _serviceLocatorWindow.searchServicesText;
			set => _serviceLocatorWindow.searchServicesText = value;
		}

		string[] _serviceSearchWords = null;
		public bool NoSearch => string.IsNullOrEmpty(SearchServicesText);

		public ServicesColumn(ServiceLocatorWindow serviceLocatorWindow)
		{
			columnInfo = new ColumnInfo
			{
				relativeWidthWeightGetter = GetColumnRelativeWidth,
				fixWidthGetter = GetColumnFixWidth,
				customHeaderDrawer = DrawHeader
			};
			_serviceLocatorWindow = serviceLocatorWindow;
		}


		float GetColumnFixWidth() => IsColumnOpen ? 75f : 50f;
		float GetColumnRelativeWidth() => IsColumnOpen ? 0.5f : 0f;


		public override void DrawCell(Rect position, FoldableRow<ServiceLocatorRow> row, GUIStyle style, Action onChanged)
		{
			GUI.enabled = row.element.enabled;
			if (row.element.Category != ServiceLocatorRow.RowCategory.Source)
			{
				ServicesEditorHelper.DrawLine(position);
				return;
			}

			List<ServiceTypeInfo> types = row.element.source.GetAllServiceInfos().ToList();

			if (types.IsNullOrEmpty())
			{
				GUI.Label(position, "-");
				return;
			}

			DrawServices(position, types);
		}

		void DrawServices(Rect position, IReadOnlyList<ServiceTypeInfo> typeInfos)
		{
			const float space = 4;
			const float iconWidth = 20;
			const float popupWidth = 25;
			if (typeInfos.Count <= 0)
				return;

			Rect typePosition = position;
			typePosition.y += 1;
			typePosition.height = 16;
			bool overflow = false;
			int overflowIndex = -1;
			for (int i = 0; i < typeInfos.Count; i++)
			{
				Type type = typeInfos[i].type;
				GUIContent content = IconHelper.GetGUIContentToType(typeInfos[i]);
				float w = ServicesEditorHelper.SmallLeftLabelStyle.CalcSize(new GUIContent(content.text)).x + iconWidth;

				overflow = i == typeInfos.Count - 1
					? typePosition.x + w > position.xMax
					: typePosition.x + w > position.xMax - popupWidth;

				if (overflow)
				{
					overflowIndex = i;
					break;
				}

				typePosition.width = w - space;
				DrawType(typePosition, content, type, typeInfos[i].isMissing);
				typePosition.x += w + space;
			}

			if (!overflow)
				return;

			int overflowCount = typeInfos.Count - overflowIndex;
			List<ServiceTypeInfo> overflownTypes = new(overflowCount);
			for (int i = overflowIndex; i < typeInfos.Count; i++)
				overflownTypes.Add(typeInfos[i]);

			bool allOverflown = overflowCount == typeInfos.Count;
			Rect popupRect = allOverflown
				? position
				: new Rect(position.xMax - popupWidth, position.y, popupWidth, position.height);

			DrawTypePopup(popupRect, overflownTypes, !allOverflown);
			position.width -= popupWidth + 1;

		}

		public static void DrawType(Rect position, GUIContent content, Type type, bool error)
		{
			if (error)
			{
				Rect pos = new(position.x + 10, position.y + 1, position.width - 6, position.height - 3);
				EditorHelper.DrawBox(pos, EditorHelper.ErrorBackgroundColor);
			}

			if (GUI.Button(position, content, ServicesEditorHelper.SmallCenterLabelStyle))
				TryPing(type);
		}

		void DrawTypePopup(Rect position, IReadOnlyList<ServiceTypeInfo> typeInfos, bool drawPlus)
		{
			position.y += 1;
			position.height -= 2;
			position.x -= 1;
			string[] contents = new string[typeInfos.Count];
			for (int i = 0; i < typeInfos.Count; i++)
				contents[i] = IconHelper.GetGUIContentToType(typeInfos[i]).tooltip;

			if (typeInfos.Any(info => info.isMissing))
				GUI.color = EditorHelper.ErrorBackgroundColor;

			int index = EditorGUI.Popup(
				position,
				selectedIndex: -1,
				contents,
				new GUIStyle(GUI.skin.button));

			GUI.Label(position, $"{(drawPlus ? "+" : "")}{typeInfos.Count}", ServicesEditorHelper.SmallCenterLabelStyle);

			GUI.color = Color.white;

			if (index >= -0)
				TryPing(typeInfos[index].type);
		}


		public static void TryPing(Type pingable)
		{
			if (pingable == null)
				return;
			Object obj = TypeToFileHelper.GetObject(pingable);
			if (obj != null)
				EditorGUIUtility.PingObject(obj);
		}

		void DrawHeader(Rect pos)
		{
			const float labelWidth = 45;
			Rect labelPos = new(
				pos.x + 2,
				pos.y + 2,
				labelWidth,
				pos.height - 4);

			GUIContent content = IsColumnOpen ? new GUIContent("Services") : new GUIContent("Serv.", "Services");
			IsColumnOpen = EditorGUI.Foldout(labelPos, IsColumnOpen, content);

			if (!IsColumnOpen)
			{
				SearchServicesText = string.Empty;
				if (_serviceSearchWords == null || _serviceSearchWords.Length > 0)
					_serviceSearchWords = new string[0];
				return;
			}
			 

			float searchTextW = Mathf.Min(200f, pos.width - 75f);
			Rect searchTypePos = new(
				pos.xMax - searchTextW - 2,
				pos.y + 3,
				searchTextW,
				pos.height - 5);
			SearchServicesText = EditorGUI.TextField(searchTypePos, SearchServicesText, GUI.skin.FindStyle("ToolbarSeachTextField"));

			_serviceSearchWords = ServicesEditorHelper.GenerateSearchWords(SearchServicesText);
		}

		public bool ApplyTypeSearchOnSource(IServiceSourceProvider provider, ServiceSource source) =>
			ApplyTypeSearchOnTypeArray(source.GetServiceTypesRecursively());

		public bool ApplyTypeSearchOnTypeArray(IEnumerable<Type> typesOnService)
		{
			if (string.IsNullOrEmpty(SearchServicesText))
				return true;
			if (_serviceSearchWords == null)
				return true;
			if (typesOnService == null)
				return false;

			Type[] types = typesOnService.ToArray();
			string[] typeTexts = types.Select(type => type.FullName.ToLower()).ToArray();

			foreach (string searchWord in _serviceSearchWords)
				if (!typeTexts.Any(typeName => typeName.Contains(searchWord)))
					return false;

			return true;
		}

		protected override GUIStyle GetDefaultStyle() => null;

		static GUIStyle _labelStyle;
		public static GUIStyle LabelStyle => _labelStyle = _labelStyle ?? new GUIStyle("Label");
	}
}
#endif
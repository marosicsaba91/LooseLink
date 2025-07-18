﻿#if UNITY_EDITOR

using System.Linq;
using MUtility;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using EasyEditor;

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


		static readonly List<ServiceTypeInfo> _typesCache = new();
		public override void DrawCell(Rect position, FoldableRow<ServiceLocatorRow> row, GUIStyle style, Action onChanged)
		{
			GUI.enabled = row.element.enabled;
			if (row.element.Category != ServiceLocatorRow.RowCategory.Source)
			{
				ServicesEditorHelper.DrawLine(position);
				return;
			}

			_typesCache.Clear();
			row.element.source.ColllectAllServiceTypeInfo(_typesCache);

			if (_typesCache.IsNullOrEmpty())
			{
				GUI.Label(position, "-");
				return;
			}

			DrawServices(position, _typesCache);
		}

		void DrawServices(Rect position, IReadOnlyList<ServiceTypeInfo> typeInfo)
		{
			const float space = 4;
			const float iconWidth = 30;
			const float popupWidth = 25;
			if (typeInfo.Count <= 0)
				return;

			Rect typePosition = position;
			typePosition.y += 1;
			typePosition.height = 16;
			bool overflow = false;
			int overflowIndex = -1;
			for (int i = 0; i < typeInfo.Count; i++)
			{
				Type type = typeInfo[i].type;
				GUIContent content = IconHelper.GetGUIContentToType(typeInfo[i]);
				GUIContent iconContent = new(content.image, content.tooltip);
				GUIContent textContent = new(content.text, content.tooltip);

				float textW = ServicesEditorHelper.SmallCenterLabelStyle.CalcSize(textContent).x + iconWidth;

				overflow = i == typeInfo.Count - 1
					? typePosition.x + textW > position.xMax
					: typePosition.x + textW > position.xMax - popupWidth;

				if (overflow)
				{
					overflowIndex = i;
					break;
				}

				typePosition.width = textW - space;

				DrawType(typePosition, textContent, iconContent, type, typeInfo[i].isMissing);
				typePosition.x += textW + space;
			}

			if (!overflow)
				return;

			int overflowCount = typeInfo.Count - overflowIndex;
			List<ServiceTypeInfo> overflownTypes = new(overflowCount);
			for (int i = overflowIndex; i < typeInfo.Count; i++)
				overflownTypes.Add(typeInfo[i]);

			bool allOverflown = overflowCount == typeInfo.Count;
			Rect popupRect = allOverflown
				? position
				: new Rect(position.xMax - popupWidth, position.y, popupWidth, position.height);

			DrawTypePopup(popupRect, overflownTypes, !allOverflown);
			position.width -= popupWidth + 1;

		}

		public static void DrawType(Rect position, GUIContent textContent, GUIContent iconContent, Type type, bool error)
		{
			if (error)
			{
				Rect pos = new(position.x + 10, position.y + 1, position.width - 6, position.height - 3);
				EditorHelper.DrawBox(pos, EditorHelper.ErrorBackgroundColor);
			}

			Rect labelRect = position;
			Rect iconRect = labelRect.SliceOut(18, Side.Left);
			labelRect.x -= 4;
			labelRect.width += 4;


			GUI.Label(iconRect, iconContent);
			GUI.Label(labelRect, textContent, ServicesEditorHelper.SmallCenterLabelStyle);
			if (GUI.Button(position, GUIContent.none, ServicesEditorHelper.SmallCenterLabelStyle))
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
			SearchServicesText = EditorGUI.TextField(searchTypePos, SearchServicesText, GUI.skin.FindStyle("ToolbarSearchTextField"));

			_serviceSearchWords = ServicesEditorHelper.GenerateSearchWords(SearchServicesText);
		}

		public bool IsSourceIncludedInSearch(ServiceSource source)
		{
			List<Type> types = new();
			source.CollectServiceTypesRecursively(types);

			if (string.IsNullOrEmpty(SearchServicesText))
				return true;
			if (_serviceSearchWords == null)
				return true;
			if (types == null)
				return false;

			string[] typeTexts = types.Select(type => type.FullName.ToLower()).ToArray();

			foreach (string searchWord in _serviceSearchWords)
				if (!typeTexts.Any(typeName => typeName.Contains(searchWord)))
					return false;

			return true;
		}

		protected override GUIStyle GetDefaultStyle() => null;

		static GUIStyle _labelStyle;
		public static GUIStyle LabelStyle => _labelStyle ??= new GUIStyle("Label");
	}
}
#endif
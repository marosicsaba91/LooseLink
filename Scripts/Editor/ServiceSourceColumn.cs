#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using MUtility;
using Object = UnityEngine.Object;
using EasyInspector;

namespace LooseLink
{
	class ServiceSourceColumn : FoldoutColumn<ServiceLocatorRow>
	{
		readonly ServiceLocatorWindow _serviceLocatorWindow;

		string SearchServiceText
		{
			get => _serviceLocatorWindow.searchServiceSourcesText;
			set => _serviceLocatorWindow.searchServiceSourcesText = value;
		}

		public ServiceSourceColumn(ServiceLocatorWindow serviceLocatorWindow)
		{
			columnInfo = new ColumnInfo
			{
				customHeaderDrawer = DrawServiceSourcesHeader,
				fixWidth = 150,
				relativeWidthWeight = 0.75f,
			};
			_serviceLocatorWindow = serviceLocatorWindow;
		}

		public override void DrawContent(Rect position, FoldableRow<ServiceLocatorRow> row, GUIStyle style, Action onChanged)
		{ }

		public override void DrawCell(Rect position, FoldableRow<ServiceLocatorRow> row, GUIStyle style, Action onChanged)
		{
			int indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			position = DrawFoldout(position, row);
			EditorGUI.indentLevel = indent;
			DrawPriority(position, row);
			DrawCell(position, row.element, selectElement: true);
		}

		static void DrawCell(Rect position, ServiceLocatorRow row, bool selectElement)
		{
			GUI.enabled = true;

			if (IsRowHighlighted(row))
				EditorGUI.DrawRect(position, EditorHelper.tableSelectedColor);

			GUI.color = Color.white;

			GUI.enabled = row.enabled;
			position.y += (position.height - 16) / 2f;
			position.height = 16;
			EditorGUI.LabelField(position, row.GetGUIContent());

			bool isRowSelectable = row.SelectionObject != null;
			if (isRowSelectable && position.Contains(Event.current.mousePosition))
				EditorGUI.DrawRect(position, EditorHelper.tableHoverColor);

			if (_rowButtonStyle == null)
				_rowButtonStyle = new GUIStyle(GUI.skin.label);

			GUI.enabled = true;
			if (GUI.Button(position, GUIContent.none, _rowButtonStyle))
				OnRowClick(row, selectElement);

			GUI.enabled = row.enabled;
		}


		static void DrawPriority(Rect position, FoldableRow<ServiceLocatorRow> row)
		{
			const float w = 50;
			position.x = position.xMax - w - 4;
			position.width = w;
			if (row.level != 0)
				return;
			IServiceSourceProvider provider;
			if (row.element.Category == ServiceLocatorRow.RowCategory.Set)
				provider = row.element.provider;
			else
				provider = row.element.source?.serviceSourceProvider;

			if (provider == null)
				return;

			bool hai = provider.PriorityType == InstallerPriority.Type.HighestAtInstallation;
			GUIContent content = new(
				provider.PriorityValue + (hai ? "*" : ""),
				null,
				hai ? $"Priority is automatically highest at installation: {provider.PriorityValue}" :
					$"Priority is a fix value: {provider.PriorityValue}");
			GUI.Label(position, content, PriorityStyle);
		}

		static void OnRowClick(ServiceLocatorRow locatorRow, bool selectElement)
		{
			Object obj = locatorRow.SelectionObject;

			if (selectElement)
			{
				if (Selection.objects.Length == 1 && Selection.objects[0] == obj)
					Selection.objects = new Object[] { };
				else
					Selection.objects = new[] { obj };
			}
			else
				EditorGUIUtility.PingObject(obj);
		}

		static bool IsRowHighlighted(ServiceLocatorRow locatorRow) =>
			locatorRow.SelectionObject != null &&
			Selection.objects.Contains(locatorRow.SelectionObject);

		void DrawServiceSourcesHeader(Rect position)
		{
			float searchTextW = Mathf.Min(200f, position.width - 110f);
			const float indent = 4;
			const float margin = 2;
			position.x += indent;
			position.width -= indent;
			GUI.Label(position, "Service Sources", LabelStyle);

			Rect searchServicePos = new(
				position.xMax - (searchTextW + margin),
				position.y + margin + 1 ,
				searchTextW,
				position.height - (2 * margin));

			GUIStyle style = GUI.skin.FindStyle("ToolbarSearchTextField");
			SearchServiceText = EditorGUI.TextField(searchServicePos, SearchServiceText, style);
		}

		public bool ApplyServiceSourceSearch(ServiceSource source) => ApplyServiceSearchOnType(source.Name);

		bool ApplyServiceSearchOnType(string text)
		{
			if (NoSearch)
				return true;
			return text.ToLower().Contains(SearchServiceText.Trim().ToLower());
		}

		static GUIStyle _labelStyle;
		static GUIStyle LabelStyle => _labelStyle = _labelStyle ?? new GUIStyle(EditorStyles.label)
		{
			alignment = TextAnchor.MiddleLeft
		};

		protected override GUIStyle GetDefaultStyle() => LabelStyle;

		static GUIStyle _rowButtonStyle;

		public bool NoSearch => string.IsNullOrEmpty(SearchServiceText);

		static GUIStyle _priorityStyle;
		static GUIStyle PriorityStyle => _priorityStyle = _priorityStyle ?? new GUIStyle
		{
			alignment = TextAnchor.MiddleRight,
			fontSize = 10,
			padding = new RectOffset(left: 2, right: 2, top: 0, bottom: 0),
			normal = { textColor = GUI.skin.label.normal.textColor },
		};
	}

}

#endif
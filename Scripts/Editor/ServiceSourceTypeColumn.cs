#if UNITY_EDITOR
using System;
using UnityEngine;
using MUtility;
using UnityEditor;

namespace LooseLink
{
	class ServiceSourceTypeColumn : Column<FoldableRow<ServiceLocatorRow>>
	{
		bool IsColumnOpen
		{
			get => _serviceLocatorWindow.isSourceCategoryOpen;
			set => _serviceLocatorWindow.isSourceCategoryOpen = value;
		}

		readonly ServiceLocatorWindow _serviceLocatorWindow;
		public ServiceSourceTypeColumn(ServiceLocatorWindow serviceLocatorWindow)
		{
			columnInfo = new ColumnInfo
			{
				fixWidthGetter = GetColumnFixWidth,
				customHeaderDrawer = DrawHeader
			};
			_serviceLocatorWindow = serviceLocatorWindow;
		}

		GUIStyle _rowButtonStyle;

		float GetColumnFixWidth() => IsColumnOpen ? 120 : 60f;

		void DrawHeader(Rect pos)
		{
			const float labelWidth = 45;
			var labelPos = new Rect(
				pos.x + 2,
				pos.y + 2,
				labelWidth,
				pos.height - 4);


			IsColumnOpen = EditorGUI.Foldout(labelPos, IsColumnOpen, IsColumnOpen ? "Source Type" : "Type");
		}

		public override void DrawCell(Rect position, FoldableRow<ServiceLocatorRow> row, GUIStyle style, Action onChanged)
		{
			GUI.enabled = row.element.enabled;

			if (row.element.Category != ServiceLocatorRow.RowCategory.Source)
			{
				ServicesEditorHelper.DrawLine(position);
				return;
			}

			GUIContent content = row.element.GetGUIContentForCategory(!IsColumnOpen);
			GUI.Label(position, content, CategoryStyle);
		}

		protected override GUIStyle GetDefaultStyle() => null;

		static GUIStyle _categoryStyle;
		static GUIStyle CategoryStyle => _categoryStyle = _categoryStyle ?? new GUIStyle
		{
			alignment = TextAnchor.MiddleLeft,
			fontSize = 10,
			padding = new RectOffset(left: 2, right: 2, top: 0, bottom: 0),
			normal = { textColor = GUI.skin.label.normal.textColor },
		};
	}
}
#endif
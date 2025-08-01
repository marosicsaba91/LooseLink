﻿#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using MUtility;
using Object = UnityEngine.Object;
using EasyEditor;
using System.Collections.Generic;

namespace LooseLink
{
	class ResolveColumn : Column<FoldableRow<ServiceLocatorRow>>
	{
		ServiceLocatorWindow _serviceLocatorWindow;
		public ResolveColumn(ServiceLocatorWindow serviceLocatorWindow)
		{
			columnInfo = new ColumnInfo
			{
				customHeaderDrawer = DrawHeader,
				relativeWidthWeight = 0.5f,
				fixWidth = 75,
			};
			_serviceLocatorWindow = serviceLocatorWindow;
		}

		GUIStyle _rowButtonStyle;

		public override void DrawCell(Rect position, FoldableRow<ServiceLocatorRow> row, GUIStyle style, Action onChanged)
		{
			GUI.enabled = row.element.enabled;

			if (row.element.Category == ServiceLocatorRow.RowCategory.Set)
			{
				ServicesEditorHelper.DrawLine(position, start: 0, end: 0.95f);
				return;
			}

			if (IsRowHighlighted(row))
				EditorGUI.DrawRect(position, EditorHelper.tableSelectedColor);

			ServiceSource source = row.element.source;
			Resolvability.Type resolvability = source.Resolvability.type;
			Object resolvedObject = row.element.resolvedInstance;

			Rect contentPos = new(
				position.x + 1,
				position.y + (position.height - 16) / 2f,
				position.width - 2,
				height: 16);

			// Resolved Element
			bool resolvable = resolvedObject != null &&
							  (resolvability == Resolvability.Type.Resolvable ||
							  resolvability == Resolvability.Type.AlwaysResolved);
			if (resolvable)
			{
				bool isRowSelectable = row.element.resolvedInstance != null;
				if (isRowSelectable && position.Contains(Event.current.mousePosition))
					EditorGUI.DrawRect(position, EditorHelper.tableHoverColor);

				bool unResolvable = resolvability != Resolvability.Type.AlwaysResolved;
				const float unResolveButtonWidth = 23;
				if (unResolvable)
					contentPos.width -= unResolveButtonWidth;
				if (row.element.Category == ServiceLocatorRow.RowCategory.Source)
				{
					GUIContent resolvedContent =
						new(resolvedObject.name, ResolvedObjectIcon(resolvedObject.GetType()));
					EditorGUI.LabelField(contentPos, resolvedContent, ServicesEditorHelper.SmallLeftLabelStyle);
				}

				// Ping The Object
				if (resolvedObject == null)
					return;
				_rowButtonStyle ??= new GUIStyle(GUI.skin.label);
				if (GUI.Button(contentPos, GUIContent.none, _rowButtonStyle))
					OnRowClick(row);

				// UnResolve Button
				if (unResolvable)
				{
					Rect unResolveButtonRect = new(
						contentPos.xMax,
						contentPos.y,
						unResolveButtonWidth,
						contentPos.height);
					GUIContent unResolveContent =new(EditorHelper.GetIcon(IconType.X));
					if (GUI.Button(unResolveButtonRect, unResolveContent))
						source.ClearCachedInstancesAndTypes_NoEnvironmentChangeEvent();
				}

			}
			else
			{
				Rect actionButtonRect = new(
					contentPos.x,
					contentPos.y,
					position.width - 2,
					contentPos.height);

				bool isResolved = source != null && source.Resolvability.IsResolvable;
				if (!isResolved)
				{
					// Resolvability Error
					GUIContent resolvabilityError = row.element.resolvability.GetGuiContent();
					GUI.Label(actionButtonRect, resolvabilityError, ServicesEditorHelper.SmallLeftLabelStyle);
				}
				else
				{
					// Resolve Button
					if (GUI.Button(actionButtonRect, "Instantiate", ServicesEditorHelper.SmallCenterButtonStyle))
					{
						ServiceSource service = row.element.source;
						DynamicServiceSource dynamicServiceSource = service.GetDynamicServiceSource();
						if (dynamicServiceSource == null)
							return;

						List<Type> types = new();
						service.CollectServiceTypesRecursively(types);
						foreach (Type type in types)
							dynamicServiceSource.TryGetService(type, provider: null, out object _);
					}
				}

				GUI.enabled = true;
			}
		}

		static Texture ResolvedObjectIcon(Type type)
		{
			if (type.IsSubclassOf(typeof(ScriptableObject)))
				return EditorGUIUtility.IconContent("ScriptableObject Icon").image;
			return EditorGUIUtility.IconContent("GameObject Icon").image;
		}


		protected override GUIStyle GetDefaultStyle() => null;


		static void OnRowClick(FoldableRow<ServiceLocatorRow> row)
		{
			Object obj = row.element.resolvedInstance;
			if (Selection.objects.Length == 1 && Selection.objects[0] == obj)
				Selection.objects = new Object[] { };
			else
				Selection.objects = new[] { obj };
		}

		static bool IsRowHighlighted(FoldableRow<ServiceLocatorRow> row) =>
			row.element.resolvedInstance != null && Selection.objects.Contains(row.element.resolvedInstance);




		void DrawHeader(Rect position)
		{
			const float buttonW = 43;
			const float indent = 4;
			const float margin = 2;
			const float settingButtonW = 20;
			position.x += indent;
			position.width -= indent;
			GUI.Label(position, "Server Object");

			Rect buttonRect = new(
				position.xMax - buttonW - margin - settingButtonW,
				position.y + margin,
				buttonW,
				position.height - 2 * margin);
			if (GUI.Button(buttonRect, "Clear"))
				Services.ClearAllCachedData();
		}
	}
}
#endif
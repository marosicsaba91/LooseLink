#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using MUtility;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
class ServiceSourceCategoryColumn: Column<FoldableRow<ServiceLocatorRow>>
{
    ServiceLocatorWindow _serviceLocatorWindow;
    public ServiceSourceCategoryColumn(ServiceLocatorWindow serviceLocatorWindow)
    {
        columnInfo = new ColumnInfo
        {
            title = "Category", 
            fixWidth = 120,
        };
        _serviceLocatorWindow = serviceLocatorWindow;
    }

    GUIStyle _rowButtonStyle;

    public override void DrawCell(Rect position, FoldableRow<ServiceLocatorRow> row, GUIStyle style, Action onChanged)
    {
        if (row.element.Category != ServiceLocatorRow.RowCategory.Source)
        {
            ServicesEditorHelper.DrawLine(position);
            return;
        }

        GUI.Label(position, row.element.GetGUIContentForCategory(), CategoryStyle);
    }
 
    protected override GUIStyle GetDefaultStyle() => null;
 
    static GUIStyle _categoryStyle;
    static GUIStyle CategoryStyle => _categoryStyle = _categoryStyle ?? new GUIStyle
    {
        alignment = TextAnchor.MiddleLeft,
        fontSize = 10,
        padding = new RectOffset(left: 2, right: 2,top: 0,bottom: 0),
        normal = {textColor = GUI.skin.label.normal.textColor},
    }; 
}
}
#endif
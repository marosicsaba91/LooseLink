#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using MUtility;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
class ServiceLoadedColumn: Column<FoldableRow<ServiceLocatorRow>>
{
    ServiceLocatorWindow _serviceLocatorWindow;
    public ServiceLoadedColumn(ServiceLocatorWindow serviceLocatorWindow)
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
        if (row.element.Category == ServiceLocatorRow.RowCategory.Set) 
        {
            ServicesEditorHelper.DrawLine(position, start: 0,end: 0.95f);
            return;
        }

        if (IsRowHighlighted(row))
            EditorGUI.DrawRect(position, EditorHelper.tableSelectedColor);

        bool isRowSelectable = row.element.loadedInstance != null;
        if (isRowSelectable && position.Contains(Event.current.mousePosition))
            EditorGUI.DrawRect(position, EditorHelper.tableHoverColor);

        Object loadedObject = row.element.loadedInstance;

        var contentPos = new Rect(
            position.x + 1,
            position.y + (position.height - 16) / 2f,
            position.width - 2,
            height: 16);

        DynamicServiceSource source = row.element.source.GetDynamicServiceSource();

        // Loaded Element
        if (loadedObject != null)
        { 
            const float unloadButtonWidth = 23;
            contentPos.width -= unloadButtonWidth;
            if (row.element.Category == ServiceLocatorRow.RowCategory.Source)
            {
                var loadedContent = new GUIContent(loadedObject.name, LoadedObjectIcon(loadedObject.GetType()));
                GUI.Label(contentPos, loadedContent, ServicesEditorHelper.SmallCenterLabelStyle);
            }
            else
            {
                contentPos.height = 8;
                contentPos.y += 4;
                var loadedContent = new GUIContent(
                    text: null,
                    EditorGUIUtility.IconContent("d_FilterSelectedOnly").image,
                    "Cached");
            GUI.Label(contentPos, loadedContent, ServicesEditorHelper.SmallCenterLabelStyle);
            }

 
            // Ping The Object
            if (loadedObject == null) return;
            _rowButtonStyle = _rowButtonStyle ?? new GUIStyle(GUI.skin.label);
            if (GUI.Button(contentPos, GUIContent.none, _rowButtonStyle))
                OnRowClick(row);

            // Load / Unload Button
            if (row.element.Category == ServiceLocatorRow.RowCategory.Source)
            {
                var unloadButtonRect = new Rect(
                    contentPos.xMax,
                    contentPos.y,
                    unloadButtonWidth,
                    contentPos.height);
                GUIContent unloadContent =
                    EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_winbtn_win_close" : "winbtn_win_close");
                if (GUI.Button(unloadButtonRect, unloadContent))
                    source.ClearInstancesAndCachedTypes();
            }

            
        }
        else if (row.element.Category == ServiceLocatorRow.RowCategory.Source)
        { 
            var actionButtonRect = new Rect(
                contentPos.x ,
                contentPos.y,
                position.width-2,
                contentPos.height);

            bool isLoadable = source != null && source.Loadability.IsLoadable;
            if (!isLoadable)
            {
                // Loadability Error
                GUIContent loadabilityError = row.element.loadability.GetGuiContent();
                GUI.Label(actionButtonRect, loadabilityError, ServicesEditorHelper.SmallCenterLabelStyle);
            }
            else
            {
                // Load Button 
                
                if (GUI.Button(actionButtonRect, "Load", ServicesEditorHelper.SmallCenterButtonStyle))
                    row.element.source?.LoadAllType();
            }

            GUI.enabled = true;
        }
    }


    static Texture LoadedObjectIcon(Type type)
    {
        if (type.IsSubclassOf(typeof(ScriptableObject)))
            return EditorGUIUtility.IconContent("ScriptableObject Icon").image;
        return EditorGUIUtility.IconContent("GameObject Icon").image;
    }


    protected override GUIStyle GetDefaultStyle() => null;


    static void OnRowClick(FoldableRow<ServiceLocatorRow> row)
    { 
        Object obj = row.element.loadedInstance;
        if (Selection.objects.Length == 1 && Selection.objects[0] == obj)
            Selection.objects = new Object[]{};
        else
            Selection.objects = new[] {obj};
    }

    static bool IsRowHighlighted(FoldableRow<ServiceLocatorRow> row) => 
        row.element.loadedInstance!=null && Selection.objects.Contains(row.element.loadedInstance);
    
  

    
    void DrawHeader(Rect position)
    {
        const float buttonW = 43;
        const float indent = 4;
        const float margin = 2;
        position.x += indent;
        position.width -= indent; 
        GUI.Label(position, "Server");

        var buttonRect = new Rect(
            position.xMax - buttonW - margin,
            position.y + margin,
            buttonW,
            position.height - 2 * margin);
        if (GUI.Button(buttonRect, "Clear"))
            ServiceLocator.ClearAllCachedData();
    }
}
}
#endif
﻿#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyEditor;
using MUtility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink
{
	class ServiceLocatorWindow : EditorWindow
	{
		const string editorPrefsKey = "ServiceLocatorWindowState";
		static readonly Vector2 minimumSize = new (300, 100);

		ServiceSourceColumn _serviceSourcesColumn;
		ServiceSourceTypeColumn _typeColumn;
		ServicesColumn _servicesColumn;
		ResolveColumn _resolveColumn;
		GUITable<FoldableRow<ServiceLocatorRow>> _serviceTable;
		GUIContent _settingIcon;
		bool _showSetup = false;

		[SerializeField] List<string> openedElements = new();
		public bool isSourceCategoryOpen = false;
		public bool isServicesOpen = false;
		public string searchServicesText = string.Empty;
		public string searchServiceSourcesText = string.Empty;

		[MenuItem("Tools/Loose Link Services")]
		static void ShowWindow()
		{
			ServiceLocatorWindow window = GetWindow<ServiceLocatorWindow>();
			window.minSize = minimumSize;
			window.titleContent = new GUIContent("Loose Link Services");
			window.Show();
		}

		public void OnEnable()
		{
			minSize = minimumSize;
			wantsMouseMove = true;
			_settingIcon = EditorGUIUtility.IconContent("Settings");

			string data = EditorPrefs.GetString(
				editorPrefsKey, JsonUtility.ToJson(this, prettyPrint: false));
			JsonUtility.FromJsonOverwrite(data, this);
			Selection.selectionChanged += Repaint;
		}

		public void OnDisable()
		{
			string data = JsonUtility.ToJson(this, prettyPrint: false);
			EditorPrefs.SetString(editorPrefsKey, data);
			Selection.selectionChanged -= Repaint;
		}

		void GenerateServiceSourceTable()
		{
			if (_serviceTable != null)
				return;

			_serviceSourcesColumn = new ServiceSourceColumn(this);
			_servicesColumn = new ServicesColumn(this);
			_typeColumn = new ServiceSourceTypeColumn(this);
			_resolveColumn = new ResolveColumn(this);

			List<IColumn<FoldableRow<ServiceLocatorRow>>> componentTypeColumns = new()
			{ _serviceSourcesColumn, _typeColumn, _servicesColumn, _resolveColumn };

			_serviceTable = new GUITable<FoldableRow<ServiceLocatorRow>>(componentTypeColumns, this)
			{
				emptyCollectionTextGetter = () => "No Service Source"
			};
		}

		void OnGUI()
		{
			if (_showSetup)
				DrawSetup();
			else
				DrawFullServicesMenu();
		}

		void DrawSetup()
		{
			float h = EditorGUIUtility.singleLineHeight;
			float s = EditorGUIUtility.standardVerticalSpacing;
			const float buttonW = 80;

			Rect settingsRect = new(
				new Vector2(s, s),
				new Vector2(position.width - (s * 3) - buttonW, h));
			ServiceLocationSetupData setup = ServiceLocationSetupData.Instance;

			if (setup.IsDefault)
			{
				if (GUI.Button(settingsRect, "Create Setting File"))
					CreateSettingAsset();
			}
			else
			{
				GUI.enabled = false;
				EditorGUI.ObjectField(
					settingsRect,
					"Setting File",
					setup.IsDefault ? null : setup,
					typeof(ServiceLocationSetupData),
					allowSceneObjects: false);
			}

			settingsRect.width = position.width - (s * 2);
			settingsRect.y += h + (3 * s);

			GUI.enabled = true;
			Rect servicesButtonRect = new (
				new Vector2(position.width - buttonW - s, s),
				new Vector2(buttonW, h));
			if (GUI.Button(servicesButtonRect, "Exit Settings"))
				_showSetup = false;
			if (setup != null)
			{
				EditorGUIUtility.labelWidth = 300;
				EditorGUILayout.Space(30);
				GUI.enabled = !setup.IsDefault;
				DoDrawDefaultInspector(setup);
			}
		}

		void CreateSettingAsset()
		{
			string path = EditorUtility.SaveFilePanelInProject(
				"Save Service Location Setup",
				"ServiceLocationSetup.asset",
				"asset",
				"Please enter a file name to save the setup file.");

			if (path != null)
			{
				ServiceLocationSetupData data = CreateInstance<ServiceLocationSetupData>();
				AssetDatabase.CreateAsset(data, path);
				EditorUtility.FocusProjectWindow();
				AssetDatabase.SaveAssets();
				Selection.activeObject = data;
			}
		}


		internal static bool DoDrawDefaultInspector(Object obj)
		{
			UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(obj);
			SerializedObject sObj = editor.serializedObject;
			EditorGUI.BeginChangeCheck();
			sObj.UpdateIfRequiredOrScript();
			SerializedProperty iterator = sObj.GetIterator();
			int index = 0;
			for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
			{
				using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
					if (iterator.propertyPath != "m_Script")
						EditorGUILayout.PropertyField(iterator, true);
				index++;
			}
			sObj.ApplyModifiedProperties();
			return EditorGUI.EndChangeCheck();
		}

		void DrawFullServicesMenu()
		{
			EventType type = Event.current.type;
			if (type == EventType.Layout)
				return;

			GenerateServiceSourceTable();
			List<FoldableRow<ServiceLocatorRow>> rows = GenerateTreeView();
			_serviceTable.Draw(new Rect(x: 0, 0, position.width, position.height), rows);

			const float buttonS = 22;
			Rect setupButtonRect = new(new Vector2(
					position.width - buttonS + 1, 0),
				new Vector2(buttonS, buttonS));


			if (GUI.Button(setupButtonRect, GUIContent.none))
				_showSetup = true;
			setupButtonRect.x += 1;
			GUI.Label(setupButtonRect, _settingIcon);
		}

		List<FoldableRow<ServiceLocatorRow>> GenerateTreeView()
		{
			List<TreeNode<ServiceLocatorRow>> roots = new();
			foreach (IServiceSourceProvider installer in Services.GetAllInstallers())
			{
				TreeNode<ServiceLocatorRow> installerNode = GetInstallerNode(installer, parentEnabled: true);
				if (installerNode != null)
					roots.Add(installerNode);
			}

			return FoldableRow<ServiceLocatorRow>.GetRows(roots, openedElements, row => row.ToString());
		}

		TreeNode<ServiceLocatorRow> GetInstallerNode(IServiceSourceProvider provider, bool parentEnabled)
		{
			ServiceLocatorRow installerRow = new(ServiceLocatorRow.RowCategory.Set)
			{
				enabled = provider.IsEnabled && parentEnabled,
				provider = provider
			};

			List<TreeNode<ServiceLocatorRow>> children = GetChildNodes(provider, provider.IsEnabled);
			if (provider.IsSingleSourceProvider)
				return children.FirstOrDefault();

			return new TreeNode<ServiceLocatorRow>(installerRow, children);

		}

		List<TreeNode<ServiceLocatorRow>> GetChildNodes(IServiceSourceProvider iProvider, bool enabled)
		{
			List<TreeNode<ServiceLocatorRow>> nodes = new();
			ServiceSource[] sources = iProvider.GetSources().ToArray();

			bool noServiceSearch = _serviceSourcesColumn.NoSearch;
			bool noTypeSearch = _servicesColumn.NoSearch;
			bool anySearch = !(noServiceSearch && noTypeSearch);

			foreach (ServiceSource source in sources)
			{
				if (source.IsSourceSet)
				{
					ServiceSourceSet set = source.GetServiceSourceSet();
					if (set == null)
						continue;
					TreeNode<ServiceLocatorRow> installerNode = GetInstallerNode(set, enabled);
					nodes.Add(installerNode);

				}
				else
				{
					TreeNode<ServiceLocatorRow> sourceNode;
					if (source.IsSourceAndNotSet)
					{

						source.ClearCachedTypes_NoEnvironmentChangeEvent();
						ServiceLocatorRow sourceRow = new(ServiceLocatorRow.RowCategory.Source)
						{
							enabled = enabled && source.Enabled,
							provider = iProvider,
							source = source,
							resolvedInstance = source.LoadedObject,
							resolvability = new Resolvability(Resolvability.Type.Resolvable)
						};

						List<TreeNode<ServiceLocatorRow>> abstractTypes = new();
						sourceNode = new TreeNode<ServiceLocatorRow>(sourceRow, abstractTypes);
						sourceRow.resolvability = source.Resolvability;
					}
					else
					{
						ServiceLocatorRow sourceRow = new(ServiceLocatorRow.RowCategory.Source)
						{
							enabled = enabled && source.Enabled,
							provider = null,
							source = source,
							resolvedInstance = null,
							resolvability = source.ServiceSourceObject == null
								? Resolvability.NoSourceObject
								: Resolvability.WrongSourcesObjectType
						};

						sourceNode = new TreeNode<ServiceLocatorRow>(sourceRow, children: null);
					}

					bool serviceMatchSearch = _serviceSourcesColumn.ApplyServiceSourceSearch(source);
					bool typeMatchSearch = _servicesColumn.IsSourceIncludedInSearch(source);

					if (!anySearch)
						nodes.Add(sourceNode);
					else if (serviceMatchSearch && typeMatchSearch)
						nodes.Add(sourceNode);
				}
			}
			return nodes;
		}

		public static int GetLastControlId()
		{
			FieldInfo getLastControlId = typeof(EditorGUIUtility).GetField("s_LastControlID", BindingFlags.Static | BindingFlags.NonPublic);
			if (getLastControlId != null)
				return (int)getLastControlId.GetValue(null);
			return 0;
		}

		public string[] GenerateSearchWords(string searchText)
		{
			string[] rawKeywords = searchText.Split(',');
			return rawKeywords.Select(keyword => keyword.Trim().ToLower()).ToArray();
		}
	}
}

#endif
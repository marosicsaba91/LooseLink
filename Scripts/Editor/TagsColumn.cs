#if UNITY_EDITOR
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
	class TagsColumn : Column<FoldableRow<ServiceLocatorRow>>
	{
		const int minTagWidth = 25;
		const int spacing = 2;

		readonly ServiceLocatorWindow _serviceLocatorWindow;

		bool IsColumnOpen
		{
			get => _serviceLocatorWindow.isTagsOpen;
			set => _serviceLocatorWindow.isTagsOpen = value;
		}

		string SearchTagText
		{
			get => _serviceLocatorWindow.searchTagsText;
			set => _serviceLocatorWindow.searchTagsText = value;
		}

		string[] _tagSearchWords = null;

		public TagsColumn(ServiceLocatorWindow serviceLocatorWindow)
		{
			columnInfo = new ColumnInfo
			{
				fixWidthGetter = GetColumnFixWidth,
				relativeWidthWeightGetter = GetColumnRelativeWidth,
				customHeaderDrawer = DrawHeader,
				visibleGetter = IsVisible
			};
			_serviceLocatorWindow = serviceLocatorWindow;
		}

		new static bool IsVisible() => ServiceLocationSetupData.Instance.enableTags;

		float GetColumnFixWidth() => IsColumnOpen ? 75f : 50f;
		float GetColumnRelativeWidth() => IsColumnOpen ? 0.5f : 0f;

		static bool _rowEnabled = true;

		public override void DrawCell(Rect position, FoldableRow<ServiceLocatorRow> row, GUIStyle style, Action onChanged)
		{
			_rowEnabled = row.element.enabled;
			GUI.enabled = _rowEnabled;
			if (row.element.Category == ServiceLocatorRow.RowCategory.Set)
			{
				ServicesEditorHelper.DrawLine(position);
				return;
			}

			List<Tag> tags = new();
			tags.AddRange(row.element.source.DynamicTags.Select(tag => new Tag(tag)));
			tags.AddRange(row.element.source.SerializedTags);

			if (tags.Count == 0)
			{
				GUI.Label(position, "-", ServicesEditorHelper.SmallCenterLabelStyle);
				return;
			}

			Rect tagsPos = new(
				position.x + spacing,
				position.y + 1,
				position.width - (2 * spacing),
				position.height - spacing);


			DrawTags(tagsPos, tags);
		}

		void DrawTags(Rect position, IReadOnlyList<Tag> tags)
		{
			int maxTagsToDraw = IsColumnOpen ? (int)((position.width + spacing) / (minTagWidth + spacing)) : 1;

			int tagCount = tags.Count;
			int tagPlacesToDraw =
				Mathf.Min(maxTagsToDraw, tagCount);
			int tagWidth = (int)((position.width + spacing) / tagPlacesToDraw) - 2;
			int tagsToDrawInLine = tagCount > maxTagsToDraw ? maxTagsToDraw - 1 : tagPlacesToDraw;
			bool hasPopup = tags.Count > tagsToDrawInLine;
			int maxXForTags = hasPopup
				? (int)(position.xMax - minTagWidth - spacing)
				: (int)position.xMax;

			List<Tag> tagsToDrawInPopup = new();
			int startPos = (int)position.x;

			int i = 0;
			foreach (Tag tag in tags)
			{
				if (i < tagsToDrawInLine)
				{
					bool isLastTag = i >= tagsToDrawInLine - 1;
					int w = isLastTag ? maxXForTags - startPos : tagWidth;
					DrawTag(new Rect(startPos, position.y, w, position.height), tag, small: true, center: true);
					startPos += tagWidth + spacing;
				}
				else
					tagsToDrawInPopup.Add(tag);

				i++;
			}

			if (tagsToDrawInPopup.Count > 0)
			{
				int w = tagsToDrawInLine <= 0 ? tagWidth : minTagWidth;
				DrawTagPopup(
					new Rect(position.xMax - w, position.y, w, position.height),
					tagsToDrawInPopup,
					tagsToDrawInLine > 1);
			}
		}

		public static void DrawTag(Rect position, Tag tag, bool small, bool center)
		{

			GUIContent content = new()
			{
				text = tag.ShortText(position.width),
				tooltip = tag.GetObjectType() == null ? null : tag.TextWithType(),
			};
			bool pingable = tag.TagObject is Object obj && obj != null;



			if (pingable)
			{
				GUIStyle style = small
						? (center ? ServicesEditorHelper.SmallCenterButtonStyle : ServicesEditorHelper.SmallLeftButtonStyle)
						: (center ? ServicesEditorHelper.CenterButtonStyle : ServicesEditorHelper.LeftButtonStyle);

				if (GUI.Button(position, content, style))
					TryPing(tag);
			}
			else
			{
				GUIStyle style = small
					? (center ? ServicesEditorHelper.SmallCenterLabelStyle : ServicesEditorHelper.SmallLeftLabelStyle)
					: (center ? ServicesEditorHelper.CenterLabelStyle : ServicesEditorHelper.LeftLabelStyle);
				EditorHelper.DrawBox(position);
				position.x += 5;
				position.width -= 10;
				GUI.Label(position, content, style);
			}

			GUI.enabled = _rowEnabled;

		}

		void DrawTagPopup(Rect position, IReadOnlyList<Tag> tagsToDrawInPopup, bool plus)
		{
			int index = EditorGUI.Popup(
				position,
				selectedIndex: -1,
				tagsToDrawInPopup.Select(tag => tag.TextWithType()).ToArray(),
				new GUIStyle(GUI.skin.button));
			GUI.Label(position, $"{(plus ? "+" : "")}{tagsToDrawInPopup.Count}", ServicesEditorHelper.SmallCenterLabelStyle);

			if (index >= -0)
				TryPing(tagsToDrawInPopup[index]);
		}

		void DrawHeader(Rect pos)
		{
			const float fullButtonWidth = 45;
			float buttonWidth = IsColumnOpen ? fullButtonWidth : pos.width - 4;
			Rect buttonPos = new(
				pos.x + 4,
				pos.y,
				buttonWidth,
				pos.height);
			IsColumnOpen = EditorGUI.Foldout(buttonPos, IsColumnOpen, "Tags");

			if (!IsColumnOpen)
			{
				if (_tagSearchWords == null || _tagSearchWords.Length > 0)
					_tagSearchWords = new string[0];
				SearchTagText = string.Empty;
				return;
			}


			float searchTextW = Mathf.Min(200f, pos.width - 55f);
			Rect searchTagPos = new(
				pos.xMax - searchTextW - 2,
				pos.y + 3,
				searchTextW,
				pos.height - 5);

			SearchTagText = EditorGUI.TextField(searchTagPos, SearchTagText, GUI.skin.FindStyle("ToolbarSearchTextField"));

			_tagSearchWords = ServicesEditorHelper.GenerateSearchWords(SearchTagText);
		}

		public bool ApplyTagSearchOnSource(IServiceSourceProvider provider, ServiceSource source) =>
			ApplyTagSearchOnTagArray(source.SerializedTags);

		public static void TryPing(object pingable)
		{
			if (pingable is Object pingableObj)
				EditorGUIUtility.PingObject(pingableObj);
		}

		public bool ApplyTagSearchOnTagArray(IReadOnlyList<Tag> tagsOnService)
		{
			if (string.IsNullOrEmpty(SearchTagText))
				return true;
			if (_tagSearchWords == null)
				return true;
			if (tagsOnService == null)
				return false;

			string[] tagTexts = tagsOnService.Select(tag => tag.TextWithType().ToLower()).ToArray();

			foreach (string searchWord in _tagSearchWords)
				if (!tagTexts.Any(tag => tag.Contains(searchWord)))
					return false;

			return true;
		}

		public bool NoSearch => string.IsNullOrEmpty(SearchTagText);

		protected override GUIStyle GetDefaultStyle() => null;


		static GUIStyle _coloredLabelStyle;

		public static GUIStyle ColoredLabelStyle => _coloredLabelStyle = _coloredLabelStyle ?? new GUIStyle
		{
			alignment = TextAnchor.MiddleCenter,
			padding = new RectOffset(left: 0, right: 0, top: 0, bottom: 0),
			normal = { textColor = GUI.skin.label.normal.textColor },
			fontSize = 10
		};
	}
}
#endif
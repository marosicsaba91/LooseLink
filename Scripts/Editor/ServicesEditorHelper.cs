#if UNITY_EDITOR

using System.Linq;
using EasyEditor;
using MUtility;
using UnityEditor;
using UnityEngine;

namespace LooseLink
{
	static class ServicesEditorHelper
	{
		const int smallMabelSize = 10;

		static GUIStyle _smallCenterLabelStyle;
		public static GUIStyle SmallCenterLabelStyle => _smallCenterLabelStyle = _smallCenterLabelStyle ?? new GUIStyle
		{
			alignment = TextAnchor.MiddleCenter,
			padding = new RectOffset(left: 0, right: 0, top: 0, bottom: 0),
			normal = { textColor = GUI.skin.label.normal.textColor },
			fontSize = smallMabelSize,
			clipping = TextClipping.Clip
		};

		public static GUIStyle GetSmallCenterLabelStyle(GUIContent text, float width)
		{
			Texture image = text.image;
			text.image = null;
			bool smaller = SmallLeftLabelStyle.CalcSize(text).x + (image == null ? 0 : 16) > width;
			text.image = image;
			if (smaller)
				return SmallLeftLabelStyle;
			return SmallCenterLabelStyle;
		}

		static GUIStyle _smallLeftLabelStyle;
		public static GUIStyle SmallLeftLabelStyle => _smallLeftLabelStyle = _smallLeftLabelStyle ?? new GUIStyle
		{
			alignment = TextAnchor.MiddleLeft,
			padding = new RectOffset(left: 0, right: 0, top: 0, bottom: 0),
			normal = { textColor = GUI.skin.label.normal.textColor },
			fontSize = smallMabelSize
		};

		static GUIStyle _centerLabelStyle;
		public static GUIStyle CenterLabelStyle => _centerLabelStyle = _centerLabelStyle ?? new GUIStyle
		{
			alignment = TextAnchor.MiddleCenter,
			padding = new RectOffset(left: 0, right: 0, top: 0, bottom: 0),
			normal = { textColor = GUI.skin.label.normal.textColor }
		};
		static GUIStyle _leftLabelStyle;
		public static GUIStyle LeftLabelStyle => _leftLabelStyle = _leftLabelStyle ?? new GUIStyle
		{
			alignment = TextAnchor.MiddleLeft,
			padding = new RectOffset(left: 0, right: 0, top: 0, bottom: 0),
			normal = { textColor = GUI.skin.label.normal.textColor }
		};

		static GUIStyle _smallCenterButtonStyle;
		public static GUIStyle SmallCenterButtonStyle =>
			_smallCenterButtonStyle = _smallCenterButtonStyle ?? new GUIStyle(GUI.skin.button) { fontSize = smallMabelSize };


		static GUIStyle _smallLeftButtonStyle;
		public static GUIStyle SmallLeftButtonStyle =>
			_smallLeftButtonStyle = _smallLeftButtonStyle ?? new GUIStyle(GUI.skin.button)
			{ fontSize = smallMabelSize, alignment = TextAnchor.MiddleLeft, };


		static GUIStyle _leftButtonStyle;
		public static GUIStyle LeftButtonStyle =>
			_leftButtonStyle = _leftButtonStyle ?? new GUIStyle(GUI.skin.button)
			{ alignment = TextAnchor.MiddleLeft, };

		static GUIStyle _centerButtonStyle;
		public static GUIStyle CenterButtonStyle =>
			_centerButtonStyle = _centerButtonStyle ?? new GUIStyle(GUI.skin.button);

		public static string[] GenerateSearchWords(string searchText)
		{
			string[] rawKeywords = searchText.Split(',');
			return rawKeywords.Select(keyword => keyword.Trim().ToLower()).ToArray();
		}

		public static void DrawLine(Rect position, float start = -0.01f, float end = 1.01f)
		{
			float lineBrightness = EditorGUIUtility.isProSkin ? 0.3f : 0.7f;
			EditorHelper.DrawLine(position,
				new Vector2(start, y: 0.5f),
				new Vector2(end, y: 0.5f),
				new Color(lineBrightness, lineBrightness, lineBrightness, a: 1));
		}
	}
}


#endif
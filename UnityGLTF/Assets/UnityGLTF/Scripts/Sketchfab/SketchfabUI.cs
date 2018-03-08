#if UNITY_EDITOR
#pragma warning disable 0168 // variable declared but not used.
#pragma warning disable 0219 // variable assigned but not used.
#pragma warning disable 0414 // private field assigned but not used.
using UnityEngine;
using UnityEditor;

namespace Sketchfab
{
	public class SketchfabUI
	{
		// Sketchfab UI
		public GUIStyle basic;
		public bool _isInitialized = false;

		// UI Elements
		public static Color SKFB_RED = new Color(0.8f, 0.0f, 0.0f);
		public static Color SKFB_BLUE = new Color(69 / 255.0f, 185 / 255.0f, 223 / 255.0f);
		public static string CLICKABLE_COLOR = "navy";
		public static string ERROR_COLOR = "red";
		public static Texture2D HEADER;
		public static Texture2D DEFAULT_AVATAR;

		Font OSBold;
		Font OSLight;
		Font OSRegular;
		Font OSSemiBold;

		Font TitiliumBlack;
		Font TitiliumBold;
		Font TitiliumLight;
		Font TitiliumRegular;
		Font TitiliumSemibold;
		Font TitiliumThin;

		public GUIStyle sketchfabModelName;
		public GUIStyle sketchfabTitleLabel;
		public GUIStyle sketchfabContentLabel;
		public GUIStyle sketchfabSubContentLabel;
		public GUIStyle keyStyle;
		public GUIStyle valueStyle;
		public GUIStyle sketchfabMiniModelname;
		public GUIStyle sketchfabMiniAuthorname;

		public GUIStyle SkfbClickableLabel;
		public GUIStyle SketchfabButton;
		public GUIStyle SketchfabLabel;

		public SketchfabUI()
		{
			Initialize();
			_isInitialized = true;
		}

		private void Initialize()
		{
			//basic
			basic = new GUIStyle();
			basic.fontStyle = FontStyle.BoldAndItalic;
			// Fonts
			OSLight = Resources.Load<Font>("OpenSans-Light");
			OSBold = Resources.Load<Font>("OpenSans-Bold");
			OSRegular = Resources.Load<Font>("OpenSans-Regular");
			OSSemiBold = Resources.Load<Font>("OpenSans-SemiBold");

			TitiliumBlack = Resources.Load<Font>("TitilliumWeb-Black");
			TitiliumBold = Resources.Load<Font>("TitilliumWeb-Bold");
			TitiliumLight = Resources.Load<Font>("TitilliumWeb-Light");
			TitiliumRegular = Resources.Load<Font>("TitilliumWeb-Regular");
			TitiliumSemibold = Resources.Load<Font>("TitilliumWeb-Semibold");
			TitiliumThin = Resources.Load<Font>("TitilliumWeb-Thin");

			sketchfabModelName = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
			sketchfabModelName.font = TitiliumBold;
			sketchfabModelName.fontSize = 20;

			sketchfabTitleLabel = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
			sketchfabTitleLabel.font = TitiliumRegular;

			// Content label
			sketchfabContentLabel = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
			sketchfabContentLabel.font = OSRegular;
			sketchfabContentLabel.fontSize = 14;
			sketchfabContentLabel.richText = true;

			sketchfabSubContentLabel = new GUIStyle(sketchfabContentLabel);
			sketchfabSubContentLabel.font = OSRegular;
			sketchfabSubContentLabel.fontSize = 12;
			sketchfabSubContentLabel.richText = true;

			keyStyle = new GUIStyle(EditorStyles.label);
			keyStyle.alignment = TextAnchor.MiddleLeft;
			keyStyle.font = OSRegular;
			keyStyle.fontSize = 12;

			valueStyle = new GUIStyle(EditorStyles.label);
			valueStyle.alignment = TextAnchor.MiddleRight;
			valueStyle.font = OSBold;
			valueStyle.fontSize = 12;

			sketchfabMiniModelname = new GUIStyle(EditorStyles.miniLabel);
			sketchfabMiniModelname.font = OSSemiBold;
			sketchfabMiniModelname.fontSize = 12;
			sketchfabMiniModelname.wordWrap = true;
			sketchfabMiniModelname.alignment = TextAnchor.UpperCenter;
			sketchfabMiniModelname.clipping = TextClipping.Clip;
			sketchfabMiniModelname.margin = new RectOffset(0, 0, 0, 0);
			sketchfabMiniModelname.padding = new RectOffset(0, 0, 0, 0);

			sketchfabMiniAuthorname = new GUIStyle(EditorStyles.miniLabel);
			sketchfabMiniAuthorname.font = OSRegular;
			sketchfabMiniAuthorname.fontSize = 10;
			sketchfabMiniAuthorname.wordWrap = true;
			sketchfabMiniAuthorname.alignment = TextAnchor.UpperCenter;
			sketchfabMiniAuthorname.clipping = TextClipping.Clip;
			sketchfabMiniAuthorname.margin = new RectOffset(0, 0, 0, 0);
			sketchfabMiniAuthorname.padding = new RectOffset(0, 0, 0, 0);

			SkfbClickableLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
			SkfbClickableLabel.richText = true;

			SketchfabButton = new GUIStyle(EditorStyles.miniButton);
			SketchfabButton.font = OSSemiBold;
			SketchfabButton.fontSize = 11;
			SketchfabButton.fixedHeight = 24;

			SketchfabLabel = new GUIStyle(EditorStyles.miniLabel);
			SketchfabLabel.richText = true;
		}

		public void displayModelName(string modelName)
		{
			GUILayout.Label(modelName, sketchfabModelName);
		}

		public void displayTitle(string title)
		{
			GUILayout.Label(title, sketchfabTitleLabel);
		}

		public void displayContent(string content)
		{
			GUILayout.Label(content, sketchfabContentLabel);
		}

		public void displaySubContent(string subContent)
		{
			GUILayout.Label(subContent, sketchfabSubContentLabel);
		}

		public void showUpToDate(string latestVersion)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Exporter is up to date (version:" + latestVersion + ")", EditorStyles.centeredGreyMiniLabel);

			GUILayout.FlexibleSpace();
			if (GUILayout.Button(ClickableTextColor("Help"), SkfbClickableLabel, GUILayout.Height(20)))
			{
				Application.OpenURL(SketchfabPlugin.Urls.latestRelease);
			}

			if (GUILayout.Button(ClickableTextColor("Report an issue"), SkfbClickableLabel, GUILayout.Height(20)))
			{
				Application.OpenURL(SketchfabPlugin.Urls.reportAnIssue);
			}
			GUILayout.EndHorizontal();
		}

		public  void displayModelStats(string key, string value)
		{
			GUILayout.BeginHorizontal(GUILayout.Width(200));
			GUILayout.Label(key, keyStyle);
			GUILayout.Label(value, valueStyle);
			GUILayout.EndHorizontal();
		}

		public static Texture2D MakeTex(int width, int height, Color col)
		{
			Color[] pix = new Color[width * height];
			for (int i = 0; i < pix.Length; ++i)
			{
				pix[i] = col;
			}
			Texture2D result = new Texture2D(width, height);
			result.SetPixels(pix);
			result.Apply();
			return result;
		}

		public static string ClickableTextColor(string text)
		{
			return "<color=" + SketchfabUI.CLICKABLE_COLOR + ">" + text + "</color>";
		}

		public static string ErrorTextColor(string text)
		{
			return "<color=" + SketchfabUI.ERROR_COLOR + ">" + text + "</color>";
		}
	}
}
#endif
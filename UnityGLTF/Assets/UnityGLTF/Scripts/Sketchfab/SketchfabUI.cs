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
		public static Color SKFB_BLUE = new Color(28 / 255.0f, 170 / 255.0f, 223 / 255.0f);
		public static Color SKFB_BLUE_2 = new Color(69 / 255.0f, 185 / 255.0f, 223 / 255.0f);

		public static string ERROR_COLOR = "red";
		public static Texture2D HEADER;
		public static Texture2D DEFAULT_AVATAR;

		public static Texture2D plusPlanIcon;
		public static Texture2D proPlanIcon;
		public static Texture2D premPlanIcon;
		public static Texture2D bizPlanIcon;
		public static Texture2D entPlanIcon;

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

		GUIStyle _sketchfabModelName;
		public GUIStyle _sketchfabTitleLabel;
		GUIStyle _sketchfabContentLabel;
		GUIStyle _sketchfabSubContentLabel;
		public GUIStyle _keyStyle;
		public GUIStyle _valueStyle;
		GUIStyle _sketchfabMiniModelname;
		GUIStyle _sketchfabMiniAuthorname;

		public GUIStyle _sketchfabClickableLabel;
		public GUIStyle _sketchfabButton;
		public GUIStyle _sketchfabBigButton;
		public GUIStyle _sketchfabLabel;
		public GUIStyle _sketchfabBigLabel;

		public Texture SKETCHFAB_ICON;

		public SketchfabUI()
		{
			Initialize();
			_isInitialized = true;
		}

		public static Texture2D getPlanIcon(string planLb)
		{
			switch (planLb)
			{
				case "plus":
					if (!plusPlanIcon)
						plusPlanIcon = Resources.Load<Texture2D>("planPlus");
					return plusPlanIcon;
				case "pro":
					if (!proPlanIcon)
						proPlanIcon = Resources.Load<Texture2D>("planPro");
					return proPlanIcon;
				case "prem":
					if (!premPlanIcon)
						premPlanIcon = Resources.Load<Texture2D>("planPrem");
					return premPlanIcon;
				case "biz":
					if (!bizPlanIcon)
						bizPlanIcon = Resources.Load<Texture2D>("planBiz");
					return bizPlanIcon;
				case "ent":
					if (!entPlanIcon)
						entPlanIcon = Resources.Load<Texture2D>("planEnt");
					return entPlanIcon;
				default:
					return null;
			}
		}

		public GUIStyle getSketchfabModelName()
		{
			if(_sketchfabModelName == null)
			{
				_sketchfabModelName = new GUIStyle(EditorStyles.miniLabel);
				_sketchfabModelName.font = TitiliumBold;
				_sketchfabModelName.fontSize = 20;
			}

			return _sketchfabModelName;
		}

		public GUIStyle getSketchfabButton()
		{
			if(_sketchfabButton == null)
			{
				_sketchfabButton = new GUIStyle(GUI.skin.button);
				_sketchfabButton.font = TitiliumRegular;
				_sketchfabButton.fontSize = 10;
				_sketchfabButton.richText = true;
			}

			return _sketchfabButton;
		}

		public GUIStyle getSketchfabBigButton()
		{
			if(_sketchfabBigButton == null)
			{
				_sketchfabBigButton = new GUIStyle(GUI.skin.button);
				_sketchfabBigButton.font = TitiliumRegular;
				_sketchfabBigButton.fontSize = 20;
				_sketchfabBigButton.richText = true;
			}

			return _sketchfabBigButton;
		}

		public GUIStyle getSketchfabMiniModelName()
		{
			if(_sketchfabMiniModelname == null)
			{
				_sketchfabMiniModelname = new GUIStyle(EditorStyles.miniLabel);
				_sketchfabMiniModelname.font = OSSemiBold;
				_sketchfabMiniModelname.fontSize = 10;
				_sketchfabMiniModelname.wordWrap = true;
				_sketchfabMiniModelname.alignment = TextAnchor.UpperCenter;
				_sketchfabMiniModelname.clipping = TextClipping.Clip;
				_sketchfabMiniModelname.margin = new RectOffset(0, 0, 0, 0);
				_sketchfabMiniModelname.padding = new RectOffset(0, 0, 0, 0);
			}

			return _sketchfabMiniModelname;
		}

		public GUIStyle getSketchfabMiniAuthorName()
		{
			if(_sketchfabMiniAuthorname == null)
			{
				_sketchfabMiniAuthorname = new GUIStyle(EditorStyles.miniLabel);
				_sketchfabMiniAuthorname.font = OSRegular;
				_sketchfabMiniAuthorname.fontSize = 8;
				_sketchfabMiniAuthorname.wordWrap = true;
				_sketchfabMiniAuthorname.alignment = TextAnchor.UpperCenter;
				_sketchfabMiniAuthorname.clipping = TextClipping.Clip;
				_sketchfabMiniAuthorname.margin = new RectOffset(0, 0, 0, 0);
				_sketchfabMiniAuthorname.padding = new RectOffset(0, 0, 0, 0);
			}

			return _sketchfabMiniAuthorname;
		}

		public GUIStyle getSketchfabContentLabel()
		{
			if(_sketchfabContentLabel == null)
			{
				_sketchfabContentLabel = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
				_sketchfabContentLabel.font = OSRegular;
				_sketchfabContentLabel.fontSize = 14;
				_sketchfabContentLabel.richText = true;
			}

			return _sketchfabContentLabel;
		}

		public GUIStyle getSketchfabSubContentLabel()
		{
			if(_sketchfabSubContentLabel == null)
			{
				_sketchfabSubContentLabel = new GUIStyle(getSketchfabContentLabel());
				_sketchfabSubContentLabel.font = OSRegular;
				_sketchfabSubContentLabel.fontSize = 12;
				_sketchfabSubContentLabel.richText = true;
			}

			return _sketchfabSubContentLabel;
		}

		public void LoadFonts()
		{
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
		}

		public GUIStyle getKeyStyle()
		{
			if(_keyStyle == null)
			{
				_keyStyle = new GUIStyle(EditorStyles.label);
				_keyStyle.alignment = TextAnchor.MiddleLeft;
				_keyStyle.font = OSRegular;
				_keyStyle.fontSize = 12;
			}

			return _keyStyle;
		}

		public GUIStyle getValueStyle()
		{
			if(_valueStyle == null)
			{
				_valueStyle = new GUIStyle(EditorStyles.label);
				_valueStyle.alignment = TextAnchor.MiddleRight;
				_valueStyle.font = OSBold;
				_valueStyle.fontSize = 12;
			}

			return _valueStyle;
		}

		public GUIStyle getSketchfabTitleLabel()
		{
			if(_sketchfabTitleLabel == null)
			{
				_sketchfabTitleLabel = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
				_sketchfabTitleLabel.font = TitiliumRegular;
			}

			return _sketchfabTitleLabel;
		}

		public GUIStyle getSketchfabClickableLabel()
		{
			if(_sketchfabClickableLabel == null)
			{
				_sketchfabClickableLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
				_sketchfabClickableLabel.richText = true;
			}

			return _sketchfabClickableLabel;
		}

		public GUIStyle getSketchfabLabel()
		{
			if(_sketchfabLabel == null)
			{
				_sketchfabLabel = new GUIStyle(EditorStyles.miniLabel);
				_sketchfabLabel.richText = true;
			}

			return _sketchfabLabel;
		}

		public GUIStyle getSketchfabBigLabel()
		{
			if (_sketchfabBigLabel == null)
			{
				_sketchfabBigLabel = new GUIStyle(EditorStyles.miniLabel);
				_sketchfabBigLabel.richText = true;
				_sketchfabBigLabel.fontSize = 14;
				_sketchfabBigLabel.alignment = TextAnchor.MiddleCenter;
			}

			return _sketchfabBigLabel;
		}

		private void Initialize()
		{
			SKETCHFAB_ICON = Resources.Load<Texture>("icon");

			//basic
			basic = new GUIStyle();
			basic.fontStyle = FontStyle.BoldAndItalic;

			// Fonts
			LoadFonts();
		}

		public void displayTitle(string title)
		{
			GUILayout.Label(title, getSketchfabTitleLabel());
		}

		public void displayContent(string content)
		{
			GUILayout.Label(content, getSketchfabContentLabel());
		}

		public void displaySubContent(string subContent)
		{
			GUILayout.Label(subContent, getSketchfabSubContentLabel());
		}

		public  void displayModelStats(string key, string value)
		{
			GUILayout.BeginHorizontal(GUILayout.Width(200));
			GUILayout.Label(key, getKeyStyle());
			GUILayout.Label(value, getValueStyle());
			GUILayout.EndHorizontal();
		}

		public static string ErrorTextColor(string text)
        {
			return "<color=" + SketchfabUI.ERROR_COLOR + ">" + text + "</color>";
		}
}
}
#endif

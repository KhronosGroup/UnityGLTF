/*
 * Copyright(c) 2017-2018 Sketchfab Inc.
 * License: https://github.com/sketchfab/UnityGLTF/blob/master/LICENSE
 */
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

// Static data and assets related to the plugin
namespace Sketchfab
{
	public class SketchfabPlugin : MonoBehaviour
	{
		public static string VERSION = "1.2.1";

		public struct Urls
		{
			public static string baseApi = "https://api.sketchfab.com";

			public static string server = "https://sketchfab.com";
			public static string latestRelease = "https://github.com/sketchfab/unity-plugin/releases/latest";
			public static string resetPassword = "https://sketchfab.com/login/reset-password";
			public static string createAccount = "https://sketchfab.com/signup";
			public static string reportAnIssue = "https://help.sketchfab.com/hc/en-us/requests/new?type=exporters&subject=Unity+Exporter";
			public static string privateInfo = "https://help.sketchfab.com/hc/en-us/articles/115000422206-Private-Models";
			public static string draftInfo = "https://help.sketchfab.com/hc/en-us/articles/115000472906-Draft-Mode";
			public static string latestReleaseCheck = "https://api.github.com/repos/sketchfab/unity-plugin/releases";
			public static string plans = "https://sketchfab.com/plans?utm_source=unity-plugin&utm_medium=plugin&utm_campaign=download-api-pro-cta";
			public static string bannerUrl = "https://static.sketchfab.com/plugins/unity/banner.jpg";
			public static string storeUrl = "https://sketchfab.com/store?utm_source=unity-plugin&utm_medium=plugin&utm_campaign=store-banner";
			public static string storeUrlButton = "https://sketchfab.com/store?utm_source=unity-plugin&utm_medium=plugin&utm_campaign=store-button";
			public static string categories = server + "/v3/categories";
			private static string dummyClientId = "IUO8d5VVOIUCzWQArQ3VuXfbwx5QekZfLeDlpOmW";
			public static string oauth = server + "/oauth2/token/?grant_type=password&client_id=" + dummyClientId;
			public static string userMe = server + "/v3/me";
			public static string userAccount = server + "/v3/me/account";
			public static string postModel = server + "/v3/models";
			public static string modelUrl = server + "/models";

			// AssetBrowser
			public static string searchEndpoint = baseApi + "/v3/search?";
			public static string ownModelsSearchEndpoint = baseApi + "/v3/me/search?";
			public static string storePurchasesModelsSearchEndpoint = baseApi + "/v3/me/models/purchases?";
			public static string categoryEndpoint = baseApi + "/v3/categories";
			public static string modelEndPoint = baseApi + "/v3/models";
		};

		public string _uploadSource = "Unity-exporter";

		// Fields limits
		public const int NAME_LIMIT = 48;
		public const int DESC_LIMIT = 1024;
		public const int TAGS_LIMIT = 50;
		public const int PASSWORD_LIMIT = 64;
		public const int SPACE_SIZE = 5;

		// UI ELEMENTS
		public static Texture2D DEFAULT_AVATAR;
		public static Texture2D bannerTexture;

		// Plugin elements
		static SketchfabUI _ui;
		static SketchfabLogger _logger;
		static SketchfabAPI _api;
		private RefreshCallback _refreshCallback;
		private static string versionCaption = "";

		// Logger needs API to check login
		// so initialize API before Logger
		public static void Initialize()
		{
			_ui = new SketchfabUI();
			_api = new SketchfabAPI();
			_logger = new SketchfabLogger();
			checkUpdates();
			retrieveBannerImage();
			DEFAULT_AVATAR = Resources.Load("defaultAvatar") as Texture2D;
		}

		public static void retrieveBannerImage()
		{
			SketchfabRequest request = new SketchfabRequest(Urls.bannerUrl);
			request.setCallback(onBannerRetrieved);
			getAPI().registerRequest(request);
		}

		public static void onBannerRetrieved(UnityWebRequest request)
		{
			bool sRGBBackup = GL.sRGBWrite;
			GL.sRGBWrite = true;
			byte[] data = request.downloadHandler.data;

			bannerTexture = new Texture2D(2, 2);
			bannerTexture.LoadImage(data);
		}

		public static void checkValidity()
		{
			if(_ui == null || _logger == null || _api == null || DEFAULT_AVATAR == null)
			{
				Initialize();
			}
		}

		public static void checkUpdates()
		{
			SketchfabRequest request = new SketchfabRequest(Urls.latestReleaseCheck);
			request.setCallback(onVersionCheckSuccess);
			getAPI().registerRequest(request);
		}

		public static void onVersionCheckSuccess(string response)
		{
			JSONNode node = Utils.JSONParse(response);
			if (node != null && node[0]["tag_name"] != null)
			{
				string fetchedVersion = node[0]["tag_name"];
				if(fetchedVersion == VERSION)
				{
					versionCaption = "(up to date)";
				}
				else
				{
					versionCaption = SketchfabUI.ErrorTextColor("(out of date)");
				}
			}
		}

		// Must be called in OnGUI function in order to have EditorStyle classes created
		public static SketchfabUI getUI()
		{
			if (_ui == null)
			{
				_ui = new SketchfabUI();
			}

			return _ui;
		}

		public static SketchfabLogger getLogger()
		{
			if (_logger == null)
				_logger = new SketchfabLogger();

			return _logger;
		}

		public static SketchfabAPI getAPI()
		{
			if (_api == null)
				_api = new SketchfabAPI();

			return _api;
		}

		// GUI functions
		public static void displayHeader()
		{
			GUILayout.BeginHorizontal(GUILayout.Height(75));
			GUILayout.BeginVertical();
			GUILayout.Space(5);
			_logger.showLoginUi();
			GUILayout.Space(5);
			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();

			// If banner available, display it
			if (bannerTexture != null)
			{
				displayBanner();
			}

			GUILayout.FlexibleSpace();
			GUILayout.BeginVertical();
			GUILayout.Space(5);
			GUILayout.FlexibleSpace();
			GUILayout.Label(Resources.Load("SketchfabWhite") as Texture2D, GUILayout.Height(40), GUILayout.Width(180));
			GUILayout.FlexibleSpace();
			GUILayout.Space(5);
			GUILayout.EndVertical();
			GUILayout.Space(5);
			GUILayout.EndHorizontal();
		}

		public static void displayBanner()
		{
			GUILayout.BeginVertical();
			GUILayout.Space(5);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(bannerTexture, _ui.getSketchfabLabel(), GUILayout.Width(345), GUILayout.Height(68)))
			{
				Application.OpenURL(Urls.storeUrl);
			}
			GUILayout.FlexibleSpace();
			GUILayout.Space(5);
			GUILayout.EndVertical();
		}

		public static void displayFooter()
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Sketchfab plugin for Unity " + VERSION + " " + versionCaption, _ui.getSketchfabLabel(), GUILayout.Height(20)))
			{
				Application.OpenURL(Urls.latestRelease);
			}
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Help", _ui.getSketchfabLabel(), GUILayout.Height(20)))
			{
				Application.OpenURL(Urls.latestRelease);
			}
			if (GUILayout.Button("Report an issue", _ui.getSketchfabLabel(), GUILayout.Height(20)))
			{
				Application.OpenURL(Urls.reportAnIssue);
			}
			GUILayout.EndHorizontal();
		}

		public static void Update()
		{
			if(_api != null)
			_api.Update();
		}
	}
}

#endif

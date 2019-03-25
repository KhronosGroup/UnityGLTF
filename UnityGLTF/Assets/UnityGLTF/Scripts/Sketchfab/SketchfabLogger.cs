#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using SimpleJSON;

namespace Sketchfab
{
	public class SketchfabProfile
	{
		public string username;
		public string displayName;
		public string accountLabel;
		public int maxUploadSize;
		public Texture2D avatar = SketchfabPlugin.DEFAULT_AVATAR;
		public bool hasAvatar = false;
		public int _userCanPrivate = -1; // Can protect model = 1  // Cannot = 0
		public Texture2D planIcon;

		public SketchfabProfile(string usrName, string usr, string planLb)
		{
			username = usrName;
			displayName = usr;

			switch (planLb)
			{
				case "pro":
					maxUploadSize = 200 * 1024 * 1024;
					accountLabel = "PRO";
					planIcon = SketchfabUI.getPlanIcon(planLb);
					break;
				case "prem":
					maxUploadSize = 500 * 1024 * 1024;
					accountLabel = "PREMIUM";
					planIcon = SketchfabUI.getPlanIcon(planLb);
					break;
				case "biz":
					maxUploadSize = 500 * 1024 * 1024;
					accountLabel = "BUSINESS";
					planIcon = SketchfabUI.getPlanIcon(planLb);
					break;
				case "ent":
					maxUploadSize = 500 * 1024 * 1024;
					accountLabel = "ENTERPRISE";
					planIcon = SketchfabUI.getPlanIcon(planLb);
					break;
				default:
					maxUploadSize = 50 * 1024 * 1024;
					accountLabel = "BASIC";
					break;
			}
		}

		public void setAvatar(Texture2D img)
		{
			avatar = img;
			hasAvatar = true;
		}

		public bool isDisplayable()
		{
			return displayName != null;
		}
	}

	public class SketchfabLogger
	{
		private string accessTokenKey = "skfb_access_token";
		SketchfabProfile _current;
		RefreshCallback _refresh;
		public Vector2 UI_SIZE = new Vector2(200, 30);
		public Vector2 AVATAR_SIZE = new Vector2(50, 50);

		string username;
		string password = "";
		bool _isUserLogged = false;
		bool _hasCheckedSession = false;

		public enum LOGIN_STEP
		{
			GET_TOKEN,
			CHECK_TOKEN,
			USER_INFO
		}

		public SketchfabLogger(RefreshCallback callback = null)
		{
			_refresh = callback;
			checkAccessTokenValidity();
			if (username == null)
			{
				username = EditorPrefs.GetString("skfb_username", "");
			}
		}

		public bool isUserLogged()
		{
			return _isUserLogged;
		}

		public bool canAccessOwnModels()
		{
			return !isUserBasic();
		}

		public SketchfabProfile getCurrentSession()
		{
			return _current;
		}

		public void showLoginUi()
		{
			GUILayout.BeginVertical(GUILayout.MinWidth(UI_SIZE.x), GUILayout.MinHeight(UI_SIZE.y));
			if (_current == null)
			{
				if (_hasCheckedSession)
				{
					GUILayout.Label("You're not logged", EditorStyles.centeredGreyMiniLabel);
					GUILayout.BeginHorizontal();
					GUILayout.BeginVertical();
					username = GUILayout.TextField(username);
					password = GUILayout.PasswordField(password, '*');

					GUI.enabled = username != null && password != null && username.Length > 0 && password.Length > 0;
					if (GUILayout.Button("Login"))
					{
						requestAccessToken(username, password);
					}
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
					GUI.enabled = true;
				}
				else
				{
					GUILayout.Label("Retrieving user data", EditorStyles.centeredGreyMiniLabel);
				}

			}
			else if (_current.isDisplayable())
			{
				GUILayout.Label("Logged in as", EditorStyles.centeredGreyMiniLabel);
				GUILayout.BeginHorizontal();
				GUILayout.Label(_current.avatar);
				GUILayout.BeginVertical();

				GUILayout.BeginHorizontal();
				GUILayout.Label("" + _current.displayName);
				if (_current.planIcon)
					GUILayout.Label(_current.planIcon, GUILayout.Height(18));
				GUILayout.EndHorizontal();

				if (GUILayout.Button("Logout"))
				{
					logout();
					return;
				}
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				if (_current._userCanPrivate == -1)
					requestCanPrivate();
			}
			GUILayout.EndVertical();
		}

		public void logout()
		{
			EditorPrefs.DeleteKey(accessTokenKey);
			_current = null;
			_isUserLogged = false;
			_hasCheckedSession = true;
		}

		public void requestAccessToken(string user_name, string user_password)
		{
			List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
			formData.Add(new MultipartFormDataSection("username", user_name));
			formData.Add(new MultipartFormDataSection("password", user_password));

			SketchfabRequest tokenRequest = new SketchfabRequest(SketchfabPlugin.Urls.oauth, formData);
			tokenRequest.setCallback(handleGetToken);
			tokenRequest.setFailedCallback(onLoginFailed);
			SketchfabPlugin.getAPI().registerRequest(tokenRequest);
		}

		public void requestCanPrivate()
		{
			SketchfabRequest canPrivateRequest = new SketchfabRequest(SketchfabPlugin.Urls.userAccount, getHeader());
			canPrivateRequest.setCallback(onCanPrivate);
			SketchfabPlugin.getAPI().registerRequest(canPrivateRequest);
		}

		private void handleGetToken(string response)
		{
			string access_token = parseAccessToken(response);
			EditorPrefs.SetString("skfb_username", username);
			if (access_token != null)
				registerAccessToken(access_token);

			if (_current == null)
			{
				requestUserData();
			}
			// _refresh();
		}

		private string parseAccessToken(string text)
		{
			JSONNode response = Utils.JSONParse(text);
			if (response["access_token"] != null)
			{
				return response["access_token"];
			}

			return null;
		}

		private void registerAccessToken(string access_token)
		{
			EditorPrefs.SetString(accessTokenKey, access_token);
		}

		public void requestAvatar(string url)
		{
			string access_token = EditorPrefs.GetString(accessTokenKey);
			if (access_token == null || access_token.Length < 30)
			{
				Debug.Log("Access token is invalid or inexistant");
				return;
			}

			Dictionary<string, string> headers = new Dictionary<string, string>();
			headers.Add("Authorization", "Bearer " + access_token);
			SketchfabRequest request = new SketchfabRequest(url, headers);
			request.setCallback(handleAvatar);
			SketchfabPlugin.getAPI().registerRequest(request);
		}

		public Dictionary<string, string> getHeader()
		{
			Dictionary<string, string> headers = new Dictionary<string, string>();
			headers.Add("Authorization", "Bearer " + EditorPrefs.GetString(accessTokenKey));
			return headers;
		}

		private string getAvatarUrl(JSONNode node)
		{
			JSONArray array = node["avatar"]["images"].AsArray;
			foreach (JSONNode elt in array)
			{
				if (elt["width"].AsInt == 100)
				{
					return elt["url"];
				}
			}

			return "";
		}

		// Callback for avatar
		private void handleAvatar(byte[] responseData)
		{
			if (_current == null)
			{
				Debug.Log("Invalid call avatar");
				return;
			}
			bool sRGBBackup = GL.sRGBWrite;
			GL.sRGBWrite = true;

			Texture2D tex = new Texture2D(4, 4);
			tex.LoadImage(responseData);
#if UNITY_5_6 || UNITY_2017
			if (PlayerSettings.colorSpace == ColorSpace.Linear)
			{
				var renderTexture = RenderTexture.GetTemporary(tex.width, tex.height, 24);
				Material linear2SRGB = new Material(Shader.Find("GLTF/Linear2sRGB"));
				linear2SRGB.SetTexture("_InputTex", tex);
				Graphics.Blit(tex, renderTexture, linear2SRGB);
				tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
			}
#endif
			TextureScale.Bilinear(tex, (int)AVATAR_SIZE.x, (int)AVATAR_SIZE.y);
			_current.setAvatar(tex);

			GL.sRGBWrite = sRGBBackup;
			if (_refresh != null)
				_refresh();
		}

		public void requestUserData()
		{
			Dictionary<string, string> headers = new Dictionary<string, string>();
			headers.Add("Authorization", "Bearer " + EditorPrefs.GetString(accessTokenKey));
			SketchfabRequest request = new SketchfabRequest(SketchfabPlugin.Urls.userMe, headers);
			request.setCallback(handleUserData);
			request.setFailedCallback(logout);
			SketchfabPlugin.getAPI().registerRequest(request);
		}

		private void onLoginFailed(string res)
		{
			JSONNode response = Utils.JSONParse(res);
			EditorUtility.DisplayDialog("Login error", "Authentication failed: " + response["error_description"], "OK");
			logout();
		}

		public void checkAccessTokenValidity()
		{
			string access_token = EditorPrefs.GetString(accessTokenKey);
			if (access_token == null || access_token.Length < 30)
			{
				_hasCheckedSession = true;
				return;
			}
			requestUserData();
		}

		private void handleUserData(string response)
		{
			JSONNode userData = Utils.JSONParse(response);
			_current = new SketchfabProfile(userData["username"], userData["displayName"], userData["account"]);
			requestAvatar(getAvatarUrl(userData));
			_isUserLogged = true;
			_hasCheckedSession = true;
		}

		public bool canPrivate()
		{
			return _current != null && _current._userCanPrivate == 1;
		}

		public bool checkUserPlanFileSizeLimit(long size)
		{
			if (_current == null)
				return false;
			if (_current.maxUploadSize > size)
				return true;

			return false;
		}

		public bool isUserBasic()
		{
			if (_current != null)
				return _current.accountLabel == "BASIC";
			else
				return true;
		}

		private void onCanPrivate(string response)
		{
			JSONNode planResponse = Utils.JSONParse(response);
			_current._userCanPrivate = planResponse["canProtectModels"].AsBool ? 1 : 0;
		}
	}
}
#endif

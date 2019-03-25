/*
 * Copyright(c) 2017-2018 Sketchfab Inc.
 * License: https://github.com/sketchfab/UnityGLTF/blob/master/LICENSE
 */

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using UnityEngine.Networking;
using System.Collections.Specialized;

namespace Sketchfab
{
	public enum SORT_BY
	{
		RELEVANCE,
		LIKES,
		VIEWS,
		RECENT,
	}

	public class SketchfabModel
	{
		// Model info
		public string uid;
		public string name;
		public string author;
		public string description = "";
		public int vertexCount = -1;
		public int faceCount = -1;
		public string hasAnimation = "";
		public string hasSkin = null;
		public JSONNode licenseJson;
		public int archiveSize;

		// Reuse download url while it's still valid
		public string tempDownloadUrl = "";
		public int urlValidityDuration;
		public double downloadRequestTime = 0.0f;

		// Assets
		public Texture2D _thumbnail;
		public Texture2D _preview;
		public string previewUrl;

		public bool isFetched = false;

		public SketchfabModel(JSONNode node)
		{
			parseFromNode(node);
		}

		public SketchfabModel(JSONNode node, Texture2D thumbnail = null)
		{
			parseFromNode(node);
			if (thumbnail != null)
				_thumbnail = thumbnail;
		}

		private void parseFromNode(JSONNode node)
		{
			name = node["name"];
			description = richifyText(node["description"]);
			author = node["user"]["displayName"];
			uid = node["uid"];
			vertexCount = node["vertexCount"].AsInt;
			faceCount = node["faceCount"].AsInt;
			archiveSize = node["archives"]["gltf"]["size"].AsInt;
		}

		private string richifyText(string text)
		{
			if(text != null)
			{
				text = text.Replace("<br>", "");
			}

			return text;
		}

		public void parseModelData(JSONNode node)
		{
			isFetched = true;

			hasAnimation = node["animationCount"].AsInt > 0 ? "Yes" : "No";
			licenseJson = node["license"].AsObject;
		}
	}

	public class SketchfabBrowserManager
	{
		public static string ALL_CATEGORIES = "All";
		SketchfabImporter _importer;
		public SketchfabAPI _api;
		private Texture2D _defaultThumbnail;

		// _categories
		Dictionary<string, string> _categories;

		// Search
		private const string INITIAL_SEARCH = "&staffpicked=true&sort_by=-publishedAt";
		private const string START_QUERY = "?type=models&downloadable=true&";
		string _lastQuery;
		string _prevCursor = "";
		string _nextCursor = "";
		public bool _isFetching = false;

		//Results
		OrderedDictionary _sketchfabModels;

		// Thumbnails (search) and previews (model info)
		int _thumbnailSize = 128;
		int _previewWidth = 512;
		float _previewRatio = 0.5625f;
		bool _hasFetchedPreviews = false;

		// Callbacks
		UpdateCallback _refreshCallback;
		RefreshCallback _downloadFinished;
		UnityGLTF.GLTFEditorImporter.ProgressCallback _importProgress;
		UnityGLTF.GLTFEditorImporter.RefreshWindow _importFinish;

		public SketchfabBrowserManager(UpdateCallback refresh = null, bool initialSearch = true)
		{
			_defaultThumbnail = Resources.Load<Texture2D>("defaultModel");
			checkValidity();
			_refreshCallback = refresh;

			if (initialSearch)
			{
				startInitialSearch();
			}
		}

		void Awake()
		{
			checkValidity();
		}

		void OnEnable()
		{
			SketchfabPlugin.Initialize();
			checkValidity();
		}

		public void Update()
		{
			checkValidity();
			SketchfabPlugin.Update();
			_importer.Update();
		}

		// Callbacks
		public void setRefreshCallback(UpdateCallback callback)
		{
			_refreshCallback = callback;
		}

		public void setImportProgressCallback(UnityGLTF.GLTFEditorImporter.ProgressCallback callback)
		{
			_importProgress = callback;
		}

		public void setImportFinishCallback(UnityGLTF.GLTFEditorImporter.RefreshWindow callback)
		{
			_importFinish = callback;
		}

		public void Refresh()
		{
			if (_refreshCallback != null)
				_refreshCallback();
		}

		// Manager integrity and reset
		private void checkValidity()
		{
			SketchfabPlugin.checkValidity();
			if (_api == null)
			{
				_api = SketchfabPlugin.getAPI();
			}

			if (_sketchfabModels == null)
			{
				_sketchfabModels = new OrderedDictionary();
			}

			if (_categories == null)
			{
				fetchCategories();
			}

			if (_importer == null)
			{
				_importer = new SketchfabImporter(ImportProgress, ImportFinish);
			}
		}

		void reset()
		{
			_sketchfabModels.Clear();
		}

		// Categories
		void fetchCategories()
		{
			_categories = new Dictionary<string, string>();
			SketchfabRequest request = new SketchfabRequest(SketchfabPlugin.Urls.categoryEndpoint);
			request.setCallback(handleCategories);
			_api.registerRequest(request);
		}

		void handleCategories(string result)
		{
			JSONArray _categoriesArray = Utils.JSONParse(result)["results"].AsArray;
			_categories.Clear();
			_categories.Add(ALL_CATEGORIES, "");
			foreach (JSONNode node in _categoriesArray)
			{
				_categories.Add(node["name"], node["slug"]);
			}
			_refreshCallback();
		}

		public List<string> getCategories()
		{
			List<string> categoryNames = new List<string>();
			foreach (string name in _categories.Keys)
			{
				categoryNames.Add(name);
			}

			return categoryNames;
		}

		//Search
		public void startInitialSearch()
		{
			_lastQuery = SketchfabPlugin.Urls.searchEndpoint + INITIAL_SEARCH;
			startSearch();
		}

		void startNewSearch()
		{
			reset();
			startSearch();
		}

		public void search(string query, bool staffpicked, bool animated, string categoryName, SORT_BY sortby, string maxFaceCount = "", string minFaceCount = "", bool myModels=false)
		{
			reset();
			string searchQuery = (myModels ? SketchfabPlugin.Urls.ownModelsSearchEndpoint : SketchfabPlugin.Urls.searchEndpoint);
			if (query.Length > 0)
			{
				searchQuery = searchQuery + "&q=" + query;
			}

			if (minFaceCount != "")
			{
				searchQuery = searchQuery + "&min_face_count=" + minFaceCount;
			}

			if (maxFaceCount != "")
			{
				searchQuery = searchQuery + "&max_face_count=" + maxFaceCount;
			}

			if (staffpicked)
			{
				searchQuery = searchQuery + "&staffpicked=true";
			}
			if (animated)
			{
				searchQuery = searchQuery + "&animated=true";
			}

			switch (sortby)
			{
				case SORT_BY.RECENT:
					searchQuery = searchQuery + "&sort_by=" + "-publishedAt";
					break;
				case SORT_BY.VIEWS:
					searchQuery = searchQuery + "&sort_by=" + "-viewCount";
					break;
				case SORT_BY.LIKES:
					searchQuery = searchQuery + "&sort_by=" + "-likeCount";
					break;
			}

			if (_categories[categoryName].Length > 0)
				searchQuery = searchQuery + "&categories=" + _categories[categoryName];

			_lastQuery = searchQuery;

			startSearch();
			_isFetching = true;
		}

		void startSearch(string cursor = "")
		{
			_hasFetchedPreviews = false;
			SketchfabRequest request = new SketchfabRequest(_lastQuery + cursor, SketchfabPlugin.getLogger().getHeader());
			request.setCallback(handleSearch);
			_api.registerRequest(request);
		}

		void handleSearch(string response)
		{
			JSONNode json = Utils.JSONParse(response);
			JSONArray array = json["results"].AsArray;
			if (array == null)
				return;

			if (json["cursors"] != null)
			{
				if (json["cursors"]["next"].AsInt == 24)
				{
					_prevCursor = "";
				}
				else if (_nextCursor != "null" && _nextCursor != "")
				{
					_prevCursor = int.Parse(_nextCursor) - 24 + "";
				}

				_nextCursor = json["cursors"]["next"];
			}

			// First model fetch from uid
			foreach (JSONNode node in array)
			{
				if (!isInModels(node["uid"]))
				{
					// Add model to results
					SketchfabModel model = new SketchfabModel(node, _defaultThumbnail);
					model.previewUrl = getThumbnailUrl(node, 768);
					_sketchfabModels.Add(node["uid"].Value, model);

					// Request model thumbnail
					SketchfabRequest request = new SketchfabRequest(getThumbnailUrl(node));
					request.setCallback(handleThumbnail);
					_api.registerRequest(request);
				}
			}
			_isFetching = false;
			Refresh();
		}

		public bool hasNextResults()
		{
			return _nextCursor.Length > 0;
		}

		public void requestNextResults()
		{
			if (!hasNextResults())
			{
				Debug.LogError("No next results");
			}

			if (_sketchfabModels.Count > 0)
				_sketchfabModels.Clear();

			string cursorParam = "&cursor=" + _nextCursor;
			startSearch(cursorParam);
		}

		public bool hasPreviousResults()
		{
			return _prevCursor != "null" && _prevCursor.Length > 0;
		}

		public void requestPreviousResults()
		{
			if (!hasNextResults())
			{
				Debug.LogError("No next results");
			}

			if (_sketchfabModels.Count > 0)
				_sketchfabModels.Clear();

			string cursorParam = "&cursor=" + _prevCursor;
			startSearch(cursorParam);
		}

		public bool hasResults()
		{
			return _sketchfabModels.Count > 0;
		}

		public OrderedDictionary getResults()
		{
			return _sketchfabModels;
		}

		// Model data
		public void fetchModelPreview()
		{
			if (!_hasFetchedPreviews)
			{
				foreach (SketchfabModel model in _sketchfabModels.Values)
				{
					// Request model thumbnail
					SketchfabRequest request = new SketchfabRequest(model.previewUrl);
					request.setCallback(handleThumbnail);
					_api.registerRequest(request);
				}
			}
			_hasFetchedPreviews = true;
		}

		public void fetchModelInfo(string uid)
		{
			SketchfabModel model = _sketchfabModels[uid] as SketchfabModel;
			if (model.licenseJson == null)
			{
				SketchfabRequest request = new SketchfabRequest(SketchfabPlugin.Urls.modelEndPoint + "/" + uid);
				request.setCallback(handleModelData);
				_api.registerRequest(request);
			}
		}

		private bool isInModels(string uid)
		{
			return _sketchfabModels.Contains(uid);
		}

		void handleModelData(string request)
		{
			JSONNode node = Utils.JSONParse(request);
			string nodeuid = node["uid"];
			if (_sketchfabModels == null || !isInModels(node["uid"]))
			{
				return;
			}
			string uid = node["uid"];
			SketchfabModel model = _sketchfabModels[uid] as SketchfabModel;
			model.parseModelData(node);
			_sketchfabModels[uid] = model;
		}

		void handleThumbnail(UnityWebRequest request)
		{
			bool sRGBBackup = GL.sRGBWrite;
			GL.sRGBWrite = true;

			string uid = extractUidFromUrl(request.url);
			if (!isInModels(uid))
			{
				return;
			}

			// Load thumbnail image
			byte[] data = request.downloadHandler.data;
			Texture2D thumb = new Texture2D(2, 2);
			thumb.LoadImage(data);
			if (thumb.width >= _previewWidth)
			{
				var renderTexture = RenderTexture.GetTemporary(_previewWidth, (int) (_previewWidth * _previewRatio), 24);
				var exportTexture = new Texture2D(thumb.height, thumb.height, TextureFormat.ARGB32, false);
#if UNITY_5_6 || UNITY_2017
				if(PlayerSettings.colorSpace == ColorSpace.Linear)
				{
					Material linear2SRGB = new Material(Shader.Find("GLTF/Linear2sRGB"));
					linear2SRGB.SetTexture("_InputTex", thumb);
					Graphics.Blit(thumb, renderTexture, linear2SRGB);
				}
				else
				{
					Graphics.Blit(thumb, renderTexture);
				}
#else
				Graphics.Blit(thumb, renderTexture);
#endif
				exportTexture.ReadPixels(new Rect((thumb.width - thumb.height) / 2, 0, renderTexture.height, renderTexture.height), 0, 0);
				exportTexture.Apply();

				TextureScale.Bilinear(thumb, _previewWidth, (int)(_previewWidth * _previewRatio));
				SketchfabModel model = _sketchfabModels[uid] as SketchfabModel;
				model._preview= thumb;
				_sketchfabModels[uid] = model;
			}
			else
			{
				// Crop it to square
				var renderTexture = RenderTexture.GetTemporary(thumb.width, thumb.height, 24);
				var exportTexture = new Texture2D(thumb.height, thumb.height, TextureFormat.ARGB32, false);

#if UNITY_5_6 || UNITY_2017
				if(PlayerSettings.colorSpace == ColorSpace.Linear)
				{
					Material linear2SRGB = new Material(Shader.Find("GLTF/Linear2sRGB"));
					linear2SRGB.SetTexture("_InputTex", thumb);
					Graphics.Blit(thumb, renderTexture, linear2SRGB);
				}
				else
				{
					Graphics.Blit(thumb, renderTexture);
				}
#else
				Graphics.Blit(thumb, renderTexture);
#endif

				exportTexture.ReadPixels(new Rect((thumb.width - thumb.height) / 2, 0, renderTexture.height, renderTexture.height), 0, 0);
				exportTexture.Apply();
				TextureScale.Bilinear(exportTexture, _thumbnailSize, _thumbnailSize);
				SketchfabModel model = _sketchfabModels[uid] as SketchfabModel;
				model._thumbnail = exportTexture;
				_sketchfabModels[uid] = model;
			}

			GL.sRGBWrite = sRGBBackup;
			Refresh();
		}

		string extractUidFromUrl(string url)
		{
			string[] spl = url.Split('/');
			return spl[4];
		}

		public SketchfabModel getModel(string uid)
		{
			if (!isInModels(uid))
			{
				Debug.LogError("Model " + uid + " is not available");
				return null;
			}

			return _sketchfabModels[uid] as SketchfabModel;
		}

		private string getThumbnailUrl(JSONNode node, int maxWidth = 257)
		{
			JSONArray array = node["thumbnails"]["images"].AsArray;
			Dictionary<int, string> _thumbUrl = new Dictionary<int, string>();
			List<int> _intlist = new List<int>();
			foreach (JSONNode elt in array)
			{
				_thumbUrl.Add(elt["width"].AsInt, elt["url"]);
				_intlist.Add(elt["width"].AsInt);
			}

			_intlist.Sort();
			_intlist.Reverse();
			foreach (int res in _intlist)
			{
				if (res < maxWidth)
					return _thumbUrl[res];
			}

			return null;
		}

		// Model archive download and import
		public void importArchive(byte[] data, string unzipDirectory, string importDirectory, string prefabName, bool addToCurrentScene = false)
		{
			if (!GLTFUtils.isFolderInProjectDirectory(importDirectory))
			{
				EditorUtility.DisplayDialog("Error", "Please select a path within your Asset directory", "OK");
				return;
			}

			_importer.configure(importDirectory, prefabName, addToCurrentScene);
			_importer.loadFromBuffer(data);
		}

		void ImportProgress(UnityGLTF.GLTFEditorImporter.IMPORT_STEP step, int current, int total)
		{
			if (_importProgress != null)
				_importProgress(step, current, total);
		}

		public void FinishUpdate()
		{
			EditorUtility.ClearProgressBar();
			if (_importFinish != null)
				_importFinish();
		}

		void ImportFinish()
		{
			if (_importFinish != null)
				_importFinish();
			_importer.cleanArtifacts();
		}
	}
}

#endif

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

	public enum SEARCH_ENDPOINT
	{
		DOWNLOADABLE,
		MY_MODELS,
		STORE_PURCHASES
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
		public string formattedLicenseRequirements;
		public int archiveSize;
		public bool isModelAvailable = false;

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

			formattedLicenseRequirements = licenseJson["requirements"];

			if (formattedLicenseRequirements.Length > 0)
			{
				formattedLicenseRequirements = licenseJson["requirements"].ToString();
				formattedLicenseRequirements = formattedLicenseRequirements.Replace(".", ".\n");
				formattedLicenseRequirements = formattedLicenseRequirements.Replace("\"", " ");
			}

			bool isEditorial = licenseJson["slug"].ToString() == "\"ed\"";
			bool isStandard = licenseJson["slug"].ToString() == "\"st\"";

			// Dirty formatting for Standard/Editorial licenses.
			// There should be a better formatting code if we add more licenses
			if (isEditorial)
			{
				formattedLicenseRequirements = formattedLicenseRequirements.Replace("that", "\n that");
			}

			if (isStandard)
			{
				formattedLicenseRequirements = formattedLicenseRequirements.Replace(" on ", "\n on ");
				formattedLicenseRequirements = formattedLicenseRequirements.Replace(" and ", "\n and ");
			}

			bool isStoreLicense = (isStandard || isEditorial);
			// Archive size is not returned by the API on store purchases for now, so in this case we can't
			// rely on it
			isModelAvailable = (archiveSize > 0 || isStoreLicense);
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
		private const string INITIAL_SEARCH = "type=models&downloadable=true&staffpicked=true&min_face_count=1&sort_by=-publishedAt";
		string _lastQuery;
		string _prevCursorUrl = "";
		string _nextCursorUrl = "";
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

		public string applySearchFilters(string searchQuery, bool staffpicked, bool animated, string categoryName, string licenseSmug, string maxFaceCount, string minFaceCount)
		{
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

			if (_categories[categoryName].Length > 0)
				searchQuery = searchQuery + "&categories=" + _categories[categoryName];

			if (licenseSmug.Length > 0)
				searchQuery = searchQuery + "&license=" + licenseSmug;

			return searchQuery;
		}

		public void search(string query, bool staffpicked, bool animated, string categoryName, string licenseSmug, string maxFaceCount = "", string minFaceCount = "", SEARCH_ENDPOINT endpoint = SEARCH_ENDPOINT.DOWNLOADABLE, SORT_BY sortBy = SORT_BY.RECENT)
		{
			reset();
			string searchQuery = "";
			switch(endpoint)
			{
				case SEARCH_ENDPOINT.DOWNLOADABLE:
					searchQuery = SketchfabPlugin.Urls.searchEndpoint;
					break;
				case SEARCH_ENDPOINT.MY_MODELS:
					searchQuery = SketchfabPlugin.Urls.ownModelsSearchEndpoint;
					break;
				case SEARCH_ENDPOINT.STORE_PURCHASES:
					searchQuery = SketchfabPlugin.Urls.storePurchasesModelsSearchEndpoint;
					break;
			}
			if (endpoint != SEARCH_ENDPOINT.STORE_PURCHASES)
			{
				// Apply default filters
				searchQuery = searchQuery + "type=models&downloadable=true";
			}

			if (query.Length > 0)
			{
				if (endpoint != SEARCH_ENDPOINT.STORE_PURCHASES)
				{
					searchQuery = searchQuery + "&";
				}

				searchQuery = searchQuery + "q=" + query;
			}

			// Search filters are not available for store purchases
			if (endpoint != SEARCH_ENDPOINT.STORE_PURCHASES)
			{
				searchQuery = applySearchFilters(searchQuery, staffpicked, animated, categoryName, licenseSmug, maxFaceCount, minFaceCount);
				switch (sortBy)
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
			}

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

		void searchCursor(string cursorUrl)
		{
			_hasFetchedPreviews = false;
			SketchfabRequest request = new SketchfabRequest(cursorUrl, SketchfabPlugin.getLogger().getHeader());
			request.setCallback(handleSearch);
			_api.registerRequest(request);
		}

		void handleSearch(string response)
		{
			JSONNode json = Utils.JSONParse(response);
			JSONArray array = json["results"].AsArray;
			if (array == null)
				return;

			_prevCursorUrl = json["previous"];
			_nextCursorUrl = json["next"];

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
			return  _nextCursorUrl.Length > 0 && _nextCursorUrl != "null";
		}

		public void requestNextResults()
		{
			if (!hasNextResults())
			{
				Debug.LogError("No next results");
			}

			if (_sketchfabModels.Count > 0)
				_sketchfabModels.Clear();

			searchCursor(_nextCursorUrl);
		}

		public bool hasPreviousResults()
		{
			return _prevCursorUrl.Length > 0 && _prevCursorUrl != "null";
		}

		public void requestPreviousResults()
		{
			if (!hasNextResults())
			{
				Debug.LogError("No next results");
			}

			if (_sketchfabModels.Count > 0)
				_sketchfabModels.Clear();

			searchCursor(_prevCursorUrl);
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

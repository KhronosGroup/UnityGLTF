/*
 * Copyright(c) 2017-2018 Sketchfab Inc.
 * License: https://github.com/sketchfab/UnityGLTF/blob/master/LICENSE
 */
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace Sketchfab
{
	public class SketchfabBrowser : EditorWindow
	{
		public Texture2D _defaultThumbnail;

		[MenuItem("Sketchfab/Browse Sketchfab")]
		static void Init()
		{
			SketchfabBrowser window = (SketchfabBrowser)EditorWindow.GetWindow(typeof(SketchfabBrowser));
			window.titleContent.text = "AssetBrowser";
			window.Show();
		}

		// Sketchfab elements
		public SketchfabBrowserManager _browserManager;
		public SketchfabLogger _logger;
		public SketchfabUI _ui;
		SketchfabModelWindow _skfbWin;

		int _thumbnailSize = 128;
		Vector2 _scrollView = new Vector2();
		int _categoryIndex;
		int _sortByIndex;
		int _polyCountIndex;

		// Upload params and options
		string[] _categoriesNames;

		// Exporter UI: dynamic elements
		string _currentUid = "";

		// Search parameters
		string[] _sortBy;
		string[] _polyCount;
		string _query = "";
		bool _animated = false;
		bool _staffpicked = true;
		string _categoryName = "";

		float framesSinceLastSearch = 0.0f;
		float nbFrameSearchCooldown = 30.0f;

		void OnEnable()
		{
			SketchfabPlugin.Initialize();
		}

		private void checkValidity()
		{
			if (_browserManager == null)
			{
				_browserManager = new SketchfabBrowserManager(OnRefreshUpdate, true);
				_staffpicked = true;
				_currentUid = "";
				_categoryName = "";
				_categoriesNames = new string[0];

				// Setup sortBy
				_sortBy = new string[] { "Relevance", "Likes", "Views", "Recent" };
				_polyCount = new string[] { "Any", "Up to 10k", "10k to 50k", "50k to 100k", "100k to 250k", "250k +" };
				_sortByIndex = 3;
				this.Repaint();
				GL.sRGBWrite = true;
			}

			SketchfabPlugin.checkValidity();
			_ui = SketchfabPlugin.getUI();
			_logger = SketchfabPlugin.getLogger();
		}

		void resizeWindow(int width, int height)
		{
			Vector2 size = this.minSize;
			this.minSize = new Vector2(width, height);
			this.Repaint();
			this.minSize = size;
		}

		private void Update()
		{
			if (_browserManager != null)
			{
				_browserManager.Update();
				if (_categoriesNames.Length == 0 && _browserManager.getCategories().Count > 0)
				{
					_categoriesNames = _browserManager.getCategories().ToArray();
					this.Repaint();
				}
				if (_browserManager.getResults().Count > 0 && _browserManager.getResults()[0]._preview == null)
				{
					_browserManager.fetchModelPreview();
					this.Repaint();
				}

				framesSinceLastSearch++;
			}
		}

		private void triggerSearch()
		{
			if (framesSinceLastSearch < nbFrameSearchCooldown)
				return;

			if (_skfbWin != null)
				_skfbWin.Close();

			SORT_BY sort;
			switch (_sortByIndex)
			{
				case 0:
					sort = SORT_BY.RELEVANCE;
					break;
				case 1:
					sort = SORT_BY.LIKES;
					break;
				case 2:
					sort = SORT_BY.VIEWS;
					break;
				case 3:
					sort = SORT_BY.RECENT;
					break;
				default:
					sort = SORT_BY.RELEVANCE;
					break;
			}
			string _minFaceCount = "";
			string _maxFaceCount = "";
			switch(_polyCountIndex)
			{
				case 0:
					break;
				case 1:
					_maxFaceCount = "10000";
					break;
				case 2:
					_minFaceCount = "10000";
					_maxFaceCount = "50000";
					break;
				case 3:
					_minFaceCount = "50000";
					_maxFaceCount = "100000";
					break;
				case 4:
					_minFaceCount = "100000";
					_maxFaceCount = "250000";
					break;
				case 5:
					_minFaceCount = "250000";
					break;
			}

			_browserManager.search(_query, _staffpicked, _animated, _categoryName, sort, _maxFaceCount, _minFaceCount);
			framesSinceLastSearch = 0.0f;
		}

		// UI
		void OnGUI()
		{
			checkValidity();
			SketchfabPlugin.displayHeader();

			if (_currentUid.Length > 0)
			{
				displaySeparatedModelPage();
			}

			displaySearchOptions();
			displayNextPrev();
			_scrollView = GUILayout.BeginScrollView(_scrollView);
			displayResults();
			GUILayout.EndScrollView();

			SketchfabPlugin.displayFooter();
		}

		void displaySearchOptions()
		{
			// Query
			displaySearchBox();
			GUILayout.BeginHorizontal("Box");
			displayCategories();
			displayFeatures();
			displayMaxFacesCount();
			displaySortBy();
			GUILayout.EndHorizontal();
			GUI.enabled = _query.Length > 0;

			GUI.enabled = true;
		}

		void displaySearchBox()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Search:", GUILayout.Width(80));
			GUI.SetNextControlName("SearchTextField");
			_query = GUILayout.TextField(_query);

			if(Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "SearchTextField")
			{
				triggerSearch();
			}

			if (GUILayout.Button("Search", GUILayout.Width(120)))
			{
				triggerSearch();
			}
			GUILayout.EndHorizontal();
		}

		void displayCategories()
		{
			if (_categoriesNames.Length > 0)
			{
				GUILayout.Label("Categories");
				int prev = _categoryIndex;
				_categoryIndex = EditorGUILayout.Popup(_categoryIndex, _categoriesNames);
				_categoryName = _categoriesNames[_categoryIndex];
				if (_categoryIndex != prev)
					triggerSearch();
			}
			else
			{
				GUILayout.FlexibleSpace();
				GUILayout.Label("Fetching categories");
				_categoryName = "";
				GUILayout.FlexibleSpace();
			}
		}

		void displayFeatures()
		{
			bool previous = _animated;
			_animated = GUILayout.Toggle(_animated, "Animated");
			if (_animated != previous)
				triggerSearch();
			previous = _staffpicked;
			_staffpicked = GUILayout.Toggle(_staffpicked, "StaffPicked");
			if (_staffpicked != previous)
				triggerSearch();
		}

		void displayMaxFacesCount()
		{
			GUILayout.Label("Max faces count: ");
			int old = _polyCountIndex;
			_polyCountIndex = EditorGUILayout.Popup(_polyCountIndex, _polyCount);
			if (_polyCountIndex != old)
				triggerSearch();
		}

		void displaySortBy()
		{
			GUILayout.Label("Sort by");
			int old = _sortByIndex;
			_sortByIndex = EditorGUILayout.Popup(_sortByIndex, _sortBy, GUILayout.Width(80));
			if (_sortByIndex != old)
				triggerSearch();
		}

		void displayNextPrev()
		{
			GUILayout.BeginHorizontal();
			if (_browserManager.hasPreviousResults())
			{
				if (GUILayout.Button("PREV"))
				{
					closeModelWindow();
					_browserManager.requestPreviousResults();
				}
			}

			GUILayout.FlexibleSpace();
			if (_browserManager.hasNextResults())
			{
				if (GUILayout.Button("NEXT"))
				{
					closeModelWindow();
					_browserManager.requestNextResults();
				}
			}

			GUILayout.EndHorizontal();
		}

		void displayResults()
		{
			int count = 0;
			int buttonLineLength = Mathf.Max(1, Mathf.Min((int)this.position.width / _thumbnailSize, 6));
			bool needClose = false;
			List<SketchfabModel> models = _browserManager.getResults();
			if (models.Count > 0) // Replace by "is ready"
			{
				foreach (SketchfabModel model in models)
				{
					if (count % buttonLineLength == 0)
					{
						GUILayout.BeginHorizontal();
						needClose = true;
					}

					GUILayout.FlexibleSpace();
					displayResult(model);
					GUILayout.FlexibleSpace();

					if (count % buttonLineLength == buttonLineLength - 1)
					{
						GUILayout.EndHorizontal();
						GUILayout.FlexibleSpace();
						needClose = false;
					}

					count++;
				}
			}
			else if (_browserManager._isFetching)
			{
				GUILayout.BeginVertical();
				GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Fetching models ....");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
			else
			{
				GUILayout.BeginVertical();
				GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("No results found.");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
			if (needClose)
			{
				GUILayout.EndHorizontal();
			}
		}

		void displayResult(SketchfabModel model)
		{
			GUILayout.BeginVertical();
			if (GUILayout.Button(new GUIContent(model._thumbnail as Texture2D), GUILayout.MaxHeight(_thumbnailSize), GUILayout.MaxWidth(_thumbnailSize)))
			{
				_currentUid = model.uid;
				_browserManager.fetchModelInfo(_currentUid);
				if (_skfbWin != null)
					_skfbWin.Focus();
			}
			GUILayout.BeginVertical(GUILayout.Width(_thumbnailSize), GUILayout.Height(50));
			GUILayout.Label(model.name, _ui.sketchfabMiniModelname);
			GUILayout.Label("by " + model.author, _ui.sketchfabMiniAuthorname);
			GUILayout.EndVertical();
			GUILayout.EndVertical();
		}

		// Model page
		void displaySeparatedModelPage()
		{
			if (_skfbWin == null)
			{
				_skfbWin = ScriptableObject.CreateInstance<SketchfabModelWindow>();
				_skfbWin.displayModelPage(_browserManager.getModel(_currentUid), this);
				_skfbWin.position = new Rect(this.position.position, new Vector2(530, 660));
				_skfbWin.Show();
				_skfbWin.Repaint();
			}

			_skfbWin.displayModelPage(_browserManager.getModel(_currentUid), this);
			_skfbWin.Show();
			_skfbWin.Repaint();
		}

		public void closeModelPage()
		{
			_currentUid = "";
		}

		void closeModelWindow()
		{
			if (_skfbWin != null)
				_skfbWin.Close();
		}

		// Callbacks
		private void OnRefreshUpdate()
		{
			this.Repaint();
		}

		private void OnFinishImport()
		{
			SketchfabModel model = _browserManager.getModel(_currentUid);
			EditorUtility.ClearProgressBar();
			EditorUtility.DisplayDialog("Import successful", "Model \n" + model.name + " by " + model.author + " has been successfully imported", "OK");
		}

		public void UpdateProgress(UnityGLTF.GLTFEditorImporter.IMPORT_STEP step, int current, int total)
		{
			string element = "";
			switch (step)
			{
				case UnityGLTF.GLTFEditorImporter.IMPORT_STEP.BUFFER:
					element = "Buffer";
					break;
				case UnityGLTF.GLTFEditorImporter.IMPORT_STEP.IMAGE:
					element = "Image";
					break;
				case UnityGLTF.GLTFEditorImporter.IMPORT_STEP.TEXTURE:
					element = "Texture";
					break;
				case UnityGLTF.GLTFEditorImporter.IMPORT_STEP.MATERIAL:
					element = "Material";
					break;
				case UnityGLTF.GLTFEditorImporter.IMPORT_STEP.MESH:
					element = "Mesh";
					break;
				case UnityGLTF.GLTFEditorImporter.IMPORT_STEP.NODE:
					element = "Node";
					break;
				case UnityGLTF.GLTFEditorImporter.IMPORT_STEP.ANIMATION:
					element = "Animation";
					break;
				case UnityGLTF.GLTFEditorImporter.IMPORT_STEP.SKIN:
					element = "Skin";
					break;
			}

			EditorUtility.DisplayProgressBar("Importing glTF", "Importing " + element + " (" + current + " / " + total + ")", (float)current / (float)total);
			this.Repaint();
		}

		private void OnDestroy()
		{
			if(_skfbWin != null)
				_skfbWin.Close();
		}
	}
}

#endif

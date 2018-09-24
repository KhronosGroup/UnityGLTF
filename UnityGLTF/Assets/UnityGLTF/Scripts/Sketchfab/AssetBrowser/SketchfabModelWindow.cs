/*
 * Copyright(c) 2017-2018 Sketchfab Inc.
 * License: https://github.com/sketchfab/UnityGLTF/blob/master/LICENSE
 */

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using System.IO;

namespace Sketchfab
{
	public class SketchfabModelWindow : EditorWindow
	{
		SketchfabModel _currentModel;
		SketchfabUI _ui;
		SketchfabBrowser _window;

		string _prefabName;
		string _importDirectory;
		bool _addToCurrentScene;
		SketchfabRequest _modelRequest;

		bool show = false;
		byte[] _lastArchive;

		Vector2 _scrollView = new Vector2();

		static void Init()
		{
			SketchfabModelWindow window = (SketchfabModelWindow)EditorWindow.GetWindow(typeof(SketchfabModelWindow));
			window.titleContent.text = "Model";
			window.Show();
		}

		public void displayModelPage(SketchfabModel model, SketchfabBrowser browser)
		{
			_window = browser;
			if(_currentModel == null || model.uid != _currentModel.uid)
			{
				_currentModel = model;
				_prefabName = GLTFUtils.cleanName(_currentModel.name);
				_importDirectory = Application.dataPath + "/Import/" + _prefabName.Replace(" ", "_");
			}
			else
			{
				_currentModel = model;
			}

			_ui = SketchfabPlugin.getUI();
			show = true;
		}

		private void OnGUI()
		{
			if (_currentModel != null && show)
			{
				_scrollView = GUILayout.BeginScrollView(_scrollView);
				SketchfabModel model = _currentModel;

				GUILayout.BeginHorizontal();

				GUILayout.BeginVertical();
				_ui.displayModelName(model.name);
				_ui.displayContent("by " + model.author);
				GUILayout.EndVertical();

				GUILayout.FlexibleSpace();
				if (GUILayout.Button("View on Sketchfab"))
				{
					Application.OpenURL(SketchfabPlugin.Urls.modelUrl + "/" + _currentModel.uid);
				}

				GUILayout.EndHorizontal();


				GUIStyle blackGround = new GUIStyle(GUI.skin.box);
				blackGround.normal.background = SketchfabUI.MakeTex(2, 2, new Color(0f, 0f, 0f, 1f));

				GUILayout.BeginHorizontal(blackGround);
				GUILayout.FlexibleSpace();
				if(PlayerSettings.colorSpace == ColorSpace.Linear)
				{
					bool backup = GL.sRGBWrite;
					GL.sRGBWrite = true;
					GUILayout.Label(model._preview);
					GL.sRGBWrite = backup;
				}
				else
				{
					GUILayout.Label(model._preview);
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				displayImportSettings();
				GUILayout.Label("");

				GUILayout.BeginHorizontal();

				GUILayout.BeginVertical(GUILayout.Width(250));
				_ui.displayTitle("MODEL INFORMATION");
				_ui.displayModelStats("Vertex count", " " + Utils.humanifySize(model.vertexCount));
				_ui.displayModelStats("Face count", " " + Utils.humanifySize(model.faceCount));
				if(model.hasAnimation != "")
					_ui.displayModelStats("Animation", model.hasAnimation);

				GUILayout.EndVertical();

				GUILayout.BeginVertical(GUILayout.Width(300));
				_ui.displayTitle("LICENSE");
				if(model.licenseJson != null)
				{
					_ui.displayContent(model.licenseJson["fullName"]);
					_ui.displaySubContent(model.licenseJson["requirements"]);
				}
				else
				{
					_ui.displaySubContent("Fetching license data");
				}
				GUILayout.EndVertical();

				GUILayout.EndHorizontal();
				GUILayout.EndScrollView();
			}
		}

		void displayImportSettings()
		{
			GUILayout.BeginVertical("Box");
			_ui.displayContent("Import into");
			GUILayout.BeginHorizontal();
			GUILayout.Label(GLTFUtils.getPathProjectFromAbsolute(_importDirectory), GUILayout.Height(18));
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Change", GUILayout.Width(80), GUILayout.Height(18)))
			{
				string newImportDir = EditorUtility.OpenFolderPanel("Choose import directory", Application.dataPath, "");
				if (GLTFUtils.isFolderInProjectDirectory(newImportDir))
				{
					_importDirectory = newImportDir;
				}
				else if (newImportDir != "")
				{
					EditorUtility.DisplayDialog("Error", "Please select a path within your current Unity project (with Assets/)", "Ok");
				}
				else
				{
					// Path is empty, user canceled. Do nothing
				}
			}
			GUILayout.EndHorizontal();
			_ui.displayContent("Options");
			GUILayout.BeginHorizontal();
			GUILayout.Label("Prefab name");
			_prefabName = GUILayout.TextField(_prefabName, GUILayout.Width(200));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			_addToCurrentScene = GUILayout.Toggle(_addToCurrentScene, "Add to current scene");

			GUILayout.BeginHorizontal();
			Color old = GUI.color;
			GUI.color = SketchfabUI.SKFB_BLUE;
			GUI.contentColor = Color.white;
			string buttonCaption = "";
			if (!_window._logger.isUserLogged())
			{
				buttonCaption = "You need to be logged in to download and import assets";
				GUI.enabled = false;
			}
			else
			{
				buttonCaption = "Download model (" + Utils.humanifyFileSize(_currentModel.archiveSize) + ")";
			}
			if (GUILayout.Button(buttonCaption))
			{

				if (!assetAlreadyExists() || EditorUtility.DisplayDialog("Override asset", "The asset " + _prefabName + " already exists in project. Do you want to override it ?", "Override", "Cancel"))
				{
					// Reuse if still valid
					if(_currentModel.tempDownloadUrl.Length > 0 && EditorApplication.timeSinceStartup - _currentModel.downloadRequestTime < _currentModel.urlValidityDuration)
					{
						requestArchive(_currentModel.tempDownloadUrl);
					}
					else
					{
						fetchGLTFModel(_currentModel.uid, OnArchiveUpdate, _window._logger.getHeader());
					}
				}
			}
			GUI.color = old;
			GUI.enabled = true;
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		private bool assetAlreadyExists()
		{
			string prefabPath = _importDirectory + "/" + _prefabName + ".prefab";
			return File.Exists(prefabPath);
		}

		private void OnArchiveUpdate()
		{
			EditorUtility.ClearProgressBar();
			string _unzipDirectory = Application.temporaryCachePath + "/unzip";
			_window._browserManager.setImportProgressCallback(UpdateProgress);
			_window._browserManager.setImportFinishCallback(OnFinishImport);
			_window._browserManager.importArchive(_lastArchive, _unzipDirectory, _importDirectory, _prefabName, _addToCurrentScene);
		}

		private void handleDownloadCallback(float current)
		{
			if(EditorUtility.DisplayCancelableProgressBar("Download", "Downloading model archive ", (float)current))
			{
				if(_modelRequest != null)
				{
					_window._browserManager._api.dropRequest(ref _modelRequest);
					_modelRequest = null;
				}
				clearProgress();
			}
		}

		private void clearProgress()
		{
			EditorUtility.ClearProgressBar();
		}

		private void OnFinishImport()
		{
			EditorUtility.ClearProgressBar();
			EditorUtility.DisplayDialog("Import successful", "Model \n" + _currentModel.name + " by " + _currentModel.author + " has been successfully imported", "OK");
		}

		public void fetchGLTFModel(string uid, RefreshCallback fetchedCallback, Dictionary<string, string> headers)
		{
			string url = SketchfabPlugin.Urls.modelEndPoint + "/" + uid + "/download";
			_modelRequest = new SketchfabRequest(url, headers);
			_modelRequest.setCallback(handleDownloadAPIResponse);
			_window._browserManager._api.registerRequest(_modelRequest);
		}

		void handleArchive(byte[] data)
		{
			_lastArchive = data;
			OnArchiveUpdate();
		}


		void handleDownloadAPIResponse(string response)
		{
			JSONNode responseJson = Utils.JSONParse(response);
			if(responseJson["gltf"] != null)
			{
				_currentModel.tempDownloadUrl = responseJson["gltf"]["url"];
				_currentModel.urlValidityDuration = responseJson["gltf"]["expires"].AsInt;
				_currentModel.downloadRequestTime = EditorApplication.timeSinceStartup;
				requestArchive(_currentModel.tempDownloadUrl);
			}
			else
			{
				Debug.Log("Unexpected Error: Model archive is not available");
			}
			this.Repaint();
		}

		void requestArchive(string modelUrl)
		{
			SketchfabRequest request = new SketchfabRequest(_currentModel.tempDownloadUrl);
			request.setCallback(handleArchive);
			request.setProgressCallback(handleDownloadCallback);
			SketchfabPlugin.getAPI().registerRequest(request);
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
			_window.closeModelPage();
		}
	}
}

#endif
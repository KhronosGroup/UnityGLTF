/*
 * Copyright(c) 2017-2018 Sketchfab Inc.
 * License: https://github.com/sketchfab/UnityGLTF/blob/master/LICENSE
 */
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor.SceneManagement;
using UnityGLTF;
using SimpleJSON;
using UnityEngine.Networking;

namespace Sketchfab
{
	public class SketchfabExporter : EditorWindow
	{
		[MenuItem("Sketchfab/Publish to Sketchfab")]
		static void Init()
		{
			SketchfabExporter window = (SketchfabExporter)EditorWindow.GetWindow(typeof(SketchfabExporter));
			window.titleContent.image = Resources.Load<Texture>("icon");
			window.titleContent.image.filterMode = FilterMode.Bilinear;
			window.titleContent.text = "Exporter";
			window.Show();
		}

		// Sketchfab elements
		SketchfabAPI _api;
		SketchfabLogger _logger;
		SketchfabUI _ui;
		SketchfabRequest _uploadRequest;

		// Upload params and options
		private bool opt_exportAnimation = true;
		private bool opt_exportSelection = false;
		private string param_name = "";
		private string param_description = "";
		private string param_tags = "";
		private bool param_autopublish = true;
		private bool param_private = false;
		private string param_password = "";

		// Export paths
		private string exportPath;
		private string zipPath;

		// Exporter UI: dynamic elements
		private string status = "";
		Vector2 _scrollView = new Vector2();

		void Awake()
		{
			zipPath = Application.temporaryCachePath + "/" + "Unity2Skfb.zip";
			exportPath = Application.temporaryCachePath + "/" + "Unity2Skfb.gltf";
		}

		void OnEnable()
		{
			// Pre-fill model name with scene name if empty
			if (param_name.Length == 0)
			{
				param_name = EditorSceneManager.GetActiveScene().name;
			}
		}

		private void checkValidity()
		{
			if (_ui == null)
			{
				_ui = SketchfabPlugin.getUI();
			}
			if (_api == null)
			{
				_api = SketchfabPlugin.getAPI();
			}
			if (_logger == null)
			{
				_logger = SketchfabPlugin.getLogger();
			}
		}

		private void Update()
		{
			SketchfabPlugin.Update();
		}

		//UI
		void OnGUI()
		{
			checkValidity();
			if (_ui == null || !_ui._isInitialized)
			{
				GUILayout.Label("Initializing ui...");
				return;
			}

			SketchfabPlugin.displayHeader();

			GUILayout.Space(SketchfabPlugin.SPACE_SIZE);

			showModelProperties();

			GUILayout.Space(SketchfabPlugin.SPACE_SIZE);
			showOptions();

			bool enable = updateExporterStatus();
			if (enable)
				GUI.color = SketchfabUI.SKFB_BLUE;
			else
				GUI.color = Color.white;

			GUI.enabled = enable;
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if (GUILayout.Button(status, GUILayout.Width(250), GUILayout.Height(40)))
			{
				if (!enable)
				{
					EditorUtility.DisplayDialog("Error", status, "Ok");
				}
				else
				{
					proceedToExportAndUpload();
				}
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUI.color = Color.white;

			SketchfabPlugin.displayFooter();
		}

		private bool updateExporterStatus()
		{
			status = "";

			if (!_logger.isUserLogged())
			{
				status = "You need to be logged to upload";
				return false;
			}

			if (param_name.Length > SketchfabPlugin.NAME_LIMIT)
			{
				status = "Model name is too long";
				return false;
			}

			if (param_name.Length == 0)
			{
				status = "Please give a name to your model";
				return false;
			}


			if (param_description.Length > SketchfabPlugin.DESC_LIMIT)
			{
				status = "Model description is too long";
				return false;
			}


			if (param_tags.Length > SketchfabPlugin.TAGS_LIMIT)
			{
				status = "Model tags are too long";
				return false;
			}

			if (opt_exportSelection)
			{
				if (Selection.GetTransforms(SelectionMode.Deep).Length == 0)
				{
					status = "No object selected to export";
					return false;
				}
				else
				{
					status = "Upload selection to Sketchfab";
				}
			}
			else
			{
				status = "Upload scene to Sketchfab";
			}

			return true;
		}

		private void showModelProperties()
		{
			_scrollView = GUILayout.BeginScrollView(_scrollView);
			// Model settings
			GUILayout.Label("Model properties", EditorStyles.boldLabel);

			// Model name
			GUILayout.Label("Name");
			param_name = EditorGUILayout.TextField(param_name);
			GUILayout.Label("(" + param_name.Length + "/" + SketchfabPlugin.NAME_LIMIT + ")", EditorStyles.centeredGreyMiniLabel);

			EditorStyles.textField.wordWrap = true;
			GUILayout.Space(SketchfabPlugin.SPACE_SIZE);

			GUILayout.Label("Description");
			param_description = EditorGUILayout.TextArea(param_description);
			GUILayout.Label("(" + param_description.Length + " / 1024)", EditorStyles.centeredGreyMiniLabel);
			GUILayout.Space(SketchfabPlugin.SPACE_SIZE);
			GUILayout.Label("Tags (separated by spaces)");
			param_tags = EditorGUILayout.TextField(param_tags);
			GUILayout.Label("'unity' and 'unity3D' added automatically (" + param_tags.Length + "/50)", EditorStyles.centeredGreyMiniLabel);

			showPrivate();

			GUILayout.EndScrollView();
		}

		private void showPrivate()
		{

			if (!_logger.canPrivate())
			{
				if (_logger.isUserBasic())
				{
					GUILayout.BeginHorizontal();
					GUIContent content = new GUIContent("features", SketchfabUI.getPlanIcon("pro"));
					GUILayout.Label(content, EditorStyles.boldLabel, GUILayout.Height(18));
					Color old = GUI.color;
					GUI.color = SketchfabUI.SKFB_BLUE;
					if (GUILayout.Button("<color=" + Color.white + ">UPGRADE</color>", _ui.getSketchfabButton(), GUILayout.Height(18)))
					{
						Application.OpenURL(SketchfabPlugin.Urls.plans);
					}
					GUI.color = old;
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
				}
				else
				{
					if (GUILayout.Button("(" + SketchfabUI.ClickableTextColor("You cannot set any other model to private (limit reached)") + ")", _ui.getSketchfabClickableLabel(), GUILayout.Height(20)))
					{
						Application.OpenURL(SketchfabPlugin.Urls.plans);
					}
				}
			}
			else
			{
				GUILayout.Label("Set the model to Private", EditorStyles.centeredGreyMiniLabel);
			}

			GUI.enabled = _logger.canPrivate();
			EditorGUILayout.BeginVertical("Box");
			GUILayout.BeginHorizontal();
			param_private = EditorGUILayout.Toggle("Private model", param_private);

			if (GUILayout.Button("( " + SketchfabUI.ClickableTextColor("more info") + ")", _ui.getSketchfabClickableLabel(), GUILayout.Height(20)))
			{
				Application.OpenURL(SketchfabPlugin.Urls.privateInfo);
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUI.enabled = param_private;
			GUILayout.Label("Password");
			param_password = EditorGUILayout.TextField(param_password);
			EditorGUILayout.EndVertical();

			GUI.enabled = true;
		}

		private void showOptions()
		{
			GUILayout.Label("Options", EditorStyles.boldLabel);
			GUILayout.BeginHorizontal();
			opt_exportAnimation = EditorGUILayout.Toggle("Export animation (beta)", opt_exportAnimation);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			opt_exportSelection = EditorGUILayout.Toggle("Export selection", opt_exportSelection);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			param_autopublish = EditorGUILayout.Toggle("Publish immediately ", param_autopublish);
			if (GUILayout.Button("(" + SketchfabUI.ClickableTextColor("more info") + ")", _ui.getSketchfabClickableLabel(), GUILayout.Height(20)))
			{
				Application.OpenURL(SketchfabPlugin.Urls.latestRelease);
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		// Export
		private void proceedToExportAndUpload()
		{
			if (System.IO.File.Exists(zipPath))
			{
				System.IO.File.Delete(zipPath);
			}

			// "Sketchfab Plugin (Unity " + Application.unityVersion + ")"
			var exporter = new GLTFEditorExporter(opt_exportSelection ? GLTFUtils.getSelectedTransforms() : GLTFUtils.getSceneTransforms());
			exporter.setProgressCallback(OnExportProgress);
			exporter.setExportFinishCallback(OnExportFinish);
			exporter.enableAnimation(opt_exportAnimation);
			exporter.SaveGLTFandBin(Path.GetDirectoryName(exportPath), Path.GetFileNameWithoutExtension(exportPath));

			GLTFUtils.buildZip(exporter.getExportedFilesList(), Path.Combine(Path.GetDirectoryName(exportPath), "Unity2Skfb.zip"), true);
			if (File.Exists(zipPath))
			{
				bool shouldUpload = checkFileSize(zipPath);

				if (!shouldUpload)
				{
					shouldUpload = EditorUtility.DisplayDialog("Error", "The export exceed the max file size allowed by your current account type", "Continue", "Cancel");
				}

				publishModel(zipPath);
			}
			else
			{
				Debug.Log("Zip file has not been generated. Aborting publish.");
			}
		}

		private bool checkFileSize(string zipPath)
		{
			FileInfo file = new FileInfo(zipPath);
			status = "Uploading " + file.Length / (1024.0f * 1024.0f);
			return _logger.checkUserPlanFileSizeLimit(file.Length);
		}

		private void OnExportProgress(UnityGLTF.GLTFEditorExporter.EXPORT_STEP step, float current, float total)
		{
			string element = "";
			switch (step)
			{
				case UnityGLTF.GLTFEditorExporter.EXPORT_STEP.NODES:
					element = "Node";
					break;
				case UnityGLTF.GLTFEditorExporter.EXPORT_STEP.ANIMATIONS:
					element = "Image";
					break;
				case UnityGLTF.GLTFEditorExporter.EXPORT_STEP.SKINNING:
					element = "Skin";
					break;
				case UnityGLTF.GLTFEditorExporter.EXPORT_STEP.IMAGES:
					element = "Image";
					break;
			}

			EditorUtility.DisplayProgressBar("Exporting Scene to glTF", "Exporting" + element + " (" + current + " / " + total + ")", (float)current / (float)total);
			this.Repaint();
		}

		private void OnExportFinish()
		{
			EditorUtility.ClearProgressBar();
		}

		private void publishModel(string zipPath)
		{
			byte[] data = File.ReadAllBytes(zipPath);
			WWWForm postForm = new WWWForm();
			Dictionary<string, string> parameters = buildParameterDictWWW();
			foreach (string param in parameters.Keys)
			{
				postForm.AddField(param, parameters[param]);
			}

			postForm.AddBinaryData("modelFile", data, zipPath, "application /zip");
			postForm.AddField("source", "unity-exporter");

			UnityWebRequest ure = UnityWebRequest.Post(SketchfabPlugin.Urls.postModel, postForm);
			ure.SetRequestHeader("Authorization", _logger.getHeader()["Authorization"]);
			SketchfabRequest request = new SketchfabRequest(ure);

			request.setCallback(onModelPublished);
			request.setProgressCallback(handleUploadCallback);
			request.setFailedCallback(handleUploadError);
			_api.registerRequest(request);
			_uploadRequest = request;
		}

		private void onModelPublished(Dictionary<string, string> responseHeaders)
		{
			EditorUtility.ClearProgressBar();
			string modeluid = responseHeaders["LOCATION"].Split('/')[responseHeaders["LOCATION"].Split('/').Length - 1];
			Application.OpenURL(SketchfabPlugin.Urls.modelUrl + "/" + modeluid);
		}

		private void handleUploadCallback(float current)
		{
			if (EditorUtility.DisplayCancelableProgressBar("Uploading", "Uploading model to Sketchfab", current))
			{
				if (_uploadRequest != null)
				{
					_api.dropRequest(ref _uploadRequest);
					_uploadRequest = null;
				}
				EditorUtility.ClearProgressBar();
			}
		}

		private void handleUploadError()
		{
			EditorUtility.ClearProgressBar();
			EditorUtility.DisplayDialog("Upload Error", "An error occured when uploading the model:\n", "Ok");
		}

		private Dictionary<string, string> buildParameterDictWWW()
		{
			Dictionary<string, string> parameters = new Dictionary<string, string>();
			parameters["name"] = param_name;
			parameters["description"] = param_description;
			parameters["tags"] = "unity unity3D " + param_tags;
			parameters["private"] = param_private ? "1" : "0";
			parameters["isPublished"] = param_autopublish ? "1" : "0";
			if (param_private)
				parameters["password"] = param_password;

			return parameters;
		}

		void OnDestroy()
		{
			if (System.IO.File.Exists(zipPath))
				System.IO.File.Delete(zipPath);
		}
	}
}
#endif

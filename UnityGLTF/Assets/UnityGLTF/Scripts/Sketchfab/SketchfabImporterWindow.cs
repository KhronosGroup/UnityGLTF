/*
 * Copyright(c) 2017-2018 Sketchfab Inc.
 * License: https://github.com/sketchfab/UnityGLTF/blob/master/LICENSE
 */
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Ionic.Zip;

namespace Sketchfab
{
	class SketchfabImporterWindow : EditorWindow
	{
		[MenuItem("Sketchfab/Import glTF")]
		static void Init()
		{
			SketchfabImporterWindow window = (SketchfabImporterWindow)EditorWindow.GetWindow(typeof(SketchfabImporterWindow));
			window.titleContent.text = "Importer";
			window.titleContent.image = Resources.Load<Texture>("icon");
			window.titleContent.image.filterMode = FilterMode.Bilinear;
			window.Show(true);
		}

		// Sketchfab elements
		SketchfabUI _ui;
		SketchfabImporter _importer;

		// Import paths and options
		string _unzipDirectory = "";
		string _importFilePath = "";
		string _defaultImportDirectory = "";
		string _importDirectory = "";

		static string _currentSampleName = "Imported";
		bool _addToCurrentScene = false;
		string[] fileFilters = { "GLTF Model", "gltf,glb", "Archive", "zip" };

		private List<string> _unzippedFiles;

		// UI elements
		Vector2 UI_SIZE = new Vector2(350, 21);
		float minWidthButton = 150;
		Vector2 _scrollView;
		string _sourceFileHint = "Select or drag and drop a file";

		private void Initialize()
		{
			SketchfabPlugin.Initialize();
			_importer = new SketchfabImporter(UpdateProgress, OnFinishImport);
			_unzippedFiles = new List<string>();
			_unzipDirectory = Application.temporaryCachePath + "/unzip";
			_defaultImportDirectory = Application.dataPath + "/Import";
			_importDirectory = _defaultImportDirectory;
			_importFilePath = _sourceFileHint;
			_ui = SketchfabPlugin.getUI();
		}

		private void Update()
		{
			SketchfabPlugin.Update();
			if (_importer != null)
				_importer.Update();

		}

		void OnCheckVersionFailure()
		{
			Debug.Log("Failed to retrieve Plugin version");
		}

		private string findGltfFile()
		{
			string gltfFile = "";
			DirectoryInfo info = new DirectoryInfo(_unzipDirectory);
			foreach (FileInfo fileInfo in info.GetFiles())
			{
				_unzippedFiles.Add(fileInfo.FullName);
				if (Path.GetExtension(fileInfo.FullName) == ".gltf")
				{
					gltfFile = fileInfo.FullName;
				}
			}

			return gltfFile;
		}

		private string unzipGltfArchive(string zipPath)
		{
			if (!Directory.Exists(_unzipDirectory))
				Directory.CreateDirectory(_unzipDirectory);

			// Clean previously unzipped files
			GLTFUtils.removeFileList(_unzippedFiles.ToArray());
			string gltfFile = findGltfFile();
			if (gltfFile != "")
			{
				File.Delete(gltfFile);
			}

			// Extract archive
			ZipFile zipfile = ZipFile.Read(zipPath);
			zipfile.ExtractAll(_unzipDirectory, ExtractExistingFileAction.OverwriteSilently);

			return findGltfFile();
		}

		private string unzipGltfArchive(byte[] zipData)
		{


			return findGltfFile();
		}

		private void checkValidity()
		{
			SketchfabPlugin.checkValidity();
			if(_ui == null)
			{
				_ui = new SketchfabUI();
			}
			if (_importer == null)
			{
				Initialize();
			}
		}

		private void handleDragNDrop()
		{
			DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

			if (Event.current.type == EventType.DragExited)
			{
				if (DragAndDrop.paths.Length > 0)
				{
					_importFilePath = DragAndDrop.paths[0];
					//updateSettingsWithFile();
				}
			}
		}

		private void updateSettingsWithFile()
		{
			string modelfileName = Path.GetFileNameWithoutExtension(_importFilePath);
			_importDirectory = GLTFUtils.unifyPathSeparator(Path.Combine(_defaultImportDirectory, modelfileName));
			_currentSampleName = modelfileName;
		}
		// UI
		private void OnGUI()
		{
			checkValidity();
			SketchfabPlugin.displayHeader();

			if (_ui == null)
				return;

			handleDragNDrop();

			_scrollView = GUILayout.BeginScrollView(_scrollView);
			displayInputInfos();
			displayImportDirectory();
			displayImportOptions();
			GUILayout.EndScrollView();

			displayImportButton();

			SketchfabPlugin.displayFooter();
		}

		private void displayInputInfos()
		{
			GUILayout.Label("Import a glTF (*.gltf, *.glb, *.zip)", _ui.getSketchfabModelName());

			_ui.displaySubContent("Source file:");
			GUILayout.BeginHorizontal();
			Color backup = GUI.color;
			if (_importFilePath == _sourceFileHint)
				GUI.contentColor = Color.red;

			GUILayout.TextField(_importFilePath, GUILayout.MinWidth(UI_SIZE.x), GUILayout.Height(UI_SIZE.y));
			GUI.contentColor = backup;
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Select file", GUILayout.Height(UI_SIZE.y), GUILayout.Width(minWidthButton)))
			{
				string filepath = EditorUtility.OpenFilePanelWithFilters("Choose a file to import", GLTFUtils.getPathAbsoluteFromProject(_importDirectory), fileFilters);
				if (File.Exists(filepath))
				{
					_importFilePath = filepath;
					//updateSettingsWithFile();
				}
				else
				{
					EditorUtility.DisplayDialog("Error", "This file doesn't exist", "Ok");
				}
			}

			GUILayout.EndHorizontal();
		}

		private void displayImportDirectory()
		{
			_ui.displaySubContent("Import into");
			GUILayout.BeginHorizontal();
			GUILayout.TextField(GLTFUtils.getPathProjectFromAbsolute(_importDirectory), GUILayout.MinWidth(UI_SIZE.x), GUILayout.Height(UI_SIZE.y));
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Change destination", GUILayout.Height(UI_SIZE.y), GUILayout.Width(minWidthButton)))
			{
				string newImportDir = EditorUtility.OpenFolderPanel("Choose import directory", _importDirectory, "");
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
		}

		private void displayImportOptions()
		{
			GUILayout.Space(2);
			_ui.displaySubContent("Options");
			GUILayout.BeginHorizontal();

			GUILayout.Label("Prefab name: ", GUILayout.Height(UI_SIZE.y));

			_currentSampleName = GUILayout.TextField(_currentSampleName, GUILayout.MinWidth(200), GUILayout.Height(UI_SIZE.y));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			_addToCurrentScene = GUILayout.Toggle(_addToCurrentScene, "Add to current scene");
			GUILayout.Space(2);
		}

		private void displayImportButton()
		{
			GUILayout.BeginHorizontal();
			Color old = GUI.color;
			GUI.color = SketchfabUI.SKFB_BLUE;
			GUI.contentColor = Color.white;
			GUI.enabled = GLTFUtils.isFolderInProjectDirectory(_importDirectory) && File.Exists(_importFilePath);
			if (GUILayout.Button("IMPORT", _ui.getSketchfabButton()))
			{
				processImportButton();
			}
			GUI.color = old;
			GUI.enabled = true;
			GUILayout.EndHorizontal();
		}

		private void emptyLines(int nbLines)
		{
			for (int i = 0; i < nbLines; ++i)
			{
				GUILayout.Label("");
			}
		}

		private void changeDirectory()
		{
			_importDirectory = EditorUtility.OpenFolderPanel("Choose import directory in Project", Application.dataPath, "Assets");

			// Discard if selected directory is outside of the project
			if (!isDirectoryInProject())
			{
				Debug.Log("Import directory is outside of project directory. Please select path in Assets/");
				_importDirectory = "";
				return;
			}
		}

		private bool isDirectoryInProject()
		{
			return _importDirectory.Contains(Application.dataPath);
		}

		private void processImportButton()
		{
			if (!isDirectoryInProject())
			{
				Debug.LogError("Import directory is outside of project directory. Please select path in Assets/");
				return;
			}

			_importer.configure(_importDirectory, _currentSampleName, _addToCurrentScene);
			_importer.loadFromFile(_importFilePath);
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

		private void OnFinishImport()
		{
			EditorUtility.ClearProgressBar();
			EditorUtility.DisplayDialog("Import successful", "Model has been successfully imported", "OK");
		}

		public void OnDestroy()
		{
			GLTFUtils.removeFileList(_unzippedFiles.ToArray());
			GLTFUtils.removeEmptyDirectory(_unzipDirectory);
		}
	}
}

#endif

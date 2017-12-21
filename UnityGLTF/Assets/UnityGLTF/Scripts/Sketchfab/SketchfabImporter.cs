using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityGLTF;
using Ionic.Zip;

class SketchfabImporter : EditorWindow
{
	[MenuItem("Sketchfab/Import glTF")]
	static void Init()
	{
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
		SketchfabImporter window = (SketchfabImporter)EditorWindow.GetWindow(typeof(SketchfabImporter));
		window.titleContent.text = "glTF importer";
		window.Show(true);
#else // and error dialog if not standalone
		EditorUtility.DisplayDialog("Error", "Your build target must be set to standalone", "Okay");
#endif
	}

	// Public
	public bool _useGLTFMaterial = false;

	private string _defaultImportDirectory = "";
	private static string _currentSampleName = "Imported";
	GLTFEditorImporter _importer;
	string _gltfPath = "";
	string _projectDirectory = "";
	string _unzipDirectory = "";
	private List<string> _unzippedFiles;
	bool _isInitialized = false;
	GUIStyle _header;
	Sketchfab.SketchfabAPI _api;
	Vector2 minimumSize = new Vector2(603, 450);

	void setupAPI()
	{
		_api = new Sketchfab.SketchfabAPI("Unity-exporter");

		//Setup callbacks
		_api.setCheckVersionSuccessCb(OnCheckVersionSuccess);
		_api.setCheckVersionFailedCb(OnCheckVersionFailure);
		_api.checkLatestExporterVersion();
	}

	private void Initialize()
	{
		SketchfabPlugin.Initialize(); // Load header image
		setupAPI();

		_importer = new GLTFEditorImporter(this.Repaint);
		_unzippedFiles = new List<string>();
		_isInitialized = true;
		_unzipDirectory = Application.temporaryCachePath + "/unzip";
		_header = new GUIStyle(EditorStyles.boldLabel);

		_defaultImportDirectory = Application.dataPath + "/Import";

		this.minSize = minimumSize;
	}

	public void displayVersionInfo()
	{
		if (_api.getLatestVersion() == null)
		{
			SketchfabPlugin.showVersionChecking();
		}
		else
		{
			if (_api.getLatestVersion().Length == 0)
			{
				SketchfabPlugin.showVersionCheckError();
			}
			else if (_api.isLatestVersion())
			{
				SketchfabPlugin.showUpToDate(_api.getLatestVersion());
			}
			else
			{
				SketchfabPlugin.showOutdatedVersionWarning(_api.getLatestVersion());
			}
		}
	}

	void OnCheckVersionSuccess()
	{
		Debug.Log("Latest version is " + _api.getLatestVersion());
		if (!_api.isLatestVersion())
		{
			SketchfabPlugin.DisplayVersionPopup();
		}
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
		if(gltfFile != "")
		{
			Debug.Log("GLTF file found, and should be cleaned :" + gltfFile);
			File.Delete(gltfFile);
		}

		// Extract archive
		ZipFile zipfile = ZipFile.Read(zipPath);
		zipfile.ExtractAll(_unzipDirectory, ExtractExistingFileAction.OverwriteSilently);
		
		return findGltfFile();
	}

	private void checkValidity()
	{
		SketchfabPlugin.CheckValidity();
		if(_importer == null)
		{
			Initialize();
		}

		if (_api == null)
		{
			setupAPI();
		}
	}

	public void OnDestroy()
	{
		GLTFUtils.removeFileList(_unzippedFiles.ToArray());
		GLTFUtils.removeEmptyDirectory(_unzipDirectory);
	}

	public void Update()
	{
		if (_api != null)
		{
			_api.Update();
		}

		if (_importer != null)
		{
			_importer.Update();
			Repaint();
		}
	}

	private void OnGUI()
	{
		if (!_isInitialized)
			Initialize();

		checkValidity();

		SketchfabPlugin.showHeader();
		displayVersionInfo();

		DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

		if (Event.current.type == EventType.DragExited)
		{
			if(DragAndDrop.paths.Length > 0)
			{
				_gltfPath = DragAndDrop.paths[0];
				string modelfileName = Path.GetFileNameWithoutExtension(_gltfPath);
				_projectDirectory = Path.Combine(_defaultImportDirectory, modelfileName);
			}
		}

		showImportUI();

		// Options
		emptyLines(1);
		showOptions();
		emptyLines(1);

		// Disable import if nothing valid to import
		GUI.enabled = _gltfPath.Length > 0 && File.Exists(_gltfPath);

		// Import button
		if (GUILayout.Button("IMPORT"))
		{
			processImportButton();
		}

		showStatus();
	}

	private string stripProjectDirectory(string directory)
	{
		return directory.Replace(Application.dataPath, "[PROJECT]");
	}

	// UI FUNCTIONS
	private void showImportUI()
	{
		// Import file
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("Import or Drag'n drop glTF asset(gltf, glb, zip supported)", _header);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.BeginVertical("Box");
		GUILayout.Label("Model to import: " + _gltfPath);
		GUILayout.Label("Import directory: " + stripProjectDirectory(_projectDirectory));

		GUILayout.EndVertical();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Import file from disk"))
		{
			_gltfPath = EditorUtility.OpenFilePanel("Choose glTF to import", Application.dataPath, "glb,gltf,zip");
			string modelfileName = Path.GetFileNameWithoutExtension(_gltfPath);
			_projectDirectory = Path.Combine(_defaultImportDirectory, modelfileName);
		}
		if (GUILayout.Button("Change import directory"))
		{
			changeDirectory();
		}
		GUILayout.EndHorizontal();
	}

	private void emptyLines(int nbLines)
	{
		for(int i=0; i< nbLines; ++i)
		{
			GUILayout.Label("");
		}
	}

	private void changeDirectory()
	{
		_projectDirectory = EditorUtility.OpenFolderPanel("Choose import directory in Project", Application.dataPath, "Assets");

		// Discard if selected directory is outside of the project
		if (!isDirectoryInProject())
		{
			Debug.Log("Import directory is outside of project directory. Please select path in Assets/");
			_projectDirectory = "";
			return;
		}
	}

	private void showOptions()
	{
		GUILayout.Label("Options", _header);
		GUILayout.BeginHorizontal();
		GUILayout.Label("Prefab name:");
		_currentSampleName = GUILayout.TextField(_currentSampleName, GUILayout.Width(250));
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}

	private bool isDirectoryInProject()
	{
		return _projectDirectory.Contains(Application.dataPath);
	}

	private void processImportButton()
	{
		if(!isDirectoryInProject())
		{
			Debug.LogError("Import directory is outside of project directory. Please select path in Assets/");
			return;
		}

		if(!Directory.Exists(_projectDirectory))
		{
			Directory.CreateDirectory(_projectDirectory);
		}

		if (Path.GetExtension(_gltfPath) == ".zip")
		{
			_gltfPath = unzipGltfArchive(_gltfPath);
		}

		_importer.setupForPath(_gltfPath, _projectDirectory, _currentSampleName);
		_importer.Load();
	}

	private void showStatus()
	{
		GUI.enabled = true;
		GUILayout.BeginHorizontal("Box");
		GUILayout.Label("Status: " + _importer.getStatus());
		GUILayout.EndHorizontal();
	}
}

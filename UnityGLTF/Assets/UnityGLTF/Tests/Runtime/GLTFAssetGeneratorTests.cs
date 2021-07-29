#if UNITY_EDITOR && HAVE_ASSET_GENERATOR
using AssetGenerator;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityGLTF;

public class GLTFAssetGeneratorTests
{
	private const int IMAGE_SIZE = 400;
	private static readonly ColorEqualityComparer ColorEqualityComparer = new ColorEqualityComparer(0.1f);
	private static readonly string GLTF_ASSETS_PATH = Application.dataPath + "/../www/glTF-Asset-Generator/Output/Positive/";
	private static readonly string GLTF_MANIFEST_PATH = GLTF_ASSETS_PATH + "Manifest.json";
	private static readonly string GLTF_SCENARIO_OUTPUT_PATH = Application.dataPath + "/../ScenarioTests/Output/";
	private static readonly string GLTF_SCENARIO_TESTS_TO_RUN = Application.dataPath + "/../ScenarioTests/TestsToRun.txt";

	private static GLTFComponent gltfComponent;
	private readonly Dictionary<string, Manifest.Model> modelManifests = new Dictionary<string, Manifest.Model>();

	[OneTimeSetUp]
	public void SetupEnvironment()
	{
		var scenePrefab = MonoBehaviour.Instantiate(Resources.Load<GameObject>("TestScenePrefab"));
		gltfComponent = scenePrefab.GetComponentInChildren<GLTFComponent>() ;

		// Parse manifest into a lookup dictionary
		List<Manifest> manifests = JsonConvert.DeserializeObject<List<Manifest>>(File.ReadAllText(GLTF_MANIFEST_PATH));
		foreach (Manifest manifest in manifests)
		{
			foreach (Manifest.Model model in manifest.Models)
			{
				// Rather than establishing a new data structure to hold the information, simply augment the
				// SampleImageName (which is actually a path) to include the starting folder as well.
				model.SampleImageName = Path.Combine(manifest.Folder, model.SampleImageName);
				modelManifests[Path.GetFileNameWithoutExtension(model.FileName)] = model;
			}
		}
	}

	[TearDown]
	public void CleanupEnvironment()
	{
		if (gltfComponent.LastLoadedScene != null)
		{
			GameObject.DestroyImmediate(gltfComponent.LastLoadedScene);
		}
	}

	public static IEnumerable<string> ModelFilePaths
	{
		get
		{
			string[] TestsFilter = File.ReadAllText(GLTF_SCENARIO_TESTS_TO_RUN).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			Dictionary<string, bool> TestsFilterMap = new Dictionary<string, bool>();
			foreach (string s in TestsFilter)
			{
				TestsFilterMap[s] = true;
			}

			Directory.CreateDirectory(GLTF_SCENARIO_OUTPUT_PATH);
			var gltfPathUri = new Uri(GLTF_ASSETS_PATH);
			string[] files = Directory.GetFiles(GLTF_ASSETS_PATH, "*.gltf", SearchOption.AllDirectories);
			List<string> filteredFiles = new List<string>();
			for (int i = 0; i < files.Length; i++)
			{
				string filename = Path.GetFileNameWithoutExtension(files[i]);

				if (TestsFilterMap.ContainsKey(filename))
				{
					var fileUri = new Uri(files[i]);
					files[i] = gltfPathUri.MakeRelativeUri(fileUri).ToString();
					filteredFiles.Add(files[i]);
				}
			}
			return filteredFiles;
		}
	}

	[UnityTest]
	public IEnumerator GLTFScenarios([ValueSource("ModelFilePaths")] string modelPath)
	{
		// Update the camera position
		Manifest.Model modelManifest = modelManifests[Path.GetFileNameWithoutExtension(modelPath)];
		Manifest.Camera cam = modelManifest.Camera;
		Camera.main.transform.position = new Vector3(cam.Translation[0], cam.Translation[1], cam.Translation[2]);

		// Load the corresponding model
		gltfComponent.GLTFUri = GLTF_ASSETS_PATH + modelPath;
		yield return gltfComponent.Load().AsCoroutine();

		// Wait one frame for rendering to complete
		yield return null;

		// Capture a render of the model
		Camera mainCamera = Camera.main;
		RenderTexture rt = new RenderTexture(IMAGE_SIZE, IMAGE_SIZE, 24);
		mainCamera.targetTexture = rt;
		Texture2D actualContents = new Texture2D(IMAGE_SIZE, IMAGE_SIZE, TextureFormat.RGB24, false);
		mainCamera.Render();
		RenderTexture.active = rt;
		actualContents.ReadPixels(new Rect(0, 0, IMAGE_SIZE, IMAGE_SIZE), 0, 0);
		Color[] actualPixels = actualContents.GetPixels();

		// Save the captured contents to a file
		byte[] pngActualfile = actualContents.EncodeToPNG();
		string outputpath = Path.GetDirectoryName(modelPath);
		string outputfullpath = GLTF_SCENARIO_OUTPUT_PATH + outputpath;
		Directory.CreateDirectory(outputfullpath);
		string filename = Path.GetFileNameWithoutExtension(modelPath);
		string actualFilePath = outputfullpath + "/" + filename + "_ACTUAL.png";
		File.WriteAllBytes(actualFilePath, pngActualfile);

		// Read the expected image
		// NOTE: Ideally this would use the expected image from Path.Combine(GLTF_ASSETS_PATH, modelManifest.SampleImageName), but the
		// current rendered image is not close enough to use this as a source of truth, so until they can be closer aligned we instead
		// generate an 'expected' image ourselves.
		string expectedFilePath = Path.Combine(outputfullpath, filename + "_EXPECTED.png");
#if ENABLE_THIS_BLOCK_TO_CREATE_EXPECTED_FILES
		File.WriteAllBytes(expectedFilePath, pngActualfile);
#endif
		if (!File.Exists(expectedFilePath))
		{
			Assert.Fail("Could not find expected image to compare against: '" + expectedFilePath + "'");
		}
		byte[] expectedFileContents = File.ReadAllBytes(expectedFilePath);
		Texture2D expectedContents = new Texture2D(IMAGE_SIZE, IMAGE_SIZE, TextureFormat.RGB24, false);
		expectedContents.LoadImage(expectedFileContents);
		Color[] expectedPixels = expectedContents.GetPixels();

		// Compare the capture against the expected image
		Assert.AreEqual(expectedPixels.Length, actualPixels.Length);
		string errormessage = "\r\nImage does not match expected within configured tolerance.\r\nExpectedPath: " + expectedFilePath + "\r\n ActualPath: " + actualFilePath;
		for (int i = 0; i < expectedPixels.Length; i++)
		{
			Assert.That(actualPixels[i], Is.EqualTo(expectedPixels[i]).Using(ColorEqualityComparer));
		}
	}
}
#endif

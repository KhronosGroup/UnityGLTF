#if UNITY_EDITOR
using AssetGenerator;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityGLTF;
using Camera = UnityEngine.Camera;
using Object = System.Object;

public class GLTFAssetGeneratorTests
{
	private const float PIXEL_TOLERANCE = 0.01f; // Tolerance based on the estimate that humans see about 1 million colors
	private static string GLTF_ASSETS_PATH = Application.dataPath + "/../www/glTF-Asset-Generator/Output/Positive/";
	private static string GLTF_MANIFEST_PATH = GLTF_ASSETS_PATH + "manifest.json";
	private static string GLTF_SCENARIO_OUTPUT_PATH = Application.dataPath + "/../ScenarioTests/Output/";
	private static string GLTF_SCENARIO_TESTS_TO_RUN = Application.dataPath + "/../ScenarioTests/TestsToRun.txt";

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

		// Read the expected image
		string expectedFilePath = Path.Combine(GLTF_ASSETS_PATH, modelManifest.SampleImageName);
		if (!File.Exists(expectedFilePath))
		{
			Assert.Fail("Could not find expected image to compare against: '" + expectedFilePath + "'");
		}
		byte[] expectedFileContents = File.ReadAllBytes(expectedFilePath);
		// TODO: Can we determine this programmatically instead?
		int dimension = 400; // (int)Math.Round(Math.Sqrt(expectedFileContents.Length));
		Texture2D expectedContents = new Texture2D(dimension, dimension, TextureFormat.RGB24, false);
		expectedContents.LoadImage(expectedFileContents);
		Color[] expectedPixels = expectedContents.GetPixels();

		// Capture a render of a matching size
		Camera mainCamera = Camera.main;
		RenderTexture rt = new RenderTexture(expectedContents.width, expectedContents.height, 24);
		mainCamera.targetTexture = rt;
		Texture2D actualContents = new Texture2D(expectedContents.width, expectedContents.height, TextureFormat.RGB24, false);
		mainCamera.Render();
		RenderTexture.active = rt;
		actualContents.ReadPixels(new Rect(0, 0, expectedContents.width, expectedContents.height), 0, 0);
		Color[] actualPixels = actualContents.GetPixels();

		// For easier debugging, save the captured contents to a file
		byte[] pngActualfile = actualContents.EncodeToPNG();
		string outputpath = Path.GetDirectoryName(modelPath);
		string outputfullpath = GLTF_SCENARIO_OUTPUT_PATH + outputpath;
		Directory.CreateDirectory(outputfullpath);
		string filename = Path.GetFileNameWithoutExtension(modelPath);
		string actualFilePath = outputfullpath + "/" + filename + "_ACTUAL.png";
		File.WriteAllBytes(actualFilePath, pngActualfile);

		// Compare the capture against the expected image
		Assert.AreEqual(expectedPixels.Length, actualPixels.Length);
		string errormessage = "\r\nImage does not match expected within configured tolerance.\r\nExpectedPath: " + expectedFilePath + "\r\n ActualPath: " + actualFilePath;

		for (int i = 0; i < expectedPixels.Length; i++)
		{
			// NOTE: When upgraded to Unity 2018, this custom equality comparison can be replaced with the ColorEqualityComparer, akin to:
			// Assert.That(actualPixels[i], Is.EqualTo(expectedPixels[i]).Using(UnityEngine.TestTools.Utils.ColorEqualityComparer.Instance));
			var rDelta = Math.Abs(expectedPixels[i].r - actualPixels[i].r);
			var gDelta = Math.Abs(expectedPixels[i].g - actualPixels[i].g);
			var bDelta = Math.Abs(expectedPixels[i].b - actualPixels[i].b);
			var aDelta = Math.Abs(expectedPixels[i].a - actualPixels[i].a);
			Assert.Less(rDelta, PIXEL_TOLERANCE, errormessage);
			Assert.Less(gDelta, PIXEL_TOLERANCE, errormessage);
			Assert.Less(bDelta, PIXEL_TOLERANCE, errormessage);
			Assert.Less(aDelta, PIXEL_TOLERANCE, errormessage);
		}
	}
}
#endif

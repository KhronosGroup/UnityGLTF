using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityGLTF;
using UnityGLTF.Examples;
using System.IO;
using System.Linq;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;
using Object = System.Object;
using Newtonsoft.Json;
using AssetGenerator;
using UnityEngine.EventSystems;
using static AssetGenerator.Manifest;
using Camera = UnityEngine.Camera;

public class GLTFAssetGeneratorTests
{
    static string GLTF_ASSETS_PATH = Application.dataPath + "/../www/glTF-Asset-Generator/Output/";
    static string GLTF_CAMERA_MANIFEST_PATH = GLTF_ASSETS_PATH + "manifest.json";
    static string GLTF_SCENARIO_OUTPUT_PATH = Application.dataPath + "/../ScenarioTests/Output/";
    static string GLTF_SCENARIO_TESTS_TO_RUN = Application.dataPath + "/../ScenarioTests/TestsToRun.txt";
    private static bool GENERATE_REFERENCEDATA = false;
    public GameObject ActiveGLTFObject { get; set; }
	
    private Dictionary<string, AssetGenerator.Manifest.Camera> cameras =
        new Dictionary<string, AssetGenerator.Manifest.Camera>();

    [OneTimeSetUp]
    public void SetupEnvironment()
    {
        MonoBehaviour.Instantiate(Resources.Load<GameObject>("TestScenePrefab"));

        //parse camera position manifest
        List<Manifest> manifests = JsonConvert.DeserializeObject<List<Manifest>>(File.ReadAllText(GLTF_CAMERA_MANIFEST_PATH));
        foreach (Manifest manifest in manifests)
        {
            foreach (Model model in manifest.Models)
            {
                cameras[Path.GetFileNameWithoutExtension(model.FileName)] = model.Camera;
            }
        }
    }

    [TearDown]
    public void CleanupEnvironment()
    {
        if (ActiveGLTFObject != null)
        {
            GameObject.DestroyImmediate(ActiveGLTFObject);
        }
        var objects = GameObject.FindObjectsOfType(typeof(GameObject));
        foreach (GameObject o in objects)
        {
            if (o.name.Contains("GLTF"))
            {
                GameObject.DestroyImmediate(o);
            }
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
        ActiveGLTFObject = new GameObject();

        GLTFComponent gltfcomponent = ActiveGLTFObject.AddComponent<GLTFComponent>();
        gltfcomponent.GLTFUri = GLTF_ASSETS_PATH + modelPath;

        AssetGenerator.Manifest.Camera cam = cameras[Path.GetFileNameWithoutExtension(modelPath)];
        Camera.main.transform.position = new Vector3(cam.Translation[0], cam.Translation[1], cam.Translation[2]);
        yield return gltfcomponent.Load();

        //wait one frame for rendering to complete
        yield return null;

        Camera mainCamera = Camera.main;
        RenderTexture rt = new RenderTexture(512, 512, 24);
        mainCamera.targetTexture = rt;
        Texture2D actualContents = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        mainCamera.Render();
        RenderTexture.active = rt;
        actualContents.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        byte[] pngActualfile = actualContents.EncodeToPNG();
        string outputpath = Path.GetDirectoryName(modelPath);
        string outputfullpath = GLTF_SCENARIO_OUTPUT_PATH + outputpath;
        Directory.CreateDirectory(outputfullpath);
        string filename = Path.GetFileNameWithoutExtension(modelPath);
        string expectedFilePath = outputfullpath + "/" + filename + "_EXPECTED.png";
        string actualFilePath = outputfullpath + "/" + filename + "_ACTUAL.png";
        //uncomment to reggenerate master images
        if (GENERATE_REFERENCEDATA)
        {
            File.WriteAllBytes(expectedFilePath, pngActualfile);
        }
        else
        {
            if (File.Exists(expectedFilePath))
            {
                byte[] pngActualfileContents = File.ReadAllBytes(expectedFilePath);

                File.WriteAllBytes(actualFilePath, pngActualfile);
                //compare against expected
                Texture2D expectedContents = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                expectedContents.LoadImage(pngActualfileContents);
                Color[] expectedPixels = expectedContents.GetPixels();
                Color[] actualPixels = actualContents.GetPixels();
                Assert.AreEqual(expectedPixels.Length, actualPixels.Length);
                string errormessage = "\r\nExpectedPath: " + expectedFilePath + "\r\n ActualPath: " + actualFilePath;

                for (int i = 0; i < expectedPixels.Length; i++)
                {
                    Assert.AreEqual(expectedPixels[i], actualPixels[i], errormessage);
                }
            }
        }
    }
}

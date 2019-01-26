using AssetGenerator;
using Newtonsoft.Json;
using NUnit.Compatibility;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityGLTF;
using UnityGLTF.Examples;
using Camera = UnityEngine.Camera;
using Object = System.Object;

public class GLTFSceneTests
{
    static string GLTF_SCENES_PATH = Application.dataPath + "./UnityGLTF/Examples/";
    static string GLTF_SCENARIO_OUTPUT_PATH = Application.dataPath + "/../ScenarioScenesTests/Output/";
    private static bool GENERATE_REFERENCEDATA = false;
 
    private Dictionary<string, AssetGenerator.Manifest.Camera> cameras =
        new Dictionary<string, AssetGenerator.Manifest.Camera>();

 
    public static IEnumerable<string> SceneFilePaths
    {
        get
        {
            Directory.CreateDirectory(GLTF_SCENARIO_OUTPUT_PATH);
            var gltfPathUri = new Uri(GLTF_SCENES_PATH);
            string[] files = Directory.GetFiles(GLTF_SCENES_PATH, "*.unity", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string filename = Path.GetFileNameWithoutExtension(files[i]);
                var fileUri = new Uri(files[i]);
                files[i] = gltfPathUri.MakeRelativeUri(fileUri).ToString();

            }
            return files;
        }
    }

    [UnityTest]
    public IEnumerator GLTFScenarios([ValueSource("SceneFilePaths")] string scenePath)
    {
	    SceneManager.LoadScene(Path.GetFileNameWithoutExtension( scenePath));
       
	    //wait one frame for loading to complete
        yield return null;
       
	    var objects = GameObject.FindObjectsOfType(typeof(GameObject));
	    foreach (GameObject o in objects)
	    {
		    if (o.name.Contains("GLTF"))
		    {
			    GLTFComponent gltfcomponent = o.GetComponent<GLTFComponent>();
			    gltfcomponent.Load();
		    }
	    }

	    //wait one seconds for textures to load
        yield return null;

        Camera mainCamera = Camera.main;
		Debug.Assert(mainCamera!=null, "Make sure you have a main camera");
        RenderTexture rt = new RenderTexture(512, 512, 24);
        mainCamera.targetTexture = rt;
        Texture2D actualContents = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        mainCamera.Render();
        RenderTexture.active = rt;
        actualContents.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        byte[] pngActualfile = actualContents.EncodeToPNG();
        string outputpath = Path.GetDirectoryName(scenePath);
        string outputfullpath = GLTF_SCENARIO_OUTPUT_PATH + outputpath;
        Directory.CreateDirectory(outputfullpath);
        string filename = Path.GetFileNameWithoutExtension(scenePath);
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

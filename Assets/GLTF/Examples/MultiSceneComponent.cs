using UnityEngine;
using GLTF;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class MultiSceneComponent : MonoBehaviour {

	[Serializable]
	public struct Header {
		public string key;
		public string value;
	}

	public GameObject webServer;

	public int SceneIndex = 0;
	public string Url;
	public Header[] Headers;
	public Shader GLTFStandardShader;
	private GLTFLoader loader;

	IEnumerator Start ()
	{
		/* prevent race condition with web server */
		while (webServer == null) 
		{
			yield return null;
		}
		WebServerComponent component = webServer.GetComponent<WebServerComponent> ();
		while (!component.isRunning) 
		{
			yield return null;
		}

		Debug.Log("Hit spacebar to change the scene.");
		loader = new GLTFLoader(
				Url,
				gameObject.transform,
				Headers.ToDictionary(h => h.key, h => h.value)
			);
		loader.SetShaderForMaterialType(GLTFLoader.MaterialType.PbrMetallicRoughness, GLTFStandardShader);
		StartCoroutine(LoadScene(SceneIndex));
	}

	void Update ()
	{
		if (Input.GetKeyDown("space"))
		{
			SceneIndex = SceneIndex == 0 ? 1 : 0;
			Debug.LogFormat("Loading scene {0}", SceneIndex);
			StartCoroutine(LoadScene(SceneIndex));
		}
	}

	IEnumerator LoadScene(int SceneIndex)
	{
		foreach (Transform child in transform) {
			GameObject.Destroy(child.gameObject);
		}

		yield return loader.Load(SceneIndex);
	}


}

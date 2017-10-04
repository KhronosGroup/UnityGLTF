using System;
using System.Collections;
using UnityEngine;
using UnityGLTF.Loader;

namespace UnityGLTF.Examples
{
	public class MultiSceneComponent : MonoBehaviour
	{
		public int SceneIndex = 0;
		public string Url;
		public Shader GLTFStandardShader;
		private GLTFSceneImporter importer;

		void Start()
		{
			Debug.Log("Hit spacebar to change the scene.");

			Uri uri = new Uri(Url);
			var directoryPath = URIHelper.AbsoluteUriPath(uri);
			var loader = new WebRequestLoader(directoryPath);
			importer = new GLTFSceneImporter(
				URIHelper.GetFileFromUri(uri),
				loader
				);

			importer.SceneParent = gameObject.transform;
			importer.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.PbrMetallicRoughness, GLTFStandardShader);
			StartCoroutine(LoadScene(SceneIndex));
		}

		void Update()
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
			foreach (Transform child in transform)
			{
				GameObject.Destroy(child.gameObject);
			}
			
			yield return importer.LoadScene(SceneIndex);
		}
	}
}

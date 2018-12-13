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

		private GLTFSceneImporter _importer;
		private AsyncCoroutineHelper _asyncCoroutineHelper;
		private ILoader _loader;
		private string _fileName;

		void Start()
		{
			Debug.Log("Hit spacebar to change the scene.");
			_asyncCoroutineHelper = gameObject.AddComponent<AsyncCoroutineHelper>();
			Uri uri = new Uri(Url);
			var directoryPath = URIHelper.AbsoluteUriPath(uri);
			_loader = new WebRequestLoader(directoryPath);
			_fileName = URIHelper.GetFileFromUri(uri);

			LoadScene(SceneIndex);
		}

		void Update()
		{
			if (Input.GetKeyDown("space"))
			{
				SceneIndex = SceneIndex == 0 ? 1 : 0;
				Debug.LogFormat("Loading scene {0}", SceneIndex);
				LoadScene(SceneIndex);
			}
		}

		async void LoadScene(int SceneIndex)
		{
			foreach (Transform child in transform)
			{
				GameObject.Destroy(child.gameObject);
			}

			_importer = new GLTFSceneImporter(
				_fileName,
				_loader,
				_asyncCoroutineHelper
				);
			
			_importer.SceneParent = gameObject.transform;
			await _importer.LoadSceneAsync(SceneIndex);
		}
	}
}

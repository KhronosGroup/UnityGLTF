using System.Collections;
using UnityEngine;

namespace UnityGLTF.Examples
{
	public class MultiSceneComponent : MonoBehaviour
	{
		public int SceneIndex = 0;
		public string Url;
		private GLTFSceneImporter loader;

		void Start()
		{
			Debug.Log("Hit spacebar to change the scene.");
			loader = new GLTFSceneImporter(
				Url,
				gameObject.transform
			);
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

			yield return loader.Load(SceneIndex);
		}
	}
}

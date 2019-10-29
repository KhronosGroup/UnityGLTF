using System;
using System.Collections;
using UnityEngine;
using UnityGLTF.Loader;

namespace UnityGLTF.Tests.Integration
{
#if UNITY_5
    public class GLTFTestComponent : MonoBehaviour
	{
		public string Url;
		public bool Multithreaded = true;


		IEnumerator Start()
		{
			ILoader loader = new WebRequestLoader(URIHelper.GetDirectoryName(Url));
			var sceneImporter = new GLTFSceneImporter(
				URIHelper.GetFileFromUri(new Uri(Url)),
				loader
				);

			sceneImporter.SceneParent = gameObject.transform;
			sceneImporter.isMultithreaded = Multithreaded;
			yield return sceneImporter.LoadScene(-1);
			IntegrationTest.Pass();
		}
	}
#endif
}

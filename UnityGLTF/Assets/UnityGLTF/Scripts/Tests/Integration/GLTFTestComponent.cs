using System.Collections;
using UnityEngine;

namespace UnityGLTF.Tests.Integration
{
	public class GLTFTestComponent : MonoBehaviour
	{
		public string Url;
		public bool Multithreaded = true;

		IEnumerator Start()
		{
			var loader = new GLTFSceneImporter(
				Url,
				gameObject.transform
			);

			yield return loader.Load(-1, Multithreaded);
			IntegrationTest.Pass();
		}
	}
}

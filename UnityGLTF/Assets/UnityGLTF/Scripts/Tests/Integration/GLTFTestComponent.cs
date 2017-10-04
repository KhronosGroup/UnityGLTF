using System;
using System.Collections;
using UnityEngine;
using UnityGLTF.Loader;

namespace UnityGLTF.Tests.Integration
{
	public class GLTFTestComponent : MonoBehaviour
	{
		public string Url;
		public bool Multithreaded = true;

		public Shader GLTFStandard;
		private ILoader loader;

		IEnumerator Start()
		{
			Uri uri = new Uri(Url);
			string directoryPath = URIHelper.AbsoluteUriPath(uri);
			loader = new WebRequestLoader(directoryPath);
			var importer = new GLTFSceneImporter(
				URIHelper.GetFileFromUri(uri),
				loader
				);

			importer.SceneParent = gameObject.transform;
			importer.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.PbrMetallicRoughness, GLTFStandard);
			yield return importer.LoadScene(-1, Multithreaded);
			IntegrationTest.Pass();
		}
	}
}

using UnityEngine;
using UnityGLTF.Loader;

namespace UnityGLTF
{
	public abstract class ImporterFactory : ScriptableObject
	{
		public abstract GLTFSceneImporter CreateSceneImporter(string gltfFileName, ILoader externalDataLoader, AsyncCoroutineHelper asyncCoroutineHelper);
	}

	public class DefaultImporterFactory : ImporterFactory
	{
		public override GLTFSceneImporter CreateSceneImporter(string gltfFileName, ILoader externalDataLoader, AsyncCoroutineHelper asyncCoroutineHelper)
		{
			return new GLTFSceneImporter(gltfFileName, externalDataLoader, asyncCoroutineHelper);
		}
	}
}

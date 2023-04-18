using System.Collections.Generic;
using GLTF.Schema;

#if UNITY_EDITOR
using UnityEditor.AssetImporters;
#endif

namespace UnityGLTF.Plugins
{
	public class GLTFImportContext
	{
#if UNITY_EDITOR
		public readonly AssetImportContext AssetContext;
		public string FilePath => AssetContext.assetPath;
#endif

		public readonly IReadOnlyList<GltfImportPluginContext> Plugins;

		public GLTFSceneImporter SceneImporter;
		public GLTFRoot Root;

#if UNITY_EDITOR
		internal GLTFImportContext(AssetImportContext assetImportContext, IReadOnlyList<GltfImportPluginContext> plugins)
		{
			AssetContext = assetImportContext;
			Plugins = plugins;
		}
#endif
	}
}

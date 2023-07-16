using System.Collections.Generic;
using GLTF.Schema;
using UnityEditor;

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
		public readonly AssetImporter SourceImporter;
#endif

		public readonly IReadOnlyList<GltfImportPluginContext> Plugins;

		public GLTFSceneImporter SceneImporter;
		public GLTFRoot Root;

#if UNITY_EDITOR
		internal GLTFImportContext(AssetImportContext assetImportContext, IReadOnlyList<GltfImportPluginContext> plugins)
		{
			AssetContext = assetImportContext;
			Plugins = plugins;
			if (assetImportContext != null)
				SourceImporter = AssetImporter.GetAtPath(assetImportContext.assetPath);
		}
#endif
	}

	public interface IGLTFImportRemap
	{

	}
}

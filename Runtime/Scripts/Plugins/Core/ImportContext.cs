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

		public readonly List<GltfImportPluginContext> Plugins;

		public GLTFSceneImporter SceneImporter;
		public GLTFRoot Root => SceneImporter?.Root;

		private List<GltfImportPluginContext> InitializePlugins(GLTFSettings settings)
		{
			var plugins = new List<GltfImportPluginContext>();
			foreach (var plugin in settings.ImportPlugins)
			{
				if (plugin != null && plugin.Enabled)
				{
					var instance = plugin.CreateInstance(this);
					if (instance != null) plugins.Add(instance);
				}
			}

			return plugins;
		}
		
#if UNITY_EDITOR
		internal GLTFImportContext(AssetImportContext assetImportContext, GLTFSettings settings)
		{
			AssetContext = assetImportContext;
			if (assetImportContext != null)
				SourceImporter = AssetImporter.GetAtPath(assetImportContext.assetPath);
			
			Plugins = InitializePlugins(settings);
		}
#endif
		internal GLTFImportContext(GLTFSettings settings)
		{
			Plugins = InitializePlugins(settings);
		}

		public bool TryGetPlugin<T>(out GltfImportPluginContext o) where T: GltfImportPluginContext
		{
			foreach (var plugin in Plugins)
			{
				if (plugin is T t)
				{
					o = t;
					return true;
				}
			}

			o = null;
			return false;
		}
	}
}

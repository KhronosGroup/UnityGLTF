using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF
{
	/// <summary>
	/// What a GLTFImporter found and created during the import, for the importer inspector and editor tools.
	/// It's a hidden sub-asset of the imported glTF, so it's part of the import result (the artifact) and not of the
	/// import settings (the .meta file).
	///
	/// Import results must not be stored in the importer: its fields are serialized into the .meta file, and
	/// references to the asset's own sub-assets there made Unity load the asset's artifact while that artifact was
	/// being replaced, which failed with "Opening file VirtualArtifacts/Primary/...: file not found" (#919).
	/// </summary>
	internal class GLTFImportInfo : ScriptableObject
	{
		internal const string Identifier = "import info";

		/// <summary>The glTF file contains a scene, materials, textures, animations.</summary>
		[SerializeField] internal bool hasSceneData;
		[SerializeField] internal bool hasMaterialData;
		[SerializeField] internal bool hasTextureData;
		[SerializeField] internal bool hasAnimationData;

		/// <summary>The imported materials and textures, in import order. Used for remapping.</summary>
		[SerializeField] internal Material[] materials = new Material[0];
		[SerializeField] internal Texture[] textures = new Texture[0];

		/// <summary>Color space and normal map usage of the textures, to check the settings of external textures.</summary>
		[NonReorderable] [SerializeField] internal List<GLTFImporter.TextureInfo> textureInfos = new List<GLTFImporter.TextureInfo>();

		[NonReorderable] [SerializeField] internal List<GLTFImporter.ExtensionInfo> extensions = new List<GLTFImporter.ExtensionInfo>();
		[SerializeField] internal DeduplicatedStatistics deduplicatedStatistics;
		[SerializeField] internal string gltfAsset;
		[SerializeField] internal string mainAssetIdentifier;

		internal static GLTFImportInfo Create()
		{
			var info = CreateInstance<GLTFImportInfo>();
			info.name = "Import Info";
			// not shown in the Project window, and not needed in builds
			info.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;
			return info;
		}

		/// <summary>
		/// The import info of the glTF at <paramref name="assetPath"/>, or null when the asset wasn't imported (yet),
		/// the import failed or the asset was imported with an older version of the importer.
		/// Don't call this while the asset itself is being imported.
		/// </summary>
		internal static GLTFImportInfo Load(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath)) return null;
			return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<GLTFImportInfo>().FirstOrDefault();
		}
	}
}

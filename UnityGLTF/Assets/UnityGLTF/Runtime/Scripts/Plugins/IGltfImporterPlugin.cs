using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Plugins
{
	public abstract class GltfImportPlugin : ScriptableObject
	{
		internal bool Expanded = true;
		public abstract string DisplayName { get; }
		public bool Enabled { get; set; } = true;
		public abstract void OnGUI();
		/// <summary>
		/// Return the Plugin Instance that receives the import callbacks
		/// </summary>
		public abstract GltfImportPluginContext CreateInstance(GLTFImportContext context);
	}

	/// <summary>
	/// Instances are created during import once, they don't have to worry about clearing state
	/// </summary>
	public abstract class GltfImportPluginContext
	{
		/// <summary>
		/// Called before import starts
		/// </summary>
		public virtual void OnBeforeImport()
		{

		}

		public virtual void OnBeforeImportRoot()
		{
		}

		/// <summary>
		/// Called when the GltfRoot has been deserialized
		/// </summary>
		public virtual void OnAfterImportRoot(GLTFRoot gltfRoot)
		{

		}

		public virtual void OnBeforeImportScene(GLTFScene scene)
		{
		}

		public virtual void OnAfterImportNode(Node node, int nodeIndex, GameObject nodeObject)
		{
		}

		public virtual void OnAfterImportMaterial(GLTFMaterial material, int materialIndex, Material materialObject)
		{
		}

		public virtual void OnAfterImportTexture(GLTFTexture texture, int textureIndex, Texture textureObject)
		{
		}

		public virtual void OnAfterImportScene(GLTFScene scene, int sceneIndex, GameObject sceneObject)
		{
		}

		public virtual void OnAfterImport()
		{

		}
	}
}

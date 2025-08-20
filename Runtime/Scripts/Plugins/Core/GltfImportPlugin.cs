using System.Collections.Generic;
using System.Threading.Tasks;
using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Plugins
{
	public abstract class GLTFImportPlugin : GLTFPlugin
	{
		/// <summary>
		/// Return the Plugin Instance that receives the import callbacks
		/// </summary>
		public abstract GLTFImportPluginContext CreateInstance(GLTFImportContext context);
	}

	/// <summary>
	/// Instances are created during import once, they don't have to worry about clearing state
	/// </summary>
	public abstract class GLTFImportPluginContext
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously.
		// These methods can be overridden if an importer needs to call async functions.
		// These are provided for backwards compatibility and by default, will call the
		// synchronous version.

		public virtual async Task OnBeforeImportAsync()
		{
			OnBeforeImport();
		}

		public virtual async Task OnBeforeImportRootAsync()
		{
			OnBeforeImportRoot();
		}

		public virtual async Task OnAfterImportRootAsync(GLTFRoot gltfRoot)
		{
			OnAfterImportRoot(gltfRoot);
		}

		public virtual async Task OnBeforeImportSceneAsync(GLTFScene scene)
		{
			OnBeforeImportScene(scene);
		}

		public virtual async Task OnAfterImportNodeAsync(Node node, int nodeIndex, GameObject nodeObject)
		{
			OnAfterImportNode(node, nodeIndex, nodeObject);
		}

		public virtual async Task OnAfterImportMaterialAsync(GLTFMaterial material, int materialIndex, Material materialObject)
		{
			OnAfterImportMaterial(material, materialIndex, materialObject);
		}

		public virtual async Task OnAfterImportTextureAsync(GLTFTexture texture, int textureIndex, Texture textureObject)
		{
			OnAfterImportTexture(texture, textureIndex, textureObject);
		}

		public virtual async Task OnAfterImportSceneAsync(GLTFScene scene, int sceneIndex, GameObject sceneObject)
		{
			OnAfterImportScene(scene, sceneIndex, sceneObject);
		}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously.
	}
}

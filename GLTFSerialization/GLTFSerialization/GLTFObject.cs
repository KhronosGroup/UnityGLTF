using GLTF.Schema;

namespace GLTF
{
	/// <summary>
	/// Represents a glTF file (specifically not GLB)
	/// </summary>
	public class GLTFObject : IGLTFObject
	{
		public GLTFObject(GLTFRoot root)
        {
            Root = root;
        }

		/// <summary>
		/// Parse glTF root into
		/// </summary>
		public GLTFRoot Root { get; internal set; }
	}
}

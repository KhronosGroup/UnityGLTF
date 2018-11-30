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
		/// Parsed glTF into a strongly typed C# object
		/// </summary>
		public GLTFRoot Root { get; internal set; }
	}
}

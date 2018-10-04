using GLTF.Schema;

namespace GLTF
{
	public class AttributeAccessor
	{
		public AccessorId AccessorId { get; set; }
		public NumericArray AccessorContent { get; set; }
		public System.IO.Stream Stream { get; set; }
		public uint Offset { get; set; }

		public AttributeAccessor()
		{
			AccessorContent = new NumericArray();
		}
	}
}

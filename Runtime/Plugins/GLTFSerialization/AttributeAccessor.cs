using GLTF.Schema;
using Unity.Collections;

namespace GLTF
{
	public class AttributeAccessor
	{
		public AccessorId AccessorId { get; set; }
		public NumericArray AccessorContent { get; set; }
		
		public NativeArray<byte> bufferData { get; set; }
		public long Offset { get; set; }

		public AttributeAccessor()
		{
			AccessorContent = new NumericArray();
		}
	}
}

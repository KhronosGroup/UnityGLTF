using System;

namespace GLTF
{
	[Serializable()]
	public class GLTFHeaderInvalidException : Exception
	{
		public GLTFHeaderInvalidException() : base() { }
		public GLTFHeaderInvalidException(string message) : base(message) { }
		public GLTFHeaderInvalidException(string message, Exception inner) : base(message, inner) { }

		protected GLTFHeaderInvalidException(System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
		{ }
	}
}

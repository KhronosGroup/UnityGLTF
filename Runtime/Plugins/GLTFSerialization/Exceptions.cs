using System;

namespace GLTF
{
	public class GLTFHeaderInvalidException : Exception
	{
		public GLTFHeaderInvalidException() : base() { }
		public GLTFHeaderInvalidException(string message) : base(message) { }
		public GLTFHeaderInvalidException(string message, Exception inner) : base(message, inner) { }
#if !WINDOWS_UWP
		protected GLTFHeaderInvalidException(System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
		{ }
#endif
	}
	
	public class GLTFParseException : Exception
	{
		public GLTFParseException() : base() { }
		public GLTFParseException(string message) : base(message) { }
		public GLTFParseException(string message, Exception inner) : base(message, inner) { }
#if !WINDOWS_UWP
		protected GLTFParseException(System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
		{ }
#endif
	}

	/// <summary>
	/// GLTFLoad exceptions occur during runtime errors through use of the GLTFSceneImporter
	/// </summary>
	public class GLTFLoadException : Exception
	{
		public GLTFLoadException() : base() { }
		public GLTFLoadException(string message) : base(message) { }
		public GLTFLoadException(string message, Exception inner) : base(message, inner) { }
#if !WINDOWS_UWP
		protected GLTFLoadException(System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
		{ }
#endif
	}
}

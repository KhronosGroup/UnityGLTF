using System;
using UnityEngine.Networking;

namespace GLTF
{
	[Serializable()]
	public class WebRequestException : Exception
	{
		public WebRequestException() : base() { }
		public WebRequestException(string message) : base(message) { }
		public WebRequestException(string message, Exception inner) : base(message, inner) { }
		public WebRequestException(UnityWebRequest www) : base(string.Format("Error: {0} when requesting: {1}", www.responseCode, www.url)) { }

		protected WebRequestException(System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
		{ }
	}

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

	[Serializable()]
	public class ShaderNotFoundException : Exception
	{
		public ShaderNotFoundException() : base() { }
		public ShaderNotFoundException(string message) : base(message) { }
		public ShaderNotFoundException(string message, Exception inner) : base(message, inner) { }

		protected ShaderNotFoundException(System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
		{ }
	}
}

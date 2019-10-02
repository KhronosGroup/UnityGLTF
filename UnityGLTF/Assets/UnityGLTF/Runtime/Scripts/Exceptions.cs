using System;
using UnityEngine.Networking;

namespace UnityGLTF 
{
	[Serializable()]
	public class WebRequestException : Exception 
	{
		public WebRequestException() : base() { }
		public WebRequestException(string message) : base(message) { }
		public WebRequestException(string message, Exception inner) : base(message, inner) { }
		public WebRequestException(UnityWebRequest www) : base(string.Format("Error: {0} when requesting: {1}", www.responseCode, www.url)) { }
#if !WINDOWS_UWP
		protected WebRequestException(System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context) { }
#endif
	}

	[Serializable()]
	public class ShaderNotFoundException : Exception
	{
		public ShaderNotFoundException() : base() { }
		public ShaderNotFoundException(string message) : base(message) { }
		public ShaderNotFoundException(string message, Exception inner) : base(message, inner) { }
#if !WINDOWS_UWP
		protected ShaderNotFoundException(System.Runtime.Serialization.SerializationInfo info,
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
	}
}

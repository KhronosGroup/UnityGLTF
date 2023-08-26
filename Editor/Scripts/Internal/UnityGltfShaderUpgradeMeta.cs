using UnityEngine;

namespace UnityGLTF
{
	public interface IUnityGltfShaderUpgradeMeta
	{
		Shader SourceShader { get; }
		bool IsUnlit { get; }
		bool IsTransparent { get; }
		bool IsDoublesided { get; }
	}

	public class UnityGltfShaderUpgradeMeta : ScriptableObject
	{
		public Shader sourceShader;
		public bool isTransparent = false;
		public bool isDoublesided = false;

		internal string GenerateNameString()
		{
			var isUnlit = sourceShader && sourceShader.name.Contains("Unlit");
			return (isTransparent ? "_tr" : "") + (isDoublesided ? "_ds" : "") + (isUnlit ? "_unlit" : "");
		}
	}
}

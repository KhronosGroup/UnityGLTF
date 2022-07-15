using UnityEngine;

namespace UnityGLTF
{
	public class UnlitGraphMap : BaseGraphMap, IUnlitUniformMap
	{
		public UnlitGraphMap() : this("UnityGLTF/UnlitGraph") {}

		protected UnlitGraphMap(string shaderName)
		{
			var s = Shader.Find(shaderName);
			if (s == null)
			{
				throw new ShaderNotFoundException(shaderName + " not found. Did you forget to add it to the build?");
			}
			_material = new Material(s);
		}

		protected UnlitGraphMap(Material mat)
		{
			_material = mat;
		}

		public override IUniformMap Clone()
		{
			var clone = new UnlitGraphMap(new Material(_material));
			clone.Material.shaderKeywords = _material.shaderKeywords;
			return clone;
		}
	}
}

using UnityEngine;

namespace UnityGLTF
{
	public class UnlitGraphMap : BaseGraphMap, IUnlitUniformMap
	{
		private const string UnlitGraphGuid = "59541e6caf586ca4f96ccf48a4813a51";
		public UnlitGraphMap() : this("UnityGLTF/UnlitGraph") {}

		protected UnlitGraphMap(string shaderName) : base(shaderName, UnlitGraphGuid) { }

		protected UnlitGraphMap(Material mat) : base(mat) { }

		public override IUniformMap Clone()
		{
			var clone = new UnlitGraphMap(new Material(_material));
			clone.Material.shaderKeywords = _material.shaderKeywords;
			return clone;
		}
	}
}

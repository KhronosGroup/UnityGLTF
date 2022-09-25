using UnityEngine;

namespace UnityGLTF
{
	public class UnlitGraphMap : BaseGraphMap, IUnlitUniformMap
	{
		private const string UnlitGraphGuid = "59541e6caf586ca4f96ccf48a4813a51";
		public UnlitGraphMap() : this("UnityGLTF/UnlitGraph") {}

#if !UNITY_2021_1_OR_NEWER
		private const string UnlitGraphTransparentGuid = "83f2caca07949794fb997734c4b0520f";
		public UnlitGraphMap(bool transparent) : base(transparent ? "UnityGLTF/UnlitGraph-Transparent" : "UnityGLTF/UnlitGraph", transparent ? UnlitGraphTransparentGuid : UnlitGraphGuid) { }
#endif

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

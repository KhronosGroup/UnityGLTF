using UnityEngine;

namespace UnityGLTF
{
	public class UnlitGraphMap : BaseGraphMap, IUnlitUniformMap
	{
		internal const string UnlitGraphGuid = "59541e6caf586ca4f96ccf48a4813a51";
		public UnlitGraphMap() : this("UnityGLTF/UnlitGraph") {}

#if !UNITY_2021_1_OR_NEWER
		private const string UnlitGraphTransparentGuid = "83f2caca07949794fb997734c4b0520f";
		private const string UnlitGraphTransparentDoubleGuid = "8a8841b4fb2f63644896f4e2b36bc06d";
		private const string UnlitGraphDoubleGuid = "33ee70a7f505ddb4e80d235c3d70766d";

		public UnlitGraphMap(bool transparent, bool doubleSided) : base(
			"UnityGLTF/UnlitGraph" + (transparent ? "-Transparent" : "") + (doubleSided ? "-Double" : ""),
			(transparent && doubleSided ? UnlitGraphTransparentDoubleGuid : transparent ? UnlitGraphTransparentGuid : doubleSided ? UnlitGraphDoubleGuid : UnlitGraphGuid)) { }
#endif

		protected UnlitGraphMap(string shaderName) : base(shaderName, UnlitGraphGuid) { }

		public UnlitGraphMap(Material mat) : base(mat) { }

		public override IUniformMap Clone()
		{
			var clone = new UnlitGraphMap(new Material(_material));
			clone.Material.shaderKeywords = _material.shaderKeywords;
			return clone;
		}
	}
}

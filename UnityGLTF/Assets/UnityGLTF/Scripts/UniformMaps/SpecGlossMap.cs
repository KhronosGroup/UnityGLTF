using System;
using UnityEngine;

namespace UnityGLTF
{
	public class SpecGlossMap : SpecGloss2StandardMap
	{
		public SpecGlossMap(int MaxLOD = 1000) : base("GLTF/PbrSpecularGlossiness", MaxLOD) { }
		public SpecGlossMap(string shaderName, int MaxLOD = 1000) : base(shaderName, MaxLOD) { }
		protected SpecGlossMap(Material m, int MaxLOD = 1000) : base(m, MaxLOD) { }

		public override int NormalTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public override int OcclusionTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public override int EmissiveTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public override int DiffuseTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public override int SpecularGlossinessTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public override IUniformMap Clone()
		{
			var copy = new SpecGlossMap(new Material(_material));
			base.Copy(copy);
			return copy;
		}
	}
}

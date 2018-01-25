using System;
using UnityEngine;
using AlphaMode = GLTF.Schema.AlphaMode;
using CullMode = UnityEngine.Rendering.CullMode;

namespace UnityGLTF
{
	class SpecGlossMap : SpecGloss2StandardMap
	{
		public SpecGlossMap(int MaxLOD = 1000)
		{
			var s = Shader.Find("GLTF/PbrSpecularGlossiness");
			s.maximumLOD = MaxLOD;
			_material = new Material(s);
		}

		public SpecGlossMap(Material m, int MaxLOD = 1000) : base(m, MaxLOD) { }

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

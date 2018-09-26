using System;
using UnityEngine;
using AlphaMode = GLTF.Schema.AlphaMode;
using CullMode = UnityEngine.Rendering.CullMode;

namespace UnityGLTF
{
	class MetalRoughMap : MetalRough2StandardMap
	{
		public MetalRoughMap(int MaxLOD = 1000) : base("GLTF/PbrMetallicRoughness", MaxLOD) { }
		public MetalRoughMap(string shaderName, int MaxLOD = 1000) : base(shaderName, MaxLOD) { }
		protected MetalRoughMap(Material m, int MaxLOD = 1000) : base(m, MaxLOD) { }

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

		public override int BaseColorTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public override Texture MetallicRoughnessTexture
		{
			get { return _material.GetTexture("_MetallicGlossMap"); }
			set
			{
				_material.SetTexture("_MetallicGlossMap", value);
				_material.EnableKeyword("_METALLICGLOSSMAP");
			}
		}

		public override int MetallicRoughnessTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public override IUniformMap Clone()
		{
			var copy = new MetalRoughMap(new Material(_material));
			base.Copy(copy);
			return copy;
		}
	}
}

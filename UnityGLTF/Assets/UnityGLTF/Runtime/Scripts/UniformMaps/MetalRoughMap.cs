using System;
using UnityEngine;
using AlphaMode = GLTF.Schema.AlphaMode;
using CullMode = UnityEngine.Rendering.CullMode;

namespace UnityGLTF
{
	public class MetalRoughMap : MetalRough2StandardMap
	{
		private Vector2 metalRoughOffset = new Vector2(0, 0);

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

		public override Vector2 MetallicRoughnessXOffset
		{
			get { return metalRoughOffset; }
			set
			{
				metalRoughOffset = value;
				var unitySpaceVec = new Vector2(metalRoughOffset.x, 1 - MetallicRoughnessXScale.y - metalRoughOffset.y);
				_material.SetTextureOffset("_MetallicGlossMap", unitySpaceVec);
			}
		}

		public override double MetallicRoughnessXRotation
		{
			get { return 0; }
			set { return; }
		}

		public override Vector2 MetallicRoughnessXScale
		{
			get { return _material.GetTextureScale("_MetallicGlossMap"); }
			set
			{
				_material.SetTextureScale("_MetallicGlossMap", value);
				MetallicRoughnessXOffset = metalRoughOffset;
			}
		}

		public override int MetallicRoughnessXTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public override double RoughnessFactor
		{
			get { return _material.GetFloat("_Glossiness"); }
			set { _material.SetFloat("_Glossiness", (float)value); }
		}

		public override IUniformMap Clone()
		{
			var copy = new MetalRoughMap(new Material(_material));
			base.Copy(copy);
			return copy;
		}
	}
}

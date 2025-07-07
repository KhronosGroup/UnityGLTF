using UnityEngine;

namespace UnityGLTF
{
	public class MetalRoughMap : MetalRough2StandardMap
	{
		private Vector2 metalRoughOffset = new Vector2(0, 0);

		public MetalRoughMap(string shaderName, int MaxLOD = 1000) : base(shaderName, null, MaxLOD) { }
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
				_material.SetTextureOffset("_MetallicGlossMap", value);
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

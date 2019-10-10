using UnityEngine;

namespace UnityGLTF
{
	public class MetalRough2StandardMap : StandardMap, IMetalRoughUniformMap
	{
		private Vector2 baseColorOffset = new Vector2(0, 0);

		public MetalRough2StandardMap(int MaxLOD = 1000) : base("Standard", MaxLOD) { }
		protected MetalRough2StandardMap(string shaderName, int MaxLOD = 1000) : base(shaderName, MaxLOD) { }
		protected MetalRough2StandardMap(Material m, int MaxLOD = 1000) : base(m, MaxLOD) { }

		public virtual Texture BaseColorTexture
		{
			get { return _material.GetTexture("_MainTex"); }
			set { _material.SetTexture("_MainTex", value); }
		}

		// not implemented by the Standard shader
		public virtual int BaseColorTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector2 BaseColorXOffset
		{
			get { return baseColorOffset; }
			set {
				baseColorOffset = value;
				var unitySpaceVec = new Vector2(baseColorOffset.x, 1 - BaseColorXScale.y - baseColorOffset.y);
				_material.SetTextureOffset("_MainTex", unitySpaceVec);
			}
		}

		public virtual double BaseColorXRotation
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector2 BaseColorXScale
		{
			get { return _material.GetTextureScale("_MainTex"); }
			set {
				_material.SetTextureScale("_MainTex", value);
				BaseColorXOffset = baseColorOffset;
			}
		}

		public virtual int BaseColorXTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public virtual Color BaseColorFactor
		{
			get { return _material.GetColor("_Color"); }
			set { _material.SetColor("_Color", value); }
		}

		public virtual Texture MetallicRoughnessTexture
		{
			get { return null; }
			set
			{
				// cap metalness at 0.5 to compensate for lack of texture
				MetallicFactor = Mathf.Min(0.5f, (float)MetallicFactor);
			}
		}

		// not implemented by the Standard shader
		public virtual int MetallicRoughnessTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector2 MetallicRoughnessXOffset
		{
			get { return new Vector2(0, 0); }
			set { return; }
		}

		public virtual double MetallicRoughnessXRotation
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector2 MetallicRoughnessXScale
		{
			get { return new Vector2(1, 1); }
			set { return; }
		}

		public virtual int MetallicRoughnessXTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public virtual double MetallicFactor
		{
			get { return _material.GetFloat("_Metallic"); }
			set { _material.SetFloat("_Metallic", (float)value); }
		}

		// not supported by the Standard shader
		public virtual double RoughnessFactor
		{
			get { return 0.5; }
			set { return; }
		}

		public override IUniformMap Clone()
		{
			var copy = new MetalRough2StandardMap(new Material(_material));
			base.Copy(copy);
			return copy;
		}
	}
}

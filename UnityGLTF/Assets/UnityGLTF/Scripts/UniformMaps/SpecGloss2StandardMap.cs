using UnityEngine;

namespace UnityGLTF
{
	class SpecGloss2StandardMap : StandardMap, ISpecGlossUniformMap
	{
		public SpecGloss2StandardMap(int MaxLOD = 1000) : base("Standard (Specular setup)", MaxLOD) { }
		protected SpecGloss2StandardMap(string shaderName, int MaxLOD = 1000) : base(shaderName, MaxLOD) { }
		protected SpecGloss2StandardMap(Material m, int MaxLOD = 1000) : base(m, MaxLOD) { }

		public virtual Texture DiffuseTexture
		{
			get { return _material.GetTexture("_MainTex"); }
			set { _material.SetTexture("_MainTex", value); }
		}

		// not implemented by the Standard shader
		public virtual int DiffuseTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public virtual Color DiffuseFactor
		{
			get { return _material.GetColor("_Color"); }
			set { _material.SetColor("_Color", value); }
		}

		public virtual Texture SpecularGlossinessTexture
		{
			get { return _material.GetTexture("_SpecGlossMap"); }
			set
			{
				_material.SetTexture("_SpecGlossMap", value);
				_material.SetFloat("_SmoothnessTextureChannel", 0);
				_material.EnableKeyword("_SPECGLOSSMAP");
			}
		}

		// not implemented by the Standard shader
		public virtual int SpecularGlossinessTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector3 SpecularFactor
		{
			get { return _material.GetVector("_SpecColor"); }
			set { _material.SetVector("_SpecColor", value); }
		}

		public virtual double GlossinessFactor
		{
			get { return _material.GetFloat("_GlossMapScale"); }
			set
			{
				_material.SetFloat("_GlossMapScale", (float)value);
				_material.SetFloat("_Glossiness", (float)value);
			}
		}

		public override IUniformMap Clone()
		{
			var copy = new SpecGloss2StandardMap(new Material(_material));
			base.Copy(copy);
			return copy;
		}
	}
}

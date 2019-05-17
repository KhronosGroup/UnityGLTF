using UnityEngine;

namespace UnityGLTF
{
	public class SpecGloss2StandardMap : StandardMap, ISpecGlossUniformMap
	{
		private Vector2 diffuseOffset = new Vector2(0, 0);
		private Vector2 specGlossOffset = new Vector2(0, 0);

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

		public virtual Vector2 DiffuseXOffset
		{
			get { return diffuseOffset; }
			set
			{
				diffuseOffset = value;
				var unitySpaceVec = new Vector2(diffuseOffset.x, 1 - DiffuseXScale.y - diffuseOffset.y);
				_material.SetTextureOffset("_MainTex", unitySpaceVec);
			}
		}

		public virtual double DiffuseXRotation
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector2 DiffuseXScale
		{
			get { return _material.GetTextureScale("_MainTex"); }
			set
			{
				_material.SetTextureScale("_MainTex", value);
				DiffuseXOffset = diffuseOffset;
			}
		}

		public virtual int DiffuseXTexCoord
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

		public virtual Vector2 SpecularGlossinessXOffset
		{
			get { return specGlossOffset; }
			set
			{
				specGlossOffset = value;
				var unitySpaceVec = new Vector2(specGlossOffset.x, 1 - SpecularGlossinessXScale.y - specGlossOffset.y);
				_material.SetTextureOffset("_SpecGlossMap", unitySpaceVec);
			}
		}

		public virtual double SpecularGlossinessXRotation
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector2 SpecularGlossinessXScale
		{
			get { return _material.GetTextureScale("_SpecGlossMap"); }
			set {
				_material.SetTextureScale("_SpecGlossMap", value);
				SpecularGlossinessXOffset = specGlossOffset;
			}
		}

		public virtual int SpecularGlossinessXTexCoord
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

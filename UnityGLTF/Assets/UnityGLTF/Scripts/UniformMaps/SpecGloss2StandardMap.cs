using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace UnityGLTF
{
	class SpecGloss2StandardMap : StandardMap, ISpecGlossUniformMap
	{
		public SpecGloss2StandardMap(int MaxLOD = 300) : base(Shader.Find("Standard (Specular setup)"), MaxLOD) { }
		public SpecGloss2StandardMap(Material m) : base(m) { }

		public Texture DiffuseTexture
		{
			get { return _material.GetTexture("_MainTex"); }
			set { _material.SetTexture("_MainTex", value); }
		}

		// not implemented by the Standard shader
		public int DiffuseTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public Color DiffuseFactor
		{
			get { return _material.GetColor("_Color"); }
			set { _material.SetColor("_Color", value); }
		}

		public Texture SpecularGlossinessTexture
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
		public int SpecularGlossinessTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public Vector3 SpecularFactor
		{
			get { return _material.GetVector("_SpecColor"); }
			set { _material.SetVector("_SpecColor", value); }
		}

		public double GlossinessFactor
		{
			get { return _material.GetFloat("_GlossMapScale"); }
			set
			{
				_material.SetFloat("_GlossMapScale", (float) value);
				_material.SetFloat("_Glossiness", (float) value);
			}
		}

		public new IUniformMap Clone()
		{
			var copy = new SpecGloss2StandardMap(new Material(_material));
			base.Copy(copy);
			return copy;
		}
	}
}

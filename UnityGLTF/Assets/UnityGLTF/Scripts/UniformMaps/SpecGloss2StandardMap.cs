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

		public Texture DiffuseTexture
		{
			get { return _material.GetTexture("_MainTex"); }
			set { _material.SetTexture("_MainTex", value); }
		}

		public Color DiffuseFactor
		{
			get { return _material.GetColor("_Color"); }
			set { _material.SetColor("_Color", value); }
		}

		public Texture SpecularGlossinessTexture
		{
			get { return _material.GetTexture("_SpecGlossMap"); }
			set { _material.SetTexture("_SpecGlossMap", value); }
		}

		public Color SpecularFactor
		{
			get { return _material.GetColor("_SpecColor"); }
			set { _material.SetColor("_SpecColor", value); }
		}

		public double GlossinessFactor
		{
			get { return _material.GetFloat("_GlossMapScale"); }
			set { _material.SetFloat("_GlossMapScale", (float) value); }
		}
	}
}

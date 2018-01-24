using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityGLTF
{
	class MetalRough2StandardMap : StandardMap, IMetalRoughUniformMap
	{
		public MetalRough2StandardMap(int MaxLOD = 300) : base(Shader.Find("GLTF/GLTFStandard"), MaxLOD) { }

		public Texture BaseColorTexture
		{
			get { return _material.GetTexture("_MainTex"); }
			set { _material.SetTexture("_MainTex", value); }
		}

		public Color BaseColorFactor
		{
			get { return _material.GetColor("_Color"); }
			set { _material.SetColor("_Color", value); }
		}

		public Texture MetallicRoughnessTexture
		{
			get { return _material.GetTexture("_MetallicRoughnessMap"); }
			set { _material.SetTexture("_MetallicRoughnessMap", value); }
		}

		public double MetallicFactor
		{
			get { return _material.GetFloat("_Metallic"); }
			set { _material.SetFloat("_Metallic", (float) value); }
		}

		public double RoughnessFactor
		{
			get { return _material.GetFloat("_Roughness"); }
			set { _material.SetFloat("_Roughness", (float) value); }
		}
	}
}

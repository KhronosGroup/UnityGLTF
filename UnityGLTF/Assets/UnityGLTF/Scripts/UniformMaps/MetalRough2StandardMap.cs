using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UnityGLTF
{
	class MetalRough2StandardMap : StandardMap, IMetalRoughUniformMap
	{
		public MetalRough2StandardMap(int MaxLOD = 300) : base(Shader.Find("Standard"), MaxLOD) { }
		public MetalRough2StandardMap(Material m) : base(m) { }

		public Texture BaseColorTexture
		{
			get { return _material.GetTexture("_MainTex"); }
			set { _material.SetTexture("_MainTex", value); }
		}

		// not implemented by the Standard shader
		public int BaseColorTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public Color BaseColorFactor
		{
			get { return _material.GetColor("_Color"); }
			set { _material.SetColor("_Color", value); }
		}

		public new Texture OcclusionTexture
		{
			get { return _material.GetTexture("_OcclusionMap"); }
			set { _material.SetTexture("_OcclusionMap", value); }
		}

		// standard shader looks at the wrong channels here
		public Texture MetallicRoughnessTexture
		{
			/*get { return _material.GetTexture("_MetallicRoughnessMap"); }
			set
			{
				_material.SetTexture("_MetallicRoughnessMap", value);
				if (value == OcclusionTexture)
				{
					_material.EnableKeyword("OCC_METAL_ROUGH_ON");
					_material.SetTexture("_OcclusionMap", null);
				}
				else
				{
					_material.DisableKeyword("OCC_METAL_ROUGH_ON");
				}
			}*/
			get { return null; }
			set
			{
				// cap metalness at 0.5 to compensate for lack of texture
				MetallicFactor = Mathf.Min(0.5f, (float)MetallicFactor);
			}
		}

		// not implemented by the Standard shader
		public int MetallicRoughnessTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public double MetallicFactor
		{
			get { return _material.GetFloat("_Metallic"); }
			set { _material.SetFloat("_Metallic", (float) value); }
		}

		// not supported by the Standard shader
		public double RoughnessFactor
		{
			get { return 0.5; }
			set { return; }
		}

		public new IUniformMap Clone()
		{
			var copy = new MetalRough2StandardMap(new Material(_material));
			base.Copy(copy);
			return copy;
		}
	}
}

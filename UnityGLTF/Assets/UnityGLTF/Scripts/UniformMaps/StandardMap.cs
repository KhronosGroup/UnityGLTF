using GLTF.Schema;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;
using Texture = UnityEngine.Texture;

namespace UnityGLTF
{
	class StandardMap : IUniformMap
	{
		protected Material _material;
		private AlphaMode _alphaMode = AlphaMode.OPAQUE;
		private double _alphaCutoff = 0.5;

		protected StandardMap(Shader s, int MaxLOD = 300)
		{
			s.maximumLOD = MaxLOD;
			_material = new Material(s);
		}

		protected StandardMap(Material mat)
		{
			_material = mat;
		}

		public Material Material { get { return _material; } }

		public Texture NormalTexture
		{
			get { return _material.GetTexture("_BumpMap"); }
			set
			{
				_material.SetTexture("_BumpMap", value);
				_material.EnableKeyword("_NORMALMAP");
			}
		}

		// not implemented by the Standard shader
		public int NormalTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public double NormalTexScale
		{
			get { return _material.GetFloat("_BumpScale"); }
			set { _material.SetFloat("_BumpScale", (float) value); }
		}

		public Texture OcclusionTexture
		{
			get { return _material.GetTexture("_OcclusionMap"); }
			set { _material.SetTexture("_OcclusionMap", value); }
		}

		// not implemented by the Standard shader
		public int OcclusionTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public double OcclusionTexStrength
		{
			get { return _material.GetFloat("_OcclusionStrength"); }
			set { _material.SetFloat("_OcclusionStrength", (float) value); }
		}

		public Texture EmissiveTexture
		{
			get { return _material.GetTexture("_EmissionMap"); }
			set
			{
				_material.SetTexture("_EmissionMap", value);
				_material.EnableKeyword("EMISSION_MAP_ON");
				_material.EnableKeyword("_EMISSION");
			}
		}

		// not implemented by the Standard shader
		public int EmissiveTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public Color EmissiveFactor
		{
			get { return _material.GetColor("_EmissionColor"); }
			set { _material.SetColor("_EmissionColor", value); }
		}

		public AlphaMode AlphaMode
		{
			get { return _alphaMode; }
			set
			{
				if (value == AlphaMode.MASK)
				{
					_material.SetOverrideTag("RenderType", "TransparentCutout");
					_material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					_material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					_material.SetInt("_ZWrite", 1);
					_material.EnableKeyword("_ALPHATEST_ON");
					_material.DisableKeyword("_ALPHABLEND_ON");
					_material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					_material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
					_material.SetFloat("_Cutoff", (float)_alphaCutoff);
				}
				else if (value == AlphaMode.BLEND)
				{
					_material.SetOverrideTag("RenderType", "Transparent");
					_material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					_material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					_material.SetInt("_ZWrite", 0);
					_material.DisableKeyword("_ALPHATEST_ON");
					_material.EnableKeyword("_ALPHABLEND_ON");
					_material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					_material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
				}
				else
				{
					_material.SetOverrideTag("RenderType", "Opaque");
					_material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					_material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					_material.SetInt("_ZWrite", 1);
					_material.DisableKeyword("_ALPHATEST_ON");
					_material.DisableKeyword("_ALPHABLEND_ON");
					_material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					_material.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Geometry;
				}

				_alphaMode = value;
			}
		}

		public double AlphaCutoff
		{
			get { return _alphaCutoff; }
			set
			{
				if (_alphaMode == AlphaMode.MASK)
					_material.SetFloat("_Cutoff", (float) value);
				_alphaCutoff = value;
			}
		}

		public bool DoubleSided
		{
			get { return _material.GetInt("_Cull") == (int) CullMode.Off; }
			set
			{
				if(value)
					_material.SetInt("_Cull", (int) CullMode.Off);
				else
					_material.SetInt("_Cull", (int) CullMode.Back);
			}
		}

		public bool VertexColorsEnabled
		{
			get { return _material.IsKeywordEnabled("VERTEX_COLOR_ON"); }
			set
			{
				if (value)
					_material.EnableKeyword("VERTEX_COLOR_ON");
				else
					_material.DisableKeyword("VERTEX_COLOR_ON");
			}
		}

		public IUniformMap Clone()
		{
			var ret = new StandardMap(new Material(_material));
			ret._alphaMode = _alphaMode;
			ret._alphaCutoff = _alphaCutoff;
			return ret;
		}

		protected void Copy(IUniformMap o)
		{
			var other = (StandardMap) o;
			other._material = _material;
			other._alphaCutoff = _alphaCutoff;
			other._alphaMode = _alphaMode;
		}
	}
}

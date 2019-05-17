using System;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;
using Texture = UnityEngine.Texture;

namespace UnityGLTF
{
	public class StandardMap : IUniformMap
	{
		protected Material _material;
		private AlphaMode _alphaMode = AlphaMode.OPAQUE;
		private double _alphaCutoff = 0.5;

		private Vector2 normalOffset = new Vector2(0, 0);
		private Vector2 occlusionOffset = new Vector2(0, 0);
		private Vector2 emissiveOffset = new Vector2(0, 0);

		protected StandardMap(string shaderName, int MaxLOD = 1000)
		{
			var s = Shader.Find(shaderName);
			if (s == null)
			{
				throw new ShaderNotFoundException(shaderName + " not found. Did you forget to add it to the build?");
			}

			s.maximumLOD = MaxLOD;
			_material = new Material(s);
		}

		protected StandardMap(Material mat, int MaxLOD = 1000)
		{
			mat.shader.maximumLOD = MaxLOD;
			_material = mat;

			if (mat.HasProperty("_Cutoff"))
			{
				_alphaCutoff = mat.GetFloat("_Cutoff");
			}

			switch (mat.renderQueue)
			{
				case (int)RenderQueue.AlphaTest:
					_alphaMode = AlphaMode.MASK;
					break;
				case (int)RenderQueue.Transparent:
					_alphaMode = AlphaMode.BLEND;
					break;
				case (int)RenderQueue.Geometry:
				default:
					_alphaMode = AlphaMode.OPAQUE;
					break;
			}
		}

		public Material Material { get { return _material; } }

		public virtual Texture NormalTexture
		{
			get { return _material.HasProperty("_BumpMap") ? _material.GetTexture("_BumpMap") : null; }
			set
			{
				if (_material.HasProperty("_BumpMap"))
				{
					_material.SetTexture("_BumpMap", value);
					_material.EnableKeyword("_NORMALMAP");
				}
				else
				{
					Debug.LogWarning("Tried to set a normal map texture to a material that does not support it.");
				}
			}
		}

		// not implemented by the Standard shader
		public virtual int NormalTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector2 NormalXOffset
		{
			get { return normalOffset; }
			set
			{
				normalOffset = value;
				var unitySpaceVec = new Vector2(normalOffset.x, 1 - NormalXScale.y - normalOffset.y);
				_material.SetTextureOffset("_BumpMap", unitySpaceVec);
			}
		}

		public virtual double NormalXRotation
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector2 NormalXScale
		{
			get { return _material.GetTextureScale("_BumpMap"); }
			set {
				_material.SetTextureScale("_BumpMap", value);
				NormalXOffset = normalOffset;
			}
		}

		public virtual int NormalXTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public virtual double NormalTexScale
		{
			get { return _material.HasProperty("_BumpScale") ? _material.GetFloat("_BumpScale") : 1; }
			set
			{
				if (_material.HasProperty("_BumpScale"))
				{
					_material.SetFloat("_BumpScale", (float)value);
				}
				else
				{
					Debug.LogWarning("Tried to set a normal map scale to a material that does not support it.");
				}
			}
		}

		public virtual Texture OcclusionTexture
		{
			get { return _material.HasProperty("_OcclusionMap") ? _material.GetTexture("_OcclusionMap") : null; }
			set
			{
				if (_material.HasProperty("_OcclusionMap"))
				{
					_material.SetTexture("_OcclusionMap", value);
				}
				else
				{
					Debug.LogWarning("Tried to set an occlusion map to a material that does not support it.");
				}
			}
		}

		// not implemented by the Standard shader
		public virtual int OcclusionTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector2 OcclusionXOffset
		{
			get { return occlusionOffset; }
			set
			{
				occlusionOffset = value;
				var unitySpaceVec = new Vector2(occlusionOffset.x, 1 - OcclusionXScale.y - occlusionOffset.y);
				_material.SetTextureOffset("_OcclusionMap", unitySpaceVec);
			}
		}

		public virtual double OcclusionXRotation
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector2 OcclusionXScale
		{
			get { return _material.GetTextureScale("_OcclusionMap"); }
			set {
				_material.SetTextureScale("_OcclusionMap", value);
				OcclusionXOffset = occlusionOffset;
			}
		}

		public virtual int OcclusionXTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public virtual double OcclusionTexStrength
		{
			get { return _material.HasProperty("_OcclusionStrength") ? _material.GetFloat("_OcclusionStrength") : 1; }
			set
			{
				if (_material.HasProperty("_OcclusionStrength"))
				{
					_material.SetFloat("_OcclusionStrength", (float)value);
				}
				else
				{
					Debug.LogWarning("Tried to set occlusion strength to a material that does not support it.");
				}
			}
		}

		public virtual Texture EmissiveTexture
		{
			get { return _material.HasProperty("_EmissionMap") ? _material.GetTexture("_EmissionMap") : null; }
			set
			{
				if (_material.HasProperty("_EmissionMap"))
				{
					_material.SetTexture("_EmissionMap", value);
					_material.EnableKeyword("_EMISSION");
				}
				else
				{
					Debug.LogWarning("Tried to set an emission map to a material that does not support it.");
				}
			}
		}

		// not implemented by the Standard shader
		public virtual int EmissiveTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector2 EmissiveXOffset
		{
			get { return emissiveOffset; }
			set
			{
				emissiveOffset = value;
				var unitySpaceVec = new Vector2(emissiveOffset.x, 1 - EmissiveXScale.y - emissiveOffset.y);
				_material.SetTextureOffset("_EmissionMap", unitySpaceVec);
			}
		}

		public virtual double EmissiveXRotation
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector2 EmissiveXScale
		{
			get { return _material.GetTextureScale("_EmissionMap"); }
			set {
				_material.SetTextureScale("_EmissionMap", value);
				EmissiveXOffset = emissiveOffset;
			}
		}

		public virtual int EmissiveXTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public virtual Color EmissiveFactor
		{
			get { return _material.HasProperty("_EmissionColor") ? _material.GetColor("_EmissionColor") : Color.white; }
			set
			{
				if (_material.HasProperty("_EmissionColor"))
				{
					_material.SetColor("_EmissionColor", value);
				}
				else
				{
					Debug.LogWarning("Tried to set an emission factor to a material that does not support it.");
				}
			}
		}

		public virtual AlphaMode AlphaMode
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
					if (_material.HasProperty("_Cutoff"))
					{
						_material.SetFloat("_Cutoff", (float)_alphaCutoff);
					}
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
					_material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
				}

				_alphaMode = value;
			}
		}

		public virtual double AlphaCutoff
		{
			get { return _alphaCutoff; }
			set
			{
				if ((_alphaMode == AlphaMode.MASK) && _material.HasProperty("_Cutoff"))
				{
					_material.SetFloat("_Cutoff", (float)value);
				}
				_alphaCutoff = value;
			}
		}

		public virtual bool DoubleSided
		{
			get { return _material.GetInt("_Cull") == (int)CullMode.Off; }
			set
			{
				if (value)
					_material.SetInt("_Cull", (int)CullMode.Off);
				else
					_material.SetInt("_Cull", (int)CullMode.Back);
			}
		}

		public virtual bool VertexColorsEnabled
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

		public virtual IUniformMap Clone()
		{
			var ret = new StandardMap(new Material(_material));
			ret._alphaMode = _alphaMode;
			ret._alphaCutoff = _alphaCutoff;
			return ret;
		}

		protected virtual void Copy(IUniformMap o)
		{
			var other = (StandardMap)o;
			other._material = _material;
			other._alphaCutoff = _alphaCutoff;
			other._alphaMode = _alphaMode;
		}
	}
}

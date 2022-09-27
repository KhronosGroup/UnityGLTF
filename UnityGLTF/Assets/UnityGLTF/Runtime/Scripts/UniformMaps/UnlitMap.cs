using UnityEngine;
using Material = UnityEngine.Material;
using Texture = UnityEngine.Texture;

namespace UnityGLTF
{
	public class UnlitMap : StandardMap, IUnlitUniformMap
	{
		private Vector2 baseColorOffset = new Vector2(0, 0);

		public UnlitMap(int MaxLOD = 1000) : base("GLTF/Unlit", "4d20effaf200f604db8b73f8e6a2e386", MaxLOD) { }
		public UnlitMap(string shaderName, string shaderGuid, int MaxLOD = 1000) : base(shaderName, shaderGuid, MaxLOD) { }
		public UnlitMap(Material m, int MaxLOD = 1000) : base(m, MaxLOD) { }

		public Texture BaseColorTexture
		{
			get { return _material.GetTexture("_MainTex"); }
			set { _material.SetTexture("_MainTex", value); }
		}

		public virtual int BaseColorTexCoord
		{
			get { return 0; }
			set { return; }
		}

		public virtual Vector2 BaseColorXOffset
		{
			get { return baseColorOffset; }
			set {
				_material.SetTextureOffset("_MainTex", value);
				baseColorOffset = value;
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
	}
}

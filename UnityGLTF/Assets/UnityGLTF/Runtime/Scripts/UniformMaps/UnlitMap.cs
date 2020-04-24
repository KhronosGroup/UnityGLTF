using System;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;
using Texture = UnityEngine.Texture;

namespace UnityGLTF
{
	public class UnlitMap : StandardMap, IUnlitUniformMap
	{
		private Vector2 baseColorOffset = new Vector2(0, 0);

		public UnlitMap(int MaxLOD = 1000) : base("GLTF/Unlit", MaxLOD) { }
		public UnlitMap(string shaderName, int MaxLOD = 1000) : base(shaderName, MaxLOD) { }
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
	}
}

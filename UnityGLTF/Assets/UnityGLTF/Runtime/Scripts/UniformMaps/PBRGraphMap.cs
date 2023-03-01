using UnityEngine;

namespace UnityGLTF
{
	public class PBRGraphMap : BaseGraphMap, IMetalRoughUniformMap, IVolumeMap, ITransmissionMap, IIORMap, IIridescenceMap, ISpecularMap, IClearcoatMap
	{
		private const string PBRGraphGuid = "478ce3626be7a5f4ea58d6b13f05a2e4";

		public PBRGraphMap() : this("UnityGLTF/PBRGraph") {}

		protected PBRGraphMap(string shaderName) : base(shaderName, PBRGraphGuid) { }

#if !UNITY_2021_1_OR_NEWER
		private const string PBRGraphTransparentGuid = "0a931320a74ca574b91d2d7d4557dcf1";
		private const string PBRGraphTransparentDoubleGuid = "54352a53405971b41a6587615f947085";
		private const string PBRGraphDoubleGuid = "8bc739b14fe811644abb82057b363ba8";

		public PBRGraphMap(bool transparent, bool doubleSided) : base(
			"UnityGLTF/PBRGraph" + (transparent ? "-Transparent" : "") + (doubleSided ? "-Double" : ""),
			(transparent && doubleSided ? PBRGraphTransparentDoubleGuid : transparent ? PBRGraphTransparentGuid : doubleSided ? PBRGraphDoubleGuid : PBRGraphGuid)) { }
#endif

		public PBRGraphMap(Material mat) : base(mat) { }

		public override IUniformMap Clone()
		{
			var clone = new PBRGraphMap(new Material(_material));
			clone.Material.shaderKeywords = _material.shaderKeywords;
			return clone;
		}

		public Texture NormalTexture
		{
			get => _material.GetTexture("normalTexture");
			set => _material.SetTexture("normalTexture", value);
		}

		public int NormalTexCoord
		{
			get => 0;
			set {}
		}

		public double NormalTexScale
		{
			get => _material.GetFloat("normalScale");
			set => _material.SetFloat("normalScale", (float) value);
		}

	    public Vector2 NormalXOffset
	    {
		    get => _material.GetTextureOffset("normalTexture");
		    set => _material.SetTextureOffset("normalTexture", value);
	    }

	    public double NormalXRotation { get; set; }

	    public Vector2 NormalXScale
	    {
		    get => _material.GetTextureScale("normalTexture");
		    set => _material.SetTextureScale("normalTexture", value);
	    }

	    public int NormalXTexCoord
	    {
		    get => 0;
		    set {}
	    }

	    public Texture OcclusionTexture
	    {
		    get => _material.GetTexture("occlusionTexture");
		    set => _material.SetTexture("occlusionTexture", value);
	    }

	    public int OcclusionTexCoord
	    {
		    get => (int) _material.GetFloat("occlusionTextureTexCoord");
		    set => _material.SetFloat("occlusionTextureTexCoord", Mathf.RoundToInt(value));
	    }

	    public double OcclusionTexStrength
	    {
		    get => _material.GetFloat("occlusionStrength");
		    set => _material.SetFloat("occlusionStrength", (float) value);
	    }

	    public Vector2 OcclusionXOffset
	    {
		    get => _material.GetTextureOffset("occlusionTexture");
		    set => _material.SetTextureOffset("occlusionTexture", value);
	    }

	    public double OcclusionXRotation
	    {
		    get => _material.GetFloat("occlusionTextureRotation");
		    set => _material.SetFloat("occlusionTextureRotation", (float) value);
	    }

	    public Vector2 OcclusionXScale
	    {
		    get => _material.GetTextureScale("occlusionTexture");
		    set => _material.SetTextureScale("occlusionTexture", value);
	    }

	    public int OcclusionXTexCoord
	    {
		    get => (int) _material.GetFloat("occlusionTextureTexCoord");
		    set => _material.SetFloat("occlusionTextureTexCoord", Mathf.RoundToInt(value));
	    }

	    public Texture EmissiveTexture
	    {
		    get => _material.GetTexture("emissiveTexture");
		    set => _material.SetTexture("emissiveTexture", value);
	    }

	    public int EmissiveTexCoord
	    {
		    get => 0;
		    set {}
	    }

	    public Color EmissiveFactor
	    {
		    get => _material.GetColor("emissiveFactor");
		    set => _material.SetColor("emissiveFactor", value);
	    }

	    public Vector2 EmissiveXOffset
	    {
		    get => _material.GetTextureOffset("emissiveTexture");
		    set => _material.SetTextureOffset("emissiveTexture", value);
	    }

	    public double EmissiveXRotation { get; set; }

	    public Vector2 EmissiveXScale
	    {
		    get => _material.GetTextureScale("emissiveTexture");
		    set => _material.SetTextureScale("emissiveTexture", value);
	    }

	    public int EmissiveXTexCoord
	    {
		    get => 0;
		    set {}
	    }

	    public Texture MetallicRoughnessTexture
	    {
		    get => _material.GetTexture("metallicRoughnessTexture");
		    set => _material.SetTexture("metallicRoughnessTexture", value);
	    }

	    public int MetallicRoughnessTexCoord
	    {
		    get => 0;
		    set {}
	    }

	    public Vector2 MetallicRoughnessXOffset
	    {
		    get => _material.GetTextureOffset("metallicRoughnessTexture");
		    set => _material.SetTextureOffset("metallicRoughnessTexture", value);
	    }

	    public double MetallicRoughnessXRotation { get; set; }

	    public Vector2 MetallicRoughnessXScale
	    {
		    get => _material.GetTextureOffset("metallicRoughnessTexture");
		    set => _material.SetTextureOffset("metallicRoughnessTexture", value);
	    }

	    public int MetallicRoughnessXTexCoord
	    {
		    get => 0;
		    set {}
	    }

	    public double MetallicFactor
	    {
		    get => _material.GetFloat("metallicFactor");
		    set => _material.SetFloat("metallicFactor", (float) value);
	    }

	    public double RoughnessFactor
	    {
		    get => _material.GetFloat("roughnessFactor");
		    set => _material.SetFloat("roughnessFactor", (float) value);
	    }

	    public double ThicknessFactor
	    {
		    get => _material.GetFloat("thicknessFactor");
		    set
		    {
			    _material.SetFloat("thicknessFactor", (float) value);
		    }
	    }

	    public Texture ThicknessTexture
	    {
		    get => _material.GetTexture("thicknessTexture");
		    set
		    {
			    _material.SetTexture("thicknessTexture", value);
		    }
	    }

	    public double AttenuationDistance
	    {
		    get => _material.GetFloat("attenuationDistance");
		    set => _material.SetFloat("attenuationDistance", (float) value);
	    }

	    public Color AttenuationColor
	    {
		    get => _material.GetColor("attenuationColor");
		    set => _material.SetColor("attenuationColor", value);
	    }

	    public double TransmissionFactor
	    {
		    get => _material.GetFloat("transmissionFactor");
		    set
		    {
			    _material.SetFloat("transmissionFactor", (float) value);
		    }
	    }

	    public Texture TransmissionTexture
	    {
		    get => _material.GetTexture("transmissionTexture");
		    set
		    {
			    _material.SetTexture("transmissionTexture", value);
		    }
	    }

	    public double IOR
	    {
		    get => _material.GetFloat("ior");
		    set => _material.SetFloat("ior", (float) value);
	    }

	    public double IridescenceFactor
	    {
		    get => _material.GetFloat("iridescenceFactor");
		    set => _material.SetFloat("iridescenceFactor", (float) value);
	    }

	    public double IridescenceIor
	    {
		    get => _material.GetFloat("iridescenceIor");
		    set => _material.SetFloat("iridescenceIor", (float) value);
	    }

	    public double IridescenceThicknessMinimum
	    {
		    get => _material.GetFloat("iridescenceThicknessMinimum");
		    set => _material.SetFloat("iridescenceThicknessMinimum", (float) value);
	    }

	    public double IridescenceThicknessMaximum
	    {
		    get => _material.GetFloat("iridescenceThicknessMaximum");
		    set => _material.SetFloat("iridescenceThicknessMaximum", (float) value);
	    }

	    public Texture IridescenceTexture
	    {
		    get => _material.GetTexture("iridescenceTexture");
		    set
		    {
			    _material.SetTexture("iridescenceTexture", value);
		    }
	    }

	    public Texture IridescenceThicknessTexture
	    {
		    get => _material.GetTexture("iridescenceThicknessTexture");
		    set
		    {
			    _material.SetTexture("iridescenceThicknessTexture", value);
		    }
	    }

	    public double SpecularFactor
	    {
		    get => _material.GetFloat("specularFactor");
		    set => _material.SetFloat("specularFactor", (float) value);
	    }

	    public Texture SpecularTexture
	    {
		    get => _material.GetTexture("specularTexture");
		    set
		    {
			    _material.SetTexture("specularTexture", value);
		    }
	    }

	    public Color SpecularColorFactor
	    {
		    get => _material.GetColor("specularColorFactor");
		    set => _material.SetColor("specularColorFactor", value);
	    }

	    public Texture SpecularColorTexture
	    {
		    get => _material.GetTexture("specularColorTexture");
		    set
		    {
			    _material.SetTexture("specularColorTexture", value);
		    }
	    }

	    public double ClearcoatFactor
	    {
		    get => _material.GetFloat("clearcoatFactor");
		    set => _material.SetFloat("clearcoatFactor", (float) value);
	    }

	    public Texture ClearcoatTexture
	    {
		    get => _material.GetTexture("clearcoatTexture");
		    set => _material.SetTexture("clearcoatTexture", value);
	    }

	    public double ClearcoatRoughnessFactor
	    {
		    get => _material.GetFloat("clearcoatRoughnessFactor");
		    set => _material.SetFloat("clearcoatRoughnessFactor", (float) value);
	    }

	    public Texture ClearcoatRoughnessTexture
	    {
		    get => _material.GetTexture("clearcoatRoughnessTexture");
		    set => _material.SetTexture("clearcoatRoughnessTexture", value);
	    }

	    public Texture ClearcoatNormalTexture
	    {
		    get => _material.GetTexture("clearcoatNormalTexture");
		    set => _material.SetTexture("clearcoatNormalTexture", value);
	    }
	}
}

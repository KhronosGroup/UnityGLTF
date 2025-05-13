using UnityEngine;

namespace UnityGLTF
{
	public class PBRGraphMap : BaseGraphMap, IMetalRoughUniformMap, IVolumeMap, ITransmissionMap, IIORMap, IIridescenceMap, ISpecularMap, IClearcoatMap, IDispersionMap, ISheenMap, IAnisotropyMap
	{
		internal const string PBRGraphGuid = "478ce3626be7a5f4ea58d6b13f05a2e4";

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
			get => (int) _material.GetFloat("normalTextureTexCoord");
			set => _material.SetFloat("normalTextureTexCoord", Mathf.RoundToInt(value));
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

	    public double NormalXRotation
	    {
		    get => _material.GetFloat("normalTextureRotation");
		    set => _material.SetFloat("normalTextureRotation", (float) value);
	    }

	    public Vector2 NormalXScale
	    {
		    get => _material.GetTextureScale("normalTexture");
		    set => _material.SetTextureScale("normalTexture", value);
	    }

	    public int NormalXTexCoord
	    {
		    get => (int) _material.GetFloat("normalTextureTexCoord");
		    set => _material.SetFloat("normalTextureTexCoord", Mathf.RoundToInt(value));
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
		    get =>  (int)_material.GetFloat("emissiveTextureTexCoord");
		    set => _material.SetFloat("emissiveTextureTexCoord", (float) value);
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

	    public double EmissiveXRotation
	    {
		    get => _material.GetFloat("emissiveTextureRotation");
		    set => _material.SetFloat("emissiveTextureRotation", (float) value);
	    }

	    public Vector2 EmissiveXScale
	    {
		    get => _material.GetTextureScale("emissiveTexture");
		    set => _material.SetTextureScale("emissiveTexture", value);
	    }

	    public int EmissiveXTexCoord
	    {
		    get =>  (int)_material.GetFloat("emissiveTextureTexCoord");
		    set => _material.SetFloat("emissiveTextureTexCoord", (float) value);
	    }

	    public Texture MetallicRoughnessTexture
	    {
		    get => _material.GetTexture("metallicRoughnessTexture");
		    set => _material.SetTexture("metallicRoughnessTexture", value);
	    }

	    public int MetallicRoughnessTexCoord
	    {
		    get =>  (int)_material.GetFloat("metallicRoughnessTextureTexCoord");
		    set => _material.SetFloat("metallicRoughnessTextureTexCoord", (float) value);
	    }

	    public Vector2 MetallicRoughnessXOffset
	    {
		    get => _material.GetTextureOffset("metallicRoughnessTexture");
		    set => _material.SetTextureOffset("metallicRoughnessTexture", value);
	    }

	    public double MetallicRoughnessXRotation
	    {
		    get => _material.GetFloat("metallicRoughnessTextureRotation");
		    set => _material.SetFloat("metallicRoughnessTextureRotation", (float) value);
	    }

	    public Vector2 MetallicRoughnessXScale
	    {
		    get => _material.GetTextureScale("metallicRoughnessTexture");
		    set => _material.SetTextureScale("metallicRoughnessTexture", value);
	    }

	    public int MetallicRoughnessXTexCoord
	    {
		    get =>  (int)_material.GetFloat("metallicRoughnessTextureTexCoord");
		    set => _material.SetFloat("metallicRoughnessTextureTexCoord", (float) value);
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
		    set => _material.SetFloat("thicknessFactor", (float) value);
	    }

	    public Texture ThicknessTexture
	    {
		    get => _material.GetTexture("thicknessTexture");
		    set => _material.SetTexture("thicknessTexture", value);
	    }

	    public double ThicknessTextureRotation
	    {
		    get => _material.GetFloat("thicknessTextureRotation");
		    set => _material.SetFloat("thicknessTextureRotation", (float) value);
	    }

	    public Vector2 ThicknessTextureOffset
	    {
		    get => _material.GetTextureOffset("thicknessTexture");
		    set => _material.SetTextureOffset("thicknessTexture", value);
	    }

	    public Vector2 ThicknessTextureScale
	    {
		    get => _material.GetTextureScale("thicknessTexture");
		    set => _material.SetTextureScale("thicknessTexture", value);
	    }

	    public int ThicknessTextureTexCoord
	    {
		    get =>  (int)_material.GetFloat("thicknessTextureTexCoord");
		    set => _material.SetFloat("thicknessTextureTexCoord", (float) value);
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
		    set => _material.SetFloat("transmissionFactor", (float) value);
	    }

	    public Texture TransmissionTexture
	    {
		    get => _material.GetTexture("transmissionTexture");
		    set => _material.SetTexture("transmissionTexture", value);
	    }

	    public double TransmissionTextureRotation
	    {
		    get => _material.GetFloat("transmissionTextureRotation");
		    set => _material.SetFloat("transmissionTextureRotation", (float) value);
	    }

	    public Vector2 TransmissionTextureOffset
	    {
		    get => _material.GetTextureOffset("transmissionTexture");
		    set => _material.SetTextureOffset("transmissionTexture", value);
	    }

	    public Vector2 TransmissionTextureScale
	    {
		    get => _material.GetTextureScale("transmissionTexture");
		    set => _material.SetTextureScale("transmissionTexture", value);
	    }

	    public int TransmissionTextureTexCoord
	    {
		    get =>  (int)_material.GetFloat("transmissionTextureTexCoord");
		    set => _material.SetFloat("transmissionTextureTexCoord", (float) value);
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
		    set => _material.SetTexture("iridescenceTexture", value);
	    }

	    public double IridescenceTextureRotation
	    {
		    get => _material.GetFloat("iridescenceTextureRotation");
		    set => _material.SetFloat("iridescenceTextureRotation", (float) value);
	    }

	    public Vector2 IridescenceTextureOffset
	    {
		    get => _material.GetTextureOffset("iridescenceTexture");
		    set => _material.SetTextureOffset("iridescenceTexture", value);
	    }

	    public Vector2 IridescenceTextureScale
	    {
		    get => _material.GetTextureScale("iridescenceTexture");
		    set => _material.SetTextureScale("iridescenceTexture", value);
	    }

	    public int IridescenceTextureTexCoord
	    {
		    get =>  (int)_material.GetFloat("iridescenceTextureTexCoord");
		    set => _material.SetFloat("iridescenceTextureTexCoord", (float) value);
	    }

	    public Texture IridescenceThicknessTexture
	    {
		    get => _material.GetTexture("iridescenceThicknessTexture");
		    set => _material.SetTexture("iridescenceThicknessTexture", value);
	    }

	    public double IridescenceThicknessTextureRotation
	    {
		    get => _material.GetFloat("iridescenceThicknessTextureRotation");
		    set => _material.SetFloat("iridescenceThicknessTextureRotation", (float) value);
	    }

	    public Vector2 IridescenceThicknessTextureOffset
	    {
		    get => _material.GetTextureOffset("iridescenceThicknessTexture");
		    set => _material.SetTextureOffset("iridescenceThicknessTexture", value);
	    }

	    public Vector2 IridescenceThicknessTextureScale
	    {
		    get => _material.GetTextureScale("iridescenceThicknessTexture");
		    set => _material.SetTextureScale("iridescenceThicknessTexture", value);
	    }

	    public int IridescenceThicknessTextureTexCoord
	    {
		    get =>  (int)_material.GetFloat("iridescenceThicknessTextureTexCoord");
		    set => _material.SetFloat("iridescenceThicknessTextureTexCoord", (float) value);
	    }

	    public double SpecularFactor
	    {
		    get => _material.GetFloat("specularFactor");
		    set => _material.SetFloat("specularFactor", (float) value);
	    }

	    public Texture SpecularTexture
	    {
		    get => _material.GetTexture("specularTexture");
		    set=> _material.SetTexture("specularTexture", value);
	    }

	    public double SpecularTextureRotation
	    {
		    get => _material.GetFloat("specularTextureRotation");
		    set => _material.SetFloat("specularTextureRotation", (float) value);
	    }

	    public Vector2 SpecularTextureOffset
	    {
		    get => _material.GetTextureOffset("specularTexture");
		    set => _material.SetTextureOffset("specularTexture", value);
	    }

	    public Vector2 SpecularTextureScale
	    {
		    get => _material.GetTextureScale("specularTexture");
		    set => _material.SetTextureScale("specularTexture", value);
	    }

	    public int SpecularTextureTexCoord
	    {
		    get =>  (int)_material.GetFloat("specularTextureTexCoord");
		    set => _material.SetFloat("specularTextureTexCoord", (float) value);
	    }

	    public Color SpecularColorFactor
	    {
		    get => _material.GetColor("specularColorFactor");
		    set => _material.SetColor("specularColorFactor", value);
	    }

	    public Texture SpecularColorTexture
	    {
		    get => _material.GetTexture("specularColorTexture");
		    set => _material.SetTexture("specularColorTexture", value);
	    }

	    public double SpecularColorTextureRotation
	    {
		    get => _material.GetFloat("specularColorTextureRotation");
		    set => _material.SetFloat("specularColorTextureRotation", (float) value);
	    }

	    public Vector2 SpecularColorTextureOffset
	    {
		    get => _material.GetTextureOffset("specularColorTexture");
		    set => _material.SetTextureOffset("specularColorTexture", value);
	    }

	    public Vector2 SpecularColorTextureScale
	    {
		    get => _material.GetTextureScale("specularColorTexture");
		    set => _material.SetTextureScale("specularColorTexture", value);
	    }

	    public int SpecularColorTextureTexCoord
	    {
		    get =>  (int)_material.GetFloat("specularColorTextureTexCoord");
		    set => _material.SetFloat("specularColorTextureTexCoord", (float) value);
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

	    public double ClearcoatTextureRotation
	    {
		    get => _material.GetFloat("clearcoatTextureRotation");
		    set => _material.SetFloat("clearcoatTextureRotation", (float) value);
	    }

	    public Vector2 ClearcoatTextureOffset
	    {
		    get => _material.GetTextureOffset("clearcoatTexture");
		    set => _material.SetTextureOffset("clearcoatTexture", value);
	    }

	    public Vector2 ClearcoatTextureScale
	    {
		    get => _material.GetTextureScale("clearcoatTexture");
		    set => _material.SetTextureScale("clearcoatTexture", value);
	    }

	    public int ClearcoatTextureTexCoord
	    {
		    get =>  (int)_material.GetFloat("clearcoatTextureTexCoord");
		    set => _material.SetFloat("clearcoatTextureTexCoord", (float) value);
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

	    public double ClearcoatRoughnessTextureRotation
	    {
		    get => _material.GetFloat("clearcoatRoughnessTextureRotation");
		    set => _material.SetFloat("clearcoatRoughnessTextureRotation", (float) value);
	    }

	    public Vector2 ClearcoatRoughnessTextureOffset
	    {
		    get => _material.GetTextureOffset("clearcoatRoughnessTexture");
		    set => _material.SetTextureOffset("clearcoatRoughnessTexture", value);
	    }

	    public Vector2 ClearcoatRoughnessTextureScale
	    {
		    get => _material.GetTextureScale("clearcoatRoughnessTexture");
		    set => _material.SetTextureScale("clearcoatRoughnessTexture", value);
	    }

	    public int ClearcoatRoughnessTextureTexCoord
	    {
		    get =>  (int)_material.GetFloat("clearcoatRoughnessTextureTexCoord");
		    set => _material.SetFloat("clearcoatRoughnessTextureTexCoord", (float) value);
	    }
	    
	    public float Dispersion
	    {
		    get =>  _material.GetFloat("dispersion");
		    set => _material.SetFloat("dispersion", value);
	    }

	    public float SheenRoughnessFactor
	    {
		    get =>  _material.GetFloat("sheenRoughnessFactor");
		    set => _material.SetFloat("sheenRoughnessFactor", value);
	    }

	    public Color SheenColorFactor
	    {
		    get =>  _material.GetColor("sheenColorFactor");
		    set => _material.SetColor("sheenColorFactor", value);
	    }
	    
	    public Texture SheenColorTexture
	    {
		    get => _material.GetTexture("sheenColorTexture");
		    set => _material.SetTexture("sheenColorTexture", value);
	    }

	    public double SheenColorTextureRotation
	    {
		    get => _material.GetFloat("sheenColorTextureRotation");
		    set => _material.SetFloat("sheenColorTextureRotation", (float) value);
	    }

	    public Vector2 SheenColorTextureOffset
	    {
		    get => _material.GetTextureOffset("sheenColorTexture");
		    set => _material.SetTextureOffset("sheenColorTexture", value);
	    }

	    public Vector2 SheenColorTextureScale
	    {
		    get => _material.GetTextureScale("sheenColorTexture");
		    set => _material.SetTextureScale("sheenColorTexture", value);
	    }

	    public int SheenColorTextureTexCoord
	    {
		    get =>  (int)_material.GetFloat("sheenColorTextureTexCoord");
		    set => _material.SetFloat("sheenColorTextureTexCoord", (float) value);
	    }
	    
	    public Texture SheenRoughnessTexture
	    {
		    get => _material.GetTexture("sheenRoughnessTexture");
		    set => _material.SetTexture("sheenRoughnessTexture", value);
	    }

	    public double SheenRoughnessTextureRotation
	    {
		    get => _material.GetFloat("sheenRoughnessTextureRotation");
		    set => _material.SetFloat("sheenRoughnessTextureRotation", (float) value);
	    }

	    public Vector2 SheenRoughnessTextureOffset
	    {
		    get => _material.GetTextureOffset("sheenRoughnessTexture");
		    set => _material.SetTextureOffset("sheenRoughnessTexture", value);
	    }

	    public Vector2 SheenRoughnessTextureScale
	    {
		    get => _material.GetTextureScale("sheenRoughnessTexture");
		    set => _material.SetTextureScale("sheenRoughnessTexture", value);
	    }

	    public int SheenRoughnessTextureTexCoord
	    {
		    get =>  (int)_material.GetFloat("sheenRoughnessTextureTexCoord");
		    set => _material.SetFloat("sheenRoughnessTextureTexCoord", (float) value);
	    }

	    
	    /* Clearcoat Normal Texture currently not supported
	    public Texture ClearcoatNormalTexture
	    {
		    get => _material.GetTexture("clearcoatNormalTexture");
		    set => _material.SetTexture("clearcoatNormalTexture", value);
	    }

	    public double ClearcoatNormalTextureRotation
	    {
		    get => _material.GetFloat("clearcoatNormalTextureRotation");
		    set => _material.SetFloat("clearcoatNormalTextureRotation", (float) value);
	    }

	    public Vector2 ClearcoatNormalTextureOffset
	    {
		    get => _material.GetTextureOffset("clearcoatNormalTexture");
		    set => _material.SetTextureOffset("clearcoatNormalTexture", value);
	    }

	    public Vector2 ClearcoatNormalTextureScale
	    {
		    get => _material.GetTextureScale("clearcoatNormalTexture");
		    set =>  _material.SetTextureScale("clearcoatNormalTexture", value);
	    }

	    public int ClearcoatNormalTextureTexCoord
	    {
		    get =>  (int)_material.GetFloat("clearcoatNormalTextureTexCoord");
		    set =>  _material.SetFloat("clearcoatNormalTextureTexCoord", (float) value);
	    }
	    */

	    public double anisotropyStrength
	    {
		    get => _material.GetFloat("anisotropyStrength");
		    set => _material.SetFloat("anisotropyStrength", (float) value);
	    }

	    public double anisotropyRotation
	    {
		    get => _material.GetFloat("anisotropyRotation");
		    set => _material.SetFloat("anisotropyRotation", (float) value);
		    
	    }
	    
	    public Texture anisotropyTexture
	    {
		    get => _material.GetTexture("anisotropyTexture");
		    set => _material.SetTexture("anisotropyTexture", value);
	    }

	    public double anisotropyTextureRotation
	    {
		    get => _material.GetFloat("anisotropyTextureRotation");
		    set => _material.SetFloat("anisotropyTextureRotation", (float) value);
	    }

	    public Vector2 anisotropyTextureOffset
	    {
		    get => _material.GetTextureOffset("anisotropyTexture");
		    set => _material.SetTextureOffset("anisotropyTexture", value);
	    }

	    public Vector2 anisotropyTextureScale
	    {
		    get => _material.GetTextureScale("anisotropyTexture");
		    set => _material.SetTextureScale("anisotropyTexture", value);
	    }

	    public int anisotropyTextureTexCoord
	    {
		    get =>  (int)_material.GetFloat("anisotropyTextureTexCoord");
		    set => _material.SetFloat("anisotropyTextureTexCoord", (float) value);
	    }

	}
}

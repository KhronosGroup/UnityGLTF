using GLTF.Schema;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF;

public class PBRGraphMap : IMetalRoughUniformMap, IVolumeMap, ITransmissionMap, IIORMap, IIridescenceMap, ISpecularMap
{
	protected Material _material;

	public PBRGraphMap() : this("UnityGLTF/PBRGraph") {}

	protected PBRGraphMap(string shaderName)
	{
		var s = Shader.Find(shaderName);
		if (s == null)
		{
			throw new ShaderNotFoundException(shaderName + " not found. Did you forget to add it to the build?");
		}
		_material = new Material(s);
	}

	protected PBRGraphMap(Material mat)
	{
		_material = mat;
	}

	public IUniformMap Clone()
	{
		var clone = new PBRGraphMap(new Material(_material));
		clone.Material.shaderKeywords = _material.shaderKeywords;
		return clone;
	}

	public Material Material => _material;

	public Texture NormalTexture
	{
		get => _material.GetTexture("_NormalTexture");
		set => _material.SetTexture("_NormalTexture", value);
	}

	public int NormalTexCoord
	{
		get => 0;
		set {}
	}

	public double NormalTexScale
	{
		get => _material.GetFloat("_NormalScale");
		set => _material.SetFloat("_NormalScale", (float) value);
	}

    public Vector2 NormalXOffset
    {
	    get => _material.GetTextureOffset("_NormalTexture");
	    set => _material.SetTextureOffset("_NormalTexture", value);
    }

    public double NormalXRotation { get; set; }

    public Vector2 NormalXScale
    {
	    get => _material.GetTextureScale("_NormalTexture");
	    set => _material.SetTextureScale("_NormalTexture", value);
    }

    public int NormalXTexCoord
    {
	    get => 0;
	    set {}
    }

    public Texture OcclusionTexture
    {
	    get => _material.GetTexture("_OcclusionTexture");
	    set => _material.SetTexture("_OcclusionTexture", value);
    }

    public int OcclusionTexCoord
    {
	    get => 0;
	    set {}
    }

    public double OcclusionTexStrength
    {
	    get => _material.GetFloat("_OcclusionStrength");
	    set => _material.SetFloat("_OcclusionStrength", (float) value);
    }

    public Vector2 OcclusionXOffset
    {
	    get => _material.GetTextureOffset("_OcclusionTexture");
	    set => _material.SetTextureOffset("_OcclusionTexture", value);
    }

    public double OcclusionXRotation { get; set; }

    public Vector2 OcclusionXScale
    {
	    get => _material.GetTextureScale("_OcclusionTexture");
	    set => _material.SetTextureScale("_OcclusionTexture", value);
    }

    public int OcclusionXTexCoord
    {
	    get => 0;
	    set {}
    }

    public Texture EmissiveTexture
    {
	    get => _material.GetTexture("_EmissiveTexture");
	    set => _material.SetTexture("_EmissiveTexture", value);
    }

    public int EmissiveTexCoord
    {
	    get => 0;
	    set {}
    }

    public Color EmissiveFactor
    {
	    get => _material.GetColor("_EmissiveFactor");
	    set => _material.SetColor("_EmissiveFactor", value);
    }

    public Vector2 EmissiveXOffset
    {
	    get => _material.GetTextureOffset("_EmissiveTexture");
	    set => _material.SetTextureOffset("_EmissiveTexture", value);
    }

    public double EmissiveXRotation { get; set; }

    public Vector2 EmissiveXScale
    {
	    get => _material.GetTextureScale("_EmissiveTexture");
	    set => _material.SetTextureScale("_EmissiveTexture", value);
    }

    public int EmissiveXTexCoord
    {
	    get => 0;
	    set {}
    }

    private AlphaMode _alphaMode;
    public virtual AlphaMode AlphaMode
    {
	    get { return _alphaMode; }
	    set
	    {
		    if (value == AlphaMode.MASK)
		    {
			    _material.SetOverrideTag("RenderType", "TransparentCutout");
			    _material.SetFloat("_Mode", 1);
			    _material.SetInt("_SrcBlend", (int)BlendMode.One);
			    _material.SetInt("_DstBlend", (int)BlendMode.Zero);
			    _material.SetInt("_ZWrite", 1);
			    _material.EnableKeyword("_ALPHATEST_ON");
			    _material.DisableKeyword("_ALPHABLEND_ON");
			    _material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			    _material.renderQueue = (int)RenderQueue.AlphaTest;
			    if (_material.HasProperty("_Cutoff"))
			    {
				    _material.SetFloat("_Cutoff", (float)AlphaCutoff);
			    }

			    SetAlphaModeMask(_material, true);
		    }
		    else if (value == AlphaMode.BLEND)
		    {
			    _material.SetOverrideTag("RenderType", "Transparent");
			    _material.SetFloat("_Mode", 2);
			    _material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
			    _material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
			    _material.SetInt("_ZWrite", 0);
			    _material.DisableKeyword("_ALPHATEST_ON");
			    _material.EnableKeyword("_ALPHABLEND_ON");
			    _material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			    _material.renderQueue = (int)RenderQueue.Transparent;
			    _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

			    SetShaderModeBlend(_material);
		    }
		    else
		    {
			    _material.SetOverrideTag("RenderType", "Opaque");
			    _material.SetFloat("_Mode", 0);
			    _material.SetInt("_SrcBlend", (int)BlendMode.One);
			    _material.SetInt("_DstBlend", (int)BlendMode.Zero);
			    _material.SetInt("_ZWrite", 1);
			    _material.DisableKeyword("_ALPHATEST_ON");
			    _material.DisableKeyword("_ALPHABLEND_ON");
			    _material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			    _material.renderQueue = (int)RenderQueue.Geometry;
		    }

		    _alphaMode = value;
	    }
    }

    protected void SetDoubleSided(Material material) {
	    material.doubleSidedGI = true;
	    material.SetFloat(cullPropId, (int) CullMode.Off);
    }

    public static readonly int cutoffPropId = Shader.PropertyToID("_Cutoff");

    protected void SetAlphaModeMask(Material material, bool isMask) {
	    material.SetFloat(cutoffPropId, isMask ? 1 : 0);
// #if USING_HDRP_10_OR_NEWER || USING_URP_12_OR_NEWER
        material.EnableKeyword(KW_ALPHATEST_ON);
        material.SetOverrideTag(TAG_RENDER_TYPE, TAG_RENDER_TYPE_CUTOUT);
        material.SetFloat(k_ZTestGBufferPropId, (int)CompareFunction.Equal); //3
// #endif
	    material.SetFloat(k_AlphaClip, 1);
    }

    static readonly int cullPropId = Shader.PropertyToID("_Cull");
    static readonly int cullModePropId = Shader.PropertyToID("_CullMode");
    static readonly int k_AlphaClip = Shader.PropertyToID("_AlphaClip");
    static readonly int k_Surface = Shader.PropertyToID("_Surface");
    static readonly int k_AlphaDstBlendPropId = Shader.PropertyToID("_AlphaDstBlend");
    static readonly int k_ZTestGBufferPropId = Shader.PropertyToID("_ZTestGBuffer");
    static readonly int srcBlendPropId = Shader.PropertyToID("_SrcBlend");
    static readonly int dstBlendPropId = Shader.PropertyToID("_DstBlend");
    static readonly int zWritePropId = Shader.PropertyToID("_ZWrite");

    const string TAG_RENDER_TYPE = "RenderType";
    const string TAG_RENDER_TYPE_CUTOUT = "TransparentCutout";
    const string TAG_RENDER_TYPE_OPAQUE = "Opaque";
    const string TAG_RENDER_TYPE_FADE = "Fade";
    const string TAG_RENDER_TYPE_TRANSPARENT = "Transparent";
    const string KW_ALPHATEST_ON = "_ALPHATEST_ON";
    const string KW_DISABLE_SSR_TRANSPARENT = "_DISABLE_SSR_TRANSPARENT";
    const string KW_ENABLE_FOG_ON_TRANSPARENT = "_ENABLE_FOG_ON_TRANSPARENT";
    const string KW_SURFACE_TYPE_TRANSPARENT = "_SURFACE_TYPE_TRANSPARENT";
    const string k_ShaderPassTransparentDepthPrepass = "TransparentDepthPrepass";
    const string k_ShaderPassTransparentDepthPostpass = "TransparentDepthPostpass";
    const string k_ShaderPassTransparentBackface = "TransparentBackface";
    const string k_ShaderPassRayTracingPrepass = "RayTracingPrepass";
    const string k_ShaderPassDepthOnlyPass = "DepthOnly";

    protected void SetShaderModeBlend(Material material)
    {
	    material.SetOverrideTag(TAG_RENDER_TYPE, TAG_RENDER_TYPE_TRANSPARENT);
	    material.EnableKeyword(KW_SURFACE_TYPE_TRANSPARENT);
	    material.EnableKeyword(KW_DISABLE_SSR_TRANSPARENT);
	    material.EnableKeyword(KW_ENABLE_FOG_ON_TRANSPARENT);
	    material.SetShaderPassEnabled(k_ShaderPassTransparentDepthPrepass, false);
	    material.SetShaderPassEnabled(k_ShaderPassTransparentDepthPostpass, false);
	    material.SetShaderPassEnabled(k_ShaderPassTransparentBackface, false);
	    material.SetShaderPassEnabled(k_ShaderPassRayTracingPrepass, false);
	    material.SetShaderPassEnabled(k_ShaderPassDepthOnlyPass, false);
	    material.SetFloat(srcBlendPropId, (int) BlendMode.SrcAlpha);//5
	    material.SetFloat(dstBlendPropId, (int)BlendMode.OneMinusSrcAlpha);//10
	    material.SetFloat(k_ZTestGBufferPropId, (int)CompareFunction.Equal); //3
	    material.SetFloat(k_AlphaDstBlendPropId, (int)BlendMode.OneMinusSrcAlpha);//10
	    material.SetFloat(k_Surface, 1);
	    material.SetFloat(zWritePropId, 0);
    }

    public double AlphaCutoff
    {
	    get => _material.GetFloat("_AlphaCutoff");
	    set => _material.SetFloat("_AlphaCutoff", (float) value);
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

    public Texture BaseColorTexture
    {
	    get => _material.GetTexture("_BaseColorTexture");
	    set => _material.SetTexture("_BaseColorTexture", value);
    }

    public int BaseColorTexCoord
    {
	    get => 0;
	    set {}
    }

    public Vector2 BaseColorXOffset
    {
	    get => _material.GetTextureOffset("_BaseColorTexture");
	    set => _material.SetTextureOffset("_BaseColorTexture", value);
    }

    public double BaseColorXRotation { get; set; }

    public Vector2 BaseColorXScale
    {
	    get => _material.GetTextureScale("_BaseColorTexture");
	    set => _material.SetTextureScale("_BaseColorTexture", value);
    }

    public int BaseColorXTexCoord
    {
	    get => 0;
	    set {}
    }

    public Color BaseColorFactor
    {
	    get => _material.GetColor("_BaseColorFactor");
	    set => _material.SetColor("_BaseColorFactor", value);
    }

    public Texture MetallicRoughnessTexture
    {
	    get => _material.GetTexture("_MetallicRoughnessTexture");
	    set => _material.SetTexture("_MetallicRoughnessTexture", value);
    }

    public int MetallicRoughnessTexCoord
    {
	    get => 0;
	    set {}
    }

    public Vector2 MetallicRoughnessXOffset
    {
	    get => _material.GetTextureOffset("_MetallicRoughnessTexture");
	    set => _material.SetTextureOffset("_MetallicRoughnessTexture", value);
    }

    public double MetallicRoughnessXRotation { get; set; }

    public Vector2 MetallicRoughnessXScale
    {
	    get => _material.GetTextureOffset("_MetallicRoughnessTexture");
	    set => _material.SetTextureOffset("_MetallicRoughnessTexture", value);
    }

    public int MetallicRoughnessXTexCoord
    {
	    get => 0;
	    set {}
    }

    public double MetallicFactor
    {
	    get => _material.GetFloat("_MetallicFactor");
	    set => _material.SetFloat("_MetallicFactor", (float) value);
    }

    public double RoughnessFactor
    {
	    get => _material.GetFloat("_RoughnessFactor");
	    set => _material.SetFloat("_RoughnessFactor", (float) value);
    }

    public double ThicknessFactor
    {
	    get => _material.GetFloat("_ThicknessFactor");
	    set
	    {
		    _material.SetFloat("_ThicknessFactor", (float) value);
	    }
    }

    public Texture ThicknessTexture
    {
	    get => _material.GetTexture("_ThicknessTexture");
	    set
	    {
		    _material.SetTexture("_ThicknessTexture", value);
	    }
    }

    public double AttenuationDistance
    {
	    get => _material.GetFloat("_AttenuationDistance");
	    set => _material.SetFloat("_AttenuationDistance", (float) value);
    }

    public Color AttenuationColor
    {
	    get => _material.GetColor("_AttenuationColor");
	    set => _material.SetColor("_AttenuationColor", value);
    }

    public double TransmissionFactor
    {
	    get => _material.GetFloat("_TransmissionFactor");
	    set
	    {
		    _material.SetFloat("_TransmissionFactor", (float) value);
	    }
    }

    public Texture TransmissionTexture
    {
	    get => _material.GetTexture("_TransmissionTexture");
	    set
	    {
		    _material.SetTexture("_TransmissionTexture", value);
	    }
    }

    public double IOR
    {
	    get => _material.GetFloat("_IOR");
	    set => _material.SetFloat("_IOR", (float) value);
    }

    public double IridescenceFactor
    {
	    get => _material.GetFloat("_IridescenceFactor");
	    set => _material.SetFloat("_IridescenceFactor", (float) value);
    }

    public double IridescenceIor
    {
	    get => _material.GetFloat("_IridescenceIor");
	    set => _material.SetFloat("_IridescenceIor", (float) value);
    }

    public double IridescenceThicknessMinimum
    {
	    get => _material.GetFloat("_IridescenceThicknessMinimum");
	    set => _material.SetFloat("_IridescenceThicknessMinimum", (float) value);
    }

    public double IridescenceThicknessMaximum
    {
	    get => _material.GetFloat("_IridescenceThicknessMaximum");
	    set => _material.SetFloat("_IridescenceThicknessMaximum", (float) value);
    }

    public Texture IridescenceTexture
    {
	    get => _material.GetTexture("_IridescenceTexture");
	    set
	    {
		    _material.SetTexture("_IridescenceTexture", value);
	    }
    }

    public Texture IridescenceThicknessTexture
    {
	    get => _material.GetTexture("_IridescenceThicknessTexture");
	    set
	    {
		    _material.SetTexture("_IridescenceThicknessTexture", value);
	    }
    }

    public double SpecularFactor
    {
	    get => _material.GetFloat("_SpecularFactor");
	    set => _material.SetFloat("_SpecularFactor", (float) value);
    }

    public Texture SpecularTexture
    {
	    get => _material.GetTexture("_SpecularTexture");
	    set
	    {
		    _material.SetTexture("_SpecularTexture", value);
	    }
    }

    public Color SpecularColorFactor
    {
	    get => _material.GetColor("_SpecularColorFactor");
	    set => _material.SetColor("_SpecularColorFactor", value);
    }

    public Texture SpecularColorTexture
    {
	    get => _material.GetTexture("_SpecularColorTexture");
	    set
	    {
		    _material.SetTexture("_SpecularColorTexture", value);
	    }
    }
}

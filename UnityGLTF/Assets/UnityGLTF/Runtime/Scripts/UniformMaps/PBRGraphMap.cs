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
			    _material.SetInt("_BUILTIN_SrcBlend", (int)BlendMode.One);
			    _material.SetInt("_BUILTIN_DstBlend", (int)BlendMode.Zero);
			    _material.SetInt("_ZWrite", 1);
			    _material.SetInt("_BUILTIN_ZWrite", 1);
			    _material.EnableKeyword("_ALPHATEST_ON");
			    _material.EnableKeyword("_BUILTIN_ALPHATEST_ON");
			    _material.DisableKeyword("_ALPHABLEND_ON");
			    _material.DisableKeyword("_BUILTIN_ALPHABLEND_ON");
			    _material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			    _material.DisableKeyword("_BUILTIN_ALPHAPREMULTIPLY_ON");
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
			    _material.SetInt("_BUILTIN_SrcBlend", (int)BlendMode.SrcAlpha);
			    _material.SetInt("_BUILTIN_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
			    _material.SetInt("_ZWrite", 0);
			    _material.SetInt("_BUILTIN_ZWrite", 0);
			    _material.DisableKeyword("_ALPHATEST_ON");
			    _material.DisableKeyword("_BUILTIN_ALPHATEST_ON");
			    _material.EnableKeyword("_ALPHABLEND_ON");
			    _material.EnableKeyword("_BUILTIN_ALPHABLEND_ON");
			    _material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			    _material.DisableKeyword("_BUILTIN_ALPHAPREMULTIPLY_ON");
			    _material.renderQueue = (int)RenderQueue.Transparent;
			    _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			    _material.EnableKeyword("_BUILTIN_SURFACE_TYPE_TRANSPARENT");

			    SetShaderModeBlend(_material);
		    }
		    else
		    {
			    _material.SetOverrideTag("RenderType", "Opaque");
			    _material.SetFloat("_Mode", 0);
			    _material.SetInt("_SrcBlend", (int)BlendMode.One);
			    _material.SetInt("_DstBlend", (int)BlendMode.Zero);
			    _material.SetInt("_BUILTIN_SrcBlend", (int)BlendMode.One);
			    _material.SetInt("_BUILTIN_DstBlend", (int)BlendMode.Zero);
			    _material.SetInt("_ZWrite", 1);
			    _material.SetInt("_BUILTIN_ZWrite", 1);
			    _material.DisableKeyword("_ALPHATEST_ON");
			    _material.DisableKeyword("_ALPHABLEND_ON");
			    _material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			    _material.DisableKeyword("_BUILTIN_ALPHATEST_ON");
			    _material.DisableKeyword("_BUILTIN_ALPHABLEND_ON");
			    _material.DisableKeyword("_BUILTIN_ALPHAPREMULTIPLY_ON");
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
        material.EnableKeyword(KW_ALPHATEST_ON_BUILTIN);
        material.SetOverrideTag(TAG_RENDER_TYPE, TAG_RENDER_TYPE_CUTOUT);
        material.SetFloat(k_ZTestGBufferPropId, (int)CompareFunction.Equal); //3
// #endif
	    material.SetFloat(k_AlphaClip, 1);
	    material.SetFloat(k_AlphaClipBuiltin, 1);
	    if (isMask) material.EnableKeyword(KW_ALPHACLIP_ON_BUILTIN);
	    else material.DisableKeyword(KW_ALPHACLIP_ON_BUILTIN);
    }

    static readonly int cullPropId = Shader.PropertyToID("_Cull");
    static readonly int cullModePropId = Shader.PropertyToID("_CullMode");
    static readonly int k_AlphaClip = Shader.PropertyToID("_AlphaClip");
    static readonly int k_AlphaClipBuiltin = Shader.PropertyToID("_BUILTIN_AlphaClip");
    static readonly int k_Surface = Shader.PropertyToID("_Surface");
    static readonly int k_SurfaceBuiltin = Shader.PropertyToID("_BUILTIN_Surface");
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
    const string KW_ALPHATEST_ON_BUILTIN = "_BUILTIN_ALPHATEST_ON";
    const string KW_ALPHACLIP_ON_BUILTIN = "_BUILTIN_AlphaClip";
    const string KW_DISABLE_SSR_TRANSPARENT = "_DISABLE_SSR_TRANSPARENT";
    const string KW_ENABLE_FOG_ON_TRANSPARENT = "_ENABLE_FOG_ON_TRANSPARENT";
    const string KW_SURFACE_TYPE_TRANSPARENT = "_SURFACE_TYPE_TRANSPARENT";
    const string KW_SURFACE_TYPE_TRANSPARENT_BUILTIN = "_BUILTIN_SURFACE_TYPE_TRANSPARENT";
    const string k_ShaderPassTransparentDepthPrepass = "TransparentDepthPrepass";
    const string k_ShaderPassTransparentDepthPostpass = "TransparentDepthPostpass";
    const string k_ShaderPassTransparentBackface = "TransparentBackface";
    const string k_ShaderPassRayTracingPrepass = "RayTracingPrepass";
    const string k_ShaderPassDepthOnlyPass = "DepthOnly";

    protected void SetShaderModeBlend(Material material)
    {
	    material.SetOverrideTag(TAG_RENDER_TYPE, TAG_RENDER_TYPE_TRANSPARENT);
	    material.EnableKeyword(KW_SURFACE_TYPE_TRANSPARENT);
	    material.EnableKeyword(KW_SURFACE_TYPE_TRANSPARENT_BUILTIN);
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
	    material.SetFloat(k_SurfaceBuiltin, 1);
	    material.SetFloat(zWritePropId, 0);
    }

    public double AlphaCutoff
    {
	    get => _material.GetFloat("alphaCutoff");
	    set => _material.SetFloat("alphaCutoff", (float) value);
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
	    get { return _material.IsKeywordEnabled("_VERTEX_COLORS_ON"); }
	    set
	    {
		    if (value)
			    _material.EnableKeyword("_VERTEX_COLORS_ON");
		    else
			    _material.DisableKeyword("_VERTEX_COLORS_ON");
	    }
    }

    public Texture BaseColorTexture
    {
	    get => _material.GetTexture("baseColorTexture");
	    set => _material.SetTexture("baseColorTexture", value);
    }

    public int BaseColorTexCoord
    {
	    get => (int) _material.GetFloat("baseColorTextureTexCoord");
	    set => _material.SetFloat("baseColorTextureTexCoord", Mathf.RoundToInt(value));
    }

    public Vector2 BaseColorXOffset
    {
	    get => _material.GetTextureOffset("baseColorTexture");
	    set => _material.SetTextureOffset("baseColorTexture", value);
    }

    public double BaseColorXRotation
    {
	    get => _material.GetFloat("baseColorTextureRotation");
	    set => _material.SetFloat("baseColorTextureRotation", (float) value);
    }

    public Vector2 BaseColorXScale
    {
	    get => _material.GetTextureScale("baseColorTexture");
	    set => _material.SetTextureScale("baseColorTexture", value);
    }

    public int BaseColorXTexCoord
    {
	    get => (int) _material.GetFloat("baseColorTextureTexCoord");
	    set => _material.SetFloat("baseColorTextureTexCoord", Mathf.RoundToInt(value));
    }

    public Color BaseColorFactor
    {
	    get => _material.GetColor("baseColorFactor");
	    set => _material.SetColor("baseColorFactor", value);
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
}

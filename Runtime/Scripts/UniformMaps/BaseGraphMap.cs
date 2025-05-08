using GLTF.Schema;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityGLTF
{
	public abstract class BaseGraphMap : IUniformMap
	{
		public abstract IUniformMap Clone();

		protected BaseGraphMap(Material mat)
		{
			_material = mat;
		}

		protected BaseGraphMap(string shaderName, string fallbackGuid)
		{
			var s = Shader.Find(shaderName);

#if UNITY_EDITOR
			// workaround for first-import issues with Shader.Find and import order
			if (!s)
				s = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(UnityEditor.AssetDatabase.GUIDToAssetPath(fallbackGuid));
#endif

			if (!s)
				throw new ShaderNotFoundException(shaderName + " not found. Did you forget to add it to the build? For more information, see: https://github.com/KhronosGroup/UnityGLTF?tab=readme-ov-file#ensure-shaders-are-available-in-your-build");

			_material = new Material(s);
		}

		public Material Material => _material;
		internal Material _material;

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
				    _material.SetInt("_AlphaClip", 1);
				    _material.SetInt("_BUILTIN_AlphaClip", 1);
				    _material.EnableKeyword("_ALPHATEST_ON");
				    _material.EnableKeyword("_BUILTIN_ALPHATEST_ON");
				    _material.DisableKeyword("_ALPHABLEND_ON");
				    _material.DisableKeyword("_BUILTIN_ALPHABLEND_ON");
				    _material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				    _material.DisableKeyword("_BUILTIN_ALPHAPREMULTIPLY_ON");
				    _material.renderQueue = (int)RenderQueue.AlphaTest;
				    if (_material.HasProperty("_Cutoff"))
					    _material.SetFloat("_Cutoff", (float)AlphaCutoff);
				    if (_material.HasProperty("alphaCutoff"))
					    _material.SetFloat("alphaCutoff", (float)AlphaCutoff);

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
				    _material.SetInt("_AlphaClip", 0);
				    _material.SetInt("_BUILTIN_AlphaClip", 0);
				    _material.DisableKeyword("_ALPHATEST_ON");
				    _material.DisableKeyword("_BUILTIN_ALPHATEST_ON");
				    _material.EnableKeyword("_ALPHABLEND_ON");
				    _material.EnableKeyword("_BUILTIN_ALPHABLEND_ON");
				    _material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				    _material.DisableKeyword("_BUILTIN_ALPHAPREMULTIPLY_ON");
				    _material.renderQueue = (int)RenderQueue.Transparent;
				    _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
				    _material.EnableKeyword("_BUILTIN_SURFACE_TYPE_TRANSPARENT");
				    _material.SetFloat("_BlendModePreserveSpecular", 0);
			
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
				    _material.SetInt("_AlphaClip", 0);
				    _material.SetInt("_BUILTIN_AlphaClip", 0);
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

	    protected void SetDoubleSided (Material material, bool doubleSided)
	    {
		    material.doubleSidedGI = doubleSided;
		    var mode = doubleSided ? (int)CullMode.Off : (int)CullMode.Back;
		    material.SetFloat(cullPropId, mode);
		    material.SetFloat(cullModePropId, mode);
		    material.SetFloat(cullModePropIdBuiltin, mode);
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
		    material.SetFloat(alphaToMask, isMask ? 1 : 0);
	    }

	    static readonly int cullPropId = Shader.PropertyToID("_Cull");
	    static readonly int cullModePropId = Shader.PropertyToID("_CullMode");
	    static readonly int cullModePropIdBuiltin = Shader.PropertyToID("_BUILTIN_CullMode");
	    static readonly int k_AlphaClip = Shader.PropertyToID("_AlphaClip");
	    static readonly int k_AlphaClipBuiltin = Shader.PropertyToID("_BUILTIN_AlphaClip");
	    static readonly int k_Surface = Shader.PropertyToID("_Surface");
	    static readonly int k_SurfaceBuiltin = Shader.PropertyToID("_BUILTIN_Surface");
	    static readonly int k_AlphaDstBlendPropId = Shader.PropertyToID("_AlphaDstBlend");
	    static readonly int k_ZTestGBufferPropId = Shader.PropertyToID("_ZTestGBuffer");
	    static readonly int srcBlendPropId = Shader.PropertyToID("_SrcBlend");
	    static readonly int dstBlendPropId = Shader.PropertyToID("_DstBlend");
	    static readonly int zWritePropId = Shader.PropertyToID("_ZWrite");
	    static readonly int alphaToMask = Shader.PropertyToID("_AlphaToMask");
	    
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
		    material.SetFloat(alphaToMask, 0);
	    }

	    public double AlphaCutoff
	    {
		    get => _material.GetFloat("alphaCutoff");
		    set
		    {
				_material.SetFloat("alphaCutoff", (float) value);
#if !UNITY_2021_2_OR_NEWER
			    // PBRGraph/UnlitGraph always have alphaCutoff on 2020.x, so we need to set it to 0 for non-masked modes
				if (_alphaMode != AlphaMode.MASK)
				    _material.SetFloat("alphaCutoff", 0f);
#endif
		    }
	    }

	    public virtual bool DoubleSided
	    {
		    get { return
#if UNITY_2019_3_OR_NEWER
			    !GraphicsSettings.currentRenderPipeline ?
			    _material.GetInt(cullModePropIdBuiltin) == (int)CullMode.Off :
#endif
				_material.GetInt(cullPropId) == (int)CullMode.Off; }
		    set { SetDoubleSided(_material, value); }
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
	}
}

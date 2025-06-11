#ifndef UNITY_DECLARE_OPAQUE_TEXTURE_INCLUDED_2
#define UNITY_DECLARE_OPAQUE_TEXTURE_INCLUDED_2
#ifdef SHADERGRAPH_PREVIEW

void SampleSceneColor2_float(float2 uv, float lod, out float3 color)
{
    color = float3(1,1,1);
}

void SampleSceneColor2_half(half2 uv, half lod, out half3 color)
{
	color = half3(1,1,1);
}

#else
// Needed to comment this out due to IN-4055 - can't build with this include being present here
// For code changes, comment this out so autocomplete etc. work, but remember to comment it again
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// TODO figure out how we can differentiate between BiRP/URP/HDRP here
#define USE_CAMERA_OPAQUE

#if defined(USE_CAMERA_OPAQUE)
float4 _CameraOpaqueTexture_TexelSize;
// float _CameraOpaqueSampling_BlurShift;
TEXTURE2D_X(_CameraOpaqueTexture);
SAMPLER(sampler_CameraOpaqueTexture);
#endif

// for future use
// float applyIorToRoughness( const in float roughness, const in float ior ) {
//     // Scale roughness with IOR so that an IOR of 1.0 results in no microfacet refraction and
//     // an IOR of 1.5 results in the default amount of microfacet refraction.
//     return roughness * clamp( ior * 2.0 - 2.0, 0.0, 1.0 );
// }

/*
float3 Sample4Tap(float2 uv, float lod)
{
	// bilinear GPU filtering doesn't look good enough;
	// here we're doing 4 taps and blending them.
	float3 c0 = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UnityStereoTransformScreenSpaceTex(uv), lod).rgb;
	float3 c1 = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UnityStereoTransformScreenSpaceTex(uv + float2(_CameraOpaqueTexture_TexelSize.x * _CameraOpaqueSampling_BlurShift, 0)), 0).rgb;
	float3 c2 = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UnityStereoTransformScreenSpaceTex(uv + float2(0, _CameraOpaqueTexture_TexelSize.y * _CameraOpaqueSampling_BlurShift)), 0).rgb;
	float3 c3 = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UnityStereoTransformScreenSpaceTex(uv + float2(-_CameraOpaqueTexture_TexelSize.x * _CameraOpaqueSampling_BlurShift, 0)), 0).rgb;
	float3 c4 = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UnityStereoTransformScreenSpaceTex(uv + float2(0, -_CameraOpaqueTexture_TexelSize.y * _CameraOpaqueSampling_BlurShift)), 0).rgb;
	
	return (c0 + c1 + c2 + c3 + c4) / 5.0;
}
*/

#ifndef  UnityStereoTransformScreenSpaceTex
float2 UnityStereoTransformScreenSpaceTex(float2 uv)
{
	#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED)
	uv.x = uv.x * 0.5 + 0.5 * unity_StereoEyeIndex;
	#endif
	return uv;
}
#endif

void SampleSceneColor_float(float2 uv, float lod, out float3 color)
{
	#if !UNITY_UV_STARTS_AT_TOP
	if (_CameraOpaqueTexture_TexelSize.y > 0)
		uv.y = 1-uv.y;
	#endif
	
	#define REQUIRE_OPAQUE_TEXTURE // seems we need to define this ourselves? HDSceneColorNode does that as well

#if defined(USE_CAMERA_OPAQUE)
	color = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UnityStereoTransformScreenSpaceTex(uv), lod).rgb;
	// color = Sample4Tap(uv, lod); // TODO higher quality refraction
#else
	// For HDRP, from HDSceneColorNode
	#if defined(REQUIRE_OPAQUE_TEXTURE) && defined(_SURFACE_TYPE_TRANSPARENT) && defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT) && (SHADERPASS != SHADERPASS_PATH_TRACING) && (SHADERPASS != SHADERPASS_RAYTRACING_VISIBILITY) && (SHADERPASS != SHADERPASS_RAYTRACING_FORWARD)
	color = SampleCameraColor(uv, lod) * 1.0; // GetInverseCurrentExposureMultiplier()
	#else
	color = float3(0.0, 0.0, 0.0);
	#endif
#endif
}

void SampleSceneColor_half(float2 uv, float lod, out float3 color)
{
	SampleSceneColor_float(uv, lod, color);
}

#endif

void Refract_float(float3 v, float3 n, float ior, out float3 refractionVector)
{
    refractionVector = refract( - v, normalize( n ), 1 / ior );
}

void Refract_half(half3 v, half3 n, half ior, out half3 refractionVector)
{
	refractionVector = refract( - v, normalize( n ), 1 / ior );
}

inline half3 bump3y (half3 x, half3 yoffset)
{
	float3 y = 1 - x * x;
	y = saturate(y-yoffset);
	return y;
}

void Spectral_float(float wavelength, out half3 color)
{
	// Based on GPU Gems
	// Optimised by Alan Zucconi
	// w: [400, 700]
	// x: [0,   1]
	half x = ((wavelength - 400.0)/ 300.0) % 1.0;

	const float3 c1 = float3(3.54585104, 2.93225262, 2.41593945);
	const float3 x1 = float3(0.69549072, 0.49228336, 0.27699880);
	const float3 y1 = float3(0.02312639, 0.15225084, 0.52607955);

	const float3 c2 = float3(3.90307140, 3.21182957, 3.96587128);
	const float3 x2 = float3(0.11748627, 0.86755042, 0.66077860);
	const float3 y2 = float3(0.84897130, 0.88445281, 0.73949448);

	color =
		bump3y(c1 * (x - x1), y1) +
		bump3y(c2 * (x - x2), y2) ;
}

#endif

void WorkaroundTilingOffset_float(UnityTexture2D Tex, float4 LegacyST, out float4 TilingOffset, out UnityTexture2D OutTex)
{
	#if UNITY_VERSION >= 202120
	TilingOffset = Tex.scaleTranslate;
	Tex.scaleTranslate = float4(1,1,0,0);
	#else
	TilingOffset = LegacyST;
	#endif
	OutTex = Tex;
}

void WorkaroundTilingOffset_half(UnityTexture2D Tex, half4 LegacyST, out half4 TilingOffset, out UnityTexture2D OutTex)
{
	#if UNITY_VERSION >= 202120
	TilingOffset = Tex.scaleTranslate;
	Tex.scaleTranslate = half4(1,1,0,0);
	#else
	TilingOffset = LegacyST;
	#endif
	OutTex = Tex;
}

void AreNormalsEncodedInXYZ_half(out bool normalsInXYZ)
{
	#if	UNITY_NO_DXT5nm
	normalsInXYZ = true;
 	#else
	normalsInXYZ = false;
	#endif
}

void AreNormalsEncodedInXYZ_float(out bool normalsInXYZ)
{
	#if	UNITY_NO_DXT5nm
	normalsInXYZ = true;
	#else
	normalsInXYZ = false;
	#endif
}
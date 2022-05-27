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
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

float4 _CameraOpaqueTexture_TexelSize;
TEXTURE2D_X(_CameraOpaqueTexture);
SAMPLER(sampler_CameraOpaqueTexture);

float applyIorToRoughness( const in float roughness, const in float ior ) {
    // Scale roughness with IOR so that an IOR of 1.0 results in no microfacet refraction and
    // an IOR of 1.5 results in the default amount of microfacet refraction.
    return roughness * clamp( ior * 2.0 - 2.0, 0.0, 1.0 );
}

void SampleSceneColor_float(float2 uv, float lod, out float3 color)
{
    // float framebufferLod = log2( 2048 ) * applyIorToRoughness( roughness, ior );
    color = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UnityStereoTransformScreenSpaceTex(uv), lod).rgb;
}

void SampleSceneColor_half(float2 uv, float lod, out float3 color)
{
	// float framebufferLod = log2( 2048 ) * applyIorToRoughness( roughness, ior );
	color = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UnityStereoTransformScreenSpaceTex(uv), lod).rgb;
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

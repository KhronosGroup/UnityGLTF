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

#endif

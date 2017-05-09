Shader "GLTF/GLTFMobileTransparentMaskDoubleSided"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Metallic("Metallic", Range(0,1)) = 0.0
		_Roughness("Roughness", Range(0,1)) = 0.5
		_MetallicRoughnessMap("Metallic Roughness", 2D) = "black" {}
		
		_OcclusionMap("Occlusion", 2D) = "white" {}
		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0

		_EmissionColor("Color", Color) = (1,1,1,0)
		_EmissionMap("Emission", 2D) = "black" {}
	}
	SubShader
	{
		Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
		LOD 100
		Cull Off

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }
			CGPROGRAM

			#define DOUBLESIDED_ON
			#define ALPHA_MASK_ON
            #include "GLTFMobileCommon.cginc"
			
			#pragma vertex gltf_mobile_vert
			#pragma fragment gltf_mobile_frag			
			
			ENDCG
		}
	}
}
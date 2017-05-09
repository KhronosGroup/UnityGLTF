Shader "GLTF/GLTFStandardTransparentMaskDoubleSided" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Metallic("Metallic", Range(0,1)) = 0.0
		_Roughness("Roughness", Range(0,1)) = 0.5
		_MetallicRoughnessMap("Metallic Roughness", 2D) = "black" {}

        _BumpScale("Scale", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}
		
		_OcclusionMap("Occlusion", 2D) = "white" {}
		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0

		_EmissionColor("Color", Color) = (1,1,1,0)
		_EmissionMap("Emission", 2D) = "black" {}
	}
	SubShader {
		Tags {"Queue" = "Transparent" "RenderType"="Transparent" }
		LOD 200
		Cull Off
		
		CGPROGRAM
		
		#define ALPHA_ON
		#pragma target 3.0
		#pragma multi_compile _ VERTEX_COLOR_ON
		#include "GLTFStandardCommon.cginc"
		#pragma surface gltf_standard_surf Standard fullforwardshadows alphatest:_Cutoff

		ENDCG
	}
	FallBack "Standard"
}

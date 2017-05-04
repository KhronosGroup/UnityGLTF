Shader "GLTF/GLTFStandardTransparentBlend" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_MetallicRoughness("Metallic Roughness", 2D) = "black" {}
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Roughness("Roughness", Range(0,1)) = 0.5
		_AOTex("Ambient Occlusion", 2D) = "white" {}
		_Occlusion("Occlusion Strength", Range(0,1)) = 1
		_EmissionTex("Emission Texture", 2D) = "black" {}
		_Emission("Emission", Color) = (1,1,1,0)
		_BumpMap("Bump Map", 2D) = "bump" {}
		_Bump("Bump", Range(0,1)) = 1
	}
	SubShader {
		Tags {"Queue" = "Transparent" "RenderType"="Transparent" }
		LOD 200
		
		CGPROGRAM
		
		#include "GLTFStandardCommon.cginc"
		#pragma surface gltf_standard_surf Standard fullforwardshadows alpha:blend

		ENDCG
	}
	FallBack "Standard"
}

Shader "GLTF/GLTFStandard" {
	
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}

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
		Tags { "RenderType"="Opaque" }
		Cull [_Cull]
		LOD 300
		
		CGPROGRAM
		// Standard Desktop Shader
		#pragma target 3.0
		// Vertex Colors
		#pragma multi_compile _ VERTEX_COLOR_ON
		// Occlusion packed in red channel of MetallicRoughnessMap
		#pragma multi_compile _ OCC_METAL_ROUGH_ON
		#include "GLTFStandardCommon.cginc"
		
		#pragma surface gltf_standard_surf Standard fullforwardshadows

		ENDCG
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		Cull [_Cull]
		LOD 200
		
		Pass {
			CGPROGRAM
			// Mobile Shader
			#pragma target 2.0
			// Vertex Colors
			#pragma multi_compile _ VERTEX_COLOR_ON
			// Occlusion packed in red channel of MetallicRoughnessMap
			#pragma multi_compile _ OCC_METAL_ROUGH_ON
			#include "GLTFMobileCommon.cginc"
			#pragma vertex gltf_mobile_vert
			#pragma fragment gltf_mobile_frag
			ENDCG
		}
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		Cull [_Cull]
		LOD 100

		Pass {
			CGPROGRAM
			// Vertex Lit Shader
			#pragma target 2.0
			// Vertex Colors
			#pragma multi_compile _ VERTEX_COLOR_ON
			// Occlusion packed in red channel of MetallicRoughnessMap
			#pragma multi_compile _ OCC_METAL_ROUGH_ON
			#pragma multi_compile_fwdbase	
			#pragma multi_compile_fog
			#include "GLTFVertexLitCommon.cginc"
			#pragma vertex gltf_vertex_lit_vert
			#pragma fragment gltf_vertex_lit_frag
			ENDCG
		}
	}
}

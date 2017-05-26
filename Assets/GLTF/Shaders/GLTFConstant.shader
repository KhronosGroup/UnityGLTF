Shader "GLTF/GLTFConstant" {
	Properties {
		_AmbientFactor ("Ambient Factor", Color) = (1,1,1,1)
		_EmissionFactor("Emission Factor", Color) = (1,1,1,1)
		_LightmapFactor("Lightmap Factor", Color) = (1,1,1,1)
		_MainTex ("Emission (RGB)", 2D) = "white" {}
		_LightmapTex("Lightmap (RGB)", 2D) = "white" {}
		_EmissionUV ("Emission UV Index", Int) = 0
		_LightmapUV("Lightmap UV Index", Int) = 0
	}
	SubShader {
		Pass {
			Tags { "RenderType" = "Opaque" }
			LOD 200
			Cull Back
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ USE_MAINMAP USE_LIGHTMAP
			#include "GLTFConstant.cginc"
			ENDCG
		}
	}
}

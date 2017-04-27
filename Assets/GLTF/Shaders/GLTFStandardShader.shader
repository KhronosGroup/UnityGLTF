Shader "GLTF/GLTFStandardShader" {
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
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _MetallicRoughness;
		sampler2D _AOTex;
		sampler2D _EmissionTex;
		sampler2D _BumpMap;

		struct Input {
			float2 uv_MainTex;
		};

		fixed4 _Color;
		half _Metallic;
		half _Roughness;
		half _Occlusion;
		fixed3 _Emission;
		half _Bump;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_CBUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_CBUFFER_END

		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			fixed4 mr = tex2D(_MetallicRoughness, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			o.Metallic = mr.b * _Metallic;
			o.Smoothness = mr.g * (1 - _Roughness);
			o.Occlusion = tex2D(_AOTex, IN.uv_MainTex).r * _Occlusion;
			o.Emission = tex2D(_EmissionTex, IN.uv_MainTex).rgb * _Emission;
			o.Normal = normalize((normalize(tex2D(_BumpMap, IN.uv_MainTex)) * 2.0 - 1.0) * fixed3(_Bump, _Bump, 1.0));
		}
		ENDCG
	}
	FallBack "Diffuse"
}

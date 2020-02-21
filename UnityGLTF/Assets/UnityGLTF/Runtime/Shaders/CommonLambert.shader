Shader "GLTF/Lambert"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
		_Ambient ("Ambient", Color) = (0,0,0,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_EmissionColor ("Emission", Color) = (0,0,0,1)
		_LightColor ("Light Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
		#pragma surface surf GLTFLambert

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

		fixed4 _Color;
		fixed4 _Ambient;
		fixed4 _EmissionColor;
		fixed4 _LightColor;

		half4 LightingGLTFLambert (SurfaceOutput s, half3 lightDir, half atten) {
			half NdotL = dot(s.Normal, lightDir);
			half4 c;
			c.rgb = (_EmissionColor.rgb + _Ambient.rgb * _LightColor.rgb + s.Albedo * _LightColor.rgb * NdotL) * atten;
			c.a = s.Alpha;
			return c;
		}

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

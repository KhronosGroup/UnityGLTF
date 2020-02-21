Shader "GLTF/Constant"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
		_EmissionColor("Emission", Color) = (0,0,0,1)
		_Ambient ("Ambient", Color) = (0,0,0,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_LightColor ("Light Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
		#pragma surface surf GLTFConstant

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

		fixed4 _Color;
		fixed4 _EmissionColor;
		fixed4 _Ambient;
		fixed4 _LightColor;

		half4 LightingGLTFConstant(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
			half4 c;
			c.rgb = (_EmissionColor.rgb + _Ambient.rgb * _LightColor.rgb +  s.Albedo) * atten;
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

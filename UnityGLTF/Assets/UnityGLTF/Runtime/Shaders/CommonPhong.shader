Shader "GLTF/Phong"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
		_Ambient ("Ambient", Color) = (0,0,0,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_EmissionColor ("Emission", Color) = (0,0,0,1)
		_SpecularColor ("Specular", Color) = (0,0,0,1)
		_Shininess ("Shininess", Range(0,1)) = 0.0
		_LightColor ("Light Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
		#pragma surface surf GLTFPhong

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

		fixed4 _Color;
		fixed4 _Ambient;
		fixed4 _EmissionColor;
		fixed4 _SpecularColor;
		fixed _Shininess;
		fixed4 _LightColor;

		half4 LightingGLTFPhong(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
			half3 h = normalize(lightDir + viewDir);

			half diff = max(0, dot(s.Normal, lightDir));

			float nh = max(0, dot(s.Normal, h));
			float spec = pow(nh, _Shininess * 48.0);

			half4 c;
			c.rgb = (_EmissionColor.rgb + _Ambient.rgb * _LightColor.rgb +  s.Albedo * _LightColor.rgb * diff + _LightColor.rgb * spec) * atten;
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

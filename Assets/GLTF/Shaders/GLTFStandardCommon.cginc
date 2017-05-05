#ifndef GLTF_STANDARD_COMMON_INCLUDED
#define GLTF_STANDARD_COMMON_INCLUDED

sampler2D _MainTex;
sampler2D _MetallicRoughness;
sampler2D _AOTex;
sampler2D _EmissionTex;
sampler2D _BumpMap;

struct Input {
    float2 uv_MainTex;

    #ifdef VERTEX_COLOR_ON
    float4 vertColor : COLOR;
    #endif
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

void gltf_standard_surf (Input IN, inout SurfaceOutputStandard o) {
    #ifdef VERTEX_COLOR_ON
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color * IN.vertColor;
    #else
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
    #endif

    fixed4 mr = tex2D(_MetallicRoughness, IN.uv_MainTex);
    o.Albedo = c.rgb;
    #ifdef ALPHA_ON
    o.Alpha = c.a;
    #endif
    o.Metallic = mr.b * _Metallic;
    o.Smoothness = mr.g * (1 - _Roughness);
    o.Occlusion = tex2D(_AOTex, IN.uv_MainTex).r * _Occlusion;
    o.Emission = tex2D(_EmissionTex, IN.uv_MainTex).rgb * _Emission;
    o.Normal = normalize((normalize(tex2D(_BumpMap, IN.uv_MainTex)) * 2.0 - 1.0) * fixed3(_Bump, _Bump, 1.0));
}

#endif // GLTF_STANDARD_COMMON_INCLUDED
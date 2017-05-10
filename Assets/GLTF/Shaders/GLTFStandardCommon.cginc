#ifndef GLTF_STANDARD_COMMON_INCLUDED
#define GLTF_STANDARD_COMMON_INCLUDED

fixed4 _Color;
sampler2D _MainTex;

half _Metallic;
half _Roughness;
sampler2D _MetallicRoughnessMap;

half _BumpScale;
sampler2D _BumpMap;

half _OcclusionStrength;

#ifndef OCC_METAL_ROUGH_ON
sampler2D _OcclusionMap;
#endif

fixed3 _EmissionColor;
sampler2D _EmissionMap;

struct Input {
    float2 uv_MainTex;

    #ifdef VERTEX_COLOR_ON
    float4 vertColor : COLOR;
    #endif
};

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

    fixed4 mr = tex2D(_MetallicRoughnessMap, IN.uv_MainTex);
    o.Albedo = c.rgb;
    #if defined(ALPHA_MODE_MASK_ON) || defined(ALPHA_MODE_BLEND_ON)
    o.Alpha = c.a;
    #endif
    o.Metallic = mr.b * _Metallic;
    o.Smoothness = mr.g * (1 - _Roughness);

    #ifdef OCC_METAL_ROUGH_ON
    o.Occlusion = mr.r * _OcclusionStrength;
    #else
    o.Occlusion = tex2D(_OcclusionMap, IN.uv_MainTex).r * _OcclusionStrength;
    #endif

    o.Emission = tex2D(_EmissionMap, IN.uv_MainTex).rgb * _EmissionColor;
    o.Normal = normalize((normalize(tex2D(_BumpMap, IN.uv_MainTex)) * 2.0 - 1.0) * fixed3(_BumpScale, _BumpScale, 1.0));
}

#endif // GLTF_STANDARD_COMMON_INCLUDED
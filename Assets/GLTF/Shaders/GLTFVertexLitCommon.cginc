#ifndef GLTF_VERTEX_LIT_COMMON_INCLUDED
#define GLTF_VERTEX_LIT_COMMON_INCLUDED
#include "HLSLSupport.cginc"
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

inline float3 LightingLambertVS(float3 normal, float3 lightDir)
{
	fixed diff = max(0, dot(normal, lightDir));
	return _LightColor0.rgb * diff;
}

struct v2f {
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	fixed3 vlight : TEXCOORD2;
	LIGHTING_COORDS(3,4)
	UNITY_FOG_COORDS(5)
};

#ifdef ALPHA_MODE_MASK_ON
half _Cutoff;
#endif

float4 _MainTex_ST;
sampler2D _MainTex;
fixed4 _Color;
half _OcclusionStrength;
#ifdef OCC_METAL_ROUGH_ON
sampler2D _MetallicRoughnessMap;
#else
sampler2D _OcclusionMap;
#endif
fixed4 _EmissionColor;
sampler2D _EmissionMap;


v2f gltf_vertex_lit_vert(appdata_full v)
{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
	float3 worldN = UnityObjectToWorldNormal(v.normal);
	o.vlight = ShadeSH9(float4(worldN,1.0));
	o.vlight += LightingLambertVS(worldN, _WorldSpaceLightPos0.xyz);
	TRANSFER_VERTEX_TO_FRAGMENT(o);
	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

fixed4 gltf_vertex_lit_frag(v2f i) : SV_Target
{
	half4 albedo = tex2D(_MainTex, i.uv) * _Color;
	fixed4 mainColor = fixed4(albedo.rgb * i.vlight, albedo.a);

	UNITY_APPLY_FOG(i.fogCoord, mainColor);

	#ifdef ALPHA_MODE_MASK_ON
	clip(mainColor.a  - _Cutoff);
	#endif

	#ifdef OCC_METAL_ROUGH_ON
    fixed4 occlusion = tex2D(_MetallicRoughnessMap, i.uv).r * _OcclusionStrength;
    #else
    fixed4 occlusion = tex2D(_OcclusionMap, i.uv).r * _OcclusionStrength;
    #endif

	fixed4 emission = tex2D(_EmissionMap, i.uv) * _EmissionColor;

	return mainColor * fixed4(occlusion.rgb, 1.0) + fixed4(emission.rgb, 0.0);
}
#endif

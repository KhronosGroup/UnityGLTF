#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "UnityStandardUtils.cginc"

struct VertexInput
{
	float4 vertex : POSITION;
	half3 normal : NORMAL;
	half4 tangent : TANGENT;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Vert2Frag
{
	UNITY_POSITION(position);
	float3 tangentToWorld[3] : TEXCOORD0;

	UNITY_FOG_COORDS(1)
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

Vert2Frag vertBase(VertexInput v)
{
	UNITY_SETUP_INSTANCE_ID(v);
	Vert2Frag o;
	UNITY_TRANSFER_INSTANCE_ID(v, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	o.position = UnityObjectToClipPos(v.vertex);

	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
	float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
	float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
	o.tangentToWorld[0].xyz = tangentToWorld[0];
	o.tangentToWorld[1].xyz = tangentToWorld[1];
	o.tangentToWorld[2].xyz = tangentToWorld[2];

	UNITY_TRANSFER_FOG(o, o.position);

	return o;
}

half4 fragBase(Vert2Frag i) : SV_TARGET
{
	UNITY_APPLY_DITHER_CROSSFADE(i.position.xy);
	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	UnityLight mainLight = MainLight();
	UNITY_LIGHT_ATTENUATION
	return half4(1, 1, 1, 1);
}

#include "UnityCG.cginc"

uniform fixed4 _AmbientFactor;
uniform fixed4 _EmissionFactor;
uniform fixed4 _LightmapFactor;

#ifdef USE_MAINMAP
	uniform sampler2D _MainTex;
	uniform float4 _MainTex_ST;
	uniform half _EmissionUV;
#endif
#ifdef USE_LIGHTMAP
	uniform sampler2D _LightmapTex;
	uniform float4 _LightmapTex_ST;
	uniform half _LightmapUV;
#endif

struct vertexInput {
	float4 vertex : POSITION;
	fixed4 color : COLOR;
	float2 uv0 : TEXCOORD0;
	float2 uv1 : TEXCOORD1;
	float2 uv2 : TEXCOORD2;
	float2 uv3 : TEXCOORD3;
};
struct vertexOutput {
	float4 pos : SV_POSITION;
	fixed4 color : COLOR;
	float2 emissionCoord : TEXCOORD0;
	float2 lightmapCoord : TEXCOORD1;
};

vertexOutput vert(vertexInput input)
{
	vertexOutput output;

#ifdef USE_MAINMAP
	float2 emissionCoord;
	switch (_EmissionUV) {
	case 0: emissionCoord = input.uv0; break;
	case 1: emissionCoord = input.uv1; break;
	case 2: emissionCoord = input.uv2; break;
	case 3: emissionCoord = input.uv3; break;
	default: emissionCoord = input.uv0; break;
	}
	output.emissionCoord = TRANSFORM_TEX(emissionCoord, _MainTex);
#endif

#ifdef USE_LIGHTMAP
	float2 lightmapCoord;
	switch (_LightmapUV) {
	case 0: lightmapCoord = input.uv0; break;
	case 1: lightmapCoord = input.uv1; break;
	case 2: lightmapCoord = input.uv2; break;
	case 3: lightmapCoord = input.uv3; break;
	default: lightmapCoord = input.uv0; break;
	}
	output.lightmapCoord = TRANSFORM_TEX(lightmapCoord, _LightTex);
#endif

	output.pos = UnityObjectToClipPos(input.vertex);
	output.color = (fixed4) input.color;
	return output;
}

fixed4 frag(vertexOutput input) : COLOR
{
	fixed4 finalColor = input.color;

#ifdef USE_MAINMAP
	finalColor = finalColor * _EmissionFactor * tex2D(_MainTex, input.emissionCoord);
#endif

#ifdef USE_LIGHTMAP
	// mix(textureColor, lightColor*textureColor, _LightmapFactor)
	fixed4 lightColor = tex2D(_LightTex, input.lightmapCoord) * finalColor;
	finalColor = (fixed4(1) - _LightmapFactor) * finalColor + _LightmapFactor * lightColor;
#endif

	fixed4 ambient = unity_AmbientSky * _AmbientFactor, 1.0;

	return ambient + finalColor;
}

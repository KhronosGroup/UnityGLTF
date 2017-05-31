#include "UnityCG.cginc"

uniform fixed4 _AmbientFactor;
uniform fixed4 _EmissionFactor;
uniform fixed4 _LightFactor;

uniform sampler2D _EmissionMap;
uniform float4 _EmissionMap_ST;
uniform half _EmissionUV;

uniform half _Cutoff;

#ifdef LIGHTMAP_ON
	uniform sampler2D _LightMap;
	uniform float4 _LightMap_ST;
	uniform half _LightUV;
#endif

struct vertexInput {
	float4 vertex : POSITION;
	#ifdef VERTEX_COLOR_ON
		fixed4 color : COLOR;
	#endif
	float2 uv0 : TEXCOORD0;
	float2 uv1 : TEXCOORD1;
	float2 uv2 : TEXCOORD2;
	float2 uv3 : TEXCOORD3;
};
struct vertexOutput {
	float4 pos : SV_POSITION;
	#ifdef VERTEX_COLOR_ON
		fixed4 color : COLOR;
	#endif
	float2 emissionCoord : TEXCOORD0;
	#ifdef LIGHTMAP_ON
		float2 lightmapCoord : TEXCOORD1;
	#endif
};

vertexOutput vert(vertexInput input)
{
	vertexOutput output;

	float2 emissionCoord;
	switch (_EmissionUV) {
	case 0: emissionCoord = input.uv0; break;
	case 1: emissionCoord = input.uv1; break;
	case 2: emissionCoord = input.uv2; break;
	case 3: emissionCoord = input.uv3; break;
	default: emissionCoord = input.uv0; break;
	}
	output.emissionCoord = TRANSFORM_TEX(emissionCoord, _EmissionMap);

	#ifdef LIGHTMAP_ON
		float2 lightmapCoord;
		switch (_LightUV) {
		case 0: lightmapCoord = input.uv0; break;
		case 1: lightmapCoord = input.uv1; break;
		case 2: lightmapCoord = input.uv2; break;
		case 3: lightmapCoord = input.uv3; break;
		default: lightmapCoord = input.uv0; break;
		}
		output.lightmapCoord = TRANSFORM_TEX(lightmapCoord, _LightMap);
	#endif

	output.pos = UnityObjectToClipPos(input.vertex);
	#ifdef VERTEX_COLOR_ON
		output.color = (fixed4) input.color;
	#endif
	return output;
}

fixed4 frag(vertexOutput input) : COLOR
{
	// do the alpha test immediately if enabled
	fixed4 texColor = tex2D(_EmissionMap, input.emissionCoord);
	#ifdef _ALPHATEST_ON
		if (texColor.a*_EmissionFactor.a < _Cutoff) {
			discard;
		}
	#endif

	#ifdef VERTEX_COLOR_ON
		fixed4 finalColor = input.color;
	#else
		fixed4 finalColor = fixed4(1,1,1,1);
	#endif

	finalColor = finalColor * _EmissionFactor * texColor;

	#ifdef LIGHTMAP_ON
		// lerp(textureColor, lightColor*textureColor, _LightmapFactor)
		fixed4 lightColor = tex2D(_LightMap, input.lightmapCoord) * finalColor;
		finalColor = (fixed4(1,1,1,1) - _LightFactor) * finalColor + _LightFactor * lightColor;
	#endif

	fixed4 ambient = unity_AmbientSky * _AmbientFactor;
	finalColor = ambient + finalColor;

	return fixed4(finalColor.rgb, texColor.a * _EmissionFactor.a);
}

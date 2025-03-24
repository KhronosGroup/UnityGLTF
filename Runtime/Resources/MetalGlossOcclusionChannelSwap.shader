Shader "Hidden/MetalGlossOcclusionChannelSwap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SmoothnessRangeMin ("Smoothness Range Min", float) = 0
		_SmoothnessRangeMax ("Smoothness Range Max", float) = 1
		_MetallicRangeMin ("Metallic Range Min", float) = 0
		_MetallicRangeMax ("Metallic Range Max", float) = 1
		_OcclusionRangeMin ("Occlusion Range Min", float) = 0
		_OcclusionRangeMax ("Occlusion Range Max", float) = 1
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;

			float _MetallicRangeMin;
			float _MetallicRangeMax;

			float _SmoothnessRangeMin;
			float _SmoothnessRangeMax;

			float _OcclusionRangeMin;
			float _OcclusionRangeMax;

			float4 frag (v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);
				// From the GLTF 2.0 spec
				// The metallic-roughness texture. The metalness values are sampled from the B channel. 
				// The roughness values are sampled from the G channel. These values are linear. 
				// If other channels are present (R or A), they are ignored for metallic-roughness calculations.
				//
				// Unity, by default, puts metallic in R channel and glossiness in A channel.
				// Unity uses a metallic-gloss texture so we need to invert the value in the g channel.
				//
				// Conversion Summary
				// Unity R channel goes into B channel
				// Unity A channel goes into G channel, then inverted
				// Unity G channel goes into R channel > Occlusion
				float4 result = float4(
					_OcclusionRangeMin + col.g * (_OcclusionRangeMax - _OcclusionRangeMin),
					1 - (_SmoothnessRangeMin + col.a * (_SmoothnessRangeMax - _SmoothnessRangeMin)),
					_MetallicRangeMin + col.r * (_MetallicRangeMax - _MetallicRangeMin),
					0);
				return result;
			}
			ENDCG
		}
	}
}

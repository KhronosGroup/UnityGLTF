Shader "Hidden/NormalChannel"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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

			// fixed4 frag (v2f i) : SV_Target
			// {
			// 	float4 col = tex2D(_MainTex, i.uv);
			// 	float4 res = float4(col.a, col.g, 1, 1);
			// 	//res = float4(0.5,0.5,1,1);
			// 	//res.xyz = GammaToLinearSpaceExact(res.xyz);
			// 	// If a texture is marked as a normal map
			// 	// the values are stored in the A and G channel.
			// 	return res;
			// }
			
			float4 frag (v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);
				// If a texture is marked as a normal map
				// the values are stored in the A and G channel.
				float3 unpacked = UnpackNormal(col);
				float4 result = float4(unpacked * 0.5f + 0.5f, 1);
				return result;
			}

			ENDCG
		}
	}
}

// Simplified VertexLit shader, optimized for high-poly meshes. Differences from regular VertexLit one:
// - less per-vertex work compared with Mobile-VertexLit
// - supports only DIRECTIONAL lights and ambient term, saves some vertex processing power
// - no per-material color
// - no specular
// - no emission

Shader "GLTF/GLTFMobileVertexLit"
{
	Properties
	{
		_BaseColorFactor("Base Color Factor", Color) = (1,1,1,1)
		_MainTex("Base Color", 2D) = "white" {}
		_EmissiveMap("Emissive Map", 2D) = "black" {}
		_EmissiveFactor("Emissive Factor", Color) = (1,1,1,0)
		_OcclusionMap("Occlusion Map", 2D) = "white" {}
		_OcclusionStrength("Occlusion Strength", Range(0, 1)) = 1
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 80

		Pass
		{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_fwdbase	
			#pragma multi_compile_fog
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
				#ifdef LIGHTMAP_OFF
				fixed3 normal : TEXCOORD1;
				#endif
				#ifndef LIGHTMAP_OFF
				float2 lmap : TEXCOORD2;
				#endif
				#ifdef LIGHTMAP_OFF
				fixed3 vlight : TEXCOORD2;
				#endif
				LIGHTING_COORDS(3,4)
				UNITY_FOG_COORDS(5)
			};

			float4 _MainTex_ST;
			sampler2D _MainTex;
			float4 _BaseColorFactor;
			float _OcclusionStrength;
			sampler2D _OcclusionMap;
			float4 _EmissiveFactor;
			sampler2D _EmissiveMap;

			v2f vert(appdata_full v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
				#ifndef LIGHTMAP_OFF
				o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				#endif
				float3 worldN = UnityObjectToWorldNormal(v.normal);
				#ifdef LIGHTMAP_OFF
				o.normal = worldN;
				#endif
				#ifdef LIGHTMAP_OFF

				o.vlight = ShadeSH9(float4(worldN,1.0));
				o.vlight += LightingLambertVS(worldN, _WorldSpaceLightPos0.xyz);

				#endif // LIGHTMAP_OFF
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed3 normal;

				#ifdef LIGHTMAP_OFF
				normal = i.normal;
				#else
				normal = 0;
				#endif

				half4 color = tex2D(_MainTex, i.uv) * _BaseColorFactor;
				half3 albedo = color.rgb;
				half4 alpha = color.a;

				fixed atten = 1.0;
				fixed4 c = 0;

				#ifdef LIGHTMAP_OFF
				c.rgb = albedo * i.vlight * atten;
				#endif // LIGHTMAP_OFF

				#ifndef LIGHTMAP_OFF
				fixed3 lm = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmap.xy));

				#ifdef SHADOWS_SCREEN
				c.rgb += o.Albedo * min(lm, atten * 2);
				#else
				c.rgb += o.Albedo * lm;
				#endif

				c.a = o.Alpha;
				#endif // !LIGHTMAP_OFF

				UNITY_APPLY_FOG(i.fogCoord, c);
				UNITY_OPAQUE_ALPHA(c.a);

				fixed4 occlusion = tex2D(_OcclusionMap, i.uv) * _OcclusionStrength;
				fixed4 emissive = tex2D(_EmissiveMap, i.uv) * _EmissiveFactor;

				return c * occlusion + emissive;
			}

			ENDCG
		}
	}

	FallBack "Mobile/VertexLit"
}

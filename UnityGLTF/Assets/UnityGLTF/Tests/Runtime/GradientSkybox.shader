// Simple shader for applying a gradient to a Skybox material, allowing the same shading
// logic as a Gradient environmental lighting, but with the Intensity not available to Gradient lighting.

// Based loosely  on the built-in hidden GradientSky shader, but with the old style for naming conventions and none of the new render-pipeline dependencies (some simplifications
// due to the limited use case).
// https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/master/com.unity.render-pipelines.high-definition/Runtime/Sky/GradientSky/GradientSky.shader
Shader "Skybox/Gradient"
{
	Properties
	{
		// Default values based on RenderSettings default values for m_AmbientSkyColor, m_AmbientEquatorColor, and m_AmbientGroundColor
		[HDR]_SkyColor("Sky Color", Color) = (0.212, 0.227, 0.259, 1)
		[HDR]_EquatorColor("Equator Color", Color) = (0.114, 0.125, 0.133, 1)
		[HDR]_GroundColor("Ground Color", Color) = (0.047, 0.043, 0.035, 1)
		_Intensity("Intensity", Float) = 1.0
	}

	SubShader
	{
		Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
		Cull Off
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			half4 _SkyColor, _EquatorColor ,_GroundColor;
			half  _Intensity;

			struct appdata_t
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 texcoord : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.vertex.xyz;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float verticalGradient = i.texcoord.y;
				half topLerpFactor = saturate(verticalGradient);
				half bottomLerpFactor = saturate(-verticalGradient);
				half3 color = lerp(_EquatorColor, _GroundColor, bottomLerpFactor);
				color = lerp(color, _SkyColor, topLerpFactor) * _Intensity;
				return float4(color, 1);
			}
			ENDCG
		}
	}

	Fallback Off
}

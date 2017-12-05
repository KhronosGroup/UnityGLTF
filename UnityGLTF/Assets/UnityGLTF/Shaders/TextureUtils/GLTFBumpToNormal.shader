
Shader "GLTF/BumpToNormal" {
	Properties{
		_BumpMap ("Noise text", 2D) = "bump" {}
		_FlipY("Flip texture Y", Int) = 0
	}

	SubShader {
		 Pass {
			 CGPROGRAM

			 #pragma vertex vert
			 #pragma fragment frag
			 #include "UnityCG.cginc"
			 #include "GLTFColorSpace.cginc"

			 struct vertInput {
			 float4 pos : POSITION;
			 float2 texcoord : TEXCOORD0;
			 };

			 struct vertOutput {
			 float4 pos : SV_POSITION;
			 float2 texcoord : TEXCOORD0;
			 };

			 sampler2D _MetallicGlossMap;
			 sampler2D _OcclusionMap;
			 sampler2D _BumpMap;
			 int _FlipY;

			 vertOutput vert(vertInput input) {
				 vertOutput o;
				 o.pos = UnityObjectToClipPos(input.pos);
				 o.texcoord.x = input.texcoord.x;
				 if(_FlipY == 1)
					o.texcoord.y = 1.0 - input.texcoord.y;
				 else
					o.texcoord.y = input.texcoord.y;

				 return o;
			 }

			 float4 frag(vertOutput output) : COLOR {
				float4 final = half4(0.0, 0.0, 0.0 ,1.0);
				float4 bump = srgbToLinear(tex2D(_BumpMap, output.texcoord));
			 	bump.r = sqrt(1.0 - pow(bump.g, 2)  - pow(bump.a, 2));

			 	final.r = linearToSrgb1(bump.a);
			 	final.g = bump.g;
			 	final.b = bump.r;

			 	return linearToSrgb(final);
			 }

			ENDCG
		}
	}
}


Shader "GLTF/Linear2sRGB" {
	Properties{
		_InputTex("Texture", 2D) = "white" {}
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

			 sampler2D _InputTex;
			 int _FlipY;

			 vertOutput vert(vertInput input) {
				 vertOutput o;
				 o.pos = UnityObjectToClipPos(input.pos);
				 o.texcoord.x = input.texcoord.x;
				 o.texcoord.y = input.texcoord.y;

				 return o;
			 }

			 float4 frag(vertOutput output) : COLOR {
				return linearToSrgb(tex2D(_InputTex, output.texcoord));
			 }

			ENDCG
		}
	}
}

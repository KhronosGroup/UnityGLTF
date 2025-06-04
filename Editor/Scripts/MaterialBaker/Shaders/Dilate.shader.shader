// origianl Source: https://github.com/sneha-belkhale/shader-bake-unity

Shader "TextureBake/Dilate"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BackgroundColor ("BackgroundColor", Color) = (0,0,0,0)
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
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _MainTex_ST;
            float4 _BackgroundColor;
            
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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }
            
            // simple dilate operator, gets the brightest pixel in a radius. 
            // not very efficient, but it is a one time operation.
            float4 dilate (float2 uv) 
            {
                float2 delta = _MainTex_TexelSize;
                float4 closestColor = _BackgroundColor;
                float maxBright = 0;
                for (float i = -3; i < 3; i++) {
                    for(float j = -3; j < 3; j++) {
                        float4 neighborCol = clamp(tex2D(_MainTex, uv + float2(i, j) * delta),0,1);
                        float brightness = distance(neighborCol.rgb, _BackgroundColor.rgb);
                        if(brightness > maxBright){
                            closestColor = neighborCol;
                            maxBright = brightness;
                        }
                    }
                }
                return closestColor; 
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
               fixed4 col = tex2D(_MainTex, i.uv);
               if(distance(col.rgb, _BackgroundColor.rgb) < 0.001) {
                  col = dilate(i.uv);
                } 
                return col;
            }
            ENDCG
        }
    }
}
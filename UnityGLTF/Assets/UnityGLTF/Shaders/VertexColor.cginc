#ifndef VERTEX_COLOR_INCLUDED
#define VERTEX_COLOR_INCLUDED

#include "UnityCG.cginc"

uniform sampler2D _MainTex;
float _Cutoff;

struct v2f {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    fixed4 color : COLOR;
};

v2f vert_vcol (appdata_full v)
{
    v2f o;
#if defined(_VERTEX_COLORS)
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord.xy;
    o.color = v.color;
#else
    o.pos = 0;
    o.uv = 0;
    o.color = 0;
#endif
    return o;
}

fixed4 frag_vcol (v2f i) : SV_Target { 
#if defined(_VERTEX_COLORS)
    fixed4 r = tex2D(_MainTex, i.uv);
    half4 alpha = r.a;
    clip(alpha - _Cutoff);
    fixed4 color = i.color;
    color.a = alpha;
    return color;
#else
    return 0;
#endif
}
#endif
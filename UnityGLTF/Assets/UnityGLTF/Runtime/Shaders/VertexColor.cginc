#ifndef VERTEX_COLOR_INCLUDED
#define VERTEX_COLOR_INCLUDED

#include "UnityCG.cginc"
#include "UnityStandardInput.cginc"
#include "UnityStandardCoreForward.cginc"

struct WrappedVertexOutputForwardBase {
    VertexOutputForwardBase innerValue;
    fixed4 color : COLOR;
};

WrappedVertexOutputForwardBase vert_vcol (appdata_full v)
{
    // Save the color information
    WrappedVertexOutputForwardBase o;
    o.color = v.color;

    // Forward this call to the default vertBase
    VertexInput vi;
    vi.vertex = v.vertex;
    vi.normal = v.normal;
    vi.uv0 = v.texcoord;
    vi.uv1 = v.texcoord1;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
    vi.uv2 = v.tetcoord2;
#endif
#ifdef _TANGENT_TO_WORLD
    vi.tangent = v.tangent;
#endif
    o.innerValue = vertBase(vi);

    return o;
}

fixed4 frag_vcol(WrappedVertexOutputForwardBase wvofb) : SV_Target
{
    // Put the original output from vertBase in a variable called 'i' for the macros
    // (e.g. FRAGMENT_SETUP) that assume that naming convention.
    VertexOutputForwardBase i = wvofb.innerValue;

    // The following section is copied from the fragBase implementation, found in
    // C:\Program Files\Unity\Hub\Editor\2017.4.33f1\Editor\Data\CGIncludes\UnityStandardCore.cginc,
    // with the name fragForwardBase/fragForwardBaseInternal.  It has been modified to
    // include a section to modify the diffColor after it is calculated but before
    // it is used in the remaining calculations.
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    FRAGMENT_SETUP(s)

    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    UnityLight mainLight = MainLight();
    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

    // Start: Modified section
    s.diffColor.x *= wvofb.color.x;
    s.diffColor.y *= wvofb.color.y;
    s.diffColor.z *= wvofb.color.z;
    // End: Modified section

    half occlusion = Occlusion(i.tex.xy);
    UnityGI gi = FragmentGI(s, occlusion, i.ambientOrLightmapUV, atten, mainLight);

    half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
    c.rgb += Emission(i.tex.xy);

    UNITY_APPLY_FOG(i.fogCoord, c.rgb);
    return OutputForward(c, s.alpha);
}
#endif

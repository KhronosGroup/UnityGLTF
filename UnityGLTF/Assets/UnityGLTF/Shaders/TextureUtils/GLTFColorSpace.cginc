#ifndef GLTF_COLOR_SPACE
#define GLTF_COLOR_SPACE

float linearToSrgb1(float c) {
	float gamma = 2.4;
    float v = 0.0;
    if (c < 0.0031308) {
        if (c > 0.0) v = c * 12.92;
    } else {
        v = 1.055 * pow(c, 1.0 / gamma) - 0.055;
    }
    return v;
};

float srgbToLinear1(float c) {
	float gamma = 2.4;
    float v = 0.0;
    if (c < 0.04045) {
        if (c >= 0.0) v = c * (1.0 / 12.92);
    } else {
        v = pow((c + 0.055) * (1.0 / 1.055), gamma);
    }
    return v;
};

float4 linearToSrgb(float4 c){
    float4 col = float4(0.0, 0.0, 0.0 ,1.0);
    col.r = linearToSrgb1(c.r);
    col.g = linearToSrgb1(c.g);
    col.b = linearToSrgb1(c.b);
    col.a = c.a;
    return col;
};

float4 srgbToLinear(float4 c){
	float4 col = float4(0.0, 0.0, 0.0 ,1.0);
    col.r = srgbToLinear1(c.r);
    col.g = srgbToLinear1(c.g);
    col.b = srgbToLinear1(c.b);
    col.a = c.a;
    return col;
};

#endif // GLTF_COLOR_SPACE

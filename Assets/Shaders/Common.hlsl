#include "UnityCG.cginc"

// Hash function from H. Schechter & R. Bridson, goo.gl/RXiKaH
uint Hash(uint s)
{
    s ^= 2747636419u;
    s *= 2654435769u;
    s ^= s >> 16;
    s *= 2654435769u;
    s ^= s >> 16;
    s *= 2654435769u;
    return s;
}

float Random(uint seed)
{
    return float(Hash(seed)) / 4294967295.0; // 2^32-1
}

// Uniformaly distributed points on a unit sphere
// http://mathworld.wolfram.com/SpherePointPicking.html
float3 RandomUnitVector(uint seed)
{
    float PI2 = 6.28318530718;
    float z = 1 - 2 * Random(seed);
    float xy = sqrt(1.0 - z * z);
    float sn, cs;
    sincos(PI2 * Random(seed + 1), sn, cs);
    return float3(sn * xy, cs * xy, z);
}

// Uniformaly distributed points inside a unit sphere
float3 RandomVector(uint seed)
{
    return RandomUnitVector(seed) * sqrt(Random(seed + 2));
}

// Uniformaly distributed points inside a unit cube
float3 RandomVector01(uint seed)
{
    return float3(Random(seed), Random(seed + 1), Random(seed + 2));
}

// Hue value -> RGB color
half3 Hue2RGB(half h)
{
    h = frac(h) * 6 - 2;
    half3 rgb = saturate(half3(abs(h - 1) - 1, 2 - abs(h), 2 - abs(h - 2)));
    return rgb;
}

// Euler angles rotation matrix
float3x3 Euler3x3(float3 v)
{
    float sx, cx;
    float sy, cy;
    float sz, cz;

    sincos(v.x, sx, cx);
    sincos(v.y, sy, cy);
    sincos(v.z, sz, cz);

    float3 row1 = float3(sx*sy*sz + cy*cz, sx*sy*cz - cy*sz, cx*sy);
    float3 row3 = float3(sx*cy*sz - sy*cz, sx*cy*cz + sy*sz, cx*cy);
    float3 row2 = float3(cx*sz, cx*cz, -sx);

    return float3x3(row1, row2, row3);
}

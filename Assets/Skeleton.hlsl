#include "Common.hlsl"
#include "UnityGBuffer.cginc"
#include "UnityStandardUtils.cginc"
#include "SimplexNoise3D.hlsl"

// Cube map shadow caster; Used to render point light shadows on platforms
// that lacks depth cube map support.
#if defined(SHADOWS_CUBE) && !defined(SHADOWS_CUBE_IN_DEPTH_TEX)
#define PASS_CUBE_SHADOWCASTER
#endif

// Fragment varyings
struct Varyings
{
    float4 position : SV_POSITION;

#if defined(PASS_CUBE_SHADOWCASTER)
    // Cube map shadow caster pass
    float3 shadow : TEXCOORD0;

#elif defined(UNITY_PASS_SHADOWCASTER)
    // Default shadow caster pass

#else
    // GBuffer construction pass
    float3 normal : NORMAL;
    half3 ambient : TEXCOORD0;
    float3 wpos : TEXCOORD1;

#endif
};

//
// Vertex stage
//

float4 Vertex(float4 position : POSITION) : POSITION
{
    // Only do object space to world space transform.
    return mul(unity_ObjectToWorld, position);
}

//
// Geometry stage
//

Varyings VertexOutput(float3 wpos, half3 wnrm)
{
    Varyings o;

#if defined(PASS_CUBE_SHADOWCASTER)
    // Cube map shadow caster pass: Transfer the shadow vector.
    o.position = UnityWorldToClipPos(float4(wpos, 1));
    o.shadow = wpos - _LightPositionRange.xyz;

#elif defined(UNITY_PASS_SHADOWCASTER)
    // Default shadow caster pass: Apply the shadow bias.
    float scos = dot(wnrm, normalize(UnityWorldSpaceLightDir(wpos)));
    wpos -= wnrm * unity_LightShadowBias.z * sqrt(1 - scos * scos);
    o.position = UnityApplyLinearShadowBias(UnityWorldToClipPos(float4(wpos, 1)));

#else
    // GBuffer construction pass
    o.position = UnityWorldToClipPos(float4(wpos, 1));
    o.normal = wnrm;
    o.ambient = ShadeSHPerVertex(wnrm, 0);
    o.wpos = wpos;

#endif
    return o;
}

[maxvertexcount(60)]
void Geometry(
    line float4 input[2] : POSITION,
    uint pid : SV_PrimitiveID,
    inout TriangleStream<Varyings> outStream
)
{
    // Input vertices
    float3 p0 = input[0].xyz;
    float3 p1 = input[1].xyz;

    float3 az = normalize(p1 - p0);
    float3 ax = normalize(cross(az, float3(0, 1, 1)));
    float3 ay = cross(az, ax);

    const int DIV = 30;
    const float RADIUS = 0.1;

    float theta = _Time.y * 5;
    float3 ext = az * 0.02;

    for (uint i = 0; i < DIV; i++)
    {
        float3 p = lerp(p0, p1, float(i) / DIV);

        float3 pd = ax * cos(theta) + ay * sin(theta);
        p += pd * RADIUS;

        outStream.Append(VertexOutput(p - ext, pd));
        outStream.Append(VertexOutput(p + ext, pd));

        theta += 0.6;
    }

    outStream.RestartStrip();

}

//
// Fragment phase
//

#if defined(PASS_CUBE_SHADOWCASTER)

// Cube map shadow caster pass
half4 Fragment(Varyings input) : SV_Target
{
    float depth = length(input.shadow) + unity_LightShadowBias.x;
    return UnityEncodeCubeShadowDepth(depth * _LightPositionRange.w);
}

#elif defined(UNITY_PASS_SHADOWCASTER)

// Default shadow caster pass
half4 Fragment() : SV_Target { return 0; }

#else

// GBuffer construction pass
void Fragment(
    Varyings input,
    float vface : VFACE,
    out half4 outGBuffer0 : SV_Target0,
    out half4 outGBuffer1 : SV_Target1,
    out half4 outGBuffer2 : SV_Target2,
    out half4 outEmission : SV_Target3
)
{
    half4 _Color = 1;
    half _Metallic = 0.5;
    half _Glossiness = 0.5;

    // PBS workflow conversion (metallic -> specular)
    half3 c_diff, c_spec;
    half not_in_use;

    c_diff = DiffuseAndSpecularFromMetallic(
        _Color.rgb, _Metallic, // input
        c_spec, not_in_use     // output
    );

    // Update the GBuffer.
    UnityStandardData data;
    data.diffuseColor = c_diff;
    data.occlusion = 1;
    data.specularColor = c_spec;
    data.smoothness = _Glossiness;
    data.normalWorld = input.normal * (vface < 0 ? -1 : 1);
    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    // Output ambient light to the emission buffer.
    half3 sh = ShadeSHPerPixel(data.normalWorld, input.ambient, input.wpos);
    outEmission = half4(sh * data.diffuseColor, 1);
}

#endif

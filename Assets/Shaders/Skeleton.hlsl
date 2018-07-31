#include "Common.hlsl"
#include "UnityGBuffer.cginc"
#include "UnityStandardUtils.cginc"
#include "SimplexNoise3D.hlsl"

// Cube map shadow caster; Used to render point light shadows on platforms
// that lacks depth cube map support.
#if defined(SHADOWS_CUBE) && !defined(SHADOWS_CUBE_IN_DEPTH_TEX)
#define PASS_CUBE_SHADOWCASTER
#endif

// Material properties
half4 _Color;
half _Metallic;
half _Glossiness;

// Vertex attributes
struct Attributes
{
    float4 position : POSITION;
    float3 normal : NORMAL;
    half2 texcoord : TEXCOORD;
};

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
    float emission : TEXCOORD2;

#endif
};

//
// Vertex stage
//

void Vertex(inout Attributes input)
{
    // Only do object space to world space transform.
    input.position = mul(unity_ObjectToWorld, input.position);
    input.normal = UnityObjectToWorldNormal(input.normal);
}

//
// Geometry stage
//

Varyings VertexOutput(float3 wpos, half3 wnrm, half em)
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
    o.emission = em;

#endif
    return o;
}

[maxvertexcount(32)]
[instance(32)]
void Geometry(
    uint primitiveID : SV_PrimitiveID,
    uint instanceID : SV_GSInstanceID,
    line Attributes input[2],
    inout TriangleStream<Varyings> outStream
)
{
    // Unique ID
    uint uid = (primitiveID * 100 + instanceID) * 20;

    // Input vertices
    float3 p1 = input[0].position.xyz;
    float3 p2 = input[1].position.xyz;

    // Bone axes
    float3 az = normalize(p2 - p1);
    float3 ax = normalize(cross(az, input[0].normal));
    float3 ay = cross(az, ax);

    // Time parameters
    float time = _Time.y + Random(uid) * 10;
    float vel = 3 * lerp(0.2, 1, Random(uid + 1));
    float avel = 0*0.5 * lerp(-1, 1, Random(uid + 2));

    // Constants
    const uint segments = 16;
    const float radius = 0.15 * input[0].texcoord.x;//* (0.2 + Random(uid + 3));
    const float3 extent = az * 0.02;
    const float trail = 0.25;

    // Geometry construction
    float param = vel * time;
    float3 last = p1;

    //half em = Random(floor((uid + _Time.y) / 4)) > 0.9;
    half em = snoise(float3(uid + _Time.y, 0, 0) * 2);
    //em = pow(abs(em), 10) * 15;
    em = smoothstep(0.55, 0.65, em) * 1.75;

    for (uint i = 0; i < segments; i++)
    {
        float param_f = frac(param);
        float3 vp = lerp(p1, p2, param_f * 3 - 1);

        float theta = avel * param;
        float3 pd = ax * cos(theta) + ay * sin(theta);
        //vp += pd * radius * (0.8 + 1 * snoise(float3(param * 6, primitiveID, time)));
        float sn1 = snoise(float3(param * 3, uid + 20, time));
        float sn2 = snoise(float3(param * 3, uid - 34, time));
        sn1 += snoise(float3(param * 6, uid + 20, time)) / 2;
        sn2 += snoise(float3(param * 6, uid - 34, time)) / 2;
        vp += ax * radius * sn1;
        vp += ay * radius * sn2;

        param_f += 0.2 * snoise(float3(param * 4, primitiveID + 100, time));
        float w = smoothstep(trail, 0.5, param_f) * smoothstep(trail, 0.5, 1 - param_f);

        float3 pn = normalize(cross(az, vp - last));

        outStream.Append(VertexOutput(vp - extent * w, pn, em));
        outStream.Append(VertexOutput(vp + extent * w, pn, em));

        last = vp;
        param += trail / segments;
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

    outEmission += half4(0.2, 0.3, 1.2, 0) * input.emission;
}

#endif

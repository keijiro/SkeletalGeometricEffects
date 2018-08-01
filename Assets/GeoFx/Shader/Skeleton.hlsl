#include "Common.hlsl"
#include "UnityGBuffer.cginc"
#include "UnityStandardUtils.cginc"
#include "SimplexNoise2D.hlsl"

// Cube map shadow caster; Used to render point light shadows on platforms
// that lacks depth cube map support.
#if defined(SHADOWS_CUBE) && !defined(SHADOWS_CUBE_IN_DEPTH_TEX)
#define PASS_CUBE_SHADOWCASTER
#endif

// Effect properties
float4 _GeoParams; // radius, width, speed, length
float4 _AnimParams; // time, wave width, wave speed, distortion

// Material properties
half4 _MatParams; // metallic, smoothness, hue shift, hilight
half4 _BaseHSVM;
half4 _AddHSVM;

// Vertex attributes
struct Attributes
{
    float4 position : POSITION;
    half3 normal : NORMAL;
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
    half2 color : COLOR;
    float3 normal : NORMAL;
    half3 ambient : TEXCOORD0;
    float3 wpos : TEXCOORD1;
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

Varyings VertexOutput(float3 wpos, half3 wnrm, half em, half rand)
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
    o.color = half2(em, rand);
    o.normal = wnrm;
    o.ambient = ShadeSHPerVertex(wnrm, 0);
    o.wpos = wpos;

#endif

    return o;
}

#define GEOFX_INSTANCE_COUNT 32
#define GEOFX_POINT_COUNT 32

[instance(GEOFX_INSTANCE_COUNT)]
[maxvertexcount(GEOFX_POINT_COUNT * 2)]
void Geometry(
    uint primitiveID : SV_PrimitiveID,
    uint instanceID : SV_GSInstanceID,
    line Attributes input[2],
    inout TriangleStream<Varyings> outStream
)
{
    const float BaseRadius = _GeoParams.x;
    const float StripWidth = _GeoParams.y;
    const float StripSpeed = _GeoParams.z;
    const float StripLength = _GeoParams.w;
    const float BaseTime = _AnimParams.x;
    const float WaveWidth = _AnimParams.y;
    const float WaveSpeed = _AnimParams.z;
    const float Distortion = _AnimParams.w;
    const float HilightProb = _MatParams.w;

    // Input vertices
    float3 p0 = input[0].position.xyz;
    float3 p1 = input[1].position.xyz;

    float3 n0 = input[0].normal;
    float3 n1 = input[1].normal;

    // Bone space axes
    half3 az_bone = normalize(p1 - p0);
    half3 ax_bone = normalize(cross(az_bone, n0));
    half3 ay_bone = cross(az_bone, ax_bone);

    // Bone parameter
    float radius_bone = input[0].texcoord.x * BaseRadius;

    // Per-strip unique ID
    uint id_strip = (primitiveID * GEOFX_INSTANCE_COUNT + instanceID) * 299;

    // Time parameters
    float time_strip = BaseTime * (0.5 + Random(id_strip));

    // Light emission intensity (per-strip)
    half em_strip = snoise(float2(id_strip, BaseTime));
    em_strip = smoothstep(1 - HilightProb, 1.1 - HilightProb, abs(em_strip));

    // Per-strip random seed
    half rand_strip = Random(id_strip + 1);

    float3 last = p0;
    for (uint i = 0; i < GEOFX_POINT_COUNT; i++)
    {
        half param = (float)i / GEOFX_POINT_COUNT;

        // Angular parameter along the bone Z axis
        float phi = StripSpeed * time_strip + StripLength * param;

        // Position parameter along the bone Z axis
        half param_z = 0.5 + 0.7 * snoise(float2(id_strip + 20, phi * 0.05));

        // Amplification parameter
        // Don't use the per-strip ID nor time parameter; This variable should
        // be only depend on the absolute position and time.
        float time_amp = BaseTime * WaveSpeed + param_z * WaveWidth;
        half amp = 1 + 0.7 * snoise(float2(primitiveID * 53, time_amp));

        // Radius
        half radius = 1 + 0.7 * snoise(float2(id_strip + 40, phi * 0.25));
        radius *= radius_bone * amp;

        // Bone space axes with turbulent noise
        half2 amod = snoise_grad(float2(id_strip + 60, phi * 0.1)).xy * Distortion;
        half3 az = normalize(az_bone + ax_bone * amod.x + ay_bone * amod.y);
        half3 ay = normalize(cross(az, ax_bone));
        half3 ax = cross(ay, az);

        // Point position
        float3 pos = lerp(p0, p1, param_z);
        pos += (ax * cos(phi) + ay * sin(phi)) * radius;

        // Normal vector calculation
        float3 nrm = normalize(cross(az, pos - last));

        // Strip extent
        float3 ext = az * StripWidth * amp;
        ext *= smoothstep(0, 0.5, param) * smoothstep(0, 0.5, 1 - param);

        // Vertex output
        outStream.Append(VertexOutput(pos - ext, nrm, em_strip, rand_strip));
        outStream.Append(VertexOutput(pos + ext, nrm, em_strip, rand_strip));

        last = pos;
    }

    outStream.RestartStrip();
}

//
// Fragment phase
//

half3 HSVM2RGB(half4 hsvm, half rand)
{
    hsvm.x += (rand - 0.5) * _MatParams.z;
    return GammaToLinearSpace(HsvToRgb(hsvm.xyz)) * hsvm.w;
}

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
    half em = input.color.x;
    half rand = input.color.y;
    half3 albedo = HSVM2RGB(_BaseHSVM, rand);

    // PBS workflow conversion (metallic -> specular)
    half3 c_diff, c_spec;
    half not_in_use;

    c_diff = DiffuseAndSpecularFromMetallic(
        albedo, _MatParams.x, // input
        c_spec, not_in_use    // output
    );

    // Update the GBuffer.
    UnityStandardData data;
    data.diffuseColor = c_diff;
    data.occlusion = 1;
    data.specularColor = c_spec;
    data.smoothness = _MatParams.y;
    data.normalWorld = input.normal * (vface < 0 ? -1 : 1);
    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    // Output ambient light to the emission buffer.
    half3 sh = ShadeSHPerPixel(data.normalWorld, input.ambient, input.wpos);
    outEmission = half4(sh * data.diffuseColor, 1);

    // Self emission term
    outEmission.xyz += HSVM2RGB(_AddHSVM, rand) * em;
}

#endif

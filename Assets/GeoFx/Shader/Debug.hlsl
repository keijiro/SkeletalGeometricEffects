#include "UnityGBuffer.cginc"
#include "UnityStandardUtils.cginc"

struct Varyings { float4 position : SV_POSITION; };

void Vertex(inout float4 position : POSITION) {}

[maxvertexcount(2)]
void Geometry(
    line float4 vertices[2] : POSITION,
    inout LineStream<Varyings> outStream
)
{
    Varyings v;
    v.position = UnityObjectToClipPos(vertices[0]); outStream.Append(v);
    v.position = UnityObjectToClipPos(vertices[1]); outStream.Append(v);
    outStream.RestartStrip();
}

void Fragment(
    Varyings input,
    out half4 outGBuffer0 : SV_Target0,
    out half4 outGBuffer1 : SV_Target1,
    out half4 outGBuffer2 : SV_Target2,
    out half4 outEmission : SV_Target3
)
{
    UnityStandardData data;
    data.diffuseColor = data.occlusion = 1;
    data.specularColor = data.smoothness = 0;
    data.normalWorld = float3(0, 0, -1);
    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);
    outEmission = 1;
}


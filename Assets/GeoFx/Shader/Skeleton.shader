Shader "Hidden/GeoFx/Skeleton"
{
    Properties
    {
        _Color("", Color) = (1, 1, 1, 1)
        [Gamma] _Metallic("", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Tags { "LightMode"="Deferred" }

            Cull Off

            CGPROGRAM

            #pragma target 5.0
            #pragma vertex Vertex
            #pragma geometry Geometry
            #pragma fragment Fragment
            #pragma multi_compile_prepassfinal noshadowmask nodynlightmap nodirlightmap nolightmap
            #pragma multi_compile _ GEOFX_DEBUG

            #ifdef GEOFX_DEBUG
            #include "Debug.hlsl"
            #else
            #include "Skeleton.hlsl"
            #endif

            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="ShadowCaster" }

            Cull Off

            CGPROGRAM

            #pragma target 5.0
            #pragma vertex Vertex
            #pragma geometry Geometry
            #pragma fragment Fragment
            #pragma multi_compile_prepassfinal noshadowmask nodynlightmap nodirlightmap nolightmap
            #pragma multi_compile _ GEOFX_DEBUG

            #ifdef GEOFX_DEBUG
            #include "Debug.hlsl"
            #else
            #include "Skeleton.hlsl"
            #endif

            ENDCG
        }
    }
}

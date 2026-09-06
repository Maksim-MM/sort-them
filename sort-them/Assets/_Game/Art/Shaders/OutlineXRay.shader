Shader "SortThem/OutlineXRay"
{
    Properties
    {
        _Color("Color", Color) = (0.72,0.3,1,1)
        _Width("Width", Float) = 0.02
        _XRayColor("XRay Color", Color) = (0.72,0.3,1,0.55)
        [Enum(UnityEngine.Rendering.CompareFunction)] _XRayZTest("XRay ZTest", Float) = 8
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Width;
            float4 _XRayColor;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; };
            V vert(A IN)
            {
                V o;
                float3 pos = IN.positionOS.xyz + normalize(IN.normalOS) * _Width;
                o.positionCS = TransformObjectToHClip(pos);
                return o;
            }
            half4 frag(V IN) : SV_Target { return _Color; }
            ENDHLSL
        }

        Pass
        {
            Name "XRay"
            Cull Back
            ZWrite Off
            ZTest [_XRayZTest]
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Width;
            float4 _XRayColor;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; };
            V vert(A IN)
            {
                V o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return o;
            }
            half4 frag(V IN) : SV_Target { return _XRayColor; }
            ENDHLSL
        }
    }
}

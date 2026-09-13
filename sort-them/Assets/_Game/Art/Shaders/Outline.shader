Shader "SortThem/Outline"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Width("Width (px at 900p)", Float) = 7
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }
        Pass
        {
            Name "Mask"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest Always
            ColorMask 0
            Stencil { Ref 1 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; };
            V vert(A IN)
            {
                V o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return o;
            }
            half4 frag(V IN) : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Stencil { Ref 1 Comp NotEqual Pass Keep }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Width;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; };
            V vert(A IN)
            {
                V o;
                float4 posCS = TransformObjectToHClip(IN.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 nCS = TransformWorldToHClipDir(nWS);
                float2 n = nCS.xy;
                float len = length(n);
                n = len > 1e-5 ? n / len : float2(0, 0);
                float2 px = _Width * posCS.w * 2.0 / 900.0;
                px.x *= _ScreenParams.y / _ScreenParams.x;
                posCS.xy += n * px;
                o.positionCS = posCS;
                return o;
            }
            half4 frag(V IN) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}

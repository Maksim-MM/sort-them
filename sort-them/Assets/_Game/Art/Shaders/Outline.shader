Shader "SortThem/Outline"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Width("Width", Float) = 0.012
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite [_ZWrite]
            ZTest [_ZTest]
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
                float3 pos = IN.positionOS.xyz + normalize(IN.normalOS) * _Width;
                o.positionCS = TransformObjectToHClip(pos);
                return o;
            }
            half4 frag(V IN) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}

Shader "SortThem/Dust"
{
    Properties
    {
        _Color("Color", Color) = (1, 0.96, 0.88, 1)
        _Intensity("Intensity", Range(0, 3)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+30" "IgnoreProjector"="True" }
        Pass
        {
            Name "Dust"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Intensity;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            V vert(A IN)
            {
                V o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.uv = IN.uv;
                o.color = IN.color;
                return o;
            }
            half4 frag(V IN) : SV_Target
            {
                float d = length(IN.uv - 0.5) * 2.0;
                float spot = smoothstep(1.0, 0.0, d);
                spot *= spot;
                float a = spot * IN.color.a * _Intensity;
                return half4(_Color.rgb * IN.color.rgb * a, a);
            }
            ENDHLSL
        }
    }
}

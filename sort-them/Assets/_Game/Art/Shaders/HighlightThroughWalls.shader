Shader "SortThem/HighlightThroughWalls"
{
    Properties
    {
        _Color("Color", Color) = (1,0.85,0.2,0.85)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Overlay-10" }
        Pass
        {
            Name "Highlight"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            V vert(A IN)
            {
                V o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return o;
            }
            half4 frag(V IN) : SV_Target
            {
                half shade = 0.7 + 0.3 * saturate(dot(normalize(IN.normalWS), half3(0.3, 0.8, 0.5)));
                return half4(_Color.rgb * shade, _Color.a);
            }
            ENDHLSL
        }
    }
}

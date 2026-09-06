Shader "SortThem/VertexColorLit"
{
    Properties
    {
        _BaseColor("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 color : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                o.color = IN.color;
                return o;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half ndl = saturate(dot(n, mainLight.direction));
                half3 ambient = SampleSH(n);
                half3 albedo = IN.color.rgb * _BaseColor.rgb;
                half3 col = albedo * (mainLight.color * ndl * mainLight.shadowAttenuation + ambient);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; };
            V vertDepth(A IN) { V o; o.positionCS = TransformObjectToHClip(IN.positionOS.xyz); return o; }
            half fragDepth(V IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}

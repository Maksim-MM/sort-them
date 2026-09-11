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
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float4 _BaseColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
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
                UNITY_SETUP_INSTANCE_ID(IN);
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
                Light mainLight = GetMainLight();
                half ndl = saturate(dot(n, mainLight.direction));
                half3 ambient = SampleSH(n);
                half3 albedo = IN.color.rgb * _BaseColor.rgb;
                half3 col = albedo * (mainLight.color * ndl + ambient);
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
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _BaseColor;
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };
            V vertDepth(A IN) { V o; UNITY_SETUP_INSTANCE_ID(IN); o.positionCS = TransformObjectToHClip(IN.positionOS.xyz); return o; }
            half fragDepth(V IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}

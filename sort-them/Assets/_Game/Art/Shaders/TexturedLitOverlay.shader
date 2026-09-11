Shader "SortThem/TexturedLitOverlay"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Overlay-5" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; };
            Varyings vert(Attributes IN)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                float4 pos = p.positionCS;
                #if UNITY_REVERSED_Z
                pos.z = pos.w - (pos.w - pos.z) * 0.02;
                #else
                pos.z = -pos.w + (pos.z + pos.w) * 0.02;
                #endif
                o.positionCS = pos;
                o.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                o.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return o;
            }
            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                Light mainLight = GetMainLight();
                half ndl = saturate(dot(n, mainLight.direction));
                half3 ambient = SampleSH(n);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb * _BaseColor.rgb;
                return half4(albedo * (mainLight.color * ndl + ambient), 1);
            }
            ENDHLSL
        }
    }
}

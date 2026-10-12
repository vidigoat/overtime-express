// Vertex-coloured lit surface: the kit's baked colours + main light with shadows + mood ambient + rim.
Shader "Overtime/Lit"
{
    Properties { _Tint("Tint", Color) = (1,1,1,1) _Emission("Emission", Float) = 0 _Rim("Rim", Float) = 0.5 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Forward" Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "OTCommon.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Tint; half _Emission; half _Rim; CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; half4 color : COLOR; half fogFactor : TEXCOORD2; };
            V vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i);
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.color = i.color; o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                half3 albedo = i.color.rgb * _Tint.rgb;
                Light l = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(n, l.direction));
                half3 amb = lerp(_OTAmbGround.rgb, _OTAmbSky.rgb, n.y * 0.5 + 0.5);
                half3 col = albedo * (amb + l.color * ndl * l.shadowAttenuation);
                half3 v = normalize(_WorldSpaceCameraPos - i.positionWS);
                half rim = pow(1.0 - saturate(dot(n, v)), 2.6);
                col += _OTRim.rgb * rim * _Rim * (0.35 + 0.65 * saturate(n.y * 0.5 + 0.5));
                col += albedo * _Emission;
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}

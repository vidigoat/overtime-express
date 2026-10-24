// Emissive pieces: vertex colour x tint x HDR intensity. Only these exceed 1.0, so only these bloom.
Shader "Overtime/Glow"
{
    Properties { [HDR] _Tint("Tint", Color) = (2,2,2,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Forward" Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Tint; CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; half4 color : COLOR; half fogFactor : TEXCOORD0; float3 positionWS : TEXCOORD1; float3 normalWS : TEXCOORD2; };
            V vert(A i) {
                V o; UNITY_SETUP_INSTANCE_ID(i);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.color = i.color; o.fogFactor = ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 frag(V i) : SV_Target {
                half3 c = i.color.rgb * _Tint.rgb;
                float3 n = i.normalWS;
                float3 view = _WorldSpaceCameraPos - i.positionWS;
                half fres = pow(saturate(dot(normalize(n), normalize(view))), 0.6);
                c *= lerp(0.35, 1.0, fres);
                return half4(MixFog(c, i.fogFactor), 1);
            }
            ENDHLSL
        }
    }
}

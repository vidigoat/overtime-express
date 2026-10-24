// Towers: procedural lit windows, neon bands, and a fade into the glow of the streets far below.
Shader "Overtime/Building"
{
    Properties { _Base("Base", Color) = (0.03,0.02,0.07,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Forward" Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "OTCommon.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Base; CBUFFER_END
            half4 _OTFog; float _OTFarFog;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; float id : TEXCOORD2; };
            V vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                float3 c = TransformObjectToWorld(float3(0, 0, 0));
                o.id = floor(c.x * 3.7 + c.z * 1.3);
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                float3 W = i.positionWS; half3 n = i.normalWS; float vId = i.id;
                float bh = ot_h21(float2(vId, 3.1));
                // night look: dark towers with lit windows
                half3 night = _Base.rgb * (0.7 + 0.8 * bh);
                // day look: toy-box pastel facades with glass panes
                float pick = ot_h21(float2(vId, 11.3));
                half3 pastel = pick < 0.2 ? half3(1.0, 0.74, 0.34) : pick < 0.4 ? half3(1.0, 0.42, 0.36) : pick < 0.6 ? half3(0.3, 0.82, 0.68) : pick < 0.8 ? half3(0.32, 0.6, 1.0) : half3(0.7, 0.5, 1.0);
                float shade = abs(n.y) > 0.5 ? 1.05 : (abs(n.x) > 0.5 ? 0.78 : 0.92);
                half3 day = pastel * shade * (0.82 + 0.3 * smoothstep(-60.0, 30.0, W.y));
                if (abs(n.y) < 0.5) {
                    float u = abs(n.x) > 0.5 ? W.z : W.x;
                    float sx = lerp(0.7, 1.15, ot_h21(float2(vId, 7.7)));
                    float2 g = float2(u * sx, W.y * 0.72);
                    float2 id = floor(g) + vId; float2 f = frac(g);
                    float h = ot_h21(id);
                    float floorOn = step(0.35, ot_h21(float2(floor(g.y), vId)));
                    float lit = step(0.48, h) * floorOn;
                    lit *= lerp(1.0, step(0.4, ot_h21(id + floor(_OTTime * 9.0))), _OTOver);
                    float w = smoothstep(0.16, 0.22, f.x) * smoothstep(0.84, 0.78, f.x) * smoothstep(0.25, 0.32, f.y) * smoothstep(0.78, 0.7, f.y);
                    half3 wc = lerp(_OTWin.rgb, _OTWin2.rgb, step(0.82, ot_h21(id * 1.31)));
                    night += wc * w * lit * (0.35 + 0.9 * ot_h21(id * 1.7));
                    float bandY = floor(bh * 9.0) * 6.0 - 20.0;
                    night += _OTWin2.rgb * smoothstep(0.22, 0.1, abs(W.y - bandY)) * step(0.55, bh) * 1.6;
                    float banner = smoothstep(0.04, 0.0, abs(frac(u * 0.17 + vId) - 0.5) - 0.47);
                    banner *= step(0.72, ot_h21(float2(vId, 4.4))) * smoothstep(-5.0, 12.0, W.y) * (1.0 - smoothstep(40.0, 80.0, W.y));
                    night += _OTWin2.rgb * banner * 0.9;
                    night += _OTWin.rgb * 0.035 * smoothstep(-40.0, 20.0, W.y);
                    half3 glass = lerp(half3(0.25, 0.42, 0.62), half3(0.75, 0.9, 1.0), smoothstep(0.3, 0.75, f.y + f.x * 0.3)) * shade;
                    day = lerp(day, glass, w * 0.92);
                    day *= 1.0 - 0.1 * step(0.94, f.y);
                } else night *= 1.4;
                // a warm horizon band so the towers catch the sky the way a diorama catches sunset
                float horGlow = exp(-pow(W.y * 0.012, 2.0)) * (1.0 - abs(n.y));
                night += _OTHor.rgb * horGlow * 0.38;
                half3 col = lerp(night, day, _OTDay);
                float d = length(W - _WorldSpaceCameraPos);
                float fog = 1.0 - exp(-_OTFarFog * _OTFarFog * d * d);
                col = lerp(col, _OTFog.rgb, fog);
                col = lerp(col, _OTAbyss.rgb, smoothstep(-8.0, -75.0, W.y) * lerp(0.92, 0.55, _OTDay));
                return half4(col, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}

// Additive effects: soft particles (_Mode 0), ribbons (1), wall chevrons (2), landing pads (3), finish film (4).
Shader "Overtime/Fx"
{
    Properties { [HDR] _Tint("Tint", Color) = (1,1,1,1) _Mode("Mode", Float) = 0 _Len("Length", Float) = 1 _Dir("Dir", Float) = 1 _Used("Used", Float) = 0 }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Blend One One ZWrite Off Cull Off
        Pass
        {
            Name "Forward" Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "OTCommon.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Tint; float _Mode; float _Len; float _Dir; float _Used; CBUFFER_END
            struct A { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };
            V vert(A i) { V o; o.positionWS = TransformObjectToWorld(i.positionOS.xyz); o.positionCS = TransformWorldToHClip(o.positionWS); o.color = i.color; o.uv = i.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                half3 tint = _Tint.rgb * i.color.rgb;
                float a = i.color.a;
                if (_Mode < 0.5) { float d = length(i.uv - 0.5) * 2.0; float s = smoothstep(1.0, 0.0, d); a *= s * s; tint *= 0.45 + 0.9 * s; }
                else if (_Mode < 1.5) { a *= smoothstep(0.0, 0.25, i.uv.y) * smoothstep(1.0, 0.75, i.uv.y); }
                else if (_Mode < 2.5) {
                    float x = i.uv.x * _Len * 0.42 * _Dir - _OTTime * 1.6;
                    float c = frac(x - abs(i.uv.y - 0.5) * 1.5);
                    float chev = smoothstep(0.0, 0.06, c) * smoothstep(0.42, 0.36, c);
                    float frame = step(i.uv.y, 0.05) + step(0.95, i.uv.y);
                    a = chev * 1.05 * (0.85 + 0.15 * sin(i.positionWS.y * 40.0 + _OTTime * 6.0)) + frame * 1.5 + 0.06;
                    tint = _OTAccent.rgb;
                }
                else if (_Mode < 3.5) {
                    float2 p = i.uv - 0.5; p.y *= 2.4;
                    float r = max(abs(p.x), abs(p.y) * 0.42) * 2.0;
                    float rings = smoothstep(0.1, 0.0, abs(frac(r * 2.0 - _OTTime * 0.9) - 0.5) - 0.32) * smoothstep(1.0, 0.9, r);
                    float border = smoothstep(0.06, 0.0, abs(r - 0.94));
                    a = (rings * 0.5 + border * 1.4 + 0.06) * lerp(1.0, 0.12, _Used) * 1.6;
                }
                else {
                    float sweep = frac(i.uv.y * 3.0 - _OTTime * 0.8);
                    float n = ot_vnoise(float2(i.uv.x * 14.0, i.uv.y * 6.0 - _OTTime * 2.0));
                    a = (0.12 + 0.5 * smoothstep(0.7, 1.0, sweep) + 0.25 * n * (0.5 + 0.5 * sin(i.uv.x * 70.0))) * smoothstep(0.0, 0.08, i.uv.y) * 2.2;
                    tint = _OTAccent.rgb;
                }
                return half4(tint * a, 1);
            }
            ENDHLSL
        }
    }
}

// Wet rooftop: concrete slabs, painted lit markings, puddles that mirror the sky and the sun-clock,
// real shadows from the main light, plus a soft contact shadow and under-glow from the runner.
Shader "Overtime/Roof"
{
    Properties { _Base("Base", Color) = (0.11,0.10,0.22,1) _PlayHalf("Play half width", Float) = 3.9 _Lane("Lane width", Float) = 2.4 }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "OTCommon.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Base; float _PlayHalf; float _Lane; CBUFFER_END
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float2 roof : TEXCOORD1; half fogFactor : TEXCOORD2; };
            V vert(A i)
            {
                V o; VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS; o.roof = i.uv; o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            float ln(float d, float w) { float fw = fwidth(d) * 1.2; return 1.0 - smoothstep(w, w + fw, abs(d)); }
            half4 frag(V i) : SV_Target
            {
                float3 W = i.positionWS;
                float3 Vd = normalize(_WorldSpaceCameraPos - W);
                float dist = length(_WorldSpaceCameraPos - W);
                float n1 = ot_vnoise(W.xz * 0.33), n2 = ot_vnoise(W.xz * 0.41 + 7.0), grain = ot_vnoise(W.xz * 2.6);
                float wet = smoothstep(0.3, 0.85, ot_vnoise(W.xz * 0.11 + 3.0) + 0.1 * grain);
                float rough = lerp(0.05, 0.02, wet) * smoothstep(120.0, 8.0, dist);
                float3 N = normalize(float3((n1 - 0.5) * rough, 1.0, (n2 - 0.5) * rough));
                float3 R = reflect(-Vd, N);
                float fres = pow(1.0 - max(dot(Vd, N), 0.0), 4.0);

                float inPlayPre = step(abs(W.x), _PlayHalf);
                half3 dayBase = lerp(half3(0.36, 0.42, 0.62), half3(0.56, 0.6, 0.76), inPlayPre) * lerp(half3(1, 1, 1), _OTAmbSky.rgb, 0.25);
                half3 base = lerp(_Base.rgb, dayBase, _OTDay) * (0.8 + 0.4 * grain) * lerp(1.0, lerp(0.75, 0.9, _OTDay), wet);
                float inPlay = step(abs(W.x), _PlayHalf);
                base *= lerp(0.62, 1.0, inPlay);
                float seam = max(ln(frac(W.z / 6.0) - 0.5, 0.004), ln(frac(W.x / 2.6 + 0.5) - 0.5, 0.006) * (1.0 - inPlay));
                base *= 1.0 - 0.55 * seam;

                Light l = GetMainLight(TransformWorldToShadowCoord(W));
                half3 col = base * (_OTAmbSky.rgb * lerp(0.9, 0.62, _OTDay) + l.color * saturate(l.direction.y) * l.shadowAttenuation * lerp(0.7, 0.62, _OTDay));

                half3 refl = min(ot_sky(R, 0.0), 0.95) * 0.62;
                col = lerp(col, refl, clamp(fres * (0.5 + 0.5 * wet) + wet * 0.05, 0.0, 0.9) * (0.45 + 0.55 * l.shadowAttenuation) * lerp(1.0, 0.3, _OTDay));

                float2 rd = W.xz - _OTRunner.xz;
                float h = max(0.0, _OTRunner.w);
                col *= 1.0 - 0.45 * exp(-dot(rd, rd) * (5.0 / (1.0 + h * 1.4))) / (1.0 + h * 0.55);
                col += _OTAccent.rgb * 0.1 * exp(-dot(rd, rd) * 0.3) / (1.0 + h) * (1.0 - _OTDay);

                float edge = ln(abs(W.x) - (_PlayHalf + 0.05), 0.035);
                float dash = step(0.45, frac(W.z / 3.0));
                float lanes = ln(abs(W.x) - _Lane * 0.5, 0.022) * dash;
                float toEnd = i.roof.y - i.roof.x;
                float lip = smoothstep(0.34, 0.26, toEnd) * inPlay;
                float chev = step(0.5, frac((toEnd + abs(W.x)) * 0.7)) * smoothstep(2.2, 2.0, toEnd) * smoothstep(0.34, 0.42, toEnd) * inPlay;
                float startLip = smoothstep(0.16, 0.1, i.roof.x) * inPlay;
                half3 em = _OTAccent.rgb * (edge * 1.5 + lanes * 0.55 + startLip * 0.9) + half3(1.0, 0.55, 0.12) * chev * 0.28 + half3(1.6, 1.5, 1.4) * lip;
                // by day the markings are paint, not light
                float paint = saturate(edge + lanes + startLip + lip);
                half3 dayCol = lerp(col, half3(1.0, 1.0, 1.0) * (0.55 + 0.45 * l.shadowAttenuation), paint * 0.9);
                dayCol = lerp(dayCol, half3(1.0, 0.78, 0.1) * (0.6 + 0.4 * l.shadowAttenuation), chev * 0.9);
                col = lerp(col + em * smoothstep(240.0, 60.0, dist), dayCol, _OTDay);
                return half4(MixFog(col, i.fogFactor), 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}

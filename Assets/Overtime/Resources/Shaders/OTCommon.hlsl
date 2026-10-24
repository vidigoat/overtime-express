#ifndef OT_COMMON
#define OT_COMMON
// Globals driven every frame by Mood.cs: the whole world blends between calm, running-low and overtime.
half4 _OTTop, _OTHor, _OTSun, _OTAccent, _OTAbyss, _OTWin, _OTWin2, _OTAmbSky, _OTAmbGround, _OTRim;
float _OTFill, _OTOver, _OTTime, _OTDay;
float4 _OTRunner; // xyz + height above the surface

float ot_h21(float2 p){ p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
float ot_vnoise(float2 p){
  float2 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
  return lerp(lerp(ot_h21(i), ot_h21(i + float2(1,0)), f.x), lerp(ot_h21(i + float2(0,1)), ot_h21(i + float2(1,1)), f.x), f.y);
}

// The sky. Shared by the skybox and by the wet rooftops, which evaluate it along the reflected view ray.
// The sun is the clock: it drains from the top as time runs out and becomes an eclipse in overtime.
half3 ot_sky(float3 d, float detail){
  float h = saturate(d.y * 1.5 + 0.1);
  half3 col = lerp(_OTHor.rgb, _OTTop.rgb, pow(h, 0.5));
  col += _OTHor.rgb * 0.78 * exp(-abs(d.y) * 5.2);
  if (detail > 0.5) {
    float band = smoothstep(0.02, 0.16, d.y) * smoothstep(0.5, 0.2, d.y);
    float cl = ot_vnoise(float2(atan2(d.x, d.z) * 5.0 + _OTTime * 0.01, d.y * 34.0));
    col = lerp(col, _OTHor.rgb * 1.25 + 0.03, smoothstep(0.58, 0.9, cl) * band * 0.4 * (1.0 - _OTDay));
    // daytime: big soft cumulus drifting across the upper sky
    float2 cuv = d.xz / (d.y + 0.3) * 1.3 + float2(_OTTime * 0.012, 0.0);
    float puff = ot_vnoise(cuv * 1.6) * 0.62 + ot_vnoise(cuv * 4.3 + 3.0) * 0.38;
    float cloud = smoothstep(0.52, 0.74, puff) * smoothstep(0.04, 0.22, d.y) * _OTDay;
    col = lerp(col, lerp(float3(1.0, 1.0, 1.0), _OTHor.rgb, 0.25) * 0.98, cloud * 0.9);
    float2 sp = d.xz / (abs(d.y) + 0.35) * 90.0;
    float sh = ot_h21(floor(sp));
    float star = step(0.99, sh) * smoothstep(0.35, 0.0, length(frac(sp) - 0.5) * 2.0);
    col += star * smoothstep(0.12, 0.5, d.y) * (0.55 + 0.45 * sin(_OTTime * 2.0 + sh * 60.0)) * (1.0 - _OTOver * 0.6) * (1.0 - _OTDay);
  }
  if (d.z > 0.05) {
    float2 p = d.xy / d.z - float2(0.0, 0.2);
    float r = length(p) / 0.25;
    col += _OTSun.rgb * exp(-r * r * 0.55) * (0.3 + 0.25 * _OTOver) * (1.0 - 0.6 * _OTDay);
    if (r < 1.25) {
      float yy = p.y / 0.25 * 0.5 + 0.5;
      float disc = smoothstep(1.0, 0.975, r);
      float bands = min(1.0, step(0.5, frac(yy * 8.0 - _OTTime * 0.12)) + step(0.46, yy));
      float filled = smoothstep(_OTFill + 0.012, _OTFill - 0.012, yy);
      half3 sun = lerp(_OTSun.rgb, half3(1.0, 0.93, 0.72), smoothstep(0.1, 0.95, yy));
      float body = disc * bands * lerp(0.07, 1.0, filled) * (1.0 - _OTOver);
      col = lerp(col, sun * 1.25, body);
      col = lerp(col, 0, disc * _OTOver);
      float rim = smoothstep(0.1, 0.0, abs(r - 1.0));
      col += _OTSun.rgb * rim * (0.5 + _OTOver * (1.6 + 0.6 * sin(_OTTime * 13.0)));
    }
  }
  return col;
}
#endif

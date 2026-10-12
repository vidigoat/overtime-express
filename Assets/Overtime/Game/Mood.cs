using Overtime.Core;
using UnityEngine;

namespace Overtime
{
    /// <summary>
    /// The sky is the clock. Each district has its own sunny look; as time runs low the city slides into sunset,
    /// and in overtime it flips to neon night.
    /// </summary>
    public class Mood
    {
        struct Pal { public Color top, hor, fog, win, win2, accent, abyss, light, sun; public float day; }
        static Pal P(int top, int hor, int fog, int win, int win2, int accent, int abyss, int light, int sun, float day) => new Pal
        { top = Mats.Hex(top), hor = Mats.Hex(hor), fog = Mats.Hex(fog), win = Mats.Hex(win), win2 = Mats.Hex(win2), accent = Mats.Hex(accent), abyss = Mats.Hex(abyss), light = Mats.Hex(light), sun = Mats.Hex(sun), day = day };
        // every district is a different neon night; the sky still tells the time
        static readonly Pal[] Districts =
        {
            P(0x05021c, 0x6a22e0, 0x170a3a, 0x39e6ff, 0xff5ad1, 0x39e6ff, 0x3a0f6e, 0x8f9cff, 0xff3d81, 0f),   // violet core
            P(0x02101c, 0x0fb89a, 0x06282e, 0x4dffc3, 0xffe14a, 0x4dffc3, 0x0a5a52, 0x8fffe0, 0xffb020, 0f),   // teal docks
            P(0x0a0224, 0xe0229a, 0x2a0a3a, 0xff5ad1, 0x39e6ff, 0xff5ad1, 0x6e0f5a, 0xff9cdc, 0x39e6ff, 0f),   // magenta strip
            P(0x02061e, 0x2266ff, 0x0a143c, 0x66aaff, 0xffffff, 0x66aaff, 0x0f2a7a, 0x9cb8ff, 0xffe14a, 0f),   // blue heights
            P(0x100318, 0xff8a1f, 0x2a1020, 0xffb347, 0xff5ad1, 0xffb347, 0x5a2410, 0xffc890, 0xff5a3c, 0f),   // amber market
            P(0x04020f, 0x9a3cff, 0x120830, 0xd9c8ff, 0xff3d81, 0xb98cff, 0x2a0f6e, 0xb4a4ff, 0xff3d81, 0f),   // clock tower
        };
        static readonly Pal Sunset = P(0x120312, 0xff6a1f, 0x33101a, 0xffb347, 0xff5a3c, 0xffa62b, 0x6a2410, 0xffb690, 0xff7a1a, 0f);
        static readonly Pal Over = P(0x000000, 0xc8001a, 0x160003, 0xff1030, 0xffe0e0, 0xff2040, 0x2a0004, 0xff7070, 0xff0020, 0f);

        public float tension, over, fill = 1, day = 1;
        public Color accent, fog, light, sun;
        public int district;

        static Color L3(Color a, Color b, Color c, float t, float o) => Color.Lerp(Color.Lerp(a, b, t), c, o);

        public void Update(Sim sim, float rdt, Light key, float time)
        {
            var calm = Districts[Mathf.Clamp(district, 0, Districts.Length - 1)];
            float tt = sim.overtime ? 1 : Mathf.Clamp01(1 - sim.clock / 12f);
            tension += (tt - tension) * Mathf.Min(1, rdt * 3);
            over += ((sim.overtime ? 1 : 0) - over) * Mathf.Min(1, rdt * 6);
            fill += (Mathf.Clamp01(sim.clock / Sim.ClockCap) * 0.92f + 0.08f - fill) * Mathf.Min(1, rdt * 5);
            float t = tension, o = over;
            day = Mathf.Lerp(Mathf.Lerp(calm.day, Sunset.day, t), Over.day, o);
            accent = L3(calm.accent, Sunset.accent, Over.accent, t, o);
            fog = L3(calm.fog, Sunset.fog, Over.fog, t, o);
            light = L3(calm.light, Sunset.light, Over.light, t, o);
            sun = L3(calm.sun, Sunset.sun, Over.sun, t, o);
            var abyss = L3(calm.abyss, Sunset.abyss, Over.abyss, t, o);
            Shader.SetGlobalColor("_OTTop", L3(calm.top, Sunset.top, Over.top, t, o));
            Shader.SetGlobalColor("_OTHor", L3(calm.hor, Sunset.hor, Over.hor, t, o));
            Shader.SetGlobalColor("_OTSun", sun);
            Shader.SetGlobalColor("_OTAccent", accent);
            Shader.SetGlobalColor("_OTAbyss", abyss);
            Shader.SetGlobalColor("_OTWin", L3(calm.win, Sunset.win, Over.win, t, o));
            Shader.SetGlobalColor("_OTWin2", L3(calm.win2, Sunset.win2, Over.win2, t, o));
            Shader.SetGlobalColor("_OTFog", fog);
            var ambSky = light * Mathf.Lerp(0.62f, 0.78f, day);
            var ambGround = Color.Lerp(abyss * 0.55f, light * 0.5f, day);
            Shader.SetGlobalColor("_OTAmbSky", ambSky);
            Shader.SetGlobalColor("_OTAmbGround", ambGround);
            Shader.SetGlobalColor("_OTRim", Color.Lerp(accent, sun, 0.35f) * Mathf.Lerp(1f, 0.35f, day));
            Shader.SetGlobalFloat("_OTFill", fill);
            Shader.SetGlobalFloat("_OTOver", over);
            Shader.SetGlobalFloat("_OTTime", time);
            Shader.SetGlobalFloat("_OTDay", day);
            Shader.SetGlobalFloat("_OTFarFog", Mathf.Lerp(0.0042f, 0.0015f, day));
            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = Mathf.Lerp(0.0105f, 0.0034f, day);
            RenderSettings.ambientLight = Color.Lerp(ambGround, ambSky, 0.6f) * 1.25f;
            if (key) { key.color = Color.Lerp(Color.white, light, 0.5f); key.intensity = Mathf.Lerp(1.1f, 1.75f, day); }
            foreach (var m in Mats.accentGlows) m.SetColor("_Tint", accent * Mathf.Lerp(2.2f, 1.0f, day));
        }

        public static Color Flow(int chain)
        {
            float t = Mathf.Min(1f, chain / 16f);
            Color a = Mats.Hex(0x39e6ff), b = Mats.Hex(0xff3dd4), c = Mats.Hex(0xffe14a);
            return t < 0.5f ? Color.Lerp(a, b, t * 2) : Color.Lerp(b, c, (t - 0.5f) * 2);
        }
    }
}

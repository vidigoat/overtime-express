using System.Collections.Generic;
using UnityEngine;

namespace Overtime
{
    /// <summary>Sound effects synthesised in code (no audio files yet): short, bright, arcade-style.</summary>
    public class Sfx
    {
        readonly AudioSource src;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        const int Rate = 44100;
        public bool muted;

        public Sfx(Transform parent)
        {
            var g = new GameObject("Sfx"); g.transform.SetParent(parent, false);
            src = g.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0;
            Make("jump", 0.16f, (t, k) => Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(280, 640, k) * t) * (1 - k));
            Make("land", 0.1f, (t, k) => Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(130, 55, k) * t) * (1 - k));
            Make("coin", 0.14f, (t, k) => (Mathf.Sin(2 * Mathf.PI * (k < 0.35f ? 1318.5f : 1760f) * t) + 0.3f * Mathf.Sin(2 * Mathf.PI * 2637f * t)) * (1 - k) * 0.6f);
            Make("capsule", 0.3f, (t, k) => Mathf.Sin(2 * Mathf.PI * (k < 0.3f ? 880f : 1318.5f) * t) * (1 - k) * 0.7f);
            Make("slide", 0.35f, (t, k) => (Random.value * 2 - 1) * (1 - k) * 0.35f);
            Make("dash", 0.28f, (t, k) => ((Random.value * 2 - 1) * 0.4f + Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(220, 900, k) * t) * 0.4f) * (1 - k));
            Make("hit", 0.32f, (t, k) => ((Random.value * 2 - 1) * 0.6f + Mathf.Sign(Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(180, 40, k) * t)) * 0.4f) * (1 - k));
            Make("lane", 0.07f, (t, k) => (Random.value * 2 - 1) * (1 - k) * 0.18f);
            Make("ot", 0.5f, (t, k) => Mathf.Sign(Mathf.Sin(2 * Mathf.PI * (Mathf.Floor(k * 6) % 2 < 1 ? 880f : 587f) * t)) * 0.35f * (1 - k * 0.3f));
            Make("win", 0.8f, (t, k) => { float[] n = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }; int i = Mathf.Min(4, (int)(k * 6)); return Mathf.Sin(2 * Mathf.PI * n[i] * t) * (1 - k) * 0.6f; });
            Make("buy", 0.4f, (t, k) => { float[] n = { 659.25f, 987.77f, 1318.5f }; int i = Mathf.Min(2, (int)(k * 4)); return Mathf.Sin(2 * Mathf.PI * n[i] * t) * (1 - k) * 0.6f; });
            Make("beep", 0.09f, (t, k) => Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 660f * t)) * 0.25f * (1 - k));
            Make("crack", 0.2f, (t, k) => (Random.value * 2 - 1) * Mathf.Pow(1 - k, 3) * 0.7f);
            for (int i = 0; i < 12; i++)
            {
                float f = 440f * Mathf.Pow(2f, new[] { 0, 3, 5, 7, 10, 12, 15, 17, 19, 22, 24, 27 }[i] / 12f);
                Make("trick" + i, 0.18f, (t, k) => (Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * t)) * 0.2f + Mathf.Sin(2 * Mathf.PI * f * 2 * t) * 0.3f) * (1 - k));
            }
        }

        void Make(string name, float len, System.Func<float, float, float> f)
        {
            int n = (int)(len * Rate); var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate, i / (float)n), -1, 1) * 0.5f;
            var c = AudioClip.Create(name, n, 1, Rate, false); c.SetData(data, 0); clips[name] = c;
        }

        public void Play(string name, float vol = 1f) { if (!muted && clips.TryGetValue(name, out var c)) src.PlayOneShot(c, vol); }
        public void Trick(int chain) => Play("trick" + Mathf.Clamp(chain - 1, 0, 11), 0.9f);
    }
}

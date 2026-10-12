using System.Collections.Generic;
using UnityEngine;

namespace Overtime
{
    /// <summary>The model kit: one mesh per named component (runner parts, obstacles, rooftop props).</summary>
    public class KitLibrary : ScriptableObject
    {
        [System.Serializable] public class Entry { public string name; public Mesh mesh; }
        public List<Entry> entries = new List<Entry>();
        Dictionary<string, Mesh> map;

        public Mesh Get(string n)
        {
            if (map == null) { map = new Dictionary<string, Mesh>(); foreach (var e in entries) map[e.name] = e.mesh; }
            return map.TryGetValue(n, out var m) ? m : null;
        }

        static KitLibrary inst;
        public static KitLibrary I => inst ??= Resources.Load<KitLibrary>("Kit");
    }

    /// <summary>Shared materials. Anything that glows goes through Glow so bloom only catches true light sources.</summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        public static readonly List<Material> accentGlows = new List<Material>();

        static Material Make(string shader) => new Material(Shader.Find(shader)) { enableInstancing = true };

        public static Material Lit(Color tint, float rim = 0.5f)
        {
            string k = $"lit{tint}{rim}";
            if (!cache.TryGetValue(k, out var m)) { m = Make("Overtime/Lit"); m.SetColor("_Tint", tint); m.SetFloat("_Rim", rim); cache[k] = m; }
            return m;
        }
        public static Material Glow(Color c, float intensity = 1.9f)
        {
            string k = $"glow{c}{intensity}";
            if (!cache.TryGetValue(k, out var m)) { m = Make("Overtime/Glow"); m.SetColor("_Tint", c * intensity); cache[k] = m; }
            return m;
        }
        /// <summary>A glow material whose colour follows the mood accent.</summary>
        public static Material AccentGlow()
        {
            if (!cache.TryGetValue("accent", out var m)) { m = Make("Overtime/Glow"); cache["accent"] = m; accentGlows.Add(m); }
            return m;
        }
        public static Material Fx(int mode, Color tint)
        {
            var m = Make("Overtime/Fx"); m.SetFloat("_Mode", mode); m.SetColor("_Tint", tint); return m;
        }
        public static Material Roof => cache.TryGetValue("roof", out var m) ? m : cache["roof"] = Make("Overtime/Roof");
        public static Material Building => cache.TryGetValue("bld", out var m) ? m : cache["bld"] = Make("Overtime/Building");

        static Mesh cube;
        public static Mesh Cube
        {
            get
            {
                if (cube == null) { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); cube = g.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(g); }
                return cube;
            }
        }
        static Mesh quad;
        public static Mesh Quad
        {
            get
            {
                if (quad == null) { var g = GameObject.CreatePrimitive(PrimitiveType.Quad); quad = g.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(g); }
                return quad;
            }
        }
        public static Color Hex(int h, float k = 1f) => new Color(((h >> 16) & 255) / 255f, ((h >> 8) & 255) / 255f, (h & 255) / 255f, 1f).linear * k;
    }
}

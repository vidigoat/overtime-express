using System.Collections.Generic;
using UnityEngine;

namespace Overtime
{
    /// <summary>Imported art: models (characters, city pieces, coins, food) and the character's animation clips, by name.</summary>
    public class ArtLibrary : ScriptableObject
    {
        [System.Serializable] public class Model { public string name; public GameObject prefab; }
        public List<Model> models = new List<Model>();
        public List<AnimationClip> clips = new List<AnimationClip>();
        Dictionary<string, GameObject> mm; Dictionary<string, AnimationClip> cm;

        public GameObject Model_(string n)
        {
            if (mm == null) { mm = new Dictionary<string, GameObject>(); foreach (var m in models) mm[m.name] = m.prefab; }
            return mm.TryGetValue(n, out var g) ? g : null;
        }
        public AnimationClip Clip(string n)
        {
            if (cm == null) { cm = new Dictionary<string, AnimationClip>(); foreach (var c in clips) if (c) cm[c.name.Substring(c.name.LastIndexOf('|') + 1)] = c; }
            return cm.TryGetValue(n, out var g) ? g : null;
        }

        /// <summary>Instantiate a model under a parent. Shadows on by default.</summary>
        public Transform Spawn(string n, Transform parent, Vector3 pos, Quaternion rot, float scale = 1f, bool shadows = true)
        {
            var p = Model_(n); if (p == null) return null;
            var g = Instantiate(p, parent);
            g.name = n; g.transform.SetPositionAndRotation(pos, rot); g.transform.localScale = Vector3.one * scale;
            if (!shadows) foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g.transform;
        }

        static ArtLibrary inst;
        public static ArtLibrary I => inst ??= Resources.Load<ArtLibrary>("Art");
    }
}

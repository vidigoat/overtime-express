using System.Collections.Generic;
using UnityEngine;

namespace Overtime
{
    /// <summary>
    /// Builds the people of the city from the designed character models: recolours their outfits, gives them
    /// friendlier proportions, and patches a quirk of their rigs (the feet hang off the root, not the legs).
    /// </summary>
    public class CharBody
    {
        public Transform model, head, chest;
        readonly Transform[] feet = new Transform[4], ankles = new Transform[4];
        readonly Vector3[] footLocalPos = new Vector3[4];
        readonly Quaternion[] footLocalRot = new Quaternion[4];
        readonly float headScale;
        public float legScale = 1f;
        Transform legL, legR;
        /// <summary>How far the feet lift off the ground when the legs are shortened.</summary>
        public float GroundDrop => legL ? (1 - legScale) * legLength : 0;
        float legLength;

        /// <summary>The courier's uniform: yellow rider helmet, hot-orange company vest, slate work gear, white runners.</summary>
        public static readonly Dictionary<string, Color> Courier = new Dictionary<string, Color>
        {
            { "Worker_Yellow", new Color(1.0f, 0.72f, 0.06f) * 1.2f }, { "Worker_Vest", new Color(1.0f, 0.3f, 0.08f) * 1.15f },
            { "LightBrown", new Color(0.2f, 0.26f, 0.5f) }, { "Brown", new Color(0.13f, 0.16f, 0.3f) }, { "Brown2", new Color(0.19f, 0.23f, 0.4f) },
            { "Black", new Color(0.92f, 0.94f, 1f) }, { "Grey", new Color(0.55f, 0.6f, 0.75f) }, { "Skin", new Color(0.78f, 0.5f, 0.33f) },
            { "Moustache", new Color(0.78f, 0.5f, 0.33f) }, { "Eyebrows", new Color(0.36f, 0.2f, 0.1f) }, { "Eye", new Color(0.03f, 0.03f, 0.05f) },
        };

        /// <summary>Kip in shorts and a company jersey: the friendlier face of the two hero options.</summary>
        public static readonly Dictionary<string, Color> CourierCasual = new Dictionary<string, Color>
        {
            { "Shirt", new Color(1.0f, 0.3f, 0.08f) * 1.15f }, { "Pants", new Color(0.13f, 0.16f, 0.34f) }, { "Socks", new Color(0.95f, 0.96f, 1f) },
            { "Skin", new Color(0.8f, 0.52f, 0.36f) }, { "Hair", new Color(0.14f, 0.08f, 0.05f) }, { "Eyes", new Color(0.03f, 0.03f, 0.05f) },
        };

        /// <summary>
        /// Build the hero courier under a parent: body, delivery pack with the glowing clock, strap, headlamp and lit soles.
        /// variant "Worker" = helmet and hi-vis vest; "Casual" = jersey, shorts and a courier cap.
        /// </summary>
        public static CharBody BuildCourier(Transform parent, string variant, float scale, Material flowGlow, Material clockGlow, out Transform pack)
        {
            bool casual = variant == "Casual";
            var b = new CharBody(variant, parent, casual ? scale * 0.4f : scale, casual ? 1.3f : 1.16f, casual ? CourierCasual : Courier, 1.5f, 0.5f);
            var yellow = Mats.Lit(new Color(2.3f, 1.6f, 0.16f), 0.6f); var ink = Mats.Lit(new Color(0.08f, 0.09f, 0.2f), 0.6f);
            Transform Box(Transform bone, Vector3 off, Vector3 euler, Vector3 size, Material m, PrimitiveType type = PrimitiveType.Cube)
            {
                if (!bone) return null;
                var g = GameObject.CreatePrimitive(type); Object.Destroy(g.GetComponent<Collider>());
                var at = b.Attach(bone, off, euler, size); g.transform.SetParent(at, false);
                var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m;
                if (m == flowGlow || m == clockGlow) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return at;
            }
            pack = null;
            if (b.chest)
            {
                pack = b.Attach(b.chest, new Vector3(0, 0.0f, -0.26f), new Vector3(2, 180, 0), Vector3.one * 1.1f);
                var kit = KitLibrary.I;
                foreach (var n in new[] { "r_pack", "r_pack__glow" })
                {
                    var m = kit.Get(n); if (m == null) continue;
                    var g = new GameObject(n); g.transform.SetParent(pack, false);
                    g.AddComponent<MeshFilter>().sharedMesh = m;
                    g.AddComponent<MeshRenderer>().sharedMaterial = n.EndsWith("__glow") ? clockGlow : yellow;
                }
                // pack straps over both shoulders, and a lit company badge on the chest
                Box(b.chest, new Vector3(-0.115f, 0.03f, 0.125f), new Vector3(-6, 0, 0), new Vector3(0.045f, 0.3f, 0.02f), ink);
                Box(b.chest, new Vector3(0.115f, 0.03f, 0.125f), new Vector3(-6, 0, 0), new Vector3(0.045f, 0.3f, 0.02f), ink);
                Box(b.chest, new Vector3(-0.115f, 0.17f, 0.02f), Vector3.zero, new Vector3(0.045f, 0.02f, 0.24f), ink);
                Box(b.chest, new Vector3(0.115f, 0.17f, 0.02f), Vector3.zero, new Vector3(0.045f, 0.02f, 0.24f), ink);
                Box(b.chest, new Vector3(0.115f, 0.06f, 0.14f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.015f), flowGlow);
            }
            if (b.head)
            {
                if (casual)
                {
                    // courier cap, worn forwards, with a lit trim on the brim
                    Box(b.head, new Vector3(0, 0.2f, -0.01f), Vector3.zero, new Vector3(0.3f, 0.2f, 0.32f), yellow, PrimitiveType.Sphere);
                    Box(b.head, new Vector3(0, 0.185f, 0.18f), new Vector3(10, 0, 0), new Vector3(0.24f, 0.025f, 0.16f), yellow);
                    Box(b.head, new Vector3(0, 0.215f, 0.14f), Vector3.zero, new Vector3(0.1f, 0.05f, 0.03f), flowGlow);
                }
                else
                {
                    Box(b.head, new Vector3(0, 0.2f, 0.13f), Vector3.zero, new Vector3(0.1f, 0.045f, 0.04f), flowGlow);     // helmet lamp
                    // rider goggles: big round lit lenses give him a friendly, eager face
                    Box(b.head, new Vector3(0, 0.088f, 0.1f), Vector3.zero, new Vector3(0.235f, 0.07f, 0.06f), ink);
                    Box(b.head, new Vector3(-0.052f, 0.088f, 0.128f), new Vector3(90, 0, 0), new Vector3(0.085f, 0.012f, 0.085f), flowGlow, PrimitiveType.Cylinder);
                    Box(b.head, new Vector3(0.052f, 0.088f, 0.128f), new Vector3(90, 0, 0), new Vector3(0.085f, 0.012f, 0.085f), flowGlow, PrimitiveType.Cylinder);
                    Box(b.head, new Vector3(0, 0.088f, -0.005f), Vector3.zero, new Vector3(0.245f, 0.035f, 0.22f), ink);     // strap round the helmet
                    // a courier's neck gaiter pulled up over the mouth, with a knot flying behind
                    var orange = Mats.Lit(new Color(1.2f, 0.36f, 0.1f), 0.6f);
                    Box(b.head, new Vector3(0, 0.005f, 0.075f), new Vector3(8, 0, 0), new Vector3(0.16f, 0.04f, 0.05f), orange);
                    Box(b.head, new Vector3(0, -0.02f, -0.01f), Vector3.zero, new Vector3(0.15f, 0.035f, 0.1f), orange);
                    Box(b.head, new Vector3(0.05f, -0.03f, -0.15f), new Vector3(-25, 12, 0), new Vector3(0.025f, 0.015f, 0.16f), orange);
                    Box(b.head, new Vector3(-0.04f, -0.05f, -0.16f), new Vector3(-40, -10, 0), new Vector3(0.025f, 0.015f, 0.14f), orange);
                }
            }
            Box(Find(b.model, "Foot.L"), new Vector3(0, -0.07f, 0.03f), Vector3.zero, new Vector3(0.1f, 0.022f, 0.25f), flowGlow);
            Box(Find(b.model, "Foot.R"), new Vector3(0, -0.07f, 0.03f), Vector3.zero, new Vector3(0.1f, 0.022f, 0.25f), flowGlow);
            return b;
        }

        public CharBody(string name, Transform parent, float scale, float headScale, Dictionary<string, Color> outfit = null, float boost = 1.5f, float rim = 0.9f)
        {
            this.headScale = headScale;
            model = ArtLibrary.I.Spawn(name, parent, Vector3.zero, Quaternion.identity, scale);
            model.localPosition = Vector3.zero; model.localRotation = Quaternion.identity;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (r.name == "Pistol") { r.gameObject.SetActive(false); continue; }
                var src = r.sharedMaterials; var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    string n = src[i] ? src[i].name : "";
                    Color c = outfit != null && outfit.TryGetValue(n, out var oc) ? oc
                        : (src[i] && src[i].HasProperty("_BaseColor") ? src[i].GetColor("_BaseColor") : Color.grey) * boost;
                    dst[i] = Mats.Lit(new Color(c.r, c.g, c.b, 1), rim);
                }
                r.sharedMaterials = dst;
                if (r is SkinnedMeshRenderer s) s.updateWhenOffscreen = true;
            }
            head = Find(model, "Head"); chest = Find(model, "Chest");
            legL = Find(model, "UpperLeg.L"); legR = Find(model, "UpperLeg.R");
            var ankle = Find(model, "LowerLeg.L_end");
            if (legL && ankle) legLength = Mathf.Abs(legL.position.y - ankle.position.y);
            // remember where each foot (and toe) sits relative to its ankle in the bind pose
            string[] f = { "Foot.L", "PT.L", "Foot.R", "PT.R" }, a = { "LowerLeg.L_end", "LowerLeg.L_end", "LowerLeg.R_end", "LowerLeg.R_end" };
            for (int i = 0; i < 4; i++)
            {
                feet[i] = Find(model, f[i]); ankles[i] = Find(model, a[i]);
                if (feet[i] && ankles[i])
                {
                    footLocalPos[i] = ankles[i].InverseTransformPoint(feet[i].position);
                    footLocalRot[i] = Quaternion.Inverse(ankles[i].rotation) * feet[i].rotation;
                }
            }
        }

        /// <summary>Call after the animation has been evaluated each frame.</summary>
        public void LateFix()
        {
            for (int i = 0; i < 4; i++)
            {
                if (!feet[i] || !ankles[i]) continue;
                var pos = ankles[i].TransformPoint(footLocalPos[i]);
                var rot = ankles[i].rotation * footLocalRot[i];
                var local = Quaternion.Inverse(model.rotation) * rot;
                var e = local.eulerAngles;
                float pitch = Mathf.DeltaAngle(0, e.x), yaw = Mathf.DeltaAngle(0, e.y);
                feet[i].SetPositionAndRotation(pos, model.rotation * Quaternion.Euler(pitch, yaw, 0));
            }
            if (head) head.localScale = Vector3.one * headScale;
            if (legL) legL.localScale = legR.localScale = new Vector3(1, legScale, 1);
        }

        public static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t) { var r = Find(c, name); if (r) return r; }
            return null;
        }

        /// <summary>Attach a prop to a bone, positioned in the model's own space while it is still in its bind pose.</summary>
        public Transform Attach(Transform bone, Vector3 modelOffset, Vector3 euler, Vector3 scale)
        {
            var t = new GameObject("Attach").transform; t.SetParent(model, false);
            t.localPosition = model.InverseTransformPoint(bone.position) + modelOffset; t.localRotation = Quaternion.Euler(euler); t.localScale = scale;
            t.SetParent(bone, true);
            return t;
        }
    }
}

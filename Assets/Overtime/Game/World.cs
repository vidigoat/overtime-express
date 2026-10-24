using System.Collections.Generic;
using Overtime.Core;
using UnityEngine;

namespace Overtime
{
    /// <summary>Builds a level's 3D world from its data: towers, wet roofs, obstacles, rails, walls, clutter, skyline.</summary>
    public class World
    {
        public const float PlayHalf = Sim.LaneW * 1.5f + 0.3f;
        public const float RoofHalf = PlayHalf + 2.9f;

        public GameObject root;
        readonly List<Transform> capsules = new List<Transform>();
        readonly List<Transform> coins = new List<Transform>();
        readonly List<Transform> powers = new List<Transform>();
        public Actor customer;
        readonly List<Material> padMats = new List<Material>();
        uint seed;
        float Rnd() { seed = seed * 1664525u + 1013904223u; return seed / 4294967296f; }

        static readonly Quaternion KitRot = Quaternion.Euler(0, 180, 0);

        GameObject Mesh(Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale, bool shadows = true)
        {
            var g = new GameObject(mesh.name);
            g.transform.SetParent(root.transform, false);
            g.transform.SetPositionAndRotation(pos, rot); g.transform.localScale = scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }
        GameObject Box(Material mat, float x, float y, float z, float sx, float sy, float sz, float ry = 0, bool shadows = true)
            => Mesh(Mats.Cube, mat, new Vector3(x, y, z), Quaternion.Euler(0, ry, 0), new Vector3(sx, sy, sz), shadows);

        /// <summary>Place a kit component (and its glow piece, if it has one).</summary>
        Transform Prop(string name, float x, float y, float z, float ryDeg = 0, float s = 1, bool shadows = true)
        {
            var kit = KitLibrary.I;
            var m = kit.Get(name);
            if (m == null) return null;
            var rot = Quaternion.Euler(0, 180 - ryDeg, 0);
            var g = Mesh(m, Mats.Lit(Color.white, 0.55f), new Vector3(x, y, z), rot, Vector3.one * s, shadows);
            var gm = kit.Get(name + "__glow");
            if (gm != null) Mesh(gm, Mats.Glow(Color.white), new Vector3(x, y, z), rot, Vector3.one * s, false);
            return g.transform;
        }

        public void Build(Level level)
        {
            if (root) Object.Destroy(root);
            root = new GameObject("World");
            capsules.Clear(); padMats.Clear(); coins.Clear(); powers.Clear(); customer?.Dispose(); customer = null;
            var art = ArtLibrary.I;
            int district = level.delivery.district;
            var coinMat = Mats.Glow(Color.white, 1.6f);
            seed = (uint)(level.length * 7919);
            var kit = KitLibrary.I;
            Material dark = Mats.Lit(Mats.Hex(0x14152b), 0.4f), mid = Mats.Lit(Mats.Hex(0x23264a), 0.4f), light = Mats.Lit(Mats.Hex(0x3a4078), 0.4f);
            Material bld = Mats.Building, accent = Mats.AccentGlow();
            bool WallAt(float s, int side) { foreach (var w in level.walls) if (w.side == side && s > w.s0 - 3 && s < w.s1 + 3) return true; return false; }
            Roof RoofUnder(float s) { foreach (var r in level.roofs) if (r.s0 <= s && r.s1 > s) return r; return null; }

            // roofs: one mesh for every wet top surface
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            // the first roof gets a run-up behind the start line, so the opening shot never looks off the back of the building
            {
                var f = level.roofs[0]; int b0 = verts.Count; const float back = 34;
                verts.Add(new Vector3(-RoofHalf, f.y, f.s0 - back)); verts.Add(new Vector3(RoofHalf, f.y, f.s0 - back));
                verts.Add(new Vector3(RoofHalf, f.y, f.s0)); verts.Add(new Vector3(-RoofHalf, f.y, f.s0));
                for (int k = 0; k < 4; k++) uvs.Add(new Vector2(50, 100));
                tris.AddRange(new[] { b0, b0 + 3, b0 + 2, b0, b0 + 2, b0 + 1 });
                Box(bld, 0, f.y - 70.06f, f.s0 - back / 2, RoofHalf * 2, 140, back, 0, false);
            }
            foreach (var r in level.roofs)
            {
                float len = r.s1 - r.s0, zc = (r.s0 + r.s1) / 2;
                int b = verts.Count;
                verts.Add(new Vector3(-RoofHalf, r.y, r.s0)); verts.Add(new Vector3(RoofHalf, r.y, r.s0));
                verts.Add(new Vector3(RoofHalf, r.y, r.s1)); verts.Add(new Vector3(-RoofHalf, r.y, r.s1));
                uvs.Add(new Vector2(0, len)); uvs.Add(new Vector2(0, len)); uvs.Add(new Vector2(len, len)); uvs.Add(new Vector2(len, len));
                tris.AddRange(new[] { b, b + 3, b + 2, b, b + 2, b + 1 });
                Box(bld, 0, r.y - 70.06f, zc, RoofHalf * 2, 140, len, 0, false);
                Box(mid, 0, r.y - 0.36f, zc, RoofHalf * 2 + 0.5f, 0.6f, len + 0.5f, 0, false);
                Box(light, 0, r.y - 1.5f, zc, RoofHalf * 2 + 0.16f, 0.2f, len + 0.16f, 0, false);
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    Box(mid, sx * (RoofHalf - 0.14f), r.y + 0.2f, zc, 0.28f, 0.4f, len);
                    Box(accent, sx * (RoofHalf - 0.14f), r.y + 0.42f, zc, 0.07f, 0.05f, len - 0.3f, 0, false);
                    float s = r.s0 + 3 + Rnd() * 4;
                    while (s < r.s1 - 4)
                    {
                        if (!WallAt(s, sx))
                        {
                            // every district dresses its rooftops differently
                            float x = sx * (PlayHalf + 1.45f + (Rnd() - 0.5f) * 0.5f);
                            var set = Dressing[Mathf.Clamp(district, 0, Dressing.Length - 1)];
                            string name = set[(int)(Rnd() * set.Length) % set.Length];
                            if (name == "sign")
                            {
                                // a neon sign pole: the strip's signature
                                float h = 2.6f + Rnd() * 3.2f; var col = Rnd() < 0.5f ? Mats.Hex(0xff3dd4) : Rnd() < 0.5f ? Mats.Hex(0x39e6ff) : Mats.Hex(0xffe14a);
                                Box(dark, x, r.y + h / 2, s, 0.16f, h, 0.16f);
                                Box(Mats.Glow(col, 1.7f), x, r.y + h * 0.72f, s, 0.2f, h * 0.5f, 0.9f, 0, false);
                                Box(Mats.Glow(col, 1.7f), x, r.y + h + 0.1f, s, 0.9f, 0.14f, 0.14f, 0, false);
                            }
                            else if (kit.Get(name) != null)
                            {
                                float ry = name == "pipes" ? 0 : name == "solar" ? (sx > 0 ? -90 : 90) : Mathf.Floor(Rnd() * 4) * 90;
                                Prop(name, x, r.y, s, ry, name == "tank" ? 0.85f : 1);
                            }
                            else art.Spawn(name, root.transform, new Vector3(x, r.y, s), Quaternion.Euler(0, Mathf.Floor(Rnd() * 4) * 90 + (Rnd() - 0.5f) * 24, 0), name.StartsWith("crate") ? 1.6f : 1.35f);
                            // a small torch on the lane side of the clutter, lining the board
                            if (Rnd() < 0.35f)
                            {
                                Box(dark, x, r.y + 0.9f, s, 0.07f, 0.9f, 0.07f);
                                Box(Mats.Glow(Mats.Hex(0xffb347), 2.4f), x, r.y + 1.45f, s, 0.18f, 0.22f, 0.18f, 0, false);
                            }
                        }
                        s += 4.5f + Rnd() * 6;
                    }
                }
            }
            var roofMesh = new Mesh { name = "Roofs" };
            roofMesh.SetVertices(verts); roofMesh.SetUVs(0, uvs); roofMesh.SetTriangles(tris, 0); roofMesh.RecalculateNormals(); roofMesh.RecalculateBounds();
            Mesh(roofMesh, Mats.Roof, Vector3.zero, Quaternion.identity, Vector3.one, false);

            // obstacles
            foreach (var o in level.obstacles)
                Prop(o.kind == ObKind.Barrier ? "barricade" : o.kind == ObKind.Beam ? "gantry" : "crate", o.lane * Sim.LaneW, o.y, o.s);

            // rails
            var railMat = Mats.Glow(Mats.Hex(0xffe14a), 1.7f);
            foreach (var r in level.rails)
            {
                float len = r.s1 - r.s0, x = r.lane * Sim.LaneW;
                Box(railMat, x, r.y - 0.08f, (r.s0 + r.s1) / 2, 0.13f, 0.13f, len, 0, false);
                for (float s = r.s0 + 0.4f; s < r.s1; s += 2.6f)
                {
                    var ro = RoofUnder(s);
                    if (ro != null && Mathf.Abs(r.y - 0.35f - ro.y) < 0.2f) Prop("railpost", x, ro.y, s);
                }
                var r0 = RoofUnder(r.s0 + 1);
                for (int k = 0; k < 3; k++)
                    Box(Mats.Glow(Mats.Hex(0xffe14a), 1.6f - k * 0.4f), x, (r0 != null ? r0.y : r.y - 0.35f) + 0.02f, r.s0 - 1.2f - k * 1.1f, 0.7f - k * 0.16f, 0.02f, 0.16f, 0, false);
            }

            // wall-run panels on steel frames
            foreach (var w in level.walls)
            {
                float len = w.s1 - w.s0, zc = (w.s0 + w.s1) / 2, x = w.side * (PlayHalf + 0.3f);
                var m = Mats.Fx(2, Color.white); m.SetFloat("_Len", len); m.SetFloat("_Dir", w.side);
                var panel = Mesh(Mats.Quad, m, new Vector3(x - w.side * 0.03f, w.y + 0.75f, zc), Quaternion.Euler(0, w.side * 90, 0), new Vector3(len, 3.2f, 1), false);
                float bx = x + w.side * 0.2f;
                Box(Mats.Lit(Mats.Hex(0x161c44), 0.2f), bx, w.y + 0.75f, zc, 0.34f, 3.5f, len + 0.4f);
                Box(light, bx, w.y + 2.55f, zc, 0.5f, 0.22f, len + 0.7f);
                Box(light, bx, w.y - 1.0f, zc, 0.5f, 0.22f, len + 0.7f);
                int posts = Mathf.Max(2, Mathf.RoundToInt(len / 5));
                for (int i = 0; i <= posts; i++) Box(mid, bx + w.side * 0.25f, w.y - 9, w.s0 + len * i / posts, 0.3f, 23.5f, 0.3f);
                Box(bld, w.side * (RoofHalf + 5.2f), w.y - 55, zc, 9, 130, len + 8, 0, false);
            }

            // landing pads
            foreach (var p in level.pads)
            {
                var m = Mats.Fx(3, Mats.Hex(0x4dff9a)); padMats.Add(m);
                Mesh(Mats.Quad, m, new Vector3(p.lane * Sim.LaneW, p.y + 0.03f, (p.s0 + p.s1) / 2), Quaternion.Euler(90, 0, 0), new Vector3(2.15f, p.s1 - p.s0, 1), false);
            }

            // time capsules
            foreach (var c in level.capsules)
            {
                var holder = new GameObject("Capsule").transform; holder.SetParent(root.transform, false);
                holder.position = new Vector3(c.lane * Sim.LaneW, c.y, c.s);
                foreach (var n in new[] { "capsule", "capsule__glow" })
                {
                    var g = new GameObject(n); g.transform.SetParent(holder, false); g.transform.localRotation = KitRot; g.transform.localScale = Vector3.one * 1.05f;
                    g.AddComponent<MeshFilter>().sharedMesh = kit.Get(n);
                    g.AddComponent<MeshRenderer>().sharedMaterial = n.EndsWith("glow") ? Mats.Glow(Color.white) : Mats.Lit(Color.white, 0.55f);
                }
                capsules.Add(holder);
            }

            // tips
            foreach (var c in level.coins)
            {
                // credits: glowing gold, so they read as light against the night
                // Tocks: the city's money is time. each one is a hex token with clock hands
                var t = Mesh(TockMesh, coinMat, new Vector3(c.x, c.y, c.s), Quaternion.identity, Vector3.one, false).transform;
                coins.Add(t);
            }

            // power-ups: a courier's lunch break. each floats on a lit pad under a beam so you can read it from far off
            foreach (var w in level.powers)
            {
                var info = PowerLook[(int)w.kind];
                var holder = new GameObject("Power " + w.kind).transform; holder.SetParent(root.transform, false);
                holder.position = new Vector3(w.lane * Sim.LaneW, w.y, w.s);
                var glow = Mats.Glow(info.color, 1.8f);
                var pad = Mesh(Mats.Cube, glow, Vector3.zero, Quaternion.identity, new Vector3(1.5f, 0.05f, 1.5f), false); pad.transform.SetParent(holder, false); pad.transform.localPosition = new Vector3(0, 0.03f, 0);
                pad.transform.localRotation = Quaternion.Euler(0, 45, 0);
                var beam = Mesh(Mats.Quad, Mats.Fx(1, info.color * 0.55f), Vector3.zero, Quaternion.identity, new Vector3(1.1f, 7f, 1), false); beam.transform.SetParent(holder, false); beam.transform.localPosition = new Vector3(0, 3.5f, 0);
                var beam2 = Mesh(Mats.Quad, Mats.Fx(1, info.color * 0.55f), Vector3.zero, Quaternion.identity, new Vector3(1.1f, 7f, 1), false); beam2.transform.SetParent(holder, false); beam2.transform.localPosition = new Vector3(0, 3.5f, 0); beam2.transform.localRotation = Quaternion.Euler(0, 90, 0);
                var spin = new GameObject("Spin").transform; spin.SetParent(holder, false); spin.localPosition = new Vector3(0, 1.15f, 0);
                var model = art.Spawn(info.model, spin, Vector3.zero, Quaternion.identity, 1f, false);
                if (model)
                {
                    model.localPosition = info.offset; model.localScale = Vector3.one * info.scale;
                    // a broken or built-in shader draws nothing in the web player; fall back to the lit courier shader
                    foreach (var rend in model.GetComponentsInChildren<Renderer>())
                    {
                        var mats = rend.sharedMaterials;
                        bool swap = false;
                        for (int m = 0; m < mats.Length; m++)
                        {
                            var sh = mats[m] ? mats[m].shader : null;
                            if (sh == null || sh.name.Contains("Error") || sh.name == "Standard" || sh.name.StartsWith("Legacy"))
                            { mats[m] = Mats.Lit(Color.white, 0.55f); swap = true; }
                        }
                        if (swap) rend.sharedMaterials = mats;
                    }
                }
                else
                {
                    var stand = Mesh(Mats.Cube, Mats.Glow(info.color, 2.4f), Vector3.zero, Quaternion.identity, Vector3.one * 0.42f, false);
                    stand.transform.SetParent(spin, false);
                }
                if (w.kind == PowerKind.Wrap)
                {
                    // the bubble-wrap parcel sits inside a shimmering bubble
                    var bub = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(bub.GetComponent<Collider>()); bub.transform.SetParent(spin, false); bub.transform.localScale = Vector3.one * 1.5f;
                    bub.GetComponent<MeshRenderer>().sharedMaterial = Mats.Fx(1, new Color(0.5f, 0.9f, 1f) * 0.35f);
                }
                powers.Add(holder);
            }

            // the customer waits just past the finish gate
            {
                string[] who = { "PunkF", "Swat", "Casual", "SciFi", "Punk", "SciFi" };
                var cpos = new Vector3(1.7f, level.roofs[level.roofs.Count - 1].y, level.length + 5.5f);
                customer = new Actor(who[Mathf.Clamp(level.delivery.district, 0, who.Length - 1)], root.transform, cpos, 180);
                // a warm doorway light so the customer reads at night
                var lg = new GameObject("Door Light"); lg.transform.SetParent(root.transform, false); lg.transform.position = cpos + new Vector3(-0.6f, 3.2f, -2.2f);
                var pl = lg.AddComponent<Light>(); pl.type = LightType.Point; pl.range = 9; pl.intensity = 26; pl.color = new Color(1f, 0.85f, 0.6f);
            }

            // the old clock tower: where the ticking parcel has been heading all along
            if (district == 5)
            {
                float tz = level.length + 95, ty = level.roofs[level.roofs.Count - 1].y;
                Box(bld, 0, ty + 10, tz, 34, 220, 34, 0, false);
                Box(mid, 0, ty + 122, tz, 38, 6, 38, 0, false);
                Box(dark, 0, ty + 140, tz, 22, 30, 22, 0, false);
                var face = Mats.Glow(Mats.Hex(0xd9c8ff), 1.6f); var hand = Mats.Glow(Mats.Hex(0xff3d81), 2.2f);
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI / 6;
                    Box(face, Mathf.Sin(a) * 12.5f, ty + 96 + Mathf.Cos(a) * 12.5f, tz - 17.3f, i % 3 == 0 ? 1.6f : 0.9f, i % 3 == 0 ? 3.2f : 1.8f, 0.4f, 0, false).transform.rotation = Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg);
                }
                Box(hand, 0, ty + 96 + 4.6f, tz - 17.4f, 1.1f, 9.5f, 0.4f, 0, false);
                Box(hand, 3.2f, ty + 96 + 1.4f, tz - 17.4f, 1.1f, 7f, 0.4f, 0, false).transform.rotation = Quaternion.Euler(0, 0, -66);
            }

            // finish gate
            float fz = level.length, fy = level.roofs[level.roofs.Count - 1].y;
            var white = Mats.Glow(Color.white, 2.4f);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                Box(light, sx * (PlayHalf + 0.2f), fy + 3.4f, fz, 0.6f, 6.8f, 0.6f);
                Box(mid, sx * (PlayHalf + 0.2f), fy + 0.3f, fz, 1, 0.6f, 1);
                Box(white, sx * (PlayHalf - 0.12f), fy + 3.3f, fz, 0.08f, 6.2f, 0.08f, 0, false);
            }
            Box(light, 0, fy + 6.9f, fz, PlayHalf * 2 + 1.4f, 0.7f, 0.7f);
            Box(white, 0, fy + 6.5f, fz, PlayHalf * 2, 0.08f, 0.08f, 0, false);
            Mesh(Mats.Quad, Mats.Fx(4, Color.white), new Vector3(0, fy + 3.3f, fz), Quaternion.identity, new Vector3(PlayHalf * 2, 6.4f, 1), false);

            // the city
            float span = level.length + 700; int count = Mathf.FloorToInt(span / 5);
            for (int i = 0; i < count; i++)
            {
                int side = i % 2 == 1 ? 1 : -1; bool near = Rnd() < 0.4f;
                float w = 9 + Rnd() * 16, dp = 9 + Rnd() * 18;
                float x = side * (near ? RoofHalf + 9 + w / 2 + Rnd() * 10 : 44 + Rnd() * 150);
                float top = near ? -16 + Rnd() * 30 : -20 + Rnd() * Rnd() * 120;
                float z = -80 + (i / (float)count) * span + Rnd() * 6;
                Box(bld, x, top - 80, z, w, 160, dp, 0, false);
                // one tall sheet down the inner face of some near towers
                if (near && i % 6 == 0)
                {
                    var fall = Mesh(Mats.Quad, Mats.Fx(1, Mats.Hex(0x9fdfff) * 0.55f), new Vector3(x - side * (w * 0.5f + 0.15f), top - 22f, z), Quaternion.identity, new Vector3(1.6f, 40f, 1f), false);
                    // a slight yaw so the sheet faces the chase camera instead of turning edge-on
                    fall.transform.rotation = Quaternion.Euler(0, side > 0 ? 18 : -18, 0);
                }
                // neon billboards on the nearer towers: the city talks about time, money and the competition
                if (near && i % 4 == 1 && z > 20 && z < level.length)
                {
                    var ad = Ads[(int)(Rnd() * Ads.Length) % Ads.Length];
                    float bw = 9.5f, bh = 5.2f, bx = x - side * (w / 2 + 0.5f), by = Mathf.Max(top - 3.5f, 3.5f) + Rnd() * 3;
                    float yaw = side > 0 ? -58 : 58;
                    var rot = Quaternion.Euler(0, yaw, 0);
                    var holder = new GameObject("Billboard").transform; holder.SetParent(root.transform, false); holder.SetPositionAndRotation(new Vector3(bx, by, z), rot);
                    void Part(Material m, Vector3 lp, Vector3 ls) { var g = Mesh(Mats.Cube, m, Vector3.zero, Quaternion.identity, ls, false); g.transform.SetParent(holder, false); g.transform.localPosition = lp; g.transform.localRotation = Quaternion.identity; }
                    var glow = Mats.Glow(ad.color, 1.8f);
                    Part(Mats.Lit(Mats.Hex(0x0b0a20), 0.2f), new Vector3(0, 0, 0.12f), new Vector3(bw, bh, 0.2f));
                    Part(glow, new Vector3(0, bh / 2, 0), new Vector3(bw, 0.14f, 0.14f)); Part(glow, new Vector3(0, -bh / 2, 0), new Vector3(bw, 0.14f, 0.14f));
                    Part(glow, new Vector3(-bw / 2, 0, 0), new Vector3(0.14f, bh, 0.14f)); Part(glow, new Vector3(bw / 2, 0, 0), new Vector3(0.14f, bh, 0.14f));
                    var tg = new GameObject("Text"); tg.transform.SetParent(holder, false); tg.transform.localPosition = new Vector3(0, 0.25f, -0.05f);
                    var tm = tg.AddComponent<TextMesh>(); tm.font = AdFont; tm.text = ad.text; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
                    tm.fontSize = 90; tm.characterSize = ad.text.Contains("\n") ? 0.17f : 0.24f; tm.color = Color.Lerp(ad.color, Color.white, 0.35f); tm.lineSpacing = 0.85f;
                    tg.GetComponent<MeshRenderer>().sharedMaterial = AdFont.material;
                }

            }
            for (int i = 0; i < 26; i++)
            {
                float a = (i / 26f - 0.5f) * 1.5f, dist = 560 + Rnd() * 240;
                Box(bld, Mathf.Sin(a) * dist, -100 + Rnd() * Rnd() * 150, level.length * 0.5f + Mathf.Cos(a) * dist + 300, 30 + Rnd() * 50, 320, 40, 0, false);
            }
        }

        /// <summary>Rooftop clutter per district: the core, the docks, the neon strip, the heights, the market, the tower.</summary>
        static readonly string[][] Dressing =
        {
            new[] { "ac_unit", "vent", "pipes", "skylight", "solar", "dish", "tank", "antenna" },
            new[] { "Barrel_A", "Barrel_B", "Barrel_C", "Box_A", "Box_B", "Box_C", "Pallet_Large", "Pallet_Small_Decorated_A", "pipes", "tank", "dumpster", "crate_potatoes" },
            new[] { "sign", "sign", "sign", "skylight", "ac_unit", "trafficlight_A", "streetlight", "vent" },
            new[] { "antenna", "antenna", "dish", "dish", "solar", "solar", "tank", "watertower" },
            new[] { "bench", "bush", "streetlight", "trash_A", "trash_B", "crate_tomatoes", "crate_carrots", "crate_cheese", "crate_buns", "firehydrant", "sign" },
            new[] { "antenna", "tank", "vent", "pipes", "sign", "watertower" },
        };

        static readonly (string name, float scale)[] Clutter =
        {
            ("watertower", 1.15f), ("box_A", 1.5f), ("box_B", 1.5f), ("trash_A", 1.5f), ("trash_B", 1.5f), ("bench", 1.5f), ("dumpster", 1.25f), ("firehydrant", 1.5f),
            ("bush", 1.7f), ("bush", 2.1f), ("streetlight", 1.35f), ("Barrel_A", 1.3f), ("Pallet_Large", 1.2f), ("Box_A", 1.3f), ("Box_C", 1.3f), ("trafficlight_A", 1.3f),
        };

        static Font adFont;
        static Font AdFont => adFont ? adFont : adFont = Resources.Load<Font>("ChakraPetch-BoldItalic") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        static readonly (string text, Color color)[] Ads =
        {
            ("LATE?", new Color(1f, 0.24f, 0.83f)), ("TIME IS\nTOCKS", new Color(1f, 0.76f, 0.1f)), ("SPEEDY-GO\nwe get there first", new Color(0.22f, 0.9f, 1f)),
            ("24H", new Color(0.3f, 1f, 0.6f)), ("OVERTIME\nEXPRESS", new Color(1f, 0.76f, 0.1f)), ("NO OVERTIME\nPAY", new Color(1f, 0.13f, 0.25f)), ("TICK\nTOCK", new Color(0.72f, 0.55f, 1f)),
        };

        static Mesh tock;
        /// <summary>A Tock: a gold hexagonal token with a dark face and two bright clock hands, built as one mesh.</summary>
        public static Mesh TockMesh
        {
            get
            {
                if (tock) return tock;
                var v = new List<Vector3>(); var c = new List<Color>(); var tri = new List<int>();
                void Hex(float r, float z0, float z1, Color col)
                {
                    int b = v.Count;
                    for (int k = 0; k < 2; k++)
                        for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3 + Mathf.PI / 6; v.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, k == 0 ? z0 : z1)); c.Add(col); }
                    for (int i = 1; i < 5; i++) { tri.AddRange(new[] { b, b + i + 1, b + i }); tri.AddRange(new[] { b + 6, b + 6 + i, b + 6 + i + 1 }); }
                    for (int i = 0; i < 6; i++) { int j = (i + 1) % 6; tri.AddRange(new[] { b + i, b + j, b + 6 + j, b + i, b + 6 + j, b + 6 + i }); }
                }
                void Bar(float x0, float y0, float x1, float y1, float w, Color col)
                {
                    var d = new Vector3(x1 - x0, y1 - y0, 0).normalized; var n = new Vector3(-d.y, d.x, 0) * w;
                    foreach (float z in new[] { -0.075f, 0.075f })
                    {
                        int b = v.Count;
                        v.Add(new Vector3(x0, y0, z) - n); v.Add(new Vector3(x0, y0, z) + n); v.Add(new Vector3(x1, y1, z) + n); v.Add(new Vector3(x1, y1, z) - n);
                        for (int i = 0; i < 4; i++) c.Add(col);
                        tri.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3, b, b + 2, b + 1, b, b + 3, b + 2 });
                    }
                }
                Color gold = new Color(1f, 0.74f, 0.08f), dark = new Color(0.1f, 0.05f, 0.0f), hand = new Color(1.6f, 1.5f, 1.2f);
                Hex(0.36f, -0.05f, 0.05f, gold);
                Hex(0.26f, -0.065f, 0.065f, dark);
                Bar(0, 0, 0, 0.19f, 0.03f, hand); Bar(0, 0, 0.12f, -0.06f, 0.03f, hand);
                tock = new Mesh { name = "Tock" };
                tock.SetVertices(v); tock.SetColors(c); tock.SetTriangles(tri, 0); tock.RecalculateNormals(); tock.RecalculateBounds();
                return tock;
            }
        }

        /// <summary>How each power-up looks in the world and in the HUD: model, size, colour, name.</summary>
        public static readonly (string model, float scale, Vector3 offset, Color color, string name, string tag)[] PowerLook =
        {
            ("food_burger", 2.05f, new Vector3(0, -0.2f, 0), new Color(1f, 0.52f, 0.1f), "BURGER RUSH", "RUSH"),
            ("Can_A", 1.95f, new Vector3(0, -0.45f, 0), new Color(0.22f, 0.9f, 1f), "SODA FIZZ", "x2"),
            ("Box_B", 0.62f, new Vector3(0, -0.4f, 0), new Color(0.85f, 0.95f, 1f), "BUBBLE WRAP", "WRAP"),
            ("Coin_C", 1.25f, new Vector3(0, -0.3f, 0), new Color(1f, 0.82f, 0.12f), "TOCK MAGNET", "PULL"),
        };

        public void Tick(Sim sim, float time, float dt)
        {
            var pw = sim.level.powers;
            for (int i = 0; i < powers.Count && i < pw.Count; i++)
            {
                var t = powers[i];
                // anything the courier has passed vanishes: behind him it would sit right in front of the camera
                bool show = !pw[i].taken && pw[i].s - sim.s < 160 && pw[i].s > sim.s - 1.2f;
                if (t.gameObject.activeSelf != show) t.gameObject.SetActive(show);
                if (!show) continue;
                var spin = t.GetChild(t.childCount - 1);
                spin.localPosition = new Vector3(0, 1.15f + Mathf.Sin(time * 2.6f + i) * 0.16f, 0);
                spin.localRotation = Quaternion.Euler(12, time * 110 + i * 40, 0);
                float pulse = 1 + Mathf.Sin(time * 5 + i) * 0.08f; t.GetChild(0).localScale = new Vector3(1.5f * pulse, 0.05f, 1.5f * pulse);
            }
            var cs = sim.level.coins;
            for (int i = 0; i < coins.Count && i < cs.Count; i++)
            {
                var t = coins[i];
                bool show = !cs[i].taken && cs[i].s - sim.s < 120 && cs[i].s > sim.s - 1.5f;
                if (t.gameObject.activeSelf != show) t.gameObject.SetActive(show);
                if (!show) continue;
                t.rotation = Quaternion.Euler(0, time * 200 + i * 31, 0);
                // with the magnet on, nearby Tocks visibly swing in toward the courier before they're collected
                var home = new Vector3(cs[i].x, cs[i].y, cs[i].s);
                if (sim.magnetT > 0)
                {
                    var kip = new Vector3(sim.x, sim.y + 1f, sim.s);
                    float d = Vector3.Distance(home, kip);
                    float pull = d < 9f ? Mathf.Pow(1 - d / 9f, 1.6f) : 0;
                    t.position = Vector3.Lerp(home, kip, pull);
                    t.localScale = Vector3.one * (1 + pull * 0.4f);
                }
                else if (t.position != home) { t.position = home; t.localScale = Vector3.one; }
            }
            customer?.Tick(dt, sim.ended == EndReason.Finish);
            var caps = sim.level.capsules;
            for (int i = 0; i < capsules.Count && i < caps.Count; i++)
            {
                var t = capsules[i];
                if (caps[i].taken) { if (t.gameObject.activeSelf) t.gameObject.SetActive(false); continue; }
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                t.position = new Vector3(caps[i].lane * Sim.LaneW, caps[i].y + Mathf.Sin(time * 3 + i) * 0.12f, caps[i].s);
                t.rotation = Quaternion.Euler(14, time * 126 + i * 57, 0);
            }
            for (int i = 0; i < padMats.Count && i < sim.level.pads.Count; i++) padMats[i].SetFloat("_Used", sim.level.pads[i].used ? 1 : 0);
        }
    }
}

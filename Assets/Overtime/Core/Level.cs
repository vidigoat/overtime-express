using System;
using System.Collections.Generic;

namespace Overtime.Core
{
    public enum ObKind { Barrier, Beam, Block }

    public class Roof { public float s0, s1, y; }
    public class Obstacle { public ObKind kind; public float s, y; public int lane; public bool done, hit; }
    public class Rail { public float s0, s1, y; public int lane; }
    public class Wall { public float s0, s1, y; public int side; }
    public class Capsule { public float s, y; public int lane; public bool taken; }
    public class Pad { public float s0, s1, y; public int lane; public bool used; }
    public class Hint { public float s; public string text; }
    public class Coin { public float s, x, y; public bool taken; }

    /// <summary>Things a courier finds on the job. Each one changes the next few seconds of the run.</summary>
    public enum PowerKind { Burger, Soda, Wrap, TipJar }
    public class Power { public PowerKind kind; public float s, y; public int lane; public bool taken; }

    /// <summary>What you carry changes how you run.</summary>
    public enum ParcelKind { Standard, Heavy, Fragile, Hot, Live, Ticking }

    /// <summary>One job: who it's for, what it is, and the story beats around it.</summary>
    public class Delivery
    {
        public string customer, customerModel, item, itemModel, rule, briefing, thanks;
        public float itemScale = 0.55f;
        public ParcelKind kind;
        public int pay, district;
    }

    /// <summary>Gear bought with wages. Each piece changes how the run plays.</summary>
    public class Gear
    {
        public bool gloves, soles, magnet, wrap, flask, board, bike;
    }

    public class Level
    {
        public string id, name, tagline;
        public float length, startTime;
        public int[] stars = new int[3];
        public List<Roof> roofs = new List<Roof>();
        public List<Obstacle> obstacles = new List<Obstacle>();
        public List<Rail> rails = new List<Rail>();
        public List<Wall> walls = new List<Wall>();
        public List<Capsule> capsules = new List<Capsule>();
        public List<Pad> pads = new List<Pad>();
        public List<Hint> hints = new List<Hint>();
        public List<Coin> coins = new List<Coin>();
        public List<Power> powers = new List<Power>();
        public Delivery delivery = new Delivery();

        /// <summary>Fresh copy with all per-run flags cleared.</summary>
        public Level Clone()
        {
            var l = new Level { id = id, name = name, tagline = tagline, length = length, startTime = startTime, stars = (int[])stars.Clone(), delivery = delivery };
            foreach (var r in roofs) l.roofs.Add(new Roof { s0 = r.s0, s1 = r.s1, y = r.y });
            foreach (var o in obstacles) l.obstacles.Add(new Obstacle { kind = o.kind, s = o.s, y = o.y, lane = o.lane });
            foreach (var r in rails) l.rails.Add(new Rail { s0 = r.s0, s1 = r.s1, y = r.y, lane = r.lane });
            foreach (var w in walls) l.walls.Add(new Wall { s0 = w.s0, s1 = w.s1, y = w.y, side = w.side });
            foreach (var c in capsules) l.capsules.Add(new Capsule { s = c.s, y = c.y, lane = c.lane });
            foreach (var p in pads) l.pads.Add(new Pad { s0 = p.s0, s1 = p.s1, y = p.y, lane = p.lane });
            foreach (var h in hints) l.hints.Add(new Hint { s = h.s, text = h.text });
            foreach (var c in coins) l.coins.Add(new Coin { s = c.s, x = c.x, y = c.y });
            foreach (var w in powers) l.powers.Add(new Power { kind = w.kind, s = w.s, y = w.y, lane = w.lane });
            return l;
        }
    }

    /// <summary>
    /// Fluent level builder. A row is (offset from roof start, 3 chars for lanes left/mid/right):
    /// J = barrier (jump)  S = beam (slide)  B = block (dodge)  c = time capsule  P = precision pad  . = empty
    /// </summary>
    public class LevelBuilder
    {
        public float s, y;
        public readonly Level L;

        public LevelBuilder(string id, string name, string tagline, float startTime, int s1, int s2, int s3)
        {
            L = new Level { id = id, name = name, tagline = tagline, startTime = startTime, stars = new[] { s1, s2, s3 } };
        }

        public LevelBuilder Roof(float len, float dy = 0, params (float o, string pat)[] rows)
        {
            y += dy;
            float s0 = s;
            L.roofs.Add(new Roof { s0 = s0, s1 = s0 + len, y = y });
            foreach (var (o, pat) in rows)
            {
                for (int i = 0; i < 3; i++)
                {
                    char ch = pat[i]; int lane = i - 1; float ps = s0 + o;
                    if (ch == 'J') L.obstacles.Add(new Obstacle { kind = ObKind.Barrier, s = ps, lane = lane, y = y });
                    else if (ch == 'S') L.obstacles.Add(new Obstacle { kind = ObKind.Beam, s = ps, lane = lane, y = y });
                    else if (ch == 'B') L.obstacles.Add(new Obstacle { kind = ObKind.Block, s = ps, lane = lane, y = y });
                    else if (ch == 'c') L.capsules.Add(new Capsule { s = ps, lane = lane, y = y + 1.1f });
                    else if (ch == 'P') L.pads.Add(new Pad { s0 = ps, s1 = ps + 5.5f, lane = lane, y = y });
                }
            }
            s += len;
            return this;
        }

        public LevelBuilder Gap(float len) { s += len; return this; }

        /// <summary>A gap crossed by a grind rail in the given lane.</summary>
        public LevelBuilder RailGap(float len, int lane, float lead = 7, float tail = 6)
        {
            L.rails.Add(new Rail { s0 = s - lead, s1 = s + len + tail, lane = lane, y = y + 0.35f });
            for (float cs = s + 3; cs < s + len - 2; cs += 7) L.capsules.Add(new Capsule { s = cs, lane = lane, y = y + 1.5f });
            s += len;
            return this;
        }

        /// <summary>A gap crossed by a wall-run on side (-1 left, 1 right).</summary>
        public LevelBuilder WallGap(float len, int side, float lead = 6, float tail = 4)
        {
            L.walls.Add(new Wall { s0 = s - lead, s1 = s + len + tail, side = side, y = y + 1.3f });
            s += len;
            return this;
        }

        public LevelBuilder Hint(float o, string text) { L.hints.Add(new Hint { s = Math.Max(0, s + o), text = text }); return this; }

        public Level Done()
        {
            Roof(70);
            L.length = s - 45;
            L.obstacles.Sort((a, b) => a.s.CompareTo(b.s));
            L.hints.Sort((a, b) => a.s.CompareTo(b.s));
            ScatterCoins();
            return L;
        }

        /// <summary>
        /// Lay tips along the route: lines through the clear stretches (in a lane that is safe at the next row, so coins
        /// double as a hint), arcs over the hurdles, and strings along every rail and wall.
        /// </summary>
        void ScatterCoins()
        {
            uint seed = 2166136261u; foreach (char ch in L.id) seed = (seed ^ ch) * 16777619u;
            float Rnd() { seed = seed * 1664525u + 1013904223u; return seed / 4294967296f; }
            const float lw = 2.4f;
            var spots = new List<(float s, int lane, float y)>();
            bool Railed(float cs) { foreach (var r in L.rails) if (cs > r.s0 - 1 && cs < r.s1 + 1) return true; return false; }
            foreach (var roof in L.roofs)
            {
                var rows = new List<float>();
                foreach (var o in L.obstacles) if (o.s > roof.s0 && o.s < roof.s1 && (rows.Count == 0 || Math.Abs(rows[rows.Count - 1] - o.s) > 0.5f)) rows.Add(o.s);
                float from = roof.s0 + 4;
                for (int i = 0; i <= rows.Count; i++)
                {
                    float to = (i < rows.Count ? rows[i] : roof.s1 - 1) - 3.5f;
                    if (to - from >= 6)
                    {
                        // prefer a lane that is not blocked at the row this stretch leads into
                        int lane = (int)(Rnd() * 3) - 1;
                        if (i < rows.Count)
                            for (int tries = 0; tries < 3; tries++)
                            {
                                bool blocked = false;
                                foreach (var o in L.obstacles) if (Math.Abs(o.s - rows[i]) < 0.5f && o.lane == lane && o.kind == ObKind.Block) blocked = true;
                                if (!blocked) break;
                                lane = lane == 1 ? -1 : lane + 1;
                            }
                        spots.Add((from + (to - from) * 0.5f, lane, roof.y));
                        int n = Math.Min(7, (int)((to - from) / 1.8f));
                        float start = from + ((to - from) - (n - 1) * 1.8f) * Rnd();
                        for (int k = 0; k < n; k++) { float cs = start + k * 1.8f; if (!Railed(cs)) L.coins.Add(new Coin { s = cs, x = lane * lw, y = roof.y + 0.9f }); }
                    }
                    if (i < rows.Count)
                    {
                        // an arc of coins over one hurdle in the row
                        foreach (var o in L.obstacles)
                            if (Math.Abs(o.s - rows[i]) < 0.5f && o.kind == ObKind.Barrier && Rnd() < 0.6f)
                            {
                                for (int k = -2; k <= 2; k++) L.coins.Add(new Coin { s = o.s + k * 1.5f, x = o.lane * lw, y = roof.y + 2.5f - 0.28f * k * k });
                                break;
                            }
                        from = rows[i] + 3.5f;
                    }
                }
            }
            foreach (var r in L.rails) for (float cs = r.s0 + 2; cs < r.s1 - 1; cs += 2.2f) L.coins.Add(new Coin { s = cs, x = r.lane * lw, y = r.y + 1.0f });
            foreach (var w in L.walls) for (float cs = w.s0 + 2; cs < w.s1 - 1; cs += 2.2f) L.coins.Add(new Coin { s = cs, x = w.side * 3.35f, y = w.y + 1.6f });
            // one power-up roughly every 150 m, in a clear stretch, cycling through the four kinds
            float next = 70; int kind = (int)(seed % 4);
            foreach (var sp in spots)
            {
                if (sp.s < next || sp.s > L.length - 30) continue;
                L.powers.Add(new Power { kind = (PowerKind)(kind++ % 4), s = sp.s, lane = sp.lane, y = sp.y });
                L.coins.RemoveAll(c => Math.Abs(c.s - sp.s) < 2.4f && Math.Abs(c.x - sp.lane * lw) < 0.5f);
                next = sp.s + 150;
            }
            L.coins.Sort((a, b) => a.s.CompareTo(b.s));
            return;
        }
    }
}

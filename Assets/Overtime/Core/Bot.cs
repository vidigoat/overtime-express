using System;
using System.Collections.Generic;

namespace Overtime.Core
{
    /// <summary>Auto-player: proves every level is clearable, tunes difficulty, and plays the menu backdrop.</summary>
    public class Bot
    {
        readonly float skill;
        readonly Random rng;
        float cooldown;
        readonly HashSet<int> skipped = new HashSet<int>(), decided = new HashSet<int>();

        public Bot(float skill = 1f, int seed = 1) { this.skill = skill; rng = new Random(seed); }

        bool Fumble(float key)
        {
            int k = (int)Math.Round(key * 2);
            if (decided.Add(k) && rng.NextDouble() > skill) skipped.Add(k);
            return skipped.Contains(k);
        }

        public void Act(Sim sim, float dt)
        {
            if (sim.ended != EndReason.None) return;
            cooldown -= dt;
            var L = sim.level; float v = sim.speed, s = sim.s;
            if (sim.state == RunState.Wall || sim.state == RunState.Grind) return;

            int? want = null; Wall wallAhead = null;
            foreach (var r in L.rails) if (s > r.s0 - 28 && s < r.s0 + 1) want = r.lane;
            foreach (var w in L.walls) if (s > w.s0 - 28 && s < w.s1 - 5) { want = w.side; wallAhead = w; }

            float look = v * 0.8f, rowS = -1;
            var row = new ObKind?[3];
            for (int i = sim.obIdx; i < L.obstacles.Count; i++)
            {
                var o = L.obstacles[i];
                if (o.s < s - 0.7f || o.done) continue;
                if (o.s > s + look) break;
                if (rowS < 0) rowS = o.s;
                if (Math.Abs(o.s - rowS) < 0.5f) row[o.lane + 1] = o.kind;
            }
            bool fum = rowS >= 0 && Fumble(rowS);

            int target = want ?? sim.lane;
            if (rowS >= 0 && !fum && row[target + 1] == ObKind.Block)
            {
                int best = target, bestCost = int.MaxValue;
                for (int l = -1; l <= 1; l++)
                {
                    if (row[l + 1] == ObKind.Block) continue;
                    int cost = Math.Abs(l - target) * 10 + Math.Abs(l - sim.lane);
                    if (cost < bestCost) { bestCost = cost; best = l; }
                }
                target = best;
            }
            if (target != sim.lane && cooldown <= 0)
            {
                sim.DoSwipe(target > sim.lane ? Swipe.Right : Swipe.Left); cooldown = 0.11f;
                return;
            }
            if (wallAhead != null && sim.lane == wallAhead.side && s >= wallAhead.s0 - 0.5f && cooldown <= 0 && !Fumble(wallAhead.s0))
            {
                sim.DoSwipe(wallAhead.side > 0 ? Swipe.Right : Swipe.Left); cooldown = 0.11f;
                if (sim.state == RunState.Wall) return;
            }

            ObKind? mine = rowS >= 0 && !fum ? row[sim.lane + 1] : null;
            float dist = rowS - s;
            if (sim.Grounded)
            {
                if (mine == ObKind.Barrier && dist < v * 0.27f) { sim.DoSwipe(Swipe.Up); return; }
                if (mine == ObKind.Beam && dist < v * 0.3f && sim.state != RunState.Slide) { sim.DoSwipe(Swipe.Down); return; }
                var r = sim.PeekRoof(s);
                if (r != null && r.s1 - s < v * 0.05f + 0.25f)
                {
                    var nxt = sim.PeekRoof(r.s1 + 0.3f);
                    bool onRail = false;
                    foreach (var rl in L.rails) if (rl.lane == sim.lane && rl.s0 < s && rl.s1 > r.s1) onRail = true;
                    if (nxt == null && !onRail && !Fumble(r.s1)) sim.DoSwipe(Swipe.Up);
                }
            }
            else if (sim.state == RunState.Air)
            {
                if (mine == ObKind.Beam && dist < v * 0.4f && sim.vy < 6 && sim.slideBuf <= 0) { sim.DoSwipe(Swipe.Down); return; }
                if (sim.canDash && sim.vy < 0 && !WillLand(sim) && wallAhead == null) sim.DoSwipe(Swipe.Up);
            }
        }

        static bool WillLand(Sim sim)
        {
            float y = sim.y, vy = sim.vy, s = sim.s;
            for (int i = 0; i < 80; i++)
            {
                vy -= Sim.G * 0.02f; y += vy * 0.02f; s += sim.speed * 0.02f;
                var r = sim.PeekRoof(s);
                if (r != null && y <= r.y) return y >= r.y - 0.6f;
                foreach (var rl in sim.level.rails) if (rl.lane == sim.lane && s >= rl.s0 && s < rl.s1 - 2 && y <= rl.y && y > rl.y - 0.8f) return true;
                if (y < sim.y - 14) return false;
            }
            return false;
        }
    }
}

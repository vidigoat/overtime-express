using System;
using System.Collections.Generic;

namespace Overtime.Core
{
    public enum RunState { Run, Slide, Air, Grind, Wall }
    public enum EndReason { None, Finish, Crash, Fall, Timeout }
    public enum Swipe { Left, Right, Up, Down }
    public enum EvType { Trick, Hit, Fall, Jump, Land, Slide, Dash, Capsule, Lane, ChainLost, OtEnter, OtExit, Finish, Dead, Hint, Grind, Wall, NoDash, Coin, Crack, Wrap, Bored, Power }

    public struct Ev
    {
        public EvType type; public string name, text; public float gain, mult; public int pts, chain;
    }

    /// <summary>
    /// Pure game rules: no rendering, no Unity objects. The clock is life, fuel and score.
    /// Distances are metres along the route (s), lateral x, height y.
    /// </summary>
    public class Sim
    {
        public const float LaneW = 2.4f, WallX = 3.35f, G = 40f, Jump = 13.5f, BaseSpeed = 14.5f;
        public const float OtLimit = 10f, ClockCap = 45f, ChainWindow = 4f, DashCost = 1f, HitCost = 4f, FallCost = 5f;

        static readonly Dictionary<string, (float b, string label)> Tricks = new Dictionary<string, (float, string)>
        {
            { "vault", (0.75f, "VAULT") }, { "slide", (0.75f, "SLIDE") }, { "close", (0.65f, "CLOSE CALL") }, { "gap", (0.65f, "GAP") },
            { "wallrun", (1.2f, "WALL RUN") }, { "walljump", (0.7f, "WALL KICK") }, { "grind", (1.1f, "GRIND") },
            { "launch", (0.65f, "RAIL LAUNCH") }, { "dash", (0f, "DASH") }, { "perfect", (1.7f, "PERFECT") }, { "smash", (0.5f, "SMASH") },
        };
        public static string TrickLabel(string n) => Tricks.TryGetValue(n, out var t) ? t.label : n;

        public readonly Level level;
        public float t, s, x, y, vy, speed = BaseSpeed, dashV;
        public int lane;
        public RunState state = RunState.Run;
        public float slideT, coyote, jumpBuf, slideBuf, wallBuf, invuln, clock, chainT, slowAmt, lastLaneT = -9, otSeconds, wallT;
        public int wallBufDir, chain, points, lastLaneFrom, bestChain, tricks, hits, finalScore;
        public bool canDash = true, overtime, slow, viaAssist;
        public string last1 = "", last2 = "";
        public EndReason ended = EndReason.None;
        public readonly List<Ev> events = new List<Ev>();
        public Rail rail; public Wall wall;
        public int roofIdx, groundRoofIdx, obIdx, hintIdx, coinIdx;

        // what the parcel and your gear change
        public readonly ParcelKind parcel;
        public readonly Gear gear;
        public int coinsGot;
        public float condition = 1f;          // parcel health, drives the tip
        public bool wrapUsed;
        // power-ups: burger rush, soda fizz, tip-jar magnet (seconds left) and a bubble-wrap shield
        public float rushT, fizzT, magnetT, dashT;
        public bool shield;
        public float shieldT;
        public const float RushTime = 30f, FizzTime = 30f, MagnetTime = 30f, ShieldTime = 30f;
        readonly float jumpV, drainMul = 1f, capsuleGain = 1.5f, chainWindow = ChainWindow, baseSpeed = BaseSpeed, coyoteTime = 0.1f, coinReach = 1.15f;

        public Sim(Level def, Gear g = null)
        {
            level = def.Clone();
            gear = g ?? new Gear();
            parcel = def.delivery != null ? def.delivery.kind : ParcelKind.Standard;
            clock = def.startTime;
            y = level.roofs[0].y;
            jumpV = Jump * (parcel == ParcelKind.Heavy ? 0.93f : 1f) * (gear.soles ? 1.05f : 1f);
            if (parcel == ParcelKind.Hot) { drainMul = 1.15f; capsuleGain = 2.2f; }
            if (parcel == ParcelKind.Ticking) chainWindow = 3.2f;
            if (gear.board) baseSpeed = BaseSpeed * 1.05f;
            if (gear.gloves) coyoteTime = 0.18f;
            if (gear.magnet) coinReach = 3.3f;
            speed = baseSpeed;
        }

        float TrickMul(string name)
        {
            float m = 1f;
            if (parcel == ParcelKind.Live) m *= name == "gap" || name == "vault" || name == "walljump" || name == "launch" ? 2f : name == "slide" ? 0.5f : 1f;
            if (parcel == ParcelKind.Ticking) m *= 1.25f;
            if (gear.board && name == "grind") m *= 2f;
            if (fizzT > 0) m *= 2f;
            return m;
        }

        public float Mult => Math.Min(1 + chain * 0.1f, 3f);
        public bool Grounded => state == RunState.Run || state == RunState.Slide;
        public float Progress => Math.Min(1f, s / level.length);

        Roof RoofAt(float ps)
        {
            var roofs = level.roofs;
            while (roofIdx < roofs.Count && roofs[roofIdx].s1 <= ps) roofIdx++;
            if (roofIdx >= roofs.Count) return null;
            var r = roofs[roofIdx];
            return r.s0 <= ps ? r : null;
        }

        /// <summary>Roof under an arbitrary s without advancing the cursor (bots, camera, shadows).</summary>
        public Roof PeekRoof(float ps)
        {
            var roofs = level.roofs;
            for (int i = Math.Max(0, roofIdx - 1); i < roofs.Count; i++)
            {
                if (roofs[i].s0 > ps) return null;
                if (roofs[i].s1 > ps) return roofs[i];
            }
            return null;
        }

        public Roof NextRoof() => roofIdx < level.roofs.Count ? level.roofs[roofIdx] : null;

        void Emit(EvType type, string name = null, string text = null) => events.Add(new Ev { type = type, name = name, text = text });

        public void DoSwipe(Swipe dir)
        {
            if (ended != EndReason.None) return;
            if (dir == Swipe.Up) PressUp();
            else if (dir == Swipe.Down) PressDown();
            else PressSide(dir == Swipe.Left ? -1 : 1);
        }

        void PressUp()
        {
            if (state == RunState.Wall)
            {
                int side = wall.side; wall = null;
                state = RunState.Air; vy = jumpV * 1.05f; lane = side; canDash = true;
                Emit(EvType.Jump); Trick("walljump");
            }
            else if (state == RunState.Grind)
            {
                rail = null; state = RunState.Air; vy = jumpV; canDash = true;
                Emit(EvType.Jump); Trick("launch");
            }
            else if (Grounded || coyote > 0) DoJump();
            else
            {
                // airborne: dash, unless we're about to touch down (then buffer a jump)
                var r = PeekRoof(s + speed * 0.08f);
                bool nearGround = r != null && vy < 0 && y - r.y < 0.7f;
                if (canDash && !nearGround)
                {
                    // a dash is a flat, fast lunge: gravity lets go for a moment, then picks you back up
                    canDash = false; vy = 0; dashT = 0.2f; dashV = 21f; clock -= DashCost;
                    Emit(EvType.Dash); Trick("dash");
                }
                else
                {
                    jumpBuf = 0.16f;
                    if (!nearGround) Emit(EvType.NoDash);
                }
            }
        }

        void DoJump()
        {
            state = RunState.Air; vy = jumpV; coyote = 0; jumpBuf = 0; canDash = true;
            Emit(EvType.Jump);
        }

        void PressDown()
        {
            if (Grounded) { state = RunState.Slide; slideT = 0.7f; Emit(EvType.Slide); }
            else if (state == RunState.Air) { vy = Math.Min(vy, -26f); slideBuf = 0.3f; }
            else if (state == RunState.Grind) { rail = null; state = RunState.Air; vy = -4; }
            else if (state == RunState.Wall) { lane = wall.side; wall = null; state = RunState.Air; vy = -4; }
        }

        void PressSide(int dir)
        {
            if (state == RunState.Wall)
            {
                if (dir == -wall.side) { lane = wall.side; wall = null; state = RunState.Air; vy = 3; }
                return;
            }
            int nl = lane + dir;
            if (nl < -1 || nl > 1)
            {
                if (!TryWall(dir)) { wallBuf = 0.35f; wallBufDir = dir; }
                return;
            }
            if (state == RunState.Grind) { rail = null; state = RunState.Air; vy = 3; }
            lastLaneFrom = lane; lastLaneT = t;
            lane = nl;
            Emit(EvType.Lane);
        }

        bool TryWall(int dir)
        {
            if (lane != dir) return false;
            foreach (var w in level.walls)
            {
                if (w.side == dir && s >= w.s0 - 1 && s <= w.s1 - 4 && y > w.y - 3.5f)
                {
                    wall = w; rail = null; state = RunState.Wall; vy = 0; canDash = true; viaAssist = true; wallBuf = 0; wallT = 0;
                    Emit(EvType.Wall); Trick("wallrun");
                    return true;
                }
            }
            return false;
        }

        void Trick(string name)
        {
            float b = Tricks[name].b;
            float stale = name == last1 ? 0.5f : name == last2 ? 0.75f : 1f;
            last2 = last1; last1 = name;
            chain++; chainT = chainWindow; tricks++;
            if (chain > bestChain) bestChain = chain;
            float mult = Mult;
            float gain = b * mult * stale * TrickMul(name);
            int pts = (int)Math.Round(100 * mult * stale * (overtime ? 2 : 1));
            AddTime(gain);
            points += pts;
            events.Add(new Ev { type = EvType.Trick, name = name, gain = gain, pts = pts, chain = chain, mult = mult });
        }

        void AddTime(float sec)
        {
            clock += sec;
            if (overtime && clock >= 0) { overtime = false; clock += 3; points += 500; Emit(EvType.OtExit); }
            if (clock > ClockCap) { points += (int)Math.Round((clock - ClockCap) * 150); clock = ClockCap; }
        }

        void Die(EndReason reason)
        {
            ended = reason; finalScore = points;
            Emit(EvType.Dead, reason.ToString());
        }

        void BreakChain()
        {
            if (chain > 0) { chain = 0; chainT = 0; last1 = last2 = ""; Emit(EvType.ChainLost); }
        }

        void Hit()
        {
            if (shield) { shield = false; shieldT = 0; invuln = 1.2f; Emit(EvType.Wrap); return; }
            // bubble wrap soaks up the first crash of the run, even in overtime
            if (gear.wrap && !wrapUsed) { wrapUsed = true; invuln = 1.2f; Emit(EvType.Wrap); return; }
            hits++; Emit(EvType.Hit);
            condition = Math.Max(0f, condition - 0.2f);
            if (overtime) { Die(EndReason.Crash); return; }
            clock -= HitCost; BreakChain(); invuln = 1.2f; speed *= 0.55f;
        }

        void FallOff()
        {
            hits++; Emit(EvType.Fall);
            condition = Math.Max(0f, condition - 0.2f);
            if (overtime) { Die(EndReason.Fall); return; }
            clock -= FallCost; BreakChain();
            var r = NextRoof();
            if (r == null) { Die(EndReason.Fall); return; }
            s = Math.Max(s, r.s0 + 1.5f);
            y = r.y; vy = 0; state = RunState.Run; rail = null; wall = null;
            invuln = 1.6f; speed = baseSpeed * 0.7f; dashV = 0;
            groundRoofIdx = roofIdx; viaAssist = false; canDash = true;
        }

        void Land(Roof r)
        {
            float impact = vy;
            bool onPad = false;
            foreach (var p in level.pads) if (!p.used && s >= p.s0 && s <= p.s1 && Math.Abs(x - p.lane * LaneW) < 1.1f) onPad = true;
            // fragile parcels crack on hard landings, unless you stick a pad
            if (parcel == ParcelKind.Fragile && impact < -23f && !onPad) { condition = Math.Max(0f, condition - 0.34f); Emit(EvType.Crack); }
            y = r.y; vy = 0; canDash = true;
            if (slideBuf > 0) { state = RunState.Slide; slideT = 0.7f; Emit(EvType.Slide); }
            else state = RunState.Run;
            Emit(EvType.Land);
            if (roofIdx != groundRoofIdx && !viaAssist) Trick("gap");
            groundRoofIdx = roofIdx; viaAssist = false;
            foreach (var p in level.pads)
            {
                if (!p.used && s >= p.s0 && s <= p.s1 && Math.Abs(x - p.lane * LaneW) < 1.1f) { p.used = true; Trick("perfect"); }
            }
        }

        /// <summary>Advance by real dt seconds. Call with small steps (the game loop substeps at 120 Hz).</summary>
        public void Step(float dt)
        {
            if (ended != EndReason.None) return;
            bool slowing = slow;
            slowAmt += ((slowing ? 1 : 0) - slowAmt) * Math.Min(1, dt * 12);
            float scale = 1 - 0.58f * slowAmt;
            float gdt = dt * scale;
            t += dt;

            clock -= dt * drainMul * (slowing ? (gear.flask ? 1f : 2f) : 1f);
            if (!overtime && clock <= 0) { overtime = true; Emit(EvType.OtEnter); }
            if (overtime)
            {
                otSeconds += dt;
                if (clock <= -OtLimit) { Die(EndReason.Timeout); return; }
            }
            if (invuln > 0) invuln -= dt;
            if (rushT > 0) rushT -= dt;
            if (fizzT > 0) fizzT -= dt;
            if (magnetT > 0) magnetT -= dt;
            if (shieldT > 0) { shieldT -= dt; if (shieldT <= 0) shield = false; }
            if (jumpBuf > 0) jumpBuf -= dt;
            if (slideBuf > 0) slideBuf -= dt;
            if (wallBuf > 0) { wallBuf -= dt; TryWall(wallBufDir); }
            if (chain > 0 && state != RunState.Grind && state != RunState.Wall)
            {
                chainT -= dt;
                if (chainT <= 0)
                {
                    // a ticking parcel hates being bored
                    if (parcel == ParcelKind.Ticking && !overtime) { clock -= 1.5f; Emit(EvType.Bored); }
                    BreakChain();
                }
            }

            float target = (baseSpeed + Math.Min(chain, 20) * 0.22f) * (overtime ? 1.18f : 1f) * (rushT > 0 ? 1.3f : 1f);
            speed += (target - speed) * Math.Min(1, gdt * 2.5f);
            s += (speed + dashV) * gdt;
            if (dashT <= 0) dashV *= (float)Math.Exp(-9 * gdt);

            float tx = state == RunState.Wall ? wall.side * WallX : lane * LaneW;
            x += (tx - x) * Math.Min(1, gdt * 18);

            if (state == RunState.Run || state == RunState.Slide) StepGround(gdt);
            else if (state == RunState.Air) StepAir(gdt, dt);
            else if (state == RunState.Grind)
            {
                y = rail.y; AddTime(0.3f * gdt);
                if (s >= rail.s1) { rail = null; state = RunState.Air; vy = 5; }
            }
            else if (state == RunState.Wall)
            {
                // a real wall-run arcs: you rise onto the wall, then gravity slowly wins
                wallT += gdt;
                float arc = wall.y + 0.55f * (float)Math.Sin(Math.Min(1f, (s - wall.s0) / Math.Max(1f, wall.s1 - wall.s0)) * Math.PI);
                y += (arc - y) * Math.Min(1, gdt * 10);
                AddTime(0.3f * gdt);
                if (s >= wall.s1) { lane = wall.side; wall = null; state = RunState.Air; vy = 6; }
            }
            if (ended != EndReason.None) return;

            Collide();

            var hints = level.hints;
            while (hintIdx < hints.Count && hints[hintIdx].s <= s) Emit(EvType.Hint, null, hints[hintIdx++].text);

            if (s >= level.length)
            {
                ended = EndReason.Finish;
                finalScore = points + (int)Math.Round(Math.Max(0, clock) * 150) + (overtime ? 1500 : 0);
                Emit(EvType.Finish);
            }
        }

        bool MountRail()
        {
            foreach (var r in level.rails)
            {
                if (s >= r.s0 && s < r.s1 - 2 && Math.Abs(x - r.lane * LaneW) < 0.8f)
                {
                    rail = r; state = RunState.Grind; y = r.y; vy = 0; canDash = true; viaAssist = true;
                    Emit(EvType.Grind); Trick("grind");
                    return true;
                }
            }
            return false;
        }

        void StepGround(float gdt)
        {
            var r = RoofAt(s);
            if (r == null) { state = RunState.Air; coyote = coyoteTime; vy = 0; return; }
            if (r.y > y + 0.45f) { FallOff(); return; }
            if (r.y < y - 0.3f) { state = RunState.Air; vy = 0; coyote = coyoteTime; return; }
            y = r.y;
            groundRoofIdx = roofIdx;
            if (state == RunState.Slide) { slideT -= gdt; if (slideT <= 0) state = RunState.Run; }
            if (jumpBuf > 0) { DoJump(); return; }
            foreach (var rl in level.rails)
            {
                if (Math.Abs(rl.y - y) < 0.6f && s >= rl.s0 && s < rl.s0 + 3 && Math.Abs(x - rl.lane * LaneW) < 0.8f) { MountRail(); return; }
            }
        }

        void StepAir(float gdt, float dt)
        {
            if (coyote > 0) coyote -= dt;
            float prevY = y;
            if (dashT > 0) { dashT -= gdt; vy = 0; if (dashT <= 0) vy = 1.5f; }
            else vy -= G * gdt;
            y += vy * gdt;
            if (vy <= 0)
            {
                foreach (var rl in level.rails)
                    if (prevY >= rl.y - 0.05f && y <= rl.y && MountRail()) return;
            }
            var r = RoofAt(s);
            if (r != null && y <= r.y)
            {
                if (prevY >= r.y - 0.45f || y >= r.y - 0.45f) Land(r); else FallOff();
                return;
            }
            var nr = NextRoof();
            float killY = (nr != null ? nr.y : 0) - 9;
            if (y < killY) FallOff();
        }

        void Collide()
        {
            var obs = level.obstacles;
            while (obIdx < obs.Count && obs[obIdx].s < s - 2) obIdx++;
            for (int i = obIdx; i < obs.Count; i++)
            {
                var o = obs[i];
                if (o.s - 0.55f > s) break;
                if (o.done) continue;
                bool inLane = Math.Abs(x - o.lane * LaneW) < 1.0f;
                if (s <= o.s + 0.55f)
                {
                    if (!inLane || invuln > 0 || state == RunState.Wall) continue;
                    float rel = y - o.y;
                    bool hit = o.kind == ObKind.Barrier ? rel < 0.85f : o.kind == ObKind.Beam ? state != RunState.Slide && rel < 1.55f : rel < 2.9f;
                    if (hit)
                    {
                        o.done = true; o.hit = true;
                        // a heavy parcel turns you into a battering ram: hurdles just break
                        if (rushT > 0 || (parcel == ParcelKind.Heavy && o.kind == ObKind.Barrier)) { Trick("smash"); continue; }
                        Hit(); if (ended != EndReason.None) return;
                    }
                }
                else
                {
                    o.done = true;
                    if (invuln > 0) continue;
                    if (inLane && state != RunState.Wall) Trick(o.kind == ObKind.Beam && state == RunState.Slide ? "slide" : "vault");
                    else if (t - lastLaneT < 0.5f && lastLaneFrom == o.lane) { lastLaneT = -9; Trick("close"); }
                }
            }
            foreach (var c in level.capsules)
            {
                if (c.taken || Math.Abs(c.s - s) > 1.3f) continue;
                if (Math.Abs(x - c.lane * LaneW) < 1.1f && Math.Abs(y + 0.9f - c.y) < 1.5f)
                {
                    c.taken = true; AddTime(capsuleGain); points += 50; Emit(EvType.Capsule);
                }
            }
            foreach (var w in level.powers)
            {
                if (w.taken || Math.Abs(w.s - s) > 1.4f) continue;
                if (Math.Abs(x - w.lane * LaneW) < 1.2f && Math.Abs(y - w.y) < 2.4f)
                {
                    w.taken = true; points += 100;
                    if (w.kind == PowerKind.Burger) rushT = RushTime;
                    else if (w.kind == PowerKind.Soda) fizzT = FizzTime;
                    else if (w.kind == PowerKind.TipJar) magnetT = MagnetTime;
                    else { shield = true; shieldT = ShieldTime; }
                    Emit(EvType.Power, w.kind.ToString());
                }
            }
            float reach = magnetT > 0 ? Math.Max(coinReach, 4.2f) : coinReach;
            var coins = level.coins;
            while (coinIdx < coins.Count && coins[coinIdx].s < s - 4) coinIdx++;
            for (int i = coinIdx; i < coins.Count; i++)
            {
                var c = coins[i];
                if (c.s - reach - 0.5f > s) break;
                if (c.taken || Math.Abs(c.s - s) > reach) continue;
                if (Math.Abs(x - c.x) < reach && Math.Abs(y + 0.9f - c.y) < reach + 0.6f) { c.taken = true; coinsGot++; points += 10; Emit(EvType.Coin); }
            }
        }
    }
}

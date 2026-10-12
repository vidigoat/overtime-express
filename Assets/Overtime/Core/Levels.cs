using System;
using System.Collections.Generic;

namespace Overtime.Core
{
    /// <summary>The authored routes. Level 1 is hand-built; later levels are composed from reusable chunks.</summary>
    public static class Levels
    {
        static (float, string) R(float o, string p) => (o, p);

        static Level FirstShift()
        {
            var b = new LevelBuilder("l1", "FIRST SHIFT", "Day one of your own company.", 35, 0, 0, 0);
            b.Hint(4, "SWIPE LEFT / RIGHT TO SWITCH LANES");
            b.Roof(100, 0, R(40, "cB."), R(62, "BBc"), R(84, ".c."));
            b.Hint(0, "SWIPE UP (OR TAP) TO JUMP");
            b.Roof(70, 0, R(22, "JJJ"), R(38, "JJJ"), R(52, ".c."));
            b.Hint(0, "SWIPE DOWN TO SLIDE");
            b.Roof(72, 0, R(22, "SSS"), R(38, "SSS"), R(54, "JJJ"));
            b.Hint(-16, "JUMP THE GAP, LAND ON THE GREEN PAD");
            b.Gap(6);
            b.Roof(64, 0, R(3, ".P."), R(24, "JJJ"), R(44, "B.B"));
            b.Hint(-16, "RIDE THE RAIL");
            b.RailGap(18, 0);
            b.Roof(66, 0, R(20, "c.."), R(30, "JSJ"), R(48, "BB."));
            b.Hint(-16, "SWIPE RIGHT AGAIN TO RUN THE WALL");
            b.WallGap(16, 1);
            b.Roof(74, 0, R(14, "..c"), R(28, "SSS"), R(44, "JBJ"));
            b.Hint(-24, "BIG GAP: JUMP, THEN TAP IN THE AIR TO DASH");
            b.Gap(12.5f);
            b.Roof(72, 0, R(22, ".c."), R(34, "BJB"), R(48, "SSS"), R(60, "c.."));
            b.Hint(-14, "CHAIN MOVES TO EARN TIME");
            b.Gap(6);
            b.Roof(52, -2, R(3, "..P"), R(22, "JJJ"), R(36, "B.B"));
            b.RailGap(22, 0);
            b.Roof(60, 0, R(18, "SJS"), R(32, "JJJ"), R(46, "c.c"));
            b.Gap(6);
            b.Roof(30, -1, R(3, ".P."), R(18, ".c."));
            return b.Done();
        }

        static readonly Dictionary<string, Action<LevelBuilder, float>> Chunks = new Dictionary<string, Action<LevelBuilder, float>>
        {
            { "slalom", (b, d) => b.Roof(26 + 3 * d, 0, R(14, "B.B"), R(14 + d, ".BB"), R(14 + 2 * d, "B.B"), R(14 + 3 * d, "BB.")) },
            { "slalomR", (b, d) => b.Roof(26 + 3 * d, 0, R(14, "BB."), R(14 + d, "B.B"), R(14 + 2 * d, ".BB"), R(14 + 3 * d, "BcB")) },
            { "hurdles", (b, d) => b.Roof(24 + 2 * d, 0, R(12, "JJJ"), R(12 + d, "SSS"), R(12 + 2 * d, "JJJ")) },
            { "limbo", (b, d) => b.Roof(24 + 2 * d, 0, R(12, "SSS"), R(12 + d, "JJJ"), R(12 + 2 * d, "SSS")) },
            { "mix", (b, d) => b.Roof(26 + 3 * d, 0, R(12, "JSB"), R(12 + d, "BJS"), R(12 + 2 * d, "SBJ"), R(12 + 3 * d, "cJc")) },
            { "mix2", (b, d) => b.Roof(26 + 3 * d, 0, R(12, "SBJ"), R(12 + d, "JJB"), R(12 + 2 * d, "BSS"), R(12 + 3 * d, "JcJ")) },
            { "weave", (b, d) => b.Roof(28 + 4 * d, 0, R(12, "BJB"), R(12 + d, "B.B"), R(12 + 2 * d, "JBS"), R(12 + 3 * d, "SBJ"), R(12 + 4 * d, ".c.")) },
            { "hop", (b, d) => { b.Roof(22 + d, 0, R(12, "J.J")); b.Gap(6.5f); b.Roof(22 + d, 0, R(3, ".P."), R(10 + d, "SSS")); } },
            { "stairs", (b, d) => {
                b.Roof(26, 0, R(14, "c..")); b.Gap(6); b.Roof(22, -1.5f, R(3, "P.."), R(13, "JJJ")); b.Gap(6.5f);
                b.Roof(22, -1.5f, R(3, ".P."), R(13, "SSS")); b.Gap(7); b.Roof(18 + d, -1.5f, R(3, "..P")); } },
            { "climb", (b, d) => { b.Roof(24, 0, R(13, "JJJ")); b.Gap(5.5f); b.Roof(22, 1, R(12, "B.B")); b.Gap(5.5f); b.Roof(16 + d, 1, R(10, ".c.")); } },
            { "railM", (b, d) => { b.Roof(30, 0, R(16, "B.B")); b.RailGap(22, 0); b.Roof(22 + d, 0, R(12 + d, "JJJ")); } },
            { "railL", (b, d) => { b.Roof(30, 0, R(16, ".BB")); b.RailGap(26, -1); b.Roof(22 + d, -1, R(12 + d, "SSS")); } },
            { "railR", (b, d) => { b.Roof(30, 0, R(16, "BB.")); b.RailGap(26, 1); b.Roof(22 + d, -1, R(12 + d, "JSJ")); } },
            { "wallR", (b, d) => { b.Roof(32, 0, R(16, "BB.")); b.WallGap(18, 1); b.Roof(22 + d, 0, R(12 + d, "SSS")); } },
            { "wallL", (b, d) => { b.Roof(32, 0, R(16, ".BB")); b.WallGap(18, -1); b.Roof(22 + d, 0.8f, R(12 + d, "JJJ")); } },
            { "dash", (b, d) => { b.Roof(30, 0, R(14, "cJc")); b.Gap(12.5f); b.Roof(24 + d, 0, R(14 + d, "B.B")); } },
            // wall-run straight into a rail: the showpiece combo
            { "wallRail", (b, d) => { b.Roof(32, 0, R(16, "BB.")); b.WallGap(18, 1); b.Roof(16); b.RailGap(22, 1, 9, 6); b.Roof(22 + d, 0, R(12 + d, "JJJ")); } },
            { "railWall", (b, d) => { b.Roof(30, 0, R(16, ".BB")); b.RailGap(20, -1); b.Roof(18); b.WallGap(18, -1, 8, 4); b.Roof(22 + d, 0, R(12 + d, "SJS")); } },
        };

        static Level Compose(LevelBuilder b, float d, params string[] seq)
        {
            b.Roof(40);
            foreach (var n in seq) Chunks[n](b, d);
            return b.Done();
        }

        static Level RushHour() => Compose(new LevelBuilder("l2", "RUSH HOUR", "Every lane is taken.", 30, 0, 0, 0), 12.5f,
            "slalom", "hurdles", "hop", "railM", "mix", "wallR", "stairs", "dash", "slalomR", "railL", "weave", "wallRail");

        static Level FineChina() => Compose(new LevelBuilder("l3", "FINE CHINA", "Land soft or pay for it.", 30, 0, 0, 0), 12.5f,
            "hop", "limbo", "stairs", "railR", "slalom", "hop", "wallL", "hurdles", "stairs", "railM", "mix");

        static Level HotStuff() => Compose(new LevelBuilder("l4", "HOT STUFF", "It's fizzing. Hurry.", 26, 0, 0, 0), 12f,
            "hurdles", "railM", "weave", "dash", "wallRail", "mix2", "hop", "slalomR", "railWall", "limbo", "climb", "wallR");

        static Level WideAwake() => Compose(new LevelBuilder("l5", "WIDE AWAKE", "Whatever's in there loves heights.", 24, 0, 0, 0), 11.5f,
            "hop", "stairs", "wallRail", "hurdles", "climb", "railL", "dash", "mix", "hop", "railWall", "weave", "stairs", "wallL", "hurdles");

        static Level DeadLine() => Compose(new LevelBuilder("l6", "DEAD LINE", "The parcel that never stopped ticking.", 20, 0, 0, 0), 11.5f,
            "mix", "hop", "wallRail", "weave", "stairs", "railWall", "slalom", "dash", "limbo", "climb", "railR", "mix2", "wallL", "hurdles", "wallRail");

        /// <summary>The story, one delivery at a time. Dot is your dispatcher; the ticking parcel is the thread.</summary>
        static readonly Delivery[] Jobs =
        {
            new Delivery { customer = "Mia (turning 7)", customerModel = "Rogue", item = "Birthday Burger", itemModel = "food_burger", itemScale = 0.9f, kind = ParcelKind.Standard, pay = 60, district = 0,
                rule = "A normal parcel. Learn the route.",
                briefing = "Dot here, your dispatcher. Congrats on the new company, boss. Staff: you. First job: a birthday burger. Kid's waiting on the roof of Block 9.",
                thanks = "\"It's still warm! You're the best delivery person EVER.\"" },
            new Delivery { customer = "Big Bjorn's Gym", customerModel = "Barbarian", item = "Protein Crate", itemModel = "crate_steak", itemScale = 0.5f, kind = ParcelKind.Heavy, pay = 90, district = 1,
                rule = "HEAVY: you jump lower, but hurdles just break.",
                briefing = "Forty kilos of steak for Bjorn. Don't go around things. Go through them. Also that ticking box is still on my desk. Not asking questions.",
                thanks = "\"You CARRIED that? Up here? Respect. Take a protein bar.\"" },
            new Delivery { customer = "Sir Reginald", customerModel = "Knight", item = "Royal Dinner on Fine China", itemModel = "food_dinner", itemScale = 0.9f, kind = ParcelKind.Fragile, pay = 120, district = 2,
                rule = "FRAGILE: hard landings crack it. Stick the green pads.",
                briefing = "A full roast dinner on plates older than this city. One hard landing and it's a jigsaw. Land the pads. The ticking box hummed at me today.",
                thanks = "\"Not a chip! A knight never forgets a punctual squire.\"" },
            new Delivery { customer = "Luna, part-time wizard", customerModel = "Mage", item = "Bubbling Potion", itemModel = "Barrel_B", itemScale = 0.45f, kind = ParcelKind.Hot, pay = 150, district = 3,
                rule = "HOT: the clock runs faster, but time capsules give more.",
                briefing = "It's a potion, it's bubbling, and the lid says DO NOT LET COOL. Grab every capsule. PS the ticking box has started glowing. Cool cool cool.",
                thanks = "\"Still fizzing! You just saved my whole Tuesday.\"" },
            new Delivery { customer = "Ash, rival courier", customerModel = "Rogue_Hooded", item = "A Box That Clucks", itemModel = "Box_B", itemScale = 0.5f, kind = ParcelKind.Live, pay = 180, district = 4,
                rule = "LIVE: it loves air. Jumps and gaps pay double, slides pay half.",
                briefing = "Something alive, addressed to Ash from Speedy-Go, your competition. It gets happy when you're airborne. The ticking box is addressed to... nobody. Weird.",
                thanks = "\"You beat my best time carrying THAT? ...Fine. You're good.\"" },
            new Delivery { customer = "???", customerModel = "Mage", item = "The Ticking Parcel", itemModel = "Box_C", itemScale = 0.55f, kind = ParcelKind.Ticking, pay = 250, district = 5,
                rule = "TICKING: never stop chaining. A broken chain costs time, every move pays more.",
                briefing = "The address just appeared on the ticking box: the old clock tower, top roof. It ticks faster when you slow down. Don't slow down, boss.",
                thanks = "Nobody's here. The box clicks open: a bike bell, and a note. \"For the fastest courier in the city. Every great company needs wheels.\"" },
        };

        static Level[] all;
        public static Level[] All
        {
            get
            {
                if (all != null) return all;
                all = new[] { FirstShift(), RushHour(), FineChina(), HotStuff(), WideAwake(), DeadLine() };
                for (int i = 0; i < all.Length; i++)
                {
                    all[i].delivery = Jobs[i];
                    // star targets are set from what a clean run scores, so they track any rule or tuning change
                    var sim = new Sim(all[i]); var bot = new Bot(1f, 1);
                    while (sim.ended == EndReason.None && sim.t < 300) { bot.Act(sim, 1f / 120f); sim.Step(1f / 120f); sim.events.Clear(); }
                    int s = Math.Max(1000, sim.finalScore);
                    all[i].stars = new[] { (int)(s * 0.35f) / 50 * 50, (int)(s * 0.62f) / 50 * 50, (int)(s * 0.88f) / 50 * 50 };
                }
                return all;
            }
        }
    }
}

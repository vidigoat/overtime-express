using System.Text;
using Overtime.Core;

namespace Overtime.EditorTools
{
    /// <summary>Headless proof that the rules port is sound: a perfect bot must clear every level without a hit.</summary>
    public static class SimCheck
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            foreach (var def in Levels.All)
            {
                var sim = new Sim(def); var bot = new Bot(1f, 1);
                const float dt = 1f / 120f;
                while (sim.ended == EndReason.None && sim.t < 300) { bot.Act(sim, dt); sim.Step(dt); sim.events.Clear(); }
                sb.AppendLine($"{def.name}[{def.delivery.kind}]: {sim.ended} t={sim.t:F1} clock={sim.clock:F1} min? score={sim.finalScore} hits={sim.hits} cond={sim.condition:F2} coins={sim.coinsGot}/{def.coins.Count} stars={def.stars[0]}/{def.stars[1]}/{def.stars[2]}");
            }
            return sb.ToString();
        }
    }
}

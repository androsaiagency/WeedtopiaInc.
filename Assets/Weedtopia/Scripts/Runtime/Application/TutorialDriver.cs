using System;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    /// <summary>Acceptance driver: uses only public player commands, no resource/quest state injection.</summary>
    public static class TutorialDriver
    {
        // Test/diagnostic simulation budget; real-time pacing remains defined by the content.
        public const int MaxSimulationSeconds = 7200;
        public static void Act(GameSession session, DateTime utc, bool allowRecovery = true)
        {
            var state = session.State; var defs = session.Definitions;
            if (!state.introSeen) session.CompleteIntro();
            var quest = session.Progression.Current;
            if (quest == null) return;
            foreach (var c in quest.conditions)
            {
                if (session.Progression.Matches(c)) continue;
                switch (c.kind)
                {
                    case ConditionKind.Talk: session.Talk(c.target); break;
                    case ConditionKind.Level:
                        var target = state.buildings.FirstOrDefault(x => x.cityId == state.activeCity && (x.id == c.target || x.family == c.target));
                        if (target == null && (c.target == "residential_house" || c.target == "park"))
                        {
                            target = state.buildings.First(x => x.cityId == state.activeCity && defs.Building(x.family).kind == BuildingKind.Plot);
                            session.ChoosePlot(target.id, c.target);
                        }
                        if (target != null && target.level < c.count && target.buildingSeconds == 0) session.Upgrades.BuyWithMoney(target.id);
                        break;
                    case ConditionKind.Harvest: session.Simulation.Harvest(quest.focus); break;
                    case ConditionKind.Assigned:
                    case ConditionKind.Automated:
                        var required = state.buildings.FirstOrDefault(x => x.cityId == state.activeCity && x.level > 0 && (x.id == c.target || x.family == c.target));
                        if (required == null) break;
                        int slots = c.kind == ConditionKind.Automated ? 1 : c.count;
                        for (int slot = 0; slot < slots && slot <= session.Workers.Slots(required); slot++)
                        {
                            if (state.workers.Any(x => x.buildingId == required.id && x.slot == slot)) continue;
                            string role = session.Workers.SlotRole(required, slot);
                            var candidate = state.workers.Where(x => x.role == role)
                                .OrderBy(x => string.IsNullOrEmpty(x.buildingId) ? 0 : 1).FirstOrDefault();
                            // An optional building may finish first and receive the guaranteed worker.
                            // Use the public reassignment command; an in-flight worker retries next tick.
                            if (candidate != null) session.Workers.Assign(candidate.id, required.id, slot);
                        }
                        break;
                    case ConditionKind.Spin: session.Workers.Spin(out _); break;
                    case ConditionKind.TownHall: session.OpenTownHall(); break;
                }
            }
            foreach (var worker in state.workers.Where(x => string.IsNullOrEmpty(x.buildingId)).ToArray())
                foreach (var b in state.buildings.Where(x => x.cityId == state.activeCity && x.level > 0))
                {
                    var d = defs.Building(b.family);
                    int slot = d.managerRole == worker.role ? 0 : d.driverRole == worker.role ? 1 : -1;
                    if (slot >= 0 && !state.workers.Any(x => x.buildingId == b.id && x.slot == slot))
                    { session.Workers.Assign(worker.id, b.id, slot); break; }
                }
            session.Daily.Claim(utc);
            foreach (var vip in state.vip.Where(x => !x.completed).ToArray()) session.Progression.ClaimVip(vip.id);
            if (session.Progression.Ready) session.Progression.Claim();
            if (allowRecovery) session.RequestRecoveryWork();
        }
    }
}

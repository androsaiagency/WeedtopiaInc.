using System;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    /// <summary>Diagnostic player: only public commands, no money, workers or completion injection.</summary>
    public static class CityProjectDriver
    {
        public const int MaxSimulationSeconds = 21600;
        public static void Act(GameSession session, string planId)
        {
            var project = session.CityProject;
            if (!project.Available || project.Completed) return;
            if (project.Plan == null) project.ChoosePlan(planId);
            foreach (var goal in project.Current.goals)
            {
                if (goal.kind != ProjectGoalKind.Level && goal.kind != ProjectGoalKind.Automated) continue;
                var target = session.State.buildings.Where(b => b.cityId == session.State.activeCity && (b.id == goal.target || b.family == goal.target))
                    .OrderByDescending(b => b.level).FirstOrDefault();
                if (target == null) continue;
                if (goal.kind == ProjectGoalKind.Level && target.level < goal.count && target.buildingSeconds == 0)
                    session.Upgrades.BuyWithMoney(target.id);
                if (goal.kind == ProjectGoalKind.Automated && target.level > 0 && !session.Workers.Automated(target))
                {
                    string role = session.Definitions.Building(target.family).managerRole;
                    var candidate = session.State.workers.Where(w => w.role == role).OrderBy(w => string.IsNullOrEmpty(w.buildingId) ? 0 : 1).FirstOrDefault();
                    if (candidate != null) session.Workers.Assign(candidate.id, target.id, 0);
                }
            }
            foreach (var worker in session.State.workers.Where(w => string.IsNullOrEmpty(w.buildingId)).ToArray())
                foreach (var building in session.State.buildings.Where(b => b.cityId == session.State.activeCity && b.level > 0))
                {
                    var definition = session.Definitions.Building(building.family);
                    int slot = definition.managerRole == worker.role ? 0 : definition.driverRole == worker.role ? 1 : -1;
                    if (slot < 0 || session.State.workers.Any(w => w.buildingId == building.id && w.slot == slot)) continue;
                    session.Workers.Assign(worker.id, building.id, slot); break;
                }
            if (project.Ready) project.Claim();
        }
    }
}

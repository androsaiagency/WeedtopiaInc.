using System;
using System.Collections.Generic;
using System.Linq;

namespace Weedtopia.Domain
{
    public enum ProjectGoalKind { Level, Automated, Residents, RefinedProduced, CoffeeSold, ButterProduced, PastrySold, Delivered }

    [Serializable] public sealed class ProjectGoal
    {
        public ProjectGoalKind kind;
        public string target, textKey;
        public int count;
        public bool IsCounter => kind >= ProjectGoalKind.RefinedProduced;
    }

    [Serializable] public sealed class ProjectStep
    {
        public string id, titleKey, bodyKey, focus, speaker;
        public ProjectGoal[] goals;
        public int moneyReward, cardsReward;
        public string[] workerRewards = Array.Empty<string>();
        public ItemStack[] itemRewards = Array.Empty<ItemStack>();
    }

    [Serializable] public sealed class ProjectPlan
    {
        public string id, titleKey, bodyKey, rewardKey, bonusFamily;
        public double salesBonus;
        public ProjectStep[] steps;
    }

    [Serializable] public sealed class CityProjectDefinition
    {
        public string id, cityId, titleKey, invitationKey, completedKey;
        public ProjectPlan[] plans;

        public void Validate(GameDefinitions defs)
        {
            void Check(bool valid) { if (!valid) throw new InvalidOperationException("Invalid city project " + id); }
            void Text(string key) { Check(!string.IsNullOrEmpty(key) && defs.texts.Any(t => t.key == key)); }
            Check(!string.IsNullOrWhiteSpace(id) && defs.instances.Any(i => i.cityId == cityId));
            Text(titleKey); Text(invitationKey); Text(completedKey);
            Check(plans != null && plans.Length >= 2 && plans.All(p => p != null) && plans.Select(p => p.id).Distinct().Count() == plans.Length);
            foreach (var plan in plans)
            {
                Check(!string.IsNullOrWhiteSpace(plan.id));
                Text(plan.titleKey); Text(plan.bodyKey); Text(plan.rewardKey);
                var family = defs.buildings.FirstOrDefault(b => b.id == plan.bonusFamily);
                Check(family != null && (family.kind == BuildingKind.Shop || family.kind == BuildingKind.Bakery));
                Check(plan.salesBonus > 0 && plan.salesBonus <= .5 && !double.IsNaN(plan.salesBonus));
                Check(plan.steps != null && plan.steps.Length > 0 && plan.steps.All(s => s != null) && plan.steps.Select(s => s.id).Distinct().Count() == plan.steps.Length);
                foreach (var step in plan.steps)
                {
                    Check(!string.IsNullOrWhiteSpace(step.id) && step.moneyReward >= 0 && step.cardsReward >= 0 && step.workerRewards != null);
                    Text(step.titleKey); Text(step.bodyKey); Text("speaker." + step.speaker);
                    Check(defs.instances.Any(i => i.id == step.focus && i.cityId == cityId));
                    foreach (string role in step.workerRewards) defs.Role(role);
                    InventoryItems.ValidateGrants(defs, step.itemRewards);
                    Check(step.goals != null && step.goals.Length > 0 && step.goals.All(g => g != null));
                    foreach (var goal in step.goals)
                    {
                        Check(Enum.IsDefined(typeof(ProjectGoalKind), goal.kind) && goal.count > 0); Text(goal.textKey);
                        if (goal.kind == ProjectGoalKind.Level)
                            Check(goal.count <= 5000 && defs.instances.Any(i => i.cityId == cityId && (i.id == goal.target || i.family == goal.target ||
                                (i.family == "settlement_plot" && (goal.target == "park" || goal.target == "residential_house")))));
                        if (goal.kind == ProjectGoalKind.Automated)
                            Check(goal.count == 1 && defs.instances.Any(i => i.id == goal.target && i.cityId == cityId && !string.IsNullOrEmpty(defs.Building(i.family).managerRole)));
                    }
                }
            }
        }
    }

    [Serializable] public sealed class CityProjectState
    {
        public string planId;
        public int stepIndex;
        public List<string> completedSteps = new List<string>();
        public Amount[] baselines = Array.Empty<Amount>();
    }

    public static class CityProjectRewards
    {
        // Derived from saved completion, never stacked or paid again when loading a game.
        public static double SalesMultiplier(GameDefinitions defs, CityState city, string family)
        {
            var definition = defs.cityProject;
            if (definition == null || definition.cityId != city.id || city.project == null) return 1;
            var plan = definition.plans.FirstOrDefault(p => p.id == city.project.planId);
            return plan != null && city.project.stepIndex == plan.steps.Length && plan.bonusFamily == family ? 1 + plan.salesBonus : 1;
        }
    }
}

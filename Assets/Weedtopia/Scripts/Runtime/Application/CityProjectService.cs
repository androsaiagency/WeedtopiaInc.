using System;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    /// <summary>Optional post-tutorial chapter. Counter goals start at the beginning of each step.</summary>
    public sealed class CityProjectService
    {
        private readonly GameDefinitions defs;
        private readonly GameState state;
        private readonly Simulation simulation;
        public CityProjectService(GameDefinitions defs, GameState state, Simulation simulation)
        { this.defs = defs; this.state = state; this.simulation = simulation; }
        public CityProjectDefinition Definition => defs.cityProject;
        public bool Available => Definition != null && Definition.cityId == state.activeCity && state.City.freeplay;
        public ProjectPlan Plan => Available ? Definition.plans.FirstOrDefault(p => p.id == state.City.project?.planId) : null;
        public bool Completed => Plan != null && state.City.project.stepIndex == Plan.steps.Length;
        public ProjectStep Current => Plan != null && !Completed ? Plan.steps[state.City.project.stepIndex] : null;
        public bool Ready => state.introSeen && Current != null && Current.goals.Select((g, i) => Progress(i) >= g.count).All(x => x);
        public string Focus => Current?.focus ?? "town_hall";

        public Failure ChoosePlan(string id)
        {
            if (!Available || !state.introSeen) return Failure.Locked;
            if (Plan != null) return Failure.Invalid;
            var plan = Definition.plans.FirstOrDefault(p => p.id == id);
            if (plan == null) return Failure.Invalid;
            state.City.project = new CityProjectState { planId = id };
            BeginStep(); state.revision++; return Failure.None;
        }

        private Amount Value(ProjectGoal goal)
        {
            var city = state.City;
            switch (goal.kind)
            {
                case ProjectGoalKind.Level:
                    return state.buildings.Where(b => b.cityId == city.id && (b.id == goal.target || b.family == goal.target)).Select(b => b.level).DefaultIfEmpty(0).Max();
                case ProjectGoalKind.Automated:
                    var building = state.buildings.FirstOrDefault(b => b.cityId == city.id && b.id == goal.target);
                    return building != null && building.level > 0 && simulation.Workers.Automated(building) ? 1 : 0;
                case ProjectGoalKind.Residents: return simulation.Residents(city.id);
                case ProjectGoalKind.RefinedProduced: return city.stats.refinedProduced;
                case ProjectGoalKind.CoffeeSold: return city.stats.coffeeSold;
                case ProjectGoalKind.ButterProduced: return city.stats.butterProduced;
                case ProjectGoalKind.PastrySold: return city.stats.pastrySold;
                case ProjectGoalKind.Delivered: return city.stats.delivered;
                default: throw new ArgumentOutOfRangeException(nameof(goal.kind));
            }
        }

        public Amount Progress(int goalIndex)
        {
            var goal = Current.goals[goalIndex];
            return Amount.Min(goal.count, Amount.Max(0, Value(goal) - (goal.IsCounter ? state.City.project.baselines[goalIndex] : Amount.Zero)));
        }

        private void BeginStep()
        {
            state.City.project.baselines = Current == null ? Array.Empty<Amount>() :
                Current.goals.Select(g => g.IsCounter ? Value(g) : Amount.Zero).ToArray();
        }

        public Failure Claim()
        {
            if (!Ready) return Failure.NotReady;
            var step = Current;
            int cards = checked(state.cards + step.cardsReward);
            state.money += step.moneyReward; state.City.stats.earned += step.moneyReward; state.cards = cards;
            var rewards = RewardHistory.Currency(step.moneyReward, step.cardsReward);
            InventoryItems.Grant(state, step.itemRewards, rewards);
            foreach (string role in step.workerRewards) rewards.Add(RewardItem.ForWorker(simulation.Workers.Grant(role)));
            state.City.project.completedSteps.Add(step.id); state.City.project.stepIndex++;
            if (Completed)
            {
                rewards.Add(RewardItem.Of(RewardKind.Title, 1, "festival"));
                rewards.Add(RewardItem.Of(RewardKind.SalesBonus, new Amount(Plan.salesBonus * 100), Plan.bonusFamily));
            }
            RewardHistory.Record(state, RewardSource.CityProject, step.id, step.titleKey, rewards);
            BeginStep(); state.revision++; return Failure.None;
        }
    }
}

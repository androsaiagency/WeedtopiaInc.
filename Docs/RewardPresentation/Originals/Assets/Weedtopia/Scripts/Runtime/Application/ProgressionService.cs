using System;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    public sealed class ProgressionService
    {
        private readonly GameDefinitions defs;
        private readonly GameState state;
        private readonly Simulation simulation;
        public ProgressionService(GameDefinitions defs, GameState state, Simulation simulation) { this.defs = defs; this.state = state; this.simulation = simulation; }
        public QuestDefinition Current => state.City.questIndex < defs.quests.Length ? defs.quests[state.City.questIndex] : null;
        public bool Ready => state.introSeen && Current != null && Current.conditions.All(Matches);
        public bool Matches(QuestCondition c)
        {
            var stats = state.City.stats;
            switch(c.kind)
            {
                case ConditionKind.Talk: return state.City.talked.Contains(c.target);
                case ConditionKind.Level: return state.buildings.Any(x => x.cityId == state.activeCity && (x.id == c.target || x.family == c.target) && x.level >= c.count);
                case ConditionKind.Harvest: return stats.manualHarvests >= c.count;
                case ConditionKind.Assigned: return state.workers.Count(x => !string.IsNullOrEmpty(x.buildingId) && (x.buildingId == c.target || state.Find(x.buildingId).family == c.target)) >= c.count;
                case ConditionKind.Delivered: return stats.deliveries >= c.count;
                case ConditionKind.Sold: return stats.coffeeSold >= c.count;
                case ConditionKind.Spin: return stats.spins >= c.count;
                case ConditionKind.TownHall: return stats.townHallVisits >= c.count;
                case ConditionKind.Residents: return simulation.Residents(state.activeCity) >= c.count;
                case ConditionKind.Daily: return string.IsNullOrEmpty(c.target) ? stats.dailyClaims >= c.count : state.buildings.Any(x => x.id == c.target && x.dailyClaims >= c.count);
                case ConditionKind.Butter: return stats.butterProduced >= c.count;
                case ConditionKind.PastrySold: return stats.pastrySold >= c.count;
                case ConditionKind.VipCompleted: return stats.vipCompleted >= c.count;
                case ConditionKind.Automated:
                    var automated = state.buildings.FirstOrDefault(x => x.cityId == state.activeCity && x.id == c.target);
                    return automated != null && automated.level > 0 && simulation.Workers.Automated(automated);
                default: return false;
            }
        }
        public Failure Claim()
        {
            if (!Ready) return Failure.NotReady;
            var q = Current;
            if (state.City.completedQuests.Contains(q.id)) return Failure.Invalid;
            state.City.completedQuests.Add(q.id); state.City.questIndex++;
            state.money += q.moneyReward; state.City.stats.earned += q.moneyReward; state.cards = checked(state.cards + q.cardsReward);
            foreach (string role in q.workerRewards) simulation.Workers.Grant(role);
            if (q.unlock > state.City.unlocked) state.City.unlocked = q.unlock;
            if (state.City.questIndex == defs.quests.Length) state.City.freeplay = true;
            state.revision++; return Failure.None;
        }
        public Amount VipValue(ConditionKind kind, string cityId)
        {
            var stats = state.cities.Single(x => x.id == cityId).stats;
            return kind == ConditionKind.Delivered ? stats.delivered : stats.rawProduced;
        }
        public Failure ClaimVip(string id)
        {
            var q = state.vip.FirstOrDefault(x => x.id == id && x.cityId == state.activeCity);
            if (q == null || q.completed || VipValue(q.kind, q.cityId) < q.target) return Failure.NotReady;
            q.completed = true; state.money += q.reward; state.City.stats.earned += q.reward; state.City.stats.vipCompleted++; state.revision++; return Failure.None;
        }
    }
}

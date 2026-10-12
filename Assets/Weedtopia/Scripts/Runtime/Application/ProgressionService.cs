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
        public QuestDefinition[] Quests => defs.QuestsFor(state.activeCity);
        public QuestDefinition Current => state.City.questIndex < Quests.Length ? Quests[state.City.questIndex] : null;
        public bool Ready => state.introSeen && Current != null && Current.conditions.All(Matches);
        public bool Matches(QuestCondition c) => Progress(c) >= c.count;
        public Amount Progress(QuestCondition c)
        {
            var stats = state.City.stats;
            switch(c.kind)
            {
                case ConditionKind.Talk: return state.City.talked.Contains(c.target) ? c.count : 0;
                case ConditionKind.Level: return state.buildings.Where(x => x.cityId == state.activeCity && (x.id == c.target || x.family == c.target)).Select(x=>x.level).DefaultIfEmpty(0).Max();
                case ConditionKind.Harvest: return stats.manualHarvests;
                case ConditionKind.Assigned: return state.workers.Count(x => !string.IsNullOrEmpty(x.buildingId) && state.Find(x.buildingId).cityId == state.activeCity && (x.buildingId == c.target || state.Find(x.buildingId).family == c.target));
                case ConditionKind.Delivered: return stats.deliveries;
                case ConditionKind.Sold: return stats.coffeeSold;
                case ConditionKind.Spin: return stats.spins;
                case ConditionKind.TownHall: return stats.townHallVisits;
                case ConditionKind.Residents: return simulation.Residents(state.activeCity);
                case ConditionKind.Daily: return string.IsNullOrEmpty(c.target) ? stats.dailyClaims : state.buildings.Where(x => x.id == c.target).Select(x=>x.dailyClaims).DefaultIfEmpty(0).Max();
                case ConditionKind.Butter: return stats.butterProduced;
                case ConditionKind.PastrySold: return stats.pastrySold;
                case ConditionKind.VipCompleted: return stats.vipCompleted;
                case ConditionKind.Automated:
                    var automated = state.buildings.FirstOrDefault(x => x.cityId == state.activeCity && x.id == c.target);
                    return automated != null && automated.level > 0 && simulation.Workers.Automated(automated) ? c.count : 0;
                default: return 0;
            }
        }
        public Failure Claim()
        {
            if (!Ready) return Failure.NotReady;
            var q = Current;
            if (state.City.completedQuests.Contains(q.id)) return Failure.Invalid;
            state.City.completedQuests.Add(q.id); state.City.questIndex++;
            state.money += q.moneyReward; state.City.stats.earned += q.moneyReward; state.cards = checked(state.cards + q.cardsReward);
            var rewards = RewardHistory.Currency(q.moneyReward, q.cardsReward);
            InventoryItems.Grant(state, q.itemRewards, rewards);
            foreach (string role in q.workerRewards) rewards.Add(RewardItem.ForWorker(simulation.Workers.Grant(role)));
            if (q.unlock > state.City.unlocked) rewards.Add(RewardItem.Of(RewardKind.District, 1, q.unlock.ToString()));
            if (q.unlock > state.City.unlocked) state.City.unlocked = q.unlock;
            if (state.City.questIndex == Quests.Length) { state.City.freeplay = true; rewards.Add(RewardItem.Of(RewardKind.Title, 1, "founder")); }
            RewardHistory.Record(state, RewardSource.Quest, q.id, q.titleKey, rewards);
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
            q.completed = true; state.money += q.reward; state.City.stats.earned += q.reward; state.City.stats.vipCompleted++;
            var rewards = RewardHistory.Currency(q.reward);
            InventoryItems.Grant(state, defs.vipItemRewards, rewards);
            RewardHistory.Record(state, RewardSource.Vip, q.id, "reward.source.vip", rewards);
            state.revision++; return Failure.None;
        }
    }
}

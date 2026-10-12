using System;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    public sealed class DailyService
    {
        private readonly GameDefinitions defs;
        private readonly GameState state;
        private readonly Simulation simulation;
        public DailyService(GameDefinitions defs, GameState state, Simulation simulation) { this.defs = defs; this.state = state; this.simulation = simulation; }
        public Failure Claim(DateTime utc)
        {
            long ticks = utc.ToUniversalTime().Ticks;
            if (ticks < state.highWaterUtcTicks) return Failure.NotReady;
            long day = ticks / TimeSpan.TicksPerDay;
            bool changed = false;
            int cardsBefore = state.cards, grindersBefore = state.grinders;
            var vipBefore = state.vip.Select(v => v.id).ToArray();
            foreach (var b in state.buildings.Where(x => x.cityId == state.activeCity && x.level > 0))
            {
                var kind = defs.Building(b.family).kind;
                if (kind != BuildingKind.House && kind != BuildingKind.Park && kind != BuildingKind.Market) continue;
                if (day <= b.lastDailyDay) continue;
                int days = (int)Math.Min(defs.maxDailyCatchup, day - b.lastDailyDay);
                // Store the tier entitlement on each day checkpoint; upgrades do not retroactively inflate missed days.
                int current = VisualTiers.Index(b.level);
                int tier = b.dailyTier == 0 ? current : Math.Min(current, b.dailyTier);
                int count = checked((days - 1) * tier + current + (int)Math.Floor(simulation.Workers.Bonus(b, WorkerAttribute.DailyBonus) - 1));
                if (kind == BuildingKind.House) state.cards = checked(state.cards + count);
                if (kind == BuildingKind.Market) state.grinders = checked(state.grinders + count);
                if (kind == BuildingKind.Park) GenerateVip(b, count, day);
                b.lastDailyDay = day; b.dailyTier = current; b.dailyClaims++; state.City.stats.dailyClaims++; changed = true;
            }
            state.highWaterUtcTicks = Math.Max(state.highWaterUtcTicks, ticks);
            if (changed)
            {
                var rewards = RewardHistory.Currency(0, state.cards - cardsBefore, state.grinders - grindersBefore);
                int orders = state.vip.Count(v => !vipBefore.Contains(v.id));
                if (orders > 0) rewards.Add(RewardItem.Of(RewardKind.VipOrders, orders));
                RewardHistory.Record(state, RewardSource.Daily, day.ToString(System.Globalization.CultureInfo.InvariantCulture), "reward.source.daily", rewards);
                state.revision++;
            }
            return changed ? Failure.None : Failure.NotReady;
        }
        private void GenerateVip(BuildingState park, int count, long day)
        {
            state.vip.RemoveAll(x => x.completed);
            var random = new SeededRandom(state.randomState);
            var stats = state.City.stats;
            for (int i = 0; i < count && state.vip.Count(x => !x.completed) < defs.maxVip; i++)
            {
                // Both goals are backed by the already unlocked, active automated farm.
                bool delivery = stats.deliveries > 0 && random.Next(2) == 1;
                var kind = delivery ? ConditionKind.Delivered : ConditionKind.Harvest;
                Amount current = delivery ? stats.delivered : stats.rawProduced;
                Amount increment = new Amount(4 * Math.Max(1, VisualTiers.Index(park.level))) / simulation.Workers.Bonus(park, WorkerAttribute.VipTarget);
                Amount target = current + increment;
                if (state.vip.Any(x => !x.completed && x.kind == kind && x.target == target)) target += increment * (i + 1);
                state.vip.Add(new VipState { id = park.id + "_" + day + "_" + i, cityId = park.cityId, kind = kind, target = target,
                    reward = new Amount(40 * Math.Max(1, VisualTiers.Index(park.level))) * simulation.Workers.Bonus(park, WorkerAttribute.VipReward) });
            }
            state.randomState = random.State;
        }
    }
}

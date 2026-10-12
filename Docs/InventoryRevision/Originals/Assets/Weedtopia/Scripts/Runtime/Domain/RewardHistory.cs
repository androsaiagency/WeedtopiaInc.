using System;
using System.Collections.Generic;
using System.Linq;

namespace Weedtopia.Domain
{
    public enum RewardKind { Money, Grinders, Cards, Worker, Title, SalesBonus, District, VipOrders }
    public enum RewardSource { Quest, CityProject, Vip, Daily, Recruitment }

    [Serializable] public sealed class RewardItem
    {
        public RewardKind kind;
        public string key, reference;
        public Amount quantity;
        public int rarity;
        public static RewardItem Of(RewardKind kind, Amount quantity, string key = null) =>
            new RewardItem { kind = kind, quantity = quantity, key = key };
        public static RewardItem ForWorker(WorkerState worker) => new RewardItem
        { kind = RewardKind.Worker, quantity = 1, key = worker.role, reference = worker.id, rarity = worker.rarity };
    }

    [Serializable] public sealed class RewardReceipt
    {
        public long sequence, simulationSeconds;
        public RewardSource source;
        public string sourceId, titleKey, cityId;
        public List<RewardItem> items = new List<RewardItem>();
    }

    /// <summary>Records already awarded rewards. Reading a receipt never grants anything.</summary>
    public static class RewardHistory
    {
        public const int Capacity = 100;
        public static List<RewardItem> Currency(Amount money, int cards = 0, int grinders = 0)
        {
            var items = new List<RewardItem>();
            if (money > 0) items.Add(RewardItem.Of(RewardKind.Money, money));
            if (cards > 0) items.Add(RewardItem.Of(RewardKind.Cards, cards));
            if (grinders > 0) items.Add(RewardItem.Of(RewardKind.Grinders, grinders));
            return items;
        }
        public static void Record(GameState state, RewardSource source, string sourceId, string titleKey, IEnumerable<RewardItem> rewards)
        {
            var items = rewards.ToList();
            if (items.Count == 0) return;
            if (state.rewardHistory == null) state.rewardHistory = new List<RewardReceipt>();
            state.rewardHistory.Add(new RewardReceipt { sequence = ++state.rewardSequence, source = source,
                sourceId = sourceId, titleKey = titleKey, cityId = state.activeCity, simulationSeconds = state.simulationSeconds, items = items });
            if (state.rewardHistory.Count > Capacity) state.rewardHistory.RemoveRange(0, state.rewardHistory.Count - Capacity);
        }
    }
}

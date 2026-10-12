using System;
using System.Collections.Generic;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    public sealed class ItemUseOffer
    {
        public string Token { get; internal set; }
        public string ItemId { get; internal set; }
        public string Target { get; internal set; }
        public int SecondsBefore { get; internal set; }
        public int SecondsAfter { get; internal set; }
        public int UnusedSeconds { get; internal set; }
        public string SplitItemId { get; internal set; }
        public int SplitCount { get; internal set; }
        internal string city;
        internal int level;
    }

    /// <summary>Inspecting never consumes. Each explicit confirmation consumes at most one item.</summary>
    public sealed class InventoryService
    {
        private readonly GameDefinitions defs;
        private readonly GameState state;
        private readonly WorkerService workers;
        private ItemUseOffer pending;
        public InventoryService(GameDefinitions defs, GameState state, WorkerService workers)
        { this.defs = defs; this.state = state; this.workers = workers; }
        public int Count(string id) => InventoryItems.Count(state, id);
        public BuildingState[] ConstructionTargets() => state.buildings.Where(b => b.cityId == state.activeCity &&
            b.buildingSeconds > 0 && defs.Building(b.family).zone <= state.City.unlocked).OrderBy(b => b.id).ToArray();
        public Failure Offer(string id, string target, out ItemUseOffer offer)
        {
            pending = null; offer = null;
            var item = defs.items?.FirstOrDefault(i => i.id == id);
            if (item == null || Count(id) < 1) return Failure.NotReady;
            var candidate = new ItemUseOffer { Token = Guid.NewGuid().ToString("N"), ItemId = id, Target = target, city = state.activeCity };
            if (item.effect == ItemEffect.ConstructionTime)
            {
                var b = ConstructionTargets().FirstOrDefault(x => x.id == target);
                if (b == null) return Failure.NotReady;
                candidate.level = b.level; candidate.SecondsBefore = b.buildingSeconds;
                candidate.SecondsAfter = Math.Max(0, b.buildingSeconds - item.value);
                candidate.UnusedSeconds = Math.Max(0, item.value - b.buildingSeconds);
            }
            if (item.effect == ItemEffect.WorkerChoice && !defs.roles.Any(r => r.id == target)) return Failure.Invalid;
            if (item.category == ItemCategory.Workers && state.City.unlocked < Zone.City) return Failure.Locked;
            if ((item.effect == ItemEffect.Cards && state.cards > int.MaxValue - item.value) ||
                (item.effect == ItemEffect.Grinders && state.grinders > int.MaxValue - item.value)) return Failure.Invalid;
            pending = candidate; offer = candidate; return Failure.None;
        }
        public ItemDefinition SplitTarget(string id)
        {
            var item = defs.items?.FirstOrDefault(i => i.id == id);
            return item?.effect == ItemEffect.ConstructionTime ? defs.items.Where(i => i.effect == ItemEffect.ConstructionTime &&
                i.value < item.value && item.value % i.value == 0).OrderByDescending(i => i.value).FirstOrDefault() : null;
        }
        public Failure OfferSplit(string id, out ItemUseOffer offer)
        {
            pending = null; offer = null;
            var smaller = SplitTarget(id);
            if (smaller == null || Count(id) < 1) return Failure.NotReady;
            int count = defs.items.Single(i => i.id == id).value / smaller.value;
            if (Count(smaller.id) > int.MaxValue - count) return Failure.Invalid;
            pending = new ItemUseOffer { Token = Guid.NewGuid().ToString("N"), ItemId = id, city = state.activeCity,
                SplitItemId = smaller.id, SplitCount = count };
            offer = pending; return Failure.None;
        }
        public void Cancel() { pending = null; }
        public Failure Confirm(string token)
        {
            var offer = pending; pending = null;
            if (offer == null || token != offer.Token || offer.city != state.activeCity || Count(offer.ItemId) < 1) return Failure.StaleOffer;
            var item = defs.items.Single(i => i.id == offer.ItemId);
            if (offer.SplitItemId != null)
            {
                var smaller = SplitTarget(item.id);
                if (smaller == null || smaller.id != offer.SplitItemId || Count(smaller.id) > int.MaxValue - offer.SplitCount) return Failure.StaleOffer;
                var exchanged = new List<RewardItem>();
                InventoryItems.Grant(state, new[] { new ItemStack { id = smaller.id, count = offer.SplitCount } }, exchanged);
                Consume(item, exchanged); return Failure.None;
            }
            BuildingState building = null;
            if (item.effect == ItemEffect.ConstructionTime)
            {
                building = ConstructionTargets().FirstOrDefault(b => b.id == offer.Target);
                // Reject a changed countdown instead of silently wasting more time than the preview promised.
                if (building == null || building.level != offer.level || building.buildingSeconds != offer.SecondsBefore) return Failure.StaleOffer;
            }
            if (item.category == ItemCategory.Workers && state.City.unlocked < Zone.City) return Failure.Locked;
            if ((item.effect == ItemEffect.Cards && state.cards > int.MaxValue - item.value) ||
                (item.effect == ItemEffect.Grinders && state.grinders > int.MaxValue - item.value)) return Failure.Invalid;
            var rewards = new List<RewardItem>();
            switch (item.effect)
            {
                case ItemEffect.ConstructionTime: ConstructionProgress.Reduce(state, building, item.value); break;
                case ItemEffect.Cards: state.cards += item.value; rewards.Add(RewardItem.Of(RewardKind.Cards, item.value)); break;
                case ItemEffect.WorkerChoice: rewards.Add(RewardItem.ForWorker(workers.Grant(offer.Target, item.rarity))); break;
                case ItemEffect.Money: state.money += item.value; state.City.stats.earned += item.value; rewards.Add(RewardItem.Of(RewardKind.Money, item.value)); break;
                case ItemEffect.Grinders: state.grinders += item.value; rewards.Add(RewardItem.Of(RewardKind.Grinders, item.value)); break;
                default: return Failure.Invalid;
            }
            Consume(item, rewards); return Failure.None;
        }
        private void Consume(ItemDefinition item, List<RewardItem> rewards)
        {
            var stack = state.inventory.Single(i => i.id == item.id);
            stack.count--; if (stack.count == 0) state.inventory.Remove(stack);
            RewardHistory.Record(state, RewardSource.Inventory, item.id, item.nameKey, rewards);
            state.revision++;
        }
    }
}

using System;
using System.IO;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Persistence.Reference
{
    internal static class RewardSaveValidator
    {
        public static void Validate(GameState state, GameDefinitions defs)
        {
            void Require(bool valid) { if (!valid) throw new InvalidDataException("Invalid reward history"); }
            var wheel = state.luckyWheel;
            if (wheel != null && !wheel.initialized)
                Require(wheel.day == 0 && wheel.spins == 0 && wheel.randomState == 0 && (wheel.prizes == null || wheel.prizes.Count == 0));
            if (wheel != null && wheel.initialized)
            {
                Require(wheel.day >= 0 && wheel.day <= state.highWaterUtcTicks / TimeSpan.TicksPerDay && wheel.randomState != 0);
                Require(wheel.spins >= 0 && wheel.spins <= LuckyWheelRules.Size);
                Require(wheel.prizes != null && wheel.prizes.Count == LuckyWheelRules.Size);
                Require(wheel.prizes.All(p => p != null && p.count > 0 && p.count <= 1000 &&
                    (defs.items ?? Array.Empty<ItemDefinition>()).Any(d => d.id == p.id)));
                Require(wheel.prizes.Select(p => p.id).Distinct().Count() == LuckyWheelRules.Size);
                Require(wheel.prizes.Count(p => p.claimed) == wheel.spins);
            }
            if (state.inventory != null)
            {
                Require(state.inventory.All(s => s != null && s.count > 0 && (defs.items ?? Array.Empty<ItemDefinition>()).Any(i => i.id == s.id)));
                Require(state.inventory.Select(s => s.id).Distinct().Count() == state.inventory.Count);
            }
            Require(state.rewardSequence >= 0 && state.rewardSequence < long.MaxValue);
            // Optional additive data: saves created before this feature have no historic receipts.
            if (state.rewardHistory == null) { Require(state.rewardSequence == 0); return; }
            Require(state.rewardHistory.Count <= RewardHistory.Capacity);
            long previous = 0;
            foreach (var receipt in state.rewardHistory)
            {
                Require(receipt != null && receipt.sequence > previous && receipt.sequence <= state.rewardSequence);
                Require(Enum.IsDefined(typeof(RewardSource), receipt.source) && !string.IsNullOrEmpty(receipt.sourceId));
                Require(defs.texts.Any(t => t.key == receipt.titleKey) && state.cities.Any(c => c.id == receipt.cityId));
                Require(receipt.simulationSeconds >= 0 && receipt.simulationSeconds <= state.simulationSeconds);
                Require(receipt.items != null && receipt.items.Count > 0 && receipt.items.Count <= 16);
                foreach (var item in receipt.items)
                {
                    Require(item != null && Enum.IsDefined(typeof(RewardKind), item.kind) && item.quantity.IsValid && item.quantity > 0);
                    Require(item.rarity >= 0 && item.rarity <= 4);
                    if (item.kind == RewardKind.Item)
                        Require((defs.items ?? Array.Empty<ItemDefinition>()).Any(i => i.id == item.key) && item.quantity <= int.MaxValue &&
                            item.quantity.ToDouble() == Math.Floor(item.quantity.ToDouble()));
                    if (item.kind == RewardKind.Worker)
                        Require(item.quantity == 1 && state.workers.Any(w => w.id == item.reference && w.role == item.key && w.rarity == item.rarity));
                    if (item.kind == RewardKind.Title) Require(item.quantity == 1 && (item.key == "founder" || item.key == "festival"));
                    if (item.kind == RewardKind.StoryMemory) Require(item.quantity == 1 && VipStoryRules.Quests(defs).Any(q => q.memoryKey == item.key && q.id == receipt.sourceId) && receipt.source == RewardSource.VipStory);
                    if (item.kind == RewardKind.SalesBonus) Require(item.quantity <= 50 && defs.buildings.Any(b => b.id == item.key));
                    if (item.kind == RewardKind.District) Require(item.quantity == 1 && Enum.TryParse(item.key, out Zone zone) && Enum.IsDefined(typeof(Zone), zone));
                }
                previous = receipt.sequence;
            }
            Require(state.rewardHistory.Count == 0 ? state.rewardSequence == 0 : previous == state.rewardSequence);
        }
    }
}

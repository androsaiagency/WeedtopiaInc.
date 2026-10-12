using System;
using System.IO;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Persistence
{
    internal static class RewardSaveValidator
    {
        public static void Validate(GameState state, GameDefinitions defs)
        {
            void Require(bool valid) { if (!valid) throw new InvalidDataException("Invalid reward history"); }
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
                    if (item.kind == RewardKind.Worker)
                        Require(item.quantity == 1 && state.workers.Any(w => w.id == item.reference && w.role == item.key && w.rarity == item.rarity));
                    if (item.kind == RewardKind.Title) Require(item.quantity == 1 && (item.key == "founder" || item.key == "festival"));
                    if (item.kind == RewardKind.SalesBonus) Require(item.quantity <= 50 && defs.buildings.Any(b => b.id == item.key));
                    if (item.kind == RewardKind.District) Require(item.quantity == 1 && Enum.TryParse(item.key, out Zone zone) && Enum.IsDefined(typeof(Zone), zone));
                }
                previous = receipt.sequence;
            }
            Require(state.rewardHistory.Count == 0 ? state.rewardSequence == 0 : previous == state.rewardSequence);
        }
    }
}

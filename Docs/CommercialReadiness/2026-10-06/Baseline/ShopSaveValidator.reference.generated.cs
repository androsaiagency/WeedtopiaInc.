using System;
using System.IO;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Persistence.Reference
{
    internal static class ShopSaveValidator
    {
        public static void RestoreSerializedNulls(GameState state)
        {
            var shop = state.shop;
            if (shop == null) return;
            // JsonUtility materializes absent/null nested plain classes as objects with zero-valued fields.
            var vip = shop.vipMembership;
            if (vip != null && vip.startedUtcTicks == 0 && vip.expiresUtcTicks == 0 && vip.rewardedDays == 0) shop.vipMembership = null;
            if (shop.vipMembership == null && (shop.purchases == null || shop.purchases.Count == 0)) state.shop = null;
        }
        public static void Validate(GameState state, GameDefinitions definitions)
        {
            if (state.shop == null) return; // Historical saves keep every existing balance and progression value.
            void Require(bool valid) { if (!valid) throw new InvalidDataException("Invalid shop state"); }
            var purchases = state.shop.purchases;
            Require(purchases != null && purchases.All(p => p != null && !string.IsNullOrWhiteSpace(p.transactionId) &&
                p.transactionId.Length <= 256 && p.utcTicks >= 0 && p.utcTicks <= state.highWaterUtcTicks &&
                (p.channel == "Test" || p.channel == "Store") && (definitions.shopProducts ?? Array.Empty<ShopProductDefinition>()).Any(d => d.id == p.productId)));
            Require(purchases.Select(p => p.transactionId).Distinct().Count() == purchases.Count);
            var vip = state.shop.vipMembership;
            if (vip == null) return;
            Require(vip.startedUtcTicks >= 0 && vip.startedUtcTicks <= state.highWaterUtcTicks && vip.expiresUtcTicks <= DateTime.MaxValue.Ticks &&
                vip.expiresUtcTicks > vip.startedUtcTicks && (vip.expiresUtcTicks - vip.startedUtcTicks) % TimeSpan.TicksPerDay == 0);
            Require(vip.rewardedDays >= 0 && vip.rewardedDays <= ShopRules.DueVipDays(vip, state.highWaterUtcTicks));
        }
    }
}

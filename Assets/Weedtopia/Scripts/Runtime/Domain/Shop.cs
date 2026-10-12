using System;
using System.Collections.Generic;
using System.Linq;

namespace Weedtopia.Domain
{
    public enum ShopCategory { Packages, Vip, Money, Grinders, Boosters }
    [Serializable] public sealed class ShopProductDefinition
    {
        public string id, titleKey, icon;
        public ShopCategory category;
        public int euroCents, money, grinders, vipDays;
        public ItemStack[] items = Array.Empty<ItemStack>();
    }
    [Serializable] public sealed class ShopPurchaseReceipt
    {
        public string transactionId, productId, channel;
        public long utcTicks;
    }
    [Serializable] public sealed class VipMembershipState
    {
        public long startedUtcTicks, expiresUtcTicks;
        public int rewardedDays;
    }
    [Serializable] public sealed class ShopState
    {
        // This ledger is independent of the bounded visual reward history: old transactions cannot grant twice.
        public List<ShopPurchaseReceipt> purchases = new List<ShopPurchaseReceipt>();
        public VipMembershipState vipMembership;
    }
    public static class ShopRules
    {
        public const int DailyGrinders = 50;
        public static bool VipActive(GameState state, long ticks) => state.shop?.vipMembership != null &&
            ticks >= state.shop.vipMembership.startedUtcTicks && ticks < state.shop.vipMembership.expiresUtcTicks;
        public static double ProductionMultiplier(GameState state, long ticks) => VipActive(state, ticks) ? 1.5 : 1;
        public static double FlowerPriceMultiplier(GameState state, long ticks) => VipActive(state, ticks) ? 1.5 : 1;
        public static int DueVipDays(VipMembershipState vip, long now)
        {
            if (vip == null || now < vip.startedUtcTicks) return 0;
            long total = (vip.expiresUtcTicks - vip.startedUtcTicks) / TimeSpan.TicksPerDay;
            return (int)Math.Min(total, (now - vip.startedUtcTicks) / TimeSpan.TicksPerDay + 1);
        }
        public static void ValidateCatalog(GameDefinitions defs)
        {
            var products = defs.shopProducts ?? Array.Empty<ShopProductDefinition>();
            if (products.Any(p => p == null || string.IsNullOrWhiteSpace(p.id)) || products.Select(p => p.id).Distinct().Count() != products.Length)
                throw new InvalidOperationException("Invalid shop product IDs");
            foreach (var p in products)
            {
                if (!Enum.IsDefined(typeof(ShopCategory), p.category) || p.euroCents <= 0 || p.money < 0 || p.grinders < 0 ||
                    p.vipDays < 0 || p.vipDays > 365 || string.IsNullOrWhiteSpace(p.icon) || !defs.texts.Any(t => t.key == p.titleKey) ||
                    (p.money == 0 && p.grinders == 0 && p.vipDays == 0 && (p.items == null || p.items.Length == 0)))
                    throw new InvalidOperationException("Invalid shop product " + p.id);
                InventoryItems.ValidateGrants(defs, p.items);
            }
        }
    }
}

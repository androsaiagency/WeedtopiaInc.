using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    public sealed class ShopPurchaseResult
    {
        public Failure failure;
        public bool cancelled, alreadyFulfilled;
    }
    /// <summary>Provider-independent fulfillment, permanent transaction deduplication and atomic persistence.</summary>
    public sealed class ShopService
    {
        private readonly GameDefinitions defs;
        private readonly GameState state;
        private readonly IClock clock;
        private readonly Action persist;
        private readonly IShopPurchaseGateway gateway;
        public bool IsTestMode => gateway.IsTestMode;
        public bool Purchasing { get; private set; }
        public long NowTicks => Math.Max(clock.UtcNow.Ticks, state.highWaterUtcTicks);
        public bool VipActive => ShopRules.VipActive(state, NowTicks);
        public ShopService(GameDefinitions definitions, GameState game, IClock time, Action save, IShopPurchaseGateway provider = null)
        { defs = definitions; state = game; clock = time; persist = save; gateway = provider ?? new UnavailableShopPurchaseGateway(); }
        public ShopListing Listing(ShopProductDefinition product) => gateway.Listing(product);
        public async Task<ShopPurchaseResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default)
        {
            var product = (defs.shopProducts ?? Array.Empty<ShopProductDefinition>()).FirstOrDefault(p => p.id == productId);
            if (product == null) return new ShopPurchaseResult { failure = Failure.Invalid };
            if (Purchasing) return new ShopPurchaseResult { failure = Failure.Busy };
            if (!gateway.Enabled || !Listing(product).available) return new ShopPurchaseResult { failure = Failure.NotReady };
            Purchasing = true;
            try
            {
                var authorization = await gateway.PurchaseAsync(productId, cancellationToken);
                if (authorization == null || authorization.productId != productId) return new ShopPurchaseResult { failure = Failure.Invalid };
                return Complete(authorization);
            }
            catch (OperationCanceledException) { return new ShopPurchaseResult { cancelled = true }; }
            finally { Purchasing = false; }
        }
        public ShopPurchaseResult Complete(ShopPurchaseAuthorization authorization)
        {
            if (authorization?.cancelled == true) return new ShopPurchaseResult { cancelled = true };
            var product = (defs.shopProducts ?? Array.Empty<ShopProductDefinition>()).FirstOrDefault(p => p.id == authorization?.productId);
            if (authorization == null || !authorization.approved || product == null || !gateway.Enabled ||
                string.IsNullOrWhiteSpace(authorization.transactionId) || authorization.transactionId.Length > 256 ||
                !Enum.IsDefined(typeof(ShopPurchaseChannel), authorization.channel) ||
                gateway.IsTestMode != (authorization.channel == ShopPurchaseChannel.Test))
                return new ShopPurchaseResult { failure = Failure.Invalid };
            var existing = state.shop?.purchases?.FirstOrDefault(p => p.transactionId == authorization.transactionId);
            if (existing != null) return new ShopPurchaseResult { failure = existing.productId == product.id ? Failure.None : Failure.Invalid, alreadyFulfilled = existing.productId == product.id };
            long now = NowTicks;
            var vip = Clone(state.shop?.vipMembership);
            int expiredDays = product.vipDays > 0 && vip != null && now >= vip.expiresUtcTicks ? ShopRules.DueVipDays(vip, now) - vip.rewardedDays : 0;
            if (product.vipDays > 0)
            {
                long start = vip != null && now < vip.expiresUtcTicks ? vip.expiresUtcTicks : now;
                long duration = product.vipDays * TimeSpan.TicksPerDay;
                if (start > DateTime.MaxValue.Ticks - duration) return new ShopPurchaseResult { failure = Failure.Invalid };
                if (vip == null || now >= vip.expiresUtcTicks) vip = new VipMembershipState { startedUtcTicks = now };
                vip.expiresUtcTicks = start + duration;
            }
            int dueDays = vip == null ? 0 : ShopRules.DueVipDays(vip, now) - vip.rewardedDays;
            long grinders = (long)product.grinders + (dueDays + expiredDays) * (long)ShopRules.DailyGrinders;
            if (grinders > int.MaxValue - state.grinders || (product.items ?? Array.Empty<ItemStack>()).Any(i => InventoryItems.Count(state, i.id) > int.MaxValue - i.count))
                return new ShopPurchaseResult { failure = Failure.Invalid };
            Atomic(() =>
            {
                state.shop ??= new ShopState(); state.shop.purchases ??= new List<ShopPurchaseReceipt>();
                state.shop.vipMembership = vip;
                if (vip != null) vip.rewardedDays += dueDays;
                // Bought currency is a grant, not flower-sale income for quest counters.
                state.money += product.money; state.grinders += (int)grinders;
                var rewards = RewardHistory.Currency(product.money, grinders: (int)grinders);
                InventoryItems.Grant(state, product.items, rewards);
                state.shop.purchases.Add(new ShopPurchaseReceipt { transactionId = authorization.transactionId,
                    productId = product.id, channel = authorization.channel.ToString(), utcTicks = now });
                RewardHistory.Record(state, RewardSource.Shop, product.id, product.titleKey, rewards);
                state.revision++;
            });
            return new ShopPurchaseResult { failure = Failure.None };
        }
        public bool RefreshVip()
        {
            var vip = state.shop?.vipMembership;
            int due = vip == null ? 0 : ShopRules.DueVipDays(vip, NowTicks) - vip.rewardedDays;
            if (due <= 0 || (long)due * ShopRules.DailyGrinders > int.MaxValue - state.grinders) return false;
            Atomic(() =>
            {
                int grant = due * ShopRules.DailyGrinders;
                state.grinders += grant; vip.rewardedDays += due; state.revision++;
                RewardHistory.Record(state, RewardSource.VipMembership, "vip_daily", "shop.vip_daily",
                    new[] { RewardItem.Of(RewardKind.Grinders, grant) });
            });
            return true;
        }
        private void Atomic(Action action)
        {
            var oldShop = state.shop;
            var purchases = oldShop?.purchases?.ToList(); var vip = Clone(oldShop?.vipMembership);
            var inventory = state.inventory?.Select(i => new ItemStack { id = i.id, count = i.count }).ToList();
            var rewards = state.rewardHistory?.ToList();
            var money = state.money; int grinders = state.grinders;
            long revision = state.revision, sequence = state.rewardSequence, last = state.lastUtcTicks, high = state.highWaterUtcTicks;
            try { action(); persist?.Invoke(); }
            catch
            {
                state.shop = oldShop;
                if (oldShop != null) { oldShop.purchases = purchases; oldShop.vipMembership = vip; }
                state.inventory = inventory; state.rewardHistory = rewards; state.money = money; state.grinders = grinders;
                state.revision = revision; state.rewardSequence = sequence; state.lastUtcTicks = last; state.highWaterUtcTicks = high;
                throw;
            }
        }
        private static VipMembershipState Clone(VipMembershipState vip) => vip == null ? null : new VipMembershipState
        { startedUtcTicks = vip.startedUtcTicks, expiresUtcTicks = vip.expiresUtcTicks, rewardedDays = vip.rewardedDays };
    }
}

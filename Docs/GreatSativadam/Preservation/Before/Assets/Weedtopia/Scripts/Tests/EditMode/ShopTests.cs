using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Persistence;

namespace Weedtopia.Tests
{
    public sealed class ShopTests
    {
        private GameDefinitions defs;
        private GameState state;
        private GameSession session;
        private ManualClock clock;
        [SetUp] public void Setup()
        {
            defs = ProgressionTests.Definitions(); clock = new ManualClock(new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));
            state = GameState.Create(defs, "Shoptest", clock.UtcNow);
            session = new GameSession(defs, state, clock, shopGateway: new TestShopPurchaseGateway()); session.CompleteIntro();
        }
        private ShopPurchaseAuthorization Receipt(string suffix, string transaction = "test_once") => new ShopPurchaseAuthorization
        { approved = true, productId = "weedtopia." + suffix, transactionId = transaction, channel = ShopPurchaseChannel.Test };
        [Test] public void ApprovedCatalogContainsEveryRequestedPriceAndExactBundleContents()
        {
            string[] ids = { "vip_30d", "starter", "development", "boost_5m", "boost_15m", "boost_1h", "boost_5h", "money_1000", "money_5000", "money_10000", "money_100000", "money_1000000", "grinders_50", "grinders_200", "grinders_500", "grinders_1000", "grinders_10000" };
            int[] prices = { 499, 299, 499, 99, 199, 299, 499, 99, 199, 299, 499, 999, 99, 199, 299, 499, 999 };
            Assert.That(defs.shopProducts.Select(p => p.id), Is.EqualTo(ids.Select(id => "weedtopia." + id)));
            Assert.That(defs.shopProducts.Select(p => p.euroCents), Is.EqualTo(prices));
            var starter = defs.shopProducts.Single(p => p.id == "weedtopia.starter");
            var development = defs.shopProducts.Single(p => p.id == "weedtopia.development");
            Assert.That((starter.money, starter.grinders, starter.items.Single().id, starter.items.Single().count), Is.EqualTo((10000, 200, "build_5m", 5)));
            Assert.That((development.money, development.grinders, development.items.Single().id, development.items.Single().count), Is.EqualTo((50000, 500, "build_15m", 5)));
            Assert.That(defs.shopProducts.Where(p => p.category == ShopCategory.Boosters).Select(p => defs.items.Single(i => i.id == p.items.Single().id).value), Is.EqualTo(new[] { 300, 900, 3600, 18000 }));
        }
        [TestCase("vip_30d")][TestCase("starter")][TestCase("development")]
        [TestCase("boost_5m")][TestCase("boost_15m")][TestCase("boost_1h")][TestCase("boost_5h")]
        [TestCase("money_1000")][TestCase("money_5000")][TestCase("money_10000")][TestCase("money_100000")][TestCase("money_1000000")]
        [TestCase("grinders_50")][TestCase("grinders_200")][TestCase("grinders_500")][TestCase("grinders_1000")][TestCase("grinders_10000")]
        public void EachProductGrantsExactlyOnceAndRemainsDeduplicatedAfterSaveLoad(string id)
        {
            var product = defs.shopProducts.Single(p => p.id == "weedtopia." + id);
            var authorization = Receipt(id);
            Assert.That(session.Shop.Complete(authorization).failure, Is.EqualTo(Failure.None));
            Assert.That(state.money, Is.EqualTo(new Amount(1000 + product.money)));
            Assert.That(state.grinders, Is.EqualTo(50 + product.grinders + (product.vipDays > 0 ? 50 : 0)));
            Assert.That(state.City.stats.earned, Is.EqualTo(Amount.Zero));
            foreach (var item in product.items) Assert.That(session.Inventory.Count(item.id), Is.EqualTo(item.count));
            string folder = Path.Combine(UnityEngine.Application.temporaryCachePath, "ShopReceipt_" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new LocalSaveStore(Path.Combine(folder, "save.json"), defs); session.Save(); store.Save(state);
                var loaded = store.Load().State; Assert.That(loaded, Is.Not.Null);
                var resumed = new GameSession(defs, loaded, clock, store, new TestShopPurchaseGateway());
                Assert.That(resumed.Shop.Complete(authorization).alreadyFulfilled, Is.True);
                Assert.That(loaded.money, Is.EqualTo(state.money)); Assert.That(loaded.grinders, Is.EqualTo(state.grinders));
                Assert.That(loaded.shop.purchases.Count, Is.EqualTo(1));
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        [Test] public void RepeatedTestOrdersHaveDifferentReceiptsAndCanBeUsedForProgressTesting()
        {
            Assert.That(session.Shop.PurchaseAsync("weedtopia.money_1000000").GetAwaiter().GetResult().failure, Is.EqualTo(Failure.None));
            Assert.That(session.Shop.PurchaseAsync("weedtopia.money_1000000").GetAwaiter().GetResult().failure, Is.EqualTo(Failure.None));
            Assert.That(state.money, Is.EqualTo(new Amount(2001000))); Assert.That(state.shop.purchases.Count, Is.EqualTo(2));
        }
        [Test] public void CancelledRejectedAndWrongChannelPurchasesCannotGrantResources()
        {
            var receipt = Receipt("starter"); receipt.cancelled = true;
            Assert.That(session.Shop.Complete(receipt).cancelled, Is.True);
            receipt.cancelled = false; receipt.approved = false;
            Assert.That(session.Shop.Complete(receipt).failure, Is.EqualTo(Failure.Invalid));
            receipt.approved = true; receipt.channel = ShopPurchaseChannel.Store;
            Assert.That(session.Shop.Complete(receipt).failure, Is.EqualTo(Failure.Invalid));
            Assert.That(state.money, Is.EqualTo(new Amount(1000))); Assert.That(state.inventory, Is.Empty); Assert.That(state.shop, Is.Null);
            var release = new GameSession(defs, state, clock);
            Assert.That(release.Shop.PurchaseAsync("weedtopia.starter").GetAwaiter().GetResult().failure, Is.EqualTo(Failure.NotReady));
        }
        [Test] public void IntegerOverflowCannotPartiallyGrantAPackage()
        {
            state.grinders = int.MaxValue - 10;
            Assert.That(session.Shop.Complete(Receipt("starter")).failure, Is.EqualTo(Failure.Invalid));
            Assert.That(state.money, Is.EqualTo(new Amount(1000))); Assert.That(state.inventory, Is.Empty); Assert.That(state.shop, Is.Null);
            state.grinders = 50;
            state.inventory.Add(new ItemStack { id = "build_5m", count = int.MaxValue });
            Assert.That(session.Shop.Complete(Receipt("starter")).failure, Is.EqualTo(Failure.Invalid));
            Assert.That(state.money, Is.EqualTo(new Amount(1000))); Assert.That(state.grinders, Is.EqualTo(50));
        }
        private sealed class BrokenSave : ISaveStore
        {
            public bool Exists => false;
            public void Save(GameState value) => throw new IOException("Expected disk failure");
            public SaveLoadResult Load() => new SaveLoadResult();
        }
        [Test] public void FailedPersistenceRollsBackCurrencyItemsVipLedgerAndRewardHistory()
        {
            var broken = new GameSession(defs, state, clock, new BrokenSave(), new TestShopPurchaseGateway());
            string before = JsonUtility.ToJson(state);
            Assert.Throws<IOException>(() => broken.Shop.Complete(Receipt("starter")));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.Throws<IOException>(() => broken.Shop.Complete(Receipt("vip_30d")));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
        }
        [Test] public void VipGrantsThirtyDailyPaymentsCatchesUpAndDoesNotRepeatOnClockRollback()
        {
            session.Shop.Complete(Receipt("vip_30d")); Assert.That(state.grinders, Is.EqualTo(100));
            clock.UtcNow = clock.UtcNow.AddDays(4); session.Shop.RefreshVip(); Assert.That(state.grinders, Is.EqualTo(300));
            session.Shop.RefreshVip(); Assert.That(state.grinders, Is.EqualTo(300));
            clock.UtcNow = clock.UtcNow.AddDays(-3); session.Shop.RefreshVip(); Assert.That(state.grinders, Is.EqualTo(300));
            clock.UtcNow = new DateTime(state.shop.vipMembership.expiresUtcTicks, DateTimeKind.Utc); session.Shop.RefreshVip();
            Assert.That(state.grinders, Is.EqualTo(1550)); Assert.That(session.Shop.VipActive, Is.False);
            clock.UtcNow = clock.UtcNow.AddDays(20); session.Shop.RefreshVip(); Assert.That(state.grinders, Is.EqualTo(1550));
        }
        [Test] public void VipExtensionAddsThirtyDaysWithoutStackingSpeedOrGivingTwoPaymentsToday()
        {
            session.Shop.Complete(Receipt("vip_30d", "one")); var start = clock.UtcNow;
            session.Shop.Complete(Receipt("vip_30d", "two"));
            Assert.That(state.shop.vipMembership.expiresUtcTicks, Is.EqualTo(start.AddDays(60).Ticks));
            Assert.That(state.grinders, Is.EqualTo(100)); Assert.That(ShopRules.ProductionMultiplier(state, start.Ticks), Is.EqualTo(1.5));
            clock.UtcNow = start.AddDays(60); session.Shop.RefreshVip(); Assert.That(state.grinders, Is.EqualTo(3050));
        }
        [Test] public void RenewingExpiredVipPreservesMissedDailyGrindersBeforeStartingNewPeriod()
        {
            session.Shop.Complete(Receipt("vip_30d", "one")); clock.UtcNow = clock.UtcNow.AddDays(35);
            session.Shop.Complete(Receipt("vip_30d", "two"));
            Assert.That(state.grinders, Is.EqualTo(1600)); Assert.That(state.shop.vipMembership.rewardedDays, Is.EqualTo(1));
        }
        [Test] public void OldSavesWithoutShopStateLoadWithOriginalBalancesAndNoVip()
        {
            var store = new LocalSaveStore("Docs/ShopRevision/unused.json", defs);
            string payload = JsonUtility.ToJson(state);
            var envelope = JsonUtility.ToJson(new SaveEnvelope { payload = payload, sha256 = LocalSaveStore.Hash(payload) });
            var old = store.Decode(envelope); Assert.That(old.shop, Is.Null); Assert.That(old.money, Is.EqualTo(state.money)); Assert.That(old.grinders, Is.EqualTo(state.grinders));
        }
        [Test] public void CorruptDuplicatePurchaseLedgerIsRejected()
        {
            session.Shop.Complete(Receipt("starter")); state.shop.purchases.Add(state.shop.purchases[0]);
            Assert.Throws<InvalidDataException>(() => SaveValidator.Validate(state, defs));
        }
        private BuildingState FlowerShop()
        {
            state.City.unlocked = Zone.Industry;
            var building = state.buildings.First(b => b.cityId == state.activeCity && defs.Building(b.family).kind == BuildingKind.Shop);
            building.level = 1; building.workSeconds = 30; building.refined = 100;
            defs.Building(building.family).rate.basis = 2; defs.Building(building.family).price.basis = 10;
            return building;
        }
        [Test] public void VipMakesFlowerThroughputAndPerUnitSalePriceFiftyPercentHigher()
        {
            FlowerShop(); session.Shop.Complete(Receipt("vip_30d")); session.Simulation.Advance(1, clock.UtcNow.Ticks);
            Assert.That(state.City.stats.coffeeSold, Is.EqualTo(new Amount(3)));
            Assert.That(state.City.stats.earned, Is.EqualTo(new Amount(45)));
        }
        [Test] public void OfflineCalculationSplitsTheVipExpiryInsteadOfApplyingItToTheEntireAbsence()
        {
            FlowerShop(); session.Shop.Complete(Receipt("vip_30d"));
            clock.UtcNow = new DateTime(state.shop.vipMembership.expiresUtcTicks, DateTimeKind.Utc).AddSeconds(-2); session.Save();
            clock.UtcNow = clock.UtcNow.AddSeconds(4); var summary = session.ApplyOffline();
            Assert.That(summary.Seconds, Is.EqualTo(4)); Assert.That(summary.Earned, Is.EqualTo(new Amount(130)));
            Assert.That(state.City.stats.coffeeSold, Is.EqualTo(new Amount(10))); Assert.That(session.Shop.VipActive, Is.False);
        }
        [Test] public void VipGrowCycleProducesThreeBatchesDuringTwoRegularCycles()
        {
            var grow = state.buildings.First(b => defs.Building(b.family).kind == BuildingKind.Grow);
            var definition = defs.Building(grow.family); definition.cycleSeconds = 12; definition.capacity.basis = 100000;
            grow.level = 1; session.Shop.Complete(Receipt("vip_30d")); session.Simulation.Advance(24, clock.UtcNow.Ticks);
            Assert.That(state.City.stats.rawProduced, Is.EqualTo(definition.yield.At(1) * 3));
        }
        private sealed class DeferredGateway : IShopPurchaseGateway
        {
            public bool Enabled => true; public bool IsTestMode => true;
            public readonly TaskCompletionSource<ShopPurchaseAuthorization> pending = new TaskCompletionSource<ShopPurchaseAuthorization>();
            public ShopListing Listing(ShopProductDefinition p) => new ShopListing { available = true, localizedPrice = "2,99 €" };
            public Task<ShopPurchaseAuthorization> PurchaseAsync(string product, CancellationToken cancellationToken) => pending.Task;
        }
        [Test] public async Task PendingPurchaseBlocksSecondOrderAndCannotGrantOnMismatchedApproval()
        {
            var gateway = new DeferredGateway(); var pending = new GameSession(defs, state, clock, shopGateway: gateway);
            var first = pending.Shop.PurchaseAsync("weedtopia.starter"); Assert.That(pending.Shop.Purchasing, Is.True);
            Assert.That((await pending.Shop.PurchaseAsync("weedtopia.development")).failure, Is.EqualTo(Failure.Busy));
            gateway.pending.SetResult(Receipt("development")); Assert.That((await first).failure, Is.EqualTo(Failure.Invalid));
            Assert.That(state.money, Is.EqualTo(new Amount(1000))); Assert.That(pending.Shop.Purchasing, Is.False);
        }
    }
}

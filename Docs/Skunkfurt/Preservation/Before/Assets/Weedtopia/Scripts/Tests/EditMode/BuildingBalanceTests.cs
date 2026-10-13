using System;
using System.Linq;
using NUnit.Framework;
using Weedtopia.Application;
using Weedtopia.Domain;

namespace Weedtopia.Tests
{
    public sealed class BuildingBalanceTests
    {
        private static readonly DateTime Start = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

        [TestCase("cultivation_plot", 100, 30)]
        [TestCase("farm_processing", 150, 60)]
        [TestCase("farm_warehouse", 175, 90)]
        [TestCase("city_warehouse", 200, 120)]
        [TestCase("coffee_shop", 250, 120)]
        [TestCase("residential_house", 1200, 300)]
        [TestCase("park", 800, 180)]
        [TestCase("butter_factory", 2500, 480)]
        [TestCase("supermarket", 2000, 360)]
        [TestCase("bakery", 3500, 600)]
        public void FirstConstructionMatchesApprovedTable(string family, int cost, int seconds)
        {
            var building = ProgressionTests.Definitions().Building(family);
            Assert.That(building.cost.At(1), Is.EqualTo(new Amount(cost)));
            Assert.That(building.BuildSeconds(1), Is.EqualTo(seconds));
        }

        [Test] public void EntireFirstSaleChainFitsStartMoneyWithoutCashRewards()
        {
            var defs = ProgressionTests.Definitions();
            var state = GameState.Create(defs, "Startbudget", Start);
            Amount total = 0;
            foreach (string id in new[] { "farm_grow_01", "farm_processing", "farm_warehouse", "city_warehouse", "coffee_01" })
                total += defs.Building(state.Find(id).family).cost.At(1);
            Assert.That(total, Is.EqualTo(new Amount(875)));
            Assert.That(state.money - total, Is.EqualTo(new Amount(125)));
        }

        [Test] public void CostGrowthChangesAfterTargetLevel25WithoutAJumpOrEffectOnProduction()
        {
            var building = ProgressionTests.Definitions().Building("coffee_shop");
            Assert.That(building.cost.At(2).ToDouble(), Is.EqualTo(270).Within(1e-8));
            Assert.That(building.cost.At(25).ToDouble(), Is.EqualTo(250 * Math.Pow(1.08, 24)).Within(1e-6));
            Assert.That((building.cost.At(26) / building.cost.At(25)).ToDouble(), Is.EqualTo(1.012).Within(1e-10));
            Assert.That(building.rate.At(100).ToDouble(), Is.EqualTo(Math.Pow(1.008, 99)).Within(1e-9));
            Assert.That(building.price.At(100).ToDouble(), Is.EqualTo(12 * Math.Pow(1.004, 99)).Within(1e-8));
            // Old content without the optional early-growth fields must remain valid.
            Assert.That(new Curve { basis = 10, growth = 2, earlyGrowth = 0 }.At(3), Is.EqualTo(new Amount(40)));
        }

        [TestCase(1, 120)]
        [TestCase(2, 0)]
        [TestCase(4, 0)]
        [TestCase(5, 150)]
        [TestCase(10, 180)]
        [TestCase(25, 240)]
        [TestCase(50, 300)]
        [TestCase(100, 360)]
        [TestCase(150, 480)]
        [TestCase(151, 0)]
        [TestCase(200, 480)]
        [TestCase(4999, 0)]
        [TestCase(5000, 480)]
        public void OnlyVisibleTierChangesCreateConstruction(int level, int seconds)
        {
            Assert.That(ProgressionTests.Definitions().Building("coffee_shop").BuildSeconds(level), Is.EqualTo(seconds));
        }

        [Test] public void InstantUpgradeChargesOnceAndCannotBeAccelerated()
        {
            var defs = ProgressionTests.Definitions();
            var state = GameState.Create(defs, "Sofortausbau", Start);
            var building = state.Find("farm_grow_01"); building.level = 1;
            var session = new GameSession(defs, state, new ManualClock(Start));
            long revision = state.revision;
            Assert.That(session.Upgrades.Duration(building), Is.Zero);
            Assert.That(session.Upgrades.BuyWithMoney(building.id), Is.EqualTo(Failure.None));
            Assert.That(building.level, Is.EqualTo(2));
            Assert.That(building.buildingSeconds, Is.Zero);
            Assert.That(state.money, Is.EqualTo(new Amount(892)));
            Assert.That(state.revision, Is.EqualTo(revision + 1));
            Assert.That(session.Upgrades.Offer(building.id, true, out _), Is.EqualTo(Failure.Invalid));
            session.Simulation.Advance(10);
            Assert.That(building.level, Is.EqualTo(2), "No delayed second completion");
        }

        [Test] public void PremiumInstantUpgradeAndTimedCompletionRemainSingleUse()
        {
            var defs = ProgressionTests.Definitions();
            var state = GameState.Create(defs, "Premiumausbau", Start); state.money = 0;
            var building = state.Find("farm_grow_01"); building.level = 1;
            var session = new GameSession(defs, state, new ManualClock(Start));
            Assert.That(session.Upgrades.Offer(building.id, false, out var offer), Is.EqualTo(Failure.None));
            Assert.That(offer.Grinders, Is.EqualTo(2));
            Assert.That(session.Upgrades.Confirm(offer.Token), Is.EqualTo(Failure.None));
            Assert.That(session.Upgrades.Confirm(offer.Token), Is.EqualTo(Failure.StaleOffer));
            Assert.That(building.level, Is.EqualTo(2)); Assert.That(building.buildingSeconds, Is.Zero);
            Assert.That(state.grinders, Is.EqualTo(48));
            building.level = 4; state.money = 1000;
            Assert.That(session.Upgrades.BuyWithMoney(building.id), Is.EqualTo(Failure.None));
            Assert.That(building.buildingSeconds, Is.EqualTo(38));
            Assert.That(session.Upgrades.Offer(building.id, true, out offer), Is.EqualTo(Failure.None));
            Assert.That(session.Upgrades.Confirm(offer.Token), Is.EqualTo(Failure.None));
            Assert.That(session.Upgrades.Confirm(offer.Token), Is.EqualTo(Failure.StaleOffer));
            session.Simulation.Advance(60);
            Assert.That(building.level, Is.EqualTo(5)); Assert.That(state.grinders, Is.EqualTo(47));
        }

        [Test] public void TimedUpgradeKeepsProducingAndBuildSpeedMatchesItsPreview()
        {
            var defs = ProgressionTests.Definitions();
            var state = GameState.Create(defs, "Weiterproduktion", Start);
            var building = state.Find("farm_grow_01"); building.level = 4;
            var session = new GameSession(defs, state, new ManualClock(Start));
            var worker = session.Workers.Grant("grow", 4);
            Assert.That(session.Workers.Assign(worker.id, building.id, 0), Is.EqualTo(Failure.None));
            int duration = session.Upgrades.Duration(building);
            Assert.That(duration, Is.EqualTo(37)); // ceil(38 / 1.05)
            Assert.That(session.Upgrades.BuyWithMoney(building.id), Is.EqualTo(Failure.None));
            Assert.That(building.buildingSeconds, Is.EqualTo(duration));
            Assert.That(session.Upgrades.BuyWithMoney(building.id), Is.EqualTo(Failure.Busy));
            session.Simulation.Advance(4);
            Assert.That(state.City.stats.rawProduced > 0, Is.True);
            Assert.That(building.level, Is.EqualTo(4));
            session.Simulation.Advance(duration - 4);
            Assert.That(building.level, Is.EqualTo(5)); Assert.That(building.buildingSeconds, Is.Zero);
        }

        [TestCase(1)]
        [TestCase(42)]
        [TestCase(420)]
        public void TutorialFinancesAllBuildingsWithoutRecoveryOrPremium(int seed)
        {
            var defs = ProgressionTests.Definitions();
            var state = GameState.Create(defs, "Balancing", Start, (uint)seed);
            var clock = new ManualClock(Start);
            var session = new GameSession(defs, state, clock);
            int firstSale = -1;
            for (int second = 0; second < TutorialDriver.MaxSimulationSeconds && !state.City.freeplay; second++)
            {
                TutorialDriver.Act(session, clock.UtcNow, allowRecovery: false);
                clock.UtcNow = clock.UtcNow.AddSeconds(1); session.Advance(1);
                Assert.That(state.money >= 0, Is.True);
                if (firstSale < 0 && state.City.stats.coffeeSold > 0) firstSale = second + 1;
            }
            Assert.That(state.City.freeplay, Is.True, session.Progression.Current?.id);
            Assert.That(state.City.completedQuests.Count, Is.EqualTo(16));
            Assert.That(firstSale, Is.InRange(300, 420));
            Assert.That(state.simulationSeconds, Is.InRange(1800L, 3000L));
            Assert.That(state.nextRecoveryWork, Is.Zero);
            Assert.That(state.grinders, Is.EqualTo(51));
        }

        [Test] public void InvalidNewCurveAndTierConfigurationIsRejected()
        {
            var defs = ProgressionTests.Definitions(); defs.buildings[0].cost.earlyGrowth = double.NaN;
            Assert.Throws<InvalidOperationException>(() => defs.Validate());
            defs = ProgressionTests.Definitions(); defs.buildings[0].tierBuildMultipliers[1] = .5;
            Assert.Throws<InvalidOperationException>(() => defs.Validate());
            defs = ProgressionTests.Definitions(); defs.buildings[0].tierBuildMultipliers[6] = 5;
            Assert.Throws<InvalidOperationException>(() => defs.Validate());
        }
    }
}

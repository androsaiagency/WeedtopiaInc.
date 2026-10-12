using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Persistence;

namespace Weedtopia.Tests
{
    public sealed class MarkWoodCityCampaignTests
    {
        private const string City = "mark_wood_city";
        private static readonly string[] Sources = { "burning_hills", "big_rock_city", "seven_hills", "little_montana", "cassa_vadela" };
        private static readonly string[] People = { "mw_mara", "mw_jonas", "mw_leila", "mw_bram", "mw_tessa", "mw_silas", "mw_nora", "mw_owen" };
        private static readonly DateTime Start = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
        private GameDefinitions defs;
        private GameState state;
        private GameSession session;
        private ManualClock clock;

        [SetUp] public void Setup()
        {
            defs = ProgressionTests.Definitions(); clock = new ManualClock(Start);
            state = GameState.Create(defs, "Mark Wood Test", Start); state.introSeen = true;
            session = new GameSession(defs, state, clock); OwnSources(session);
            PopulateSources(0, 0, 0, 0, 0);
        }

        [Test] public void SixthCityUsesTheFirstFiveCitiesAndIndependentLocalContent()
        {
            var charter = defs.cities.Single(c => c.id == City);
            CollectionAssert.AreEqual(Sources, charter.unlockSources);
            Assert.That(charter.unlockResidents, Is.EqualTo(100000)); Assert.That(charter.introducedVersion, Is.EqualTo(7));
            Assert.That(charter.bridgeMoney, Is.EqualTo(7500)); Assert.That(charter.bridgeCards, Is.EqualTo(5));
            Assert.That(CityCampaignRules.DialoguePrefix(charter), Is.EqualTo("mw.dialog."));
            var buildings = defs.instances.Where(i => i.cityId == City).ToArray();
            Assert.That(buildings.Length, Is.EqualTo(56)); Assert.That(buildings.All(i => i.id.StartsWith("mw_", StringComparison.Ordinal)), Is.True);
            var ids = buildings.Select(i => i.id).ToHashSet();
            var routes = defs.routes.Where(route => ids.Contains(route.from)).ToArray();
            Assert.That(routes.Length, Is.EqualTo(35)); Assert.That(routes.All(route => ids.Contains(route.to)), Is.True);
            Assert.That(defs.QuestsFor(City).Length, Is.EqualTo(18));
            CollectionAssert.AreEquivalent(People, defs.vipStories.Where(p => p.cityId == City).Select(p => p.id));
            Assert.That(defs.vipStories.Where(p => p.cityId == City).Sum(p => p.chapters.Length), Is.EqualTo(32));
            Assert.That(defs.vipStories.Where(p => p.cityId == City).Sum(p => p.recurring.Length), Is.EqualTo(24));
            Assert.That(defs.vipCollaborations.Count(p => p.cityId == City), Is.EqualTo(3)); defs.Validate();
        }

        [TestCase(99999, false)] [TestCase(100000, true)] [TestCase(100001, true)]
        public void CombinedPopulationBoundaryIsExact(int total, bool offered)
        {
            PopulateSources(20000, 20000, 20000, 20000, total - 80000); session.Campaign.RefreshMilestones();
            Assert.That(session.Campaign.UnlockProgress(City), Is.EqualTo(total));
            Assert.That(session.Campaign.Offered(City), Is.EqualTo(offered));
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(offered ? Failure.None : Failure.NotReady));
            Assert.That(session.Campaign.Travel(City), Is.EqualTo(offered ? Failure.None : Failure.Locked));
            SaveValidator.Validate(state, defs);
        }

        [TestCase("big_rock_city")] [TestCase("seven_hills")] [TestCase("little_montana")] [TestCase("cassa_vadela")]
        public void UnacceptedCitiesDoNotContributeTheirPopulation(string source)
        {
            state = GameState.Create(defs, "Unbestätigter Wald", Start); state.introSeen = true;
            session = new GameSession(defs, state, clock); Populate(session, source, 20000);
            Assert.That(session.Simulation.Residents(source), Is.EqualTo(20000));
            Assert.That(session.Campaign.UnlockProgress(City), Is.Zero); Assert.That(session.Campaign.Offered(City), Is.False);
            SaveValidator.Validate(state, defs);
        }

        [Test] public void SeparateHistoricalHighsAndDestinationPopulationCannotUnlockTheCity()
        {
            // Source-charter setup legitimately records 50,000 simultaneous residents.
            // Isolate this test's later, non-overlapping 20,000-resident peaks.
            state.cities.Single(c => c.id == City).charterResidentMilestone = 0;
            foreach (string current in Sources)
            {
                foreach (string source in Sources) Populate(session, source, source == current ? 20000 : 0);
                session.Campaign.RefreshMilestones();
            }
            Populate(session, City, 20000);
            Assert.That(state.cities.Where(c => Sources.Contains(c.id)).Sum(c => c.storyResidentMilestone), Is.EqualTo(100000));
            Assert.That(session.Campaign.UnlockProgress(City), Is.EqualTo(20000)); Assert.That(session.Campaign.Offered(City), Is.False);
            Assert.That(state.cities.Single(c => c.id == City).charterResidentMilestone, Is.EqualTo(20000));
            SaveValidator.Validate(state, defs);
        }

        [TestCase("burning_hills")] [TestCase("big_rock_city")] [TestCase("seven_hills")] [TestCase("little_montana")] [TestCase("cassa_vadela")]
        public void AcceptanceFromAnySourceGrantsBridgeOnceAndPreservesMainHistories(string current)
        {
            PopulateSources(20000, 20000, 20000, 20000, 20000); state.activeCity = current;
            var histories = state.cities.Where(c => Sources.Contains(c.id)).Select(c => string.Join(",", c.completedQuests)).ToArray();
            Amount money = state.money; int cards = state.cards;
            Assert.That(session.TransactCampaign(() => session.Campaign.Accept(City)), Is.EqualTo(Failure.None));
            Assert.That(state.money, Is.EqualTo(money + 7500)); Assert.That(state.cards, Is.EqualTo(cards + 5));
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.NotReady));
            Assert.That(state.money, Is.EqualTo(money + 7500)); Assert.That(state.cards, Is.EqualTo(cards + 5));
            CollectionAssert.AreEqual(histories, state.cities.Where(c => Sources.Contains(c.id)).Select(c => string.Join(",", c.completedQuests)));
            Assert.That(state.rewardHistory.Count(reward => reward.source == RewardSource.CityCharter && reward.sourceId == City), Is.EqualTo(1));
            SaveValidator.Validate(state, defs);
        }

        [Test] public void OfferedCharterSurvivesPopulationDeclineSaveAndContinue()
        {
            PopulateSources(20000, 20000, 20000, 20000, 20000); session.Campaign.RefreshMilestones(); PopulateSources(0, 0, 0, 0, 0);
            var restored = Roundtrip(state); var resumed = new GameSession(defs, restored, clock);
            Assert.That(resumed.Campaign.UnlockProgress(City), Is.Zero); Assert.That(resumed.Campaign.Offered(City), Is.True);
            Assert.That(resumed.Campaign.Accept(City), Is.EqualTo(Failure.None)); Assert.That(resumed.Campaign.Travel(City), Is.EqualTo(Failure.None));
            Assert.That(Roundtrip(restored).activeCity, Is.EqualTo(City));
        }

        [Test] public void VersionSixMigrationAddsOnlyLockedCityAndKeepsWorkersCargoInventoryAndStories()
        {
            var previous = VersionSix(); var old = GameState.Create(previous, "Waldmigration", Start); old.introSeen = true;
            var game = new GameSession(previous, old, clock); OwnSources(game); old.activeCity = "cassa_vadela";
            foreach (string city in Sources) Populate(game, city, 0);
            old.City.storyResidentMilestone = 100; game.Stories.RefreshUnlocks();
            var first = game.Stories.People.First(p => game.Stories.Offer(p.id) != null); var offer = game.Stories.Offer(first.id);
            Assert.That(game.Stories.Accept(first.id, offer.id, offer.choices[0].id), Is.EqualTo(Failure.None));
            old.Find("farm_warehouse").level = 1; old.Find("city_warehouse").level = 1;
            old.workers.Add(new WorkerState { id = "markwood_migration_driver", role = "farm_driver", buildingId = "farm_warehouse", slot = 1,
                attributes = new List<WorkerAttribute> { previous.Role("farm_driver").attributes[0] } });
            old.deliveries.Add(new DeliveryState { route = "farm_city", workerId = "markwood_migration_driver", cargo = new Amount(7), remainingSeconds = 9, totalSeconds = 15 });
            old.inventory.Add(new ItemStack { id = "build_1m", count = 3 }); SaveValidator.Validate(old, previous);
            string[] cities = old.cities.Select(JsonUtility.ToJson).ToArray(), buildings = old.buildings.Select(JsonUtility.ToJson).ToArray();
            string[] workers = old.workers.Select(JsonUtility.ToJson).ToArray(), deliveries = old.deliveries.Select(JsonUtility.ToJson).ToArray();
            string inventory = JsonUtility.ToJson(new InventorySnapshot { items = old.inventory.ToArray() }); Amount money = old.money;
            var loaded = Roundtrip(old); var resumed = new GameSession(defs, loaded, clock);
            Assert.That(loaded.contentVersion, Is.EqualTo(7)); Assert.That(loaded.buildings.Count, Is.EqualTo(previous.instances.Length + 56));
            Assert.That(loaded.activeCity, Is.EqualTo("cassa_vadela")); Assert.That(loaded.money, Is.EqualTo(money));
            CollectionAssert.AreEqual(cities, loaded.cities.Take(5).Select(JsonUtility.ToJson));
            CollectionAssert.AreEqual(buildings, loaded.buildings.Take(previous.instances.Length).Select(JsonUtility.ToJson));
            CollectionAssert.AreEqual(workers, loaded.workers.Select(JsonUtility.ToJson)); CollectionAssert.AreEqual(deliveries, loaded.deliveries.Select(JsonUtility.ToJson));
            Assert.That(JsonUtility.ToJson(new InventorySnapshot { items = loaded.inventory.ToArray() }), Is.EqualTo(inventory));
            Assert.That(resumed.Campaign.Accessible(City), Is.False); Assert.That(resumed.Campaign.Offered(City), Is.False);
            Assert.That(loaded.buildings.Where(b => b.cityId == City).All(b => b.level == (b.family == "town_hall" ? 1 : 0)), Is.True);
        }

        [Test] public void MigrationRejectsMissingHistoricalBuildingsAndForgedCharters()
        {
            var old = GameState.Create(VersionSix(), "Beschädigter Wald", Start); old.buildings.RemoveAt(0);
            Assert.Throws<InvalidDataException>(() => Roundtrip(old));
            state.cities.Single(c => c.id == City).charterAccepted = true;
            Assert.Throws<InvalidDataException>(() => SaveValidator.Validate(state, defs));
        }

        [Test] public void SixOwnedCitiesContinueOfflineWithoutMovingTheirAssignedWorkers()
        {
            OwnAndTravel(); foreach (var city in state.cities) city.unlocked = Zone.Industry;
            foreach (var city in state.cities)
            {
                var grow = state.buildings.First(b => b.cityId == city.id && b.family == "cultivation_plot"); grow.level = 25;
                state.workers.Add(new WorkerState { id = "markwood_grower_" + city.id, role = "grow", buildingId = grow.id, slot = 0,
                    attributes = new List<WorkerAttribute> { defs.Role("grow").attributes[0] } });
            }
            var assignments = state.workers.ToDictionary(w => w.id, w => (w.buildingId, w.slot));
            session.Save(); clock.UtcNow = clock.UtcNow.AddSeconds(60); var offline = session.ApplyOffline();
            Assert.That(offline.Seconds, Is.EqualTo(60)); Assert.That(state.cities.All(c => c.stats.rawProduced > 0), Is.True);
            foreach (var worker in state.workers) Assert.That((worker.buildingId, worker.slot), Is.EqualTo(assignments[worker.id]));
            string snapshot = JsonUtility.ToJson(state); Assert.That(session.ApplyOffline().Seconds, Is.Zero);
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(snapshot)); SaveValidator.Validate(state, defs);
        }

        [TestCase(0)] [TestCase(1)] public void StoriesRecurringWorkAndCommunityProjectsPersistEveryChoice(int branch)
        {
            OwnAndTravel(); state.City.unlocked = Zone.Industry; state.City.storyResidentMilestone = 20000; session.Stories.RefreshUnlocks();
            foreach (var person in session.Stories.People.ToArray())
                for (int chapter = 0; chapter < person.chapters.Length; chapter++) CompleteStory(person.id, branch);
            foreach (var joint in session.Stories.Collaborations.ToArray()) CompleteStory(joint.id, branch);
            Assert.That(state.City.vipStories.Sum(person => person.completed.Count), Is.EqualTo(32));
            Assert.That(state.City.vipCollaborations.Count(project => project.completed != null), Is.EqualTo(3));
            foreach (var person in session.Stories.People.ToArray())
                for (int repeat = 0; repeat < person.recurring.Length; repeat++)
                {
                    state.simulationSeconds = Math.Max(state.simulationSeconds, session.Stories.Person(person.id).nextRecurringSecond);
                    Assert.That(session.Stories.Offer(person.id), Is.SameAs(person.recurring[repeat])); CompleteStory(person.id, branch);
                    Assert.That(session.Stories.Offer(person.id), Is.Null);
                }
            Assert.That(state.City.vipStories.Sum(person => person.recurringCompleted), Is.EqualTo(24));
            Assert.That(JsonUtility.ToJson(Roundtrip(state).City), Is.EqualTo(JsonUtility.ToJson(state.City)));
        }

        [Test] public void LocalFoundationCompletesTwelveMainQuestsWithTheBridgeAndNoPreviousCityIncome()
        {
            PopulateSources(20000, 20000, 20000, 20000, 20000); state.money = 0; state.cards = 0;
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.None)); Assert.That(session.Campaign.Travel(City), Is.EqualTo(Failure.None));
            session.Stories.RefreshUnlocks(); Assert.That(state.money, Is.EqualTo(new Amount(7500))); Assert.That(state.cards, Is.EqualTo(5));
            int grinders = state.grinders, awardedGrinders = 0; long recovery = state.nextRecoveryWork, rewardSequence = state.rewardSequence;
            var earnings = state.cities.Where(c => Sources.Contains(c.id)).ToDictionary(c => c.id, c => c.stats.earned);
            var assignments = state.workers.ToDictionary(w => w.id, w => (w.buildingId, w.slot));
            var histories = state.cities.Where(c => Sources.Contains(c.id)).Select(c => string.Join(",", c.completedQuests)).ToArray();
            for (int tick = 0; tick < TutorialDriver.MaxSimulationSeconds && state.City.questIndex < 12; tick++)
            {
                int before = state.grinders;
                TutorialDriver.Act(session, clock.UtcNow, allowRecovery: false); clock.UtcNow = clock.UtcNow.AddSeconds(1); session.Advance(1);
                int awards = state.rewardHistory.Where(reward => reward.sequence > rewardSequence).SelectMany(reward => reward.items)
                    .Where(item => item.kind == RewardKind.Grinders).Sum(item => checked((int)Math.Round(item.quantity.ToDouble())));
                awardedGrinders += awards; rewardSequence = state.rewardSequence;
                Assert.That(state.grinders, Is.EqualTo(before + awards), "Premium currency spent at " + session.Progression.Current?.id);
            }
            Assert.That(state.City.questIndex, Is.GreaterThanOrEqualTo(12), "Foundation stalled at " + session.Progression.Current?.id);
            Assert.That(state.City.stats.butterProduced > 0 && state.City.stats.pastrySold > 0, Is.True);
            Assert.That(state.grinders, Is.EqualTo(grinders + awardedGrinders)); Assert.That(state.nextRecoveryWork, Is.EqualTo(recovery));
            foreach (var city in state.cities.Where(c => Sources.Contains(c.id))) Assert.That(city.stats.earned, Is.EqualTo(earnings[city.id]));
            CollectionAssert.AreEqual(histories, state.cities.Where(c => Sources.Contains(c.id)).Select(c => string.Join(",", c.completedQuests)));
            foreach (var assignment in assignments)
            {
                var worker = state.workers.Single(w => w.id == assignment.Key);
                Assert.That((worker.buildingId, worker.slot), Is.EqualTo(assignment.Value));
            }
            Assert.That(Roundtrip(state).City.questIndex, Is.EqualTo(state.City.questIndex));
        }

        private void CompleteStory(string id, int branch)
        {
            var offer = session.Stories.Offer(id); Assert.That(offer, Is.Not.Null, id);
            Assert.That(session.Stories.Accept(id, offer.id, offer.choices[branch % offer.choices.Length].id), Is.EqualTo(Failure.None));
            var active = session.Stories.Active(id); Assert.That(session.Stories.Ready(id), Is.False, "Every chapter needs fresh activity: " + offer.id);
            for (int index = 0; index < offer.goals.Length; index++)
            {
                var goal = offer.goals[index]; int count = active.targets[index];
                switch (goal.kind)
                {
                    case StoryGoalKind.Level:
                        var building = state.buildings.FirstOrDefault(b => b.cityId == City && (b.id == goal.target || b.family == goal.target));
                        if (building == null) { building = state.buildings.First(b => b.cityId == City && b.family == "settlement_plot"); building.family = goal.target; }
                        building.level = Math.Max(building.level, count); break;
                    case StoryGoalKind.Residents: Populate(session, City, count); break;
                    case StoryGoalKind.NpcVisited:
                        for (int visit = 0; visit < count; visit++) Assert.That(session.Stories.Visit(goal.target), Is.EqualTo(Failure.None)); break;
                    case StoryGoalKind.RawProduced: state.City.stats.rawProduced += count; break;
                    case StoryGoalKind.RefinedProduced: state.City.stats.refinedProduced += count; break;
                    case StoryGoalKind.CoffeeSold: state.City.stats.coffeeSold += count; break;
                    case StoryGoalKind.ButterProduced: state.City.stats.butterProduced += count; break;
                    case StoryGoalKind.PastrySold: state.City.stats.pastrySold += count; break;
                    case StoryGoalKind.DeliveredGoods: state.City.stats.delivered += count; break;
                    case StoryGoalKind.Deliveries: state.City.stats.deliveries += count; break;
                    default: Assert.Fail("Unsupported story fixture goal " + goal.kind); break;
                }
            }
            Assert.That(session.Stories.Claim(id, offer.id), Is.EqualTo(Failure.None), offer.id); SaveValidator.Validate(state, defs);
        }

        private void OwnAndTravel()
        {
            PopulateSources(20000, 20000, 20000, 20000, 20000); Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.None));
            Assert.That(session.Campaign.Travel(City), Is.EqualTo(Failure.None)); session.Stories.RefreshUnlocks();
        }
        private static void OwnSources(GameSession game)
        {
            game.State.City.storyResidentMilestone = 1000; Assert.That(game.Campaign.Accept("big_rock_city"), Is.EqualTo(Failure.None));
            Populate(game, "burning_hills", 3000); Populate(game, "big_rock_city", 2000);
            Assert.That(game.Campaign.Accept("seven_hills"), Is.EqualTo(Failure.None));
            foreach (string city in Sources.Take(3)) Populate(game, city, 5000);
            Assert.That(game.Campaign.Accept("little_montana"), Is.EqualTo(Failure.None));
            foreach (string city in Sources.Take(4)) Populate(game, city, 12500);
            Assert.That(game.Campaign.Accept("cassa_vadela"), Is.EqualTo(Failure.None));
        }
        private void PopulateSources(params int[] residents)
        { for (int index = 0; index < Sources.Length; index++) Populate(session, Sources[index], residents[index]); }
        private static void Populate(GameSession game, string city, int residents)
        {
            var plots = game.State.buildings.Where(b => b.cityId == city && (b.family == "settlement_plot" || b.family == "residential_house")).ToArray();
            foreach (var plot in plots) { plot.level = 0; game.State.workers.RemoveAll(w => w.buildingId == plot.id); }
            if (residents == 0) return;
            var choices = new Dictionary<int, (int level, int workerLevel, int rank)>();
            for (int tier = 1; tier < VisualTiers.Starts.Length; tier++)
                for (int level = 1; level <= 100; level++) for (int rank = 1; rank <= 10; rank++)
                {
                    int count = (int)Math.Floor(game.Definitions.Building("residential_house").baseResidents * (1 + tier) *
                        (1 + game.Definitions.workerAttributeStep * level * rank));
                    if (!choices.ContainsKey(count)) choices[count] = (VisualTiers.Starts[tier], level, rank);
                }
            var parts = new List<int>(); int remaining = residents;
            while (remaining >= 5000) { parts.Add(5000); remaining -= 5000; }
            if (remaining > 0 && remaining < choices.Keys.Min() && parts.Count > 0)
            { parts.RemoveAt(parts.Count - 1); remaining += 5000; }
            if (remaining > 0)
            {
                if (choices.ContainsKey(remaining)) parts.Add(remaining);
                else { int first = choices.Keys.First(n => n < remaining && choices.ContainsKey(remaining - n)); parts.Add(first); parts.Add(remaining - first); }
            }
            Assert.That(parts.Count, Is.LessThanOrEqualTo(plots.Length));
            for (int index = 0; index < parts.Count; index++)
            {
                var configuration = choices[parts[index]]; var building = plots[index]; building.family = "residential_house"; building.level = configuration.level;
                game.State.workers.Add(new WorkerState { id = "markwood_fixture_" + building.id, role = "caretaker", level = configuration.workerLevel,
                    rank = configuration.rank, buildingId = building.id, slot = 0, attributes = new List<WorkerAttribute> { WorkerAttribute.Residents } });
            }
            Assert.That(game.Simulation.Residents(city), Is.EqualTo(residents));
        }
        private GameDefinitions VersionSix()
        {
            var previous = JsonUtility.FromJson<GameDefinitions>(JsonUtility.ToJson(defs)); previous.version = 6;
            previous.cities = previous.cities.Where(c => c.introducedVersion <= 6).ToArray();
            previous.instances = previous.instances.Where(i => i.introducedVersion <= 6).ToArray();
            var ids = previous.instances.Select(i => i.id).ToHashSet();
            previous.routes = previous.routes.Where(r => ids.Contains(r.from) && ids.Contains(r.to)).ToArray();
            previous.quests = previous.quests.Where(q => q.cityId != City).ToArray();
            previous.vipStories = previous.vipStories.Where(p => p.cityId != City).ToArray();
            previous.vipCollaborations = previous.vipCollaborations.Where(p => p.cityId != City).ToArray(); previous.Validate(); return previous;
        }
        private GameState Roundtrip(GameState source)
        {
            string payload = JsonUtility.ToJson(source);
            return new LocalSaveStore(Path.Combine("Temp", "MarkWoodCityUnused.json"), defs).Decode(
                JsonUtility.ToJson(new SaveEnvelope { payload = payload, sha256 = LocalSaveStore.Hash(payload) }));
        }
        [Serializable] private sealed class InventorySnapshot { public ItemStack[] items; }
    }
}

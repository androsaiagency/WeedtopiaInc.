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
    public sealed class SkunkfurtCampaignTests
    {
        private const string City = "skunkfurt";
        private static readonly string[] Sources = { "burning_hills", "big_rock_city", "seven_hills", "little_montana", "cassa_vadela", "mark_wood_city", "green_leaf_village", "purple_garden", "golden_flower", "saint_mary_ville", "great_kushville" };
        private static readonly string[] People = { "sf_alma", "sf_cem", "sf_rika", "sf_jonas", "sf_lotte", "sf_mina" };
        private static readonly DateTime Start = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
        private GameDefinitions defs;
        private GameState state;
        private GameSession session;
        private ManualClock clock;

        [SetUp] public void Setup()
        {
            defs = VersionThirteenFixture(ProgressionTests.Definitions()); clock = new ManualClock(Start);
            state = GameState.Create(defs, "Skunkfurt Test", Start); state.introSeen = true;
            session = new GameSession(defs, state, clock); OwnSources(session); PopulateSources(new int[Sources.Length]);
            state.cities.Single(city => city.id == City).charterResidentMilestone = 0;
        }

        [Test] public void TwelfthCityHasItsOwnRoutesSixPermanentResidentsAndThreeCommunityProjects()
        {
            var charter = defs.cities.Single(c => c.id == City);
            CollectionAssert.AreEqual(Sources, charter.unlockSources);
            Assert.That(charter.unlockResidents, Is.EqualTo(10000000)); Assert.That(charter.introducedVersion, Is.EqualTo(13));
            Assert.That(charter.bridgeMoney, Is.EqualTo(15000)); Assert.That(charter.bridgeCards, Is.EqualTo(8));
            Assert.That(CityCampaignRules.DialoguePrefix(charter), Is.EqualTo("sf.dialog."));
            var buildings = defs.instances.Where(i => i.cityId == City).ToArray();
            Assert.That(buildings.Length, Is.EqualTo(56)); Assert.That(buildings.All(i => i.id.StartsWith("sf_", StringComparison.Ordinal)), Is.True);
            var ids = buildings.Select(i => i.id).ToHashSet(); var routes = defs.routes.Where(r => ids.Contains(r.from)).ToArray();
            Assert.That(routes.Length, Is.EqualTo(35)); Assert.That(routes.All(r => ids.Contains(r.to)), Is.True);
            Assert.That(defs.QuestsFor(City).Length, Is.EqualTo(18)); Assert.That(defs.QuestsFor(City).First().id, Is.EqualTo("sf_arrival"));
            CollectionAssert.AreEquivalent(People, defs.vipStories.Where(p => p.cityId == City).Select(p => p.id));
            Assert.That(defs.vipStories.Where(p => p.cityId == City).Sum(p => p.chapters.Length), Is.EqualTo(18));
            Assert.That(defs.vipStories.Where(p => p.cityId == City).Sum(p => p.recurring.Length), Is.EqualTo(18));
            Assert.That(defs.vipCollaborations.Count(p => p.cityId == City), Is.EqualTo(3)); defs.Validate();
        }

        [TestCase(9999999, false)] [TestCase(10000000, true)] [TestCase(10000001, true)]
        public void TenMillionBoundaryUsesActualSimultaneousPopulation(int total, bool offered)
        {
            PopulateThreshold(total);
            session.Campaign.RefreshMilestones();
            Assert.That(session.Campaign.UnlockProgress(City), Is.EqualTo(total)); Assert.That(session.Campaign.Offered(City), Is.EqualTo(offered));
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(offered ? Failure.None : Failure.NotReady));
            Assert.That(session.Campaign.Travel(City), Is.EqualTo(offered ? Failure.None : Failure.Locked)); SaveValidator.Validate(state, defs);
        }

        [Test] public void SixOriginalBiographiesAndEighteenChaptersConnectPopulationProductionAndPersonalProgress()
        {
            var people = defs.vipStories.Where(person => person.cityId == City).ToArray();
            Assert.That(people.Select(person => defs.Text(person.biographyKey)).Distinct().Count(), Is.EqualTo(6));
            foreach (var person in people)
            {
                Assert.That(person.chapters.Length, Is.EqualTo(3), person.id);
                Assert.That(person.recurring.Length, Is.EqualTo(3), person.id);
                Assert.That(person.residents, Is.GreaterThan(0), person.id);
                foreach (string key in new[] { person.nameKey, person.roleKey, person.biographyKey, person.greetingKey })
                    Assert.That(defs.Text(key), Does.Not.StartWith("["), key);
                foreach (var chapter in person.chapters)
                {
                    Assert.That(chapter.id, Does.StartWith("sf_"), chapter.id);
                    Assert.That(chapter.residents, Is.GreaterThan(0), chapter.id);
                    foreach (string key in new[] { chapter.titleKey, chapter.bodyKey, chapter.completedKey, chapter.memoryKey })
                        Assert.That(defs.Text(key), Does.Not.StartWith("["), key);
                    Assert.That(chapter.goals.Any(goal => goal.IsCounter), Is.True, "Each personal chapter needs new activity: " + chapter.id);
                }
            }
            var goals = people.SelectMany(person => person.chapters).SelectMany(chapter => chapter.goals).ToArray();
            Assert.That(goals.Any(goal => goal.kind == StoryGoalKind.Level), Is.True);
            Assert.That(goals.Any(goal => goal.kind == StoryGoalKind.RawProduced || goal.kind == StoryGoalKind.RefinedProduced), Is.True);
            Assert.That(goals.Any(goal => goal.kind == StoryGoalKind.DeliveredGoods || goal.kind == StoryGoalKind.Deliveries), Is.True);
            Assert.That(people.SelectMany(person => person.chapters).Any(chapter => chapter.choices.Length > 1), Is.True);
            var conditions = defs.QuestsFor(City).SelectMany(quest => quest.conditions).ToArray();
            Assert.That(conditions.Any(condition => condition.kind == ConditionKind.Residents), Is.True, "The main campaign must develop occupied housing.");
            Assert.That(conditions.Any(condition => condition.kind == ConditionKind.Talk), Is.True, "The main campaign retains personal conversations.");
        }

        [TestCase("burning_hills")] [TestCase("big_rock_city")] [TestCase("seven_hills")] [TestCase("little_montana")]
        [TestCase("cassa_vadela")] [TestCase("mark_wood_city")] [TestCase("green_leaf_village")] [TestCase("purple_garden")] [TestCase("golden_flower")] [TestCase("saint_mary_ville")] [TestCase("great_kushville")]
        public void AcceptanceFromEverySourceCityGrantsTheBridgeExactlyOnce(string activeCity)
        {
            PopulateThreshold(); state.activeCity = activeCity;
            string[] history = state.cities.Where(c => Sources.Contains(c.id)).Select(c => string.Join(",", c.completedQuests)).ToArray();
            Amount money = state.money; int cards = state.cards; var charter = defs.cities.Single(c => c.id == City);
            Assert.That(session.TransactCampaign(() => session.Campaign.Accept(City)), Is.EqualTo(Failure.None));
            Assert.That(state.money, Is.EqualTo(money + charter.bridgeMoney)); Assert.That(state.cards, Is.EqualTo(cards + charter.bridgeCards));
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.NotReady));
            Assert.That(state.money, Is.EqualTo(money + charter.bridgeMoney)); Assert.That(state.cards, Is.EqualTo(cards + charter.bridgeCards));
            CollectionAssert.AreEqual(history, state.cities.Where(c => Sources.Contains(c.id)).Select(c => string.Join(",", c.completedQuests)));
            Assert.That(state.rewardHistory.Count(r => r.source == RewardSource.CityCharter && r.sourceId == City), Is.EqualTo(1)); SaveValidator.Validate(state, defs);
            var restored = Roundtrip(state); var resumed = new GameSession(defs, restored, clock);
            Assert.That(resumed.Campaign.Accept(City), Is.EqualTo(Failure.NotReady));
            Assert.That(restored.money, Is.EqualTo(money + charter.bridgeMoney)); Assert.That(restored.cards, Is.EqualTo(cards + charter.bridgeCards));
            Assert.That(restored.rewardHistory.Count(r => r.source == RewardSource.CityCharter && r.sourceId == City), Is.EqualTo(1));
        }

        [TestCase("big_rock_city")] [TestCase("seven_hills")] [TestCase("little_montana")]
        [TestCase("cassa_vadela")] [TestCase("mark_wood_city")] [TestCase("green_leaf_village")] [TestCase("purple_garden")] [TestCase("golden_flower")] [TestCase("saint_mary_ville")] [TestCase("great_kushville")]
        public void UnacceptedCitiesCannotContributePopulation(string source)
        {
            state = GameState.Create(defs, "Unbestätigte Stadt", Start); state.introSeen = true;
            session = new GameSession(defs, state, clock); Populate(session, source, 20000);
            Assert.That(session.Simulation.Residents(source), Is.EqualTo(20000));
            Assert.That(session.Campaign.UnlockProgress(City), Is.Zero); Assert.That(session.Campaign.Offered(City), Is.False); SaveValidator.Validate(state, defs);
        }

        [Test] public void SeparateHistoricalHighsAndDestinationResidentsNeverCountTowardsTenMillion()
        {
            state.cities.Single(c => c.id == City).charterResidentMilestone = 0;
            foreach (var source in state.cities.Where(c => Sources.Contains(c.id))) source.storyResidentMilestone = 0;
            int maximum = MaximumCityPopulation(defs);
            foreach (string current in Sources)
            {
                foreach (string source in Sources) Populate(session, source, source == current ? maximum : 0);
                session.Campaign.RefreshMilestones();
            }
            Populate(session, City, 30000);
            Assert.That(state.cities.Where(c => Sources.Contains(c.id)).Sum(c => c.storyResidentMilestone), Is.GreaterThanOrEqualTo(10000000));
            Assert.That(session.Campaign.UnlockProgress(City), Is.EqualTo(maximum)); Assert.That(session.Campaign.Offered(City), Is.False);
            Assert.That(state.cities.Single(c => c.id == City).charterResidentMilestone, Is.EqualTo(maximum)); SaveValidator.Validate(state, defs);
        }

        [Test] public void PlannedEmptyHousingAndStoredCityHighsCannotCreateAnInvitation()
        {
            foreach (string source in Sources)
            {
                state.cities.Single(city => city.id == source).storyResidentMilestone = MaximumCityPopulation(defs);
                foreach (var plot in state.buildings.Where(building => building.cityId == source && building.family == "settlement_plot").ToArray())
                    SelectPlot(session, source, plot, "residential_house");
                Assert.That(session.Simulation.Residents(source), Is.Zero, source);
            }
            Assert.That(Sources.Length * MaximumCityPopulation(defs), Is.GreaterThan(10000000));
            session.Campaign.RefreshMilestones();
            Assert.That(session.Campaign.UnlockProgress(City), Is.Zero); Assert.That(session.Campaign.Offered(City), Is.False);
            Assert.That(state.cities.Single(city => city.id == City).charterResidentMilestone, Is.Zero); SaveValidator.Validate(state, defs);
        }

        [Test] public void AllElevenPreviousCitiesMustContributeRealPopulationAtTheBoundary()
        {
            PopulateThreshold();
            Assert.That(Sources.All(source => session.Simulation.Residents(source) > 0), Is.True);
            Assert.That(MaximumCityPopulation(defs) * (Sources.Length - 1), Is.LessThan(10000000));
            Populate(session, "great_kushville", 0);
            Assert.That(session.Campaign.UnlockProgress(City), Is.LessThan(10000000)); Assert.That(session.Campaign.Offered(City), Is.False);
            PopulateThreshold(); Assert.That(session.Campaign.UnlockProgress(City), Is.EqualTo(10000000)); Assert.That(session.Campaign.Offered(City), Is.True);
        }
        [Test] public void ReachedTenMillionIsLatchedAcrossDeclineSaveAndContinue()
        {
            PopulateThreshold(); session.Campaign.RefreshMilestones();
            PopulateSources(new int[Sources.Length]); var restored = Roundtrip(state); var resumed = new GameSession(defs, restored, clock);
            Assert.That(resumed.Campaign.UnlockProgress(City), Is.Zero); Assert.That(resumed.Campaign.Offered(City), Is.True);
            Assert.That(resumed.Campaign.Accept(City), Is.EqualTo(Failure.None)); Assert.That(resumed.Campaign.Travel(City), Is.EqualTo(Failure.None));
            Assert.That(Roundtrip(restored).activeCity, Is.EqualTo(City));
        }

        [Test] public void FailedSaveRollsBackCharterRewardPopulationLatchAndTravel()
        {
            PopulateThreshold();
            var failing = new GameSession(defs, state, clock, new FailingStore()); state.cities.Single(c => c.id == City).charterResidentMilestone = 0;
            string before = JsonUtility.ToJson(state); Assert.Throws<IOException>(() => failing.TransactCampaign(() => failing.Campaign.Accept(City)));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before)); Assert.That(failing.Campaign.Accessible(City), Is.False);
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.None)); before = JsonUtility.ToJson(state);
            Assert.Throws<IOException>(() => failing.TransactCampaign(() => failing.Campaign.Travel(City))); Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
        }

        [Test] public void PopulationUnlockedResidentsRemainAfterDeclineAndContinue()
        {
            OwnAndTravel(); Assert.That(session.Stories.People.All(p => !session.Stories.Unlocked(p.id)), Is.True);
            var first = defs.vipStories.Where(p => p.cityId == City).OrderBy(p => p.residents).First();
            Populate(session, City, first.residents); session.Stories.RefreshUnlocks(); Assert.That(session.Stories.Unlocked(first.id), Is.True);
            PopulateSources(new int[Sources.Length]); Populate(session, City, 0); session.Stories.RefreshUnlocks();
            var restored = Roundtrip(state); var resumed = new GameSession(defs, restored, clock);
            Assert.That(resumed.Simulation.Residents(City), Is.Zero); Assert.That(resumed.Stories.Unlocked(first.id), Is.True);
            Assert.That(resumed.Campaign.UnlockProgress(City), Is.Zero); Assert.That(resumed.Campaign.Accessible(City), Is.True);
        }

        [Test] public void VersionTwelveMigrationPreservesEveryPreviousCityWorkerCargoStoryAndInventoryAndIsIdempotent()
        {
            var previous = VersionTwelve(); var old = GameState.Create(previous, "Skunkfurtmigration", Start); old.introSeen = true;
            var game = new GameSession(previous, old, clock); OwnSources(game); old.activeCity = "saint_mary_ville";
            foreach (string city in Sources) Populate(game, city, 0);
            old.City.storyResidentMilestone = 700; game.Stories.RefreshUnlocks(); var person = game.Stories.People.First(p => game.Stories.Offer(p.id) != null);
            var offer = game.Stories.Offer(person.id); Assert.That(game.Stories.Accept(person.id, offer.id, offer.choices[0].id), Is.EqualTo(Failure.None));
            old.Find("farm_warehouse").level = 1; old.Find("city_warehouse").level = 1;
            old.workers.Add(new WorkerState { id = "skunkfurt_migration_driver", role = "farm_driver", buildingId = "farm_warehouse", slot = 1,
                attributes = new List<WorkerAttribute> { previous.Role("farm_driver").attributes[0] } });
            old.deliveries.Add(new DeliveryState { route = "farm_city", workerId = "skunkfurt_migration_driver", cargo = new Amount(7), remainingSeconds = 9, totalSeconds = 15 });
            old.inventory.Add(new ItemStack { id = "build_1m", count = 3 }); SaveValidator.Validate(old, previous);
            string[] cities = old.cities.Select(JsonUtility.ToJson).ToArray(), buildings = old.buildings.Select(JsonUtility.ToJson).ToArray();
            string[] workers = old.workers.Select(JsonUtility.ToJson).ToArray(), deliveries = old.deliveries.Select(JsonUtility.ToJson).ToArray();
            string inventory = JsonUtility.ToJson(new InventorySnapshot { items = old.inventory.ToArray() }); Amount money = old.money;
            int cards = old.cards; string[] rewards = old.rewardHistory.Select(JsonUtility.ToJson).ToArray();
            var loaded = Roundtrip(old); var resumed = new GameSession(defs, loaded, clock);
            Assert.That(loaded.contentVersion, Is.EqualTo(13)); Assert.That(loaded.buildings.Count, Is.EqualTo(defs.instances.Length)); Assert.That(loaded.cities.Count, Is.EqualTo(12));
            Assert.That(loaded.activeCity, Is.EqualTo("saint_mary_ville")); Assert.That(loaded.money, Is.EqualTo(money));
            Assert.That(loaded.cards, Is.EqualTo(cards)); CollectionAssert.AreEqual(rewards, loaded.rewardHistory.Select(JsonUtility.ToJson));
            Assert.That(loaded.rewardHistory.Any(reward => reward.source == RewardSource.CityCharter && reward.sourceId == City), Is.False);
            CollectionAssert.AreEqual(cities, loaded.cities.Take(11).Select(JsonUtility.ToJson)); CollectionAssert.AreEqual(buildings, loaded.buildings.Take(previous.instances.Length).Select(JsonUtility.ToJson));
            CollectionAssert.AreEqual(workers, loaded.workers.Select(JsonUtility.ToJson)); CollectionAssert.AreEqual(deliveries, loaded.deliveries.Select(JsonUtility.ToJson));
            Assert.That(JsonUtility.ToJson(new InventorySnapshot { items = loaded.inventory.ToArray() }), Is.EqualTo(inventory));
            Assert.That(resumed.Campaign.Accessible(City), Is.False); Assert.That(resumed.Campaign.Offered(City), Is.False);
            Assert.That(loaded.buildings.Where(b => b.cityId == City).All(b => b.level == (b.family == "town_hall" ? 1 : 0)), Is.True);
            Assert.That(JsonUtility.ToJson(Roundtrip(loaded)), Is.EqualTo(JsonUtility.ToJson(loaded)));
        }

        [Test] public void FullyDevelopedVersionTwelveHousingReachesTenMillionAfterMigration()
        {
            var previous = VersionTwelve(); var old = GameState.Create(previous, "Skunkfurt Altstand", Start); old.introSeen = true;
            var game = new GameSession(previous, old, clock); OwnSources(game);
            foreach (string source in Sources) Populate(game, source, MaximumCityPopulation(previous));
            Assert.That(Sources.Sum(game.Simulation.Residents), Is.EqualTo(5722200));
            Amount money = old.money; int cards = old.cards;
            var restored = Roundtrip(old); var resumed = new GameSession(defs, restored, clock);
            Assert.That(resumed.Campaign.UnlockProgress(City), Is.EqualTo(Sources.Length * MaximumCityPopulation(defs))); Assert.That(resumed.Campaign.Offered(City), Is.True);
            Assert.That(restored.money, Is.EqualTo(money)); Assert.That(restored.cards, Is.EqualTo(cards));
            Assert.That(resumed.Campaign.Accept(City), Is.EqualTo(Failure.None)); Assert.That(resumed.Campaign.Travel(City), Is.EqualTo(Failure.None));
            var continued = Roundtrip(restored); var continuedSession = new GameSession(defs, continued, clock);
            Assert.That(continuedSession.Campaign.Accept(City), Is.EqualTo(Failure.NotReady));
            Assert.That(continued.money, Is.EqualTo(money + 15000)); Assert.That(continued.cards, Is.EqualTo(cards + 8));
            Assert.That(continued.rewardHistory.Count(reward => reward.source == RewardSource.CityCharter && reward.sourceId == City), Is.EqualTo(1));
            foreach (string source in Sources.Skip(1))
                Assert.That(continued.rewardHistory.Count(reward => reward.source == RewardSource.CityCharter && reward.sourceId == source), Is.EqualTo(1), source);
            foreach (string source in Sources)
            {
                Assert.That(restored.buildings.Count(b => b.cityId == source && b.family == "residential_house"), Is.EqualTo(17));
                Assert.That(restored.buildings.Count(b => b.cityId == source && b.family == "park" && b.level >= 1), Is.EqualTo(1));
            }
            SaveValidator.Validate(restored, defs);
        }

        [Test] public void FinalHousingTierMakesTenMillionReachableAndPreservesEarlierPopulationBands()
        {
            var current = defs.Building("residential_house"); var previous = VersionTwelve().Building("residential_house");
            for (int level = 1; level < 5000; level++) Assert.That(current.ResidentCapacity(level), Is.EqualTo(previous.ResidentCapacity(level)), "Changed housing before the final band at " + level);
            Assert.That(previous.residentTierBonuses[24], Is.EqualTo(475)); Assert.That(current.residentTierBonuses[24], Is.EqualTo(1000));
            Assert.That(previous.ResidentCapacity(5000), Is.EqualTo(600)); Assert.That(current.ResidentCapacity(5000), Is.EqualTo(1125));
            Assert.That(MaximumCityPopulation(defs), Is.EqualTo(975375));
            foreach (string source in Sources)
            {
                Populate(session, source, MaximumCityPopulation(defs));
                Assert.That(state.buildings.Count(b => b.cityId == source && b.family == "residential_house"), Is.EqualTo(17));
                Assert.That(state.buildings.Count(b => b.cityId == source && b.family == "park" && b.level >= 1), Is.EqualTo(1));
            }
            session.Campaign.RefreshMilestones();
            Assert.That(session.Campaign.UnlockProgress(City), Is.EqualTo(10729125));
            Assert.That(session.Campaign.UnlockProgress(City), Is.EqualTo(Sources.Length * MaximumCityPopulation(defs))); Assert.That(session.Campaign.Offered(City), Is.True);
            SaveValidator.Validate(state, defs);
        }
        [Test] public void HousingSelectionReservesAParkAndNeverAllowsAnEighteenthHouse()
        {
            var fresh = GameState.Create(defs, "Wohnraum und Stadtgarten", Start); fresh.introSeen = true; fresh.City.unlocked = Zone.Settlement;
            var game = new GameSession(defs, fresh, clock); var plots = fresh.buildings.Where(b => b.cityId == fresh.activeCity && b.family == "settlement_plot").ToArray();
            Assert.That(plots.Length, Is.EqualTo(18));
            foreach (var plot in plots.Take(17)) Assert.That(game.ChoosePlot(plot.id, "residential_house"), Is.EqualTo(Failure.None));
            Assert.That(game.ChoosePlot(plots.Last().id, "residential_house"), Is.EqualTo(Failure.ReservedPlot));
            Assert.That(game.ChoosePlot(plots.Last().id, "park"), Is.EqualTo(Failure.None));
            Assert.That(fresh.buildings.Count(b => b.cityId == fresh.activeCity && b.family == "residential_house"), Is.EqualTo(17));
            Assert.That(fresh.buildings.Count(b => b.cityId == fresh.activeCity && b.family == "park"), Is.EqualTo(1));
        }

        [Test] public void MigrationRejectsHistoricalCorruptionForgedChartersAndUnsupportedFutureVersions()
        {
            var old = GameState.Create(VersionTwelve(), "Beschädigte Stadt", Start); old.buildings.RemoveAt(0);
            Assert.Throws<InvalidDataException>(() => Roundtrip(old));
            state.cities.Single(c => c.id == City).charterAccepted = true; Assert.Throws<InvalidDataException>(() => SaveValidator.Validate(state, defs));
            old = GameState.Create(VersionTwelve(), "Zukünftige Stadt", Start); var future = JsonUtility.FromJson<GameDefinitions>(JsonUtility.ToJson(defs)); future.version = ProgressionTests.Definitions().version + 1;
            string payload = JsonUtility.ToJson(old); var store = new LocalSaveStore(Path.Combine("Temp", "UnsupportedSkunkfurt.json"), future);
            Assert.Throws<InvalidDataException>(() => store.Decode(JsonUtility.ToJson(new SaveEnvelope { payload = payload, sha256 = LocalSaveStore.Hash(payload) })));
        }

        [TestCase(0)] [TestCase(1)] public void EveryStoryChoiceRecurringOfferAndCommunityProjectPersists(int branch)
        {
            OwnAndTravel(); state.City.unlocked = Zone.Industry; state.City.storyResidentMilestone = 30000; session.Stories.RefreshUnlocks();
            CompletePersonalStoriesAndCommunityProjects(branch);
            Assert.That(state.City.vipStories.Sum(p => p.completed.Count), Is.EqualTo(18)); Assert.That(state.City.vipCollaborations.Count(p => p.completed != null), Is.EqualTo(3));
            foreach (var person in session.Stories.People.ToArray()) for (int repeat = 0; repeat < person.recurring.Length; repeat++)
            {
                state.simulationSeconds = Math.Max(state.simulationSeconds, session.Stories.Person(person.id).nextRecurringSecond);
                Assert.That(session.Stories.Offer(person.id), Is.SameAs(person.recurring[repeat])); CompleteStory(person.id, branch); Assert.That(session.Stories.Offer(person.id), Is.Null);
            }
            Assert.That(state.City.vipStories.Sum(p => p.recurringCompleted), Is.EqualTo(18));
            Assert.That(JsonUtility.ToJson(Roundtrip(state).City), Is.EqualTo(JsonUtility.ToJson(state.City)));
        }

        [Test] public void AcceptedStoryCapturesExistingActivityAsBaselineAndSurvivesContinue()
        {
            OwnAndTravel(); state.City.unlocked = Zone.Industry; state.City.storyResidentMilestone = 30000;
            state.City.stats.rawProduced = 10000; state.City.stats.refinedProduced = 10000; state.City.stats.coffeeSold = 10000;
            state.City.stats.butterProduced = 10000; state.City.stats.pastrySold = 10000; state.City.stats.delivered = 10000; state.City.stats.deliveries = 10000;
            session.Stories.RefreshUnlocks(); session.Stories.Person("sf_jonas").visits = 10000; var offer = session.Stories.Offer("sf_jonas");
            Assert.That(offer, Is.Not.Null); Assert.That(offer.goals.Any(g => g.IsCounter), Is.True);
            Assert.That(session.Stories.Accept("sf_jonas", offer.id, offer.choices.Last().id), Is.EqualTo(Failure.None)); var active = session.Stories.Active("sf_jonas");
            for (int i = 0; i < offer.goals.Length; i++) if (offer.goals[i].IsCounter)
            {
                Assert.That(active.baselines[i], Is.EqualTo(VipStoryRules.Value(defs, state, session.Simulation, offer.goals[i], City)));
                Assert.That(session.Stories.Progress("sf_jonas", i), Is.EqualTo(Amount.Zero));
            }
            Assert.That(session.Stories.Ready("sf_jonas"), Is.False); var restored = Roundtrip(state); var resumed = new GameSession(defs, restored, clock);
            Assert.That(JsonUtility.ToJson(resumed.Stories.Active("sf_jonas")), Is.EqualTo(JsonUtility.ToJson(active)));
            Assert.That(resumed.Stories.Claim("sf_jonas", offer.id), Is.EqualTo(Failure.NotReady));
        }

        [Test] public void CommunityProjectsFollowResidentChaptersWithoutBlockingTheirOwnPrerequisites()
        {
            var residents = defs.vipStories.Where(p => p.cityId == City).ToArray();
            var chapterIds = residents.SelectMany(p => p.chapters).Select(c => c.id).ToHashSet();
            foreach (var chapter in residents.SelectMany(p => p.chapters))
                Assert.That(chapter.prerequisites.All(chapterIds.Contains), Is.True, "A personal chapter must remain reachable before its community project: " + chapter.id);
            var projects = defs.vipCollaborations.Where(p => p.cityId == City).ToArray();
            Assert.That(projects.All(p => p.quest.prerequisites.Count(chapterIds.Contains) >= 6), Is.True, "Every restoration needs progress from all six permanent inhabitants.");
        }

        [Test] public void LocalFoundationCompletesTwelveMainQuestsUsingOnlyTheCharterBridge()
        {
            PopulateThreshold(); state.money = 0; state.cards = 0;
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.None)); Assert.That(session.Campaign.Travel(City), Is.EqualTo(Failure.None)); session.Stories.RefreshUnlocks();
            int grinders = state.grinders, awardsTotal = 0; long recovery = state.nextRecoveryWork, sequence = state.rewardSequence;
            var earnings = state.cities.Where(c => Sources.Contains(c.id)).ToDictionary(c => c.id, c => c.stats.earned);
            var assignments = state.workers.ToDictionary(w => w.id, w => (w.buildingId, w.slot));
            var histories = state.cities.Where(c => Sources.Contains(c.id)).Select(c => string.Join(",", c.completedQuests)).ToArray();
            for (int tick = 0; tick < TutorialDriver.MaxSimulationSeconds && state.City.questIndex < 12; tick++)
            {
                int before = state.grinders; TutorialDriver.Act(session, clock.UtcNow, allowRecovery: false); clock.UtcNow = clock.UtcNow.AddSeconds(1); session.Advance(1);
                int awards = state.rewardHistory.Where(r => r.sequence > sequence).SelectMany(r => r.items).Where(i => i.kind == RewardKind.Grinders).Sum(i => checked((int)Math.Round(i.quantity.ToDouble())));
                awardsTotal += awards; sequence = state.rewardSequence; Assert.That(state.grinders, Is.EqualTo(before + awards), "Premium currency spent at " + session.Progression.Current?.id);
            }
            Assert.That(state.City.questIndex, Is.GreaterThanOrEqualTo(12), "Foundation stalled at " + session.Progression.Current?.id);
            Assert.That(state.City.stats.butterProduced > 0 && state.City.stats.pastrySold > 0, Is.True);
            Assert.That(state.grinders, Is.EqualTo(grinders + awardsTotal)); Assert.That(state.nextRecoveryWork, Is.EqualTo(recovery));
            foreach (var city in state.cities.Where(c => Sources.Contains(c.id))) Assert.That(city.stats.earned, Is.EqualTo(earnings[city.id]));
            CollectionAssert.AreEqual(histories, state.cities.Where(c => Sources.Contains(c.id)).Select(c => string.Join(",", c.completedQuests)));
            foreach (var item in assignments) { var worker = state.workers.Single(w => w.id == item.Key); Assert.That((worker.buildingId, worker.slot), Is.EqualTo(item.Value)); }
            Assert.That(Roundtrip(state).City.questIndex, Is.EqualTo(state.City.questIndex));
        }

        [TestCase("fair_finance_desk")] [TestCase("housing_council")]
        public void JonasCityCharterChoicePersistsWithoutChangingOtherCities(string choice)
        {
            OwnAndTravel(); state.City.unlocked = Zone.Industry; state.City.storyResidentMilestone = 30000;
            session.Stories.RefreshUnlocks(); var person = defs.vipStories.Single(p => p.id == "sf_jonas");
            CollectionAssert.AreEquivalent(new[] { "fair_finance_desk", "housing_council" }, person.chapters[2].choices.Select(c => c.id));
            CompletePersonalStoriesAndCommunityProjects(choice == "fair_finance_desk" ? 0 : 1);
            Assert.That(session.Stories.Person("sf_jonas").completed[2].choiceId, Is.EqualTo(choice));
            var restored = Roundtrip(state); var resumed = new GameSession(defs, restored, clock);
            Assert.That(resumed.Stories.Person("sf_jonas").completed[2].choiceId, Is.EqualTo(choice));
            foreach (var city in restored.cities.Where(c => Sources.Contains(c.id)))
                Assert.That(city.vipStories.Any(p => p.id.StartsWith("sf_", StringComparison.Ordinal)), Is.False, city.id);
            Assert.That(resumed.Stories.Claim("sf_jonas", person.chapters[2].id), Is.EqualTo(Failure.NotReady));
            SaveValidator.Validate(restored, defs);
        }

        [Test] public void TwelveCitySaveSurvivesOfflineAndVisitsEveryOwnedCityWithoutMovingWorkersOrLocalQuestAndStoryHistories()
        {
            OwnAndTravel(); state.City.unlocked = Zone.Industry; state.City.storyResidentMilestone = 30000; session.Stories.RefreshUnlocks();
            CompletePersonalStoriesAndCommunityProjects(1);
            Assert.That(session.Stories.Person("sf_jonas").completed[2].choiceId, Is.EqualTo("housing_council"));
            Assert.That(session.Campaign.Travel("saint_mary_ville"), Is.EqualTo(Failure.None)); session.Stories.RefreshUnlocks();
            var person = session.Stories.People.First(p => session.Stories.Offer(p.id) != null); var offer = session.Stories.Offer(person.id);
            Assert.That(session.Stories.Accept(person.id, offer.id, offer.choices[0].id), Is.EqualTo(Failure.None));
            foreach (var city in state.cities)
            {
                city.questIndex = 1; city.completedQuests = defs.QuestsFor(city.id).Take(1).Select(quest => quest.id).ToList();
            }
            Assert.That(session.Campaign.Travel(City), Is.EqualTo(Failure.None));
            var assignment = state.workers.Select(w => w.id + "/" + w.buildingId + "/" + w.slot).ToArray();
            var histories = state.cities.ToDictionary(city => city.id, city => string.Join(",", city.completedQuests));
            var stories = state.cities.ToDictionary(city => city.id, city => JsonUtility.ToJson(new CityStorySnapshot {
                people = city.vipStories.ToArray(), projects = city.vipCollaborations.ToArray() }));
            state.Find("farm_grow_01").level = 25; state.Find("sf_farm_grow_01").level = 25;
            var restored = Roundtrip(state); clock.UtcNow = Start.AddSeconds(30); var resumed = new GameSession(defs, restored, clock);
            Assert.That(resumed.ApplyOffline().Seconds, Is.EqualTo(30)); Assert.That(resumed.ApplyOffline().Seconds, Is.Zero);
            foreach (var city in defs.cities)
            {
                Assert.That(resumed.Campaign.Travel(city.id), Is.EqualTo(Failure.None), city.id);
                Assert.That(restored.activeCity, Is.EqualTo(city.id)); Assert.That(resumed.Progression.Current.id, Is.EqualTo(defs.QuestsFor(city.id)[1].id));
                Assert.That(string.Join(",", restored.City.completedQuests), Is.EqualTo(histories[city.id]));
                Assert.That(JsonUtility.ToJson(new CityStorySnapshot { people = restored.City.vipStories.ToArray(), projects = restored.City.vipCollaborations.ToArray() }), Is.EqualTo(stories[city.id]));
                SaveValidator.Validate(restored, defs);
            }
            CollectionAssert.AreEqual(assignment, restored.workers.Select(w => w.id + "/" + w.buildingId + "/" + w.slot));
            Assert.That(resumed.Stories.Person("sf_jonas").completed[2].choiceId, Is.EqualTo("housing_council"));
            Assert.That(resumed.Stories.People.All(p => resumed.Stories.Unlocked(p.id)), Is.True, "Permanent residents must survive offline continue.");
            Assert.That(Roundtrip(restored).cities.Count, Is.EqualTo(12));
        }
        private void CompleteStory(string id, int branch)
        {
            var offer = session.Stories.Offer(id); Assert.That(offer, Is.Not.Null, id);
            Assert.That(session.Stories.Accept(id, offer.id, offer.choices[branch % offer.choices.Length].id), Is.EqualTo(Failure.None));
            var active = session.Stories.Active(id); Assert.That(session.Stories.Ready(id), Is.False, "Fresh activity required: " + offer.id);
            for (int i = 0; i < offer.goals.Length; i++)
            {
                var goal = offer.goals[i]; int count = active.targets[i];
                switch (goal.kind)
                {
                    case StoryGoalKind.Level:
                        var building = state.buildings.FirstOrDefault(b => b.cityId == City && (b.id == goal.target || b.family == goal.target));
                        if (building == null) { building = state.buildings.First(b => b.cityId == City && b.family == "settlement_plot"); building.family = goal.target; }
                        building.level = Math.Max(building.level, count); break;
                    case StoryGoalKind.Residents: Populate(session, City, count); break;
                    case StoryGoalKind.NpcVisited: for (int visit = 0; visit < count; visit++) Assert.That(session.Stories.Visit(goal.target), Is.EqualTo(Failure.None)); break;
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

        private void CompletePersonalStoriesAndCommunityProjects(int branch)
        {
            var people = session.Stories.People.ToArray(); var projects = session.Stories.Collaborations.ToArray();
            int required = people.Sum(p => p.chapters.Length) + projects.Length;
            for (int pass = 0; pass < required; pass++)
            {
                bool progress = false;
                foreach (var person in people)
                {
                    var offer = session.Stories.Offer(person.id);
                    if (offer == null || !person.chapters.Contains(offer)) continue;
                    CompleteStory(person.id, branch); progress = true;
                }
                foreach (var project in projects)
                {
                    if (session.Stories.Offer(project.id) == null) continue;
                    CompleteStory(project.id, branch); progress = true;
                }
                int completed = state.City.vipStories.Sum(p => p.completed.Count) + state.City.vipCollaborations.Count(p => p.completed != null);
                if (completed == required) return;
                Assert.That(progress, Is.True, "Skunkfurt chapter/project prerequisites stopped progress after " + completed + " completions.");
            }
            Assert.Fail("Skunkfurt story graph did not finish within its finite quest count.");
        }

        private void OwnAndTravel()
        {
            PopulateThreshold();
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.None)); Assert.That(session.Campaign.Travel(City), Is.EqualTo(Failure.None)); session.Stories.RefreshUnlocks();
        }
        private static void OwnSources(GameSession game)
        {
            game.State.City.storyResidentMilestone = 1000; Assert.That(game.Campaign.Accept("big_rock_city"), Is.EqualTo(Failure.None));
            Populate(game, "burning_hills", 3000); Populate(game, "big_rock_city", 2000); Assert.That(game.Campaign.Accept("seven_hills"), Is.EqualTo(Failure.None));
            foreach (string city in Sources.Take(3)) Populate(game, city, 5000); Assert.That(game.Campaign.Accept("little_montana"), Is.EqualTo(Failure.None));
            foreach (string city in Sources.Take(4)) Populate(game, city, 12500); Assert.That(game.Campaign.Accept("cassa_vadela"), Is.EqualTo(Failure.None));
            foreach (string city in Sources.Take(5)) Populate(game, city, 20000); Assert.That(game.Campaign.Accept("mark_wood_city"), Is.EqualTo(Failure.None));
            foreach (string city in Sources.Take(5)) Populate(game, city, 50000); Assert.That(game.Campaign.Accept("green_leaf_village"), Is.EqualTo(Failure.None));
            foreach (string city in Sources.Take(7)) Populate(game, city, 70000); Populate(game, "green_leaf_village", 80000);
            Assert.That(game.Campaign.Accept("purple_garden"), Is.EqualTo(Failure.None));
            foreach (string city in Sources.Take(8)) Populate(game, city, 125715);
            Assert.That(game.Campaign.Accept("golden_flower"), Is.EqualTo(Failure.None));
            foreach (string city in Sources.Take(9)) Populate(game, city, 225420);
            Assert.That(game.Campaign.Accept("saint_mary_ville"), Is.EqualTo(Failure.None));
            // Historical density reaches the charter with seventeen full houses; 500,000 cannot fit its final remainder in the one available slot.
            int greatKushvilleSourcePopulation = game.Definitions.version <= 12 ? MaximumCityPopulation(game.Definitions) : 500000;
            foreach (string city in Sources.Take(10)) Populate(game, city, greatKushvilleSourcePopulation);
            Assert.That(game.Campaign.Accept("great_kushville"), Is.EqualTo(Failure.None));
        }
        private void PopulateSources(params int[] residents)
        { Assert.That(residents.Length, Is.EqualTo(Sources.Length)); for (int i = 0; i < Sources.Length; i++) Populate(session, Sources[i], residents[i]); }
        private void PopulateThreshold(int total = 10000000)
        {
            // Spread real, currently occupied housing across all eleven previous cities. Each retains its mandatory park.
            int maximum = MaximumCityPopulation(defs), remaining = total;
            var parts = new int[Sources.Length];
            for (int index = 0; index < parts.Length; index++) { parts[index] = Math.Min(maximum, remaining); remaining -= parts[index]; }
            Assert.That(remaining, Is.Zero, "The real legal housing capacity must reach the advertised threshold.");
            PopulateSources(parts);
        }
        private static int MaximumCityPopulation(GameDefinitions definitions) => 17 * (int)Math.Floor(
            definitions.Building("residential_house").ResidentCapacity(5000) * (1 + definitions.workerAttributeStep * 100 * 10));
        private static void SelectPlot(GameSession game, string city, BuildingState plot, string family)
        {
            string previousActiveCity = game.State.activeCity; var destination = game.State.cities.Single(c => c.id == city); Zone previousZone = destination.unlocked;
            try
            {
                // Fixtures accelerate district access, while retaining the real irreversible plot-selection rule.
                game.State.activeCity = city; if (destination.unlocked < Zone.Settlement) destination.unlocked = Zone.Settlement;
                Assert.That(game.ChoosePlot(plot.id, family), Is.EqualTo(Failure.None), "Plot selection failed in " + city);
            }
            finally { destination.unlocked = previousZone; game.State.activeCity = previousActiveCity; }
        }
        private static void Populate(GameSession game, string city, int residents)
        {
            var park = game.State.buildings.FirstOrDefault(b => b.cityId == city && b.family == "park");
            if (park == null)
            {
                park = game.State.buildings.First(b => b.cityId == city && b.family == "settlement_plot");
                SelectPlot(game, city, park, "park");
            }
            park.level = Math.Max(1, park.level);
            var plots = game.State.buildings.Where(b => b.cityId == city && (b.family == "settlement_plot" || b.family == "residential_house")).ToArray();
            Assert.That(plots.Length, Is.LessThanOrEqualTo(17), "Each city retains its required park.");
            foreach (var plot in plots) { plot.level = 0; game.State.workers.RemoveAll(w => w.buildingId == plot.id); }
            if (residents == 0) return;
            var choices = new Dictionary<int, (int level, int workerLevel, int rank)>();
            for (int tier = 1; tier < VisualTiers.Starts.Length; tier++) for (int level = 1; level <= 100; level++) for (int rank = 1; rank <= 10; rank++)
            {
                int count = (int)Math.Floor(game.Definitions.Building("residential_house").ResidentCapacity(VisualTiers.Starts[tier]) * (1 + game.Definitions.workerAttributeStep * level * rank));
                if (!choices.ContainsKey(count)) choices[count] = (VisualTiers.Starts[tier], level, rank);
            }
            var parts = PopulationParts(residents, plots.Length, choices.Keys);
            Assert.That(parts.Count, Is.LessThanOrEqualTo(plots.Length), "Real housing capacity cannot reach " + residents + " residents in " + city);
            for (int i = 0; i < parts.Count; i++)
            {
                var config = choices[parts[i]]; var building = plots[i];
                if (building.family == "settlement_plot") SelectPlot(game, city, building, "residential_house");
                building.level = config.level;
                game.State.workers.Add(new WorkerState { id = "skunkfurt_fixture_" + building.id, role = "caretaker", level = config.workerLevel, rank = config.rank,
                    buildingId = building.id, slot = 0, attributes = new List<WorkerAttribute> { WorkerAttribute.Residents } });
            }
            Assert.That(game.Simulation.Residents(city), Is.EqualTo(residents));
        }
        private static List<int> PopulationParts(int residents, int slots, IEnumerable<int> possibleCounts)
        {
            var available = possibleCounts.OrderBy(count => count).ToArray(); var possible = new HashSet<int>(available); int maximum = available.Last();
            for (int full = Math.Min(slots, residents / maximum); full >= 0; full--)
            {
                int remainder = residents - full * maximum;
                if (!TryPopulationRemainder(remainder, slots - full, available, possible, out var tail)) continue;
                var result = Enumerable.Repeat(maximum, full).ToList(); result.AddRange(tail); return result;
            }
            Assert.Fail("Cannot represent " + residents + " residents using " + slots + " legally selected houses.");
            return new List<int>();
        }
        private static bool TryPopulationRemainder(int residents, int slots, int[] available, HashSet<int> possible, out List<int> parts)
        {
            parts = new List<int>(); if (residents == 0) return true;
            if (slots < 1) return false;
            if (possible.Contains(residents)) { parts.Add(residents); return true; }
            // Try the actual remainder first; removing a full house can make a valid two-house remainder impossible.
            if (slots < 2) return false;
            if (TryPopulationPair(residents, available, possible, out int first, out int second))
            { parts.Add(first); parts.Add(second); return true; }
            if (slots < 3) return false;
            foreach (int candidate in available)
            {
                if (candidate >= residents) break;
                if (!TryPopulationPair(residents - candidate, available, possible, out first, out second)) continue;
                parts.Add(candidate); parts.Add(first); parts.Add(second); return true;
            }
            return false;
        }
        private static bool TryPopulationPair(int residents, int[] available, HashSet<int> possible, out int first, out int second)
        {
            foreach (int candidate in available)
            {
                if (candidate >= residents) break;
                if (!possible.Contains(residents - candidate)) continue;
                first = candidate; second = residents - candidate; return true;
            }
            first = second = 0; return false;
        }
        private static GameDefinitions VersionThirteenFixture(GameDefinitions current)
        {
            // Preserve the verified twelfth-city balance as a historical regression; the next city has separate current-content tests.
            var previous = JsonUtility.FromJson<GameDefinitions>(JsonUtility.ToJson(current)); previous.version = 13;
            previous.Building("residential_house").residentTierBonuses[24] = 1000;
            previous.cities = previous.cities.Where(c => c.introducedVersion <= 13).ToArray(); previous.instances = previous.instances.Where(i => i.introducedVersion <= 13).ToArray();
            var ids = previous.instances.Select(i => i.id).ToHashSet(); previous.routes = previous.routes.Where(r => ids.Contains(r.from) && ids.Contains(r.to)).ToArray();
            var cityIds = previous.cities.Select(c => c.id).ToHashSet(); previous.quests = previous.quests.Where(q => cityIds.Contains(string.IsNullOrEmpty(q.cityId) ? "burning_hills" : q.cityId)).ToArray();
            previous.vipStories = previous.vipStories.Where(p => cityIds.Contains(p.cityId)).ToArray(); previous.vipCollaborations = previous.vipCollaborations.Where(p => cityIds.Contains(p.cityId)).ToArray();
            previous.Validate(); return previous;
        }
        private GameDefinitions VersionTwelve()
        {
            var previous = JsonUtility.FromJson<GameDefinitions>(JsonUtility.ToJson(defs)); previous.version = 12;
            previous.Building("residential_house").residentTierBonuses[24] = 475;
            previous.cities = previous.cities.Where(c => c.introducedVersion <= 12).ToArray(); previous.instances = previous.instances.Where(i => i.introducedVersion <= 12).ToArray();
            var ids = previous.instances.Select(i => i.id).ToHashSet(); previous.routes = previous.routes.Where(r => ids.Contains(r.from) && ids.Contains(r.to)).ToArray();
            var cityIds = previous.cities.Select(c => c.id).ToHashSet(); previous.quests = previous.quests.Where(q => cityIds.Contains(string.IsNullOrEmpty(q.cityId) ? "burning_hills" : q.cityId)).ToArray();
            previous.vipStories = previous.vipStories.Where(p => cityIds.Contains(p.cityId)).ToArray(); previous.vipCollaborations = previous.vipCollaborations.Where(p => cityIds.Contains(p.cityId)).ToArray();
            previous.Validate(); return previous;
        }
        private GameState Roundtrip(GameState source)
        {
            string payload = JsonUtility.ToJson(source); return new LocalSaveStore(Path.Combine("Temp", "SkunkfurtUnused.json"), defs).Decode(
                JsonUtility.ToJson(new SaveEnvelope { payload = payload, sha256 = LocalSaveStore.Hash(payload) }));
        }
        private sealed class FailingStore : ISaveStore
        {
            public bool Exists => false;
            public SaveLoadResult Load() => new SaveLoadResult();
            public void Save(GameState value) => throw new IOException("Intentional Skunkfurt save failure.");
        }
        [Serializable] private sealed class InventorySnapshot { public ItemStack[] items; }
        [Serializable] private sealed class CityStorySnapshot { public VipStoryState[] people; public VipCollaborationState[] projects; }
    }
}

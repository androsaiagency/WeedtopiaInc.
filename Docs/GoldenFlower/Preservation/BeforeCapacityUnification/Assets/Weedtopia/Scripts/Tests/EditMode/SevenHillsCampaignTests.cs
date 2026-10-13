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
    public sealed class SevenHillsCampaignTests
    {
        private GameDefinitions defs;
        private GameState state;
        private GameSession session;
        private ManualClock clock;
        private const string City = "seven_hills";
        private static readonly DateTime Start = new DateTime(2026, 10, 7, 10, 0, 0, DateTimeKind.Utc);
        [SetUp] public void Setup()
        {
            defs = ProgressionTests.Definitions(); state = GameState.Create(defs, "Seven Hills Test", Start); state.introSeen = true;
            clock = new ManualClock(Start); session = new GameSession(defs, state, clock);
            state.City.storyResidentMilestone = 1000;
            Assert.That(session.Campaign.Accept("big_rock_city"), Is.EqualTo(Failure.None));
        }
        [Test] public void CatalogHasACombinedFiveThousandCharterAndItsOwnCompleteNeighborhood()
        {
            var charter = defs.cities.Single(c => c.id == City);
            Assert.That(charter.unlockSources, Is.EquivalentTo(new[] { "burning_hills", "big_rock_city" }));
            Assert.That(charter.unlockResidents, Is.EqualTo(5000)); Assert.That(charter.introducedVersion, Is.EqualTo(4));
            Assert.That(defs.instances.Count(i => i.cityId == City), Is.EqualTo(56));
            Assert.That(defs.instances.Where(i => i.cityId == City).Select(i => i.family),
                Is.EquivalentTo(defs.instances.Where(i => i.cityId == "burning_hills").Select(i => i.family)));
            Assert.That(defs.vipStories.Count(p => p.cityId == City), Is.EqualTo(7));
            Assert.That(defs.vipStories.Where(p => p.cityId == City).Sum(p => p.chapters.Length), Is.EqualTo(28));
            Assert.That(defs.vipStories.Where(p => p.cityId == City).Sum(p => p.recurring.Length), Is.EqualTo(21));
            Assert.That(defs.vipCollaborations.Count(p => p.cityId == City), Is.EqualTo(3));
            defs.Validate();
        }
        [Test] public void FourThousandNineHundredNinetyNineResidentsRemainLocked()
        {
            Populate("burning_hills", 3000); Populate("big_rock_city", 1999); session.Campaign.RefreshMilestones();
            Assert.That(session.Campaign.UnlockProgress(City), Is.EqualTo(4999));
            Assert.That(session.Campaign.Offered(City), Is.False); Assert.That(session.Campaign.Travel(City), Is.EqualTo(Failure.Locked));
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.NotReady)); SaveValidator.Validate(state, defs);
        }
        [TestCase(3000, 2000)] [TestCase(4200, 800)] [TestCase(5000, 0)]
        public void FiveThousandResidentsOfferTheCityWithEveryDistribution(int burning, int rock)
        {
            Populate("burning_hills", burning); Populate("big_rock_city", rock); session.Campaign.RefreshMilestones();
            Assert.That(session.Campaign.UnlockProgress(City), Is.EqualTo(5000)); Assert.That(session.Campaign.Offered(City), Is.True);
            Assert.That(state.cities.Single(c => c.id == City).charterResidentMilestone, Is.EqualTo(5000));
            SaveValidator.Validate(state, defs);
        }
        [Test] public void SeparateHistoricalHighsNeverSatisfyTheCombinedThreshold()
        {
            Populate("burning_hills", 3000); Populate("big_rock_city", 0); session.Campaign.RefreshMilestones();
            Populate("burning_hills", 0); Populate("big_rock_city", 3000); session.Campaign.RefreshMilestones();
            Assert.That(state.cities.Where(c => c.id != City).Sum(c => c.storyResidentMilestone), Is.EqualTo(6000));
            Assert.That(session.Campaign.UnlockProgress(City), Is.EqualTo(3000));
            Assert.That(state.cities.Single(c => c.id == City).charterResidentMilestone, Is.EqualTo(3000));
            Assert.That(session.Campaign.Offered(City), Is.False); SaveValidator.Validate(state, defs);
        }
        [Test] public void OfferSurvivesPopulationDeclineAndSaveRestoreBeforeAcceptance()
        {
            Populate("burning_hills", 3000); Populate("big_rock_city", 2000); session.Campaign.RefreshMilestones();
            Populate("burning_hills", 0); Populate("big_rock_city", 0);
            var restored = Roundtrip(state); var resumed = new GameSession(defs, restored, clock);
            Assert.That(resumed.Campaign.UnlockProgress(City), Is.Zero); Assert.That(resumed.Campaign.Offered(City), Is.True);
            Assert.That(resumed.Campaign.Accept(City), Is.EqualTo(Failure.None)); Assert.That(resumed.Campaign.Accessible(City), Is.True);
            Assert.That(Roundtrip(restored).cities.Single(c => c.id == City).charterAccepted, Is.True);
        }
        [TestCase("burning_hills")] [TestCase("big_rock_city")]
        public void AcceptanceWorksFromBothCitiesAndPaysOnlyOnce(string current)
        {
            Populate("burning_hills", 3000); Populate("big_rock_city", 2000); state.activeCity = current;
            Amount money = state.money; int cards = state.cards; string firstCityQuests = string.Join(",", state.cities[0].completedQuests);
            Assert.That(session.TransactCampaign(() => session.Campaign.Accept(City)), Is.EqualTo(Failure.None));
            Assert.That(state.money, Is.EqualTo(money + 2500)); Assert.That(state.cards, Is.EqualTo(cards + 2));
            Assert.That(string.Join(",", state.cities[0].completedQuests), Is.EqualTo(firstCityQuests));
            Assert.That(state.rewardHistory.Count(r => r.source == RewardSource.CityCharter && r.sourceId == City), Is.EqualTo(1));
            string accepted = JsonUtility.ToJson(state);
            Assert.That(session.TransactCampaign(() => session.Campaign.Accept(City)), Is.EqualTo(Failure.NotReady));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(accepted)); SaveValidator.Validate(state, defs);
        }
        [Test] public void AcceptanceFailureRestoresTheCombinedLatchAndAllSharedResources()
        {
            Populate("burning_hills", 3000); Populate("big_rock_city", 2000);
            var failing = new GameSession(defs, state, clock, new FailingStore());
            // Exercise the latch captured inside Accept, rather than one already observed by the session constructor.
            state.cities.Single(c => c.id == City).charterResidentMilestone = 0;
            string before = JsonUtility.ToJson(state);
            Assert.Throws<IOException>(() => failing.TransactCampaign(() => failing.Campaign.Accept(City)));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before)); Assert.That(failing.Campaign.Accessible(City), Is.False);
        }
        [Test] public void AWorkerAssignmentCapturesTheOfferBeforeTheNextSimulationTick()
        {
            Populate("burning_hills", 3000); Populate("big_rock_city", 2000); state.activeCity = "big_rock_city"; session.Paused = true;
            var caretaker = state.workers.Single(w => w.buildingId != null && state.Find(w.buildingId).cityId == "big_rock_city");
            string home = caretaker.buildingId; session.Workers.Remove(caretaker.id);
            Assert.That(session.Campaign.Offered(City), Is.False);
            Assert.That(session.Transact(() => session.Workers.Assign(caretaker.id, home, 0)), Is.EqualTo(Failure.None));
            Assert.That(state.cities.Single(c => c.id == City).charterResidentMilestone, Is.EqualTo(5000));
            Assert.That(session.Transact(() => session.Workers.Remove(caretaker.id)), Is.EqualTo(Failure.None));
            Assert.That(session.Campaign.UnlockProgress(City), Is.LessThan(5000)); Assert.That(session.Campaign.Offered(City), Is.True);
            Assert.That(new GameSession(defs, Roundtrip(state), clock).Campaign.Offered(City), Is.True);
        }
        [Test] public void FailedWorkerAssignmentRestoresTheCapturedOfferAndAssignment()
        {
            Populate("burning_hills", 3000); Populate("big_rock_city", 2000); state.activeCity = "big_rock_city";
            var caretaker = state.workers.Single(w => w.buildingId != null && state.Find(w.buildingId).cityId == "big_rock_city");
            string home = caretaker.buildingId; session.Workers.Remove(caretaker.id);
            var failing = new GameSession(defs, state, clock, new FailingStore()); string before = JsonUtility.ToJson(state);
            Assert.Throws<IOException>(() => failing.Transact(() => failing.Workers.Assign(caretaker.id, home, 0)));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before)); Assert.That(failing.Campaign.Offered(City), Is.False);
        }
        [Test] public void ExistingVersionThreeSaveAddsOnlyTheNewCityAndOffersItsCurrentPopulation()
        {
            var previous = JsonUtility.FromJson<GameDefinitions>(File.ReadAllText("Docs/SevenHills/Preservation/BurningHills.v3.json"));
            var old = GameState.Create(previous, "Bestehende Städte", Start); old.introSeen = true;
            var oldSession = new GameSession(previous, old, clock); old.City.storyResidentMilestone = 1000; oldSession.Campaign.Accept("big_rock_city");
            Populate(oldSession, "burning_hills", 3000); Populate(oldSession, "big_rock_city", 2000); old.activeCity = "big_rock_city";
            string[] cities = old.cities.Select(JsonUtility.ToJson).ToArray(); string[] buildings = old.buildings.Select(JsonUtility.ToJson).ToArray();
            string[] workers = old.workers.Select(JsonUtility.ToJson).ToArray(); Amount money = old.money;
            var loaded = Roundtrip(old);
            Assert.That(loaded.contentVersion, Is.EqualTo(defs.version)); Assert.That(loaded.buildings.Count, Is.EqualTo(defs.instances.Length));
            Assert.That(loaded.activeCity, Is.EqualTo("big_rock_city")); Assert.That(loaded.money, Is.EqualTo(money));
            CollectionAssert.AreEqual(cities, loaded.cities.Take(2).Select(JsonUtility.ToJson));
            CollectionAssert.AreEqual(buildings, loaded.buildings.Take(112).Select(JsonUtility.ToJson));
            CollectionAssert.AreEqual(workers, loaded.workers.Select(JsonUtility.ToJson));
            Assert.That(new GameSession(defs, loaded, clock).Campaign.Offered(City), Is.True);
        }
        [Test] public void CorruptVersionThreeSaveCannotBeRepairedByNewCityMigration()
        {
            var previous = JsonUtility.FromJson<GameDefinitions>(File.ReadAllText("Docs/SevenHills/Preservation/BurningHills.v3.json"));
            var old = GameState.Create(previous, "Beschädigt", Start); old.buildings.RemoveAt(0);
            Assert.Throws<InvalidDataException>(() => Roundtrip(old));
        }
        [Test] public void MilaIsPresentUponFirstArrivalAndOwnedCitiesKeepProducing()
        {
            Populate("burning_hills", 3000); Populate("big_rock_city", 2000); session.Campaign.Accept(City); session.Campaign.Travel(City); session.Stories.RefreshUnlocks();
            var mila = session.Stories.People.Single(p => p.id == "sh_mila"); Assert.That(mila.residents, Is.Zero);
            Assert.That(session.Stories.Offer(mila.id), Is.Not.Null); Assert.That(session.Simulation.Residents(City), Is.Zero);
            foreach (string id in new[] { "farm_grow_01", "br_farm_grow_01", "sh_farm_grow_01" }) state.Find(id).level = 25;
            session.Advance(8);
            Assert.That(state.cities.Where(c => session.Campaign.Accessible(c.id)).All(c => c.stats.rawProduced > 0), Is.True); SaveValidator.Validate(state, defs);
            Assert.That(Roundtrip(state).activeCity, Is.EqualTo(City));
        }
        [Test] public void ForgedAcceptedCityWithoutASimultaneousMilestoneIsRejected()
        {
            state.cities.Single(c => c.id == City).charterAccepted = true;
            state.cities.Where(c => c.id != City).ToList().ForEach(c => c.storyResidentMilestone = 10000);
            Assert.Throws<InvalidDataException>(() => SaveValidator.Validate(state, defs));
        }
        [Test] public void FiveThousandInBurningHillsNeedNoAdditionalBigRockAcceptance()
        {
            state = GameState.Create(defs, "Erster direkter Auftrag", Start); state.introSeen = true;
            session = new GameSession(defs, state, clock);
            Populate("burning_hills", 5000); session.Campaign.RefreshMilestones();
            Assert.That(session.Campaign.Offered("big_rock_city"), Is.True); Assert.That(session.Campaign.Offered(City), Is.True);
            Assert.That(session.Campaign.CanAccept(City), Is.True); Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.None));
            Assert.That(session.Campaign.Travel(City), Is.EqualTo(Failure.None));
            Assert.That(session.Campaign.Accessible("big_rock_city"), Is.False); SaveValidator.Validate(state, defs);
        }
        private void Populate(string city, int residents) => Populate(session, city, residents);
        private static void Populate(GameSession game, string city, int residents)
        {
            var plots = game.State.buildings.Where(b => b.cityId == city && (b.family == "settlement_plot" || b.family == "residential_house")).ToArray();
            foreach (var plot in plots) { plot.level = 0; game.State.workers.RemoveAll(w => w.buildingId == plot.id); }
            if (residents == 0) return;
            // Real housing and legal caretaker configurations produce exact boundary totals; no historical counters stand in for residents.
            var choices = new Dictionary<int, (int level, int workerLevel, int rank)>();
            for (int tier = 1; tier < VisualTiers.Starts.Length; tier++)
                for (int level = 1; level <= 100; level++) for (int rank = 1; rank <= 10; rank++)
                {
                    int count = (int)Math.Floor(game.Definitions.Building("residential_house").baseResidents * (1 + tier) *
                        (1 + game.Definitions.workerAttributeStep * level * rank));
                    if (count <= residents && !choices.ContainsKey(count)) choices[count] = (VisualTiers.Starts[tier], level, rank);
                }
            var parts = choices.ContainsKey(residents) ? new[] { residents } :
                choices.Keys.Where(n => choices.ContainsKey(residents - n)).Select(n => new[] { n, residents - n }).FirstOrDefault();
            Assert.That(parts, Is.Not.Null, "Cannot author exact resident fixture " + residents);
            for (int i = 0; i < parts.Length; i++)
            {
                var configuration = choices[parts[i]]; var building = plots[i]; building.family = "residential_house"; building.level = configuration.level;
                game.State.workers.Add(new WorkerState { id = "fixture_" + building.id, role = "caretaker", level = configuration.workerLevel,
                    rank = configuration.rank, buildingId = building.id, slot = 0, attributes = new List<WorkerAttribute> { WorkerAttribute.Residents } });
            }
            Assert.That(game.Simulation.Residents(city), Is.EqualTo(residents));
        }
        private GameState Roundtrip(GameState source)
        {
            string payload = JsonUtility.ToJson(source);
            return new LocalSaveStore(Path.Combine("Temp", "SevenHillsUnused.json"), defs).Decode(JsonUtility.ToJson(new SaveEnvelope { payload = payload, sha256 = LocalSaveStore.Hash(payload) }));
        }
        private sealed class FailingStore : ISaveStore
        { public bool Exists => false; public void Save(GameState game) => throw new IOException("Disk failure"); public SaveLoadResult Load() => new SaveLoadResult(); }
    }
}

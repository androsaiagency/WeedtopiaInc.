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
    public sealed class LittleMontanaCampaignTests
    {
        private const string City = "little_montana";
        private const string PreviousContent = "Docs/LittleMontana/Preservation/BurningHills.v4.json";
        private static readonly string[] Sources = { "burning_hills", "big_rock_city", "seven_hills" };
        private static readonly DateTime Start = new DateTime(2026, 10, 7, 10, 0, 0, DateTimeKind.Utc);
        private GameDefinitions defs;
        private GameState state;
        private GameSession session;
        private ManualClock clock;

        [SetUp] public void Setup()
        {
            defs = ProgressionTests.Definitions(); state = GameState.Create(defs, "Little Montana Test", Start); state.introSeen = true;
            clock = new ManualClock(Start); session = new GameSession(defs, state, clock);
            state.City.storyResidentMilestone = 1000;
            Assert.That(session.Campaign.Accept("big_rock_city"), Is.EqualTo(Failure.None));
            Populate(session, "burning_hills", 3000); Populate(session, "big_rock_city", 2000);
            Assert.That(session.Campaign.Accept("seven_hills"), Is.EqualTo(Failure.None));
            foreach (string city in Sources) Populate(session, city, 0);
        }

        [Test] public void FourthCityUsesExactlyTheFirstThreeSourcesAndItsOwnContent()
        {
            var charter = defs.cities.Single(c => c.id == City);
            CollectionAssert.AreEqual(Sources, charter.unlockSources);
            Assert.That(charter.unlockResidents, Is.EqualTo(15000)); Assert.That(charter.introducedVersion, Is.EqualTo(5));
            Assert.That(charter.bridgeMoney, Is.EqualTo(3500)); Assert.That(charter.bridgeCards, Is.EqualTo(3));
            Assert.That(CityCampaignRules.InvitationNoticeKey(charter), Is.EqualTo("campaign.little_montana_notice"));
            Assert.That(CityCampaignRules.InvitationLockedKey(charter), Is.EqualTo("campaign.little_montana_locked"));
            Assert.That(CityCampaignRules.DialoguePrefix(charter), Is.EqualTo("lm.dialog."));
            Assert.That(defs.instances.Count(i => i.cityId == City), Is.EqualTo(56));
            Assert.That(defs.instances.Where(i => i.cityId == City).Select(i => i.family),
                Is.EquivalentTo(defs.instances.Where(i => i.cityId == "burning_hills").Select(i => i.family)));
            Assert.That(defs.QuestsFor(City).Length, Is.EqualTo(18)); Assert.That(defs.QuestsFor(City).First().id, Is.EqualTo("lm_arrival"));
            Assert.That(defs.vipStories.Count(p => p.cityId == City), Is.EqualTo(7));
            Assert.That(defs.vipStories.Where(p => p.cityId == City).Sum(p => p.chapters.Length), Is.EqualTo(28));
            Assert.That(defs.vipStories.Where(p => p.cityId == City).Sum(p => p.recurring.Length), Is.EqualTo(21));
            Assert.That(defs.vipCollaborations.Count(p => p.cityId == City), Is.EqualTo(3)); defs.Validate();
        }

        [Test] public void FourteenThousandNineHundredNinetyNineResidentsRemainLocked()
        {
            PopulateSources(5000, 5000, 4999); session.Campaign.RefreshMilestones();
            Assert.That(session.Campaign.UnlockProgress(City), Is.EqualTo(14999)); Assert.That(session.Campaign.Offered(City), Is.False);
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.NotReady)); Assert.That(session.Campaign.Travel(City), Is.EqualTo(Failure.Locked));
            SaveValidator.Validate(state, defs);
        }

        [TestCase(5000, 5000, 5000)] [TestCase(15000, 0, 0)] [TestCase(0, 15000, 0)] [TestCase(0, 0, 15000)] [TestCase(7000, 6500, 1500)]
        public void FifteenThousandResidentsOfferTheCityForEveryDistribution(int burning, int rock, int seven)
        {
            PopulateSources(burning, rock, seven); session.Campaign.RefreshMilestones();
            Assert.That(session.Campaign.UnlockProgress(City), Is.EqualTo(15000)); Assert.That(session.Campaign.Offered(City), Is.True);
            Assert.That(state.cities.Single(c => c.id == City).charterResidentMilestone, Is.EqualTo(15000)); SaveValidator.Validate(state, defs);
        }

        [Test] public void SeparateHistoricalHighsAndLockedDestinationPopulationCannotUnlockTheCity()
        {
            foreach (string current in Sources)
            {
                foreach (string city in Sources) Populate(session, city, city == current ? 6000 : 0);
                session.Campaign.RefreshMilestones();
            }
            Populate(session, City, 15000);
            Assert.That(state.cities.Where(c => Sources.Contains(c.id)).Sum(c => c.storyResidentMilestone), Is.EqualTo(18000));
            Assert.That(session.Campaign.UnlockProgress(City), Is.EqualTo(6000)); Assert.That(session.Campaign.Offered(City), Is.False);
            Assert.That(state.cities.Single(c => c.id == City).charterResidentMilestone, Is.EqualTo(6000)); SaveValidator.Validate(state, defs);
        }

        [Test] public void OfferSurvivesPopulationDeclineAndSaveRestoreBeforeAcceptance()
        {
            PopulateSources(5000, 5000, 5000); session.Campaign.RefreshMilestones(); PopulateSources(0, 0, 0);
            var restored = Roundtrip(state); var resumed = new GameSession(defs, restored, clock);
            Assert.That(resumed.Campaign.UnlockProgress(City), Is.Zero); Assert.That(resumed.Campaign.Offered(City), Is.True);
            Assert.That(resumed.Campaign.Accept(City), Is.EqualTo(Failure.None)); Assert.That(resumed.Campaign.Travel(City), Is.EqualTo(Failure.None));
            Assert.That(Roundtrip(restored).activeCity, Is.EqualTo(City));
        }

        [TestCase("burning_hills")] [TestCase("big_rock_city")] [TestCase("seven_hills")]
        public void AcceptanceWorksFromEachSourceCityAndPaysOnlyOnce(string current)
        {
            PopulateSources(5000, 5000, 5000); state.activeCity = current; Amount money = state.money; int cards = state.cards;
            var histories = state.cities.Where(c => Sources.Contains(c.id)).Select(c => string.Join(",", c.completedQuests)).ToArray();
            Assert.That(session.TransactCampaign(() => session.Campaign.Accept(City)), Is.EqualTo(Failure.None));
            Assert.That(state.money, Is.EqualTo(money + 3500)); Assert.That(state.cards, Is.EqualTo(cards + 3));
            CollectionAssert.AreEqual(histories, state.cities.Where(c => Sources.Contains(c.id)).Select(c => string.Join(",", c.completedQuests)));
            Assert.That(state.rewardHistory.Count(r => r.source == RewardSource.CityCharter && r.sourceId == City), Is.EqualTo(1));
            string accepted = JsonUtility.ToJson(state);
            Assert.That(session.TransactCampaign(() => session.Campaign.Accept(City)), Is.EqualTo(Failure.NotReady));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(accepted)); SaveValidator.Validate(state, defs);
        }

        [Test] public void SaveFailureRestoresTheThresholdLatchCharterRewardAndTravel()
        {
            PopulateSources(5000, 5000, 5000); var failing = new GameSession(defs, state, clock, new FailingStore());
            state.cities.Single(c => c.id == City).charterResidentMilestone = 0; string before = JsonUtility.ToJson(state);
            Assert.Throws<IOException>(() => failing.TransactCampaign(() => failing.Campaign.Accept(City)));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before)); Assert.That(failing.Campaign.Accessible(City), Is.False);
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.None)); before = JsonUtility.ToJson(state);
            Assert.Throws<IOException>(() => failing.TransactCampaign(() => failing.Campaign.Travel(City)));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
        }

        [Test] public void FinalWorkerAssignmentCapturesTheOfferInsideThePublicTransaction()
        {
            PopulateSources(5000, 5000, 5000); state.activeCity = "seven_hills"; session.Paused = true;
            var caretaker = state.workers.First(w => w.buildingId != null && state.Find(w.buildingId).cityId == "seven_hills");
            string home = caretaker.buildingId; session.Workers.Remove(caretaker.id);
            Assert.That(session.Campaign.Offered(City), Is.False);
            Assert.That(session.Transact(() => session.Workers.Assign(caretaker.id, home, 0)), Is.EqualTo(Failure.None));
            Assert.That(state.cities.Single(c => c.id == City).charterResidentMilestone, Is.EqualTo(15000));
            Assert.That(session.Transact(() => session.Workers.Remove(caretaker.id)), Is.EqualTo(Failure.None));
            Assert.That(session.Campaign.UnlockProgress(City), Is.LessThan(15000));
            Assert.That(new GameSession(defs, Roundtrip(state), clock).Campaign.Offered(City), Is.True);
        }

        [Test] public void VersionFourMigrationAddsOnlyLittleMontanaAndPreservesAllThreeExistingCities()
        {
            var previous = JsonUtility.FromJson<GameDefinitions>(File.ReadAllText(PreviousContent));
            var old = GameState.Create(previous, "Bestehendes Tal", Start); old.introSeen = true;
            var oldSession = new GameSession(previous, old, clock); old.City.storyResidentMilestone = 1000; oldSession.Campaign.Accept("big_rock_city");
            Populate(oldSession, "burning_hills", 3000); Populate(oldSession, "big_rock_city", 2000); oldSession.Campaign.Accept("seven_hills");
            foreach (string city in Sources) Populate(oldSession, city, 5000);
            old.activeCity = "seven_hills"; oldSession.Stories.RefreshUnlocks();
            var mara = oldSession.Stories.Offer("sh_mila");
            Assert.That(oldSession.Stories.Accept("sh_mila", mara.id, mara.choices[0].id), Is.EqualTo(Failure.None));
            string[] cities = old.cities.Select(JsonUtility.ToJson).ToArray(), buildings = old.buildings.Select(JsonUtility.ToJson).ToArray();
            string[] workers = old.workers.Select(JsonUtility.ToJson).ToArray(); Amount money = old.money;
            var loaded = Roundtrip(old);
            Assert.That(loaded.contentVersion, Is.EqualTo(defs.version)); Assert.That(loaded.buildings.Count, Is.EqualTo(defs.instances.Length));
            Assert.That(loaded.activeCity, Is.EqualTo("seven_hills")); Assert.That(loaded.money, Is.EqualTo(money));
            CollectionAssert.AreEqual(cities, loaded.cities.Take(3).Select(JsonUtility.ToJson));
            CollectionAssert.AreEqual(buildings, loaded.buildings.Take(168).Select(JsonUtility.ToJson));
            CollectionAssert.AreEqual(workers, loaded.workers.Select(JsonUtility.ToJson));
            Assert.That(loaded.cities.Single(c => c.id == City).charterAccepted, Is.False);
            Assert.That(new GameSession(defs, loaded, clock).Campaign.Offered(City), Is.True);
        }

        [Test] public void MissingOldBuildingAndForgedFourthCityCharterAreRejected()
        {
            var previous = JsonUtility.FromJson<GameDefinitions>(File.ReadAllText(PreviousContent));
            var old = GameState.Create(previous, "Beschädigtes Tal", Start); old.buildings.RemoveAt(0);
            Assert.Throws<InvalidDataException>(() => Roundtrip(old));
            state.cities.Single(c => c.id == City).charterAccepted = true;
            foreach (var source in state.cities.Where(c => Sources.Contains(c.id))) source.storyResidentMilestone = 15000;
            Assert.Throws<InvalidDataException>(() => SaveValidator.Validate(state, defs));
        }

        [Test] public void SufficientPopulationInBurningHillsOffersMontanaWithoutAnotherCharterAcceptance()
        {
            state = GameState.Create(defs, "Direkter Talauftrag", Start); state.introSeen = true; session = new GameSession(defs, state, clock);
            Populate(session, "burning_hills", 15000); session.Campaign.RefreshMilestones();
            Assert.That(session.Campaign.Offered(City), Is.True); Assert.That(session.Campaign.CanAccept(City), Is.True);
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.None)); Assert.That(session.Campaign.Travel(City), Is.EqualTo(Failure.None));
            Assert.That(session.Campaign.Accessible("big_rock_city"), Is.False); Assert.That(session.Campaign.Accessible("seven_hills"), Is.False);
            SaveValidator.Validate(state, defs);
        }

        [Test] public void ResidentsInAnUnacceptedSourceCityCannotContributeToTheThreshold()
        {
            state = GameState.Create(defs, "Unbestätigter Auftrag", Start); state.introSeen = true; session = new GameSession(defs, state, clock);
            Populate(session, "seven_hills", 15000);
            Assert.That(session.Simulation.Residents("seven_hills"), Is.EqualTo(15000)); Assert.That(session.Campaign.Accessible("seven_hills"), Is.False);
            session.Campaign.RefreshMilestones(); Assert.That(session.Campaign.UnlockProgress(City), Is.Zero);
            Assert.That(session.Campaign.Offered(City), Is.False); SaveValidator.Validate(state, defs);
        }

        [Test] public void FirstThreeCityCatalogsStoriesAndTextsStayUnchanged()
        {
            var previous = JsonUtility.FromJson<GameDefinitions>(File.ReadAllText(PreviousContent));
            CollectionAssert.AreEqual(previous.cities.Select(JsonUtility.ToJson), defs.cities.Take(3).Select(JsonUtility.ToJson));
            CollectionAssert.AreEqual(previous.instances.Select(JsonUtility.ToJson), defs.instances.Where(i => Sources.Contains(i.cityId)).Select(JsonUtility.ToJson));
            var previousRouteIds = previous.routes.Select(r => r.id).ToArray();
            CollectionAssert.AreEqual(previous.routes.Select(JsonUtility.ToJson), defs.routes.Where(r => previousRouteIds.Contains(r.id)).Select(JsonUtility.ToJson));
            CollectionAssert.AreEqual(previous.quests.Select(JsonUtility.ToJson), defs.quests.Where(q => Sources.Contains(q.cityId)).Select(JsonUtility.ToJson));
            CollectionAssert.AreEqual(previous.vipStories.Select(JsonUtility.ToJson), defs.vipStories.Where(p => Sources.Contains(p.cityId)).Select(JsonUtility.ToJson));
            CollectionAssert.AreEqual(previous.vipCollaborations.Select(JsonUtility.ToJson), defs.vipCollaborations.Where(p => Sources.Contains(p.cityId)).Select(JsonUtility.ToJson));
            foreach (var text in previous.texts) Assert.That(defs.Text(text.key), Is.EqualTo(text.de), text.key);
        }

        [Test] public void FourthCityResidentsNeverFeedBackIntoTheirOwnThreshold()
        {
            OwnAndTravel(); PopulateSources(0, 0, 0); Populate(session, City, 15000);
            Assert.That(session.Simulation.Residents(City), Is.EqualTo(15000)); Assert.That(session.Campaign.UnlockProgress(City), Is.Zero);
            Assert.That(session.Campaign.Accessible(City), Is.True); Assert.That(Roundtrip(state).cities.Single(c => c.id == City).charterAccepted, Is.True);
        }

        [Test] public void MaraIsPresentOnArrivalAndEveryOwnedCityKeepsProducing()
        {
            OwnAndTravel(); session.Stories.RefreshUnlocks();
            Assert.That(session.Stories.People.Single(p => p.id == "lm_mara").residents, Is.Zero);
            Assert.That(session.Stories.Offer("lm_mara"), Is.Not.Null); Assert.That(session.Simulation.Residents(City), Is.Zero);
            foreach (string id in new[] { "farm_grow_01", "br_farm_grow_01", "sh_farm_grow_01", "lm_farm_grow_01" }) state.Find(id).level = 25;
            session.Advance(8); Assert.That(state.cities.Where(c => session.Campaign.Accessible(c.id)).All(c => c.stats.rawProduced > 0), Is.True); SaveValidator.Validate(state, defs);
        }

        [Test] public void FirstTwelveFoundationQuestsFinishThroughPublicCommandsWithoutPremium()
        {
            PopulateSources(5000, 5000, 5000);
            // The arrival budget is only the charter grant. No earlier shared wallet or cards fund this foundation.
            state.money = 0; state.cards = 0;
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.None)); Assert.That(session.Campaign.Travel(City), Is.EqualTo(Failure.None));
            session.Stories.RefreshUnlocks(); Assert.That(state.money, Is.EqualTo(new Amount(3500))); Assert.That(state.cards, Is.EqualTo(3));
            int grinders = state.grinders, initialCards = state.cards; Amount initialMoney = state.money;
            var previousHistories = state.cities.Take(3).Select(c => string.Join(",", c.completedQuests)).ToArray();
            var sourceEarnings = state.cities.Where(c => Sources.Contains(c.id)).ToDictionary(c => c.id, c => c.stats.earned);
            var sourceAssignments = state.workers.ToDictionary(w => w.id, w => (w.buildingId, w.slot));
            long firstSecond = state.simulationSeconds, recoveryCheckpoint = state.nextRecoveryWork, rewardSequence = state.rewardSequence;
            int awardedGrinders = 0;
            for (int tick = 0; tick < TutorialDriver.MaxSimulationSeconds && state.City.questIndex < 12; tick++)
            {
                int previousGrinders = state.grinders;
                TutorialDriver.Act(session, clock.UtcNow, allowRecovery: false); clock.UtcNow = clock.UtcNow.AddSeconds(1); session.Advance(1);
                int awards = state.rewardHistory.Where(r => r.sequence > rewardSequence).SelectMany(r => r.items)
                    .Where(item => item.kind == RewardKind.Grinders).Sum(item => checked((int)Math.Round(item.quantity.ToDouble())));
                awardedGrinders += awards; rewardSequence = state.rewardSequence;
                Assert.That(state.grinders, Is.EqualTo(previousGrinders + awards), "Premium currency was spent at " + session.Progression.Current?.id);
            }
            Assert.That(state.City.questIndex, Is.GreaterThanOrEqualTo(12), "Little Montana foundation stalled at " + session.Progression.Current?.id);
            Assert.That(state.grinders, Is.EqualTo(grinders + awardedGrinders)); Assert.That(state.nextRecoveryWork, Is.EqualTo(recoveryCheckpoint));
            CollectionAssert.AreEqual(previousHistories, state.cities.Take(3).Select(c => string.Join(",", c.completedQuests)));
            foreach (var city in state.cities.Where(c => Sources.Contains(c.id))) Assert.That(city.stats.earned, Is.EqualTo(sourceEarnings[city.id]), "Other cities funded the foundation.");
            foreach (var worker in state.workers.Where(w => sourceAssignments.ContainsKey(w.id)))
            {
                Assert.That(worker.buildingId, Is.EqualTo(sourceAssignments[worker.id].buildingId), "A source-city worker was borrowed.");
                Assert.That(worker.slot, Is.EqualTo(sourceAssignments[worker.id].slot));
            }
            SaveValidator.Validate(state, defs); Assert.That(Roundtrip(state).City.questIndex, Is.EqualTo(state.City.questIndex));
            Directory.CreateDirectory("Docs/LittleMontana");
            File.WriteAllText("Docs/LittleMontana/foundation_simulation.json", JsonUtility.ToJson(new FoundationReport {
                utc = DateTime.UtcNow.ToString("O"), status = "passed", city = City,
                contentHash = LocalSaveStore.Hash(File.ReadAllText("Assets/Weedtopia/Game/Content/BurningHills.json")),
                seconds = state.simulationSeconds - firstSecond, quests = state.City.questIndex, quest = session.Progression.Current?.id,
                initialMoney = initialMoney.ToDouble(), finalMoney = state.money.ToDouble(), initialCards = initialCards, finalCards = state.cards,
                initialGrinders = grinders, awardedGrinders = awardedGrinders, finalGrinders = state.grinders,
                noPremiumSpend = state.grinders == grinders + awardedGrinders, noRecovery = state.nextRecoveryWork == recoveryCheckpoint,
                nextRecoveryWork = state.nextRecoveryWork
            }, true));
        }

        [Serializable] private sealed class FoundationReport
        {
            public string utc, status, city, contentHash, quest;
            public long seconds, nextRecoveryWork;
            public int quests, initialGrinders, awardedGrinders, finalGrinders, initialCards, finalCards;
            public double initialMoney, finalMoney;
            public bool noPremiumSpend, noRecovery;
        }

        [TestCase(0)] [TestCase(1)] public void AllResidentChaptersRecurringStoriesAndCollaborationsKeepTheirChoices(int branch)
        {
            OwnAndTravel(); state.City.unlocked = Zone.Industry; state.City.storyResidentMilestone = 10000; session.Stories.RefreshUnlocks();
            foreach (var person in session.Stories.People.ToArray())
                for (int chapter = 0; chapter < person.chapters.Length; chapter++) CompleteStory(person.id, branch);
            foreach (var joint in session.Stories.Collaborations.ToArray()) CompleteStory(joint.id, branch);
            Assert.That(state.City.vipStories.Sum(p => p.completed.Count), Is.EqualTo(28));
            Assert.That(state.City.vipCollaborations.Count(p => p.completed != null), Is.EqualTo(3));
            foreach (var person in session.Stories.People.ToArray())
                for (int repeat = 0; repeat < person.recurring.Length; repeat++)
                {
                    state.simulationSeconds = Math.Max(state.simulationSeconds, session.Stories.Person(person.id).nextRecurringSecond);
                    var offer = session.Stories.Offer(person.id); Assert.That(offer, Is.SameAs(person.recurring[repeat])); CompleteStory(person.id, branch);
                    Assert.That(session.Stories.Offer(person.id), Is.Null, "Recurring cooldown must survive completion.");
                }
            Assert.That(state.City.vipStories.Sum(p => p.recurringCompleted), Is.EqualTo(21));
            Assert.That(JsonUtility.ToJson(Roundtrip(state).City), Is.EqualTo(JsonUtility.ToJson(state.City)));
        }

        private void CompleteStory(string id, int branch)
        {
            var offer = session.Stories.Offer(id); Assert.That(offer, Is.Not.Null, id);
            Assert.That(session.Stories.Accept(id, offer.id, offer.choices[branch % offer.choices.Length].id), Is.EqualTo(Failure.None));
            var active = session.Stories.Active(id);
            if (offer.goals.Any(g => g.IsCounter)) Assert.That(session.Stories.Ready(id), Is.False, "Fresh counters must require new progress: " + offer.id);
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
                    default: Assert.Fail("Unsupported fixture goal " + goal.kind); break;
                }
            }
            Assert.That(session.Stories.Claim(id, offer.id), Is.EqualTo(Failure.None), offer.id); SaveValidator.Validate(state, defs);
        }

        private void OwnAndTravel()
        {
            PopulateSources(5000, 5000, 5000); Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.None));
            Assert.That(session.Campaign.Travel(City), Is.EqualTo(Failure.None)); session.Stories.RefreshUnlocks();
        }
        private void PopulateSources(int burning, int rock, int seven)
        { Populate(session, Sources[0], burning); Populate(session, Sources[1], rock); Populate(session, Sources[2], seven); }
        private static void Populate(GameSession game, string city, int residents)
        {
            var plots = game.State.buildings.Where(b => b.cityId == city && (b.family == "settlement_plot" || b.family == "residential_house")).ToArray();
            foreach (var plot in plots) { plot.level = 0; game.State.workers.RemoveAll(w => w.buildingId == plot.id); }
            if (residents == 0) return;
            var choices = new Dictionary<int, (int level, int workerLevel, int rank)>();
            for (int tier = 1; tier < VisualTiers.Starts.Length; tier++)
                for (int level = 1; level <= 100; level++) for (int rank = 1; rank <= 10; rank++)
                {
                    int count = (int)Math.Floor(game.Definitions.Building("residential_house").ResidentCapacity(VisualTiers.Starts[tier]) *
                        (1 + game.Definitions.workerAttributeStep * level * rank));
                    if (!choices.ContainsKey(count)) choices[count] = (VisualTiers.Starts[tier], level, rank);
                }
            var parts = new List<int>(); int remaining = residents;
            while (remaining >= 5000) { parts.Add(5000); remaining -= 5000; }
            if (remaining > 0)
            {
                if (choices.ContainsKey(remaining)) parts.Add(remaining);
                else
                {
                    int first = choices.Keys.First(n => n < remaining && choices.ContainsKey(remaining - n));
                    parts.Add(first); parts.Add(remaining - first);
                }
            }
            Assert.That(parts.Count, Is.LessThanOrEqualTo(plots.Length));
            for (int index = 0; index < parts.Count; index++)
            {
                var configuration = choices[parts[index]]; var building = plots[index]; building.family = "residential_house"; building.level = configuration.level;
                game.State.workers.Add(new WorkerState { id = "montana_fixture_" + building.id, role = "caretaker", level = configuration.workerLevel,
                    rank = configuration.rank, buildingId = building.id, slot = 0, attributes = new List<WorkerAttribute> { WorkerAttribute.Residents } });
            }
            Assert.That(game.Simulation.Residents(city), Is.EqualTo(residents));
        }
        private GameState Roundtrip(GameState source)
        {
            string payload = JsonUtility.ToJson(source);
            return new LocalSaveStore(Path.Combine("Temp", "LittleMontanaUnused.json"), defs).Decode(
                JsonUtility.ToJson(new SaveEnvelope { payload = payload, sha256 = LocalSaveStore.Hash(payload) }));
        }
        private sealed class FailingStore : ISaveStore
        { public bool Exists => false; public void Save(GameState game) => throw new IOException("Disk failure"); public SaveLoadResult Load() => new SaveLoadResult(); }
    }
}

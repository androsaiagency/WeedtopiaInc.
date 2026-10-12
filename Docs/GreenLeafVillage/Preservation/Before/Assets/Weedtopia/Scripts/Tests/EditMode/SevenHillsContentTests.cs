using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Persistence;

namespace Weedtopia.Tests
{
    public sealed class SevenHillsContentTests
    {
        private const string City = "seven_hills";
        private static readonly DateTime Start = new DateTime(2026, 10, 7, 10, 0, 0, DateTimeKind.Utc);
        private GameDefinitions defs;
        private GameState state;
        private GameSession session;

        [SetUp] public void Setup()
        {
            defs = ProgressionTests.Definitions();
            state = GameState.Create(defs, "Seven Hills Content", Start);
            state.introSeen = true;
            session = new GameSession(defs, state, new ManualClock(Start));
        }

        // Charter population itself is tested independently with real houses by SevenHillsCampaignTests.
        private void Visit(int milestone = 5000)
        {
            state.cities.Single(c => c.id == City).charterResidentMilestone = 5000;
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.None));
            Assert.That(session.Campaign.Travel(City), Is.EqualTo(Failure.None));
            state.City.unlocked = Zone.Industry;
            state.City.storyResidentMilestone = milestone;
            session.Stories.RefreshUnlocks();
        }

        [Test] public void ThirdCityRetainsEstablishedBuildingAndDeliveryContracts()
        {
            defs.Validate();
            var buildings = defs.instances.Where(i => i.cityId == City).ToArray();
            var oldBuildings = defs.instances.Where(i => i.cityId == "burning_hills").ToArray();
            Assert.That(buildings.Length, Is.EqualTo(56));
            CollectionAssert.AreEquivalent(oldBuildings.Select(i => i.family), buildings.Select(i => i.family));
            var ids = buildings.Select(i => i.id).ToArray();
            var routes = defs.routes.Where(r => ids.Contains(r.from)).ToArray();
            Assert.That(routes.Length, Is.EqualTo(35));
            Assert.That(routes.All(r => ids.Contains(r.to)), Is.True);
            Assert.That(defs.QuestsFor(City).Length, Is.EqualTo(18));
            Assert.That(defs.QuestsFor("burning_hills").Length, Is.EqualTo(16));
            Assert.That(defs.QuestsFor("big_rock_city").Length, Is.EqualTo(16));
            Assert.That(defs.vipStories.Count(p => p.cityId == City), Is.EqualTo(7));
            Assert.That(defs.vipStories.Where(p => p.cityId == City).Sum(p => p.chapters.Length), Is.EqualTo(28));
            Assert.That(defs.vipStories.Where(p => p.cityId == City).Sum(p => p.recurring.Length), Is.EqualTo(21));
            Assert.That(defs.vipCollaborations.Count(p => p.cityId == City), Is.EqualTo(3));
        }

        [Test] public void MainChaptersHaveExactPopulationGatesAndNeverSkip()
        {
            Visit();
            var gated = defs.QuestsFor(City).Where(q => q.conditions.Any(c => c.kind == ConditionKind.Residents && c.count > 1)).ToArray();
            CollectionAssert.AreEqual(new[] { 250, 750, 1500, 3000, 5000, 5000 },
                gated.Select(q => q.conditions.Single(c => c.kind == ConditionKind.Residents).count));
            Assert.That(session.Progression.Current.id, Is.EqualTo("sh_arrival"));
            state.City.storyResidentMilestone = 10000;
            Assert.That(session.Progression.Current.id, Is.EqualTo("sh_arrival"));
            Assert.That(state.City.freeplay, Is.False);
        }

        [Test] public void FoundationReachesLocalSupplyUsingPublicCommandsWithoutRecoveryOrPremium()
        {
            state.cities.Single(c => c.id == City).charterResidentMilestone = 5000;
            Assert.That(session.Campaign.Accept(City), Is.EqualTo(Failure.None));
            Assert.That(session.Campaign.Travel(City), Is.EqualTo(Failure.None));
            Assert.That(state.City.unlocked, Is.EqualTo(Zone.Farm));
            int initialGrinders = state.grinders;
            long initialRewardSequence = state.rewardSequence;
            var clock = new ManualClock(Start);
            int seconds = 0;
            for (; seconds < TutorialDriver.MaxSimulationSeconds && state.City.questIndex < 12; seconds++)
            {
                TutorialDriver.Act(session, clock.UtcNow, allowRecovery: false);
                clock.UtcNow = clock.UtcNow.AddSeconds(1);
                session.Advance(1);
            }
            var newReceipts = state.rewardHistory.Where(r => r.sequence > initialRewardSequence).ToArray();
            Assert.That(newReceipts, Is.Not.Empty);
            Assert.That(newReceipts[0].sequence, Is.EqualTo(initialRewardSequence + 1), "The no-spend proof needs the complete reward ledger");
            Amount grinderAwards = newReceipts.SelectMany(r => r.items).Where(i => i.kind == RewardKind.Grinders)
                .Aggregate(Amount.Zero, (sum, item) => sum + item.quantity);
            int awardedGrinders = checked((int)grinderAwards.ToDouble());
            Assert.That(grinderAwards, Is.EqualTo(new Amount(awardedGrinders)), "Grinder grants must be integral");
            int spentGrinders = checked(initialGrinders + awardedGrinders - state.grinders);
            Directory.CreateDirectory("Docs/SevenHills");
            File.WriteAllText("Docs/SevenHills/foundation_simulation.json", JsonUtility.ToJson(new FoundationResult
            {
                simulationSeconds = seconds, completedQuests = state.City.questIndex,
                quest = session.Progression.Current?.id, money = state.money.Serialize(),
                grinders = state.grinders, initialGrinders = initialGrinders, finalGrinders = state.grinders,
                awardedGrinders = awardedGrinders, spentGrinders = spentGrinders,
                recoveryCheckpoint = state.nextRecoveryWork, noPremiumSpend = spentGrinders == 0,
                noRecoveryUsed = state.nextRecoveryWork == 0, freeplay = state.City.freeplay
            }, true));
            Assert.That(state.City.questIndex, Is.EqualTo(12), "Foundation stalled at " + session.Progression.Current?.id);
            Assert.That(session.Progression.Current.id, Is.EqualTo("sh_morning_courtyard"));
            Assert.That(state.City.stats.butterProduced > 0 && state.City.stats.pastrySold > 0, Is.True);
            // None of the first twelve main quests grants direct premium currency. The newly
            // restored level-one supermarket grants exactly one grinder through its first daily claim.
            Assert.That(newReceipts.Where(r => r.items.Any(i => i.kind == RewardKind.Grinders)).All(r => r.source == RewardSource.Daily), Is.True);
            Assert.That(awardedGrinders, Is.EqualTo(1));
            Assert.That(state.grinders, Is.EqualTo(initialGrinders + awardedGrinders));
            Assert.That(spentGrinders, Is.Zero);
            Assert.That(state.nextRecoveryWork, Is.Zero);
            Assert.That(state.City.freeplay, Is.False);
            SaveValidator.Validate(state, defs);
        }

        [Serializable] private sealed class FoundationResult
        {
            public int simulationSeconds, completedQuests, grinders, initialGrinders, finalGrinders, awardedGrinders, spentGrinders;
            public long recoveryCheckpoint;
            public string quest, money;
            public bool freeplay, noPremiumSpend, noRecoveryUsed;
        }

        [Test] public void MilaAppearsUponArrivalBeforeAnyNewPopulation()
        {
            Visit(0);
            Assert.That(session.Simulation.Residents(City), Is.Zero);
            Assert.That(session.Stories.Unlocked("sh_mila"), Is.True);
            Assert.That(session.Stories.Offer("sh_mila").id, Is.EqualTo("sh_mila_chapter_1"));
            Assert.That(session.Stories.Unlocked("sh_tariq"), Is.False);
        }

        [TestCase("sh_tariq", 50)]
        [TestCase("sh_juna", 150)]
        [TestCase("sh_emil", 300)]
        [TestCase("sh_safiya", 500)]
        [TestCase("sh_clara", 800)]
        [TestCase("sh_anton", 1200)]
        public void ResidentsAppearAtTheirAuthoredMilestone(string id, int population)
        {
            Visit(population - 1);
            Assert.That(session.Stories.Unlocked(id), Is.False);
            state.City.storyResidentMilestone = population;
            session.Stories.RefreshUnlocks();
            Assert.That(session.Stories.Unlocked(id), Is.True);
            Assert.That(session.Stories.Offer(id).id, Is.EqualTo(id + "_chapter_1"));
        }

        [Test] public void EveryPersonalChapterAndCommunityProjectRequiresFreshActivityAndPersists()
        {
            Visit();
            var original = state.buildings.Where(b => b.cityId != City).Select(JsonUtility.ToJson).ToArray();
            foreach (var person in session.Stories.People.ToArray())
                for (int index = 0; index < 4; index++) Complete(person.id, index % person.chapters[index].choices.Length);
            foreach (var project in session.Stories.Collaborations.ToArray()) Complete(project.id);
            Assert.That(state.City.vipStories.Sum(p => p.completed.Count), Is.EqualTo(28));
            Assert.That(state.City.vipCollaborations.Count(p => p.completed != null), Is.EqualTo(3));
            Assert.That(state.City.vipStories.Single(p => p.id == "sh_safiya").completed[2].choiceId, Is.EqualTo("quiet"));
            CollectionAssert.AreEqual(original, state.buildings.Where(b => b.cityId != City).Select(JsonUtility.ToJson));
            var restored = Roundtrip();
            Assert.That(JsonUtility.ToJson(restored.City), Is.EqualTo(JsonUtility.ToJson(state.City)));
            Assert.That(restored.rewardSequence, Is.EqualTo(state.rewardSequence));
        }

        [Test] public void RecurringPoolsRotateAllTwentyOneVariantsAndWaitWithoutExpiring()
        {
            Visit();
            foreach (var person in session.Stories.People.ToArray())
            {
                for (int chapter = 0; chapter < 4; chapter++) Complete(person.id);
                for (int repeat = 0; repeat < 3; repeat++)
                {
                    Assert.That(session.Stories.Offer(person.id).id, Is.EqualTo(person.id + "_repeat_" + (repeat + 1)));
                    Complete(person.id);
                    Assert.That(session.Stories.Offer(person.id), Is.Null);
                    state.simulationSeconds += person.repeatDelaySeconds;
                }
                Assert.That(session.Stories.Person(person.id).recurringCompleted, Is.EqualTo(3));
                Assert.That(session.Stories.Offer(person.id).id, Is.EqualTo(person.id + "_repeat_1"));
                var offer = session.Stories.Offer(person.id);
                Assert.That(session.Stories.Accept(person.id, offer.id, offer.choices[0].id), Is.EqualTo(Failure.None));
                state.simulationSeconds += 86400;
                Assert.That(session.Stories.Active(person.id).questId, Is.EqualTo(offer.id));
                Satisfy(person.id);
                Assert.That(session.Stories.Claim(person.id, offer.id), Is.EqualTo(Failure.None));
            }
            SaveValidator.Validate(state, defs);
        }

        [Test] public void PanoramaRingWaitsForAllSevenFirstChaptersAndSavedChoices()
        {
            Visit();
            Assert.That(session.Stories.Offer("sh_joint_panorama_ring"), Is.Null);
            foreach (var person in session.Stories.People.Where(p => p.id != "sh_anton").ToArray()) Complete(person.id);
            Assert.That(session.Stories.Offer("sh_joint_panorama_ring"), Is.Null);
            Complete("sh_anton");
            var offer = session.Stories.Offer("sh_joint_panorama_ring");
            Assert.That(offer, Is.Not.Null);
            Assert.That(offer.residents, Is.EqualTo(5000));
            Assert.That(session.Stories.Accept("sh_joint_panorama_ring", offer.id, "together"), Is.EqualTo(Failure.None));
            var restored = Roundtrip();
            var resumed = new GameSession(defs, restored, new ManualClock(Start));
            Assert.That(resumed.Stories.Active("sh_joint_panorama_ring").choiceId, Is.EqualTo("together"));
            Assert.That(resumed.Stories.Progress("sh_joint_panorama_ring", 0), Is.EqualTo(Amount.Zero));
        }

        [Test] public void EveryNewTextExistsInAllFiveLanguagesWithIdenticalPlaceholders()
        {
            string[] languages = { "de", "en", "es", "fr", "ru" };
            var documents = languages.Select(lang => JsonUtility.FromJson<LocaleDocument>(
                File.ReadAllText("Assets/Weedtopia/Game/Localization/" + lang + ".json"))).ToArray();
            var registry = new[] { new LanguageDefinition("de", "Deutsch", "de-DE"), new LanguageDefinition("en", "English", "en-GB"),
                new LanguageDefinition("es", "Español", "es-ES"), new LanguageDefinition("fr", "Français", "fr-FR"), new LanguageDefinition("ru", "Русский", "ru-RU") };
            var localization = new LocalizationService(registry, documents);
            var keys = defs.texts.Where(t => t.key.StartsWith("sh.") || t.key.StartsWith("district.seven_hills.") ||
                t.key.StartsWith("campaign.seven_hills") || t.key == "city.seven_hills" || t.key == "campaign.combined_locked" ||
                t.key == "campaign.view_offer").Select(t => t.key).ToArray();
            Assert.That(keys.Length, Is.GreaterThanOrEqualTo(226));
            foreach (var document in documents)
            {
                localization.Select(document.language);
                foreach (string key in keys)
                {
                    Assert.That(document.entries.Any(entry => entry.key == key && !string.IsNullOrWhiteSpace(entry.value)), Is.True, document.language + ":" + key);
                    Assert.That(localization.Get(key), Does.Not.StartWith("["), key);
                }
                Assert.DoesNotThrow(() => localization.Format("campaign.combined_locked", 5000, 4999));
                foreach (string key in keys.Where(key => key.StartsWith("sh.goal.")))
                    Assert.DoesNotThrow(() => localization.Format(key, "Building", new Amount(4), 12));
            }
        }

        private void Complete(string id, int choice = 0)
        {
            var offer = session.Stories.Offer(id);
            Assert.That(offer, Is.Not.Null, id);
            Assert.That(session.Stories.Accept(id, offer.id, offer.choices[choice].id), Is.EqualTo(Failure.None));
            Assert.That(session.Stories.Ready(id), Is.False, "Already produced goods must not satisfy " + offer.id);
            for (int i = 0; i < offer.goals.Length; i++)
                if (offer.goals[i].IsCounter) Assert.That(session.Stories.Progress(id, i), Is.EqualTo(Amount.Zero));
            Satisfy(id);
            Assert.That(session.Stories.Claim(id, offer.id), Is.EqualTo(Failure.None), offer.id);
            string snapshot = JsonUtility.ToJson(state);
            Assert.That(session.Stories.Claim(id, offer.id), Is.EqualTo(Failure.NotReady));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(snapshot), "A repeated claim must not pay again");
            SaveValidator.Validate(state, defs);
        }

        private void Satisfy(string id)
        {
            var quest = session.Stories.ActiveDefinition(id);
            var active = session.Stories.Active(id);
            for (int i = 0; i < quest.goals.Length; i++)
            {
                var goal = quest.goals[i];
                int count = active.targets[i];
                switch (goal.kind)
                {
                    case StoryGoalKind.Level:
                        var building = state.buildings.FirstOrDefault(b => b.cityId == City && (b.id == goal.target || b.family == goal.target));
                        if (building == null) { building = state.buildings.First(b => b.cityId == City && b.family == "settlement_plot"); building.family = goal.target; }
                        building.level = Math.Max(building.level, count); break;
                    case StoryGoalKind.RawProduced: state.City.stats.rawProduced += count; break;
                    case StoryGoalKind.RefinedProduced: state.City.stats.refinedProduced += count; break;
                    case StoryGoalKind.CoffeeSold: state.City.stats.coffeeSold += count; break;
                    case StoryGoalKind.ButterProduced: state.City.stats.butterProduced += count; break;
                    case StoryGoalKind.PastrySold: state.City.stats.pastrySold += count; break;
                    case StoryGoalKind.DeliveredGoods: state.City.stats.delivered += count; break;
                    case StoryGoalKind.Deliveries: state.City.stats.deliveries += count; break;
                    default: Assert.Fail("Unexpected Seven Hills goal " + goal.kind); break;
                }
            }
            Assert.That(session.Stories.Ready(id), Is.True, quest.id);
        }

        private GameState Roundtrip()
        {
            string payload = JsonUtility.ToJson(state);
            return new LocalSaveStore(Path.Combine("Temp", "SevenHillsContentUnused.json"), defs).Decode(
                JsonUtility.ToJson(new SaveEnvelope { payload = payload, sha256 = LocalSaveStore.Hash(payload) }));
        }
    }
}

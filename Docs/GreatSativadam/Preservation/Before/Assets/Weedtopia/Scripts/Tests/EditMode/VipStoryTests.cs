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
    public sealed class VipStoryTests
    {
        private GameDefinitions defs;
        private GameState state;
        private GameSession session;
        private ManualClock clock;
        private static DateTime Start => new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        [SetUp] public void Setup()
        {
            defs = ProgressionTests.Definitions();
            defs.vipStories=defs.vipStories.Where(p=>p.cityId=="burning_hills").ToArray();
            defs.vipCollaborations=defs.vipCollaborations.Where(p=>p.cityId=="burning_hills").ToArray();
            state = GameState.Create(defs, "VIP Test", Start); state.introSeen = true;
            state.City.unlocked = Zone.Industry; clock = new ManualClock(Start); session = new GameSession(defs, state, clock);
        }
        private void Milestone(int population) { state.City.storyResidentMilestone = population; session.Stories.RefreshUnlocks(); }
        private void Accept(string id, int choice = 0)
        {
            var quest = session.Stories.Offer(id); Assert.That(quest, Is.Not.Null, id);
            Assert.That(session.Stories.Accept(id, quest.id, quest.choices[choice].id), Is.EqualTo(Failure.None));
        }
        private void Ready(string id)
        {
            var q = session.Stories.ActiveDefinition(id); var active = session.Stories.Active(id);
            for (int i = 0; i < q.goals.Length; i++)
            {
                var goal = q.goals[i]; int n = active.targets[i];
                switch (goal.kind)
                {
                    case StoryGoalKind.NpcVisited: Assert.That(session.Stories.Visit(goal.target), Is.EqualTo(Failure.None)); break;
                    case StoryGoalKind.Deliveries: state.City.stats.deliveries += n; break;
                    case StoryGoalKind.DeliveredGoods: state.City.stats.delivered += n; break;
                    case StoryGoalKind.RawProduced: state.City.stats.rawProduced += n; break;
                    case StoryGoalKind.RefinedProduced: state.City.stats.refinedProduced += n; break;
                    case StoryGoalKind.CoffeeSold: state.City.stats.coffeeSold += n; break;
                    case StoryGoalKind.ButterProduced: state.City.stats.butterProduced += n; break;
                    case StoryGoalKind.PastrySold: state.City.stats.pastrySold += n; break;
                    case StoryGoalKind.Level:
                        var building = state.buildings.FirstOrDefault(b => b.id == goal.target || b.family == goal.target);
                        if (building == null) { building = state.buildings.First(b => b.family == "settlement_plot"); building.family = goal.target; }
                        building.level = Math.Max(building.level, n); break;
                }
            }
            Assert.That(session.Stories.Ready(id), Is.True, id);
        }
        private void Complete(string id, int choice = 0)
        { Accept(id, choice); Ready(id); Assert.That(session.Stories.Claim(id, session.Stories.Active(id).questId), Is.EqualTo(Failure.None)); }
        private GameState RoundTrip()
        {
            string payload = JsonUtility.ToJson(state);
            return new LocalSaveStore(Path.Combine("Temp", "VIPUnused.json"), defs).Decode(JsonUtility.ToJson(new SaveEnvelope { payload = payload, sha256 = LocalSaveStore.Hash(payload) }));
        }
        [Test] public void AllApprovedCharactersChaptersAndRecurringPoolsValidate()
        {
            defs.Validate(); Assert.That(defs.vipStories.Length, Is.EqualTo(10));
            Assert.That(defs.vipStories.Sum(p => p.chapters.Length), Is.EqualTo(40));
            Assert.That(defs.vipStories.Sum(p => p.recurring.Length), Is.EqualTo(30)); Assert.That(defs.vipCollaborations.Length, Is.EqualTo(5));
        }
        [TestCase("toni",50)][TestCase("juno",100)][TestCase("rosa",200)][TestCase("mira",350)][TestCase("kalle",500)]
        [TestCase("alma",750)][TestCase("selma",1000)][TestCase("oskar",1500)][TestCase("dalia",2000)][TestCase("valentina",3000)]
        public void InhabitantsUnlockAtTheirExactSavedPopulationMilestone(string id, int population)
        {
            Milestone(population - 1); Assert.That(session.Stories.Unlocked(id), Is.False);
            Milestone(population); Assert.That(session.Stories.Unlocked(id), Is.True);
            Assert.That(session.Stories.Offer(id).id, Is.EqualTo(id + "_chapter_1"));
        }
        [Test] public void LiveResidentsRaiseTheMilestoneAndRemovalOfAWorkerDoesNotRemoveAnInhabitant()
        {
            var house = state.Find("settlement_01"); house.family = "residential_house"; house.level = 1;
            var worker = session.Workers.Grant("caretaker", 4); worker.level = 80; session.Workers.Assign(worker.id, house.id, 0);
            Assert.That(session.Simulation.Residents(state.activeCity), Is.EqualTo(50)); session.Stories.RefreshUnlocks();
            Assert.That(session.Stories.Unlocked("toni"), Is.True); session.Workers.Remove(worker.id); session.Stories.RefreshUnlocks();
            Assert.That(session.Simulation.Residents(state.activeCity), Is.EqualTo(10)); Assert.That(state.City.storyResidentMilestone, Is.EqualTo(50));
            Assert.That(session.Stories.Unlocked("toni"), Is.True); SaveValidator.Validate(state, defs);
        }
        [Test] public void HighestChapterIsReachableWithExistingHousingAndWorkerRules()
        {
            var plots = state.buildings.Where(b => b.family == "settlement_plot").ToArray();
            for (int i = 0; i < plots.Length - 1; i++)
            {
                plots[i].family = "residential_house"; plots[i].level = 5000;
                var worker = session.Workers.Grant("caretaker", 4); worker.level = 100; worker.rank = 10; session.Workers.Assign(worker.id, plots[i].id, 0);
            }
            session.Stories.RefreshUnlocks();
            Assert.That(session.Simulation.Residents(state.activeCity), Is.GreaterThanOrEqualTo(VipStoryRules.Quests(defs).Max(q => q.residents)));
            Assert.That(state.City.vipStories.Count, Is.EqualTo(10)); SaveValidator.Validate(state, defs);
        }
        [Test] public void PopulationGrowthNeverSkipsChaptersOrPaysEarlierActivityAgain()
        {
            Milestone(10000); state.City.stats.deliveries = 900; state.City.stats.delivered = 100000;
            Accept("toni"); Assert.That(session.Stories.Progress("toni", 0), Is.EqualTo(Amount.Zero));
            Assert.That(session.Stories.Claim("toni", "toni_chapter_1"), Is.EqualTo(Failure.NotReady));
            Ready("toni"); Amount money = state.money; session.Stories.Claim("toni", "toni_chapter_1");
            Assert.That(state.money, Is.EqualTo(money + 500)); Assert.That(session.Stories.Offer("toni").id, Is.EqualTo("toni_chapter_2"));
            string before = JsonUtility.ToJson(state); Assert.That(session.Stories.Claim("toni", "toni_chapter_1"), Is.EqualTo(Failure.NotReady));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
        }
        [Test] public void ThreeActiveQuestsAreAllowedAndRejectedAcceptsHaveNoSideEffects()
        {
            Milestone(10000); foreach (string id in new[] { "toni", "juno", "rosa" }) Accept(id);
            string before = JsonUtility.ToJson(state); var offer = session.Stories.Offer("mira");
            Assert.That(session.Stories.Accept("mira", offer.id, offer.choices[0].id), Is.EqualTo(Failure.Busy));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Ready("toni"); session.Stories.Claim("toni", "toni_chapter_1"); Accept("mira"); Assert.That(session.Stories.ActiveCount, Is.EqualTo(3));
        }
        [Test] public void ChoicesBaselinesAndMemoriesSurviveSavingWithoutNewRewards()
        {
            Milestone(10000); Complete("toni"); Accept("toni", 1); state.City.stats.delivered += 12;
            var loaded = RoundTrip(); var resumed = new GameSession(defs, loaded, clock);
            Assert.That(resumed.Stories.Active("toni").choiceId, Is.EqualTo("honest"));
            Assert.That(resumed.Stories.Progress("toni", 0), Is.EqualTo(new Amount(12)));
            Assert.That(loaded.rewardSequence, Is.EqualTo(state.rewardSequence)); Assert.That(loaded.money, Is.EqualTo(state.money));
            Assert.That(loaded.City.vipStories.Single(p => p.id == "toni").completed.Single().questId, Is.EqualTo("toni_chapter_1"));
        }
        [Test] public void RecurringQuestsRotateScaleAndWaitWithoutExpiring()
        {
            Milestone(200); Complete("toni");
            var first = session.Stories.Offer("toni"); Assert.That(first.id, Is.EqualTo("toni_recurring_1")); Complete("toni");
            Assert.That(session.Stories.Offer("toni"), Is.Null); state.simulationSeconds += 300;
            Assert.That(session.Stories.Offer("toni").id, Is.EqualTo("toni_recurring_2")); Complete("toni"); state.simulationSeconds += 300;
            Milestone(1500); Accept("toni"); // Story chapter two has priority at the new milestone.
            Assert.That(session.Stories.Active("toni").questId, Is.EqualTo("toni_chapter_2"));
            state.simulationSeconds += 100000; var loaded = RoundTrip();
            Assert.That(loaded.City.vipStories.Single(p => p.id == "toni").active.questId, Is.EqualTo("toni_chapter_2"));
        }
        [Test] public void AllFortyPersonalChaptersAndFiveCollaborationsCompleteAndRemainSaved()
        {
            Milestone(10000);
            foreach (var person in defs.vipStories) for (int i = 0; i < person.chapters.Length; i++) { Complete(person.id, i % person.chapters[i].choices.Length); SaveValidator.Validate(state, defs); }
            foreach (var joint in defs.vipCollaborations) { Complete(joint.id); SaveValidator.Validate(state, defs); }
            Assert.That(state.City.vipStories.Sum(p => p.completed.Count), Is.EqualTo(40));
            Assert.That(state.City.vipCollaborations.Count(p => p.completed != null), Is.EqualTo(5));
            Assert.That(state.rewardHistory.Count(r => r.source == RewardSource.VipStory), Is.EqualTo(45));
            Assert.That(state.rewardHistory.Sum(r => r.items.Count(i => i.kind == RewardKind.StoryMemory)), Is.EqualTo(45));
            var loaded = RoundTrip(); Assert.That(JsonUtility.ToJson(loaded), Is.EqualTo(JsonUtility.ToJson(state)));
            var resumed = new GameSession(defs, loaded, clock);
            foreach (var person in defs.vipStories)
            {
                var offer = resumed.Stories.Offer(person.id); Assert.That(offer, Is.Not.Null);
                Assert.That(person.recurring.Contains(offer), Is.True); resumed.Stories.Accept(person.id, offer.id, offer.choices[0].id);
                Assert.That(resumed.Stories.Active(person.id)?.scale ?? 10, Is.EqualTo(10));
                if (resumed.Stories.ActiveCount == 3) break;
            }
        }
        [Test] public void CollaborationsRequireTheActualEarlierChaptersAndCannotPayTwice()
        {
            Milestone(10000); Assert.That(session.Stories.Offer("joint_lantern_evening"), Is.Null);
            foreach (string id in new[] { "juno", "rosa", "toni" }) Complete(id);
            Complete("joint_lantern_evening"); string before = JsonUtility.ToJson(state);
            Assert.That(session.Stories.Claim("joint_lantern_evening", "joint_lantern_evening"), Is.EqualTo(Failure.NotReady));
            Assert.That(session.Stories.Offer("joint_lantern_evening"), Is.Null); Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
        }
        [TestCase("id")][TestCase("id_empty")][TestCase("choice")][TestCase("baseline")][TestCase("history")][TestCase("scale")][TestCase("time")]
        public void CorruptProgressIsRejected(string field)
        {
            Milestone(10000); Accept("toni"); var progress = session.Stories.Person("toni");
            switch (field)
            {
                case "id": progress.active.questId = "missing"; break;
                case "id_empty": progress.active.questId = ""; break;
                case "choice": progress.active.choiceId = "missing"; break;
                case "baseline": progress.active.baselines[0] = 100; break;
                case "history": progress.completed.Add(new StoryCompletion { questId = "toni_chapter_4", choiceId = "start" }); break;
                case "scale": progress.active.scale = 4; break;
                case "time": progress.active.acceptedSecond = 100; break;
            }
            Assert.Throws<InvalidDataException>(() => SaveValidator.Validate(state, defs));
            Assert.Throws<InvalidDataException>(() => RoundTrip());
        }
        [Test] public void LegacySaveWithoutStoryFieldsKeepsExistingProgressAndResources()
        {
            state.money = 43210; state.grinders = 37; state.Find("farm_grow_01").level = 10;
            string payload = JsonUtility.ToJson(state).Replace(",\"storyResidentMilestone\":0", "").Replace(",\"vipStories\":[]", "").Replace(",\"vipCollaborations\":[]", "");
            var store = new LocalSaveStore(Path.Combine("Temp", "VIPUnused.json"), defs);
            var loaded = store.Decode(JsonUtility.ToJson(new SaveEnvelope { payload = payload, sha256 = LocalSaveStore.Hash(payload) }));
            Assert.That(loaded.money, Is.EqualTo(state.money)); Assert.That(loaded.grinders, Is.EqualTo(37)); Assert.That(loaded.Find("farm_grow_01").level, Is.EqualTo(10));
            Assert.That(new GameSession(defs, loaded, clock).Stories.Unlocked("toni"), Is.False);
        }
        private sealed class FailingStore : ISaveStore
        {
            public bool Fail; public bool Exists => false;
            public void Save(GameState value) { if (Fail) throw new IOException("Test disk failure"); }
            public SaveLoadResult Load() => new SaveLoadResult();
        }
        [Test] public void FailedDiskWritesRestoreAcceptedQuestsAndAllClaimedRewards()
        {
            Milestone(10000); var store = new FailingStore(); session = new GameSession(defs, state, clock, store);
            var offer = session.Stories.Offer("toni"); string before = JsonUtility.ToJson(state); store.Fail = true;
            Assert.Throws<IOException>(() => session.TransactStory(() => session.Stories.Accept("toni", offer.id, offer.choices[0].id)));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            store.Fail = false; session.TransactStory(() => session.Stories.Accept("toni", offer.id, offer.choices[0].id)); Ready("toni");
            before = JsonUtility.ToJson(state); store.Fail = true;
            Assert.Throws<IOException>(() => session.TransactStory(() => session.Stories.Claim("toni", offer.id)));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
        }
        [Test] public void OfflineSimulationAdvancesAcceptedCountersButNeverAutomaticallyAwardsTheQuest()
        {
            Milestone(350); state.Find("farm_grow_01").level = 250;
            var park = state.Find("settlement_01"); park.family = "park"; park.level = 1;
            Accept("mira"); var baseline = session.Stories.Active("mira").baselines[0];
            var loaded = RoundTrip(); var resumed = new GameSession(defs, loaded, new ManualClock(Start.AddSeconds(60)));
            long receipts = loaded.rewardSequence; var offline = resumed.ApplyOffline();
            Assert.That(offline.Seconds, Is.GreaterThan(0));
            Assert.That(resumed.Stories.Active("mira").baselines[0], Is.EqualTo(baseline));
            Assert.That(resumed.Stories.Progress("mira", 0), Is.EqualTo(new Amount(80)));
            Assert.That(resumed.Stories.Ready("mira"), Is.True);
            Assert.That(resumed.Stories.Active("mira"), Is.Not.Null); Assert.That(loaded.rewardSequence, Is.EqualTo(receipts));
            Assert.That(resumed.Stories.Claim("mira", "mira_chapter_1"), Is.EqualTo(Failure.None));
            Assert.That(loaded.rewardSequence, Is.EqualTo(receipts + 1));
            Assert.That(resumed.Stories.Claim("mira", "mira_chapter_1"), Is.EqualTo(Failure.NotReady));
        }
    }
}

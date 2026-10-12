using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Weedtopia.Application;
using Weedtopia.Domain;

namespace Weedtopia.Tests
{
    public sealed class SessionPersistenceTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);
        private GameDefinitions defs;
        private GameState state;
        private ManualClock clock;
        private ControlledStore store;
        private GameSession session;

        [SetUp] public void Setup()
        {
            defs = ProgressionTests.Definitions();
            state = GameState.Create(defs, "Speichersicherheit", Start);
            state.introSeen = true;
            clock = new ManualClock(Start);
            store = new ControlledStore();
            session = new GameSession(defs, state, clock, store);
        }

        [Test] public void FailedConstructionSaveRestoresPaymentTimerRevisionAndBuildingIdentity()
        {
            var buildings = state.buildings;
            var building = state.Find("farm_grow_01");
            string before = JsonUtility.ToJson(state);
            clock.UtcNow = Start.AddSeconds(15);
            store.Fail = true;
            Assert.Throws<IOException>(() => session.Transact(() => session.Upgrades.BuyWithMoney(building.id)));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(session.State, Is.SameAs(state));
            Assert.That(state.buildings, Is.SameAs(buildings));
            Assert.That(state.Find(building.id), Is.SameAs(building));
            Assert.That(session.LastSaveError, Is.TypeOf<IOException>());
            store.Fail = false;
            Assert.That(session.Transact(() => session.Upgrades.BuyWithMoney(building.id)), Is.EqualTo(Failure.None));
            Assert.That(building.buildingSeconds, Is.GreaterThan(0));
            Assert.That(session.LastSaveError, Is.Null);
        }

        [Test] public void UnauthorizedSaveRestoresTransactionAndMarksTheExactStorageError()
        {
            var building = state.Find("farm_grow_01"); string before = JsonUtility.ToJson(state);
            var error = new UnauthorizedAccessException("Simulated storage access denied");
            store.Fail = true; store.Error = error;
            Assert.Throws<UnauthorizedAccessException>(() => session.Transact(() => session.Upgrades.BuyWithMoney(building.id)));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(state.Find(building.id), Is.SameAs(building));
            Assert.That(session.LastSaveError, Is.SameAs(error));
            Assert.That(session.SaveRetrySeconds, Is.EqualTo(30));
        }

        [Test] public void FailedWorkerAssignmentSaveRestoresBothWorkersInPlace()
        {
            var building = state.Find("farm_grow_01"); building.level = 1;
            string role = defs.Building(building.family).managerRole;
            var first = session.Workers.Grant(role);
            var second = session.Workers.Grant(role);
            Assert.That(session.Workers.Assign(first.id, building.id, 0), Is.EqualTo(Failure.None));
            string before = JsonUtility.ToJson(state);
            store.Fail = true;
            Assert.Throws<IOException>(() => session.Transact(() => session.Workers.Assign(second.id, building.id, 0)));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(state.workers.Single(w => w.id == first.id), Is.SameAs(first));
            Assert.That(state.workers.Single(w => w.id == second.id), Is.SameAs(second));
        }

        [Test] public void FailedRewardSaveRemovesGrantedWorkerAndRestoresRandomAndReceipts()
        {
            var workers = state.workers; var receipts = state.rewardHistory;
            string before = JsonUtility.ToJson(state);
            store.Fail = true;
            Assert.Throws<IOException>(() => session.Transact(() =>
            {
                var worker = session.Workers.Grant("grow");
                RewardHistory.Record(state, RewardSource.Recruitment, "test", "test", new[] { RewardItem.ForWorker(worker) });
                return Failure.None;
            }));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(state.workers, Is.SameAs(workers));
            Assert.That(state.rewardHistory, Is.SameAs(receipts));
        }

        [Test] public void FailedCommandAndThrownCommandRestoreMutationsWithoutSaving()
        {
            var talked = state.City.talked;
            string before = JsonUtility.ToJson(state);
            Assert.That(session.Transact(() => { state.money = 0; talked.Add("mila"); return Failure.Invalid; }), Is.EqualTo(Failure.Invalid));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.Throws<InvalidOperationException>(() => session.Transact(() =>
            { state.money = 0; talked.Add("mila"); throw new InvalidOperationException("command defect"); }));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(store.Attempts, Is.Zero);
        }

        [Test] public void EveryMutableSaveFieldAndNestedReferenceSurvivesFailedTransaction()
        {
            // Populate every serialized model type. This guards schema growth as well as
            // existing commands: new scalar fields must be covered by the explicit snapshot.
            var city = state.City;
            city.project.baselines = new Amount[] { 12, 34 };
            city.project.completedSteps.Add("step");
            var story = new VipStoryState { id = "snapshot_fixture", active = new ActiveStoryQuest
                { questId = "quest", choiceId = "choice", baselines = new Amount[] { 56 }, targets = new[] { 78 } } };
            story.completed.Add(new StoryCompletion { questId = "done", choiceId = "done_choice" });
            city.vipStories.Add(story);
            city.vipCollaborations.Add(new VipCollaborationState { id = "collaboration", active = new ActiveStoryQuest
                { baselines = new Amount[] { 90 }, targets = new[] { 12 } }, completed = new StoryCompletion { questId = "done" } });
            state.workers.Add(new WorkerState { id = "worker", attributes = new List<WorkerAttribute> { WorkerAttribute.Cargo } });
            state.deliveries.Add(new DeliveryState { route = "route", workerId = "worker", cargo = 4 });
            state.vip.Add(new VipState { id = "vip", target = 7, reward = 8 });
            state.inventory.Add(new ItemStack { id = "item", count = 3 });
            state.rewardHistory.Add(new RewardReceipt { sequence = 1, items = new List<RewardItem>
                { new RewardItem { key = "reward", reference = "worker", quantity = 9 } } });
            state.shop = new ShopState { vipMembership = new VipMembershipState { startedUtcTicks = Start.Ticks,
                expiresUtcTicks = Start.AddDays(1).Ticks }, purchases = new List<ShopPurchaseReceipt>
                { new ShopPurchaseReceipt { transactionId = "transaction", productId = "product", channel = "Test" } } };
            string before = JsonUtility.ToJson(state);
            var objects = CaptureReferences(state);
            store.Fail = true;
            Assert.Throws<IOException>(() => session.Transact(() => { MutateGraph(state); return Failure.None; }));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            var restored = CaptureReferences(state);
            Assert.That(restored.Count, Is.EqualTo(objects.Count));
            foreach (var pair in objects) Assert.That(restored[pair.Key], Is.SameAs(pair.Value), pair.Key);
        }

        [Test] public void AutosaveFailureKeepsLiveProgressAndBacksOffUntilStoreRecovers()
        {
            store.Fail = true;
            Assert.DoesNotThrow(() => session.Advance(30));
            Assert.That(store.Attempts, Is.EqualTo(1));
            Assert.That(session.SaveRetrySeconds, Is.EqualTo(30));
            for (int i = 0; i < 299; i++) session.Advance(.1);
            Assert.That(store.Attempts, Is.EqualTo(1));
            session.Advance(.2);
            Assert.That(store.Attempts, Is.EqualTo(2));
            Assert.That(session.SaveRetrySeconds, Is.EqualTo(60));
            Assert.That(state.simulationSeconds, Is.EqualTo(60));
            store.Fail = false;
            session.Advance(60);
            Assert.That(store.Attempts, Is.EqualTo(3));
            Assert.That(store.Saved.simulationSeconds, Is.EqualTo(state.simulationSeconds));
            Assert.That(session.LastSaveError, Is.Null);
            Assert.That(session.ConsecutiveSaveFailures, Is.Zero);
            session.Advance(29);
            Assert.That(store.Attempts, Is.EqualTo(3));
            session.Advance(1);
            Assert.That(store.Attempts, Is.EqualTo(4));
        }

        [Test] public void RetryDelayIsCappedAndManualSaveCanRecoverImmediately()
        {
            store.Fail = true;
            for (int i = 0; i < 8; i++)
            {
                Assert.Throws<IOException>(() => session.Save());
                Assert.That(session.SaveRetrySeconds, Is.LessThanOrEqualTo(300));
            }
            Assert.That(session.SaveRetrySeconds, Is.EqualTo(300));
            store.Fail = false;
            session.Save();
            Assert.That(session.LastSaveError, Is.Null);
            Assert.That(session.SaveRetrySeconds, Is.Zero);
        }

        [Test] public void PausedSessionRetriesUnsavedProgressWithoutSimulating()
        {
            store.Fail = true;
            Assert.Throws<IOException>(() => session.Save());
            session.Paused = true; store.Fail = false;
            session.Advance(30);
            Assert.That(store.Attempts, Is.EqualTo(2));
            Assert.That(state.simulationSeconds, Is.Zero);
            Assert.That(session.LastSaveError, Is.Null);
        }

        [Test] public void VipDailySaveFailureRetriesAtBackoffAndGrantsExactlyOnce()
        {
            state.shop = new ShopState { vipMembership = new VipMembershipState
                { startedUtcTicks = Start.Ticks, expiresUtcTicks = Start.AddDays(3).Ticks } };
            int grinders = state.grinders;
            store.Fail = true;
            Assert.DoesNotThrow(() => session.Advance(0));
            Assert.That(store.Attempts, Is.EqualTo(1));
            Assert.That(state.grinders, Is.EqualTo(grinders));
            for (int i = 0; i < 20; i++) session.Advance(.1);
            Assert.That(store.Attempts, Is.EqualTo(1));
            store.Fail = false;
            session.Advance(28);
            Assert.That(store.Attempts, Is.EqualTo(2));
            Assert.That(state.grinders, Is.EqualTo(grinders + ShopRules.DailyGrinders));
            Assert.That(state.shop.vipMembership.rewardedDays, Is.EqualTo(1));
            session.Advance(0);
            Assert.That(state.grinders, Is.EqualTo(grinders + ShopRules.DailyGrinders));
        }

        [TestCase("complete", false), TestCase("complete", true)]
        [TestCase("advance", false), TestCase("advance", true)]
        [TestCase("advance_to_complete", false), TestCase("advance_to_complete", true)]
        [TestCase("restart", false), TestCase("restart", true)]
        public void FailedIntroSaveRestoresPageCompletionAndUnlockedStories(string action, bool denied)
        {
            state.introSeen = action == "restart";
            state.introLine = action == "advance_to_complete" ? 2 : action == "restart" ? 3 : 0;
            if (!state.introSeen) foreach (var city in state.cities) { city.vipStories.Clear(); city.vipCollaborations.Clear(); }
            var stories = state.City.vipStories; string before = JsonUtility.ToJson(state);
            store.Fail = true;
            store.Error = denied ? (Exception)new UnauthorizedAccessException("Intro access denied") : new IOException("Intro disk failure");
            void Act()
            {
                if (action == "complete") session.CompleteIntro();
                else if (action == "restart") session.RestartIntro();
                else session.NextIntro();
            }
            if (denied) Assert.Throws<UnauthorizedAccessException>(Act); else Assert.Throws<IOException>(Act);
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(state.City.vipStories, Is.SameAs(stories));
            Assert.That(session.LastSaveError, Is.SameAs(store.Error));
            Assert.That(store.Attempts, Is.EqualTo(1));
        }

        [Test] public void IntroAdvanceCompletionAndRestartEachPersistExactlyOnce()
        {
            int attempts = 0;
            session.RestartIntro(); Assert.That(store.Attempts, Is.EqualTo(++attempts));
            session.NextIntro(); Assert.That(store.Attempts, Is.EqualTo(++attempts)); Assert.That(state.introLine, Is.EqualTo(1));
            session.NextIntro(); Assert.That(store.Attempts, Is.EqualTo(++attempts)); Assert.That(state.introLine, Is.EqualTo(2));
            session.NextIntro(); Assert.That(store.Attempts, Is.EqualTo(++attempts)); Assert.That(state.introSeen, Is.True);
            session.RestartIntro(); Assert.That(store.Attempts, Is.EqualTo(++attempts));
            session.CompleteIntro(); Assert.That(store.Attempts, Is.EqualTo(++attempts)); Assert.That(state.introSeen, Is.True);
        }
        [Test] public void SaveFailureDoesNotConsumeTimestampAndInvalidFrameDoesNotTouchStore()
        {
            clock.UtcNow = Start.AddHours(1); store.Fail = true;
            Assert.Throws<IOException>(() => session.Save());
            Assert.That(state.lastUtcTicks, Is.EqualTo(Start.Ticks));
            Assert.That(state.highWaterUtcTicks, Is.EqualTo(Start.Ticks));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.Advance(double.NaN));
            Assert.That(store.Attempts, Is.EqualTo(1));
            Assert.That(state.simulationSeconds, Is.Zero);
        }

        private sealed class ControlledStore : ISaveStore
        {
            public bool Fail;
            public Exception Error;
            public int Attempts;
            public GameState Saved;
            public bool Exists => Saved != null;
            public void Save(GameState game)
            {
                Attempts++;
                if (Fail) throw Error ?? new IOException("Simulated disk failure");
                Saved = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(game));
            }
            public SaveLoadResult Load() => new SaveLoadResult { State = Saved };
        }

        private static Dictionary<string, object> CaptureReferences(object root)
        {
            var references = new Dictionary<string, object>();
            void Visit(object value, string path)
            {
                if (value == null || value is string || value.GetType().IsValueType) return;
                references.Add(path, value);
                if (value is IList list)
                { for (int i = 0; i < list.Count; i++) Visit(list[i], path + "[" + i + "]"); return; }
                foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                    Visit(field.GetValue(value), path + "." + field.Name);
            }
            Visit(root, "state"); return references;
        }
        private static void MutateGraph(object value)
        {
            if (value is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    object item = list[i];
                    if (item != null && !item.GetType().IsValueType && !(item is string)) MutateGraph(item);
                    else if (item is Amount) list[i] = new Amount(999);
                    else if (item is int number) list[i] = number + 1;
                }
                if (!list.IsFixedSize) list.Clear();
                return;
            }
            foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object old = field.GetValue(value); Type type = field.FieldType;
                if (type == typeof(string)) field.SetValue(value, "mutated_" + old);
                else if (type == typeof(bool)) field.SetValue(value, !(bool)old);
                else if (type == typeof(int)) field.SetValue(value, (int)old + 7);
                else if (type == typeof(long)) field.SetValue(value, (long)old + 11);
                else if (type == typeof(uint)) field.SetValue(value, (uint)old + 13);
                else if (type == typeof(double)) field.SetValue(value, (double)old + .5);
                else if (type == typeof(float)) field.SetValue(value, (float)old + .25f);
                else if (type == typeof(Amount)) field.SetValue(value, new Amount(987));
                else if (type.IsEnum) field.SetValue(value, Enum.ToObject(type, Convert.ToInt32(old) + 1));
                else if (old != null) { MutateGraph(old); field.SetValue(value, null); }
            }
        }
    }
}

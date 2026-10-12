using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Weedtopia.Application;
using Weedtopia.Domain;

namespace Weedtopia.Tests
{
    public sealed class OfflineStartupTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);
        private GameDefinitions defs;
        [SetUp] public void Setup() => defs = ProgressionTests.Definitions();

        [TestCase(1), TestCase(7), TestCase(64), TestCase(128)]
        public void ChunkedOfflineExactlyMatchesSynchronousResultIncludingTwoCitiesAndVipExpiry(int batch)
        {
            var seed = ProductionState();
            var syncState = Clone(seed); var incrementalState = Clone(seed);
            var clock = new ManualClock(Start.AddSeconds(4097));
            var syncStore = new CountingStore(); var incrementalStore = new CountingStore();
            var synchronous = new GameSession(defs, syncState, clock, syncStore);
            var incremental = new GameSession(defs, incrementalState, clock, incrementalStore);
            var expected = synchronous.ApplyOffline();
            OfflineSummary actual = null; int previous = 0;
            foreach (var progress in incremental.ApplyOfflineIncrementally(batch))
            {
                Assert.That(progress.CompletedSeconds, Is.GreaterThanOrEqualTo(previous));
                Assert.That(progress.CompletedSeconds - previous, Is.LessThanOrEqualTo(batch));
                Assert.That(progress.TotalSeconds, Is.EqualTo(4097));
                Assert.That(progress.Fraction, Is.InRange(0d, 1d));
                if (!progress.IsComplete) Assert.That(incrementalStore.Attempts, Is.Zero, "Incomplete offline model must never reach disk");
                else { actual = progress.Summary; Assert.That(progress.Fraction, Is.EqualTo(1)); }
                previous = progress.CompletedSeconds;
            }
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.Seconds, Is.EqualTo(expected.Seconds));
            Assert.That(actual.Earned, Is.EqualTo(expected.Earned));
            Assert.That(actual.ClockWentBack, Is.EqualTo(expected.ClockWentBack));
            Assert.That(JsonUtility.ToJson(incrementalState), Is.EqualTo(JsonUtility.ToJson(syncState)));
            Assert.That(incrementalStore.Attempts, Is.EqualTo(syncStore.Attempts));
            Assert.That(incrementalState.cities.All(c => c.stats.playSeconds == 4097), Is.True);
        }

        [Test] public void DisposedPartialOfflineRestoresModelAndRejectsSaveAdvanceAndReentrancy()
        {
            var state = GameState.Create(defs, "Unterbrochene Berechnung", Start); state.introSeen = true;
            state.Find("farm_grow_01").level = 1;
            var store = new CountingStore();
            var session = new GameSession(defs, state, new ManualClock(Start.AddSeconds(500)), store);
            var building = state.Find("farm_grow_01"); string before = JsonUtility.ToJson(state);
            using (var progress = session.ApplyOfflineIncrementally(7).GetEnumerator())
            {
                Assert.That(progress.MoveNext(), Is.True); Assert.That(progress.Current.CompletedSeconds, Is.Zero);
                Assert.That(session.IsApplyingOffline, Is.True);
                Assert.Throws<InvalidOperationException>(() => session.Save());
                Assert.Throws<InvalidOperationException>(() => session.Advance(1));
                bool ran = false;
                Assert.That(session.Transact(() => { ran = true; return Failure.None; }), Is.EqualTo(Failure.Busy));
                Assert.That(ran, Is.False);
                using (var another = session.ApplyOfflineIncrementally().GetEnumerator())
                    Assert.Throws<InvalidOperationException>(() => another.MoveNext());
                Assert.That(session.IsApplyingOffline, Is.True);
                Assert.That(progress.MoveNext(), Is.True); Assert.That(state.simulationSeconds, Is.EqualTo(7));
                Assert.That(store.Attempts, Is.Zero);
            }
            Assert.That(session.IsApplyingOffline, Is.False);
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(state.Find(building.id), Is.SameAs(building));
            Assert.That(session.ApplyOffline().Seconds, Is.EqualTo(500));
            Assert.That(state.simulationSeconds, Is.EqualTo(500));
            Assert.That(store.Attempts, Is.EqualTo(1));
        }

        [Test] public void FailedFinalOfflineWriteRestoresCountersResourcesTimestampAndReferences()
        {
            var state = GameState.Create(defs, "Fehlgeschlagener Start", Start); state.introSeen = true;
            state.Find("farm_grow_01").level = 1;
            var store = new CountingStore { Fail = true };
            var session = new GameSession(defs, state, new ManualClock(Start.AddSeconds(500)), store);
            var building = state.Find("farm_grow_01"); string before = JsonUtility.ToJson(state);
            Assert.Throws<IOException>(() => { foreach (var progress in session.ApplyOfflineIncrementally(64)) { } });
            Assert.That(session.IsApplyingOffline, Is.False);
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(state.Find(building.id), Is.SameAs(building));
            Assert.That(store.Attempts, Is.EqualTo(1));
            store.Fail = false;
            var summary = session.ApplyOffline();
            Assert.That(summary.Seconds, Is.EqualTo(500));
            Assert.That(session.ApplyOffline().Seconds, Is.Zero);
            Assert.That(state.simulationSeconds, Is.EqualTo(500));
        }

        [TestCase(0), TestCase(129)] public void InvalidOfflineBatchIsRejectedBeforeAnyMutation(int batch)
        {
            var state = GameState.Create(defs, "Ungültiger Schritt", Start); state.introSeen = true;
            var session = new GameSession(defs, state, new ManualClock(Start.AddHours(1)), new CountingStore());
            string before = JsonUtility.ToJson(state);
            Assert.Throws<ArgumentOutOfRangeException>(() => session.ApplyOfflineIncrementally(batch).GetEnumerator().MoveNext());
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(session.IsApplyingOffline, Is.False);
        }

        [Test] public void OfflineInitialProgressPrecedesSimulationAndZeroElapsedStillCompletes()
        {
            var state = GameState.Create(defs, "Direkter Start", Start); state.introSeen = true;
            var store = new CountingStore(); var session = new GameSession(defs, state, new ManualClock(Start), store);
            using (var progress = session.ApplyOfflineIncrementally().GetEnumerator())
            {
                Assert.That(progress.MoveNext(), Is.True); Assert.That(progress.Current.Fraction, Is.Zero);
                Assert.That(store.Attempts, Is.Zero); Assert.That(state.simulationSeconds, Is.Zero);
                Assert.That(progress.MoveNext(), Is.True); Assert.That(progress.Current.IsComplete, Is.True);
                Assert.That(progress.Current.Summary.Seconds, Is.Zero); Assert.That(progress.Current.Fraction, Is.EqualTo(1));
                Assert.That(progress.MoveNext(), Is.False);
            }
            Assert.That(store.Attempts, Is.EqualTo(1));
        }

        [Test] public void OfflineWithDueVipUsesOneCommitEvenIfASecondWriteWouldFail()
        {
            var state = GameState.Create(defs, "Ein Offline-Commit", Start); state.introSeen = true;
            state.Find("farm_grow_01").level = 1;
            state.shop = new ShopState { vipMembership = new VipMembershipState
                { startedUtcTicks = Start.Ticks, expiresUtcTicks = Start.AddDays(3).Ticks } };
            var store = new CountingStore { FailOnAttempt = 2 };
            var session = new GameSession(defs, state, new ManualClock(Start.AddSeconds(500)), store);
            int grinders = state.grinders;
            var summary = session.ApplyOffline();
            Assert.That(summary.Seconds, Is.EqualTo(500));
            Assert.That(store.Attempts, Is.EqualTo(1), "VIP refresh and offline progress must have one shared persistence point");
            Assert.That(state.grinders, Is.EqualTo(grinders + ShopRules.DailyGrinders));
            Assert.That(state.shop.vipMembership.rewardedDays, Is.EqualTo(1));
            Assert.That(state.rewardHistory.Count(r => r.source == RewardSource.VipMembership), Is.EqualTo(1));
            Assert.That(JsonUtility.ToJson(store.Saved), Is.EqualTo(JsonUtility.ToJson(state)), "Committed disk snapshot and live session must agree");
        }

        [Test] public void FailedSingleOfflineVipCommitRollsBackAllProgressAndRetryGrantsOnce()
        {
            var state = GameState.Create(defs, "Offline erneut", Start); state.introSeen = true;
            state.Find("farm_grow_01").level = 1;
            state.shop = new ShopState { vipMembership = new VipMembershipState
                { startedUtcTicks = Start.Ticks, expiresUtcTicks = Start.AddDays(3).Ticks } };
            var store = new CountingStore { Fail = true };
            var session = new GameSession(defs, state, new ManualClock(Start.AddSeconds(500)), store);
            var vip = state.shop.vipMembership; var building = state.Find("farm_grow_01");
            string before = JsonUtility.ToJson(state); int grinders = state.grinders;
            Assert.Throws<IOException>(() => { foreach (var step in session.ApplyOfflineIncrementally(64)) { } });
            Assert.That(store.Attempts, Is.EqualTo(1)); Assert.That(store.Saved, Is.Null);
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(state.shop.vipMembership, Is.SameAs(vip)); Assert.That(state.Find(building.id), Is.SameAs(building));
            Assert.That(session.IsApplyingOffline, Is.False);
            store.Fail = false;
            Assert.That(session.ApplyOffline().Seconds, Is.EqualTo(500));
            Assert.That(store.Attempts, Is.EqualTo(2), "One failed attempt and one successful retry");
            Assert.That(state.grinders, Is.EqualTo(grinders + ShopRules.DailyGrinders));
            Assert.That(vip.rewardedDays, Is.EqualTo(1));
            Assert.That(state.rewardHistory.Count(r => r.source == RewardSource.VipMembership), Is.EqualTo(1));
            Assert.That(JsonUtility.ToJson(store.Saved), Is.EqualTo(JsonUtility.ToJson(state)));
            Assert.That(session.ApplyOffline().Seconds, Is.Zero);
            Assert.That(state.grinders, Is.EqualTo(grinders + ShopRules.DailyGrinders));
            Assert.That(state.rewardHistory.Count(r => r.source == RewardSource.VipMembership), Is.EqualTo(1));
        }

        private GameState ProductionState()
        {
            var state = GameState.Create(defs, "Beide Städte", Start); state.introSeen = true; state.tickRemainder = .375;
            state.money = new Amount(1, 20); state.activeCity = "big_rock_city";
            var workers = new WorkerService(defs, state);
            // All destinations were legitimately opened before this isolated offline fixture.
            foreach (var city in state.cities)
            { city.charterAccepted = true; city.unlocked = Zone.Industry; city.storyResidentMilestone = 5000;
                city.charterResidentMilestone = CityCampaignRules.Combined(defs.cities.Single(c => c.id == city.id)) ? 5000 : 0; }
            foreach (var building in state.buildings)
            {
                var definition = defs.Building(building.family);
                if (definition.kind == BuildingKind.Plot) continue;
                building.level = string.IsNullOrEmpty(definition.driverRole) ? 1 : 5;
                if (!string.IsNullOrEmpty(definition.managerRole))
                { var worker = workers.Grant(definition.managerRole); worker.buildingId = building.id; worker.slot = 0; }
                if (!string.IsNullOrEmpty(definition.driverRole))
                { var worker = workers.Grant(definition.driverRole); worker.buildingId = building.id; worker.slot = 1; }
            }
            state.shop = new ShopState { vipMembership = new VipMembershipState
                { startedUtcTicks = Start.AddHours(-1).Ticks, expiresUtcTicks = Start.AddSeconds(2048).Ticks, rewardedDays = 1 } };
            return state;
        }
        private static GameState Clone(GameState state) => JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state));
        private sealed class CountingStore : ISaveStore
        {
            public int Attempts; public bool Fail; public int FailOnAttempt; public GameState Saved;
            public bool Exists => false;
            public SaveLoadResult Load() => new SaveLoadResult();
            public void Save(GameState state)
            {
                Attempts++;
                if (Fail || Attempts == FailOnAttempt) throw new IOException("Offline final write failed");
                Saved = Clone(state);
            }
        }
    }
}

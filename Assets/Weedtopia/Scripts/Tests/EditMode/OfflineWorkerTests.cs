using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Weedtopia.Application;
using Weedtopia.Domain;

namespace Weedtopia.Tests
{
    public sealed class OfflineWorkerTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        // A bounded deadlock watchdog for functional tests, not a performance acceptance limit.
        // ProductionState uses the full authored catalog: thirteen cities (728 buildings).
        // The eight-hour synchronous reference took about 191 seconds in the v14 Unity run;
        // keep a finite ten-minute watchdog with headroom for the equivalent isolated worker.
        private const double WorkerWatchdogSeconds = 600;
        // These cases run a complete reference or more than one isolated worker.
        // NUnit's default 180-second case timeout is shorter than those combined phases.
        private const int MultiPhaseCaseTimeoutMilliseconds = 900000;
        private GameDefinitions defs;
        [SetUp] public void Setup() => defs = ProgressionTests.Definitions();

        [TestCase(.375), TestCase(.1)]
        public void WorkerExactlyMatchesHistoricalResponsiveTwoCityConstructionVipAndPersistedState(double remainder)
        {
            var seed = ProductionState();
            seed.tickRemainder = remainder;
            var expectedState = Clone(seed); var actualState = Clone(seed);
            var clock = new OwnerClock(Start.AddSeconds(4097));
            var expectedStore = new OwnerStore(); var actualStore = new OwnerStore();
            var expectedSession = new GameSession(defs, expectedState, clock, expectedStore);
            var actualSession = new GameSession(defs, actualState, clock, actualStore);
            // ContinueResponsive historically applied 32-second batches. Nonbinary remainders
            // expose its exact floating-point contract even when the executed tick count agrees.
            var expected = expectedSession.ApplyOfflineIncrementally(32).Last().Summary;
            var synchronousState = Clone(seed); var synchronousStore = new OwnerStore();
            var synchronous = new GameSession(defs, synchronousState, clock, synchronousStore).ApplyOffline();
            Assert.That(synchronous.Seconds, Is.EqualTo(expected.Seconds));
            Assert.That(synchronous.Earned, Is.EqualTo(expected.Earned));
            Assert.That(JsonUtility.ToJson(synchronousState), Is.EqualTo(JsonUtility.ToJson(expectedState)),
                "The synchronous convenience API must use the same canonical historical UI grouping.");
            Assert.That(synchronousStore.Saved, Is.EqualTo(expectedStore.Saved));
            Assert.That(synchronousStore.Attempts, Is.EqualTo(1));
            var buildings = actualState.buildings; var building = buildings[0];
            var worker = actualState.workers[0]; var attributes = worker.attributes;
            using (var operation = actualSession.StartOfflineSimulation())
            {
                Assert.That(actualSession.IsApplyingOffline, Is.True);
                var actual = Finish(operation);
                Assert.That(actual.Seconds, Is.EqualTo(expected.Seconds));
                Assert.That(actual.Earned, Is.EqualTo(expected.Earned));
                Assert.That(operation.WorkerThreadId, Is.Not.EqualTo(Thread.CurrentThread.ManagedThreadId));
                Assert.That(operation.CompletedSeconds, Is.EqualTo(4097));
                Assert.That(actualStore.Attempts, Is.EqualTo(1));
                Assert.That(JsonUtility.ToJson(actualState), Is.EqualTo(JsonUtility.ToJson(expectedState)));
                Assert.That(actualStore.Saved, Is.EqualTo(expectedStore.Saved));
                Assert.That(actualState.buildings, Is.SameAs(buildings));
                Assert.That(actualState.buildings[0], Is.SameAs(building));
                Assert.That(actualState.workers[0], Is.SameAs(worker));
                Assert.That(worker.attributes, Is.SameAs(attributes));
            }
            Assert.That(actualSession.IsApplyingOffline, Is.False);
        }

        [TestCase(-100), TestCase(0), TestCase(86400), Timeout(MultiPhaseCaseTimeoutMilliseconds)]
        public void WorkerPreservesUtcRollbackZeroElapsedAndEightHourCap(int elapsed)
        {
            var expectedState = ProductionState(); var actualState = Clone(expectedState);
            var clock = new OwnerClock(Start.AddSeconds(elapsed));
            var expected = new GameSession(defs, expectedState, clock, new OwnerStore()).ApplyOffline();
            var session = new GameSession(defs, actualState, clock, new OwnerStore());
            using (var operation = session.StartOfflineSimulation())
            {
                var actual = Finish(operation);
                Assert.That(actual.Seconds, Is.EqualTo(expected.Seconds));
                Assert.That(actual.ClockWentBack, Is.EqualTo(elapsed < 0));
                Assert.That(actual.Seconds, Is.EqualTo(elapsed <= 0 ? 0 : 28800));
                Assert.That(JsonUtility.ToJson(actualState), Is.EqualTo(JsonUtility.ToJson(expectedState)));
            }
        }

        [Test, Timeout(MultiPhaseCaseTimeoutMilliseconds)] public void FailedCommitRollsBackEveryFieldAndReferenceAndRetryGrantsVipOnce()
        {
            var state = ProductionState();
            state.shop.vipMembership.startedUtcTicks = Start.Ticks;
            state.shop.vipMembership.expiresUtcTicks = Start.AddDays(3).Ticks;
            state.shop.vipMembership.rewardedDays = 0;
            var store = new OwnerStore { Fail = true };
            var session = new GameSession(defs, state, new OwnerClock(Start.AddDays(1).AddSeconds(500)), store);
            string before = JsonUtility.ToJson(state);
            var buildings = state.buildings; var first = buildings[0]; var membership = state.shop.vipMembership;
            using (var operation = session.StartOfflineSimulation())
                Assert.Throws<IOException>(() => Finish(operation));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(session.LastSaveError, Is.TypeOf<IOException>());
            Assert.That(state.buildings, Is.SameAs(buildings)); Assert.That(buildings[0], Is.SameAs(first));
            Assert.That(state.shop.vipMembership, Is.SameAs(membership));
            Assert.That(session.IsApplyingOffline, Is.False);
            store.Fail = false;
            using (var operation = session.StartOfflineSimulation()) Finish(operation);
            Assert.That(store.Attempts, Is.EqualTo(2), "One failed write plus one successful retry.");
            Assert.That(membership.rewardedDays, Is.EqualTo(2));
            int grinders = state.grinders;
            using (var operation = session.StartOfflineSimulation()) Assert.That(Finish(operation).Seconds, Is.Zero);
            Assert.That(state.grinders, Is.EqualTo(grinders));
        }

        [Test, Timeout(MultiPhaseCaseTimeoutMilliseconds)] public void CancelledCalculationKeepsSessionBusyUntilRollbackAndNeverSaves()
        {
            var state = ProductionState(); var store = new OwnerStore();
            var session = new GameSession(defs, state, new OwnerClock(Start.AddHours(8)), store);
            string before = JsonUtility.ToJson(state); var deliveries = state.deliveries;
            using (var operation = session.StartOfflineSimulation())
            {
                Assert.Throws<InvalidOperationException>(() => session.Save());
                Assert.Throws<InvalidOperationException>(() => session.Advance(0));
                Assert.That(session.Transact(() => Failure.None), Is.EqualTo(Failure.Busy));
                Assert.Throws<InvalidOperationException>(() => session.StartOfflineSimulation());
                WaitUntil(() => operation.CompletedSeconds > 0 || operation.SimulationFinished);
                operation.Cancel();
                Assert.Throws<OperationCanceledException>(() => Finish(operation));
            }
            Assert.That(session.IsApplyingOffline, Is.False);
            Assert.That(store.Attempts, Is.Zero);
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(state.deliveries, Is.SameAs(deliveries));
            using (var operation = session.StartOfflineSimulation()) Finish(operation);
            Assert.That(store.Attempts, Is.EqualTo(1));
        }

        [Test] public void DisposingAnUnpublishedOperationRollsBackAfterWorkerStopsWithoutPersisting()
        {
            var state = ProductionState(); var store = new OwnerStore();
            var session = new GameSession(defs, state, new OwnerClock(Start.AddHours(8)), store);
            string before = JsonUtility.ToJson(state); var building = state.buildings[0];
            var operation = session.StartOfflineSimulation();
            operation.Dispose();
            Assert.That(store.Attempts, Is.Zero);
            WaitUntil(() => !session.IsApplyingOffline);
            Assert.That(operation.SimulationFinished, Is.True);
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(state.buildings[0], Is.SameAs(building));
            Assert.Throws<ObjectDisposedException>(() => operation.TryComplete(out _));
            operation.Dispose();
        }

        [Test] public void OtherThreadsCannotStartCompleteOrDisposeAndClockAndSaveStayOnOwnerThread()
        {
            var state = ProductionState(); var store = new OwnerStore();
            var session = new GameSession(defs, state, new OwnerClock(Start.AddSeconds(500)), store);
            Assert.That(Task.Run(() => Reject(() => session.StartOfflineSimulation())).GetAwaiter().GetResult(), Is.True);
            using (var operation = session.StartOfflineSimulation())
            {
                Assert.That(Task.Run(() => Reject(() => operation.TryComplete(out _))).GetAwaiter().GetResult(), Is.True);
                Assert.That(Task.Run(() => Reject(operation.Dispose)).GetAwaiter().GetResult(), Is.True);
                Assert.That(Task.Run(() => Reject(operation.Cancel)).GetAwaiter().GetResult(), Is.True);
                Assert.That(store.Attempts, Is.Zero);
                Finish(operation);
            }
            Assert.That(store.Attempts, Is.EqualTo(1));
            Assert.That(store.SaveThreadId, Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
        }

        [Test] public void RepeatedCompletionReturnsSameSummaryWithoutASecondSave()
        {
            var store = new OwnerStore();
            var session = new GameSession(defs, ProductionState(), new OwnerClock(Start.AddSeconds(500)), store);
            using (var operation = session.StartOfflineSimulation())
            {
                var first = Finish(operation);
                Assert.That(operation.TryComplete(out var second), Is.True);
                Assert.That(second, Is.SameAs(first));
                Assert.That(store.Attempts, Is.EqualTo(1));
            }
        }

        [Test] public void WorkerModelExceptionRollsBackWithoutCallingSaveOrSwallowingTheDefect()
        {
            var state = ProductionState(); var store = new OwnerStore();
            var session = new GameSession(defs, state, new OwnerClock(Start.AddSeconds(500)), store);
            state.deliveries.Add(new DeliveryState { route = "missing_route", workerId = state.workers[0].id,
                remainingSeconds = 1, totalSeconds = 1 });
            string before = JsonUtility.ToJson(state); var trip = state.deliveries[0];
            using (var operation = session.StartOfflineSimulation())
                Assert.Throws<KeyNotFoundException>(() => Finish(operation));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(state.deliveries[0], Is.SameAs(trip));
            Assert.That(store.Attempts, Is.Zero);
            Assert.That(session.LastSaveError, Is.Null);
            Assert.That(session.IsApplyingOffline, Is.False);
        }

        [Test] public void CancellationAfterSimulationCompletionStillDiscardsTheUncommittedModel()
        {
            var state = ProductionState(); var store = new OwnerStore();
            var session = new GameSession(defs, state, new OwnerClock(Start.AddSeconds(500)), store);
            string before = JsonUtility.ToJson(state);
            using (var operation = session.StartOfflineSimulation())
            {
                WaitUntil(() => operation.SimulationFinished);
                Assert.That(store.Attempts, Is.Zero);
                operation.Cancel();
                Assert.Throws<OperationCanceledException>(() => Finish(operation));
            }
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(store.Attempts, Is.Zero);
        }

        [Test] public void ReentrantStoreCallbacksCannotDisposeOrRepeatAnInProgressCommit()
        {
            var state = ProductionState(); var store = new OwnerStore();
            var session = new GameSession(defs, state, new OwnerClock(Start.AddSeconds(500)), store);
            using (var operation = session.StartOfflineSimulation())
            {
                store.DuringSave = () =>
                {
                    operation.Cancel();
                    operation.Dispose();
                    Assert.Throws<InvalidOperationException>(() => operation.TryComplete(out _));
                };
                Assert.That(Finish(operation).Seconds, Is.EqualTo(500));
                Assert.That(state.simulationSeconds, Is.EqualTo(500));
                Assert.That(store.Saved, Is.EqualTo(JsonUtility.ToJson(state)));
                Assert.That(store.Attempts, Is.EqualTo(1));
            }
            Assert.That(session.IsApplyingOffline, Is.False);
        }

        private GameState ProductionState()
        {
            var state = GameState.Create(defs, "Worker Test", Start); state.introSeen = true;
            state.tickRemainder = .375; state.activeCity = "big_rock_city";
            // Offline equivalence starts after witnessed mandates; population may since have declined.
            foreach (var city in state.cities)
            { city.charterAccepted = true; city.unlocked = Zone.Industry; city.storyResidentMilestone = 5000;
                city.charterResidentMilestone = CityCampaignRules.Combined(defs.cities.Single(c => c.id == city.id)) ? 5000 : 0; }
            var workers = new WorkerService(defs, state);
            int index = 0;
            foreach (var building in state.buildings)
            {
                var definition = defs.Building(building.family);
                if (definition.kind == BuildingKind.Plot) continue;
                building.level = 5; building.buildingSeconds = index++ % 7 == 0 ? 12 : 0;
                building.raw = 50; building.refined = 20; building.butter = 10; building.pastry = 5;
                if (!string.IsNullOrEmpty(definition.managerRole))
                { var worker = workers.Grant(definition.managerRole, 4); worker.buildingId = building.id; worker.slot = 0; }
                if (!string.IsNullOrEmpty(definition.driverRole))
                { var worker = workers.Grant(definition.driverRole, 4); worker.buildingId = building.id; worker.slot = 1; }
            }
            state.shop = new ShopState { vipMembership = new VipMembershipState
                { startedUtcTicks = Start.AddHours(-1).Ticks, expiresUtcTicks = Start.AddSeconds(2048).Ticks, rewardedDays = 1 } };
            return state;
        }
        private static GameState Clone(GameState state) => JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state));
        private static OfflineSummary Finish(OfflineSimulationOperation operation)
        {
            OfflineSummary summary = null;
            WaitUntil(() => operation.TryComplete(out summary));
            return summary;
        }
        private static void WaitUntil(Func<bool> condition)
        {
            var watch = Stopwatch.StartNew();
            while (!condition())
            {
                if (watch.Elapsed.TotalSeconds > WorkerWatchdogSeconds)
                    throw new TimeoutException("The full authored city-catalog isolated worker did not finish within the " + WorkerWatchdogSeconds + "-second functional-test watchdog.");
                Thread.Sleep(1);
            }
        }
        private static bool Reject(Action action)
        { try { action(); return false; } catch (InvalidOperationException) { return true; } }
        private sealed class OwnerClock : IClock
        {
            private readonly int owner = Thread.CurrentThread.ManagedThreadId;
            private readonly DateTime now;
            public OwnerClock(DateTime now) { this.now = now; }
            public DateTime UtcNow
            {
                get
                {
                    if (Thread.CurrentThread.ManagedThreadId != owner) throw new InvalidOperationException("Clock read on worker.");
                    return now;
                }
            }
        }
        private sealed class OwnerStore : ISaveStore
        {
            private readonly int owner = Thread.CurrentThread.ManagedThreadId;
            public int Attempts, SaveThreadId; public bool Fail; public string Saved; public Action DuringSave;
            public bool Exists => false;
            public SaveLoadResult Load() => new SaveLoadResult();
            public void Save(GameState state)
            {
                SaveThreadId = Thread.CurrentThread.ManagedThreadId;
                if (SaveThreadId != owner) throw new InvalidOperationException("Save/serialization called on worker.");
                Attempts++;
                if (Fail) throw new IOException("Worker offline final write failed.");
                DuringSave?.Invoke();
                Saved = JsonUtility.ToJson(state);
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Weedtopia.Domain;

namespace Weedtopia.Tests
{
    public sealed class SimulationOfflineOptimizationTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        private GameDefinitions definitions;
        [SetUp] public void Setup() => definitions = ProgressionTests.Definitions();
        private GameState State(double remainder = .375)
        {
            var state = GameState.Create(definitions, "Offlinevergleich", Start);
            state.introSeen = true; state.profileNamePending = false; state.tickRemainder = remainder;
            foreach (var city in state.cities) { city.charterAccepted = true; city.unlocked = Zone.Industry; city.storyResidentMilestone = 1000; }
            int index = 0;
            foreach (var building in state.buildings)
            {
                if (definitions.Building(building.family).kind == BuildingKind.Plot) building.family = index++ % 2 == 0 ? "residential_house" : "park";
                building.level = 500; building.buildingSeconds = index++ % 7 == 0 ? 12 : 0;
                building.workSeconds = 3; building.raw = 50; building.refined = 20; building.butter = 10; building.pastry = 5;
                var definition = definitions.Building(building.family);
                if (!string.IsNullOrEmpty(definition.managerRole)) Worker(state, building, definition.managerRole, 0);
                if (!string.IsNullOrEmpty(definition.driverRole)) for (int slot = 1; slot <= 2; slot++) Worker(state, building, definition.driverRole, slot);
            }
            return state;
        }
        private void Worker(GameState state, BuildingState building, string role, int slot)
        {
            state.workers.Add(new WorkerState { id = "offline_" + state.workers.Count, buildingId = building.id, slot = slot, role = role,
                level = 4, rank = 2, rarity = 4, attributes = definitions.Role(role).attributes.Take(5).ToList() });
        }
        private static string Bits(object value)
        {
            if (value == null) return "null;";
            if (value is double number) return "double:" + BitConverter.DoubleToInt64Bits(number).ToString("X16") + ";";
            if (value is float single) return "float:" + BitConverter.ToInt32(BitConverter.GetBytes(single), 0).ToString("X8") + ";";
            if (value is string text) return "string:" + text.Length + ":" + text + ";";
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum) return type.Name + ":" + Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) + ";";
            if (value is IEnumerable items) return "[" + string.Concat(items.Cast<object>().Select(Bits)) + "]";
            return "{" + string.Concat(type.GetFields(BindingFlags.Instance | BindingFlags.Public).Where(field => !field.IsNotSerialized)
                .OrderBy(field => field.Name, StringComparer.Ordinal).Select(field => field.Name + ":" + Bits(field.GetValue(value)))) + "}";
        }
        private void LegacyAdvance(Simulation simulation, int seconds, int chunk)
        {
            for (int simulated = 0; simulated < seconds;)
            { int step = Math.Min(chunk, seconds - simulated); simulation.Advance(step, Start.Ticks + simulated * TimeSpan.TicksPerSecond); simulated += step; }
        }
        [TestCase(0d), TestCase(.1d), TestCase(.375d), TestCase(.999999998d), TestCase(.9999999999d)]
        public void RetainedPlanMatchesLegacyUtcRoundingAndEveryFieldBit(double remainder)
        {
            var expected = State(remainder); var actual = State(remainder);
            foreach (var state in new[] { expected, actual }) state.shop = new ShopState { vipMembership = new VipMembershipState {
                startedUtcTicks = Start.AddSeconds(20).Ticks + 1, expiresUtcTicks = Start.AddSeconds(150).Ticks - 1 } };
            LegacyAdvance(new Simulation(definitions, expected), 4097, 128);
            var completions = new List<int>();
            new Simulation(definitions, actual).AdvanceOffline(4097, Start.Ticks, CancellationToken.None, completions.Add);
            Assert.That(Bits(actual), Is.EqualTo(Bits(expected)), "Includes all Amount mantissa bits, exponents, counters, buffers, workers and deliveries.");
            Assert.That(completions.Last(), Is.EqualTo(4097));
            Assert.That(completions.Select((value, index) => value - (index == 0 ? 0 : completions[index - 1])).All(value => value > 0 && value <= 128), Is.True);
        }
        [TestCase(1), TestCase(32), TestCase(128)]
        public void RetainedPlanMatchesPublicChunkingThroughConstructionAndVipChanges(int chunk)
        {
            var expected = State(); var actual = State();
            foreach (var state in new[] { expected, actual }) state.shop = new ShopState { vipMembership = new VipMembershipState {
                startedUtcTicks = Start.AddSeconds(20).Ticks, expiresUtcTicks = Start.AddSeconds(150).Ticks } };
            LegacyAdvance(new Simulation(definitions, expected), 513, chunk);
            new Simulation(definitions, actual).AdvanceOffline(513, Start.Ticks, CancellationToken.None, null);
            Assert.That(Bits(actual), Is.EqualTo(Bits(expected)));
        }
        [Test]
        public void CancellationBeforeAnyWorkDoesNotMutateThePrivateModel()
        {
            var state = State(); string before = Bits(state); var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => new Simulation(definitions, state).AdvanceOffline(500, Start.Ticks, cancellation.Token, null));
            Assert.That(Bits(state), Is.EqualTo(before));
        }
        [TestCase(128), TestCase(256)]
        public void CancellationLeavesExactlyCompletedPrivateSegmentsAndClearsPlanForReuse(int stop)
        {
            var expected = State(); var actual = State();
            LegacyAdvance(new Simulation(definitions, expected), stop, 128);
            var simulation = new Simulation(definitions, actual); var cancellation = new CancellationTokenSource();
            Assert.Throws<OperationCanceledException>(() => simulation.AdvanceOffline(1000, Start.Ticks, cancellation.Token, completed => { if (completed == stop) cancellation.Cancel(); }));
            Assert.That(Bits(actual), Is.EqualTo(Bits(expected)));
            var building = actual.buildings.First(b => definitions.Building(b.family).kind == BuildingKind.Processing);
            Worker(actual, building, definitions.Building(building.family).managerRole, 0);
            actual.workers.Last().level = 20;
            Assert.That(simulation.Capacity(building), Is.EqualTo(new Simulation(definitions, actual).Capacity(building)));
            simulation.Advance(1, Start.AddSeconds(stop).Ticks);
        }
        [Test]
        public void ThrowingProgressCallbackClearsPlanAndRecursiveOfflineAdvanceIsRejected()
        {
            var state = State(); var simulation = new Simulation(definitions, state);
            Assert.Throws<InvalidOperationException>(() => simulation.AdvanceOffline(1000, Start.Ticks, CancellationToken.None,
                completed => simulation.AdvanceOffline(1, Start.Ticks, CancellationToken.None, null)));
            Assert.That(state.simulationSeconds, Is.EqualTo(128));
            simulation.Advance(1, Start.AddSeconds(128).Ticks);
            Assert.That(state.simulationSeconds, Is.EqualTo(129));
        }
        [TestCase(-1), TestCase(604801)]
        public void InvalidOfflineDurationDoesNotChangeAnyField(int seconds)
        {
            var state = State(); string before = Bits(state);
            Assert.Throws<ArgumentOutOfRangeException>(() => new Simulation(definitions, state).AdvanceOffline(seconds, Start.Ticks, CancellationToken.None, null));
            Assert.That(Bits(state), Is.EqualTo(before));
        }
        [Test]
        public void EmptyOfflineAdvanceHasNoProgressOrModelChanges()
        {
            var state = State(); string before = Bits(state); int callbacks = 0;
            new Simulation(definitions, state).AdvanceOffline(0, Start.Ticks, CancellationToken.None, completed => callbacks++);
            Assert.That(callbacks, Is.Zero); Assert.That(Bits(state), Is.EqualTo(before));
        }
    }
}

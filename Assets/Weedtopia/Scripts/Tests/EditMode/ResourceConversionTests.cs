using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Persistence;

namespace Weedtopia.Tests
{
    public sealed class ResourceConversionTests
    {
        private static readonly DateTime Start = new DateTime(2026, 9, 26, 0, 0, 0, DateTimeKind.Utc);

        private static Simulation Create(BuildingKind kind, out GameState state, int level = 1,
            double rate = 100, double capacity = 1000)
        {
            var definitions = new GameDefinitions
            {
                buildings = new[] { new BuildingDefinition { id = "converter", kind = kind, zone = Zone.Industry,
                    rate = new Curve { basis = rate, growth = 10 }, capacity = new Curve { basis = capacity, growth = 10 } } },
                instances = new[] { new InstanceDefinition { id = "converter", family = "converter" } },
                routes = Array.Empty<RouteDefinition>(), roles = Array.Empty<RoleDefinition>(),
                quests = Array.Empty<QuestDefinition>(), texts = Array.Empty<TextEntry>()
            };
            state = GameState.Create(definitions, "Tester", Start);
            state.City.unlocked = Zone.Industry;
            state.Find("converter").level = level;
            var simulation = new Simulation(definitions, state);
            Assert.That(simulation.Work("converter"), Is.EqualTo(Failure.None));
            return simulation;
        }

        [TestCase(0)]
        [TestCase(9)]
        [TestCase(400)]
        [TestCase(2000)]
        public void ExhaustingFractionalButterInputCannotOverdrawTheBuffer(int exponent)
        {
            var simulation = Create(BuildingKind.Butter, out var state, exponent + 1);
            var factory = state.Find("converter");
            var available = new Amount(6.98234169929, exponent);
            factory.refined = available;

            Assert.DoesNotThrow(() => simulation.Advance(1));

            Assert.That(factory.refined, Is.EqualTo(Amount.Zero));
            Assert.That(factory.butter, Is.EqualTo(available / 2));
            Assert.That(state.City.stats.butterProduced, Is.EqualTo(factory.butter));
            var produced = factory.butter;
            Assert.DoesNotThrow(() => simulation.Advance(10));
            Assert.That(factory.butter, Is.EqualTo(produced), "An exhausted buffer cannot produce more.");
        }

        [Test]
        public void SeededFractionalInputsRemainValidAcrossAllConverters()
        {
            var random = new System.Random(20260926);
            foreach (var kind in new[] { BuildingKind.Processing, BuildingKind.Butter, BuildingKind.Bakery })
            {
                var simulation = Create(kind, out var state);
                var building = state.Find("converter");
                var input = kind == BuildingKind.Processing ? Resource.Raw : kind == BuildingKind.Butter ? Resource.Refined : Resource.Butter;
                int ratio = kind == BuildingKind.Butter ? 2 : 1;
                for (int i = 0; i < 2000; i++)
                {
                    building.raw = building.refined = building.butter = building.pastry = Amount.Zero;
                    building.workSeconds = 30;
                    var available = new Amount(1 + random.NextDouble() * 9, random.Next(-5, 2));
                    building.Set(input, available);
                    var previous = kind == BuildingKind.Processing ? state.City.stats.refinedProduced :
                        kind == BuildingKind.Butter ? state.City.stats.butterProduced : state.City.stats.pastryProduced;

                    simulation.Advance(1);

                    Assert.That(building.Get(input), Is.EqualTo(Amount.Zero), kind + ": " + available.Serialize());
                    foreach (Resource resource in Enum.GetValues(typeof(Resource)))
                    {
                        Assert.That(building.Get(resource).IsValid, Is.True);
                        Assert.That(building.Get(resource) >= 0, Is.True);
                        Assert.That(building.Get(resource) <= simulation.Capacity(building), Is.True);
                    }
                    var total = kind == BuildingKind.Processing ? state.City.stats.refinedProduced :
                        kind == BuildingKind.Butter ? state.City.stats.butterProduced : state.City.stats.pastryProduced;
                    Assert.That(total, Is.EqualTo(previous + available / ratio));
                }
            }
        }

        [TestCase(0, 4, 2, 1000)]
        [TestCase(999, 6, 1, 1000)]
        [TestCase(1000, 8, 0, 1000)]
        [TestCase(1001, 8, 0, 1001)]
        public void RateAndStorageLimitsPreserveUnconsumedInput(double stock, double remaining,
            double produced, double expectedMaximum)
        {
            var simulation = Create(BuildingKind.Butter, out var state, rate: 2);
            var factory = state.Find("converter");
            factory.refined = 8; factory.butter = stock;

            simulation.Advance(1);

            Assert.That(factory.refined, Is.EqualTo(new Amount(remaining)));
            Assert.That(factory.butter, Is.EqualTo(new Amount(stock + produced)));
            Assert.That(factory.butter <= expectedMaximum, Is.True);
            Assert.That(state.City.stats.butterProduced, Is.EqualTo(new Amount(produced)));
            Assert.That(factory.refined + (factory.butter - stock) * 2, Is.EqualTo(new Amount(8)));
        }

        [Test]
        public void FractionalConversionConservesResourcesWithinAmountPrecision()
        {
            var simulation = Create(BuildingKind.Butter, out var state, rate: .731234567891, capacity: 10);
            var factory = state.Find("converter");
            factory.refined = 9.98234169929;
            double available = factory.refined.ToDouble();
            for (int tick = 0; tick < 20; tick++)
            {
                simulation.Advance(1);
                Assert.That(factory.refined.ToDouble() + factory.butter.ToDouble() * 2,
                    Is.EqualTo(available).Within(available * 1e-10));
                Assert.That(factory.refined >= 0 && factory.butter >= 0, Is.True);
            }
        }

        [Test]
        public void BuildingBuffersStillRejectNegativeAndInvalidAmounts()
        {
            var building = new BuildingState { refined = 3 };
            Assert.Throws<ArgumentOutOfRangeException>(() => building.Set(Resource.Refined, new Amount(-1, -12)));
            Assert.Throws<ArgumentOutOfRangeException>(() => building.Set(Resource.Refined, new Amount { mantissa = double.NaN }));
            Assert.That(building.refined, Is.EqualTo(new Amount(3)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SavedFractionalInputResumesThroughLiveFramesAndOfflineProgress(bool offline)
        {
            var definitions = ProgressionTests.Definitions();
            var state = GameState.Create(definitions, "Tester", Start);
            state.introSeen = true; state.City.unlocked = Zone.Industry;
            var factory = state.Find("butter_factory");
            factory.level = 1; factory.refined = 6.98234169929; factory.workSeconds = 30;
            var expected = new GameSession(definitions, JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state)), new ManualClock(Start));
            var directory = Path.Combine(Path.GetFullPath("Temp"), "ResourceConversionSave_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var store = new LocalSaveStore(Path.Combine(directory, "save.json"), definitions);
                store.Save(state);
                var restored = store.Load();
                Assert.That(restored.State, Is.Not.Null);
                Assert.That(restored.Recovered, Is.False);
                var clock = new ManualClock(Start.AddSeconds(30));
                var actual = new GameSession(definitions, restored.State, clock, store);

                Assert.DoesNotThrow(() => expected.Advance(30));
                if (offline) Assert.That(actual.ApplyOffline().Seconds, Is.EqualTo(30));
                else for (int frame = 0; frame < 300; frame++) actual.Advance(.1);
                actual.Save();

                Assert.That(JsonUtility.ToJson(actual.State.Find(factory.id)), Is.EqualTo(JsonUtility.ToJson(expected.State.Find(factory.id))));
                Assert.That(actual.State.City.stats.butterProduced, Is.EqualTo(expected.State.City.stats.butterProduced));
                Assert.That(actual.State.Find(factory.id).refined, Is.EqualTo(Amount.Zero));
                Assert.That(store.Load().State, Is.Not.Null, "The resumed state must still pass save validation.");
                Assert.That(actual.ApplyOffline().Seconds, Is.Zero, "Offline progress cannot be claimed twice.");
            }
            finally { Directory.Delete(directory, true); }
        }
    }
}

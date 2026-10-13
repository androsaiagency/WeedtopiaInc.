using System;
using System.Linq;
using NUnit.Framework;
using Weedtopia.Domain;

namespace Weedtopia.Tests
{
    public sealed class SimulationBatchTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        [Test] public void WorkerChangesAreVisibleInTheNextBatchAndPublicQueries()
        {
            var definitions = ProgressionTests.Definitions();
            var state = GameState.Create(definitions, "Tester", Start);
            var building = state.buildings.First(b => definitions.Building(b.family).kind == BuildingKind.Processing);
            building.level = 10;
            state.City.unlocked = Zone.Industry;
            var simulation = new Simulation(definitions, state);
            Amount baseCapacity = simulation.Capacity(building), baseRate = simulation.Rate(building);
            simulation.Advance(2);
            state.workers.Add(new WorkerState { id = "cache-manager", buildingId = building.id, slot = 0, level = 10,
                attributes = new System.Collections.Generic.List<WorkerAttribute> { WorkerAttribute.Storage, WorkerAttribute.Throughput } });
            Assert.That(simulation.Capacity(building), Is.GreaterThan(baseCapacity));
            Assert.That(simulation.Rate(building), Is.GreaterThan(baseRate));
            building.raw = 100;
            simulation.Advance(1);
            Assert.That(building.refined, Is.GreaterThan(Amount.Zero));
            state.workers.Clear(); building.workSeconds = 0;
            Amount before = building.refined;
            simulation.Advance(1);
            Assert.That(building.refined, Is.EqualTo(before));
            Assert.That(simulation.Capacity(building), Is.EqualTo(baseCapacity));
        }
        [Test] public void HouseCompletionInvalidatesResidentsWithinTheSameBatch()
        {
            var definitions = ProgressionTests.Definitions();
            var state = GameState.Create(definitions, "Tester", Start);
            var house = state.buildings.First(b => definitions.Building(b.family).kind == BuildingKind.Plot);
            house.family = "residential_house";
            house.level = 0; house.buildingSeconds = 2;
            state.cities.Single(c => c.id == house.cityId).unlocked = Zone.Industry;
            var simulation = new Simulation(definitions, state);
            Assert.That(simulation.Residents(house.cityId), Is.Zero);
            simulation.Advance(3);
            Assert.That(house.level, Is.EqualTo(1));
            int expectedResidents = new Simulation(definitions, state).Residents(house.cityId);
            Assert.That(expectedResidents, Is.GreaterThan(0));
            Assert.That(simulation.Residents(house.cityId), Is.EqualTo(expectedResidents));
        }
        [Test] public void VipExpiryInsideABatchMatchesSecondBySecondExecution()
        {
            var definitions = ProgressionTests.Definitions();
            var a = GameState.Create(definitions, "Tester", Start);
            var b = GameState.Create(definitions, "Tester", Start);
            foreach (var state in new[] { a, b })
            {
                var building = state.buildings.First(x => definitions.Building(x.family).kind == BuildingKind.Grow);
                building.level = 10;
                state.tickRemainder = .375;
                state.shop = new ShopState { vipMembership = new VipMembershipState { startedUtcTicks = Start.Ticks,
                    expiresUtcTicks = Start.AddSeconds(11).Ticks } };
            }
            new Simulation(definitions, a).Advance(30, Start.Ticks);
            var step = new Simulation(definitions, b);
            for (int i = 0; i < 30; i++) step.Advance(1, Start.AddSeconds(i).Ticks);
            Assert.That(UnityEngine.JsonUtility.ToJson(a), Is.EqualTo(UnityEngine.JsonUtility.ToJson(b)));
        }
    }
}

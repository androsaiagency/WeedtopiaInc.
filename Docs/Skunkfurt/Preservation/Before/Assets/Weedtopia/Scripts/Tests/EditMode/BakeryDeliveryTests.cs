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
    public sealed class BakeryDeliveryTests
    {
        private GameDefinitions defs;
        private GameState state;
        private Simulation simulation;
        private WorkerState baker;
        private BuildingState bakery, factory;
        private RouteDefinition route;
        private static readonly DateTime Start=new DateTime(2026,9,25,8,0,0,DateTimeKind.Utc);
        [SetUp] public void Setup()
        {
            defs=ProgressionTests.Definitions();state=GameState.Create(defs,"Bäcker-Test",Start);state.introSeen=true;state.City.unlocked=Zone.Industry;
            simulation=new Simulation(defs,state);factory=state.Find("butter_factory");bakery=state.Find("bakery_01");factory.level=bakery.level=1;factory.butter=24;
            baker=Staff(bakery);route=defs.routes.Single(r=>r.to==bakery.id&&r.pickupByRecipient);
        }
        private WorkerState Staff(BuildingState home)
        {
            var worker=simulation.Workers.Grant("baker");worker.attributes=new List<WorkerAttribute>{WorkerAttribute.BuildSpeed};
            Assert.That(simulation.Workers.Assign(worker.id,home.id,0),Is.EqualTo(Failure.None));return worker;
        }
        private DeliveryState Depart(){simulation.Advance(1);return state.deliveries.Single(t=>t.workerId==baker.id);}
        private static void Near(Amount actual,double expected)=>Assert.That(actual.mantissa*Math.Pow(10,actual.exponent),Is.EqualTo(expected).Within(.000001));
        [Test] public void BakerDrivesEmptyLoadsAtFactoryAndOnlyBakesAfterReturning()
        {
            var trip=Depart();Assert.That(trip.fetching,Is.True);Near(trip.cargo,0);Near(factory.butter,24);Near(bakery.butter,0);
            Assert.That(simulation.Workers.Remove(baker.id),Is.EqualTo(Failure.Busy));Assert.That(simulation.Work(bakery.id),Is.EqualTo(Failure.Busy));
            simulation.Advance(trip.remainingSeconds-1);Near(factory.butter,24);Near(state.City.stats.pastrySold,0);
            simulation.Advance(1);Assert.That(trip.fetching,Is.False);Near(trip.cargo,8);Near(factory.butter,16);Near(bakery.butter,0);
            Assert.That(trip.remainingSeconds,Is.EqualTo(route.travelSeconds));SaveValidator.Validate(state,defs);
            simulation.Advance(trip.remainingSeconds-1);Near(bakery.butter,0);Near(state.City.stats.pastrySold,0);
            simulation.Advance(1);Assert.That(state.deliveries,Is.Empty);Near(bakery.butter,7);Near(state.City.stats.pastrySold,1);
            Assert.That(state.City.stats.deliveries,Is.EqualTo(1));Assert.That(simulation.Workers.Remove(baker.id),Is.EqualTo(Failure.None));
        }
        [Test] public void NoBakerNoBeltAndNoPickupWhenTheDestinationIsStocked()
        {
            simulation.Workers.Remove(baker.id);factory.workSeconds=30;bakery.workSeconds=30;simulation.Advance(10);
            Near(factory.butter,24);Near(bakery.butter,0);Assert.That(state.deliveries,Is.Empty);
            simulation.Workers.Assign(baker.id,bakery.id,0);bakery.butter=24;simulation.Advance(1);Assert.That(state.deliveries,Is.Empty);
            factory.butter=0;bakery.butter=0;simulation.Advance(10);Assert.That(state.deliveries,Is.Empty);
        }
        [Test] public void MultipleBakersReserveWithoutDuplicatingOrLosingButter()
        {
            factory.butter=12;var second=state.Find("bakery_02");second.level=1;Staff(second);simulation.Advance(1);
            Assert.That(state.deliveries.Count,Is.EqualTo(2));Near(state.deliveries.Aggregate(Amount.Zero,(a,t)=>a+t.reservedCargo),12);Near(factory.butter,12);
            for(int i=0;i<90;i++)
            {
                simulation.Advance(1);SaveValidator.Validate(state,defs);
                var total=factory.butter+bakery.butter+second.butter+state.City.stats.pastryProduced+state.deliveries.Aggregate(Amount.Zero,(a,t)=>a+t.cargo);
                Near(total,12);
            }
            Near(state.City.stats.pastrySold,12);Assert.That(state.deliveries,Is.Empty);
        }
        [Test] public void FullArrivalBufferCanDrainWhileTheVanWaitsAndCargoIsPreserved()
        {
            var trip=Depart();simulation.Advance(trip.remainingSeconds);Near(trip.cargo,8);
            bakery.butter=simulation.Capacity(bakery);simulation.Advance(trip.remainingSeconds);
            Assert.That(trip.remainingSeconds,Is.Zero);Near(trip.cargo,8);Near(bakery.butter,23);
            simulation.Advance(9);Assert.That(state.deliveries,Is.Empty);Near(state.City.stats.delivered,8);SaveValidator.Validate(state,defs);
        }
        [Test] public void MissingReservedStockReturnsAnEmptyVanWithoutInventingADelivery()
        {
            var trip=Depart();factory.butter=0;simulation.Advance(trip.remainingSeconds);
            Assert.That(trip.fetching,Is.False);Near(trip.cargo,0);SaveValidator.Validate(state,defs);
            simulation.Advance(trip.remainingSeconds);Assert.That(state.deliveries,Is.Empty);Assert.That(state.City.stats.deliveries,Is.Zero);
        }
        [TestCase(true)] [TestCase(false)] public void SaveReloadAndOfflineResumeEachLegExactly(bool outbound)
        {
            var trip=Depart();if(!outbound)simulation.Advance(trip.remainingSeconds);simulation.Advance(1);
            string directory=Path.GetFullPath("Temp/BakerySave_"+Guid.NewGuid().ToString("N"));var store=new LocalSaveStore(Path.Combine(directory,"save.json"),defs);
            try
            {
                store.Save(state);var resumed=store.Load().State;Assert.That(resumed,Is.Not.Null);Assert.That(JsonUtility.ToJson(resumed),Is.EqualTo(JsonUtility.ToJson(state)));
                simulation.Advance(60);var clock=new ManualClock(Start.AddSeconds(60));var session=new GameSession(defs,resumed,clock,store);
                Assert.That(session.ApplyOffline().Seconds,Is.EqualTo(60));
                foreach(var b in state.buildings)Assert.That(JsonUtility.ToJson(resumed.Find(b.id)),Is.EqualTo(JsonUtility.ToJson(b)),b.id);
                Assert.That(JsonUtility.ToJson(resumed.City.stats),Is.EqualTo(JsonUtility.ToJson(state.City.stats)));
                Assert.That(JsonUtility.ToJson(new Trips{values=resumed.deliveries}),Is.EqualTo(JsonUtility.ToJson(new Trips{values=state.deliveries})));
                Assert.That(session.ApplyOffline().Seconds,Is.Zero);
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        [Serializable] private sealed class Trips{public List<DeliveryState> values;}
        [Test] public void SavesRejectWrongCourierAndImpossiblePickupPhases()
        {
            var trip=Depart();trip.cargo=1;Assert.Throws<InvalidDataException>(()=>SaveValidator.Validate(state,defs));trip.cargo=0;
            trip.reservedCargo=0;Assert.Throws<InvalidDataException>(()=>SaveValidator.Validate(state,defs));trip.reservedCargo=8;
            trip.fetching=false;Assert.Throws<InvalidDataException>(()=>SaveValidator.Validate(state,defs));trip.fetching=true;
            trip.workerId="missing";Assert.Throws<InvalidDataException>(()=>SaveValidator.Validate(state,defs));
        }
        [Test] public void ExistingSavesWithoutPickupFieldsRemainReadable()
        {
            simulation.Workers.Remove(baker.id);string payload=JsonUtility.ToJson(state);
            var store=new LocalSaveStore("Temp/BakeryLegacyNeverWritten.json",defs);
            var restored=store.Decode(JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256=LocalSaveStore.Hash(payload)}));
            Assert.That(restored.contentVersion,Is.EqualTo(state.contentVersion));Assert.That(restored.schemaVersion,Is.EqualTo(state.schemaVersion));Near(restored.Find(factory.id).butter,24);
        }
    }
}

using System;
using System.Linq;
using NUnit.Framework;
using Weedtopia.Domain;

namespace Weedtopia.Tests
{
    public sealed class DomainTests
    {
        [Test] public void AmountArithmeticOrderingAndInvariantRoundtrip()
        {
            Assert.That((new Amount(1250) + 25).ToString(), Is.EqualTo("1.28K"));
            Assert.That((new Amount(6, 800) * new Amount(2, 500)).Serialize(), Is.EqualTo("1.2e1301"));
            Assert.That(Amount.Parse("3.14e400"), Is.EqualTo(new Amount(3.14, 400)));
            Assert.That(new Amount(-2, 500) < new Amount(-1, 400), Is.True);
            Assert.That((new Amount(10) - 10), Is.EqualTo(Amount.Zero));
            Assert.That(new Amount(1.9).Floor(), Is.EqualTo(new Amount(1)));
            Assert.That(new Amount(-0.2).Floor(), Is.EqualTo(new Amount(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Amount(double.NaN));
            Assert.Throws<DivideByZeroException>(() => { var ignored = new Amount(1) / Amount.Zero; });
        }
        [Test] public void All5001LevelsResolveContiguousExactBands()
        {
            for(int level=0;level<=5000;level++)
            {
                int tier=VisualTiers.Index(level);
                Assert.That(level, Is.GreaterThanOrEqualTo(VisualTiers.Starts[tier]));
                if(tier<24) Assert.That(level, Is.LessThan(VisualTiers.Starts[tier+1]));
            }
            Assert.That(VisualTiers.Id(5), Is.EqualTo("T02_L005"));
            Assert.That(VisualTiers.Id(5000), Is.EqualTo("T24_L5000"));
            Assert.Throws<ArgumentOutOfRangeException>(()=>VisualTiers.Index(5001));
        }
        [Test] public void CurvesRemainMonotoneBeyondDoubleRange()
        {
            var curve=new Curve{basis=100,growth=1.2}; Amount previous=0;
            for(int level=1;level<=5000;level++){Amount value=curve.At(level);Assert.That(value>=previous,Is.True);Assert.That(value.IsValid,Is.True);previous=value;}
            Assert.That(previous.exponent,Is.GreaterThan(308));
        }
        [Test] public void SeededRandomReplaysAndRejectsInvalidRange()
        {
            var a=new SeededRandom(420);var b=new SeededRandom(420);
            for(int i=0;i<1000;i++)Assert.That(a.Next(100),Is.EqualTo(b.Next(100)));
            Assert.Throws<ArgumentOutOfRangeException>(()=>a.Next(0));
        }
        private static GameDefinitions Fixture()
        {
            var grow=new BuildingDefinition{id="grow",kind=BuildingKind.Grow,managerRole="grow",yield=new Curve{basis=4},capacity=new Curve{basis=8},rate=new Curve{basis=8},cost=new Curve{basis=100}};
            var proc=new BuildingDefinition{id="proc",kind=BuildingKind.Processing,managerRole="proc",capacity=new Curve{basis=8},rate=new Curve{basis=1}};
            var warehouse=new BuildingDefinition{id="warehouse",kind=BuildingKind.Warehouse,managerRole="warehouse",driverRole="driver",capacity=new Curve{basis=8},rate=new Curve{basis=8}};
            return new GameDefinitions{buildings=new[]{grow,proc,warehouse},instances=new[]{new InstanceDefinition{id="g",family="grow"},new InstanceDefinition{id="p",family="proc"},new InstanceDefinition{id="w",family="warehouse"},new InstanceDefinition{id="c",family="warehouse"}},
                routes=new[]{new RouteDefinition{id="gp",from="g",to="p",resource=Resource.Raw},new RouteDefinition{id="pw",from="p",to="w",resource=Resource.Refined},new RouteDefinition{id="wc",from="w",to="c",resource=Resource.Refined,vehicle=true,travelSeconds=5,capacity=4}},
                roles=new[]{"grow","proc","warehouse","driver"}.Select(id=>new RoleDefinition{id=id,attributes=new[]{WorkerAttribute.BuildSpeed,WorkerAttribute.Throughput,WorkerAttribute.Storage,WorkerAttribute.CycleSpeed,WorkerAttribute.Yield}}).ToArray(),quests=Array.Empty<QuestDefinition>(),texts=Array.Empty<TextEntry>()};
        }
        private static GameState State(GameDefinitions d)
        {
            var s=GameState.Create(d,"Tester",new DateTime(2026,9,21,0,0,0,DateTimeKind.Utc));foreach(var b in s.buildings)b.level=1;return s;
        }
        [Test] public void ManualCropStopsAtCapacityAndManagerEnablesTransfer()
        {
            var d=Fixture();var s=State(d);var sim=new Simulation(d,s);sim.Advance(30);
            Assert.That(s.Find("g").raw,Is.EqualTo(new Amount(8)));Assert.That(s.Find("p").raw,Is.EqualTo(Amount.Zero));
            var w=sim.Workers.Grant("grow");Assert.That(sim.Workers.Assign(w.id,"g",0),Is.EqualTo(Failure.None));sim.Advance(1);
            Assert.That(s.Find("p").raw>0,Is.True);
        }
        [Test] public void FullBuffersAndInFlightCargoConserveMass()
        {
            var d=Fixture();var s=State(d);var sim=new Simulation(d,s);
            foreach(var id in new[]{"g","p","w"}){var b=s.Find(id);var w=sim.Workers.Grant(d.Building(b.family).managerRole);sim.Workers.Assign(w.id,id,0);}
            var driver=sim.Workers.Grant("driver");sim.Workers.Assign(driver.id,"w",1);
            sim.Advance(400);
            Amount total=s.buildings.Aggregate(Amount.Zero,(a,b)=>a+b.Total)+s.deliveries.Aggregate(Amount.Zero,(a,b)=>a+b.cargo);
            Assert.That(total,Is.EqualTo(s.City.stats.rawProduced));Assert.That(s.Find("c").refined,Is.EqualTo(new Amount(8)));
            Assert.That(s.City.stats.deliveries,Is.GreaterThan(0));
        }
        [Test] public void RenderFramePartitionDoesNotChangeSimulation()
        {
            var d=Fixture();var a=State(d);var b=State(d);var sa=new Simulation(d,a);var sb=new Simulation(d,b);
            sa.Advance(30);for(int i=0;i<300;i++)sb.Advance(0.1);
            Assert.That(a.simulationSeconds,Is.EqualTo(b.simulationSeconds));Assert.That(a.Find("g").raw,Is.EqualTo(b.Find("g").raw));
        }
        [Test] public void PremiumRequiresSingleUseConfirmationAndCancelDoesNotSpend()
        {
            var d=Fixture();var s=State(d);var service=new UpgradeService(d,s,new WorkerService(d,s));s.money=0;
            Assert.That(service.BuyWithMoney("g"),Is.EqualTo(Failure.GrinderConfirmation));Assert.That(s.grinders,Is.EqualTo(50));
            service.Offer("g",false,out var offer);service.Cancel(offer.Token);Assert.That(service.Confirm(offer.Token),Is.EqualTo(Failure.StaleOffer));
            service.Offer("g",false,out offer);Assert.That(service.Confirm(offer.Token),Is.EqualTo(Failure.None));Assert.That(s.grinders,Is.EqualTo(49));
            Assert.That(service.Confirm(offer.Token),Is.EqualTo(Failure.StaleOffer));Assert.That(s.grinders,Is.EqualTo(49));
        }
        [Test] public void MoneyConstructionAndInsufficientGrindersAreAtomic()
        {
            var d=Fixture();var s=State(d);var service=new UpgradeService(d,s,new WorkerService(d,s));
            Assert.That(service.BuyWithMoney("g"),Is.EqualTo(Failure.None));Assert.That(s.money,Is.EqualTo(new Amount(900)));
            Assert.That(service.BuyWithMoney("g"),Is.EqualTo(Failure.Busy));new Simulation(d,s).Advance(d.Building("grow").BuildSeconds(2));Assert.That(s.Find("g").level,Is.EqualTo(2));
            s.money=0;s.grinders=0;service.Offer("g",false,out var offer);Assert.That(service.Confirm(offer.Token),Is.EqualTo(Failure.Grinders));Assert.That(s.Find("g").buildingSeconds,Is.Zero);
        }
        [Test] public void RolesRarityAssignmentSwapAndDriverTiers()
        {
            var d=Fixture();var s=State(d);var service=new WorkerService(d,s);
            for(int r=0;r<5;r++){var w=service.Grant("grow",r);Assert.That(w.attributes.Distinct().Count(),Is.EqualTo(r+1));}
            var a=s.workers[0];var b=s.workers[1];Assert.That(service.Assign(a.id,"p",0),Is.EqualTo(Failure.Incompatible));
            service.Assign(a.id,"g",0);service.Assign(b.id,"g",0);Assert.That(a.buildingId,Is.Null);Assert.That(b.buildingId,Is.EqualTo("g"));
            s.Find("w").level=5;Assert.That(service.Slots(s.Find("w")),Is.EqualTo(2));service.Remove(b.id);Assert.That(b.slot,Is.EqualTo(-1));
        }
        [Test] public void DriverCannotBeRemovedDuringTripAndBlockedCargoIsRetained()
        {
            var d=Fixture();var s=State(d);var sim=new Simulation(d,s);s.Find("w").refined=4;sim.Work("w");
            var driver=sim.Workers.Grant("driver");Assert.That(sim.Workers.Assign(driver.id,"w",1),Is.EqualTo(Failure.None));
            sim.Advance(1);Assert.That(s.deliveries.Single().remainingSeconds,Is.EqualTo(5));
            Assert.That(sim.Workers.Remove(driver.id),Is.EqualTo(Failure.Busy));sim.Advance(4);
            Assert.That(s.Find("c").refined,Is.EqualTo(Amount.Zero));s.Find("c").refined=8;sim.Advance(1);
            Assert.That(s.deliveries.Single().cargo,Is.EqualTo(new Amount(4)));Assert.That(s.City.stats.deliveries,Is.Zero);
            s.Find("c").refined=0;sim.Advance(1);Assert.That(s.deliveries,Is.Empty);Assert.That(s.Find("c").refined,Is.EqualTo(new Amount(4)));
            Assert.That(s.City.stats.delivered,Is.EqualTo(new Amount(4)));Assert.That(sim.Workers.Remove(driver.id),Is.EqualTo(Failure.None));
        }
        [Test] public void WorkerLevelAndRankCapsAreIndependent()
        {
            var d=Fixture();var s=State(d);s.money=new Amount(1,20);s.cards=100;var service=new WorkerService(d,s);var w=service.Grant("grow");
            w.level=100;Assert.That(service.Improve(w.id,true),Is.EqualTo(Failure.None));Assert.That(service.Improve(w.id,false),Is.EqualTo(Failure.Invalid));
            w.level=1;w.rank=10;Assert.That(service.Improve(w.id,false),Is.EqualTo(Failure.None));Assert.That(service.Improve(w.id,true),Is.EqualTo(Failure.Invalid));
        }
    }
}

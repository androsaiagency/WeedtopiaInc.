using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Application;
using Weedtopia.Persistence;

namespace Weedtopia.Tests
{
    public sealed class PersistenceTests
    {
        private string directory;
        private GameDefinitions defs;
        private DateTime start;
        [SetUp] public void Setup(){directory=Path.GetFullPath("Temp/WeedtopiaSaveTests_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);defs=ProgressionTests.Definitions();start=new DateTime(2026,9,21,12,0,0,DateTimeKind.Utc);}
        [TearDown] public void Cleanup(){if(directory!=null&&Path.GetFileName(directory).StartsWith("WeedtopiaSaveTests_"))Directory.Delete(directory,true);}
        private LocalSaveStore Store()=>new LocalSaveStore(Path.Combine(directory,"save.json"),defs);
        [Test] public void ActiveSessionAutomaticallyPersistsAtThirtySecondsWithoutManualSave()
        {
            var store=Store();var state=GameState.Create(defs,"Autosave",start);var clock=new ManualClock(start);
            var session=new GameSession(defs,state,clock,store);session.CompleteIntro();
            state.money=1234;clock.UtcNow=clock.UtcNow.AddSeconds(29);session.Advance(29);
            Assert.That(store.Load().State.money,Is.EqualTo(new Amount(1000)));
            clock.UtcNow=clock.UtcNow.AddSeconds(1);session.Advance(1);
            Assert.That(store.Load().State.money,Is.EqualTo(new Amount(1234)));
            Assert.That(store.Load().State.lastUtcTicks,Is.EqualTo(clock.UtcNow.Ticks));
        }
        [Test] public void PreviousDistrictSaveAddsOnlyNewPlotsAndPreservesProgressAndCargo()
        {
            var previous=JsonUtility.FromJson<GameDefinitions>(JsonUtility.ToJson(defs));previous.version=1;
            previous.instances=previous.instances.Where(x=>x.introducedVersion<=1).ToArray();
            var ids=previous.instances.Select(x=>x.id).ToArray();previous.routes=previous.routes.Where(x=>ids.Contains(x.from)&&ids.Contains(x.to)).ToArray();
            Assert.That(previous.instances.Length,Is.EqualTo(16));
            var state=GameState.Create(previous,"Bestehende Farm",start);state.introSeen=true;
            state.Find("farm_warehouse").level=1;state.Find("city_warehouse").level=1;
            var worker=new WorkerService(previous,state).Grant("farm_driver");
            Assert.That(new WorkerService(previous,state).Assign(worker.id,"farm_warehouse",1),Is.EqualTo(Failure.None));
            state.deliveries.Add(new DeliveryState{route="farm_city",workerId=worker.id,cargo=7,totalSeconds=10,remainingSeconds=6});
            state.money=12345;state.Find("farm_grow_01").level=25;
            string existing=JsonUtility.ToJson(state.Find("farm_grow_01"));
            var oldStore=new LocalSaveStore(Path.Combine(directory,"save.json"),previous);oldStore.Save(state);
            var migrated=Store().Load().State;Assert.That(migrated,Is.Not.Null);Assert.That(migrated.contentVersion,Is.EqualTo(2));Assert.That(migrated.buildings.Count,Is.EqualTo(56));
            Assert.That(JsonUtility.ToJson(migrated.Find("farm_grow_01")),Is.EqualTo(existing));Assert.That(migrated.money,Is.EqualTo(state.money));
            Assert.That(JsonUtility.ToJson(migrated.deliveries[0]),Is.EqualTo(JsonUtility.ToJson(state.deliveries[0])));
            Assert.That(migrated.buildings.Where(x=>!ids.Contains(x.id)).All(x=>x.level==0&&x.raw==0&&x.refined==0),Is.True);
            Store().Save(migrated);Assert.That(Store().Load().State.buildings.Count,Is.EqualTo(56));
            state.buildings.RemoveAt(0);string payload=JsonUtility.ToJson(state);
            Assert.Throws<InvalidDataException>(()=>Store().Decode(JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256=LocalSaveStore.Hash(payload)})));
        }
        [Test] public void FullTutorialSaveRoundtripIncludesChoicesWorkersCargoAndStats()
        {
            var state=GameState.Create(defs,"Save-Test",start);var clock=new ManualClock(start);var store=Store();var session=new GameSession(defs,state,clock,store);
            // Use the same bounded tutorial budget as the progression test; pickup routes include a return leg.
            for(int i=0;i<1800&&!state.City.freeplay;i++){TutorialDriver.Act(session,clock.UtcNow);clock.UtcNow=clock.UtcNow.AddSeconds(1);session.Advance(1);}
            session.Save();var loaded=store.Load();Assert.That(loaded.State,Is.Not.Null);Assert.That(loaded.Recovered,Is.False);
            Assert.That(JsonUtility.ToJson(loaded.State),Is.EqualTo(JsonUtility.ToJson(state)));Assert.That(state.City.freeplay,Is.True);
            for(int i=0;i<3;i++){store.Save(loaded.State);loaded=store.Load();}Assert.That(loaded.State.City.stats.earned,Is.EqualTo(state.City.stats.earned));
        }
        [Test] public void CorruptMainRecoversAndSavingDoesNotOverwriteValidBackupWithCorruption()
        {
            var store=Store();var state=GameState.Create(defs,"Tester",start);store.Save(state);state.money=900;store.Save(state);
            File.WriteAllText(store.Path,"{broken");var loaded=store.Load();Assert.That(loaded.Recovered,Is.True);Assert.That(loaded.State.money,Is.EqualTo(new Amount(1000)));
            store.Save(loaded.State);Assert.That(store.Load().Recovered,Is.False);Assert.That(Directory.GetFiles(directory,"*.corrupt_*").Length,Is.EqualTo(1));
            Assert.That(store.Decode(File.ReadAllText(store.Path+".bak")).money,Is.EqualTo(new Amount(1000)));
        }
        [Test] public void SchemaOneMigratesAndFutureVersionIsRejected()
        {
            var store=Store();var state=GameState.Create(defs,"Tester",start);state.schemaVersion=1;state.highWaterUtcTicks=0;
            string payload=JsonUtility.ToJson(state);File.WriteAllText(store.Path,JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256=LocalSaveStore.Hash(payload)}));
            var loaded=store.Load().State;Assert.That(loaded.schemaVersion,Is.EqualTo(2));Assert.That(loaded.highWaterUtcTicks,Is.EqualTo(start.Ticks));
            state.schemaVersion=3;payload=JsonUtility.ToJson(state);Assert.Throws<InvalidDataException>(()=>store.Decode(JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256=LocalSaveStore.Hash(payload)})));
        }
        [Test] public void InvalidAssignmentsAndNanNeverReachDisk()
        {
            var store=Store();var s=GameState.Create(defs,"Tester",start);s.money=new Amount{mantissa=double.NaN,exponent=0};Assert.Throws<InvalidDataException>(()=>store.Save(s));Assert.That(store.Exists,Is.False);
            s.money=1000;s.buildings[0].id=s.buildings[1].id;Assert.Throws<InvalidDataException>(()=>store.Save(s));
        }
        [Test] public void OfflineUsesSameSimulationCapsDurationAndDoesNotRewardRepeatedLoadOrRollback()
        {
            var store=Store();var s=GameState.Create(defs,"Tester",start);s.introSeen=true;s.Find("farm_grow_01").level=1;
            var online=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(s));new Simulation(defs,online).Advance(100);
            var clock=new ManualClock(start.AddSeconds(100));var session=new GameSession(defs,s,clock,store);var summary=session.ApplyOffline();
            Assert.That(summary.Seconds,Is.EqualTo(100));Assert.That(s.Find("farm_grow_01").raw,Is.EqualTo(online.Find("farm_grow_01").raw));
            Assert.That(new GameSession(defs,store.Load().State,clock,store).ApplyOffline().Seconds,Is.Zero);
            clock.UtcNow=start.AddDays(-1);Assert.That(session.ApplyOffline().ClockWentBack,Is.True);Assert.That(s.highWaterUtcTicks,Is.EqualTo(start.AddSeconds(100).Ticks));
            clock.UtcNow=start.AddDays(30);Assert.That(session.ApplyOffline().Seconds,Is.EqualTo(defs.maxOfflineSeconds));
        }
        [Test] public void LocalLeaderboardPersistsBestAndOnlineAdapterRemainsDisabled()
        {
            var board=new LocalLeaderboard(Path.Combine(directory,"scores.json"));board.Record(new LeaderboardEntry{player="A",cityId="burning_hills",earned=500});board.Record(new LeaderboardEntry{player="A",cityId="burning_hills",earned=100});
            Assert.That(board.Read().Single().earned,Is.EqualTo(new Amount(500)));Assert.That(new DisabledOnlineLeaderboard().StatusKey,Is.EqualTo("online.disabled"));
        }
    }
}

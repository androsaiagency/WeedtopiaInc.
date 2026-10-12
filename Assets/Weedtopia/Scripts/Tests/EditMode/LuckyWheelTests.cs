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
    public sealed class LuckyWheelTests
    {
        private GameDefinitions defs;
        private GameState state;
        private GameSession session;
        private ManualClock clock;
        [SetUp] public void Setup()
        {
            defs=ProgressionTests.Definitions();clock=new ManualClock(new DateTime(2026,10,1,12,0,0,DateTimeKind.Utc));
            state=GameState.Create(defs,"Wheel Test",clock.UtcNow);session=new GameSession(defs,state,clock);state.grinders=10000;
        }
        private WheelSpinResult Draw()
        {
            Assert.That(session.Wheel.Offer(out var offer),Is.EqualTo(Failure.None));
            Assert.That(session.SpinWheel(offer.Token,out var result),Is.EqualTo(Failure.None));return result;
        }
        [Test] public void BoardHasTenDistinctItemsAndDoesNotChangeOnReadOrReopen()
        {
            string json=JsonUtility.ToJson(state.luckyWheel);uint recruitmentRandom=state.randomState;
            Assert.That(session.Wheel.Remaining.Select(p=>p.id).Distinct().Count(),Is.EqualTo(10));
            for(int i=0;i<20;i++){session.RefreshWheel();session.Wheel.Offer(out _);session.Wheel.Cancel();}
            Assert.That(JsonUtility.ToJson(state.luckyWheel),Is.EqualTo(json));Assert.That(state.randomState,Is.EqualTo(recruitmentRandom));
            var other=GameState.Create(defs,"Other Test",clock.UtcNow);new GameSession(defs,other,clock);
            Assert.That(JsonUtility.ToJson(other.luckyWheel),Is.EqualTo(json));
        }
        [Test] public void TenDrawsFollowExactCostsRemoveEachPrizeAndThenStop()
        {
            int[] costs={0,50,100,200,500,1000,2000,2000,2000,2000};int initial=state.grinders;
            var drawn=new System.Collections.Generic.HashSet<string>();
            for(int i=0;i<10;i++)
            {
                Assert.That(session.Wheel.NextCost,Is.EqualTo(costs[i]));var result=Draw();
                Assert.That(result.Cost,Is.EqualTo(costs[i]));Assert.That(drawn.Add(result.Prize.id),Is.True);
                Assert.That(session.Inventory.Count(result.Prize.id),Is.EqualTo(result.Prize.count));
                Assert.That(session.Wheel.Remaining.Length,Is.EqualTo(9-i));
            }
            Assert.That(state.grinders,Is.EqualTo(initial-9850));Assert.That(state.rewardHistory.Count,Is.EqualTo(10));
            string json=JsonUtility.ToJson(state);
            Assert.That(session.Wheel.Offer(out _),Is.EqualTo(Failure.NotReady));Assert.That(JsonUtility.ToJson(state),Is.EqualTo(json));
            SaveValidator.Validate(state,defs);
        }
        [Test] public void FailedFundsAndCancelledOrReusedOffersDoNotChangeResourcesOrRandom()
        {
            Draw();state.grinders=49;string json=JsonUtility.ToJson(state);
            Assert.That(session.Wheel.Offer(out _),Is.EqualTo(Failure.Grinders));Assert.That(JsonUtility.ToJson(state),Is.EqualTo(json));
            state.grinders=50;session.Wheel.Offer(out var offer);session.Wheel.Cancel();json=JsonUtility.ToJson(state);
            Assert.That(session.SpinWheel(offer.Token,out _),Is.EqualTo(Failure.StaleOffer));Assert.That(JsonUtility.ToJson(state),Is.EqualTo(json));
            session.Wheel.Offer(out offer);session.SpinWheel(offer.Token,out _);json=JsonUtility.ToJson(state);
            Assert.That(session.SpinWheel(offer.Token,out _),Is.EqualTo(Failure.StaleOffer));Assert.That(JsonUtility.ToJson(state),Is.EqualTo(json));
        }
        [Test] public void UtcMidnightResetsBoardFreeSpinAndCountdownTogether()
        {
            Draw();clock.UtcNow=new DateTime(2026,10,1,23,59,59,DateTimeKind.Utc);session.Save();
            Assert.That(session.Wheel.UntilReset.TotalSeconds,Is.EqualTo(1));var oldDay=state.luckyWheel.day;
            clock.UtcNow=clock.UtcNow.AddSeconds(1);session.RefreshWheel();
            Assert.That(state.luckyWheel.day,Is.EqualTo(oldDay+1));Assert.That(session.Wheel.NextCost,Is.Zero);
            Assert.That(session.Wheel.Remaining.Length,Is.EqualTo(10));Assert.That(session.Wheel.UntilReset.TotalHours,Is.EqualTo(24));
            Assert.That(state.inventory.Count,Is.EqualTo(1));
        }
        [Test] public void ForwardJumpDoesNotAccumulateFreeSpinsAndRollbackDoesNotReset()
        {
            Draw();clock.UtcNow=clock.UtcNow.AddDays(10);session.RefreshWheel();Draw();
            long day=state.luckyWheel.day;clock.UtcNow=clock.UtcNow.AddDays(-9);session.RefreshWheel();
            Assert.That(state.luckyWheel.day,Is.EqualTo(day));Assert.That(session.Wheel.NextCost,Is.EqualTo(50));
            Assert.That(session.Wheel.Remaining.Length,Is.EqualTo(9));
        }
        [Test] public void MidnightInvalidatesPaidConfirmationWithoutCharging()
        {
            Draw();session.Wheel.Offer(out var offer);int before=state.grinders;
            clock.UtcNow=clock.UtcNow.AddDays(1);
            Assert.That(session.SpinWheel(offer.Token,out _),Is.EqualTo(Failure.StaleOffer));
            Assert.That(state.grinders,Is.EqualTo(before));Assert.That(session.Wheel.NextCost,Is.Zero);
            Assert.That(state.rewardHistory.Count,Is.EqualTo(1));
        }
        [Test] public void CapacityOverflowNeverChargesOrDraws()
        {
            var prize=session.Wheel.Remaining[0];state.inventory.Add(new ItemStack{id=prize.id,count=int.MaxValue});
            string json=JsonUtility.ToJson(state);Assert.That(session.Wheel.Offer(out _),Is.EqualTo(Failure.Invalid));
            Assert.That(JsonUtility.ToJson(state),Is.EqualTo(json));
        }
        [Test] public void ActualSaveReloadKeepsBoardConsumedFreeSpinAndPrize()
        {
            string path=Path.Combine(UnityEngine.Application.temporaryCachePath,"wheel_"+Guid.NewGuid()+".json");
            try
            {
                var store=new LocalSaveStore(path,defs);session=new GameSession(defs,state,clock,store);Draw();
                var loaded=store.Load();Assert.That(loaded.State,Is.Not.Null,loaded.Message);
                string board=JsonUtility.ToJson(state.luckyWheel);
                var resumed=new GameSession(defs,loaded.State,clock,store);
                Assert.That(JsonUtility.ToJson(loaded.State.luckyWheel),Is.EqualTo(board));
                Assert.That(resumed.Wheel.NextCost,Is.EqualTo(50));Assert.That(loaded.State.inventory.Sum(p=>p.count),Is.EqualTo(state.inventory.Sum(p=>p.count)));
            }
            finally{foreach(var p in new[]{path,path+".bak"})if(File.Exists(p))File.Delete(p);}
        }
        private sealed class BrokenStore : ISaveStore
        {
            public bool Exists=>false;
            public void Save(GameState state)=>throw new IOException("Disk unavailable");
            public SaveLoadResult Load()=>new SaveLoadResult();
        }
        [Test] public void SaveFailureRollsBackPrizeCurrencyBoardReceiptAndRng()
        {
            Draw();session=new GameSession(defs,state,clock,new BrokenStore());session.Wheel.Offer(out var offer);
            string json=JsonUtility.ToJson(state);
            Assert.Throws<IOException>(()=>session.SpinWheel(offer.Token,out _));Assert.That(JsonUtility.ToJson(state),Is.EqualTo(json));
        }
        [Test] public void LegacySaveWithoutWheelIsAcceptedAndReceivesOneBoard()
        {
            state.luckyWheel=null;state.inventory=null;SaveValidator.Validate(state,defs);
            session=new GameSession(defs,state,clock);Assert.That(session.Wheel.Remaining.Length,Is.EqualTo(10));
            Draw();Assert.That(state.inventory,Is.Not.Null);
        }
        [TestCase("count")][TestCase("duplicate")][TestCase("unknown")][TestCase("spins")][TestCase("day")][TestCase("random")]
        public void InvalidWheelSaveIsRejected(string problem)
        {
            var board=state.luckyWheel;
            if(problem=="count")board.prizes[0].count=0;
            if(problem=="duplicate")board.prizes[0].id=board.prizes[1].id;
            if(problem=="unknown")board.prizes[0].id="unknown";
            if(problem=="spins")board.spins=1;
            if(problem=="day")board.day++;
            if(problem=="random")board.randomState=0;
            Assert.Throws<InvalidDataException>(()=>SaveValidator.Validate(state,defs));
        }
    }
}

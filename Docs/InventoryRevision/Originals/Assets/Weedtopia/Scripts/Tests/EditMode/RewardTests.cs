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
    public sealed class RewardTests
    {
        private GameDefinitions defs;
        private GameState state;
        private GameSession session;
        private ManualClock clock;
        [SetUp] public void Setup()
        {
            defs=ProgressionTests.Definitions();clock=new ManualClock(new DateTime(2026,9,30,12,0,0,DateTimeKind.Utc));
            state=GameState.Create(defs,"Belohnung",clock.UtcNow);session=new GameSession(defs,state,clock);session.CompleteIntro();
        }
        private void DriveUntil(Func<bool> condition)
        {
            for(int i=0;i<TutorialDriver.MaxSimulationSeconds&&!condition();i++)
            {TutorialDriver.Act(session,clock.UtcNow,false);clock.UtcNow=clock.UtcNow.AddSeconds(1);session.Advance(1);}
            Assert.That(condition(),Is.True);
        }
        [Test] public void PreviewHasNoStateOrRandomEffects()
        {
            string before=JsonUtility.ToJson(state);
            for(int i=0;i<100;i++){RewardCatalog.Preview(session);RewardCatalog.Inventory(state);RewardCatalog.Achievements(session);}
            Assert.That(JsonUtility.ToJson(state),Is.EqualTo(before));
        }
        [Test] public void MainQuestReceiptsMatchAllActualAwardsAndSurviveSave()
        {
            DriveUntil(()=>state.City.freeplay);
            var questReceipts=state.rewardHistory.Where(r=>r.source==RewardSource.Quest).ToArray();
            foreach(var q in defs.quests.Where(q=>q.moneyReward>0))
            {
                var receipt=questReceipts.Single(r=>r.sourceId==q.id);
                Assert.That(receipt.items.Single(i=>i.kind==RewardKind.Money).quantity,Is.EqualTo(new Amount(q.moneyReward)));
                Assert.That(receipt.items.Where(i=>i.kind==RewardKind.Worker).Select(i=>i.key),Is.EquivalentTo(q.workerRewards));
                foreach(var item in receipt.items.Where(i=>i.kind==RewardKind.Worker))Assert.That(state.workers.Any(w=>w.id==item.reference&&w.role==item.key),Is.True);
                Assert.That(receipt.items.Count,Is.LessThanOrEqualTo(6));
            }
            SaveValidator.Validate(state,defs);
            string path=Path.Combine(Path.GetTempPath(),"reward_"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                var store=new LocalSaveStore(path,defs);store.Save(state);var restored=store.Load().State;
                Assert.That(JsonUtility.ToJson(restored.rewardHistory.Last()),Is.EqualTo(JsonUtility.ToJson(state.rewardHistory.Last())));
                Assert.That(RewardCatalog.Achievements(new GameSession(defs,restored,clock)).Single().key,Is.EqualTo("founder"));
            }
            finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");}
        }
        [Test] public void ReadingHistoryDoesNotRestoreSpentMoneyOrCards()
        {
            DriveUntil(()=>state.City.questIndex>=4);
            string receipt=JsonUtility.ToJson(state.rewardHistory.Last());state.money=5;state.cards=0;
            var inventory=RewardCatalog.Inventory(state);
            Assert.That(inventory.Single(i=>i.kind==RewardKind.Money).quantity,Is.EqualTo(new Amount(5)));
            Assert.That(JsonUtility.ToJson(state.rewardHistory.Last()),Is.EqualTo(receipt));
        }
        [Test] public void OldSaveWithoutLedgerKeepsProgressAndDerivedAwards()
        {
            DriveUntil(()=>state.City.freeplay);state.rewardHistory=null;state.rewardSequence=0;
            SaveValidator.Validate(state,defs);
            Assert.That(RewardCatalog.Achievements(session).Any(r=>r.key=="founder"),Is.True);
            Assert.That(state.rewardHistory,Is.Null);
        }
        [Test] public void HistoryIsBoundedAndSequencesRemainMonotonic()
        {
            for(int i=0;i<120;i++)RewardHistory.Record(state,RewardSource.Daily,i.ToString(),"reward.source.daily",RewardHistory.Currency(1));
            Assert.That(state.rewardHistory.Count,Is.EqualTo(100));Assert.That(state.rewardHistory[0].sequence,Is.EqualTo(21));
            Assert.That(state.rewardHistory.Last().sequence,Is.EqualTo(120));SaveValidator.Validate(state,defs);
        }
        [TestCase("sequence")][TestCase("worker")][TestCase("amount")][TestCase("title")][TestCase("null")]
        public void DamagedRewardHistoryIsRejected(string damage)
        {
            RewardHistory.Record(state,RewardSource.Daily,"day","reward.source.daily",RewardHistory.Currency(20));
            var receipt=state.rewardHistory[0];
            switch(damage)
            {
                case "sequence":receipt.sequence=2;break;
                case "worker":receipt.items[0]=RewardItem.Of(RewardKind.Worker,1,"grow");break;
                case "amount":receipt.items[0].quantity=-1;break;
                case "title":receipt.titleKey="missing";break;
                case "null":receipt.items[0]=null;break;
            }
            Assert.Throws<InvalidDataException>(()=>SaveValidator.Validate(state,defs));
        }
        [Test] public void RecruitmentReceiptIdentifiesTheExactRolledWorker()
        {
            state.City.unlocked=Zone.City;state.cards=1;
            Assert.That(session.Workers.Spin(out var worker),Is.EqualTo(Failure.None));
            var item=state.rewardHistory.Single().items.Single();
            Assert.That(item.reference,Is.EqualTo(worker.id));Assert.That(item.rarity,Is.EqualTo(worker.rarity));
            long sequence=state.rewardSequence;Assert.That(session.Workers.Spin(out _),Is.EqualTo(Failure.Card));Assert.That(state.rewardSequence,Is.EqualTo(sequence));
        }
        [Test] public void DailyReceiptContainsOnlyNewRewardsAndCannotBeClaimedTwice()
        {
            var home=state.buildings.First(b=>b.family=="settlement_plot");home.family="residential_house";home.level=1;
            Assert.That(session.Daily.Claim(clock.UtcNow),Is.EqualTo(Failure.None));
            var receipt=state.rewardHistory.Single();Assert.That(receipt.items.Single().quantity,Is.EqualTo(new Amount(state.cards)));
            Assert.That(session.Daily.Claim(clock.UtcNow),Is.EqualTo(Failure.NotReady));Assert.That(state.rewardHistory.Count,Is.EqualTo(1));
        }
        [TestCase("neighborhood")][TestCase("gourmet")]
        public void FestivalFinaleListsTitleAndExactlyOnePermanentBonus(string plan)
        {
            DriveUntil(()=>state.City.freeplay);session.CityProject.ChoosePlan(plan);
            for(int i=0;i<14400&&!session.CityProject.Completed;i++)
            {CityProjectDriver.Act(session,plan);clock.UtcNow=clock.UtcNow.AddSeconds(1);session.Advance(1);}
            Assert.That(session.CityProject.Completed,Is.True);
            var receipt=state.rewardHistory.Last(r=>r.source==RewardSource.CityProject);
            Assert.That(receipt.items.Count(i=>i.kind==RewardKind.Title),Is.EqualTo(1));
            Assert.That(receipt.items.Single(i=>i.kind==RewardKind.SalesBonus).quantity,Is.EqualTo(new Amount(10)));
            var money=state.money;long sequence=state.rewardSequence;
            Assert.That(session.CityProject.Claim(),Is.EqualTo(Failure.NotReady));Assert.That(state.money,Is.EqualTo(money));Assert.That(state.rewardSequence,Is.EqualTo(sequence));
            SaveValidator.Validate(state,defs);
        }
    }
}

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
    public sealed class InventoryTests
    {
        private GameDefinitions defs;
        private GameState state;
        private GameSession session;
        private ManualClock clock;
        [SetUp] public void Setup()
        {
            defs=ProgressionTests.Definitions();clock=new ManualClock(new DateTime(2026,9,30,12,0,0,DateTimeKind.Utc));
            state=GameState.Create(defs,"Inventar",clock.UtcNow);session=new GameSession(defs,state,clock);session.CompleteIntro();
        }
        private void Grant(string id,int count=1)=>InventoryItems.Grant(state,new[]{new ItemStack{id=id,count=count}},new List<RewardItem>());
        private BuildingState Construction(int seconds)
        {var b=state.Find("farm_grow_01");b.buildingSeconds=seconds;return b;}
        private Failure Use(string id,string target=null)
        {var result=session.Inventory.Offer(id,target,out var offer);return result==Failure.None?session.Inventory.Confirm(offer.Token):result;}
        [Test] public void CatalogContainsEveryDurationAndAllItemsAreObtainableWithoutChangingStarterBalance()
        {
            Assert.That(defs.items.Where(i=>i.effect==ItemEffect.ConstructionTime).Select(i=>i.value),Is.EquivalentTo(new[]{60,300,900,1800,3600,18000,36000,86400}));
            Assert.That(defs.items.Select(i=>i.id),Is.EquivalentTo(defs.quests.SelectMany(q=>q.itemRewards).Select(r=>r.id).Distinct()));
            Assert.That(state.money,Is.EqualTo(new Amount(1000)));Assert.That(state.inventory,Is.Empty);defs.Validate();
        }
        [TestCase("build_1m",60)][TestCase("build_5m",300)][TestCase("build_15m",900)][TestCase("build_30m",1800)]
        [TestCase("build_1h",3600)][TestCase("build_5h",18000)][TestCase("build_10h",36000)][TestCase("build_24h",86400)]
        public void SpeedupOnlyReducesChosenConstructionByItsExactDuration(string id,int seconds)
        {
            var b=Construction(100000);var other=state.Find("farm_processing");other.buildingSeconds=500;
            Grant(id,2);var money=state.money;long time=state.simulationSeconds;uint random=state.randomState;
            Assert.That(Use(id,b.id),Is.EqualTo(Failure.None));Assert.That(b.buildingSeconds,Is.EqualTo(100000-seconds));
            Assert.That(b.level,Is.Zero);Assert.That(other.buildingSeconds,Is.EqualTo(500));Assert.That(session.Inventory.Count(id),Is.EqualTo(1));
            Assert.That(state.money,Is.EqualTo(money));Assert.That(state.simulationSeconds,Is.EqualTo(time));Assert.That(state.randomState,Is.EqualTo(random));
        }
        [Test] public void CompletionClampsAndPreservesFirstBuildDailyEntitlement()
        {
            var b=Construction(45);Grant("build_1m");Assert.That(session.Inventory.Offer("build_1m",b.id,out var offer),Is.EqualTo(Failure.None));
            Assert.That(offer.UnusedSeconds,Is.EqualTo(15));Assert.That(offer.SecondsAfter,Is.Zero);
            Assert.That(session.Inventory.Confirm(offer.Token),Is.EqualTo(Failure.None));Assert.That(b.level,Is.EqualTo(1));Assert.That(b.buildingSeconds,Is.Zero);
            Assert.That(b.dailyTier,Is.EqualTo(1));Assert.That(b.lastDailyDay,Is.EqualTo(state.highWaterUtcTicks/TimeSpan.TicksPerDay-1));
            Assert.That(session.Inventory.Confirm(offer.Token),Is.EqualTo(Failure.StaleOffer));Assert.That(state.inventory,Is.Empty);
        }
        [TestCase("tick")][TestCase("finished")][TestCase("cancel")][TestCase("new_offer")][TestCase("spent")]
        public void StaleOffersNeverSpendAnItem(string cause)
        {
            var b=Construction(120);Grant("build_1m",2);session.Inventory.Offer("build_1m",b.id,out var offer);
            switch(cause)
            {
                case "tick":session.Simulation.Advance(1);break;
                case "finished":ConstructionProgress.Reduce(state,b,120);break;
                case "cancel":session.Inventory.Cancel();break;
                case "new_offer":session.Inventory.Offer("build_1m",b.id,out _);break;
                case "spent":state.inventory.Clear();break;
            }
            int count=session.Inventory.Count("build_1m");Assert.That(session.Inventory.Confirm(offer.Token),Is.EqualTo(Failure.StaleOffer));
            Assert.That(session.Inventory.Count("build_1m"),Is.EqualTo(count));
        }
        [Test] public void OpeningAndCancellingAreReadOnlyAndInvalidTargetsAreRejected()
        {
            var b=Construction(120);Grant("build_1m");string before=JsonUtility.ToJson(state);
            for(int i=0;i<3;i++){session.Inventory.Offer("build_1m",b.id,out _);session.Inventory.Cancel();RewardCatalog.Inventory(state);}
            Assert.That(JsonUtility.ToJson(state),Is.EqualTo(before));
            Assert.That(Use("build_1m","missing"),Is.EqualTo(Failure.NotReady));
            b.cityId="locked_city";Assert.That(Use("build_1m",b.id),Is.EqualTo(Failure.NotReady));Assert.That(session.Inventory.Count("build_1m"),Is.EqualTo(1));
        }
        [Test] public void PackCashAndGrindersGrantExactlyTheirContentsAndAreNotReplayed()
        {
            state.City.unlocked=Zone.City;
            foreach(var id in new[]{"team_pack","cash_crate","grinder_pouch"}){Grant(id);Assert.That(Use(id),Is.EqualTo(Failure.None));Assert.That(Use(id),Is.EqualTo(Failure.NotReady));}
            Assert.That(state.cards,Is.EqualTo(3));Assert.That(state.money,Is.EqualTo(new Amount(1500)));Assert.That(state.grinders,Is.EqualTo(60));
            Assert.That(state.inventory,Is.Empty);Assert.That(state.rewardHistory.Count(r=>r.source==RewardSource.Inventory),Is.EqualTo(3));
        }
        [Test] public void SpecialistChoiceUsesExistingRoleAndRarityRules()
        {
            Grant("specialist_contract");Assert.That(Use("specialist_contract","baker"),Is.EqualTo(Failure.Locked));
            state.City.unlocked=Zone.City;Assert.That(Use("specialist_contract","unknown"),Is.EqualTo(Failure.Invalid));
            uint random=state.randomState;session.Inventory.Offer("specialist_contract","baker",out var offer);Assert.That(state.randomState,Is.EqualTo(random));
            Assert.That(session.Inventory.Confirm(offer.Token),Is.EqualTo(Failure.None));var worker=state.workers.Single();
            Assert.That(worker.role,Is.EqualTo("baker"));Assert.That(worker.rarity,Is.EqualTo(1));Assert.That(worker.attributes.Count,Is.EqualTo(2));
            Assert.That(state.rewardHistory.Single().items.Single().reference,Is.EqualTo(worker.id));
        }
        [TestCase("build_5m")][TestCase("build_15m")][TestCase("build_30m")][TestCase("build_1h")]
        [TestCase("build_5h")][TestCase("build_10h")][TestCase("build_24h")]
        public void SplittingPreservesEverySecondWithoutAConstructionAndIsSingleUse(string id)
        {
            Grant(id,2);var item=defs.items.Single(i=>i.id==id);var before=state.money;
            Assert.That(session.Inventory.OfferSplit(id,out var offer),Is.EqualTo(Failure.None));
            Assert.That(session.Inventory.Count(id),Is.EqualTo(2));
            Assert.That(session.Inventory.Confirm(offer.Token),Is.EqualTo(Failure.None));
            Assert.That(session.Inventory.Count(id),Is.EqualTo(1));
            var smaller=defs.items.Single(i=>i.id==offer.SplitItemId);
            Assert.That(session.Inventory.Count(smaller.id)*smaller.value,Is.EqualTo(item.value));
            Assert.That(session.Inventory.Confirm(offer.Token),Is.EqualTo(Failure.StaleOffer));
            Assert.That(state.money,Is.EqualTo(before));Assert.That(state.simulationSeconds,Is.Zero);
            SaveValidator.Validate(state,defs);
        }
        [Test] public void SmallestBoosterCannotBeSplitAndSplitOverflowCannotSpend()
        {
            Grant("build_1m");Assert.That(session.Inventory.OfferSplit("build_1m",out _),Is.EqualTo(Failure.NotReady));
            Grant("build_5m");state.inventory.Single(i=>i.id=="build_1m").count=int.MaxValue;
            Assert.That(session.Inventory.OfferSplit("build_5m",out _),Is.EqualTo(Failure.Invalid));Assert.That(session.Inventory.Count("build_5m"),Is.EqualTo(1));
        }
        [Test] public void NumericOverflowDoesNotConsumeThePack()
        {
            state.City.unlocked=Zone.City;state.cards=int.MaxValue;Grant("team_pack");
            Assert.That(Use("team_pack"),Is.EqualTo(Failure.Invalid));Assert.That(session.Inventory.Count("team_pack"),Is.EqualTo(1));
        }
        [Test] public void AllQuestItemGrantsMatchPreviewAndCannotBeClaimedTwice()
        {
            for(int n=0;n<TutorialDriver.MaxSimulationSeconds&&!state.City.freeplay;n++)
            {
                if(session.Progression.Ready)
                {
                    var q=session.Progression.Current;var preview=RewardCatalog.Preview(session).Where(i=>i.kind==RewardKind.Item).ToArray();
                    var before=q.itemRewards.ToDictionary(i=>i.id,i=>session.Inventory.Count(i.id));
                    session.Progression.Claim();
                    foreach(var item in preview)Assert.That(session.Inventory.Count(item.key),Is.EqualTo(before[item.key]+(int)item.quantity.ToDouble()));
                    Assert.That(state.City.completedQuests.Count(x=>x==q.id),Is.EqualTo(1));
                }
                else TutorialDriver.Act(session,clock.UtcNow,false);
                clock.UtcNow=clock.UtcNow.AddSeconds(1);session.Advance(1);
            }
            Assert.That(state.City.freeplay,Is.True);SaveValidator.Validate(state,defs);
        }
        [Test] public void VipSpeedupsAreRepeatableButEachOrderPaysOnlyOnce()
        {
            state.vip.Add(new VipState{id="test",cityId=state.activeCity,kind=ConditionKind.Harvest,target=1,reward=20});state.City.stats.rawProduced=1;
            Assert.That(session.Progression.ClaimVip("test"),Is.EqualTo(Failure.None));Assert.That(session.Inventory.Count("build_5m"),Is.EqualTo(1));
            Assert.That(session.Progression.ClaimVip("test"),Is.EqualTo(Failure.NotReady));Assert.That(session.Inventory.Count("build_5m"),Is.EqualTo(1));
        }
        [Test] public void InventoryRoundTripKeepsSpentCountsAndLegacySavesRemainEmpty()
        {
            Grant("build_1m",3);Grant("cash_crate");Use("cash_crate");var b=Construction(100);Use("build_1m",b.id);
            string path=Path.Combine(Path.GetTempPath(),"inventory_"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                var store=new LocalSaveStore(path,defs);store.Save(state);var restored=store.Load().State;
                Assert.That(InventoryItems.Count(restored,"build_1m"),Is.EqualTo(2));Assert.That(InventoryItems.Count(restored,"cash_crate"),Is.Zero);
                Assert.That(restored.money,Is.EqualTo(new Amount(1500)));Assert.That(restored.Find(b.id).buildingSeconds,Is.EqualTo(40));
                restored.inventory=null;SaveValidator.Validate(restored,defs);store.Save(restored);Assert.That(InventoryItems.Count(store.Load().State,"build_1m"),Is.Zero);
            }
            finally{foreach(var p in new[]{path,path+".bak"})if(File.Exists(p))File.Delete(p);}
        }
        [TestCase("negative")][TestCase("duplicate")][TestCase("unknown")][TestCase("null")]
        public void DamagedInventoryIsRejected(string cause)
        {
            Grant("build_1m");if(cause=="negative")state.inventory[0].count=-1;
            if(cause=="duplicate")state.inventory.Add(new ItemStack{id="build_1m",count=1});
            if(cause=="unknown")state.inventory[0].id="not_an_item";if(cause=="null")state.inventory.Add(null);
            Assert.Throws<InvalidDataException>(()=>SaveValidator.Validate(state,defs));
        }
    }
}

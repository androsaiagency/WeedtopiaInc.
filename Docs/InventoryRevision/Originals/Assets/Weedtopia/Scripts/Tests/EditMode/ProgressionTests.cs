using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Application;

namespace Weedtopia.Tests
{
    public sealed class ProgressionTests
    {
        public static GameDefinitions Definitions() => JsonUtility.FromJson<GameDefinitions>(File.ReadAllText("Assets/Weedtopia/Game/Content/BurningHills.json"));
        private static DateTime Start => new DateTime(2026,9,21,12,0,0,DateTimeKind.Utc);
        [Test] public void AuthoredContentAndAllCurvesAreValid()
        {
            var d=Definitions();d.Validate();Assert.That(d.instances.Length,Is.EqualTo(56));Assert.That(d.roles.Length,Is.EqualTo(12));
            foreach(var b in d.buildings)
                for(int n=2;n<=5000;n++)
                {
                    Assert.That(b.cost.At(n)>=b.cost.At(n-1),Is.True);Assert.That(b.capacity.At(n)>=b.capacity.At(n-1),Is.True);
                    Assert.That(b.rate.At(n)>=b.rate.At(n-1),Is.True);if (VisualTiers.Starts.Contains(n)) Assert.That(b.BuildSeconds(n)>=b.BuildSeconds(VisualTiers.Starts[VisualTiers.Index(n)-1]),Is.True);
                    else Assert.That(b.BuildSeconds(n),Is.Zero,b.id+" level "+n);
                }
        }
        [Test] public void FullTutorialReachesFreeplayWithoutPremiumOrStateInjection()
        {
            var d=Definitions();var s=GameState.Create(d,"Testperson",Start);var clock=new ManualClock(Start);var session=new GameSession(d,s,clock);
            int seconds=0;
            for(;seconds<TutorialDriver.MaxSimulationSeconds&&!s.City.freeplay;seconds++)
            { TutorialDriver.Act(session,clock.UtcNow,allowRecovery:false);clock.UtcNow=clock.UtcNow.AddSeconds(1);session.Advance(1); }
            Directory.CreateDirectory("Docs/Implementation/Validation");
            File.WriteAllText("Docs/Implementation/Validation/progression_simulation.json",JsonUtility.ToJson(new Result{utc=DateTime.UtcNow.ToString("O"),seconds=seconds,freeplay=s.City.freeplay,quests=s.City.questIndex,grinders=s.grinders,money=s.money.Serialize(),quest=session.Progression.Current?.id},true));
            if(!s.City.freeplay) File.WriteAllText("Docs/Implementation/Validation/progression_failure_state.json",JsonUtility.ToJson(s,true));
            Assert.That(s.City.freeplay,Is.True,"Stuck at "+session.Progression.Current?.id);
            Assert.That(s.grinders,Is.GreaterThanOrEqualTo(50));Assert.That(s.nextRecoveryWork,Is.Zero);Assert.That(s.City.stats.pastrySold>0,Is.True);
            Assert.That(session.Simulation.Residents(s.activeCity),Is.GreaterThan(0));
            Assert.That(s.City.completedQuests.Distinct().Count(),Is.EqualTo(d.quests.Length));
            Amount money=s.money;Assert.That(session.Progression.Claim(),Is.EqualTo(Failure.NotReady));Assert.That(s.money,Is.EqualTo(money));
        }
        [Serializable] private sealed class Result {public string utc,money,quest;public int seconds,quests,grinders;public bool freeplay;}
        [Test] public void DailyBoundariesCatchupAndClockRollbackAreDefensive()
        {
            var d=Definitions();var s=GameState.Create(d,"Tester",Start);s.City.unlocked=Zone.Industry;
            var home=s.Find("settlement_01");home.family="residential_house";home.level=5;home.lastDailyDay=Start.Date.Ticks/TimeSpan.TicksPerDay-1;
            var session=new GameSession(d,s,new ManualClock(Start));
            Assert.That(session.Daily.Claim(Start),Is.EqualTo(Failure.None));Assert.That(s.cards,Is.EqualTo(2));
            Assert.That(session.Daily.Claim(Start),Is.EqualTo(Failure.NotReady));
            Assert.That(session.Daily.Claim(Start.AddDays(-1)),Is.EqualTo(Failure.NotReady));Assert.That(s.cards,Is.EqualTo(2));
            session.Daily.Claim(Start.AddDays(20));Assert.That(s.cards,Is.EqualTo(16));
            session.Daily.Claim(Start.AddDays(20));Assert.That(s.cards,Is.EqualTo(16));
        }
        [Test] public void LastPlotProtectsMissingTutorialChoiceAndChoiceIsPermanent()
        {
            var d=Definitions();var s=GameState.Create(d,"Tester",Start);s.City.unlocked=Zone.Settlement;var session=new GameSession(d,s,new ManualClock(Start));
            var plots=d.instances.Where(x=>x.family=="settlement_plot").ToArray();
            foreach(var plot in plots.Take(plots.Length-1))Assert.That(session.ChoosePlot(plot.id,"park"),Is.EqualTo(Failure.None));
            string last=plots.Last().id;
            Assert.That(session.ChoosePlot(last,"park"),Is.EqualTo(Failure.ReservedPlot));
            Assert.That(session.ChoosePlot(last,"residential_house"),Is.EqualTo(Failure.None));
            Assert.That(session.ChoosePlot(last,"park"),Is.EqualTo(Failure.Invalid));
        }
        [Test] public void SpinConsumesOneCardAndReplaysExactly()
        {
            var d=Definitions();var a=GameState.Create(d,"Tester",Start,121);var b=GameState.Create(d,"Tester",Start,121);
            a.City.unlocked=b.City.unlocked=Zone.City;a.cards=b.cards=1;
            var wa=new WorkerService(d,a);var wb=new WorkerService(d,b);wa.Spin(out var x);wb.Spin(out var y);
            Assert.That(x.role,Is.EqualTo(y.role));Assert.That(x.rarity,Is.EqualTo(y.rarity));CollectionAssert.AreEqual(x.attributes,y.attributes);
            Assert.That(a.cards,Is.Zero);Assert.That(wa.Spin(out _),Is.EqualTo(Failure.Card));Assert.That(a.workers.Count,Is.EqualTo(1));
        }
        [Test] public void OverspendingRecoveryIsTimeLimitedAndDoesNotSpendPremium()
        {
            var d=Definitions();var s=GameState.Create(d,"Tester",Start);s.money=0;var session=new GameSession(d,s,new ManualClock(Start));
            Assert.That(session.RequestRecoveryWork(),Is.EqualTo(Failure.None));Assert.That(session.RequestRecoveryWork(),Is.EqualTo(Failure.NotReady));
            Assert.That(s.money,Is.EqualTo(new Amount(100)));Assert.That(s.grinders,Is.EqualTo(50));
        }
        [Test] public void NewGameNameIntroReplayAndPauseDoNotDuplicateStartResources()
        {
            var d=Definitions();Assert.Throws<ArgumentException>(()=>GameState.Create(d,"<bad>",Start));
            var s=GameState.Create(d,"Tester",Start);var session=new GameSession(d,s,new ManualClock(Start));
            Assert.That(s.money,Is.EqualTo(new Amount(1000)));Assert.That(s.grinders,Is.EqualTo(50));
            session.CompleteIntro();session.RestartIntro();session.CompleteIntro();session.Paused=true;session.Advance(10);
            Assert.That(s.simulationSeconds,Is.Zero);Assert.That(s.money,Is.EqualTo(new Amount(1000)));
        }
        [Test] public void EveryRoleRarityUsesExactlyItsRelevantAttributePool()
        {
            var d=Definitions();var s=GameState.Create(d,"Tester",Start);var service=new WorkerService(d,s);
            foreach(var role in d.roles)for(int rarity=0;rarity<5;rarity++)
            {var w=service.Grant(role.id,rarity);Assert.That(w.attributes.Count,Is.EqualTo(rarity+1));Assert.That(w.attributes.Distinct().Count(),Is.EqualTo(rarity+1));Assert.That(w.attributes.All(role.attributes.Contains),Is.True);}
        }
        [Test] public void DailyUpgradeDoesNotMultiplyPastDaysAndMidnightIsSingleClaim()
        {
            var d=Definitions();var s=GameState.Create(d,"Tester",Start);s.City.unlocked=Zone.Settlement;
            var home=s.Find("settlement_01");home.family="residential_house";home.level=5;home.dailyTier=1;home.lastDailyDay=Start.Date.Ticks/TimeSpan.TicksPerDay-3;
            var session=new GameSession(d,s,new ManualClock(Start));session.Daily.Claim(Start);Assert.That(s.cards,Is.EqualTo(4));
            session.Daily.Claim(Start.Date.AddDays(1).AddTicks(-1));Assert.That(s.cards,Is.EqualTo(4));
            session.Daily.Claim(Start.Date.AddDays(1));Assert.That(s.cards,Is.EqualTo(6));
            session.Daily.Claim(Start.Date.AddDays(1));Assert.That(s.cards,Is.EqualTo(6));
        }
        [Test] public void VipGoalsAreReachableFromFarmAndRewardsCannotBeClaimedTwice()
        {
            var d=Definitions();var s=GameState.Create(d,"Tester",Start);var clock=new ManualClock(Start);var session=new GameSession(d,s,clock);
            for(int i=0;i<TutorialDriver.MaxSimulationSeconds&&!s.City.freeplay;i++){TutorialDriver.Act(session,clock.UtcNow,allowRecovery:false);clock.UtcNow=clock.UtcNow.AddSeconds(1);session.Advance(1);}
            session.Daily.Claim(clock.UtcNow.AddDays(1));var v=s.vip.First(x=>!x.completed);
            Assert.That(v.kind==ConditionKind.Harvest||v.kind==ConditionKind.Delivered,Is.True);
            // Existing built production chain and public work commands must make the generated goal attainable.
            for(int i=0;i<200&&session.Progression.VipValue(v.kind,v.cityId)<v.target;i++)
            {foreach(var b in s.buildings.Where(x=>x.level>0))session.Simulation.Work(b.id);session.Advance(1);}
            Amount earned=s.City.stats.earned;Assert.That(session.Progression.ClaimVip(v.id),Is.EqualTo(Failure.None));Assert.That(s.City.stats.earned,Is.EqualTo(earned+v.reward));
            Assert.That(v.completed,Is.True);Amount money=s.money;int completed=s.City.stats.vipCompleted;
            Assert.That(session.Progression.ClaimVip(v.id),Is.EqualTo(Failure.NotReady));Assert.That(s.money,Is.EqualTo(money));Assert.That(s.City.stats.vipCompleted,Is.EqualTo(completed));
        }
        [Test] public void OptionalSecondCropAndBakeryDoNotAddUnfundedTutorialManagerRequirements()
        {
            var d=Definitions();var s=GameState.Create(d,"Tester",Start);var clock=new ManualClock(Start);var session=new GameSession(d,s,clock);
            session.CompleteIntro();Assert.That(session.Upgrades.BuyWithMoney("farm_grow_02"),Is.EqualTo(Failure.None));
            for(int i=0;i<TutorialDriver.MaxSimulationSeconds&&!s.City.freeplay;i++)
            {
                if(s.City.unlocked==Zone.Industry&&s.Find("bakery_02").level==0&&s.Find("bakery_02").buildingSeconds==0)session.Upgrades.BuyWithMoney("bakery_02");
                TutorialDriver.Act(session,clock.UtcNow,allowRecovery:false);clock.UtcNow=clock.UtcNow.AddSeconds(1);session.Advance(1);
            }
            Assert.That(s.City.freeplay,Is.True,session.Progression.Current?.id);Assert.That(s.Find("farm_grow_02").level,Is.EqualTo(1));Assert.That(s.Find("bakery_02").level,Is.EqualTo(1));Assert.That(s.grinders,Is.GreaterThanOrEqualTo(50));
        }
        [Test] public void MissingQuestReferencesAndDuplicateIdsAreRejected()
        {
            var d=Definitions();d.quests[0].focus="missing";Assert.Throws<InvalidOperationException>(()=>d.Validate());
            d=Definitions();d.instances[1].id=d.instances[0].id;Assert.Throws<InvalidOperationException>(()=>d.Validate());
            d=Definitions();d.quests[0].conditions[0].kind=ConditionKind.Automated;d.quests[0].conditions[0].target="missing";Assert.Throws<InvalidOperationException>(()=>d.Validate());
        }
        [Test] public void QuestRewardsContributeToLifetimeEarningsOnlyOnce()
        {
            var d=Definitions();var s=GameState.Create(d,"Tester",Start);var session=new GameSession(d,s,new ManualClock(Start));
            session.CompleteIntro();session.Talk("mila");session.Progression.Claim();session.Upgrades.BuyWithMoney("farm_grow_01");session.Advance(s.Find("farm_grow_01").buildingSeconds);
            Assert.That(s.City.stats.earned,Is.EqualTo(Amount.Zero));Assert.That(session.Progression.Claim(),Is.EqualTo(Failure.None));Assert.That(s.City.stats.earned,Is.EqualTo(new Amount(150)));
            Assert.That(session.Progression.Claim(),Is.EqualTo(Failure.NotReady));Assert.That(s.City.stats.earned,Is.EqualTo(new Amount(150)));
        }
        [Test] public void AuthoredGermanTextPreservesUnicodeAcrossContentImport()
        {
            var d=Definitions();Assert.That(d.Text("building.bakery"),Is.EqualTo("B\u00e4ckerei"));Assert.That(d.Text("ui.skip"),Is.EqualTo("Intro \u00fcberspringen"));
            Assert.That(d.Text("ui.stats"),Does.Contain("Verf\u00fcgbar"));
            foreach(var text in d.texts)Assert.That(text.de.Any(c=>c=='\u00c3'||c=='\u00c2'||c=='\ufffd'),Is.False,text.key);
        }
    }
}

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
    public sealed class CityProjectTests
    {
        private static readonly DateTime Start = new DateTime(2026,9,30,12,0,0,DateTimeKind.Utc);
        private GameDefinitions defs;
        private GameState state;
        private ManualClock clock;
        private GameSession session;
        [SetUp] public void Setup()
        {
            defs=ProgressionTests.Definitions();state=GameState.Create(defs,"Stadtfest",Start);clock=new ManualClock(Start);
            session=new GameSession(defs,state,clock);session.CompleteIntro();
        }
        private void TutorialCompleteFixture()
        {
            state.City.questIndex=defs.QuestsFor(state.activeCity).Length;state.City.completedQuests=defs.QuestsFor(state.activeCity).Select(q=>q.id).ToList();
            state.City.freeplay=true;state.City.unlocked=Zone.Industry;
        }
        private GameState RoundTrip(GameState source)
        {
            string payload=JsonUtility.ToJson(source);
            return new LocalSaveStore(Path.Combine("Temp","CityFestivalUnused.json"),defs).Decode(JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256=LocalSaveStore.Hash(payload)}));
        }
        [Test] public void PlanRequiresCompletedTutorialAndChoiceCannotBeRepeatedOrSwitched()
        {
            Assert.That(session.CityProject.ChoosePlan("neighborhood"),Is.EqualTo(Failure.Locked));
            TutorialCompleteFixture();Assert.That(session.CityProject.ChoosePlan("missing"),Is.EqualTo(Failure.Invalid));
            Assert.That(session.CityProject.ChoosePlan("neighborhood"),Is.EqualTo(Failure.None));
            string before=JsonUtility.ToJson(state);
            Assert.That(session.CityProject.ChoosePlan("gourmet"),Is.EqualTo(Failure.Invalid));
            Assert.That(session.CityProject.ChoosePlan("neighborhood"),Is.EqualTo(Failure.Invalid));
            Assert.That(JsonUtility.ToJson(state),Is.EqualTo(before));
        }
        [Test] public void PreviousProductionDoesNotCompleteNewGoalsAndStepRewardsArePaidOnce()
        {
            TutorialCompleteFixture();state.City.stats.refinedProduced=100000;session.CityProject.ChoosePlan("neighborhood");
            state.Find("farm_grow_01").level=state.Find("farm_processing").level=5;
            Assert.That(session.CityProject.Progress(2),Is.EqualTo(Amount.Zero));Assert.That(session.CityProject.Ready,Is.False);
            state.City.stats.refinedProduced+=200;Assert.That(session.CityProject.Ready,Is.True);
            Amount money=state.money;int count=state.workers.Count;
            Assert.That(session.CityProject.Claim(),Is.EqualTo(Failure.None));
            Assert.That(state.money,Is.EqualTo(money+1000));Assert.That(state.workers.Count,Is.EqualTo(count+1));
            string before=JsonUtility.ToJson(state);
            Assert.That(session.CityProject.Claim(),Is.EqualTo(Failure.NotReady));Assert.That(JsonUtility.ToJson(state),Is.EqualTo(before));
            Assert.That(session.CityProject.Progress(2),Is.EqualTo(Amount.Zero));
        }
        [Test] public void LegacySaveWithoutProjectFieldLoadsWithAllTutorialProgressAndResources()
        {
            TutorialCompleteFixture();state.money=43210;state.grinders=37;state.Find("farm_grow_01").level=10;
            string payload=JsonUtility.ToJson(state);
            string field="\"project\":"+JsonUtility.ToJson(state.City.project);
            Assert.That(payload,Does.Contain(field));payload=payload.Replace(","+field,"");
            var store=new LocalSaveStore(Path.Combine("Temp","CityFestivalUnused.json"),defs);
            var loaded=store.Decode(JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256=LocalSaveStore.Hash(payload)}));
            var resumed=new GameSession(defs,loaded,clock);
            Assert.That(resumed.CityProject.Available,Is.True);Assert.That(resumed.CityProject.Plan,Is.Null);
            Assert.That(loaded.money,Is.EqualTo(state.money));Assert.That(loaded.grinders,Is.EqualTo(37));
            Assert.That(loaded.Find("farm_grow_01").level,Is.EqualTo(10));Assert.That(loaded.City.questIndex,Is.EqualTo(16));
        }
        [Test] public void MidStepSaveAndOfflineProgressKeepTheSameBaselineWithoutClaimingRewards()
        {
            TutorialCompleteFixture();state.Find("farm_grow_01").level=state.Find("farm_processing").level=5;
            foreach(string id in new[]{"farm_grow_01","farm_processing"})session.Workers.Assign(session.Workers.Grant(defs.Building(state.Find(id).family).managerRole).id,id,0);
            state.City.stats.refinedProduced=20;session.CityProject.ChoosePlan("gourmet");session.Advance(10);
            var loaded=RoundTrip(state);var resumed=new GameSession(defs,loaded,new ManualClock(Start.AddMinutes(20)));
            Assert.That(JsonUtility.ToJson(loaded.City.project),Is.EqualTo(JsonUtility.ToJson(state.City.project)));
            var baseline=loaded.City.project.baselines[2];resumed.ApplyOffline();
            Assert.That(loaded.City.project.baselines[2],Is.EqualTo(baseline));Assert.That(resumed.CityProject.Progress(2)>0,Is.True);
            Assert.That(loaded.City.project.stepIndex,Is.Zero);Assert.That(loaded.City.project.completedSteps,Is.Empty);
            SaveValidator.Validate(loaded,defs);
        }
        [TestCase("plan")][TestCase("history")][TestCase("baseline")][TestCase("future")][TestCase("locked")]
        public void CorruptChapterProgressIsRejected(string corruption)
        {
            TutorialCompleteFixture();session.CityProject.ChoosePlan("neighborhood");
            switch(corruption)
            {
                case "plan":state.City.project.planId="missing";break;
                case "history":state.City.project.completedSteps.Add("harvest");break;
                case "baseline":state.City.project.baselines[2]=100;break;
                case "future":state.City.project.stepIndex=99;break;
                case "locked":state.City.freeplay=false;state.City.questIndex=0;state.City.completedQuests.Clear();break;
            }
            Assert.Throws<InvalidDataException>(()=>SaveValidator.Validate(state,defs));
        }
        [Test] public void BonusAppliesOnlyAfterCompletionToChosenFamilyAndSurvivesReload()
        {
            TutorialCompleteFixture();session.CityProject.ChoosePlan("gourmet");
            Assert.That(CityProjectRewards.SalesMultiplier(defs,state.City,"bakery"),Is.EqualTo(1));
            var plan=session.CityProject.Plan;state.City.project.stepIndex=plan.steps.Length;
            state.City.project.completedSteps=plan.steps.Select(s=>s.id).ToList();state.City.project.baselines=Array.Empty<Amount>();
            var loaded=RoundTrip(state);
            Assert.That(CityProjectRewards.SalesMultiplier(defs,loaded.City,"bakery"),Is.EqualTo(1.1));
            Assert.That(CityProjectRewards.SalesMultiplier(defs,loaded.City,"coffee_shop"),Is.EqualTo(1));
            Assert.That(CityProjectRewards.SalesMultiplier(defs,new CityState{id="another_city",project=loaded.City.project},"bakery"),Is.EqualTo(1));
            var building=loaded.Find("bakery_01");building.level=1;building.pastry=10;building.workSeconds=30;
            var resumed=new GameSession(defs,loaded,clock);Amount money=loaded.money;
            resumed.Advance(1);Assert.That(loaded.money-money,Is.EqualTo(new Amount(38.5)));
            string before=JsonUtility.ToJson(loaded);Assert.That(resumed.CityProject.Claim(),Is.EqualTo(Failure.NotReady));
            Assert.That(JsonUtility.ToJson(loaded),Is.EqualTo(before));
        }
        [TestCase("neighborhood",420u)][TestCase("gourmet",420u)][TestCase("neighborhood",121u)][TestCase("gourmet",121u)]
        // Whole-campaign simulations retain their tick limits; twelve cities need a larger wall-clock budget.
        [Timeout(600000)]
        public void BothPlansCompleteFrom1000UsingOnlyPlayerCommandsWithoutPremiumOrRecovery(string planId,uint seed)
        {
            state=GameState.Create(defs,"Stadtfest",Start,seed);session=new GameSession(defs,state,clock);
            int seconds=0;
            for(;seconds<TutorialDriver.MaxSimulationSeconds&&!state.City.freeplay;seconds++)
            {TutorialDriver.Act(session,clock.UtcNow,allowRecovery:false);clock.UtcNow=clock.UtcNow.AddSeconds(1);session.Advance(1);}
            Assert.That(state.City.freeplay,Is.True);int tutorialSeconds=seconds;int grinders=state.grinders;
            var timings=new List<int>();int lastStep=0;
            for(seconds=0;seconds<CityProjectDriver.MaxSimulationSeconds&&!session.CityProject.Completed;seconds++)
            {
                CityProjectDriver.Act(session,planId);clock.UtcNow=clock.UtcNow.AddSeconds(1);session.Advance(1);
                if(state.City.project.stepIndex!=lastStep){timings.Add(seconds+1);lastStep=state.City.project.stepIndex;SaveValidator.Validate(state,defs);}
            }
            Directory.CreateDirectory("Docs/CityFestivalRevision");
            File.WriteAllText("Docs/CityFestivalRevision/simulation_"+planId+"_"+seed+".json",JsonUtility.ToJson(new SimulationReceipt{
                plan=planId,seed=seed,tutorialSeconds=tutorialSeconds,chapterSeconds=seconds,completed=session.CityProject.Completed,
                stepCompletionSeconds=timings.ToArray(),money=state.money.ToString(),grinders=state.grinders},true));
            Assert.That(session.CityProject.Completed,Is.True,"Stuck at "+session.CityProject.Current?.id+" with "+state.money);
            Assert.That(state.grinders,Is.EqualTo(grinders));Assert.That(state.nextRecoveryWork,Is.Zero);
            Assert.That(state.City.completedQuests.Count,Is.EqualTo(16));Assert.That(state.City.project.completedSteps.Count,Is.EqualTo(6));
            Assert.That(JsonUtility.ToJson(RoundTrip(state)),Is.EqualTo(JsonUtility.ToJson(state)));
        }
        [Serializable] private sealed class SimulationReceipt
        { public string plan,money;public uint seed;public int tutorialSeconds,chapterSeconds,grinders;public bool completed;public int[] stepCompletionSeconds; }
    }
}

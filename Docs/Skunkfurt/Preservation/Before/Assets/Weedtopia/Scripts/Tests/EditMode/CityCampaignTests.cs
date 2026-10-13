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
    public sealed class CityCampaignTests
    {
        private GameDefinitions defs;
        private GameState state;
        private GameSession session;
        private ManualClock clock;
        private static DateTime Start=>new DateTime(2026,10,2,10,0,0,DateTimeKind.Utc);
        [SetUp] public void Setup()
        {defs=ProgressionTests.Definitions();state=GameState.Create(defs,"Stadttest",Start);state.introSeen=true;clock=new ManualClock(Start);session=new GameSession(defs,state,clock);}
        private void OwnAndTravel()
        {state.City.storyResidentMilestone=1000;Assert.That(session.Campaign.Accept("big_rock_city"),Is.EqualTo(Failure.None));Assert.That(session.Campaign.Travel("big_rock_city"),Is.EqualTo(Failure.None));session.Stories.RefreshUnlocks();}
        private GameState Roundtrip()
        {string payload=JsonUtility.ToJson(state);return new LocalSaveStore(Path.Combine("Temp","CampaignUnused.json"),defs).Decode(JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256=LocalSaveStore.Hash(payload)}));}
        [Test] public void BothCitiesHaveSameFamiliesAndIndependentIdentities()
        {
            defs.Validate();Assert.That(defs.cities.Select(c=>c.id),Is.EquivalentTo(defs.instances.Select(i=>i.cityId).Distinct()));
            var source=defs.instances.Where(i=>i.cityId=="burning_hills").ToArray();var target=defs.instances.Where(i=>i.cityId=="big_rock_city").ToArray();
            Assert.That(target.Length,Is.EqualTo(56));Assert.That(source.Select(i=>i.family),Is.EquivalentTo(target.Select(i=>i.family)));
            Assert.That(defs.QuestsFor("burning_hills").Length,Is.EqualTo(16));Assert.That(defs.QuestsFor("big_rock_city").Length,Is.EqualTo(16));
            Assert.That(defs.routes.Count(r=>r.id.StartsWith("br_")),Is.EqualTo(35));
        }
        [Test] public void InvitationStartsAtOneThousandAndSurvivesPopulationDecline()
        {
            state.City.storyResidentMilestone=999;Assert.That(session.Campaign.Offered("big_rock_city"),Is.False);
            Assert.That(session.Campaign.Accept("big_rock_city"),Is.EqualTo(Failure.NotReady));
            state.City.storyResidentMilestone=1000;Assert.That(session.Campaign.Offered("big_rock_city"),Is.True);
            Assert.That(session.Simulation.Residents("burning_hills"),Is.Zero);Assert.That(session.Campaign.Offered("big_rock_city"),Is.True);
            Assert.That(Roundtrip().City.storyResidentMilestone,Is.EqualTo(1000));
        }
        [Test] public void RealResidentCalculationUnlocksTheMandate()
        {
            var plots=state.buildings.Where(b=>b.cityId=="burning_hills"&&b.family=="settlement_plot").Take(10).ToArray();
            // An early housing tier reaches the original mandate without depending on later city density bonuses.
            foreach(var plot in plots){plot.family="residential_house";plot.level=1750;}
            Assert.That(session.Simulation.Residents("burning_hills"),Is.EqualTo(1000));
            session.Campaign.RefreshMilestones();Assert.That(session.Campaign.Offered("big_rock_city"),Is.True);
            Assert.That(state.City.storyResidentMilestone,Is.EqualTo(1000));
        }
        [Test] public void AcceptPaysExactlyOnceAndKeepsOriginalQuests()
        {
            state.City.storyResidentMilestone=1000;var before=JsonUtility.ToJson(state.City);Amount money=state.money;int cards=state.cards;
            Assert.That(session.TransactCampaign(()=>session.Campaign.Accept("big_rock_city")),Is.EqualTo(Failure.None));
            Assert.That(state.money,Is.EqualTo(money+2500));Assert.That(state.cards,Is.EqualTo(cards+2));
            Assert.That(state.City.questIndex,Is.Zero);Assert.That(session.Progression.Current.id,Is.EqualTo("farm_welcome"));
            Assert.That(state.rewardHistory.Single().source,Is.EqualTo(RewardSource.CityCharter));
            string accepted=JsonUtility.ToJson(state);Assert.That(session.Campaign.Accept("big_rock_city"),Is.EqualTo(Failure.NotReady));Assert.That(JsonUtility.ToJson(state),Is.EqualTo(accepted));
            SaveValidator.Validate(state,defs);
        }
        [Test] public void LockedCityDoesNotSimulateAndCannotBeVisited()
        {
            state.Find("br_farm_grow_01").level=1;session.Advance(20);
            Assert.That(state.cities.Single(c=>c.id=="big_rock_city").stats.playSeconds,Is.Zero);
            Assert.That(state.Find("br_farm_grow_01").raw,Is.EqualTo(Amount.Zero));
            Assert.That(session.Campaign.Travel("big_rock_city"),Is.EqualTo(Failure.Locked));Assert.That(state.activeCity,Is.EqualTo("burning_hills"));
        }
        [Test] public void AssignedWorkersInBurningHillsCannotSatisfyBigRockGoals()
        {
            state.Find("farm_grow_01").level=1;var worker=session.Workers.Grant("grow");session.Workers.Assign(worker.id,"farm_grow_01",0);
            OwnAndTravel();Assert.That(session.Progression.Matches(new QuestCondition{kind=ConditionKind.Assigned,target="cultivation_plot",count=1}),Is.False);
        }
        [Test] public void BothTutorialsFinishUsingPublicCommandsAndNoPremium()
        {
            Drive();Assert.That(state.City.freeplay,Is.True);
            var oldQuests=state.City.completedQuests.ToArray();var oldBuildings=state.buildings.Where(b=>b.cityId=="burning_hills").ToDictionary(b=>b.id,b=>new{b.family,b.level});
            OwnAndTravel();int grinders=state.grinders;Drive();
            Assert.That(state.City.freeplay,Is.True,"Big Rock stalled at "+session.Progression.Current?.id);
            Assert.That(state.City.completedQuests.Count,Is.EqualTo(16));Assert.That(state.grinders,Is.GreaterThanOrEqualTo(grinders));
            CollectionAssert.AreEqual(oldQuests,state.cities.Single(c=>c.id=="burning_hills").completedQuests);
            foreach(var b in state.buildings.Where(b=>b.cityId=="burning_hills")){Assert.That(b.family,Is.EqualTo(oldBuildings[b.id].family));Assert.That(b.level,Is.EqualTo(oldBuildings[b.id].level));}
            SaveValidator.Validate(state,defs);Assert.That(Roundtrip().activeCity,Is.EqualTo("big_rock_city"));
        }
        private void Drive()
        {for(int i=0;i<TutorialDriver.MaxSimulationSeconds&&!state.City.freeplay;i++){TutorialDriver.Act(session,clock.UtcNow,allowRecovery:false);clock.UtcNow=clock.UtcNow.AddSeconds(1);session.Advance(1);}}
        [Test] public void BothOwnedCitiesProduceDuringOnlineAndOfflineSimulation()
        {
            OwnAndTravel();state.Find("farm_grow_01").level=25;state.Find("br_farm_grow_01").level=25;
            session.Advance(8);var original=state.cities.Single(c=>c.id=="burning_hills").stats.rawProduced;var rock=state.City.stats.rawProduced;
            Assert.That(original>0&&rock>0,Is.True);
            state.Find("farm_grow_01").raw=0;state.Find("br_farm_grow_01").raw=0;
            foreach(var id in new[]{"supermarket","br_supermarket"}){state.Find(id).level=1;state.Find(id).workSeconds=30;}
            state.cities.Where(c=>session.Campaign.Accessible(c.id)).ToList().ForEach(c=>c.unlocked=Zone.Industry);
            foreach(var id in new[]{"settlement_01","br_settlement_01"}){state.Find(id).family="residential_house";state.Find(id).level=5;}
            clock.UtcNow=clock.UtcNow.AddSeconds(8);session.Save();clock.UtcNow=clock.UtcNow.AddSeconds(8);var beforeMoney=state.money;var offline=session.ApplyOffline();
            Assert.That(offline.Earned,Is.EqualTo(state.money-beforeMoney));Assert.That(offline.Earned>0,Is.True);
            Assert.That(state.cities.Single(c=>c.id=="burning_hills").stats.rawProduced>original,Is.True);Assert.That(state.City.stats.rawProduced>rock,Is.True);
            var snapshot=JsonUtility.ToJson(state);session.ApplyOffline();Assert.That(JsonUtility.ToJson(state),Is.EqualTo(snapshot));
        }
        [Test] public void CharterAndTravelRollBackOnSaveFailure()
        {
            state.City.storyResidentMilestone=1000;var failed=new GameSession(defs,state,clock,new FailingStore());string before=JsonUtility.ToJson(state);
            Assert.Throws<IOException>(()=>failed.TransactCampaign(()=>failed.Campaign.Accept("big_rock_city")));Assert.That(JsonUtility.ToJson(state),Is.EqualTo(before));
            session.Campaign.Accept("big_rock_city");before=JsonUtility.ToJson(state);
            Assert.Throws<IOException>(()=>failed.TransactCampaign(()=>failed.Campaign.Travel("big_rock_city")));Assert.That(JsonUtility.ToJson(state),Is.EqualTo(before));
        }
        private sealed class FailingStore:ISaveStore
        {public bool Exists=>false;public void Save(GameState s)=>throw new IOException("Disk failure");public SaveLoadResult Load()=>new SaveLoadResult();}
        [Test] public void ExistingVersionTwoSavePreservesStoriesConstructionInventoryWorkersAndCargo()
        {
            var previous=JsonUtility.FromJson<GameDefinitions>(File.ReadAllText("Docs/BigRockCity/Preservation/BurningHills.v2.json"));
            var old=GameState.Create(previous,"Bestehende Stadt",Start);old.introSeen=true;old.City.unlocked=Zone.Industry;old.City.storyResidentMilestone=1000;
            var oldSession=new GameSession(previous,old,clock);old.Find("farm_grow_01").level=10;old.Find("farm_grow_01").buildingSeconds=100;
            old.Find("farm_warehouse").level=1;old.Find("city_warehouse").level=1;
            var worker=oldSession.Workers.Grant("farm_driver");oldSession.Workers.Assign(worker.id,"farm_warehouse",1);
            old.deliveries.Add(new DeliveryState{route="farm_city",workerId=worker.id,cargo=7,totalSeconds=10,remainingSeconds=5});
            oldSession.Stories.Accept("toni","toni_chapter_1","begin");
            // Use the authored first choice; the exact story identity remains checked below.
            if(oldSession.Stories.Active("toni")==null){var offer=oldSession.Stories.Offer("toni");oldSession.Stories.Accept("toni",offer.id,offer.choices[0].id);}
            string oldCity=JsonUtility.ToJson(old.City);var buildings=old.buildings.Select(JsonUtility.ToJson).ToArray();string cargo=JsonUtility.ToJson(old.deliveries[0]);
            string payload=JsonUtility.ToJson(old);var loaded=new LocalSaveStore(Path.Combine("Temp","CampaignMigrationUnused.json"),defs).Decode(JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256=LocalSaveStore.Hash(payload)}));
            Assert.That(loaded.contentVersion,Is.EqualTo(defs.version));Assert.That(loaded.buildings.Count,Is.EqualTo(defs.instances.Length));Assert.That(JsonUtility.ToJson(loaded.cities[0]),Is.EqualTo(oldCity));
            CollectionAssert.AreEqual(buildings,loaded.buildings.Take(buildings.Length).Select(JsonUtility.ToJson));Assert.That(JsonUtility.ToJson(loaded.deliveries[0]),Is.EqualTo(cargo));
            Assert.That(loaded.cities.Single(c=>c.id=="big_rock_city").charterAccepted,Is.False);
            Assert.That(new GameSession(defs,loaded,clock).Campaign.Offered("big_rock_city"),Is.True);
        }
        [Test] public void CorruptOldSaveCannotBeRepairedByAddingNewCityBuildings()
        {
            var previous=JsonUtility.FromJson<GameDefinitions>(File.ReadAllText("Docs/BigRockCity/Preservation/BurningHills.v2.json"));
            var old=GameState.Create(previous,"Beschädigt",Start);old.buildings.RemoveAt(0);string payload=JsonUtility.ToJson(old);
            Assert.Throws<InvalidDataException>(()=>new LocalSaveStore(Path.Combine("Temp","CorruptCampaign.json"),defs).Decode(JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256=LocalSaveStore.Hash(payload)})));
        }
        [Test] public void AllSixInhabitantsAndTheirTwentyFourChaptersCompleteAndRemainSaved()
        {
            OwnAndTravel();state.City.unlocked=Zone.Industry;state.City.storyResidentMilestone=10000;session.Stories.RefreshUnlocks();
            Assert.That(session.Stories.People.Count(),Is.EqualTo(6));Assert.That(session.Stories.People.Sum(p=>p.recurring.Length),Is.EqualTo(18));
            foreach(var goal in session.Stories.People.SelectMany(p=>p.chapters.Concat(p.recurring)).Concat(session.Stories.Collaborations.Select(c=>c.quest)).SelectMany(q=>q.goals))
            {
                Assert.That(goal.count,Is.GreaterThan(0));
                Assert.That(string.Format(defs.Text(goal.textKey),"Gebäude",Amount.Zero,goal.count),Does.EndWith("/"+goal.count));
                if(defs.Text(goal.textKey).Contains("Butterproduktion"))Assert.That(goal.kind,Is.EqualTo(StoryGoalKind.ButterProduced));
                if(defs.Text(goal.textKey).Contains("verkauftes Gebäck"))Assert.That(goal.kind,Is.EqualTo(StoryGoalKind.PastrySold));
            }
            foreach(var person in session.Stories.People.ToArray())for(int i=0;i<person.chapters.Length;i++)Complete(person.id,i%person.chapters[i].choices.Length);
            foreach(var project in session.Stories.Collaborations.ToArray())Complete(project.id,0);
            Assert.That(state.City.vipStories.Sum(p=>p.completed.Count),Is.EqualTo(24));Assert.That(state.City.vipCollaborations.Count(p=>p.completed!=null),Is.EqualTo(3));
            var restored=Roundtrip();Assert.That(JsonUtility.ToJson(restored.City),Is.EqualTo(JsonUtility.ToJson(state.City)));
            foreach(var person in session.Stories.People)Assert.That(session.Stories.Offer(person.id),Is.Not.Null);
        }
        private void Complete(string id,int choice)
        {
            var offer=session.Stories.Offer(id);Assert.That(offer,Is.Not.Null,id);session.Stories.Accept(id,offer.id,offer.choices[choice].id);
            Assert.That(session.Stories.Ready(id),Is.False,"A new counter quest must require fresh progress: "+offer.id);
            foreach(var goal in offer.goals)
            {
                if(goal.kind==StoryGoalKind.Level)
                {
                    var building=state.buildings.FirstOrDefault(b=>b.cityId==state.activeCity&&b.family==goal.target);
                    if(building==null){building=state.buildings.First(b=>b.cityId==state.activeCity&&b.family=="settlement_plot");building.family=goal.target;}
                    building.level=Math.Max(building.level,goal.count);
                }
                else switch(goal.kind)
                {
                    case StoryGoalKind.RawProduced:state.City.stats.rawProduced+=goal.count;break;
                    case StoryGoalKind.RefinedProduced:state.City.stats.refinedProduced+=goal.count;break;
                    case StoryGoalKind.CoffeeSold:state.City.stats.coffeeSold+=goal.count;break;
                    case StoryGoalKind.ButterProduced:state.City.stats.butterProduced+=goal.count;break;
                    case StoryGoalKind.PastrySold:state.City.stats.pastrySold+=goal.count;break;
                    case StoryGoalKind.DeliveredGoods:state.City.stats.delivered+=goal.count;break;
                    case StoryGoalKind.Deliveries:state.City.stats.deliveries+=goal.count;break;
                }
            }
            Assert.That(session.Stories.Claim(id,offer.id),Is.EqualTo(Failure.None),offer.id);SaveValidator.Validate(state,defs);
        }
        [Test] public void ForgedAccessAndCrossCityQuestHistoriesAreRejected()
        {
            state.cities.Single(c=>c.id=="big_rock_city").charterAccepted=true;
            Assert.Throws<InvalidDataException>(()=>SaveValidator.Validate(state,defs));
            state.City.storyResidentMilestone=1000;state.activeCity="big_rock_city";state.City.questIndex=1;state.City.completedQuests.Add("farm_welcome");
            Assert.Throws<InvalidDataException>(()=>SaveValidator.Validate(state,defs));
        }
    }
}

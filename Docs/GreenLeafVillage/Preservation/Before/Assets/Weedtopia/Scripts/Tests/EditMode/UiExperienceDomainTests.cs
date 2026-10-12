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
    public sealed class UiExperienceDomainTests
    {
        private GameDefinitions defs;
        private ManualClock clock;
        private GameState state;
        private GameSession session;
        [SetUp] public void Setup()
        {
            defs=ProgressionTests.Definitions();clock=new ManualClock(new DateTime(2026,10,3,12,0,0,DateTimeKind.Utc));
            state=GameState.Create(defs,"Profiltest",clock.UtcNow);session=new GameSession(defs,state,clock);
        }
        [TestCase("",false)][TestCase("a",false)][TestCase("<Rocko>",false)][TestCase("  Mila  ",true)][TestCase("Änne",true)]
        public void ProfileUsesOneNameRule(string name,bool expected)=>Assert.That(PlayerProfile.ValidName(name),Is.EqualTo(expected));
        [Test] public void FreshTutorialRequiresANameAndProfileEditingPreservesProgress()
        {
            state.profileNamePending=true;Assert.Throws<InvalidOperationException>(()=>session.CompleteIntro());
            string buildings=JsonUtility.ToJson(state.Find("farm_grow_01"));Amount money=state.money;
            Assert.That(session.UpdateProfile("  Kaya  ","player_female_03"),Is.EqualTo(Failure.None));session.CompleteIntro();
            Assert.That(state.playerName,Is.EqualTo("Kaya"));Assert.That(state.playerAvatar,Is.EqualTo("player_female_03"));Assert.That(state.profileNamePending,Is.False);
            Assert.That(state.money,Is.EqualTo(money));Assert.That(JsonUtility.ToJson(state.Find("farm_grow_01")),Is.EqualTo(buildings));
        }
        [Test] public void HistoricalSaveKeepsItsNameAndReceivesAValidAvatar()
        {
            state.introSeen=true;state.money=6789;string payload=JsonUtility.ToJson(state).Replace(",\"playerAvatar\":\"player_male_01\"","").Replace(",\"profileNamePending\":false","");
            var store=new LocalSaveStore(Path.Combine("Temp","ProfileLegacyUnused.json"),defs);
            var loaded=store.Decode(JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256=LocalSaveStore.Hash(payload)}));
            Assert.That(loaded.playerName,Is.EqualTo("Profiltest"));Assert.That(loaded.playerAvatar,Is.EqualTo(PlayerProfile.DefaultAvatar));Assert.That(loaded.profileNamePending,Is.False);Assert.That(loaded.money,Is.EqualTo(new Amount(6789)));
        }
        private sealed class FailingStore:ISaveStore
        {
            public bool Exists=>false;public SaveLoadResult Load()=>new SaveLoadResult();public void Save(GameState value)=>throw new IOException("Owned test failure");
        }
        [TestCase("rocko")][TestCase("mila")][TestCase("jaro")][TestCase("nia")][TestCase("city_host")][TestCase("gardener")][TestCase("baker")][TestCase("driver")]
        public void ResidentAvatarsMigrateWithoutChangingThePlayerOrTown(string oldAvatar)
        {
            state.playerAvatar=oldAvatar;state.money=7654;state.Find("farm_grow_01").level=5;
            string payload=JsonUtility.ToJson(state);var store=new LocalSaveStore(Path.Combine("Temp","AvatarMigrationUnused.json"),defs);
            var loaded=store.Decode(JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256=LocalSaveStore.Hash(payload)}));
            Assert.That(PlayerProfile.ValidAvatar(loaded.playerAvatar),Is.True);Assert.That(loaded.playerAvatar,Is.EqualTo(PlayerProfile.MigrateAvatar(oldAvatar)));
            Assert.That(loaded.playerName,Is.EqualTo(state.playerName));Assert.That(loaded.money,Is.EqualTo(state.money));Assert.That(loaded.Find("farm_grow_01").level,Is.EqualTo(5));
            Assert.That(session.UpdateProfile("Kaya",oldAvatar),Is.EqualTo(Failure.Invalid));
        }
        [Test] public void FailedProfileSaveRestoresThePreviousProfile()
        {
            var failing=new GameSession(defs,state,clock,new FailingStore());long revision=state.revision;
            Assert.Throws<IOException>(()=>failing.UpdateProfile("Anderer Name","player_female_01"));
            Assert.That(state.playerName,Is.EqualTo("Profiltest"));Assert.That(state.playerAvatar,Is.EqualTo(PlayerProfile.DefaultAvatar));Assert.That(state.revision,Is.EqualTo(revision));
        }
        [Test] public void RestartArchiveSurvivesLaterAutosavesAndProfileRoundTrips()
        {
            string folder=Path.GetFullPath("Temp/ProfileRestart_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            try
            {
                var store=new LocalSaveStore(Path.Combine(folder,"save.json"),defs);state.money=4567;store.Save(state);string archive=store.BackupForRestart();string original=File.ReadAllText(archive);
                var fresh=GameState.Create(defs,"Neuling",clock.UtcNow);fresh.profileNamePending=true;var next=new GameSession(defs,fresh,clock,store);
                next.Save();next.UpdateProfile("Spielerin","player_female_04");next.CompleteIntro();next.Save();
                Assert.That(File.ReadAllText(archive),Is.EqualTo(original));Assert.That(store.Decode(original).money,Is.EqualTo(new Amount(4567)));
                Assert.That(store.Load().State.playerAvatar,Is.EqualTo("player_female_04"));Assert.That(store.Load().State.playerName,Is.EqualTo("Spielerin"));
            }
            finally{Directory.Delete(folder,true);}
        }
        [Test] public void CoffeeshopChecklistNamesTheExactShopAndShowsEveryMissingCondition()
        {
            session.CompleteIntro();state.City.questIndex=session.Progression.Quests.Length;state.City.freeplay=true;state.City.unlocked=Zone.Industry;
            state.City.project=new CityProjectState{planId="neighborhood",stepIndex=2,baselines=new Amount[]{0,0,1000}};
            foreach(string id in new[]{"coffee_01","coffee_03","coffee_04"}){state.Find(id).level=5;session.Workers.Assign(session.Workers.Grant("coffee").id,id,0);}
            state.Find("coffee_02").level=1;state.City.stats.coffeeSold=1100;
            var checklist=QuestGuidance.Active(session).Single(q=>q.id=="project");
            Assert.That(checklist.ready,Is.False);Assert.That(checklist.steps[0].text,Does.Contain("Coffeeshop 2"));Assert.That(checklist.steps[0].text,Does.Contain("1 / 5"));
            Assert.That(checklist.steps[1].done,Is.False);Assert.That(checklist.steps[1].focus,Is.EqualTo("coffee_02"));Assert.That(checklist.steps[2].text,Does.Contain("100 / 360"));
            state.Find("coffee_02").level=5;session.Workers.Assign(session.Workers.Grant("coffee").id,"coffee_02",0);state.City.stats.coffeeSold=1360;
            checklist=QuestGuidance.Active(session).Single(q=>q.id=="project");Assert.That(checklist.ready,Is.EqualTo(session.CityProject.Ready));Assert.That(checklist.steps.All(s=>s.done),Is.True);
        }
        [Test] public void GuidanceIsReadOnlyAndEveryTutorialHasSeparateNpcSpeech()
        {
            session.CompleteIntro();string before=JsonUtility.ToJson(state);var guide=QuestGuidance.Active(session);
            Assert.That(guide[0].steps[0].text,Does.Contain("Mila"));Assert.That(JsonUtility.ToJson(state),Is.EqualTo(before));
            foreach(var quest in defs.quests){string speech=defs.Text(quest.bodyKey+".dialogue");Assert.That(speech,Does.Not.StartWith("["));Assert.That(speech,Is.Not.EqualTo(defs.Text(quest.bodyKey)));}
        }
    }
}

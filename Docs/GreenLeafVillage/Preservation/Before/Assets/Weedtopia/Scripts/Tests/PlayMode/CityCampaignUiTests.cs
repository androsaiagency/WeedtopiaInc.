using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object=UnityEngine.Object;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class CityCampaignUiTests
    {
        private GameRoot root;
        private string path;
        private ManualClock clock;
        private byte[] persistentScores;
        private string persistentScorePath;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/Bootstrap.unity");
            yield return WaitCity("burning_hills",false);
            root.enabled=false;clock=new ManualClock(new DateTime(2026,10,2,10,0,0,DateTimeKind.Utc));
            path=Path.GetFullPath(Path.Combine(UnityEngine.Application.temporaryCachePath,"city_campaign_"+Guid.NewGuid()+".json"));
            persistentScorePath=Path.Combine(UnityEngine.Application.persistentDataPath,"local_leaderboard.json");
            persistentScores=File.Exists(persistentScorePath)?File.ReadAllBytes(persistentScorePath):null;
            root.UseTestStore(path,clock);Assert.That(root.NewGame("Reisetest"),Is.True);root.Session.CompleteIntro();root.Ui.Close();yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(root!=null){root.Ui.Rewards.DismissAll();root.Ui.Close();}
            foreach(string file in new[]{path,path+".bak",path+".scores"})if(File.Exists(file))File.Delete(file);
            if(persistentScores==null)Assert.That(File.Exists(persistentScorePath),Is.False,"Tests must not create the player's leaderboard");
            else CollectionAssert.AreEqual(persistentScores,File.ReadAllBytes(persistentScorePath),"Travel must preserve the isolated test leaderboard");
            yield return null;
        }
        private IEnumerator WaitCity(string city,bool session=true)
        {
            for(int i=0;i<200;i++)
            {
                var candidate=Object.FindFirstObjectByType<GameRoot>();
                if(candidate!=null&&candidate.SceneCityId==city&&(!session||candidate.Session!=null)){root=candidate;root.enabled=false;yield break;}
                yield return new WaitForSecondsRealtime(.1f);
            }
            Assert.Fail("City did not become ready: "+city);
        }
        private void Accept()
        {
            root.Session.State.City.storyResidentMilestone=1000;root.Refresh();root.Ui.ShowCities();
            Assert.That(root.Ui.Campaign.accept.interactable,Is.True);root.Ui.Campaign.accept.onClick.Invoke();root.Ui.Rewards.DismissAll();
        }
        [UnityTest] public IEnumerator AuthoredHeaderAndRockoOfferRespectThresholdAndPayOnce()
        {
            var panel=root.Ui.Campaign;Assert.That(panel,Is.Not.Null);Assert.That(panel.headerEntry,Is.Not.Null);Assert.That(panel.journalEntry,Is.Not.Null);
            panel.headerEntry.onClick.Invoke();Assert.That(panel.gameObject.activeSelf,Is.True);Assert.That(panel.accept.interactable,Is.False);
            root.Session.State.City.storyResidentMilestone=999;root.Refresh();Assert.That(panel.offerBadge.activeSelf,Is.False);
            root.Session.State.City.storyResidentMilestone=1000;root.Refresh();Assert.That(panel.offerBadge.activeSelf,Is.True);
            root.Session.State.City.unlocked=Zone.City;root.Ui.Close();root.Select("town_hall");root.Ui.SpeakToSelected();Assert.That(panel.gameObject.activeSelf,Is.True);
            Assert.That(panel.invitation.text,Does.Contain("Startkapital durchgebrannt"));panel.accept.onClick.Invoke();root.Ui.Rewards.DismissAll();
            Assert.That(root.Session.State.money,Is.EqualTo(new Amount(3500)));Assert.That(panel.InvitationCityId,Is.EqualTo("seven_hills"));Assert.That(panel.accept.interactable,Is.False);Assert.That(panel.rockTravel.interactable,Is.True);
            Assert.That(root.Session.State.City.questIndex,Is.Zero);yield return null;
        }
        [UnityTest] public IEnumerator ActualSceneTravelRoundtripPreservesBothCitiesAndTheSavePath()
        {
            Accept();var old=root.Session.State;old.Find("farm_grow_01").level=25;
            root.Ui.Campaign.rockTravel.onClick.Invoke();yield return WaitCity("big_rock_city");
            Assert.That(root.Session.State,Is.SameAs(old));Assert.That(root.SavePath,Is.EqualTo(path));Assert.That(root.Sites.Length,Is.EqualTo(56));
            Assert.That(root.Sites.All(s=>s.InstanceId.StartsWith("br_")),Is.True);Assert.That(root.Session.Progression.Current.id,Is.EqualTo("br_farm_welcome"));
            Assert.That(root.Ui.GetComponentInChildren<CityStatusBar>(true).CityName.text,Is.EqualTo("Big Rock City"));
            root.Session.State.Find("br_farm_grow_01").level=10;root.Session.State.City.stats.rawProduced=17;root.Refresh();
            Assert.That(root.TravelToCity("burning_hills"),Is.EqualTo(Failure.None));yield return WaitCity("burning_hills");
            Assert.That(root.Session.State.Find("farm_grow_01").level,Is.EqualTo(25));Assert.That(root.Session.State.Find("br_farm_grow_01").level,Is.EqualTo(10));
            Assert.That(root.Session.State.cities.Single(c=>c.id=="big_rock_city").stats.rawProduced>=17,Is.True);
            Assert.That(root.Session.Progression.Current.id,Is.EqualTo("farm_welcome"));Assert.That(root.Ui.Stories.rows.Length,Is.EqualTo(15));yield return null;
        }
        [UnityTest] public IEnumerator ContinueOpensTheSavedCityAndDoesNotApplyOfflineTwice()
        {
            Accept();root.TravelToCity("big_rock_city");yield return WaitCity("big_rock_city");
            root.Session.State.Find("br_farm_grow_01").level=25;root.SaveSilently();
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");yield return WaitCity("burning_hills",false);
            root.UseTestStore(path,clock);Assert.That(root.Continue(),Is.True);yield return WaitCity("big_rock_city");
            Assert.That(root.SavePath,Is.EqualTo(path));Assert.That(root.Session.State.Find("br_farm_grow_01").level,Is.EqualTo(25));
            Assert.That(root.Session.ApplyOffline().Seconds,Is.Zero);yield return null;
        }
        [UnityTest] public IEnumerator BigRockInhabitantsUseTheirOwnAuthoredRowsArtAndWorldPicking()
        {
            Accept();root.TravelToCity("big_rock_city");yield return WaitCity("big_rock_city");
            var city=root.Session.State.City;city.unlocked=Zone.Industry;city.storyResidentMilestone=1000;root.Session.Stories.RefreshUnlocks();root.Refresh();
            Assert.That(root.StoryWorld.actors.Length,Is.EqualTo(6));Assert.That(root.StoryWorld.actors.All(a=>a.Visible),Is.True);
            Assert.That(root.Ui.Stories.rows.Length,Is.EqualTo(9));root.Ui.ShowStories("br_berta");
            Assert.That(root.Ui.Stories.nameLabel.text,Does.Contain("Berta"));Assert.That(root.Ui.Stories.biography.text,Does.Contain("B\u00fcrgermeister"));
            Assert.That(root.Ui.Stories.goalLabels[0].text,Does.Contain("0/2"));Assert.That(root.Ui.Stories.goalLabels[1].text,Does.Contain("0/30"));
            root.Ui.Stories.accept.onClick.Invoke();Assert.That(root.Session.Stories.ActiveDefinition("br_berta").id,Is.EqualTo("br_berta_chapter_1"));
            root.Ui.Close();root.StoryWorld.Focus("br_berta");root.CameraRig.SetDiagnosticZoom(17);for(int i=0;i<20;i++)yield return null;
            var actor=root.StoryWorld.Actor("br_berta");var point=root.CameraRig.RenderCamera.WorldToScreenPoint(actor.spriteRenderer.bounds.center);
            Assert.That(root.StoryWorld.TrySelect(root.CameraRig.RenderCamera.ScreenPointToRay(point)),Is.True);
            Assert.That(root.Ui.Stories.SelectedId,Is.EqualTo("br_berta"));Assert.That(root.Ui.Stories.questTitle.text,Does.Contain("Plan"));
            yield return null;
        }
        [UnityTest] public IEnumerator BigRockWorldActorsKeepTheirClickedIdentityWithLockedWorkplaces()
        {
            Accept();root.TravelToCity("big_rock_city");yield return WaitCity("big_rock_city");root.Session.CompleteIntro();root.Ui.Close();root.Select("br_farm_warehouse");string selection=root.SelectedId;root.CameraRig.ShowOverview();
            Assert.That(root.Session.Campaign.AnyOffered,Is.False,"This fixture covers ordinary actor conversations without a city mandate.");
            foreach(var actor in root.Presentation.characters)
            {
                root.Ui.Close();root.CameraRig.Focus(actor.transform.position);root.CameraRig.transform.position=root.CameraRig.TargetPosition;yield return null;
                var point=root.CameraRig.RenderCamera.WorldToScreenPoint(actor.GetComponent<BoxCollider>().bounds.center);
                Assert.That(root.CameraRig.TrySelect(point),Is.True,actor.speakerId);Assert.That(root.Ui.Experience.conversation.SpeakerId,Is.EqualTo(actor.speakerId));Assert.That(root.SelectedId,Is.EqualTo(selection));
            }
            LogAssert.NoUnexpectedReceived();
        }
        private void OfferSevenHills()
        {
            foreach(string city in new[]{"burning_hills","big_rock_city"})
            {
                foreach(var plot in root.Session.State.buildings.Where(b=>b.cityId==city&&b.family=="settlement_plot").Take(10))
                {
                    plot.family="residential_house";plot.level=5000;
                    root.Session.State.workers.Add(new WorkerState{id="resident_fixture_"+plot.id,role="caretaker",buildingId=plot.id,slot=0,level=20,rank=1,
                        attributes=new System.Collections.Generic.List<WorkerAttribute>{WorkerAttribute.Residents}});
                }
            }
            root.Session.Campaign.RefreshMilestones();root.Refresh();root.Ui.ShowCities();
            Assert.That(root.Session.Campaign.UnlockProgress("seven_hills"),Is.EqualTo(5000));
        }
        [UnityTest] public IEnumerator SevenHillsUsesItsAuthoredThirdRowAndRockoOffersItInEitherSourceCity()
        {
            Accept();root.TravelToCity("big_rock_city");yield return WaitCity("big_rock_city");OfferSevenHills();
            var panel=root.Ui.Campaign;
            Assert.That(panel.travelRows.Select(r=>r.cityId),Is.EquivalentTo(root.Definitions.cities.Select(c => c.id)));
            Assert.That(panel.InvitationCityId,Is.EqualTo("seven_hills"));Assert.That(panel.offerBadge.activeSelf,Is.True);
            Assert.That(panel.accept.interactable,Is.True);Assert.That(panel.invitation.text,Does.Contain("Seven Hills"));
            Assert.That(panel.travelRows.Single(r=>r.cityId=="seven_hills").travel.interactable,Is.True);
            root.Session.State.City.unlocked=Zone.City;root.Ui.Close();
            Assert.That(root.Session.State.City.talked.Count(s=>s=="rocko"),Is.Zero);
            root.Ui.SpeakToCharacter("rocko");
            Assert.That(panel.gameObject.activeSelf,Is.True);Assert.That(panel.InvitationCityId,Is.EqualTo("seven_hills"));
            Assert.That(root.Session.State.City.talked.Count(s=>s=="rocko"),Is.EqualTo(1));
            root.Ui.Close();root.Select("br_town_hall");root.Ui.SpeakToSelected();
            Assert.That(panel.gameObject.activeSelf,Is.True);Assert.That(panel.InvitationCityId,Is.EqualTo("seven_hills"));
            Assert.That(root.Session.State.City.talked.Count(s=>s=="rocko"),Is.EqualTo(1));
            Amount before=root.Session.State.money;int cards=root.Session.State.cards;
            panel.accept.onClick.Invoke();root.Ui.Rewards.DismissAll();
            Assert.That(root.Session.State.money,Is.EqualTo(before+2500));Assert.That(root.Session.State.cards,Is.EqualTo(cards+2));
            Assert.That(panel.travelRows.Single(r=>r.cityId=="seven_hills").travel.interactable,Is.True);
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest] public IEnumerator DirectRockoKeepsTheCityArrivalConversationWhenNoCampaignIsOffered()
        {
            Accept();root.TravelToCity("big_rock_city");yield return WaitCity("big_rock_city");
            Assert.That(root.Session.Campaign.AnyOffered,Is.False);root.Ui.Close();
            root.Ui.SpeakToCharacter("rocko");yield return null;
            var conversation=root.Ui.Experience.conversation;
            Assert.That(conversation.gameObject.activeInHierarchy,Is.True);
            Assert.That(conversation.SpeakerId,Is.EqualTo("rocko"));
            Assert.That(conversation.body.text,Is.EqualTo(root.Definitions.Text("br.dialog.rocko").Split(new[]{"\n\n"},StringSplitOptions.RemoveEmptyEntries)[0]));
            Assert.That(root.Ui.Campaign.gameObject.activeSelf,Is.False);
            Assert.That(root.Session.State.City.talked.Count(s=>s=="rocko"),Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ActualThirdCityTravelAndContinuePreserveAllThreeCities()
        {
            Accept();OfferSevenHills();root.Ui.Campaign.accept.onClick.Invoke();root.Ui.Rewards.DismissAll();
            var original=root.Session.State;string[] oldQuests=original.cities.Take(2).Select(c=>string.Join(",",c.completedQuests)).ToArray();
            root.Ui.Campaign.travelRows.Single(r=>r.cityId=="seven_hills").travel.onClick.Invoke();yield return WaitCity("seven_hills");
            Assert.That(root.Session.State,Is.SameAs(original));Assert.That(root.SavePath,Is.EqualTo(path));
            Assert.That(root.Sites.Length,Is.EqualTo(56));Assert.That(root.Sites.All(s=>s.InstanceId.StartsWith("sh_")),Is.True);
            Assert.That(root.Ui.GetComponentInChildren<CityStatusBar>(true).CityName.text,Is.EqualTo("Seven Hills"));
            Assert.That(root.StoryWorld.actors.Length,Is.EqualTo(7));Assert.That(root.StoryWorld.Actor("sh_mila").Visible,Is.True);
            var places=Object.FindObjectsByType<SevenHillsPlaceVisual>(FindObjectsSortMode.None);Assert.That(places.Length,Is.EqualTo(7));
            foreach(var place in places){Assert.That(place.root,Is.SameAs(root));place.Refresh();Assert.That(place.closedPlace.activeSelf,Is.EqualTo(original.City.storyResidentMilestone<place.residents));Assert.That(place.completedPlace.activeSelf,Is.False);}
            int arrivalMilestone=original.City.storyResidentMilestone;original.City.storyResidentMilestone=5000;original.revision++;
            foreach(var place in places){place.Refresh();Assert.That(place.closedPlace.activeSelf,Is.False);Assert.That(place.growingPlace.activeSelf,Is.True);}
            original.City.storyResidentMilestone=arrivalMilestone;original.revision++;foreach(var place in places)place.Refresh();
            Assert.That(root.Ui.Stories.rows.Length,Is.EqualTo(10));root.Ui.ShowStories("sh_mila");Assert.That(root.Ui.Stories.nameLabel.text,Does.Contain("Mila"));
            root.Ui.Close();root.Session.State.Find("sh_farm_grow_01").level=25;root.SaveSilently();
            CollectionAssert.AreEqual(oldQuests,original.cities.Take(2).Select(c=>string.Join(",",c.completedQuests)));
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");yield return WaitCity("burning_hills",false);
            root.UseTestStore(path,clock);Assert.That(root.Continue(),Is.True);yield return WaitCity("seven_hills");
            Assert.That(root.Session.State.Find("sh_farm_grow_01").level,Is.EqualTo(25));Assert.That(root.Session.ApplyOffline().Seconds,Is.Zero);
            root.Ui.ShowCities();root.Ui.Campaign.travelRows.Single(r=>r.cityId=="burning_hills").travel.onClick.Invoke();yield return WaitCity("burning_hills");
            Assert.That(root.Session.State.Find("sh_farm_grow_01").level,Is.EqualTo(25));
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest] public IEnumerator SevenHillsOfferCanBeSelectedWithoutAcceptingTheEarlierBigRockMandate()
        {
            foreach(var plot in root.Session.State.buildings.Where(b=>b.cityId=="burning_hills"&&b.family=="settlement_plot").Take(10))
            {
                plot.family="residential_house";plot.level=5000;
                root.Session.State.workers.Add(new WorkerState{id="resident_fixture_"+plot.id,role="caretaker",buildingId=plot.id,slot=0,level=60,rank=1,
                    attributes=new System.Collections.Generic.List<WorkerAttribute>{WorkerAttribute.Residents}});
            }
            root.Session.Campaign.RefreshMilestones();root.Refresh();root.Ui.ShowCities();
            var panel=root.Ui.Campaign;Assert.That(panel.InvitationCityId,Is.EqualTo("big_rock_city"));
            var third=panel.travelRows.Single(r=>r.cityId=="seven_hills");Assert.That(third.travel.interactable,Is.True);third.travel.onClick.Invoke();
            Assert.That(panel.InvitationCityId,Is.EqualTo("seven_hills"));Assert.That(panel.accept.interactable,Is.True);
            panel.accept.onClick.Invoke();root.Ui.Rewards.DismissAll();
            Assert.That(root.Session.Campaign.Accessible("seven_hills"),Is.True);Assert.That(root.Session.Campaign.Accessible("big_rock_city"),Is.False);
            LogAssert.NoUnexpectedReceived();yield return null;
        }

        private void OfferLittleMontana()
        {
            OfferSevenHills(); root.Ui.Campaign.accept.onClick.Invoke(); root.Ui.Rewards.DismissAll();
            foreach (string city in new[] { "burning_hills", "big_rock_city", "seven_hills" })
            {
                var plots = root.Session.State.buildings.Where(b => b.cityId == city && (b.family == "settlement_plot" || b.family == "residential_house")).Take(10).ToArray();
                foreach (var plot in plots)
                {
                    root.Session.State.workers.RemoveAll(w => w.buildingId == plot.id); plot.family = "residential_house"; plot.level = 5000;
                    root.Session.State.workers.Add(new WorkerState { id = "montana_ui_fixture_" + plot.id, role = "caretaker", buildingId = plot.id,
                        slot = 0, level = 60, rank = 1, attributes = new System.Collections.Generic.List<WorkerAttribute> { WorkerAttribute.Residents } });
                }
                Assert.That(root.Session.Simulation.Residents(city), Is.EqualTo(5000));
            }
            root.Session.Campaign.RefreshMilestones(); root.Refresh(); root.Ui.ShowCities();
            Assert.That(root.Session.Campaign.UnlockProgress("little_montana"), Is.EqualTo(15000));
        }

        [UnityTest] public IEnumerator LittleMontanaFourthRowShowsTheThreeSourceThresholdAndPaysOnce()
        {
            Accept(); var panel = root.Ui.Campaign;
            CollectionAssert.AreEquivalent(root.Definitions.cities.Select(c => c.id), panel.travelRows.Select(r => r.cityId));
            Assert.That(panel.shared.text, Is.EqualTo(root.Definitions.Text("campaign.shared_cities")));
            panel.ShowInvitation("little_montana"); root.Refresh();
            Assert.That(panel.InvitationCityId, Is.EqualTo("little_montana")); Assert.That(panel.accept.interactable, Is.False);
            Assert.That(panel.status.text, Does.Contain("Burning Hills")); Assert.That(panel.status.text, Does.Contain("Big Rock City"));
            Assert.That(panel.status.text, Does.Contain("Seven Hills"));
            panel.ShowInvitation("seven_hills"); OfferLittleMontana();
            var fourth = panel.travelRows.Single(r => r.cityId == "little_montana");
            Assert.That(fourth.travel.interactable, Is.True); fourth.travel.onClick.Invoke();
            Assert.That(panel.InvitationCityId, Is.EqualTo("little_montana")); Assert.That(panel.accept.interactable, Is.True);
            Assert.That(panel.status.text, Is.EqualTo(root.Definitions.Text("campaign.little_montana_notice")));
            Assert.That(panel.invitation.text, Does.Contain("Little Montana")); Amount before = root.Session.State.money; int cards = root.Session.State.cards;
            panel.accept.onClick.Invoke(); root.Ui.Rewards.DismissAll();
            Assert.That(root.Session.State.money, Is.EqualTo(before + 3500)); Assert.That(root.Session.State.cards, Is.EqualTo(cards + 3));
            Assert.That(root.Session.State.rewardHistory.Count(r => r.source == RewardSource.CityCharter && r.sourceId == "little_montana"), Is.EqualTo(1));
            panel.accept.onClick.Invoke(); Assert.That(root.Session.State.money, Is.EqualTo(before + 3500));
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        [UnityTest] public IEnumerator ActualFourthCityTravelContinueAndReturnPreserveAllCitiesAndStories()
        {
            Accept(); OfferLittleMontana(); root.Ui.Campaign.accept.onClick.Invoke(); root.Ui.Rewards.DismissAll();
            var original = root.Session.State; string[] previousQuests = original.cities.Take(3).Select(c => string.Join(",", c.completedQuests)).ToArray();
            root.Ui.Campaign.travelRows.Single(r => r.cityId == "little_montana").travel.onClick.Invoke(); yield return WaitCity("little_montana");
            Assert.That(root.Session.State, Is.SameAs(original)); Assert.That(root.SavePath, Is.EqualTo(path));
            Assert.That(root.Sites.Length, Is.EqualTo(56)); Assert.That(root.Sites.All(s => s.InstanceId.StartsWith("lm_")), Is.True);
            Assert.That(root.Ui.GetComponentInChildren<CityStatusBar>(true).CityName.text, Is.EqualTo("Little Montana"));
            Assert.That(root.Session.Progression.Current.id, Is.EqualTo("lm_arrival"));
            Assert.That(root.StoryWorld.actors.Length, Is.EqualTo(7)); Assert.That(root.StoryWorld.Actor("lm_mara").Visible, Is.True);
            var places = Object.FindObjectsByType<CityPlaceProgressVisual>(FindObjectsSortMode.None); Assert.That(places.Length, Is.EqualTo(6));
            foreach (var place in places)
            {
                Assert.That(place.root, Is.SameAs(root)); Assert.That(place.cityId, Is.EqualTo("little_montana")); place.Refresh();
                Assert.That(place.closedPlace.activeSelf, Is.EqualTo(original.City.storyResidentMilestone < place.residents));
                Assert.That(place.completedPlace.activeSelf, Is.False);
            }
            int arrivalMilestone = original.City.storyResidentMilestone; original.City.storyResidentMilestone = 6000; original.revision++;
            foreach (var place in places) { place.Refresh(); Assert.That(place.closedPlace.activeSelf, Is.False); Assert.That(place.growingPlace.activeSelf, Is.True); }
            original.City.storyResidentMilestone = arrivalMilestone; original.revision++; foreach (var place in places) place.Refresh();
            Assert.That(root.Ui.Stories.rows.Length, Is.EqualTo(10)); root.Ui.ShowStories("lm_mara");
            Assert.That(root.Ui.Stories.nameLabel.text, Does.Contain("Mara")); root.Ui.Stories.accept.onClick.Invoke();
            Assert.That(root.Session.Stories.ActiveDefinition("lm_mara").id, Is.EqualTo("lm_mara_chapter_1"));
            root.Ui.Close(); root.Session.State.Find("lm_farm_grow_01").level = 25; root.SaveSilently();
            string activeStory = UnityEngine.JsonUtility.ToJson(root.Session.Stories.Active("lm_mara"));
            CollectionAssert.AreEqual(previousQuests, original.cities.Take(3).Select(c => string.Join(",", c.completedQuests)));
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity"); yield return WaitCity("burning_hills", false);
            root.UseTestStore(path, clock); Assert.That(root.Continue(), Is.True); yield return WaitCity("little_montana");
            Assert.That(root.SavePath, Is.EqualTo(path)); Assert.That(root.Session.State.Find("lm_farm_grow_01").level, Is.EqualTo(25));
            Assert.That(UnityEngine.JsonUtility.ToJson(root.Session.Stories.Active("lm_mara")), Is.EqualTo(activeStory));
            Assert.That(root.Session.ApplyOffline().Seconds, Is.Zero);
            root.Ui.ShowCities(); root.Ui.Campaign.travelRows.Single(r => r.cityId == "burning_hills").travel.onClick.Invoke(); yield return WaitCity("burning_hills");
            Assert.That(root.Session.State.Find("lm_farm_grow_01").level, Is.EqualTo(25));
            CollectionAssert.AreEqual(previousQuests, root.Session.State.cities.Take(3).Select(c => string.Join(",", c.completedQuests)));
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        [UnityTest] public IEnumerator LittleMontanaRockoUsesItsOwnAuthoredDialogueAfterArrival()
        {
            Accept(); OfferLittleMontana(); root.Ui.Campaign.accept.onClick.Invoke(); root.Ui.Rewards.DismissAll();
            root.TravelToCity("little_montana"); yield return WaitCity("little_montana"); root.Ui.Close();
            Assert.That(root.Session.Campaign.AnyOffered, Is.False); root.Ui.SpeakToCharacter("rocko"); yield return null;
            var conversation = root.Ui.Experience.conversation;
            Assert.That(conversation.gameObject.activeInHierarchy, Is.True); Assert.That(conversation.SpeakerId, Is.EqualTo("rocko"));
            Assert.That(root.Session.Progression.Current.id, Is.EqualTo("lm_arrival")); Assert.That(root.Session.Progression.Ready, Is.True);
            string readyKey = root.Session.Progression.Current.bodyKey + ".dialogue_ready";
            Assert.That(conversation.body.text, Is.EqualTo(root.Definitions.Text(readyKey).Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)[0]),
                "The ready arrival quest must retain priority over the general city conversation.");
            Assert.That(root.Command(() => root.Session.Progression.Claim()), Is.EqualTo(Failure.None));
            Assert.That(root.Session.Progression.Current.id, Is.EqualTo("lm_first_garden"));
            root.Ui.Close(); root.Ui.SpeakToCharacter("rocko"); yield return null;
            Assert.That(conversation.body.text, Is.EqualTo(root.Definitions.Text("lm.dialog.rocko").Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)[0]));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator LittleMontanaProjectStatusExplainsMissingChaptersAfterItsPopulationThreshold()
        {
            Accept(); OfferLittleMontana(); root.Ui.Campaign.accept.onClick.Invoke(); root.Ui.Rewards.DismissAll();
            root.TravelToCity("little_montana"); yield return WaitCity("little_montana");
            var ui = root.Ui.Stories; var city = root.Session.State.City;
            var project = root.Session.Stories.Collaborations.Single(c => c.id == "lm_joint_station_square");
            root.Ui.ShowStories(project.id); yield return null;
            Assert.That(ui.questTitle.text, Is.EqualTo(string.Format(root.Definitions.Text("story.locked"), project.quest.residents)));
            Assert.That(ui.accept.gameObject.activeSelf, Is.False);

            // A historical population fixture isolates the presentation of unmet personal chapters.
            // The charter and scene travel above use the real three-source campaign threshold.
            city.storyResidentMilestone = 6000; city.unlocked = Zone.Industry;
            root.Session.Stories.RefreshUnlocks(); root.Session.State.revision++; root.Refresh();
            ui.togetherTab.onClick.Invoke(); yield return null;
            foreach (var collaboration in root.Session.Stories.Collaborations)
            {
                Assert.That(collaboration.participants.All(root.Session.Stories.Unlocked), Is.True);
                Assert.That(city.storyResidentMilestone, Is.GreaterThanOrEqualTo(collaboration.quest.residents));
                Assert.That(root.Session.Stories.Offer(collaboration.id), Is.Null);
                Assert.That(ui.rows.Single(r => r.id == collaboration.id).status.text, Is.EqualTo(root.Definitions.Text("story.requirements")), collaboration.id);
            }
            ui.Select(project.id, true); yield return null;
            Assert.That(ui.questTitle.text, Is.EqualTo(root.Definitions.Text("story.requirements")));
            Assert.That(ui.accept.gameObject.activeSelf, Is.False);
            foreach (string prerequisite in project.quest.prerequisites)
                Assert.That(ui.description.text, Does.Contain(root.Definitions.Text(root.Session.Stories.Definition(prerequisite).titleKey)));

            // Complete each actual first chapter through its authored accept and claim actions.
            foreach (string participant in project.participants)
            {
                root.Ui.ShowStories(participant); var chapter = root.Session.Stories.Offer(participant);
                Assert.That(chapter.id, Is.EqualTo(participant + "_chapter_1"));
                ui.accept.onClick.Invoke(); Assert.That(root.Session.Stories.Active(participant), Is.Not.Null);
                foreach (var goal in chapter.goals)
                {
                    switch (goal.kind)
                    {
                        case StoryGoalKind.Level:
                            root.Session.State.buildings.First(b => b.cityId == "little_montana" && (b.id == goal.target || b.family == goal.target)).level = goal.count; break;
                        case StoryGoalKind.Deliveries: city.stats.deliveries += goal.count; break;
                        case StoryGoalKind.DeliveredGoods: city.stats.delivered += goal.count; break;
                        case StoryGoalKind.PastrySold: city.stats.pastrySold += goal.count; break;
                        default: Assert.Fail("Unexpected first-chapter fixture goal: " + goal.kind); break;
                    }
                }
                root.Session.State.revision++; root.Refresh(); Assert.That(ui.claim.interactable, Is.True);
                ui.claim.onClick.Invoke(); root.Ui.Rewards.DismissAll();
                Assert.That(root.Session.Stories.Person(participant).completed.Single().questId, Is.EqualTo(chapter.id));
            }
            root.Ui.ShowStories(project.id); yield return null;
            Assert.That(root.Session.Stories.Offer(project.id), Is.SameAs(project.quest));
            Assert.That(ui.rows.Single(r => r.id == project.id).status.text, Is.EqualTo(root.Definitions.Text("story.available")));
            Assert.That(ui.accept.gameObject.activeSelf, Is.True); ui.accept.onClick.Invoke();
            Assert.That(ui.rows.Single(r => r.id == project.id).status.text, Is.EqualTo(root.Definitions.Text("story.active")));
            Assert.That(ui.claim.interactable, Is.False);
            Assert.That(project.quest.goals.Single().kind, Is.EqualTo(StoryGoalKind.CoffeeSold));
            city.stats.coffeeSold += project.quest.goals.Single().count; root.Session.State.revision++; root.Refresh();
            Assert.That(ui.rows.Single(r => r.id == project.id).status.text, Is.EqualTo(root.Definitions.Text("story.ready")));
            Assert.That(ui.claim.interactable, Is.True); ui.claim.onClick.Invoke(); root.Ui.Rewards.DismissAll();
            Assert.That(ui.rows.Single(r => r.id == project.id).status.text, Is.EqualTo(root.Definitions.Text("story.complete")));
            Assert.That(ui.accept.gameObject.activeSelf, Is.False); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator FourthCityTravelActionsUseTheLocalizedTravelVerbInAllFiveLanguages()
        {
            bool hadPreference = PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey);
            string previousPreference = PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey), previousLanguage = root.Localization.Language;
            var languages = new[] { "de", "en", "es", "fr", "ru" };
            var verbs = new[] { "Reisen", "Travel", "Viajar", "Voyager", "\u041f\u043e\u0435\u0445\u0430\u0442\u044c" };
            try
            {
                Assert.That(root.Definitions.texts.Any(t => t.key == "campaign.travel_action"), Is.True);
                root.Ui.ShowCities();
                for (int i = 0; i < languages.Length; i++)
                {
                    Assert.That(root.SetLanguage(languages[i]), Is.True); yield return null;
                    foreach (var row in root.Ui.Campaign.travelRows)
                    {
                        string label = row.travel.GetComponentInChildren<Text>().text;
                        if (row.cityId == root.Session.State.activeCity)
                            Assert.That(label, Is.EqualTo(root.Definitions.Text("campaign.current")), languages[i]);
                        else
                        {
                            var definition = root.Definitions.cities.Single(c => c.id == row.cityId);
                            Assert.That(label, Is.EqualTo(string.Format(root.Definitions.Text("campaign.travel_action"), root.Definitions.Text(definition.nameKey))), languages[i] + ": " + row.cityId);
                            Assert.That(label, Does.Contain(verbs[i]), languages[i] + ": action must describe travel, not a time relation.");
                        }
                    }
                }
                var originalTexts = root.Definitions.texts;
                try
                {
                    root.Definitions.texts = originalTexts.Where(t => t.key != "campaign.travel_action").ToArray();
                    var fourth = root.Ui.Campaign.travelRows.Single(r => r.cityId == "little_montana"); fourth.Refresh();
                    Assert.That(fourth.travel.GetComponentInChildren<Text>().text, Is.EqualTo(string.Format(root.Definitions.Text("campaign.travel"), "Little Montana")),
                        "A historical catalog without the additive action key must retain its original fallback.");
                }
                finally { root.Definitions.texts = originalTexts; root.Refresh(); }
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                root.SetLanguage(previousLanguage);
                if (hadPreference) PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey, previousPreference);
                else PlayerPrefs.DeleteKey(GameRoot.LanguagePreferenceKey);
                PlayerPrefs.Save();
            }
        }

        private void OfferCassaVadela()
        {
            Accept(); OfferLittleMontana(); root.Ui.Campaign.accept.onClick.Invoke(); root.Ui.Rewards.DismissAll();
            foreach (string city in new[] { "burning_hills", "big_rock_city", "seven_hills", "little_montana" })
            {
                foreach (var plot in root.Session.State.buildings.Where(b => b.cityId == city &&
                    (b.family == "settlement_plot" || b.family == "residential_house")).Take(10))
                {
                    root.Session.State.workers.RemoveAll(w => w.buildingId == plot.id);
                    plot.family = "residential_house"; plot.level = 5000;
                    root.Session.State.workers.Add(new WorkerState { id = "cassa_ui_fixture_" + plot.id, role = "caretaker",
                        buildingId = plot.id, slot = 0, level = 60, rank = 3,
                        attributes = new System.Collections.Generic.List<WorkerAttribute> { WorkerAttribute.Residents } });
                }
                Assert.That(root.Session.Simulation.Residents(city), Is.EqualTo(12500));
            }
            root.Session.Campaign.RefreshMilestones(); root.Refresh(); root.Ui.ShowCities();
            Assert.That(root.Session.Campaign.UnlockProgress("cassa_vadela"), Is.EqualTo(50000));
        }

        [UnityTest] public IEnumerator CassaVadelaFifthRowOffersFourCityMandateAndPaysOnlyOnce()
        {
            var panel = root.Ui.Campaign;
            CollectionAssert.AreEquivalent(root.Definitions.cities.Select(c => c.id), panel.travelRows.Select(r => r.cityId));
            panel.ShowInvitation("cassa_vadela"); root.Refresh();
            Assert.That(panel.InvitationCityId, Is.EqualTo("cassa_vadela")); Assert.That(panel.accept.interactable, Is.False);
            Assert.That(panel.status.text, Does.Contain("50000"));
            Assert.That(panel.status.text, Does.Contain("ersten vier"));
            Assert.That(panel.status.text, Does.Contain("zusammen"));
            CollectionAssert.AreEquivalent(new[] { "burning_hills", "big_rock_city", "seven_hills", "little_montana" },
                root.Definitions.cities.Single(city => city.id == "cassa_vadela").unlockSources);
            panel.ShowInvitation("big_rock_city"); OfferCassaVadela();
            var fifth = panel.travelRows.Single(r => r.cityId == "cassa_vadela");
            Assert.That(fifth.travel.interactable, Is.True); fifth.travel.onClick.Invoke();
            Assert.That(panel.InvitationCityId, Is.EqualTo("cassa_vadela")); Assert.That(panel.accept.interactable, Is.True);
            Assert.That(panel.invitation.text, Does.Contain("Cassa Vadela"));
            Amount before = root.Session.State.money; int cards = root.Session.State.cards;
            panel.accept.onClick.Invoke(); root.Ui.Rewards.DismissAll();
            Assert.That(root.Session.State.money, Is.EqualTo(before + 5000)); Assert.That(root.Session.State.cards, Is.EqualTo(cards + 4));
            panel.accept.onClick.Invoke(); Assert.That(root.Session.State.money, Is.EqualTo(before + 5000));
            Assert.That(root.Session.State.rewardHistory.Count(r => r.source == RewardSource.CityCharter && r.sourceId == "cassa_vadela"), Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        [UnityTest] public IEnumerator ActualFifthCityTravelContinueAndReturnPreserveAllCitiesAndAcceptedStory()
        {
            OfferCassaVadela(); var panel = root.Ui.Campaign; panel.ShowInvitation("cassa_vadela"); root.Refresh();
            panel.accept.onClick.Invoke(); root.Ui.Rewards.DismissAll();
            var original = root.Session.State;
            string[] previousQuests = original.cities.Take(4).Select(c => string.Join(",", c.completedQuests)).ToArray();
            string[] previousWorkers = original.workers.Select(w => JsonUtility.ToJson(w)).ToArray();
            panel.travelRows.Single(r => r.cityId == "cassa_vadela").travel.onClick.Invoke(); yield return WaitCity("cassa_vadela");
            Assert.That(root.Session.State, Is.SameAs(original)); Assert.That(root.SavePath, Is.EqualTo(path));
            Assert.That(root.Sites.Length, Is.EqualTo(56)); Assert.That(root.Sites.All(s => s.InstanceId.StartsWith("cv_")), Is.True);
            Assert.That(root.Ui.GetComponentInChildren<CityStatusBar>(true).CityName.text, Is.EqualTo("Cassa Vadela"));
            Assert.That(root.Session.Progression.Current.id, Is.EqualTo("cv_welcome"));
            Assert.That(root.StoryWorld.actors.Length, Is.EqualTo(8)); Assert.That(root.StoryWorld.Actor("cv_ines").Visible, Is.True);
            var places = Object.FindObjectsByType<CityPlaceProgressVisual>(FindObjectsSortMode.None);
            Assert.That(places.Length, Is.EqualTo(3));
            Assert.That(places.All(p => p.cityId == "cassa_vadela" && p.root == root), Is.True);
            var fountain = places.Single(p => p.collaborationId == "cv_joint_fountain_market");
            Assert.That(fountain.keepGrowingPlaceAfterCompletion, Is.False);
            int arrivalMilestone = original.City.storyResidentMilestone;
            original.City.storyResidentMilestone = fountain.residents; original.revision++; fountain.Refresh();
            Assert.That(fountain.closedPlace.activeSelf, Is.False); Assert.That(fountain.growingPlace.activeSelf, Is.True);
            Assert.That(fountain.completedPlace.activeSelf, Is.False);
            var joint = original.City.vipCollaborations.SingleOrDefault(c => c.id == fountain.collaborationId);
            bool fixtureAdded = joint == null;
            if (fixtureAdded) { joint = new VipCollaborationState { id = fountain.collaborationId }; original.City.vipCollaborations.Add(joint); }
            joint.completed = new StoryCompletion { questId = fountain.collaborationId, choiceId = "help" };
            original.revision++; fountain.Refresh();
            Assert.That(fountain.closedPlace.activeSelf, Is.False); Assert.That(fountain.growingPlace.activeSelf, Is.False);
            Assert.That(fountain.completedPlace.activeSelf, Is.True, "The flowing fountain replaces both dry stages after completion.");
            if (fixtureAdded) original.City.vipCollaborations.Remove(joint); else joint.completed = null;
            original.City.storyResidentMilestone = arrivalMilestone; original.revision++; fountain.Refresh();
            root.Ui.ShowStories("cv_ines"); root.Ui.Stories.accept.onClick.Invoke();
            Assert.That(root.Session.Stories.ActiveDefinition("cv_ines").id, Is.EqualTo("cv_ines_chapter_1"));
            string activeStory = JsonUtility.ToJson(root.Session.Stories.Active("cv_ines"));
            root.Ui.Close(); root.Session.State.Find("cv_farm_grow_01").level = 25; root.SaveSilently();
            CollectionAssert.AreEqual(previousWorkers, original.workers.Select(w => JsonUtility.ToJson(w)));
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity"); yield return WaitCity("burning_hills", false);
            root.UseTestStore(path, clock); Assert.That(root.Continue(), Is.True); yield return WaitCity("cassa_vadela");
            Assert.That(root.Session.State.Find("cv_farm_grow_01").level, Is.EqualTo(25));
            Assert.That(JsonUtility.ToJson(root.Session.Stories.Active("cv_ines")), Is.EqualTo(activeStory));
            Assert.That(root.Session.ApplyOffline().Seconds, Is.Zero);
            root.Ui.ShowCities(); root.Ui.Campaign.travelRows.Single(r => r.cityId == "burning_hills").travel.onClick.Invoke(); yield return WaitCity("burning_hills");
            CollectionAssert.AreEqual(previousQuests, root.Session.State.cities.Take(4).Select(c => string.Join(",", c.completedQuests)));
            CollectionAssert.AreEqual(previousWorkers, root.Session.State.workers.Select(w => JsonUtility.ToJson(w)));
            Assert.That(root.Session.State.Find("cv_farm_grow_01").level, Is.EqualTo(25));
            LogAssert.NoUnexpectedReceived(); yield return null;
        }
    }
}

using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class CassaVadelaFountainPlayModeTests
    {
        private const string City = "cassa_vadela", Project = "cv_joint_fountain_market";
        private GameRoot root;
        private string path, persistentScorePath;
        private byte[] persistentScores;
        private ManualClock clock;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/Bootstrap.unity");
            yield return WaitCity("burning_hills",false);
            clock = new ManualClock(new DateTime(2026,10,8,12,0,0,DateTimeKind.Utc));
            path = Path.GetFullPath(Path.Combine(UnityEngine.Application.temporaryCachePath,"cassa_fountain_"+Guid.NewGuid()+".json"));
            persistentScorePath = Path.Combine(UnityEngine.Application.persistentDataPath,"local_leaderboard.json");
            persistentScores = File.Exists(persistentScorePath) ? File.ReadAllBytes(persistentScorePath) : null;
            root.UseTestStore(path,clock); Assert.That(root.NewGame("Brunnenzustand"),Is.True);
            root.Session.CompleteIntro(); root.Ui.Close(); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(root!=null){root.Ui.Rewards.DismissAll();root.Ui.Close();}
            foreach(var file in new[]{path,path+".bak",path+".scores"})if(File.Exists(file))File.Delete(file);
            if(persistentScores==null)Assert.That(File.Exists(persistentScorePath),Is.False);
            else CollectionAssert.AreEqual(persistentScores,File.ReadAllBytes(persistentScorePath));
            yield return null;
        }
        private IEnumerator WaitCity(string city,bool session=true)
        {
            for(int index=0;index<200;index++)
            {
                var candidate=Object.FindFirstObjectByType<GameRoot>();
                if(candidate!=null&&candidate.SceneCityId==city&&(!session||candidate.Session!=null))
                {root=candidate;root.enabled=false;yield break;}
                yield return new WaitForSecondsRealtime(.1f);
            }
            Assert.Fail("City did not become ready: "+city);
        }
        private void AssertStages(bool grown,bool completed)
        {
            var place=Object.FindObjectsByType<CityPlaceProgressVisual>(FindObjectsSortMode.None).Single(p=>p.collaborationId==Project);
            place.Refresh();
            Assert.That(place.keepGrowingPlaceAfterCompletion,Is.False);
            Assert.That(place.closedPlace.activeSelf,Is.EqualTo(!grown&&!completed));
            Assert.That(place.growingPlace.activeSelf,Is.EqualTo(grown&&!completed));
            Assert.That(place.completedPlace.activeSelf,Is.EqualTo(completed));
            var wet=place.completedPlace.GetComponentsInChildren<SpriteRenderer>(true).Single(renderer=>renderer.sprite!=null&&renderer.sprite.texture.name=="fountain_square");
            Assert.That(wet.gameObject.activeInHierarchy,Is.EqualTo(completed));
            foreach(var stage in new[]{place.closedPlace,place.growingPlace})
            {
                var dry=stage.GetComponentsInChildren<SpriteRenderer>(true).Single(renderer=>renderer.sprite!=null&&renderer.sprite.texture.name=="fountain_dry");
                Assert.That(dry.gameObject.activeInHierarchy,Is.EqualTo(stage.activeInHierarchy));
            }
        }
        [UnityTest] public IEnumerator SavedFountainStagesStayDryUntilProjectCompletionAndRestoredWaterSurvivesContinue()
        {
            // A charter fixture isolates scene visibility; campaign thresholds have separate UI/domain coverage.
            var destination = root.Session.State.cities.Single(city=>city.id==City);
            destination.charterResidentMilestone = root.Definitions.cities.Single(city=>city.id==City).unlockResidents;
            destination.charterAccepted = true; root.Session.State.revision++;
            Assert.That(root.TravelToCity(City),Is.EqualTo(Failure.None));yield return WaitCity(City);root.Ui.Close();
            AssertStages(false,false);
            root.Session.State.City.storyResidentMilestone=1800;root.Session.State.revision++;root.Session.Stories.RefreshUnlocks();root.Refresh();yield return null;
            AssertStages(true,false);
            foreach(var id in new[]{"cv_ines","cv_mateo","cv_lucia"})
            {
                var chapter=root.Definitions.vipStories.Single(person=>person.id==id).chapters[0];
                root.Session.Stories.Person(id).completed.Add(new StoryCompletion{questId=chapter.id,choiceId=chapter.choices[0].id});
            }
            var project=root.Definitions.vipCollaborations.Single(definition=>definition.id==Project).quest;
            root.Session.State.City.vipCollaborations.Add(new VipCollaborationState{id=Project,completed=new StoryCompletion{questId=project.id,choiceId=project.choices[0].id}});
            root.Session.State.revision++;root.Refresh();yield return null;AssertStages(true,true);root.SaveSilently();
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");yield return WaitCity("burning_hills",false);
            root.UseTestStore(path,clock);Assert.That(root.Continue(),Is.True);yield return WaitCity(City);root.Ui.Close();yield return null;
            AssertStages(true,true);Assert.That(root.SavePath,Is.EqualTo(path));LogAssert.NoUnexpectedReceived();
        }
    }
}

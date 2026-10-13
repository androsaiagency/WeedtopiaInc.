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
    public sealed class PurpleGardenPlayModeTests
    {
        private const string City = "purple_garden";
        private GameRoot root;
        private string path, persistentScorePath;
        private byte[] persistentScores;
        private ManualClock clock;

        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/Bootstrap.unity");
            yield return WaitCity("burning_hills", false);
            clock = new ManualClock(new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc));
            path = Path.GetFullPath(Path.Combine(UnityEngine.Application.temporaryCachePath, "purplegarden_" + Guid.NewGuid() + ".json"));
            persistentScorePath = Path.Combine(UnityEngine.Application.persistentDataPath, "local_leaderboard.json");
            persistentScores = File.Exists(persistentScorePath) ? File.ReadAllBytes(persistentScorePath) : null;
            root.UseTestStore(path, clock); Assert.That(root.NewGame("Gartenstadtprüfung"), Is.True);
            root.Session.CompleteIntro(); root.Ui.Close(); yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (root != null) { root.Ui.Rewards.DismissAll(); root.Ui.Close(); }
            foreach (var file in new[] { path, path + ".bak", path + ".scores" }) if (File.Exists(file)) File.Delete(file);
            if (persistentScores == null) Assert.That(File.Exists(persistentScorePath), Is.False, "Tests must preserve the player's score store.");
            else CollectionAssert.AreEqual(persistentScores, File.ReadAllBytes(persistentScorePath));
            yield return null;
        }

        private IEnumerator WaitCity(string city, bool session = true)
        {
            for (int index = 0; index < 200; index++)
            {
                var candidate = Object.FindFirstObjectByType<GameRoot>();
                if (candidate != null && candidate.SceneCityId == city && (!session || candidate.Session != null))
                { root = candidate; root.enabled = false; yield break; }
                yield return new WaitForSecondsRealtime(.1f);
            }
            Assert.Fail("City did not become ready: " + city);
        }

        private void OwnPreviousCities()
        {
            // Saved-charter fixtures isolate scene/UI behavior; real population boundaries are covered by domain tests.
            var game = root.Session.State;
            game.cities.Single(city => city.id == "burning_hills").storyResidentMilestone = 1000;
            foreach (var definition in root.Definitions.cities.Where(city => city.introducedVersion < 9 && CityCampaignRules.HasCharter(city)))
            {
                var city = game.cities.Single(state => state.id == definition.id);
                city.charterAccepted = true;
                if (CityCampaignRules.Combined(definition)) city.charterResidentMilestone = definition.unlockResidents;
            }
            game.revision++;
        }

        private void OwnPurpleGarden()
        {
            OwnPreviousCities(); var city = root.Session.State.cities.Single(destination => destination.id == City);
            city.charterResidentMilestone = 500000;
            Assert.That(root.Session.Campaign.Accept(City), Is.EqualTo(Failure.None)); root.Session.State.revision++;
        }

        [UnityTest] public IEnumerator SavedInvitationThresholdBindsAuthoredTravelRowAndPaysBridgeOnlyOnce()
        {
            OwnPreviousCities(); var destination = root.Session.State.cities.Single(city => city.id == City);
            var panel = root.Ui.Campaign; panel.ShowInvitation(City);
            destination.charterResidentMilestone = 499999; root.Session.State.revision++; root.Refresh();
            Assert.That(panel.InvitationCityId, Is.EqualTo(City)); Assert.That(panel.accept.interactable, Is.False);
            var row = panel.travelRows.Single(travel => travel.cityId == City); Assert.That(row.travel.interactable, Is.False);
            destination.charterResidentMilestone = 500000; root.Session.State.revision++; root.Refresh();
            Assert.That(panel.accept.interactable, Is.True); Assert.That(row.travel.interactable, Is.True);
            Amount money = root.Session.State.money; int cards = root.Session.State.cards;
            panel.accept.onClick.Invoke(); root.Ui.Rewards.DismissAll();
            Assert.That(root.Session.State.money, Is.EqualTo(money + 12500)); Assert.That(root.Session.State.cards, Is.EqualTo(cards + 7));
            Assert.That(root.Session.Campaign.Accept(City), Is.EqualTo(Failure.NotReady));
            Assert.That(root.Session.State.money, Is.EqualTo(money + 12500)); Assert.That(row.travel.interactable, Is.True);
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator AuthoredEighthCityTravelKeepsAllCitiesAndAssignedWorkersThenContinuesInPurpleGarden()
        {
            OwnPurpleGarden(); var state = root.Session.State;
            state.Find("farm_grow_01").level = 25;
            state.workers.Add(new WorkerState { id = "purplegarden_playmode_grow", role = "grow", buildingId = "farm_grow_01", slot = 0,
                attributes = new System.Collections.Generic.List<WorkerAttribute> { root.Definitions.Role("grow").attributes[0] } });
            var before = state.cities.ToDictionary(city => city.id, city => string.Join(",", city.completedQuests));
            root.Ui.ShowCities(); root.Ui.Campaign.travelRows.Single(row => row.cityId == City).travel.onClick.Invoke();
            yield return WaitCity(City); root.Ui.Close();
            Assert.That(root.Session.State, Is.SameAs(state)); Assert.That(root.SavePath, Is.EqualTo(path));
            Assert.That(root.Sites.Length, Is.EqualTo(56)); Assert.That(root.Sites.All(site => site.InstanceId.StartsWith("pg_", StringComparison.Ordinal)), Is.True);
            Assert.That(root.Presentation.routes.Length, Is.EqualTo(35));
            Assert.That(root.Ui.GetComponentInChildren<CityStatusBar>(true).CityName.text, Is.EqualTo("Purple Garden"));
            Assert.That(root.Session.Progression.Current.id, Does.StartWith("pg_"));
            state.Find("pg_farm_grow_01").level = 25; root.SaveSilently(); clock.UtcNow = clock.UtcNow.AddSeconds(8);
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity"); yield return WaitCity("burning_hills", false);
            root.UseTestStore(path, clock); Assert.That(root.Continue(), Is.True); yield return WaitCity(City); root.Ui.Close();
            Assert.That(root.SavePath, Is.EqualTo(path)); Assert.That(root.Session.State.Find("pg_farm_grow_01").level, Is.EqualTo(25));
            var assigned = root.Session.State.workers.Single(worker => worker.id == "purplegarden_playmode_grow");
            Assert.That(assigned.buildingId, Is.EqualTo("farm_grow_01")); Assert.That(assigned.slot, Is.Zero);
            foreach (var city in root.Session.State.cities) Assert.That(string.Join(",", city.completedQuests), Is.EqualTo(before[city.id]));
            Assert.That(root.Session.ApplyOffline().Seconds, Is.Zero); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator ResidentChoicesAndCompletedPlacesRemainVisibleAfterSaveAndContinue()
        {
            OwnPurpleGarden(); Assert.That(root.TravelToCity(City), Is.EqualTo(Failure.None)); yield return WaitCity(City); root.Ui.Close();
            var city = root.Session.State.City; city.unlocked = Zone.Industry; city.storyResidentMilestone = 20000;
            root.Session.Stories.RefreshUnlocks(); root.Session.State.revision++; root.Refresh(); yield return null;
            Assert.That(root.StoryWorld.actors.Length, Is.EqualTo(8)); Assert.That(root.StoryWorld.actors.All(actor => actor.Visible), Is.True);
            var person = root.Definitions.vipStories.Single(resident => resident.id == "pg_theo");
            var progress = root.Session.Stories.Person(person.id);
            for (int chapter = 0; chapter < 3; chapter++) progress.completed.Add(new StoryCompletion {
                questId = person.chapters[chapter].id,
                choiceId = person.chapters[chapter].choices[chapter == 2 ? person.chapters[chapter].choices.Length - 1 : 0].id });
            foreach (var joint in root.Definitions.vipCollaborations.Where(project => project.cityId == City))
            {
                foreach (string prerequisite in joint.quest.prerequisites)
                {
                    var owner = root.Definitions.vipStories.Single(resident => resident.cityId == City && resident.chapters.Any(chapter => chapter.id == prerequisite));
                    var story = root.Session.Stories.Person(owner.id); int required = Array.FindIndex(owner.chapters, chapter => chapter.id == prerequisite);
                    while (story.completed.Count <= required)
                    {
                        var chapter = owner.chapters[story.completed.Count];
                        story.completed.Add(new StoryCompletion { questId = chapter.id, choiceId = chapter.choices[0].id });
                    }
                }
                city.vipCollaborations.Add(new VipCollaborationState { id = joint.id, completed = new StoryCompletion {
                    questId = joint.quest.id, choiceId = joint.quest.choices[0].id } });
            }
            root.Session.State.revision++; root.Refresh(); yield return null; AssertWorldProgress(); root.SaveSilently();
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity"); yield return WaitCity("burning_hills", false);
            root.UseTestStore(path, clock); Assert.That(root.Continue(), Is.True); yield return WaitCity(City); root.Ui.Close(); yield return null;
            AssertWorldProgress(); Assert.That(root.Session.Stories.Person("pg_theo").completed[2].choiceId,
                Is.EqualTo(person.chapters[2].choices.Last().id)); LogAssert.NoUnexpectedReceived();
        }

        private void AssertWorldProgress()
        {
            var actor = root.StoryWorld.Actor("pg_theo"); Assert.That(actor.Visible, Is.True);
            string chosen = root.Session.Stories.Person("pg_theo").completed[2].choiceId;
            foreach (var memory in actor.memories.Where(memory => memory.chapter == 2))
                Assert.That(memory.target.activeSelf, Is.EqualTo(string.IsNullOrEmpty(memory.choiceId) || memory.choiceId == chosen));
            foreach (var place in Object.FindObjectsByType<CityPlaceProgressVisual>(FindObjectsSortMode.None).Where(place => place.cityId == City))
            { place.Refresh(); Assert.That(place.completedPlace.activeSelf, Is.True); Assert.That(place.closedPlace.activeSelf, Is.False); }
            var archive = Object.FindObjectsByType<CityPlaceProgressVisual>(FindObjectsSortMode.None)
                .Single(place => place.cityId == City && place.collaborationId == "pg_joint_seed_archive");
            CollectionAssert.AreEquivalent(root.Definitions.vipStories.Single(person => person.id == "pg_theo").chapters[2].choices.Select(choice => choice.id), archive.choiceValues);
            Assert.That(archive.choicePlaces.Length, Is.EqualTo(archive.choiceValues.Length));
            for (int index = 0; index < archive.choicePlaces.Length; index++)
                Assert.That(archive.choicePlaces[index].activeSelf, Is.EqualTo(archive.choiceValues[index] == chosen));
            Assert.That(root.StoryWorld.actors.All(resident => resident.Visible), Is.True, "Completing a story must retain its resident.");
        }
    }
}

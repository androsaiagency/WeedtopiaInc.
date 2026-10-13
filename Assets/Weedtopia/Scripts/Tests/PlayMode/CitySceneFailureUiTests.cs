using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Persistence;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class CitySceneFailureUiTests
    {
        private const string TargetCity = "big_rock_city";
        private GameRoot root;
        private ManualClock clock;
        private string directory, path;
        private static Type HandoffType => typeof(GameRoot).Assembly.GetType("Weedtopia.Unity.CitySceneHandoff", true);

        [UnitySetUp] public IEnumerator Setup()
        {
            HandoffType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");
            root = Object.FindFirstObjectByType<GameRoot>(); root.enabled = false;
            directory = Path.Combine(UnityEngine.Application.temporaryCachePath,
                "Weedtopia_CitySceneFailure_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); path = Path.Combine(directory, "save.json");
            clock = new ManualClock(new DateTime(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc));
            root.UseTestStore(path, clock);
            Assert.That(root.NewGame("Scene Failure"), Is.True);
            Assert.That(root.CompleteIntro(), Is.True); root.Ui.Close();
            yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (root != null) Object.Destroy(root.gameObject);
            yield return null;
            HandoffType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [UnityTest] public IEnumerator SceneLoaderExceptionRetainsOldSessionAndAllowsCommittedCityRetry()
            => ExerciseSceneFailure(new FailingLoader(true));

        [UnityTest] public IEnumerator RejectedSceneLoadRetainsOldSessionAndAllowsCommittedCityRetry()
            => ExerciseSceneFailure(new FailingLoader(false));

        private IEnumerator ExerciseSceneFailure(FailingLoader loader)
        {
            var previous = root.Session;
            string previousJson = JsonUtility.ToJson(previous.State), previousSelection = root.SelectedId;
            bool previousPaused = previous.Paused;
            // Keep the active Burning Hills session separate from the valid disk candidate for Big Rock.
            var saved = GameState.Create(root.Definitions, "Scene Failure", clock.UtcNow);
            saved.introSeen = true;
            var candidateSession = new GameSession(root.Definitions, saved, clock);
            // Witness the three-city population with legal housing, then accept each mandate through its public command.
            PopulateCandidate(candidateSession, "burning_hills");
            Assert.That(candidateSession.Campaign.Accept("big_rock_city"), Is.EqualTo(Failure.None));
            Assert.That(candidateSession.Campaign.Travel("big_rock_city"), Is.EqualTo(Failure.None));
            PopulateCandidate(candidateSession, "big_rock_city");
            Assert.That(candidateSession.Campaign.Accept("seven_hills"), Is.EqualTo(Failure.None));
            Assert.That(candidateSession.Campaign.Travel("seven_hills"), Is.EqualTo(Failure.None));
            PopulateCandidate(candidateSession, "seven_hills");
            Assert.That(candidateSession.Campaign.UnlockProgress("little_montana"), Is.EqualTo(15000));
            Assert.That(candidateSession.Campaign.Accept("little_montana"), Is.EqualTo(Failure.None));
            Assert.That(saved.cities.Single(city => city.id == "little_montana").charterResidentMilestone, Is.EqualTo(15000));
            Assert.That(candidateSession.Campaign.Travel(TargetCity), Is.EqualTo(Failure.None));
            // The accepted charter survives a later decline; no productive homes or borrowed workers fund the offline candidate.
            foreach (var building in saved.buildings.Where(building => building.family == "residential_house"))
            { building.family = "settlement_plot"; building.level = 0; }
            saved.workers.RemoveAll(worker => worker.id.StartsWith("scene_failure_fixture_", StringComparison.Ordinal));
            candidateSession.Campaign.RefreshMilestones();
            Assert.That(candidateSession.Campaign.UnlockProgress("little_montana"), Is.Zero);
            var store = new LocalSaveStore(path, root.Definitions); store.Save(saved);
            string beforeDisk = File.ReadAllText(path);
            clock.UtcNow = clock.UtcNow.AddSeconds(257);
            root.UseTestSceneLoader(loader);
            LogAssert.Expect(LogType.Error, "City scene load failed. Retry remains available.");
            Assert.That(root.ContinueResponsive(), Is.True);
            var completionWatch = System.Diagnostics.Stopwatch.StartNew();
            while (root.ContinueBusy && completionWatch.Elapsed.TotalSeconds < 10) yield return null;
            Assert.That(root.ContinueBusy, Is.False, "Small offline candidate must finish before retry assertions.");
            Assert.That(loader.Calls, Is.EqualTo(1));
            Assert.That(loader.LastScene, Is.EqualTo(root.Definitions.cities.Single(city => city.id == TargetCity).scenePath));
            Assert.That(root.TravelLoading, Is.False);
            Assert.That(root.ContinuationPending, Is.True);
            Assert.That(root.Session, Is.SameAs(previous));
            Assert.That(root.SelectedId, Is.EqualTo(previousSelection));
            Assert.That(previous.Paused, Is.EqualTo(previousPaused));
            Assert.That(JsonUtility.ToJson(previous.State), Is.EqualTo(previousJson));
            Assert.That(HandoffType.GetField("pending", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null), Is.Null);
            Assert.That(root.Ui.Element<Button>("ui.start").interactable, Is.True);
            Assert.That(root.Ui.Element<Text>("message").text, Is.EqualTo(root.Definitions.Text("campaign.no_destination")));

            var committed = store.Load();
            Assert.That(committed.State, Is.Not.Null); Assert.That(committed.Recovered, Is.False);
            Assert.That(committed.State.activeCity, Is.EqualTo(TargetCity));
            Assert.That(committed.State.simulationSeconds - saved.simulationSeconds, Is.EqualTo(257));
            string committedDisk = File.ReadAllText(path);
            Assert.That(committedDisk, Is.Not.EqualTo(beforeDisk), "Offline commit succeeds before the independent scene-load failure.");
            InvokeRoot("Update"); root.SaveSilently(); root.Save(); root.Pause(true);
            InvokeRoot("OnApplicationFocus", false); InvokeRoot("OnApplicationPause", true);
            bool invoked = false;
            Assert.That(root.Command(() => { invoked = true; return Failure.None; }), Is.EqualTo(Failure.Busy));
            Assert.That(invoked, Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo(committedDisk), "The retained session must not overwrite the committed candidate.");
            Assert.That(JsonUtility.ToJson(previous.State), Is.EqualTo(previousJson));
            Assert.That(previous.Paused, Is.EqualTo(previousPaused));

            root.UseTestSceneLoader(new UnityCitySceneLoader());
            Assert.That(root.ContinueResponsive(), Is.True);
            var travelWatch = System.Diagnostics.Stopwatch.StartNew();
            bool ready = false;
            while (travelWatch.Elapsed.TotalSeconds < 30)
            {
                yield return null;
                var candidate = Object.FindFirstObjectByType<GameRoot>();
                if (candidate == null || candidate.SceneCityId != TargetCity || candidate.SavePath != Path.GetFullPath(path)) continue;
                root = candidate; root.enabled = false;
                ready = root.Session != null && !root.TravelLoading && !root.ContinueBusy && !root.ContinuationPending
                    && root.Ui.Element<RectTransform>("offline").gameObject.activeSelf;
                if (ready) break;
            }
            Assert.That(ready, Is.True, "A fresh standard loader must finish the committed city's actual scene handoff.");
            Assert.That(root.Session, Is.Not.SameAs(previous));
            Assert.That(root.Session.State.activeCity, Is.EqualTo(TargetCity));
            Assert.That(root.Session.State.simulationSeconds, Is.EqualTo(committed.State.simulationSeconds));
            Assert.That(root.Session.Paused, Is.True);
            Assert.That(root.Session.ApplyOffline().Seconds, Is.Zero, "Retry loads the committed anchor without granting offline time twice.");
            Assert.That(JsonUtility.ToJson(previous.State), Is.EqualTo(previousJson));
            Assert.That(JsonUtility.ToJson(store.Load().State), Is.EqualTo(JsonUtility.ToJson(root.Session.State)));
            LogAssert.NoUnexpectedReceived();
        }

        private void InvokeRoot(string method, params object[] arguments)
            => typeof(GameRoot).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(root, arguments);

        private static void PopulateCandidate(GameSession candidate, string city)
        {
            foreach (var plot in candidate.State.buildings.Where(building => building.cityId == city && building.family == "settlement_plot").Take(10))
            {
                plot.family = "residential_house"; plot.level = 1750;
                candidate.State.workers.Add(new WorkerState { id = "scene_failure_fixture_" + plot.id, role = "caretaker", buildingId = plot.id,
                    slot = 0, level = 80, rank = 1, attributes = new System.Collections.Generic.List<WorkerAttribute> { WorkerAttribute.Residents } });
            }
            Assert.That(candidate.Simulation.Residents(city), Is.EqualTo(5000));
        }

        private sealed class FailingLoader : ICitySceneLoader
        {
            private readonly bool throws;
            public int Calls { get; private set; }
            public string LastScene { get; private set; }
            public FailingLoader(bool throws) { this.throws = throws; }
            public bool Start(string scenePath)
            {
                Calls++; LastScene = scenePath;
                if (throws) throw new InvalidOperationException("Controlled scene loader failure");
                return false;
            }
        }
    }
}

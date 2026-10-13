using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Persistence;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Actual rendered scene travel and saved story-state evidence, using an isolated diagnostic save.</summary>
    [InitializeOnLoad] public static class GoldenFlowerEvidence
    {
        private const string Key = "Weedtopia.GoldenFlowerEvidence", Folder = "Docs/GoldenFlower";
        private static GameRoot root;
        private static ManualClock clock;
        private static List<Func<bool>> steps;
        private static readonly List<string> screenshots = new List<string>();
        private static readonly List<Dimensions> dimensions = new List<Dimensions>();
        private static int index, frames, width = 1920, height = 1080;
        private static string pending, travel;
        private static DateTime captureUtc;
        private static double deadline, settle;
        private static bool savedStoryRoundtrip, housingInspectionMatchesSimulation;
        static GoldenFlowerEvidence() { if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick; }
        public static void Begin()
        {
            if (!UnityEngine.Application.isBatchMode) throw new InvalidOperationException("Owned batch editor required.");
            BurningHillsLayoutInstaller.RequireSafeEditor(); Directory.CreateDirectory(Folder + "/Screenshots");
            SessionState.SetBool(Key + ".hadLanguage", PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey));
            SessionState.SetString(Key + ".language", PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey));
            ProductionUiEvidence.Reset(); screenshots.Clear(); dimensions.Clear(); steps = null;
            index = frames = 0; pending = travel = null; deadline = settle = 0; savedStoryRoundtrip = housingInspectionMatchesSimulation = false;
            SessionState.SetBool(Key, true); EditorApplication.update -= Tick; EditorApplication.update += Tick;
            EditorSceneManager.OpenScene(GameScaffold.GameScene); SetSize(1920, 1080); EditorApplication.EnterPlaymode();
        }
        private static void SetSize(int w, int h) { PhoneUiEvidence.FixedTestView(w, h); width = w; height = h; }
        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            try
            {
                if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 480;
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Golden Flower evidence timed out.");
                if (steps == null)
                {
                    root = UnityEngine.Object.FindFirstObjectByType<GameRoot>(); if (root == null) return;
                    root.enabled = false; clock = new ManualClock(new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc));
                    root.UseTestStore(Path.GetFullPath(Folder + "/capture_save.json"), clock); root.NewGame("Sonnental"); Prepare();
                }
                if (++frames < 15 || EditorApplication.timeSinceStartup < settle)
                { if (root != null && !root.TravelLoading) root.Refresh(); EditorApplication.QueuePlayerLoopUpdate(); return; }
                if (pending != null)
                {
                    if (!File.Exists(pending) || File.GetLastWriteTimeUtc(pending) < captureUtc) { EditorApplication.QueuePlayerLoopUpdate(); return; }
                    var bytes = File.ReadAllBytes(pending); if (bytes.Length < 24) return;
                    int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                    int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                    if (w != width || h != height) throw new InvalidOperationException("Unexpected Game View dimensions: " + w + "x" + h);
                    screenshots.Add(pending); dimensions.Add(new Dimensions { path = pending, width = w, height = h }); pending = null;
                }
                if (index < steps.Count)
                { if (steps[index]()) { index++; frames = 0; settle = EditorApplication.timeSinceStartup + .15; } EditorApplication.QueuePlayerLoopUpdate(); return; }
                File.WriteAllText(Folder + "/gameview_receipt.json", JsonUtility.ToJson(new Receipt {
                    utc = DateTime.UtcNow.ToString("O"), screenshots = screenshots.ToArray(), dimensions = dimensions.ToArray(),
                    savedStoryRoundtrip = savedStoryRoundtrip, housingInspectionMatchesSimulation = housingInspectionMatchesSimulation,
                    returnedToPreviousCity = root.SceneCityId == "burning_hills"
                }, true));
                ProductionUiEvidence.Write(null, Folder + "/ui_overflow.json", new[] { "1920x1080", "1366x768", "2560x1080", "1080x1920" }); Finish(0);
            }
            catch (Exception error) { Debug.LogException(error); Finish(1); }
        }
        private static void Finish(int code)
        {
            if (SessionState.GetBool(Key + ".hadLanguage", false)) PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey, SessionState.GetString(Key + ".language", ""));
            else PlayerPrefs.DeleteKey(GameRoot.LanguagePreferenceKey);
            PlayerPrefs.Save(); SessionState.SetBool(Key, false); EditorApplication.update -= Tick; EditorApplication.Exit(code);
        }
        [Serializable] private sealed class Dimensions { public string path; public int width, height; }
        [Serializable] private sealed class Receipt
        {
            public string utc, mode = "actual_playmode_gameview_with_diagnostic_growth", status = "passed";
            public string[] screenshots;
            public Dimensions[] dimensions;
            public bool savedStoryRoundtrip, housingInspectionMatchesSimulation, returnedToPreviousCity, physicalTouch, userVisualApproval;
            public int cities = 9, inhabitants = 5, completedPersonalChapters = 15, completedCommunityProjects = 4;
        }
        private static void Capture(string name)
        {
            Canvas.ForceUpdateCanvases(); root.Refresh();
            if (Screen.width != width || Screen.height != height) throw new InvalidOperationException("Game View resize has not settled.");
            ProductionUiEvidence.Inspect(root.Ui, name); captureUtc = DateTime.UtcNow;
            pending = Folder + "/Screenshots/" + name + ".png"; ScreenCapture.CaptureScreenshot(pending);
        }
        private static void Shot(string name, Action action)
        {
            steps.Add(() => { action(); return true; });
            steps.Add(() => SettledCapture(name));
        }
        private static bool SettledCapture(string name)
        {
            if (Screen.width != width || Screen.height != height ||
                Vector3.Distance(root.CameraRig.transform.position, root.CameraRig.TargetPosition) > .04f) return false;
            Capture(name); return true;
        }
        private static void Travel(string city, string shot, Action ready)
        {
            steps.Add(() =>
            {
                if (travel == null)
                {
                    root.Ui.Rewards.DismissAll(); root.Ui.ShowCities(); root.Refresh();
                    var row = root.Ui.Campaign.travelRows.Single(r => r != null && r.cityId == city);
                    if (!row.travel.interactable) throw new InvalidOperationException("Authored travel row is unavailable: " + city);
                    row.travel.onClick.Invoke(); if (!root.TravelLoading) throw new InvalidOperationException("Travel did not begin.");
                    travel = city; return false;
                }
                var candidate = UnityEngine.Object.FindFirstObjectByType<GameRoot>();
                if (candidate?.Session == null || candidate.SceneCityId != city) return false;
                root = candidate; root.enabled = false; travel = null; ready(); root.Refresh(); root.Ui.Notify(""); return true;
            });
            steps.Add(() => SettledCapture(shot));
        }
        private static void Prepare()
        {
            root.Session.CompleteIntro(); root.SetLanguage("de"); root.Ui.Close(); steps = new List<Func<bool>>();
            foreach (var city in root.Session.State.cities.Where(c => c.id != GoldenFlowerAuthoring.City))
            { city.charterAccepted = true; Populate(city.id, true); }
            root.Session.Campaign.RefreshMilestones();
            if (!root.Session.Campaign.Offered(GoldenFlowerAuthoring.City)) throw new InvalidOperationException("Population fixture did not reach one million.");
            Shot("01_golden_flower_invitation", () => { root.Ui.Campaign.ShowInvitation(GoldenFlowerAuthoring.City); CitiesScroll(1); });
            Shot("02_charter_accepted", () =>
            {
                root.Ui.Campaign.accept.onClick.Invoke();
                if (!root.Session.Campaign.Accessible(GoldenFlowerAuthoring.City)) throw new InvalidOperationException("UI did not accept Golden Flower.");
                root.Ui.Rewards.DismissAll(); root.Ui.ShowCities(); CitiesScroll(0);
            });
            Travel(GoldenFlowerAuthoring.City, "03_arrival", () => { root.Ui.Close(); root.Focus("gf_town_hall"); root.CameraRig.SetDiagnosticZoom(35); });
            Shot("04_first_quest", () => root.Ui.Experience.quests.Open(root.Session.Progression.Current.id));
            Shot("05_community_before", () =>
            {
                root.Ui.Close(); var place = Places().First(); place.Refresh(); root.CameraRig.Focus(place.transform.position); root.CameraRig.SetDiagnosticZoom(24);
            });
            Shot("06_resident_alma", () =>
            {
                GrownCity(); root.Ui.ShowStories("gf_alma");
                if (!root.StoryWorld.actors.All(a => a.Visible)) throw new InvalidOperationException("The five authored inhabitants are not visible.");
            });
            Shot("07_completed_glasshouse", () =>
            {
                root.Ui.Close(); CompleteAllStories(); var place = Places().ElementAt(1); place.Refresh();
                if (!place.completedPlace.activeSelf) throw new InvalidOperationException("Completed glasshouse is not visible.");
                root.CameraRig.Focus(place.transform.position); root.CameraRig.SetDiagnosticZoom(26);
            });
            Shot("08_gold_flower_festival", () =>
            {
                foreach (var place in Places()) { place.Refresh(); if (!place.completedPlace.activeSelf || place.closedPlace.activeSelf) throw new InvalidOperationException("Community stage was not retained."); }
                root.CameraRig.Focus(Places().Last().transform.position); root.CameraRig.SetDiagnosticZoom(40); ValidateSavedStories();
            });
            foreach (var size in new[] { new Vector2Int(1366, 768), new Vector2Int(2560, 1080), new Vector2Int(1080, 1920) })
            {
                steps.Add(() => { SetSize(size.x, size.y); frames = 0; settle = EditorApplication.timeSinceStartup + .7; return true; });
                Shot("09_story_" + size.x + "x" + size.y, () => root.Ui.ShowStories("gf_nora"));
                Shot("10_city_list_" + size.x + "x" + size.y, () => { root.Ui.ShowCities(); CitiesScroll(0); });
            }
            Travel("burning_hills", "11_return_to_burning_hills", () => { root.Ui.Close(); root.Focus("town_hall"); root.CameraRig.SetDiagnosticZoom(35); });
        }
        private static IEnumerable<CityPlaceProgressVisual> Places() => UnityEngine.Object.FindObjectsByType<CityPlaceProgressVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(p => p.cityId == GoldenFlowerAuthoring.City).OrderBy(p => Array.IndexOf(root.Session.Stories.Collaborations.Select(c => c.id).ToArray(), p.collaborationId));
        private static void CitiesScroll(float value)
        {
            var layout = root.Ui.Campaign.GetComponent<ModulePanelLayout>();
            if (layout?.scroll == null) throw new InvalidOperationException("Missing authored city scroll.");
            layout.scroll.StopMovement(); layout.scroll.verticalNormalizedPosition = value; Canvas.ForceUpdateCanvases();
        }
        private static void Populate(string city, bool populated)
        {
            var state = root.Session.State;
            string previousCity = state.activeCity;
            var targetCity = state.cities.Single(c => c.id == city);
            var previousZone = targetCity.unlocked;
            try
            {
                state.activeCity = city;
                if (targetCity.unlocked < Zone.Settlement) targetCity.unlocked = Zone.Settlement;
                if (!state.buildings.Any(b => b.cityId == city && b.family == "park"))
                {
                    var site = state.buildings.First(b => b.cityId == city && b.family == "settlement_plot");
                    if (root.Session.ChoosePlot(site.id, "park") != Failure.None) throw new InvalidOperationException("Could not retain the mandatory city park.");
                    site.level = 50;
                }
                foreach (var plot in state.buildings.Where(b => b.cityId == city && (b.family == "settlement_plot" || b.family == "residential_house")).ToArray())
                {
                    state.workers.RemoveAll(w => w.buildingId == plot.id);
                    if (plot.family == "settlement_plot" && root.Session.ChoosePlot(plot.id, "residential_house") != Failure.None)
                        throw new InvalidOperationException("The population fixture bypassed a reserved plot.");
                    plot.level = populated ? 5000 : 0;
                    if (populated) state.workers.Add(new WorkerState { id = "golden_capture_" + plot.id, role = "caretaker", buildingId = plot.id,
                        slot = 0, level = 100, rank = 10, attributes = new List<WorkerAttribute> { WorkerAttribute.Residents } });
                }
            }
            finally { state.activeCity = previousCity; targetCity.unlocked = previousZone; }
        }
        private static void GrownCity()
        {
            var state = root.Session.State; state.money = 1000000; state.City.unlocked = Zone.Industry;
            state.City.questIndex = root.Session.Progression.Quests.Length; state.City.completedQuests = root.Session.Progression.Quests.Select(q => q.id).ToList(); state.City.freeplay = true;
            foreach (var building in state.buildings.Where(b => b.cityId == GoldenFlowerAuthoring.City)) building.level = 50;
            Populate(GoldenFlowerAuthoring.City, true); ValidateHousingInspection();
            root.Session.Campaign.RefreshMilestones(); root.Session.Stories.RefreshUnlocks(); state.revision++; root.Refresh();
        }
        private static void ValidateHousingInspection()
        {
            var house = root.Session.State.buildings.First(b => b.cityId == GoldenFlowerAuthoring.City && b.family == "residential_house");
            string label = GameText.Get("project.goal.2.15");
            if (BuildingInspectionMetrics.Read(root, house).Single(row => row.Label == label).Current != "7395" ||
                root.Session.Simulation.Residents(GoldenFlowerAuthoring.City) != 125715)
                throw new InvalidOperationException("Final housing inspection disagrees with the real seventeen-house simulation.");
            int previousLevel = house.level;
            try
            {
                house.level = 4999;
                var preview = BuildingInspectionMetrics.Read(root, house).Single(row => row.Label == label);
                if (preview.Current != "6120" || preview.Next != "7395") throw new InvalidOperationException("Housing tier preview is inconsistent.");
            }
            finally { house.level = previousLevel; }
            housingInspectionMatchesSimulation = true;
        }
        private static void CompleteAllStories()
        {
            for (int chapter = 0; chapter < 3; chapter++) foreach (var person in root.Session.Stories.People) Complete(person.id);
            foreach (var project in root.Session.Stories.Collaborations) Complete(project.id);
            if (root.Session.State.City.vipStories.Sum(p => p.completed.Count) != 15 || root.Session.State.City.vipCollaborations.Count(p => p.completed != null) != 4)
                throw new InvalidOperationException("Incomplete Golden Flower stories.");
            root.Session.State.revision++; root.Refresh();
        }
        private static void Complete(string id)
        {
            var stories = root.Session.Stories; var quest = stories.Offer(id); if (quest == null) throw new InvalidOperationException("Missing story offer " + id);
            if (stories.Accept(id, quest.id, quest.choices[0].id) != Failure.None) throw new InvalidOperationException("Could not accept " + quest.id);
            var active = stories.Active(id);
            for (int i = 0; i < quest.goals.Length; i++)
            {
                var goal = quest.goals[i]; int target = active.targets[i]; var stats = root.Session.State.City.stats;
                switch (goal.kind)
                {
                    case StoryGoalKind.Level:
                        var building = root.Session.State.buildings.FirstOrDefault(b => b.cityId == GoldenFlowerAuthoring.City && (b.id == goal.target || b.family == goal.target));
                        if (building == null)
                        {
                            building = root.Session.State.buildings.First(b => b.cityId == GoldenFlowerAuthoring.City && root.Definitions.instances.Single(i => i.id == b.id).family == "settlement_plot");
                            root.Session.State.workers.RemoveAll(worker => worker.buildingId == building.id);
                            building.family = goal.target;
                        }
                        building.level = Math.Max(50, target); break;
                    case StoryGoalKind.Residents: Populate(GoldenFlowerAuthoring.City, true); break;
                    case StoryGoalKind.NpcVisited: for (int visit = 0; visit < target; visit++) if (stories.Visit(goal.target) != Failure.None) throw new InvalidOperationException("Visit failed"); break;
                    case StoryGoalKind.RawProduced: stats.rawProduced += target; break;
                    case StoryGoalKind.RefinedProduced: stats.refinedProduced += target; break;
                    case StoryGoalKind.CoffeeSold: stats.coffeeSold += target; break;
                    case StoryGoalKind.ButterProduced: stats.butterProduced += target; break;
                    case StoryGoalKind.PastrySold: stats.pastrySold += target; break;
                    case StoryGoalKind.DeliveredGoods: stats.delivered += target; break;
                    case StoryGoalKind.Deliveries: stats.deliveries += target; break;
                }
            }
            if (stories.Claim(id, quest.id) != Failure.None) throw new InvalidOperationException("Could not claim " + quest.id);
        }
        private static void ValidateSavedStories()
        {
            var state = root.Session.State; root.Session.Save();
            var loaded = new LocalSaveStore(Path.GetFullPath(Folder + "/capture_save.json"), root.Definitions).Load();
            if (loaded.State == null || JsonUtility.ToJson(loaded.State.City) != JsonUtility.ToJson(state.City)) throw new InvalidOperationException("Story state roundtrip failed.");
            savedStoryRoundtrip = true;
        }
    }
}

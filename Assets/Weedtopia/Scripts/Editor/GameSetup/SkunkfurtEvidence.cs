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
    [InitializeOnLoad] public static class SkunkfurtEvidence
    {
        private const string Key = "Weedtopia.SkunkfurtEvidence", Folder = "Docs/Skunkfurt";
        private static GameRoot root;
        private static ManualClock clock;
        private static List<Func<bool>> steps;
        private static readonly List<string> screenshots = new List<string>();
        private static readonly List<Dimensions> dimensions = new List<Dimensions>();
        private static int index, frames, width = 1920, height = 1080;
        private static string pending, travel;
        private static CityPlaceProgressVisual pendingPlace;
        private static float pendingPlaceZoom;
        private static DateTime captureUtc;
        private static double deadline, settle;
        private static bool savedStoryRoundtrip, housingInspectionMatchesSimulation, twelveCityOfflineRoundtrip, architectureGroundAnchorsVerified;
        private static LocalSaveStore branchBaseline;
        private static readonly string[] languages = { "de", "en", "es", "fr", "ru" };
        private static readonly HashSet<string> visitedCities = new HashSet<string>();
        private static readonly HashSet<string> verifiedChoices = new HashSet<string>();
        private static readonly List<MatrixCapture> matrix = new List<MatrixCapture>();
        static SkunkfurtEvidence() { if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick; }
        public static void Begin()
        {
            if (!UnityEngine.Application.isBatchMode) throw new InvalidOperationException("Owned batch editor required.");
            BurningHillsLayoutInstaller.RequireSafeEditor(); Directory.CreateDirectory(Folder + "/Screenshots");
            SessionState.SetBool(Key + ".hadLanguage", PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey));
            SessionState.SetString(Key + ".language", PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey));
            ProductionUiEvidence.Reset(); screenshots.Clear(); dimensions.Clear(); steps = null;
            index = frames = 0; pending = travel = null; deadline = settle = 0; savedStoryRoundtrip = housingInspectionMatchesSimulation = twelveCityOfflineRoundtrip = architectureGroundAnchorsVerified = false;
            visitedCities.Clear(); verifiedChoices.Clear(); matrix.Clear(); branchBaseline = null;
            pendingPlace = null; pendingPlaceZoom = 0;
            SessionState.SetBool(Key, true); EditorApplication.update -= Tick; EditorApplication.update += Tick;
            EditorSceneManager.OpenScene(GameScaffold.GameScene); SetSize(1920, 1080); EditorApplication.EnterPlaymode();
        }
        private static void SetSize(int w, int h) { PhoneUiEvidence.FixedTestView(w, h); width = w; height = h; }
        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            try
            {
                if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 1200;
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Skunkfurt evidence timed out.");
                if (steps == null)
                {
                    root = UnityEngine.Object.FindFirstObjectByType<GameRoot>(); if (root == null) return;
                    root.enabled = false; clock = new ManualClock(new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc));
                    root.UseTestStore(Path.GetFullPath(Folder + "/capture_save.json"), clock); root.NewGame("Skunkfurt"); Prepare();
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
                if (!savedStoryRoundtrip || !housingInspectionMatchesSimulation || !twelveCityOfflineRoundtrip || !architectureGroundAnchorsVerified ||
                    !visitedCities.SetEquals(root.Definitions.cities.Select(c => c.id)) ||
                    !verifiedChoices.SetEquals(new[] { "fair_finance_desk", "housing_council" }) || matrix.Count != 60 || screenshots.Count != 85)
                    throw new InvalidOperationException("Rendered city evidence did not complete every required scenario.");
                ProductionUiEvidence.Write(null, Folder + "/ui_overflow.json", new[] { "1920x1080", "1366x768", "2560x1080", "1080x1920" });
                if (!JsonUtility.FromJson<UiOverflowReceipt>(File.ReadAllText(Folder + "/ui_overflow.json")).passed)
                    throw new InvalidOperationException("Localized Game View UI overflow validation failed.");
                File.WriteAllText(Folder + "/gameview_receipt.json", JsonUtility.ToJson(new Receipt {
                    utc = DateTime.UtcNow.ToString("O"), screenshots = screenshots.ToArray(), dimensions = dimensions.ToArray(),
                    savedStoryRoundtrip = savedStoryRoundtrip, housingInspectionMatchesSimulation = housingInspectionMatchesSimulation,
                    returnedToPreviousCity = root.SceneCityId == "burning_hills", allTwelveCitiesVisited = visitedCities.SetEquals(root.Definitions.cities.Select(city => city.id)),
                    twelveCityOfflineRoundtrip = twelveCityOfflineRoundtrip, jonasChoiceVariantsVerified = verifiedChoices.SetEquals(new[] { "fair_finance_desk", "housing_council" }),
                    architectureGroundAnchorsVerified = architectureGroundAnchorsVerified,
                    languages = languages, matrix = matrix.ToArray(), visitedCities = visitedCities.ToArray(), verifiedChoices = verifiedChoices.ToArray()
                }, true));
                Finish(0);
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
            public bool savedStoryRoundtrip, housingInspectionMatchesSimulation, returnedToPreviousCity, allTwelveCitiesVisited, twelveCityOfflineRoundtrip, jonasChoiceVariantsVerified, architectureGroundAnchorsVerified, physicalTouch, userVisualApproval;
            public string[] languages, visitedCities, verifiedChoices; public MatrixCapture[] matrix;
            public int cities = 12, inhabitants = 6, completedPersonalChapters = 18, completedCommunityProjects = 3;
        }
        private static void Capture(string name)
        {
            Canvas.ForceUpdateCanvases(); root.Refresh();
            if (Screen.width != width || Screen.height != height) throw new InvalidOperationException("Game View resize has not settled.");
            VerifyArchitectureGroundAnchors();
            ProductionUiEvidence.Inspect(root.Ui, name); captureUtc = DateTime.UtcNow;
            pending = Folder + "/Screenshots/" + name + ".png"; ScreenCapture.CaptureScreenshot(pending);
        }
        private static void VerifyArchitectureGroundAnchors()
        {
            if (root.SceneCityId != SkunkfurtAuthoring.City) return;
            var landmarks = root.gameObject.scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<CityLandmarkVisual>(true))
                .Where(landmark => landmark.gameObject.activeInHierarchy).ToArray();
            if (landmarks.Length == 0) throw new InvalidOperationException("No visible authored Skunkfurt architecture.");
            foreach (var landmark in landmarks)
            {
                var renderer = landmark.GetComponent<SpriteRenderer>();
                if (renderer == null || renderer.sprite == null || landmark.transform.parent == null ||
                    renderer.sortingOrder != WorldSort.At(landmark.transform.parent.position))
                    throw new InvalidOperationException("Architecture is not sorted by its serialized ground anchor: " + landmark.name);
            }
            architectureGroundAnchorsVerified = true;
        }        private static void Shot(string name, Action action)
        {
            steps.Add(() => { action(); return true; });
            steps.Add(() => SettledCapture(name));
        }
        private static bool SettledCapture(string name)
        {
            if (Screen.width != width || Screen.height != height) return false;
            // Activated landmarks billboard in LateUpdate; frame their settled bounds, not their ground anchors.
            Bounds projectBounds = pendingPlace != null ? FramePendingPlace() : default;
            if (Vector3.Distance(root.CameraRig.transform.position, root.CameraRig.TargetPosition) > .04f) return false;
            if (pendingPlace != null) RequireProjectInsideSafeViewport(projectBounds, name);
            Capture(name); pendingPlace = null; pendingPlaceZoom = 0; return true;
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
                root = candidate; root.enabled = false; visitedCities.Add(city); travel = null; ready(); root.Refresh(); root.Ui.Notify(""); return true;
            });
            steps.Add(() => SettledCapture(shot));
        }
        private static void Prepare()
        {
            root.Session.CompleteIntro(); root.SetLanguage("de"); root.Ui.Close(); steps = new List<Func<bool>>();
            foreach (var city in root.Session.State.cities.Where(c => c.id != SkunkfurtAuthoring.City))
            { city.charterAccepted = true; Populate(city.id, true); }
            root.Session.Campaign.RefreshMilestones();
            if (!root.Session.Campaign.Offered(SkunkfurtAuthoring.City)) throw new InvalidOperationException("Population fixture did not reach ten million.");
            Shot("01_skunkfurt_invitation", () => { root.Ui.Campaign.ShowInvitation(SkunkfurtAuthoring.City); CitiesScroll(1); });
            Shot("02_charter_accepted", () =>
            {
                root.Ui.Campaign.accept.onClick.Invoke();
                if (!root.Session.Campaign.Accessible(SkunkfurtAuthoring.City)) throw new InvalidOperationException("UI did not accept Skunkfurt.");
                root.Ui.Rewards.DismissAll(); root.Ui.ShowCities(); CitiesScroll(0);
            });
            Travel(SkunkfurtAuthoring.City, "03_arrival", () => { root.Ui.Close(); root.Focus("sf_town_hall"); root.CameraRig.SetDiagnosticZoom(35); });
            Shot("04_community_before", () => { root.Ui.Close(); FocusPlace(Places().First(), 24); });
            MatrixShots("initial_quest", () => root.Ui.Experience.quests.Open(root.Session.Progression.Current.id));
            MatrixShots("travel_final_row", () =>
            {
                root.Ui.ShowCities(); CitiesScroll(0); Canvas.ForceUpdateCanvases();
                var row = root.Ui.Campaign.travelRows.Single(r => r.cityId == SkunkfurtAuthoring.City);
                var viewport = root.Ui.Campaign.GetComponent<ModulePanelLayout>().scroll.viewport;
                var rowBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, (RectTransform)row.transform);
                if (row.travel.interactable || rowBounds.min.y < viewport.rect.yMin - 2 || rowBounds.max.y > viewport.rect.yMax + 2)
                    throw new InvalidOperationException("Final current-city travel row is clipped or has an incorrect action state.");
            });
            Shot("05_community_growing", () =>
            {
                root.SetLanguage("de"); SetSize(1920, 1080); root.Ui.Close(); GrownCity();
                foreach (var place in Places())
                { place.Refresh(); if (!place.growingPlace.activeSelf || place.closedPlace.activeSelf || place.completedPlace.activeSelf) throw new InvalidOperationException("Restoration growing stage failed."); }
                FocusPlace(Places().First(), 28);
            });
            Shot("06_resident_jonas", () => root.Ui.ShowStories("sf_jonas"));
            steps.Add(() =>
            {
                root.Ui.Close(); foreach (var person in root.Session.Stories.People) { Complete(person.id, 0); Complete(person.id, 0); }
                foreach (var project in root.Session.Stories.Collaborations.Where(p => root.Session.Stories.Offer(p.id) != null).ToArray()) Complete(project.id, 0);
                root.Session.State.revision++; root.Refresh();
                var offer = root.Session.Stories.Offer("sf_jonas");
                if (offer == null || offer.id != "sf_jonas_chapter_3" || offer.choices.Length != 2) throw new InvalidOperationException("Jonas chapter-three choice is not available.");
                // Use the production save envelope/decoder so nullable story/shop records follow normal save handling.
                branchBaseline = new LocalSaveStore(Path.GetFullPath(Folder + "/capture_branch_baseline.json"), root.Definitions);
                branchBaseline.Save(root.Session.State); return true;
            });
            MatrixShots("jonas_choice", () =>
            {
                root.Ui.ShowStories("sf_jonas");
                if (!root.Ui.Stories.accept.interactable || !root.Ui.Stories.alternate.interactable) throw new InvalidOperationException("Both authored Jonas actions must be available.");
            });
            Shot("07_fair_finance_desk", () => { root.SetLanguage("de"); SetSize(1920, 1080); root.Ui.Close(); CompleteAllStories(0); VerifyChoice("fair_finance_desk"); FocusPlace(Places().Last(), 28); ValidateSavedStories(); });
            Shot("08_housing_council", () =>
            {
                if (branchBaseline == null) throw new InvalidOperationException("Missing saved pre-choice branch baseline.");
                var loadedBranch = branchBaseline.Load();
                if (loadedBranch.State == null || loadedBranch.Recovered || loadedBranch.Error != null)
                    throw new InvalidDataException("Could not load the exact valid pre-choice branch baseline.", loadedBranch.Error);
                SaveValidator.Validate(loadedBranch.State, root.Definitions);
                new LocalSaveStore(Path.GetFullPath(Folder + "/capture_save.json"), root.Definitions).Save(loadedBranch.State);
                if (!root.Continue()) throw new InvalidOperationException("Could not restore the saved pre-choice branch.");
                root.enabled = false; root.Ui.Close(); CompleteAllStories(1); VerifyChoice("housing_council"); FocusPlace(Places().Last(), 28); ValidateSavedStories();
            });
            for (int project = 0; project < 3; project++)
            {
                int selected = project;
                Shot("09_completed_project_" + selected, () => { root.Ui.Close(); FocusPlace(Places().ElementAt(selected), 28); });
            }
            Shot("10_offline_story_continue", () => { ValidateOfflineContinue(); root.Ui.Close(); FocusPlace(Places().First(), 32); });
            foreach (var destination in root.Definitions.cities)
            {
                string city = destination.id;
                Travel(city, "11_twelve_city_" + city, () =>
                {
                    root.Ui.Close(); var hall = root.Session.State.buildings.Single(b => b.cityId == city && b.family == "town_hall"); root.Focus(hall.id); root.CameraRig.SetDiagnosticZoom(35);
                    SaveValidator.Validate(root.Session.State, root.Definitions);
                    if (root.Sites.Length != root.Definitions.instances.Count(i => i.cityId == city) || root.Ui.Campaign.travelRows.Length != 12) throw new InvalidOperationException("Authored city catalog mismatch in " + city);
                });
            }
            Travel("burning_hills", "12_return_to_burning_hills", () => { root.Ui.Close(); root.Focus("town_hall"); root.CameraRig.SetDiagnosticZoom(35); });
        }
        private static void MatrixShots(string scenario, Action action)
        {
            foreach (string code in languages) foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1366, 768), new Vector2Int(2560, 1080), new Vector2Int(1080, 1920) })
            {
                string language = code; var dimensions = size;
                steps.Add(() => { if (!root.SetLanguage(language)) throw new InvalidOperationException("Unsupported evidence locale."); SetSize(dimensions.x, dimensions.y); settle = EditorApplication.timeSinceStartup + .7; return true; });
                string name = scenario + "_" + language + "_" + dimensions.x + "x" + dimensions.y;
                Shot(name, action);
                steps.Add(() => { matrix.Add(new MatrixCapture { scenario = scenario, language = language, width = dimensions.x, height = dimensions.y, path = Folder + "/Screenshots/" + name + ".png" }); return true; });
            }
        }
        [Serializable] private sealed class MatrixCapture { public string scenario, language, path; public int width, height; }
        private static void FocusPlace(CityPlaceProgressVisual place, float zoom)
        {
            place.Refresh(); pendingPlace = place; pendingPlaceZoom = zoom;
        }
        private static Bounds FramePendingPlace()
        {
            var renderers = pendingPlace.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => renderer.gameObject.activeInHierarchy && renderer.enabled && renderer.sprite != null).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("No visible project architecture: " + pendingPlace.collaborationId);
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var camera = root.CameraRig.RenderCamera;
            Vector3 up = camera.transform.up, right = camera.transform.right;
            float projectedHeight = 2 * (Mathf.Abs(up.x) * bounds.extents.x + Mathf.Abs(up.y) * bounds.extents.y + Mathf.Abs(up.z) * bounds.extents.z);
            float projectedWidth = 2 * (Mathf.Abs(right.x) * bounds.extents.x + Mathf.Abs(right.y) * bounds.extents.y + Mathf.Abs(right.z) * bounds.extents.z);
            // Leave an inner margin beyond the asserted safe viewport, including the camera easing tolerance.
            float zoom = Mathf.Max(pendingPlaceZoom, projectedHeight / (2 * .68f), projectedWidth / (2 * camera.aspect * .82f));
            Vector3 groundUp = Vector3.ProjectOnPlane(up, Vector3.up).normalized;
            Vector3 focus = new Vector3(bounds.center.x, 0, bounds.center.z)
                + groundUp * (bounds.center.y * up.y / Vector3.Dot(groundUp, up));
            root.CameraRig.SetDiagnosticZoom(zoom); root.CameraRig.Focus(focus);
            return bounds;
        }
        private static void RequireProjectInsideSafeViewport(Bounds bounds, string shot)
        {
            var camera = root.CameraRig.RenderCamera;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = bounds.center + new Vector3(
                    (corner & 1) == 0 ? -bounds.extents.x : bounds.extents.x,
                    (corner & 2) == 0 ? -bounds.extents.y : bounds.extents.y,
                    (corner & 4) == 0 ? -bounds.extents.z : bounds.extents.z);
                Vector3 viewport = camera.WorldToViewportPoint(point);
                if (viewport.z <= 0 || viewport.x < .08f || viewport.x > .92f || viewport.y < .15f || viewport.y > .85f)
                    throw new InvalidOperationException("Project architecture is outside the HUD-safe viewport in " + shot + ": "
                        + pendingPlace.collaborationId + " at " + viewport.ToString("F3"));
            }
        }
        private static void VerifyChoice(string chosen)
        {
            var actor = root.StoryWorld.Actor("sf_jonas"); var progress = root.Session.Stories.Person("sf_jonas");
            if (!actor.Visible || progress.completed[2].choiceId != chosen) throw new InvalidOperationException("Jonas saved branch did not match.");
            foreach (var memory in actor.memories.Where(m => m.chapter == 2))
                if (memory.target.activeSelf != (memory.choiceId == chosen || string.IsNullOrEmpty(memory.choiceId))) throw new InvalidOperationException("Jonas memory did not match the chosen branch.");
            var yard = Places().Single(p => p.collaborationId == "sf_joint_city_charter"); yard.Refresh();
            if (yard.choiceValues.Length != 2 || yard.choicePlaces.Length != 2) throw new InvalidOperationException("Two authored city-charter branches are required.");
            for (int branch = 0; branch < yard.choicePlaces.Length; branch++)
            {
                if (yard.choicePlaces[branch].activeSelf != (yard.choiceValues[branch] == chosen)) throw new InvalidOperationException("City-charter visual did not match Jonas choice.");
                string architecture = yard.choiceValues[branch] == "fair_finance_desk" ? "FairFinanceHouse" : "RooftopGardenCourt";
                if (!yard.choicePlaces[branch].GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite != null && renderer.sprite.name == architecture) ||
                    yard.choicePlaces[branch].transform.localPosition != DistrictMap.ToWorld(new Vector2(0, -30)))
                    throw new InvalidOperationException("Saved Jonas branch architecture or ground placement did not match: " + architecture);
            }
            if (!root.StoryWorld.actors.All(p => p.Visible)) throw new InvalidOperationException("Completing stories removed a permanent inhabitant.");
            verifiedChoices.Add(chosen);
        }
        private static void ValidateOfflineContinue()
        {
            var state = root.Session.State; long previousSecond = state.simulationSeconds;
            string[] stories = state.cities.Select(c => JsonUtility.ToJson(new CityStorySnapshot { people = c.vipStories.ToArray(), projects = c.vipCollaborations.ToArray(), quests = c.completedQuests.ToArray() })).ToArray();
            string[] assignments = state.workers.Select(w => w.id + "/" + w.buildingId + "/" + w.slot).ToArray();
            root.Session.Save(); clock.UtcNow = clock.UtcNow.AddSeconds(30);
            if (!root.Continue()) throw new InvalidOperationException("Twelve-city offline continue failed."); root.enabled = false;
            if (root.Session.State.simulationSeconds != previousSecond + 30 || root.Session.ApplyOffline().Seconds != 0) throw new InvalidOperationException("Offline seconds were missed or applied twice.");
            var resumed = root.Session.State;
            if (!stories.SequenceEqual(resumed.cities.Select(c => JsonUtility.ToJson(new CityStorySnapshot { people = c.vipStories.ToArray(), projects = c.vipCollaborations.ToArray(), quests = c.completedQuests.ToArray() }))) ||
                !assignments.SequenceEqual(resumed.workers.Select(w => w.id + "/" + w.buildingId + "/" + w.slot)) || resumed.cities.Count != 12)
                throw new InvalidOperationException("Offline continue changed city stories, ownership or assignments.");
            VerifyChoice("housing_council"); SaveValidator.Validate(resumed, root.Definitions); twelveCityOfflineRoundtrip = true;
        }
        [Serializable] private sealed class CityStorySnapshot { public VipStoryState[] people; public VipCollaborationState[] projects; public string[] quests; }
        private static IEnumerable<CityPlaceProgressVisual> Places() => UnityEngine.Object.FindObjectsByType<CityPlaceProgressVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(p => p.cityId == SkunkfurtAuthoring.City).OrderBy(p => Array.IndexOf(root.Session.Stories.Collaborations.Select(c => c.id).ToArray(), p.collaborationId));
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
                    if (populated) state.workers.Add(new WorkerState { id = "skunkfurt_capture_" + plot.id, role = "caretaker", buildingId = plot.id,
                        slot = 0, level = 100, rank = 10, attributes = new List<WorkerAttribute> { WorkerAttribute.Residents } });
                }
            }
            finally { state.activeCity = previousCity; targetCity.unlocked = previousZone; }
        }
        private static void GrownCity()
        {
            var state = root.Session.State; state.money = 1000000; state.City.unlocked = Zone.Industry;
            state.City.questIndex = root.Session.Progression.Quests.Length; state.City.completedQuests = root.Session.Progression.Quests.Select(q => q.id).ToList(); state.City.freeplay = true;
            foreach (var building in state.buildings.Where(b => b.cityId == SkunkfurtAuthoring.City)) building.level = 50;
            Populate(SkunkfurtAuthoring.City, true); ValidateHousingInspection();
            root.Session.Campaign.RefreshMilestones(); root.Session.Stories.RefreshUnlocks(); state.revision++; root.Refresh();
        }
        private static void ValidateHousingInspection()
        {
            var house = root.Session.State.buildings.First(b => b.cityId == SkunkfurtAuthoring.City && b.family == "residential_house");
            string label = GameText.Get("project.goal.2.15");
            if (BuildingInspectionMetrics.Read(root, house).Single(row => row.Label == label).Current != "57375" ||
                root.Session.Simulation.Residents(SkunkfurtAuthoring.City) != 975375)
                throw new InvalidOperationException("Final housing inspection disagrees with the real seventeen-house simulation.");
            int previousLevel = house.level;
            try
            {
                house.level = 4999;
                var preview = BuildingInspectionMetrics.Read(root, house).Single(row => row.Label == label);
                if (preview.Current != "6120" || preview.Next != "57375") throw new InvalidOperationException("Housing tier preview is inconsistent.");
            }
            finally { house.level = previousLevel; }
            housingInspectionMatchesSimulation = true;
        }
        private static void CompleteAllStories(int branch)
        {
            int required = root.Session.Stories.People.Sum(p => p.chapters.Length) + root.Session.Stories.Collaborations.Count();
            for (int pass = 0; pass < required; pass++)
            {
                bool changed = false;
                foreach (var person in root.Session.Stories.People)
                { var quest = root.Session.Stories.Offer(person.id); if (quest != null && person.chapters.Contains(quest)) { Complete(person.id, branch); changed = true; } }
                foreach (var project in root.Session.Stories.Collaborations)
                { if (root.Session.Stories.Offer(project.id) != null) { Complete(project.id, branch); changed = true; } }
                if (root.Session.State.City.vipStories.Sum(p => p.completed.Count) == 18 && root.Session.State.City.vipCollaborations.Count(p => p.completed != null) == 3)
                { root.Session.State.revision++; root.Refresh(); return; }
                if (!changed) throw new InvalidOperationException("Skunkfurt story prerequisites made no progress.");
            }
            throw new InvalidOperationException("Skunkfurt story graph did not complete.");
        }
        private static void Complete(string id, int branch)
        {
            var stories = root.Session.Stories; var quest = stories.Offer(id); if (quest == null) throw new InvalidOperationException("Missing story offer " + id);
            if (stories.Accept(id, quest.id, quest.choices[branch % quest.choices.Length].id) != Failure.None) throw new InvalidOperationException("Could not accept " + quest.id);
            var active = stories.Active(id);
            for (int i = 0; i < quest.goals.Length; i++)
            {
                var goal = quest.goals[i]; int target = active.targets[i]; var stats = root.Session.State.City.stats;
                switch (goal.kind)
                {
                    case StoryGoalKind.Level:
                        var building = root.Session.State.buildings.FirstOrDefault(b => b.cityId == SkunkfurtAuthoring.City && (b.id == goal.target || b.family == goal.target));
                        if (building == null)
                        {
                            building = root.Session.State.buildings.First(b => b.cityId == SkunkfurtAuthoring.City && root.Definitions.instances.Single(i => i.id == b.id).family == "settlement_plot");
                            root.Session.State.workers.RemoveAll(worker => worker.buildingId == building.id);
                            building.family = goal.target;
                        }
                        building.level = Math.Max(50, target); break;
                    case StoryGoalKind.Residents: Populate(SkunkfurtAuthoring.City, true); break;
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

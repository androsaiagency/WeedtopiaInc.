using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit owned-save validation in the existing editor, with restored scene and Game View state.</summary>
    [InitializeOnLoad]
    public static class PhoneUiEvidence
    {
        private const string Key = "Weedtopia.PhoneUiEvidence", Folder = "Docs/PhoneUiRevision/Screenshots";
        [Serializable] private sealed class Receipt { public string utc; public bool complete, physicalDeviceVerified = default; public int checks; public string[] screenshots, issues; }
        [Serializable] private sealed class Scenes { public SceneSetup[] setup; }
        private static GameRoot root;
        private static readonly List<(int w, int h, float density, float scale, string name, Action action)> steps = new List<(int, int, float, float, string, Action)>();
        private static readonly List<string> shots = new List<string>(), issues = new List<string>();
        private static int index, frames, checks;
        private static string pending;
        private static double deadline;
        private static readonly Dictionary<(int, int), int> sizeSelections = new Dictionary<(int, int), int>();
        static PhoneUiEvidence() { if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick; }
        public static void Begin()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor(); Directory.CreateDirectory(Folder);
            SessionState.SetString(Key + ".scenes", JsonUtility.ToJson(new Scenes { setup = EditorSceneManager.GetSceneManagerSetup() }));
            SessionState.SetInt(Key + ".size", GameViewSelection());
            SessionState.SetBool(Key + ".background", UnityEngine.Application.runInBackground);
            SessionState.SetBool(Key + ".paused", EditorApplication.isPaused);
            UnityEngine.Application.runInBackground = true; EditorApplication.isPaused = false;
            SessionState.SetBool(Key, true); EditorApplication.update -= Tick; EditorApplication.update += Tick;
            EditorSceneManager.OpenScene(GameScaffold.GameScene); EditorApplication.EnterPlaymode();
        }
        private static EditorWindow GameView()
        {
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            return Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w => w.GetType() == type)
                ?? EditorWindow.GetWindow(type, false, "Game", false);
        }
        public static int GameViewSelection() => (int)GameView().GetType().GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameView());
        public static void SelectSize(int value) => GameView().GetType().GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.Instance).Invoke(GameView(), new object[] { value, null });
        public static void FixedTestView(int width, int height) => Size(width, height);
        private static void Size(int width, int height)
        {
            if (sizeSelections.TryGetValue((width, height), out int selected)) { SelectSize(selected); GameView().Repaint(); return; }
            var assembly = typeof(UnityEditor.Editor).Assembly; var type = assembly.GetType("UnityEditor.GameViewSizes");
            var instance = assembly.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(type).GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            var group = type.GetProperty("currentGroup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
            var size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"), new[] { Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"), 1), (object)width, height, "Weedtopia Phone QA " + width + "x" + height });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            int selection = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
            sizeSelections[(width, height)] = selection; SelectSize(selection); GameView().Repaint();
        }
        private static void World()
        {
            root.NewGame("Handyprüfung"); root.Session.CompleteIntro(); root.Ui.Close();
            root.Session.State.City.unlocked = Zone.Industry; root.Session.State.money = 1000000; root.Refresh();
        }
        private static void Building()
        {
            World(); var building = root.Session.State.Find("farm_warehouse");
            if (building == null) building = root.Session.State.buildings.First(b => root.Definitions.Building(b.family).kind == BuildingKind.Warehouse);
            building.level = 133;
            var manager = root.Session.Workers.Grant(root.Definitions.Building(building.family).managerRole, 4); manager.level = 80;
            root.Session.Workers.Assign(manager.id, building.id, 0); root.Ui.Interaction.OpenBuilding(building.id); root.Refresh();
        }
        private static void Stories()
        {
            World(); var house = root.Session.State.Find("settlement_01"); house.family = "residential_house"; house.level = 1;
            var worker = root.Session.Workers.Grant("caretaker", 4); worker.level = 80; root.Session.Workers.Assign(worker.id, house.id, 0);
            root.Session.Stories.RefreshUnlocks(); root.Refresh(); root.Ui.ShowStories();
        }
        private static void Prepare()
        {
            foreach (var size in new[] { (1920, 1080, 1f), (2340, 1080, 2.75f), (1080, 2340, 2.75f), (844, 390, 1f) })
            foreach (float scale in new[] { 1f, 1.2f })
            {
                void Add(string name, Action action) => steps.Add((size.Item1, size.Item2, size.Item3, scale, name, action));
                Add("building", Building);
                Add("building_worker", () => root.Ui.Interaction.inspector.GetComponent<BuildingInspectorLayout>().scroll.verticalNormalizedPosition = 0);
                Add("building_closed", () => root.Ui.Interaction.closeBuilding.onClick.Invoke());
                Add("story_roster", Stories);
                Add("toni_offer", () => { Hit(root.Ui.Stories.rows.First(r => r.id == "toni").button); root.Ui.Stories.rows.First(r => r.id == "toni").button.onClick.Invoke(); });
                Add("toni_active", () => { Hit(root.Ui.Stories.accept); root.Ui.Stories.accept.onClick.Invoke(); Check(root.Session.Stories.Active("toni") != null, "Toni acceptance must create an active quest"); });
                Add("toni_ready", () => { root.Session.State.City.stats.deliveries += 3; root.Session.State.revision++; root.Refresh(); });
                Add("toni_claimed", () =>
                {
                    Hit(root.Ui.Stories.claim); root.Ui.Stories.claim.onClick.Invoke(); Check(root.Ui.Rewards.IsOpen, "Toni claim must show the actual reward");
                    root.Ui.Rewards.DismissAll(); root.Continue(); root.Ui.Close(); root.Ui.ShowStories("toni");
                    Check(root.Session.Stories.Person("toni").completed.Count == 1, "Toni completion must survive Continue");
                });
                Add("story_back", () => root.Ui.Stories.GetComponent<VipStoryPanelLayout>().back.onClick.Invoke());
                Add("story_locked", () => root.Ui.Stories.Select("juno"));
            }
        }
        private static void Inspect(string name)
        {
            float pixels = UiDisplayMetrics.Scale(root.Ui.GetComponent<Canvas>());
            if (root.Ui.Interaction.BuildingOpen)
            {
                var ui = root.Ui.Interaction; var layout = ui.inspector.GetComponent<BuildingInspectorLayout>();
                Check(ui.inspector.transform.localScale == Vector3.one, "Building panel must not shrink as a group");
                Check(ui.staffBody.fontSize * pixels >= 17.5f, "Readable worker text");
                foreach (var label in ui.metricLabels.Where(t => t.gameObject.activeInHierarchy)) Check(label.fontSize * pixels >= 17.5f, "Readable metric: " + label.text);
                Check(((RectTransform)ui.upgrade.transform).rect.height * pixels >= 47.5f, "Upgrade has a full touch target");
                Check(Contained((RectTransform)ui.upgrade.transform, (RectTransform)ui.inspector.transform), "Upgrade stays inside the building window");
                Check(Contained((RectTransform)ui.closeBuilding.transform, (RectTransform)ui.inspector.transform), "Building close stays inside its window");
                Hit(ui.upgrade); Hit(ui.closeBuilding);
                Check(!root.Ui.GetComponent<WorldNavigationBar>().Dock.gameObject.activeInHierarchy, "Building hides the navigation dock");
                var header = root.Ui.GetComponentInChildren<CityStatusBar>(true);
                Check(!header.City.gameObject.activeInHierarchy && !header.Resources.gameObject.activeInHierarchy && !header.Settings.gameObject.activeInHierarchy, "Building hides the entire header");
                Check(Contained(layout.scroll.viewport, (RectTransform)ui.inspector.transform), "Building scroll stays inside its window");
            }
            if (name == "building_closed")
            {
                Check(root.Ui.GetComponent<WorldNavigationBar>().Dock.gameObject.activeInHierarchy, "Navigation returns after closing");
                Check(root.Ui.GetComponentInChildren<CityStatusBar>(true).Settings.gameObject.activeInHierarchy, "Settings return after closing");
            }
            if (!root.Ui.Stories.gameObject.activeInHierarchy) return;
            var view = root.Ui.Stories; var panel = view.GetComponent<VipStoryPanelLayout>();
            bool detail = view.ShowingDetails;
            Check(view.detailScroll.gameObject.activeInHierarchy == detail && view.rosterScroll.gameObject.activeInHierarchy != detail, "Exactly one story route is visible");
            if (detail)
            {
                Hit(panel.back);
                Check(Contained((RectTransform)view.detailScroll.transform, panel.frame.viewport), "Story detail scroll fits the visible viewport");
                var action = view.accept.gameObject.activeSelf ? view.accept : view.claim.gameObject.activeSelf ? view.claim : null;
                if (action != null)
                {
                    Check(Contained((RectTransform)action.transform, panel.frame.viewport), "Story action stays visible");
                    Hit(action);
                }
                if (name == "story_locked") Check(!view.accept.gameObject.activeSelf && view.questTitle.text.Contains("100"), "Locked resident explains the population requirement");
                if (name == "toni_active") Check(!view.claim.interactable && view.goals.Any(g => g.gameObject.activeSelf), "Active Toni quest shows unfinished goals");
                if (name == "toni_ready") Check(view.claim.interactable, "Toni reward unlocks only when deliveries are complete");
            }
            else Hit(view.rows.First(r => r.id == "toni").button);
            Hit(root.Ui.Element<Button>("ui.close"));
        }
        private static void Check(bool result, string message) { checks++; if (!result) throw new InvalidOperationException(message); }
        private static bool Contained(RectTransform child, RectTransform parent)
        {
            var corners = new Vector3[4]; child.GetWorldCorners(corners);
            return corners.All(c => { var p = parent.InverseTransformPoint(c); var r = parent.rect; return p.x >= r.xMin - 2 && p.x <= r.xMax + 2 && p.y >= r.yMin - 2 && p.y <= r.yMax + 2; });
        }
        private static void Hit(Button button)
        {
            Canvas.ForceUpdateCanvases(); var r = (RectTransform)button.transform; var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = r.TransformPoint(r.rect.center) }, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, "Button reachable: " + button.name + (hits.Count > 0 ? " covered by " + hits[0].gameObject.name : " no hit"));
        }
        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            try
            {
                UnityEngine.Application.runInBackground = true;
                GameView().Repaint();
                if (File.Exists("Docs/PhoneUiRevision/cancel-capture.request"))
                { File.Delete("Docs/PhoneUiRevision/cancel-capture.request"); throw new OperationCanceledException("Explicit cancellation of the owned capture."); }
                if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 900;
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Phone UI validation deadline.");
                if (root == null)
                {
                    root = Object.FindFirstObjectByType<GameRoot>(); if (root == null) return; root.enabled = false;
                    root.UseTestStore(Path.GetFullPath("Docs/PhoneUiRevision/owned-validation-save.json"), new ManualClock(new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc))); Prepare();
                }
                if (pending != null)
                {
                    if (!File.Exists(pending)) { EditorApplication.QueuePlayerLoopUpdate(); return; }
                    shots.Add(pending); pending = null; index++; frames = 0;
                }
                if (index >= steps.Count) { Finish(); return; }
                var step = steps[index];
                if (frames == 0)
                {
                    UiDisplayMetrics.EvidenceDensity = step.density; Size(step.w, step.h); step.action();
                    root.Session.State.uiScale = step.scale; root.Refresh(); root.Ui.Notify("");
                }
                if (++frames < 18) { EditorApplication.QueuePlayerLoopUpdate(); return; }
                if (Screen.width != step.w || Screen.height != step.h)
                { if (frames < 300) { EditorApplication.QueuePlayerLoopUpdate(); return; } throw new InvalidOperationException("Game View did not resize."); }
                Inspect(step.name); pending = Folder + "/" + step.w + "x" + step.h + "_scale" + step.scale.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "_" + step.name + ".png";
                if (File.Exists(pending)) File.Delete(pending);
                ScreenCapture.CaptureScreenshot(pending); EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (Exception ex) { issues.Add(ex.ToString()); Debug.LogException(ex); Finish(); }
        }
        private static void Finish()
        {
            File.WriteAllText("Docs/PhoneUiRevision/capture_receipt.json", JsonUtility.ToJson(new Receipt { utc = DateTime.UtcNow.ToString("O"), complete = index == steps.Count && issues.Count == 0, checks = checks, screenshots = shots.ToArray(), issues = issues.ToArray() }, true));
            UiDisplayMetrics.EvidenceDensity = 0; SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
            UnityEngine.Application.runInBackground = SessionState.GetBool(Key + ".background", false);
            EditorApplication.isPaused = SessionState.GetBool(Key + ".paused", false);
            SelectSize(SessionState.GetInt(Key + ".size", 0)); EditorApplication.ExitPlaymode();
            EditorApplication.playModeStateChanged += Restore;
        }
        private static void Restore(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= Restore;
            var saved = JsonUtility.FromJson<Scenes>(SessionState.GetString(Key + ".scenes", "{}"));
            if (saved?.setup != null) EditorSceneManager.RestoreSceneManagerSetup(saved.setup);
        }
    }
}

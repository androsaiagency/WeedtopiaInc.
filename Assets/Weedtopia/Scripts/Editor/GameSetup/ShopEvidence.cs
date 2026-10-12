using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// <summary>Actual Game View captures, using an isolated save and a separately launched owned editor.</summary>
    [InitializeOnLoad]
    public static class ShopEvidence
    {
        private const string Key = "Weedtopia.ShopEvidence", Folder = "Docs/ShopRevision/Screenshots";
        [Serializable] private sealed class Receipt { public string utc; public bool complete, physicalDeviceVerified = default; public int checks; public string[] screenshots, issues; }
        [Serializable] private sealed class Scenes { public SceneSetup[] setup; }
        private static GameRoot root;
        private static readonly List<(int w, int h, float density, float scale, string name, Action action)> steps = new List<(int, int, float, float, string, Action)>();
        private static readonly List<string> shots = new List<string>(), issues = new List<string>();
        private static int index, frames, checks;
        private static string pending;
        private static double deadline;
        static ShopEvidence() { if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick; }
        public static void BeginOwned()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "Weedtopia.Editor.GameSetup.ShopEvidence.BeginOwned") < 0)
                throw new InvalidOperationException("Launch a separate owned validation editor explicitly.");
            BurningHillsLayoutInstaller.RequireSafeEditor(); ShopAuthoring.Apply(); Directory.CreateDirectory(Folder);
            SessionState.SetInt(Key + ".pid", System.Diagnostics.Process.GetCurrentProcess().Id);
            SessionState.SetString(Key + ".scenes", JsonUtility.ToJson(new Scenes { setup = EditorSceneManager.GetSceneManagerSetup() }));
            SessionState.SetInt(Key + ".size", PhoneUiEvidence.GameViewSelection());
            SessionState.SetBool(Key + ".background", UnityEngine.Application.runInBackground);
            SessionState.SetBool(Key + ".paused", EditorApplication.isPaused);
            UnityEngine.Application.runInBackground = true; EditorApplication.isPaused = false;
            SessionState.SetBool(Key, true); EditorApplication.update -= Tick; EditorApplication.update += Tick;
            EditorSceneManager.OpenScene(GameScaffold.GameScene); EditorApplication.EnterPlaymode();
        }
        private static void World()
        { root.NewGame("Shopprüfung"); root.Session.CompleteIntro(); root.Ui.Close(); root.Refresh(); }
        private static void Prepare()
        {
            foreach (var size in new[] { (1920, 1080, 1f), (2340, 1080, 2.75f), (1080, 2340, 2.75f), (844, 390, 1f) })
            foreach (float scale in new[] { 1f, 1.2f })
            {
                void Add(string name, Action action) => steps.Add((size.Item1, size.Item2, size.Item3, scale, name, action));
                Add("navigation", World);
                Add("packages", () => { Hit(root.Ui.Element<Button>("shopDock")); root.Ui.ShowShop(); });
                Add("packages_actions", () => root.Ui.Shop.productScroll.verticalNormalizedPosition = 0);
                Add("vip", () => root.Ui.Shop.SelectCategory(ShopCategory.Vip));
                Add("money", () => root.Ui.Shop.SelectCategory(ShopCategory.Money));
                Add("money_bottom", () => root.Ui.Shop.productScroll.verticalNormalizedPosition = 0);
                Add("grinders", () => root.Ui.Shop.SelectCategory(ShopCategory.Grinders));
                Add("boosters", () => root.Ui.Shop.SelectCategory(ShopCategory.Boosters));
                Add("starter_confirmation", () => root.Ui.Shop.Preview("weedtopia.starter"));
                Add("starter_purchased", () => { Hit(root.Ui.Shop.confirmPurchase); root.Ui.Shop.confirmPurchase.onClick.Invoke(); });
                Add("vip_active", () => { root.Ui.Shop.SelectCategory(ShopCategory.Vip); root.Ui.Shop.Preview("weedtopia.vip_30d"); root.Ui.Shop.confirmPurchase.onClick.Invoke(); });
                Add("closed", () => root.Ui.Element<Button>("ui.close").onClick.Invoke());
            }
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
        private static void Inspect(string name)
        {
            var ui = root.Ui.Shop; float pixels = UiDisplayMetrics.Scale(root.Ui.GetComponent<Canvas>());
            if (!ui.gameObject.activeInHierarchy)
            { Hit(root.Ui.Element<Button>("shopDock")); return; }
            Hit(root.Ui.Element<Button>("ui.close"));
            var frame = ui.GetComponent<ModalFrame>();
            Check(frame.body.localScale == Vector3.one, "Shop keeps native text and touch sizing");
            if (ui.Confirming)
            {
                Hit(ui.confirmPurchase); Hit(ui.confirmBack);
                Check(Contained(ui.confirmationScroll.viewport, frame.viewport), "Confirmation scroll is inside the modal");
                Check(Contained((RectTransform)ui.confirmPurchase.transform, frame.viewport), "Confirmation buy action stays visible");
                Check(ui.confirmContents.fontSize * pixels >= 17.5f, "Readable confirmation contents");
            }
            else
            {
                foreach (var button in ui.categories) { Hit(button); Check(((RectTransform)button.transform).rect.height * pixels >= 47.5f, "Category touch target"); }
                Check(Contained(ui.productScroll.viewport, frame.viewport), "Product scroll is inside the modal");
                foreach (var card in ui.cards.Where(c => c.gameObject.activeSelf))
                {
                    Check(card.contents.fontSize * pixels >= 17.5f, "Readable product contents");
                    Check(((RectTransform)card.purchase.transform).rect.height * pixels >= 47.5f, "Purchase touch target");
                    Check(Contained((RectTransform)card.purchase.transform, (RectTransform)card.transform), "Purchase stays inside its card");
                    Check(Contained(card.contents.rectTransform, (RectTransform)card.transform), "Contents stay inside the card");
                    Check(DialoguePanel.PreferredHeight(card.price, card.price.rectTransform.rect.width) <= card.price.rectTransform.rect.height + 2, "The entire price remains readable");
                }
                if (name == "packages_actions" || name == "money_bottom")
                    Hit(ui.cards.Last(c => c.gameObject.activeSelf).purchase);
            }
            if (name == "starter_purchased")
            { Check(root.Session.State.money == new Amount(11000), "Starter money granted"); Check(root.Session.Inventory.Count("build_5m") == 5, "Five starter speedups granted"); }
            if (name == "vip_active") Check(root.Session.Shop.VipActive, "VIP successfully activated");
        }
        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            try
            {
                UnityEngine.Application.runInBackground = true;
                var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w => w.GetType() == type)?.Repaint();
                if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 600;
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Shop capture deadline.");
                if (root == null)
                {
                    root = Object.FindFirstObjectByType<GameRoot>(); if (root == null) return; root.enabled = false;
                    root.UseTestStore(Path.GetFullPath("Docs/ShopRevision/owned-validation-save.json"), new ManualClock(new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc))); Prepare();
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
                    UiDisplayMetrics.EvidenceDensity = step.density; PhoneUiEvidence.FixedTestView(step.w, step.h); step.action();
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
            bool complete = index == steps.Count && issues.Count == 0;
            File.WriteAllText("Docs/ShopRevision/capture_receipt.json", JsonUtility.ToJson(new Receipt { utc = DateTime.UtcNow.ToString("O"), complete = complete, checks = checks, screenshots = shots.ToArray(), issues = issues.ToArray() }, true));
            SessionState.SetBool(Key + ".passed", complete); UiDisplayMetrics.EvidenceDensity = 0; SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
            UnityEngine.Application.runInBackground = SessionState.GetBool(Key + ".background", false);
            EditorApplication.isPaused = SessionState.GetBool(Key + ".paused", false);
            PhoneUiEvidence.SelectSize(SessionState.GetInt(Key + ".size", 0)); EditorApplication.playModeStateChanged += Restore; EditorApplication.ExitPlaymode();
        }
        private static void Restore(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= Restore;
            var saved = JsonUtility.FromJson<Scenes>(SessionState.GetString(Key + ".scenes", "{}"));
            if (saved?.setup?.Any(s => s.isLoaded) == true) EditorSceneManager.RestoreSceneManagerSetup(saved.setup);
            if (SessionState.GetInt(Key + ".pid", 0) == System.Diagnostics.Process.GetCurrentProcess().Id)
                EditorApplication.Exit(SessionState.GetBool(Key + ".passed", false) ? 0 : 2);
        }
    }
}

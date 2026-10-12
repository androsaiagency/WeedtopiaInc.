using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Actual Game View with an isolated save and restored language preference. No mouse/keyboard automation.</summary>
    [InitializeOnLoad] public static class LocalizationEvidence
    {
        private const string Key="Weedtopia.LocalizationEvidence";
        private static string Folder=>SessionState.GetString(Key+".folder","Docs/Localization");
        private static GameRoot root;
        private static List<Action> steps;
        private static readonly List<string> screenshots=new List<string>();
        private static int index,frames,expectedWidth,expectedHeight;
        private static string pending,requested;
        private static DateTime requestUtc;
        private static double deadline,settle;
        static LocalizationEvidence(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
        public static void Begin()
        { BeginCapture("Docs/Localization"); }
        public static void BeginTypography()
        { BeginCapture("Docs/Typography"); }
        public static void BeginRevision()
        { LocalizationRevisionEvidence.Begin(); }
        private static void BeginCapture(string folder)
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Owned batch editor required");
            SessionState.SetString(Key+".folder",folder);
            Directory.CreateDirectory(Folder+"/Screenshots");ProductionUiEvidence.Reset();
            SessionState.SetBool(Key+".hadPreference",PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey));SessionState.SetString(Key+".preference",PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey));
            SessionState.SetBool(Key,true);EditorApplication.update+=Tick;
            EditorSceneManager.OpenScene(GameScaffold.GameScene);SetSize(1920,1080);EditorApplication.EnterPlaymode();
        }
        private static void SetSize(int width,int height)
        {
            PhoneUiEvidence.FixedTestView(width,height);expectedWidth=width;expectedHeight=height;
        }
        private static void Shot(string name,Action action)
        {steps.Add(()=>{root.Ui.Rewards.DismissAll();action();root.Refresh();root.Ui.Notify("");requested=Path.GetFullPath(Folder+"/Screenshots/"+name+".png");frames=0;settle=EditorApplication.timeSinceStartup+1;});}
        private static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+360;
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Localization Game View capture");
                if(steps==null)
                {
                    root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();if(root==null)return;
                    root.enabled=false;root.UseTestStore(Path.GetFullPath(Folder+"/capture_save.json"),new ManualClock(new DateTime(2026,10,4,12,0,0,DateTimeKind.Utc)));root.NewGame("Sprachprüfung");root.Session.CompleteIntro();root.Ui.Close();steps=new List<Action>();
                    foreach(var resolution in new[]{new[]{1920,1080},new[]{1080,1920}})
                    {
                        var size=resolution;
                        foreach(var language in root.Localization.Languages)
                        {
                            string code=language.Code;
                            Shot(code+"-settings-"+size[0]+"x"+size[1],()=>{SetSize(size[0],size[1]);root.SetLanguage(code);root.Ui.Experience.ShowSettings();});
                        }
                        Shot("ru-dropdown-"+size[0]+"x"+size[1],()=>{root.Ui.Experience.language.Show();});
                        Shot("ru-intro-"+size[0]+"x"+size[1],()=>{root.Ui.Experience.language.Hide();root.Session.State.introLine=1;root.Session.State.introSeen=false;root.Ui.ShowIntro();});
                        steps.Add(()=>{root.Session.CompleteIntro();root.Ui.Close();});
                    }
                }
                if(++frames<10||EditorApplication.timeSinceStartup<settle){EditorApplication.QueuePlayerLoopUpdate();return;}
                if(pending!=null)
                {
                    if(!File.Exists(pending)||File.GetLastWriteTimeUtc(pending)<requestUtc){EditorApplication.QueuePlayerLoopUpdate();return;}
                    screenshots.Add(pending);pending=null;index++;frames=0;return;
                }
                if(requested!=null)
                {
                    if(Screen.width!=expectedWidth||Screen.height!=expectedHeight){EditorApplication.QueuePlayerLoopUpdate();return;}
                    pending=requested;requested=null;ProductionUiEvidence.Inspect(root.Ui,Path.GetFileName(pending));requestUtc=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(pending);frames=0;EditorApplication.QueuePlayerLoopUpdate();return;
                }
                if(index<steps.Count)
                {
                    steps[index]();if(requested==null)index++;EditorApplication.QueuePlayerLoopUpdate();return;
                }
                ProductionUiEvidence.Write(Fingerprint(),Folder+"/ui_overflow.json",new[]{"1920x1080","1080x1920"});
                File.WriteAllText(Folder+"/gameview_receipt.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),screenshots=screenshots.ToArray()},true));Finish(0);
            }
            catch(Exception error){File.WriteAllText(Folder+"/gameview_error.txt",error.ToString());Debug.LogException(error);Finish(1);}
        }
        private static string Fingerprint()
        {
            using(var hash=SHA256.Create())
            {
                var files=Directory.GetFiles(LocalizationAuthoring.Folder,"*",SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(TypographyAuthoring.Folder,"*",SearchOption.AllDirectories))
                    .Concat(new[]{ProductionArtImporter.ArtPath})
                    .Concat(Directory.GetFiles("Assets/Weedtopia/Scripts","*.cs",SearchOption.AllDirectories))
                    .Concat(new[]{GameScaffold.GameScene,"Assets/Weedtopia/Game/Scenes/BigRockCity.unity","Assets/Weedtopia/Game/Content/BurningHillsContent.asset"})
                    .OrderBy(path=>path,StringComparer.Ordinal);
                string stamp=string.Join("\n",files.Select(path=>path+":"+BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path)))));
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(stamp))).Replace("-","").ToLowerInvariant();
            }
        }
        private static void Finish(int code)
        {
            if(SessionState.GetBool(Key+".hadPreference",false))PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey,SessionState.GetString(Key+".preference",""));else PlayerPrefs.DeleteKey(GameRoot.LanguagePreferenceKey);PlayerPrefs.Save();
            SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.Exit(code);
        }
        [Serializable]private sealed class Receipt{public string utc;public string[] screenshots;public bool actualPlayModeGameView=true,physicalTouchVerified=false,nativeSpeakerReviewCompleted=false;}
    }

    /// <summary>Explicit screenshot and pointer regression for language selection, inventory and premium confirmation.</summary>
    [InitializeOnLoad]
    internal static class LocalizationRevisionEvidence
    {
        private const string Key = "Weedtopia.LocalizationRevisionEvidence";
        private const string Folder = "Docs/LocalizationRevision/2026-10-06";
        private static readonly List<Step> steps = new List<Step>();
        private static readonly List<Check> checks = new List<Check>();
        private static readonly List<string> screenshots = new List<string>();
        private static GameRoot root;
        private static int index, frames, width, height;
        private static double deadline, settle;
        private static string pending;
        private static DateTime requestUtc;
        private static bool stepStarted;
        private static bool awaitingCity;
        private static int premiumBalance;

        private sealed class Step
        {
            public string name;
            public Action action, inspect;
        }
        [Serializable] private sealed class Check
        {
            public string scenario, operation, language, city;
            public bool passed;
            public string detail;
            public int width, height;
            public float density, uiScale;
        }
        [Serializable] private sealed class Receipt
        {
            public string utc, status, fingerprint;
            public string[] screenshots;
            public Check[] checks;
            public int assertions;
            public bool actualPlayModeGameView = true;
            public bool pointerEventsVerified = true;
            public bool physicalTouchVerified, nativeSpeakerReviewCompleted;
        }
        static LocalizationRevisionEvidence()
        {
            if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
        }
        public static void Begin()
        {
            if (!UnityEngine.Application.isBatchMode) throw new InvalidOperationException("Owned batch editor required for revision evidence.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Existing Play Mode is protected.");
            foreach (var scene in Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt))
                if (scene.isDirty) throw new InvalidOperationException("Unsaved scene changes are protected.");
            Directory.CreateDirectory(Folder + "/Screenshots");
            SessionState.SetBool(Key + ".hadPreference", PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey));
            SessionState.SetString(Key + ".preference", PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey));
            SessionState.SetBool(Key, true);
            EditorApplication.update += Tick;
            EditorSceneManager.OpenScene(GameScaffold.GameScene);
            PhoneUiEvidence.FixedTestView(1920, 1080);
            width = 1920; height = 1080;
            EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            try
            {
                if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 600;
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Localization revision Game View evidence exceeded its deadline.");
                if (awaitingCity)
                {
                    var destination = UnityEngine.Object.FindFirstObjectByType<GameRoot>();
                    if (destination == null || destination.SceneCityId != "big_rock_city" || destination.Session == null)
                    { EditorApplication.QueuePlayerLoopUpdate(); return; }
                    root = destination; root.enabled = false; root.Session.CompleteIntro(); root.Ui.Close();
                    awaitingCity = false;
                }
                if (root == null)
                {
                    root = UnityEngine.Object.FindFirstObjectByType<GameRoot>();
                    if (root == null) return;
                    root.enabled = false;
                    root.UseTestStore(Path.GetFullPath(Folder + "/capture_save.json"), new ManualClock(new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc)));
                    root.NewGame("Sprachprüfung"); root.Session.CompleteIntro(); root.Ui.Close();
                    BuildSteps();
                }
                if (pending != null)
                {
                    if (!File.Exists(pending) || File.GetLastWriteTimeUtc(pending) < requestUtc) { EditorApplication.QueuePlayerLoopUpdate(); return; }
                    screenshots.Add(pending); pending = null; Advance();
                }
                if (index == steps.Count) { WriteReceipt("passed"); Finish(0); return; }
                var step = steps[index];
                if (!stepStarted)
                {
                    stepStarted = true; step.action?.Invoke(); frames = 0;
                    settle = EditorApplication.timeSinceStartup + .3;
                }
                root.Refresh(); Canvas.ForceUpdateCanvases();
                if (++frames < 8 || EditorApplication.timeSinceStartup < settle || Screen.width != width || Screen.height != height)
                { EditorApplication.QueuePlayerLoopUpdate(); return; }
                step.inspect?.Invoke();
                if (step.name == null) { Advance(); EditorApplication.QueuePlayerLoopUpdate(); return; }
                pending = Path.GetFullPath(Folder + "/Screenshots/" + step.name + ".png");
                requestUtc = DateTime.UtcNow; ScreenCapture.CaptureScreenshot(pending);
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (Exception error)
            {
                File.WriteAllText(Folder + "/gameview_error.txt", error.ToString());
                WriteReceipt("failed"); Debug.LogException(error); Finish(1);
            }
        }
        private static void Advance() { index++; stepStarted = false; frames = 0; }
        private static void Add(Action action, Action inspect = null, string name = null)
        { steps.Add(new Step { action = action, inspect = inspect, name = name }); }
        private static void BuildSteps()
        {
            float minimum = root.Ui.Element<Slider>("scale").minValue, maximum = root.Ui.Element<Slider>("scale").maxValue;
            SettingsScenario("landscape-min-en", 1920, 1080, 1, minimum, "en", false);
            SettingsScenario("screenshot-max-en", 1644, 762, 1, maximum, "en", false);
            SettingsScenario("phone-min-ru", 2340, 1080, 3, minimum, "ru", true);
            SettingsScenario("phone-max-ru", 2340, 1080, 3, maximum, "ru", true);
            SettingsScenario("portrait-min-en", 1080, 1920, 3, minimum, "en", false);
            SettingsScenario("portrait-max-ru", 1080, 1920, 3, maximum, "ru", false);
            foreach (var language in root.Localization.Languages)
            {
                string code = language.Code;
                PremiumScenario(code + "-premium-landscape", 2340, 1080, 3, 1, code);
                InventoryScenario(code + "-inventory-landscape", 2340, 1080, 3, 1, code);
            }
            PremiumScenario("ru-premium-portrait", 1080, 1920, 3, maximum, "ru");
            InventoryScenario("de-inventory-portrait", 1080, 1920, 3, maximum, "de");
            Add(() =>
            {
                root.Ui.Close(); root.Session.State.City.storyResidentMilestone = 1000; root.Refresh(); root.Ui.ShowCities();
                root.Ui.Campaign.accept.onClick.Invoke(); root.Ui.Rewards.DismissAll(); root.Ui.Close();
                var result = root.TravelToCity("big_rock_city");
                if (result != Failure.None) throw new InvalidOperationException("Could not prepare Big Rock City scene regression: " + result);
                awaitingCity = true;
            });
            SettingsScenario("big-rock-phone-max-ru", 2340, 1080, 3, maximum, "ru", false);
            PremiumScenario("big-rock-ru-premium-landscape", 2340, 1080, 3, maximum, "ru");
            InventoryScenario("big-rock-ru-inventory-landscape", 2340, 1080, 3, maximum, "ru");
        }
        private static void Configure(int w, int h, float density, float uiScale, string language)
        {
            root.Ui.Experience.language.Hide(); root.Ui.Close();
            PhoneUiEvidence.FixedTestView(w, h); width = w; height = h;
            UiDisplayMetrics.EvidenceDensity = density; root.Session.State.uiScale = uiScale;
            root.SetLanguage(language); root.Refresh();
        }
        private static void SettingsScenario(string scenario, int w, int h, float density, float uiScale, string language, bool scrollLast)
        {
            // Configuration settles before the dropdown is opened, so old geometry cannot determine popup placement.
            Add(() => { Configure(w, h, density, uiScale, language); root.Ui.Experience.ShowSettings(); });
            Add(() => Click(root.Ui.Experience.language, scenario + "/open"), () => InspectPopupBounds(scenario + "/initial"));
            Add(() => PopupScroll().verticalNormalizedPosition = 1,
                () => InspectPopup(scenario, "Deutsch"), scenario + "-dropdown");
            Add(() => ClickChoice("Deutsch", scenario + "/select-de"), () => VerifyLanguage(scenario, "de"));
            Add(null, () => InspectSettings(scenario), scenario + "-deutsch");
            Add(() => root.Ui.Experience.SelectSettingsTab(true),
                () => RequireCaption(root.Ui.Experience.applyProfile, "authored.profil_speichern.a883f67d", scenario + "/profile-de"));
            Add(() => root.Ui.Experience.SelectSettingsTab(false));
            if (!scrollLast) return;
            Add(() => Click(root.Ui.Experience.language, scenario + "/reopen-last"));
            Add(() => PopupScroll().verticalNormalizedPosition = 0,
                () => InspectPopup(scenario + "/scrolled", "Русский"), scenario + "-last-option");
            Add(() => ClickChoice("Русский", scenario + "/select-ru"), () => VerifyLanguage(scenario, "ru"));
            Add(() => Click(root.Ui.Experience.language, scenario + "/return-de"));
            Add(() => PopupScroll().verticalNormalizedPosition = 1,
                () => InspectPopup(scenario + "/return", "Deutsch"));
            Add(() => ClickChoice("Deutsch", scenario + "/return-select-de"), () => VerifyLanguage(scenario + "/return", "de"));
        }
        private static ScrollRect PopupScroll()
        {
            var list = root.Ui.Experience.language.GetComponentsInChildren<ScrollRect>(true).Where(x => x.gameObject.activeInHierarchy).ToArray();
            if (list.Length != 1) throw new InvalidOperationException("Expected one live language popup scroll, found " + list.Length + ".");
            return list[0];
        }
        private static Toggle Choice(string caption)
        {
            var matches = PopupScroll().GetComponentsInChildren<Toggle>().Where(t =>
                t.GetComponentsInChildren<Text>().Any(text => text.text == caption)).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected exactly one live language option " + caption + ", found " + matches.Length + ".");
            return matches[0];
        }
        private static void ClickChoice(string caption, string scenario)
        {
            var scroll = PopupScroll(); var toggle = Choice(caption);
            Assert(scenario, "option visible within scroll viewport", Contains(ScreenRect(scroll.viewport), ScreenRect((RectTransform)toggle.transform), 2), caption);
            Click(toggle, scenario);
        }
        private static void Click(Component expected, string scenario)
        {
            var rect = (RectTransform)expected.transform; Canvas.ForceUpdateCanvases();
            var point = ScreenRect(rect).center;
            var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            bool reached = hits.Count > 0 && (hits[0].gameObject.transform == expected.transform || hits[0].gameObject.transform.IsChildOf(expected.transform));
            Assert(scenario, "actual UI raycast reaches control", reached, hits.Count == 0 ? expected.name + ": no raycast hit" : expected.name + ": first hit " + hits[0].gameObject.name);
            pointer.pointerCurrentRaycast = hits[0];
            Assert(scenario, "pointer click handler executed", ExecuteEvents.Execute(expected.gameObject, pointer, ExecuteEvents.pointerClickHandler), expected.name);
        }
        private static void VerifyLanguage(string scenario, string language)
        {
            Assert(scenario, "runtime selected language", root.Localization.Language == language, root.Localization.Language);
            Assert(scenario, "persisted selected language", PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey) == language, PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey));
            Assert(scenario, "selected caption refreshed", root.Ui.Experience.language.captionText.text == root.Localization.Languages.Single(l => l.Code == language).NativeName,
                root.Ui.Experience.language.captionText.text);
        }
        private static void InspectPopup(string scenario, string caption)
        {
            InspectPopupBounds(scenario);
            var scroll = PopupScroll();
            var option = Choice(caption);
            Assert(scenario, "language option visible in viewport", Contains(ScreenRect(scroll.viewport), ScreenRect((RectTransform)option.transform), 2), caption);
            RequireFont(option.GetComponentInChildren<Text>(), 14, scenario);
        }
        private static void InspectPopupBounds(string scenario)
        {
            var scroll = PopupScroll(); var popup = (RectTransform)scroll.transform;
            Assert(scenario, "popup within actual safe area", Contains(Screen.safeArea, ScreenRect(popup), 2), ScreenRect(popup).ToString());
            Assert(scenario, "popup viewport within actual safe area", Contains(Screen.safeArea, ScreenRect(scroll.viewport), 2), ScreenRect(scroll.viewport).ToString());
        }
        private static void InspectSettings(string scenario)
        {
            Assert(scenario, "German settings title", root.Ui.Element<Text>("settingsTitle").text == root.Definitions.Text("ui.pause"), root.Ui.Element<Text>("settingsTitle").text);
            RequireCaption(root.Ui.Experience.gameTab, "settings.game", scenario);
            RequireCaption(root.Ui.Experience.profileTab, "settings.profile", scenario);
            RequireCaption(root.Ui.Element<Button>("ui.resume"), "ui.resume", scenario);
            RequireCaption(root.Ui.Element<Button>("ui.restart_intro"), "ui.restart_intro", scenario);
            RequireCaption(root.Ui.Element<Button>("ui.reset"), "ui.reset", scenario);
            Assert(scenario, "volume label localized", root.Ui.Element<Text>("volumeLabel").text == root.Definitions.Text("ui.volume"), root.Ui.Element<Text>("volumeLabel").text);
            Assert(scenario, "scale label localized", root.Ui.Element<Text>("scaleLabel").text == root.Definitions.Text("ui.scale"), root.Ui.Element<Text>("scaleLabel").text);
            if (root.Ui.Experience.copyDiagnostics != null) RequireCaption(root.Ui.Experience.copyDiagnostics, "settings.diagnostics.copy", scenario);
            RequireFont(root.Ui.Experience.language.captionText, 16, scenario);
            VerifyLanguage(scenario, "de");
        }
        private static void PremiumScenario(string scenario, int w, int h, float density, float uiScale, string language)
        {
            Add(() =>
            {
                Configure(w, h, density, uiScale, language);
                // Start real construction through the public command. The following click opens its genuine offer.
                var building = root.Session.State.Find(BuildingId);
                building.buildingSeconds = 0; root.Session.State.money = new Amount(1000);
                premiumBalance = root.Session.State.grinders;
                var result = root.Session.Upgrades.BuyWithMoney(building.id);
                if (result != Failure.None) throw new InvalidOperationException("Could not prepare construction offer: " + result);
                root.Select(building.id, true); root.Refresh();
                root.Ui.Element<Button>("ui.finish").onClick.Invoke();
            }, () => InspectPremium(scenario), scenario);
            Add(() => Click(root.Ui.Element<Button>("ui.cancel"), scenario + "/cancel"),
                () => Assert(scenario, "cancel closes offer without spending", !root.Ui.ModalOpen && root.Session.State.grinders == premiumBalance,
                    "grinders=" + root.Session.State.grinders));
        }
        private static void InspectPremium(string scenario)
        {
            var panel = root.Ui.Element<RectTransform>("premium");
            Assert(scenario, "premium panel visible", panel.gameObject.activeInHierarchy, panel.name);
            Assert(scenario, "premium panel within safe area", Contains(Screen.safeArea, ScreenRect(panel), 2), ScreenRect(panel).ToString());
            var frame = panel.GetComponent<ModalFrame>();
            Assert(scenario, "premium heading localized", frame.heading.text == root.Definitions.Text("ui.premium_title"), frame.heading.text);
            var body = root.Ui.Element<Text>("premiumText");
            // The selected building's family is the source of the translated name, including city-specific definitions.
            string expected = GameText.Format("ui.premium", Math.Max(1, (int)Math.Ceiling((double)root.Session.State.Find(BuildingId).buildingSeconds / root.Definitions.secondsPerGrinder)),
                root.Definitions.Text(root.Definitions.Building(root.Session.State.Find(BuildingId).family).nameKey));
            Assert(scenario, "premium body localized", body.text == expected, body.text);
            RequireFont(frame.heading, 20, scenario); RequireFont(body, 18, scenario); RequireTextFits(frame.heading, scenario); RequireTextFits(body, scenario);
            foreach (var pair in new[] { new[] { "ui.cancel", "ui.cancel" }, new[] { "ui.confirm", "ui.confirm" } })
            {
                var button = root.Ui.Element<Button>(pair[0]); var caption = button.GetComponentInChildren<Text>();
                Assert(scenario, "premium action localized", caption.text == root.Definitions.Text(pair[1]), caption.text);
                RequireFont(caption, 16, scenario); RequireTextFits(caption, scenario);
                Assert(scenario, "premium action minimum display height", ScreenRect((RectTransform)button.transform).height / UiDisplayMetrics.Density >= 43.5f,
                    button.name + ": " + (ScreenRect((RectTransform)button.transform).height / UiDisplayMetrics.Density).ToString("F2"));
            }
        }
        private static void InventoryScenario(string scenario, int w, int h, float density, float uiScale, string language)
        {
            Add(() =>
            {
                Configure(w, h, density, uiScale, language);
                var item = root.Definitions.items.First(i => i.effect == ItemEffect.ConstructionTime);
                InventoryItems.Grant(root.Session.State, new[] { new ItemStack { id = item.id, count = 1 } }, new List<RewardItem>());
                root.Session.State.revision++; root.Ui.ShowCollection();
            }, () => InspectInventory(scenario), scenario);
            Add(() => Click(root.Ui.Collection.categoryButtons[2], scenario + "/boosters"),
                () => Assert(scenario, "inventory category pointer selection", root.Ui.Collection.SelectedCategory == 2, "category=" + root.Ui.Collection.SelectedCategory));
            Add(() => Click(root.Ui.Collection.tiles.First(t => t.gameObject.activeInHierarchy).button, scenario + "/open-item"),
                () => InspectInventoryDetail(scenario), scenario == "de-inventory-portrait" ? scenario + "-detail" : null);
        }
        private static void InspectInventory(string scenario)
        {
            var view = root.Ui.Collection;
            Assert(scenario, "inventory title localized", view.heading.text == root.Definitions.Text("reward.inventory"), view.heading.text);
            RequireTextFits(view.GetComponent<ModalFrame>().heading, scenario);
            RequireCaption(view.inventoryTab, "reward.tab.inventory", scenario);
            RequireCaption(view.achievementsTab, "reward.tab.achievements", scenario);
            RequireCaption(view.historyTab, "reward.tab.history", scenario);
            string[] keys = { "inventory.all", "inventory.filter.team", "inventory.filter.build_time", "inventory.category.Supplies" };
            for (int i = 0; i < view.categoryButtons.Length; i++)
            {
                var text = view.categoryButtons[i].GetComponentInChildren<Text>();
                Assert(scenario, "inventory category localized", text.text == root.Definitions.Text(keys[i]), keys[i] + ": " + text.text);
                RequireFont(text, 12, scenario); RequireTextFits(text, scenario);
                Assert(scenario, "inventory category within safe area", Contains(Screen.safeArea, ScreenRect((RectTransform)view.categoryButtons[i].transform), 2), text.text);
            }
            InspectInventoryTiles(scenario);
        }
        private static void InspectInventoryTiles(string scenario)
        {
            var view = root.Ui.Collection;
            var layout = view.GetComponent<InventoryPanelLayout>();
            var viewport = ScreenRect(layout.grid.viewport);
            foreach (var tile in view.tiles.Where(t => t.gameObject.activeInHierarchy && t.title.enabled && !string.IsNullOrWhiteSpace(t.title.text)))
            {
                var title = ScreenRect(tile.title.rectTransform);
                // Off-screen data rows remain active inside the authored ScrollRect; intentional clipping is valid.
                if (!title.Overlaps(viewport)) continue;
                Assert(scenario, "inventory tile title localized", tile.title.text == RewardText.Name(root.Definitions, tile.Item, true), tile.title.text);
                RequireFont(tile.title, 14, scenario); RequireTextFits(tile.title, scenario);
                var iconMask = (RectTransform)tile.icon.transform.parent;
                var obstacles = new List<RectTransform> { tile.icon.rectTransform, iconMask, tile.quantityBadge.rectTransform };
                if (tile.quantity.enabled && !string.IsNullOrWhiteSpace(tile.quantity.text)) obstacles.Add(tile.quantity.rectTransform);
                foreach (var obstacle in obstacles)
                {
                    if (!obstacle.gameObject.activeInHierarchy) continue;
                    Assert(scenario, "inventory tile text avoids icon and quantity", !Overlap(title, ScreenRect(obstacle), 1),
                        tile.title.text + ": title=" + title + ", " + obstacle.name + "=" + ScreenRect(obstacle));
                }
                if (tile.quantityBadge.gameObject.activeInHierarchy)
                    Assert(scenario, "inventory tile icon mask avoids quantity badge", !Overlap(ScreenRect(iconMask), ScreenRect(tile.quantityBadge.rectTransform), 1),
                        tile.title.text + ": icon mask=" + ScreenRect(iconMask) + ", quantity badge=" + ScreenRect(tile.quantityBadge.rectTransform));
            }
        }
        private static void InspectInventoryDetail(string scenario)
        {
            var detail = root.Ui.Collection.details;
            var item = root.Definitions.items.Single(i => i.id == detail.SelectedItem.key);
            Assert(scenario, "inventory detail title localized", detail.title.text == root.Definitions.Text(item.nameKey), detail.title.text);
            Assert(scenario, "inventory detail description localized", detail.description.text == root.Definitions.Text(item.descriptionKey), detail.description.text);
            Assert(scenario, "inventory detail action localized", detail.actionLabel.text == root.Definitions.Text("inventory.boost"), detail.actionLabel.text);
            Assert(scenario, "inventory detail target heading localized", detail.choiceHeading.text == root.Definitions.Text("inventory.choose_build"), detail.choiceHeading.text);
            foreach (var text in new[] { detail.title, detail.metadata, detail.description, detail.effect, detail.choiceHeading, detail.choiceLabel, detail.actionLabel })
                if (text.gameObject.activeInHierarchy && text.enabled && !string.IsNullOrWhiteSpace(text.text)) RequireTextFits(text, scenario);
            if (detail.cancel.gameObject.activeInHierarchy) RequireCaption(detail.cancel, "inventory.cancel", scenario);
        }
        private static void RequireCaption(Button button, string key, string scenario)
        {
            var text = button.GetComponentInChildren<Text>(true);
            Assert(scenario, "button caption matches selected language", text.text == root.Definitions.Text(key), key + ": " + text.text);
        }
        private static void RequireFont(Text text, float minimum, string scenario)
        {
            var rect = ScreenRect(text.rectTransform);
            float scale = text.rectTransform.rect.width > 0 ? rect.width / text.rectTransform.rect.width : 0;
            int font = text.resizeTextForBestFit ? text.cachedTextGenerator.fontSizeUsedForBestFit : text.fontSize;
            float displayFont = font * scale / UiDisplayMetrics.Density;
            Assert(scenario, "rendered font minimum", displayFont >= minimum - .5f,
                text.name + ": " + displayFont.ToString("F2") + " display units, expected >= " + minimum);
        }
        private static void RequireTextFits(Text text, string scenario)
        {
            var size = text.rectTransform.rect.size;
            float preferred = text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text, text.GetGenerationSettings(size)) / text.pixelsPerUnit;
            Assert(scenario, "localized text fits available rectangle", preferred <= size.y + 2,
                text.name + ": preferred=" + preferred.ToString("F2") + ", available=" + size.y.ToString("F2"));
        }
        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>(); var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var points = corners.Select(corner => RectTransformUtility.WorldToScreenPoint(camera, corner)).ToArray();
            return Rect.MinMaxRect(points.Min(point => point.x), points.Min(point => point.y), points.Max(point => point.x), points.Max(point => point.y));
        }
        private static bool Contains(Rect outer, Rect inner, float tolerance)
        { return inner.xMin >= outer.xMin - tolerance && inner.yMin >= outer.yMin - tolerance && inner.xMax <= outer.xMax + tolerance && inner.yMax <= outer.yMax + tolerance; }
        private static bool Overlap(Rect first, Rect second, float tolerance)
        { return Mathf.Min(first.xMax, second.xMax) - Mathf.Max(first.xMin, second.xMin) > tolerance && Mathf.Min(first.yMax, second.yMax) - Mathf.Max(first.yMin, second.yMin) > tolerance; }
        private static void Assert(string scenario, string operation, bool passed, string detail)
        {
            checks.Add(new Check { scenario = scenario, operation = operation, passed = passed, detail = detail, width = Screen.width, height = Screen.height,
                density = UiDisplayMetrics.Density, uiScale = root.Session.State.uiScale, language = root.Localization.Language, city = root.SceneCityId });
            if (!passed) throw new InvalidOperationException(scenario + ": " + operation + " failed: " + detail);
        }
        private static string BuildingId => root.SceneCityId == "big_rock_city" ? "br_farm_grow_01" : "farm_grow_01";
        private static string Fingerprint()
        {
            using (var hash = SHA256.Create())
            {
                var files = Directory.GetFiles(LocalizationAuthoring.Folder, "*", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles("Assets/Weedtopia/Scripts", "*.cs", SearchOption.AllDirectories))
                    .Concat(new[] { GameScaffold.GameScene }).OrderBy(path => path, StringComparer.Ordinal);
                var stamp = string.Join("\n", files.Select(path => path + ":" + BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path)))));
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(stamp))).Replace("-", "").ToLowerInvariant();
            }
        }
        private static void WriteReceipt(string status)
        {
            File.WriteAllText(Folder + "/gameview_receipt.json", JsonUtility.ToJson(new Receipt { utc = DateTime.UtcNow.ToString("O"), status = status,
                screenshots = screenshots.ToArray(), checks = checks.ToArray(), assertions = checks.Count, fingerprint = Fingerprint() }, true));
        }
        private static void Finish(int code)
        {
            UiDisplayMetrics.EvidenceDensity = 0;
            if (SessionState.GetBool(Key + ".hadPreference", false)) PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey, SessionState.GetString(Key + ".preference", ""));
            else PlayerPrefs.DeleteKey(GameRoot.LanguagePreferenceKey);
            PlayerPrefs.Save(); SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
            EditorApplication.Exit(code);
        }
    }
}

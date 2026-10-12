using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit, repeatable migration of the existing UI; never creates fixed UI at runtime.</summary>
    public static class MenuLayoutAuthoring
    {
        public const string HeaderPath = "Assets/Weedtopia/Production/Prefabs/CityStatusBar.prefab";
        public const string CounterPath = "Assets/Weedtopia/Production/Prefabs/ResourceCounter.prefab";
        public const float DockWidth = 1008;
        private static readonly Color Paper = new Color(.96f, .91f, .77f), Gold = new Color(.98f, .76f, .36f);

        [MenuItem("Weedtopia/Presentation/Install Direct Menu Layout")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before authoring.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save manual scene changes first.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var root = Object.FindFirstObjectByType<GameRoot>();
                var ui = root.Ui;
                if (ui.GetComponentInChildren<CityStatusBar>(true) != null)
                {
                    EnsureDock(root.Presentation.art);
                    var existingDock = ui.GetComponent<WorldNavigationBar>().Dock;
                    bool changed = BindInventoryNavigation(ui, existingDock);
                    if (existingDock.sizeDelta.x != DockWidth)
                    {
                        existingDock.sizeDelta = new Vector2(DockWidth, 104);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(existingDock);
                        changed = true;
                    }
                    if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
                    Validate(root); return;
                }
                var art = root.Presentation.art;
                var serialized = new SerializedObject(ui);
                var bindings = serialized.FindProperty("bindings");
                var map = new Dictionary<string, GameObject>();
                for (int i = 0; i < bindings.arraySize; i++)
                {
                    var entry = bindings.GetArrayElementAtIndex(i);
                    map.Add(entry.FindPropertyRelative("id").stringValue, (GameObject)entry.FindPropertyRelative("target").objectReferenceValue);
                }
                void Bind(string id, GameObject target)
                {
                    for (int i = 0; i < bindings.arraySize; i++)
                    {
                        var entry = bindings.GetArrayElementAtIndex(i);
                        if (entry.FindPropertyRelative("id").stringValue != id) continue;
                        entry.FindPropertyRelative("target").objectReferenceValue = target; return;
                    }
                    int index = bindings.arraySize++;
                    var added = bindings.GetArrayElementAtIndex(index);
                    added.FindPropertyRelative("id").stringValue = id;
                    added.FindPropertyRelative("target").objectReferenceValue = target;
                }

                EnsureDock(art);
                EnsureCounter(art);
                EnsureHeader(art);
                var safe = (RectTransform)serialized.FindProperty("safeArea").objectReferenceValue;
                var header = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HeaderPath), safe);
                var status = header.GetComponent<CityStatusBar>();
                serialized.FindProperty("cityStatus").objectReferenceValue = status;
                Bind("ui.pause", status.Settings.gameObject);
                var dock = ui.GetComponent<WorldNavigationBar>().Dock;
                dock.sizeDelta = new Vector2(DockWidth, 104);
                PrefabUtility.RecordPrefabInstancePropertyModifications(dock);
                var legacy = Rect("ArchivedTownMenu", ui.transform, Vector2.zero);
                legacy.gameObject.SetActive(false);
                foreach (var old in new[] { ui.Interaction.overview.gameObject, ui.Interaction.inventory.gameObject, ui.Interaction.launcher.gameObject, map["ui.pause"], map["ui.save"], map["ui.townhall"], ui.Interaction.townMenu })
                { old.transform.SetParent(legacy, false); old.SetActive(false); }
                ui.Interaction.overview = dock.Find("Overview").GetComponent<Button>();
                ui.Interaction.inventory = dock.Find("Team").GetComponent<Button>();

                var workers = map["workers"].GetComponent<RectTransform>();
                workers.sizeDelta = new Vector2(1130, 800);
                Place(map["workersTitle"].transform, workers, new Vector2(-245, -48), new Vector2(530, 60));
                map["workersTitle"].GetComponent<Text>().text = "Mein Team";
                Place(map["filter"].transform, workers, new Vector2(-280, -124), new Vector2(470, 48));
                Place(map["workerList"].transform.parent, workers, new Vector2(-280, -450), new Vector2(470, 580));
                var cards = Counter(workers, art, "Cards", "cards", "KARTEN", new Vector2(280, -76));
                Place(map["ui.spin"].transform, workers, new Vector2(280, -183), new Vector2(472, 58));
                var hint = Label("TeamSpinHint", workers, "", new Vector2(280, -239), new Vector2(472, 46), art.bodyFont, 18);
                hint.alignment = TextAnchor.MiddleCenter; hint.color = Gold; Bind("teamSpinHint", hint.gameObject);
                var reveal = root.Presentation.workerReveal;
                Place(reveal, workers, new Vector2(280, -285), new Vector2(472, 153)); reveal.pivot = new Vector2(.5f, 1);
                Place(map["workerText"].transform, workers, new Vector2(280, -501), new Vector2(470, 126));
                Place(map["ui.remove"].transform, workers, new Vector2(280, -605), new Vector2(450, 48));
                Place(map["ui.worker_level"].transform, workers, new Vector2(280, -665), new Vector2(450, 48));
                Place(map["ui.worker_rank"].transform, workers, new Vector2(280, -725), new Vector2(450, 48));

                var journal = map["journal"].GetComponent<RectTransform>();
                journal.sizeDelta = new Vector2(1180, 800);
                var quest = map["questTitle"].transform.parent;
                Place(quest, journal, new Vector2(-375, -125), new Vector2(350, 390));
                ((RectTransform)quest).pivot = new Vector2(.5f, 1);
                Place(map["journalList"].transform.parent, journal, new Vector2(175, -415), new Vector2(680, 590));
                Place(map["ui.daily"].transform, journal, new Vector2(-375, -570), new Vector2(350, 60));
                Place(map["recovery"].transform, journal, new Vector2(-375, -645), new Vector2(350, 60));

                root.Presentation.currencyValues = new[]
                {
                    status.Resources.Find("Money/Value").GetComponent<Text>(),
                    status.Resources.Find("Grinders/Value").GetComponent<Text>(),
                    cards.Find("Value").GetComponent<Text>(),
                    status.Resources.Find("Residents/Value").GetComponent<Text>()
                };
                header.transform.SetAsLastSibling(); dock.SetAsLastSibling(); map["modalShade"].transform.SetAsLastSibling();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                BindInventoryNavigation(ui, dock);
                EditorUtility.SetDirty(ui.Interaction); EditorUtility.SetDirty(root.Presentation); EditorUtility.SetDirty(ui);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Validate(root);
            }
            finally { if (!UnityEngine.Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static void EnsureDock(PresentationArt art)
        {
            var contents = PrefabUtility.LoadPrefabContents(WorldNavigationAuthoring.DockPath);
            try
            {
                bool changed=false;
                string[] names = { "Overview", "Farm", "City", "Settlement", "Industry", "Orders", "Team", "Inventory", "LuckyWheel" };
                string[] icons = { "zoom", "farm", "city", "settlement", "industry", "quest", "person", "cargo", "laurel" };
                for(int i=0;i<names.Length;i++)
                    if(contents.transform.Find(names[i])==null){Round(contents.transform,art,names[i],icons[i]);changed=true;}
                var size=new Vector2(DockWidth,104);
                if(contents.GetComponent<RectTransform>().sizeDelta!=size){contents.GetComponent<RectTransform>().sizeDelta=size;changed=true;}
                for (int i = 0; i < names.Length; i++)
                {
                    var item = (RectTransform)contents.transform.Find(names[i]);
                    var position=new Vector2((i-4)*112,0);
                    if(item.GetSiblingIndex()==i&&item.anchoredPosition==position)continue;
                    item.SetSiblingIndex(i); item.anchoredPosition=position;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(item);changed=true;
                }
                var wheel=contents.transform.Find("LuckyWheel");
                if(wheel.Find("WheelSymbol")==null)
                {
                    var symbol=Rect("WheelSymbol",wheel,new Vector2(64,64));symbol.anchorMin=symbol.anchorMax=new Vector2(.5f,.5f);symbol.anchoredPosition=Vector2.zero;
                    var graphic=symbol.gameObject.AddComponent<LuckyWheelGraphic>();graphic.raycastTarget=false;
                    graphic.SetSectors(new[]{new Color(.12f,.37f,.29f),new Color(.50f,.32f,.10f),new Color(.13f,.32f,.44f),
                        new Color(.11f,.31f,.29f),new Color(.34f,.19f,.47f),new Color(.11f,.31f,.29f),
                        new Color(.13f,.32f,.44f),new Color(.11f,.31f,.29f),new Color(.34f,.19f,.47f),new Color(.11f,.31f,.29f)});
                    symbol.SetSiblingIndex(0);
                    var icon=wheel.Find("Icon").GetComponent<RectTransform>();icon.sizeDelta=new Vector2(13,13);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(icon);changed=true;
                }
                if(wheel.Find("FreeBadge")==null)
                {
                    var badge=Rect("FreeBadge",wheel,new Vector2(22,22));badge.anchorMin=badge.anchorMax=new Vector2(.5f,.5f);badge.anchoredPosition=new Vector2(30,30);
                    var image=badge.gameObject.AddComponent<Image>();image.sprite=art.Get("star");image.preserveAspect=true;image.raycastTarget=false;changed=true;
                }
                if(changed)PrefabUtility.SaveAsPrefabAsset(contents, WorldNavigationAuthoring.DockPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
        private static bool BindInventoryNavigation(GameUi ui,RectTransform dock)
        {
            var serialized=new SerializedObject(ui);var bindings=serialized.FindProperty("bindings");bool changed=false;
            foreach(var pair in new[]{("inventoryDock",dock.Find("Inventory")),("wheelDock",dock.Find("LuckyWheel")),("wheelDockFree",dock.Find("LuckyWheel/FreeBadge"))})
            {
                int index=-1;
                for(int i=0;i<bindings.arraySize;i++)if(bindings.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue==pair.Item1){index=i;break;}
                if(index<0){index=bindings.arraySize++;bindings.GetArrayElementAtIndex(index).FindPropertyRelative("id").stringValue=pair.Item1;changed=true;}
                var target=bindings.GetArrayElementAtIndex(index).FindPropertyRelative("target");
                if(target.objectReferenceValue==pair.Item2.gameObject)continue;
                target.objectReferenceValue=pair.Item2.gameObject;changed=true;
            }
            if(changed){serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(ui);}
            return changed;
        }

        private static void EnsureCounter(PresentationArt art)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CounterPath) != null) return;
            var counter = Rect("ResourceCounter", null, new Vector2(218, 74));
            counter.pivot = new Vector2(.5f, 1);
            var background = counter.gameObject.AddComponent<Image>();
            background.sprite = art.Get("button"); background.type = Image.Type.Sliced; background.pixelsPerUnitMultiplier = 2;
            var icon = Rect("Icon", counter, new Vector2(44, 44)).gameObject.AddComponent<Image>();
            icon.rectTransform.anchoredPosition = new Vector2(-77, -37); icon.sprite = art.Get("money"); icon.preserveAspect = true; icon.raycastTarget = false;
            Label("Caption", counter, "GELD", new Vector2(23, -21), new Vector2(136, 22), art.headingFont, 13).color = Gold;
            var value = Label("Value", counter, "0", new Vector2(23, -48), new Vector2(136, 34), art.headingFont, 25);
            value.resizeTextForBestFit = true; value.resizeTextMinSize = 16; value.resizeTextMaxSize = 25;
            PrefabUtility.SaveAsPrefabAsset(counter.gameObject, CounterPath); Object.DestroyImmediate(counter.gameObject);
        }

        private static void EnsureHeader(PresentationArt art)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HeaderPath) != null) return;
            var header = Rect("CityStatusBar", null, Vector2.zero);
            header.anchorMin = Vector2.zero; header.anchorMax = Vector2.one; header.offsetMin = header.offsetMax = Vector2.zero;
            var city = Rect("City", header, new Vector2(290, 74));
            city.anchorMin = city.anchorMax = city.pivot = new Vector2(0, 1); city.anchoredPosition = new Vector2(24, -18);
            var background = city.gameObject.AddComponent<Image>(); background.sprite = art.Get("button"); background.type = Image.Type.Sliced; background.pixelsPerUnitMultiplier = 2;
            Label("Caption", city, "DEINE STADT", new Vector2(0, -21), new Vector2(250, 22), art.headingFont, 13).color = Gold;
            var name = Label("CityName", city, "Burning Hills", new Vector2(0, -49), new Vector2(250, 34), art.headingFont, 25);
            var resources = Rect("Resources", header, new Vector2(682, 74)); resources.pivot = new Vector2(.5f, 1); resources.anchoredPosition = new Vector2(0, -18);
            Counter(resources, art, "Money", "money", "GELD", new Vector2(-232, 0));
            Counter(resources, art, "Grinders", "grinder", "GRINDER", Vector2.zero);
            Counter(resources, art, "Residents", "residents", "EINWOHNER", new Vector2(232, 0));
            var settings = Round(header, art, "Settings", "settings");
            settings.sizeDelta = new Vector2(80, 80); settings.anchorMin = settings.anchorMax = settings.pivot = Vector2.one; settings.anchoredPosition = new Vector2(-24, -18);
            settings.Find("Icon").GetComponent<RectTransform>().sizeDelta = new Vector2(48, 48);
            PrefabUtility.RecordPrefabInstancePropertyModifications(settings);
            PrefabUtility.RecordPrefabInstancePropertyModifications(settings.Find("Icon"));
            header.gameObject.AddComponent<CityStatusBar>().EditorConfigure(city, resources, settings, name);
            PrefabUtility.SaveAsPrefabAsset(header.gameObject, HeaderPath); Object.DestroyImmediate(header.gameObject);
        }

        private static RectTransform Counter(Transform parent, PresentationArt art, string name, string icon, string caption, Vector2 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CounterPath), parent);
            instance.name = name; var rect = instance.GetComponent<RectTransform>(); rect.anchoredPosition = position;
            var image = instance.transform.Find("Icon").GetComponent<Image>(); image.sprite = art.Get(icon);
            var text = instance.transform.Find("Caption").GetComponent<Text>(); text.text = caption;
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect); PrefabUtility.RecordPrefabInstancePropertyModifications(image); PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            return rect;
        }

        private static RectTransform Round(Transform parent, PresentationArt art, string name, string icon)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WorldNavigationAuthoring.ButtonPath), parent);
            instance.name = name;
            var image = instance.GetComponentInChildren<Image>(); image.sprite = art.Get(icon);
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            return instance.GetComponent<RectTransform>();
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = size; return rect;
        }
        private static void Place(Transform target, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)target; rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        private static Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, Font font, int fontSize)
        {
            var rect = Rect(name, parent, size); rect.anchoredPosition = position;
            var label = rect.gameObject.AddComponent<Text>(); label.font = font; label.fontSize = fontSize; label.text = value;
            label.color = Paper; label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false; return label;
        }
        private static void Validate(GameRoot root)
        {
            var header = root.Ui.GetComponentInChildren<CityStatusBar>(true);
            var buttons = root.Ui.GetComponent<WorldNavigationBar>().Dock.GetComponentsInChildren<Button>(true);
            int expected = root.Ui.GetComponent<WorldNavigationBar>().Dock.Find("Shop") != null ? 10 : 9;
            if (header == null || buttons.Length != expected || buttons.Any(x => x.GetComponent<RoundNavigationGraphic>() == null))
                throw new InvalidOperationException("Expected " + expected + " round navigation buttons and the authored city header.");
            if (root.Ui.Interaction.townMenu.activeInHierarchy || root.Ui.Interaction.launcher.gameObject.activeInHierarchy)
                throw new InvalidOperationException("The retired town menu must remain archived.");
            Directory.CreateDirectory("Docs/MenuLayoutRevision");
            File.WriteAllText("Docs/MenuLayoutRevision/authoring_receipt.json", "{\"utc\":\"" + DateTime.UtcNow.ToString("O") + "\",\"roundNavigationButtons\":" + expected + ",\"hiddenMenuRemoved\":true,\"cardsAndSpinInTeam\":true,\"inventoryAndWheelInDock\":true}");
        }
    }
}

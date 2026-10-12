using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit installation only. New prefab roots extend the scene without rebuilding existing objects.</summary>
    public static class VipStoriesAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/VipStories";
        public const string CatalogPath = Folder + "/VipVisuals.asset", PanelPath = Folder + "/VipStories.prefab";
        public const string WorldPath = Folder + "/VipStoryWorld.prefab", ActorPath = Folder + "/VipActor.prefab", RowPath = Folder + "/VipRosterRow.prefab";
        public const string JournalEntryPath = Folder + "/VipJournalEntry.prefab";
        private static PresentationArt art;
        private static VipVisualCatalog visuals;
        private static GameDefinitions defs;
        private static Sprite surface;
        private static readonly Color Gold = new Color(1, .79f, .37f), Paper = new Color(.97f, .94f, .84f), Muted = new Color(.65f, .79f, .75f);
        private static readonly Dictionary<string, Vector2> Locations = new Dictionary<string, Vector2>
        {
            {"toni",new Vector2(50,-2)}, {"juno",new Vector2(5,-33)}, {"rosa",new Vector2(18,-82)},
            {"mira",new Vector2(-38,116)}, {"kalle",new Vector2(77,8)}, {"alma",new Vector2(-38,170)},
            {"selma",new Vector2(-29,14)}, {"oskar",new Vector2(18,-184)}, {"dalia",new Vector2(51,-236)}, {"valentina",new Vector2(27,14)}
        };

        [MenuItem("Weedtopia/Stories/Install or Repair VIP Stories")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var root = Object.FindFirstObjectByType<GameRoot>(); art = root.Presentation.art;
                defs = JsonUtility.FromJson<GameDefinitions>(File.ReadAllText("Assets/Weedtopia/Game/Content/BurningHills.json")); defs.Validate();
                // This installer owns only Burning Hills, even when the shared content includes other cities.
                defs.vipStories=defs.vipStories.Where(p=>p.cityId=="burning_hills").ToArray();
                defs.vipCollaborations=defs.vipCollaborations.Where(p=>p.cityId=="burning_hills").ToArray();
                if (defs.vipStories.Length != 10 || defs.vipStories.Sum(p => p.chapters.Length) != 40 || defs.vipCollaborations.Length != 5)
                    throw new InvalidOperationException("Approved VIP content is missing");
                Directory.CreateDirectory(Folder + "/Prefabs"); AssetDatabase.Refresh();
                surface = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Weedtopia/Production/Rewards/Art/UiSurface.png");
                visuals = CreateVisuals(); CreateActor(); CreateRosterRow(); CreateWorld(); CreatePanel();
                var rootData = new SerializedObject(root); var uiData = new SerializedObject(root.Ui);
                var bindings = uiData.FindProperty("bindings");
                var map = new Dictionary<string, GameObject>();
                for (int i = 0; i < bindings.arraySize; i++)
                {
                    var binding = bindings.GetArrayElementAtIndex(i);
                    map.Add(binding.FindPropertyRelative("id").stringValue, (GameObject)binding.FindPropertyRelative("target").objectReferenceValue);
                }
                bool changed = false;
                if (root.StoryWorld == null)
                {
                    var world = Instance(WorldPath, null);
                    rootData.FindProperty("storyWorld").objectReferenceValue = world.GetComponent<VipTownPresentation>();
                    rootData.ApplyModifiedPropertiesWithoutUndo(); changed = true;
                }
                if (root.Ui.Stories == null)
                {
                    if (map.ContainsKey("vipStories")) throw new InvalidOperationException("VIP panel binding needs manual review");
                    var panel = Instance(PanelPath, map["journal"].transform.parent); panel.SetActive(false);
                    var added = bindings.GetArrayElementAtIndex(bindings.arraySize++);
                    added.FindPropertyRelative("id").stringValue = "vipStories";
                    added.FindPropertyRelative("target").objectReferenceValue = panel;
                    uiData.FindProperty("stories").objectReferenceValue = panel.GetComponent<VipStoriesUi>();
                    var panels = uiData.FindProperty("panels"); panels.GetArrayElementAtIndex(panels.arraySize++).objectReferenceValue = panel;
                    uiData.ApplyModifiedPropertiesWithoutUndo(); changed = true;
                }
                if (!map.ContainsKey("storyJournal"))
                {
                    if (!File.Exists(JournalEntryPath))
                    {
                        var template = (Button)uiData.FindProperty("listRowPrefab").objectReferenceValue;
                        var entryPrefab = Instance(AssetDatabase.GetAssetPath(template), null); entryPrefab.name = "VipStoryJournalEntry";
                        entryPrefab.GetComponentInChildren<Text>(true).text = defs.Text("story.entry");
                        PrefabUtility.SaveAsPrefabAsset(entryPrefab, JournalEntryPath); Object.DestroyImmediate(entryPrefab);
                    }
                    var entry = Instance(JournalEntryPath, map["journalList"].transform); entry.transform.SetAsFirstSibling();
                    var binding = bindings.GetArrayElementAtIndex(bindings.arraySize++);
                    binding.FindPropertyRelative("id").stringValue = "storyJournal";
                    binding.FindPropertyRelative("target").objectReferenceValue = entry;
                    uiData.ApplyModifiedPropertiesWithoutUndo(); changed = true;
                }
                if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); }
                if (root.StoryWorld.actors.Length != 10 || root.Ui.Stories.rows.Length != 15 || root.Ui.Stories.goals.Length != 6)
                    throw new InvalidOperationException("VIP authored references are incomplete");
                Directory.CreateDirectory("Docs/VipStories");
                File.WriteAllText("Docs/VipStories/authoring_receipt.json", "{\"status\":\"passed\",\"changedScene\":" + (changed ? "true" : "false") + ",\"people\":10,\"chapters\":40,\"recurring\":30,\"collaborations\":5,\"rosterRows\":15,\"goalRows\":6}");
            }
            finally { if (setup.Any(s => s.isLoaded) && setup.Any(s => s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static VipVisualCatalog CreateVisuals()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VipVisualCatalog>(CatalogPath);
            if (existing != null) return existing;
            var catalog = ScriptableObject.CreateInstance<VipVisualCatalog>();
            var entries = new List<VipVisualEntry>();
            foreach (var person in defs.vipStories)
            {
                string path = Folder + "/Art/" + person.id + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing VIP artwork " + path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = true; importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp; importer.maxTextureSize = 2048; importer.spritePixelsPerUnit = 256;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
#pragma warning disable 618
                importer.spritesheet = new[] {
                    new SpriteMetaData { name = person.id + "_body", rect = new Rect(0, 0, 1024, 1536), alignment = 9, pivot = new Vector2(.5f, .02f) },
                    new SpriteMetaData { name = person.id + "_portrait", rect = new Rect(100, 690, 824, 800), alignment = 9, pivot = new Vector2(.5f, .5f) }
                };
#pragma warning restore 618
                importer.SaveAndReimport();
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                entries.Add(new VipVisualEntry { id = person.id, body = sprites.Single(s => s.name == person.id + "_body"), portrait = sprites.Single(s => s.name == person.id + "_portrait") });
            }
            catalog.entries = entries.ToArray(); AssetDatabase.CreateAsset(catalog, CatalogPath); return catalog;
        }
        private static void CreateActor()
        {
            if (File.Exists(ActorPath)) return;
            var go = new GameObject("VipActor", typeof(VipStoryActor), typeof(BoxCollider)); var actor = go.GetComponent<VipStoryActor>();
            actor.selectionBox = go.GetComponent<BoxCollider>(); actor.selectionBox.center = new Vector3(0, 2.6f, 0); actor.selectionBox.size = new Vector3(3.6f, 5.8f, 3.6f); actor.selectionBox.enabled = false;
            actor.visibleRoot = new GameObject("VisibleInhabitant"); actor.visibleRoot.transform.SetParent(go.transform, false);
            actor.spriteRenderer = ProductionWorldAuthoring.Sprite("Artwork", actor.visibleRoot.transform, visuals.Get("toni").body, 5.6f, Vector3.zero); actor.artwork = actor.spriteRenderer.transform;
            actor.marker = ProductionWorldAuthoring.Sprite("QuestMarker", actor.artwork, art.Get("quest"), 1.15f, new Vector3(0, 6.6f, 0));
            actor.visibleRoot.SetActive(false); PrefabUtility.SaveAsPrefabAsset(go, ActorPath); Object.DestroyImmediate(go);
        }
        private static void CreateWorld()
        {
            if (File.Exists(WorldPath)) return;
            var world = new GameObject("VIP_StoryWorld", typeof(VipTownPresentation)); var view = world.GetComponent<VipTownPresentation>();
            var stages = new[] {
                new[]{"Prop_crates","Prop_sign","Prop_crates","Town_flower_bicycle"},
                new[]{"Prop_sign","Prop_lantern","Prop_sign","Town_reading_corner"},
                new[]{"Town_tea_patio","Prop_crates","Town_tea_patio","Town_picnic_nook"},
                new[]{"Town_herb_garden","Prop_sign","Town_reading_corner","Town_fern_garden"},
                new[]{"Prop_salvage","Prop_crates","Town_workshop_corner","Town_flower_bicycle"},
                new[]{"Town_fern_garden","Prop_planter","Town_reading_corner","Town_picnic_nook"},
                new[]{"Prop_sign","Prop_crates","Town_reading_corner","Prop_sign"},
                new[]{"Prop_sign","Prop_crates","Town_reading_corner","Town_reading_corner"},
                new[]{"Prop_lantern","Prop_planter","Prop_sign","Prop_lantern"},
                new[]{"Prop_sign","Town_reading_corner","Prop_crates","Town_tea_patio"}
            };
            var actors = new List<VipStoryActor>();
            for (int i = 0; i < defs.vipStories.Length; i++)
            {
                var person = defs.vipStories[i]; string path = Folder + "/Prefabs/Vip_" + person.id + ".prefab";
                if (!File.Exists(path))
                {
                    var obj = Instance(ActorPath, null); obj.name = "VIP_" + person.id; var actor = obj.GetComponent<VipStoryActor>();
                    actor.vipId = person.id; actor.spriteRenderer.sprite = visuals.Get(person.id).body;
                    var memories = new List<VipMemoryVisual>();
                    for (int chapter = 0; chapter < person.chapters.Length; chapter++)
                    {
                        var choices = person.chapters[chapter].choices;
                        for (int choice = 0; choice < choices.Length; choice++)
                        {
                            string propName = choice == 0 ? stages[i][chapter] : stages[i][chapter].Contains("lantern") ? "Prop_flowers" : "Town_reading_corner";
                            string propPath = propName.StartsWith("Prop_") ? "Assets/Weedtopia/Production/Prefabs/" : "Assets/Weedtopia/Production/TownVitality/Prefabs/";
                            var memory = Instance(propPath + propName + ".prefab", actor.visibleRoot.transform);
                            memory.name = "Memory_" + (chapter + 1) + "_" + choices[choice].id;
                            memory.transform.localPosition = DistrictMap.ToWorld(new Vector2(6 + chapter % 2 * 6, -5 - chapter / 2 * 6));
                            memory.transform.localScale *= propName.StartsWith("Prop_") ? .85f : .38f;
                            memory.SetActive(false);
                            memories.Add(new VipMemoryVisual { chapter = chapter, choiceId = choices.Length > 1 ? choices[choice].id : "", target = memory });
                        }
                    }
                    actor.memories = memories.ToArray(); PrefabUtility.SaveAsPrefabAsset(obj, path); Object.DestroyImmediate(obj);
                }
                var instance = Instance(path, world.transform); instance.transform.position = DistrictMap.ToWorld(Locations[person.id]); actors.Add(instance.GetComponent<VipStoryActor>());
            }
            view.actors = actors.ToArray(); PrefabUtility.SaveAsPrefabAsset(world, WorldPath); Object.DestroyImmediate(world);
        }
        private static void CreateRosterRow()
        {
            if (File.Exists(RowPath)) return;
            var rect = Rect("VipRosterRow", null, new Vector2(360, 104)); var row = rect.gameObject.AddComponent<VipStoryRosterRow>();
            var bg = rect.gameObject.AddComponent<Image>(); bg.sprite = surface; bg.color = new Color(.06f, .16f, .16f);
            row.button = rect.gameObject.AddComponent<Button>(); row.button.targetGraphic = bg;
            row.accent = Image("Accent", rect, surface, new Vector2(-177, 0), new Vector2(3, 104), Gold);
            var viewport = Rect("PortraitMask", rect, new Vector2(76, 90)); viewport.anchoredPosition = new Vector2(-132, 0); viewport.gameObject.AddComponent<RectMask2D>();
            row.portrait = Image("Portrait", viewport, visuals.Get("toni").portrait, Vector2.zero, new Vector2(76, 90));
            row.caption = Label("Name", rect, "", new Vector2(43, 17), new Vector2(242, 50), 18, Paper); row.caption.fontStyle = FontStyle.Bold;
            row.status = Label("Status", rect, "", new Vector2(43, -30), new Vector2(242, 30), 14, Muted);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 104;
            PrefabUtility.SaveAsPrefabAsset(rect.gameObject, RowPath); Object.DestroyImmediate(rect.gameObject);
        }
        private static void CreatePanel()
        {
            if (File.Exists(PanelPath)) return;
            var rect = Rect("VipStories", null, new Vector2(1400, 950)); var view = rect.gameObject.AddComponent<VipStoriesUi>(); view.visuals = visuals;
            var bg = rect.gameObject.AddComponent<Image>(); bg.sprite = surface; bg.color = new Color(.025f, .077f, .086f, .995f);
            Image("TopRule", rect, surface, new Vector2(0, 473), new Vector2(1400, 3), Gold);
            view.heading = Label("Heading", rect, "", new Vector2(-180, 419), new Vector2(990, 64), 42, Gold, true);
            view.subtitle = Label("Subtitle", rect, "", new Vector2(215, 414), new Vector2(760, 30), 18, Muted); view.subtitle.gameObject.SetActive(false);
            view.milestone = Label("Milestones", rect, "", new Vector2(60, 365), new Vector2(1120, 34), 17, Muted);
            view.peopleTab = Button("PeopleTab", rect, "", new Vector2(-570, 310), new Vector2(195, 50));
            view.togetherTab = Button("TogetherTab", rect, "", new Vector2(-358, 310), new Vector2(200, 50));
            view.rosterScroll = Scroll("Roster", rect, new Vector2(-470, -55), new Vector2(390, 650));
            var rosterRows = new List<VipStoryRosterRow>();
            foreach (var person in defs.vipStories)
            { var row = Instance(RowPath, view.rosterScroll.content).GetComponent<VipStoryRosterRow>(); row.id = person.id; rosterRows.Add(row); }
            foreach (var joint in defs.vipCollaborations)
            { var row = Instance(RowPath, view.rosterScroll.content).GetComponent<VipStoryRosterRow>(); row.id = joint.id; row.collaboration = true; row.gameObject.SetActive(false); rosterRows.Add(row); }
            view.rows = rosterRows.ToArray();
            view.nameLabel = Label("SelectedName", rect, "", new Vector2(210, 302), new Vector2(830, 60), 31, Gold, true);
            view.role = Label("SelectedRole", rect, "", new Vector2(210, 255), new Vector2(830, 48), 18, Muted);
            view.portrait = Image("SelectedPortrait", rect, visuals.Get("toni").body, new Vector2(540, 130), new Vector2(185, 218));
            view.biography = Label("Biography", rect, "", new Vector2(100, 160), new Vector2(560, 133), 18, Paper);
            view.greeting = Label("Greeting", rect, "", new Vector2(100, 61), new Vector2(560, 58), 18, Gold); view.greeting.fontStyle = FontStyle.Italic;
            view.detailScroll = Scroll("QuestDetails", rect, new Vector2(210, -154), new Vector2(860, 354));
            view.questTitle = FlowLabel("QuestTitle", view.detailScroll.content, 27, Gold, true);
            view.description = FlowLabel("Invitation", view.detailScroll.content, 19, Paper);
            view.goals = new Button[6]; view.goalLabels = new Text[6];
            for (int i = 0; i < 6; i++)
            {
                var button = Button("Goal_" + i, view.detailScroll.content, "", Vector2.zero, new Vector2(815, 50));
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 52; view.goals[i] = button; view.goalLabels[i] = button.GetComponentInChildren<Text>();
                var colors = button.colors; colors.disabledColor = new Color(.68f, .8f, .75f, 1); button.colors = colors;
            }
            view.reward = FlowLabel("Rewards", view.detailScroll.content, 18, Gold);
            view.memories = FlowLabel("Memories", view.detailScroll.content, 18, Muted);
            view.accept = Button("Accept", rect, "", new Vector2(10, -367), new Vector2(390, 54)); view.acceptLabel = view.accept.GetComponentInChildren<Text>();
            view.alternate = Button("Alternative", rect, "", new Vector2(430, -367), new Vector2(390, 54)); view.alternateLabel = view.alternate.GetComponentInChildren<Text>();
            view.claim = Button("Claim", rect, "", new Vector2(210, -367), new Vector2(430, 54));
            view.speak = Button("Speak", rect, "", new Vector2(-65, -425), new Vector2(240, 44));
            view.focus = Button("Focus", rect, "", new Vector2(213, -425), new Vector2(275, 44));
            view.remember = Button("Remember", rect, "", new Vector2(500, -425), new Vector2(250, 44));
            view.hint = Label("Hint", rect, "", new Vector2(-470, -421), new Vector2(390, 72), 15, Muted);
            PrefabUtility.SaveAsPrefabAsset(rect.gameObject, PanelPath); Object.DestroyImmediate(rect.gameObject);
        }
        private static GameObject Instance(string path, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing existing prefab " + path);
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        { var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.sizeDelta = size; return r; }
        private static Image Image(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, Color? color = null)
        { var r = Rect(name, parent, size); r.anchoredPosition = pos; var image = r.gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color ?? Color.white; image.preserveAspect = sprite != surface; image.raycastTarget = false; return image; }
        private static Text Label(string name, Transform parent, string text, Vector2 pos, Vector2 size, int font, Color color, bool heading = false)
        { var r = Rect(name, parent, size); r.anchoredPosition = pos; var label = r.gameObject.AddComponent<Text>(); label.font = heading ? art.headingFont : art.bodyFont; label.fontSize = font; label.color = color; label.text = text; label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false; label.supportRichText = false; return label; }
        private static Text FlowLabel(string name, Transform parent, int font, Color color, bool heading = false)
        { var text = Label(name, parent, "", Vector2.zero, new Vector2(820, 40), font, color, heading); text.alignment = TextAnchor.UpperLeft; text.gameObject.AddComponent<VipStoryParagraph>().label = text; return text; }

        [MenuItem("Weedtopia/Stories/Refine VIP Presentation")]
        public static void RefinePresentation()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var common = PrefabUtility.LoadPrefabContents(ActorPath);
            try
            {
                var actor = common.GetComponent<VipStoryActor>();
                if (actor.marker.transform.parent != actor.artwork)
                {
                    actor.marker.transform.SetParent(actor.artwork, false); actor.marker.transform.localPosition = new Vector3(0,6.6f,0);
                    actor.marker.transform.localRotation = Quaternion.identity;
                    PrefabUtility.SaveAsPrefabAsset(common, ActorPath);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(common); }
            var panel = PrefabUtility.LoadPrefabContents(PanelPath);
            try
            {
                var view = panel.GetComponent<VipStoriesUi>(); bool changed = false;
                foreach (var label in new[] { view.questTitle, view.description, view.reward, view.memories })
                    if (label.GetComponent<VipStoryParagraph>() == null) { label.gameObject.AddComponent<VipStoryParagraph>().label = label; changed = true; }
                if (changed) PrefabUtility.SaveAsPrefabAsset(panel, PanelPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(panel); }
            var world = PrefabUtility.LoadPrefabContents(WorldPath);
            try
            {
                bool changed = false;
                foreach (var actor in world.GetComponent<VipTownPresentation>().actors)
                {
                    var position = DistrictMap.ToWorld(Locations[actor.vipId]);
                    if (actor.transform.localPosition == position) continue;
                    actor.transform.localPosition = position; changed = true;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(world, WorldPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(world); }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Docs/VipStories/refinement_receipt.json", "{\"status\":\"passed\",\"scope\":\"VIP panel paragraphs, common quest marker and VIP positions only\"}");
        }
        private static Button Button(string name, Transform parent, string text, Vector2 pos, Vector2 size)
        {
            var go = Instance(RewardPresentationAuthoring.ButtonPath, parent); go.name = name; var rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = pos; rect.sizeDelta = size;
            var label = go.GetComponentInChildren<Text>(); label.rectTransform.sizeDelta = new Vector2(size.x - 20, size.y - 4); label.text = text; label.fontSize = 18; return go.GetComponent<Button>();
        }
        private static ScrollRect Scroll(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var rect = Rect(name, parent, size); rect.anchoredPosition = pos; var scroll = rect.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", rect, size); var hit = viewport.gameObject.AddComponent<Image>(); hit.sprite = surface; hit.color = new Color(.04f, .12f, .13f); viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport, new Vector2(size.x - 20, 0)); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(-20, 0);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 10; layout.padding = new RectOffset(10, 10, 10, 10); layout.childControlHeight = layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }
    }
}

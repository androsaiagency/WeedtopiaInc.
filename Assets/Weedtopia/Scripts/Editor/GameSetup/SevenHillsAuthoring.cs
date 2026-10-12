using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Weedtopia.Buildings.Presentation;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>
    /// Explicit additive city authoring. The current second-city scene supplies shared shop, localization,
    /// mobile controls and world contracts. Earlier city installers are deliberately never invoked here.
    /// </summary>
    public static class SevenHillsAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/SevenHills";
        public const string ScenePath = "Assets/Weedtopia/Game/Scenes/SevenHills.unity";
        public const string CatalogPath = Folder + "/BuildingVisuals.asset";
        public const string StoriesPath = Folder + "/ResidentStories.prefab";
        public const string PeoplePath = Folder + "/Residents.prefab";
        public const string VisualsPath = Folder + "/ResidentVisuals.asset";
        public const string LandmarksPath = Folder + "/Prefabs/SevenTerraces.prefab";
        public const string MapPath = Folder + "/DistrictMap.asset";
        private static GameDefinitions definitions;
        private static readonly Dictionary<string, Vector2> Places = new Dictionary<string, Vector2>
        {
            {"sh_mila", new Vector2(-38,116)}, {"sh_tariq", new Vector2(-29,14)},
            {"sh_juna", new Vector2(18,-82)}, {"sh_emil", new Vector2(14,182)},
            {"sh_safiya", new Vector2(5,-33)}, {"sh_clara", new Vector2(27,14)},
            {"sh_anton", new Vector2(-38,300)}
        };

        [MenuItem("Weedtopia/Cities/Install or Repair Seven Hills")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string sub in new[] { "", "/Materials", "/Buildings", "/Prefabs" }) Directory.CreateDirectory(Folder + sub);
                AssetDatabase.Refresh();
                definitions = AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
                if (definitions.cities.All(c => c.id != "seven_hills")) throw new InvalidOperationException("Seven Hills content must be installed before scene authoring.");
                var sourceScene = EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var source = FindRoot(sourceScene);
                var sourceCatalog = (BuildingVisualCatalog)new SerializedObject(source).FindProperty("productionCatalog").objectReferenceValue;
                Buildings(sourceCatalog);
                var visuals = Residents();
                ResidentPanel(visuals); ResidentWorld(visuals); Landmarks(); Districts(); ImportSingle(Folder + "/Art/town_sign.png", .06f);
                UpgradeCampaignPrefab();
                var changedExisting = new List<string>();
                foreach (string path in new[] { GameScaffold.GameScene, BigRockCityAuthoring.ScenePath })
                {
                    var scene = EditorSceneManager.OpenScene(path);
                    if (CampaignRows(FindRoot(scene).Ui.Campaign))
                    {
                        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); changedExisting.Add(path);
                    }
                }
                bool created = !File.Exists(ScenePath);
                if (created && !AssetDatabase.CopyAsset(BigRockCityAuthoring.ScenePath, ScenePath))
                    throw new InvalidOperationException("Could not copy current authored Big Rock City scene.");
                var cityScene = EditorSceneManager.OpenScene(ScenePath);
                var root = FindRoot(cityScene);
                bool changed = false;
                if (created)
                {
                    RemapCity(cityScene);
                    var data = new SerializedObject(root);
                    data.FindProperty("sceneCityId").stringValue = "seven_hills";
                    data.FindProperty("productionCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(CatalogPath);
                    var previousWorld = root.StoryWorld.gameObject;
                    var world = Instance(PeoplePath, null).GetComponent<VipTownPresentation>();
                    data.FindProperty("storyWorld").objectReferenceValue = world;
                    data.ApplyModifiedPropertiesWithoutUndo(); Object.DestroyImmediate(previousWorld);
                    ReplaceResidentPanel(root);
                    foreach (var quarry in cityScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BigRockCityStamp>(true)).ToArray())
                        Object.DestroyImmediate(quarry.gameObject);
                    ThemeLandscape(cityScene, root);
                    changed = true;
                }
                // Repair only missing Seven Hills-owned assets/references. Authored/manual objects retain identity.
                var rootData = new SerializedObject(root);
                if (rootData.FindProperty("productionCatalog").objectReferenceValue == null)
                {
                    rootData.FindProperty("productionCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(CatalogPath);
                    rootData.ApplyModifiedPropertiesWithoutUndo(); changed = true;
                }
                if (root.StoryWorld == null)
                {
                    rootData.FindProperty("storyWorld").objectReferenceValue = Instance(PeoplePath, null).GetComponent<VipTownPresentation>();
                    rootData.ApplyModifiedPropertiesWithoutUndo(); changed = true;
                }
                changed |= RepairEmilPlacement(root.StoryWorld);
                if (root.Ui.Stories == null) throw new InvalidOperationException("Missing story UI; refusing a broad UI rebuild.");
                if (root.Ui.Stories.visuals == null) { root.Ui.Stories.visuals = AssetDatabase.LoadAssetAtPath<VipVisualCatalog>(VisualsPath); changed = true; }
                if (!cityScene.GetRootGameObjects().Any(g => g.GetComponentInChildren<SevenHillsStamp>(true) != null))
                { Instance(LandmarksPath, null); changed = true; }
                changed |= MapAndLabels(root, cityScene);
                foreach (var place in cityScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SevenHillsPlaceVisual>(true)))
                    if (place.root != root) { place.root = root; PrefabUtility.RecordPrefabInstancePropertyModifications(place); changed = true; }
                changed |= WelcomeSigns(cityScene);
                changed |= CampaignRows(root.Ui.Campaign);
                if (changed) { EditorSceneManager.MarkSceneDirty(cityScene); EditorSceneManager.SaveScene(cityScene); }
                var scenes = EditorBuildSettings.scenes.ToList();
                if (!scenes.Any(s => s.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
                else if (scenes.Any(s => s.path == ScenePath && !s.enabled)) { scenes.Single(s => s.path == ScenePath).enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); }
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Docs/SevenHills");
                File.WriteAllText("Docs/SevenHills/authoring_receipt.json", JsonUtility.ToJson(new Receipt
                {
                    utc = DateTime.UtcNow.ToString("O"), createdScene = created, changedExistingCityScenes = changedExisting.ToArray(),
                    buildings = definitions.instances.Count(i => i.cityId == "seven_hills"),
                    routes = definitions.routes.Count(r => definitions.instances.Single(i => i.id == r.from).cityId == "seven_hills"),
                    mainQuests = definitions.QuestsFor("seven_hills").Length
                }, true));
            }
            finally
            {
                if (setup.Any(s => s.isLoaded) && setup.Count(s => s.isActive) == 1) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        [Serializable] private sealed class Receipt
        {
            public string utc, status = "passed";
            public bool createdScene, runtimeStaticObjects;
            public string[] changedExistingCityScenes;
            public int buildings, routes, mainQuests, buildingVariants = 251, hills = 7, residents = 7, chapters = 28, recurring = 21, sharedProjects = 3;
        }
        private static GameRoot FindRoot(Scene scene) => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameRoot>(true)).Single();
        private static BuildingVisualCatalog Buildings(BuildingVisualCatalog source)
        {
            var existing = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(CatalogPath);
            if (existing != null) return existing;
            var shader = Shader.Find("Weedtopia/Seven Hills/Building Palette");
            if (shader == null) throw new InvalidOperationException("Missing Seven Hills building shader.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/GreenPlaster.mat");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, Folder + "/Materials/GreenPlaster.mat"); }
            var families = new List<BuildingVisualFamily>();
            foreach (var family in source.Families)
            {
                Directory.CreateDirectory(Folder + "/Buildings/" + family.BuildingId); AssetDatabase.Refresh();
                var bands = new List<BuildingVisualBand>();
                foreach (var band in family.Bands)
                {
                    string path = Folder + "/Buildings/" + family.BuildingId + "/" + band.TierId + ".prefab";
                    if (!File.Exists(path))
                    {
                        if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(band.WrapperPrefab), path)) throw new InvalidOperationException("Building wrapper copy failed: " + path);
                        var copy = PrefabUtility.LoadPrefabContents(path);
                        try
                        {
                            foreach (var sprite in copy.GetComponentsInChildren<BuildingSpriteVisual>(true)) sprite.SpriteRenderer.sharedMaterial = material;
                            PrefabUtility.SaveAsPrefabAsset(copy, path);
                        }
                        finally { PrefabUtility.UnloadPrefabContents(copy); }
                    }
                    bands.Add(new BuildingVisualBand(band.TierId, band.MinLevel, band.MaxLevel, AssetDatabase.LoadAssetAtPath<GameObject>(path)));
                }
                families.Add(new BuildingVisualFamily(family.BuildingId, bands));
            }
            var catalog = ScriptableObject.CreateInstance<BuildingVisualCatalog>(); catalog.EditorSetFamilies(families); AssetDatabase.CreateAsset(catalog, CatalogPath); return catalog;
        }
        private static VipVisualCatalog Residents()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VipVisualCatalog>(VisualsPath); if (existing != null) return existing;
            var entries = new List<VipVisualEntry>();
            foreach (var person in definitions.vipStories.Where(p => p.cityId == "seven_hills"))
            {
                string path = Folder + "/Art/" + person.id + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing Seven Hills resident artwork " + path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple; importer.spritePixelsPerUnit = 256;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = true; importer.maxTextureSize = 2048; importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
#pragma warning disable 618
                importer.spritesheet = new[]
                {
                    new SpriteMetaData { name = person.id + "_body", rect = new Rect(0, 0, 1024, 1536), alignment = 9, pivot = new Vector2(.5f, .02f) },
                    new SpriteMetaData { name = person.id + "_portrait", rect = new Rect(100, 690, 824, 800), alignment = 9, pivot = new Vector2(.5f, .5f) }
                };
#pragma warning restore 618
                importer.SaveAndReimport();
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                entries.Add(new VipVisualEntry { id = person.id, body = sprites.Single(s => s.name == person.id + "_body"), portrait = sprites.Single(s => s.name == person.id + "_portrait") });
            }
            var visuals = ScriptableObject.CreateInstance<VipVisualCatalog>(); visuals.entries = entries.ToArray(); AssetDatabase.CreateAsset(visuals, VisualsPath); return visuals;
        }
        private static void ResidentPanel(VipVisualCatalog visuals)
        {
            if (File.Exists(StoriesPath)) return;
            var panel = PrefabUtility.LoadPrefabContents(BigRockCityAuthoring.StoriesPath);
            try
            {
                var ui = panel.GetComponent<VipStoriesUi>(); ui.visuals = visuals;
                foreach (var row in ui.rows) Object.DestroyImmediate(row.gameObject);
                var rows = new List<VipStoryRosterRow>();
                foreach (var person in definitions.vipStories.Where(p => p.cityId == "seven_hills"))
                {
                    var row = Instance(VipStoriesAuthoring.RowPath, ui.rosterScroll.content).GetComponent<VipStoryRosterRow>();
                    row.id = person.id; row.portrait.sprite = visuals.Get(person.id).portrait; rows.Add(row);
                }
                foreach (var collaboration in definitions.vipCollaborations.Where(p => p.cityId == "seven_hills"))
                {
                    var row = Instance(VipStoriesAuthoring.RowPath, ui.rosterScroll.content).GetComponent<VipStoryRosterRow>();
                    row.id = collaboration.id; row.collaboration = true; row.gameObject.SetActive(false); rows.Add(row);
                }
                ui.rows = rows.ToArray(); ui.portrait.sprite = visuals.entries[0].body;
                panel.name = "SevenHillsResidentStories"; PrefabUtility.SaveAsPrefabAsset(panel, StoriesPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(panel); }
        }
        private static void ResidentWorld(VipVisualCatalog visuals)
        {
            if (File.Exists(PeoplePath))
            {
                var existing = PrefabUtility.LoadPrefabContents(PeoplePath);
                try
                {
                    if (RepairEmilPlacement(existing.GetComponent<VipTownPresentation>()))
                        PrefabUtility.SaveAsPrefabAsset(existing, PeoplePath);
                }
                finally { PrefabUtility.UnloadPrefabContents(existing); }
                return;
            }
            var world = new GameObject("SevenHillsResidents", typeof(VipTownPresentation)); var actors = new List<VipStoryActor>();
            foreach (var person in definitions.vipStories.Where(p => p.cityId == "seven_hills"))
            {
                string path = Folder + "/Prefabs/" + person.id + ".prefab";
                if (!File.Exists(path))
                {
                    var actorObject = PrefabUtility.LoadPrefabContents(VipStoriesAuthoring.ActorPath);
                    try
                    {
                        var actor = actorObject.GetComponent<VipStoryActor>(); actor.vipId = person.id; actorObject.name = person.id;
                        actor.spriteRenderer.sprite = visuals.Get(person.id).body;
                        actor.artwork.localScale = Vector3.one * (5.6f / actor.spriteRenderer.sprite.bounds.size.y);
                        var memories = new List<VipMemoryVisual>();
                        for (int chapter = 0; chapter < person.chapters.Length; chapter++)
                            for (int choice = 0; choice < person.chapters[chapter].choices.Length; choice++)
                            {
                                string prop = MemoryProp(person.id, chapter, choice);
                                var memory = Instance(prop, actor.visibleRoot.transform);
                                memory.name = "Memory_" + (chapter + 1) + "_" + person.chapters[chapter].choices[choice].id;
                                memory.transform.localPosition = DistrictMap.ToWorld(new Vector2(6 + chapter % 2 * 6, -5 - chapter / 2 * 6));
                                memory.transform.localScale *= prop.Contains("Town_") ? .38f : .85f; memory.SetActive(false);
                                memories.Add(new VipMemoryVisual { chapter = chapter, choiceId = person.chapters[chapter].choices.Length > 1 ? person.chapters[chapter].choices[choice].id : "", target = memory });
                            }
                        actor.memories = memories.ToArray(); PrefabUtility.SaveAsPrefabAsset(actorObject, path);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(actorObject); }
                }
                var instance = Instance(path, world.transform); instance.transform.localPosition = DistrictMap.ToWorld(Places[person.id]); actors.Add(instance.GetComponent<VipStoryActor>());
            }
            world.GetComponent<VipTownPresentation>().actors = actors.ToArray(); PrefabUtility.SaveAsPrefabAsset(world, PeoplePath); Object.DestroyImmediate(world);
        }
        private static bool RepairEmilPlacement(VipTownPresentation world)
        {
            var actor = world?.Actor("sh_emil");
            if (actor == null) throw new InvalidOperationException("Seven Hills resident presentation is missing Emil.");
            // The residential lane at plan y=182 lies below the third row of houses (y=200).
            // Keep his feet and authored memories on this open ground instead of the house canopy.
            var position = DistrictMap.ToWorld(Places["sh_emil"]);
            if ((actor.transform.localPosition - position).sqrMagnitude < .0001f) return false;
            actor.transform.localPosition = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
            return true;
        }
        private static string MemoryProp(string person, int chapter, int choice)
        {
            if (person == "sh_mila" || person == "sh_safiya" && choice == 0)
                return "Assets/Weedtopia/Production/TownVitality/Prefabs/Town_reading_corner.prefab";
            if (person == "sh_tariq" || person == "sh_juna" && chapter == 0)
                return "Assets/Weedtopia/Production/Prefabs/Prop_crates.prefab";
            return chapter % 2 == 0 ? "Assets/Weedtopia/Production/Prefabs/Prop_flowers.prefab" : "Assets/Weedtopia/Production/Prefabs/Prop_lantern.prefab";
        }
        private static void Landmarks()
        {
            if (File.Exists(LandmarksPath)) return;
            ImportSingle(Folder + "/Art/terrace_landmark.png", .04f);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Art/terrace_landmark.png");
            var world = new GameObject("SevenTerraces", typeof(SevenHillsStamp));
            var positions = new[] { new Vector2(315, -12), new Vector2(484, -12), new Vector2(110, -76), new Vector2(110, -184), new Vector2(-40, 188), new Vector2(110, 324), new Vector2(-204, 70) };
            var names = world.GetComponent<SevenHillsStamp>().hills;
            for (int i = 0; i < positions.Length; i++)
            {
                var hill = new GameObject("Hill_" + (i + 1) + "_" + names[i], typeof(SevenHillsPlaceVisual)); hill.transform.SetParent(world.transform, false);
                hill.transform.localPosition = DistrictMap.ToWorld(positions[i]);
                var renderer = ProductionWorldAuthoring.Sprite("Terrace", hill.transform, sprite, 24, Vector3.zero);
                renderer.gameObject.AddComponent<CityLandmarkVisual>();
                renderer.color = i % 3 == 0 ? new Color(.93f, 1, .97f) : i % 3 == 1 ? new Color(.94f, .92f, 1) : Color.white;
                var flowers = Instance("Assets/Weedtopia/Production/Prefabs/Prop_flowers.prefab", hill.transform);
                flowers.name = "VioletTerraceGarden"; flowers.transform.localPosition = DistrictMap.ToWorld(new Vector2(-9, -4));
                foreach (var leaf in flowers.GetComponentsInChildren<SpriteRenderer>(true)) leaf.color = new Color(.83f, .69f, 1);
                var place = hill.GetComponent<SevenHillsPlaceVisual>();
                place.residents = i < 2 ? 250 : i < 4 ? 750 : 1500;
                place.collaborationId = i < 2 ? "sh_joint_morning_courtyard" : i < 4 ? "sh_joint_connection_terrace" : "sh_joint_panorama_ring";
                place.growingPlace = flowers; flowers.SetActive(false);
                var closed = Instance("Assets/Weedtopia/Production/Prefabs/Prop_salvage.prefab", hill.transform);
                closed.name = "AbandonedSevenHeights"; closed.transform.localPosition = DistrictMap.ToWorld(new Vector2(8, -4)); place.closedPlace = closed;
                var finished = Instance("Assets/Weedtopia/Production/TownVitality/Prefabs/" + (i < 2 ? "Town_reading_corner" : i < 4 ? "Town_produce_stall" : "Town_tea_patio") + ".prefab", hill.transform);
                finished.name = "RestoredNeighbourhood"; finished.transform.localPosition = DistrictMap.ToWorld(new Vector2(8, -4)); finished.transform.localScale *= .45f;
                place.completedPlace = finished; finished.SetActive(false);
                if (i == 2 || i == 3)
                {
                    var quiet = Instance("Assets/Weedtopia/Production/TownVitality/Prefabs/Town_reading_corner.prefab", hill.transform);
                    quiet.name = "QuietPublicTerrace"; quiet.transform.localPosition = DistrictMap.ToWorld(new Vector2(0, -10)); quiet.transform.localScale *= .4f; quiet.SetActive(false); place.quietPlace = quiet;
                    var lively = Instance("Assets/Weedtopia/Production/TownVitality/Prefabs/Town_flower_bicycle.prefab", hill.transform);
                    lively.name = "LivelyPublicTerrace"; lively.transform.localPosition = DistrictMap.ToWorld(new Vector2(0, -10)); lively.transform.localScale *= .4f; lively.SetActive(false); place.livelyPlace = lively;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(world, LandmarksPath); Object.DestroyImmediate(world);
        }
        private static void Districts()
        {
            if (File.Exists(MapPath)) return;
            if (!AssetDatabase.CopyAsset(BigRockCityAuthoring.Folder + "/DistrictMap.asset", MapPath)) throw new InvalidOperationException("Could not copy district map.");
            var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(MapPath);
            foreach (var range in map.districts)
            {
                string suffix = range.zone == Zone.Farm ? "farm" : range.zone == Zone.City ? "city" : range.zone == Zone.Settlement ? "settlement" : "industry";
                range.labelKey = "district.seven_hills." + suffix;
                range.label = range.zone == Zone.Farm ? "Terrassengärten · Farm" : range.zone == Zone.City ? "Altstadtbogen · Stadt" : range.zone == Zone.Settlement ? "Panoramaquartier · Wohnviertel" : "Werkhof · Industrie";
            }
            EditorUtility.SetDirty(map);
        }
        private static bool MapAndLabels(GameRoot root, Scene scene)
        {
            bool changed = false;
            var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(MapPath); var cameraData = new SerializedObject(root.CameraRig);
            if (cameraData.FindProperty("districtMap").objectReferenceValue != map)
            { cameraData.FindProperty("districtMap").objectReferenceValue = map; cameraData.ApplyModifiedPropertiesWithoutUndo(); changed = true; }
            foreach (var navigator in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DistrictNavigator>(true)))
            {
                var data = new SerializedObject(navigator); var label = data.FindProperty("cityLabel");
                if (label.stringValue == "Seven Hills") continue; label.stringValue = "Seven Hills"; data.ApplyModifiedPropertiesWithoutUndo(); changed = true;
            }
            return changed;
        }
        private static void UpgradeCampaignPrefab()
        {
            var panel = PrefabUtility.LoadPrefabContents(BigRockCityAuthoring.CampaignPath);
            try { if (CampaignRows(panel.GetComponent<CityCampaignUi>())) PrefabUtility.SaveAsPrefabAsset(panel, BigRockCityAuthoring.CampaignPath); }
            finally { PrefabUtility.UnloadPrefabContents(panel); }
        }
        private static bool CampaignRows(CityCampaignUi ui)
        {
            if (ui == null) throw new InvalidOperationException("Existing city campaign UI is missing; refusing a broad UI rebuild.");
            bool changed = false;
            var progress = ui.transform.GetComponentsInChildren<Text>(true).SingleOrDefault(t => t.name == "SevenProgress");
            var travel = ui.transform.GetComponentsInChildren<Button>(true).SingleOrDefault(t => t.name == "TravelSeven");
            if (progress == null)
            {
                var copy = Object.Instantiate(ui.rockProgress.gameObject, ui.rockProgress.transform.parent); copy.name = "SevenProgress";
                progress = copy.GetComponent<Text>(); progress.text = ""; changed = true;
            }
            if (travel == null)
            {
                var copy = Object.Instantiate(ui.rockTravel.gameObject, ui.rockTravel.transform.parent); copy.name = "TravelSeven";
                travel = copy.GetComponent<Button>(); travel.onClick = new Button.ButtonClickedEvent();
                travel.GetComponentInChildren<Text>(true).text = ""; changed = true;
            }
            var labels = new[] { ui.burningProgress, ui.rockProgress, progress }; var buttons = new[] { ui.burningTravel, ui.rockTravel, travel };
            var ids = new[] { "burning_hills", "big_rock_city", "seven_hills" }; var rows = new List<CityTravelRow>();
            for (int i = 0; i < ids.Length; i++)
            {
                var row = labels[i].GetComponent<CityTravelRow>();
                if (row == null) { row = labels[i].gameObject.AddComponent<CityTravelRow>(); changed = true; }
                if (row.cityId != ids[i] || row.progress != labels[i] || row.travel != buttons[i])
                { row.cityId = ids[i]; row.progress = labels[i]; row.travel = buttons[i]; changed = true; }
                rows.Add(row);
            }
            // A repeat third-city install retains authored destinations introduced by later city installers.
            foreach (var row in ui.travelRows ?? Array.Empty<CityTravelRow>())
                if (row != null && row.progress != null && row.travel != null && !string.IsNullOrEmpty(row.cityId) &&
                    rows.All(existing => existing.cityId != row.cityId)) rows.Add(row);
            if (ui.travelRows == null || !ui.travelRows.SequenceEqual(rows)) { ui.travelRows = rows.ToArray(); changed = true; }
            if (changed)
            {
                foreach (var row in rows) { EditorUtility.SetDirty(row); PrefabUtility.RecordPrefabInstancePropertyModifications(row); }
                EditorUtility.SetDirty(ui); PrefabUtility.RecordPrefabInstancePropertyModifications(ui);
            }
            return changed;
        }
        private static void ReplaceResidentPanel(GameRoot root)
        {
            var previous = root.Ui.Stories.gameObject; var panel = Instance(StoriesPath, previous.transform.parent).GetComponent<VipStoriesUi>(); panel.gameObject.SetActive(false);
            var data = new SerializedObject(root.Ui); data.FindProperty("stories").objectReferenceValue = panel;
            var panels = data.FindProperty("panels"); for (int i = 0; i < panels.arraySize; i++) if (panels.GetArrayElementAtIndex(i).objectReferenceValue == previous) panels.GetArrayElementAtIndex(i).objectReferenceValue = panel.gameObject;
            var bindings = data.FindProperty("bindings"); for (int i = 0; i < bindings.arraySize; i++) { var e = bindings.GetArrayElementAtIndex(i); if (e.FindPropertyRelative("target").objectReferenceValue == previous) e.FindPropertyRelative("target").objectReferenceValue = panel.gameObject; }
            data.ApplyModifiedPropertiesWithoutUndo(); Object.DestroyImmediate(previous);
        }
        private static void RemapCity(Scene scene)
        {
            var instances = definitions.instances.Where(i => i.cityId == "big_rock_city").Select(i => i.id).ToHashSet();
            var routes = definitions.routes.Where(r => instances.Contains(r.from)).Select(r => r.id).ToHashSet();
            var ids = instances.Concat(routes).ToHashSet();
            foreach (var transform in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)))
            {
                if (transform.name.StartsWith("BigRock", StringComparison.Ordinal)) transform.name = transform.name.Replace("BigRock", "SevenHills");
                foreach (var component in transform.GetComponents<MonoBehaviour>().Where(c => c != null))
                {
                    var data = new SerializedObject(component); var property = data.GetIterator(); bool changed = false;
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.String || !ids.Contains(property.stringValue)) continue;
                        if (property.name != "instanceId" && property.name != "workplaceId" && property.name != "requiredBuilding" && property.name != "routeId") continue;
                        property.stringValue = "sh_" + property.stringValue.Substring(3); changed = true;
                    }
                    if (changed) data.ApplyModifiedPropertiesWithoutUndo();
                }
                var text = transform.GetComponent<Text>(); if (text != null && text.text.Contains("Big Rock City")) text.text = text.text.Replace("Big Rock City", "Seven Hills");
            }
        }
        private static void ThemeLandscape(Scene scene, GameRoot root)
        {
            var materials = new Dictionary<Material, Material>();
            foreach (var renderer in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
            {
                var shared = renderer.sharedMaterials; bool changed = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    var source = shared[i]; if (source == null || source.shader == null) continue; string shader = source.shader.name;
                    if (!shader.Contains("NaturalGround") && !shader.Contains("LivingWater") && !shader.Contains("FeatheredGround") && !shader.Contains("AtlasGround") && !shader.Contains("LivingFoliage")) continue;
                    if (!materials.TryGetValue(source, out var themed))
                    {
                        string path = Folder + "/Materials/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)) + ".mat";
                        themed = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (themed == null)
                        {
                            themed = new Material(source) { name = "SevenHills_" + source.name };
                            if (shader.Contains("Ground"))
                            {
                                if (themed.HasProperty("_BaseColor")) themed.SetColor("_BaseColor", new Color(.31f, .46f, .30f));
                                if (themed.HasProperty("_Tint")) themed.SetColor("_Tint", new Color(.91f, 1.14f, .96f));
                                if (themed.HasProperty("_Saturation")) themed.SetFloat("_Saturation", .72f);
                                if (themed.HasProperty("_Contrast")) themed.SetFloat("_Contrast", .48f);
                            }
                            if (shader.Contains("Foliage") && themed.HasProperty("_Color")) themed.SetColor("_Color", new Color(.83f, 1.10f, .91f));
                            AssetDatabase.CreateAsset(themed, path);
                        }
                        materials.Add(source, themed);
                    }
                    shared[i] = themed; changed = true;
                }
                if (changed) renderer.sharedMaterials = shared;
            }
            root.CameraRig.RenderCamera.backgroundColor = new Color(.13f, .21f, .18f);
        }
        private static void ImportSingle(string path, float pivot)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing city artwork " + path);
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single) return;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 256;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = true; importer.maxTextureSize = 2048;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteAlignment = 9; settings.spritePivot = new Vector2(.5f, pivot); importer.SetTextureSettings(settings); importer.SaveAndReimport();
        }
        private static bool WelcomeSigns(Scene scene)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Art/town_sign.png"); bool changed = false;
            foreach (var transform in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "Living_town_sign" || t.name == "SevenHillsWelcomeSign" || t.name == "BigRockWelcomeSign"))
            {
                var renderer = transform.GetComponentInChildren<SpriteRenderer>(true); if (renderer.sprite == sprite) continue;
                float height = renderer.sprite.bounds.size.y * renderer.transform.localScale.y; renderer.sprite = sprite; renderer.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform);
                transform.name = "SevenHillsWelcomeSign"; PrefabUtility.RecordPrefabInstancePropertyModifications(transform.gameObject); changed = true;
            }
            return changed;
        }
        private static GameObject Instance(string path, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (prefab == null) throw new InvalidOperationException("Missing authored prefab " + path);
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }
    }
}

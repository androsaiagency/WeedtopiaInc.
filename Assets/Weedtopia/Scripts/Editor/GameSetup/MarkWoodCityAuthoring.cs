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
    /// Explicit additive sixth-city authoring. The current second-city scene supplies shared shop, localization,
    /// mobile controls and world contracts. Earlier city installers are deliberately never invoked here.
    /// </summary>
    public static class MarkWoodCityAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/MarkWoodCity";
        public const string ScenePath = "Assets/Weedtopia/Game/Scenes/MarkWoodCity.unity";
        public const string CatalogPath = Folder + "/BuildingVisuals.asset";
        public const string StoriesPath = Folder + "/ResidentStories.prefab";
        public const string PeoplePath = Folder + "/Residents.prefab";
        public const string VisualsPath = Folder + "/ResidentVisuals.asset";
        public const string LandmarksPath = Folder + "/Prefabs/ForestRiverPlaces.prefab";
        public const string MapPath = Folder + "/DistrictMap.asset";
        private const string City = "mark_wood_city";
        private static GameDefinitions definitions;
        private static readonly Dictionary<string, Vector2> Places = new Dictionary<string, Vector2>
        {
            { "mw_mara", new Vector2(240, -95) }, { "mw_jonas", new Vector2(348, -86) },
            { "mw_leila", new Vector2(-27, -57) }, { "mw_bram", new Vector2(-96, 112) },
            { "mw_tessa", new Vector2(-152, 64) }, { "mw_silas", new Vector2(58, -69) },
            { "mw_nora", new Vector2(-28, 14) }, { "mw_owen", new Vector2(84, 14) }
        };

        [MenuItem("Weedtopia/Cities/Install or Repair Mark Wood City")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string sub in new[] { "", "/Materials", "/Buildings", "/Prefabs" }) Directory.CreateDirectory(Folder + sub);
                AssetDatabase.Refresh();
                definitions = AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
                RequireAuthoringInputs();
                if (definitions.cities.All(c => c.id != "mark_wood_city")) throw new InvalidOperationException("Mark Wood City content must be installed before scene authoring.");
                var sourceScene = EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var source = FindRoot(sourceScene);
                var sourceCatalog = (BuildingVisualCatalog)new SerializedObject(source).FindProperty("productionCatalog").objectReferenceValue;
                Buildings(sourceCatalog);
                var visuals = Residents();
                ResidentPanel(visuals); ResidentWorld(visuals); Landmarks(); Districts(); ImportSingle(Folder + "/Art/town_sign.png", .06f);
                var changedExisting = MarkWoodCityCampaignAuthoring.ApplyWithReport();
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
                    data.FindProperty("sceneCityId").stringValue = "mark_wood_city";
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
                // Repair only missing Mark Wood City-owned assets/references. Authored/manual objects retain identity.
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
                
                if (root.Ui.Stories == null) throw new InvalidOperationException("Missing story UI; refusing a broad UI rebuild.");
                if (root.Ui.Stories.visuals == null) { root.Ui.Stories.visuals = AssetDatabase.LoadAssetAtPath<VipVisualCatalog>(VisualsPath); changed = true; }
                if (!cityScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MarkWoodCityStamp>(true)).Any())
                { Instance(LandmarksPath, null); changed = true; }
                changed |= MapAndLabels(root, cityScene, created);
                foreach (var place in cityScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CityPlaceProgressVisual>(true)))
                    if (place.root != root) { place.root = root; PrefabUtility.RecordPrefabInstancePropertyModifications(place); changed = true; }
                changed |= WelcomeSigns(cityScene, created);
                changed |= MarkWoodCityCampaignAuthoring.ApplyCampaignRows(root.Ui.Campaign);
                if (changed) { EditorSceneManager.MarkSceneDirty(cityScene); EditorSceneManager.SaveScene(cityScene); }
                var scenes = EditorBuildSettings.scenes.ToList();
                if (!scenes.Any(s => s.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
                else if (scenes.Any(s => s.path == ScenePath && !s.enabled)) { scenes.Single(s => s.path == ScenePath).enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); }
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Docs/MarkWoodCity");
                File.WriteAllText("Docs/MarkWoodCity/authoring_receipt.json", JsonUtility.ToJson(new Receipt
                {
                    utc = DateTime.UtcNow.ToString("O"), createdScene = created, changedExistingCityScenes = changedExisting,
                    buildings = definitions.instances.Count(i => i.cityId == "mark_wood_city"),
                    routes = definitions.routes.Count(r => definitions.instances.Single(i => i.id == r.from).cityId == "mark_wood_city"),
                    mainQuests = definitions.QuestsFor("mark_wood_city").Length
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
            public int buildings, routes, mainQuests, buildingVariants = 251, landmarks = 3, residents = 8, chapters = 32, recurring = 24, sharedProjects = 3;
        }
        private static GameRoot FindRoot(Scene scene) => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameRoot>(true)).Single();
        private static BuildingVisualCatalog Buildings(BuildingVisualCatalog source)
        {
            var existing = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(CatalogPath);
            if (existing != null) return existing;
            var shader = Shader.Find("Weedtopia/Mark Wood City/Building Palette");
            if (shader == null) throw new InvalidOperationException("Missing Mark Wood City building shader.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/CedarForestCopper.mat");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, Folder + "/Materials/CedarForestCopper.mat"); }
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
            foreach (var person in definitions.vipStories.Where(p => p.cityId == "mark_wood_city"))
            {
                string path = Folder + "/Art/" + person.id + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing Mark Wood City resident artwork " + path);
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
                foreach (var person in definitions.vipStories.Where(p => p.cityId == "mark_wood_city"))
                {
                    var row = Instance(VipStoriesAuthoring.RowPath, ui.rosterScroll.content).GetComponent<VipStoryRosterRow>();
                    row.id = person.id; row.portrait.sprite = visuals.Get(person.id).portrait; rows.Add(row);
                }
                foreach (var collaboration in definitions.vipCollaborations.Where(p => p.cityId == "mark_wood_city"))
                {
                    var row = Instance(VipStoriesAuthoring.RowPath, ui.rosterScroll.content).GetComponent<VipStoryRosterRow>();
                    row.id = collaboration.id; row.collaboration = true; row.gameObject.SetActive(false); rows.Add(row);
                }
                ui.rows = rows.ToArray(); ui.portrait.sprite = visuals.entries[0].body;
                panel.name = "MarkWoodCityResidentStories"; PrefabUtility.SaveAsPrefabAsset(panel, StoriesPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(panel); }
        }
        private static void ResidentWorld(VipVisualCatalog visuals)
        {
            if (File.Exists(PeoplePath)) return;
            var world = new GameObject("MarkWoodCityResidents", typeof(VipTownPresentation)); var actors = new List<VipStoryActor>();
            foreach (var person in definitions.vipStories.Where(p => p.cityId == "mark_wood_city"))
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
                                memory.transform.localPosition = AuthoredMemoryPosition(person.id, chapter);
                                memory.transform.localScale *= MemoryScaleFactor(prop); EnsurePropDepth(memory); memory.SetActive(false);
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
        /// <summary>Fail before changing travel rows or scene references if approved presentation inputs are absent.</summary>
        private static void RequireAuthoringInputs()
        {
            var people = definitions.vipStories.Where(p => p.cityId == City).ToArray();
            if (people.Length != Places.Count || people.Any(p => !Places.ContainsKey(p.id)))
                throw new InvalidOperationException("Mark Wood City requires its eight authored inhabitants before installation.");
            foreach (var person in people)
            {
                if (!(AssetImporter.GetAtPath(Folder + "/Art/" + person.id + ".png") is TextureImporter))
                    throw new InvalidOperationException("Missing Mark Wood City resident artwork: " + person.id);
                for (int chapter = 0; chapter < person.chapters.Length; chapter++)
                    for (int choice = 0; choice < person.chapters[chapter].choices.Length; choice++)
                        RequirePrefab(MemoryProp(person.id, chapter, choice));
            }
            if (!(AssetImporter.GetAtPath(Folder + "/Art/town_sign.png") is TextureImporter))
                throw new InvalidOperationException("Missing Mark Wood City welcome-sign artwork.");
            foreach (string prop in new[] { "Prop_salvage", "Prop_crates", "Prop_lantern", "Prop_sign", "Prop_pine", "Prop_tree", "Town_produce_stall", "Town_workshop_corner", "Town_reading_corner", "Town_picnic_nook", "Town_fern_garden", "Town_herb_garden", "Town_rowan_tree" })
                RequirePrefab(PropPath(prop));
            RequirePrefab(BigRockCityAuthoring.StoriesPath);
            RequirePrefab(VipStoriesAuthoring.RowPath);
            RequirePrefab(VipStoriesAuthoring.ActorPath);
            if (!File.Exists(BigRockCityAuthoring.ScenePath)) throw new InvalidOperationException("Missing current authored Big Rock City source scene.");
            if (Shader.Find("Weedtopia/Mark Wood City/Building Palette") == null)
                throw new InvalidOperationException("Missing Mark Wood City building palette shader.");
        }

        private static void RequirePrefab(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                throw new InvalidOperationException("Missing authored prefab " + path);
        }

        private static string PropPath(string prop)
            => (prop.StartsWith("Town_", StringComparison.Ordinal) ? "Assets/Weedtopia/Production/TownVitality/Prefabs/" : "Assets/Weedtopia/Production/Prefabs/") + prop + ".prefab";

        private static string MemoryProp(string person, int chapter, int choice)
        {
            string[] props;
            string alternate;
            switch (person)
            {
                case "mw_mara": props = new[] { "Prop_sign", "Town_fern_garden", "Town_reading_corner", "Town_rowan_tree" }; alternate = "Town_picnic_nook"; break;
                case "mw_jonas": props = new[] { "Prop_planter", "Town_herb_garden", "Town_fern_garden", "Town_produce_stall" }; alternate = "Town_herb_garden"; break;
                case "mw_leila": props = new[] { "Prop_crates", "Town_produce_stall", "Town_picnic_nook", "Town_tea_patio" }; alternate = "Prop_sign"; break;
                case "mw_bram": props = new[] { "Prop_barrels", "Town_workshop_corner", "Town_workshop_corner", "Prop_lantern" }; alternate = "Town_reading_corner"; break;
                case "mw_tessa": props = new[] { "Prop_crates", "Town_workshop_corner", "Town_workshop_corner", "Town_reading_corner" }; alternate = "Town_reading_corner"; break;
                case "mw_silas": props = new[] { "Prop_lantern", "Town_tea_patio", "Town_picnic_nook", "Town_reading_corner" }; alternate = "Prop_sign"; break;
                case "mw_nora": props = new[] { "Prop_sign", "Town_reading_corner", "Prop_sign", "Town_reading_corner" }; alternate = "Town_reading_corner"; break;
                case "mw_owen": props = new[] { "Prop_lantern", "Prop_crates", "Prop_sign", "Town_produce_stall" }; alternate = "Town_reading_corner"; break;
                default: throw new InvalidOperationException("Unknown Mark Wood City inhabitant " + person);
            }
            if (chapter < 0 || chapter >= props.Length) throw new InvalidOperationException("Unsupported resident chapter " + chapter);
            return PropPath(chapter == 2 && choice == 1 ? alternate : props[chapter]);
        }

        private static Vector3 AuthoredMemoryPosition(string person, int chapter)
        {
            // Each resident retains a clear foreground slot and a second row for permanent chapter memories.
            // The workshop props have larger footprints, so their two rows are spaced further apart.
            bool workshop = person == "mw_bram" || person == "mw_tessa";
            float side = person == "mw_leila" || person == "mw_nora" || person == "mw_tessa" ? -1 : 1;
            float across = workshop ? 20 : 16;
            return DistrictMap.ToWorld(new Vector2(side * (8 + chapter % 2 * across), -9 - chapter / 2 * (workshop ? 20 : 16)));
        }

        private static float MemoryScaleFactor(string path)
        {
            switch (Path.GetFileNameWithoutExtension(path))
            {
                case "Town_reading_corner": case "Town_tea_patio": case "Town_workshop_corner": return .65f;
                case "Town_picnic_nook": case "Town_fern_garden": case "Town_herb_garden": return .75f;
                case "Town_bread_cart": case "Town_produce_stall": return .55f;
                case "Town_rowan_tree": return .38f;
                case "Prop_crates": case "Prop_barrels": return 1.3f;
                case "Prop_planter": return 1.1f;
                case "Prop_sign": return 1;
                case "Prop_lantern": return .85f;
                default: throw new InvalidOperationException("Unsupported Mark Wood City memory prop " + path);
            }
        }

        private static void EnsurePropDepth(GameObject prop)
        {
            foreach (var sprite in prop.GetComponentsInChildren<SpriteRenderer>(true))
            {
                // Ground contact shadows retain their authored flat projection.
                if (sprite.gameObject.name == "ContactShadow" || sprite.GetComponent<CityLandmarkVisual>() != null) continue;
                sprite.gameObject.AddComponent<CityLandmarkVisual>();
            }
        }

        private static GameObject PlaceProp(string prop, string name, Transform parent, Vector2 position, float scale = 1, bool visible = true)
        {
            var instance = Instance(PropPath(prop), parent);
            instance.name = name;
            instance.transform.localPosition = DistrictMap.ToWorld(position);
            instance.transform.localScale *= scale;
            EnsurePropDepth(instance);
            instance.SetActive(visible);
            return instance;
        }

        private static void Landmarks()
        {
            if (File.Exists(LandmarksPath)) return;
            var world = new GameObject("ForestRiverPlaces", typeof(MarkWoodCityStamp));
            try
            {
                var positions = new[] { new Vector2(112, -90), new Vector2(-173, 100), new Vector2(94, 292) };
                var names = new[] { "RiverMarketCommons", "OpenSawYards", "ForestRiverMeeting" };
                var projects = new[] { "mw_joint_river_market", "mw_joint_open_saw_yards", "mw_joint_forest_river_day" };
                var milestones = new[] { 1800, 5500, 11000 };
                for (int index = 0; index < positions.Length; index++)
                {
                    var placeObject = new GameObject(names[index], typeof(CityPlaceProgressVisual));
                    placeObject.transform.SetParent(world.transform, false);
                    placeObject.transform.localPosition = DistrictMap.ToWorld(positions[index]);
                    var place = placeObject.GetComponent<CityPlaceProgressVisual>();
                    place.cityId = City;
                    place.residents = milestones[index];
                    place.collaborationId = projects[index];
                    place.keepGrowingPlaceAfterCompletion = false;
                    place.closedPlace = PlaceProp("Prop_salvage", "ClosedCommunityPlace", placeObject.transform, new Vector2(1, -2));
                    var supplies = new GameObject("SharedRestorationSupplies");
                    supplies.transform.SetParent(placeObject.transform, false);
                    PlaceProp("Prop_crates", "SortedReclaimedTimber", supplies.transform, new Vector2(-8, -5), 1.5f);
                    PlaceProp("Prop_lantern", "VolunteerMeetingLight", supplies.transform, new Vector2(9, 2));
                    supplies.SetActive(false);
                    place.growingPlace = supplies;
                    var completed = new GameObject("RestoredCommunityPlace");
                    completed.transform.SetParent(placeObject.transform, false);
                    completed.SetActive(false);
                    place.completedPlace = completed;
                    if (index == 0)
                    {
                        PlaceProp("Town_produce_stall", "RiverMarketProduceTable", completed.transform, new Vector2(-13, -2), .85f);
                        PlaceProp("Town_picnic_nook", "NeighbourRiverTable", completed.transform, new Vector2(12, -11), .75f);
                        PlaceProp("Town_herb_garden", "MarketSeedExchange", completed.transform, new Vector2(10, 9), .65f);
                    }
                    else if (index == 1)
                    {
                        PlaceProp("Town_workshop_corner", "ReclaimedTimberWorkshop", completed.transform, new Vector2(-12, -2), .8f);
                        PlaceProp("Prop_sign", "OldSawYardWitnessBoard", completed.transform, new Vector2(13, 5), 1.15f);
                        PlaceProp("Town_reading_corner", "WoodgrainArchive", completed.transform, new Vector2(14, -13), .65f);
                        place.choiceResidentId = "mw_bram";
                        place.choiceChapter = 2;
                        place.choiceValues = new[] { "repair_rack", "learning_bench" };
                        var repair = PlaceProp("Town_workshop_corner", "NeighbourRepairRack", placeObject.transform, new Vector2(0, -27), .65f, false);
                        var learning = PlaceProp("Town_reading_corner", "OpenLearningBench", placeObject.transform, new Vector2(0, -27), .65f, false);
                        place.choicePlaces = new[] { repair, learning };
                    }
                    else
                    {
                        PlaceProp("Town_picnic_nook", "ForestRiverNeighboursTable", completed.transform, new Vector2(-13, -2), .85f);
                        PlaceProp("Town_fern_garden", "ForestTrailLivingGarden", completed.transform, new Vector2(12, 8), .75f);
                        PlaceProp("Prop_sign", "SharedForestRiverRoute", completed.transform, new Vector2(12, -12));
                        PlaceProp("Town_reading_corner", "ListeningAndStoryBench", completed.transform, new Vector2(-15, -22), .65f);
                    }
                }
                // Forest belts frame the four authored districts without touching road or building anchors.
                var forest = new GameObject("ProtectedWoodlandBelt");
                forest.transform.SetParent(world.transform, false);
                var treePositions = new[]
                {
                    new Vector2(200, -124), new Vector2(248, -125), new Vector2(290, -123), new Vector2(334, -122),
                    new Vector2(380, -124), new Vector2(424, -125), new Vector2(470, -120), new Vector2(516, -117),
                    new Vector2(571, -75), new Vector2(566, -18), new Vector2(530, 13), new Vector2(484, 16),
                    new Vector2(-74, 150), new Vector2(-72, 201), new Vector2(-75, 252), new Vector2(-72, 304),
                    new Vector2(-224, 10), new Vector2(-226, 64), new Vector2(-227, 123), new Vector2(-191, 154),
                    new Vector2(70, 384), new Vector2(117, 388), new Vector2(165, 385), new Vector2(212, 382)
                };
                for (int index = 0; index < treePositions.Length; index++)
                {
                    var tree = PlaceProp(index % 3 == 0 ? "Prop_tree" : "Prop_pine", "ProtectedForestTree_" + (index + 1), forest.transform,
                        treePositions[index], 1.1f + index % 3 * .12f);
                    foreach (var renderer in tree.GetComponentsInChildren<SpriteRenderer>(true))
                        if (renderer.gameObject.name != "ContactShadow") renderer.color = new Color(.79f, .98f, .85f, 1);
                }
                PlaceProp("Town_fern_garden", "ForestGardenTrailhead", world.transform, new Vector2(283, -106), .8f);
                PlaceProp("Town_herb_garden", "OpenWoodlandSeedBeds", world.transform, new Vector2(455, -102), .8f);
                PlaceProp("Town_rowan_tree", "CanopyQuarterMemoryTree", world.transform, new Vector2(80, 179), .75f);
                PlaceProp("Town_fern_garden", "CanopyQuarterUnderstory", world.transform, new Vector2(79, 225), .75f);
                PrefabUtility.SaveAsPrefabAsset(world, LandmarksPath);
            }
            finally { Object.DestroyImmediate(world); }
        }
        private static void Districts()
        {
            if (File.Exists(MapPath)) return;
            if (!AssetDatabase.CopyAsset(BigRockCityAuthoring.Folder + "/DistrictMap.asset", MapPath)) throw new InvalidOperationException("Could not copy district map.");
            var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(MapPath);
            foreach (var range in map.districts)
            {
                string suffix = range.zone == Zone.Farm ? "farm" : range.zone == Zone.City ? "city" : range.zone == Zone.Settlement ? "settlement" : "industry";
                range.labelKey = "district.mark_wood_city." + suffix;
                range.label = range.zone == Zone.Farm ? "Waldgärten · Farm" : range.zone == Zone.City ? "Flussmarkt · Stadt" : range.zone == Zone.Settlement ? "Baumkronenviertel · Wohnviertel" : "Alte Sägehöfe · Industrie";
            }
            EditorUtility.SetDirty(map);
        }
        private static bool MapAndLabels(GameRoot root, Scene scene, bool created)
        {
            bool changed = false;
            var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(MapPath); var cameraData = new SerializedObject(root.CameraRig);
            if (created || cameraData.FindProperty("districtMap").objectReferenceValue == null)
            { cameraData.FindProperty("districtMap").objectReferenceValue = map; cameraData.ApplyModifiedPropertiesWithoutUndo(); changed = true; }
            foreach (var navigator in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DistrictNavigator>(true)))
            {
                var data = new SerializedObject(navigator); var label = data.FindProperty("cityLabel");
                if (!created && !string.IsNullOrEmpty(label.stringValue) && label.stringValue != "Big Rock City") continue; label.stringValue = "Mark Wood City"; data.ApplyModifiedPropertiesWithoutUndo(); changed = true;
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
                if (transform.name.StartsWith("BigRock", StringComparison.Ordinal)) transform.name = transform.name.Replace("BigRock", "MarkWoodCity");
                foreach (var component in transform.GetComponents<MonoBehaviour>().Where(c => c != null))
                {
                    var data = new SerializedObject(component); var property = data.GetIterator(); bool changed = false;
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.String || !ids.Contains(property.stringValue)) continue;
                        if (property.name != "instanceId" && property.name != "workplaceId" && property.name != "requiredBuilding" && property.name != "routeId") continue;
                        property.stringValue = "mw_" + property.stringValue.Substring(3); changed = true;
                    }
                    if (changed) data.ApplyModifiedPropertiesWithoutUndo();
                }
                var text = transform.GetComponent<Text>(); if (text != null && text.text.Contains("Big Rock City")) text.text = text.text.Replace("Big Rock City", "Mark Wood City");
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
                            themed = new Material(source) { name = "MarkWoodCity_" + source.name };
                            if (shader.Contains("Ground"))
                            {
                                if (themed.HasProperty("_BaseColor")) themed.SetColor("_BaseColor", new Color(.28f, .42f, .29f));
                                if (themed.HasProperty("_Tint")) themed.SetColor("_Tint", new Color(.91f, 1.07f, .88f));
                                if (themed.HasProperty("_Saturation")) themed.SetFloat("_Saturation", .72f);
                                if (themed.HasProperty("_Contrast")) themed.SetFloat("_Contrast", .48f);
                            }
                            if (shader.Contains("Foliage") && themed.HasProperty("_Color")) themed.SetColor("_Color", new Color(.78f, 1.0f, .85f));
                            AssetDatabase.CreateAsset(themed, path);
                        }
                        materials.Add(source, themed);
                    }
                    shared[i] = themed; changed = true;
                }
                if (changed) renderer.sharedMaterials = shared;
            }
            root.CameraRig.RenderCamera.backgroundColor = new Color(.12f, .23f, .23f);
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
        private static bool WelcomeSigns(Scene scene, bool created)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Art/town_sign.png"); bool changed = false;
            foreach (var transform in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "Living_town_sign" || t.name == "MarkWoodCityWelcomeSign" || t.name == "BigRockWelcomeSign" || t.name == "SevenHillsWelcomeSign"))
            {
                var renderer = transform.GetComponentInChildren<SpriteRenderer>(true); if ((!created && renderer.sprite != null) || renderer.sprite == sprite) continue;
                float height = renderer.sprite == null ? 8 : renderer.sprite.bounds.size.y * renderer.transform.localScale.y; renderer.sprite = sprite; renderer.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform);
                transform.name = "MarkWoodCityWelcomeSign"; PrefabUtility.RecordPrefabInstancePropertyModifications(transform.gameObject); changed = true;
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

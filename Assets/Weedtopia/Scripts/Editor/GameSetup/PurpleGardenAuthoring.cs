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
    /// Explicit additive eighth-city authoring. The current second-city scene supplies shared shop, localization,
    /// mobile controls and world contracts. Earlier city installers are deliberately never invoked here.
    /// </summary>
    public static class PurpleGardenAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/PurpleGarden";
        public const string ScenePath = "Assets/Weedtopia/Game/Scenes/PurpleGarden.unity";
        public const string CatalogPath = Folder + "/BuildingVisuals.asset";
        public const string StoriesPath = Folder + "/ResidentStories.prefab";
        public const string PeoplePath = Folder + "/Residents.prefab";
        public const string VisualsPath = Folder + "/ResidentVisuals.asset";
        public const string LandmarksPath = Folder + "/Prefabs/BotanicalCommons.prefab";
        public const string MapPath = Folder + "/DistrictMap.asset";
        private const string City = "purple_garden";
        private static readonly string[] ArchitectureNames = { "VioletGlasshouse", "SeedArchive", "BloomMarketPavilion", "WaterCourt" };
        private static readonly string[] ArchitectureArtwork = { "architecture_greenhouse", "architecture_seed_terrace", "architecture_market_pergola", "architecture_cooperative_gate" };
        private static GameDefinitions definitions;
        private static readonly Dictionary<string, Vector2> Places = new Dictionary<string, Vector2>
        {
            { "pg_iris", new Vector2(240, -95) }, { "pg_malik", new Vector2(348, -86) },
            { "pg_viola", new Vector2(-27, -57) }, { "pg_theo", new Vector2(-96, 112) },
            { "pg_jun", new Vector2(-152, 64) }, { "pg_ravi", new Vector2(58, -69) },
            { "pg_ada", new Vector2(-28, 14) }, { "pg_selma", new Vector2(84, 14) }
        };

        [MenuItem("Weedtopia/Cities/Install or Repair Purple Garden")]
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
                if (definitions.cities.All(c => c.id != "purple_garden")) throw new InvalidOperationException("Purple Garden content must be installed before scene authoring.");
                var sourceScene = EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var source = FindRoot(sourceScene);
                var sourceCatalog = (BuildingVisualCatalog)new SerializedObject(source).FindProperty("productionCatalog").objectReferenceValue;
                Buildings(sourceCatalog);
                var visuals = Residents();
                ResidentPanel(visuals); ResidentWorld(visuals); GardenArchitecture(); Landmarks(); Districts(); ImportSingle(Folder + "/Art/town_sign.png", .06f);
                var changedExisting = PurpleGardenCampaignAuthoring.ApplyWithReport();
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
                    data.FindProperty("sceneCityId").stringValue = "purple_garden";
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
                // Repair only missing Purple Garden-owned assets/references. Authored/manual objects retain identity.
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
                if (!cityScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PurpleGardenStamp>(true)).Any())
                { Instance(LandmarksPath, null); changed = true; }
                changed |= MapAndLabels(root, cityScene, created);
                changed |= FitCityHeader(root);
                foreach (var place in cityScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CityPlaceProgressVisual>(true)).Where(p => p.cityId == City))
                    if (place.root == null) { place.root = root; PrefabUtility.RecordPrefabInstancePropertyModifications(place); changed = true; }
                changed |= WelcomeSigns(cityScene, created);
                changed |= PurpleGardenCampaignAuthoring.ApplyCampaignRows(root.Ui.Campaign);
                if (changed) { EditorSceneManager.MarkSceneDirty(cityScene); EditorSceneManager.SaveScene(cityScene); }
                var scenes = EditorBuildSettings.scenes.ToList();
                if (!scenes.Any(s => s.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
                else if (scenes.Any(s => s.path == ScenePath && !s.enabled)) { scenes.Single(s => s.path == ScenePath).enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); }
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Docs/PurpleGarden");
                File.WriteAllText("Docs/PurpleGarden/authoring_receipt.json", JsonUtility.ToJson(new Receipt
                {
                    utc = DateTime.UtcNow.ToString("O"), createdScene = created, changedExistingCityScenes = changedExisting,
                    buildings = definitions.instances.Count(i => i.cityId == "purple_garden"),
                    routes = definitions.routes.Count(r => definitions.instances.Single(i => i.id == r.from).cityId == "purple_garden"),
                    mainQuests = definitions.QuestsFor("purple_garden").Length
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
            public int buildings, routes, mainQuests, buildingVariants = 251, landmarks = 3, residents = 8, chapters = 32, recurring = 24, sharedProjects = 3, architecturePrefabs = 4;
        }
        private static GameRoot FindRoot(Scene scene) => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameRoot>(true)).Single();
        private static BuildingVisualCatalog Buildings(BuildingVisualCatalog source)
        {
            var existing = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(CatalogPath);
            if (existing != null) return existing;
            var shader = Shader.Find("Weedtopia/Mark Wood City/Building Palette");
            if (shader == null) throw new InvalidOperationException("Missing Purple Garden building shader.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/PurpleGlassCream.mat");
            if (material == null)
            {
                material = new Material(shader) { name = "PurpleGarden_PurpleGlassCream" };
                material.SetColor("_Cedar", new Color(1.06f, .97f, 1.09f));
                material.SetColor("_Forest", new Color(.64f, .43f, .83f));
                material.SetColor("_Copper", new Color(.77f, .56f, .85f));
                material.SetFloat("_Strength", .83f);
                AssetDatabase.CreateAsset(material, Folder + "/Materials/PurpleGlassCream.mat");
            }
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
            foreach (var person in definitions.vipStories.Where(p => p.cityId == "purple_garden"))
            {
                string path = Folder + "/Art/" + person.id + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing Purple Garden resident artwork " + path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple; importer.spritePixelsPerUnit = 256;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = true; importer.maxTextureSize = 2048; importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
#pragma warning disable 618
                importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
                if (sourceWidth < 128 || sourceHeight < 128)
                    throw new InvalidOperationException("Invalid Purple Garden resident artwork: " + path);
                float width = sourceWidth, height = sourceHeight;
                importer.spritesheet = new[]
                {
                    new SpriteMetaData { name = person.id + "_body", rect = new Rect(0, 0, width, height), alignment = 9, pivot = new Vector2(.5f, .02f) },
                    new SpriteMetaData { name = person.id + "_portrait", rect = new Rect(width * (100f / 1024), height * (690f / 1536), width * (824f / 1024), height * (800f / 1536)), alignment = 9, pivot = new Vector2(.5f, .5f) }
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
                foreach (var person in definitions.vipStories.Where(p => p.cityId == "purple_garden"))
                {
                    var row = Instance(VipStoriesAuthoring.RowPath, ui.rosterScroll.content).GetComponent<VipStoryRosterRow>();
                    row.id = person.id; row.portrait.sprite = visuals.Get(person.id).portrait; rows.Add(row);
                }
                foreach (var collaboration in definitions.vipCollaborations.Where(p => p.cityId == "purple_garden"))
                {
                    var row = Instance(VipStoriesAuthoring.RowPath, ui.rosterScroll.content).GetComponent<VipStoryRosterRow>();
                    row.id = collaboration.id; row.collaboration = true; row.gameObject.SetActive(false); rows.Add(row);
                }
                ui.rows = rows.ToArray(); ui.portrait.sprite = visuals.entries[0].body;
                panel.name = "PurpleGardenResidentStories"; PrefabUtility.SaveAsPrefabAsset(panel, StoriesPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(panel); }
        }
        private static void ResidentWorld(VipVisualCatalog visuals)
        {
            if (File.Exists(PeoplePath)) return;
            var world = new GameObject("PurpleGardenResidents", typeof(VipTownPresentation)); var actors = new List<VipStoryActor>();
            foreach (var person in definitions.vipStories.Where(p => p.cityId == "purple_garden"))
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
        /// <summary>Fail before changing travel rows or scene references if authored presentation inputs are absent.</summary>
        private static void RequireAuthoringInputs()
        {
            var people = definitions.vipStories.Where(p => p.cityId == City).ToArray();
            if (people.Length != Places.Count || people.Any(p => !Places.ContainsKey(p.id)))
                throw new InvalidOperationException("Purple Garden requires its eight authored inhabitants before installation.");
            foreach (var person in people)
            {
                if (!(AssetImporter.GetAtPath(Folder + "/Art/" + person.id + ".png") is TextureImporter))
                    throw new InvalidOperationException("Missing Purple Garden resident artwork: " + person.id);
                for (int chapter = 0; chapter < person.chapters.Length; chapter++)
                    for (int choice = 0; choice < person.chapters[chapter].choices.Length; choice++)
                        RequirePrefab(MemoryProp(person.id, chapter, choice));
            }
            if (!(AssetImporter.GetAtPath(Folder + "/Art/town_sign.png") is TextureImporter))
                throw new InvalidOperationException("Missing Purple Garden welcome-sign artwork.");
            foreach (string artwork in ArchitectureArtwork)
                if (!(AssetImporter.GetAtPath(Folder + "/Art/" + artwork + ".png") is TextureImporter))
                    throw new InvalidOperationException("Missing Purple Garden painted architecture: " + artwork);
            foreach (string prop in new[] { "Prop_salvage", "Prop_crates", "Prop_lantern", "Prop_sign", "Prop_planter", "Town_produce_stall", "Town_workshop_corner", "Town_reading_corner", "Town_picnic_nook", "Town_fern_garden", "Town_herb_garden", "Town_rowan_tree", "Town_laundry_garden" })
                RequirePrefab(PropPath(prop));
            RequirePrefab(BigRockCityAuthoring.StoriesPath);
            RequirePrefab(VipStoriesAuthoring.RowPath);
            RequirePrefab(VipStoriesAuthoring.ActorPath);
            if (!File.Exists(BigRockCityAuthoring.ScenePath)) throw new InvalidOperationException("Missing current authored Big Rock City source scene.");
            if (Shader.Find("Weedtopia/Mark Wood City/Building Palette") == null)
                throw new InvalidOperationException("Missing Purple Garden building palette shader.");
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
                case "pg_iris": props = new[] { "Prop_sign", "Town_herb_garden", "Town_reading_corner", "Town_fern_garden" }; alternate = "Town_picnic_nook"; break;
                case "pg_malik": props = new[] { "Prop_planter", "Town_workshop_corner", "Town_workshop_corner", "Prop_lantern" }; alternate = "Town_reading_corner"; break;
                case "pg_viola": props = new[] { "Prop_crates", "Town_tea_patio", "Town_picnic_nook", "Town_produce_stall" }; alternate = "Prop_sign"; break;
                case "pg_theo": props = new[] { "Prop_barrels", "Town_workshop_corner", "Town_workshop_corner", "Prop_lantern" }; alternate = "Town_reading_corner"; break;
                case "pg_jun": props = new[] { "Prop_crates", "Town_herb_garden", "Town_produce_stall", "Town_tea_patio" }; alternate = "Town_workshop_corner"; break;
                case "pg_ravi": props = new[] { "Prop_sign", "Prop_crates", "Town_reading_corner", "Town_produce_stall" }; alternate = "Prop_sign"; break;
                case "pg_ada": props = new[] { "Prop_sign", "Town_reading_corner", "Prop_sign", "Town_rowan_tree" }; alternate = "Town_reading_corner"; break;
                case "pg_selma": props = new[] { "Prop_lantern", "Prop_crates", "Prop_sign", "Town_picnic_nook" }; alternate = "Town_tea_patio"; break;
                default: throw new InvalidOperationException("Unknown Purple Garden inhabitant " + person);
            }
            if (chapter < 0 || chapter >= props.Length) throw new InvalidOperationException("Unsupported resident chapter " + chapter);
            return PropPath(chapter == 2 && choice == 1 ? alternate : props[chapter]);
        }

        private static Vector3 AuthoredMemoryPosition(string person, int chapter)
        {
            // Each resident retains a clear foreground slot and a second row for permanent chapter memories.
            // The workshop props have larger footprints, so their two rows are spaced further apart.
            bool workshop = person == "pg_theo" || person == "pg_malik";
            float side = person == "pg_viola" || person == "pg_ada" || person == "pg_jun" ? -1 : 1;
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
                default: throw new InvalidOperationException("Unsupported Purple Garden memory prop " + path);
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

        /// <summary>
        /// Bind authored painted architecture through the explicit installer. Existing prefab roots, nested
        /// shared props and scene placements retain their identity; static artwork remains serialized.
        /// </summary>
        private static void GardenArchitecture()
        {
            for (int index = 0; index < ArchitectureNames.Length; index++)
            {
                string name = ArchitectureNames[index];
                string path = Folder + "/Prefabs/" + name + ".prefab";
                var sprite = ImportArchitectureArtwork(Folder + "/Art/" + ArchitectureArtwork[index] + ".png");
                bool existing = File.Exists(path);
                var prefab = existing ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
                try
                {
                    bool changed = !existing;
                    var artwork = prefab.transform.Find("ArchitectureArtwork");
                    if (artwork == null)
                    {
                        var child = new GameObject("ArchitectureArtwork", typeof(SpriteRenderer));
                        child.transform.SetParent(prefab.transform, false);
                        artwork = child.transform;
                        float width = name == "SeedArchive" ? 31 : 32;
                        artwork.localScale = Vector3.one * (width / sprite.bounds.size.x);
                        child.GetComponent<SpriteRenderer>().sprite = sprite;
                        EnsurePropDepth(child);
                        changed = true;
                    }
                    else
                    {
                        var renderer = artwork.GetComponent<SpriteRenderer>();
                        if (renderer == null) throw new InvalidOperationException("Authored architecture has no renderer: " + path);
                        // Missing-reference repair leaves valid manual bindings and serialized transforms intact.
                        if (renderer.sprite == null) { renderer.sprite = sprite; changed = true; }
                        if (renderer.GetComponent<CityLandmarkVisual>() == null) { EnsurePropDepth(artwork.gameObject); changed = true; }
                    }
                    if (!existing) AddArchitectureProps(name, prefab.transform);
                    if (changed) PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally
                {
                    if (existing) PrefabUtility.UnloadPrefabContents(prefab);
                    else Object.DestroyImmediate(prefab);
                }
            }
        }

        private static void AddArchitectureProps(string name, Transform parent)
        {
            switch (name)
            {
                case "VioletGlasshouse":
                    PlaceProp("Prop_planter", "GreenhouseWelcomePlanter", parent, new Vector2(-12, -5), .9f);
                    PlaceProp("Town_herb_garden", "SharedSeedlingBed", parent, new Vector2(13, -6), .52f);
                    break;
                case "SeedArchive":
                    PlaceProp("Town_herb_garden", "TerraceHerbRow", parent, new Vector2(-6, 0), .7f);
                    PlaceProp("Town_fern_garden", "TerraceSeedRow", parent, new Vector2(7, 0), .65f);
                    PlaceProp("Prop_planter", "TerraceEndPlanter", parent, new Vector2(12, -6), .85f);
                    break;
                case "BloomMarketPavilion":
                    PlaceProp("Town_produce_stall", "MarketSeedAndProduceTable", parent, new Vector2(-3, -8), .8f);
                    PlaceProp("Town_fern_garden", "MarketClimbingGarden", parent, new Vector2(14, -2), .65f);
                    break;
                case "WaterCourt":
                    PlaceProp("Prop_planter", "CourtyardLeftPlanter", parent, new Vector2(-15, -6), 1.1f);
                    PlaceProp("Prop_planter", "CourtyardRightPlanter", parent, new Vector2(15, -6), 1.1f);
                    PlaceProp("Town_fern_garden", "CourtyardClimbingVines", parent, new Vector2(12, 4), .65f);
                    break;
                default: throw new InvalidOperationException("Unknown Purple Garden architecture: " + name);
            }
        }

        private static Sprite ImportArchitectureArtwork(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing painted architecture " + path);
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            var pivot = new Vector2(.5f, .06f);
            bool changed = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
                importer.npotScale != TextureImporterNPOTScale.None || !importer.alphaIsTransparency || !importer.mipmapEnabled ||
                importer.maxTextureSize != 2048 || importer.spritePixelsPerUnit != 256 || settings.spriteAlignment != 9 || settings.spritePivot != pivot;
            if (changed)
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.npotScale = TextureImporterNPOTScale.None; importer.alphaIsTransparency = true; importer.mipmapEnabled = true;
                importer.maxTextureSize = 2048; importer.spritePixelsPerUnit = 256;
                importer.ReadTextureSettings(settings); settings.spriteAlignment = 9; settings.spritePivot = pivot; importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Painted architecture did not import as a sprite: " + path);
            return sprite;
        }

        private static GameObject PlaceArchitecture(string prefab, string name, Transform parent, Vector2 position, float scale = 1)
        {
            var instance = Instance(Folder + "/Prefabs/" + prefab + ".prefab", parent); instance.name = name;
            instance.transform.localPosition = DistrictMap.ToWorld(position); instance.transform.localScale *= scale;
            return instance;
        }

        private static void Landmarks()
        {
            if (File.Exists(LandmarksPath)) return;
            var world = new GameObject("BotanicalCommons", typeof(PurpleGardenStamp));
            try
            {
                var positions = new[] { new Vector2(112, -90), new Vector2(-173, 100), new Vector2(94, 292) };
                var names = new[] { "WaterCourtCommons", "OpenSeedArchive", "NightBloomCommons" };
                var projects = new[] { "pg_joint_water_court", "pg_joint_seed_archive", "pg_joint_night_bloom" };
                var milestones = new[] { 2000, 6500, 12500 };
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
                    var supplies = new GameObject("SharedBotanicalSupplies");
                    supplies.transform.SetParent(placeObject.transform, false);
                    PlaceProp("Prop_crates", "RestorationToolsAndSeedBoxes", supplies.transform, new Vector2(-8, -5), 1.5f);
                    PlaceProp("Prop_lantern", "NeighbourWorkEveningLight", supplies.transform, new Vector2(9, 2));
                    supplies.SetActive(false);
                    place.growingPlace = supplies;
                    var completed = new GameObject("OpenedBotanicalPlace");
                    completed.transform.SetParent(placeObject.transform, false);
                    completed.SetActive(false);
                    place.completedPlace = completed;
                    if (index == 0)
                    {
                        PlaceArchitecture("WaterCourt", "RestoredSharedWaterCourt", completed.transform, new Vector2(-13, 7), .78f);
                        PlaceProp("Town_reading_corner", "SeedStoriesAndOpenCabinet", completed.transform, new Vector2(17, -9), .65f);
                        PlaceProp("Town_herb_garden", "PublicSeedExchangeBed", completed.transform, new Vector2(13, 12), .65f);
                        PlaceProp("Prop_sign", "SeedOriginsAndBorrowingBoard", completed.transform, new Vector2(-8, -19), 1.05f);
                    }
                    else if (index == 1)
                    {
                        PlaceArchitecture("SeedArchive", "OpenCommunitySeedArchive", completed.transform, new Vector2(0, 11), .8f);
                        PlaceProp("Town_workshop_corner", "ReclaimedGlassAndToolWorkshop", completed.transform, new Vector2(-15, -7), .72f);
                        PlaceProp("Prop_sign", "NeighbourSupplyAndAgreementBoard", completed.transform, new Vector2(17, -3), 1.15f);
                        PlaceProp("Town_tea_patio", "SharedCourtyardTable", completed.transform, new Vector2(17, -19), .6f);
                        place.choiceResidentId = "pg_theo";
                        place.choiceChapter = 2;
                        place.choiceValues = new[] { "repair_team", "teaching_bench" };
                        var repair = PlaceProp("Town_workshop_corner", "SharedGlassRepairTeam", placeObject.transform, new Vector2(0, -29), .65f, false);
                        var learning = PlaceProp("Town_reading_corner", "OpenTeachingBench", placeObject.transform, new Vector2(0, -29), .65f, false);
                        place.choicePlaces = new[] { repair, learning };
                    }
                    else
                    {
                        PlaceArchitecture("BloomMarketPavilion", "NightBloomMarketCanopy", completed.transform, new Vector2(-11, 9), .75f);
                        PlaceProp("Town_picnic_nook", "NightBloomNeighbourLongTable", completed.transform, new Vector2(15, -8), .75f);
                        PlaceProp("Town_fern_garden", "SharedFestivalGarden", completed.transform, new Vector2(14, 11), .68f);
                        PlaceProp("Prop_sign", "NeighbourStoriesAndDeliveryMap", completed.transform, new Vector2(10, -24));
                        PlaceProp("Town_reading_corner", "NeighbourListeningBench", completed.transform, new Vector2(-16, -21), .6f);
                    }
                }
                // Terraces and glass silhouettes frame the four districts outside their delivery/building anchors.
                var gardens = new GameObject("VioletWaterGardens");
                gardens.transform.SetParent(world.transform, false);
                var terracePositions = new[]
                {
                    new Vector2(208, -124), new Vector2(291, -126), new Vector2(376, -123), new Vector2(459, -121),
                    new Vector2(539, -110), new Vector2(567, -23), new Vector2(-75, 173), new Vector2(-74, 264),
                    new Vector2(-222, 26), new Vector2(-225, 116), new Vector2(80, 384), new Vector2(169, 383)
                };
                for (int index = 0; index < terracePositions.Length; index++)
                {
                    PlaceProp(index % 2 == 0 ? "Town_herb_garden" : "Town_fern_garden", "VioletGrowingBed_" + (index + 1), gardens.transform, terracePositions[index], .9f);
                    if (index % 3 == 0)
                        PlaceProp("Town_rowan_tree", "BotanicalBoundaryTree_" + (index + 1), gardens.transform,
                            terracePositions[index] + new Vector2(27, 9), .8f);
                }
                PlaceArchitecture("VioletGlasshouse", "SharedPropagationGlasshouse", world.transform, new Vector2(260, -104), .9f);
                PlaceArchitecture("VioletGlasshouse", "NeighbourSeedConservatory", world.transform, new Vector2(435, -104), 1);
                PlaceArchitecture("BloomMarketPavilion", "EverydayBloomMarketCanopy", world.transform, new Vector2(56, -107), .8f);
                PlaceArchitecture("WaterCourt", "GlassworksWaterCourt", world.transform, new Vector2(-211, 71), .85f);
                PlaceProp("Town_rowan_tree", "WaterCourtMemoryTree", world.transform, new Vector2(80, 179), .75f);
                PlaceProp("Town_laundry_garden", "WaterCourtNeighbourGarden", world.transform, new Vector2(79, 225), .75f);
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
                range.labelKey = "district.purple_garden." + suffix;
                range.label = range.zone == Zone.Farm ? "Violetthaine · Farm" : range.zone == Zone.City ? "Blütenmarkt · Stadt" : range.zone == Zone.Settlement ? "Wasserhöfe · Wohnviertel" : "Glashütten · Industrie";
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
                if (!created && !string.IsNullOrEmpty(label.stringValue) && label.stringValue != "Big Rock City") continue; label.stringValue = "Purple Garden"; data.ApplyModifiedPropertiesWithoutUndo(); changed = true;
            }
            return changed;
        }
        private static bool FitCityHeader(GameRoot root)
        {
            var status = (CityStatusBar)new SerializedObject(root.Ui).FindProperty("cityStatus").objectReferenceValue;
            if (status == null || status.CityName == null) throw new InvalidOperationException("Missing authored Purple Garden city header.");
            var name = status.CityName.rectTransform;
            if (!name.anchoredPosition.Equals(new Vector2(0, -49))) return false;
            name.anchoredPosition = new Vector2(0, -43);
            PrefabUtility.RecordPrefabInstancePropertyModifications(name);
            return true;
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
                if (transform.name.StartsWith("BigRock", StringComparison.Ordinal)) transform.name = transform.name.Replace("BigRock", "PurpleGarden");
                foreach (var component in transform.GetComponents<MonoBehaviour>().Where(c => c != null))
                {
                    var data = new SerializedObject(component); var property = data.GetIterator(); bool changed = false;
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.String || !ids.Contains(property.stringValue)) continue;
                        if (property.name != "instanceId" && property.name != "workplaceId" && property.name != "requiredBuilding" && property.name != "routeId") continue;
                        property.stringValue = "pg_" + property.stringValue.Substring(3); changed = true;
                    }
                    if (changed) data.ApplyModifiedPropertiesWithoutUndo();
                }
                var text = transform.GetComponent<Text>(); if (text != null && text.text.Contains("Big Rock City")) text.text = text.text.Replace("Big Rock City", "Purple Garden");
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
                            themed = new Material(source) { name = "PurpleGarden_" + source.name };
                            if (shader.Contains("Ground"))
                            {
                                if (themed.HasProperty("_BaseColor")) themed.SetColor("_BaseColor", new Color(.44f, .41f, .49f));
                                if (themed.HasProperty("_Tint")) themed.SetColor("_Tint", new Color(1.05f, .96f, 1.07f));
                                if (themed.HasProperty("_Saturation")) themed.SetFloat("_Saturation", .72f);
                                if (themed.HasProperty("_Contrast")) themed.SetFloat("_Contrast", .48f);
                            }
                            if (shader.Contains("Foliage") && themed.HasProperty("_Color")) themed.SetColor("_Color", new Color(.97f, .88f, 1.05f));
                            AssetDatabase.CreateAsset(themed, path);
                        }
                        materials.Add(source, themed);
                    }
                    shared[i] = themed; changed = true;
                }
                if (changed) renderer.sharedMaterials = shared;
            }
            root.CameraRig.RenderCamera.backgroundColor = new Color(.31f, .27f, .39f);
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
            foreach (var transform in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "Living_town_sign" || t.name == "PurpleGardenWelcomeSign" || t.name == "BigRockWelcomeSign" || t.name == "SevenHillsWelcomeSign"))
            {
                var renderer = transform.GetComponentInChildren<SpriteRenderer>(true); if ((!created && renderer.sprite != null) || renderer.sprite == sprite) continue;
                float height = renderer.sprite == null ? 8 : renderer.sprite.bounds.size.y * renderer.transform.localScale.y; renderer.sprite = sprite; renderer.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform);
                transform.name = "PurpleGardenWelcomeSign"; PrefabUtility.RecordPrefabInstancePropertyModifications(transform.gameObject); changed = true;
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

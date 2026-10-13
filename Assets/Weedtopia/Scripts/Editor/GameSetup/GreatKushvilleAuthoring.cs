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
    /// <summary>Explicit, additive city installation. Static objects are saved as prefabs and scene objects.</summary>
    public static class GreatKushvilleAuthoring
    {
        public const string City = "great_kushville", Folder = "Assets/Weedtopia/Production/GreatKushville";
        public const string ScenePath = "Assets/Weedtopia/Game/Scenes/GreatKushville.unity";
        public const string CatalogPath = Folder + "/BuildingVisuals.asset", VisualsPath = Folder + "/ResidentVisuals.asset";
        public const string StoriesPath = Folder + "/ResidentStories.prefab", PeoplePath = Folder + "/Residents.prefab";
        public const string LandmarksPath = Folder + "/Prefabs/CanalCommons.prefab", MapPath = Folder + "/DistrictMap.asset";
        private static readonly string[] People = { "gk_selma", "gk_aurel", "gk_daria", "gk_leandro", "gk_kira", "gk_benno", "gk_yuna", "gk_oskar" };
        private static readonly string[] Architecture = { "CanalAssemblyHall", "ExchangeArcade", "FoundersPromenade", "CanalKeeperWorkshop", "TerraceGardenCourt", "CivicWaterLaboratory", "CivicMusicPavilion", "CanalDispatchHouse" };
        private static readonly Vector2[] ResidentPlaces = {
            new Vector2(318, -95), new Vector2(240, -95), new Vector2(-27, -57), new Vector2(-152, 64),
            new Vector2(84, 145), new Vector2(84, -157), new Vector2(84, 214), new Vector2(-183, 82)
        };
        private static readonly string[][] Memories = {
            new[] { "Town_workshop_corner", "Prop_sign", "Town_workshop_corner", "Town_tea_patio" },
            new[] { "Town_herb_garden", "Prop_planter", "Town_reading_corner", "Town_picnic_nook" },
            new[] { "Town_produce_stall", "Prop_crates", "Town_bread_cart", "Town_tea_patio" },
            new[] { "Prop_sign", "Town_workshop_corner", "Town_flower_bicycle", "Prop_lantern" },
            new[] { "Town_laundry_garden", "Town_herb_garden", "Town_reading_corner", "Town_picnic_nook" },
            new[] { "Prop_sign", "Town_reading_corner", "Town_rowan_tree", "Town_tea_patio" },
            new[] { "Prop_lantern", "Town_tea_patio", "Town_picnic_nook", "Town_flower_bicycle" },
            new[] { "Prop_crates", "Town_workshop_corner", "Prop_sign", "Town_bread_cart" }
        };
        private static GameDefinitions definitions;

        [MenuItem("Weedtopia/Cities/Install or Repair Great Kushville")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string sub in new[] { "", "/Art", "/Materials", "/Buildings", "/Prefabs" }) Directory.CreateDirectory(Folder + sub);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                definitions = AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
                if (!definitions.cities.Any(c => c.id == City) || !definitions.vipStories.Where(p => p.cityId == City).Select(p => p.id).OrderBy(x => x).SequenceEqual(People.OrderBy(x => x)))
                    throw new InvalidOperationException("Great Kushville content and its eight inhabitants must be installed first.");
                foreach (string image in new[] { "resident_atlas.png", "architecture_atlas.png", "town_sign.png" })
                    if (!File.Exists(Folder + "/Art/" + image)) throw new FileNotFoundException("Missing Great Kushville artwork", image);
                foreach (string prop in Memories.SelectMany(x => x).Concat(new[] { "Prop_salvage", "Prop_planter", "Prop_crates", "Prop_lantern", "Town_reading_corner", "Town_produce_stall" }).Distinct()) RequirePrefab(PropPath(prop));
                var source = EditorSceneManager.OpenScene(BigRockCityAuthoring.ScenePath);
                var sourceRoot = Root(source);
                var sourceCatalog = (BuildingVisualCatalog)new SerializedObject(sourceRoot).FindProperty("productionCatalog").objectReferenceValue;
                if (sourceCatalog == null) throw new InvalidOperationException("Missing authored source building catalog.");
                AuthorBuildings(sourceCatalog);
                AuthorArchitecture();
                var visuals = AuthorResidents(); AuthorResidentPanel(visuals); AuthorResidentWorld(visuals);
                AuthorLandmarks(); AuthorMap(); ImportSign();
                var changedExisting = GreatKushvilleCampaignAuthoring.ApplyWithReport();
                bool created = !File.Exists(ScenePath);
                if (created && !AssetDatabase.CopyAsset(BigRockCityAuthoring.ScenePath, ScenePath)) throw new IOException("Could not copy the authored source city.");
                var scene = EditorSceneManager.OpenScene(ScenePath); var root = Root(scene); bool changed = false;
                if (!created && root.SceneCityId != City) throw new InvalidOperationException("Preserve independently authored scene with a different city identity.");
                if (created)
                {
                    RemapInstances(scene);
                    var data = new SerializedObject(root);
                    data.FindProperty("sceneCityId").stringValue = City;
                    data.FindProperty("productionCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(CatalogPath);
                    var oldWorld = root.StoryWorld.gameObject;
                    data.FindProperty("storyWorld").objectReferenceValue = Instance(PeoplePath, null).GetComponent<VipTownPresentation>();
                    data.ApplyModifiedPropertiesWithoutUndo(); Object.DestroyImmediate(oldWorld);
                    ReplaceStories(root);
                    foreach (var stamp in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<BigRockCityStamp>(true)).ToArray()) Object.DestroyImmediate(stamp.gameObject);
                    ThemeLandscape(scene, root); changed = true;
                }
                var rootData = new SerializedObject(root);
                if (rootData.FindProperty("productionCatalog").objectReferenceValue == null)
                { rootData.FindProperty("productionCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(CatalogPath); changed = true; }
                if (rootData.FindProperty("storyWorld").objectReferenceValue == null)
                { rootData.FindProperty("storyWorld").objectReferenceValue = Instance(PeoplePath, null).GetComponent<VipTownPresentation>(); changed = true; }
                rootData.ApplyModifiedPropertiesWithoutUndo();
                if (!scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<GreatKushvilleStamp>(true)).Any()) { Instance(LandmarksPath, null); changed = true; }
                foreach (var place in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<CityPlaceProgressVisual>(true)).Where(p => p.cityId == City))
                    if (place.root == null) { place.root = root; PrefabUtility.RecordPrefabInstancePropertyModifications(place); changed = true; }
                var cameraData = new SerializedObject(root.CameraRig);
                if (created || cameraData.FindProperty("districtMap").objectReferenceValue == null)
                { cameraData.FindProperty("districtMap").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DistrictMap>(MapPath); cameraData.ApplyModifiedPropertiesWithoutUndo(); changed = true; }
                foreach (var navigator in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<DistrictNavigator>(true)))
                {
                    var data = new SerializedObject(navigator); var label = data.FindProperty("cityLabel");
                    if (created || string.IsNullOrEmpty(label.stringValue)) { label.stringValue = "Great Kushville"; data.ApplyModifiedPropertiesWithoutUndo(); changed = true; }
                }
                changed |= WelcomeSigns(scene, created);
                changed |= GreatKushvilleCampaignAuthoring.ApplyCampaignRows(root.Ui.Campaign);
                if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                var scenes = EditorBuildSettings.scenes.ToList();
                if (!scenes.Any(s => s.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
                else if (scenes.Any(s => s.path == ScenePath && !s.enabled)) { scenes.Single(s => s.path == ScenePath).enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); }
                AssetDatabase.SaveAssets(); Directory.CreateDirectory("Docs/GreatKushville");
                File.WriteAllText("Docs/GreatKushville/authoring_receipt.json", JsonUtility.ToJson(new Receipt {
                    utc = DateTime.UtcNow.ToString("O"), createdScene = created, changedExistingCityScenes = changedExisting,
                    buildings = definitions.instances.Count(i => i.cityId == City), mainQuests = definitions.QuestsFor(City).Length,
                    routes = definitions.routes.Count(r => definitions.instances.Single(i => i.id == r.from).cityId == City)
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
            public int contentVersion = 12;
            public int buildings, routes, mainQuests, inhabitants = 8, chapters = 32, recurring = 24, landmarks = 3, architecturePrefabs = 8, buildingVariants = 251;
        }
        private static GameRoot Root(Scene scene) => scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<GameRoot>(true)).Single();
        private static string PropPath(string prop) => (prop.StartsWith("Town_", StringComparison.Ordinal) ? "Assets/Weedtopia/Production/TownVitality/Prefabs/" : "Assets/Weedtopia/Production/Prefabs/") + prop + ".prefab";
        private static void RequirePrefab(string path) { if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) throw new FileNotFoundException("Missing authored prefab", path); }
        private static GameObject Instance(string path, Transform parent)
        { RequirePrefab(path); return (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent); }

        private static void AuthorBuildings(BuildingVisualCatalog source)
        {
            if (AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(CatalogPath) != null) return;
            var shader = Shader.Find("Weedtopia/Mark Wood City/Building Palette");
            if (shader == null) throw new InvalidOperationException("Missing shared building palette shader.");
            string materialPath = Folder + "/Materials/GoldenStoneCanalJade.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "GreatKushville_GoldenStoneAndCanalJade" };
                material.SetColor("_Cedar", new Color(1.15f, 1.00f, .72f)); material.SetColor("_Forest", new Color(.45f, .94f, .78f));
                material.SetColor("_Copper", new Color(1.05f, .68f, .35f)); material.SetFloat("_Strength", .83f);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var families = new List<BuildingVisualFamily>();
            foreach (var family in source.Families)
            {
                Directory.CreateDirectory(Folder + "/Buildings/" + family.BuildingId); AssetDatabase.Refresh(); var bands = new List<BuildingVisualBand>();
                foreach (var band in family.Bands)
                {
                    string path = Folder + "/Buildings/" + family.BuildingId + "/" + band.TierId + ".prefab";
                    if (!File.Exists(path))
                    {
                        if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(band.WrapperPrefab), path)) throw new IOException("Building wrapper copy failed: " + path);
                        var prefab = PrefabUtility.LoadPrefabContents(path);
                        try { foreach (var sprite in prefab.GetComponentsInChildren<BuildingSpriteVisual>(true)) sprite.SpriteRenderer.sharedMaterial = material; PrefabUtility.SaveAsPrefabAsset(prefab, path); }
                        finally { PrefabUtility.UnloadPrefabContents(prefab); }
                    }
                    bands.Add(new BuildingVisualBand(band.TierId, band.MinLevel, band.MaxLevel, AssetDatabase.LoadAssetAtPath<GameObject>(path)));
                }
                families.Add(new BuildingVisualFamily(family.BuildingId, bands));
            }
            var catalog = ScriptableObject.CreateInstance<BuildingVisualCatalog>(); catalog.EditorSetFamilies(families); AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        // Sprite rectangles trim each atlas cell's alpha padding; the source image bytes remain untouched.
        private static Sprite[] ImportAtlas(string file, string[] names, int columns, int rows, bool portraits)
        {
            string path = Folder + "/Art/" + file;
            var existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            var expectedNames = portraits ? names.SelectMany(n => new[] { n + "_body", n + "_portrait" }).ToArray() : names;
            if (existing.Select(s => s.name).OrderBy(n => n).SequenceEqual(expectedNames.OrderBy(n => n))) return existing;
            var probe = new Texture2D(2, 2);
            try
            {
                if (!probe.LoadImage(File.ReadAllBytes(path))) throw new InvalidDataException("Invalid image: " + path);
                int width = probe.width, height = probe.height; var pixels = probe.GetPixels32(); var slices = new List<SpriteMetaData>();
                for (int i = 0; i < names.Length; i++)
                {
                    int x0 = width * (i % columns) / columns, x1 = width * (i % columns + 1) / columns;
                    int y0 = height * (rows - 1 - i / columns) / rows, y1 = height * (rows - i / columns) / rows;
                    if (y0 > 0) y0 = AlphaGap(pixels, width, y0, height / (rows * 4), 0, width, false);
                    if (y1 < height) y1 = AlphaGap(pixels, width, y1, height / (rows * 4), 0, width, false);
                    if (x0 > 0) x0 = AlphaGap(pixels, width, x0, width / (columns * 4), y0, y1, true);
                    if (x1 < width) x1 = AlphaGap(pixels, width, x1, width / (columns * 4), y0, y1, true);
                    int left = x1, right = x0, bottom = y1, top = y0;
                    for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
                        if (pixels[y * width + x].a > 24) { left = Math.Min(left, x); right = Math.Max(right, x); bottom = Math.Min(bottom, y); top = Math.Max(top, y); }
                    if (right <= left || top <= bottom) throw new InvalidDataException("Empty atlas cell: " + names[i]);
                    var rect = new Rect(left, bottom, right - left + 1, top - bottom + 1);
                    slices.Add(new SpriteMetaData { name = names[i] + (portraits ? "_body" : ""), rect = rect, alignment = 9, pivot = new Vector2(.5f, portraits ? .02f : .06f) });
                    if (portraits) slices.Add(new SpriteMetaData { name = names[i] + "_portrait", rect = new Rect(rect.x, rect.y + rect.height * .48f, rect.width, rect.height * .52f), alignment = 9, pivot = new Vector2(.5f, .5f) });
                }
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Atlas was not imported: " + path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 256; importer.alphaIsTransparency = true; importer.mipmapEnabled = true;
                importer.maxTextureSize = 4096; importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
#pragma warning disable 618
                importer.spritesheet = slices.ToArray();
#pragma warning restore 618
                importer.SaveAndReimport(); return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            }
            finally { Object.DestroyImmediate(probe); }
        }
        private static int AlphaGap(Color32[] pixels, int width, int expected, int radius, int start, int end, bool vertical)
        {
            int best = expected, fewest = int.MaxValue;
            for (int candidate = expected - radius; candidate <= expected + radius; candidate++)
            {
                int opaque = 0;
                for (int cross = start; cross < end; cross++)
                    if (pixels[vertical ? cross * width + candidate : candidate * width + cross].a > 24) opaque++;
                if (opaque < fewest || (opaque == fewest && Math.Abs(candidate - expected) < Math.Abs(best - expected)))
                { best = candidate; fewest = opaque; }
            }
            if (fewest > 0) throw new InvalidDataException("Atlas subjects overlap at a sprite boundary.");
            return best;
        }
        private static VipVisualCatalog AuthorResidents()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VipVisualCatalog>(VisualsPath); if (existing != null) return existing;
            var sprites = ImportAtlas("resident_atlas.png", People, 4, 2, true);
            var catalog = ScriptableObject.CreateInstance<VipVisualCatalog>();
            catalog.entries = People.Select(id => new VipVisualEntry { id = id, body = sprites.Single(s => s.name == id + "_body"), portrait = sprites.Single(s => s.name == id + "_portrait") }).ToArray();
            AssetDatabase.CreateAsset(catalog, VisualsPath); return catalog;
        }
        private static void AuthorResidentPanel(VipVisualCatalog visuals)
        {
            if (File.Exists(StoriesPath)) return;
            var prefab = PrefabUtility.LoadPrefabContents(BigRockCityAuthoring.StoriesPath);
            try
            {
                var ui = prefab.GetComponent<VipStoriesUi>(); ui.visuals = visuals;
                foreach (var row in ui.rows) Object.DestroyImmediate(row.gameObject);
                var rows = new List<VipStoryRosterRow>();
                foreach (string id in People)
                {
                    var row = Instance(VipStoriesAuthoring.RowPath, ui.rosterScroll.content).GetComponent<VipStoryRosterRow>();
                    row.id = id; row.portrait.sprite = visuals.Get(id).portrait; rows.Add(row);
                }
                foreach (var joint in definitions.vipCollaborations.Where(c => c.cityId == City))
                {
                    var row = Instance(VipStoriesAuthoring.RowPath, ui.rosterScroll.content).GetComponent<VipStoryRosterRow>();
                    row.id = joint.id; row.collaboration = true; row.gameObject.SetActive(false); rows.Add(row);
                }
                ui.rows = rows.ToArray(); ui.portrait.sprite = visuals.entries[0].body;
                prefab.name = "GreatKushvilleResidentStories"; PrefabUtility.SaveAsPrefabAsset(prefab, StoriesPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        private static void AuthorResidentWorld(VipVisualCatalog visuals)
        {
            if (File.Exists(PeoplePath)) return;
            var world = new GameObject("GreatKushvilleResidents", typeof(VipTownPresentation));
            try
            {
                var actors = new List<VipStoryActor>();
                for (int i = 0; i < People.Length; i++)
                {
                    string id = People[i], path = Folder + "/Prefabs/" + id + ".prefab";
                    if (!File.Exists(path))
                    {
                        var prefab = PrefabUtility.LoadPrefabContents(VipStoriesAuthoring.ActorPath);
                        try
                        {
                            var actor = prefab.GetComponent<VipStoryActor>(); actor.vipId = id; prefab.name = id;
                            actor.spriteRenderer.sprite = visuals.Get(id).body; actor.artwork.localScale = Vector3.one * (5.6f / actor.spriteRenderer.sprite.bounds.size.y);
                            var memories = new List<VipMemoryVisual>(); var person = definitions.vipStories.Single(p => p.id == id);
                            for (int chapter = 0; chapter < person.chapters.Length; chapter++) for (int choice = 0; choice < person.chapters[chapter].choices.Length; choice++)
                            {
                                var memory = id == "gk_selma" && chapter == 2
                                    ? PlaceArchitecture(person.chapters[chapter].choices[choice].id == "water_lab" ? "CivicWaterLaboratory" : "CanalKeeperWorkshop",
                                        "Memory_3_" + person.chapters[chapter].choices[choice].id, actor.visibleRoot.transform, new Vector2(10, -29), .42f)
                                    : PlaceProp(Memories[i][chapter], "Memory_" + (chapter + 1) + "_" + person.chapters[chapter].choices[choice].id,
                                        actor.visibleRoot.transform, new Vector2(10 + chapter % 2 * 19, -10 - chapter / 2 * 19), .6f);
                                memory.SetActive(false); memories.Add(new VipMemoryVisual { chapter = chapter, choiceId = person.chapters[chapter].choices.Length > 1 ? person.chapters[chapter].choices[choice].id : "", target = memory });
                            }
                            actor.memories = memories.ToArray(); PrefabUtility.SaveAsPrefabAsset(prefab, path);
                        }
                        finally { PrefabUtility.UnloadPrefabContents(prefab); }
                    }
                    var instance = Instance(path, world.transform); instance.transform.localPosition = DistrictMap.ToWorld(ResidentPlaces[i]); actors.Add(instance.GetComponent<VipStoryActor>());
                }
                world.GetComponent<VipTownPresentation>().actors = actors.ToArray(); PrefabUtility.SaveAsPrefabAsset(world, PeoplePath);
            }
            finally { Object.DestroyImmediate(world); }
        }
        private static void Depth(GameObject target)
        {
            foreach (var sprite in target.GetComponentsInChildren<SpriteRenderer>(true))
                if (sprite.gameObject.name != "ContactShadow" && sprite.GetComponent<CityLandmarkVisual>() == null) sprite.gameObject.AddComponent<CityLandmarkVisual>();
        }
        private static GameObject PlaceProp(string prop, string name, Transform parent, Vector2 position, float scale = 1)
        {
            var instance = Instance(PropPath(prop), parent); instance.name = name; instance.transform.localPosition = DistrictMap.ToWorld(position);
            instance.transform.localScale *= scale; Depth(instance); return instance;
        }
        private static void AuthorArchitecture()
        {
            var sprites = ImportAtlas("architecture_atlas.png", Architecture, 4, 2, false);
            foreach (string name in Architecture)
            {
                string path = Folder + "/Prefabs/" + name + ".prefab";
                if (File.Exists(path))
                {
                    var existing = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        if (AuthorArchitectureGroundAnchor(existing)) PrefabUtility.SaveAsPrefabAsset(existing, path);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(existing); }
                    continue;
                }
                var prefab = new GameObject(name);
                try
                {
                    var artwork = new GameObject("Artwork", typeof(SpriteRenderer), typeof(CityLandmarkVisual));
                    artwork.transform.SetParent(prefab.transform, false);
                    var sprite = sprites.Single(s => s.name == name); artwork.GetComponent<SpriteRenderer>().sprite = sprite;
                    prefab.transform.localScale = Vector3.one * (34 / sprite.bounds.size.x); Depth(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally { Object.DestroyImmediate(prefab); }
            }
        }
        private static bool AuthorArchitectureGroundAnchor(GameObject prefab)
        {
            var source = prefab.GetComponent<SpriteRenderer>();
            if (source == null)
            {
                var artwork = prefab.transform.Find("Artwork");
                if (artwork == null || artwork.GetComponent<SpriteRenderer>() == null || artwork.GetComponent<CityLandmarkVisual>() == null)
                    throw new InvalidOperationException("Preserve independently changed architecture hierarchy: " + prefab.name);
                return false;
            }
            if (prefab.transform.childCount != 0 || prefab.GetComponent<CityLandmarkVisual>() == null)
                throw new InvalidOperationException("Preserve independently changed architecture renderer: " + prefab.name);
            // CityLandmarkVisual sorts the renderer's parent. Keep the serialized root as its own ground anchor.
            var child = new GameObject("Artwork", typeof(SpriteRenderer), typeof(CityLandmarkVisual));
            child.transform.SetParent(prefab.transform, false);
            var target = child.GetComponent<SpriteRenderer>();
            target.sprite = source.sprite; target.sharedMaterials = source.sharedMaterials; target.color = source.color;
            target.flipX = source.flipX; target.flipY = source.flipY; target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder; target.maskInteraction = source.maskInteraction;
            Object.DestroyImmediate(prefab.GetComponent<CityLandmarkVisual>());
            Object.DestroyImmediate(source);
            return true;
        }
        private static GameObject PlaceArchitecture(string architecture, string name, Transform parent, Vector2 position, float scale = 1)
        {
            var instance = Instance(Folder + "/Prefabs/" + architecture + ".prefab", parent);
            instance.name = name; instance.transform.localPosition = DistrictMap.ToWorld(position); instance.transform.localScale *= scale;
            return instance;
        }
        private static void AuthorLandmarks()
        {
            if (File.Exists(LandmarksPath))
            {
                var existing = PrefabUtility.LoadPrefabContents(LandmarksPath);
                try
                {
                    if (AuthorRestorationStages(existing)) PrefabUtility.SaveAsPrefabAsset(existing, LandmarksPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(existing); }
                return;
            }
            var world = new GameObject("GreatKushvilleCanalCommons", typeof(GreatKushvilleStamp));
            try
            {
                var projectIds = new[] { "gk_joint_water_compact", "gk_joint_shared_exchange", "gk_joint_founders_walk" };
                var placeIds = new[] { "gk_water_compact", "gk_shared_exchange", "gk_founders_walk" };
                var positions = new[] { new Vector2(112, -90), new Vector2(-63, -167), new Vector2(-173, -42) };
                for (int index = 0; index < projectIds.Length; index++)
                {
                    var project = definitions.vipCollaborations.Single(c => c.cityId == City && c.id == projectIds[index]);
                    var group = new GameObject(placeIds[index], typeof(CityPlaceProgressVisual)); group.transform.SetParent(world.transform, false);
                    group.transform.localPosition = DistrictMap.ToWorld(positions[index]); var place = group.GetComponent<CityPlaceProgressVisual>();
                    place.cityId = City; place.collaborationId = project.id; place.residents = project.quest.residents; place.keepGrowingPlaceAfterCompletion = false;
                    var closed = new GameObject("ClosedCommunityPlace"); closed.transform.SetParent(group.transform, false);
                    PlaceProp("Prop_salvage", "BeforeNeighboursRestoreThisPlace", closed.transform, Vector2.zero);
                    place.closedPlace = closed;
                    var growing = new GameObject("NeighboursReconnectingTheCanalCity"); growing.transform.SetParent(group.transform, false);
                    PlaceProp("Prop_crates", "SharedRestorationSupplies", growing.transform, new Vector2(-7, -5));
                    PlaceProp("Prop_lantern", "NeighboursEveningWorkLight", growing.transform, new Vector2(9, 1));
                    growing.SetActive(false); place.growingPlace = growing;
                    var completed = new GameObject("PermanentCommunityPlace"); completed.transform.SetParent(group.transform, false);
                    PlaceArchitecture(Architecture[index], "Restored_" + Architecture[index], completed.transform, new Vector2(-7, 8), .82f);
                    PlaceProp(index == 0 ? "Town_workshop_corner" : index == 1 ? "Town_produce_stall" : "Town_reading_corner",
                        "NeighboursGatheringSpace", completed.transform, new Vector2(17, -12), .85f);
                    completed.SetActive(false); place.completedPlace = completed;
                    if (index == 0)
                    {
                        place.choiceResidentId = "gk_selma"; place.choiceChapter = 2;
                        place.choiceValues = new[] { "water_lab", "canal_workshop" };
                        var laboratory = PlaceArchitecture("CivicWaterLaboratory", "Selma_WaterLaboratory", group.transform, new Vector2(0, -30), .66f);
                        var workshop = PlaceArchitecture("CanalKeeperWorkshop", "Selma_CanalWorkshop", group.transform, new Vector2(0, -30), .66f);
                        laboratory.SetActive(false); workshop.SetActive(false); place.choicePlaces = new[] { laboratory, workshop };
                    }
                }
                var gardens = new GameObject("gk_kushgaerten"); gardens.transform.SetParent(world.transform, false);
                foreach (var position in new[] { new Vector2(205, -126), new Vector2(369, -126), new Vector2(459, -121) })
                    PlaceProp("Town_herb_garden", "HistoricCanalGardenBed", gardens.transform, position, 1.10f);
                PlaceArchitecture("TerraceGardenCourt", "AurelsCanalGardenConservatory", gardens.transform, new Vector2(280, -110), .88f);
                var market = new GameObject("gk_kanalmarkt"); market.transform.SetParent(world.transform, false);
                PlaceProp("Town_produce_stall", "DariasExchangeStall", market.transform, new Vector2(-80, -72), .95f);
                PlaceProp("Town_bread_cart", "OpenCanalBreadCart", market.transform, new Vector2(72, -81), .8f);
                PlaceArchitecture("CanalDispatchHouse", "OskarsGoodsDispatchOffice", market.transform, new Vector2(64, -201), .8f);
                var homes = new GameObject("gk_terrassenhoefe"); homes.transform.SetParent(world.transform, false);
                PlaceProp("Town_laundry_garden", "KirasSharedTerraceGarden", homes.transform, new Vector2(80, 179), .95f);
                PlaceArchitecture("CivicMusicPavilion", "YunasOpenMusicPavilion", homes.transform, new Vector2(126, 292), .88f);
                PlaceProp("Town_rowan_tree", "TerraceNeighboursMemoryTree", homes.transform, new Vector2(169, 383), 1.0f);
                var founders = new GameObject("gk_gruenderbogen"); founders.transform.SetParent(world.transform, false);
                PlaceArchitecture("CanalKeeperWorkshop", "LeandrosEverydayCanalRepairShed", founders.transform, new Vector2(-222, 71), .78f);
                PlaceProp("Town_workshop_corner", "SharedCanalRepairTools", founders.transform, new Vector2(-225, 116), .85f);
                PlaceProp("Town_reading_corner", "BennosPublicCanalArchive", founders.transform, new Vector2(-120, 94), .85f);
                AuthorRestorationStages(world);
                PrefabUtility.SaveAsPrefabAsset(world, LandmarksPath);
            }
            finally { Object.DestroyImmediate(world); }
        }
        private static bool AuthorRestorationStages(GameObject world)
        {
            const string fencePath = "Assets/Weedtopia/Production/Construction/Prefabs/ConstructionFence.prefab";
            RequirePrefab(fencePath);
            var ids = new[] { "gk_joint_water_compact", "gk_joint_shared_exchange", "gk_joint_founders_walk" };
            bool changed = false;
            foreach (var place in world.GetComponentsInChildren<CityPlaceProgressVisual>(true).Where(p => p.cityId == City))
            {
                int index = Array.IndexOf(ids, place.collaborationId);
                if (index < 0 || place.closedPlace == null || place.growingPlace == null)
                    throw new InvalidOperationException("Unknown or incomplete Great Kushville restoration place.");
                if (place.closedPlace.name != "ClosedCommunityPlace")
                {
                    if (place.closedPlace.name != "BeforeNeighboursRestoreThisPlace")
                        throw new InvalidOperationException("Preserve independently authored closed place: " + place.collaborationId);
                    var salvage = place.closedPlace;
                    var closed = new GameObject("ClosedCommunityPlace");
                    closed.transform.SetParent(place.transform, false);
                    closed.SetActive(salvage.activeSelf);
                    salvage.transform.SetParent(closed.transform, true);
                    salvage.SetActive(true);
                    place.closedPlace = closed;
                    changed = true;
                }
                changed |= AuthorRestorationStage(place.closedPlace, Architecture[index], true, fencePath);
                changed |= AuthorRestorationStage(place.growingPlace, Architecture[index], false, fencePath);
            }
            return changed;
        }
        private static bool AuthorRestorationStage(GameObject stage, string architecture, bool closed, string fencePath)
        {
            bool changed = false;
            if (stage.transform.Find("RestorationArchitecture") == null)
            {
                PlaceArchitecture(architecture, "RestorationArchitecture", stage.transform, new Vector2(-7, 8), .82f);
                changed = true;
            }
            var expectedTint = closed ? new Color(.48f, .56f, .58f, 1) : new Color(.78f, .84f, .79f, 1);
            foreach (var renderer in stage.transform.Find("RestorationArchitecture").GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.color != expectedTint)
                {
                    renderer.color = expectedTint;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    changed = true;
                }
            if (stage.transform.Find("RestorationEnclosure") == null)
            {
                var enclosure = Instance(fencePath, stage.transform);
                enclosure.name = "RestorationEnclosure";
                enclosure.transform.localPosition = DistrictMap.ToWorld(new Vector2(-7, 8));
                var fence = enclosure.GetComponent<ConstructionFenceView>();
                if (fence == null) throw new InvalidOperationException("Missing reusable construction fence presentation.");
                fence.Present(true, 28, null);
                changed = true;
            }
            if (closed)
                changed |= EnlargeRestorationProp(stage.transform, "BeforeNeighboursRestoreThisPlace", "Prop_salvage", 3);
            else
            {
                changed |= EnlargeRestorationProp(stage.transform, "SharedRestorationSupplies", "Prop_crates", 3);
                changed |= EnlargeRestorationProp(stage.transform, "NeighboursEveningWorkLight", "Prop_lantern", 1.4f);
                if (stage.transform.Find("SharedRestorationWorkbench") == null)
                {
                    PlaceProp("Town_workshop_corner", "SharedRestorationWorkbench", stage.transform, new Vector2(17, -12), 1.25f);
                    changed = true;
                }
            }
            return changed;
        }
        private static bool EnlargeRestorationProp(Transform stage, string name, string prop, float factor)
        {
            var target = stage.Find(name);
            if (target == null) throw new InvalidOperationException("Missing authored restoration supply: " + name);
            Vector3 original = AssetDatabase.LoadAssetAtPath<GameObject>(PropPath(prop)).transform.localScale;
            Vector3 expected = original * factor;
            if (Vector3.Distance(target.localScale, expected) < .00001f) return false;
            if (Vector3.Distance(target.localScale, original) > .00001f)
                throw new InvalidOperationException("Preserve independently adjusted restoration supply: " + name);
            target.localScale = expected;
            return true;
        }
        private static void AuthorMap()
        {
            if (File.Exists(MapPath)) return;
            if (!AssetDatabase.CopyAsset(BigRockCityAuthoring.Folder + "/DistrictMap.asset", MapPath)) throw new IOException("District map copy failed.");
            var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(MapPath);
            foreach (var range in map.districts)
            {
                string suffix = range.zone == Zone.Farm ? "farm" : range.zone == Zone.City ? "city" : range.zone == Zone.Settlement ? "settlement" : "industry";
                range.labelKey = "district.great_kushville." + suffix;
                range.label = definitions.Text(range.labelKey);
            }
            EditorUtility.SetDirty(map);
        }
        private static void RemapInstances(Scene scene)
        {
            var instances = definitions.instances.Where(i => i.cityId == "big_rock_city").Select(i => i.id).ToHashSet();
            var ids = instances.Concat(definitions.routes.Where(r => instances.Contains(r.from)).Select(r => r.id)).ToHashSet();
            foreach (var transform in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)))
            {
                if (transform.name.StartsWith("BigRock", StringComparison.Ordinal)) transform.name = transform.name.Replace("BigRock", "GreatKushville");
                foreach (var component in transform.GetComponents<MonoBehaviour>().Where(c => c != null))
                {
                    var data = new SerializedObject(component); var property = data.GetIterator(); bool changed = false;
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.String && ids.Contains(property.stringValue) &&
                            (property.name == "instanceId" || property.name == "workplaceId" || property.name == "requiredBuilding" || property.name == "routeId"))
                        { property.stringValue = "gk_" + property.stringValue.Substring(3); changed = true; }
                    if (changed) data.ApplyModifiedPropertiesWithoutUndo();
                }
                var text = transform.GetComponent<Text>(); if (text != null && text.text.Contains("Big Rock City")) text.text = text.text.Replace("Big Rock City", "Great Kushville");
            }
        }
        private static void ReplaceStories(GameRoot root)
        {
            var previous = root.Ui.Stories.gameObject; var panel = Instance(StoriesPath, previous.transform.parent).GetComponent<VipStoriesUi>(); panel.gameObject.SetActive(false);
            var data = new SerializedObject(root.Ui); data.FindProperty("stories").objectReferenceValue = panel;
            var panels = data.FindProperty("panels"); for (int i = 0; i < panels.arraySize; i++) if (panels.GetArrayElementAtIndex(i).objectReferenceValue == previous) panels.GetArrayElementAtIndex(i).objectReferenceValue = panel.gameObject;
            var bindings = data.FindProperty("bindings"); for (int i = 0; i < bindings.arraySize; i++)
            { var target = bindings.GetArrayElementAtIndex(i).FindPropertyRelative("target"); if (target.objectReferenceValue == previous) target.objectReferenceValue = panel.gameObject; }
            data.ApplyModifiedPropertiesWithoutUndo(); Object.DestroyImmediate(previous);
        }
        private static void ThemeLandscape(Scene scene, GameRoot root)
        {
            var materials = new Dictionary<Material, Material>();
            foreach (var renderer in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Renderer>(true)))
            {
                var shared = renderer.sharedMaterials; bool changed = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    var source = shared[i]; if (source?.shader == null) continue; string shader = source.shader.name;
                    if (!shader.Contains("NaturalGround") && !shader.Contains("FeatheredGround") && !shader.Contains("AtlasGround") && !shader.Contains("LivingWater") && !shader.Contains("LivingFoliage")) continue;
                    if (!materials.TryGetValue(source, out var themed))
                    {
                        string path = Folder + "/Materials/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)) + ".mat"; themed = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (themed == null)
                        {
                            themed = new Material(source) { name = "GreatKushville_" + source.name };
                            if (shader.Contains("Ground"))
                            { if (themed.HasProperty("_BaseColor")) themed.SetColor("_BaseColor", new Color(.44f, .52f, .31f)); if (themed.HasProperty("_Tint")) themed.SetColor("_Tint", new Color(1.06f, 1.01f, .83f)); }
                            if (shader.Contains("LivingWater") && themed.HasProperty("_Tint")) themed.SetColor("_Tint", new Color(.43f, .89f, .81f));
                            if (shader.Contains("Foliage") && themed.HasProperty("_Color")) themed.SetColor("_Color", new Color(.85f, 1.07f, .86f));
                            AssetDatabase.CreateAsset(themed, path);
                        }
                        materials.Add(source, themed);
                    }
                    shared[i] = themed; changed = true;
                }
                if (changed) renderer.sharedMaterials = shared;
            }
            root.CameraRig.RenderCamera.backgroundColor = new Color(.25f, .43f, .36f);
        }
        private static void ImportSign()
        {
            var importer = AssetImporter.GetAtPath(Folder + "/Art/town_sign.png") as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Great Kushville welcome sign was not imported.");
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
                importer.alphaIsTransparency && importer.spritePixelsPerUnit == 256 && importer.npotScale == TextureImporterNPOTScale.None) return;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = true; importer.spritePixelsPerUnit = 256;
            importer.maxTextureSize = 2048; importer.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteAlignment = 9; settings.spritePivot = new Vector2(.5f, .06f); importer.SetTextureSettings(settings); importer.SaveAndReimport();
        }
        private static bool WelcomeSigns(Scene scene, bool created)
        {
            bool changed = false; var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Art/town_sign.png");
            foreach (var transform in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "Living_town_sign" || t.name == "GreatKushvilleWelcomeSign" || t.name == "BigRockWelcomeSign"))
            {
                var renderer = transform.GetComponentInChildren<SpriteRenderer>(true); if (!created && renderer.sprite != null) continue;
                float height = renderer.sprite == null ? 8 : renderer.sprite.bounds.size.y * renderer.transform.localScale.y;
                renderer.sprite = sprite; renderer.transform.localScale = Vector3.one * (height / sprite.bounds.size.y); transform.name = "GreatKushvilleWelcomeSign";
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform); changed = true;
            }
            return changed;
        }
    }
}

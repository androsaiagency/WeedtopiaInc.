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
    public static class GoldenFlowerAuthoring
    {
        public const string City = "golden_flower", Folder = "Assets/Weedtopia/Production/GoldenFlower";
        public const string ScenePath = "Assets/Weedtopia/Game/Scenes/GoldenFlower.unity";
        public const string CatalogPath = Folder + "/BuildingVisuals.asset", VisualsPath = Folder + "/ResidentVisuals.asset";
        public const string StoriesPath = Folder + "/ResidentStories.prefab", PeoplePath = Folder + "/Residents.prefab";
        public const string LandmarksPath = Folder + "/Prefabs/GoldenCommons.prefab", MapPath = Folder + "/DistrictMap.asset";
        private static readonly string[] People = { "gf_alma", "gf_ida", "gf_milan", "gf_nora", "gf_theo" };
        private static readonly string[] Architecture = { "GoldenGlasshouse", "SeedHouse", "CommunityMarket", "FestivalBandstand" };
        private static readonly Vector2[] ResidentPlaces = { new Vector2(240, -95), new Vector2(-152, 64), new Vector2(-27, -57), new Vector2(-28, 14), new Vector2(84, 14) };
        private static readonly string[][] Memories = {
            new[] { "Prop_sign", "Town_herb_garden", "Town_rowan_tree" },
            new[] { "Prop_crates", "Town_workshop_corner", "Prop_lantern" },
            new[] { "Town_bread_cart", "Town_tea_patio", "Town_produce_stall" },
            new[] { "Prop_sign", "Town_reading_corner", "Town_reading_corner" },
            new[] { "Prop_lantern", "Town_picnic_nook", "Town_tea_patio" }
        };
        private static GameDefinitions definitions;

        [MenuItem("Weedtopia/Cities/Install or Repair Golden Flower")]
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
                    throw new InvalidOperationException("Golden Flower content and its five inhabitants must be installed first.");
                foreach (string image in new[] { "resident_atlas.png", "architecture_atlas.png", "town_sign.png" })
                    if (!File.Exists(Folder + "/Art/" + image)) throw new FileNotFoundException("Missing Golden Flower artwork", image);
                foreach (string prop in Memories.SelectMany(x => x).Concat(new[] { "Prop_salvage", "Prop_planter" }).Distinct()) RequirePrefab(PropPath(prop));
                var source = EditorSceneManager.OpenScene(BigRockCityAuthoring.ScenePath);
                var sourceRoot = Root(source);
                var sourceCatalog = (BuildingVisualCatalog)new SerializedObject(sourceRoot).FindProperty("productionCatalog").objectReferenceValue;
                AuthorBuildings(sourceCatalog);
                var visuals = AuthorResidents(); AuthorResidentPanel(visuals); AuthorResidentWorld(visuals);
                AuthorArchitecture(); AuthorLandmarks(); AuthorMap(); ImportSign();
                var changedExisting = GoldenFlowerCampaignAuthoring.ApplyWithReport();
                bool created = !File.Exists(ScenePath);
                if (created && !AssetDatabase.CopyAsset(BigRockCityAuthoring.ScenePath, ScenePath)) throw new IOException("Could not copy the authored source city.");
                var scene = EditorSceneManager.OpenScene(ScenePath); var root = Root(scene); bool changed = false;
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
                if (!scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<GoldenFlowerStamp>(true)).Any()) { Instance(LandmarksPath, null); changed = true; }
                foreach (var place in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<CityPlaceProgressVisual>(true)).Where(p => p.cityId == City))
                    if (place.root == null) { place.root = root; PrefabUtility.RecordPrefabInstancePropertyModifications(place); changed = true; }
                var cameraData = new SerializedObject(root.CameraRig);
                if (created || cameraData.FindProperty("districtMap").objectReferenceValue == null)
                { cameraData.FindProperty("districtMap").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DistrictMap>(MapPath); cameraData.ApplyModifiedPropertiesWithoutUndo(); changed = true; }
                foreach (var navigator in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<DistrictNavigator>(true)))
                {
                    var data = new SerializedObject(navigator); var label = data.FindProperty("cityLabel");
                    if (created || string.IsNullOrEmpty(label.stringValue)) { label.stringValue = "Golden Flower"; data.ApplyModifiedPropertiesWithoutUndo(); changed = true; }
                }
                changed |= WelcomeSigns(scene, created);
                changed |= GoldenFlowerCampaignAuthoring.ApplyCampaignRows(root.Ui.Campaign);
                if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                var scenes = EditorBuildSettings.scenes.ToList();
                if (!scenes.Any(s => s.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
                else if (scenes.Any(s => s.path == ScenePath && !s.enabled)) { scenes.Single(s => s.path == ScenePath).enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); }
                AssetDatabase.SaveAssets(); Directory.CreateDirectory("Docs/GoldenFlower");
                File.WriteAllText("Docs/GoldenFlower/authoring_receipt.json", JsonUtility.ToJson(new Receipt {
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
            public int buildings, routes, mainQuests, inhabitants = 5, chapters = 15, recurring = 15, landmarks = 4, architecturePrefabs = 4, buildingVariants = 251;
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
            string materialPath = Folder + "/Materials/GoldenSandstone.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "GoldenFlower_SandstoneAndBrass" };
                material.SetColor("_Cedar", new Color(1.12f, 1.03f, .82f)); material.SetColor("_Forest", new Color(.81f, .89f, .47f));
                material.SetColor("_Copper", new Color(1.08f, .84f, .38f)); material.SetFloat("_Strength", .78f);
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
            var sprites = ImportAtlas("resident_atlas.png", People, 5, 1, true);
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
                prefab.name = "GoldenFlowerResidentStories"; PrefabUtility.SaveAsPrefabAsset(prefab, StoriesPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        private static void AuthorResidentWorld(VipVisualCatalog visuals)
        {
            if (File.Exists(PeoplePath)) return;
            var world = new GameObject("GoldenFlowerResidents", typeof(VipTownPresentation));
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
                                var memory = PlaceProp(Memories[i][chapter], "Memory_" + chapter + "_" + choice, actor.visibleRoot.transform, new Vector2(10 + chapter % 2 * 19, -10 - chapter / 2 * 19), .6f);
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
            var sprites = ImportAtlas("architecture_atlas.png", Architecture, 2, 2, false);
            foreach (string name in Architecture)
            {
                string path = Folder + "/Prefabs/" + name + ".prefab"; if (File.Exists(path)) continue;
                var prefab = new GameObject(name, typeof(SpriteRenderer));
                try
                {
                    var sprite = sprites.Single(s => s.name == name); prefab.GetComponent<SpriteRenderer>().sprite = sprite;
                    prefab.transform.localScale = Vector3.one * (34 / sprite.bounds.size.x); Depth(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally { Object.DestroyImmediate(prefab); }
            }
        }
        private static void AuthorLandmarks()
        {
            if (File.Exists(LandmarksPath)) return;
            var world = new GameObject("GoldenFlowerCommons", typeof(GoldenFlowerStamp));
            try
            {
                var projects = definitions.vipCollaborations.Where(c => c.cityId == City).ToArray();
                if (projects.Length != 4) throw new InvalidOperationException("Golden Flower requires four permanent city projects.");
                var positions = new[] { new Vector2(112, -90), new Vector2(280, -110), new Vector2(56, -106), new Vector2(94, 292) };
                var architecture = new[] { 1, 0, 2, 3 };
                for (int i = 0; i < projects.Length; i++)
                {
                    var group = new GameObject(projects[i].id, typeof(CityPlaceProgressVisual)); group.transform.SetParent(world.transform, false);
                    group.transform.localPosition = DistrictMap.ToWorld(positions[i]); var place = group.GetComponent<CityPlaceProgressVisual>();
                    place.cityId = City; place.collaborationId = projects[i].id; place.residents = projects[i].quest.residents; place.keepGrowingPlaceAfterCompletion = false;
                    place.closedPlace = PlaceProp("Prop_salvage", "BeforeRestoration", group.transform, Vector2.zero, 1);
                    var growing = new GameObject("CommunityWorkInProgress"); growing.transform.SetParent(group.transform, false);
                    PlaceProp("Prop_crates", "SharedSupplies", growing.transform, new Vector2(-7, -5));
                    PlaceProp("Prop_planter", "FirstGoldenFlowers", growing.transform, new Vector2(9, 1)); growing.SetActive(false); place.growingPlace = growing;
                    var completed = new GameObject("PermanentCommunityPlace"); completed.transform.SetParent(group.transform, false);
                    Instance(Folder + "/Prefabs/" + Architecture[architecture[i]] + ".prefab", completed.transform);
                    PlaceProp(i == 1 ? "Town_herb_garden" : i == 3 ? "Town_picnic_nook" : "Prop_planter", "NeighbourhoodLife", completed.transform, new Vector2(19, -11), .7f);
                    completed.SetActive(false); place.completedPlace = completed;
                }
                foreach (var position in new[] { new Vector2(205, -126), new Vector2(369, -126), new Vector2(459, -121), new Vector2(-222, 26), new Vector2(-225, 116), new Vector2(169, 383) })
                    PlaceProp("Town_herb_garden", "GoldenTerraceBed", world.transform, position, .85f);
                PrefabUtility.SaveAsPrefabAsset(world, LandmarksPath);
            }
            finally { Object.DestroyImmediate(world); }
        }
        private static void AuthorMap()
        {
            if (File.Exists(MapPath)) return;
            if (!AssetDatabase.CopyAsset(BigRockCityAuthoring.Folder + "/DistrictMap.asset", MapPath)) throw new IOException("District map copy failed.");
            var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(MapPath);
            foreach (var range in map.districts)
            {
                string suffix = range.zone == Zone.Farm ? "farm" : range.zone == Zone.City ? "city" : range.zone == Zone.Settlement ? "settlement" : "industry";
                range.labelKey = "district.golden_flower." + suffix;
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
                if (transform.name.StartsWith("BigRock", StringComparison.Ordinal)) transform.name = transform.name.Replace("BigRock", "GoldenFlower");
                foreach (var component in transform.GetComponents<MonoBehaviour>().Where(c => c != null))
                {
                    var data = new SerializedObject(component); var property = data.GetIterator(); bool changed = false;
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.String && ids.Contains(property.stringValue) &&
                            (property.name == "instanceId" || property.name == "workplaceId" || property.name == "requiredBuilding" || property.name == "routeId"))
                        { property.stringValue = "gf_" + property.stringValue.Substring(3); changed = true; }
                    if (changed) data.ApplyModifiedPropertiesWithoutUndo();
                }
                var text = transform.GetComponent<Text>(); if (text != null && text.text.Contains("Big Rock City")) text.text = text.text.Replace("Big Rock City", "Golden Flower");
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
                            themed = new Material(source) { name = "GoldenFlower_" + source.name };
                            if (shader.Contains("Ground"))
                            { if (themed.HasProperty("_BaseColor")) themed.SetColor("_BaseColor", new Color(.51f, .48f, .32f)); if (themed.HasProperty("_Tint")) themed.SetColor("_Tint", new Color(1.07f, 1.02f, .86f)); }
                            if (shader.Contains("Foliage") && themed.HasProperty("_Color")) themed.SetColor("_Color", new Color(1.03f, 1.01f, .78f));
                            AssetDatabase.CreateAsset(themed, path);
                        }
                        materials.Add(source, themed);
                    }
                    shared[i] = themed; changed = true;
                }
                if (changed) renderer.sharedMaterials = shared;
            }
            root.CameraRig.RenderCamera.backgroundColor = new Color(.36f, .37f, .26f);
        }
        private static void ImportSign()
        {
            var importer = AssetImporter.GetAtPath(Folder + "/Art/town_sign.png") as TextureImporter;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = true; importer.spritePixelsPerUnit = 256;
            importer.maxTextureSize = 2048; importer.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteAlignment = 9; settings.spritePivot = new Vector2(.5f, .06f); importer.SetTextureSettings(settings); importer.SaveAndReimport();
        }
        private static bool WelcomeSigns(Scene scene, bool created)
        {
            bool changed = false; var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Art/town_sign.png");
            foreach (var transform in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "Living_town_sign" || t.name == "GoldenFlowerWelcomeSign" || t.name == "BigRockWelcomeSign"))
            {
                var renderer = transform.GetComponentInChildren<SpriteRenderer>(true); if (!created && renderer.sprite != null) continue;
                float height = renderer.sprite == null ? 8 : renderer.sprite.bounds.size.y * renderer.transform.localScale.y;
                renderer.sprite = sprite; renderer.transform.localScale = Vector3.one * (height / sprite.bounds.size.y); transform.name = "GoldenFlowerWelcomeSign";
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform); changed = true;
            }
            return changed;
        }
    }
}

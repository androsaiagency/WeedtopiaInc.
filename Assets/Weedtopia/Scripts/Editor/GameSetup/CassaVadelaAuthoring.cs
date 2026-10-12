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
    /// Explicit additive fifth-city authoring. The current second-city scene supplies shared shop, localization,
    /// mobile controls and world contracts. Earlier city installers are deliberately never invoked here.
    /// </summary>
    public static class CassaVadelaAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/CassaVadela";
        public const string ScenePath = "Assets/Weedtopia/Game/Scenes/CassaVadela.unity";
        public const string CatalogPath = Folder + "/BuildingVisuals.asset";
        public const string StoriesPath = Folder + "/ResidentStories.prefab";
        public const string PeoplePath = Folder + "/Residents.prefab";
        public const string VisualsPath = Folder + "/ResidentVisuals.asset";
        public const string LandmarksPath = Folder + "/Prefabs/TerracePlaces.prefab";
        public const string MapPath = Folder + "/DistrictMap.asset";
        public const string DryFountainPath = Folder + "/Prefabs/DryFountain.prefab";
        private const string City = "cassa_vadela";
        private static GameDefinitions definitions;
        private static readonly Dictionary<string, Vector2> Places = new Dictionary<string, Vector2>
        {
            {"cv_ines", new Vector2(-38,116)}, {"cv_mateo", new Vector2(-29,14)},
            {"cv_lucia", new Vector2(18,-82)}, {"cv_rafael", new Vector2(18,-184)},
            {"cv_sofia", new Vector2(14,182)}, {"cv_elio", new Vector2(5,-33)},
            {"cv_amara", new Vector2(-38,300)}, {"cv_nico", new Vector2(30,12)}
        };

        [MenuItem("Weedtopia/Cities/Install or Repair Cassa Vadela")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            CassaVadelaCampaignAuthoring.Apply();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string sub in new[] { "", "/Materials", "/Buildings", "/Prefabs" }) Directory.CreateDirectory(Folder + sub);
                AssetDatabase.Refresh();
                definitions = AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
                if (definitions.cities.All(c => c.id != "cassa_vadela")) throw new InvalidOperationException("Cassa Vadela content must be installed before scene authoring.");
                var sourceScene = EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var source = FindRoot(sourceScene);
                var sourceCatalog = (BuildingVisualCatalog)new SerializedObject(source).FindProperty("productionCatalog").objectReferenceValue;
                Buildings(sourceCatalog);
                var visuals = Residents();
                ResidentPanel(visuals); RepairResidentMemoryAssets(); ResidentWorld(visuals); Landmarks(); Districts(); ImportSingle(Folder + "/Art/town_sign.png", .06f);
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
                    data.FindProperty("sceneCityId").stringValue = "cassa_vadela";
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
                // Repair only missing Cassa Vadela-owned assets/references. Authored/manual objects retain identity.
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
                if (!cityScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CityPlaceProgressVisual>(true)).Any(place => place.cityId == City))
                { Instance(LandmarksPath, null); changed = true; }
                changed |= MapAndLabels(root, cityScene, created);
                foreach (var place in cityScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CityPlaceProgressVisual>(true)))
                    if (place.root != root) { place.root = root; PrefabUtility.RecordPrefabInstancePropertyModifications(place); changed = true; }
                changed |= WelcomeSigns(cityScene, created);
                changed |= CassaVadelaCampaignAuthoring.ApplyCampaignRows(root.Ui.Campaign);
                if (changed) { EditorSceneManager.MarkSceneDirty(cityScene); EditorSceneManager.SaveScene(cityScene); }
                var scenes = EditorBuildSettings.scenes.ToList();
                if (!scenes.Any(s => s.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
                else if (scenes.Any(s => s.path == ScenePath && !s.enabled)) { scenes.Single(s => s.path == ScenePath).enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); }
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Docs/CassaVadela");
                File.WriteAllText("Docs/CassaVadela/authoring_receipt.json", JsonUtility.ToJson(new Receipt
                {
                    utc = DateTime.UtcNow.ToString("O"), createdScene = created, changedExistingCityScenes = Array.Empty<string>(),
                    buildings = definitions.instances.Count(i => i.cityId == "cassa_vadela"),
                    routes = definitions.routes.Count(r => definitions.instances.Single(i => i.id == r.from).cityId == "cassa_vadela"),
                    mainQuests = definitions.QuestsFor("cassa_vadela").Length
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
            var shader = Shader.Find("Weedtopia/Cassa Vadela/Building Palette");
            if (shader == null) throw new InvalidOperationException("Missing Cassa Vadela building shader.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/SandstoneTerracotta.mat");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, Folder + "/Materials/SandstoneTerracotta.mat"); }
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
            foreach (var person in definitions.vipStories.Where(p => p.cityId == "cassa_vadela"))
            {
                string path = Folder + "/Art/" + person.id + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing Cassa Vadela resident artwork " + path);
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
                foreach (var person in definitions.vipStories.Where(p => p.cityId == "cassa_vadela"))
                {
                    var row = Instance(VipStoriesAuthoring.RowPath, ui.rosterScroll.content).GetComponent<VipStoryRosterRow>();
                    row.id = person.id; row.portrait.sprite = visuals.Get(person.id).portrait; rows.Add(row);
                }
                foreach (var collaboration in definitions.vipCollaborations.Where(p => p.cityId == "cassa_vadela"))
                {
                    var row = Instance(VipStoriesAuthoring.RowPath, ui.rosterScroll.content).GetComponent<VipStoryRosterRow>();
                    row.id = collaboration.id; row.collaboration = true; row.gameObject.SetActive(false); rows.Add(row);
                }
                ui.rows = rows.ToArray(); ui.portrait.sprite = visuals.entries[0].body;
                panel.name = "CassaVadelaResidentStories"; PrefabUtility.SaveAsPrefabAsset(panel, StoriesPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(panel); }
        }
        private static void ResidentWorld(VipVisualCatalog visuals)
        {
            if (File.Exists(PeoplePath)) return;
            var world = new GameObject("CassaVadelaResidents", typeof(VipTownPresentation)); var actors = new List<VipStoryActor>();
            foreach (var person in definitions.vipStories.Where(p => p.cityId == "cassa_vadela"))
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
        private static string MemoryProp(string person, int chapter, int choice)
        {
            string[] props;
            string alternate;
            switch (person)
            {
                case "cv_ines": props = new[] { "Prop_crates", "Town_herb_garden", "Prop_sign", "Town_reading_corner" }; alternate = "Town_picnic_nook"; break;
                case "cv_mateo": props = new[] { "Prop_lantern", "Prop_crates", "Town_reading_corner", "Town_produce_stall" }; alternate = "Town_tea_patio"; break;
                case "cv_lucia": props = new[] { "Prop_crates", "Town_herb_garden", "Town_produce_stall", "Town_bread_cart" }; alternate = "Town_tea_patio"; break;
                case "cv_rafael": props = new[] { "Prop_pump", "Prop_crates", "Town_workshop_corner", "Prop_lantern" }; alternate = "Town_reading_corner"; break;
                case "cv_sofia": props = new[] { "Prop_crates", "Town_workshop_corner", "Prop_sign", "Town_produce_stall" }; alternate = "Town_reading_corner"; break;
                case "cv_elio": props = new[] { "Prop_lantern", "Town_reading_corner", "Town_tea_patio", "Prop_flowers" }; alternate = "Town_picnic_nook"; break;
                case "cv_amara": props = new[] { "Town_picnic_nook", "Prop_lantern", "Prop_sign", "Town_rowan_tree" }; alternate = "Town_reading_corner"; break;
                case "cv_nico": props = new[] { "Prop_crates", "Prop_lantern", "Prop_sign", "Town_produce_stall" }; alternate = "Town_picnic_nook"; break;
                default: throw new InvalidOperationException("Unknown Cassa Vadela resident " + person);
            }
            if (chapter < 0 || chapter >= props.Length) throw new InvalidOperationException("Unsupported resident chapter " + chapter);
            string prop = chapter == 2 && choice == 1 ? alternate : props[chapter];
            return (prop.StartsWith("Town_", StringComparison.Ordinal) ? "Assets/Weedtopia/Production/TownVitality/Prefabs/" : "Assets/Weedtopia/Production/Prefabs/") + prop + ".prefab";
        }
        private static Vector3 AuthoredMemoryPosition(string personId, int chapter)
        {
            if (personId == "cv_elio" && chapter == 1) return DistrictMap.ToWorld(new Vector2(15,8));
            if (personId == "cv_elio" && chapter == 2) return DistrictMap.ToWorld(new Vector2(-9,6));
            if (personId == "cv_lucia" && chapter == 3) return DistrictMap.ToWorld(new Vector2(22,1));
            if (personId == "cv_amara" && chapter == 3) return DistrictMap.ToWorld(new Vector2(24,6));
            return OriginalMemoryPosition(chapter);
        }
        private static Vector3 OriginalMemoryPosition(int chapter)
            => DistrictMap.ToWorld(new Vector2(6 + chapter % 2 * 9, -5 - chapter / 2 * 13));
        private static void RepairResidentMemoryAssets()
        {
            foreach (string personId in new[] { "cv_elio", "cv_lucia", "cv_amara" })
            {
                string path = Folder + "/Prefabs/" + personId + ".prefab";
                if (!File.Exists(path)) continue;
                var owned = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (RepairResidentMemoryTargets(owned, path)) PrefabUtility.SaveAsPrefabAsset(owned, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(owned); }
            }
        }
        private static bool RepairResidentMemoryTargets(GameObject owned, string ownerPath)
        {
            var actor = owned == null ? null : owned.GetComponent<VipStoryActor>();
            if (actor == null || !new[] { "cv_elio", "cv_lucia", "cv_amara" }.Contains(actor.vipId)
                || ownerPath != Folder + "/Prefabs/" + actor.vipId + ".prefab" || owned.name != actor.vipId
                || string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(ownerPath))
                || !owned.transform.localPosition.Equals(Vector3.zero) || !owned.transform.localRotation.Equals(Quaternion.identity)
                || !owned.transform.localScale.Equals(Vector3.one) || actor.spriteRenderer == null
                || AssetDatabase.GetAssetPath(actor.spriteRenderer.sprite) != Folder + "/Art/" + actor.vipId + ".png"
                || actor.visibleRoot == null || !actor.visibleRoot.transform.localPosition.Equals(Vector3.zero)
                || !actor.visibleRoot.transform.localRotation.Equals(Quaternion.identity) || !actor.visibleRoot.transform.localScale.Equals(Vector3.one)) return false;
            var loaded = definitions ?? AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
            var person = loaded.vipStories.Single(p => p.id == actor.vipId); bool changed = false;
            foreach (var memory in actor.memories)
            {
                int chapter = memory.chapter; var next = AuthoredMemoryPosition(actor.vipId, chapter);
                if (next.Equals(OriginalMemoryPosition(chapter)) || memory.target == null) continue;
                int choice = person.chapters[chapter].choices.Length == 1 ? 0 : Array.FindIndex(person.chapters[chapter].choices, c => c.id == memory.choiceId);
                if (choice < 0) continue;
                string source = MemoryProp(actor.vipId, chapter, choice);
                if (memory.target.transform.parent != actor.visibleRoot.transform
                    || !DefaultStage(memory.target, "Memory_" + (chapter + 1) + "_" + person.chapters[chapter].choices[choice].id,
                        source, new Vector2(6 + chapter % 2 * 9, -5 - chapter / 2 * 13), MemoryScaleFactor(source))) continue;
                memory.target.transform.localPosition = next; EditorUtility.SetDirty(memory.target.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(memory.target.transform); changed = true;
            }
            return changed;
        }
        private static float MemoryScaleFactor(string path)
        {
            switch (Path.GetFileNameWithoutExtension(path))
            {
                case "Town_reading_corner": case "Town_tea_patio": case "Town_workshop_corner": return .7f;
                case "Town_picnic_nook": case "Town_fern_garden": case "Town_herb_garden": return .9f;
                case "Town_bread_cart": case "Town_produce_stall": return .55f;
                case "Town_rowan_tree": return .38f;
                case "Prop_crates": return 1.6f;
                case "Prop_pump": return 1.3f;
                case "Prop_sign": return 1.1f;
                case "Prop_lantern": return .85f;
                case "Prop_flowers": return 1.6f;
                default: throw new InvalidOperationException("Unsupported Cassa Vadela memory prop " + path);
            }
        }
        private static bool EnsurePropDepth(GameObject prop)
        {
            bool changed = false;
            foreach (var sprite in prop.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sprite.GetComponent<CityLandmarkVisual>() != null) continue;
                // Shared props are authored at a default depth. Their owning place supplies the world anchor.
                sprite.gameObject.AddComponent<CityLandmarkVisual>(); changed = true;
            }
            return changed;
        }
        private static void Landmarks()
        {
            EnsureDryFountainPrefab();
            // A repeated repair changes only known Cassa-owned defaults; manual choices remain intact.
            if (File.Exists(LandmarksPath))
            {
                var existing = PrefabUtility.LoadPrefabContents(LandmarksPath);
                try
                {
                    bool changed = RepairLandmarkTargets(existing, LandmarksPath);
                    changed |= RepairFountainStages(existing, LandmarksPath);
                    changed |= RepairFountainMarketStall(existing, LandmarksPath);
                    if (changed) PrefabUtility.SaveAsPrefabAsset(existing, LandmarksPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(existing); }
                return;
            }
            ImportSingle(Folder + "/Art/fountain_square.png", .04f);
            ImportSingle(Folder + "/Art/terraced_garden.png", .04f);
            var fountain = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Art/fountain_square.png");
            var garden = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Art/terraced_garden.png");
            var world = new GameObject("CassaVadelaPlaces");
            var positions = new[] { new Vector2(143,-76), new Vector2(-142,54), new Vector2(-40,324) };
            var names = new[] { "FountainMarket", "OpenCeramicYard", "TerracePromenade" };
            var projects = new[] { "cv_joint_fountain_market", "cv_joint_open_ceramic_yard", "cv_joint_terrace_walk" };
            var milestones = new[] { 1800, 4500, 9000 };
            var restoredProps = new[] { "Town_produce_stall", "Town_workshop_corner", "Town_picnic_nook" };
            for (int index = 0; index < positions.Length; index++)
            {
                var placeObject = new GameObject(names[index], typeof(CityPlaceProgressVisual));
                placeObject.transform.SetParent(world.transform, false);
                placeObject.transform.localPosition = DistrictMap.ToWorld(positions[index]);
                var place = placeObject.GetComponent<CityPlaceProgressVisual>();
                place.cityId = City; place.residents = milestones[index]; place.collaborationId = projects[index];
                if (index != 1)
                {
                    var sprite = ProductionWorldAuthoring.Sprite(index == 0 ? "HistoricFountainSquare" : "TerracedPromenade",
                        placeObject.transform, index == 0 ? fountain : garden, index == 0 ? 25 : 27, Vector3.zero);
                    sprite.gameObject.AddComponent<CityLandmarkVisual>();
                }
                var closed = Instance("Assets/Weedtopia/Production/Prefabs/Prop_salvage.prefab", placeObject.transform);
                closed.name = "DamagedCommunityPlace";
                closed.transform.localPosition = DistrictMap.ToWorld(new Vector2(8,-6)); place.closedPlace = closed;
                var growing = Instance("Assets/Weedtopia/Production/Prefabs/Prop_crates.prefab", placeObject.transform);
                growing.name = "SharedRestorationSupplies"; growing.transform.localPosition = DistrictMap.ToWorld(new Vector2(-8,-6));
                growing.transform.localScale *= 1.6f; growing.SetActive(false); place.growingPlace = growing;
                var restored = Instance("Assets/Weedtopia/Production/TownVitality/Prefabs/" + restoredProps[index] + ".prefab", placeObject.transform);
                restored.name = "RestoredCommunityPlace"; restored.transform.localPosition = DistrictMap.ToWorld(index == 0 ? new Vector2(-14,-6) : new Vector2(8,-6));
                restored.transform.localScale *= index == 2 ? 1.2f : .85f; restored.SetActive(false); place.completedPlace = restored;
                if (index == 0)
                {
                    var pump = Instance("Assets/Weedtopia/Production/Prefabs/Prop_pump.prefab", restored.transform);
                    pump.name = "RestoredFountainPump"; pump.transform.localPosition = DistrictMap.ToWorld(new Vector2(-11,3));
                    var flowers = Instance("Assets/Weedtopia/Production/Prefabs/Prop_flowers.prefab", restored.transform);
                    flowers.name = "MarketFlowers"; flowers.transform.localPosition = DistrictMap.ToWorld(new Vector2(-5,7));
                }
                if (index == 1)
                {
                    place.choiceResidentId = "cv_rafael"; place.choiceChapter = 2; place.choiceValues = new[] { "repair", "teach" };
                    var repair = Instance("Assets/Weedtopia/Production/TownVitality/Prefabs/Town_workshop_corner.prefab", placeObject.transform);
                    repair.name = "NeighbourRepairBench"; repair.transform.localPosition = DistrictMap.ToWorld(new Vector2(0,-14));
                    repair.transform.localScale *= .8f; repair.SetActive(false);
                    var teach = Instance("Assets/Weedtopia/Production/TownVitality/Prefabs/Town_reading_corner.prefab", placeObject.transform);
                    teach.name = "OpenLearningBench"; teach.transform.localPosition = DistrictMap.ToWorld(new Vector2(0,-14));
                    teach.transform.localScale *= .8f; teach.SetActive(false);
                    place.choicePlaces = new[] { repair, teach };
                }
            }
            var gardenPositions = new[] { new Vector2(310,25), new Vector2(470,12), new Vector2(110,156) };
            for (int index = 0; index < gardenPositions.Length; index++)
            {
                var anchor = new GameObject("OliveTerraceGarden_" + (index + 1));
                anchor.transform.SetParent(world.transform, false); anchor.transform.localPosition = DistrictMap.ToWorld(gardenPositions[index]);
                var sprite = ProductionWorldAuthoring.Sprite("StoneGardenTerrace", anchor.transform, garden, index == 2 ? 19 : 24, Vector3.zero);
                sprite.gameObject.AddComponent<CityLandmarkVisual>();
            }
            for (int index = 0; index < 18; index++)
            {
                var tree = Instance("Assets/Weedtopia/Production/Prefabs/Prop_tree.prefab", world.transform);
                tree.name = "TerraceShadeTree_" + (index + 1);
                var position = index < 6 ? new Vector2(-80 - index % 2 * 9, 70 + index * 52)
                    : index < 12 ? new Vector2(170 + (index - 6) * 61, -38 - index % 2 * 10)
                    : new Vector2(65 + (index - 12) * 65, 385 + index % 2 * 9);
                tree.transform.localPosition = DistrictMap.ToWorld(position); tree.transform.localScale *= 1.15f + index % 3 * .1f;
                foreach (var renderer in tree.GetComponentsInChildren<SpriteRenderer>(true)) renderer.color = new Color(.93f,.98f,.77f,1);
            }
            AuthorFountainStages(world.transform.Find("FountainMarket").GetComponent<CityPlaceProgressVisual>());
            EnsurePropDepth(world); PrefabUtility.SaveAsPrefabAsset(world, LandmarksPath); Object.DestroyImmediate(world);
        }
        private static void EnsureDryFountainPrefab()
        {
            if (File.Exists(DryFountainPath)) return;
            ImportSingle(Folder + "/Art/fountain_dry.png", .04f);
            var dry = new GameObject("DryFountain");
            try
            {
                var sprite = ProductionWorldAuthoring.Sprite("DryFountainArtwork", dry.transform,
                    AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Art/fountain_dry.png"), 25, Vector3.zero);
                sprite.gameObject.AddComponent<CityLandmarkVisual>(); PrefabUtility.SaveAsPrefabAsset(dry, DryFountainPath);
            }
            finally { Object.DestroyImmediate(dry); }
        }
        private static bool RepairFountainStages(GameObject world, string ownerPath)
        {
            if (ownerPath != LandmarksPath || world == null || world.name != Path.GetFileNameWithoutExtension(LandmarksPath)
                || !world.transform.localPosition.Equals(Vector3.zero) || !world.transform.localRotation.Equals(Quaternion.identity)
                || !world.transform.localScale.Equals(Vector3.one)) return false;
            var fountain = world.transform.Find("FountainMarket");
            var place = fountain == null ? null : fountain.GetComponent<CityPlaceProgressVisual>();
            if (place == null || place.cityId != City || place.collaborationId != "cv_joint_fountain_market" || place.residents != 1800
                || !fountain.localPosition.Equals(DistrictMap.ToWorld(new Vector2(143,-76)))
                || !fountain.localRotation.Equals(Quaternion.identity) || !fountain.localScale.Equals(Vector3.one)) return false;
            var wet = fountain.Find("HistoricFountainSquare");
            var sprite = wet == null ? null : wet.GetComponent<SpriteRenderer>();
            if (sprite == null || wet.GetComponent<CityLandmarkVisual>() == null || !sprite.color.Equals(Color.white)
                || AssetDatabase.GetAssetPath(sprite.sprite) != Folder + "/Art/fountain_square.png"
                || !wet.localPosition.Equals(Vector3.zero) || !wet.localRotation.Equals(Quaternion.Euler(35.264f,45,0))
                || !wet.localScale.Equals(Vector3.one * (25f / sprite.sprite.bounds.size.y))) return false;
            if (!DefaultStage(place.closedPlace,"DamagedCommunityPlace","Assets/Weedtopia/Production/Prefabs/Prop_salvage.prefab",new Vector2(8,-6),1)
                || !DefaultStage(place.growingPlace,"SharedRestorationSupplies","Assets/Weedtopia/Production/Prefabs/Prop_crates.prefab",new Vector2(-8,-6),1.6f)
                || !DefaultStage(place.completedPlace,"RestoredCommunityPlace","Assets/Weedtopia/Production/TownVitality/Prefabs/Town_produce_stall.prefab",new Vector2(8,-6),.85f)) return false;
            AuthorFountainStages(place); return true;
        }
        private static bool RepairFountainMarketStall(GameObject world, string ownerPath)
        {
            if (ownerPath != LandmarksPath || world == null || world.name != Path.GetFileNameWithoutExtension(LandmarksPath)
                || string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(ownerPath))
                || !world.transform.localPosition.Equals(Vector3.zero) || !world.transform.localRotation.Equals(Quaternion.identity)
                || !world.transform.localScale.Equals(Vector3.one)) return false;
            var fountain = world.transform.Find("FountainMarket"); var place = fountain == null ? null : fountain.GetComponent<CityPlaceProgressVisual>();
            if (place == null || place.cityId != City || place.collaborationId != "cv_joint_fountain_market" || place.residents != 1800
                || place.keepGrowingPlaceAfterCompletion || !fountain.localPosition.Equals(DistrictMap.ToWorld(new Vector2(143,-76)))
                || !fountain.localRotation.Equals(Quaternion.identity) || !fountain.localScale.Equals(Vector3.one)
                || !DefaultStage(place.completedPlace,"RestoredCommunityPlace","Assets/Weedtopia/Production/TownVitality/Prefabs/Town_produce_stall.prefab",new Vector2(8,-6),.85f)) return false;
            var anchor = place.completedPlace.transform.Find("RestoredFountainAnchor");
            var wet = anchor == null ? null : anchor.Find("HistoricFountainSquare"); var renderer = wet == null ? null : wet.GetComponent<SpriteRenderer>();
            if (anchor == null || renderer == null || AssetDatabase.GetAssetPath(renderer.sprite) != Folder + "/Art/fountain_square.png"
                || !renderer.color.Equals(Color.white) || Vector3.Distance(anchor.position,fountain.position) > .0001f
                || Quaternion.Angle(anchor.rotation,Quaternion.identity) > .001f || Vector3.Distance(anchor.lossyScale,Vector3.one) > .0001f
                || !wet.localPosition.Equals(Vector3.zero) || !wet.localRotation.Equals(Quaternion.Euler(35.264f,45,0))
                || Vector3.Distance(wet.localScale,Vector3.one * (25f / renderer.sprite.bounds.size.y)) > .0001f) return false;
            place.completedPlace.transform.localPosition = DistrictMap.ToWorld(new Vector2(-14,-6));
            AlignStageArtwork(anchor,place.completedPlace.transform,fountain.position);
            EditorUtility.SetDirty(place.completedPlace.transform); EditorUtility.SetDirty(anchor);
            PrefabUtility.RecordPrefabInstancePropertyModifications(place.completedPlace.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(anchor); return true;
        }
        private static bool DefaultStage(GameObject target, string name, string source, Vector2 position, float scale)
        {
            if (target == null || target.name != name || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target) != source) return false;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
            return prefab != null && target.transform.localPosition.Equals(DistrictMap.ToWorld(position))
                && target.transform.localRotation.Equals(prefab.transform.localRotation)
                && target.transform.localScale.Equals(prefab.transform.localScale * scale);
        }
        private static void AuthorFountainStages(CityPlaceProgressVisual place)
        {
            var wet = place.transform.Find("HistoricFountainSquare");
            if (wet == null) throw new InvalidOperationException("Missing known fountain artwork; refusing a broad landmark rebuild.");
            foreach (var stage in new[] { place.closedPlace, place.growingPlace })
            {
                var dry = Instance(DryFountainPath, stage.transform);
                dry.name = stage == place.closedPlace ? "DryFountainClosed" : "DryFountainGrowing";
                AlignStageArtwork(dry.transform, stage.transform, place.transform.position);
                PrefabUtility.RecordPrefabInstancePropertyModifications(dry); PrefabUtility.RecordPrefabInstancePropertyModifications(dry.transform);
            }
            var anchor = new GameObject("RestoredFountainAnchor"); anchor.transform.SetParent(place.completedPlace.transform,false);
            AlignStageArtwork(anchor.transform, place.completedPlace.transform, place.transform.position);
            wet.SetParent(anchor.transform,true);
            place.keepGrowingPlaceAfterCompletion = false; EditorUtility.SetDirty(place);
            PrefabUtility.RecordPrefabInstancePropertyModifications(place);
        }
        private static void AlignStageArtwork(Transform artwork, Transform stage, Vector3 foot)
        {
            artwork.SetPositionAndRotation(foot,Quaternion.identity);
            var scale = stage.lossyScale; artwork.localScale = new Vector3(1 / scale.x,1 / scale.y,1 / scale.z);
        }
        /// <summary>Only the two linked, untouched first-install defaults opt into this explicit layout repair.</summary>
        private static bool RepairLandmarkTargets(GameObject world, string ownerPath)
        {
            if (ownerPath != LandmarksPath || world == null || world.name != Path.GetFileNameWithoutExtension(LandmarksPath)
                || !world.transform.localPosition.Equals(Vector3.zero)
                || !world.transform.localRotation.Equals(Quaternion.identity)
                || !world.transform.localScale.Equals(Vector3.one)) return false;
            bool changed = false;
            var fountain = world.transform.Find("FountainMarket");
            var progress = fountain == null ? null : fountain.GetComponent<CityPlaceProgressVisual>();
            if (progress != null && progress.cityId == City && progress.collaborationId == "cv_joint_fountain_market"
                && progress.residents == 1800)
                changed |= MoveDefaultLandmarkAnchor(fountain, new Vector2(110,-76), new Vector2(143,-76));
            var garden = world.transform.Find("OliveTerraceGarden_1");
            var gardenVisual = garden == null ? null : garden.GetComponentInChildren<CityLandmarkVisual>(true);
            var gardenSprite = gardenVisual == null ? null : gardenVisual.GetComponent<SpriteRenderer>();
            if (garden != null && garden.GetComponent<CityPlaceProgressVisual>() == null && gardenVisual != null
                && gardenVisual.gameObject.name == "StoneGardenTerrace" && gardenSprite != null
                && AssetDatabase.GetAssetPath(gardenSprite.sprite) == Folder + "/Art/terraced_garden.png")
                changed |= MoveDefaultLandmarkAnchor(garden, new Vector2(310,-12), new Vector2(310,25));
            return changed;
        }
        private static bool MoveDefaultLandmarkAnchor(Transform target, Vector2 previous, Vector2 next)
        {
            if (!target.localPosition.Equals(DistrictMap.ToWorld(previous))
                || !target.localRotation.Equals(Quaternion.identity) || !target.localScale.Equals(Vector3.one)) return false;
            target.localPosition = DistrictMap.ToWorld(next); EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target); return true;
        }
        private static void Districts()
        {
            if (File.Exists(MapPath)) return;
            if (!AssetDatabase.CopyAsset(BigRockCityAuthoring.Folder + "/DistrictMap.asset", MapPath)) throw new InvalidOperationException("Could not copy district map.");
            var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(MapPath);
            foreach (var range in map.districts)
            {
                string suffix = range.zone == Zone.Farm ? "farm" : range.zone == Zone.City ? "city" : range.zone == Zone.Settlement ? "settlement" : "industry";
                range.labelKey = "district.cassa_vadela." + suffix;
                range.label = range.zone == Zone.Farm ? "Oliventerrassen · Farm" : range.zone == Zone.City ? "Brunnenmarkt · Stadt" : range.zone == Zone.Settlement ? "Sonnenhang · Wohnviertel" : "Alter Keramikhof · Industrie";
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
                if (!created && !string.IsNullOrEmpty(label.stringValue) && label.stringValue != "Big Rock City") continue; label.stringValue = "Cassa Vadela"; data.ApplyModifiedPropertiesWithoutUndo(); changed = true;
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
                if (transform.name.StartsWith("BigRock", StringComparison.Ordinal)) transform.name = transform.name.Replace("BigRock", "CassaVadela");
                foreach (var component in transform.GetComponents<MonoBehaviour>().Where(c => c != null))
                {
                    var data = new SerializedObject(component); var property = data.GetIterator(); bool changed = false;
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.String || !ids.Contains(property.stringValue)) continue;
                        if (property.name != "instanceId" && property.name != "workplaceId" && property.name != "requiredBuilding" && property.name != "routeId") continue;
                        property.stringValue = "cv_" + property.stringValue.Substring(3); changed = true;
                    }
                    if (changed) data.ApplyModifiedPropertiesWithoutUndo();
                }
                var text = transform.GetComponent<Text>(); if (text != null && text.text.Contains("Big Rock City")) text.text = text.text.Replace("Big Rock City", "Cassa Vadela");
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
                            themed = new Material(source) { name = "CassaVadela_" + source.name };
                            if (shader.Contains("Ground"))
                            {
                                if (themed.HasProperty("_BaseColor")) themed.SetColor("_BaseColor", new Color(.56f, .55f, .34f));
                                if (themed.HasProperty("_Tint")) themed.SetColor("_Tint", new Color(1.08f, 1.01f, .79f));
                                if (themed.HasProperty("_Saturation")) themed.SetFloat("_Saturation", .72f);
                                if (themed.HasProperty("_Contrast")) themed.SetFloat("_Contrast", .48f);
                            }
                            if (shader.Contains("Foliage") && themed.HasProperty("_Color")) themed.SetColor("_Color", new Color(.94f, .97f, .63f));
                            AssetDatabase.CreateAsset(themed, path);
                        }
                        materials.Add(source, themed);
                    }
                    shared[i] = themed; changed = true;
                }
                if (changed) renderer.sharedMaterials = shared;
            }
            root.CameraRig.RenderCamera.backgroundColor = new Color(.27f, .36f, .38f);
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
            foreach (var transform in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "Living_town_sign" || t.name == "CassaVadelaWelcomeSign" || t.name == "BigRockWelcomeSign" || t.name == "SevenHillsWelcomeSign"))
            {
                var renderer = transform.GetComponentInChildren<SpriteRenderer>(true); if ((!created && renderer.sprite != null) || renderer.sprite == sprite) continue;
                float height = renderer.sprite == null ? 8 : renderer.sprite.bounds.size.y * renderer.transform.localScale.y; renderer.sprite = sprite; renderer.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform);
                transform.name = "CassaVadelaWelcomeSign"; PrefabUtility.RecordPrefabInstancePropertyModifications(transform.gameObject); changed = true;
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

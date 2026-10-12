using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Buildings.Presentation;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class LittleMontanaAssetTests
    {
        [Test]
        public void EveryPlayableCityHasAuthoredTravelRowsForEveryDestinationAndOnlyLocalRoutes()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var definitions = AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
                Assert.That(definitions.cities.Count(city => city.introducedVersion <= 5), Is.EqualTo(4));
                foreach (var city in definitions.cities)
                {
                    var scene = EditorSceneManager.OpenScene(city.scenePath);
                    var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameRoot>(true)).Single();
                    Assert.That(root.SceneCityId, Is.EqualTo(city.id), city.id);
                    CollectionAssert.AreEquivalent(definitions.instances.Where(i => i.cityId == city.id).Select(i => i.id), root.Sites.Select(s => s.InstanceId), city.id);
                    var expectedRoutes = definitions.routes.Where(route => definitions.instances.Single(i => i.id == route.from).cityId == city.id).Select(route => route.id);
                    CollectionAssert.AreEquivalent(expectedRoutes, root.Presentation.routes.Select(route => route.routeId), city.id);
                    CollectionAssert.AreEquivalent(definitions.cities.Select(c => c.id), root.Ui.Campaign.travelRows.Select(r => r.cityId), city.id);
                    Assert.That(root.Ui.Campaign.travelRows.All(r => r.progress != null && r.travel != null), Is.True, city.id);
                    Assert.That(root.Ui.Campaign.headerEntry, Is.Not.Null);
                    Assert.That(root.Ui.Campaign.journalEntry, Is.Not.Null);
                    Assert.That(root.Presentation.characters.Single(c => c.speakerId == "rocko").GetComponentInChildren<CityMandateActor>(true).root, Is.SameAs(root));
                    Assert.That(EditorBuildSettings.scenes.Any(s => s.path == city.scenePath && s.enabled), Is.True, city.id);
                }
            }
            finally { Restore(setup); }
        }

        [Test]
        public void LittleMontanaHasSevenPermanentResidentsWithFourAuthoredMemoriesAndSixLivingPlaces()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(LittleMontanaAuthoring.ScenePath);
                var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameRoot>(true)).Single();
                var definitions = AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
                var residents = definitions.vipStories.Where(v => v.cityId == "little_montana").Select(v => v.id).ToArray();
                Assert.That(residents.Length, Is.EqualTo(7));
                CollectionAssert.AreEquivalent(residents, root.StoryWorld.actors.Select(a => a.vipId));
                Assert.That(root.StoryWorld.actors.Select(a => a.spriteRenderer.sprite.texture).Distinct().Count(), Is.EqualTo(7));
                Assert.That(root.StoryWorld.actors.All(a => a.selectionBox != null && a.memories.All(m => m.target != null)), Is.True);
                Assert.That(root.StoryWorld.actors.All(a => a.memories.Select(m => m.chapter).Distinct().Count() == 4), Is.True);
                foreach (var actor in root.StoryWorld.actors)
                {
                    var memoryPaths = actor.memories.Select(memory => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(memory.target)).ToArray();
                    Assert.That(memoryPaths.All(memoryPath => !string.IsNullOrEmpty(memoryPath)), Is.True, actor.vipId);
                    Assert.That(memoryPaths.Distinct().Count(), Is.GreaterThanOrEqualTo(4), "Each chapter needs a distinct persistent object: " + actor.vipId);
                    var branchPaths = actor.memories.Where(memory => memory.chapter == 2).Select(memory => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(memory.target)).ToArray();
                    Assert.That(branchPaths.Length, Is.EqualTo(2), actor.vipId);
                    Assert.That(branchPaths.Distinct().Count(), Is.EqualTo(2), "Saved third-chapter choices need visibly different objects: " + actor.vipId);
                }
                Assert.That(root.Ui.Stories.rows.Length, Is.EqualTo(10));
                Assert.That(root.Ui.Stories.GetComponent<VipStoryPanelLayout>(), Is.Not.Null);
                var places = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CityPlaceProgressVisual>(true)).ToArray();
                Assert.That(places.Length, Is.EqualTo(6));
                Assert.That(places.All(place => place.cityId == "little_montana" && place.root == root && place.closedPlace != null && place.growingPlace != null && place.completedPlace != null), Is.True);
                var stamp = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LittleMontanaStamp>(true)).Single();
                Assert.That(stamp.buildingVariants, Is.EqualTo(251));
                Assert.That(stamp.inhabitants, Is.EqualTo(7));
                Assert.That(stamp.landmarks, Is.EqualTo(6));
                Assert.That(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BigRockCityStamp>(true)), Is.Empty);
                Assert.That(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SevenHillsStamp>(true)), Is.Empty);
                var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(LittleMontanaAuthoring.MapPath);
                Assert.That(map.districts.Length, Is.EqualTo(4));
                Assert.That(map.districts.All(d => d.labelKey.StartsWith("district.little_montana.", StringComparison.Ordinal)), Is.True);
                var signs = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "LittleMontanaWelcomeSign").ToArray();
                Assert.That(signs.Length, Is.EqualTo(2));
                foreach (var sign in signs) Assert.That(AssetDatabase.GetAssetPath(sign.GetComponentInChildren<SpriteRenderer>().sprite), Is.EqualTo(LittleMontanaAuthoring.Folder + "/Art/town_sign.png"));
            }
            finally { Restore(setup); }
        }

        [Test]
        public void All251FourthCityVariantsRetainSourceSpritesColliderGeometryAndGameplayAnchors()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var root = UnityEngine.Object.FindFirstObjectByType<GameRoot>();
                var source = (BuildingVisualCatalog)new SerializedObject(root).FindProperty("productionCatalog").objectReferenceValue;
                var target = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(LittleMontanaAuthoring.CatalogPath);
                Assert.That(target.Families.Sum(f => f.Bands.Count), Is.EqualTo(251));
                foreach (var family in source.Families)
                    foreach (var band in family.Bands)
                    {
                        Assert.That(target.TryGetVisual(family.BuildingId, band.MinLevel, out var prefab), Is.True);
                        Assert.That(target.TryGetVisual(family.BuildingId, band.MaxLevel, out var end), Is.True);
                        Assert.That(end, Is.SameAs(prefab));
                        Assert.That(AssetDatabase.GetAssetPath(prefab), Does.StartWith(LittleMontanaAuthoring.Folder + "/Buildings/"));
                        var original = band.WrapperPrefab.GetComponent<BuildingVisualWrapper>();
                        var current = prefab.GetComponent<BuildingVisualWrapper>();
                        Assert.That(current.BuildingId, Is.EqualTo(original.BuildingId));
                        Assert.That(current.TierId, Is.EqualTo(original.TierId));
                        var oldAnchors = new[] { original.InteractionAnchor, original.WorkerAnchor, original.DeliveryInputAnchor, original.DeliveryOutputAnchor, original.VfxAnchor, original.ColliderRoot };
                        var newAnchors = new[] { current.InteractionAnchor, current.WorkerAnchor, current.DeliveryInputAnchor, current.DeliveryOutputAnchor, current.VfxAnchor, current.ColliderRoot };
                        for (int i = 0; i < oldAnchors.Length; i++)
                        {
                            Assert.That(newAnchors[i], Is.Not.Null);
                            Assert.That(newAnchors[i].localPosition, Is.EqualTo(oldAnchors[i].localPosition));
                            Assert.That(newAnchors[i].localRotation, Is.EqualTo(oldAnchors[i].localRotation));
                            Assert.That(newAnchors[i].localScale, Is.EqualTo(oldAnchors[i].localScale));
                        }
                        CollectionAssert.AreEqual(band.WrapperPrefab.GetComponentsInChildren<BoxCollider>(true).Select(c => c.center + "/" + c.size + "/" + c.enabled), prefab.GetComponentsInChildren<BoxCollider>(true).Select(c => c.center + "/" + c.size + "/" + c.enabled));
                        var oldSprites = band.WrapperPrefab.GetComponentsInChildren<BuildingSpriteVisual>(true);
                        var newSprites = prefab.GetComponentsInChildren<BuildingSpriteVisual>(true);
                        Assert.That(newSprites.Length, Is.EqualTo(oldSprites.Length));
                        for (int i = 0; i < oldSprites.Length; i++)
                        {
                            Assert.That(newSprites[i].ResourcePath, Is.EqualTo(oldSprites[i].ResourcePath));
                            Assert.That(newSprites[i].SpriteRenderer.sprite, Is.SameAs(oldSprites[i].SpriteRenderer.sprite));
                            Assert.That(newSprites[i].transform.localPosition, Is.EqualTo(oldSprites[i].transform.localPosition));
                            Assert.That(newSprites[i].transform.localRotation, Is.EqualTo(oldSprites[i].transform.localRotation));
                            Assert.That(newSprites[i].transform.localScale, Is.EqualTo(oldSprites[i].transform.localScale));
                            Assert.That(newSprites[i].FootprintInset, Is.EqualTo(oldSprites[i].FootprintInset));
                            Assert.That(newSprites[i].SpriteRenderer.sharedMaterial.shader.name, Is.EqualTo("Weedtopia/Little Montana/Building Palette"));
                        }
                    }
            }
            finally { Restore(setup); }
        }

        [Test]
        public void MemoryRepairKeepsManualSourceLinksUnpackedObjectsTransformsAndNames()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            string folder = "Assets/__LittleMontanaManualFixture_" + Guid.NewGuid().ToString("N");
            string path = folder + "/Fixture.prefab";
            GameObject fixture = null, contents = null;
            byte[] beforeBytes = null;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
                var person = AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath)
                    .Load().vipStories.Single(p => p.id == "lm_mara");
                fixture = new GameObject("OwnManualMemoryFixture", typeof(VipStoryActor));
                var actor = fixture.GetComponent<VipStoryActor>(); actor.vipId = person.id;
                string[] sources = {
                    "Assets/Weedtopia/Production/Prefabs/Prop_lantern.prefab",
                    "Assets/Weedtopia/Production/TownVitality/Prefabs/Town_herb_garden.prefab",
                    "Assets/Weedtopia/Production/Prefabs/Prop_sign.prefab",
                    "Assets/Weedtopia/Production/TownVitality/Prefabs/Town_picnic_nook.prefab",
                    "Assets/Weedtopia/Production/TownVitality/Prefabs/Town_reading_corner.prefab"
                };
                int[] chapters = { 0, 1, 2, 2, 3 }, choices = { 0, 0, 0, 1, 0 };
                actor.memories = new VipMemoryVisual[sources.Length];
                for (int i = 0; i < sources.Length; i++)
                {
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(sources[i]);
                    Assert.That(source, Is.Not.Null, sources[i]);
                    var prop = (GameObject)PrefabUtility.InstantiatePrefab(source, fixture.transform);
                    string choice = person.chapters[chapters[i]].choices[choices[i]].id;
                    prop.name = "Memory_" + (chapters[i] + 1) + "_" + choice;
                    prop.transform.localPosition = new Vector3(i * 2 + .17f, .23f, i + .41f);
                    prop.transform.localRotation = Quaternion.Euler(0, i * 13 + 7, 0);
                    prop.transform.localScale = source.transform.localScale * (sources[i].Contains("Town_") ? .38f : .85f);
                    prop.SetActive(false);
                    actor.memories[i] = new VipMemoryVisual { chapter = chapters[i], choiceId = chapters[i] == 2 ? choice : "", target = prop };
                }
                PrefabUtility.UnpackPrefabInstance(actor.memories[1].target, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                actor.memories[2].target.transform.localScale = new Vector3(2.3f, 1.7f, .97f);
                actor.memories[3].target.name = "ManualReadingSpot";
                actor.memories[4].target.transform.localPosition = DistrictMap.ToWorld(new Vector2(12, -11));
                PrefabUtility.SaveAsPrefabAsset(fixture, path);
                UnityEngine.Object.DestroyImmediate(fixture); fixture = null;
                beforeBytes = File.ReadAllBytes(path);
                contents = PrefabUtility.LoadPrefabContents(path);
                actor = contents.GetComponent<VipStoryActor>();
                var targets = actor.memories.Select(m => m.target).ToArray();
                var positions = targets.Select(t => t.transform.localPosition).ToArray();
                var rotations = targets.Select(t => t.transform.localRotation).ToArray();
                var scales = targets.Select(t => t.transform.localScale).ToArray();
                var links = targets.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).ToArray();
                var names = targets.Select(t => t.name).ToArray();
                var depth = targets.Select(t => t.GetComponentsInChildren<CityLandmarkVisual>(true).Length).ToArray();
                var repair = typeof(LittleMontanaAuthoring).GetMethod("RepairMemoryTargets", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(repair, Is.Not.Null, "The fixture must exercise the same contents operation used by the explicit installer.");
                Assert.That((bool)repair.Invoke(null, new object[] { contents, person }), Is.True, "The untouched owned old scale should migrate.");
                for (int i = 0; i < targets.Length; i++)
                {
                    Assert.That(actor.memories[i].target, Is.SameAs(targets[i]), "No manual memory target may be replaced.");
                    Assert.That(targets[i].transform.localPosition, Is.EqualTo(i == 4
                        ? DistrictMap.ToWorld(new Vector2(15, -18)) : positions[i]));
                    Assert.That(targets[i].transform.localRotation, Is.EqualTo(rotations[i]));
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targets[i]), Is.EqualTo(links[i]));
                    Assert.That(targets[i].name, Is.EqualTo(names[i]));
                    Assert.That(targets[i].activeSelf, Is.False);
                    if (i < 4) Assert.That(targets[i].transform.localScale, Is.EqualTo(scales[i]));
                    if (i == 0 || i == 1 || i == 3) Assert.That(targets[i].GetComponentsInChildren<CityLandmarkVisual>(true).Length, Is.EqualTo(depth[i]));
                }
                var reading = AssetDatabase.LoadAssetAtPath<GameObject>(sources[4]);
                Assert.That(targets[4].transform.localScale, Is.EqualTo(reading.transform.localScale * .7f));
                Assert.That((bool)repair.Invoke(null, new object[] { contents, person }), Is.False, "An explicit repeated repair must be stable.");
                AssertPineRepairPreservesManualWork(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                if (contents != null) PrefabUtility.UnloadPrefabContents(contents);
                if (fixture != null) UnityEngine.Object.DestroyImmediate(fixture);
                if (beforeBytes != null && File.Exists(path))
                {
                    File.WriteAllBytes(path, beforeBytes);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
                Restore(setup);
            }
        }

        private static void AssertPineRepairPreservesManualWork(GameObject contents)
        {
            var repair = typeof(LittleMontanaAuthoring).GetMethod("RepairPineTargets", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(repair, Is.Not.Null, "The isolated pine fixture must exercise the explicit installer's repair operation.");
            var pine = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Weedtopia/Production/Prefabs/Prop_pine.prefab");
            var foreign = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Weedtopia/Production/Prefabs/Prop_lantern.prefab");
            Assert.That(pine, Is.Not.Null); Assert.That(foreign, Is.Not.Null);
            var oldPlans = new[] { new Vector2(160, -51), new Vector2(250, -44) };
            var newPlans = new[] { new Vector2(150, -51), new Vector2(260, -44) };
            string[] scenarios = { "UntouchedOwned", "ForeignSource", "Unpacked", "ManualPosition", "ManualName", "ManualScaleAndRotation", "NestedSourceChild" };
            for (int scenario = 0; scenario < scenarios.Length; scenario++)
            {
                var root = new GameObject("OwnPineGuard_" + scenarios[scenario]);
                root.transform.SetParent(contents.transform, false);
                var targets = new GameObject[2];
                for (int index = 0; index < targets.Length; index++)
                {
                    targets[index] = (GameObject)PrefabUtility.InstantiatePrefab(scenario == 1 ? foreign : pine, root.transform);
                    if (scenario == 6)
                    {
                        targets[index].name = "ManualSourceRoot_" + index;
                        Assert.That(targets[index].transform.childCount, Is.GreaterThan(0));
                        targets[index] = targets[index].transform.GetChild(0).gameObject;
                    }
                    targets[index].name = index == 0 ? "ValleyPine_9" : "ValleyPine_11";
                    targets[index].transform.localPosition = DistrictMap.ToWorld(oldPlans[index]);
                    targets[index].SetActive(false);
                    if (scenario == 2) PrefabUtility.UnpackPrefabInstance(targets[index], PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    if (scenario == 3) targets[index].transform.localPosition += new Vector3(.17f, .23f, .41f);
                    if (scenario == 4) targets[index].name = "ManuallyRenamedPine_" + index;
                    if (scenario == 5)
                    {
                        targets[index].transform.localScale = new Vector3(2.3f, 1.7f, .97f);
                        targets[index].transform.localRotation = Quaternion.Euler(3, 27 + index * 11, 7);
                    }
                }
                var positions = targets.Select(target => target.transform.localPosition).ToArray();
                var parents = targets.Select(target => target.transform.parent).ToArray();
                var rotations = targets.Select(target => target.transform.localRotation).ToArray();
                var scales = targets.Select(target => target.transform.localScale).ToArray();
                var links = targets.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).ToArray();
                var names = targets.Select(target => target.name).ToArray();
                var objectCount = root.GetComponentsInChildren<Transform>(true).Length;
                bool shouldMove = scenario == 0 || scenario == 5;
                Assert.That((bool)repair.Invoke(null, new object[] { root }), Is.EqualTo(shouldMove), scenarios[scenario]);
                for (int index = 0; index < targets.Length; index++)
                {
                    Assert.That(targets[index].transform.parent, Is.SameAs(parents[index]));
                    Assert.That(targets[index].transform.localPosition, Is.EqualTo(shouldMove ? DistrictMap.ToWorld(newPlans[index]) : positions[index]), scenarios[scenario]);
                    Assert.That(targets[index].transform.localRotation, Is.EqualTo(rotations[index]), scenarios[scenario]);
                    Assert.That(targets[index].transform.localScale, Is.EqualTo(scales[index]), scenarios[scenario]);
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targets[index]), Is.EqualTo(links[index]), scenarios[scenario]);
                    Assert.That(targets[index].name, Is.EqualTo(names[index]), scenarios[scenario]);
                    Assert.That(targets[index].activeSelf, Is.False, scenarios[scenario]);
                }
                Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objectCount), "Only position values may change.");
                Assert.That((bool)repair.Invoke(null, new object[] { root }), Is.False, "A repeated guarded pine repair must be stable: " + scenarios[scenario]);
            }
        }

        [Test]
        public void ExplicitRepeatInstallationRetainsAllFourScenesExistingCityAssetsAndContentBytes()
        {
            var definitions = AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
            var paths = Directory.GetFiles(LittleMontanaAuthoring.Folder, "*", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(BigRockCityAuthoring.Folder, "*", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(SevenHillsAuthoring.Folder, "*", SearchOption.AllDirectories))
                .Concat(definitions.cities.SelectMany(city => new[] { city.scenePath, city.scenePath + ".meta" }))
                .Concat(new[] { BigRockCityAuthoring.CampaignPath, "Assets/Weedtopia/Game/Content/BurningHills.json", "ProjectSettings/EditorBuildSettings.asset" })
                .Distinct().ToArray();
            var before = paths.ToDictionary(path => path, File.ReadAllBytes);
            LittleMontanaAuthoring.Apply();
            foreach (var pair in before) Assert.That(pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)), Is.True, pair.Key);
            Directory.CreateDirectory("Docs/LittleMontana");
            File.WriteAllText("Docs/LittleMontana/idempotence_receipt.json", "{\"utc\":\"" + DateTime.UtcNow.ToString("O") + "\",\"status\":\"passed\",\"preservedFiles\":" + paths.Length + "}");
        }

        private static void Restore(SceneSetup[] setup)
        {
            if (setup.Any(scene => scene.isLoaded) && setup.Count(scene => scene.isActive) == 1) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Buildings.Presentation;
using Weedtopia.Domain;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class GreenLeafVillageAssetTests
    {
        private const string City = "green_leaf_village";
        private static readonly string[] People = { "gl_elva", "gl_theo", "gl_farah", "gl_casper", "gl_livia", "gl_ruben", "gl_ada", "gl_pekko" };
        private static readonly string[] Architecture = { "NeighbourGreenhouse", "SeedTerrace", "LeafMarketPergola", "CooperativeGate" };
        private static readonly string[] ArchitectureArtwork = { "architecture_greenhouse", "architecture_seed_terrace", "architecture_market_pergola", "architecture_cooperative_gate" };
        private static GameDefinitions Definitions => AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
        private static GameRoot Root => UnityEngine.Object.FindFirstObjectByType<GameRoot>();

        [Test] public void EveryCityHasSerializedTravelToAllSevenCitiesAndItsOwnSitesAndRoutes()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var definitions = Definitions;
                Assert.That(definitions.cities.Count(city => city.introducedVersion <= 8), Is.EqualTo(7));
                foreach (var city in definitions.cities)
                {
                    EditorSceneManager.OpenScene(city.scenePath); var root = Root;
                    Assert.That(root.SceneCityId, Is.EqualTo(city.id));
                    CollectionAssert.AreEquivalent(definitions.instances.Where(i => i.cityId == city.id).Select(i => i.id), root.Sites.Select(site => site.InstanceId));
                    CollectionAssert.AreEquivalent(definitions.routes.Where(route => definitions.instances.Single(i => i.id == route.from).cityId == city.id).Select(route => route.id), root.Presentation.routes.Select(route => route.routeId));
                    CollectionAssert.AreEquivalent(definitions.cities.Select(c => c.id), root.Ui.Campaign.travelRows.Select(row => row.cityId));
                    Assert.That(root.Ui.Campaign.travelRows.All(row => row.progress != null && row.travel != null), Is.True, city.id);
                    Assert.That(EditorBuildSettings.scenes.Any(scene => scene.path == city.scenePath && scene.enabled), Is.True, city.id);
                }
            }
            finally { Restore(setup); }
        }

        [Test] public void EightResidentsHaveSerializedChapterMemoriesAndThreePermanentCommunityPlaces()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(GreenLeafVillageAuthoring.ScenePath); var root = Root; var definitions = Definitions;
                Assert.That(root.Sites.Length, Is.EqualTo(56)); Assert.That(root.Presentation.routes.Length, Is.EqualTo(35));
                var status = (CityStatusBar)new SerializedObject(root.Ui).FindProperty("cityStatus").objectReferenceValue;
                Assert.That(status, Is.Not.Null);
                Assert.That(status.CityName.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(0, -43)));
                Assert.That(status.CityName.rectTransform.sizeDelta.y, Is.EqualTo(34));
                CollectionAssert.AreEquivalent(People, root.StoryWorld.actors.Select(actor => actor.vipId));
                Assert.That(root.StoryWorld.actors.Select(actor => actor.spriteRenderer.sprite.texture).Distinct().Count(), Is.EqualTo(8));
                Assert.That(root.Ui.Stories.rows.Length, Is.EqualTo(11)); Assert.That(root.Ui.Stories.GetComponent<VipStoryPanelLayout>(), Is.Not.Null);
                foreach (var actor in root.StoryWorld.actors)
                {
                    Assert.That(actor.selectionBox, Is.Not.Null, actor.vipId);
                    CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, actor.memories.Select(memory => memory.chapter).Distinct());
                    Assert.That(actor.memories.Length, Is.EqualTo(5), actor.vipId);
                    Assert.That(actor.memories.All(memory => memory.target != null), Is.True, actor.vipId);
                    var sourcePrefabs = actor.memories.Select(memory => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(memory.target)).ToArray();
                    Assert.That(sourcePrefabs.All(path => !string.IsNullOrEmpty(path)), Is.True, actor.vipId);
                    var branches = definitions.vipStories.Single(person => person.id == actor.vipId).chapters[2].choices.Select(choice => choice.id);
                    CollectionAssert.AreEquivalent(branches, actor.memories.Where(memory => memory.chapter == 2).Select(memory => memory.choiceId));
                }
                var places = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<CityPlaceProgressVisual>(true)).ToArray();
                CollectionAssert.AreEquivalent(new[] { "gl_joint_seed_library", "gl_joint_coop_courtyard", "gl_joint_green_leaf_day" }, places.Select(place => place.collaborationId));
                CollectionAssert.AreEquivalent(new[] { 2000, 6500, 12500 }, places.Select(place => place.residents));
                Assert.That(places.All(place => place.cityId == City && place.root == root && place.closedPlace != null && place.growingPlace != null && place.completedPlace != null), Is.True);
                var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(GreenLeafVillageAuthoring.MapPath);
                Assert.That(map, Is.Not.Null); Assert.That(map.districts.Length, Is.EqualTo(4));
                Assert.That(map.districts.All(district => district.labelKey.StartsWith("district.green_leaf_village.", StringComparison.Ordinal)), Is.True);
                Assert.That(scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<BigRockCityStamp>(true)), Is.Empty);
            }
            finally { Restore(setup); }
        }

        [Test] public void BuildingPalettePreservesAll251ApprovedSpritesAndGameplayAnchors()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var source = (BuildingVisualCatalog)new SerializedObject(Root).FindProperty("productionCatalog").objectReferenceValue;
                var target = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(GreenLeafVillageAuthoring.CatalogPath);
                Assert.That(target, Is.Not.Null); Assert.That(target.Families.Sum(family => family.Bands.Count), Is.EqualTo(251));
                foreach (var family in source.Families)
                    foreach (var band in family.Bands)
                    {
                        Assert.That(target.TryGetVisual(family.BuildingId, band.MinLevel, out var prefab), Is.True);
                        Assert.That(target.TryGetVisual(family.BuildingId, band.MaxLevel, out var last), Is.True); Assert.That(last, Is.SameAs(prefab));
                        Assert.That(AssetDatabase.GetAssetPath(prefab), Does.StartWith(GreenLeafVillageAuthoring.Folder + "/Buildings/"));
                        var before = band.WrapperPrefab.GetComponent<BuildingVisualWrapper>(); var after = prefab.GetComponent<BuildingVisualWrapper>();
                        Assert.That(after.BuildingId, Is.EqualTo(before.BuildingId)); Assert.That(after.TierId, Is.EqualTo(before.TierId));
                        var beforeAnchors = new[] { before.InteractionAnchor, before.WorkerAnchor, before.DeliveryInputAnchor, before.DeliveryOutputAnchor, before.VfxAnchor, before.ColliderRoot };
                        var afterAnchors = new[] { after.InteractionAnchor, after.WorkerAnchor, after.DeliveryInputAnchor, after.DeliveryOutputAnchor, after.VfxAnchor, after.ColliderRoot };
                        for (int index = 0; index < beforeAnchors.Length; index++)
                        {
                            Assert.That(afterAnchors[index], Is.Not.Null);
                            Assert.That(afterAnchors[index].localPosition, Is.EqualTo(beforeAnchors[index].localPosition));
                            Assert.That(afterAnchors[index].localRotation, Is.EqualTo(beforeAnchors[index].localRotation));
                            Assert.That(afterAnchors[index].localScale, Is.EqualTo(beforeAnchors[index].localScale));
                        }
                        CollectionAssert.AreEqual(band.WrapperPrefab.GetComponentsInChildren<BoxCollider>(true).Select(box => box.center + "/" + box.size + "/" + box.enabled), prefab.GetComponentsInChildren<BoxCollider>(true).Select(box => box.center + "/" + box.size + "/" + box.enabled));
                        var beforeSprites = band.WrapperPrefab.GetComponentsInChildren<BuildingSpriteVisual>(true); var afterSprites = prefab.GetComponentsInChildren<BuildingSpriteVisual>(true);
                        Assert.That(afterSprites.Length, Is.EqualTo(beforeSprites.Length));
                        for (int index = 0; index < beforeSprites.Length; index++)
                        {
                            Assert.That(afterSprites[index].ResourcePath, Is.EqualTo(beforeSprites[index].ResourcePath));
                            Assert.That(afterSprites[index].SpriteRenderer.sprite, Is.SameAs(beforeSprites[index].SpriteRenderer.sprite));
                            Assert.That(afterSprites[index].transform.localPosition, Is.EqualTo(beforeSprites[index].transform.localPosition));
                            Assert.That(afterSprites[index].transform.localRotation, Is.EqualTo(beforeSprites[index].transform.localRotation));
                            Assert.That(afterSprites[index].transform.localScale, Is.EqualTo(beforeSprites[index].transform.localScale));
                            Assert.That(afterSprites[index].FootprintInset, Is.EqualTo(beforeSprites[index].FootprintInset));
                        }
                    }
            }
            finally { Restore(setup); }
        }

        [Test] public void ResidentWelcomeAndArchitectureArtworkHasSpriteImportsAndActualTransparency()
        {
            foreach (string name in People.Concat(new[] { "town_sign" }).Concat(ArchitectureArtwork))
            {
                string path = GreenLeafVillageAuthoring.Folder + "/Art/" + name + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path); Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
                    var pixels = texture.GetPixels32(); Assert.That(pixels.Any(pixel => pixel.a == 0), Is.True, path);
                    Assert.That(pixels.Any(pixel => pixel.a > 250), Is.True, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
        }

        [Test] public void FourPaintedArchitecturePrefabsKeepGroundAnchorsAndSharedPropsWithoutFacetedArtwork()
        {
            for (int index = 0; index < Architecture.Length; index++)
            {
                string name = Architecture[index];
                string prefabPath = GreenLeafVillageAuthoring.Folder + "/Prefabs/" + name + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(prefab, Is.Not.Null, name);
                var artwork = prefab.transform.Find("ArchitectureArtwork");
                Assert.That(artwork, Is.Not.Null, name);
                var renderer = artwork.GetComponent<SpriteRenderer>();
                Assert.That(renderer, Is.Not.Null, name);
                string artPath = GreenLeafVillageAuthoring.Folder + "/Art/" + ArchitectureArtwork[index] + ".png";
                Assert.That(AssetDatabase.GetAssetPath(renderer.sprite), Is.EqualTo(artPath), name);
                Assert.That(renderer.color, Is.EqualTo(Color.white), name);
                Assert.That(renderer.GetComponent<CityLandmarkVisual>(), Is.Not.Null, name);
                Assert.That(artwork.localPosition, Is.EqualTo(Vector3.zero), name);
                Assert.That(artwork.localScale.x, Is.EqualTo(artwork.localScale.y).Within(.00001f), name);
                Assert.That(renderer.sprite.bounds.size.x * artwork.localScale.x, Is.EqualTo(name == "SeedTerrace" ? 31 : 32).Within(.001f), name);
                var importer = (TextureImporter)AssetImporter.GetAtPath(artPath);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), name);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), name);
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                Assert.That(settings.spritePivot, Is.EqualTo(new Vector2(.5f, .06f)), name);
                Assert.That(prefab.GetComponentsInChildren<SpriteRenderer>(true).Any(sprite => sprite.sprite != null &&
                    AssetDatabase.GetAssetPath(sprite.sprite).StartsWith(GreenLeafVillageAuthoring.Folder + "/Geometry/", StringComparison.Ordinal)), Is.False, name);
                Assert.That(prefab.GetComponentsInChildren<Transform>(true).Any(transform => transform.name.Contains("GeometrySpriteRenderer")), Is.False, name);
                string[] props = name == "NeighbourGreenhouse" ? new[] { "GreenhouseWelcomePlanter", "SharedSeedlingBed" } :
                    name == "SeedTerrace" ? new[] { "TerraceHerbRow", "TerraceSeedRow", "TerraceEndPlanter" } :
                    name == "LeafMarketPergola" ? new[] { "MarketSeedAndProduceTable", "MarketClimbingGarden" } :
                    new[] { "CourtyardLeftPlanter", "CourtyardRightPlanter", "CourtyardClimbingVines" };
                foreach (string prop in props)
                {
                    var child = prefab.transform.Find(prop);
                    Assert.That(child, Is.Not.Null, name + "/" + prop);
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject), Is.Not.Empty, name + "/" + prop);
                }
                string archivedMeta = "Docs/GreenLeafVillage/ArtArchive/FacetedArchitecture/Prefabs/" + name + ".prefab.meta";
                if (File.Exists(archivedMeta))
                    CollectionAssert.AreEqual(File.ReadAllBytes(archivedMeta), File.ReadAllBytes(prefabPath + ".meta"), "Existing prefab GUID changed: " + name);
            }
            Assert.That(Directory.Exists(GreenLeafVillageAuthoring.Folder + "/Geometry"), Is.False, "Retired polygon textures must not remain in production.");
        }
        [Test] public void RepeatedExplicitInstallKeepsSavedSceneAndOwnedAssetBytes()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                GreenLeafVillageAuthoring.Apply(); var before = Snapshot(); GreenLeafVillageAuthoring.Apply(); var after = Snapshot();
                CollectionAssert.AreEquivalent(before.Keys, after.Keys);
                foreach (string path in before.Keys) CollectionAssert.AreEqual(before[path], after[path], "Repeat install changed " + path);
            }
            finally { Restore(setup); }
        }
        private static System.Collections.Generic.Dictionary<string, byte[]> Snapshot() =>
            Directory.GetFiles(GreenLeafVillageAuthoring.Folder, "*", SearchOption.AllDirectories).Concat(new[] { GreenLeafVillageAuthoring.ScenePath })
                .ToDictionary(path => path, File.ReadAllBytes);
        private static void Restore(SceneSetup[] setup)
        { if (setup.Any(scene => scene.isLoaded) && setup.Count(scene => scene.isActive) == 1) EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}

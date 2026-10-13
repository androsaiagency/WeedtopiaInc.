using System;
using System.Collections.Generic;
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
    public sealed class SkunkfurtAssetTests
    {
        private const string City = "skunkfurt";
        private static readonly string[] People = { "sf_alma", "sf_cem", "sf_rika", "sf_jonas", "sf_lotte", "sf_mina" };
        private static readonly string[] Architecture = { "MainBridgeCommons", "CopperMarketHall", "TramDepot", "RiverArchive", "RooftopGardenCourt", "FairFinanceHouse", "StationMusicPavilion", "SkylineDispatchHouse" };
        private static readonly Dictionary<string, string> ProjectArchitecture = new Dictionary<string, string> {
            { "sf_joint_bridge_table", "MainBridgeCommons" }, { "sf_joint_market_commons", "CopperMarketHall" }, { "sf_joint_city_charter", "RiverArchive" } };
        private static GameDefinitions Definitions => AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
        private static GameRoot Root => UnityEngine.Object.FindFirstObjectByType<GameRoot>();

        [Test] public void TwelveCitiesHaveAuthoredTravelRowsAndTheirOwnSitesRoutesAndBuildEntries()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var definitions = Definitions; Assert.That(definitions.cities.Length, Is.GreaterThanOrEqualTo(12));
                foreach (var city in definitions.cities)
                {
                    EditorSceneManager.OpenScene(city.scenePath); var root = Root; Assert.That(root.SceneCityId, Is.EqualTo(city.id));
                    CollectionAssert.AreEquivalent(definitions.instances.Where(i => i.cityId == city.id).Select(i => i.id), root.Sites.Select(site => site.InstanceId));
                    CollectionAssert.AreEquivalent(definitions.routes.Where(route => definitions.instances.Single(i => i.id == route.from).cityId == city.id).Select(route => route.id), root.Presentation.routes.Select(route => route.routeId));
                    CollectionAssert.AreEquivalent(definitions.cities.Select(c => c.id), root.Ui.Campaign.travelRows.Select(row => row.cityId));
                    Assert.That(root.Ui.Campaign.travelRows.All(row => row.progress != null && row.travel != null), Is.True, city.id);
                    Assert.That(EditorBuildSettings.scenes.Any(scene => scene.path == city.scenePath && scene.enabled), Is.True, city.id);
                }
            }
            finally { Restore(setup); }
        }

        [Test] public void SixPermanentResidentsHaveEveryAuthoredChoiceMemoryAndThreeContentLinkedPlaces()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(SkunkfurtAuthoring.ScenePath); var root = Root; var definitions = Definitions;
                Assert.That(root.Sites.Length, Is.EqualTo(56)); Assert.That(root.Presentation.routes.Length, Is.EqualTo(35));
                CollectionAssert.AreEquivalent(People, root.StoryWorld.actors.Select(actor => actor.vipId));
                Assert.That(root.StoryWorld.actors.All(actor => actor.spriteRenderer != null && actor.spriteRenderer.sprite != null), Is.True);
                Assert.That(root.StoryWorld.actors.Select(actor => actor.spriteRenderer.sprite).Distinct().Count(), Is.EqualTo(6));
                Assert.That(root.Ui.Stories.rows.Length, Is.EqualTo(9)); Assert.That(root.Ui.Stories.GetComponent<VipStoryPanelLayout>(), Is.Not.Null);
                foreach (var actor in root.StoryWorld.actors)
                {
                    var person = definitions.vipStories.Single(p => p.id == actor.vipId); Assert.That(actor.selectionBox, Is.Not.Null, actor.vipId);
                    CollectionAssert.AreEquivalent(Enumerable.Range(0, person.chapters.Length), actor.memories.Select(m => m.chapter).Distinct());
                    Assert.That(actor.memories.Length, Is.EqualTo(person.chapters.Sum(chapter => chapter.choices.Length)), actor.vipId);
                    Assert.That(actor.memories.All(memory => memory.target != null), Is.True, actor.vipId);
                    Assert.That(actor.memories.All(memory => !string.IsNullOrEmpty(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(memory.target))), Is.True, actor.vipId);
                    for (int chapter = 0; chapter < person.chapters.Length; chapter++)
                    {
                        var choices = person.chapters[chapter].choices;
                        CollectionAssert.AreEquivalent(choices.Length > 1 ? choices.Select(c => c.id) : new[] { "" }, actor.memories.Where(m => m.chapter == chapter).Select(m => m.choiceId));
                    }
                }
                var places = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<CityPlaceProgressVisual>(true)).ToArray();
                Assert.That(places.Length, Is.EqualTo(3));
                CollectionAssert.AreEquivalent(definitions.vipCollaborations.Where(project => project.cityId == City).Select(p => p.id), places.Select(p => p.collaborationId));
                foreach (var place in places)
                {
                    Assert.That(place.cityId, Is.EqualTo(City)); Assert.That(place.root, Is.SameAs(root));
                    Assert.That(place.closedPlace != null && place.growingPlace != null && place.completedPlace != null, Is.True, place.collaborationId);
                    Assert.That(place.residents, Is.EqualTo(definitions.vipCollaborations.Single(project => project.id == place.collaborationId).quest.residents));
                    Assert.That(new[] { place.closedPlace, place.growingPlace, place.completedPlace }.Distinct().Count(), Is.EqualTo(3));
                    var closedArtwork = place.closedPlace.transform.Find("RestorationArchitecture").GetComponentInChildren<SpriteRenderer>(true);
                    var growingArtwork = place.growingPlace.transform.Find("RestorationArchitecture").GetComponentInChildren<SpriteRenderer>(true);
                    var completedArtwork = place.completedPlace.GetComponentsInChildren<SpriteRenderer>(true).Single(renderer => renderer.sprite == closedArtwork.sprite);
                    Assert.That(closedArtwork.sprite.name, Is.EqualTo(ProjectArchitecture[place.collaborationId]), place.collaborationId);
                    Assert.That(growingArtwork.sprite, Is.SameAs(closedArtwork.sprite));
                    Assert.That(new[] { closedArtwork.color, growingArtwork.color, completedArtwork.color }.Distinct().Count(), Is.EqualTo(3), "The saved place requires three visibly distinct restoration stages.");
                    float artworkWidth = closedArtwork.sprite.bounds.size.x * Mathf.Abs(closedArtwork.transform.lossyScale.x);
                    Assert.That(artworkWidth, Is.GreaterThan(24), "The existing architecture remains readable before completion.");
                    foreach (var stage in new[] { place.closedPlace, place.growingPlace })
                    {
                        var enclosure = stage.transform.Find("RestorationEnclosure").GetComponent<ConstructionFenceView>();
                        Assert.That(enclosure.gameObject.activeSelf, Is.True, "Fence visibility must survive prefab serialization.");
                        Assert.That(enclosure.Sides.Length, Is.EqualTo(4)); Assert.That(enclosure.Posts.Length, Is.EqualTo(4));
                        float enclosureWidth = enclosure.Sides[0].size.x * Mathf.Sqrt(2) * Mathf.Abs(enclosure.transform.localScale.x);
                        Assert.That(enclosureWidth, Is.InRange(artworkWidth + 2, artworkWidth + 6), "Saved fence geometry fits the place footprint.");
                    }
                    foreach (var stage in new[] { place.closedPlace, place.growingPlace, place.completedPlace })
                        Assert.That(stage.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite != null), Is.True, place.collaborationId);
                }
                var cityCharter = places.Single(place => place.collaborationId == "sf_joint_city_charter");
                CollectionAssert.AreEquivalent(new[] { "fair_finance_desk", "housing_council" }, cityCharter.choiceValues);
                Assert.That(cityCharter.choicePlaces.Length, Is.EqualTo(2)); Assert.That(cityCharter.choicePlaces.All(choice => choice != null), Is.True);
                Assert.That(cityCharter.choiceResidentId, Is.EqualTo("sf_jonas")); Assert.That(cityCharter.choiceChapter, Is.EqualTo(2));
                for (int branch = 0; branch < cityCharter.choiceValues.Length; branch++)
                {
                    string architecture = cityCharter.choiceValues[branch] == "fair_finance_desk" ? "FairFinanceHouse" : "RooftopGardenCourt";
                    Assert.That(cityCharter.choicePlaces[branch].GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite != null && renderer.sprite.name == architecture), Is.True, architecture);
                    Assert.That(cityCharter.choicePlaces[branch].transform.localPosition, Is.EqualTo(DistrictMap.ToWorld(new Vector2(0, -30))));
                }
                var jonas = root.StoryWorld.actors.Single(actor => actor.vipId == "sf_jonas");
                foreach (var memory in jonas.memories.Where(memory => memory.chapter == 2))
                {
                    string architecture = memory.choiceId == "fair_finance_desk" ? "FairFinanceHouse" : "RooftopGardenCourt";
                    Assert.That(memory.target.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite != null && renderer.sprite.name == architecture), Is.True, architecture);
                    Assert.That(memory.target.transform.localPosition, Is.EqualTo(DistrictMap.ToWorld(new Vector2(10, -29))));
                }
                var positions = new Dictionary<string, Vector2> { { "sf_joint_bridge_table", new Vector2(112, -90) },
                    { "sf_joint_market_commons", new Vector2(-63, -167) }, { "sf_joint_city_charter", new Vector2(-173, -42) } };
                foreach (var place in places) Assert.That(place.transform.position, Is.EqualTo(DistrictMap.ToWorld(positions[place.collaborationId])));
                var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(SkunkfurtAuthoring.MapPath); Assert.That(map, Is.Not.Null); Assert.That(map.districts.Length, Is.EqualTo(4));
                Assert.That(map.districts.All(district => district.labelKey.StartsWith("district.skunkfurt.", StringComparison.Ordinal)), Is.True);
                Assert.That(scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<SkunkfurtStamp>(true)).Count(), Is.EqualTo(1));
                Assert.That(scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<GreatKushvilleStamp>(true)), Is.Empty);
                Assert.That(scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<BigRockCityStamp>(true)), Is.Empty);
            }
            finally { Restore(setup); }
        }

        [Test] public void All251BuildingVariantsPreserveSpritesTransformsCollidersAndGameplayAnchors()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var source = (BuildingVisualCatalog)new SerializedObject(Root).FindProperty("productionCatalog").objectReferenceValue;
                var target = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(SkunkfurtAuthoring.CatalogPath);
                Assert.That(target, Is.Not.Null); Assert.That(target.Families.Sum(family => family.Bands.Count), Is.EqualTo(251));
                foreach (var family in source.Families) foreach (var band in family.Bands)
                {
                    Assert.That(target.TryGetVisual(family.BuildingId, band.MinLevel, out var prefab), Is.True);
                    Assert.That(target.TryGetVisual(family.BuildingId, band.MaxLevel, out var last), Is.True); Assert.That(last, Is.SameAs(prefab));
                    Assert.That(AssetDatabase.GetAssetPath(prefab), Does.StartWith(SkunkfurtAuthoring.Folder + "/Buildings/"));
                    var before = band.WrapperPrefab.GetComponent<BuildingVisualWrapper>(); var after = prefab.GetComponent<BuildingVisualWrapper>();
                    Assert.That(after.BuildingId, Is.EqualTo(before.BuildingId)); Assert.That(after.TierId, Is.EqualTo(before.TierId));
                    var beforeAnchors = new[] { before.InteractionAnchor, before.WorkerAnchor, before.DeliveryInputAnchor, before.DeliveryOutputAnchor, before.VfxAnchor, before.ColliderRoot };
                    var afterAnchors = new[] { after.InteractionAnchor, after.WorkerAnchor, after.DeliveryInputAnchor, after.DeliveryOutputAnchor, after.VfxAnchor, after.ColliderRoot };
                    for (int i = 0; i < beforeAnchors.Length; i++)
                    {
                        Assert.That(afterAnchors[i], Is.Not.Null); Assert.That(afterAnchors[i].localPosition, Is.EqualTo(beforeAnchors[i].localPosition));
                        Assert.That(afterAnchors[i].localRotation, Is.EqualTo(beforeAnchors[i].localRotation)); Assert.That(afterAnchors[i].localScale, Is.EqualTo(beforeAnchors[i].localScale));
                    }
                    CollectionAssert.AreEqual(band.WrapperPrefab.GetComponentsInChildren<BoxCollider>(true).Select(box => box.center + "/" + box.size + "/" + box.enabled), prefab.GetComponentsInChildren<BoxCollider>(true).Select(box => box.center + "/" + box.size + "/" + box.enabled));
                    var beforeSprites = band.WrapperPrefab.GetComponentsInChildren<BuildingSpriteVisual>(true); var afterSprites = prefab.GetComponentsInChildren<BuildingSpriteVisual>(true);
                    Assert.That(afterSprites.Length, Is.EqualTo(beforeSprites.Length));
                    for (int i = 0; i < beforeSprites.Length; i++)
                    {
                        Assert.That(afterSprites[i].ResourcePath, Is.EqualTo(beforeSprites[i].ResourcePath)); Assert.That(afterSprites[i].SpriteRenderer.sprite, Is.SameAs(beforeSprites[i].SpriteRenderer.sprite));
                        Assert.That(afterSprites[i].transform.localPosition, Is.EqualTo(beforeSprites[i].transform.localPosition)); Assert.That(afterSprites[i].transform.localRotation, Is.EqualTo(beforeSprites[i].transform.localRotation));
                        Assert.That(afterSprites[i].transform.localScale, Is.EqualTo(beforeSprites[i].transform.localScale)); Assert.That(afterSprites[i].FootprintInset, Is.EqualTo(beforeSprites[i].FootprintInset));
                    }
                }
            }
            finally { Restore(setup); }
        }

        [Test] public void AuthoredArchitectureUsesReusablePrefabsWithIndependentGroundAnchors()
        {
            foreach (string name in Architecture)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkunkfurtAuthoring.Folder + "/Prefabs/" + name + ".prefab"); Assert.That(prefab, Is.Not.Null, name);
                Assert.That(prefab.GetComponent<SpriteRenderer>(), Is.Null, "The root remains the independent ground anchor.");
                Assert.That(prefab.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite != null), Is.True, name);
                Assert.That(prefab.GetComponentsInChildren<CityLandmarkVisual>(true).Length, Is.GreaterThan(0), name);
                Assert.That(prefab.transform.localScale.x, Is.EqualTo(prefab.transform.localScale.y).Within(.00001f));
            }
        }

        [Test] public void OriginalResidentArchitectureAndWelcomeArtworkHaveTransparentSpriteImports()
        {
            foreach (string name in new[] { "resident_atlas", "architecture_atlas", "town_sign" })
            {
                string path = SkunkfurtAuthoring.Folder + "/Art/" + name + ".png"; var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path); Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path); Assert.That(importer.alphaIsTransparency, Is.True, path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path); var pixels = texture.GetPixels32();
                    Assert.That(pixels.Any(pixel => pixel.a == 0), Is.True, path); Assert.That(pixels.Any(pixel => pixel.a > 250), Is.True, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            var portraits = AssetDatabase.LoadAllAssetsAtPath(SkunkfurtAuthoring.Folder + "/Art/resident_atlas.png").OfType<Sprite>().ToArray();
            CollectionAssert.AreEquivalent(People.SelectMany(person => new[] { person + "_body", person + "_portrait" }), portraits.Select(sprite => sprite.name));
            var architecture = AssetDatabase.LoadAllAssetsAtPath(SkunkfurtAuthoring.Folder + "/Art/architecture_atlas.png").OfType<Sprite>().ToArray();
            CollectionAssert.AreEquivalent(Architecture, architecture.Select(sprite => sprite.name));
        }
        [Test, Timeout(600000)] public void RepeatedExplicitInstallPreservesCityOwnedAssetsAndAllTwelveSavedSceneBytes()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                SkunkfurtAuthoring.Apply(); var before = Snapshot(); SkunkfurtAuthoring.Apply(); var after = Snapshot();
                CollectionAssert.AreEquivalent(before.Keys, after.Keys); foreach (string path in before.Keys) CollectionAssert.AreEqual(before[path], after[path], "Repeat install changed " + path);
            }
            finally { Restore(setup); }
        }
        private static Dictionary<string, byte[]> Snapshot() => Directory.GetFiles(SkunkfurtAuthoring.Folder, "*", SearchOption.AllDirectories)
            .Concat(Definitions.cities.SelectMany(city => new[] { city.scenePath, city.scenePath + ".meta" }))
            .Distinct().ToDictionary(path => path, File.ReadAllBytes);
        private static void Restore(SceneSetup[] setup)
        { if (setup.Any(scene => scene.isLoaded) && setup.Count(scene => scene.isActive) == 1) EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}

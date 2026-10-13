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
    public sealed class GoldenFlowerAssetTests
    {
        private const string City = "golden_flower";
        private static readonly string[] People = { "gf_alma", "gf_ida", "gf_milan", "gf_nora", "gf_theo" };
        private static readonly string[] Architecture = { "GoldenGlasshouse", "SeedHouse", "CommunityMarket", "FestivalBandstand" };
        private static GameDefinitions Definitions => AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
        private static GameRoot Root => UnityEngine.Object.FindFirstObjectByType<GameRoot>();

        [Test] public void NineCitiesHaveAuthoredTravelRowsAndTheirOwnSitesRoutesAndBuildEntries()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var definitions = Definitions; Assert.That(definitions.cities.Count(city => city.introducedVersion <= 10), Is.EqualTo(9));
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

        [Test] public void FivePermanentResidentsHaveEveryAuthoredChoiceMemoryAndFourContentLinkedPlaces()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(GoldenFlowerAuthoring.ScenePath); var root = Root; var definitions = Definitions;
                Assert.That(root.Sites.Length, Is.EqualTo(56)); Assert.That(root.Presentation.routes.Length, Is.EqualTo(35));
                CollectionAssert.AreEquivalent(People, root.StoryWorld.actors.Select(actor => actor.vipId));
                Assert.That(root.StoryWorld.actors.Select(actor => actor.spriteRenderer.sprite).Distinct().Count(), Is.EqualTo(5));
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
                Assert.That(places.Length, Is.EqualTo(4));
                CollectionAssert.AreEquivalent(definitions.vipCollaborations.Where(project => project.cityId == City).Select(p => p.id), places.Select(p => p.collaborationId));
                foreach (var place in places)
                {
                    Assert.That(place.cityId, Is.EqualTo(City)); Assert.That(place.root, Is.SameAs(root));
                    Assert.That(place.closedPlace != null && place.growingPlace != null && place.completedPlace != null, Is.True, place.collaborationId);
                    Assert.That(place.residents, Is.EqualTo(definitions.vipCollaborations.Single(project => project.id == place.collaborationId).quest.residents));
                    Assert.That(place.completedPlace.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite != null), Is.True, place.collaborationId);
                }
                var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(GoldenFlowerAuthoring.MapPath); Assert.That(map, Is.Not.Null); Assert.That(map.districts.Length, Is.EqualTo(4));
                Assert.That(map.districts.All(district => district.labelKey.StartsWith("district.golden_flower.", StringComparison.Ordinal)), Is.True);
                Assert.That(scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<GoldenFlowerStamp>(true)).Count(), Is.EqualTo(1));
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
                var target = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(GoldenFlowerAuthoring.CatalogPath);
                Assert.That(target, Is.Not.Null); Assert.That(target.Families.Sum(family => family.Bands.Count), Is.EqualTo(251));
                foreach (var family in source.Families) foreach (var band in family.Bands)
                {
                    Assert.That(target.TryGetVisual(family.BuildingId, band.MinLevel, out var prefab), Is.True);
                    Assert.That(target.TryGetVisual(family.BuildingId, band.MaxLevel, out var last), Is.True); Assert.That(last, Is.SameAs(prefab));
                    Assert.That(AssetDatabase.GetAssetPath(prefab), Does.StartWith(GoldenFlowerAuthoring.Folder + "/Buildings/"));
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

        [Test] public void ResidentAtlasArchitectureAtlasAndWelcomeSignHaveTransparentSpriteImports()
        {
            foreach (string name in new[] { "resident_atlas", "architecture_atlas", "town_sign" })
            {
                string path = GoldenFlowerAuthoring.Folder + "/Art/" + name + ".png"; var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path); Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path); Assert.That(importer.alphaIsTransparency, Is.True, path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path); var pixels = texture.GetPixels32();
                    Assert.That(pixels.Any(pixel => pixel.a == 0), Is.True, path); Assert.That(pixels.Any(pixel => pixel.a > 250), Is.True, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            var portraits = AssetDatabase.LoadAllAssetsAtPath(GoldenFlowerAuthoring.Folder + "/Art/resident_atlas.png").OfType<Sprite>().ToArray();
            CollectionAssert.AreEquivalent(People.SelectMany(person => new[] { person + "_body", person + "_portrait" }), portraits.Select(sprite => sprite.name));
            foreach (string name in Architecture)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoldenFlowerAuthoring.Folder + "/Prefabs/" + name + ".prefab"); Assert.That(prefab, Is.Not.Null, name);
                var renderer = prefab.GetComponent<SpriteRenderer>(); Assert.That(renderer != null && renderer.sprite != null, Is.True, name);
                Assert.That(renderer.sprite.name, Is.EqualTo(name)); Assert.That(renderer.color, Is.EqualTo(Color.white)); Assert.That(renderer.GetComponent<CityLandmarkVisual>(), Is.Not.Null);
                Assert.That(prefab.transform.localScale.x, Is.EqualTo(prefab.transform.localScale.y).Within(.00001f));
            }
        }

        [Test] public void RepeatedExplicitInstallPreservesCityOwnedAssetAndSavedSceneBytes()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                GoldenFlowerAuthoring.Apply(); var before = Snapshot(); GoldenFlowerAuthoring.Apply(); var after = Snapshot();
                CollectionAssert.AreEquivalent(before.Keys, after.Keys); foreach (string path in before.Keys) CollectionAssert.AreEqual(before[path], after[path], "Repeat install changed " + path);
            }
            finally { Restore(setup); }
        }
        private static Dictionary<string, byte[]> Snapshot() => Directory.GetFiles(GoldenFlowerAuthoring.Folder, "*", SearchOption.AllDirectories)
            .Concat(new[] { GoldenFlowerAuthoring.ScenePath }).ToDictionary(path => path, File.ReadAllBytes);
        private static void Restore(SceneSetup[] setup)
        { if (setup.Any(scene => scene.isLoaded) && setup.Count(scene => scene.isActive) == 1) EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}

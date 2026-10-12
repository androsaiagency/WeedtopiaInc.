using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Buildings.Presentation;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class SevenHillsAssetTests
    {
        [Test] public void ThreePlayableScenesHaveCompleteAuthoredTravelRowsAndLocalRoutes()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var definitions = AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
                foreach (var entry in new[] { (GameScaffold.GameScene, "burning_hills"), (BigRockCityAuthoring.ScenePath, "big_rock_city"), (SevenHillsAuthoring.ScenePath, "seven_hills") })
                {
                    var scene = EditorSceneManager.OpenScene(entry.Item1);
                    var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameRoot>(true)).Single();
                    Assert.That(root.SceneCityId, Is.EqualTo(entry.Item2));
                    CollectionAssert.AreEquivalent(definitions.instances.Where(i => i.cityId == entry.Item2).Select(i => i.id), root.Sites.Select(s => s.InstanceId));
                    Assert.That(root.Presentation.routes.Length, Is.EqualTo(35));
                    Assert.That(root.Presentation.routes.All(r => definitions.instances.Single(i => i.id == definitions.routes.Single(d => d.id == r.routeId).from).cityId == entry.Item2), Is.True);
                    Assert.That(root.Ui.Campaign.travelRows.Length, Is.EqualTo(definitions.cities.Length));
                    CollectionAssert.AreEquivalent(definitions.cities.Select(city => city.id), root.Ui.Campaign.travelRows.Select(r => r.cityId));
                    Assert.That(root.Ui.Campaign.travelRows.All(r => r.progress != null && r.travel != null), Is.True);
                    Assert.That(root.Ui.Campaign.headerEntry, Is.Not.Null); Assert.That(root.Ui.Campaign.journalEntry, Is.Not.Null);
                    Assert.That(root.Presentation.characters.Single(c => c.speakerId == "rocko").GetComponentInChildren<CityMandateActor>(true).root, Is.SameAs(root));
                }
                Assert.That(EditorBuildSettings.scenes.Any(s => s.path == SevenHillsAuthoring.ScenePath && s.enabled), Is.True);
            }
            finally { Restore(setup); }
        }
        [Test] public void NewCityHasSevenIllustratedPermanentResidentsFourDistrictsAndSevenAuthoredHills()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(SevenHillsAuthoring.ScenePath);
                var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameRoot>(true)).Single();
                var definitions = AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
                Assert.That(root.StoryWorld.actors.Length, Is.EqualTo(7)); Assert.That(root.Ui.Stories.rows.Length, Is.EqualTo(10));
                CollectionAssert.AreEquivalent(definitions.vipStories.Where(v => v.cityId == "seven_hills").Select(v => v.id), root.StoryWorld.actors.Select(a => a.vipId));
                Assert.That(root.StoryWorld.actors.Select(a => a.spriteRenderer.sprite.texture).Distinct().Count(), Is.EqualTo(7));
                Assert.That(root.StoryWorld.actors.All(a => a.memories.Select(m => m.chapter).Distinct().Count() == 4), Is.True);
                Assert.That(root.StoryWorld.actors.All(a => a.selectionBox != null && a.memories.All(m => m.target != null)), Is.True);
                Assert.That(root.Ui.Stories.GetComponent<VipStoryPanelLayout>(), Is.Not.Null, "The current mobile story UI must be retained.");
                var stamp = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SevenHillsStamp>(true)).Single();
                Assert.That(stamp.landmarks, Is.EqualTo(7)); Assert.That(stamp.inhabitants, Is.EqualTo(7));
                Assert.That(stamp.transform.childCount, Is.EqualTo(7)); Assert.That(stamp.hills.Distinct().Count(), Is.EqualTo(7));
                var places = stamp.GetComponentsInChildren<SevenHillsPlaceVisual>(true);
                Assert.That(places.Length, Is.EqualTo(7)); Assert.That(places.All(p => p.root == root && p.closedPlace != null && p.growingPlace != null && p.completedPlace != null), Is.True);
                Assert.That(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BigRockCityStamp>(true)), Is.Empty);
                var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(SevenHillsAuthoring.MapPath);
                Assert.That(map.districts.Length, Is.EqualTo(4)); Assert.That(map.districts.All(d => d.labelKey.StartsWith("district.seven_hills.", StringComparison.Ordinal)), Is.True);
                var signs = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "SevenHillsWelcomeSign").ToArray();
                Assert.That(signs.Length, Is.EqualTo(2));
                foreach (var sign in signs) Assert.That(AssetDatabase.GetAssetPath(sign.GetComponentInChildren<SpriteRenderer>().sprite), Is.EqualTo(SevenHillsAuthoring.Folder + "/Art/town_sign.png"));
            }
            finally { Restore(setup); }
        }
        [Test] public void All251ThirdCityVariantsPreserveSpritesCollidersAndGameplayAnchors()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene); var root = UnityEngine.Object.FindFirstObjectByType<GameRoot>();
                var source = (BuildingVisualCatalog)new SerializedObject(root).FindProperty("productionCatalog").objectReferenceValue;
                var target = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(SevenHillsAuthoring.CatalogPath);
                Assert.That(target.Families.Sum(f => f.Bands.Count), Is.EqualTo(251));
                foreach (var family in source.Families)
                    foreach (var band in family.Bands)
                    {
                        Assert.That(target.TryGetVisual(family.BuildingId, band.MinLevel, out var prefab), Is.True);
                        Assert.That(target.TryGetVisual(family.BuildingId, band.MaxLevel, out var end), Is.True); Assert.That(end, Is.SameAs(prefab));
                        Assert.That(AssetDatabase.GetAssetPath(prefab), Does.StartWith(SevenHillsAuthoring.Folder + "/Buildings/"));
                        var old = band.WrapperPrefab.GetComponent<BuildingVisualWrapper>(); var current = prefab.GetComponent<BuildingVisualWrapper>();
                        Assert.That(current.BuildingId, Is.EqualTo(old.BuildingId)); Assert.That(current.TierId, Is.EqualTo(old.TierId));
                        var a = new[] { old.InteractionAnchor, old.WorkerAnchor, old.DeliveryInputAnchor, old.DeliveryOutputAnchor, old.VfxAnchor, old.ColliderRoot };
                        var b = new[] { current.InteractionAnchor, current.WorkerAnchor, current.DeliveryInputAnchor, current.DeliveryOutputAnchor, current.VfxAnchor, current.ColliderRoot };
                        for (int i = 0; i < a.Length; i++) { Assert.That(b[i], Is.Not.Null); Assert.That(b[i].localPosition, Is.EqualTo(a[i].localPosition)); Assert.That(b[i].localRotation, Is.EqualTo(a[i].localRotation)); }
                        CollectionAssert.AreEqual(band.WrapperPrefab.GetComponentsInChildren<BoxCollider>(true).Select(c => c.center + "/" + c.size), prefab.GetComponentsInChildren<BoxCollider>(true).Select(c => c.center + "/" + c.size));
                        var oldSprites = band.WrapperPrefab.GetComponentsInChildren<BuildingSpriteVisual>(true);
                        var newSprites = prefab.GetComponentsInChildren<BuildingSpriteVisual>(true);
                        Assert.That(newSprites.Length, Is.EqualTo(oldSprites.Length));
                        for (int i = 0; i < oldSprites.Length; i++)
                        {
                            Assert.That(newSprites[i].SpriteRenderer.sprite, Is.SameAs(oldSprites[i].SpriteRenderer.sprite));
                            Assert.That(newSprites[i].transform.localPosition, Is.EqualTo(oldSprites[i].transform.localPosition));
                            Assert.That(newSprites[i].SpriteRenderer.sharedMaterial.shader.name, Is.EqualTo("Weedtopia/Seven Hills/Building Palette"));
                        }
                    }
            }
            finally { Restore(setup); }
        }
        [Test] public void ExplicitRepeatInstallationPreservesAllExistingAssetsAndScenesByteForByte()
        {
            var paths = Directory.GetFiles(SevenHillsAuthoring.Folder, "*", SearchOption.AllDirectories)
                .Concat(new[] { GameScaffold.GameScene, GameScaffold.GameScene + ".meta", BigRockCityAuthoring.ScenePath, BigRockCityAuthoring.ScenePath + ".meta", SevenHillsAuthoring.ScenePath, SevenHillsAuthoring.ScenePath + ".meta", BigRockCityAuthoring.CampaignPath, "Assets/Weedtopia/Game/Content/BurningHills.json", "ProjectSettings/EditorBuildSettings.asset" }).ToArray();
            var before = paths.ToDictionary(p => p, File.ReadAllBytes); SevenHillsAuthoring.Apply();
            foreach (var pair in before) Assert.That(pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)), Is.True, pair.Key);
            Directory.CreateDirectory("Docs/SevenHills");
            File.WriteAllText("Docs/SevenHills/idempotence_receipt.json", "{\"status\":\"passed\",\"preservedFiles\":" + paths.Length + "}");
        }
        private static void Restore(SceneSetup[] setup)
        {
            if (setup.Any(s => s.isLoaded) && setup.Count(s => s.isActive) == 1) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}

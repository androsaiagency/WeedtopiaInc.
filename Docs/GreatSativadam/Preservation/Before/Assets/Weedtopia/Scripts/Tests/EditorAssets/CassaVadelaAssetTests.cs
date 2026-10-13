using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
    public sealed class CassaVadelaAssetTests
    {
        private const string City = "cassa_vadela";
        private static readonly string[] People = { "cv_ines", "cv_mateo", "cv_lucia", "cv_rafael", "cv_sofia", "cv_elio", "cv_amara", "cv_nico" };
        private static GameDefinitions Definitions => AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
        private static GameRoot SceneRoot() => UnityEngine.Object.FindFirstObjectByType<GameRoot>();

        [Test]
        public void EveryCityHasSerializedTravelRowsLocalSitesRoutesAndAnEnabledBuildScene()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var definitions = Definitions;
                Assert.That(definitions.cities.Count(city => city.introducedVersion <= 6), Is.EqualTo(5));
                foreach (var city in definitions.cities)
                {
                    EditorSceneManager.OpenScene(city.scenePath);
                    var root = SceneRoot(); Assert.That(root.SceneCityId, Is.EqualTo(city.id));
                    CollectionAssert.AreEquivalent(definitions.instances.Where(i => i.cityId == city.id).Select(i => i.id), root.Sites.Select(site => site.InstanceId), city.id);
                    CollectionAssert.AreEquivalent(definitions.routes.Where(route => definitions.instances.Single(i => i.id == route.from).cityId == city.id).Select(route => route.id), root.Presentation.routes.Select(route => route.routeId), city.id);
                    CollectionAssert.AreEquivalent(definitions.cities.Select(c => c.id), root.Ui.Campaign.travelRows.Select(row => row.cityId), city.id);
                    Assert.That(root.Ui.Campaign.travelRows.All(row => row.progress != null && row.travel != null), Is.True, city.id);
                    Assert.That(EditorBuildSettings.scenes.Any(scene => scene.path == city.scenePath && scene.enabled), Is.True, city.id);
                }
            }
            finally { Restore(setup); }
        }

        [Test]
        public void FifthCityHasEightAuthoredResidentsThirtyTwoMemoriesAndThreeEvolvingCommunityPlaces()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(CassaVadelaAuthoring.ScenePath); var root = SceneRoot();
                Assert.That(root.Sites.Length, Is.EqualTo(56)); Assert.That(root.Presentation.routes.Length, Is.EqualTo(35));
                CollectionAssert.AreEquivalent(People, root.StoryWorld.actors.Select(actor => actor.vipId));
                Assert.That(root.StoryWorld.actors.Select(actor => actor.spriteRenderer.sprite.texture).Distinct().Count(), Is.EqualTo(8));
                Assert.That(root.Ui.Stories.rows.Length, Is.EqualTo(11));
                Assert.That(root.Ui.Stories.GetComponent<VipStoryPanelLayout>(), Is.Not.Null);
                foreach (var actor in root.StoryWorld.actors)
                {
                    Assert.That(actor.selectionBox, Is.Not.Null, actor.vipId);
                    CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, actor.memories.Select(memory => memory.chapter).Distinct(), actor.vipId);
                    Assert.That(actor.memories.Length, Is.EqualTo(5), actor.vipId);
                    var sources = actor.memories.Select(memory => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(memory.target)).ToArray();
                    Assert.That(sources.All(path => !string.IsNullOrEmpty(path)), Is.True, actor.vipId);
                    Assert.That(sources.Distinct().Count(), Is.EqualTo(5), actor.vipId);
                    var choices = Definitions.vipStories.Single(person => person.id == actor.vipId).chapters[2].choices.Select(choice => choice.id);
                    CollectionAssert.AreEquivalent(choices, actor.memories.Where(memory => memory.chapter == 2).Select(memory => memory.choiceId), actor.vipId);
                }
                var places = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<CityPlaceProgressVisual>(true)).ToArray();
                Assert.That(places.Length, Is.EqualTo(3));
                CollectionAssert.AreEquivalent(new[] { "cv_joint_fountain_market", "cv_joint_open_ceramic_yard", "cv_joint_terrace_walk" }, places.Select(place => place.collaborationId));
                CollectionAssert.AreEquivalent(new[] { 1800, 4500, 9000 }, places.Select(place => place.residents));
                Assert.That(places.All(place => place.cityId == City && place.root == root && place.closedPlace != null && place.growingPlace != null && place.completedPlace != null), Is.True);
                Assert.That(places.Single(place => place.collaborationId == "cv_joint_open_ceramic_yard").choiceValues, Is.EqualTo(new[] { "repair", "teach" }));
                Assert.That(scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<BigRockCityStamp>(true)), Is.Empty);
                var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(CassaVadelaAuthoring.MapPath);
                Assert.That(map.districts.Length, Is.EqualTo(4));
                Assert.That(map.districts.All(district => district.labelKey.StartsWith("district.cassa_vadela.", StringComparison.Ordinal)), Is.True);
                var signs = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<Transform>(true)).Where(obj => obj.name == "CassaVadelaWelcomeSign").ToArray();
                Assert.That(signs.Length, Is.EqualTo(2));
                Assert.That(signs.All(sign => AssetDatabase.GetAssetPath(sign.GetComponentInChildren<SpriteRenderer>().sprite) == CassaVadelaAuthoring.Folder + "/Art/town_sign.png"), Is.True);
            }
            finally { Restore(setup); }
        }

        [Test]
        public void FifthCityBuildingPalettePreservesAll251SourceSpritesGeometryAndGameplayAnchors()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var source = (BuildingVisualCatalog)new SerializedObject(SceneRoot()).FindProperty("productionCatalog").objectReferenceValue;
                var target = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(CassaVadelaAuthoring.CatalogPath);
                Assert.That(target.Families.Sum(family => family.Bands.Count), Is.EqualTo(251));
                foreach (var family in source.Families)
                    foreach (var band in family.Bands)
                    {
                        Assert.That(target.TryGetVisual(family.BuildingId, band.MinLevel, out var prefab), Is.True);
                        Assert.That(target.TryGetVisual(family.BuildingId, band.MaxLevel, out var end), Is.True); Assert.That(end, Is.SameAs(prefab));
                        Assert.That(AssetDatabase.GetAssetPath(prefab), Does.StartWith(CassaVadelaAuthoring.Folder + "/Buildings/"));
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
                            Assert.That(afterSprites[index].SpriteRenderer.sharedMaterial.shader.name, Is.EqualTo("Weedtopia/Cassa Vadela/Building Palette"));
                        }
                    }
            }
            finally { Restore(setup); }
        }

        [Test]
        public void TwelveNewArtworkSourcesHaveRealTransparentPixelsAndSerializedSpriteImports()
        {
            var names = People.Concat(new[] { "town_sign", "fountain_square", "fountain_dry", "terraced_garden" }).ToArray();
            foreach (var name in names)
            {
                string path = CassaVadelaAuthoring.Folder + "/Art/" + name + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path); Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(People.Contains(name) ? SpriteImportMode.Multiple : SpriteImportMode.Single), path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
                    var pixels = texture.GetPixels32();
                    Assert.That(pixels.Any(pixel => pixel.a == 0), Is.True, "Artwork needs actual transparent background: " + path);
                    Assert.That(pixels.Any(pixel => pixel.a > 250), Is.True, "Artwork needs an opaque subject: " + path);
                    if (People.Contains(name)) { Assert.That(texture.width, Is.EqualTo(1024), path); Assert.That(texture.height, Is.EqualTo(1536), path); }
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
        }

        [Test]
        public void ExplicitRepairIsIdempotentAndKeepsManualSceneNamesTransformsMapsAndArtworkReferences()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            byte[] original = File.ReadAllBytes(CassaVadelaAuthoring.ScenePath);
            try
            {
                CassaVadelaAuthoring.Apply(); var before = Snapshot();
                CassaVadelaAuthoring.Apply(); var after = Snapshot();
                CollectionAssert.AreEquivalent(before.Keys, after.Keys);
                foreach (var path in before.Keys) Assert.That(after[path], Is.EqualTo(before[path]), "Repeated explicit repair changed " + path);
                var scene = EditorSceneManager.OpenScene(CassaVadelaAuthoring.ScenePath); var root = SceneRoot();
                var actor = root.StoryWorld.actors.Single(person => person.vipId == "cv_sofia");
                var actorPosition = actor.transform.localPosition + new Vector3(.71f,.19f,.37f); actor.transform.localPosition = actorPosition;
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
                var place = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<CityPlaceProgressVisual>(true)).First();
                var placePosition = place.transform.localPosition + new Vector3(.27f,.15f,.51f); place.transform.localPosition = placePosition;
                place.transform.parent.name = "ManualTerracePlaces"; PrefabUtility.RecordPrefabInstancePropertyModifications(place.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(place.transform.parent.gameObject);
                var manualMap = AssetDatabase.LoadAssetAtPath<DistrictMap>(BigRockCityAuthoring.Folder + "/DistrictMap.asset");
                Assert.That(manualMap, Is.Not.Null);
                string manualMapPath = AssetDatabase.GetAssetPath(manualMap), manualMapGuid = AssetDatabase.AssetPathToGUID(manualMapPath);
                var cameraData = new SerializedObject(root.CameraRig); cameraData.FindProperty("districtMap").objectReferenceValue = manualMap; cameraData.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(root.CameraRig);
                var sign = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<Transform>(true)).First(obj => obj.name == "CassaVadelaWelcomeSign");
                var signRenderer = sign.GetComponentInChildren<SpriteRenderer>(); var manualSprite = root.StoryWorld.actors[0].spriteRenderer.sprite;
                signRenderer.sprite = manualSprite; PrefabUtility.RecordPrefabInstancePropertyModifications(signRenderer);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                var manuallyAuthored = File.ReadAllBytes(CassaVadelaAuthoring.ScenePath);
                CassaVadelaAuthoring.Apply();
                CollectionAssert.AreEqual(manuallyAuthored, File.ReadAllBytes(CassaVadelaAuthoring.ScenePath), "Explicit repair must preserve the saved manual scene byte for byte.");
                EditorSceneManager.OpenScene(CassaVadelaAuthoring.ScenePath); root = SceneRoot();
                Assert.That(root.StoryWorld.actors.Single(person => person.vipId == "cv_sofia").transform.localPosition, Is.EqualTo(actorPosition));
                var restoredMap = new SerializedObject(root.CameraRig).FindProperty("districtMap").objectReferenceValue;
                Assert.That(restoredMap, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(restoredMap), Is.EqualTo(manualMapPath));
                Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(restoredMap)), Is.EqualTo(manualMapGuid));
                Assert.That(UnityEngine.Object.FindFirstObjectByType<CityPlaceProgressVisual>().transform.parent.name, Is.EqualTo("ManualTerracePlaces"));
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                File.WriteAllBytes(CassaVadelaAuthoring.ScenePath, original); AssetDatabase.ImportAsset(CassaVadelaAuthoring.ScenePath, ImportAssetOptions.ForceUpdate);
                Restore(setup);
            }
        }
        [Test]
        public void DefaultLandmarkRepairMovesOnlyItsOwnedUntouchedDefaultsAndKeepsManualEdits()
        {
            var repair = typeof(CassaVadelaAuthoring).GetMethod("RepairLandmarkTargets", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(repair, Is.Not.Null);
            byte[] before = File.ReadAllBytes(CassaVadelaAuthoring.LandmarksPath);
            var world = PrefabUtility.LoadPrefabContents(CassaVadelaAuthoring.LandmarksPath);
            try
            {
                var fountain = world.transform.Find("FountainMarket"); var garden = world.transform.Find("OliveTerraceGarden_1");
                var progress = fountain.GetComponent<CityPlaceProgressVisual>();
                var fountainNext = DistrictMap.ToWorld(new Vector2(143,-76)); var gardenNext = DistrictMap.ToWorld(new Vector2(310,25));
                var fountainBefore = DistrictMap.ToWorld(new Vector2(110,-76)); var gardenBefore = DistrictMap.ToWorld(new Vector2(310,-12));
                fountain.localPosition = fountainBefore; garden.localPosition = gardenBefore;
                Assert.That((bool)repair.Invoke(null, new object[] { world, CassaVadelaAuthoring.LandmarksPath }), Is.True);
                Assert.That(world.transform.Find("FountainMarket"), Is.SameAs(fountain)); Assert.That(world.transform.Find("OliveTerraceGarden_1"), Is.SameAs(garden));
                Assert.That(fountain.localPosition, Is.EqualTo(fountainNext)); Assert.That(garden.localPosition, Is.EqualTo(gardenNext));
                Assert.That((bool)repair.Invoke(null, new object[] { world, CassaVadelaAuthoring.LandmarksPath }), Is.False);
                fountain.localPosition = fountainBefore;
                Assert.That((bool)repair.Invoke(null, new object[] { world, "Assets/Foreign/Manual.prefab" }), Is.False);
                Assert.That(fountain.localPosition, Is.EqualTo(fountainBefore));
                world.name = "ManualTerracePlaces";
                Assert.That((bool)repair.Invoke(null, new object[] { world, CassaVadelaAuthoring.LandmarksPath }), Is.False);
                world.name = Path.GetFileNameWithoutExtension(CassaVadelaAuthoring.LandmarksPath);
                progress.cityId = "manual_city";
                Assert.That((bool)repair.Invoke(null, new object[] { world, CassaVadelaAuthoring.LandmarksPath }), Is.False); progress.cityId = City;
                progress.collaborationId = "manual_project";
                Assert.That((bool)repair.Invoke(null, new object[] { world, CassaVadelaAuthoring.LandmarksPath }), Is.False); progress.collaborationId = "cv_joint_fountain_market";
                fountain.localScale = new Vector3(1.03f, .97f, 1.11f);
                Assert.That((bool)repair.Invoke(null, new object[] { world, CassaVadelaAuthoring.LandmarksPath }), Is.False);
                Assert.That(fountain.localScale, Is.EqualTo(new Vector3(1.03f, .97f, 1.11f))); fountain.localScale = Vector3.one;
                fountain.localRotation = Quaternion.Euler(0,13,0);
                Assert.That((bool)repair.Invoke(null, new object[] { world, CassaVadelaAuthoring.LandmarksPath }), Is.False); fountain.localRotation = Quaternion.identity;
                var manualPosition = fountainBefore + new Vector3(.17f,.23f,.41f); fountain.localPosition = manualPosition;
                Assert.That((bool)repair.Invoke(null, new object[] { world, CassaVadelaAuthoring.LandmarksPath }), Is.False);
                Assert.That(fountain.localPosition, Is.EqualTo(manualPosition)); fountain.localPosition = fountainNext;
                garden.localPosition = gardenBefore; garden.localScale = new Vector3(.91f,1.12f,.94f);
                Assert.That((bool)repair.Invoke(null, new object[] { world, CassaVadelaAuthoring.LandmarksPath }), Is.False);
                Assert.That(garden.localPosition, Is.EqualTo(gardenBefore)); garden.localScale = Vector3.one;
                var gardenSprite = garden.GetComponentInChildren<SpriteRenderer>(); var savedSprite = gardenSprite.sprite; gardenSprite.sprite = null;
                Assert.That((bool)repair.Invoke(null, new object[] { world, CassaVadelaAuthoring.LandmarksPath }), Is.False);
                Assert.That(garden.localPosition, Is.EqualTo(gardenBefore)); gardenSprite.sprite = savedSprite;
            }
            finally { PrefabUtility.UnloadPrefabContents(world); }
            CollectionAssert.AreEqual(before, File.ReadAllBytes(CassaVadelaAuthoring.LandmarksPath), "The isolated fixture must not write the production prefab.");
        }
        [Test]
        public void FountainHasTwoSharedDryStagesAndKeepsRestoredWaterUnderItsCompletedStage()
        {
            var world = PrefabUtility.LoadPrefabContents(CassaVadelaAuthoring.LandmarksPath);
            try
            {
                var place = world.transform.Find("FountainMarket").GetComponent<CityPlaceProgressVisual>();
                Assert.That(place.keepGrowingPlaceAfterCompletion, Is.False);
                Assert.That(place.completedPlace.transform.localPosition, Is.EqualTo(DistrictMap.ToWorld(new Vector2(-14,-6))));
                foreach (var stage in new[] { place.closedPlace, place.growingPlace })
                {
                    var dry = stage.GetComponentsInChildren<SpriteRenderer>(true).Single(renderer => AssetDatabase.GetAssetPath(renderer.sprite) == CassaVadelaAuthoring.Folder + "/Art/fountain_dry.png");
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(dry.gameObject), Is.EqualTo(CassaVadelaAuthoring.DryFountainPath));
                    Assert.That(Vector3.Distance(dry.transform.parent.position, place.transform.position), Is.LessThan(.0001f));
                    Assert.That(dry.transform.lossyScale.x * dry.sprite.bounds.size.y, Is.EqualTo(25).Within(.0001f));
                }
                var wet = place.completedPlace.GetComponentsInChildren<SpriteRenderer>(true).Single(renderer => AssetDatabase.GetAssetPath(renderer.sprite) == CassaVadelaAuthoring.Folder + "/Art/fountain_square.png");
                Assert.That(wet.transform.parent.name, Is.EqualTo("RestoredFountainAnchor"));
                Assert.That(Vector3.Distance(wet.transform.parent.position, place.transform.position), Is.LessThan(.0001f));
                Assert.That(wet.transform.lossyScale.x * wet.sprite.bounds.size.y, Is.EqualTo(25).Within(.0001f));
                Assert.That(place.transform.GetComponentsInChildren<SpriteRenderer>(true).Count(renderer => AssetDatabase.GetAssetPath(renderer.sprite) == CassaVadelaAuthoring.Folder + "/Art/fountain_square.png"), Is.EqualTo(1));
                Assert.That(place.transform.Find("HistoricFountainSquare"), Is.Null);
                Assert.That(world.GetComponentsInChildren<CityPlaceProgressVisual>(true).Where(other => other != place).All(other => other.keepGrowingPlaceAfterCompletion), Is.True);
            }
            finally { PrefabUtility.UnloadPrefabContents(world); }
        }
        [Test]
        public void MemoryClearanceRepairPreservesManualOverridesAndMovesOnlyKnownLinkedDefaults()
        {
            var repair = typeof(CassaVadelaAuthoring).GetMethod("RepairResidentMemoryTargets", BindingFlags.NonPublic | BindingFlags.Static);
            foreach (string id in new[] { "cv_elio", "cv_lucia", "cv_amara" })
            {
                string path = CassaVadelaAuthoring.Folder + "/Prefabs/" + id + ".prefab";
                var before = File.ReadAllBytes(path); var owned = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var actor = owned.GetComponent<VipStoryActor>();
                    var selected = actor.memories.Where(m => id == "cv_elio" ? m.chapter == 1 || m.chapter == 2 : m.chapter == 3).ToArray();
                    var final = selected.ToDictionary(m => m.target,m => m.target.transform.localPosition);
                    foreach (var m in selected) m.target.transform.localPosition = DistrictMap.ToWorld(new Vector2(6 + m.chapter % 2 * 9,-5 - m.chapter / 2 * 13));
                    var manual = selected[0]; var original = manual.target.transform.localPosition; var manualPosition = original + new Vector3(.13f,.19f,.27f);
                    manual.target.transform.localPosition = manualPosition;
                    repair.Invoke(null,new object[]{owned,path}); Assert.That(manual.target.transform.localPosition,Is.EqualTo(manualPosition));
                    foreach (var m in selected.Skip(1)) Assert.That(m.target.transform.localPosition,Is.EqualTo(final[m.target]));
                    manual.target.transform.localPosition = original;
                    Assert.That((bool)repair.Invoke(null,new object[]{owned,"Assets/Foreign/Manual.prefab"}),Is.False);
                    var vip = actor.vipId; actor.vipId = "manual_person";
                    Assert.That((bool)repair.Invoke(null,new object[]{owned,path}),Is.False); actor.vipId = vip;
                    var rotation = manual.target.transform.localRotation; manual.target.transform.localRotation = Quaternion.Euler(0,7,0);
                    Assert.That((bool)repair.Invoke(null,new object[]{owned,path}),Is.False); manual.target.transform.localRotation = rotation;
                    Assert.That((bool)repair.Invoke(null,new object[]{owned,path}),Is.True);
                    foreach (var m in selected) Assert.That(m.target.transform.localPosition,Is.EqualTo(final[m.target]));
                    Assert.That((bool)repair.Invoke(null,new object[]{owned,path}),Is.False);
                }
                finally { PrefabUtility.UnloadPrefabContents(owned); }
                CollectionAssert.AreEqual(before,File.ReadAllBytes(path));
            }
        }
        [Test]
        public void MarketStallRepairKeepsWetFountainFootAndPreservesManualTransforms()
        {
            var repair = typeof(CassaVadelaAuthoring).GetMethod("RepairFountainMarketStall", BindingFlags.NonPublic | BindingFlags.Static);
            var before = File.ReadAllBytes(CassaVadelaAuthoring.LandmarksPath); var owned = PrefabUtility.LoadPrefabContents(CassaVadelaAuthoring.LandmarksPath);
            try
            {
                var place = owned.transform.Find("FountainMarket").GetComponent<CityPlaceProgressVisual>();
                var stage = place.completedPlace; var anchor = stage.transform.Find("RestoredFountainAnchor"); var wet = anchor.Find("HistoricFountainSquare");
                stage.transform.localPosition = DistrictMap.ToWorld(new Vector2(8,-6)); anchor.position = place.transform.position;
                var wetPosition = wet.position; var wetScale = wet.lossyScale; var defaultPosition = stage.transform.localPosition;
                var manual = defaultPosition + new Vector3(.17f,.23f,.41f); stage.transform.localPosition = manual;
                Assert.That((bool)repair.Invoke(null,new object[]{owned,CassaVadelaAuthoring.LandmarksPath}),Is.False);
                Assert.That(stage.transform.localPosition,Is.EqualTo(manual)); stage.transform.localPosition = defaultPosition;
                Assert.That((bool)repair.Invoke(null,new object[]{owned,"Assets/Foreign/Manual.prefab"}),Is.False);
                var wetLocal = wet.localPosition; wet.localPosition += new Vector3(.13f,.19f,.27f);
                Assert.That((bool)repair.Invoke(null,new object[]{owned,CassaVadelaAuthoring.LandmarksPath}),Is.False); wet.localPosition = wetLocal;
                Assert.That((bool)repair.Invoke(null,new object[]{owned,CassaVadelaAuthoring.LandmarksPath}),Is.True);
                Assert.That(place.completedPlace,Is.SameAs(stage)); Assert.That(stage.transform.localPosition,Is.EqualTo(DistrictMap.ToWorld(new Vector2(-14,-6))));
                Assert.That(Vector3.Distance(wet.position,wetPosition),Is.LessThan(.0001f)); Assert.That(Vector3.Distance(wet.lossyScale,wetScale),Is.LessThan(.0001f));
                Assert.That((bool)repair.Invoke(null,new object[]{owned,CassaVadelaAuthoring.LandmarksPath}),Is.False);
            }
            finally { PrefabUtility.UnloadPrefabContents(owned); }
            CollectionAssert.AreEqual(before,File.ReadAllBytes(CassaVadelaAuthoring.LandmarksPath));
        }
        [Test]
        public void DryWetRepairKeepsExistingStageReferencesAndSkipsManuallyChangedArtworkAndTransforms()
        {
            var repair = typeof(CassaVadelaAuthoring).GetMethod("RepairFountainStages", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(repair, Is.Not.Null); byte[] before = File.ReadAllBytes(CassaVadelaAuthoring.LandmarksPath);
            var world = PrefabUtility.LoadPrefabContents(CassaVadelaAuthoring.LandmarksPath);
            try
            {
                var place = world.transform.Find("FountainMarket").GetComponent<CityPlaceProgressVisual>();
                var stages = new[] { place.closedPlace, place.growingPlace, place.completedPlace };
                stages[2].transform.localPosition = DistrictMap.ToWorld(new Vector2(8,-6));
                var wet = place.completedPlace.GetComponentsInChildren<SpriteRenderer>(true).Single(renderer => AssetDatabase.GetAssetPath(renderer.sprite) == CassaVadelaAuthoring.Folder + "/Art/fountain_square.png");
                wet.transform.SetParent(place.transform,false); wet.transform.localPosition = Vector3.zero;
                wet.transform.localRotation = Quaternion.Euler(35.264f,45,0); wet.transform.localScale = Vector3.one * (25f / wet.sprite.bounds.size.y);
                UnityEngine.Object.DestroyImmediate(stages[0].transform.Find("DryFountainClosed").gameObject);
                UnityEngine.Object.DestroyImmediate(stages[1].transform.Find("DryFountainGrowing").gameObject);
                UnityEngine.Object.DestroyImmediate(stages[2].transform.Find("RestoredFountainAnchor").gameObject);
                place.keepGrowingPlaceAfterCompletion = true;
                var manualPosition = new Vector3(.17f,.23f,.41f); wet.transform.localPosition = manualPosition;
                Assert.That((bool)repair.Invoke(null,new object[] { world,CassaVadelaAuthoring.LandmarksPath }), Is.False);
                Assert.That(wet.transform.localPosition, Is.EqualTo(manualPosition)); wet.transform.localPosition = Vector3.zero;
                wet.color = new Color(.9f,.8f,.7f,1);
                Assert.That((bool)repair.Invoke(null,new object[] { world,CassaVadelaAuthoring.LandmarksPath }), Is.False); wet.color = Color.white;
                var source = wet.sprite; wet.sprite = null;
                Assert.That((bool)repair.Invoke(null,new object[] { world,CassaVadelaAuthoring.LandmarksPath }), Is.False); wet.sprite = source;
                var stagePosition = stages[2].transform.localPosition; stages[2].transform.localPosition += new Vector3(.27f,.19f,.37f);
                Assert.That((bool)repair.Invoke(null,new object[] { world,CassaVadelaAuthoring.LandmarksPath }), Is.False); stages[2].transform.localPosition = stagePosition;
                Assert.That((bool)repair.Invoke(null,new object[] { world,"Assets/Foreign/Manual.prefab" }), Is.False);
                Assert.That((bool)repair.Invoke(null,new object[] { world,CassaVadelaAuthoring.LandmarksPath }), Is.True);
                Assert.That(place.closedPlace, Is.SameAs(stages[0])); Assert.That(place.growingPlace, Is.SameAs(stages[1])); Assert.That(place.completedPlace, Is.SameAs(stages[2]));
                Assert.That(wet.transform.parent.parent, Is.SameAs(stages[2].transform)); Assert.That(place.keepGrowingPlaceAfterCompletion, Is.False);
                Assert.That((bool)repair.Invoke(null,new object[] { world,CassaVadelaAuthoring.LandmarksPath }), Is.False);
            }
            finally { PrefabUtility.UnloadPrefabContents(world); }
            CollectionAssert.AreEqual(before,File.ReadAllBytes(CassaVadelaAuthoring.LandmarksPath));
        }
        private static Dictionary<string,string> Snapshot()
        {
            var paths = Directory.GetFiles(CassaVadelaAuthoring.Folder, "*", SearchOption.AllDirectories)
                .Concat(Definitions.cities.Select(city => city.scenePath)).Concat(new[] { BigRockCityAuthoring.CampaignPath, "ProjectSettings/EditorBuildSettings.asset" });
            using (var hash = SHA256.Create()) return paths.Distinct().ToDictionary(path => path, path => BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))));
        }
        private static void Restore(SceneSetup[] setup)
        { if (setup.Any(scene => scene.isLoaded) && setup.Count(scene => scene.isActive) == 1) EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}
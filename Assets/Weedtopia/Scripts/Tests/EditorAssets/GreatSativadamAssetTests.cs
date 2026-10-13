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
    public sealed class GreatSativadamAssetTests
    {
        private const string City = "great_sativadam";
        private static readonly string[] People = { "gs_noor", "gs_bram", "gs_esra", "gs_daan", "gs_fenna", "gs_joost" };
        private static readonly string[] Architecture = { "TideGateCommons", "SeedQuayHall", "FerryLanding", "CanalArchive", "HouseboatGardenCourt", "RepairWharf", "TideMusicPavilion", "WaterDispatchHouse" };
        private static readonly Dictionary<string, string> ProjectArchitecture = new Dictionary<string, string> {
            { "gs_joint_tide_court", "TideGateCommons" }, { "gs_joint_seed_quay", "SeedQuayHall" }, { "gs_joint_open_water_charter", "CanalArchive" } };
        private static GameDefinitions Definitions => AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
        private static GameRoot Root => UnityEngine.Object.FindFirstObjectByType<GameRoot>();

        [Test] public void ThirteenCitiesHaveAuthoredTravelRowsAndTheirOwnSitesRoutesAndBuildEntries()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var definitions = Definitions; Assert.That(definitions.cities.Length, Is.EqualTo(13));
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
                var scene = EditorSceneManager.OpenScene(GreatSativadamAuthoring.ScenePath); var root = Root; var definitions = Definitions;
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
                var cityCharter = places.Single(place => place.collaborationId == "gs_joint_open_water_charter");
                CollectionAssert.AreEquivalent(new[] { "repair_wharf", "learning_dock" }, cityCharter.choiceValues);
                Assert.That(cityCharter.choicePlaces.Length, Is.EqualTo(2)); Assert.That(cityCharter.choicePlaces.All(choice => choice != null), Is.True);
                Assert.That(cityCharter.choiceResidentId, Is.EqualTo("gs_joost")); Assert.That(cityCharter.choiceChapter, Is.EqualTo(2));
                for (int branch = 0; branch < cityCharter.choiceValues.Length; branch++)
                {
                    string architecture = cityCharter.choiceValues[branch] == "repair_wharf" ? "RepairWharf" : "FerryLanding";
                    Assert.That(cityCharter.choicePlaces[branch].GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite != null && renderer.sprite.name == architecture), Is.True, architecture);
                    Assert.That(cityCharter.choicePlaces[branch].transform.localPosition, Is.EqualTo(DistrictMap.ToWorld(new Vector2(0, -30))));
                }
                var joost = root.StoryWorld.actors.Single(actor => actor.vipId == "gs_joost");
                foreach (var memory in joost.memories.Where(memory => memory.chapter == 2))
                {
                    string architecture = memory.choiceId == "repair_wharf" ? "RepairWharf" : "FerryLanding";
                    Assert.That(memory.target.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite != null && renderer.sprite.name == architecture), Is.True, architecture);
                    Assert.That(memory.target.transform.localPosition, Is.EqualTo(DistrictMap.ToWorld(new Vector2(10, -29))));
                }
                var positions = new Dictionary<string, Vector2> { { "gs_joint_tide_court", new Vector2(112, -90) },
                    { "gs_joint_seed_quay", new Vector2(-63, -167) }, { "gs_joint_open_water_charter", new Vector2(-173, -42) } };
                foreach (var place in places) Assert.That(place.transform.position, Is.EqualTo(DistrictMap.ToWorld(positions[place.collaborationId])));
                var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(GreatSativadamAuthoring.MapPath); Assert.That(map, Is.Not.Null); Assert.That(map.districts.Length, Is.EqualTo(4));
                Assert.That(map.districts.All(district => district.labelKey.StartsWith("district.great_sativadam.", StringComparison.Ordinal)), Is.True);
                Assert.That(scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<GreatSativadamStamp>(true)).Count(), Is.EqualTo(1));
                Assert.That(scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<SkunkfurtStamp>(true)), Is.Empty);
                Assert.That(scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<BigRockCityStamp>(true)), Is.Empty);
            }
            finally { Restore(setup); }
        }

        [Test] public void JoostBranchActivitiesKeepGroundOffsetsAndSpriteBoundsIndependentOfArchitectureScale()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(GreatSativadamAuthoring.ScenePath);
                var charter = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<CityPlaceProgressVisual>(true))
                    .Single(place => place.collaborationId == "gs_joint_open_water_charter");
                var joost = Root.StoryWorld.actors.Single(actor => actor.vipId == "gs_joost");
                foreach (string choice in new[] { "repair_wharf", "learning_dock" })
                {
                    bool repair = choice == "repair_wharf";
                    string sourcePath = "Assets/Weedtopia/Production/TownVitality/Prefabs/" + (repair ? "Town_workshop_corner" : "Town_reading_corner") + ".prefab";
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                    Assert.That(source, Is.Not.Null, sourcePath);
                    int branch = Array.IndexOf(charter.choiceValues, choice); Assert.That(branch, Is.GreaterThanOrEqualTo(0));
                    var architecture = charter.choicePlaces[branch].transform;
                    AssertGroundActivity(architecture.Find(repair ? "RepairNeighboursBench" : "OpenLearningBench"), architecture, source, .5f, choice + " community");
                    var memory = joost.memories.Single(item => item.chapter == 2 && item.choiceId == choice).target.transform;
                    AssertGroundActivity(memory.Find("JoostBranchActivity"), memory, source, .45f, choice + " resident memory");
                }
            }
            finally { Restore(setup); }
        }
        private static void AssertGroundActivity(Transform activity, Transform architecture, GameObject source, float scale, string context)
        {
            Assert.That(activity, Is.Not.Null, context);
            Vector3 actualOffset = activity.position - architecture.position;
            Vector3 expectedOffset = DistrictMap.ToWorld(new Vector2(7, -7));
            Assert.That(Vector3.Distance(actualOffset, expectedOffset), Is.LessThan(.002f), context + ": the bench remains next to its architecture ground anchor.");
            Assert.That(actualOffset.magnitude, Is.InRange(9.89f, 9.91f), context + ": imported artwork scaling must not magnify the ground distance.");
            Assert.That(Vector3.Distance(activity.lossyScale, source.transform.lossyScale * scale), Is.LessThan(.0002f), context + ": the prop keeps its authored world size.");
            var actualRenderer = activity.GetComponentsInChildren<SpriteRenderer>(true).Single(renderer => renderer.sprite != null);
            var sourceRenderer = source.GetComponentsInChildren<SpriteRenderer>(true).Single(renderer => renderer.sprite != null);
            Assert.That(actualRenderer.sprite, Is.SameAs(sourceRenderer.sprite), context);
            Vector3 expectedBoundsSize = SourceSpriteBoundsSize(sourceRenderer, scale);
            Assert.That(expectedBoundsSize.magnitude, Is.GreaterThan(1), context);
            Assert.That(Vector3.Distance(actualRenderer.bounds.size, expectedBoundsSize), Is.LessThan(.01f), context + ": actual visible bounds must match the reusable source prop.");
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(activity.gameObject), Is.EqualTo(AssetDatabase.GetAssetPath(source)), context);
        }
        private static Vector3 SourceSpriteBoundsSize(SpriteRenderer source, float scale)
        {
            // The persistent prefab asset need not have native Renderer.bounds initialized.
            // Project its sprite geometry through its authored transform instead.
            Bounds local = source.sprite.bounds;
            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, source.transform.rotation, source.transform.lossyScale * scale);
            Bounds bounds = new Bounds(matrix.MultiplyPoint3x4(local.center), Vector3.zero);
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = local.center + new Vector3(
                    (corner & 1) == 0 ? -local.extents.x : local.extents.x,
                    (corner & 2) == 0 ? -local.extents.y : local.extents.y,
                    (corner & 4) == 0 ? -local.extents.z : local.extents.z);
                bounds.Encapsulate(matrix.MultiplyPoint3x4(point));
            }
            return bounds.size;
        }

        [Test] public void All251BuildingVariantsPreserveSpritesTransformsCollidersAndGameplayAnchors()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(SkunkfurtAuthoring.ScenePath);
                var source = (BuildingVisualCatalog)new SerializedObject(Root).FindProperty("productionCatalog").objectReferenceValue;
                var target = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(GreatSativadamAuthoring.CatalogPath);
                Assert.That(target, Is.Not.Null); Assert.That(target.Families.Sum(family => family.Bands.Count), Is.EqualTo(251));
                foreach (var family in source.Families) foreach (var band in family.Bands)
                {
                    Assert.That(target.TryGetVisual(family.BuildingId, band.MinLevel, out var prefab), Is.True);
                    Assert.That(target.TryGetVisual(family.BuildingId, band.MaxLevel, out var last), Is.True); Assert.That(last, Is.SameAs(prefab));
                    Assert.That(AssetDatabase.GetAssetPath(prefab), Does.StartWith(GreatSativadamAuthoring.Folder + "/Buildings/"));
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
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GreatSativadamAuthoring.Folder + "/Prefabs/" + name + ".prefab"); Assert.That(prefab, Is.Not.Null, name);
                Assert.That(prefab.GetComponent<SpriteRenderer>(), Is.Null, "The root remains the independent ground anchor.");
                Assert.That(prefab.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite != null), Is.True, name);
                Assert.That(prefab.GetComponentsInChildren<CityLandmarkVisual>(true).Length, Is.GreaterThan(0), name);
                Assert.That(prefab.transform.localScale.x, Is.EqualTo(prefab.transform.localScale.y).Within(.00001f));
            }
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(GreatSativadamAuthoring.ScenePath);
                var landmarks = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<GreatSativadamStamp>(true)).Single();
                var boat = landmarks.transform.Find("gs_houseboatcourts/NeighbourhoodHouseboatGarden");
                Assert.That(boat, Is.Not.Null); Assert.That(boat.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(Vector3.Distance(boat.position, DistrictMap.ToWorld(new Vector2(176, 236))), Is.LessThan(.001f));
                Assert.That(Vector3.Distance(boat.lossyScale, Vector3.one * 18.921738f), Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(boat.rotation, Quaternion.identity), Is.LessThan(.001f));
                string sourcePath = GreatSativadamAuthoring.Folder + "/Prefabs/HouseboatGardenCourt.prefab";
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(boat.gameObject), Is.EqualTo(sourcePath));
                var artwork = boat.Find("Artwork").GetComponent<SpriteRenderer>();
                var sourceArtwork = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath).GetComponentInChildren<SpriteRenderer>();
                Assert.That(artwork.sprite, Is.SameAs(sourceArtwork.sprite));
                CollectionAssert.AreEqual(sourceArtwork.sharedMaterials, artwork.sharedMaterials);
                Assert.That(artwork.sprite.bounds.size.x * boat.lossyScale.x, Is.EqualTo(32.3f).Within(.01f));
                var water = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<MeshFilter>(true))
                    .Single(filter => filter.name == "FlowingTurquoiseRiver" && filter.gameObject.activeInHierarchy);
                Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(water.sharedMesh)), Is.EqualTo("8bc7b7807b2e66549a508ac32e864ef0"));
                Vector2 plan = DistrictMap.ToPlan(boat.position), waterSpan = OpaqueWaterSpanAtPlanY(water, plan.y);
                Assert.That(plan.x, Is.InRange(waterSpan.x + 1, waterSpan.y - 1), "The actual serialized opaque canal lies under the berth anchor.");
                Assert.That(80, Is.LessThan(waterSpan.x), "The former dry-land anchor cannot satisfy the water proof.");
                Assert.That(Root.Sites.Min(site => Vector3.Distance(site.transform.position, boat.position)), Is.GreaterThan(80));
                var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(GreatSativadamAuthoring.MapPath);
                float projection = Vector3.ProjectOnPlane(Root.CameraRig.RenderCamera.transform.up, Vector3.up).magnitude;
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1366, 768), new Vector2Int(2560, 1080), new Vector2Int(1080, 1920) })
                {
                    float aspect = size.x / (float)size.y, zoom = 44;
                    Assert.That(Vector2.Distance(map.ClampNavigation(plan, zoom, aspect, projection), plan), Is.LessThan(.001f), size + ": normal navigation reaches the berth without clamping.");
                    Assert.That(map.MaximumNavigationZoom(aspect), Is.GreaterThanOrEqualTo(zoom));
                    Bounds sprite = artwork.sprite.bounds; float scale = boat.lossyScale.x;
                    Assert.That(.5f + sprite.min.x * scale / (2 * zoom * aspect), Is.GreaterThanOrEqualTo(.08f), size.ToString());
                    Assert.That(.5f + sprite.max.x * scale / (2 * zoom * aspect), Is.LessThanOrEqualTo(.92f), size.ToString());
                    Assert.That(.5f + sprite.min.y * scale / (2 * zoom), Is.GreaterThanOrEqualTo(.15f), size.ToString());
                    Assert.That(.5f + sprite.max.y * scale / (2 * zoom), Is.LessThanOrEqualTo(.85f), size.ToString());
                }
            }
            finally { Restore(setup); }
        }
        private static Vector2 OpaqueWaterSpanAtPlanY(MeshFilter water, float y)
        {
            var mesh = water.sharedMesh; var vertices = mesh.vertices; var colors = mesh.colors; var indices = mesh.triangles;
            Assert.That(colors.Length, Is.EqualTo(vertices.Length));
            var intersections = new List<float>();
            for (int i = 0; i < indices.Length; i += 3)
            {
                var triangle = new[] { indices[i], indices[i + 1], indices[i + 2] };
                if (triangle.Any(index => colors[index].a < .999f)) continue;
                for (int edge = 0; edge < 3; edge++)
                {
                    Vector2 a = DistrictMap.ToPlan(water.transform.TransformPoint(vertices[triangle[edge]]));
                    Vector2 b = DistrictMap.ToPlan(water.transform.TransformPoint(vertices[triangle[(edge + 1) % 3]]));
                    if (Mathf.Abs(a.y - b.y) < .00001f || y < Mathf.Min(a.y, b.y) || y > Mathf.Max(a.y, b.y)) continue;
                    intersections.Add(Mathf.Lerp(a.x, b.x, (y - a.y) / (b.y - a.y)));
                }
            }
            Assert.That(intersections, Is.Not.Empty, "Read the real mesh's opaque triangles rather than assuming a river formula.");
            return new Vector2(intersections.Min(), intersections.Max());
        }

        [Test] public void OriginalResidentArchitectureAndWelcomeArtworkHaveTransparentSpriteImports()
        {
            foreach (string name in new[] { "resident_atlas", "architecture_atlas", "town_sign" })
            {
                string path = GreatSativadamAuthoring.Folder + "/Art/" + name + ".png"; var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path); Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path); Assert.That(importer.alphaIsTransparency, Is.True, path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path); var pixels = texture.GetPixels32();
                    Assert.That(pixels.Any(pixel => pixel.a == 0), Is.True, path); Assert.That(pixels.Any(pixel => pixel.a > 250), Is.True, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            var portraits = AssetDatabase.LoadAllAssetsAtPath(GreatSativadamAuthoring.Folder + "/Art/resident_atlas.png").OfType<Sprite>().ToArray();
            CollectionAssert.AreEquivalent(People.SelectMany(person => new[] { person + "_body", person + "_portrait" }), portraits.Select(sprite => sprite.name));
            var architecture = AssetDatabase.LoadAllAssetsAtPath(GreatSativadamAuthoring.Folder + "/Art/architecture_atlas.png").OfType<Sprite>().ToArray();
            CollectionAssert.AreEquivalent(Architecture, architecture.Select(sprite => sprite.name));
        }
        [Test, Timeout(600000)] public void RepeatedExplicitInstallPreservesCityOwnedAssetsAndAllThirteenSavedSceneBytes()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                GreatSativadamAuthoring.Apply(); var before = Snapshot(); GreatSativadamAuthoring.Apply(); var after = Snapshot();
                CollectionAssert.AreEquivalent(before.Keys, after.Keys); foreach (string path in before.Keys) CollectionAssert.AreEqual(before[path], after[path], "Repeat install changed " + path);
            }
            finally { Restore(setup); }
        }
        private static Dictionary<string, byte[]> Snapshot() => Directory.GetFiles(GreatSativadamAuthoring.Folder, "*", SearchOption.AllDirectories)
            .Concat(Definitions.cities.SelectMany(city => new[] { city.scenePath, city.scenePath + ".meta" }))
            .Distinct().ToDictionary(path => path, File.ReadAllBytes);
        private static void Restore(SceneSetup[] setup)
        { if (setup.Any(scene => scene.isLoaded) && setup.Count(scene => scene.isActive) == 1) EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}

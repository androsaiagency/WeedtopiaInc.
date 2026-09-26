using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Weedtopia.Buildings.Presentation;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.BuildingArtPipeline.Tests
{
    public sealed class BuildingManifestTests
    {
        [Test]
        public void ManifestHasAll251UniqueSlotsAndContinuousBands()
        {
            BuildingArtManifest manifest = BuildingManifestReader.Load();
            Assert.That(BuildingManifestReader.ValidateSchema(manifest), Is.Empty);
            Assert.That(manifest.slots.Select(s => s.Key).Distinct().Count(), Is.EqualTo(251));
        }

        [TestCase("../outside.png")]
        [TestCase("Assets//test.prefab")]
        [TestCase("Assets/./test.prefab")]
        [TestCase("C:/outside.png")]
        [TestCase("Assets\\test.prefab")]
        [TestCase("/absolute/path")]
        public void ManifestRejectsUnsafePaths(string path) => Assert.That(BuildingManifestReader.IsRelativeNormalized(path), Is.False);

        [Test]
        public void ManifestRejectsDuplicateIdentityWrongBandAndFaceBudget()
        {
            BuildingArtManifest manifest = BuildingManifestReader.Load();
            manifest.slots[1].tier_id = manifest.slots[0].tier_id;
            manifest.slots[1].min_level = 3;
            manifest.slots[1].target_faces = 16000;
            List<string> errors = BuildingManifestReader.ValidateSchema(manifest);
            Assert.That(errors.Any(e => e.Contains("Duplicate slot")), Is.True);
            Assert.That(errors.Any(e => e.Contains("Incorrect level band")), Is.True);
            Assert.That(errors.Any(e => e.Contains("Face budget")), Is.True);
        }

        [Test]
        public void RuntimeLookupUsesExactBandsAndNeverSpawnsObjects()
        {
            BuildingVisualCatalog catalog = ScriptableObject.CreateInstance<BuildingVisualCatalog>();
            var ruin = new GameObject("ruin");
            var first = new GameObject("first");
            var second = new GameObject("second");
            try
            {
                catalog.EditorSetFamilies(new List<BuildingVisualFamily> { new BuildingVisualFamily("family", new List<BuildingVisualBand>
                {
                    new BuildingVisualBand("T00_RUIN", 0, 0, ruin), new BuildingVisualBand("T01_L001", 1, 4, first),
                    new BuildingVisualBand("T02_L005", 5, 9, second)
                }) });
                Assert.That(catalog.TryGetVisual("family", 0, out GameObject atZero) && atZero == ruin, Is.True);
                Assert.That(catalog.TryGetVisual("family", 4, out GameObject atFour) && atFour == first, Is.True);
                Assert.That(catalog.TryGetVisual("family", 5, out GameObject atFive) && atFive == second, Is.True);
                Assert.That(catalog.TryGetVisual("family", -1, out _), Is.False);
                Assert.That(catalog.TryGetVisual("family", 10, out _), Is.False);
                Assert.That(catalog.TryGetVisual("unknown", 1, out _), Is.False);
            }
            finally { Object.DestroyImmediate(catalog); Object.DestroyImmediate(ruin); Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
        }
    }

    public sealed class BuildingScaffoldIntegrationTests
    {
        private string testRoot;
        private BuildingArtSlot slot;

        [SetUp]
        public void SetUp()
        {
            testRoot = "Assets/Weedtopia/IntakeTests_" + Guid.NewGuid().ToString("N");
            BuildingIntakeService.EnsureAssetFolder(testRoot);
            slot = JsonUtility.FromJson<BuildingArtSlot>(JsonUtility.ToJson(BuildingManifestReader.Load().slots[0]));
            slot.unity_incoming_folder = testRoot + "/Incoming/" + slot.Key;
            slot.unity_prefab_target = testRoot + "/WT_" + slot.building_id + "_" + slot.tier_id + ".prefab";
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(testRoot) && testRoot.StartsWith("Assets/Weedtopia/IntakeTests_", StringComparison.Ordinal)) AssetDatabase.DeleteAsset(testRoot);
        }

        [Test]
        public void FullScaffoldSecondRunLeavesAll251PrefabsAndCatalogByteIdentical()
        {
            BuildingArtManifest manifest = BuildingManifestReader.Load();
            BuildingScaffoldResult first = BuildingIntakeService.CreateOrRepairScaffold(manifest, testRoot);
            Assert.That(first.wrappersCreated, Is.EqualTo(251));
            string prefabPath = testRoot + "/Art/Buildings/Prefabs/" + slot.building_id + "/WT_" + slot.building_id + "_" + slot.tier_id + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                BuildingVisualWrapper wrapper = root.GetComponent<BuildingVisualWrapper>();
                wrapper.WorkerAnchor.localPosition = new Vector3(1.5f, 2.5f, -3f);
                wrapper.VisualRoot.localRotation = Quaternion.Euler(0, 32, 0);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var before = Directory.GetFiles(BuildingManifestReader.AbsolutePath(testRoot), "*", SearchOption.AllDirectories)
                .ToDictionary(p => p, File.ReadAllBytes);
            BuildingScaffoldResult second = BuildingIntakeService.CreateOrRepairScaffold(manifest, testRoot);
            Assert.That(second.wrappersCreated, Is.Zero);
            Assert.That(second.wrappersRepaired, Is.Zero);
            Assert.That(second.wrappersUnchanged, Is.EqualTo(251));
            Assert.That(second.catalogChanged, Is.False);
            foreach (var pair in before) CollectionAssert.AreEqual(pair.Value, File.ReadAllBytes(pair.Key), pair.Key);
            BuildingVisualCatalog catalog = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(testRoot + "/Data/Buildings/BuildingVisualCatalog.asset");
            Assert.That(catalog.Families.Sum(f => f.Bands.Count), Is.EqualTo(251));
            Assert.That(catalog.Families.SelectMany(f => f.Bands).All(b => b.WrapperPrefab != null), Is.True);
        }

        [Test]
        public void ModelUpdatePreservesAuthoredOffsetsButRespectsNewSourceDefaults()
        {
            GameObject sourceA = CreateSource("sourceA", 1f);
            GameObject sourceB = CreateSource("sourceB", 4f);
            byte[] sourceBefore = File.ReadAllBytes(BuildingManifestReader.AbsolutePath(AssetDatabase.GetAssetPath(sourceA)));
            BuildingIntakeService.AssignModel(slot, sourceA);
            GameObject root = PrefabUtility.LoadPrefabContents(slot.unity_prefab_target);
            try
            {
                BuildingVisualWrapper wrapper = root.GetComponent<BuildingVisualWrapper>();
                wrapper.VisualRoot.localPosition = new Vector3(2, 0, 1);
                wrapper.DeliveryInputAnchor.localPosition = new Vector3(-2, 0, 3);
                wrapper.ManagedModel.localScale = Vector3.one * 1.2f;
                wrapper.ManagedModel.Find("Adjusted").localPosition = new Vector3(7, 2, 3);
                var added = new GameObject("HandAuthored");
                added.transform.SetParent(wrapper.ManagedModel.Find("Adjusted"), false);
                added.transform.localPosition = new Vector3(0, 5, 0);
                PrefabUtility.SaveAsPrefabAsset(root, slot.unity_prefab_target);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            BuildingIntakeService.AssignModel(slot, sourceB);
            BuildingVisualWrapper result = AssetDatabase.LoadAssetAtPath<GameObject>(slot.unity_prefab_target).GetComponent<BuildingVisualWrapper>();
            Assert.That(result.SourceModel, Is.EqualTo(sourceB));
            Assert.That(result.VisualRoot.localPosition, Is.EqualTo(new Vector3(2, 0, 1)));
            Assert.That(result.DeliveryInputAnchor.localPosition, Is.EqualTo(new Vector3(-2, 0, 3)));
            Assert.That(result.ManagedModel.localScale, Is.EqualTo(Vector3.one * 1.2f));
            Assert.That(result.ManagedModel.Find("Adjusted").localPosition, Is.EqualTo(new Vector3(7, 2, 3)));
            Assert.That(result.ManagedModel.Find("Adjusted").localScale, Is.EqualTo(Vector3.one * 4f), "Unmodified scale must use the new source default.");
            Assert.That(result.ManagedModel.Find("Unchanged").localPosition, Is.EqualTo(Vector3.right * 4f));
            Assert.That(result.ManagedModel.Find("Adjusted/HandAuthored").localPosition, Is.EqualTo(new Vector3(0, 5, 0)));
            CollectionAssert.AreEqual(sourceBefore, File.ReadAllBytes(BuildingManifestReader.AbsolutePath(AssetDatabase.GetAssetPath(sourceA))));
            byte[] assignedBefore = File.ReadAllBytes(BuildingManifestReader.AbsolutePath(slot.unity_prefab_target));
            BuildingIntakeService.AssignModel(slot, sourceB);
            Assert.That(BuildingIntakeService.EnsureWrapper(slot), Is.False);
            CollectionAssert.AreEqual(assignedBefore, File.ReadAllBytes(BuildingManifestReader.AbsolutePath(slot.unity_prefab_target)));
        }

        [Test]
        public void ReplacementMissingManuallyAdjustedChildDoesNotSave()
        {
            GameObject source = CreateSource("original", 1f);
            BuildingIntakeService.AssignModel(slot, source);
            GameObject root = PrefabUtility.LoadPrefabContents(slot.unity_prefab_target);
            try
            {
                root.GetComponent<BuildingVisualWrapper>().ManagedModel.Find("Adjusted").localPosition = Vector3.up * 9f;
                PrefabUtility.SaveAsPrefabAsset(root, slot.unity_prefab_target);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var replacement = new GameObject("different");
            GameObject replacementAsset;
            try { replacementAsset = PrefabUtility.SaveAsPrefabAsset(replacement, testRoot + "/different.prefab"); }
            finally { Object.DestroyImmediate(replacement); }
            byte[] before = File.ReadAllBytes(BuildingManifestReader.AbsolutePath(slot.unity_prefab_target));
            Assert.Throws<InvalidOperationException>(() => BuildingIntakeService.AssignModel(slot, replacementAsset));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(BuildingManifestReader.AbsolutePath(slot.unity_prefab_target)));
        }

        [Test]
        public void ValidatorDoesNotModifyAssetsOrSourceFiles()
        {
            GameObject source = CreateSource("validationSource", 1f);
            BuildingIntakeService.AssignModel(slot, source);
            var before = Directory.GetFiles(BuildingManifestReader.AbsolutePath(testRoot), "*", SearchOption.AllDirectories).ToDictionary(p => p, File.ReadAllBytes);
            var report = new BuildingValidationReport();
            BuildingIntakeValidator.ValidateSlot(slot, null, report);
            Assert.That(report.warnings, Is.GreaterThan(0));
            foreach (var pair in before) CollectionAssert.AreEqual(pair.Value, File.ReadAllBytes(pair.Key), pair.Key);
        }

        private GameObject CreateSource(string name, float geometryDefault)
        {
            var root = new GameObject(name);
            try
            {
                var adjusted = new GameObject("Adjusted");
                adjusted.transform.SetParent(root.transform, false);
                adjusted.transform.localScale = Vector3.one * geometryDefault;
                var unchanged = new GameObject("Unchanged");
                unchanged.transform.SetParent(root.transform, false);
                unchanged.transform.localPosition = Vector3.right * geometryDefault;
                return PrefabUtility.SaveAsPrefabAsset(root, testRoot + "/" + name + ".prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}

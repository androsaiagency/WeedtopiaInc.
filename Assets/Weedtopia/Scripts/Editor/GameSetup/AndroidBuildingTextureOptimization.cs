using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Weedtopia.Editor.BuildingArtPipeline;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit Android import migration. It never edits source pixels, wrappers or scenes.</summary>
    public static class AndroidBuildingTextureOptimization
    {
        public const string EvidenceFolder = "Docs/CommercialReadiness/AndroidTextures";
        public const int MaximumSize = 1024;
        public const TextureImporterFormat AndroidFormat = TextureImporterFormat.ETC2_RGBA8;

        [Serializable] public sealed class Entry
        {
            public string path, guid, sourceSha256, importedFormat;
            public int sourceWidth, sourceHeight, importedWidth, importedHeight, mipCount;
            public bool androidOverride, policyMatches;
            public string automaticFormat;
            public Vector3 spriteBoundsCenter, spriteBoundsSize;
            public Vector2 spritePivot;
            public Vector2 worldPivot;
            public float spritePixelsPerUnit;
        }
        [Serializable] public sealed class Report
        {
            public string utc, status, activeTarget, buildSubtarget, textureCompressionOverride;
            public int maximumTextureSizeOverride;
            public int textures, changedImports;
            public bool sourcePixelsPreserved, wrapperAndCatalogBytesPreserved, spriteGeometryPreserved, importedAndroidCompressionVerified;
            public bool deviceVisualAcceptance, packageSizeVerified;
            public bool serializedAndroidProbeVerified, originalSpriteMetricsPreserved;
            public string previousTextureCompressionOverride, restoredTextureCompressionOverride;
            public int previousMaximumTextureSizeOverride, restoredMaximumTextureSizeOverride;
            public string detail;
            public Entry[] entries;
        }
        [Serializable] public sealed class ProbeReport
        {
            public string utc, status, bundlePath, sourcePath, sourceSha256, guid, detail, unityVersion, requestedFormat, textureCompressionOverride;
            public int maximumTextureSizeOverride;
            public Entry before, compressedPreview, restored;
            public bool sourcePixelsPreserved, metaBytesPreserved, spriteGeometryPreserved, originalSpriteMetricsPreserved;
            public int requestedMaximumSize;
            public string effectiveTextureCompressionOverride, restoredTextureCompressionOverride;
            public int effectiveMaximumTextureSizeOverride, restoredMaximumTextureSizeOverride;
        }
        [Serializable] private sealed class VerifiedTexture { public int width = default, height = default, formatId = default; }
        [Serializable] private sealed class VerifiedProbe
        {
            public string bundle = default, bundleSha256 = default, sourcePath = default, sourceSha256 = default, guid = default;
            public bool actualEtc2CompressionVerified = default, sourcePixelsPreserved = default, metaBytesPreserved = default, spriteGeometryPreserved = default;
            public VerifiedTexture texture = default;
        }

        [MenuItem("Weedtopia/Build/Probe Android Building ETC2 Serialization")]
        public static void Probe() => ProbeCore(TextureImporterFormat.ETC2_RGBA8, "probe", 2048);

        [MenuItem("Weedtopia/Build/Probe Android Building ASTC 4x4 Serialization")]
        public static void ProbeAstc4() => ProbeCore(TextureImporterFormat.ASTC_4x4, "probe-astc4", 2048);

        [MenuItem("Weedtopia/Build/Probe Android Building ETC2 At 1024")]
        public static void Probe1024() => ProbeCore(TextureImporterFormat.ETC2_RGBA8, "probe-1024", 1024);

        private static void ProbeCore(TextureImporterFormat format, string reportName, int maximumSize = MaximumSize)
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Android must be active for the isolated texture probe.");
            string path = ProductionPaths()[0];
            string folder = Path.GetFullPath(".utmp/CommercialReadiness/AndroidProbe/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(folder);
            byte[] meta = File.ReadAllBytes(path + ".meta");
            var previousCompressionOverride = EditorUserBuildSettings.overrideTextureCompression;
            int previousSizeOverride = EditorUserBuildSettings.overrideMaxTextureSize;
            var report = new ProbeReport { utc = DateTime.UtcNow.ToString("O"), status = "running", sourcePath = path,
                sourceSha256 = Digest(path), guid = AssetDatabase.AssetPathToGUID(path), unityVersion = UnityEngine.Application.unityVersion,
                before = ReadEntry(path), bundlePath = folder + "/building-probe", requestedFormat = format.ToString(), requestedMaximumSize = maximumSize,
                textureCompressionOverride = EditorUserBuildSettings.overrideTextureCompression.ToString(),
                maximumTextureSizeOverride = EditorUserBuildSettings.overrideMaxTextureSize };
            Exception failure = null;
            try
            {
                EditorUserBuildSettings.overrideTextureCompression = OverrideTextureCompression.NoOverride;
                EditorUserBuildSettings.overrideMaxTextureSize = 0;
                report.effectiveTextureCompressionOverride = EditorUserBuildSettings.overrideTextureCompression.ToString();
                report.effectiveMaximumTextureSizeOverride = EditorUserBuildSettings.overrideMaxTextureSize;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var settings = importer.GetPlatformTextureSettings("Android"); Configure(settings); settings.format = format; settings.maxTextureSize = maximumSize;
                importer.SetPlatformTextureSettings(settings); importer.SaveAndReimport();
                report.compressedPreview = ReadEntry(path);
                var manifest = BuildPipeline.BuildAssetBundles(folder,
                    new[] { new AssetBundleBuild { assetBundleName = "building-probe", assetNames = new[] { path } } },
                    BuildAssetBundleOptions.UncompressedAssetBundle | BuildAssetBundleOptions.DisableWriteTypeTree
                    | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.Android);
                if (manifest == null || !File.Exists(report.bundlePath))
                    throw new InvalidOperationException("Isolated Android texture bundle did not build.");
                report.status = "built_pending_serialized_verification";
            }
            catch (Exception exception) { failure = exception; report.status = "failed"; report.detail = exception.ToString(); }
            finally
            {
                EditorUserBuildSettings.overrideTextureCompression = previousCompressionOverride;
                EditorUserBuildSettings.overrideMaxTextureSize = previousSizeOverride;
                report.restoredTextureCompressionOverride = EditorUserBuildSettings.overrideTextureCompression.ToString();
                report.restoredMaximumTextureSizeOverride = EditorUserBuildSettings.overrideMaxTextureSize;
                File.WriteAllBytes(path + ".meta", meta);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                report.restored = ReadEntry(path);
                report.sourcePixelsPreserved = Digest(path) == report.sourceSha256 && AssetDatabase.AssetPathToGUID(path) == report.guid;
                report.metaBytesPreserved = File.ReadAllBytes(path + ".meta").SequenceEqual(meta);
                report.originalSpriteMetricsPreserved = SameGeometry(report.before, report.restored)
                    && (report.compressedPreview == null || SameGeometry(report.before, report.compressedPreview));
                report.spriteGeometryPreserved = SameGeometry(report.before, report.restored)
                    && (report.compressedPreview == null || SameWorldGeometry(report.before, report.compressedPreview));
                Directory.CreateDirectory(EvidenceFolder);
                File.WriteAllText(EvidenceFolder + "/" + reportName + ".json", JsonUtility.ToJson(report, true));
            }
            if (!report.sourcePixelsPreserved || !report.metaBytesPreserved || !report.spriteGeometryPreserved)
                throw new InvalidDataException("Probe did not preserve its source/import/sprite contract. Inspect probe.json.");
            if (failure != null) throw new InvalidOperationException("Android texture probe failed", failure);
        }

        [MenuItem("Weedtopia/Build/Inspect Android Building Textures")]
        public static void Inspect()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            Write("inspection.json", InspectCurrent());
        }

        public static Report InspectCurrent()
        {
            var paths = ProductionPaths();
            var entries = new List<Entry>();
            foreach (string path in paths)
                entries.Add(ReadEntry(path));
            bool android = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
            return new Report
            {
                utc = DateTime.UtcNow.ToString("O"), status = "inspected", textures = entries.Count,
                activeTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                buildSubtarget = EditorUserBuildSettings.androidBuildSubtarget.ToString(), entries = entries.ToArray(),
                textureCompressionOverride = EditorUserBuildSettings.overrideTextureCompression.ToString(),
                maximumTextureSizeOverride = EditorUserBuildSettings.overrideMaxTextureSize,
                importedAndroidCompressionVerified = android && entries.All(e => e.importedFormat == TextureFormat.ETC2_RGBA8.ToString())
            };
        }

        private static Entry ReadEntry(string path)
        {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidDataException("Building texture was not imported: " + path);
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var settings = importer.GetPlatformTextureSettings("Android");
                return new Entry
                {
                    path = path, guid = AssetDatabase.AssetPathToGUID(path), sourceSha256 = Digest(path),
                    sourceWidth = width, sourceHeight = height, androidOverride = settings.overridden,
                    policyMatches = MatchesPolicy(settings), automaticFormat = importer.GetAutomaticFormat("Android").ToString(),
                    importedFormat = texture == null ? "missing" : texture.format.ToString(),
                    importedWidth = texture == null ? 0 : texture.width,
                    importedHeight = texture == null ? 0 : texture.height,
                    mipCount = texture == null ? 0 : texture.mipmapCount,
                    spriteBoundsCenter = sprite == null ? Vector3.zero : sprite.bounds.center,
                    spriteBoundsSize = sprite == null ? Vector3.zero : sprite.bounds.size,
                    spritePivot = sprite == null ? Vector2.zero : sprite.pivot,
                    worldPivot = sprite == null ? Vector2.zero : sprite.pivot / sprite.pixelsPerUnit,
                    spritePixelsPerUnit = sprite == null ? 0 : sprite.pixelsPerUnit
                };
        }

        private static bool SameGeometry(Entry a, Entry b) => Vector3.Distance(a.spriteBoundsCenter, b.spriteBoundsCenter) < .00001f
            && Vector3.Distance(a.spriteBoundsSize, b.spriteBoundsSize) < .00001f
            && Vector2.Distance(a.spritePivot, b.spritePivot) < .00001f
            && Mathf.Abs(a.spritePixelsPerUnit - b.spritePixelsPerUnit) < .00001f;

        private static bool SameWorldGeometry(Entry a, Entry b) => Vector3.Distance(a.spriteBoundsCenter, b.spriteBoundsCenter) < .00001f
            && Vector3.Distance(a.spriteBoundsSize, b.spriteBoundsSize) < .00001f
            && Vector2.Distance(a.worldPivot, b.worldPivot) < .00001f;

        [MenuItem("Weedtopia/Build/Optimize Android Building Texture Imports")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Select Android before this explicit optimization so native compression can be verified.");
            RequireVerifiedProbe();
            var before = InspectCurrent();
            Write("before.json", before);
            string[] paths = before.entries.Select(e => e.path).ToArray();
            var protectedPaths = BuildingManifestReader.Load().slots.Select(s => s.unity_prefab_target)
                .Concat(new[] { BuildingManifestReader.CatalogPath, GameScaffold.GameScene, BigRockCityAuthoring.ScenePath }).ToArray();
            var protectedHashes = protectedPaths.ToDictionary(p => p, Digest);
            var originalMetas = paths.ToDictionary(p => p, p => File.ReadAllBytes(p + ".meta"));
            var changed = new List<string>();
            var previousCompressionOverride = EditorUserBuildSettings.overrideTextureCompression;
            int previousSizeOverride = EditorUserBuildSettings.overrideMaxTextureSize;
            Report outcome = null;
            try
            {
                EditorUserBuildSettings.overrideTextureCompression = OverrideTextureCompression.NoOverride;
                EditorUserBuildSettings.overrideMaxTextureSize = 0;
                foreach (string path in paths)
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    var settings = importer.GetPlatformTextureSettings("Android");
                    if (MatchesPolicy(settings)) continue;
                    string backup = EvidenceFolder + "/OriginalMetas/" + path.Substring(ProductionBuildingIntegration.ResourceFolder.Length) + ".meta";
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    if (!File.Exists(backup)) File.WriteAllBytes(backup, originalMetas[path]);
                    Configure(settings);
                    importer.SetPlatformTextureSettings(settings);
                    changed.Add(path);
                    importer.SaveAndReimport();
                }
                var after = InspectCurrent();
                after.sourcePixelsPreserved = before.entries.All(e => after.entries.Any(a => a.path == e.path && a.guid == e.guid && a.sourceSha256 == e.sourceSha256));
                after.wrapperAndCatalogBytesPreserved = protectedHashes.All(p => Digest(p.Key) == p.Value);
                after.spriteGeometryPreserved = before.entries.All(e => after.entries.Any(a => a.path == e.path && SameWorldGeometry(e, a)));
                after.originalSpriteMetricsPreserved = before.entries.All(e => after.entries.Any(a => a.path == e.path && SameGeometry(e, a)));
                after.changedImports = changed.Count;
                if (!after.sourcePixelsPreserved || !after.wrapperAndCatalogBytesPreserved || !after.spriteGeometryPreserved)
                    throw new InvalidDataException("Protected building art, wrapper or catalog changed during platform import.");
                // A matching desired meta alone is insufficient. The source-bound Android bundle probe
                // proves this exact 1024 ETC2 profile; the fresh APK separately verifies all 251 textures.
                if (!after.entries.All(e => e.policyMatches && e.importedWidth == MaximumSize && e.importedHeight == MaximumSize))
                    throw new InvalidOperationException("Android texture profile/dimensions did not persist for every building.");
                after.serializedAndroidProbeVerified = true;
                after.status = "applied";
                outcome = after;
                Write("optimization.json", after);
            }
            catch (Exception exception)
            {
                foreach (string path in changed)
                {
                    File.WriteAllBytes(path + ".meta", originalMetas[path]);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                }
                var report = InspectCurrent(); report.status = "rolled_back"; report.changedImports = changed.Count;
                outcome = report;
                report.detail = exception.ToString(); Write("optimization.json", report);
                throw;
            }
            finally
            {
                EditorUserBuildSettings.overrideTextureCompression = previousCompressionOverride;
                EditorUserBuildSettings.overrideMaxTextureSize = previousSizeOverride;
                if (outcome != null)
                {
                    outcome.previousTextureCompressionOverride = previousCompressionOverride.ToString();
                    outcome.previousMaximumTextureSizeOverride = previousSizeOverride;
                    outcome.restoredTextureCompressionOverride = EditorUserBuildSettings.overrideTextureCompression.ToString();
                    outcome.restoredMaximumTextureSizeOverride = EditorUserBuildSettings.overrideMaxTextureSize;
                    Write("optimization.json", outcome);
                }
            }
        }

        private static void RequireVerifiedProbe()
        {
            string path = EvidenceFolder + "/probe-1024-serialized-verification.json";
            if (!File.Exists(path)) throw new InvalidOperationException("Run and serialize-verify the isolated 1024 Android probe first.");
            var proof = JsonUtility.FromJson<VerifiedProbe>(File.ReadAllText(path));
            if (proof == null || !proof.actualEtc2CompressionVerified || !proof.sourcePixelsPreserved
                || !proof.metaBytesPreserved || !proof.spriteGeometryPreserved || proof.texture == null
                || proof.texture.formatId != 47 || proof.texture.width != MaximumSize || proof.texture.height != MaximumSize
                || !ProductionPaths().Contains(proof.sourcePath) || !File.Exists(proof.bundle)
                || Digest(proof.bundle) != proof.bundleSha256 || Digest(proof.sourcePath) != proof.sourceSha256
                || AssetDatabase.AssetPathToGUID(proof.sourcePath) != proof.guid)
                throw new InvalidDataException("The 1024 ETC2 Android probe receipt is stale, unbound or unsuccessful.");
        }

        public static string[] ProductionPaths()
        {
            string[] paths = BuildingManifestReader.Load().slots.Select(slot => ProductionBuildingIntegration.SpritePath("WeedtopiaBuildings/" + slot.Key)).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            if (paths.Length != 251 || paths.Distinct().Count() != 251 || paths.Any(p => !File.Exists(p)))
                throw new InvalidDataException("Expected all 251 individually approved building cutouts.");
            return paths;
        }

        public static void Configure(TextureImporterPlatformSettings settings)
        {
            settings.name = "Android"; settings.overridden = true;
            settings.maxTextureSize = MaximumSize; settings.format = AndroidFormat;
            settings.textureCompression = TextureImporterCompression.CompressedHQ;
            settings.compressionQuality = 100; settings.crunchedCompression = false;
            settings.allowsAlphaSplitting = false;
        }

        public static bool MatchesPolicy(TextureImporterPlatformSettings settings) => settings.overridden
            && settings.maxTextureSize == MaximumSize && settings.format == AndroidFormat
            && settings.textureCompression == TextureImporterCompression.CompressedHQ
            && settings.compressionQuality == 100 && !settings.crunchedCompression && !settings.allowsAlphaSplitting;

        private static string Digest(string path)
        {
            using (var hash = SHA256.Create()) using (var stream = File.OpenRead(path))
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static void Write(string name, Report report)
        {
            Directory.CreateDirectory(EvidenceFolder);
            File.WriteAllText(EvidenceFolder + "/" + name, JsonUtility.ToJson(report, true));
        }
    }
}

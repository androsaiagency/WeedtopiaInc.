using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Weedtopia.Editor.GameSetup;

namespace Weedtopia.Tests.Assets
{
    public sealed class ReleaseFingerprintTests
    {
        private string fixture;
        [SetUp] public void CreateFixture()
        {
            fixture = Path.Combine(Path.GetTempPath(), "weedtopia-release-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(fixture, "ProjectSettings"));
            File.WriteAllText(Path.Combine(fixture, "ProjectSettings/ProjectSettings.asset"), "AndroidArchitectures: ARM64");
            File.WriteAllText(Path.Combine(fixture, "scene.unity"), "scene: Burning Hills");
        }
        [TearDown] public void RemoveFixture()
        {
            if (fixture != null && Directory.Exists(fixture)) Directory.Delete(fixture, true);
        }
        private ReleaseInputManifest Capture(params string[] paths)
            => ReleaseFingerprint.Capture(fixture, paths, "target=Android\noptions=0\nprofile=platform");

        [Test] public void ReorderingAndDuplicatePathsKeepTheSameIdentity()
        {
            var first = Capture("scene.unity", "ProjectSettings/ProjectSettings.asset");
            var second = Capture("ProjectSettings\\ProjectSettings.asset", "scene.unity", "scene.unity");
            Assert.That(second.fingerprint, Is.EqualTo(first.fingerprint));
            Assert.That(second.files.Length, Is.EqualTo(2));
            Assert.That(second.schemaVersion, Is.EqualTo(2));
        }
        [Test] public void PlayerSettingsChangesInvalidateTheGateIdentity()
        {
            var before = Capture("scene.unity", "ProjectSettings/ProjectSettings.asset");
            File.WriteAllText(Path.Combine(fixture, "ProjectSettings/ProjectSettings.asset"), "AndroidArchitectures: ARMv7");
            Assert.That(Capture("scene.unity", "ProjectSettings/ProjectSettings.asset").fingerprint, Is.Not.EqualTo(before.fingerprint));
        }
        [Test] public void MissingMandatoryInputIsRecordedAndItsRestorationChangesIdentity()
        {
            var before = Capture("Packages/manifest.json");
            Assert.That(before.files.Single().sha256, Is.EqualTo("missing"));
            Assert.That(before.files.Single().bytes, Is.EqualTo(-1));
            Directory.CreateDirectory(Path.Combine(fixture, "Packages"));
            File.WriteAllText(Path.Combine(fixture, "Packages/manifest.json"), "{}");
            Assert.That(Capture("Packages/manifest.json").fingerprint, Is.Not.EqualTo(before.fingerprint));
        }
        [TestCase("target=StandaloneWindows64\noptions=0\nprofile=platform")]
        [TestCase("target=Android\noptions=1\nprofile=platform")]
        [TestCase("target=Android\noptions=0\nprofile=Assets/Profiles/Release.asset")]
        public void PlatformOptionsAndBuildProfileEachChangeIdentity(string context)
        {
            var before = Capture("scene.unity");
            var after = ReleaseFingerprint.Capture(fixture, new[] { "scene.unity" }, context);
            Assert.That(after.fingerprint, Is.Not.EqualTo(before.fingerprint));
        }
        [TestCase("target=Android\ntextureCompressionOverride=ForceUncompressed\nmaximumTextureSizeOverride=0")]
        [TestCase("target=Android\ntextureCompressionOverride=NoOverride\nmaximumTextureSizeOverride=512")]
        public void EffectiveTextureOverrideContextChangesInvalidateIdentity(string changedContext)
        {
            const string releaseContext = "target=Android\ntextureCompressionOverride=NoOverride\nmaximumTextureSizeOverride=0";
            var baseline = ReleaseFingerprint.Capture(fixture, new[] { "scene.unity" }, releaseContext);
            var changed = ReleaseFingerprint.Capture(fixture, new[] { "scene.unity" }, changedContext);
            Assert.That(changed.fingerprint, Is.Not.EqualTo(baseline.fingerprint));
        }
        [Test] public void CurrentEffectiveTextureOverridesAreRecordedWithoutChangingEditorSettings()
        {
            string context = ProductionVisualPreflight.BuildContext(EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
            Assert.That(context, Does.Contain("textureCompressionOverride=" + EditorUserBuildSettings.overrideTextureCompression));
            Assert.That(context, Does.Contain("textureCompressionOverrideValue=" + (int)EditorUserBuildSettings.overrideTextureCompression));
            Assert.That(context, Does.Contain("maximumTextureSizeOverride=" + EditorUserBuildSettings.overrideMaxTextureSize));
        }
        [Test] public void BuildInputsIncludePlayerSettingsResourceAssetsAndImportMetadata()
        {
            var files = ProductionVisualPreflight.FingerprintInputPaths().ToArray();
            Assert.That(files, Does.Contain("ProjectSettings/ProjectSettings.asset"));
            Assert.That(files, Does.Contain("ProjectSettings/ProjectVersion.txt"));
            var resources = AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/", StringComparison.Ordinal)
                && path.Contains("/Resources/") && !AssetDatabase.IsValidFolder(path)).ToArray();
            Assert.That(resources, Is.Not.Empty, "Current game ships buildings through Resources");
            foreach (string resource in resources)
            {
                Assert.That(files, Does.Contain(resource));
                Assert.That(files, Does.Contain(resource + ".meta"));
            }
        }
        [Test] public void ActualBuildOptionsArePartOfTheRecordedContext()
        {
            var release = ProductionVisualPreflight.BuildContext(EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
            var development = ProductionVisualPreflight.BuildContext(EditorUserBuildSettings.activeBuildTarget, BuildOptions.Development);
            Assert.That(development, Is.Not.EqualTo(release));
            Assert.That(release, Does.Contain("scenes="));
            Assert.That(release, Does.Contain("backend="));
        }
    }
}

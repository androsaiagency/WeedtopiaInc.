using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class ReleaseDiagnosticsTests
    {
        [Test] public void ExplicitReleaseDiagnosticsUsesReleaseCompilerOptionsAndDevelopmentRemainsTheDefault()
        {
            Assert.That(AndroidTestBuild.RequestedBuildOptions(new AndroidTestBuild.Request()), Is.EqualTo(BuildOptions.Development));
            Assert.That(AndroidTestBuild.RequestedBuildOptions(new AndroidTestBuild.Request { releaseDiagnostics = true }), Is.EqualTo(BuildOptions.None));
        }
        [Test] public void EmbeddedPackageContentAndMetadataBothInvalidateReleaseIdentity()
        {
            string folder = Path.Combine(Path.GetTempPath(), "weedtopia-embedded-" + Guid.NewGuid().ToString("N"));
            try
            {
                string package = Path.Combine(folder, "Packages/com.test.embedded"); Directory.CreateDirectory(package);
                File.WriteAllText(Path.Combine(package, "package.json"), "{}"); File.WriteAllText(Path.Combine(package, "sample.hlsl"), "float value=1;");
                File.WriteAllText(Path.Combine(package, "sample.hlsl.meta"), "guid: stable");
                var inputs = ProductionVisualPreflight.EmbeddedPackageInputs(Path.Combine(folder, "Packages")).ToArray();
                Assert.That(inputs.Length, Is.EqualTo(3));
                var original = ReleaseFingerprint.Capture(folder, inputs, "embedded");
                File.WriteAllText(Path.Combine(package, "sample.hlsl"), "float value=2;");
                var changedCode = ReleaseFingerprint.Capture(folder, inputs, "embedded");
                Assert.That(changedCode.fingerprint, Is.Not.EqualTo(original.fingerprint));
                File.WriteAllText(Path.Combine(package, "sample.hlsl.meta"), "guid: changed");
                Assert.That(ReleaseFingerprint.Capture(folder, inputs, "embedded").fingerprint, Is.Not.EqualTo(changedCode.fingerprint));
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        [Test] public void AndroidSymbolContextAndAdaptiveIconLayersAreRecordedAndPreserved()
        {
            string context = ProductionVisualPreflight.BuildContext(BuildTarget.Android, BuildOptions.Development);
            Assert.That(context, Does.Contain("androidSymbolLevel=" + UserBuildSettings.DebugSymbols.level));
            Assert.That(context, Does.Contain("androidSymbolFormat=" + UserBuildSettings.DebugSymbols.format));
            var adaptive = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive);
            Assert.That(adaptive, Is.Not.Empty);
            foreach (var slot in adaptive)
            {
                var layers = slot.GetTextures(); Assert.That(layers.Length, Is.EqualTo(2));
                Assert.That(layers[0], Is.Not.Null); Assert.That(layers[1], Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(layers[0]), Is.EqualTo(GameIconAuthoring.BackgroundPath));
                Assert.That(AssetDatabase.GetAssetPath(layers[1]), Is.EqualTo(GameIconAuthoring.ForegroundPath));
            }
            Assert.That(GameIconAuthoring.DeprecatedAndroidKinds().Count(), Is.EqualTo(2), "Reviewed Android platform advertises the two deprecated kinds without accessing obsolete properties.");
            foreach (var kind in GameIconAuthoring.DeprecatedAndroidKinds())
                Assert.That(PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind).SelectMany(slot => slot.GetTextures()).Any(texture => texture != null), Is.False);
        }
        [Test] public void LocalDiagnosticsDiscardPrivateMessagesArgumentsAndMachinePaths()
        {
            var record = LocalPlayerDiagnostics.CreateEvent(LogType.Exception, "IOException: save for Alice Smith at C:\\Users\\Alice\\secret-save.json failed; token=secret-key",
                "at Weedtopia.Persistence.LocalSaveStore.Save (Alice Smith, secret-key) in C:\\Users\\Alice\\secret-save.json:42\nWeedtopia.Unity.GameRoot:Continue (Alice) (at C:\\Private\\GameRoot.cs:99)\nNative unknown user Alice secret-key");
            string json = JsonUtility.ToJson(record);
            Assert.That(record.code, Is.EqualTo("IOException"));
            Assert.That(record.frames, Is.EqualTo(new[] { "Weedtopia.Persistence.LocalSaveStore.Save", "Weedtopia.Unity.GameRoot.Continue" }));
            foreach (string privateValue in new[] { "Alice", "secret-key", "secret-save", "Private", "Users", ":42", ":99" }) Assert.That(json, Does.Not.Contain(privateValue));
        }
        [Test] public void DiagnosticFramesAndEventsAreBoundedAndUnknownConditionsAreDiscarded()
        {
            var record = LocalPlayerDiagnostics.CreateEvent(LogType.Error, "Alice's private message", string.Concat(Enumerable.Repeat("at Weedtopia.Unity.GameRoot.Continue (Alice)\n", 100)));
            Assert.That(record.code, Is.EqualTo("Unclassified")); Assert.That(record.frames.Length, Is.EqualTo(16));
            Assert.That(LocalPlayerDiagnostics.MaximumEventsPerLaunch, Is.EqualTo(256));
            Assert.That(LocalPlayerDiagnostics.MaximumFileBytes, Is.EqualTo(256 * 1024));
        }
        [TestCase(LocalPlayerDiagnostics.Operation.StartupLoad)]
        [TestCase(LocalPlayerDiagnostics.Operation.OfflineSimulation)]
        [TestCase(LocalPlayerDiagnostics.Operation.OfflineCommit)]
        [TestCase(LocalPlayerDiagnostics.Operation.StartupReady)]
        public void StartupTimingRecordsOnlyTheTechnicalPhaseAndMeasurements(LocalPlayerDiagnostics.Operation operation)
        {
            var record = LocalPlayerDiagnostics.CreateTimingEvent(operation, 1234.5, 28800, 118, 112);
            Assert.That(record.type, Is.EqualTo("Timing")); Assert.That(record.code, Is.EqualTo("StartupPhase"));
            Assert.That(record.operation, Is.EqualTo(operation.ToString())); Assert.That(record.frames, Is.Empty);
            Assert.That(record.elapsedMs, Is.EqualTo(1234.5)); Assert.That(record.offlineSeconds, Is.EqualTo(28800));
            Assert.That(record.workers, Is.EqualTo(118)); Assert.That(record.buildings, Is.EqualTo(112));
        }
        [Test] public void InvalidTimingCannotPolluteTheJournalOrInterruptStartup()
        {
            foreach (double elapsed in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1d })
                Assert.That(LocalPlayerDiagnostics.CreateTimingEvent(LocalPlayerDiagnostics.Operation.StartupLoad, elapsed, 0, 0, 0), Is.Null);
            Assert.That(LocalPlayerDiagnostics.CreateTimingEvent(LocalPlayerDiagnostics.Operation.Save, 0, 0, 0, 0), Is.Null);
            Assert.That(LocalPlayerDiagnostics.CreateTimingEvent(LocalPlayerDiagnostics.Operation.StartupLoad, 0, -1, 0, 0), Is.Null);
            Assert.That(LocalPlayerDiagnostics.CreateTimingEvent(LocalPlayerDiagnostics.Operation.StartupLoad, 0, 0, -1, 0), Is.Null);
            Assert.That(LocalPlayerDiagnostics.CreateTimingEvent(LocalPlayerDiagnostics.Operation.StartupLoad, 0, 0, 0, -1), Is.Null);
        }
        [TestCase(false)] [TestCase(true)] public void ElfBuildIdsAreReadFromNativeNoteHeaders(bool elf64)
        {
            byte[] data = new byte[200]; data[0] = 0x7f; data[1] = (byte)'E'; data[2] = (byte)'L'; data[3] = (byte)'F'; data[4] = (byte)(elf64 ? 2 : 1); data[5] = 1;
            Action<int, byte[]> put = (offset, value) => Array.Copy(value, 0, data, offset, value.Length);
            put(elf64 ? 32 : 28, elf64 ? BitConverter.GetBytes(64UL) : BitConverter.GetBytes(64U));
            put(elf64 ? 54 : 42, BitConverter.GetBytes((ushort)(elf64 ? 56 : 32))); put(elf64 ? 56 : 44, BitConverter.GetBytes((ushort)1));
            put(64, BitConverter.GetBytes(4U)); put(64 + (elf64 ? 8 : 4), elf64 ? BitConverter.GetBytes(128UL) : BitConverter.GetBytes(128U));
            put(64 + (elf64 ? 32 : 16), elf64 ? BitConverter.GetBytes(20UL) : BitConverter.GetBytes(20U));
            put(128, BitConverter.GetBytes(4U)); put(132, BitConverter.GetBytes(4U)); put(136, BitConverter.GetBytes(3U));
            put(140, new byte[] { (byte)'G', (byte)'N', (byte)'U', 0 }); put(144, new byte[] { 0x12, 0xab, 0, 0xef });
            using (var stream = new MemoryStream(data)) Assert.That(NativeSymbolArtifacts.ReadElfBuildId(stream), Is.EqualTo("12ab00ef"));
            put(132, BitConverter.GetBytes(uint.MaxValue));
            using (var malformed = new MemoryStream(data)) Assert.That(NativeSymbolArtifacts.ReadElfBuildId(malformed), Is.Empty);
        }
    }
}

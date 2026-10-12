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
        private static void WithTemporaryBuildRequest(Action<string, string, string> verify)
        {
            string root = Path.Combine(UnityEngine.Application.temporaryCachePath, "weedtopia-build-bootstrap-" + Guid.NewGuid().ToString("N"));
            string source = Path.Combine(root, ".utmp", "explicit.json"), temporary = Path.Combine(root, AndroidTestBuild.RequestPath);
            Directory.CreateDirectory(Path.GetDirectoryName(source));
            try { verify(root, source, temporary); }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
        private static AndroidTestBuild.Request ReadBatchRequest(string[] arguments, string root, string temporary, int owner = 0, string previousRequest = "")
        {
            var method = typeof(AndroidTestBuild).GetMethod("ReadOwnedBatchRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            try { return (AndroidTestBuild.Request)method.Invoke(null, new object[] { arguments, root, temporary, 42, owner, previousRequest }); }
            catch (System.Reflection.TargetInvocationException exception)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
        private static string[] ExplicitRequest(string source) => new[] { "Unity", "-weedtopiaAndroidBuildRequest", source };
        private static byte[] WriteExplicitRequest(string source, string id = "owned-1", bool release = true)
        {
            File.WriteAllText(source, JsonUtility.ToJson(new AndroidTestBuild.Request { id = id, releaseDiagnostics = release }, true) + "\n", new System.Text.UTF8Encoding(true));
            return File.ReadAllBytes(source);
        }
        [Test] public void ExplicitBatchRequestSurvivesStartupTempRemovalAndPreservesOriginalBytes()
        {
            WithTemporaryBuildRequest((root, source, temporary) =>
            {
                byte[] original = WriteExplicitRequest(source);
                Assert.That(File.Exists(temporary), Is.False);
                var request = ReadBatchRequest(ExplicitRequest(source), root, temporary);
                Assert.That(request.id, Is.EqualTo("owned-1")); Assert.That(request.releaseDiagnostics, Is.True);
                Assert.That(File.ReadAllBytes(source).SequenceEqual(original), Is.True);
                Assert.That(File.ReadAllBytes(temporary).SequenceEqual(original), Is.True);
            });
        }
        [Test] public void IdenticalBatchRequestBootstrapIsIdempotentWithoutRewritingTemp()
        {
            WithTemporaryBuildRequest((root, source, temporary) =>
            {
                byte[] original = WriteExplicitRequest(source);
                ReadBatchRequest(ExplicitRequest(source), root, temporary);
                var timestamp = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc); File.SetLastWriteTimeUtc(temporary, timestamp);
                Assert.That(ReadBatchRequest(ExplicitRequest(source), root, temporary, 42, "owned-1").id, Is.EqualTo("owned-1"));
                Assert.That(File.GetLastWriteTimeUtc(temporary), Is.EqualTo(timestamp));
                Assert.That(File.ReadAllBytes(temporary).SequenceEqual(original), Is.True);
            });
        }
        [Test] public void BatchWithoutPersistentArgumentKeepsExistingTempRequestContract()
        {
            WithTemporaryBuildRequest((root, source, temporary) =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(temporary)); byte[] original = WriteExplicitRequest(temporary, "menu-1", false);
                var request = ReadBatchRequest(new[] { "Unity", "-batchmode" }, root, temporary);
                Assert.That(request.id, Is.EqualTo("menu-1")); Assert.That(request.releaseDiagnostics, Is.False);
                Assert.That(File.ReadAllBytes(temporary).SequenceEqual(original), Is.True);
                Assert.That(File.Exists(source), Is.False);
            });
        }
        [Test] public void BatchWithoutArgumentDoesNotDiscoverOrCopyUnrequestedPersistentFiles()
        {
            WithTemporaryBuildRequest((root, source, temporary) =>
            {
                byte[] original = WriteExplicitRequest(source);
                Assert.Throws<FileNotFoundException>(() => ReadBatchRequest(new[] { "Unity" }, root, temporary));
                Assert.That(File.Exists(temporary), Is.False); Assert.That(File.ReadAllBytes(source).SequenceEqual(original), Is.True);
            });
        }
        [TestCase(null)] [TestCase("")] [TestCase("../foreign")] [TestCase("other/request")] [TestCase("a.b")]
        public void InvalidExplicitRequestIdNeverCreatesTemp(string id)
        {
            WithTemporaryBuildRequest((root, source, temporary) =>
            {
                byte[] original = WriteExplicitRequest(source, id);
                Assert.Throws<InvalidOperationException>(() => ReadBatchRequest(ExplicitRequest(source), root, temporary));
                Assert.That(File.Exists(temporary), Is.False); Assert.That(File.ReadAllBytes(source).SequenceEqual(original), Is.True);
            });
        }
        [TestCase("foreign-1", true)] [TestCase("owned-1", false)]
        public void ExplicitRequestNeverReplacesForeignTempBytesEvenWithMatchingId(string existingId, bool release)
        {
            WithTemporaryBuildRequest((root, source, temporary) =>
            {
                byte[] original = WriteExplicitRequest(source);
                Directory.CreateDirectory(Path.GetDirectoryName(temporary)); byte[] foreign = WriteExplicitRequest(temporary, existingId, release);
                Assert.Throws<InvalidOperationException>(() => ReadBatchRequest(ExplicitRequest(source), root, temporary));
                Assert.That(File.ReadAllBytes(temporary).SequenceEqual(foreign), Is.True);
                Assert.That(File.ReadAllBytes(source).SequenceEqual(original), Is.True);
            });
        }
        [TestCase(99, "")] [TestCase(42, "different-1")]
        public void BatchOwnershipIsCheckedBeforeAnyRequestCopy(int owner, string previousRequest)
        {
            WithTemporaryBuildRequest((root, source, temporary) =>
            {
                WriteExplicitRequest(source);
                Assert.Throws<InvalidOperationException>(() => ReadBatchRequest(ExplicitRequest(source), root, temporary, owner, previousRequest));
                Assert.That(File.Exists(temporary), Is.False);
            });
        }
        [TestCase(".utmp/../../outside.json")] [TestCase(".utmp-foreign/request.json")]
        [TestCase("Temp/request.json")] [TestCase(".utmp/request.txt")]
        public void PersistentRequestMustRemainInsideExactProjectStagingFolder(string relative)
        {
            WithTemporaryBuildRequest((root, source, temporary) =>
            {
                WriteExplicitRequest(source);
                Assert.Throws<IOException>(() => ReadBatchRequest(ExplicitRequest(relative), root, temporary));
                Assert.That(File.Exists(temporary), Is.False);
            });
        }
        [Test] public void AbsoluteForeignProjectRequestIsRejectedBeforeReadingIt()
        {
            WithTemporaryBuildRequest((root, source, temporary) =>
            {
                Assert.Throws<IOException>(() => ReadBatchRequest(ExplicitRequest(Path.Combine(root + "-foreign", ".utmp", "request.json")), root, temporary));
                Assert.That(File.Exists(temporary), Is.False);
            });
        }
        [TestCase(false)] [TestCase(true)] public void ExplicitCliArgumentRequiresOnePath(bool nextIsFlag)
        {
            WithTemporaryBuildRequest((root, source, temporary) =>
            {
                string[] arguments = nextIsFlag ? new[] { "Unity", "-weedtopiaAndroidBuildRequest", "-batchmode" } : new[] { "Unity", "-weedtopiaAndroidBuildRequest" };
                Assert.Throws<InvalidOperationException>(() => ReadBatchRequest(arguments, root, temporary));
                Assert.That(File.Exists(temporary), Is.False);
            });
        }
        [Test] public void DuplicateExplicitCliArgumentIsRejectedBeforeCreatingTemp()
        {
            WithTemporaryBuildRequest((root, source, temporary) =>
            {
                WriteExplicitRequest(source);
                Assert.Throws<InvalidOperationException>(() => ReadBatchRequest(new[] { "Unity", "-weedtopiaAndroidBuildRequest", source, "-weedtopiaAndroidBuildRequest", source }, root, temporary));
                Assert.That(File.Exists(temporary), Is.False);
            });
        }
        [Test] public void RelativeProjectRequestIsExplicitlyResolvedWithoutChangingWorkingDirectory()
        {
            WithTemporaryBuildRequest((root, source, temporary) =>
            {
                WriteExplicitRequest(source);
                Assert.That(ReadBatchRequest(ExplicitRequest(".utmp/explicit.json"), root, temporary).id, Is.EqualTo("owned-1"));
                Assert.That(File.Exists(temporary), Is.True);
            });
        }

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
            Assert.That(GameIconAuthoring.DeprecatedAndroidKinds(), Is.Not.Empty, "The installed Android platform must expose deprecated icon kinds; their inventory can vary by Unity version.");
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
        private static void WithTemporaryJournal(Action<string> verify)
        {
            string folder = Path.Combine(UnityEngine.Application.temporaryCachePath, "weedtopia-journal-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            var type = typeof(LocalPlayerDiagnostics);
            var gateField = type.GetField("Gate", flags); var fileField = type.GetField("file", flags); var eventsField = type.GetField("events", flags);
            Assert.That(gateField, Is.Not.Null); Assert.That(fileField, Is.Not.Null); Assert.That(eventsField, Is.Not.Null);
            try
            {
                lock (gateField.GetValue(null))
                {
                    object previousFile = fileField.GetValue(null), previousEvents = eventsField.GetValue(null);
                    try
                    {
                        fileField.SetValue(null, Path.Combine(folder, "events.ndjson")); eventsField.SetValue(null, 0);
                        verify(folder);
                    }
                    finally { fileField.SetValue(null, previousFile); eventsField.SetValue(null, previousEvents); }
                }
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        [Test] public void LocalJournalRotatesAndExportsOnlyBoundedCompleteEvents()
        {
            WithTemporaryJournal(folder =>
            {
                string file = Path.Combine(folder, "events.ndjson");
                // Synthetic content at the exact byte limit exercises rotation on the next technical event.
                string existing = new string('x', LocalPlayerDiagnostics.MaximumFileBytes); File.WriteAllText(file, existing);
                Assert.That(LocalPlayerDiagnostics.ReadSnapshot(), Is.Empty, "An unfinished journal line is excluded from an explicit export.");
                LocalPlayerDiagnostics.RecordTiming(LocalPlayerDiagnostics.Operation.StartupReady, 1234, 28800, 118, 112);
                Assert.That(File.ReadAllText(file + ".previous"), Is.EqualTo(existing));
                Assert.That(new FileInfo(file + ".previous").Length, Is.LessThanOrEqualTo(LocalPlayerDiagnostics.MaximumFileBytes));
                Assert.That(new FileInfo(file).Length, Is.LessThanOrEqualTo(LocalPlayerDiagnostics.MaximumFileBytes));
                string snapshot = LocalPlayerDiagnostics.ReadSnapshot(); Assert.That(snapshot, Is.EqualTo(File.ReadAllText(file)));
                var record = JsonUtility.FromJson<LocalPlayerDiagnostics.TechnicalEvent>(snapshot);
                Assert.That(record.operation, Is.EqualTo("StartupReady")); Assert.That(record.offlineSeconds, Is.EqualTo(28800));
                // A second rotation replaces only the owned previous file, keeping exactly one backup.
                File.WriteAllText(file, new string('y', LocalPlayerDiagnostics.MaximumFileBytes));
                LocalPlayerDiagnostics.RecordTiming(LocalPlayerDiagnostics.Operation.StartupLoad, 10, 0, 0, 0);
                Assert.That(File.ReadAllText(file + ".previous"), Is.EqualTo(new string('y', LocalPlayerDiagnostics.MaximumFileBytes)));
                Assert.That(Directory.GetFiles(folder).Length, Is.EqualTo(2));
                Assert.That(new FileInfo(file).Length, Is.LessThanOrEqualTo(LocalPlayerDiagnostics.MaximumFileBytes));
                Assert.That(new FileInfo(file + ".previous").Length, Is.LessThanOrEqualTo(LocalPlayerDiagnostics.MaximumFileBytes));
            });
        }
        [Test] public void LocalJournalQuotaRejectsThe257thEvent()
        {
            WithTemporaryJournal(folder =>
            {
                typeof(LocalPlayerDiagnostics).GetMethod("Write", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    .Invoke(null, new object[]{LocalPlayerDiagnostics.CreateLaunchEvent()});
                for (int i = 1; i < LocalPlayerDiagnostics.MaximumEventsPerLaunch; i++)
                    LocalPlayerDiagnostics.RecordTiming(LocalPlayerDiagnostics.Operation.OfflineSimulation, i, 28800, 118, 112);
                string before = LocalPlayerDiagnostics.ReadSnapshot();
                string[] lines=File.ReadAllLines(Path.Combine(folder, "events.ndjson"));
                Assert.That(lines.Length, Is.EqualTo(256));
                Assert.That(JsonUtility.FromJson<LocalPlayerDiagnostics.TechnicalEvent>(lines[0]).code, Is.EqualTo("PlayerStarted"));
                LocalPlayerDiagnostics.RecordTiming(LocalPlayerDiagnostics.Operation.StartupReady, 9999, 28800, 118, 112);
                Assert.That(LocalPlayerDiagnostics.ReadSnapshot(), Is.EqualTo(before));
                Assert.That(new FileInfo(Path.Combine(folder, "events.ndjson")).Length, Is.LessThanOrEqualTo(LocalPlayerDiagnostics.MaximumFileBytes));
            });
        }
        [Test] public void LocalJournalIOFailureCannotBreakStartupOrReadSnapshot()
        {
            WithTemporaryJournal(folder =>
            {
                var field = typeof(LocalPlayerDiagnostics).GetField("file", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                field.SetValue(null, folder); // A directory cannot be opened as a journal file.
                Assert.DoesNotThrow(() => LocalPlayerDiagnostics.RecordTiming(LocalPlayerDiagnostics.Operation.StartupLoad, 1, 0, 0, 0));
                Assert.That(LocalPlayerDiagnostics.ReadSnapshot(), Is.Empty);
                field.SetValue(null, Path.Combine(folder, "missing-parent/events.ndjson"));
                Assert.DoesNotThrow(() => LocalPlayerDiagnostics.RecordTiming(LocalPlayerDiagnostics.Operation.StartupLoad, 1, 0, 0, 0));
                Assert.That(LocalPlayerDiagnostics.ReadSnapshot(), Is.Empty);
                Assert.That(Directory.GetFiles(folder).Length, Is.EqualTo(0));
            });
        }

    }
}

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit complete regression with editor-state restoration. Only an explicitly owned batch process exits after restoration.</summary>
    [InitializeOnLoad]
    public static class CommercialReadinessTestJob
    {
        private const string Key = "Weedtopia.CommercialReadinessTests";
        public const string Folder = "Docs/CommercialReadiness/2026-10-06/Tests";
        private const string RepairFolder = "Docs/CommercialReadiness/2026-10-05/Tests";
        public const string StartupBenchmarkFolder = "Docs/CommercialReadiness/2026-10-06/StartupBenchmarkTests";
        private const string FullScope = "complete_editmode_then_playmode";
        private const string StartupScope = "focused_preservation_then_responsive_startup_benchmark";
        private const string RepairScope = "native_editmode_repair_aggregate_then_complete_playmode";
        private const string RepairPriorRun = "20261005T053428609-258904";
        private const string RepairTest = "Weedtopia.Tests.SimulationBatchTests.HouseCompletionInvalidatesResidentsWithinTheSameBatch";
        private const string RepairTestPath = "Assets/Weedtopia/Scripts/Tests/EditMode/SimulationBatchTests.cs";
        private const string CorrectedTestSha = "8b1f98832b9c0b4257f5fe3d58df99d2b205d2226ef26c963b8ce53acedd5f9d";
        private const string PriorInputsSha = "f9d4196509ad70b43657b7b119a8c24808db22d5e1c347064ff1885166df1dbf";
        private const string PriorXmlSha = "ffbd90e5a93bdee24d18a3e605bbf4ddd2b3c5a7bde294509e543038950cfd29";
        private static readonly string[] RepairAllowedPaths =
        {
            RepairTestPath,
            "Assets/Weedtopia/Scripts/Editor/GameSetup/CommercialReadinessTestJob.cs",
            "Assets/Weedtopia/Scripts/Editor/GameSetup/LayoutEditorJob.cs"
        };
        private const string StartupTest = "Weedtopia.Tests.PlayMode.ResponsiveStartupBenchmarkTests.EightHourResponsiveContinueMatchesSynchronousStateAndRecordsFrameLatency";
        private static readonly string[] PreservationTests =
        {
            "Weedtopia.Tests.Assets.BigRockCityAssetTests.ExplicitInstallerPreservesExistingSceneAndAssetBytesOnARepeatRun",
            "Weedtopia.Tests.Assets.GameAssetTests.SecondGameScaffoldPreservesEveryAuthoredAssetByte",
            "Weedtopia.Tests.Assets.TypographyAssetTests.ReusableTextPrefabsUseTheFamilyAndFontRepairIsIdempotent"
        };
        private static readonly Callback callback = new Callback();
        private static readonly string[] OtherJobs =
        {
            "Weedtopia.BigRockEvidence",
            "Weedtopia.ExplicitVisualEvidence",
            "Weedtopia.InteractionCapture",
            "Weedtopia.LayoutCapture",
            "Weedtopia.LocalizationEvidence",
            "Weedtopia.PhoneUiEvidence",
            "Weedtopia.PhoneUiTests",
            "Weedtopia.ProductionVisualEvidence",
            "Weedtopia.ShopEvidence",
            "Weedtopia.ShopValidation",
            "Weedtopia.UiExperienceCapture",
            "Weedtopia.UiPolishCapture",
            "Weedtopia.UiRefinementCapture"
        };
        [Serializable] private sealed class EditorState
        {
            public SceneSetup[] scenes;
            public string[] selection;
            public int gameViewSize;
            public bool runInBackground, paused, hadLanguage;
            public string language;
        }
        [Serializable] private sealed class StageResult
        {
            public string utc, stage, status;
            public int passed, failed, skipped, inconclusive;
        }
        [Serializable] private sealed class RunResult
        {
            public string id, startedUtc, finishedUtc, status, folder, detail, unity, scope, prerequisiteFullRunId;
            public int processId;
            public bool editorRestored, releaseAccepted, deviceAccepted, humanAccepted;
            public bool repairContinuation;
            public string editModeEvidenceMode, aggregateEvidencePath, sourceValidationPath;
        }
        [Serializable] private sealed class RepairSourceValidation
        {
            public string priorRunId, priorInputsPath, priorInputsSha256, currentInputsPath, currentFingerprint;
            public string correctedTestPath, correctedTestSha256;
            public bool allOtherScriptsAndMetasUnchanged, pathsIdentical, runtimeChangesPermitted;
            public int comparedFileCount;
            public string[] allowedChangedPaths;
            public ReleaseInputFile[] previousAllowedFiles, currentAllowedFiles;
        }
        [Serializable] private sealed class EditModeAggregate
        {
            public string schema = "native_editmode_repair_aggregate_v1", status, priorRunId, replacedFailedTest;
            public string originalXmlPath, originalXmlSha256, repairXmlPath, repairXmlSha256, sourceValidationPath;
            public int originalPassed, originalFailed, repairedPassed, combinedPassed, passed, previouslyPassed, rerunPassed, failed, skipped, inconclusive;
            public bool originalXmlUnmodified, fullSuiteRerun, releaseAccepted, deviceAccepted, humanAccepted;
        }
        static CommercialReadinessTestJob()
        {
            if (!SessionState.GetBool(Key, false)) return;
            TestRunnerApi.RegisterTestCallback(callback);
            EditorApplication.update += Maintain;
        }
        private static bool RunnerActive()
        {
            // This installed Test Framework keeps its status method internal. Read it; never modify runner state.
            var method = typeof(TestRunnerApi).GetMethod("IsRunActive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("Cannot safely determine whether another test run is active.");
            return (bool)method.Invoke(null, null);
        }
        [MenuItem("Weedtopia/Validation/Run Commercial Readiness Tests")]
        public static void Begin() => BeginRun(false);
        // Command-line automation entry point. It cannot close an interactive or foreign editor.
        public static void BeginOwnedBatch()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Owned batch validation requires batch mode.");
            SessionState.SetInt(Key+".ownedBatchPid",System.Diagnostics.Process.GetCurrentProcess().Id);
            Begin();
        }
        [MenuItem("Weedtopia/Validation/Run Responsive Startup Benchmark")]
        public static void BeginStartupBenchmark() => BeginRun(true);
        [MenuItem("Weedtopia/Validation/Continue Commercial Tests After House Assertion Repair")]
        public static void BeginRepairContinuation() => BeginRun(false, true);
        private static void BeginRun(bool startupBenchmark, bool repairContinuation = false)
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            if (SessionState.GetBool(Key, false) || RunnerActive() || OtherJobs.Any(job => SessionState.GetBool(job, false))
                || BuildPipeline.isBuildingPlayer || File.Exists(AndroidTestBuild.RequestPath))
                throw new InvalidOperationException("Another explicit test, capture or build job is active. Existing work is protected.");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Wait for the requested source import and compilation before running regression.");
            string prerequisiteFullRunId = startupBenchmark ? RequireCompletedFullSuite() : repairContinuation ? RequireRepairPrerequisite() : "";
            string topFolder = startupBenchmark ? StartupBenchmarkFolder : Folder;
            string scope = startupBenchmark ? StartupScope : repairContinuation ? RepairScope : FullScope;
            var saved = new EditorState
            {
                scenes = EditorSceneManager.GetSceneManagerSetup(),
                selection = Selection.objects.Where(item => item != null).Select(item => GlobalObjectId.GetGlobalObjectIdSlow(item).ToString()).ToArray(),
                gameViewSize = PhoneUiEvidence.GameViewSelection(), runInBackground = UnityEngine.Application.runInBackground,
                paused = EditorApplication.isPaused, hadLanguage = PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey),
                language = PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey)
            };
            string id = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + System.Diagnostics.Process.GetCurrentProcess().Id;
            string folder = topFolder + "/" + id;
            Directory.CreateDirectory(folder);
            SessionState.SetString(Key + ".editor", JsonUtility.ToJson(saved));
            SessionState.SetString(Key + ".folder", folder);
            SessionState.SetString(Key + ".topFolder", topFolder);
            SessionState.SetString(Key + ".scope", scope);
            SessionState.SetString(Key + ".prerequisite", prerequisiteFullRunId);
            SessionState.SetBool(Key + ".startup", startupBenchmark);
            SessionState.SetBool(Key + ".repair", repairContinuation);
            SessionState.SetString(Key + ".failure", "");
            SessionState.SetString(Key + ".restoreError", "");
            SessionState.SetString(Key + ".id", id);
            SessionState.SetString(Key + ".started", DateTime.UtcNow.ToString("O"));
            SessionState.SetInt(Key + ".stage", 0);
            SessionState.SetBool(Key + ".failed", false);
            SessionState.SetBool(Key + ".pending", true);
            SessionState.SetBool(Key, true);
            try
            {
                var sources = ReleaseFingerprint.Capture(Directory.GetCurrentDirectory(),
                    Directory.GetFiles("Assets/Weedtopia/Scripts", "*", SearchOption.AllDirectories),
                    "scope=" + scope + "\nunity=" + UnityEngine.Application.unityVersion);
                File.WriteAllText(folder + "/source_inputs.json", JsonUtility.ToJson(sources, true));
                if (repairContinuation)
                {
                    SessionState.SetString(Key + ".sourceFingerprint", sources.fingerprint);
                    SessionState.SetString(Key + ".sourceInputSha", Sha256File(folder + "/source_inputs.json"));
                    var validation = ValidateRepairSources(sources);
                    validation.currentInputsPath = folder + "/source_inputs.json";
                    File.WriteAllText(folder + "/source_validation.json", JsonUtility.ToJson(validation, true));
                    File.Copy(RepairFolder + "/" + RepairPriorRun + "/source_inputs.json", folder + "/previous_source_inputs.json", false);
                }
                PhoneUiEvidence.FixedTestView(1920, 1080);
                UnityEngine.Application.runInBackground = true;
                EditorApplication.isPaused = false;
                TestRunnerApi.RegisterTestCallback(callback);
                EditorApplication.update -= Maintain;
                EditorApplication.update += Maintain;
                WriteRun("requested", startupBenchmark
                    ? "Three focused EditMode preservation checks, then one responsive-startup PlayMode benchmark after a passed, restored complete regression; all acceptance gates remain separate."
                    : repairContinuation
                        ? "One failed native EditMode assertion is rerun against an otherwise unchanged script/meta input set. Its XML combines with the 395 original native passes in an explicit aggregate, then all PlayMode tests run."
                        : "Complete EditMode first; PlayMode starts only after a successful EditMode result.");
            }
            catch (Exception exception) { FailAndRestore(exception); }
        }
        private static string RequireCompletedFullSuite()
        {
            string latest = Folder + "/latest.json";
            if (!File.Exists(latest)) throw new InvalidOperationException("Complete native regression must pass and restore the editor before the startup benchmark.");
            var completed = JsonUtility.FromJson<RunResult>(File.ReadAllText(latest));
            if (completed == null || completed.status != "passed" || !completed.editorRestored || string.IsNullOrEmpty(completed.finishedUtc)
                || string.IsNullOrEmpty(completed.id) || completed.id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || (!string.IsNullOrEmpty(completed.scope) && completed.scope != FullScope && completed.scope != RepairScope))
                throw new InvalidOperationException("The latest complete native regression has not passed with successful editor restoration.");
            // Accept the earlier full-run schema without a scope field only within its exact owned full-run folder.
            string expected = Path.GetFullPath(Path.Combine(Folder, completed.id));
            string allowed = Path.GetFullPath(Folder) + Path.DirectorySeparatorChar;
            if (!expected.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(expected, Path.GetFullPath(completed.folder ?? ""), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The prerequisite does not belong to the complete-regression evidence folder.");
            foreach (string stage in new[] { "EditMode", "PlayMode" })
            {
                string stagePath = Path.Combine(expected, stage + ".json");
                if (completed.scope == RepairScope && stage == "EditMode")
                {
                    RequireValidAggregate(expected);
                    continue;
                }
                if (!File.Exists(stagePath) || !File.Exists(Path.Combine(expected, stage + ".xml")))
                    throw new InvalidOperationException("Missing original " + stage + " result for the complete regression.");
                var result = JsonUtility.FromJson<StageResult>(File.ReadAllText(stagePath));
                if (result == null || result.status != "passed" || result.passed < 1 || result.failed != 0 || result.inconclusive != 0)
                    throw new InvalidOperationException("The prerequisite " + stage + " stage did not pass.");
            }
            return completed.id;
        }
        private static string Sha256File(string path)
        {
            using (var hash = SHA256.Create()) using (var stream = File.OpenRead(path))
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        private static XmlDocument ReadNativeXml(string path)
        {
            var document = new XmlDocument { XmlResolver = null };
            using (var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null })) document.Load(reader);
            if (document.DocumentElement == null || document.DocumentElement.Name != "test-run") throw new InvalidOperationException("Missing native test-run XML: " + path);
            return document;
        }
        private static void RequireXmlCounts(string path, int passed, int failed, string selectedTest, bool selectedPassed)
        {
            var document = ReadNativeXml(path); var root = document.DocumentElement;
            foreach (var pair in new[] { Tuple.Create("passed", passed), Tuple.Create("failed", failed), Tuple.Create("skipped", 0), Tuple.Create("inconclusive", 0), Tuple.Create("total", passed + failed), Tuple.Create("testcasecount", passed + failed) })
                if (!int.TryParse(root.GetAttribute(pair.Item1), out int actual) || actual != pair.Item2) throw new InvalidOperationException("Unexpected native XML count " + pair.Item1 + " in " + path);
            var cases = document.SelectNodes("//test-case").Cast<XmlElement>().ToArray();
            if (cases.Length != passed + failed || cases.Count(item => item.GetAttribute("result") == "Passed") != passed || cases.Count(item => item.GetAttribute("result") == "Failed") != failed
                || cases.Select(item => item.GetAttribute("fullname")).Distinct(StringComparer.Ordinal).Count() != cases.Length) throw new InvalidOperationException("Native XML leaf cases do not match its counts: " + path);
            var selected = cases.Where(item => item.GetAttribute("fullname") == selectedTest).ToArray();
            if (selected.Length != 1 || selected[0].GetAttribute("result") != (selectedPassed ? "Passed" : "Failed")
                || (!selectedPassed && cases.Any(item => item.GetAttribute("result") == "Failed" && item.GetAttribute("fullname") != selectedTest))) throw new InvalidOperationException("The expected single repaired test does not match native XML: " + path);
        }
        private static void RequirePassedCase(string path, string test)
        {
            var cases = ReadNativeXml(path).SelectNodes("//test-case").Cast<XmlElement>().Where(item => item.GetAttribute("fullname") == test).ToArray();
            if (cases.Length != 1 || cases[0].GetAttribute("result") != "Passed") throw new InvalidOperationException("Complete PlayMode must execute and pass the responsive-startup benchmark: " + test);
        }
        private static string RequireRepairPrerequisite()
        {
            string previousFolder = RepairFolder + "/" + RepairPriorRun;
            var previous = JsonUtility.FromJson<RunResult>(File.ReadAllText(RepairFolder + "/latest.json"));
            if (previous == null || previous.id != RepairPriorRun || previous.status != "failed" || !previous.editorRestored || previous.scope != FullScope || string.IsNullOrEmpty(previous.finishedUtc)
                || previous.unity != UnityEngine.Application.unityVersion || Path.GetFullPath(previous.folder ?? "") != Path.GetFullPath(previousFolder) || File.Exists(previousFolder + "/PlayMode.xml"))
                throw new InvalidOperationException("Repair requires the exact restored full run with one EditMode failure and no PlayMode run.");
            var stage = JsonUtility.FromJson<StageResult>(File.ReadAllText(previousFolder + "/EditMode.json"));
            if (stage == null || stage.status != "failed" || stage.passed != 395 || stage.failed != 1 || stage.skipped != 0 || stage.inconclusive != 0) throw new InvalidOperationException("Previous full EditMode must contain exactly 395 passes and one failure.");
            if (Sha256File(previousFolder + "/EditMode.xml") != PriorXmlSha) throw new InvalidOperationException("Original native EditMode XML changed.");
            RequireXmlCounts(previousFolder + "/EditMode.xml", 395, 1, RepairTest, false);
            ValidateRepairSources(ReleaseFingerprint.Capture(Directory.GetCurrentDirectory(), Directory.GetFiles("Assets/Weedtopia/Scripts", "*", SearchOption.AllDirectories), "scope=" + RepairScope + "\nunity=" + UnityEngine.Application.unityVersion));
            return RepairPriorRun;
        }
        private static RepairSourceValidation ValidateRepairSources(ReleaseInputManifest current)
        {
            string previousPath = RepairFolder + "/" + RepairPriorRun + "/source_inputs.json";
            if (Sha256File(previousPath) != PriorInputsSha) throw new InvalidOperationException("Original complete-regression source manifest changed.");
            var previous = JsonUtility.FromJson<ReleaseInputManifest>(File.ReadAllText(previousPath));
            if (previous == null || previous.schemaVersion != 2 || previous.files == null || current == null || current.files == null || previous.buildContext != "scope=" + FullScope + "\nunity=" + UnityEngine.Application.unityVersion) throw new InvalidOperationException("Missing or incompatible original source manifest.");
            var oldFiles = previous.files.ToDictionary(file => file.path, StringComparer.Ordinal); var newFiles = current.files.ToDictionary(file => file.path, StringComparer.Ordinal);
            if (oldFiles.Count != newFiles.Count || oldFiles.Keys.Any(path => !newFiles.ContainsKey(path))) throw new InvalidOperationException("A script/meta was added, deleted or renamed; full regression is required.");
            foreach (var pair in oldFiles)
            {
                var now = newFiles[pair.Key];
                if ((pair.Value.sha256 != now.sha256 || pair.Value.bytes != now.bytes) && !RepairAllowedPaths.Contains(pair.Key, StringComparer.Ordinal)) throw new InvalidOperationException("Non-allowlisted script/meta changed: " + pair.Key);
            }
            if (!newFiles.TryGetValue(RepairTestPath, out var corrected) || corrected.sha256 != CorrectedTestSha || !oldFiles.ContainsKey(RepairTestPath) || oldFiles[RepairTestPath].sha256 == corrected.sha256) throw new InvalidOperationException("Only the exact reviewed house assertion correction is permitted.");
            return new RepairSourceValidation
            {
                priorRunId = RepairPriorRun, priorInputsPath = previousPath, priorInputsSha256 = PriorInputsSha, currentFingerprint = current.fingerprint, correctedTestPath = RepairTestPath, correctedTestSha256 = CorrectedTestSha,
                allOtherScriptsAndMetasUnchanged = true, pathsIdentical = true, runtimeChangesPermitted = false, comparedFileCount = oldFiles.Count, allowedChangedPaths = RepairAllowedPaths,
                previousAllowedFiles = RepairAllowedPaths.Select(path => oldFiles[path]).ToArray(), currentAllowedFiles = RepairAllowedPaths.Select(path => newFiles[path]).ToArray()
            };
        }
        private static void RequireStableRepairInputs()
        {
            string folder = SessionState.GetString(Key + ".folder", "");
            var saved = JsonUtility.FromJson<ReleaseInputManifest>(File.ReadAllText(folder + "/source_inputs.json"));
            var current = ReleaseFingerprint.Capture(Directory.GetCurrentDirectory(), Directory.GetFiles("Assets/Weedtopia/Scripts", "*", SearchOption.AllDirectories), "scope=" + RepairScope + "\nunity=" + UnityEngine.Application.unityVersion);
            if (saved == null || saved.fingerprint != current.fingerprint || saved.fingerprint != SessionState.GetString(Key + ".sourceFingerprint", "") || Sha256File(folder + "/source_inputs.json") != SessionState.GetString(Key + ".sourceInputSha", "")) throw new InvalidOperationException("Scripts/metas or owned source manifest changed during continuation.");
            ValidateRepairSources(current);
        }
        private static void WriteAggregate(string folder)
        {
            string original = RepairFolder + "/" + RepairPriorRun + "/EditMode.xml";
            if (Sha256File(original) != PriorXmlSha) throw new InvalidOperationException("Original native XML changed during continuation.");
            RequireXmlCounts(original, 395, 1, RepairTest, false);
            var aggregate = new EditModeAggregate
            {
                status = "passed", priorRunId = RepairPriorRun, replacedFailedTest = RepairTest, originalXmlPath = original, originalXmlSha256 = PriorXmlSha,
                repairXmlPath = folder + "/EditModeRepair.xml", repairXmlSha256 = Sha256File(folder + "/EditModeRepair.xml"), sourceValidationPath = folder + "/source_validation.json",
                originalPassed = 395, originalFailed = 1, repairedPassed = 1, combinedPassed = 396, passed = 396, previouslyPassed = 395, rerunPassed = 1, failed = 0, skipped = 0, inconclusive = 0,
                originalXmlUnmodified = true, fullSuiteRerun = false, releaseAccepted = false, deviceAccepted = false, humanAccepted = false
            };
            File.WriteAllText(folder + "/EditModeAggregate.json", JsonUtility.ToJson(aggregate, true));
        }
        private static void RequireValidAggregate(string folder)
        {
            var aggregate = JsonUtility.FromJson<EditModeAggregate>(File.ReadAllText(Path.Combine(folder, "EditModeAggregate.json")));
            string original = RepairFolder + "/" + RepairPriorRun + "/EditMode.xml";
            if (aggregate == null || aggregate.schema != "native_editmode_repair_aggregate_v1" || aggregate.status != "passed" || aggregate.priorRunId != RepairPriorRun || aggregate.replacedFailedTest != RepairTest
                || aggregate.originalPassed != 395 || aggregate.originalFailed != 1 || aggregate.repairedPassed != 1 || aggregate.combinedPassed != 396 || aggregate.passed != 396 || aggregate.previouslyPassed != 395 || aggregate.rerunPassed != 1 || aggregate.failed != 0 || aggregate.skipped != 0 || aggregate.inconclusive != 0
                || !aggregate.originalXmlUnmodified || aggregate.fullSuiteRerun || aggregate.releaseAccepted || aggregate.deviceAccepted || aggregate.humanAccepted
                || Path.GetFullPath(aggregate.originalXmlPath ?? "") != Path.GetFullPath(original) || Path.GetFullPath(aggregate.repairXmlPath ?? "") != Path.Combine(folder, "EditModeRepair.xml")
                || Path.GetFullPath(aggregate.sourceValidationPath ?? "") != Path.Combine(folder, "source_validation.json") || aggregate.originalXmlSha256 != PriorXmlSha || Sha256File(original) != PriorXmlSha || Sha256File(aggregate.repairXmlPath) != aggregate.repairXmlSha256)
                throw new InvalidOperationException("Native EditMode aggregate is incomplete or no longer matches original evidence.");
            RequireXmlCounts(original, 395, 1, RepairTest, false); RequireXmlCounts(aggregate.repairXmlPath, 1, 0, RepairTest, true);
            var sourceValidation = JsonUtility.FromJson<RepairSourceValidation>(File.ReadAllText(aggregate.sourceValidationPath));
            var currentInputs = JsonUtility.FromJson<ReleaseInputManifest>(File.ReadAllText(Path.Combine(folder, "source_inputs.json"))); var checkedInputs = ValidateRepairSources(currentInputs);
            if (sourceValidation == null || !sourceValidation.allOtherScriptsAndMetasUnchanged || !sourceValidation.pathsIdentical || sourceValidation.runtimeChangesPermitted || sourceValidation.priorInputsSha256 != PriorInputsSha
                || sourceValidation.currentFingerprint != currentInputs.fingerprint || sourceValidation.comparedFileCount != checkedInputs.comparedFileCount || Sha256File(Path.Combine(folder, "previous_source_inputs.json")) != PriorInputsSha)
                throw new InvalidOperationException("Native aggregate source comparison is missing or invalid.");
        }

        private static void Maintain()
        {
            if (!SessionState.GetBool(Key, false)) { EditorApplication.update -= Maintain; return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || RunnerActive()
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            int stage = SessionState.GetInt(Key + ".stage", 0);
            if (stage >= 2) { Restore(); return; }
            if (!SessionState.GetBool(Key + ".pending", false)) return;
            try
            {
                BurningHillsLayoutInstaller.RequireSafeEditor();
                if (SessionState.GetBool(Key + ".repair", false)) RequireStableRepairInputs();
                SessionState.SetBool(Key + ".pending", false);
                var filter = new Filter
                {
                    testMode = stage == 0 ? TestMode.EditMode : TestMode.PlayMode,
                    assemblyNames = stage == 0
                        ? new[] { "Weedtopia.Game.EditModeTests", "Weedtopia.Game.AssetTests", "Weedtopia.Editor.BuildingArtPipeline.Tests" }
                        : new[] { "Weedtopia.Game.PlayModeTests" }
                };
                if (SessionState.GetBool(Key + ".startup", false))
                {
                    filter.assemblyNames = stage == 0 ? new[] { "Weedtopia.Game.AssetTests" } : new[] { "Weedtopia.Game.PlayModeTests" };
                    filter.testNames = stage == 0 ? PreservationTests : new[] { StartupTest };
                }
                if (SessionState.GetBool(Key + ".repair", false) && stage == 0)
                {
                    filter.assemblyNames = new[] { "Weedtopia.Game.EditModeTests" };
                    filter.testNames = new[] { RepairTest };
                }
                WriteRun(stage == 0 ? "running_editmode" : "running_playmode", "Native Unity tests; no physical device or human acceptance implied.");
                var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                SessionState.SetString(Key + ".runner", api.Execute(new ExecutionSettings(filter)));
            }
            catch (Exception exception) { FailAndRestore(exception); }
        }
        private static void FailAndRestore(Exception exception)
        {
            SessionState.SetBool(Key + ".failed", true);
            SessionState.SetInt(Key + ".stage", 2);
            SessionState.SetString(Key + ".failure", exception.ToString());
            WriteRun("failed", exception.ToString());
            if (!RunnerActive() && !EditorApplication.isPlayingOrWillChangePlaymode) Restore();
        }
        private static void Restore()
        {
            try
            {
                // Tests begin with clean scenes. Never discard a new unsaved manual edit while restoring.
                BurningHillsLayoutInstaller.RequireSafeEditor();
                var saved = JsonUtility.FromJson<EditorState>(SessionState.GetString(Key + ".editor", ""));
                if (saved == null) throw new InvalidOperationException("Owned editor-state snapshot is missing.");
                if (saved.scenes != null && saved.scenes.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(saved.scenes);
                PhoneUiEvidence.SelectSize(saved.gameViewSize);
                UnityEngine.Application.runInBackground = saved.runInBackground;
                EditorApplication.isPaused = saved.paused;
                if (saved.hadLanguage) PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey, saved.language);
                else PlayerPrefs.DeleteKey(GameRoot.LanguagePreferenceKey);
                PlayerPrefs.Save();
                Selection.objects = (saved.selection ?? Array.Empty<string>()).Select(text =>
                    GlobalObjectId.TryParse(text, out var id) ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) : null)
                    .Where(item => item != null).ToArray();
                if (SessionState.GetBool(Key + ".repair", false))
                {
                    try { RequireStableRepairInputs(); }
                    catch (Exception exception)
                    {
                        SessionState.SetBool(Key + ".failed", true);
                        SessionState.SetString(Key + ".failure", "Source inputs changed during native repair continuation: " + exception);
                    }
                }
                WriteRun(SessionState.GetBool(Key + ".failed", false) ? "failed" : "passed",
                    SessionState.GetString(Key + ".failure", "Complete regression finished; original scenes and editor settings restored."), true);
                SessionState.SetBool(Key, false);
                TestRunnerApi.UnregisterTestCallback(callback);
                EditorApplication.update -= Maintain;
                if(UnityEngine.Application.isBatchMode&&SessionState.GetInt(Key+".ownedBatchPid",-1)==System.Diagnostics.Process.GetCurrentProcess().Id)
                    EditorApplication.Exit(SessionState.GetBool(Key+".failed",false)?1:0);
            }
            catch (Exception exception)
            {
                // Keep the snapshot and active guard for a later safe restore; no test result is converted into acceptance.
                if (SessionState.GetString(Key + ".restoreError", "") != exception.Message)
                {
                    SessionState.SetString(Key + ".restoreError", exception.Message);
                    WriteRun("restore_blocked", exception.ToString());
                    Debug.LogError("Commercial regression finished but editor restoration is blocked: " + exception.Message);
                }
            }
        }
        private static void WriteRun(string status, string detail, bool restored = false)
        {
            var result = new RunResult
            {
                id = SessionState.GetString(Key + ".id", ""), startedUtc = SessionState.GetString(Key + ".started", ""),
                finishedUtc = restored ? DateTime.UtcNow.ToString("O") : "", status = status,
                folder = SessionState.GetString(Key + ".folder", ""), detail = detail,
                scope = SessionState.GetString(Key + ".scope", FullScope), prerequisiteFullRunId = SessionState.GetString(Key + ".prerequisite", ""),
                unity = UnityEngine.Application.unityVersion, processId = System.Diagnostics.Process.GetCurrentProcess().Id,
                editorRestored = restored, releaseAccepted = false, deviceAccepted = false, humanAccepted = false,
                repairContinuation = SessionState.GetBool(Key + ".repair", false),
                editModeEvidenceMode = SessionState.GetBool(Key + ".repair", false) ? "395_original_native_passes_plus_1_native_repaired_pass" : "direct_native_stage",
                aggregateEvidencePath = SessionState.GetBool(Key + ".repair", false) ? SessionState.GetString(Key + ".folder", "") + "/EditModeAggregate.json" : "",
                sourceValidationPath = SessionState.GetBool(Key + ".repair", false) ? SessionState.GetString(Key + ".folder", "") + "/source_validation.json" : ""
            };
            string json = JsonUtility.ToJson(result, true);
            File.WriteAllText(result.folder + "/run.json", json);
            File.WriteAllText(SessionState.GetString(Key + ".topFolder", Folder) + "/latest.json", json);
        }
        private sealed class Callback : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (!SessionState.GetBool(Key, false)) return;
                try
                {
                    int stage = SessionState.GetInt(Key + ".stage", 0);
                    if (stage > 1) return;
                    bool startupBenchmark = SessionState.GetBool(Key + ".startup", false);
                    bool repairContinuation = SessionState.GetBool(Key + ".repair", false);
                    string name = startupBenchmark ? (stage == 0 ? "EditModePreservation" : "PlayModeStartupBenchmark")
                        : repairContinuation && stage == 0 ? "EditModeRepair" : stage == 0 ? "EditMode" : "PlayMode";
                    string folder = SessionState.GetString(Key + ".folder", "");
                    TestRunnerApi.SaveResultToFile(result, Path.GetFullPath(folder + "/" + name + ".xml"));
                    bool passed = result.ResultState == "Passed" && result.FailCount == 0 && result.PassCount > 0 && result.InconclusiveCount == 0
                        && (!startupBenchmark || (result.PassCount == (stage == 0 ? PreservationTests.Length : 1) && result.SkipCount == 0));
                    if (repairContinuation)
                    {
                        passed &= result.SkipCount == 0;
                        RequireStableRepairInputs();
                        if (stage == 0)
                        {
                            passed &= result.PassCount == 1 && result.SkipCount == 0;
                            if (passed)
                            {
                                RequireXmlCounts(folder + "/EditModeRepair.xml", 1, 0, RepairTest, true);
                                WriteAggregate(folder);
                            }
                        }
                        else if (passed)
                        {
                            RequirePassedCase(folder + "/PlayMode.xml", StartupTest);
                            RequireValidAggregate(Path.GetFullPath(folder));
                        }
                    }
                    if ((startupBenchmark || repairContinuation) && stage == 1)
                    {
                        const string benchmark = "Docs/CommercialReadiness/2026-10-06/NativeResponsiveStartupBenchmark.json";
                        if (File.Exists(benchmark) && File.GetLastWriteTimeUtc(benchmark) >= DateTime.Parse(SessionState.GetString(Key + ".started", ""), null, System.Globalization.DateTimeStyles.RoundtripKind))
                        {
                            File.Copy(benchmark, folder + "/benchmark.json", false);
                            const string screenshot = "Docs/CommercialReadiness/2026-10-06/NativeResponsiveStartupBenchmark.offline.png";
                            if (File.Exists(screenshot) && File.GetLastWriteTimeUtc(screenshot) >= DateTime.Parse(SessionState.GetString(Key + ".started", ""), null, System.Globalization.DateTimeStyles.RoundtripKind))
                                File.Copy(screenshot, folder + "/offline.png", false);
                        }
                        else passed = false;
                    }
                    File.WriteAllText(folder + "/" + name + ".json", JsonUtility.ToJson(new StageResult
                    {
                        utc = DateTime.UtcNow.ToString("O"), stage = name, status = passed ? "passed" : "failed",
                        passed = result.PassCount, failed = result.FailCount, skipped = result.SkipCount, inconclusive = result.InconclusiveCount
                    }, true));
                    if (!passed)
                    {
                        SessionState.SetBool(Key + ".failed", true);
                        SessionState.SetString(Key + ".failure", name + " did not pass. See the original XML; later stages were not started.");
                    }
                    SessionState.SetInt(Key + ".stage", stage == 0 && passed ? 1 : 2);
                    SessionState.SetBool(Key + ".pending", true);
                }
                catch (Exception exception) { FailAndRestore(exception); }
            }
        }
    }
}

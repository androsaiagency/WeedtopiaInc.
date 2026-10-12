using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit complete regression in the existing editor. Preserves editor state and never exits it.</summary>
    [InitializeOnLoad]
    public static class CommercialReadinessTestJob
    {
        private const string Key = "Weedtopia.CommercialReadinessTests";
        public const string Folder = "Docs/CommercialReadiness/2026-10-05/Tests";
        public const string StartupBenchmarkFolder = "Docs/CommercialReadiness/2026-10-05/StartupBenchmarkTests";
        private const string FullScope = "complete_editmode_then_playmode";
        private const string StartupScope = "focused_preservation_then_responsive_startup_benchmark";
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
        [MenuItem("Weedtopia/Validation/Run Responsive Startup Benchmark")]
        public static void BeginStartupBenchmark() => BeginRun(true);
        private static void BeginRun(bool startupBenchmark)
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            if (SessionState.GetBool(Key, false) || RunnerActive() || OtherJobs.Any(job => SessionState.GetBool(job, false))
                || BuildPipeline.isBuildingPlayer || File.Exists(AndroidTestBuild.RequestPath))
                throw new InvalidOperationException("Another explicit test, capture or build job is active. Existing work is protected.");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Wait for the requested source import and compilation before running regression.");
            string prerequisiteFullRunId = startupBenchmark ? RequireCompletedFullSuite() : "";
            string topFolder = startupBenchmark ? StartupBenchmarkFolder : Folder;
            string scope = startupBenchmark ? StartupScope : FullScope;
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
                PhoneUiEvidence.FixedTestView(1920, 1080);
                UnityEngine.Application.runInBackground = true;
                EditorApplication.isPaused = false;
                TestRunnerApi.RegisterTestCallback(callback);
                EditorApplication.update -= Maintain;
                EditorApplication.update += Maintain;
                WriteRun("requested", startupBenchmark
                    ? "Three focused EditMode preservation checks, then one responsive-startup PlayMode benchmark after a passed, restored complete regression; all acceptance gates remain separate."
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
                || (!string.IsNullOrEmpty(completed.scope) && completed.scope != FullScope))
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
                if (!File.Exists(stagePath) || !File.Exists(Path.Combine(expected, stage + ".xml")))
                    throw new InvalidOperationException("Missing original " + stage + " result for the complete regression.");
                var result = JsonUtility.FromJson<StageResult>(File.ReadAllText(stagePath));
                if (result == null || result.status != "passed" || result.passed < 1 || result.failed != 0 || result.inconclusive != 0)
                    throw new InvalidOperationException("The prerequisite " + stage + " stage did not pass.");
            }
            return completed.id;
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
                WriteRun(SessionState.GetBool(Key + ".failed", false) ? "failed" : "passed",
                    SessionState.GetString(Key + ".failure", "Complete regression finished; original scenes and editor settings restored."), true);
                SessionState.SetBool(Key, false);
                TestRunnerApi.UnregisterTestCallback(callback);
                EditorApplication.update -= Maintain;
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
                editorRestored = restored, releaseAccepted = false, deviceAccepted = false, humanAccepted = false
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
                    string name = startupBenchmark ? (stage == 0 ? "EditModePreservation" : "PlayModeStartupBenchmark") : stage == 0 ? "EditMode" : "PlayMode";
                    string folder = SessionState.GetString(Key + ".folder", "");
                    TestRunnerApi.SaveResultToFile(result, Path.GetFullPath(folder + "/" + name + ".xml"));
                    bool passed = result.ResultState == "Passed" && result.FailCount == 0 && result.PassCount > 0 && result.InconclusiveCount == 0
                        && (!startupBenchmark || (result.PassCount == (stage == 0 ? PreservationTests.Length : 1) && result.SkipCount == 0));
                    if (startupBenchmark && stage == 1)
                    {
                        const string benchmark = "Docs/CommercialReadiness/2026-10-05/NativeResponsiveStartupBenchmark.json";
                        if (File.Exists(benchmark) && File.GetLastWriteTimeUtc(benchmark) >= DateTime.Parse(SessionState.GetString(Key + ".started", ""), null, System.Globalization.DateTimeStyles.RoundtripKind))
                        {
                            File.Copy(benchmark, folder + "/benchmark.json", false);
                            const string screenshot = "Docs/CommercialReadiness/2026-10-05/NativeResponsiveStartupBenchmark.offline.png";
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

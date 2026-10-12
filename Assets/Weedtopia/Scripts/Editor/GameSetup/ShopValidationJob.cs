using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit validation in a separately launched, owned editor. Never exits an existing user editor.</summary>
    [InitializeOnLoad]
    public static class ShopValidationJob
    {
        private const string Key = "Weedtopia.ShopValidation", Folder = "Docs/ShopRevision/Tests";
        private static readonly Callback callback = new Callback();
        static ShopValidationJob()
        {
            if (Owned && SessionState.GetBool(Key, false)) TestRunnerApi.RegisterTestCallback(callback);
            if (Owned && SessionState.GetBool(Key + ".build", false)) EditorApplication.update += WatchBuild;
        }
        private static bool Owned => SessionState.GetInt(Key + ".pid", 0) == System.Diagnostics.Process.GetCurrentProcess().Id;
        private static void RequireOwnedLaunch(string method)
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, method) < 0) throw new InvalidOperationException("This method requires its own explicit command-line editor launch.");
            SessionState.SetInt(Key + ".pid", System.Diagnostics.Process.GetCurrentProcess().Id);
        }
        public static void BeginOwnedTests()
        {
            RequireOwnedLaunch("Weedtopia.Editor.GameSetup.ShopValidationJob.BeginOwnedTests");
            SessionState.SetBool(Key + ".focused", false); BeginTests();
        }
        public static void BeginOwnedRecheck()
        {
            RequireOwnedLaunch("Weedtopia.Editor.GameSetup.ShopValidationJob.BeginOwnedRecheck");
            SessionState.SetBool(Key + ".focused", true); BeginTests();
        }
        private static void BeginTests()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor(); Directory.CreateDirectory(Folder);
            EditorSceneManager.OpenScene(GameScaffold.GameScene);
            SessionState.SetBool(Key, true); SessionState.SetInt(Key + ".stage", 0);
            SessionState.SetInt(Key + ".size", PhoneUiEvidence.GameViewSelection());
            PhoneUiEvidence.FixedTestView(1920, 1080); UnityEngine.Application.runInBackground = true;
            TestRunnerApi.RegisterTestCallback(callback); Run();
        }
        private static void Run()
        {
            int stage = SessionState.GetInt(Key + ".stage", 0);
            var filter = new Filter { testMode = stage == 0 ? TestMode.PlayMode : TestMode.EditMode,
                assemblyNames = new[] { stage == 0 ? "Weedtopia.Game.PlayModeTests" : "Weedtopia.Game.AssetTests" } };
            if (stage == 1) filter.groupNames = new[] { "Weedtopia.Tests.Assets.ShopAssetTests", "Weedtopia.Tests.Assets.WorldNavigationTests", "Weedtopia.Tests.Assets.ProductionPresentationTests.ProductionPreflightRecognizesApprovedSpritesAndKeepsIndependentReleaseGates" };
            if (stage == 0 && SessionState.GetBool(Key + ".focused", false)) filter.groupNames = new[] { "Weedtopia.Tests.PlayMode.ShopUiTests", "Weedtopia.Tests.PlayMode.BuildingInteractionTests.FreeWorldHasNoPersistentPanelsOrPositionScrollbar" };
            ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(filter));
        }
        private sealed class Callback : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (!Owned || !SessionState.GetBool(Key, false)) return;
                int stage = SessionState.GetInt(Key + ".stage", 0);
                TestRunnerApi.SaveResultToFile(result, Path.GetFullPath(Folder + (stage == 0 ? (SessionState.GetBool(Key + ".focused", false) ? "/PlayModeFocused.xml" : "/PlayMode.xml") : "/Assets.xml")));
                if (stage == 0 && result.FailCount == 0) { SessionState.SetInt(Key + ".stage", 1); EditorApplication.delayCall += Run; }
                else
                {
                    SessionState.SetBool(Key, false); TestRunnerApi.UnregisterTestCallback(this);
                    PhoneUiEvidence.SelectSize(SessionState.GetInt(Key + ".size", 0));
                    EditorApplication.delayCall += () => EditorApplication.Exit(result.FailCount == 0 && result.PassCount > 0 ? 0 : 2);
                }
            }
        }
        public static void BeginOwnedBuild()
        {
            RequireOwnedLaunch("Weedtopia.Editor.GameSetup.ShopValidationJob.BeginOwnedBuild");
            BurningHillsLayoutInstaller.RequireSafeEditor();
            if (File.Exists(AndroidTestBuild.RequestPath)) throw new InvalidOperationException("Another Android build is already requested.");
            string id = "shop-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            SessionState.SetString(Key + ".buildId", id); SessionState.SetBool(Key + ".build", true);
            Directory.CreateDirectory("Temp");
            File.WriteAllText(AndroidTestBuild.RequestPath, JsonUtility.ToJson(new AndroidTestBuild.Request { id = id, refreshAssets = true, applySelectedGameIcon = true }, true));
            EditorApplication.update += WatchBuild;
        }
        [Serializable] private sealed class BuildStatus { public string status = default; }
        private static void WatchBuild()
        {
            if (!Owned || !SessionState.GetBool(Key + ".build", false)) return;
            string path = "Builds/Android/" + SessionState.GetString(Key + ".buildId", "") + "/build-result.json";
            if (!File.Exists(path)) return;
            var result = JsonUtility.FromJson<BuildStatus>(File.ReadAllText(path));
            if (result.status != "succeeded" && result.status != "failed") return;
            SessionState.SetBool(Key + ".build", false); EditorApplication.update -= WatchBuild;
            EditorApplication.Exit(result.status == "succeeded" ? 0 : 2);
        }
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit editor test runs, surviving runner domain reloads without closing the user's editor.</summary>
    [InitializeOnLoad]
    public static class PhoneUiTestJob
    {
        private const string Key = "Weedtopia.PhoneUiTests", Folder = "Docs/PhoneUiRevision/Tests";
        private static readonly Callback callback = new Callback();
        static PhoneUiTestJob() { if (SessionState.GetBool(Key, false)) TestRunnerApi.RegisterTestCallback(callback); }
        public static void Begin()
        { BeginCore(false); }
        public static void BeginFocused()
        { BeginCore(true); }
        public static void ResumeEditMode()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            if (!SessionState.GetBool(Key, false) || SessionState.GetInt(Key + ".stage", 0) != 1)
                throw new InvalidOperationException("No owned EditMode continuation is pending.");
            TestRunnerApi.RegisterTestCallback(callback); Run();
        }
        public static void RecheckAssets()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            SessionState.SetBool(Key, true); SessionState.SetInt(Key + ".stage", 2);
            SessionState.SetInt(Key + ".size", PhoneUiEvidence.GameViewSelection());
            SessionState.SetBool(Key + ".background", UnityEngine.Application.runInBackground);
            TestRunnerApi.RegisterTestCallback(callback); Run();
        }
        private static void BeginCore(bool focused)
        {
            BurningHillsLayoutInstaller.RequireSafeEditor(); Directory.CreateDirectory(Folder);
            SessionState.SetBool(Key, true); SessionState.SetInt(Key + ".stage", 0);
            SessionState.SetBool(Key + ".focused", focused);
            SessionState.SetInt(Key + ".size", PhoneUiEvidence.GameViewSelection());
            PhoneUiEvidence.FixedTestView(1920, 1080);
            SessionState.SetBool(Key + ".background", UnityEngine.Application.runInBackground);
            UnityEngine.Application.runInBackground = true;
            TestRunnerApi.RegisterTestCallback(callback); Run();
        }
        private static void Run()
        {
            int stage = SessionState.GetInt(Key + ".stage", 0);
            var filter = new Filter { testMode = stage == 0 ? TestMode.PlayMode : TestMode.EditMode,
                assemblyNames = stage == 0 ? new[] { "Weedtopia.Game.PlayModeTests" } : new[] { "Weedtopia.Game.EditModeTests", "Weedtopia.Game.AssetTests" } };
            if (stage == 0 && SessionState.GetBool(Key + ".focused", false))
                filter.groupNames = new[] { "Weedtopia.Tests.PlayMode.BuildingInteractionTests", "Weedtopia.Tests.PlayMode.UiExperienceTests", "Weedtopia.Tests.PlayMode.VipStoryUiTests", "Weedtopia.Tests.PlayMode.PhoneUiTests" };
            if (stage == 2) filter.groupNames = new[] { "Weedtopia.Tests.Assets.ProductionPresentationTests" };
            ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(filter));
        }
        [Serializable] private sealed class Result { public string utc, status, stage; public int passed, failed, skipped; }
        private sealed class Callback : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (!SessionState.GetBool(Key, false)) return;
                int stage = SessionState.GetInt(Key + ".stage", 0); string name = stage == 0 ? (SessionState.GetBool(Key + ".focused", false) ? "PlayModeFocused" : "PlayMode") : "EditMode";
                if (stage == 2) name = "AssetRecheck";
                TestRunnerApi.SaveResultToFile(result, Path.GetFullPath(Folder + "/" + name + ".xml"));
                File.WriteAllText(Folder + "/" + name + ".json", JsonUtility.ToJson(new Result { utc = DateTime.UtcNow.ToString("O"), status = result.FailCount == 0 && result.PassCount > 0 ? "passed" : "failed", stage = name, passed = result.PassCount, failed = result.FailCount, skipped = result.SkipCount }, true));
                if (stage == 0 && result.FailCount == 0)
                { SessionState.SetInt(Key + ".stage", 1); EditorApplication.delayCall += Run; }
                else { PhoneUiEvidence.SelectSize(SessionState.GetInt(Key + ".size", 0)); UnityEngine.Application.runInBackground = SessionState.GetBool(Key + ".background", false); SessionState.SetBool(Key, false); TestRunnerApi.UnregisterTestCallback(this); }
            }
        }
    }
}

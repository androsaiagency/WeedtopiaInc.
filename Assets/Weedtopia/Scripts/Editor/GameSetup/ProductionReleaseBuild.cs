using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit Android APK release build. Existing production gates remain mandatory.</summary>
    public static class ProductionReleaseBuild
    {
        [Serializable] private sealed class Result
        {
            public string utc = "", status = "", runId = "", apk = "", fingerprint = "", nativeSymbolReceipt = "", detail = "";
            public string previousSymbolLevel = "", restoredSymbolLevel = "", previousSymbolFormat = "", restoredSymbolFormat = "";
            public string previousManagedStacktrace = "", effectiveManagedStacktrace = "", restoredManagedStacktrace = "";
            public string previousErrorStacktrace = "", effectiveErrorStacktrace = "", restoredErrorStacktrace = "";
            public string previousExceptionStacktrace = "", effectiveExceptionStacktrace = "", restoredExceptionStacktrace = "";
            public bool releaseAccepted = false, deviceAccepted = false, humanAccepted = false;
        }
        [MenuItem("Weedtopia/Build/Production Android Release")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Finish the current editor operation before an explicit release build.");
            if (Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(scene => scene.isDirty))
                throw new InvalidOperationException("Unsaved scenes are protected. Save them before a release build.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android || !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Select the installed Android build target before this APK release build.");
            if (EditorUserBuildSettings.buildAppBundle || EditorUserBuildSettings.exportAsGoogleAndroidProject
                || PlayerSettings.Android.splitApplicationBinary || PlayerSettings.Android.buildApkPerCpuArchitecture)
                throw new InvalidOperationException("This explicit build archives a single APK. Configure APK output before building.");
            var scenes = ProductionVisualPreflight.ScenesForBuild();
            if (scenes.Length == 0 || scenes.Any(scene => !File.Exists(scene))) throw new IOException("Enabled release scenes are missing.");
            string runId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N");
            string folder = Path.GetFullPath(Path.Combine("Builds/Android/Release", runId));
            if (Directory.Exists(folder)) throw new IOException("Refusing to replace a previous release artifact.");
            Directory.CreateDirectory(folder);
            var result = new Result { utc = DateTime.UtcNow.ToString("O"), runId = runId, status = "building", apk = Path.Combine(folder, "WeedtopiaInc-Android-Release.apk") };
            var previousManagedStacktrace = PlayerSettings.GetIl2CppStacktraceInformation(NamedBuildTarget.Android);
            var previousErrorStacktrace = PlayerSettings.GetStackTraceLogType(LogType.Error);
            var previousExceptionStacktrace = PlayerSettings.GetStackTraceLogType(LogType.Exception);
            // PlayerSettings.SetStackTraceLogType also changes the running editor's Application setting.
            var previousRuntimeErrorStacktrace = UnityEngine.Application.GetStackTraceLogType(LogType.Error);
            var previousRuntimeExceptionStacktrace = UnityEngine.Application.GetStackTraceLogType(LogType.Exception);
            result.previousManagedStacktrace = previousManagedStacktrace.ToString();
            result.previousErrorStacktrace = previousErrorStacktrace.ToString();
            result.previousExceptionStacktrace = previousExceptionStacktrace.ToString();
            var previousLevel = UserBuildSettings.DebugSymbols.level;
            var previousFormat = UserBuildSettings.DebugSymbols.format;
            result.previousSymbolLevel = previousLevel.ToString(); result.previousSymbolFormat = previousFormat.ToString();
            DateTime started = DateTime.UtcNow;
            try
            {
                PlayerSettings.SetIl2CppStacktraceInformation(NamedBuildTarget.Android, Il2CppStacktraceInformation.MethodFileLineNumber);
                PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
                PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
                result.effectiveManagedStacktrace = PlayerSettings.GetIl2CppStacktraceInformation(NamedBuildTarget.Android).ToString();
                result.effectiveErrorStacktrace = PlayerSettings.GetStackTraceLogType(LogType.Error).ToString();
                result.effectiveExceptionStacktrace = PlayerSettings.GetStackTraceLogType(LogType.Exception).ToString();
                UserBuildSettings.DebugSymbols.level = global::Unity.Android.Types.DebugSymbolLevel.Full;
                UserBuildSettings.DebugSymbols.format = global::Unity.Android.Types.DebugSymbolFormat.Zip;
                result.fingerprint = ProductionVisualPreflight.Fingerprint(BuildTarget.Android, BuildOptions.None);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes, locationPathName = result.apk, target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android,
                    subtarget = (int)EditorUserBuildSettings.androidBuildSubtarget, options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded || !File.Exists(result.apk))
                    throw new BuildFailedException("The production Android build did not succeed: " + report.summary.result);
                result.nativeSymbolReceipt = NativeSymbolArtifacts.Capture(report, runId, started);
                result.status = "built_pending_release_acceptance";
            }
            catch (Exception error) { result.status = "failed"; result.detail = error.ToString(); throw; }
            finally
            {
                PlayerSettings.SetIl2CppStacktraceInformation(NamedBuildTarget.Android, previousManagedStacktrace);
                PlayerSettings.SetStackTraceLogType(LogType.Error, previousErrorStacktrace);
                PlayerSettings.SetStackTraceLogType(LogType.Exception, previousExceptionStacktrace);
                UnityEngine.Application.SetStackTraceLogType(LogType.Error, previousRuntimeErrorStacktrace);
                UnityEngine.Application.SetStackTraceLogType(LogType.Exception, previousRuntimeExceptionStacktrace);
                result.restoredManagedStacktrace = PlayerSettings.GetIl2CppStacktraceInformation(NamedBuildTarget.Android).ToString();
                result.restoredErrorStacktrace = PlayerSettings.GetStackTraceLogType(LogType.Error).ToString();
                result.restoredExceptionStacktrace = PlayerSettings.GetStackTraceLogType(LogType.Exception).ToString();
                UserBuildSettings.DebugSymbols.level = previousLevel; UserBuildSettings.DebugSymbols.format = previousFormat;
                result.restoredSymbolLevel = UserBuildSettings.DebugSymbols.level.ToString(); result.restoredSymbolFormat = UserBuildSettings.DebugSymbols.format.ToString();
                File.WriteAllText(Path.Combine(folder, "build-result.json"), JsonUtility.ToJson(result, true));
            }
        }
    }
}

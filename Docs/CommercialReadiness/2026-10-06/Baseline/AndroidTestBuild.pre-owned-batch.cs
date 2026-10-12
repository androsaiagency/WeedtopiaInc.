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
    /// <summary>Builds an explicitly requested, single-file test APK in the existing editor.</summary>
    [InitializeOnLoad]
    public static class AndroidTestBuild
    {
        public const string RequestPath = "Temp/WeedtopiaAndroidDevelopmentBuild.request.json";
        private const string OutputRoot = "Builds/Android";
        private static double nextCheck;
        private static string releaseDiagnosticApk = "";
        public static bool IsReleaseDiagnosticBuild(BuildReport report) => releaseDiagnosticApk.Length > 0
            && report.summary.platform == BuildTarget.Android && report.summary.options == BuildOptions.None
            && string.Equals(Path.GetFullPath(report.summary.outputPath), releaseDiagnosticApk, StringComparison.OrdinalIgnoreCase);
        public static BuildOptions RequestedBuildOptions(Request request) => request != null && request.releaseDiagnostics ? BuildOptions.None : BuildOptions.Development;

        [Serializable]
        public sealed class Request
        {
            public string id;
            public bool refreshAssets, applySelectedGameIcon, refreshOnly;
            public bool releaseDiagnostics = false;
        }

        [Serializable]
        private sealed class Result
        {
            public string id, status, startedUtc, finishedUtc, unityVersion, apkPath;
            public string packageName, version, architectures, scriptingBackend, buildType, detail, iconAssetPath, iconReceipt, textureSubtarget;
            public string previousTextureCompressionOverride, effectiveTextureCompressionOverride, restoredTextureCompressionOverride;
            public int previousMaximumTextureSizeOverride, effectiveMaximumTextureSizeOverride, restoredMaximumTextureSizeOverride;
            public string nativeSymbolReceipt = "", previousSymbolLevel = "", effectiveSymbolLevel = "", restoredSymbolLevel = "";
            public string previousSymbolFormat = "", effectiveSymbolFormat = "", restoredSymbolFormat = "";
            public string previousManagedStacktrace = "", effectiveManagedStacktrace = "", restoredManagedStacktrace = "";
            public string previousErrorStacktrace = "", effectiveErrorStacktrace = "", restoredErrorStacktrace = "";
            public string previousExceptionStacktrace = "", effectiveExceptionStacktrace = "", restoredExceptionStacktrace = "";
            public bool releaseAccepted = false, deviceAccepted = false, humanAccepted = false;
            public int processId, versionCode, minSdk, errors, warnings, maxJvmHeapMb;
            public long bytes;
            public double seconds;
            public string[] scenes, buildErrors;
        }

        static AndroidTestBuild() { EditorApplication.update += Poll; }

        [MenuItem("Weedtopia/Build/Android Test APK")]
        public static void RequestBuild()
        {
            if (File.Exists(RequestPath))
                throw new InvalidOperationException("An Android build request already exists: " + RequestPath);
            Directory.CreateDirectory("Temp");
            File.WriteAllText(RequestPath, JsonUtility.ToJson(new Request
            {
                id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"), refreshAssets = true, applySelectedGameIcon = true
            }, true));
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || BuildPipeline.isBuildingPlayer) return;

            Request request;
            try { request = JsonUtility.FromJson<Request>(File.ReadAllText(RequestPath)); }
            catch (Exception) { return; }
            if (request == null || string.IsNullOrEmpty(request.id)
                || request.id.Any(c => !char.IsLetterOrDigit(c) && c != '-')) return;

            string folder = Path.GetFullPath(OutputRoot + "/" + request.id);
            string resultPath = folder + "/build-result.json";
            Directory.CreateDirectory(folder);
            var result = File.Exists(resultPath)
                ? JsonUtility.FromJson<Result>(File.ReadAllText(resultPath))
                : new Result
                {
                    id = request.id, status = "requested", startedUtc = DateTime.UtcNow.ToString("O"),
                    processId = System.Diagnostics.Process.GetCurrentProcess().Id,
                    unityVersion = UnityEngine.Application.unityVersion,
                    apkPath = folder + (request.releaseDiagnostics ? "/WeedtopiaInc-Android-ReleaseDiagnostic.apk" : "/WeedtopiaInc-Android-Test.apk")
                };
            if (result.status == "succeeded" || result.status == "failed" || result.status == "refreshed")
            {
                File.Delete(RequestPath); // Consume this completed request; every retry uses a fresh ID.
                return;
            }

            try
            {
                if (result.processId != System.Diagnostics.Process.GetCurrentProcess().Id
                    || result.status == "building")
                    throw new InvalidOperationException("The previous build was interrupted. Submit a new request ID.");
                RequireReadyEditor();
                if (result.status == "requested" && request.refreshAssets)
                {
                    result.status = "refreshing";
                    Write(resultPath, result);
                    AssetDatabase.Refresh();
                    return; // Resume after any requested source import and compilation.
                }
                if (request.refreshOnly)
                {
                    result.status = "refreshed";
                    result.finishedUtc = DateTime.UtcNow.ToString("O");
                    Write(resultPath, result);
                    File.Delete(RequestPath);
                    return;
                }
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                    throw new InvalidOperationException("Android Build Support is not installed for this editor.");

                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                {
                    if (result.status != "switching")
                    {
                        result.status = "switching";
                        Write(resultPath, result);
                        if (!EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android))
                            throw new InvalidOperationException("Unity could not switch the active target to Android.");
                    }
                    return; // Continue only after platform import, compilation and domain reload.
                }

                if (request.applySelectedGameIcon)
                {
                    GameIconAuthoring.Apply();
                    result.iconAssetPath = GameIconAuthoring.PrimaryIconPath;
                    result.iconReceipt = GameIconAuthoring.ReceiptPath;
                }
                result.scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
                if (result.scenes.Length == 0 || result.scenes.Any(x => !File.Exists(x)))
                    throw new InvalidOperationException("Enabled build scenes are missing or empty.");
                string expectedApk = folder + (request.releaseDiagnostics ? "/WeedtopiaInc-Android-ReleaseDiagnostic.apk" : "/WeedtopiaInc-Android-Test.apk");
                if (!string.Equals(Path.GetFullPath(result.apkPath), Path.GetFullPath(expectedApk), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The requested build kind changed during preparation. Submit a fresh request ID.");
                if (File.Exists(result.apkPath))
                    throw new IOException("Refusing to replace an existing APK: " + result.apkPath);
                result.packageName = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
                result.version = PlayerSettings.bundleVersion;
                result.versionCode = PlayerSettings.Android.bundleVersionCode;
                result.minSdk = (int)PlayerSettings.Android.minSdkVersion;
                result.architectures = PlayerSettings.Android.targetArchitectures.ToString();
                result.scriptingBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android).ToString();
                result.buildType = request.releaseDiagnostics ? "ReleaseDiagnostic" : "Development";
                result.status = "building";
                Write(resultPath, result);
                Build(result, resultPath, request);
            }
            catch (Exception exception)
            {
                result.status = "failed";
                result.detail = exception.ToString();
                result.finishedUtc = DateTime.UtcNow.ToString("O");
                Write(resultPath, result);
                Debug.LogError("Android test build failed. See " + resultPath + "\n" + exception);
            }
        }

        private static void RequireReadyEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before requesting a build.");
            var dirty = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Where(x => x.isDirty).Select(x => x.path).ToArray();
            if (dirty.Length > 0)
                throw new InvalidOperationException("Unsaved scenes are protected. Save them before building: "
                    + string.Join(", ", dirty));
        }

        private static void Build(Result result, string resultPath, Request request)
        {
            bool oldBundle = EditorUserBuildSettings.buildAppBundle;
            bool oldExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            bool oldSplit = PlayerSettings.Android.splitApplicationBinary;
            bool oldPerCpu = PlayerSettings.Android.buildApkPerCpuArchitecture;
            bool oldKeystore = PlayerSettings.Android.useCustomKeystore;
            var previousManagedStacktrace = PlayerSettings.GetIl2CppStacktraceInformation(NamedBuildTarget.Android);
            var previousErrorStacktrace = PlayerSettings.GetStackTraceLogType(LogType.Error);
            var previousExceptionStacktrace = PlayerSettings.GetStackTraceLogType(LogType.Exception);
            // PlayerSettings.SetStackTraceLogType also changes the running editor's Application setting.
            var previousRuntimeErrorStacktrace = UnityEngine.Application.GetStackTraceLogType(LogType.Error);
            var previousRuntimeExceptionStacktrace = UnityEngine.Application.GetStackTraceLogType(LogType.Exception);
            result.previousManagedStacktrace = previousManagedStacktrace.ToString();
            result.previousErrorStacktrace = previousErrorStacktrace.ToString();
            result.previousExceptionStacktrace = previousExceptionStacktrace.ToString();
            int oldHeap = AndroidExternalToolsSettings.maxJvmHeapSize;
            var oldSymbolLevel = UserBuildSettings.DebugSymbols.level;
            var oldSymbolFormat = UserBuildSettings.DebugSymbols.format;
            result.previousSymbolLevel = oldSymbolLevel.ToString();
            result.previousSymbolFormat = oldSymbolFormat.ToString();
            MobileTextureSubtarget oldTextureSubtarget = EditorUserBuildSettings.androidBuildSubtarget;
            var oldTextureCompressionOverride = EditorUserBuildSettings.overrideTextureCompression;
            int oldMaximumTextureSizeOverride = EditorUserBuildSettings.overrideMaxTextureSize;
            result.previousTextureCompressionOverride = oldTextureCompressionOverride.ToString();
            result.previousMaximumTextureSizeOverride = oldMaximumTextureSizeOverride;
            try
            {
                // Large illustrated scenes need a larger packaging heap. Restore the user's global preference afterward.
                AndroidExternalToolsSettings.maxJvmHeapSize = Mathf.Max(oldHeap, 8192);
                result.maxJvmHeapMb = AndroidExternalToolsSettings.maxJvmHeapSize;
                PlayerSettings.SetIl2CppStacktraceInformation(NamedBuildTarget.Android, Il2CppStacktraceInformation.MethodFileLineNumber);
                PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
                PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
                result.effectiveManagedStacktrace = PlayerSettings.GetIl2CppStacktraceInformation(NamedBuildTarget.Android).ToString();
                result.effectiveErrorStacktrace = PlayerSettings.GetStackTraceLogType(LogType.Error).ToString();
                result.effectiveExceptionStacktrace = PlayerSettings.GetStackTraceLogType(LogType.Exception).ToString();
                UserBuildSettings.DebugSymbols.level = global::Unity.Android.Types.DebugSymbolLevel.Full;
                UserBuildSettings.DebugSymbols.format = global::Unity.Android.Types.DebugSymbolFormat.Zip;
                result.effectiveSymbolLevel = UserBuildSettings.DebugSymbols.level.ToString();
                result.effectiveSymbolFormat = UserBuildSettings.DebugSymbols.format.ToString();
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                PlayerSettings.Android.splitApplicationBinary = false;
                PlayerSettings.Android.buildApkPerCpuArchitecture = false;
                PlayerSettings.Android.useCustomKeystore = false;
                // The configured Vulkan/GLES3 devices support ETC2, including smooth RGBA cutout alpha.
                // Supplying only target leaves BuildPlayerOptions.subtarget at the platform default.
                EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
                result.textureSubtarget = MobileTextureSubtarget.ETC2.ToString();
                // Development import overrides are persistent and can otherwise disable all GPU compression in a build.
                EditorUserBuildSettings.overrideTextureCompression = OverrideTextureCompression.NoOverride;
                EditorUserBuildSettings.overrideMaxTextureSize = 0;
                result.effectiveTextureCompressionOverride = EditorUserBuildSettings.overrideTextureCompression.ToString();
                result.effectiveMaximumTextureSizeOverride = EditorUserBuildSettings.overrideMaxTextureSize;
                releaseDiagnosticApk = request.releaseDiagnostics ? Path.GetFullPath(result.apkPath) : "";
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = result.scenes, locationPathName = result.apkPath,
                    target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android,
                    subtarget = (int)MobileTextureSubtarget.ETC2,
                    options = RequestedBuildOptions(request)
                });
                result.errors = (int)report.summary.totalErrors;
                result.warnings = (int)report.summary.totalWarnings;
                result.seconds = report.summary.totalTime.TotalSeconds;
                result.buildErrors = report.steps.SelectMany(x => x.messages)
                    .Where(x => x.type == LogType.Error || x.type == LogType.Exception)
                    .Select(x => x.content).ToArray();
                bool passed = report.summary.result == BuildResult.Succeeded && File.Exists(result.apkPath);
                if (passed) result.nativeSymbolReceipt = NativeSymbolArtifacts.Capture(report, result.id, DateTime.Parse(result.startedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind));
                result.status = passed ? "succeeded" : "failed";
                result.detail = report.summary.result.ToString();
                result.bytes = passed ? new FileInfo(result.apkPath).Length : 0;
                result.finishedUtc = DateTime.UtcNow.ToString("O");
                Write(resultPath, result);
                Debug.Log("Android test build " + result.status + ": " + result.apkPath);
            }
            finally
            {
                releaseDiagnosticApk = "";
                EditorUserBuildSettings.buildAppBundle = oldBundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = oldExport;
                PlayerSettings.Android.splitApplicationBinary = oldSplit;
                PlayerSettings.Android.buildApkPerCpuArchitecture = oldPerCpu;
                PlayerSettings.Android.useCustomKeystore = oldKeystore;
                AndroidExternalToolsSettings.maxJvmHeapSize = oldHeap;
                PlayerSettings.SetIl2CppStacktraceInformation(NamedBuildTarget.Android, previousManagedStacktrace);
                PlayerSettings.SetStackTraceLogType(LogType.Error, previousErrorStacktrace);
                PlayerSettings.SetStackTraceLogType(LogType.Exception, previousExceptionStacktrace);
                UnityEngine.Application.SetStackTraceLogType(LogType.Error, previousRuntimeErrorStacktrace);
                UnityEngine.Application.SetStackTraceLogType(LogType.Exception, previousRuntimeExceptionStacktrace);
                result.restoredManagedStacktrace = PlayerSettings.GetIl2CppStacktraceInformation(NamedBuildTarget.Android).ToString();
                result.restoredErrorStacktrace = PlayerSettings.GetStackTraceLogType(LogType.Error).ToString();
                result.restoredExceptionStacktrace = PlayerSettings.GetStackTraceLogType(LogType.Exception).ToString();
                UserBuildSettings.DebugSymbols.level = oldSymbolLevel;
                UserBuildSettings.DebugSymbols.format = oldSymbolFormat;
                result.restoredSymbolLevel = UserBuildSettings.DebugSymbols.level.ToString();
                result.restoredSymbolFormat = UserBuildSettings.DebugSymbols.format.ToString();
                EditorUserBuildSettings.androidBuildSubtarget = oldTextureSubtarget;
                EditorUserBuildSettings.overrideTextureCompression = oldTextureCompressionOverride;
                EditorUserBuildSettings.overrideMaxTextureSize = oldMaximumTextureSizeOverride;
                result.restoredTextureCompressionOverride = EditorUserBuildSettings.overrideTextureCompression.ToString();
                result.restoredMaximumTextureSizeOverride = EditorUserBuildSettings.overrideMaxTextureSize;
                Write(resultPath, result);
            }
        }

        private static void Write(string path, Result result)
        {
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(result, true));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
    }
}

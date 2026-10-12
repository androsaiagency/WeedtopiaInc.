using System;
using System.IO;
using System.Linq;
using UnityEditor;
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

        [Serializable]
        public sealed class Request
        {
            public string id;
            public bool refreshAssets;
        }

        [Serializable]
        private sealed class Result
        {
            public string id, status, startedUtc, finishedUtc, unityVersion, apkPath;
            public string packageName, version, architectures, scriptingBackend, buildType, detail;
            public int processId, versionCode, minSdk, errors, warnings;
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
                id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"), refreshAssets = true
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
                    apkPath = folder + "/WeedtopiaInc-Android-Test.apk"
                };
            if (result.status == "succeeded" || result.status == "failed")
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

                result.scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
                if (result.scenes.Length == 0 || result.scenes.Any(x => !File.Exists(x)))
                    throw new InvalidOperationException("Enabled build scenes are missing or empty.");
                if (File.Exists(result.apkPath))
                    throw new IOException("Refusing to replace an existing APK: " + result.apkPath);
                result.packageName = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
                result.version = PlayerSettings.bundleVersion;
                result.versionCode = PlayerSettings.Android.bundleVersionCode;
                result.minSdk = (int)PlayerSettings.Android.minSdkVersion;
                result.architectures = PlayerSettings.Android.targetArchitectures.ToString();
                result.scriptingBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android).ToString();
                result.buildType = "Development";
                result.status = "building";
                Write(resultPath, result);
                Build(result, resultPath);
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

        private static void Build(Result result, string resultPath)
        {
            bool oldBundle = EditorUserBuildSettings.buildAppBundle;
            bool oldExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            bool oldSplit = PlayerSettings.Android.splitApplicationBinary;
            bool oldPerCpu = PlayerSettings.Android.buildApkPerCpuArchitecture;
            bool oldKeystore = PlayerSettings.Android.useCustomKeystore;
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                PlayerSettings.Android.splitApplicationBinary = false;
                PlayerSettings.Android.buildApkPerCpuArchitecture = false;
                PlayerSettings.Android.useCustomKeystore = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = result.scenes, locationPathName = result.apkPath,
                    target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.Development
                });
                result.errors = (int)report.summary.totalErrors;
                result.warnings = (int)report.summary.totalWarnings;
                result.seconds = report.summary.totalTime.TotalSeconds;
                result.buildErrors = report.steps.SelectMany(x => x.messages)
                    .Where(x => x.type == LogType.Error || x.type == LogType.Exception)
                    .Select(x => x.content).ToArray();
                bool passed = report.summary.result == BuildResult.Succeeded && File.Exists(result.apkPath);
                result.status = passed ? "succeeded" : "failed";
                result.detail = report.summary.result.ToString();
                result.bytes = passed ? new FileInfo(result.apkPath).Length : 0;
                result.finishedUtc = DateTime.UtcNow.ToString("O");
                Write(resultPath, result);
                Debug.Log("Android test build " + result.status + ": " + result.apkPath);
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = oldBundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = oldExport;
                PlayerSettings.Android.splitApplicationBinary = oldSplit;
                PlayerSettings.Android.buildApkPerCpuArchitecture = oldPerCpu;
                PlayerSettings.Android.useCustomKeystore = oldKeystore;
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

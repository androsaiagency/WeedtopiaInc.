using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit file-backed jobs in the already-open editor. Never starts or closes an editor.</summary>
    [InitializeOnLoad]
    public static class LayoutEditorJob
    {
        public const string Folder = "Docs/LayoutRevision/EditorJobs";
        [Serializable] public sealed class Request { public string id, command; }
        [Serializable] private sealed class Result
        {
            public string id, command, utc, status, detail;
            public int processId;
            public bool playing, compiling;
            public string[] scenes, dirtyScenes;
        }
        private static double nextCheck;
        static LayoutEditorJob() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            string path = Folder + "/request.json";
            if (!File.Exists(path) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            Request job;
            try { job = JsonUtility.FromJson<Request>(File.ReadAllText(path)); }
            catch (Exception) { return; } // Writer may still be replacing its own request.
            if (job == null || string.IsNullOrEmpty(job.id) || job.id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return;
            string resultPath = Folder + "/" + job.id + ".json";
            if (File.Exists(resultPath)) return;
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
            var result = new Result { id = job.id, command = job.command, utc = DateTime.UtcNow.ToString("O"),
                processId = System.Diagnostics.Process.GetCurrentProcess().Id, playing = EditorApplication.isPlaying,
                compiling = EditorApplication.isCompiling, scenes = scenes.Select(x => x.path).ToArray(),
                dirtyScenes = scenes.Where(x => x.isDirty).Select(x => x.path).ToArray() };
            try
            {
                if (job.command == "status") result.status = "observed";
                else
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode || result.dirtyScenes.Length > 0)
                        throw new InvalidOperationException("User Play Mode or unsaved scene changes are protected. Save and stop Play Mode before an explicit job.");
                    // Methods are resolved only for explicit allowlisted commands, after their scripts have imported.
                    string type, method;
                    switch (job.command)
                    {
                        case "apply": type = "Weedtopia.Editor.GameSetup.BurningHillsLayoutInstaller"; method = "Apply"; break;
                        case "integrate": type = "Weedtopia.Editor.GameSetup.ProductionBuildingIntegration"; method = "Apply"; break;
                        case "phone-ui": type = "Weedtopia.Editor.GameSetup.PhoneUiAuthoring"; method = "Apply"; break;
                        case "shop": type = "Weedtopia.Editor.GameSetup.ShopAuthoring"; method = "Apply"; break;
                        case "phone-ui-verify": type = "Weedtopia.Editor.GameSetup.PhoneUiEvidence"; method = "Begin"; break;
                        case "commercial-tests": type = "Weedtopia.Editor.GameSetup.CommercialReadinessTestJob"; method = "Begin"; break;
                        case "commercial-startup-benchmark": type = "Weedtopia.Editor.GameSetup.CommercialReadinessTestJob"; method = "BeginStartupBenchmark"; break;
                        case "phone-ui-tests": type = "Weedtopia.Editor.GameSetup.PhoneUiTestJob"; method = "Begin"; break;
                        case "phone-ui-retest": type = "Weedtopia.Editor.GameSetup.PhoneUiTestJob"; method = "BeginFocused"; break;
                        case "phone-ui-edit-tests": type = "Weedtopia.Editor.GameSetup.PhoneUiTestJob"; method = "ResumeEditMode"; break;
                        case "phone-ui-asset-recheck": type = "Weedtopia.Editor.GameSetup.PhoneUiTestJob"; method = "RecheckAssets"; break;
                        case "android-building-textures-inspect": type = "Weedtopia.Editor.GameSetup.AndroidBuildingTextureOptimization"; method = "Inspect"; break;
                        case "android-building-textures": type = "Weedtopia.Editor.GameSetup.AndroidBuildingTextureOptimization"; method = "Apply"; break;
                        case "android-building-textures-probe": type = "Weedtopia.Editor.GameSetup.AndroidBuildingTextureOptimization"; method = "Probe"; break;
                        case "android-building-textures-probe-astc4": type = "Weedtopia.Editor.GameSetup.AndroidBuildingTextureOptimization"; method = "ProbeAstc4"; break;
                        case "android-building-textures-probe-1024": type = "Weedtopia.Editor.GameSetup.AndroidBuildingTextureOptimization"; method = "Probe1024"; break;
                        default: throw new InvalidOperationException("Unknown explicit job " + job.command);
                    }
                    var target = typeof(LayoutEditorJob).Assembly.GetType(type, true);
                    target.GetMethod(method, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null, null);
                    result.status = "completed";
                }
            }
            catch (Exception ex) { result.status = "blocked_or_failed"; result.detail = ex.ToString(); }
            Directory.CreateDirectory(Folder);
            File.WriteAllText(resultPath, JsonUtility.ToJson(result, true));
        }
    }
}

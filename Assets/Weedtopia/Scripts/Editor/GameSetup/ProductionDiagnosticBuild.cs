using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Weedtopia.Editor.GameSetup
{
    public static class ProductionDiagnosticBuild
    {
        public static void Build()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Diagnostic build requires an owned batch editor");
            string folder="Builds/ProductionFinishDiagnostic";Directory.CreateDirectory(folder);
            string fingerprint=ProductionVisualPreflight.Fingerprint(BuildTarget.StandaloneWindows64,BuildOptions.Development);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=ProductionVisualPreflight.ScenesForBuild(),locationPathName=folder+"/BurningHillsDiagnostic.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            bool passed=report.summary.result==BuildResult.Succeeded;
            File.WriteAllText("Docs/ProductionFinish/Evidence/development_build.json","{\"utc\":\""+DateTime.UtcNow.ToString("O")+"\",\"passed\":"+(passed?"true":"false")+",\"releaseAccepted\":false,\"target\":\"StandaloneWindows64 Development\",\"bytes\":"+report.summary.totalSize+",\"fingerprint\":\""+fingerprint+"\"}");
            if(!passed)throw new InvalidOperationException("Development diagnostic build failed: "+report.summary.result);
        }
    }
}

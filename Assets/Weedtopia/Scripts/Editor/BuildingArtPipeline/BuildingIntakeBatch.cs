using System;
using System.IO;
using UnityEngine;

namespace Weedtopia.Editor.BuildingArtPipeline
{
    /// <summary>Invoke only in an authorized, exclusively owned Unity editor process.</summary>
    public static class BuildingIntakeBatch
    {
        public const string ReportPath = "Docs/ArtProduction/Validation/unity_intake_report.json";

        public static void CreateOrRepairAndValidate()
        {
            BuildingArtManifest manifest = BuildingManifestReader.Load();
            BuildingScaffoldResult scaffold = BuildingIntakeService.CreateOrRepairScaffold(manifest);
            BuildingValidationReport report = BuildingIntakeValidator.Validate(manifest);
            WriteReport(report);
            Debug.Log($"Scaffold created={scaffold.wrappersCreated}, repaired={scaffold.wrappersRepaired}, unchanged={scaffold.wrappersUnchanged}. " +
                      $"Intake errors={report.errors}, warnings={report.warnings}. Report: {ReportPath}");
            if (report.errors > 0) throw new InvalidOperationException("Intake/production validation has errors; inspect the saved report.");
        }

        public static void ValidateAndWriteReport()
        {
            BuildingValidationReport report = BuildingIntakeValidator.Validate(BuildingManifestReader.Load());
            WriteReport(report);
            Debug.Log($"Read-only asset validation: errors={report.errors}, warnings={report.warnings}. Explicit report export: {ReportPath}");
            if (report.errors > 0) throw new InvalidOperationException("Validation failed; inspect the report.");
        }

        private static void WriteReport(BuildingValidationReport report)
        {
            string path = BuildingManifestReader.AbsolutePath(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(report, true));
            if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
        }
    }
}

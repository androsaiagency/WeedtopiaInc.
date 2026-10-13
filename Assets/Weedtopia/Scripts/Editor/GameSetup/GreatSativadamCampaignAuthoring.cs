using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicitly authors serialized travel rows while preserving each existing city panel.</summary>
    public static class GreatSativadamCampaignAuthoring
    {
        public static bool ApplyCampaignRows(CityCampaignUi ui)
        {
            if (ui == null) throw new InvalidOperationException("Missing authored city travel panel.");
            var definitions = AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
            var rows = (ui.travelRows ?? Array.Empty<CityTravelRow>()).Where(r => r != null).ToList();
            if (rows.GroupBy(r => r.cityId).Any(group => group.Count() != 1))
                throw new InvalidOperationException("Preserve the independently authored panel: duplicate travel destinations.");
            if (rows.Any(row => !definitions.cities.Any(city => city.id == row.cityId) || row.progress == null || row.travel == null))
                throw new InvalidOperationException("Preserve the independently authored panel: unknown or incomplete travel destination.");
            bool changed = false;
            foreach (var city in definitions.cities.Where(city => rows.All(row => row.cityId != city.id)))
            {
                if (ui.rockProgress == null || ui.rockTravel == null) throw new InvalidOperationException("Missing authored travel templates.");
                string objectName = city.id == GreatSativadamAuthoring.City ? "GreatSativadam" : "City_" + city.id;
                var progressObject = Object.Instantiate(ui.rockProgress.gameObject, ui.rockProgress.transform.parent);
                progressObject.name = objectName + "Progress";
                var progress = progressObject.GetComponent<Text>(); progress.text = "";
                var buttonObject = Object.Instantiate(ui.rockTravel.gameObject, ui.rockTravel.transform.parent);
                buttonObject.name = "Travel" + objectName;
                var button = buttonObject.GetComponent<Button>(); button.onClick = new Button.ButtonClickedEvent();
                button.GetComponentInChildren<Text>(true).text = "";
                var row = progressObject.GetComponent<CityTravelRow>() ?? progressObject.AddComponent<CityTravelRow>();
                row.cityId = city.id; row.progress = progress; row.travel = button;
                rows.Add(row); EditorUtility.SetDirty(row); changed = true;
            }
            if (rows.Count != definitions.cities.Length)
                throw new InvalidOperationException("Travel rows do not cover all authored cities.");
            if (!changed) return false;
            ui.travelRows = rows.ToArray(); EditorUtility.SetDirty(ui);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ui);
            return true;
        }

        public static string[] ApplyWithReport()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var changed = new List<string>();
            try
            {
                var panel = PrefabUtility.LoadPrefabContents(BigRockCityAuthoring.CampaignPath);
                try
                {
                    if (ApplyCampaignRows(panel.GetComponent<CityCampaignUi>()))
                        PrefabUtility.SaveAsPrefabAsset(panel, BigRockCityAuthoring.CampaignPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(panel); }
                var definitions = AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
                foreach (string path in definitions.cities.Select(city => city.scenePath).Distinct().Where(File.Exists))
                {
                    var scene = EditorSceneManager.OpenScene(path);
                    var root = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<GameRoot>(true)).Single();
                    if (!ApplyCampaignRows(root.Ui.Campaign)) continue;
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); changed.Add(path);
                }
                AssetDatabase.SaveAssets(); return changed.ToArray();
            }
            finally
            {
                if (setup.Any(scene => scene.isLoaded) && setup.Count(scene => scene.isActive) == 1)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
    }
}

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
    /// <summary>Adds a serialized travel row without rebuilding existing menus or cities.</summary>
    public static class GoldenFlowerCampaignAuthoring
    {
        public static bool ApplyCampaignRows(CityCampaignUi ui)
        {
            if (ui == null) throw new InvalidOperationException("Missing authored city travel panel.");
            var rows = (ui.travelRows ?? Array.Empty<CityTravelRow>()).Where(r => r != null).ToList();
            if (rows.Any(r => r.cityId == GoldenFlowerAuthoring.City)) return false;
            if (ui.rockProgress == null || ui.rockTravel == null) throw new InvalidOperationException("Missing authored travel templates.");
            var progressObject = Object.Instantiate(ui.rockProgress.gameObject, ui.rockProgress.transform.parent);
            progressObject.name = "GoldenFlowerProgress";
            var progress = progressObject.GetComponent<Text>(); progress.text = "";
            var buttonObject = Object.Instantiate(ui.rockTravel.gameObject, ui.rockTravel.transform.parent);
            buttonObject.name = "TravelGoldenFlower";
            var button = buttonObject.GetComponent<Button>(); button.onClick = new Button.ButtonClickedEvent();
            button.GetComponentInChildren<Text>(true).text = "";
            var row = progressObject.GetComponent<CityTravelRow>() ?? progressObject.AddComponent<CityTravelRow>();
            row.cityId = GoldenFlowerAuthoring.City; row.progress = progress; row.travel = button;
            rows.Add(row); ui.travelRows = rows.ToArray();
            EditorUtility.SetDirty(ui); EditorUtility.SetDirty(row);
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
                foreach (string path in definitions.cities.Select(c => c.scenePath).Distinct().Where(File.Exists))
                {
                    var scene = EditorSceneManager.OpenScene(path);
                    var root = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<GameRoot>(true)).Single();
                    if (!ApplyCampaignRows(root.Ui.Campaign)) continue;
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); changed.Add(path);
                }
                AssetDatabase.SaveAssets(); return changed.ToArray();
            }
            finally
            {
                if (setup.Any(s => s.isLoaded) && setup.Count(s => s.isActive) == 1) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
    }
}

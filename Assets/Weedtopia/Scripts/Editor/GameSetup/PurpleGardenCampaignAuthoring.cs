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
    /// <summary>Explicit additive travel-row installation. Existing manual rows and scene objects are retained.</summary>
    public static class PurpleGardenCampaignAuthoring
    {
        [MenuItem("Weedtopia/Cities/Install Purple Garden Travel Entry")]
        public static void Apply() => ApplyWithReport();

        public static string[] ApplyWithReport()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var changedScenes = new List<string>();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var panel = PrefabUtility.LoadPrefabContents(BigRockCityAuthoring.CampaignPath);
                try
                {
                    if (ApplyCampaignRows(panel.GetComponent<CityCampaignUi>()))
                        PrefabUtility.SaveAsPrefabAsset(panel, BigRockCityAuthoring.CampaignPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(panel); }
                foreach (var path in new[] { GameScaffold.GameScene, BigRockCityAuthoring.ScenePath,
                    SevenHillsAuthoring.ScenePath, LittleMontanaAuthoring.ScenePath,
                    CassaVadelaAuthoring.ScenePath, MarkWoodCityAuthoring.ScenePath, GreenLeafVillageAuthoring.ScenePath, PurpleGardenAuthoring.ScenePath })
                {
                    if (!File.Exists(path)) continue;
                    var scene = EditorSceneManager.OpenScene(path);
                    var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameRoot>(true)).Single();
                    if (!ApplyCampaignRows(root.Ui.Campaign)) continue;
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    changedScenes.Add(path);
                }
                AssetDatabase.SaveAssets();
                return changedScenes.ToArray();
            }
            finally
            {
                if (setup.Any(s => s.isLoaded) && setup.Count(s => s.isActive) == 1)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        public static bool ApplyCampaignRows(CityCampaignUi ui)
        {
            if (ui == null) throw new InvalidOperationException("Missing authored city campaign UI.");
            var rows = (ui.travelRows ?? Array.Empty<CityTravelRow>()).Where(r => r != null).ToList();
            if (rows.Any(r => r.cityId == "purple_garden")) return false;
            if (ui.rockProgress == null || ui.rockTravel == null)
                throw new InvalidOperationException("Missing authored travel templates; refusing a broad UI rebuild.");
            var progressCopy = Object.Instantiate(ui.rockProgress.gameObject, ui.rockProgress.transform.parent);
            progressCopy.name = "PurpleGardenProgress";
            var progress = progressCopy.GetComponent<Text>(); progress.text = "";
            var travelCopy = Object.Instantiate(ui.rockTravel.gameObject, ui.rockTravel.transform.parent);
            travelCopy.name = "TravelPurpleGarden";
            var travel = travelCopy.GetComponent<Button>(); travel.onClick = new Button.ButtonClickedEvent();
            travel.GetComponentInChildren<Text>(true).text = "";
            var row = progressCopy.GetComponent<CityTravelRow>() ?? progressCopy.AddComponent<CityTravelRow>();
            row.cityId = "purple_garden"; row.progress = progress; row.travel = travel;
            rows.Add(row); ui.travelRows = rows.ToArray();
            EditorUtility.SetDirty(row); EditorUtility.SetDirty(ui);
            PrefabUtility.RecordPrefabInstancePropertyModifications(row);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ui);
            return true;
        }
    }
}

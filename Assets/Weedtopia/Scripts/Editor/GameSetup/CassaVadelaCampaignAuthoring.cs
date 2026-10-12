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
    public static class CassaVadelaCampaignAuthoring
    {
        [MenuItem("Weedtopia/Cities/Install Cassa Vadela Travel Entry")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
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
                    "Assets/Weedtopia/Game/Scenes/CassaVadela.unity" })
                {
                    if (!File.Exists(path)) continue;
                    var scene = EditorSceneManager.OpenScene(path);
                    var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameRoot>(true)).Single();
                    if (!ApplyCampaignRows(root.Ui.Campaign)) continue;
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();
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
            if (rows.Any(r => r.cityId == "cassa_vadela")) return false;
            if (ui.rockProgress == null || ui.rockTravel == null)
                throw new InvalidOperationException("Missing authored travel templates; refusing a broad UI rebuild.");
            var progressCopy = Object.Instantiate(ui.rockProgress.gameObject, ui.rockProgress.transform.parent);
            progressCopy.name = "CassaVadelaProgress";
            var progress = progressCopy.GetComponent<Text>(); progress.text = "";
            var travelCopy = Object.Instantiate(ui.rockTravel.gameObject, ui.rockTravel.transform.parent);
            travelCopy.name = "TravelCassaVadela";
            var travel = travelCopy.GetComponent<Button>(); travel.onClick = new Button.ButtonClickedEvent();
            travel.GetComponentInChildren<Text>(true).text = "";
            var row = progressCopy.GetComponent<CityTravelRow>() ?? progressCopy.AddComponent<CityTravelRow>();
            row.cityId = "cassa_vadela"; row.progress = progress; row.travel = travel;
            rows.Add(row); ui.travelRows = rows.ToArray();
            EditorUtility.SetDirty(row); EditorUtility.SetDirty(ui);
            PrefabUtility.RecordPrefabInstancePropertyModifications(row);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ui);
            return true;
        }
    }
}

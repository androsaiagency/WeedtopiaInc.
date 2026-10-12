using System;
using System.Linq;
using UnityEngine;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    public static class ConstructionFenceEvidence
    {
        public static void Prepare(GameRoot root, Action<string, Action> shot)
        {
            var life = UnityEngine.Object.FindFirstObjectByType<LivingTownPresentation>();
            life.diagnosticFreeze = true; life.PresentAt(20);
            root.Presentation.conveyors.diagnosticFreeze = true;
            foreach (var building in root.Session.State.buildings) building.buildingSeconds = 0;
            void View(string id, int level, float zoom)
            {
                var state = root.Session.State.Find(id); state.level = level; state.buildingSeconds = 120;
                root.Ui.Close(); root.Ui.Interaction.CloseBuilding(); root.CameraRig.ShowOverview();
                root.CameraRig.SetDiagnosticZoom(zoom);
                root.CameraRig.Focus(root.Site(id).transform.position + DistrictMap.ToWorld(new Vector2(0,12)));
            }
            shot("01_cultivation", () => View("farm_grow_01", 1, 21));
            shot("02_processing", () => View("farm_processing", 10, 26));
            shot("03_farm_warehouse", () => View("farm_warehouse", 125, 29));
            shot("04_city_warehouse", () => View("city_warehouse", 4999, 31));
            foreach (string family in new[] { "coffee_shop", "residential_house", "butter_factory", "bakery", "supermarket" })
            {
                string id = root.Session.State.buildings.First(b => b.family == family).id;
                int level = family == "residential_house" || family == "supermarket" ? 4999 : 125;
                shot("family_" + family, () => View(id, level, 30));
            }
            string park = root.Session.State.buildings.First(b => b.id.StartsWith("settlement_")).id;
            shot("05_park", () => { root.Session.State.Find(park).family = "park"; View(park, 125, 29); });
            shot("06_adjacent_buildings", () => {
                View("farm_grow_01", 4999, 40); View("farm_grow_02", 4999, 40);
                root.CameraRig.Focus((root.Site("farm_grow_01").transform.position + root.Site("farm_grow_02").transform.position) * .5f + DistrictMap.ToWorld(new Vector2(0,12)));
            });
            shot("07_upgrade_started", () => View("farm_grow_01", 1, 21));
            shot("08_paused", () => { root.Session.Paused = true; root.Session.Advance(20); });
            shot("09_completed", () => {
                root.Session.Paused = false; root.Session.State.Find("farm_grow_01").buildingSeconds = 1; root.Session.Advance(1);
            });
        }
    }
}

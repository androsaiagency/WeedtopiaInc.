using System;
using System.Linq;
using System.IO;
using UnityEngine;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    public static class ConveyorVisualEvidence
    {
        public static void Prepare(GameRoot root, Action<string, Action> shot, bool cargoCloseups = false)
        {
            var network = root.Presentation.conveyors; network.diagnosticFreeze = true;
            var life = UnityEngine.Object.FindFirstObjectByType<LivingTownPresentation>(); life.diagnosticFreeze = true; life.PresentAt(20);
            var defs = root.Definitions.routes.Where(d => network.routes.Any(r => r.routeId == d.id)).ToArray();
            foreach (var route in defs)
            {
                var source = root.Session.State.Find(route.from);
                string role = root.Definitions.Building(source.family).managerRole;
                var worker = root.Session.Workers.Grant(role, 2); root.Session.Workers.Assign(worker.id, source.id, 0);
            }
            // Owned diagnostic stock exercises actual Simulation.Belt transfer notifications.
            void TickStock()
            {
                foreach (var def in defs)
                {
                    var source = root.Session.State.Find(def.from);
                    source.Set(def.resource, 1); source.cycleProgress = 0;
                    root.Session.State.Find(def.to).Set(def.resource, 0);
                }
                root.Session.Advance(1); root.Refresh();
            }
            for (int second = 0; second < 50; second++)
            { TickStock(); network.Animate(1, false, root.CameraRig.RenderCamera); }
            if (cargoCloseups)
            {
                PrepareCargoCloseups(root, shot, TickStock);
                return;
            }
            File.WriteAllText("Docs/ConveyorRevision/motion_receipt.json", JsonUtility.ToJson(new MotionReceipt {
                utc = DateTime.UtcNow.ToString("O"), routes = network.routes.Select(r => new RouteReceipt {
                    id = r.routeId, moving = r.IsMoving, visibleCargo = r.VisibleCargoCount, travelled = r.AnimationDistance }).ToArray() }, true));
            void View(Vector2 position, float zoom)
            {
                root.Ui.Close(); root.Ui.Interaction.CloseBuilding(); root.CameraRig.ShowOverview();
                root.CameraRig.SetDiagnosticZoom(zoom); root.CameraRig.Focus(DistrictMap.ToWorld(position));
            }
            shot("01_farm_network", () => View(new Vector2(400, -27), 86));
            shot("02_reference_area", () => View(new Vector2(342, -24), 47));
            shot("03_junction_detail", () => View(new Vector2(335, -10), 20));
            shot("04_processing_connection", () => View(new Vector2(258, -28), 28));
            shot("05_paused", () => { root.Session.Paused = true; network.Animate(5, true, root.CameraRig.RenderCamera); });
            shot("06_motion_start", () => { root.Session.Paused = false; View(new Vector2(327, -19), 30); });
            for (int i = 0; i < 48; i++)
            {
                int frame = i;
                shot("motion_" + frame.ToString("D3"), () => {
                    if (frame % 24 == 0) TickStock();
                    network.Animate(1f / 24, false, root.CameraRig.RenderCamera);
                });
            }
        }
        private static void PrepareCargoCloseups(GameRoot root, Action<string, Action> shot, Action tickStock)
        {
            var network = root.Presentation.conveyors;
            var route = network.routes.First(r => r.routeId == "grow1_processing");
            var camera = root.CameraRig.RenderCamera;
            var art = route.cargoMarkers[0].GetComponentInChildren<SpriteRenderer>(true);
            var authoredOffset = art.transform.localPosition;
            var path = route.conveyorPath;
            float distance = 0;
            var samples = new System.Collections.Generic.List<(string name, float distance)>();
            for (int i = 1; i < path.Length; i++)
            {
                float length = Vector3.Distance(path[i - 1], path[i]);
                if (i == 1) samples.Add(("vertical", distance + length * .5f));
                if (i == 7) samples.Add(("corner", distance + length * .5f));
                if (i == path.Length - 1) samples.Add(("horizontal", distance + length * .5f));
                distance += length;
            }
            var receipt = new CargoReceipt { utc = DateTime.UtcNow.ToString("O") };
            foreach (var sample in samples)
            {
                shot(sample.name + "_before", () => {
                    foreach (var r in network.routes) r.ResetConveyorVisuals();
                    route.spawnDelay = 0; route.ResetConveyorVisuals(); tickStock();
                    route.Animate(sample.distance / route.cargoSpeed, false, camera);
                    if (!art.gameObject.activeInHierarchy) throw new InvalidOperationException("Diagnostic cargo did not spawn.");
                    art.transform.localPosition = Vector3.zero;
                    root.Ui.Close(); root.Ui.Interaction.CloseBuilding(); root.CameraRig.ShowOverview();
                    root.CameraRig.SetDiagnosticZoom(12); root.CameraRig.Focus(route.cargoMarkers[0].position);
                });
                shot(sample.name + "_after", () => {
                    art.transform.localPosition = authoredOffset;
                    float surfaceY = network.sections.Max(s => s.transform.position.y);
                    float clearance = art.bounds.min.y - surfaceY;
                    float screenShift = Vector2.Distance(camera.WorldToScreenPoint(art.transform.position), camera.WorldToScreenPoint(route.cargoMarkers[0].position));
                    if (clearance <= 0 || screenShift > .05f) throw new InvalidOperationException("Cargo intersects the deck or changes screen position.");
                    receipt.views.Add(new CargoView { name = sample.name, deckClearance = clearance, screenShiftPixels = screenShift });
                    File.WriteAllText("Docs/ConveyorCargoFix/geometry_receipt.json", JsonUtility.ToJson(receipt, true));
                });
            }
        }
        [Serializable] private sealed class CargoReceipt { public string utc; public System.Collections.Generic.List<CargoView> views = new System.Collections.Generic.List<CargoView>(); }
        [Serializable] private sealed class CargoView { public string name; public float deckClearance, screenShiftPixels; }
        [Serializable] private sealed class MotionReceipt { public string utc; public RouteReceipt[] routes; }
        [Serializable] private sealed class RouteReceipt { public string id; public bool moving; public int visibleCargo; public float travelled; }
    }
}

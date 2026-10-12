using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Authored route points shared by the road mesh and its pooled delivery representation.</summary>
    public sealed class PresentationRoute : MonoBehaviour
    {
        public string routeId;
        public Vector3[] points;
        public bool conveyor;
        public Renderer belt;
        public Transform[] cargoMarkers;
        public Vector3[] conveyorPath;
        public float cargoSpeed = 3.6f;
        public float spawnDelay;
        private float[] cargoDistances;
        private SpriteRenderer[] cargoSprites;
        private float pathLength, spawnCooldown;
        private bool pendingCargo, operating, outputAvailable;
        private float transferHold;
        public bool IsMoving => conveyor && operating && (outputAvailable || transferHold > 0);
        public int VisibleCargoCount { get; private set; }
        public float AnimationDistance { get; private set; }
        public Vector3 Position(float progress)
        {
            if(points==null||points.Length<2)throw new System.InvalidOperationException("Route needs authored points: "+routeId);
            float total=0;for(int i=1;i<points.Length;i++)total+=Vector3.Distance(points[i-1],points[i]);
            float distance=Mathf.Clamp01(progress)*total;
            for(int i=1;i<points.Length;i++)
            {
                float length=Vector3.Distance(points[i-1],points[i]);
                if(distance<=length)return Vector3.Lerp(points[i-1],points[i],length>0?distance/length:0);
                distance-=length;
            }
            return points[points.Length-1];
        }
        public void SetOperatingState(bool running, bool hasOutputSpace)
        { operating = running; outputAvailable = hasOutputSpace; }
        public void NotifyCargoTransfer()
        {
            if (!conveyor) return;
            pendingCargo = true;
            // A completed transfer may fill the target. Keep that tick visible until the next simulation tick.
            transferHold = 1.1f;
        }
        public float MovingSeconds(float delta, bool paused)
        {
            if (paused || !IsMoving) return 0;
            return outputAvailable ? Mathf.Max(0, delta) : Mathf.Min(Mathf.Max(0, delta), transferHold);
        }
        public void ResetConveyorVisuals()
        {
            pendingCargo = false; transferHold = 0; VisibleCargoCount = 0; AnimationDistance = 0; spawnCooldown = spawnDelay;
            if (cargoMarkers == null) return;
            for (int i = 0; i < cargoMarkers.Length; i++)
            { if (cargoDistances != null) cargoDistances[i] = -1; cargoMarkers[i].gameObject.SetActive(false); }
        }
        public void Animate(float delta, bool paused, Camera camera)
        {
            if (!conveyor || cargoMarkers == null || conveyorPath == null || conveyorPath.Length < 2) return;
            if (cargoDistances == null)
            {
                cargoDistances = new float[cargoMarkers.Length]; cargoSprites = new SpriteRenderer[cargoMarkers.Length];
                for (int i = 0; i < cargoMarkers.Length; i++)
                { cargoDistances[i] = -1; cargoSprites[i] = cargoMarkers[i].GetComponentInChildren<SpriteRenderer>(true); cargoMarkers[i].gameObject.SetActive(false); }
                for (int i = 1; i < conveyorPath.Length; i++) pathLength += Vector3.Distance(conveyorPath[i - 1], conveyorPath[i]);
                spawnCooldown = spawnDelay;
            }
            float movingSeconds = MovingSeconds(delta, paused);
            float step = movingSeconds * cargoSpeed;
            AnimationDistance += step;
            spawnCooldown -= movingSeconds;
            // A bounded visual pool represents real transfer events; it never changes inventory or transport rules.
            if (!paused && IsMoving && pendingCargo && spawnCooldown <= 0)
                for (int i = 0; i < cargoDistances.Length; i++)
                    if (cargoDistances[i] < 0)
                    { cargoDistances[i] = 0; pendingCargo = false; spawnCooldown = Mathf.Max(3, pathLength / (Mathf.Max(1, cargoDistances.Length) * cargoSpeed)); break; }
            VisibleCargoCount = 0;
            for (int i = 0; i < cargoMarkers.Length; i++)
            {
                if (cargoDistances[i] >= 0) { cargoDistances[i] += step; if (cargoDistances[i] >= pathLength) cargoDistances[i] = -1; }
                bool visible = cargoDistances[i] >= 0;
                var marker = cargoMarkers[i]; if (marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
                if (!visible) continue;
                VisibleCargoCount++;
                marker.position = ConveyorPosition(cargoDistances[i]) + Vector3.up * (.85f + .025f * Mathf.Sin(cargoDistances[i] * 5));
                var sprite = cargoSprites[i];
                if (sprite == null) continue;
                if (camera != null) sprite.transform.rotation = camera.transform.rotation;
                sprite.sortingOrder = WorldSort.At(marker.position) + 4;
                float fade = Mathf.SmoothStep(0, 1, Mathf.Min(cargoDistances[i], pathLength - cargoDistances[i]) / 1.5f);
                sprite.color = new Color(1, 1, 1, fade);
            }
            if (!paused) transferHold = Mathf.Max(0, transferHold - Mathf.Max(0, delta));
        }
        private Vector3 ConveyorPosition(float distance)
        {
            for (int i = 1; i < conveyorPath.Length; i++)
            {
                float length = Vector3.Distance(conveyorPath[i - 1], conveyorPath[i]);
                if (distance <= length) return Vector3.Lerp(conveyorPath[i - 1], conveyorPath[i], length > 0 ? distance / length : 0);
                distance -= length;
            }
            return conveyorPath[conveyorPath.Length - 1];
        }
    }
}

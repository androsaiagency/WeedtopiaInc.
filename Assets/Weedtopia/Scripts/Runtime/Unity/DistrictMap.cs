using System;
using System.Linq;
using UnityEngine;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    [Serializable] public sealed class DistrictRange
    {
        public Zone zone;
        public string label;
        public string labelKey;
        public Vector2 start, end, direction;
        public float zoom = 44;
        public Vector2 Clamp(Vector2 point)
        {
            Vector2 delta = end - start;
            float t = delta.sqrMagnitude < .01f ? 0 : Mathf.Clamp01(Vector2.Dot(point - start, delta) / delta.sqrMagnitude);
            return start + delta * t;
        }
        public float Fraction(Vector2 point) => (end - start).sqrMagnitude < .01f ? 0 : Mathf.Clamp01(Vector2.Dot(point - start, end - start) / (end - start).sqrMagnitude);
    }
    [CreateAssetMenu(menuName = "Weedtopia/Presentation/District Map")]
    public sealed class DistrictMap : ScriptableObject
    {
        public DistrictRange[] districts;
        public Vector2 overview = new Vector2(174,65);
        public float overviewZoom = 205;
        [Tooltip("Aspect ratio of the authored overview. Its visible ground rectangle is the exploration boundary.")]
        public float overviewAspect = 16f / 9f;
        public const float Diagonal = .70710678118f;
        public static Vector3 ToWorld(Vector2 plan, float height = 0) => new Vector3((plan.x + plan.y) * Diagonal, height, (plan.y - plan.x) * Diagonal);
        public static Vector2 ToPlan(Vector3 world) => new Vector2((world.x - world.z) * Diagonal, (world.x + world.z) * Diagonal);
        public DistrictRange Range(Zone zone) => districts.Single(x => x.zone == zone);
        public Rect OverviewBounds(float groundProjection)
        {
            var halfSize=new Vector2(overviewZoom*overviewAspect,overviewZoom/Mathf.Max(.01f,groundProjection));
            return new Rect(overview-halfSize,halfSize*2);
        }
        public float MaximumNavigationZoom(float aspect) => overviewZoom*Mathf.Min(1,overviewAspect/Mathf.Max(.01f,aspect));
        public Vector2 ClampNavigation(Vector2 point,float viewZoom,float aspect,float groundProjection)
        {
            var bounds=OverviewBounds(groundProjection);
            var halfSize=new Vector2(viewZoom*aspect,viewZoom/Mathf.Max(.01f,groundProjection));
            // An oversized view is centred; the camera also reduces zoom to fit before rendering.
            var freedom=Vector2.Max(Vector2.zero,bounds.size*.5f-halfSize);
            return new Vector2(Mathf.Clamp(point.x,overview.x-freedom.x,overview.x+freedom.x),
                Mathf.Clamp(point.y,overview.y-freedom.y,overview.y+freedom.y));
        }
    }
}

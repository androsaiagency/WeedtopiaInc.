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
        public const float Diagonal = .70710678118f;
        public static Vector3 ToWorld(Vector2 plan, float height = 0) => new Vector3((plan.x + plan.y) * Diagonal, height, (plan.y - plan.x) * Diagonal);
        public static Vector2 ToPlan(Vector3 world) => new Vector2((world.x - world.z) * Diagonal, (world.x + world.z) * Diagonal);
        public DistrictRange Range(Zone zone) => districts.Single(x => x.zone == zone);
    }
}

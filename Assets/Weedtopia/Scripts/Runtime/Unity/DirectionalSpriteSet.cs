using System;
using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Painted views in ground-plane compass order: E, NE, N, NW, W, SW, S, SE.</summary>
    [CreateAssetMenu(menuName = "Weedtopia/Presentation/Directional Animation")]
    public sealed class DirectionalSpriteSet : ScriptableObject
    {
        [Serializable] public sealed class View
        {
            public Sprite[] frames;
            public bool mirror;
            public float scale = 1;
        }

        public View[] views = new View[8];
        [Min(.01f)] public float distancePerCycle = 3;
        public int restingFrame = 2;
        public bool aerial;

        public static Vector2 GroundHeading(Vector3 direction, Camera camera)
        {
            // Screen Y is foreshortened by the camera pitch. Resolve on the ground before quantizing.
            Vector3 right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
            Vector3 north = Vector3.ProjectOnPlane(camera.transform.up, Vector3.up).normalized;
            return new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, north));
        }

        public static int Sector(Vector2 groundDirection, int previous = 0)
        {
            if (groundDirection.sqrMagnitude < .000001f) return previous;
            float angle = Mathf.Atan2(groundDirection.y, groundDirection.x) * Mathf.Rad2Deg;
            return Mathf.FloorToInt(Mathf.Repeat(angle + 22.5f, 360) / 45) % 8;
        }
    }
}

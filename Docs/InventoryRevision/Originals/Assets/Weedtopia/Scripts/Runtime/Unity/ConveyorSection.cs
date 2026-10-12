using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>One authored physical span, even when several logical delivery routes share it.</summary>
    public sealed class ConveyorSection : MonoBehaviour
    {
        public Renderer surface;
        public PresentationRoute[] owners;
        public bool junction;
        public float speed = 3.6f;
        public Vector2 spanUv = Vector2.right;
        private MaterialPropertyBlock properties;
        public float Travel { get; private set; }
        public bool Moving { get; private set; }
        private static readonly int TravelId = Shader.PropertyToID("_Travel"), TintId = Shader.PropertyToID("_Tint"), SpanId = Shader.PropertyToID("_SpanUv");

        public void Present(float delta, bool paused)
        {
            Moving = false;
            float movingSeconds = 0;
            foreach (var route in owners)
            { Moving |= route.IsMoving; movingSeconds = Mathf.Max(movingSeconds, route.MovingSeconds(delta, paused)); }
            // Wrap at a whole number of texture repeats or turntable revolutions, with no visible phase jump.
            Travel = Mathf.Repeat(Travel + movingSeconds * speed, junction ? 8 * Mathf.PI : 18);
            if (properties == null) properties = new MaterialPropertyBlock();
            properties.SetFloat(TravelId, Travel);
            properties.SetVector(SpanId, new Vector4(spanUv.x, spanUv.y, 0, 0));
            properties.SetColor(TintId, Moving ? Color.white : new Color(.82f, .86f, .85f));
            surface.SetPropertyBlock(properties);
        }
    }
}

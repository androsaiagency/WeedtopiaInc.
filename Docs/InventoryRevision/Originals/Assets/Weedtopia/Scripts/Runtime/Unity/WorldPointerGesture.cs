using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>A pointer belongs to its press surface until release. A drag can never become a tap.</summary>
    public sealed class WorldPointerGesture
    {
        private Vector2 start, previous;
        private bool accepted, dragging;
        public bool Active { get; private set; }
        public bool Dragging => Active && dragging;
        public void Begin(Vector2 point, bool startsOverUi)
        { start = previous = point; accepted = !startsOverUi; dragging = false; Active = true; }
        public Vector2 Move(Vector2 point, float threshold)
        {
            if (!Active || !accepted) return Vector2.zero;
            Vector2 delta = point - previous; previous = point;
            if (!dragging && Vector2.Distance(start, point) >= threshold) { dragging = true; return point - start; }
            return dragging ? delta : Vector2.zero;
        }
        public bool End(Vector2 point, float threshold)
        {
            bool tap = Active && accepted && !dragging && Vector2.Distance(start, point) < threshold;
            Cancel(); return tap;
        }
        public void Cancel() { Active = accepted = dragging = false; }
    }
}

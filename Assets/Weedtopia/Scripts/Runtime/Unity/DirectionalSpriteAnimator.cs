using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Distance-driven painted animation; the owner supplies motion, never the simulation.</summary>
    public sealed class DirectionalSpriteAnimator : MonoBehaviour
    {
        public SpriteRenderer artwork;
        public DirectionalSpriteSet animationSet;
        public Transform groundContact;
        public int CurrentDirection { get; private set; }
        public int CurrentFrame { get; private set; }

        public void Present(Vector3 heading, float travelledDistance, bool moving, Camera camera)
        {
            if (animationSet == null || artwork == null) return;
            CurrentDirection = DirectionalSpriteSet.Sector(DirectionalSpriteSet.GroundHeading(heading, camera), CurrentDirection);
            var view = animationSet.views[animationSet.aerial ? 0 : CurrentDirection];
            CurrentFrame = moving
                ? Mathf.FloorToInt(Mathf.Repeat(travelledDistance / animationSet.distancePerCycle, 1) * view.frames.Length)
                : Mathf.Clamp(animationSet.restingFrame, 0, view.frames.Length - 1);
            artwork.sprite = view.frames[CurrentFrame];
            // Reflect geometry: the authored foliage shader does not consume SpriteRenderer flip properties.
            // This also keeps the standard vehicle sprite material on the same facing convention.
            artwork.flipX = false;
            artwork.transform.localScale = new Vector3(view.mirror ? -view.scale : view.scale, view.scale, view.scale);
            artwork.transform.rotation = camera.transform.rotation;
            if (groundContact != null && heading.sqrMagnitude > .000001f)
                groundContact.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(heading, Vector3.up));
            if (animationSet.aerial && heading.sqrMagnitude > .000001f)
            {
                Vector2 projected = new Vector2(Vector3.Dot(heading, camera.transform.right), Vector3.Dot(heading, camera.transform.up));
                artwork.transform.rotation *= Quaternion.Euler(0, 0, Mathf.Atan2(projected.y, projected.x) * Mathf.Rad2Deg - 90);
            }
        }
    }
}

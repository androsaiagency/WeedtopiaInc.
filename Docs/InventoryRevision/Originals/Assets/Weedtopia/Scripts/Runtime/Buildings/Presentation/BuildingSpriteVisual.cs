using UnityEngine;

namespace Weedtopia.Buildings.Presentation
{
    /// <summary>
    /// Authored 2D/2.5D building presentation. The sprite and pivot are serialized in the prefab;
    /// this component only keeps the baked isometric view aligned with the active render camera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildingSpriteVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool faceRenderCamera = true;
        [SerializeField] private Camera cameraOverride;
        [SerializeField] private Vector3 rotationOffset;
        [SerializeField, Min(0.01f)] private float authoredWorldHeight = 1f;
        [SerializeField] private string resourcePath;
        [SerializeField, Min(0)] private float footprintInset;
        private bool acquired;
        private Transform groundPivot;

        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public bool FaceRenderCamera => faceRenderCamera;
        public float AuthoredWorldHeight => authoredWorldHeight;
        public string ResourcePath => resourcePath;
        public float FootprintInset => footprintInset;

        private void OnEnable()
        {
            var wrapper = GetComponentInParent<BuildingVisualWrapper>();
            groundPivot = wrapper != null ? wrapper.transform : transform;
            if (!UnityEngine.Application.isPlaying || string.IsNullOrEmpty(resourcePath)) return;
            spriteRenderer.sprite = BuildingSpriteCache.Acquire(resourcePath);
            acquired = true;
        }
        private void OnDisable()
        {
            if (!acquired) return;
            spriteRenderer.sprite = null;
            BuildingSpriteCache.Release(resourcePath);
            acquired = false;
        }

        private void LateUpdate()
        {
            if (!faceRenderCamera) return;
            Camera renderCamera = cameraOverride != null ? cameraOverride : Camera.main;
            if (renderCamera == null) return;
            transform.rotation = renderCamera.transform.rotation * Quaternion.Euler(rotationOffset);
            if(groundPivot!=null)spriteRenderer.sortingOrder = 1000 - Mathf.RoundToInt((groundPivot.position.x + groundPivot.position.z) * 8);
        }

#if UNITY_EDITOR
        public void EditorConfigure(SpriteRenderer renderer, bool faceCamera, float worldHeight)
        {
            spriteRenderer = renderer;
            faceRenderCamera = faceCamera;
            authoredWorldHeight = Mathf.Max(0.01f, worldHeight);
        }
        public void EditorSetResource(string path) { resourcePath = path; }
        // Store the authored adjustment so rerunning alignment preserves additional manual offsets.
        public void EditorSetFootprintInset(float inset)
        {
            inset = Mathf.Max(0, inset);
            transform.localPosition -= Quaternion.Euler(35.264f, 45, 0) * Vector3.up * (inset - footprintInset);
            footprintInset = inset;
        }
#endif
    }
}

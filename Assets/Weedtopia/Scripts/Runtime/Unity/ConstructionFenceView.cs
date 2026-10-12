using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>One reusable, authored enclosure. Visibility follows the existing construction timer.</summary>
    public sealed class ConstructionFenceView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] sides;
        [SerializeField] private SpriteRenderer[] posts;
        [SerializeField, Min(.1f)] private float fenceHeight = 3.2f;
        [SerializeField, Min(0)] private float clearance = 4;
        private float fittedWidth = -1;
        public SpriteRenderer[] Sides => sides;
        public SpriteRenderer[] Posts => posts;
        public float EnclosureWidth { get; private set; }
        private static readonly Vector3[] Corners = {
            new Vector3(-1,0,-1), new Vector3(-1,0,1), new Vector3(1,0,1), new Vector3(1,0,-1)
        };

        public void Present(bool constructing, float footprintWidth, Camera camera)
        {
            if (gameObject.activeSelf != constructing) gameObject.SetActive(constructing);
            if (!constructing) return;
            float width = Mathf.Max(10, footprintWidth + clearance);
            if (!Mathf.Approximately(width, fittedWidth))
            {
                fittedWidth = width; EnclosureWidth = width;
                float length = width / Mathf.Sqrt(2), half = length * .5f;
                sides[0].transform.localPosition = new Vector3(0,.04f,-half);
                sides[1].transform.localPosition = new Vector3(-half,.04f,0);
                sides[2].transform.localPosition = new Vector3(0,.04f,half);
                sides[3].transform.localPosition = new Vector3(half,.04f,0);
                foreach (var side in sides) side.size = new Vector2(length, fenceHeight);
                for (int i = 0; i < posts.Length; i++) posts[i].transform.localPosition = Corners[i] * half;
            }
            int depth = WorldSort.At(transform.position);
            for (int i = 0; i < sides.Length; i++) sides[i].sortingOrder = depth + (i < 2 ? 1 : -1);
            for (int i = 0; i < posts.Length; i++)
            {
                posts[i].sortingOrder = depth + (i == 2 ? -2 : 2);
                if (camera != null) posts[i].transform.rotation = camera.transform.rotation;
            }
        }
#if UNITY_EDITOR
        public void EditorConfigure(SpriteRenderer[] panels, SpriteRenderer[] corners)
        { sides = panels; posts = corners; }
#endif
    }
}

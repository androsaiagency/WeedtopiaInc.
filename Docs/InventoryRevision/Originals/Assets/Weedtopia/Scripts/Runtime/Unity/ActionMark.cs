using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Crisp vector UI marks. Geometry remains sharp at every canvas scale.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ActionMark : MaskableGraphic
    {
        public enum Shape { Plus, Cross, Arrow }
        [SerializeField] private Shape shape;
        public Shape Mark { get => shape; set { shape = value; SetVerticesDirty(); } }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); float s = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .31f;
            void Line(Vector2 a, Vector2 b)
            {
                Vector2 n = new Vector2(-(b-a).y, (b-a).x).normalized * s * .18f;
                int i = vh.currentVertCount;
                vh.AddVert(a-n, color, Vector2.zero); vh.AddVert(a+n, color, Vector2.zero);
                vh.AddVert(b+n, color, Vector2.zero); vh.AddVert(b-n, color, Vector2.zero);
                vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
            }
            if (shape == Shape.Arrow) { Line(new Vector2(-s,0),new Vector2(s,0)); Line(new Vector2(0,s),new Vector2(s,0)); Line(new Vector2(0,-s),new Vector2(s,0)); }
            else if (shape == Shape.Cross) { Line(new Vector2(-s,-s),new Vector2(s,s)); Line(new Vector2(-s,s),new Vector2(s,-s)); }
            else { Line(new Vector2(-s,0),new Vector2(s,0)); Line(new Vector2(0,-s),new Vector2(0,s)); }
        }
    }
}

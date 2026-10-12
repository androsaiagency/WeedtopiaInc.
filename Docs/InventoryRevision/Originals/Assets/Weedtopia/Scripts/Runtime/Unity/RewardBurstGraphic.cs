using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Light rays rendered by one authored UI graphic; no particle GameObject spawning.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RewardBurstGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2 / 16;
                int offset = mesh.currentVertCount;
                mesh.AddVert(Vector3.zero, color, Vector2.zero);
                var edge = color; edge.a = 0;
                mesh.AddVert(new Vector3(Mathf.Cos(a-.075f), Mathf.Sin(a-.075f)) * radius, edge, Vector2.zero);
                mesh.AddVert(new Vector3(Mathf.Cos(a+.075f), Mathf.Sin(a+.075f)) * radius, edge, Vector2.zero);
                mesh.AddTriangle(offset, offset+1, offset+2);
            }
        }
    }
}

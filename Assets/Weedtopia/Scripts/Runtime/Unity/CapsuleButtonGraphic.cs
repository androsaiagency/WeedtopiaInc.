using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Vector capsule; rounded ends always follow the actual button height.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CapsuleButtonGraphic : MaskableGraphic
    {
        private const int Segments=96;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=GetPixelAdjustedRect();float h=Mathf.Min(rect.height,rect.width),rim=Mathf.Max(1,h*.025f);
            float[] insets={0,rim,rim*2,rim*3,h*.5f};
            Color[] colors={new Color(.025f,.13f,.13f),new Color(.44f,.68f,.40f),new Color(.93f,.92f,.69f),new Color(.24f,.55f,.33f),new Color(.24f,.55f,.33f)};
            for(int ring=0;ring<insets.Length;ring++)
            {
                float radius=Mathf.Max(0,h*.5f-insets[ring]),straight=Mathf.Max(0,(rect.width-h)*.5f);
                for(int i=0;i<Segments;i++)
                {
                    float angle=i*Mathf.PI*2/Segments,x=Mathf.Cos(angle),y=Mathf.Sin(angle);
                    var tint=colors[ring]*color;if(ring>=3)tint=Color.Lerp(tint,new Color(.55f,.75f,.35f)*color,(y+1)*.30f);
                    vh.AddVert(rect.center+new Vector2(x*radius+(x<0?-straight:straight),y*radius),tint,Vector2.zero);
                }
                if(ring==0)continue;
                for(int i=0;i<Segments;i++){int n=(i+1)%Segments,a=(ring-1)*Segments+i,b=(ring-1)*Segments+n,c=ring*Segments+i,d=ring*Segments+n;vh.AddTriangle(a,b,c);vh.AddTriangle(b,d,c);}
            }
        }
    }
}

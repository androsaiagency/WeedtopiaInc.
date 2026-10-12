using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Resolution-independent circular button skin and matching circular hit area.</summary>
    [AddComponentMenu("Weedtopia/UI/Round Navigation Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundNavigationGraphic : MaskableGraphic
    {
        [SerializeField] private Color rim=new Color(.89f,.65f,.29f), face=new Color(.045f,.24f,.24f);
        private const int Segments=80;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=GetPixelAdjustedRect();float radius=Mathf.Min(rect.width,rect.height)*.5f;
            float[] rings={1,.97f,.91f,.87f,0};
            Color[] colors={new Color(.12f,.095f,.055f),rim,new Color(1,.86f,.48f),face,face};
            for(int ring=0;ring<rings.Length;ring++)
            {
                for(int i=0;i<Segments;i++)
                {
                    float angle=i*Mathf.PI*2/Segments;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    var tint=colors[ring]*color;
                    if(ring>=3)tint=Color.Lerp(tint,new Color(.13f,.39f,.36f)*color,(direction.y*rings[ring]+1)*.3f);
                    vh.AddVert(rect.center+direction*(radius*rings[ring]),tint,Vector2.zero);
                }
                if(ring==0)continue;
                for(int i=0;i<Segments;i++)
                {
                    int next=(i+1)%Segments,a=(ring-1)*Segments+i,b=(ring-1)*Segments+next,c=ring*Segments+i,d=ring*Segments+next;
                    vh.AddTriangle(a,b,c);vh.AddTriangle(b,d,c);
                }
            }
        }
        public override bool Raycast(Vector2 screenPoint,Camera eventCamera)
        {
            if(!base.Raycast(screenPoint,eventCamera)||!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screenPoint,eventCamera,out var local))return false;
            var rect=rectTransform.rect;float radius=Mathf.Min(rect.width,rect.height)*.5f;
            return (local-rect.center).sqrMagnitude<=radius*radius;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Authored vector wheel with gold rims, botanical teal sectors and warm marquee lights.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LuckyWheelGraphic : MaskableGraphic
    {
        [SerializeField] private int sectors = 10;
        public bool frameOnly;
        public Color[] palette;
        public void SetSectors(Color[] colors) { sectors = colors.Length; palette = colors; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); float r = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
            var gold = new Color(1,.76f,.31f); var shadow = new Color(.015f,.048f,.055f);
            if(frameOnly)
            {
                Ring(vh,r*.94f,r,shadow); Ring(vh,r*.94f,r*.98f,gold);
                Ring(vh,r*.875f,r*.945f,new Color(.1f,.28f,.26f));
                Ring(vh,r*.87f,r*.885f,new Color(.98f,.88f,.56f));
                for(int i=0;i<40;i++)
                {
                    float a=i*Mathf.PI*2/40; var p=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r*.914f;
                    Circle(vh,p,r*.016f,new Color(1,.72f,.18f,.24f)); Circle(vh,p,r*.009f,new Color(1,.95f,.7f));
                }
                return;
            }
            Circle(vh,Vector2.zero,r,shadow);
            if(sectors>0)
            {
                for(int i=0;i<sectors;i++)
                {
                    float middle=90-i*360f/sectors, half=180f/sectors;
                    var c=palette!=null&&i<palette.Length?palette[i]:new Color(.08f,.28f,.28f);
                    Wedge(vh,r,middle-half+.7f,middle+half-.7f,c);
                    Wedge(vh,r*.96f,middle-half+1,middle+half-1,Color.Lerp(c,Color.black,.13f));
                    Arc(vh,r*.96f,r*.99f,middle-half+1,middle+half-1,gold);
                }
            }
            Circle(vh,Vector2.zero,r*.20f,shadow); Circle(vh,Vector2.zero,r*.18f,gold);
            Circle(vh,Vector2.zero,r*.158f,new Color(.08f,.30f,.28f));
        }
        private static void Circle(VertexHelper vh,Vector2 p,float r,Color c)
        {
            for(int i=0;i<64;i++)Triangle(vh,p,p+Point(r,i*360f/64),p+Point(r,(i+1)*360f/64),c);
        }
        private static Vector2 Point(float r,float degrees)=>new Vector2(Mathf.Cos(degrees*Mathf.Deg2Rad),Mathf.Sin(degrees*Mathf.Deg2Rad))*r;
        private static void Ring(VertexHelper vh,float inner,float outer,Color c)=>Arc(vh,inner,outer,0,360,c);
        private static void Wedge(VertexHelper vh,float r,float a,float b,Color c)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt((b-a)/3));
            for(int i=0;i<steps;i++)Triangle(vh,Vector2.zero,Point(r,Mathf.Lerp(a,b,(float)i/steps)),Point(r,Mathf.Lerp(a,b,(float)(i+1)/steps)),c);
        }
        private static void Arc(VertexHelper vh,float inner,float outer,float a,float b,Color c)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt((b-a)/3));
            for(int i=0;i<steps;i++)
            {
                float x=Mathf.Lerp(a,b,(float)i/steps),y=Mathf.Lerp(a,b,(float)(i+1)/steps);
                Triangle(vh,Point(inner,x),Point(outer,x),Point(outer,y),c);
                Triangle(vh,Point(inner,x),Point(outer,y),Point(inner,y),c);
            }
        }
        private static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color tint)
        {int i=vh.currentVertCount;vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);vh.AddVert(c,tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);}
    }
}

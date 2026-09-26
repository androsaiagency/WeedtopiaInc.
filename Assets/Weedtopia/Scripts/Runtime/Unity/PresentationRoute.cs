using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Authored route points shared by the road mesh and its pooled delivery representation.</summary>
    public sealed class PresentationRoute : MonoBehaviour
    {
        public string routeId;
        public Vector3[] points;
        public bool conveyor;
        public Renderer belt;
        public Transform[] cargoMarkers;
        private MaterialPropertyBlock block;
        private float phase;
        public Vector3 Position(float progress)
        {
            if(points==null||points.Length<2)throw new System.InvalidOperationException("Route needs authored points: "+routeId);
            float total=0;for(int i=1;i<points.Length;i++)total+=Vector3.Distance(points[i-1],points[i]);
            float distance=Mathf.Clamp01(progress)*total;
            for(int i=1;i<points.Length;i++)
            {
                float length=Vector3.Distance(points[i-1],points[i]);
                if(distance<=length)return Vector3.Lerp(points[i-1],points[i],length>0?distance/length:0);
                distance-=length;
            }
            return points[points.Length-1];
        }
        public void Present(bool moving,bool cargoAvailable,float delta)
        {
            if(!conveyor||belt==null)return;
            if(block==null)block=new MaterialPropertyBlock();
            if(moving)phase=(phase+delta*.7f)%1;
            block.SetVector("_MainTex_ST",new Vector4(1,1,0,phase));
            block.SetColor("_Tint",moving?Color.white:new Color(.65f,.65f,.65f));belt.SetPropertyBlock(block);
            if(cargoMarkers!=null)for(int i=0;i<cargoMarkers.Length;i++)
            {
                var marker=cargoMarkers[i];marker.gameObject.SetActive(cargoAvailable);
                marker.position=Position((phase+i/(float)cargoMarkers.Length)%1)+Vector3.up*.25f;
                var sprite=marker.GetComponentInChildren<SpriteRenderer>();if(sprite!=null)sprite.sortingOrder=WorldSort.At(marker.position)+2;
            }
        }
    }
}

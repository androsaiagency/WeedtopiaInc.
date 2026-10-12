using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Weedtopia.Unity
{
    /// <summary>Small state-dependent 2.5D motion on an authored character. Never changes simulation.</summary>
    public sealed class CharacterActor : MonoBehaviour
    {
        public string speakerId, workplaceId;
        public Transform artwork;
        public SpriteRenderer spriteRenderer;
        private GameRoot root;
        private Vector3 rest;
        private BoxCollider selectionBox;
        private HashSet<string> pickupRoutes;
        private Renderer[] renderers;
        private bool[] rendererEnabled;
        private bool wasAway,colliderEnabled;
        public bool AwayForPickup => root!=null&&root.Session!=null&&pickupRoutes.Count>0&&
            root.Session.State.deliveries.Any(t=>pickupRoutes.Contains(t.route)&&(t.fetching||t.remainingSeconds>0));
        public void Initialize(GameRoot value)
        {
            root=value;rest=artwork.localPosition;selectionBox=GetComponent<BoxCollider>();colliderEnabled=selectionBox!=null&&selectionBox.enabled;
            pickupRoutes=new HashSet<string>(root.Definitions.routes.Where(r=>r.pickupByRecipient&&r.to==workplaceId).Select(r=>r.id));
            renderers=GetComponentsInChildren<Renderer>(true);rendererEnabled=renderers.Select(r=>r.enabled).ToArray();
        }
        public bool TryPick(Ray ray,out float distance)
        {distance=float.PositiveInfinity;return !AwayForPickup&&AuthoredBoxPicking.TryHit(selectionBox,ray,out distance);}
        private void LateUpdate()
        {
            if(root==null||artwork==null)return;
            bool away=AwayForPickup;
            if(away!=wasAway)
            {
                for(int i=0;i<renderers.Length;i++)renderers[i].enabled=!away&&rendererEnabled[i];
                if(selectionBox!=null)selectionBox.enabled=!away&&colliderEnabled;
                wasAway=away;
            }
            bool working=root.Session!=null&&!root.Session.Paused&&!string.IsNullOrEmpty(workplaceId)&&root.Session.State.Find(workplaceId).level>0;
            float speed=working?3.2f:1.35f;
            artwork.localPosition=rest+Vector3.up*(Mathf.Sin(Time.time*speed+transform.position.x)* (working?.07f:.025f));
            artwork.rotation=root.CameraRig.RenderCamera.transform.rotation;
            spriteRenderer.sortingOrder=WorldSort.At(transform.position)+3;
        }
    }
    public static class WorldSort
    {
        public static int At(Vector3 position)=>1000-Mathf.RoundToInt((position.x+position.z)*8);
    }
}

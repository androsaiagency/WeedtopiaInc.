using System.Linq;
using UnityEngine;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    /// <summary>Fixed authored pool, one representation per active trip. Never one object per resource.</summary>
    public sealed class DeliveryView : MonoBehaviour
    {
        [SerializeField] private GameObject body;
        [SerializeField] private SpriteRenderer artwork;
        [SerializeField] private Sprite leftSprite,rightSprite;
        [SerializeField] private SpriteRenderer cargo;
        [SerializeField,Min(.1f)] private float pickupVanScale=1.65f;
        [SerializeField] private DirectionalSpriteAnimator directionalAnimation;
        private Vector3 bodyScale=Vector3.one;
        private DeliveryState currentDelivery;
        private GameRoot currentRoot;
        private RouteDefinition definition;
        private PresentationRoute authoredRoute;
        private Vector3 start,end;
        private float routeLength;
        private void Awake(){if(body!=null)bodyScale=body.transform.localScale;}
        public void Present(DeliveryState delivery,GameRoot root)
        {
            body.SetActive(delivery!=null);if(delivery==null)return;
            if(currentRoot!=root||definition==null||definition.id!=delivery.route)
            {
                definition=root.Definitions.routes.Single(x=>x.id==delivery.route);
                authoredRoute=root.Presentation?.Route(delivery.route);
                start=root.Site(definition.from).transform.position;end=root.Site(definition.to).transform.position;
                routeLength=Vector3.Distance(start,end);
                if(authoredRoute!=null)
                {
                    routeLength=0;
                    for(int i=1;i<authoredRoute.points.Length;i++)routeLength+=Vector3.Distance(authoredRoute.points[i-1],authoredRoute.points[i]);
                }
            }
            currentDelivery=delivery;currentRoot=root;RenderTrip();
        }
        private void LateUpdate()
        {
            // Rendering follows the fractional simulation clock every frame, independently of the 10 Hz HUD refresh.
            if(currentDelivery!=null&&currentRoot!=null&&body.activeSelf)RenderTrip();
        }
        private void RenderTrip()
        {
            var delivery=currentDelivery;var root=currentRoot;var route=definition;
            body.transform.localScale=bodyScale*(route.pickupByRecipient?pickupVanScale:1);
            Vector3 a=start,b=end;
            float progress=Mathf.Clamp01(1-((float)delivery.remainingSeconds-(float)root.Session.State.tickRemainder)/Mathf.Max(1,delivery.totalSeconds));
            if(delivery.fetching)progress=1-progress;
            var authored=authoredRoute;
            transform.position=(authored!=null?authored.Position(progress):Vector3.Lerp(a,b,progress))+Vector3.up*.06f;
            if(artwork!=null)
            {
                transform.rotation=Quaternion.identity;artwork.transform.rotation=root.CameraRig.RenderCamera.transform.rotation;
                Vector3 heading=authored!=null?authored.Position(Mathf.Min(1,progress+.015f))-authored.Position(Mathf.Max(0,progress-.015f)):b-a;
                if(delivery.fetching)heading=-heading;
                if(directionalAnimation!=null)
                    directionalAnimation.Present(heading,(delivery.fetching?1-progress:progress)*routeLength,delivery.remainingSeconds>0,root.CameraRig.RenderCamera);
                else
                {
                    bool right=Vector3.Dot(heading,root.CameraRig.RenderCamera.transform.right)>0;
                    artwork.sprite=right?rightSprite:leftSprite;
                }
                artwork.sortingOrder=WorldSort.At(transform.position)+4;
                if(cargo!=null)
                {
                    cargo.gameObject.SetActive(delivery.cargo>0);
                    if(root.Presentation!=null)cargo.sprite=root.Presentation.art.Get(route.resource==Resource.Butter?"butter":"cargo");
                    cargo.transform.rotation=artwork.transform.rotation;cargo.sortingOrder=artwork.sortingOrder+1;
                    if(directionalAnimation!=null)cargo.transform.position=artwork.transform.position+root.CameraRig.RenderCamera.transform.up*(artwork.sprite.bounds.max.y*artwork.transform.lossyScale.y+.65f);
                }
            }
            else if((b-a).sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(delivery.fetching?a-b:b-a);
        }
#if UNITY_EDITOR
        public void EditorConfigure(GameObject value){body=value;}
#endif
    }
}

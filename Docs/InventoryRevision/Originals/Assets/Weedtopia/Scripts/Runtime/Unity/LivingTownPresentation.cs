using System;
using UnityEngine;
using Weedtopia.Application;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    /// <summary>One presentation clock for authored residents and wildlife. Never modifies the simulation.</summary>
    public sealed class LivingTownPresentation : MonoBehaviour
    {
        [Serializable] public sealed class Actor
        {
            public Transform anchor;
            public SpriteRenderer artwork;
            public Sprite[] frames;
            public DirectionalSpriteAnimator animation;
            public Vector3[] path;
            public float speed=2, phase, restSeconds=3, frameRate=3, bob=.04f;
            public bool airborne, closedLoop;
            public Zone zone;
            public string requiredBuilding;
            [NonSerialized] public float[] distances;
            [NonSerialized] public float length;
            [NonSerialized] public Vector3 restPosition;
            [NonSerialized] public BuildingState building;
        }
        public GameRoot root;
        public Actor[] actors=Array.Empty<Actor>();
        public int authoredLandmarks;
        public int revision=1;
        [NonSerialized] public bool diagnosticFreeze;
        public float Elapsed {get;private set;}
        private GameSession boundSession;
        private void Awake()
        {
            foreach(var actor in actors)
            {
                actor.restPosition=actor.artwork.transform.localPosition;
                actor.distances=new float[actor.path.Length];
                for(int i=1;i<actor.path.Length;i++)actor.distances[i]=actor.distances[i-1]+Vector3.Distance(actor.path[i-1],actor.path[i]);
                actor.length=actor.distances[actor.distances.Length-1];
            }
        }
        private void LateUpdate()
        {
            if(diagnosticFreeze)return;
            bool running=root!=null&&root.Session!=null&&!root.Session.Paused;
            PresentAt(Elapsed+(running?Time.unscaledDeltaTime:0));
        }
        public void PresentAt(float seconds)
        {
            Elapsed=Mathf.Max(0,seconds);
            if(root==null)return;
            if(root.Session!=boundSession)
            {
                boundSession=root.Session;
                foreach(var a in actors)a.building=boundSession!=null&&!string.IsNullOrEmpty(a.requiredBuilding)?boundSession.State.Find(a.requiredBuilding):null;
            }
            foreach(var a in actors)
            {
                bool visible=boundSession!=null&&boundSession.State.City.unlocked>=a.zone&&(string.IsNullOrEmpty(a.requiredBuilding)||a.building!=null&&a.building.level>0);
                if(a.anchor.gameObject.activeSelf!=visible)a.anchor.gameObject.SetActive(visible);
                if(!visible||a.length<=.001f)continue;
                float travel=a.length/Mathf.Max(.01f,a.speed),t=Mathf.Repeat(Elapsed+a.phase,a.closedLoop?travel:2*(travel+a.restSeconds));
                bool returning=!a.closedLoop&&t>=travel+a.restSeconds;
                float local=returning?t-travel-a.restSeconds:t;
                bool moving=a.closedLoop||local<travel;
                float distance=Mathf.Min(local,travel)*a.speed;
                if(returning)distance=a.length-distance;
                int segment=1;while(segment<a.path.Length-1&&a.distances[segment]<distance)segment++;
                float fraction=Mathf.InverseLerp(a.distances[segment-1],a.distances[segment],distance);
                a.anchor.position=Vector3.Lerp(a.path[segment-1],a.path[segment],fraction);
                var heading=(a.path[segment]-a.path[segment-1])*(returning?-1:1);
                if(a.animation!=null)
                {
                    float cycle=2*(travel+a.restSeconds);
                    float travelled=a.closedLoop?(Elapsed+a.phase)*a.speed:
                        Mathf.Floor((Elapsed+a.phase)/cycle)*2*a.length+(returning?a.length:0)+Mathf.Min(local,travel)*a.speed;
                    a.animation.Present(heading,travelled,moving,root.CameraRig.RenderCamera);
                    a.artwork.transform.localPosition=a.restPosition;
                }
                else
                {
                    // Compatibility for archived scenes; production authoring binds directional sets to every actor.
                    a.artwork.flipX=Vector3.Dot(heading,root.CameraRig.RenderCamera.transform.right)<0;
                    int frame=moving?(int)((Elapsed+a.phase)*a.frameRate)%a.frames.Length:0;
                    a.artwork.sprite=a.frames[frame];
                    a.artwork.transform.localPosition=a.restPosition+Vector3.up*(moving?Mathf.Sin((Elapsed+a.phase)*a.frameRate*Mathf.PI*2)*a.bob:0);
                }
                a.artwork.sortingOrder=a.airborne?12000:WorldSort.At(a.anchor.position)+3;
            }
        }
    }
}

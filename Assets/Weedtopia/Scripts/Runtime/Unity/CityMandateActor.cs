using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>An authored, state-driven quest sign on Rocko's existing character.</summary>
    public sealed class CityMandateActor : MonoBehaviour
    {
        public GameRoot root;
        public GameObject visible;
        public SpriteRenderer artwork;
        private void LateUpdate()
        {
            bool offered=root!=null&&root.Session!=null&&root.Session.State.introSeen&&root.Session.Campaign.AnyOffered;
            visible.SetActive(offered);
            if(!offered)return;
            artwork.transform.rotation=root.CameraRig.RenderCamera.transform.rotation;
            artwork.sortingOrder=WorldSort.At(transform.parent.position)+7;
        }
    }
}

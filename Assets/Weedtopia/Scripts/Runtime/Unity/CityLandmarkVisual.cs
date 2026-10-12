using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Billboards a serialized landmark and sorts it by its own ground anchor.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CityLandmarkVisual : MonoBehaviour
    {
        private SpriteRenderer artwork;
        private void Awake(){artwork=GetComponent<SpriteRenderer>();}
        private void LateUpdate()
        {
            var camera=Camera.main;if(camera!=null)transform.rotation=camera.transform.rotation;
            artwork.sortingOrder=WorldSort.At(transform.parent.position);
        }
    }
}

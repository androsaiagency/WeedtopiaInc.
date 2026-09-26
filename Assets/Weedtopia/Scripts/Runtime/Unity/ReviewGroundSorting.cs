using UnityEngine;
namespace Weedtopia.Unity
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ReviewGroundSorting : MonoBehaviour
    {
        private SpriteRenderer sprite;
        private void Awake(){sprite=GetComponent<SpriteRenderer>();}
        private void LateUpdate(){var camera=Camera.main;if(camera!=null)sprite.sortingOrder=-Mathf.RoundToInt(Vector3.Dot(transform.root.position,camera.transform.forward)*100);}
    }
}

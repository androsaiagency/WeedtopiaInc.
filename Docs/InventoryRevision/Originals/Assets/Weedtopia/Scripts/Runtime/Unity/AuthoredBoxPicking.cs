using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Exact selection against the authored box, independent of the physics simulation/broadphase.</summary>
    public static class AuthoredBoxPicking
    {
        public static bool TryHit(BoxCollider box,Ray worldRay,out float distance)
        {
            distance=float.PositiveInfinity;
            if(box==null||!box.enabled||!box.gameObject.activeInHierarchy)return false;
            var tr=box.transform;
            var localRay=new Ray(tr.InverseTransformPoint(worldRay.origin),tr.InverseTransformVector(worldRay.direction));
            if(!new Bounds(box.center,box.size).IntersectRay(localRay,out float localDistance))return false;
            var point=tr.TransformPoint(localRay.GetPoint(localDistance));
            distance=Vector3.Dot(point-worldRay.origin,worldRay.direction);
            return distance>=0;
        }
    }
}

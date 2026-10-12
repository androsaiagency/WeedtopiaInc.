using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Authored fourth-city content receipt. Scene presentation is serialized in prefabs.</summary>
    public sealed class LittleMontanaStamp : MonoBehaviour
    {
        public int buildingVariants = 251, landmarks = 6, inhabitants = 7;
        public string palette = "warm cedar, cream plaster, slate-blue roofs, deep pine foliage";
        public string[] places = { "OldStation", "NeighbourPlatform", "OpenMillYard", "LearningCourtyard", "EagleTrail", "PineTrailhead" };
    }
}

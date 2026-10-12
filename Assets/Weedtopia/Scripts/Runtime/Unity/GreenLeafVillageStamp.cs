using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Authored seventh-city receipt. All permanent presentation objects are serialized in scene or prefabs.</summary>
    public sealed class GreenLeafVillageStamp : MonoBehaviour
    {
        public int buildingVariants = 251, landmarks = 3, inhabitants = 8, architecturePrefabs = 4;
        public string palette = "leaf green, cream plaster, reclaimed glass, pale timber";
        public string[] places = { "SeedLibrary", "CooperativeCourtyard", "GreenLeafDayCommons" };
    }
}

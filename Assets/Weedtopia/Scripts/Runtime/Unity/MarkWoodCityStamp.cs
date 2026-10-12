using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Authored sixth-city receipt. All permanent presentation objects are serialized in scene or prefabs.</summary>
    public sealed class MarkWoodCityStamp : MonoBehaviour
    {
        public int buildingVariants = 251, landmarks = 3, inhabitants = 8;
        public string palette = "warm reclaimed cedar, forest green, river slate, copper roofs";
        public string[] places = { "RiverMarketCommons", "OpenSawYards", "ForestRiverMeeting" };
    }
}
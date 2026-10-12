using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Authored eighth-city presentation. Permanent objects live in scenes and reusable prefabs.</summary>
    public sealed class PurpleGardenStamp : MonoBehaviour
    {
        public int buildingVariants = 251, landmarks = 3, inhabitants = 8, architecturePrefabs = 4;
        public string palette = "lavender glass, cream stone, muted plum, brass and sage foliage";
        public string[] places = { "WaterCourtCommons", "OpenSeedArchive", "NightBloomCommons" };
    }
}

using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Authored third-city content receipt. No scene objects are constructed at runtime.</summary>
    public sealed class SevenHillsStamp : MonoBehaviour
    {
        public int buildingVariants = 251, landmarks = 7, inhabitants = 7;
        public string palette = "light plaster, forest green, teal roofs, violet flowers";
        public string[] hills = { "Quellhügel", "Morgenhügel", "Treppenhügel", "Markthügel", "Aussichtshügel", "Wolkenhügel", "Verbindungshügel" };
    }
}

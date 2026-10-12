using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Authored content inventory for the explicit town decoration installer.</summary>
    public sealed class TownVitalityStamp : MonoBehaviour
    {
        public int revision=1;
        public int uniqueAssets,decorations,residents;
        public GameObject[] preservedStreetTrees = System.Array.Empty<GameObject>();
    }
}

using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Serialized ownership and inspection data for the explicitly authored landscape.</summary>
    public sealed class NaturalEnvironmentStamp : MonoBehaviour
    {
        public int revision=1;
        public int authoredPlants,authoredGardenProps;
        public string sourceManifest="Assets/Weedtopia/Production/Environment/art_manifest.json";
    }
}

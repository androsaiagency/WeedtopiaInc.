using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Serialized evidence of a separately authored city; contains no runtime scene construction.</summary>
    public sealed class BigRockCityStamp : MonoBehaviour
    {
        public int buildingVariants, landmarks, inhabitants;
        public string palette = "warm sandstone, copper, petrol, dry meadow";
    }
}

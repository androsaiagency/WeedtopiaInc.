using System;
using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Inventory and connected paths of the editor-authored countryside destinations.</summary>
    public sealed class WorldCornersStamp : MonoBehaviour
    {
        [Serializable] public sealed class Walk { public string name; public Vector2[] points; }
        public int revision = 1;
        public int uniqueAssets, decorations, residents, wildlife;
        public Walk[] walks = Array.Empty<Walk>();
        public Vector2[] destinationEntrances = Array.Empty<Vector2>();
    }
}

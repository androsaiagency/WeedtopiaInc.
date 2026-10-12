using System;
using System.Linq;
using UnityEngine;

namespace Weedtopia.Unity
{
    [Serializable] public sealed class VipVisualEntry { public string id; public Sprite body, portrait; }
    [CreateAssetMenu(menuName = "Weedtopia/Presentation/VIP Visuals")]
    public sealed class VipVisualCatalog : ScriptableObject
    {
        public VipVisualEntry[] entries = Array.Empty<VipVisualEntry>();
        public VipVisualEntry Get(string id) => entries.Single(e => e.id == id);
    }
}

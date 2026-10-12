using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Shared authored product-card prefab; the catalog changes its data, not its visual structure.</summary>
    public sealed class ShopProductCard : MonoBehaviour
    {
        public string productId;
        public Image icon;
        public Text ribbon, title, contents, price;
        public Button purchase;
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Allows for font raster rounding when a story panel changes canvas scale.</summary>
    [RequireComponent(typeof(Text))]
    public sealed class VipStoryParagraph : MonoBehaviour, ILayoutElement
    {
        public Text label;
        public float verticalPadding = 14;
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public float minWidth => -1;
        public float preferredWidth => -1;
        public float flexibleWidth => -1;
        public float minHeight => -1;
        public float preferredHeight => label == null ? -1 : label.preferredHeight + verticalPadding;
        public float flexibleHeight => -1;
        public int layoutPriority => 1;
    }
}

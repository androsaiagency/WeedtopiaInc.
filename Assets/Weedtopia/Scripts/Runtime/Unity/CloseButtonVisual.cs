using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>All window close actions share the authored gold X and the same edge inset.</summary>
    public sealed class CloseButtonVisual : MonoBehaviour
    {
        public Image icon;
        public Text caption;
        public float inset = 2;
        public void Fit()
        {
            if (caption != null) caption.enabled = false;
            icon.enabled = true;
            icon.rectTransform.anchorMin = Vector2.zero;
            icon.rectTransform.anchorMax = Vector2.one;
            icon.rectTransform.offsetMin = Vector2.one * inset;
            icon.rectTransform.offsetMax = Vector2.one * -inset;
            icon.preserveAspect = true;
        }
        private void OnEnable() => Fit();
    }
}

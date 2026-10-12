using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Fits authored header groups inside the safe area without moving the centered resources.</summary>
    public sealed class CityStatusBar : MonoBehaviour
    {
        [SerializeField] private RectTransform city, resources, settings;
        [SerializeField] private Text cityName;
        private GameRoot root;

        public RectTransform City => city;
        public RectTransform Resources => resources;
        public RectTransform Settings => settings;
        public Text CityName => cityName;

        public void Initialize(GameRoot game) { root = game; Refresh(); }
        private void LateUpdate() { if (root != null) Refresh(); }
        public void Refresh()
        {
            bool visible = root.Session != null && root.Session.State.introSeen && !root.Ui.LegacyModalOpen;
            SetVisible(city, visible); SetVisible(resources, visible); SetVisible(settings, visible);
            if (root.Session != null) cityName.text = root.Definitions.Text("city." + root.Session.State.activeCity);
            float width = ((RectTransform)transform).rect.width;
            float side = Mathf.Min(1, Mathf.Max(0, width - 32) / 800f);
            city.localScale = settings.localScale = Vector3.one * side;
            bool compact = width < 1320;
            float available = compact ? width - 32 : width - 2 * (city.sizeDelta.x + 40);
            resources.localScale = Vector3.one * Mathf.Min(1, Mathf.Max(0, available) / resources.sizeDelta.x);
            resources.anchoredPosition = new Vector2(0, compact ? -120 * side : -18);
        }
        private static void SetVisible(RectTransform group, bool visible)
        { if (group.gameObject.activeSelf != visible) group.gameObject.SetActive(visible); }
#if UNITY_EDITOR
        public void EditorConfigure(RectTransform cityGroup, RectTransform resourceGroup, RectTransform settingsButton, Text name)
        { city = cityGroup; resources = resourceGroup; settings = settingsButton; cityName = name; }
#endif
    }
}

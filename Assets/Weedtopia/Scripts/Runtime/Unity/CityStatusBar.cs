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
            bool visible = root.Session != null && root.Session.State.introSeen && !root.Ui.ModalOpen && !root.Ui.Interaction.BuildingOpen;
            SetVisible(city, visible); SetVisible(resources, visible); SetVisible(settings, visible);
            if (root.Session != null) cityName.text = root.Definitions.Text("city." + root.Session.State.activeCity);
            float width = ((RectTransform)transform).rect.width;
            float side = Mathf.Min(1, Mathf.Max(0, width - 32) / 800f);
            city.localScale = Vector3.one * side;settings.localScale=Vector3.one;
            float pixels=UiDisplayMetrics.Scale(GetComponentInParent<Canvas>());
            settings.sizeDelta=Vector2.one*Mathf.Max(80,48/pixels);
            settings.anchoredPosition=new Vector2(-Mathf.Max(24,12/pixels),-Mathf.Max(18,8/pixels));
            bool compact = width < 1320;
            float available = compact ? width - 32 : width - 2 * (city.sizeDelta.x + 40);
            resources.localScale = Vector3.one * Mathf.Min(1, Mathf.Max(0, available) / resources.sizeDelta.x);
            resources.anchoredPosition = new Vector2(0, compact ? -settings.sizeDelta.y-2*Mathf.Max(24,12/pixels) : -18);
        }
        private static void SetVisible(RectTransform group, bool visible)
        { if (group.gameObject.activeSelf != visible) group.gameObject.SetActive(visible); }
#if UNITY_EDITOR
        public void EditorConfigure(RectTransform cityGroup, RectTransform resourceGroup, RectTransform settingsButton, Text name)
        { city = cityGroup; resources = resourceGroup; settings = settingsButton; cityName = name; }
#endif
    }
}

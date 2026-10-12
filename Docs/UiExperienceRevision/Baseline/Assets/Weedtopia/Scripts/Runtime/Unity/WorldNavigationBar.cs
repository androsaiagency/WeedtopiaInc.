using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Visibility and safe-area fitting for the authored navigation prefab. GameUi owns its actions.</summary>
    public sealed class WorldNavigationBar : MonoBehaviour
    {
        [SerializeField] private RectTransform dock;
        private GameRoot root;
        public RectTransform Dock => dock;
        public void Initialize(GameRoot game) { root=game; Refresh(); }
        private void LateUpdate() { if(root!=null)Refresh(); }
        private void Refresh()
        {
            bool visible=root.Session!=null && root.Session.State.introSeen && !root.Ui.ModalOpen;
            if(dock.gameObject.activeSelf!=visible)dock.gameObject.SetActive(visible);
            var parent=(RectTransform)dock.parent;
            float scale=Mathf.Min(1,Mathf.Max(0,parent.rect.width-32)/dock.sizeDelta.x);
            dock.localScale=Vector3.one*scale;
        }
#if UNITY_EDITOR
        public void EditorConfigure(RectTransform value) { dock=value; }
#endif
    }
}

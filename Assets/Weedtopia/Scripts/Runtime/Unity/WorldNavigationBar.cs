using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Visibility and safe-area fitting for the authored navigation prefab. GameUi owns its actions.</summary>
    public sealed class WorldNavigationBar : MonoBehaviour
    {
        [SerializeField] private RectTransform dock;
        private GameRoot root;
        private RectTransform[] buttons;
        public RectTransform Dock => dock;
        public void Initialize(GameRoot game) { root=game;buttons=System.Array.ConvertAll(dock.GetComponentsInChildren<Button>(true),b=>(RectTransform)b.transform);Refresh(); }
        private void LateUpdate() { if(root!=null)Refresh(); }
        private void Refresh()
        {
            bool visible=root.Session!=null && root.Session.State.introSeen && !root.Ui.ModalOpen && !root.Ui.Interaction.BuildingOpen;
            if(dock.gameObject.activeSelf!=visible)dock.gameObject.SetActive(visible);
            var parent=(RectTransform)dock.parent;
            float pixels=UiDisplayMetrics.Scale(GetComponent<Canvas>()),margin=Mathf.Max(18,12/pixels);
            float unit=Mathf.Max(96,48/pixels),gap=Mathf.Max(16,6/pixels);
            int columns=parent.rect.width*pixels<510?Mathf.Min(5,buttons.Length):buttons.Length;
            columns=Mathf.Max(1,columns);int rows=Mathf.CeilToInt(buttons.Length/(float)columns);
            unit=Mathf.Min(unit,(parent.rect.width-2*margin-(columns-1)*gap-16)/columns);
            dock.localScale=Vector3.one;dock.anchorMin=dock.anchorMax=dock.pivot=new Vector2(.5f,0);dock.anchoredPosition=new Vector2(0,margin);
            dock.sizeDelta=new Vector2(columns*unit+(columns-1)*gap+16,rows*unit+(rows-1)*gap+16);
            for(int i=0;i<buttons.Length;i++)
            {
                int row=i/columns,col=i%columns,count=Mathf.Min(columns,buttons.Length-row*columns);
                var button=buttons[i];button.anchorMin=button.anchorMax=new Vector2(.5f,0);button.pivot=new Vector2(.5f,.5f);button.sizeDelta=Vector2.one*unit;
                button.anchoredPosition=new Vector2((col-(count-1)*.5f)*(unit+gap),(rows-row-.5f)*unit+(rows-row-1)*gap+8);
                foreach(string name in new[]{"Icon","WheelSymbol"})
                {
                    var icon=button.Find(name)?.GetComponent<RectTransform>();if(icon==null)continue;
                    icon.anchorMin=icon.anchorMax=new Vector2(.5f,.5f);icon.anchoredPosition=Vector2.zero;
                    icon.sizeDelta=Vector2.one*unit*(name=="Icon"&&button.name=="LuckyWheel"?.13f:.62f);
                }
            }
        }
#if UNITY_EDITOR
        public void EditorConfigure(RectTransform value) { dock=value; }
#endif
    }
}

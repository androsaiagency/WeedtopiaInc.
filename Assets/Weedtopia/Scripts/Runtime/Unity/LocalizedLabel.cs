using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    /// <summary>Only for authored static captions. Dynamic text is rendered by its owning presenter.</summary>
    [DisallowMultipleComponent,RequireComponent(typeof(Text))]
    public sealed class LocalizedLabel : MonoBehaviour
    {
        [SerializeField] private string key;
        public string Key => key;
        private void OnEnable(){GameText.Changed+=Refresh;Refresh();}
        private void OnDisable(){GameText.Changed-=Refresh;}
        private void Refresh(){if(GameText.Current!=null)GetComponent<Text>().text=GameText.Get(key);}
#if UNITY_EDITOR
        public void EditorConfigure(string value){key=value;}
#endif
    }
}

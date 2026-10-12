using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    public sealed class VipStoryRosterRow : MonoBehaviour
    {
        public string id;
        public bool collaboration;
        public Button button;
        public Image portrait, accent;
        public Text caption, status;
    }
}

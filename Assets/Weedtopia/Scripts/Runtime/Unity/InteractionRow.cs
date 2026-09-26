using System;
using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Reusable authored card for inventory entries and individual fleet slots.</summary>
    public sealed class InteractionRow : MonoBehaviour
    {
        public Image portrait;
        public Text title, body;
        public Button choose, remove;
        public void Bind(Sprite sprite, string heading, string detail, string action, bool enabled, Action select, Action unassign = null)
        {
            portrait.sprite = sprite; title.text = heading; body.text = detail;
            choose.GetComponentInChildren<Text>().text = action; choose.interactable = enabled;
            choose.onClick.RemoveAllListeners(); choose.onClick.AddListener(() => select());
            remove.gameObject.SetActive(unassign != null); remove.onClick.RemoveAllListeners();
            if (unassign != null) remove.onClick.AddListener(() => unassign());
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    public sealed class RewardCardView : MonoBehaviour
    {
        public Image icon, accent;
        public Text title, quantity, detail;
        public Button action;
        public CanvasGroup reveal;
        public void Bind(GameRoot root, RewardItem item, bool inventory = false, Action clicked = null)
        {
            icon.sprite = root.Presentation.art.Get(RewardText.Icon(item, root.Definitions));
            title.text = RewardText.Name(root.Definitions, item);
            quantity.text = RewardText.Quantity(item, inventory);
            detail.text = RewardText.Detail(root, item, inventory);
            accent.color = item.kind == RewardKind.Worker ? Rarity(item.rarity) : item.kind == RewardKind.Title || item.kind == RewardKind.SalesBonus ? new Color(1,.77f,.3f) : new Color(.27f,.76f,.66f);
            action.onClick.RemoveAllListeners();
            action.interactable = clicked != null;
            if (clicked != null) action.onClick.AddListener(() => clicked());
            Reveal(1);
        }
        public void Reveal(float value)
        {
            reveal.alpha = value;
            transform.localScale = Vector3.one * Mathf.Lerp(.85f, 1, value);
        }
        private static Color Rarity(int rarity) => rarity == 4 ? new Color(1,.77f,.3f) : rarity == 3 ? new Color(.76f,.45f,1) :
            rarity == 2 ? new Color(.36f,.7f,1) : rarity == 1 ? new Color(.3f,.85f,.6f) : new Color(.77f,.72f,.57f);
    }
}

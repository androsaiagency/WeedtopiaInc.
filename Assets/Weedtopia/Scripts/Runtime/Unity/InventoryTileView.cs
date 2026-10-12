using System;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    public sealed class InventoryTileView : MonoBehaviour
    {
        public Button button;
        public Image icon, accent, selection, quantityBadge;
        public Text title, quantity;
        // Historical serialized UI label; preserve every existing prefab reference.
        public new Text tag;
        public RewardItem Item { get; private set; }
        public void Bind(GameRoot root, RewardItem item, bool selected, Action clicked)
        {
            Item = item;
            icon.sprite = root.Presentation.art.Get(RewardText.Icon(item, root.Definitions));
            icon.rectTransform.anchoredPosition = RewardText.Icon(item, root.Definitions) == "time" ? new Vector2(icon.rectTransform.sizeDelta.x * .15f,0) : Vector2.zero;
            title.text = RewardText.Name(root.Definitions, item, true);
            quantity.text = item.kind == RewardKind.Worker ? "" : "×" + item.quantity;
            quantityBadge.gameObject.SetActive(item.kind != RewardKind.Worker);
            int rarity = item.kind == RewardKind.Item ? Array.Find(root.Definitions.items, i => i.id == item.key).rarity : item.rarity;
            accent.color = InventoryText.Rarity(rarity);
            tag.text = item.kind == RewardKind.Worker ? root.Definitions.Text("rarity." + rarity) :
                item.kind == RewardKind.Cards ? root.Definitions.Text("reward.kind.Cards") : root.Definitions.Text("inventory.category." + Array.Find(root.Definitions.items, i => i.id == item.key).category);
            selection.gameObject.SetActive(selected);
            button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => clicked());
        }
    }

    public static class InventoryText
    {
        public static Color Rarity(int rarity) => rarity == 4 ? new Color(1,.69f,.26f) : rarity == 3 ? new Color(.75f,.51f,1) :
            rarity == 2 ? new Color(.38f,.73f,1) : rarity == 1 ? new Color(.43f,.86f,.62f) : new Color(.73f,.79f,.77f);
        public static string Duration(int seconds)
        {
            seconds=Math.Max(0,seconds);
            var t = TimeSpan.FromSeconds(seconds);
            if (seconds >= 3600) return GameText.Format("format.duration.hours",(int)t.TotalHours) +
                (t.Minutes > 0 ? " " + GameText.Format("format.duration.minutes",t.Minutes) : "") +
                (t.Seconds > 0 ? " " + GameText.Format("format.duration.seconds",t.Seconds) : "");
            if (seconds >= 60) return GameText.Format("format.duration.minutes",(int)t.TotalMinutes) +
                (t.Seconds > 0 ? " " + GameText.Format("format.duration.seconds",t.Seconds) : "");
            return GameText.Format("format.duration.seconds",seconds);
        }
        public static string Identity(RewardItem item) => item == null ? "" : item.kind + ":" + (item.reference ?? item.key ?? "");
    }
}

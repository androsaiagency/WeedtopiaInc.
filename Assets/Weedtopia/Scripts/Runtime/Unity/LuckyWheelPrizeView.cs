using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    public sealed class LuckyWheelPrizeView : MonoBehaviour
    {
        public Button button;
        public Image icon;
        public Text caption;
        public RectTransform content;
        public WheelPrize Prize { get; private set; }
        public void Bind(GameRoot root,WheelPrize prize,Action clicked)
        {
            Prize=prize;var def=Array.Find(root.Definitions.items,i=>i.id==prize.id);
            icon.sprite=root.Presentation.art.Get(def.icon);
            icon.rectTransform.anchoredPosition=def.icon=="time"?new Vector2(icon.rectTransform.sizeDelta.x*.15f,0):Vector2.zero;
            RefreshLanguage(root);
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>clicked());
        }
        public void RefreshLanguage(GameRoot root)
        {
            if(Prize==null)return;
            var def=Array.Find(root.Definitions.items,i=>i.id==Prize.id);
            string key="wheel.short."+def.id;
            string shortName=def.effect==ItemEffect.ConstructionTime?InventoryText.Duration(def.value):
                root.Definitions.texts.Any(t=>t.key==key)?root.Definitions.Text(key):root.Definitions.Text(def.shortNameKey);
            caption.text="×"+Prize.count+"\n"+shortName;
        }
    }
}

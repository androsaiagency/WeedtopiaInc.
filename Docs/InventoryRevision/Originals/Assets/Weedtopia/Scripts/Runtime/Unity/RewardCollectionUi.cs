using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Application;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    public sealed class RewardCollectionUi : MonoBehaviour
    {
        public Button inventoryTab, achievementsTab, historyTab, previousButton, nextButton;
        public Text heading, description, pageLabel, emptyLabel;
        public RewardCardView[] cards;
        private GameRoot root;
        private int tab, page, pages;
        private long revision = -1;
        public int SelectedTab => tab;
        public int Page => page;
        public void Initialize(GameRoot game)
        {
            root=game;
            inventoryTab.onClick.AddListener(()=>SelectTab(0));
            achievementsTab.onClick.AddListener(()=>SelectTab(1));
            historyTab.onClick.AddListener(()=>SelectTab(2));
            previousButton.onClick.AddListener(()=>{page=Mathf.Max(0,page-1);Paint();});
            nextButton.onClick.AddListener(()=>{page=Mathf.Min(pages-1,page+1);Paint();});
        }
        public void SelectTab(int index)
        {
            tab = Mathf.Clamp(index,0,2); page=0; Paint();
        }
        public void Refresh()
        {
            if (gameObject.activeInHierarchy && root.Session != null && revision != root.Session.State.revision) Paint();
        }
        public void Paint()
        {
            if(root?.Session==null)return;
            var session=root.Session;var state=session.State;var defs=root.Definitions;revision=state.revision;
            var list=new List<RewardItem>();
            heading.text=defs.Text(tab==0?"reward.inventory":tab==1?"reward.achievements":"reward.history");
            description.text=defs.Text(tab==0?"reward.inventory_hint":tab==1?"reward.achievements_hint":"reward.history_hint");
            if(tab==2)
            {
                var receipts=state.rewardHistory ?? new List<RewardReceipt>();
                pages=Mathf.Max(1,receipts.Count);page=Mathf.Clamp(page,0,pages-1);
                if(receipts.Count>0)
                {
                    var receipt=receipts[receipts.Count-1-page];
                    list=receipt.items;
                    description.text=defs.Text(receipt.titleKey)+" · "+RewardText.Source(defs,receipt)+"\n"+defs.Text("reward.history_hint");
                }
            }
            else
            {
                var all=tab==0?RewardCatalog.Inventory(state):RewardCatalog.Achievements(session);
                pages=Mathf.Max(1,Mathf.CeilToInt((float)all.Count/cards.Length));page=Mathf.Clamp(page,0,pages-1);
                list=all.Skip(page*cards.Length).Take(cards.Length).ToList();
            }
            emptyLabel.gameObject.SetActive(list.Count==0);
            emptyLabel.text=defs.Text(tab==1?"reward.no_achievements":"reward.no_history");
            for(int i=0;i<cards.Length;i++)
            {
                cards[i].gameObject.SetActive(i<list.Count);
                if(i>=list.Count)continue;
                var item=list[i];
                Action clicked=null;
                if(item.kind==RewardKind.Worker && !string.IsNullOrEmpty(item.reference))
                {string id=item.reference;clicked=()=>root.Ui.ShowWorker(id);}
                cards[i].Bind(root,item,tab==0,clicked);
            }
            inventoryTab.interactable=tab!=0;achievementsTab.interactable=tab!=1;historyTab.interactable=tab!=2;
            previousButton.interactable=page>0;nextButton.interactable=page<pages-1;
            pageLabel.text=(page+1)+" / "+pages;
        }
    }
}

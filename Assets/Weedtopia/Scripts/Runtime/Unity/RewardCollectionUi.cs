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
        public int layoutVersion;
        public Button inventoryTab, achievementsTab, historyTab, previousButton, nextButton;
        public Button[] categoryButtons;
        public Text heading, description, pageLabel, emptyLabel, balance;
        public RewardCardView[] cards;
        public InventoryTileView[] tiles;
        public InventoryTileView tilePrefab;
        public RewardCardView cardPrefab;
        public InventoryDetailView details;
        public GameObject inventoryArea, archiveArea;
        private GameRoot root;
        private int tab, category;
        private long revision = -1;
        private string selection;
        public int SelectedTab => tab;
        public int SelectedCategory => category;
        public int Page => 0;
        public void Initialize(GameRoot game)
        {
            root = game;
            inventoryTab.onClick.AddListener(() => SelectTab(0));
            achievementsTab.onClick.AddListener(() => SelectTab(1));
            historyTab.onClick.AddListener(() => SelectTab(2));
            for (int i = 0; i < categoryButtons.Length; i++) { int index = i; categoryButtons[i].onClick.AddListener(() => SelectCategory(index)); }
            details.Initialize(game);
        }
        public void SelectTab(int index)
        {
            details.Show(null); selection = null; tab = Mathf.Clamp(index, 0, 2); category = 0; Paint();ResetScroll();
        }
        public void SelectCategory(int index)
        {
            details.Show(null); selection = null; category = Mathf.Clamp(index, 0, 3); Paint();ResetScroll();
        }
        public void Inspect(RewardItem item)
        {
            selection = InventoryText.Identity(item); details.Show(item); Paint();GetComponent<InventoryPanelLayout>()?.RevealDetails();
        }
        public void ClearSelection(){selection=null;details.Show(null);Paint();}
        private void ResetScroll()=>GetComponent<InventoryPanelLayout>()?.ResetList();
        public void Refresh()
        {
            if (!gameObject.activeInHierarchy || root.Session == null) return;
            if (revision != root.Session.State.revision) Paint();
            RefreshBalance();
            details.Refresh();
        }
        public void Paint()
        {
            if (root?.Session == null) return;
            var session = root.Session; var state = session.State; var defs = root.Definitions; revision = state.revision;
            Caption(inventoryTab,"reward.tab.inventory");
            Caption(achievementsTab,"reward.tab.achievements");
            Caption(historyTab,"reward.tab.history");
            var categoryKeys=new[]{"inventory.all","inventory.filter.team","inventory.filter.build_time","inventory.category.Supplies"};
            for(int i=0;i<Math.Min(categoryButtons.Length,categoryKeys.Length);i++)Caption(categoryButtons[i],categoryKeys[i]);
            RefreshBalance();
            heading.text = defs.Text(tab == 0 ? "reward.inventory" : tab == 1 ? "reward.achievements" : "reward.history");
            description.text = defs.Text(tab == 0 ? "reward.inventory_hint" : tab == 1 ? "reward.achievements_hint" : "reward.history_hint");
            inventoryArea.SetActive(tab == 0); archiveArea.SetActive(tab != 0);
            var list = new List<RewardItem>();
            var sources = new List<string>();
            if (tab == 0)
            {
                var all = RewardCatalog.Inventory(state).Where(MatchesCategory).ToList();
                list = all;
                // Only data rows are instantiated; all surfaces and controls are authored in prefabs.
                if(all.Count>tiles.Length)
                {
                    var pool=tiles.ToList();while(pool.Count<all.Count)pool.Add(Instantiate(tilePrefab,tiles[0].transform.parent));tiles=pool.ToArray();
                }
                if (!all.Any(i => InventoryText.Identity(i) == selection) && selection != null) { selection = null; details.Show(null); }
                for (int i = 0; i < tiles.Length; i++)
                {
                    tiles[i].gameObject.SetActive(i < list.Count); if (i >= list.Count) continue;
                    var item = list[i]; tiles[i].Bind(root, item, InventoryText.Identity(item) == selection, () => Inspect(item));
                }
                for (int i = 0; i < categoryButtons.Length; i++) categoryButtons[i].interactable = i != category;
            }
            else
            {
                if (tab == 2)
                {
                    var receipts = state.rewardHistory ?? new List<RewardReceipt>();
                    foreach(var receipt in receipts.AsEnumerable().Reverse())
                    {list.AddRange(receipt.items);sources.AddRange(receipt.items.Select(_=>defs.Text(receipt.titleKey)+" · "+RewardText.Source(defs,receipt)));}
                }
                else
                {
                    var all = RewardCatalog.Achievements(session);
                    list = all.ToList();
                }
                if(list.Count>cards.Length){var pool=cards.ToList();while(pool.Count<list.Count)pool.Add(Instantiate(cardPrefab,cards[0].transform.parent));cards=pool.ToArray();}
                for (int i = 0; i < cards.Length; i++)
                { cards[i].gameObject.SetActive(i < list.Count); if (i < list.Count){cards[i].Bind(root, list[i]);if(tab==2)cards[i].detail.text=sources[i]+"\n"+cards[i].detail.text;} }
            }
            emptyLabel.gameObject.SetActive(list.Count == 0);
            emptyLabel.rectTransform.anchoredPosition = tab == 0 ? new Vector2(-230,-40) : Vector2.zero;
            emptyLabel.rectTransform.sizeDelta = tab == 0 ? new Vector2(720,160) : new Vector2(1100,160);
            emptyLabel.text = defs.Text(tab == 0 ? category == 0 ? "inventory.empty" : "inventory.no_filter" : tab == 1 ? "reward.no_achievements" : "reward.no_history");
            inventoryTab.interactable = tab != 0; achievementsTab.interactable = tab != 1; historyTab.interactable = tab != 2;
            previousButton.gameObject.SetActive(false);nextButton.gameObject.SetActive(false);pageLabel.gameObject.SetActive(false);
        }
        private void RefreshBalance() => balance.text = string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,root.Definitions.Text("inventory.balance"), root.Session.State.money, root.Session.State.grinders);
        private void Caption(Button button,string key)
        {
            if(button==null)return;
            var label=button.GetComponentInChildren<Text>(true);
            if(label!=null)label.text=root.Definitions.Text(key);
        }
        private bool MatchesCategory(RewardItem item)
        {
            if (category == 0) return true;
            var itemCategory = item.kind == RewardKind.Item ? root.Definitions.items.First(i => i.id == item.key).category : ItemCategory.Workers;
            return (int)itemCategory == category - 1;
        }
    }
}

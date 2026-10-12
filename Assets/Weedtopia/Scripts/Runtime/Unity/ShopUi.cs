using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Application;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    public sealed class ShopUi : MonoBehaviour
    {
        public RectTransform catalog, confirmation;
        public Text mode, balance, status;
        public Button[] categories;
        public ShopProductCard[] cards;
        public ScrollRect productScroll, confirmationScroll;
        public Text confirmTitle, confirmContents, confirmPrice, confirmNotice;
        public Image confirmIcon;
        public Button confirmPurchase, confirmBack;
        public ShopCategory Category { get; private set; }
        public ShopProductDefinition Selected { get; private set; }
        public bool Confirming => Selected != null;
        private GameRoot root;
        private string feedbackKey;
        private ShopProductDefinition feedbackProduct;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        public void Initialize(GameRoot value)
        {
            root = value;
            for (int i = 0; i < categories.Length; i++) { var category = (ShopCategory)i; categories[i].onClick.AddListener(() => SelectCategory(category)); }
            foreach (var card in cards) { var id = card.productId; card.purchase.onClick.AddListener(() => Preview(id)); }
            confirmBack.onClick.AddListener(() => { if (!root.Session.Shop.Purchasing) { Selected = null; Refresh(); } });
            confirmPurchase.onClick.AddListener(Purchase);
        }
        private void OnDestroy() { lifetime.Cancel(); lifetime.Dispose(); }
        public void Open() { Selected = null; ClearFeedback(); SelectCategory(ShopCategory.Packages); }
        public void SelectCategory(ShopCategory category)
        {
            if (root.Session?.Shop.Purchasing == true) return;
            Category = category; Selected = null; ClearFeedback(); Refresh();
            productScroll.StopMovement(); productScroll.verticalNormalizedPosition = 1;
        }
        public void Preview(string id)
        {
            if (root.Session.Shop.Purchasing) return;
            Selected = root.Definitions.shopProducts.FirstOrDefault(p => p.id == id); Refresh();
            confirmationScroll.StopMovement(); confirmationScroll.verticalNormalizedPosition = 1;
        }
        public void Refresh()
        {
            if (root?.Session == null) return;
            var shop = root.Session.Shop;
            bool test = shop.IsTestMode;
            catalog.gameObject.SetActive(!Confirming); confirmation.gameObject.SetActive(Confirming);
            mode.text = test ? GameText.Get("presentation.shopui.testshop_keine_echten_zahlungen.c61901df") : GameText.Get("presentation.shopui.weedtopia_shop.22c219ce");
            balance.text = GameText.Format("shop.balance",root.Session.State.money,root.Session.State.grinders.ToString("N0",root.Localization.Culture));
            status.text = FeedbackText();
            if (string.IsNullOrEmpty(feedbackKey) && Category == ShopCategory.Vip)
            {
                var vip = root.Session.State.shop?.vipMembership;
                status.text = shop.VipActive ? GameText.Format("shop.vip_until",new DateTime(vip.expiresUtcTicks,DateTimeKind.Utc).ToLocalTime().ToString("g",root.Localization.Culture))
                    : GameText.Get("presentation.shopui.dein_vip_pass_30_tage_vorteile_f_r_deine.87176bcb");
            }
            for (int i = 0; i < categories.Length; i++) categories[i].interactable = (int)Category != i && !shop.Purchasing;
            foreach (var card in cards)
            {
                var product = root.Definitions.shopProducts.First(p => p.id == card.productId);
                card.gameObject.SetActive(product.category == Category);
                if (!card.gameObject.activeSelf) continue;
                var listing = shop.Listing(product);
                card.title.text = root.Definitions.Text(product.titleKey);
                card.contents.text = product.category == ShopCategory.Money ? GameText.Get("presentation.shopui.f_r_geb_ude_und_ausbau_direkt_gutgeschri.d1597c82")
                    : product.category == ShopCategory.Grinders ? GameText.Get("presentation.shopui.f_r_dein_team_und_neue_m_glichkeiten_dir.0d183134") : Contents(product);
                card.icon.sprite = root.Presentation.art.Get(product.icon);
                card.ribbon.text = product.vipDays > 0 ? GameText.Get("presentation.shopui.vip_pass.03284444") : product.category == ShopCategory.Packages ? GameText.Get("presentation.shopui.paket.f1f37bb2") : product.category == ShopCategory.Boosters ? GameText.Get("presentation.shopui.bauzeit.47b2494b") : GameText.Get("presentation.shopui.stadtvorrat.709a9e16");
                card.price.text = (test ? GameText.Get("presentation.shopui.sp_ter.87f7d25f") : "") + listing.localizedPrice;
                card.purchase.interactable = listing.available && !shop.Purchasing;
                card.purchase.GetComponentInChildren<Text>().text = listing.available ? test ? GameText.Get("presentation.shopui.testkauf_kostenlos.f90c7b16") : GameText.Get("presentation.shopui.kaufen.ad27b781") : GameText.Get("presentation.shopui.bald_verf_gbar.c5d41f6f");
            }
            if (Selected != null)
            {
                var listing = shop.Listing(Selected);
                confirmTitle.text = root.Definitions.Text(Selected.titleKey);
                confirmContents.text = Contents(Selected);
                confirmIcon.sprite = root.Presentation.art.Get(Selected.icon);
                confirmPrice.text = test ? GameText.Get("presentation.shopui.jetzt_kostenlos_testen_geplanter_preis.35feb397") + listing.localizedPrice : listing.localizedPrice;
                confirmNotice.text = Selected.vipDays > 0
                    ? GameText.Get("presentation.shopui.die_ersten_50_grinder_erh_ltst_du_sofort.3b798b75")
                    : (Selected.items?.Length > 0 ? GameText.Get("presentation.shopui.die_beschleuniger_findest_du_anschlie_en.71932b4b") : GameText.Get("presentation.shopui.der_betrag_wird_deinem_vorhandenen_spiel.aa9992d3"));
                if (test) confirmNotice.text += GameText.Get("presentation.shopui.testkauf_es_wird_kein_echtgeld_abgebucht.023b9d8d");
                confirmPurchase.GetComponentInChildren<Text>().text = shop.Purchasing ? GameText.Get("presentation.shopui.wird_verarbeitet.e1118ba6") : test ? GameText.Get("presentation.shopui.kostenlos_best_tigen.66f13e0d") : GameText.Get("presentation.shopui.kaufen.415cf217") + listing.localizedPrice;
                confirmPurchase.interactable = listing.available && !shop.Purchasing;
                confirmBack.interactable = !shop.Purchasing;
            }
            GetComponent<ShopPanelLayout>().Fit(UiDisplayMetrics.Scale(root.Ui.GetComponent<Canvas>()));
        }
        public string Contents(ShopProductDefinition product)
        {
            if (product.vipDays > 0) return GameText.Get("presentation.shopui.50_grinder_pro_tag_50_produktionsgeschwi.e0896ae5");
            var lines = new System.Collections.Generic.List<string>();
            if (product.money > 0) lines.Add(GameText.Format("format.money",product.money.ToString("N0",root.Localization.Culture)));
            if (product.grinders > 0) lines.Add(GameText.Format("format.grinders",product.grinders.ToString("N0",root.Localization.Culture)));
            foreach (var item in product.items ?? Array.Empty<ItemStack>())
            {
                var definition = root.Definitions.items.First(d => d.id == item.id);
                string duration = definition.value < 3600 ? GameText.Format("shop.duration_minutes",definition.value/60) : GameText.Format("shop.duration_hours",definition.value/3600);
                lines.Add(GameText.Format("shop.accelerator",item.count,duration));
            }
            return string.Join("\n", lines);
        }
        private void ClearFeedback(){feedbackKey=null;feedbackProduct=null;}
        private string FeedbackText()
        {
            if(string.IsNullOrEmpty(feedbackKey))return "";
            string text=root.Definitions.Text(feedbackKey);
            if(feedbackProduct!=null)
                text+=feedbackProduct.vipDays>0?GameText.Get("presentation.shopui.vip_f_r_weitere_30_tage.ad9d584f"):Contents(feedbackProduct).Replace("\n"," · ");
            return text;
        }
        private async void Purchase()
        {
            if (Selected == null || root.Session.Shop.Purchasing) return;
            var session = root.Session; var product = Selected;
            try
            {
                var task = session.Shop.PurchaseAsync(product.id, lifetime.Token); Refresh();
                var result = await task;
                if (this == null || root == null || root.Session != session) return;
                ClearFeedback();
                if (result.cancelled) feedbackKey = "presentation.shopui.kauf_abgebrochen_es_wurde_nichts_gutgesc.c070ecdb";
                else if (result.failure != Failure.None) feedbackKey = "failure." + result.failure;
                else
                {
                    feedbackKey=result.alreadyFulfilled?"presentation.shopui.dieser_kauf_wurde_bereits_gutgeschrieben.404bbb2e":"presentation.shopui.erhalten.21402251";
                    if(!result.alreadyFulfilled)feedbackProduct=product;
                    Selected=null;
                }
                root.Refresh(); Refresh();
            }
            catch (Exception exception)
            {
                if (this == null) return;
                ClearFeedback();feedbackKey="presentation.shopui.gutschrift_konnte_nicht_gespeichert_werd.66c2836e";
                root.Ui.Notify(FeedbackText()); Debug.LogWarning("Shop purchase failed: " + exception.Message); Refresh();
            }
        }
    }
}

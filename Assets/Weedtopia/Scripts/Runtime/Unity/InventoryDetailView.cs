using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Application;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    /// <summary>All controls are authored. The view issues application commands, never changes item balances itself.</summary>
    public sealed class InventoryDetailView : MonoBehaviour
    {
        public Image icon, accent;
        public Text title, metadata, description, effect, choiceLabel, choiceHeading, actionLabel;
        public Button action, cancel, previousChoice, nextChoice, split;
        public Text splitLabel;
        private GameRoot root;
        private RewardItem selected;
        private string target;
        private ItemUseOffer offer;
        private bool confirming, priorPaused;
        private GameSession pausedSession;
        public RewardItem SelectedItem => selected;
        public bool Confirming => confirming;
        public void Initialize(GameRoot game)
        {
            root = game;
            action.onClick.AddListener(Activate);
            split.onClick.AddListener(PrepareSplit);
            cancel.onClick.AddListener(() => { CancelOffer(); Refresh(); });
            previousChoice.onClick.AddListener(() => ChangeChoice(-1));
            nextChoice.onClick.AddListener(() => ChangeChoice(1));
        }
        private string T(string key) => root.Definitions.Text(key);
        private ItemDefinition Definition => selected?.kind == RewardKind.Item ? root.Definitions.items.First(i => i.id == selected.key) : null;
        public void Show(RewardItem item)
        {
            CancelOffer(); selected = item; target = null;
            Refresh();
        }
        private string[] Choices(ItemDefinition definition)
        {
            if (definition?.effect == ItemEffect.ConstructionTime) return root.Session.Inventory.ConstructionTargets().Select(b => b.id).ToArray();
            if (definition?.effect == ItemEffect.WorkerChoice) return root.Definitions.roles.Select(r => r.id).ToArray();
            return Array.Empty<string>();
        }
        private void ChangeChoice(int direction)
        {
            CancelOffer(); var choices = Choices(Definition); if (choices.Length == 0) return;
            target = choices[(Math.Max(0, Array.IndexOf(choices, target)) + direction + choices.Length) % choices.Length]; Refresh();
        }
        public void Refresh()
        {
            if (root?.Session == null) return;
            var session = root.Session;
            split.gameObject.SetActive(false);
            action.gameObject.SetActive(selected != null); cancel.gameObject.SetActive(confirming);
            previousChoice.gameObject.SetActive(false); nextChoice.gameObject.SetActive(false);
            choiceLabel.text = choiceHeading.text = effect.text = "";
            if (selected == null)
            {
                icon.sprite = root.Presentation.art.Get("gift"); icon.rectTransform.anchoredPosition = Vector2.zero; title.text = T("inventory.pick"); metadata.text = "";
                description.text = T("inventory.pick_hint"); accent.color = InventoryText.Rarity(4); return;
            }
            icon.sprite = root.Presentation.art.Get(RewardText.Icon(selected, root.Definitions));
            icon.rectTransform.anchoredPosition = RewardText.Icon(selected, root.Definitions) == "time" ? new Vector2(icon.rectTransform.sizeDelta.x * .15f,0) : Vector2.zero;
            title.text = RewardText.Name(root.Definitions, selected);
            if (selected.kind == RewardKind.Worker)
            {
                var worker = session.State.workers.FirstOrDefault(w => w.id == selected.reference);
                if (worker == null) { Show(null); return; }
                accent.color = InventoryText.Rarity(worker.rarity);
                metadata.text = string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("inventory.worker_meta"), T("rarity." + worker.rarity), worker.level, worker.rank);
                description.text = T("inventory.worker_info");
                effect.text = string.Join("\n", worker.attributes.Select(a => T("attribute." + (int)a) + " +" + (session.Workers.Effect(worker,a)*100).ToString("0.#",root.Localization.Culture) + " %"));
                choiceLabel.text = string.IsNullOrEmpty(worker.buildingId) ? T("ui.unassigned") :
                    T(root.Definitions.Building(session.State.Find(worker.buildingId).family).nameKey);
                actionLabel.text = T("inventory.manage"); action.interactable = true; return;
            }
            if (selected.kind == RewardKind.Cards)
            {
                accent.color = InventoryText.Rarity(1); metadata.text = string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("inventory.owned"), session.State.cards);
                description.text = T("inventory.cards_info"); actionLabel.text = T("inventory.recruit");
                action.interactable = session.State.City.unlocked >= Zone.City && session.State.cards > 0;
                if (!action.interactable) effect.text = T("inventory.locked"); return;
            }
            var item = Definition;
            if (item == null) { action.interactable = false; return; }
            accent.color = InventoryText.Rarity(item.rarity);
            metadata.text = string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("inventory.owned"), session.Inventory.Count(item.id)) + " · " + T("rarity." + item.rarity);
            description.text = T(item.descriptionKey);
            var choices = Choices(item);
            if (!choices.Contains(target)) target = choices.FirstOrDefault();
            bool needsTarget = item.effect == ItemEffect.ConstructionTime || item.effect == ItemEffect.WorkerChoice;
            bool locked = item.category == ItemCategory.Workers && session.State.City.unlocked < Zone.City;
            action.interactable = session.Inventory.Count(item.id) > 0 && (!needsTarget || target != null) && !locked;
            actionLabel.text = T(confirming ? "inventory.confirm" : item.effect == ItemEffect.ConstructionTime ? "inventory.boost" :
                item.effect == ItemEffect.WorkerChoice ? "inventory.choose_worker" : "inventory.open");
            choiceHeading.text = needsTarget ? T(item.effect == ItemEffect.ConstructionTime ? "inventory.choose_build" : "inventory.choose_role") : "";
            previousChoice.gameObject.SetActive(choices.Length > 1 && !confirming); nextChoice.gameObject.SetActive(choices.Length > 1 && !confirming);
            if (item.effect == ItemEffect.ConstructionTime)
            {
                var building = session.Inventory.ConstructionTargets().FirstOrDefault(b => b.id == target);
                if (building == null) effect.text = T("inventory.no_build");
                else
                {
                    int before = building.buildingSeconds, after = Math.Max(0, before - item.value), wasted = Math.Max(0, item.value - before);
                    choiceLabel.text = T(root.Definitions.Building(building.family).nameKey) + " · " + GameText.Format("format.level",building.level+1) + "\n" +
                        (Array.IndexOf(choices, target) + 1) + " / " + choices.Length;
                    effect.text = string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("inventory.before_after"), InventoryText.Duration(before), InventoryText.Duration(after));
                    if (after == 0) effect.text += "\n" + T("inventory.ready");
                    if (wasted > 0) effect.text += "\n" + string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("inventory.waste"), InventoryText.Duration(wasted));
                }
            }
            else if (item.effect == ItemEffect.WorkerChoice && target != null)
                choiceLabel.text = T(root.Definitions.Role(target).nameKey) + "\n" + (Array.IndexOf(choices,target) + 1) + " / " + choices.Length;
            else effect.text = string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("inventory.contents"), item.value, T("reward.kind." + (item.effect == ItemEffect.Cards ? "Cards" : item.effect == ItemEffect.Grinders ? "Grinders" : "Money")));
            if (locked) effect.text = T("inventory.locked");
            var smaller = session.Inventory.SplitTarget(item.id);
            split.gameObject.SetActive(smaller != null && !confirming);
            if (smaller != null)
            {
                split.interactable = session.Inventory.Count(item.id) > 0;
                splitLabel.text = string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("inventory.split"), item.value / smaller.value, InventoryText.Duration(smaller.value));
            }
            if (confirming)
            {
                choiceHeading.text = T("inventory.confirm_hint");
                if (offer.SplitItemId != null)
                {
                    action.interactable = true; choiceLabel.text = "";
                    effect.text = string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("inventory.split_preview"), InventoryText.Duration(item.value), offer.SplitCount,
                        InventoryText.Duration(root.Definitions.items.First(i => i.id == offer.SplitItemId).value));
                }
            }
        }
        public void Activate()
        {
            if (selected == null || !action.interactable) return;
            if (selected.kind == RewardKind.Worker) { root.Ui.ShowWorker(selected.reference); return; }
            if (selected.kind == RewardKind.Cards) { root.Ui.ShowWorkers(); return; }
            if (confirming)
            {
                string token = offer.Token; bool speedup = Definition.effect == ItemEffect.ConstructionTime && offer.SplitItemId == null;
                ReleasePause(); confirming = false; offer = null;
                var confirmed = root.Command(() => root.Session.Inventory.Confirm(token));
                if (speedup && confirmed == Failure.None) root.Ui.Notify(T("inventory.applied"));
                Refresh(); return;
            }
            var result = root.Session.Inventory.Offer(selected.key, target, out offer);
            if (result != Failure.None) { root.Ui.Notify(T("failure." + result)); Refresh(); return; }
            // The confirmation preview stays exact while it is visible. Closing or changing selection releases the pause.
            pausedSession = root.Session; priorPaused = pausedSession.Paused; pausedSession.Paused = true;
            confirming = true; Refresh();
        }
        private void PrepareSplit()
        {
            if (selected?.kind != RewardKind.Item || confirming) return;
            var result = root.Session.Inventory.OfferSplit(selected.key, out offer);
            if (result != Failure.None) { root.Ui.Notify(T("failure." + result)); return; }
            pausedSession = root.Session; priorPaused = pausedSession.Paused; pausedSession.Paused = true;
            confirming = true; Refresh();
        }
        private void ReleasePause()
        {
            if (pausedSession != null) pausedSession.Paused = priorPaused;
            pausedSession = null;
        }
        private void CancelOffer()
        {
            if (confirming) pausedSession?.Inventory.Cancel();
            ReleasePause(); confirming = false; offer = null;
        }
        private void OnDisable() { CancelOffer(); }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    public sealed class RewardPresentation : MonoBehaviour
    {
        public RectTransform stage, chest, rays;
        public RectTransform[] sparkles;
        public Text headline, source, caption, counter, continueText, collectionText;
        public Button continueButton, collectionButton;
        public RewardCardView[] cards;
        public AudioSource audioSource;
        private readonly Queue<RewardReceipt> pending = new Queue<RewardReceipt>();
        private GameRoot root;
        private float started;
        private bool priorPaused, revealed;
        private int visibleCount, itemOffset;
        public bool IsOpen => gameObject.activeSelf;
        public RewardReceipt Current { get; private set; }
        public void Initialize(GameRoot game)
        {
            root = game;
            continueButton.onClick.AddListener(Advance);
            collectionButton.onClick.AddListener(() => { DismissAll(); root.Ui.ShowCollection(); });
            continueText.text = root.Definitions.Text("reward.continue");
            collectionText.text = root.Definitions.Text("reward.open_collection");
            gameObject.SetActive(false);
        }
        public void EnqueueAfter(long sequence)
        {
            var receipts = root.Session.State.rewardHistory;
            if (receipts == null) return;
            foreach (var receipt in receipts.Where(r => r.sequence > sequence)) pending.Enqueue(receipt);
            if (!IsOpen && pending.Count > 0)
            {
                priorPaused = root.Session.Paused;
                root.Session.Paused = true;
                gameObject.SetActive(true); Next();
            }
        }
        private void Next()
        {
            Current = pending.Dequeue(); itemOffset = 0; PaintPage();
        }
        private void PaintPage()
        {
            started = Time.unscaledTime; revealed = false;
            PaintReceiptText();
            visibleCount = Mathf.Min(cards.Length, Current.items.Count - itemOffset);
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].gameObject.SetActive(i < visibleCount);
                if (i >= visibleCount) continue;
                cards[i].Bind(root, Current.items[itemOffset + i]); cards[i].Reveal(0);
                int rowCount = Mathf.Min(3, visibleCount - i / 3 * 3);
                ((RectTransform)cards[i].transform).anchoredPosition = new Vector2((i % 3 - (rowCount - 1) * .5f) * 350, -132 - i / 3 * 168);
            }
            if (audioSource != null && root.Presentation.upgradeSound != null) audioSource.PlayOneShot(root.Presentation.upgradeSound, .55f);
            RefreshLayout();
        }
        public void RefreshLanguage()
        {
            continueText.text = root.Definitions.Text("reward.continue");
            collectionText.text = root.Definitions.Text("reward.open_collection");
            if (!IsOpen || Current == null) return;
            PaintReceiptText();
            for (int i = 0; i < visibleCount; i++)
            {
                var item = Current.items[itemOffset + i];
                // Binding/revealing a card would restart presentation or replace its input state.
                cards[i].title.text = RewardText.Name(root.Definitions, item);
                cards[i].quantity.text = RewardText.Quantity(item);
                cards[i].detail.text = RewardText.Detail(root, item, false);
            }
        }
        private void PaintReceiptText()
        {
            headline.text = root.Definitions.Text(Current.source == RewardSource.Recruitment ? "reward.welcome" : "reward.headline");
            source.text = root.Definitions.Text(Current.titleKey);
            caption.text = root.Definitions.Text("reward.secured");
            counter.text = RewardText.Source(root.Definitions, Current) + (Current.items.Count > cards.Length ? " · " + (itemOffset / cards.Length + 1) + " / " + Mathf.CeilToInt((float)Current.items.Count / cards.Length) : "");
        }
        public void Advance()
        {
            if (!IsOpen) return;
            if (!revealed) { revealed = true; foreach (var card in cards) card.Reveal(1); return; }
            if (itemOffset + visibleCount < Current.items.Count) { itemOffset += visibleCount; PaintPage(); }
            else if (pending.Count > 0) Next(); else DismissAll();
        }
        public void DismissAll()
        {
            if (IsOpen && root.Session != null) root.Session.Paused = priorPaused;
            pending.Clear(); Current = null; gameObject.SetActive(false);
        }
        public void RefreshLayout()
        {
            if (!IsOpen) return;
            transform.SetAsLastSibling();
            var bounds = ((RectTransform)transform).rect;
            stage.localScale = Vector3.one * Mathf.Min(1, (bounds.width-24)/stage.sizeDelta.x, (bounds.height-24)/stage.sizeDelta.y);
        }
        private void Update()
        {
            if (root == null || Current == null) return;
            RefreshLayout(); float elapsed = Time.unscaledTime - started;
            float pop = Mathf.Clamp01(elapsed / .55f);
            chest.localScale = Vector3.one * (Mathf.Lerp(.65f, 1, 1-Mathf.Pow(1-pop,3)) + Mathf.Sin(pop*Mathf.PI)*.08f);
            rays.localRotation = Quaternion.Euler(0,0,elapsed*4);
            for (int i=0; i<sparkles.Length; i++)
            {
                float angle = i * 2.399963f + elapsed * .06f;
                float radius = 150 + (i % 5) * 37;
                sparkles[i].anchoredPosition = new Vector2(Mathf.Cos(angle)*radius, 120+Mathf.Sin(angle)*radius*.7f);
                sparkles[i].localScale = Vector3.one * (.35f + .65f * Mathf.Abs(Mathf.Sin(elapsed*1.7f+i)));
            }
            if (!revealed)
            {
                for (int i=0;i<visibleCount;i++) cards[i].Reveal(Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-.35f-i*.12f)/.35f)));
                if (elapsed > .8f + visibleCount*.12f) revealed = true;
            }
        }
    }
}

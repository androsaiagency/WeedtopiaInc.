using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Density-aware cards and an independently scrolling confirmation with fixed actions.</summary>
    public sealed class ShopPanelLayout : MonoBehaviour
    {
        public ModalFrame frame;
        public ShopUi ui;
        public void Fit(float pixels)
        {
            float font = Mathf.Max(26, 18 / pixels), gap = Mathf.Max(16, 8 / pixels), button = Mathf.Max(72, 48 / pixels);
            float w = frame.viewport.rect.width, h = frame.viewport.rect.height;
            Stretch(frame.body); frame.body.localScale = Vector3.one;
            Stretch(ui.catalog); Stretch(ui.confirmation);
            if (!ui.Confirming)
            {
                float y = Paragraph(ui.mode, 0, w, font * .8f) + gap * .5f;
                y += Paragraph(ui.balance, y, w, font * .9f) + gap;
                int columns = w * pixels < 540 ? 3 : 5;
                float cell = (w - gap * (columns - 1)) / columns;
                for (int i = 0; i < ui.categories.Length; i++) Action(ui.categories[i], -w * .5f + cell * .5f + i % columns * (cell + gap), y + i / columns * (button + gap), cell, button, font * .9f);
                y += Mathf.Ceil(ui.categories.Length / (float)columns) * (button + gap);
                if (!string.IsNullOrEmpty(ui.status.text)) y += Paragraph(ui.status, y, w, font * .85f) + gap;
                Put((RectTransform)ui.productScroll.transform, 0, y, w, Mathf.Max(button, h - y)); Stretch(ui.productScroll.viewport);
                var cards = ui.cards.Where(c => c.gameObject.activeSelf).ToArray();
                columns = ui.Category == Weedtopia.Domain.ShopCategory.Vip ? 1 : w * pixels >= 850 ? 3 : w * pixels >= 580 ? 2 : 1;
                cell = (w - gap * (columns - 1)) / columns;
                if (ui.Category == Weedtopia.Domain.ShopCategory.Vip) cell = Mathf.Min(cell, Mathf.Max(850, 600 / pixels));
                float gridWidth = cell * columns + gap * (columns - 1);
                bool compact = h * pixels < 420;
                float ribbonHeight = compact ? 0 : font * 1.3f + gap * .5f;
                float cardHeight = 0;
                foreach (var card in cards)
                {
                    card.title.fontSize = Mathf.RoundToInt(font * 1.1f); card.contents.fontSize = Mathf.RoundToInt(font);
                    float textWidth = cell - 3 * gap - font * 3;
                    cardHeight = Mathf.Max(cardHeight, gap + ribbonHeight + Mathf.Max(font * 1.4f, DialoguePanel.PreferredHeight(card.title, cell - 2 * gap)) + gap
                        + Mathf.Max(font * 3, DialoguePanel.PreferredHeight(card.contents, textWidth)) + gap + button + gap);
                }
                for (int i = 0; i < cards.Length; i++)
                {
                    var card = cards[i];
                    Put((RectTransform)card.transform, -gridWidth * .5f + cell * .5f + i % columns * (cell + gap), i / columns * (cardHeight + gap), cell, cardHeight);
                    card.ribbon.gameObject.SetActive(!compact);
                    card.ribbon.fontSize = Mathf.RoundToInt(font * .75f); Put(card.ribbon.rectTransform, 0, gap, cell - 2 * gap, font * 1.3f);
                    float top = gap + ribbonHeight;
                    float titleHeight = Mathf.Max(font * 1.4f, DialoguePanel.PreferredHeight(card.title, cell - 2 * gap));
                    Put(card.title.rectTransform, 0, top, cell - 2 * gap, titleHeight); top += titleHeight + gap;
                    float icon = font * 3, textWidth = cell - 3 * gap - icon;
                    Put(card.icon.rectTransform, -cell * .5f + gap + icon * .5f, top, icon, icon);
                    float contentsHeight = Mathf.Max(icon, DialoguePanel.PreferredHeight(card.contents, textWidth));
                    Put(card.contents.rectTransform, (icon + gap) * .5f, top, textWidth, contentsHeight);
                    card.price.fontSize = Mathf.RoundToInt(font * .9f);
                    float priceWidth = (cell - 3 * gap) * .32f, buyWidth = cell - 3 * gap - priceWidth;
                    Put(card.price.rectTransform, -cell * .5f + gap + priceWidth * .5f, cardHeight - button - gap, priceWidth, button);
                    Action(card.purchase, cell * .5f - gap - buyWidth * .5f, cardHeight - button - gap, buyWidth, button, font * .9f);
                }
                Content(ui.productScroll.content, Mathf.Max(h - y, Mathf.Ceil(cards.Length / (float)columns) * (cardHeight + gap)));
            }
            else
            {
                float footer = 2 * button + gap;
                Put((RectTransform)ui.confirmationScroll.transform, 0, 0, w, Mathf.Max(button, h - footer - gap)); Stretch(ui.confirmationScroll.viewport);
                float icon = font * 3; Put(ui.confirmIcon.rectTransform, -w * .5f + icon * .5f + gap, gap, icon, icon);
                ui.confirmTitle.fontSize = Mathf.RoundToInt(font * 1.2f);
                float titleHeight = Mathf.Max(icon, DialoguePanel.PreferredHeight(ui.confirmTitle, w - icon - 3 * gap));
                Put(ui.confirmTitle.rectTransform, (icon + gap) * .5f, gap, w - icon - 3 * gap, titleHeight);
                float y = titleHeight + 2 * gap;
                y += Paragraph(ui.confirmContents, y, w - 2 * gap, font) + gap;
                y += Paragraph(ui.confirmPrice, y, w - 2 * gap, font) + gap;
                y += Paragraph(ui.confirmNotice, y, w - 2 * gap, font * .9f) + gap;
                Content(ui.confirmationScroll.content, Mathf.Max(h - footer - gap, y));
                Action(ui.confirmPurchase, 0, h - footer, w, button, font);
                Action(ui.confirmBack, 0, h - button, w, button, font);
            }
        }
        private static float Paragraph(Text text, float top, float width, float font)
        { text.fontSize = Mathf.RoundToInt(font); float h = Mathf.Max(font * 1.4f, DialoguePanel.PreferredHeight(text, width)); Put(text.rectTransform, 0, top, width, h); return h; }
        private static void Action(Button button, float x, float top, float width, float height, float font)
        { DialoguePanel.FitCaption(button, font); Put((RectTransform)button.transform, x, top, width, height); }
        private static void Content(RectTransform content, float height)
        { content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); content.localScale = Vector3.one; content.sizeDelta = new Vector2(0, height); }
        private static void Put(RectTransform rect, float x, float top, float width, float height)
        { DialoguePanel.Place(rect, new Vector2(.5f, 1), new Vector2(x, -top - height * .5f), new Vector2(width, height)); }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}

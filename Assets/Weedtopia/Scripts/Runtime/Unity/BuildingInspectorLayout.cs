using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Fits authored building controls in display units; the primary action stays outside the scroll area.</summary>
    public sealed class BuildingInspectorLayout : MonoBehaviour
    {
        public BuildingInteractionUi ui;
        public ScrollRect scroll;
        public Text metricsHeading, previewHeading;
        private float font, gap, button;

        public void Fit(RectTransform safe, float pixels)
        {
            font = Mathf.Max(26, 18 / pixels);
            gap = Mathf.Max(16, 8 / pixels);
            button = Mathf.Max(72, 48 / pixels);
            float margin = Mathf.Max(24, 12 / pixels);
            bool compact = safe.rect.height * pixels < 600 || safe.rect.width * pixels < 700;
            float width = compact ? safe.rect.width - 2 * margin : Mathf.Min(660, safe.rect.width - 2 * margin);
            float height = safe.rect.height - 2 * margin;
            var panel = (RectTransform)transform;
            panel.localScale = Vector3.one;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0, 1);
            panel.anchoredPosition = new Vector2(margin, -margin);
            panel.sizeDelta = new Vector2(width, height);
            float inner = width - 2 * gap;
            ui.heading.fontSize = Mathf.RoundToInt(font * 1.15f);
            float close = button;
            float headingHeight = Mathf.Max(close, DialoguePanel.PreferredHeight(ui.heading, inner - close - gap));
            Put(ui.heading.rectTransform, -close * .5f - gap * .5f, gap, inner - close - gap, headingHeight);
            DialoguePanel.Place((RectTransform)ui.closeBuilding.transform, Vector2.one,
                new Vector2(-gap - close * .5f, -gap - close * .5f), Vector2.one * close);
            var visual = ui.closeBuilding.GetComponent<CloseButtonVisual>();
            if (visual != null) { visual.inset = Mathf.Max(4, 4 / pixels); visual.Fit(); }
            ui.closeBuilding.transform.SetAsLastSibling();

            var footer = new[] { ui.work, ui.talk }.Where(a => a.gameObject.activeSelf).ToArray();
            float footerHeight = footer.Length == 0 ? 0 : button + gap;
            bool horizontalActions = inner * pixels >= 660 && ui.upgrade.gameObject.activeSelf && footer.Length > 0;
            if (horizontalActions)
            {
                float primaryWidth = (inner - footer.Length * gap) * (footer.Length == 1 ? .68f : .55f);
                float otherWidth = (inner - footer.Length * gap - primaryWidth) / footer.Length;
                Footer(ui.upgrade, -inner * .5f + primaryWidth * .5f, gap, primaryWidth, button);
                for (int i = 0; i < footer.Length; i++)
                    Footer(footer[i], -inner * .5f + primaryWidth + gap + otherWidth * .5f + i * (otherWidth + gap), gap, otherWidth, button);
            }
            else if (ui.upgrade.gameObject.activeSelf)
            {
                Footer(ui.upgrade, 0, gap + footerHeight, inner, button);
                footerHeight += button + gap;
            }
            for (int i = 0; !horizontalActions && i < footer.Length; i++)
            {
                float cell = (inner - (footer.Length - 1) * gap) / footer.Length;
                Footer(footer[i], -inner * .5f + cell * .5f + i * (cell + gap), gap, cell, button);
            }
            float top = headingHeight + 2 * gap;
            float available = Mathf.Max(button, height - top - footerHeight - gap);
            Put((RectTransform)scroll.transform, 0, top, inner, available);
            Stretch(scroll.viewport);
            var content = scroll.content;
            content.localScale = Vector3.one;
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = new Vector2(0, content.sizeDelta.y);

            // Landscape phones use both columns; narrow screens keep the same controls in one scrollable column.
            bool columns = inner * pixels >= 660;
            float columnWidth = columns ? (inner - 2 * gap) * .5f : inner;
            float left = columns ? -(columnWidth + 2 * gap) * .5f : 0;
            float right = -left;
            float y = Paragraph(ui.status, left, 0, columnWidth);
            metricsHeading.fontSize = previewHeading.fontSize = Mathf.RoundToInt(font * .8f);
            float metricHeadingHeight = Mathf.Max(font * 1.5f, Mathf.Max(DialoguePanel.PreferredHeight(metricsHeading, columnWidth * .6f), DialoguePanel.PreferredHeight(previewHeading, columnWidth * .4f)));
            Put(metricsHeading.rectTransform, left - columnWidth * .2f, y, columnWidth * .6f, metricHeadingHeight);
            Put(previewHeading.rectTransform, left + columnWidth * .3f, y, columnWidth * .4f, metricHeadingHeight);
            y += metricHeadingHeight + gap;
            for (int i = 0; i < ui.metricLabels.Length; i++)
            {
                if (!ui.metricLabels[i].transform.parent.gameObject.activeSelf) continue;
                var row = (RectTransform)ui.metricLabels[i].transform.parent;
                foreach (var label in new[] { ui.metricLabels[i], ui.metricCurrent[i], ui.metricNext[i] }) label.fontSize = Mathf.RoundToInt(font);
                float h;
                if (columns)
                {
                    h = Mathf.Max(font * 1.5f, DialoguePanel.PreferredHeight(ui.metricLabels[i], columnWidth * .44f));
                    Put(row, left, y, columnWidth, h);
                    Cell(ui.metricLabels[i].rectTransform, -.28f, .44f, columnWidth, h);
                    Cell(ui.metricCurrent[i].rectTransform, .05f, .22f, columnWidth, h);
                    ui.metricCurrent[i].alignment = TextAnchor.MiddleRight;
                    Cell(ui.metricNext[i].rectTransform, .38f, .24f, columnWidth, h);
                    DialoguePanel.Place((RectTransform)ui.metricArrows[i].transform, new Vector2(.5f, .5f),
                        new Vector2(columnWidth * .23f, 0), Vector2.one * font);
                }
                else
                {
                    float labelHeight = Mathf.Max(font * 1.5f, DialoguePanel.PreferredHeight(ui.metricLabels[i], columnWidth));
                    float valueHeight = Mathf.Max(font * 1.5f, Mathf.Max(DialoguePanel.PreferredHeight(ui.metricCurrent[i], columnWidth * .4f), DialoguePanel.PreferredHeight(ui.metricNext[i], columnWidth * .4f)));
                    float valueTop = labelHeight + gap * .25f;
                    h = valueTop + valueHeight;
                    Put(row, left, y, columnWidth, h);
                    Put(ui.metricLabels[i].rectTransform, 0, 0, columnWidth, labelHeight);
                    Put(ui.metricCurrent[i].rectTransform, -columnWidth * .3f, valueTop, columnWidth * .4f, valueHeight);
                    ui.metricCurrent[i].alignment = TextAnchor.MiddleLeft;
                    Put(ui.metricNext[i].rectTransform, columnWidth * .3f, valueTop, columnWidth * .4f, valueHeight);
                    DialoguePanel.Place((RectTransform)ui.metricArrows[i].transform, new Vector2(.5f, 1),
                        new Vector2(0, -valueTop - valueHeight * .5f), Vector2.one * font);
                }
                y += h + gap * .5f;
            }
            float detailsHeight = y;
            y = columns ? 0 : y + gap;
            float portrait = font * 4;
            Put(ui.staffPortrait.rectTransform, right - columnWidth * .5f + portrait * .5f, y, portrait, portrait);
            DialoguePanel.Place((RectTransform)ui.staffAction.transform, new Vector2(.5f, 1),
                new Vector2(right + columnWidth * .5f - button * .5f, -y - button * .5f), Vector2.one * button);
            DialoguePanel.Place((RectTransform)ui.staffMark.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.one * font * 1.4f);
            float nameWidth = columnWidth - portrait - button - gap;
            ui.staffName.fontSize = Mathf.RoundToInt(font);
            if (nameWidth < font * 13)
            {
                y += portrait + gap;
                y = Paragraph(ui.staffName, right, y, columnWidth);
            }
            else
            {
                float nameHeight = Mathf.Max(portrait, DialoguePanel.PreferredHeight(ui.staffName, nameWidth));
                Put(ui.staffName.rectTransform, right + (portrait - button) * .5f, y, nameWidth, nameHeight);
                y += nameHeight + gap;
            }
            y = Paragraph(ui.staffBody, right, y, columnWidth);
            var training = new[] { ui.train, ui.rank }.Where(a => a.gameObject.activeSelf).ToArray();
            float trainingHeight = button * 1.2f;
            foreach (var action in training)
            {
                FitAction(action);
                float cell = (columnWidth - (training.Length - 1) * gap) / training.Length;
                trainingHeight = Mathf.Max(trainingHeight, DialoguePanel.PreferredHeight(action.GetComponentInChildren<Text>(), cell - font - 3 * gap) + 8);
            }
            for (int i = 0; i < training.Length; i++)
            {
                float cell = (columnWidth - (training.Length - 1) * gap) / training.Length;
                Action(training[i], right - columnWidth * .5f + cell * .5f + i * (cell + gap), y, cell, trainingHeight);
            }
            if (training.Length > 0) y += trainingHeight + gap;
            if (ui.fleet.gameObject.activeSelf) { Action(ui.fleet, right, y, columnWidth, button); y += button + gap; }
            content.sizeDelta = new Vector2(0, Mathf.Max(available, Mathf.Max(detailsHeight, y) + gap));
        }
        private float Paragraph(Text label, float x, float y, float width)
        {
            label.fontSize = Mathf.RoundToInt(font);
            float h = Mathf.Max(font * 1.5f, DialoguePanel.PreferredHeight(label, width));
            Put(label.rectTransform, x, y, width, h); return y + h + gap;
        }
        private void Action(Button action, float x, float y, float width, float height)
        { Put((RectTransform)action.transform, x, y, width, height); FitAction(action); }
        private void Footer(Button action, float x, float bottom, float width, float height)
        {
            DialoguePanel.Place((RectTransform)action.transform, new Vector2(.5f, 0), new Vector2(x, bottom + height * .5f), new Vector2(width, height));
            FitAction(action);
        }
        private void FitAction(Button action)
        {
            DialoguePanel.FitCaption(action, font);
            var icon = action.transform.Find("Icon") as RectTransform;
            if (icon == null) return;
            DialoguePanel.Place(icon, new Vector2(0, .5f), new Vector2(gap + font * .5f, 0), Vector2.one * font);
            action.GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(font + 2 * gap, 4);
        }
        private static void Cell(RectTransform rect, float center, float fraction, float width, float height)
        { DialoguePanel.Place(rect, new Vector2(.5f, .5f), new Vector2(center * width, 0), new Vector2(fraction * width, height)); }
        private static void Put(RectTransform rect, float x, float top, float width, float height)
        { DialoguePanel.Place(rect, new Vector2(.5f, 1), new Vector2(x, -top - height * .5f), new Vector2(width, height)); }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}

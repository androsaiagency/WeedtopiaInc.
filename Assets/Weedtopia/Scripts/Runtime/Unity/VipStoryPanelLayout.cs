using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>An explicit roster/detail route with fixed offer actions and independently scrolling story content.</summary>
    public sealed class VipStoryPanelLayout : MonoBehaviour
    {
        public VipStoriesUi ui;
        public ModalFrame frame;
        public Button back;
        public void Fit(float pixels)
        {
            float font = Mathf.Max(26, 18 / pixels), gap = Mathf.Max(18, 10 / pixels), button = Mathf.Max(78, 48 / pixels);
            float width = frame.viewport.rect.width, height = frame.viewport.rect.height;
            var body = frame.body;
            // Each route has its own scroll view. The former whole-window scroller must not retain momentum or move the fixed actions.
            var outer = GetComponent<ModulePanelLayout>().scroll;
            outer.StopMovement(); outer.horizontal = outer.vertical = false;
            body.localScale = Vector3.one; body.anchorMin = new Vector2(0, 1); body.anchorMax = Vector2.one; body.pivot = new Vector2(.5f, 1);
            body.sizeDelta = new Vector2(0, height); body.anchoredPosition = Vector2.zero;
            bool details = ui.ShowingDetails;
            ui.rosterScroll.gameObject.SetActive(!details); ui.detailScroll.gameObject.SetActive(details);
            ui.peopleTab.gameObject.SetActive(!details); ui.togetherTab.gameObject.SetActive(!details);
            back.gameObject.SetActive(details);
            float cell = (width - gap) * .5f;
            Action(ui.peopleTab, -(cell + gap) * .5f, 0, cell, button, font);
            Action(ui.togetherTab, (cell + gap) * .5f, 0, cell, button, font);
            float top = details ? 0 : button + gap;
            ui.milestone.gameObject.SetActive(!details);
            if (!details)
            {
                ui.milestone.fontSize = Mathf.RoundToInt(font * .9f);
                float h = Mathf.Max(font * 1.5f, DialoguePanel.PreferredHeight(ui.milestone, width));
                Put(ui.milestone.rectTransform, 0, top, width, h); top += h + gap;
                Put((RectTransform)ui.rosterScroll.transform, 0, top, width, height - top);
                Stretch(ui.rosterScroll.viewport);
                foreach (var row in ui.rows)
                {
                    float icon = font * 3.5f;
                    float textWidth = width - icon - 4 * gap;
                    row.caption.fontSize = Mathf.RoundToInt(font); row.status.fontSize = Mathf.RoundToInt(font * .9f);
                    float captionHeight = Mathf.Max(font * 2.5f, DialoguePanel.PreferredHeight(row.caption, textWidth));
                    float statusHeight = Mathf.Max(font * 1.8f, DialoguePanel.PreferredHeight(row.status, textWidth));
                    float rowHeight = Mathf.Max(font * 5, captionHeight + statusHeight + gap);
                    row.GetComponent<LayoutElement>().preferredHeight = rowHeight;
                    DialoguePanel.Place(row.accent.rectTransform, new Vector2(0, .5f), new Vector2(2, 0), new Vector2(4, rowHeight - gap));
                    DialoguePanel.Place((RectTransform)row.portrait.transform.parent, new Vector2(0, .5f), new Vector2(icon * .5f + gap, 0), new Vector2(icon, rowHeight - gap));
                    DialoguePanel.Place(row.caption.rectTransform, new Vector2(1, 1), new Vector2(-textWidth * .5f - gap, -gap * .5f - captionHeight * .5f), new Vector2(textWidth, captionHeight));
                    DialoguePanel.Place(row.status.rectTransform, new Vector2(1, 1), new Vector2(-textWidth * .5f - gap, -gap * .5f - captionHeight - statusHeight * .5f), new Vector2(textWidth, statusHeight));
                }
            }
            else
            {
                Action(back, 0, top, width, button, font); top += button + gap;
                float bottom = height;
                var primary = new[] { ui.accept, ui.alternate, ui.claim }.Where(a => a.gameObject.activeSelf).ToArray();
                bottom = Actions(primary, bottom, width, button, gap, font);
                Put((RectTransform)ui.detailScroll.transform, 0, top, width, Mathf.Max(font * 2, bottom - top));
                Stretch(ui.detailScroll.viewport);
                foreach (var text in ui.detailScroll.content.GetComponentsInChildren<Text>(true)) text.fontSize = Mathf.RoundToInt(font);
                ui.nameLabel.fontSize = ui.questTitle.fontSize = Mathf.RoundToInt(font * 1.15f);
                ui.portrait.GetComponent<LayoutElement>().preferredHeight = font * 4;
                foreach (var goal in ui.goals)
                {
                    DialoguePanel.FitCaption(goal, font);
                    goal.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(button, DialoguePanel.PreferredHeight(goal.GetComponentInChildren<Text>(), width - 48) + 24);
                }
                foreach (var action in new[] { ui.speak, ui.focus, ui.remember })
                { DialoguePanel.FitCaption(action, font); action.GetComponent<LayoutElement>().preferredHeight = button; }
                var flow = ui.detailScroll.content.GetComponent<VerticalLayoutGroup>();
                flow.spacing = gap; flow.padding = new RectOffset(Mathf.CeilToInt(gap), Mathf.CeilToInt(gap), Mathf.CeilToInt(gap), Mathf.CeilToInt(gap));
            }
            // Actions belong to the detail route, including locked residents and capacity-limited offers.
            foreach (var action in new[] { ui.accept, ui.alternate, ui.claim, ui.speak, ui.focus, ui.remember })
            {
                var group = action.GetComponent<CanvasGroup>();
                group.alpha = details ? 1 : 0; group.interactable = group.blocksRaycasts = details;
            }
        }
        private static float Actions(Button[] actions, float bottom, float width, float height, float gap, float font)
        {
            if (actions.Length == 0) return bottom;
            float cell = (width - (actions.Length - 1) * gap) / actions.Length;
            foreach (var action in actions) DialoguePanel.FitCaption(action, font);
            foreach (var action in actions) height = Mathf.Max(height, DialoguePanel.PreferredHeight(action.GetComponentInChildren<Text>(), cell - 48) + 24);
            bottom -= height;
            for (int i = 0; i < actions.Length; i++) Action(actions[i], -width * .5f + cell * .5f + i * (cell + gap), bottom, cell, height, font);
            return bottom - gap;
        }
        private static void Action(Button action, float x, float top, float width, float height, float font)
        { DialoguePanel.FitCaption(action, font); Put((RectTransform)action.transform, x, top, width, height); }
        private static void Put(RectTransform rect, float x, float top, float width, float height)
        { DialoguePanel.Place(rect, new Vector2(.5f, 1), new Vector2(x, -top - height * .5f), new Vector2(width, height)); }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}

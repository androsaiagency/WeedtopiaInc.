using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Reflows the existing confirmation controls at readable display sizes without scaling their authored body.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(ModalFrame))]
    public sealed class PremiumConfirmationLayout : MonoBehaviour
    {
        public ModalFrame frame;
        public Text message;
        public Button cancel, confirm;

        public float PreferredHeight(float availableWidth, float pixels)
        {
            float width = ContentWidth(availableWidth, pixels), font = BodyFont(width, pixels), gap = Gap(pixels);
            PrepareMessage(font);
            float messageHeight = Mathf.Max(font * 1.5f, DialoguePanel.PreferredHeight(message, width));
            bool stacked = width * pixels < 560;
            float cell = stacked ? width : (width - gap) * .5f;
            float cancelHeight = ActionHeight(cancel, cell, pixels), confirmHeight = ActionHeight(confirm, cell, pixels);
            float actions = stacked ? cancelHeight + confirmHeight + gap : Mathf.Max(cancelHeight, confirmHeight);
            return messageHeight + gap * 1.5f + actions + 2 * Padding(pixels);
        }

        public void Fit(float pixels)
        {
            var body = frame.body;
            body.anchorMin = Vector2.zero; body.anchorMax = Vector2.one;
            body.pivot = new Vector2(.5f, .5f); body.offsetMin = body.offsetMax = Vector2.zero;
            body.localScale = Vector3.one;
            float width = ContentWidth(frame.viewport.rect.width, pixels), font = BodyFont(width, pixels), gap = Gap(pixels);
            PrepareMessage(font);
            float messageHeight = Mathf.Max(font * 1.5f, DialoguePanel.PreferredHeight(message, width));
            bool stacked = width * pixels < 560;
            float cell = stacked ? width : (width - gap) * .5f;
            float cancelHeight = ActionHeight(cancel, cell, pixels), confirmHeight = ActionHeight(confirm, cell, pixels);
            float actions = stacked ? cancelHeight + confirmHeight + gap : Mathf.Max(cancelHeight, confirmHeight);
            float total = messageHeight + gap * 1.5f + actions;
            float top = Mathf.Max(Padding(pixels), (body.rect.height - total) * .5f);
            Put(message.rectTransform, 0, top, width, messageHeight);
            top += messageHeight + gap * 1.5f;
            if (stacked)
            {
                Put((RectTransform)cancel.transform, 0, top, width, cancelHeight);
                Put((RectTransform)confirm.transform, 0, top + cancelHeight + gap, width, confirmHeight);
            }
            else
            {
                float height = Mathf.Max(cancelHeight, confirmHeight);
                Put((RectTransform)cancel.transform, -(cell + gap) * .5f, top, cell, height);
                Put((RectTransform)confirm.transform, (cell + gap) * .5f, top, cell, height);
            }
            FitAction(cancel, pixels); FitAction(confirm, pixels);
        }

        private void PrepareMessage(float font)
        {
            message.fontSize = Mathf.CeilToInt(font); message.resizeTextForBestFit = false;
            message.alignment = TextAnchor.MiddleLeft;
            message.horizontalOverflow = HorizontalWrapMode.Wrap; message.verticalOverflow = VerticalWrapMode.Truncate;
        }
        private static float ActionHeight(Button button, float width, float pixels)
        {
            var caption = button.GetComponentInChildren<Text>(true);
            caption.fontSize = Mathf.CeilToInt(CaptionFont(width, pixels)); caption.resizeTextForBestFit = false;
            caption.horizontalOverflow = HorizontalWrapMode.Wrap; caption.verticalOverflow = VerticalWrapMode.Truncate;
            float minimum = Mathf.Max(80, 50 / pixels);
            float iconWidth = Icon(button) == null ? 0 : minimum * .8f;
            return Mathf.Max(minimum, DialoguePanel.PreferredHeight(caption, width - iconWidth - 24) + 16);
        }
        private static void FitAction(Button button, float pixels)
        {
            var rect = (RectTransform)button.transform;
            DialoguePanel.FitCaption(button, Mathf.CeilToInt(CaptionFont(rect.rect.width, pixels)));
            var caption = button.GetComponentInChildren<Text>(true);
            var icon = Icon(button);
            float inset = 12;
            if (icon != null)
            {
                float size = Mathf.Min(rect.rect.height * .48f, 30 / pixels);
                DialoguePanel.Place(icon, new Vector2(0, .5f), new Vector2(size * .5f + 12, 0), Vector2.one * size);
                inset += size + 12;
            }
            caption.rectTransform.offsetMin = new Vector2(inset, 8);
            caption.rectTransform.offsetMax = new Vector2(-12, -8);
        }
        private static RectTransform Icon(Button button) => (button.transform.Find("ActionIcon") ?? button.transform.Find("Icon")) as RectTransform;
        // Compact windows choose a smaller comfortable base size before the fixed readable display minimum applies.
        private static float BodyFont(float width, float pixels) => Mathf.Max(22 / pixels, Mathf.Min(34, width / 12));
        private static float CaptionFont(float width, float pixels) => Mathf.Max(20 / pixels, Mathf.Min(30, width / 12));
        private static float Padding(float pixels) => Mathf.Max(24, 12 / pixels);
        private static float Gap(float pixels) => Mathf.Max(20, 12 / pixels);
        private static float ContentWidth(float width, float pixels) => Mathf.Max(1, Mathf.Min(width - 2 * Padding(pixels), Mathf.Max(1200, 700 / pixels)));
        private static void Put(RectTransform rect, float x, float top, float width, float height)
            => DialoguePanel.Place(rect, new Vector2(.5f, 1), new Vector2(x, -top - height * .5f), new Vector2(width, height));
    }
}
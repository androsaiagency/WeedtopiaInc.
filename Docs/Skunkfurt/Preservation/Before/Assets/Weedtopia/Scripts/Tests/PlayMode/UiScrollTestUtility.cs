using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Tests.PlayMode
{
    internal static class UiScrollTestUtility
    {
        // Scroll the real authored viewport before raycasting a control below the fold.
        public static void Reveal(Button button)
        {
            foreach(var view in button.GetComponentsInParent<ScrollRect>())
            {
                if(view.content==null||view.viewport==null||!view.enabled||(!view.horizontal&&!view.vertical))continue;
                Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
                var center=rect.TransformPoint(rect.rect.center);var local=view.viewport.InverseTransformPoint(center);
                if(view.viewport.rect.Contains(local))continue;
                view.StopMovement();var delta=view.viewport.rect.center-(Vector2)local;
                var shift=view.content.parent.InverseTransformVector(view.viewport.TransformVector(new Vector3(view.horizontal?delta.x:0,view.vertical?delta.y:0,0)));
                view.content.anchoredPosition+=(Vector2)shift;view.verticalNormalizedPosition=Mathf.Clamp01(view.verticalNormalizedPosition);Canvas.ForceUpdateCanvases();
            }
        }
    }
}

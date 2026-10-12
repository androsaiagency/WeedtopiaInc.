using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class InventoryTileLayoutAssetTests
    {
        [TestCase(1920, 1080, 1f)]
        [TestCase(844, 390, .25f)]
        [TestCase(844, 390, .75f)]
        [TestCase(844, 390, .33f)]
        [TestCase(390, 844, .25f)]
        [TestCase(390, 844, 1f)]
        public void LongTranslatedTitlesHaveTheirOwnSpaceAndScrollableRowsKeepReadableGeometry(int width, int height, float pixels)
        {
            var setup = EditorSceneManager.GetSceneManagerSetup(); RectTransform safe = null;
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                safe = new GameObject("InventoryTileGeometrySafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
                safe.sizeDelta = new Vector2(width, height) / pixels;
                var root = Object.FindFirstObjectByType<GameRoot>();
                var layout = root.Ui.GetComponentInChildren<InventoryPanelLayout>(true);
                Assert.That(layout, Is.Not.Null); Assert.That(layout.view.tiles.Length, Is.GreaterThanOrEqualTo(8));
                layout.gameObject.SetActive(true);
                string[] titles =
                {
                    "Construction Time", "Tiempo de Construcción", "Temps de Construction",
                    "Baubeschleuniger für umfangreiche Bauarbeiten", "Ускоритель длительного строительства",
                    "A deliberately longer future item title with several translated words",
                    "Accélérateur de construction pour plusieurs nouveaux bâtiments", "Construction Time"
                };
                for (int i = 0; i < layout.view.tiles.Length; i++)
                {
                    var tile = layout.view.tiles[i]; tile.gameObject.SetActive(i < titles.Length);
                    if (i >= titles.Length) continue;
                    tile.title.text = titles[i]; tile.quantity.text = i == titles.Length - 1 ? "×1234567890" : "×2";
                    tile.quantityBadge.gameObject.SetActive(true);
                }
                layout.frame.Fit(safe, pixels); layout.Fit(pixels);
                Assert.That(layout.VisibleCapacity, Is.EqualTo(8), "The existing nominal eight-entry contract is preserved.");
                Assert.That(layout.grid.vertical, Is.True);
                foreach (var tile in layout.view.tiles.Take(titles.Length))
                {
                    var card = (RectTransform)tile.transform;
                    var icon = (RectTransform)tile.icon.transform.parent;
                    Assert.That(tile.title.fontSize * pixels, Is.GreaterThanOrEqualTo(14), tile.title.text);
                    Assert.That(tile.title.resizeTextForBestFit, Is.False);
                    Assert.That(card.localScale, Is.EqualTo(Vector3.one));
                    Assert.That(tile.title.rectTransform.rect.height + .1f,
                        Is.GreaterThanOrEqualTo(DialoguePanel.PreferredHeight(tile.title, tile.title.rectTransform.rect.width)), tile.title.text);
                    AssertInside(card, tile.title.rectTransform); AssertInside(card, icon); AssertInside(card, tile.quantityBadge.rectTransform);
                    AssertSeparate(card, tile.title.rectTransform, icon);
                    AssertSeparate(card, tile.title.rectTransform, tile.quantityBadge.rectTransform);
                    AssertSeparate(card, tile.title.rectTransform, tile.quantity.rectTransform);
                    AssertSeparate(card, icon, tile.quantityBadge.rectTransform);
                }
                var last = (RectTransform)layout.view.tiles[titles.Length - 1].transform;
                AssertInside(layout.grid.content, last);
                Assert.That(layout.grid.content.rect.height + .1f, Is.GreaterThanOrEqualTo(((RectTransform)layout.grid.transform).rect.height));
            }
            finally
            {
                if (safe != null) Object.DestroyImmediate(safe.gameObject);
                if (setup.Any(s => s.isLoaded)) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
        private static Rect Bounds(RectTransform parent, RectTransform child)
        {
            var corners = new Vector3[4]; child.GetWorldCorners(corners);
            var points = corners.Select(corner => parent.InverseTransformPoint(corner)).ToArray();
            return Rect.MinMaxRect(points.Min(point => point.x), points.Min(point => point.y), points.Max(point => point.x), points.Max(point => point.y));
        }
        private static void AssertInside(RectTransform parent, RectTransform child)
        {
            var bounds = Bounds(parent, child);
            Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(parent.rect.xMin - .1f), child.name);
            Assert.That(bounds.xMax, Is.LessThanOrEqualTo(parent.rect.xMax + .1f), child.name);
            Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(parent.rect.yMin - .1f), child.name);
            Assert.That(bounds.yMax, Is.LessThanOrEqualTo(parent.rect.yMax + .1f), child.name);
        }
        private static void AssertSeparate(RectTransform parent, RectTransform first, RectTransform second)
            => Assert.That(Bounds(parent, first).Overlaps(Bounds(parent, second)), Is.False, first.name + " overlaps " + second.name);
    }
}
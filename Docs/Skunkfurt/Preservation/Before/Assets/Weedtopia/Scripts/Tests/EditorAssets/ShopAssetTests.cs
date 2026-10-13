using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class ShopAssetTests
    {
        [Test] public void AllCitiesShareAuthoredShopPrefabsAndTenRoundNavigationButtons()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var path in new[] { GameScaffold.GameScene, BigRockCityAuthoring.ScenePath, SevenHillsAuthoring.ScenePath })
                {
                    EditorSceneManager.OpenScene(path); var root = Object.FindFirstObjectByType<GameRoot>();
                    var shop = root.Ui.Shop; Assert.That(shop, Is.Not.Null);
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(shop.gameObject), Is.EqualTo(ShopAuthoring.PanelPath));
                    Assert.That(shop.cards.Length, Is.EqualTo(17)); Assert.That(shop.cards.Select(c => c.productId).Distinct().Count(), Is.EqualTo(17));
                    foreach (var card in shop.cards) Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(card.gameObject), Is.EqualTo(ShopAuthoring.CardPath));
                    var navigation = root.Ui.GetComponent<WorldNavigationBar>(); Assert.That(navigation.Dock.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(10));
                    Assert.That(navigation.Dock.Find("Shop").GetComponent<RoundNavigationGraphic>(), Is.Not.Null);
                    Assert.That(shop.GetComponent<ModalFrame>().viewport.GetComponent<RectMask2D>(), Is.Not.Null);
                    Assert.That(shop.GetComponent<ShopPanelLayout>(), Is.Not.Null);
                    Assert.That(shop.productScroll.content, Is.Not.Null); Assert.That(shop.confirmationScroll.content, Is.Not.Null);
                }
            }
            finally { if (setup.Any(s => s.isLoaded)) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        [Test] public void ExplicitInstallerIsIdempotentAndRetainsManualUiAndWorldBytes()
        {
            var files = new[] { GameScaffold.GameScene, BigRockCityAuthoring.ScenePath, WorldNavigationAuthoring.DockPath, ShopAuthoring.PanelPath, ShopAuthoring.CardPath };
            var before = files.ToDictionary(path => path, File.ReadAllBytes); ShopAuthoring.Apply();
            foreach (var entry in before) Assert.That(File.ReadAllBytes(entry.Key), Is.EqualTo(entry.Value), entry.Key);
        }
        [Test] public void PolishingShopPreservesDedicatedLayoutAndEveryAuthoredByte()
        {
            var before=File.ReadAllBytes(ShopAuthoring.PanelPath);
            AssertDedicatedShopLayout();
            UiPolishAuthoring.Apply();
            Assert.That(File.ReadAllBytes(ShopAuthoring.PanelPath).SequenceEqual(before),Is.True,
                "Polishing an installed ShopPanel must preserve its complete existing file, without normalization.");
            AssertDedicatedShopLayout();
        }
        private static void AssertDedicatedShopLayout()
        {
            var contents=PrefabUtility.LoadPrefabContents(ShopAuthoring.PanelPath);
            try
            {
                var frame=contents.GetComponent<ModalFrame>();var shop=contents.GetComponent<ShopUi>();var layout=contents.GetComponent<ShopPanelLayout>();
                Assert.That(frame,Is.Not.Null);Assert.That(shop,Is.Not.Null);Assert.That(layout,Is.Not.Null);
                Assert.That(layout.frame,Is.SameAs(frame));Assert.That(layout.ui,Is.SameAs(shop));
                Assert.That(contents.GetComponent<ModulePanelLayout>(),Is.Null,
                    "An authored ShopPanel has one dedicated layout owner; generic module layout must not be attached.");
                Assert.That(frame.viewport.GetComponent<ScrollRect>(),Is.Null,
                    "The outer modal viewport must not gain a third scroll that competes with catalog/confirmation input.");
                var scrolls=contents.GetComponentsInChildren<ScrollRect>(true);
                Assert.That(scrolls.Length,Is.EqualTo(2));Assert.That(scrolls,Is.EquivalentTo(new[]{shop.productScroll,shop.confirmationScroll}));
                Assert.That(shop.productScroll.content,Is.Not.Null);Assert.That(shop.confirmationScroll.content,Is.Not.Null);
                Assert.That(shop.productScroll.content,Is.Not.SameAs(shop.confirmationScroll.content));
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
        }
    }
}

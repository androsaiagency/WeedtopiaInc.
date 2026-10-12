using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class ShopUiTests
    {
        private GameRoot root;
        private string folder;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");
            root = UnityEngine.Object.FindFirstObjectByType<GameRoot>(); root.enabled = false;
            folder = Path.Combine(UnityEngine.Application.temporaryCachePath, "ShopUi_" + Guid.NewGuid().ToString("N"));
            root.UseTestStore(Path.Combine(folder, "save.json"), new ManualClock(new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc)));
            root.NewGame("Shopprüfung"); root.Session.CompleteIntro(); root.Ui.Close(); root.Refresh(); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if (Directory.Exists(folder)) Directory.Delete(folder, true); yield return null; }
        [UnityTest] public IEnumerator NavigationOpensShopAndEveryProductIsAccessibleThroughItsCategory()
        {
            root.Ui.Element<Button>("shopDock").onClick.Invoke(); yield return null;
            var shop = root.Ui.Shop; Assert.That(shop.gameObject.activeInHierarchy, Is.True);
            Assert.That(shop.mode.text, Does.Contain("Keine echten Zahlungen"));
            Assert.That(shop.cards.Select(c => c.productId).Distinct().Count(), Is.EqualTo(17));
            foreach (ShopCategory category in Enum.GetValues(typeof(ShopCategory)))
            {
                shop.SelectCategory(category); yield return null;
                Assert.That(shop.cards.Where(c => c.gameObject.activeSelf).Select(c => c.productId),
                    Is.EquivalentTo(root.Definitions.shopProducts.Where(p => p.category == category).Select(p => p.id)));
            }
            root.Ui.Element<Button>("ui.close").onClick.Invoke(); yield return null;
            Assert.That(root.Ui.GetComponent<WorldNavigationBar>().Dock.gameObject.activeInHierarchy, Is.True);
            Assert.That(shop.gameObject.activeInHierarchy, Is.False); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PreviewAndCancellationDoNotGrantAndConfirmedStarterSurvivesContinue()
        {
            root.Ui.ShowShop(); var shop = root.Ui.Shop;
            shop.Preview("weedtopia.starter"); yield return null;
            Assert.That(shop.confirmation.gameObject.activeInHierarchy, Is.True);
            Assert.That(shop.confirmNotice.text, Does.Contain("kein Echtgeld"));
            Assert.That(root.Session.State.money, Is.EqualTo(new Amount(1000)));
            shop.confirmBack.onClick.Invoke(); yield return null;
            Assert.That(shop.Confirming, Is.False); Assert.That(root.Session.State.inventory, Is.Empty);
            shop.Preview("weedtopia.starter"); shop.confirmPurchase.onClick.Invoke(); yield return null;
            Assert.That(root.Session.State.money, Is.EqualTo(new Amount(11000)));
            Assert.That(root.Session.State.grinders, Is.EqualTo(250)); Assert.That(root.Session.Inventory.Count("build_5m"), Is.EqualTo(5));
            Assert.That(shop.status.text, Does.Contain("Erhalten")); Assert.That(shop.Confirming, Is.False);
            Assert.That(root.Continue(), Is.True); root.Ui.Close(); yield return null;
            Assert.That(root.Session.State.money, Is.EqualTo(new Amount(11000))); Assert.That(root.Session.Inventory.Count("build_5m"), Is.EqualTo(5));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator VipConfirmationGrantsFirstDayAndShowsTheSavedExpiration()
        {
            root.Ui.ShowShop(); var shop = root.Ui.Shop; shop.SelectCategory(ShopCategory.Vip); shop.Preview("weedtopia.vip_30d");
            shop.confirmPurchase.onClick.Invoke(); yield return null;
            Assert.That(root.Session.Shop.VipActive, Is.True); Assert.That(root.Session.State.grinders, Is.EqualTo(100));
            shop.SelectCategory(ShopCategory.Vip); yield return null;
            Assert.That(shop.status.text, Does.Contain("VIP aktiv bis"));
            Assert.That(shop.cards.Single(c => c.productId == "weedtopia.vip_30d").contents.text, Does.Contain("+50 %"));
            LogAssert.NoUnexpectedReceived();
        }
    }
}

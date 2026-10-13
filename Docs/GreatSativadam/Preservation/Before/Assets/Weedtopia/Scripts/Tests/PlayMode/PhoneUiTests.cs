using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class PhoneUiTests
    {
        private GameRoot root;
        private string folder;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");
            root = UnityEngine.Object.FindFirstObjectByType<GameRoot>(); root.enabled = false;
            folder = Path.Combine(UnityEngine.Application.temporaryCachePath, "PhoneUi_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            root.UseTestStore(Path.Combine(folder, "save.json"), new ManualClock(new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc)));
            root.NewGame("Handytest"); root.Session.CompleteIntro(); root.Ui.Close(); root.Session.State.City.unlocked = Zone.Industry; root.Refresh(); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup() { UiDisplayMetrics.EvidenceDensity = 0; if (Directory.Exists(folder)) Directory.Delete(folder, true); yield return null; }
        [UnityTest] public IEnumerator BuildingAndPickerHideHudAndClosingRestoresItsActions()
        {
            var ui = root.Ui.Interaction; var dock = root.Ui.GetComponent<WorldNavigationBar>().Dock;
            var header = root.Ui.GetComponentInChildren<CityStatusBar>(true);
            ui.OpenBuilding("farm_processing"); yield return null;
            Assert.That(dock.gameObject.activeInHierarchy || header.Settings.gameObject.activeInHierarchy || header.City.gameObject.activeInHierarchy || header.Resources.gameObject.activeInHierarchy, Is.False);
            ui.OpenPicker(0); yield return null; ui.closePicker.onClick.Invoke(); yield return null;
            Assert.That(ui.BuildingOpen, Is.True); Assert.That(header.Settings.gameObject.activeInHierarchy, Is.False);
            ui.closeBuilding.onClick.Invoke(); yield return null;
            Assert.That(dock.gameObject.activeInHierarchy && header.Settings.gameObject.activeInHierarchy && header.City.gameObject.activeInHierarchy && header.Resources.gameObject.activeInHierarchy, Is.True);
            root.Ui.Experience.ShowSettings(); yield return null; Assert.That(root.Ui.Element<RectTransform>("settings").gameObject.activeInHierarchy, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ToniRosterClickOpensVisibleOfferAndBackRestoresRosterWithoutAccepting()
        {
            var house = root.Session.State.Find("settlement_01"); house.family = "residential_house"; house.level = 1;
            var caretaker = root.Session.Workers.Grant("caretaker", 4); caretaker.level = 80; root.Session.Workers.Assign(caretaker.id, house.id, 0);
            root.Session.Stories.RefreshUnlocks(); root.Refresh(); root.Ui.ShowStories(); yield return null;
            var ui = root.Ui.Stories;
            Assert.That(ui.rosterScroll.gameObject.activeInHierarchy, Is.True); Assert.That(ui.ShowingDetails, Is.False);
            ui.rows.Single(r => r.id == "toni").button.onClick.Invoke(); yield return null;
            Assert.That(ui.ShowingDetails && ui.detailScroll.gameObject.activeInHierarchy, Is.True);
            Assert.That(ui.accept.GetComponent<CanvasGroup>().blocksRaycasts, Is.True); Assert.That(root.Session.Stories.Active("toni"), Is.Null);
            ui.GetComponent<VipStoryPanelLayout>().back.onClick.Invoke(); yield return null;
            Assert.That(ui.rosterScroll.gameObject.activeInHierarchy, Is.True); Assert.That(ui.accept.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            ui.rows.Single(r => r.id == "toni").button.onClick.Invoke(); ui.accept.onClick.Invoke(); yield return null;
            Assert.That(root.Session.Stories.Active("toni").questId, Is.EqualTo("toni_chapter_1")); Assert.That(ui.claim.interactable, Is.False);
            LogAssert.NoUnexpectedReceived();
        }
    }
}

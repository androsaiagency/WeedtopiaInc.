using System;
using System.Collections;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class PremiumConfirmationUiTests
    {
        private GameRoot root;
        private bool hadPreference;
        private string previousPreference, folder;
        [UnitySetUp] public IEnumerator Setup()
        {
            hadPreference = PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey);
            previousPreference = PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey);
            PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey, "de");
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");
            root = UnityEngine.Object.FindFirstObjectByType<GameRoot>(); root.enabled = false;
            folder = Path.Combine(UnityEngine.Application.temporaryCachePath, "PremiumUi_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            root.UseTestStore(Path.Combine(folder, "save.json"), new ManualClock(new DateTime(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc)));
            root.NewGame("Grinder-Sprachtest"); root.Session.CompleteIntro(); root.Ui.Close();
            root.Select("farm_grow_01"); root.Session.State.money = 0; root.Refresh(); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (root != null) { root.enabled = false; root.Ui.Close(); }
            if (hadPreference) PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey, previousPreference);
            else PlayerPrefs.DeleteKey(GameRoot.LanguagePreferenceKey);
            PlayerPrefs.Save();
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            yield return null;
        }
        [UnityTest] public IEnumerator OpenOfferRefreshesAllFiveLanguagesAndConfirmationStillSpendsOnlyOnce()
        {
            var building = root.Session.State.Find(root.SelectedId);
            int cost = Math.Max(1, (int)Math.Ceiling(root.Session.Upgrades.Cost(building).ToDouble() / root.Definitions.moneyPerGrinder));
            int before = root.Session.State.grinders;
            root.Ui.Element<Button>("ui.upgrade").onClick.Invoke(); yield return null;
            var panel = root.Ui.Element<RectTransform>("premium"); var frame = panel.GetComponent<ModalFrame>();
            Assert.That(panel.GetComponent<PremiumConfirmationLayout>(), Is.Not.Null);
            Assert.That(frame.headingSource.gameObject.activeSelf, Is.False, "The authored legacy title is an inactive source.");
            foreach (var language in root.Localization.Languages)
            {
                Assert.That(root.SetLanguage(language.Code), Is.True); yield return null;
                root.Ui.Experience.Fit(); Canvas.ForceUpdateCanvases();
                Assert.That(panel.gameObject.activeSelf, Is.True);
                Assert.That(frame.heading.text, Is.EqualTo(root.Definitions.Text("ui.premium_title")));
                string expected = string.Format(root.Localization.Culture ?? CultureInfo.InvariantCulture, root.Definitions.Text("ui.premium"), cost, root.Definitions.Text(root.Definitions.Building(building.family).nameKey));
                Assert.That(root.Ui.Element<Text>("premiumText").text, Is.EqualTo(expected));
                foreach (string id in new[] { "ui.cancel", "ui.confirm" })
                    Assert.That(root.Ui.Element<Button>(id).GetComponentInChildren<Text>(true).text, Is.EqualTo(root.Definitions.Text(id)));
                Assert.That(root.Session.State.grinders, Is.EqualTo(before), "Switching language must not spend a pending offer.");
            }
            root.Ui.Element<Button>("ui.confirm").onClick.Invoke();
            Assert.That(root.Session.State.grinders, Is.EqualTo(before - cost)); Assert.That(building.buildingSeconds, Is.GreaterThan(0));
            root.Ui.Element<Button>("ui.confirm").onClick.Invoke(); Assert.That(root.Session.State.grinders, Is.EqualTo(before - cost));
            yield return null; LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CancelAfterChangingLanguagePreservesCurrencyAndConstruction()
        {
            int grinders = root.Session.State.grinders;
            root.Ui.Element<Button>("ui.upgrade").onClick.Invoke(); Assert.That(root.SetLanguage("en"), Is.True); yield return null;
            root.Ui.Element<Button>("ui.cancel").onClick.Invoke();
            Assert.That(root.Session.State.grinders, Is.EqualTo(grinders));
            Assert.That(root.Session.State.Find(root.SelectedId).buildingSeconds, Is.Zero); Assert.That(root.Ui.ModalOpen, Is.False);
            yield return null; LogAssert.NoUnexpectedReceived();
        }
    }
}
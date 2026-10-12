using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class LocalizationFlowTests
    {
        private bool hadPreference;
        private string previousPreference,save;
        private GameRoot root;
        [UnitySetUp]public IEnumerator Setup()
        {
            hadPreference=PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey);previousPreference=PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey);
            PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey,"de");
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");
            root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();root.enabled=false;
            save=Path.Combine(UnityEngine.Application.temporaryCachePath,"Weedtopia_Localization_"+Guid.NewGuid().ToString("N")+".json");
            root.UseTestStore(save,new ManualClock(new DateTime(2026,10,4,0,0,0,DateTimeKind.Utc)));root.NewGame("Sprachtest");root.Session.CompleteIntro();root.Ui.Close();
            root.Ui.Experience.ShowSettings();yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(hadPreference)PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey,previousPreference);else PlayerPrefs.DeleteKey(GameRoot.LanguagePreferenceKey);PlayerPrefs.Save();
            foreach(var file in new[]{save,save+".bak",save+".scores"})if(File.Exists(file))File.Delete(file);yield return null;
        }
        [UnityTest]public IEnumerator SettingsDropdownSwitchesAllFiveLanguagesWithoutChangingProgress()
        {
            string state=JsonUtility.ToJson(root.Session.State);var experience=root.Ui.Experience;
            for(int i=0;i<root.Localization.Languages.Count;i++)
            {
                experience.language.value=i;yield return null;
                string code=root.Localization.Languages[i].Code;Assert.That(root.Localization.Language,Is.EqualTo(code));
                Assert.That(PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey),Is.EqualTo(code));
                Assert.That(experience.languageLabel.text,Is.EqualTo(root.Definitions.Text("settings.language")));
                Assert.That(root.Ui.Element<UnityEngine.UI.Text>("settingsTitle").text,Is.EqualTo(root.Definitions.Text("ui.pause")));
                Assert.That(root.Ui.Element<UnityEngine.UI.Text>("volumeLabel").text,Is.EqualTo(root.Definitions.Text("ui.volume")));
                Assert.That(root.Ui.Element<UnityEngine.UI.Text>("scaleLabel").text,Is.EqualTo(root.Definitions.Text("ui.scale")));
                Assert.That(experience.conversation.speaker.GetComponents<LocalizedLabel>(),Is.Empty,"Dynamic speaker belongs to its presenter");
                Assert.That(experience.conversation.body.GetComponents<LocalizedLabel>(),Is.Empty);
                experience.conversation.Present("rocko","greeting",root.Definitions.Text("speaker.rocko"),root.Definitions.Text("dialog.rocko"));root.Ui.ShowPanel("conversation");yield return null;
                Assert.That(experience.conversation.speaker.text,Is.EqualTo(root.Definitions.Text("speaker.rocko")));
                experience.ShowSettings();
                Assert.That(JsonUtility.ToJson(root.Session.State),Is.EqualTo(state));
            }
        }
        [UnityTest]public IEnumerator PersistedRussianPreferenceIsUsedInTheOtherCityAndTitle()
        {
            Assert.That(root.SetLanguage("ru"),Is.True);yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BigRockCity.unity");
            root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();root.enabled=false;
            Assert.That(root.Localization.Language,Is.EqualTo("ru"));Assert.That(root.Ui.Element<UnityEngine.UI.Button>("ui.start").GetComponentInChildren<UnityEngine.UI.Text>().text,Is.EqualTo("Играть"));
            Assert.That(root.Ui.Experience.language.options.Select(x=>x.text),Is.EqualTo(new[]{"Deutsch","English","Español","Français","Русский"}));
            Assert.That(root.SetLanguage("unsupported"),Is.False);Assert.That(root.Localization.Language,Is.EqualTo("ru"));yield return null;
        }
        [UnityTest]public IEnumerator LanguagePopupKeepsGermanAndTheLastLanguageReachableAtEveryInterfaceScale()
        {
            var experience=root.Ui.Experience;
            var scale=root.Ui.Element<Slider>("scale");
            foreach(float value in new[]{scale.minValue,1f,scale.maxValue})
            {
                root.Session.State.uiScale=value;root.SetLanguage("en");experience.ShowSettings();root.Refresh();
                yield return null;yield return null;Canvas.ForceUpdateCanvases();experience.Fit();
                experience.language.Show();yield return null;Canvas.ForceUpdateCanvases();
                var popup=experience.language.transform.Find("Dropdown List");
                Assert.That(popup,Is.Not.Null);
                var scroll=popup.GetComponent<ScrollRect>();
                AssertInside(experience.safeArea,scroll.viewport);
                var german=popup.GetComponentsInChildren<Toggle>().Single(t=>t.GetComponentInChildren<Text>().text=="Deutsch");
                AssertInside(scroll.viewport,(RectTransform)german.transform);
                ExecuteEvents.Execute(german.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(root.Localization.Language,Is.EqualTo("de"));
                Assert.That(PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey),Is.EqualTo("de"));

                experience.language.Show();yield return null;
                popup=experience.language.transform.Find("Dropdown List");scroll=popup.GetComponent<ScrollRect>();
                scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;
                var russian=popup.GetComponentsInChildren<Toggle>().Single(t=>t.GetComponentInChildren<Text>().text=="Русский");
                AssertInside(scroll.viewport,(RectTransform)russian.transform);
                ExecuteEvents.Execute(russian.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(root.Localization.Language,Is.EqualTo("ru"));
            }
        }
        private static void AssertInside(RectTransform parent,RectTransform child)
        {
            var corners=new Vector3[4];child.GetWorldCorners(corners);
            foreach(var point in corners)
            {
                var local=parent.InverseTransformPoint(point);
                Assert.That(local.x,Is.InRange(parent.rect.xMin-1,parent.rect.xMax+1),child.name+" horizontal bounds");
                Assert.That(local.y,Is.InRange(parent.rect.yMin-1,parent.rect.yMax+1),child.name+" vertical bounds");
            }
        }
        [UnityTest]public IEnumerator ExistingProfileFeedbackAndShopReceiptFollowLanguageChangesWithoutRepeatingCommands()
        {
            root.SetLanguage("en");var experience=root.Ui.Experience;
            experience.SelectSettingsTab(true);experience.profileName.text="x";experience.applyProfile.onClick.Invoke();
            string feedbackKey="presentation.uiexperience.bitte_gib_einen_namen_mit_2_24_zeichen_e.add3a32b";
            Assert.That(experience.profileFeedback.text,Is.EqualTo(root.Definitions.Text(feedbackKey)));
            string state=JsonUtility.ToJson(root.Session.State);
            root.Ui.Notify("Previous language notification");
            root.SetLanguage("de");yield return null;
            Assert.That(root.Ui.Element<Text>("message").text,Is.Empty);
            Assert.That(experience.profileFeedback.text,Is.EqualTo(root.Definitions.Text(feedbackKey)));
            Assert.That(JsonUtility.ToJson(root.Session.State),Is.EqualTo(state));

            root.SetLanguage("en");root.Ui.ShowShop();var shop=root.Ui.Shop;
            shop.Preview("weedtopia.starter");shop.confirmPurchase.onClick.Invoke();yield return null;
            Assert.That(shop.Confirming,Is.False);
            state=JsonUtility.ToJson(root.Session.State);
            foreach(string code in new[]{"de","fr","ru","en"})
            {
                root.SetLanguage(code);yield return null;
                Assert.That(shop.status.text,Does.StartWith(root.Definitions.Text("presentation.shopui.erhalten.21402251")));
                Assert.That(JsonUtility.ToJson(root.Session.State),Is.EqualTo(state),"Changing language must not repeat the purchase");
            }
        }
        [UnityTest]public IEnumerator InventoryDurationUsesLocalizedUnitsIncludingHoursMinutesAndSeconds()
        {
            foreach(var language in root.Localization.Languages)
            {
                root.SetLanguage(language.Code);
                Assert.That(InventoryText.Duration(3661),Is.EqualTo(root.Localization.Format("format.duration.hours",1)+" "+root.Localization.Format("format.duration.minutes",1)+" "+root.Localization.Format("format.duration.seconds",1)));
                Assert.That(InventoryText.Duration(60),Is.EqualTo(root.Localization.Format("format.duration.minutes",1)));
                Assert.That(InventoryText.Duration(-1),Is.EqualTo(root.Localization.Format("format.duration.seconds",0)));
            }
            yield return null;
        }
    }
}

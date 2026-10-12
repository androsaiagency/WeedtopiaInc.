using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class UiExperienceTests
    {
        private GameRoot root;
        private string folder,save;
        private UiExperience View=>root.Ui.Experience;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();root.enabled=false;
            folder=Path.Combine(UnityEngine.Application.temporaryCachePath,"UiExperience_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);save=Path.Combine(folder,"save.json");
            root.UseTestStore(save,new ManualClock(new DateTime(2026,10,3,12,0,0,DateTimeKind.Utc)));root.Ui.ShowStart();yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup(){if(Directory.Exists(folder))Directory.Delete(folder,true);yield return null;}
        private void Start(){root.NewGame("Fenstertest");root.Session.CompleteIntro();root.Ui.Close();root.Refresh();}
        private Button Hit(Button expected)
        {
            Canvas.ForceUpdateCanvases();var rect=(RectTransform)expected.transform;var data=new PointerEventData(EventSystem.current){position=rect.TransformPoint(rect.rect.center)};
            if(expected.name=="ui.close"){var visual=expected.GetComponent<CloseButtonVisual>();Assert.That(visual,Is.Not.Null);Assert.That(visual.caption.enabled,Is.False);Assert.That(visual.icon.enabled,Is.True);Assert.That(visual.icon.sprite,Is.EqualTo(root.Presentation.art.Get("close")));}
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Assert.That(hits.Count,Is.GreaterThan(0),expected.name);var button=hits[0].gameObject.GetComponentInParent<Button>();Assert.That(button,Is.EqualTo(expected),expected.name+" covered by "+hits[0].gameObject.name);return button;
        }
        [UnityTest] public IEnumerator OnePlayButtonStartsNamesAndResumesTheSavedGame()
        {
            Assert.That(View.logo.text,Is.EqualTo("WEEDTOPIA"));var start=root.Ui.Element<RectTransform>("start");Assert.That(start.GetComponentsInChildren<Button>().Length,Is.EqualTo(1));
            Hit(root.Ui.Element<Button>("ui.start")).onClick.Invoke();yield return null;
            Assert.That(root.Session.State.profileNamePending,Is.True);Assert.That(View.intro.nameGroup.gameObject.activeInHierarchy,Is.True);
            View.intro.nameInput.text="A";View.intro.next.onClick.Invoke();Assert.That(root.Session.State.profileNamePending,Is.True);
            View.intro.nameInput.text="Kaya";View.intro.next.onClick.Invoke();for(int i=0;i<3;i++)View.intro.tapScreen.onClick.Invoke();yield return null;
            Assert.That(root.Session.State.playerName,Is.EqualTo("Kaya"));Assert.That(root.Session.State.introSeen,Is.True);
            root.Session.State.money=2345;root.SaveSilently();root.Ui.ShowStart();root.Ui.Element<Button>("ui.start").onClick.Invoke();while(root.ContinueBusy)yield return null;root.Ui.Close();yield return null;
            Assert.That(root.Session.State.playerName,Is.EqualTo("Kaya"));Assert.That(root.Session.State.money,Is.EqualTo(new Amount(2345)));Assert.That(root.Session.State.profileNamePending,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ProfileTabChangesNameAndAvatarWithoutResettingTheTown()
        {
            Start();root.Session.State.money=4321;root.Session.State.Find("farm_grow_01").level=5;View.ShowSettings();View.SelectSettingsTab(true);yield return null;
            Hit(View.nextAvatar).onClick.Invoke();View.profileName.text="Neuer Name";Hit(View.applyProfile).onClick.Invoke();yield return null;
            Assert.That(root.Session.State.playerName,Is.EqualTo("Neuer Name"));Assert.That(root.Session.State.playerAvatar,Is.EqualTo("player_male_02"));Assert.That(View.profileFeedback.text,Does.Contain("gespeichert"));
            root.Ui.Close();root.Continue();root.Ui.Close();yield return null;
            Assert.That(root.Session.State.money,Is.EqualTo(new Amount(4321)));Assert.That(root.Session.State.Find("farm_grow_01").level,Is.EqualTo(5));Assert.That(root.Session.State.playerAvatar,Is.EqualTo("player_male_02"));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NewGameRequiresInGameConfirmationAndKeepsItsOwnArchive()
        {
            Start();root.Session.State.money=9876;View.ShowSettings();root.Ui.Element<Button>("ui.reset").onClick.Invoke();yield return null;
            Assert.That(root.Ui.Element<RectTransform>("newGameConfirm").gameObject.activeInHierarchy,Is.True);Hit(View.cancelRestart).onClick.Invoke();Assert.That(root.Session.State.money,Is.EqualTo(new Amount(9876)));
            root.Ui.Element<Button>("ui.reset").onClick.Invoke();yield return null;Hit(View.confirmRestart).onClick.Invoke();yield return null;
            Assert.That(root.Session.State.money,Is.EqualTo(new Amount(1000)));Assert.That(root.Session.State.profileNamePending,Is.True);Assert.That(Directory.GetFiles(folder,"*.before_restart_*.json").Length,Is.EqualTo(1));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator MilaUsesSpeechAndDifferentGesturesAboveTheBottomDialogueBox()
        {
            Start();root.Session.State.City.questIndex=1;root.Session.State.City.completedQuests.Add("farm_welcome");root.Select("farm_grow_01");root.Ui.SpeakToSelected();yield return null;
            Assert.That(View.conversation.SpeakerId,Is.EqualTo("mila"));Assert.That(View.conversation.Emotion,Is.EqualTo("explaining"));Assert.That(View.conversation.body.text,Does.Not.Contain("Sprich mit Mila"));var explaining=View.conversation.portrait.sprite;string previousSpeech=View.conversation.body.text;
            var box=new Vector3[4];var portrait=new Vector3[4];View.conversation.box.GetWorldCorners(box);View.conversation.portrait.rectTransform.GetWorldCorners(portrait);
            Assert.That(box[0].y,Is.LessThan(Screen.height*.15f));Assert.That(portrait[0].y,Is.GreaterThanOrEqualTo(box[1].y-8));
            root.Session.State.Find("farm_grow_01").level=1;root.Ui.SpeakToSelected();yield return null;Assert.That(View.conversation.Emotion,Is.EqualTo("pleased"));Assert.That(View.conversation.portrait.sprite,Is.Not.EqualTo(explaining));Assert.That(View.conversation.body.text,Is.Not.EqualTo(previousSpeech));Hit(View.conversation.tapScreen).onClick.Invoke();Assert.That(root.Ui.ModalOpen,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator WorkerPickerCloseDoesNotOpenSettingsAndGlobalMenusRemainReachable()
        {
            Start();root.Session.State.Find("farm_grow_01").level=1;root.Session.Workers.Grant("grow");root.Ui.Interaction.OpenBuilding("farm_grow_01");root.Ui.Interaction.OpenPicker(0);root.Refresh();yield return null;
            var header=root.Ui.GetComponentInChildren<CityStatusBar>(true);var settings=(RectTransform)header.Settings;var close=(RectTransform)root.Ui.Interaction.closePicker.transform;
            Assert.That(settings.gameObject.activeInHierarchy,Is.False,"The context window hides the global settings button");
            Hit(root.Ui.Interaction.closePicker).onClick.Invoke();Assert.That(root.Ui.Interaction.PickerOpen,Is.False);Assert.That(root.Ui.Element<RectTransform>("settings").gameObject.activeSelf,Is.False);
            Hit(root.Ui.Interaction.closeBuilding).onClick.Invoke();root.Refresh();yield return null;Hit(header.Settings.GetComponent<Button>()).onClick.Invoke();yield return null;
            Assert.That(root.Ui.Interaction.PickerOpen||root.Ui.Interaction.BuildingOpen,Is.False);Assert.That(root.Ui.Element<RectTransform>("settings").gameObject.activeSelf,Is.True);Hit(root.Ui.Element<Button>("ui.close")).onClick.Invoke();yield return null;
            Assert.That(root.Ui.ModalOpen,Is.False);Assert.That(root.Ui.GetComponent<WorldNavigationBar>().Dock.gameObject.activeInHierarchy,Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ModalCloseButtonsAreAboveTheirContentAndRestoreNavigation()
        {
            Start();root.Session.State.City.unlocked=Zone.Industry;
            foreach(string id in new[]{"workers","journal","settings","collection","luckyWheel","vipStories","cityCampaign","questPreview","questDetails","offline","newGameConfirm","townhall"})
            {
                root.Ui.ShowPanel(id);yield return null;Hit(root.Ui.Element<Button>("ui.close")).onClick.Invoke();yield return null;Assert.That(root.Ui.ModalOpen,Is.False,id);
            }
            root.Ui.ShowPanel("premium");yield return null;Hit(root.Ui.Element<Button>("ui.cancel")).onClick.Invoke();yield return null;Assert.That(root.Ui.ModalOpen,Is.False,"Premium confirmation must have a reachable cancel action");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RoundQuestShortcutOpensANextStepPreviewBeforeTheChecklist()
        {
            Start();root.Refresh();yield return null;
            var shortcut=View.quests.Buttons.First(b=>b.gameObject.activeInHierarchy);Assert.That(shortcut.targetGraphic,Is.TypeOf<RoundNavigationGraphic>());
            Hit(shortcut).onClick.Invoke();yield return null;
            Assert.That(View.quests.preview.gameObject.activeInHierarchy,Is.True);Assert.That(root.Ui.Element<RectTransform>("questDetails").gameObject.activeSelf,Is.False);
            Assert.That(View.quests.previewBody.text,Does.Contain("Als Nächstes"));Assert.That(View.quests.previewBody.text,Does.Contain("Mila"));
            Hit(View.quests.previewDetails).onClick.Invoke();yield return null;Assert.That(root.Ui.Element<RectTransform>("questDetails").gameObject.activeInHierarchy,Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EveryClickedActorKeepsItsIdentityEvenWhenItsWorkplaceIsLocked()
        {
            Start();root.Select("farm_warehouse");string selection=root.SelectedId;root.CameraRig.ShowOverview();
            foreach(var actor in root.Presentation.characters)
            {
                root.Ui.Close();root.CameraRig.Focus(actor.transform.position);root.CameraRig.transform.position=root.CameraRig.TargetPosition;
                yield return null;Canvas.ForceUpdateCanvases();
                var collider=actor.GetComponent<BoxCollider>();var point=root.CameraRig.RenderCamera.WorldToScreenPoint(collider.bounds.center);
                Assert.That(actor.TryPick(root.CameraRig.RenderCamera.ScreenPointToRay(point),out _),Is.True,actor.speakerId);
                Assert.That(root.CameraRig.TrySelect(point),Is.True,actor.speakerId);
                Assert.That(View.conversation.SpeakerId,Is.EqualTo(actor.speakerId),actor.speakerId);
                Assert.That(root.SelectedId,Is.EqualTo(selection),"NPC identity must not depend on changing the selected building");
                Assert.That(View.conversation.portrait.rectTransform.localScale.x,Is.LessThan(0),actor.speakerId);
                Assert.That(View.conversation.next.gameObject.activeInHierarchy,Is.False);Assert.That(root.Ui.Element<Button>("ui.close").gameObject.activeInHierarchy,Is.False);
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator WholeScreenTapAdvancesPagesAndConsumesTheFinalTap()
        {
            Start();root.Definitions.TextLookup=new DialogueLookup(root.Definitions.TextLookup);root.Ui.SpeakToCharacter("nia");yield return null;
            string selection=root.SelectedId;Assert.That(View.conversation.body.text,Is.EqualTo("Erster Satz."));
            ClickScreen(new Vector2(3,Screen.height-3));yield return null;Assert.That(View.conversation.body.text,Is.EqualTo("Zweiter Satz."));Assert.That(root.Ui.ModalOpen,Is.True);
            ClickScreen(new Vector2(Screen.width-3,3));yield return null;Assert.That(root.Ui.ModalOpen,Is.False);Assert.That(root.SelectedId,Is.EqualTo(selection));Assert.That(root.Ui.Interaction.BuildingOpen,Is.False);
            LogAssert.NoUnexpectedReceived();
        }
        private sealed class DialogueLookup : ITextLookup
        {
            private readonly ITextLookup original;
            public DialogueLookup(ITextLookup value){original=value;}
            public string Get(string key)=>key=="dialog.nia"?"Erster Satz.\n\nZweiter Satz.":original.Get(key);
        }
        private void ClickScreen(Vector2 point)
        {
            Canvas.ForceUpdateCanvases();var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Assert.That(hits.First().gameObject.GetComponentInParent<Button>(),Is.EqualTo(View.conversation.tapScreen));
            ExecuteEvents.ExecuteHierarchy(hits.First().gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        [UnityTest] public IEnumerator LegacyWindowCloseLivesInAReservedHeaderOutsideTheContent()
        {
            Start();foreach(string id in new[]{"workers","journal","collection","luckyWheel","vipStories","cityCampaign","offline","premium"})
            {
                root.Ui.ShowPanel(id);yield return null;var frame=root.Ui.Element<RectTransform>(id).GetComponent<ModalFrame>();Assert.That(frame,Is.Not.Null,id);
                var a=new Vector3[4];var b=new Vector3[4];frame.viewport.GetWorldCorners(a);
                if(id!="premium"){var close=Hit(root.Ui.Element<Button>("ui.close"));((RectTransform)close.transform).GetWorldCorners(b);Assert.That(b.Min(v=>v.y),Is.GreaterThanOrEqualTo(a.Max(v=>v.y)),id);close.onClick.Invoke();}
                else root.Ui.Element<Button>("ui.cancel").onClick.Invoke();
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AuthoredModuleScrollKeepsLowerInventoryActionsReachable()
        {
            Start();root.Session.Workers.Grant("grow");root.Ui.ShowCollection();yield return null;
            var inventory=root.Ui.Collection;var layout=inventory.GetComponent<InventoryPanelLayout>();
            inventory.tiles.First(t=>t.gameObject.activeInHierarchy).button.onClick.Invoke();yield return null;
            UiScrollTestUtility.Reveal(inventory.details.action);yield return null;Canvas.ForceUpdateCanvases();
            Assert.That(layout.grid.vertical,Is.True);Assert.That(inventory.previousButton.gameObject.activeSelf,Is.False);Assert.That(inventory.details.action.gameObject.activeInHierarchy,Is.True);
            Hit(inventory.details.action);Hit(root.Ui.Element<Button>("ui.close"));LogAssert.NoUnexpectedReceived();
        }
    }
}

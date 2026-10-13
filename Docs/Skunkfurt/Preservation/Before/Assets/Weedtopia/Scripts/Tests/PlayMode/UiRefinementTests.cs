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
    public sealed class UiRefinementTests
    {
        private GameRoot root;
        private string folder;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();root.enabled=false;
            folder=Path.Combine(UnityEngine.Application.temporaryCachePath,"UiRefinement_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            root.UseTestStore(Path.Combine(folder,"save.json"),new ManualClock(new DateTime(2026,10,3,12,0,0,DateTimeKind.Utc)));
            root.NewGame("Profiltest");root.Session.CompleteIntro();root.Session.State.City.unlocked=Zone.Industry;root.Ui.Close();root.Refresh();yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup(){root.Ui.Close();if(Directory.Exists(folder))Directory.Delete(folder,true);yield return null;}
        [UnityTest] public IEnumerator TenProfileChoicesShowDistinctPortraitsAndRoundTripThroughTheSave()
        {
            var view=root.Ui.Experience;var portraits=new HashSet<Sprite>();view.ShowSettings();view.SelectSettingsTab(true);yield return null;
            for(int i=0;i<10;i++)
            {
                portraits.Add(view.avatar.sprite);view.applyProfile.onClick.Invoke();Assert.That(root.Session.State.playerAvatar,Is.EqualTo(PlayerProfile.Avatars[i]));
                if(i<9)view.nextAvatar.onClick.Invoke();yield return null;
            }
            Assert.That(portraits.Count,Is.EqualTo(10));root.Ui.Close();root.Continue();Assert.That(root.Session.State.playerAvatar,Is.EqualTo(PlayerProfile.Avatars[9]));
            root.Ui.SpeakToCharacter("mila");Assert.That(view.conversation.SpeakerId,Is.EqualTo("mila"));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ContinuousInventoryShowsEightEntriesAndScrollsToTheLastWithoutPagesOrNewPools()
        {
            for(int i=0;i<30;i++)root.Session.Workers.Grant("grow");root.Ui.ShowCollection();yield return null;
            var ui=root.Ui.Collection;var layout=ui.GetComponent<InventoryPanelLayout>();var tiles=ui.tiles.Where(t=>t.gameObject.activeSelf).ToArray();
            Assert.That(tiles.Length,Is.EqualTo(30));Assert.That(ui.previousButton.gameObject.activeSelf||ui.nextButton.gameObject.activeSelf||ui.pageLabel.gameObject.activeSelf,Is.False);
            Assert.That(tiles.Count(t=>Contained((RectTransform)t.transform,layout.grid.viewport)),Is.GreaterThanOrEqualTo(8));
            var ids=ui.tiles.Select(t=>t.GetInstanceID()).ToArray();ui.Paint();CollectionAssert.AreEqual(ids,ui.tiles.Select(t=>t.GetInstanceID()));
            layout.grid.verticalNormalizedPosition=0;yield return null;Canvas.ForceUpdateCanvases();Assert.That(Contained((RectTransform)tiles.Last().transform,layout.grid.viewport),Is.True);
            Hit(tiles.Last().button);tiles.Last().button.onClick.Invoke();yield return null;Assert.That(ui.details.SelectedItem.reference,Is.EqualTo(tiles.Last().Item.reference));
            UiScrollTestUtility.Reveal(ui.details.action);Hit(ui.details.action);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator WheelAndPaidConfirmationFitTheSingleViewport()
        {
            root.Session.State.grinders=10000;root.Ui.ShowLuckyWheel();yield return null;CheckWheel();
            var scale=root.Ui.Element<Slider>("scale");
            foreach(float value in new[]{scale.minValue,1f,scale.maxValue})
            {
                root.Session.State.uiScale=value;root.Refresh();yield return null;
                foreach(var prize in root.Session.Wheel.Remaining)
                {root.Ui.LuckyWheel.Inspect(prize);root.Refresh();yield return null;CheckWheel();}
            }
            root.Ui.LuckyWheel.spin.onClick.Invoke();root.Ui.Close();root.Ui.ShowLuckyWheel();yield return null;
            root.Ui.LuckyWheel.spin.onClick.Invoke();yield return null;Assert.That(root.Ui.LuckyWheel.Confirming,Is.True);CheckWheel();Hit(root.Ui.LuckyWheel.cancel);LogAssert.NoUnexpectedReceived();
        }
        private void CheckWheel()
        {
            var wheel=root.Ui.LuckyWheel;var layout=wheel.GetComponent<WheelPanelLayout>();Assert.That(Contained(layout.stage,layout.frame.viewport),Is.True,"whole wheel");
            foreach(var r in new[]{wheel.remaining.rectTransform,wheel.countdown.rectTransform,wheel.description.rectTransform,wheel.costBody.rectTransform,wheel.balance.rectTransform,(RectTransform)wheel.spin.transform,(RectTransform)wheel.inventory.transform})Assert.That(Contained(r,layout.frame.viewport),Is.True,r.name);
            if(wheel.cancel.gameObject.activeSelf)Assert.That(Contained((RectTransform)wheel.cancel.transform,layout.frame.viewport),Is.True);
        }
        [UnityTest] public IEnumerator EveryCloseUsesTheGoldArtworkAtTheOutsideEdgeAndWorkerAssignIconStaysLeft()
        {
            root.Ui.Experience.ShowSettings();yield return null;
            foreach(var visual in root.Ui.GetComponentsInChildren<CloseButtonVisual>(true))
            {
                visual.Fit();Assert.That(visual.icon.sprite,Is.EqualTo(root.Presentation.art.Get("close")));Assert.That(visual.caption==null||!visual.caption.enabled,Is.True);
                var rect=(RectTransform)visual.transform;Assert.That(visual.icon.rectTransform.rect.width/rect.rect.width,Is.GreaterThan(.9f));
            }
            root.Ui.Close();root.Session.State.Find("farm_grow_01").level=1;root.Session.Workers.Grant("grow");root.Ui.Interaction.OpenBuilding("farm_grow_01");root.Ui.Interaction.OpenPicker(0);root.Refresh();yield return null;
            var row=root.Ui.Interaction.pickerContent.GetComponentInChildren<InteractionRow>();var button=(RectTransform)row.choose.transform;var icon=(RectTransform)button.Find("Icon");
            float x=button.InverseTransformPoint(icon.position).x;Assert.That(x,Is.LessThan(-button.rect.width*.3f));Hit(row.choose);LogAssert.NoUnexpectedReceived();
        }
        private static bool Contained(RectTransform child,RectTransform parent)
        {
            var corners=new Vector3[4];child.GetWorldCorners(corners);return corners.All(c=>parent.rect.Contains((Vector2)parent.InverseTransformPoint(c)+new Vector2(.01f,-.01f))||DistanceInside(c,parent));
        }
        private static bool DistanceInside(Vector3 point,RectTransform parent){var p=parent.InverseTransformPoint(point);var r=parent.rect;return p.x>=r.xMin-2&&p.x<=r.xMax+2&&p.y>=r.yMin-2&&p.y<=r.yMax+2;}
        private static void Hit(Button button)
        {
            Canvas.ForceUpdateCanvases();var r=(RectTransform)button.transform;var results=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=r.TransformPoint(r.rect.center)},results);
            Assert.That(results.First().gameObject.GetComponentInParent<Button>(),Is.EqualTo(button),button.name);
        }
    }
}

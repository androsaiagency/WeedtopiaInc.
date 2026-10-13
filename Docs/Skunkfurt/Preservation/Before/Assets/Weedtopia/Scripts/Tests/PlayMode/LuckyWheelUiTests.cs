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
using Object=UnityEngine.Object;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class LuckyWheelUiTests
    {
        private GameRoot root;
        private ManualClock clock;
        private string path;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/Bootstrap.unity");
            for(int i=0;i<200&&Object.FindFirstObjectByType<GameRoot>()==null;i++)yield return new WaitForSecondsRealtime(.1f);
            root=Object.FindFirstObjectByType<GameRoot>();root.enabled=false;
            clock=new ManualClock(new DateTime(2026,10,1,12,0,0,DateTimeKind.Utc));
            path=Path.Combine(UnityEngine.Application.temporaryCachePath,"wheel_ui_"+Guid.NewGuid()+".json");
            root.UseTestStore(path,clock);root.NewGame("Wheel Test");root.Session.CompleteIntro();root.Ui.Close();root.Ui.ShowLuckyWheel();yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            root.Ui.Rewards.DismissAll();root.Ui.Close();
            foreach(var p in new[]{path,path+".bak",path+".scores"})if(File.Exists(p))File.Delete(p);
            yield return null;
        }
        [UnityTest] public IEnumerator FreeSpinIsSavedBeforeAnimationAndShowsExactlyOneReward()
        {
            var ui=root.Ui.LuckyWheel;ui.Spin();ui.Spin();yield return null;
            Assert.That(ui.Spinning,Is.True);Assert.That(root.Session.State.luckyWheel.spins,Is.EqualTo(1));
            Assert.That(root.Session.State.grinders,Is.EqualTo(50));Assert.That(root.Session.Paused,Is.True);
            Assert.That(File.Exists(path),Is.True);Assert.That(root.Ui.Rewards.IsOpen,Is.False);
            yield return new WaitForSecondsRealtime(4.4f);
            Assert.That(ui.Spinning,Is.False);Assert.That(root.Ui.Rewards.IsOpen,Is.True);
            Assert.That(root.Ui.Rewards.Current.source,Is.EqualTo(RewardSource.LuckyWheel));
            root.Ui.Rewards.DismissAll();Assert.That(root.Session.Paused,Is.False);
            Assert.That(ui.slots.Count(s=>s.gameObject.activeSelf),Is.EqualTo(9));
            Assert.That(ui.spinLabel.text,Does.Contain("50"));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ClosingAnimationKeepsSavedPrizeAndPaidSpinRequiresConfirmation()
        {
            var ui=root.Ui.LuckyWheel;ui.Spin();root.Ui.Close();yield return null;
            Assert.That(root.Session.Paused,Is.False);Assert.That(root.Session.State.luckyWheel.spins,Is.EqualTo(1));
            root.Ui.ShowLuckyWheel();ui.Spin();yield return null;
            Assert.That(ui.Confirming,Is.True);Assert.That(ui.Spinning,Is.False);Assert.That(root.Session.State.grinders,Is.EqualTo(50));
            ui.cancel.onClick.Invoke();Assert.That(ui.Confirming,Is.False);
            ui.Spin();ui.Spin();root.Ui.Close();yield return null;
            Assert.That(root.Session.State.grinders,Is.Zero);Assert.That(root.Session.State.luckyWheel.spins,Is.EqualTo(2));
            root.Continue();yield return null;
            Assert.That(root.Session.State.luckyWheel.spins,Is.EqualTo(2));Assert.That(root.Session.State.grinders,Is.Zero);
            Assert.That(root.Ui.Rewards.IsOpen,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EntriesSpinPrizeAndConfirmationReceivePointerHits()
        {
            void Hit(Button button)
            {
                UiScrollTestUtility.Reveal(button);
                Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
                var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.EqualTo(button),button.name);
            }
            var ui=root.Ui.LuckyWheel;Hit(ui.spin);Hit(ui.inventory);Hit(ui.slots[0].button);Hit(root.Ui.Element<Button>("ui.close"));
            ui.slots[0].button.onClick.Invoke();Assert.That(ui.itemName.text,Is.Not.Empty);
            ui.Spin();root.Ui.Close();root.Ui.ShowLuckyWheel();ui.Spin();yield return null;Hit(ui.spin);Hit(ui.cancel);
            root.Ui.ShowJournal();yield return null;Hit(root.Ui.Element<Button>("wheelJournal"));
            root.Ui.ShowCollection();yield return null;Hit(root.Ui.Element<Button>("wheelInventory"));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CountdownUpdatesAndMidnightRestoresTenSlotsAndFreeSpin()
        {
            var ui=root.Ui.LuckyWheel;ui.Spin();root.Ui.Close();root.Ui.ShowLuckyWheel();
            clock.UtcNow=clock.UtcNow.Date.AddDays(1).AddSeconds(-1);root.Refresh();yield return null;
            Assert.That(ui.countdown.text,Does.Contain("00:00:01"));clock.UtcNow=clock.UtcNow.AddSeconds(1);root.Refresh();yield return null;
            Assert.That(ui.slots.Count(s=>s.gameObject.activeSelf),Is.EqualTo(10));Assert.That(ui.spinLabel.text,Does.Contain("Kostenlos"));
            Assert.That(ui.countdown.text,Does.Contain("24:00:00"));LogAssert.NoUnexpectedReceived();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class RewardPresentationTests
    {
        private GameRoot root;private ManualClock clock;private string path;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/Bootstrap.unity");
            for(int i=0;i<200&&Object.FindFirstObjectByType<GameRoot>()==null;i++)yield return new WaitForSecondsRealtime(.1f);
            root=Object.FindFirstObjectByType<GameRoot>();root.enabled=false;
            clock=new ManualClock(new DateTime(2026,9,30,12,0,0,DateTimeKind.Utc));path=Path.Combine(UnityEngine.Application.temporaryCachePath,"reward_ui_"+Guid.NewGuid().ToString("N")+".json");
            root.UseTestStore(path,clock);root.NewGame("Sammlung");root.Session.CompleteIntro();root.Ui.Close();yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            root.Ui.Rewards.DismissAll();
            foreach(string p in new[]{path,path+".bak",path+".scores"})if(File.Exists(p))File.Delete(p);
            yield return null;
        }
        private void ReadyForFirstReward()
        {
            while(root.Session.State.City.questIndex<1)TutorialDriver.Act(root.Session,clock.UtcNow);
            var field=root.Session.State.Find("farm_grow_01");Assert.That(root.Session.Upgrades.BuyWithMoney(field.id),Is.EqualTo(Failure.None));
            for(int i=0;i<300&&!root.Session.Progression.Ready;i++){clock.UtcNow=clock.UtcNow.AddSeconds(1);root.Session.Advance(1);}
            Assert.That(root.Session.Progression.Ready,Is.True);root.Ui.ShowJournal();root.Refresh();
        }
        [UnityTest] public IEnumerator ClaimShowsSavedRewardAndDismissalCannotDuplicateIt()
        {
            ReadyForFirstReward();var money=root.Session.State.money;int value=root.Session.Progression.Current.moneyReward;
            root.Ui.Element<Button>("ui.claim").onClick.Invoke();yield return null;
            Assert.That(root.Ui.Rewards.IsOpen,Is.True);Assert.That(root.Session.Paused,Is.True);
            Assert.That(root.Session.State.money,Is.EqualTo(money+value));Assert.That(File.Exists(path),Is.True);
            Assert.That(root.Ui.Rewards.Current.items[0].quantity,Is.EqualTo(new Amount(value)));
            root.Ui.Rewards.Advance();root.Ui.Rewards.Advance();yield return null;
            Assert.That(root.Ui.Rewards.IsOpen,Is.False);Assert.That(root.Session.Paused,Is.False);
            root.Ui.ShowCollection();root.Ui.Collection.SelectTab(2);yield return null;
            Assert.That(root.Session.State.money,Is.EqualTo(money+value));
            root.Continue();Assert.That(root.Ui.Rewards.IsOpen,Is.False);Assert.That(root.Session.State.money,Is.EqualTo(money+value));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CollectionUsesLiveBalancesAndWorkerCardOpensExistingTeam()
        {
            var worker=root.Session.Workers.Grant("grow");root.Session.State.money=23;
            root.Ui.ShowCollection();yield return null;
            var visible=root.Ui.Collection.cards.Where(c=>c.gameObject.activeSelf).ToArray();Assert.That(visible.Length,Is.EqualTo(4));
            Assert.That(visible[0].quantity.text,Is.EqualTo("23"));
            visible.Last().action.onClick.Invoke();yield return null;
            Assert.That(root.Ui.Element<RectTransform>("workers").gameObject.activeSelf,Is.True);
            Assert.That(root.Ui.Element<Text>("workerText").text,Does.Contain(root.Definitions.Text(root.Definitions.Role(worker.role).nameKey)));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NewGameClearsPendingCelebrationAndHistory()
        {
            ReadyForFirstReward();root.Ui.Element<Button>("ui.claim").onClick.Invoke();yield return null;
            Assert.That(root.Ui.Rewards.IsOpen,Is.True);root.NewGame("Neustart");yield return null;
            Assert.That(root.Ui.Rewards.IsOpen,Is.False);Assert.That(root.Session.State.rewardHistory,Is.Empty);
            Assert.That(root.Session.Paused,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CollectionAndCelebrationButtonsReceiveTheTopmostPointerHit()
        {
            void Hit(Button button)
            {
                Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
                var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
                var data=new PointerEventData(EventSystem.current){position=point};var hits=new List<RaycastResult>();
                EventSystem.current.RaycastAll(data,hits);
                Assert.That(hits.Count,Is.GreaterThan(0),button.name);
                Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.EqualTo(button),button.name+" is covered by "+hits[0].gameObject.name);
            }
            root.Ui.ShowCollection();root.Refresh();yield return null;
            Hit(root.Ui.Element<Button>("ui.close"));Hit(root.Ui.Collection.historyTab);Hit(root.Ui.Collection.achievementsTab);
            root.Ui.Close();ReadyForFirstReward();root.Ui.Element<Button>("ui.claim").onClick.Invoke();yield return null;
            Hit(root.Ui.Rewards.continueButton);Hit(root.Ui.Rewards.collectionButton);
            LogAssert.NoUnexpectedReceived();
        }
    }
}

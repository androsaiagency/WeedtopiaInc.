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
            var visible=root.Ui.Collection.tiles.Where(c=>c.gameObject.activeSelf).ToArray();Assert.That(visible.Length,Is.EqualTo(1));
            Assert.That(root.Ui.Collection.balance.text,Does.Contain("23"));
            visible.Single().button.onClick.Invoke();yield return null;
            Assert.That(root.Ui.Collection.details.SelectedItem.reference,Is.EqualTo(worker.id));
            Assert.That(root.Ui.Element<RectTransform>("workers").gameObject.activeSelf,Is.False);
            root.Ui.Collection.details.action.onClick.Invoke();yield return null;
            Assert.That(root.Ui.Element<RectTransform>("workers").gameObject.activeSelf,Is.True);
            Assert.That(root.Ui.Element<Text>("workerText").text,Does.Contain(root.Definitions.Text(root.Definitions.Role(worker.role).nameKey)));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SplitIsClickableWithoutConstructionAndKeepsAllTimeInSavedInventory()
        {
            GrantItem("build_24h");root.Ui.ShowCollection();var ui=root.Ui.Collection;
            yield return null;
            foreach(var button in new[]{ui.tiles[0].button,ui.categoryButtons[1],ui.categoryButtons[2],ui.categoryButtons[3]})
            {
                UiScrollTestUtility.Reveal(button);
                Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
                var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                Assert.That(hits.First().gameObject.GetComponentInParent<Button>(),Is.EqualTo(button),button.name+" is covered by "+hits.First().gameObject.name);
            }
            ui.tiles[0].button.onClick.Invoke();yield return null;
            Assert.That(ui.details.action.interactable,Is.False);Assert.That(ui.details.split.interactable,Is.True);
            UiScrollTestUtility.Reveal(ui.details.split);Canvas.ForceUpdateCanvases();
            var splitRect=(RectTransform)ui.details.split.transform;var splitHits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,splitRect.TransformPoint(splitRect.rect.center))},splitHits);
            Assert.That(splitHits.First().gameObject.GetComponentInParent<Button>(),Is.EqualTo(ui.details.split));
            ui.details.split.onClick.Invoke();yield return null;
            Assert.That(ui.details.Confirming,Is.True);Assert.That(ui.details.effect.text,Does.Contain("24"));
            ui.details.action.onClick.Invoke();yield return null;
            Assert.That(root.Session.Inventory.Count("build_24h"),Is.Zero);Assert.That(root.Session.Inventory.Count("build_1h"),Is.EqualTo(24));
            Assert.That(root.Ui.Rewards.IsOpen,Is.True);root.Ui.Rewards.DismissAll();Assert.That(root.Session.Paused,Is.False);
            root.Continue();yield return null;Assert.That(root.Session.Inventory.Count("build_1h"),Is.EqualTo(24));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NewGameClearsPendingCelebrationAndHistory()
        {
            ReadyForFirstReward();root.Ui.Element<Button>("ui.claim").onClick.Invoke();yield return null;
            Assert.That(root.Ui.Rewards.IsOpen,Is.True);root.NewGame("Neustart");yield return null;
            Assert.That(root.Ui.Rewards.IsOpen,Is.False);Assert.That(root.Session.State.rewardHistory,Is.Empty);
            Assert.That(root.Session.Paused,Is.False);LogAssert.NoUnexpectedReceived();
        }
        private void GrantItem(string id,int count=1)
        {
            InventoryItems.Grant(root.Session.State,new[]{new ItemStack{id=id,count=count}},new List<RewardItem>());
            root.Session.State.revision++;
        }
        [UnityTest] public IEnumerator InventoryFiltersInspectAndConfirmOneSpeedupThenPersist()
        {
            GrantItem("build_1m",2);GrantItem("team_pack");GrantItem("cash_crate");root.Session.Workers.Grant("grow");
            var b=root.Session.State.Find("farm_grow_01");b.buildingSeconds=150;
            root.Ui.ShowCollection();var ui=root.Ui.Collection;yield return null;
            Assert.That(ui.tiles.Count(t=>t.gameObject.activeSelf),Is.EqualTo(4));
            ui.categoryButtons[2].onClick.Invoke();yield return null;
            Assert.That(ui.tiles.Count(t=>t.gameObject.activeSelf),Is.EqualTo(1));
            ui.tiles[0].button.onClick.Invoke();yield return null;
            Assert.That(ui.details.title.text,Does.Contain("Baubeschleuniger"));
            Assert.That(root.Session.Inventory.Count("build_1m"),Is.EqualTo(2));Assert.That(root.Session.Paused,Is.False);
            ui.details.action.onClick.Invoke();yield return null;
            Assert.That(ui.details.Confirming,Is.True);Assert.That(root.Session.Paused,Is.True);
            Assert.That(b.buildingSeconds,Is.EqualTo(150));
            ui.details.action.onClick.Invoke();yield return null;
            Assert.That(b.buildingSeconds,Is.EqualTo(90));Assert.That(root.Session.Inventory.Count("build_1m"),Is.EqualTo(1));Assert.That(root.Session.Paused,Is.False);
            root.Continue();yield return null;
            Assert.That(root.Session.Inventory.Count("build_1m"),Is.EqualTo(1));Assert.That(root.Session.State.Find(b.id).buildingSeconds,Is.EqualTo(90));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CancelOrNavigateAwayNeverConsumesItemsOrLeavesTheSimulationPaused()
        {
            GrantItem("build_24h");root.Session.State.Find("farm_grow_01").buildingSeconds=120;
            root.Ui.ShowCollection();var ui=root.Ui.Collection;ui.tiles[0].button.onClick.Invoke();ui.details.action.onClick.Invoke();yield return null;
            Assert.That(ui.details.effect.text,Does.Contain("verfallen"));ui.details.cancel.onClick.Invoke();yield return null;
            Assert.That(root.Session.Paused,Is.False);Assert.That(root.Session.Inventory.Count("build_24h"),Is.EqualTo(1));
            ui.details.action.onClick.Invoke();ui.categoryButtons[1].onClick.Invoke();yield return null;
            Assert.That(root.Session.Paused,Is.False);Assert.That(ui.tiles.Any(t=>t.gameObject.activeSelf),Is.False);
            ui.categoryButtons[2].onClick.Invoke();ui.tiles[0].button.onClick.Invoke();ui.details.action.onClick.Invoke();root.Ui.Close();yield return null;
            Assert.That(root.Session.Paused,Is.False);Assert.That(root.Session.Inventory.Count("build_24h"),Is.EqualTo(1));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator OpeningAPackShowsActualSavedContentsAndConsumesOnlyOne()
        {
            root.Session.State.City.unlocked=Zone.City;GrantItem("team_pack",2);root.Ui.ShowCollection();
            var ui=root.Ui.Collection;ui.tiles[0].button.onClick.Invoke();ui.details.action.onClick.Invoke();yield return null;
            Assert.That(root.Session.State.cards,Is.Zero);ui.details.action.onClick.Invoke();yield return null;
            Assert.That(root.Session.State.cards,Is.EqualTo(3));Assert.That(root.Session.Inventory.Count("team_pack"),Is.EqualTo(1));
            Assert.That(root.Ui.Rewards.IsOpen,Is.True);Assert.That(root.Ui.Rewards.Current.source,Is.EqualTo(RewardSource.Inventory));
            root.Ui.Rewards.DismissAll();Assert.That(root.Session.Paused,Is.False);root.Continue();yield return null;
            Assert.That(root.Session.State.cards,Is.EqualTo(3));Assert.That(root.Session.Inventory.Count("team_pack"),Is.EqualTo(1));LogAssert.NoUnexpectedReceived();
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

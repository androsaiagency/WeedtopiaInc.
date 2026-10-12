using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class BuildingInteractionTests
    {
        private GameRoot root;
        private string save;
        private Mouse mouse;
        private Touchscreen touch;
        private InputSettings originalSettings, testSettings;
        private BuildingInteractionUi Ui => root.Ui.Interaction;
        [UnitySetUp] public IEnumerator Setup()
        {
            // Owned batch tests have no focused Game View. Route virtual devices to the player loop
            // using a temporary settings copy; preserve the user's real editor input configuration.
            originalSettings=InputSystem.settings;testSettings=UnityEngine.Object.Instantiate(originalSettings);
            testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=testSettings;
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");
            root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();root.enabled=false;
            save=Path.Combine(UnityEngine.Application.temporaryCachePath,"Weedtopia_Interaction_"+Guid.NewGuid().ToString("N")+".json");
            root.UseTestStore(save,new ManualClock(new DateTime(2026,9,25,0,0,0,DateTimeKind.Utc)));root.NewGame("Interaktionstest");root.Session.CompleteIntro();root.Ui.Close();
            root.Session.State.City.unlocked=Zone.Industry;root.Session.State.money=100000;
            foreach(var b in root.Session.State.buildings){if(b.family=="settlement_plot")b.family="residential_house";b.level=10;}
            root.Refresh();mouse=InputSystem.AddDevice<Mouse>();touch=InputSystem.AddDevice<Touchscreen>();yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);if(touch!=null&&touch.added)InputSystem.RemoveDevice(touch);
            InputSystem.settings=originalSettings;UnityEngine.Object.Destroy(testSettings);
            foreach(var p in new[]{save,save+".bak",save+".scores"})if(File.Exists(p))File.Delete(p);
            yield return null;
        }
        private IEnumerator Settle()
        {
            float until=Time.realtimeSinceStartup+4;
            while(Time.realtimeSinceStartup<until&&(Vector3.Distance(root.CameraRig.transform.position,root.CameraRig.TargetPosition)>.01f||Mathf.Abs(root.CameraRig.RenderCamera.orthographicSize-root.CameraRig.DesiredZoom)>.01f))yield return null;
            yield return null;Canvas.ForceUpdateCanvases();
        }
        private IEnumerator MouseAt(Vector2 p,bool pressed)
        {InputSystem.QueueStateEvent(mouse,new MouseState{position=p,buttons=(ushort)(pressed?1:0)});yield return null;yield return null;}
        private IEnumerator Finger(int id,Vector2 p,TouchPhase phase)
        {InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,position=p,phase=phase});yield return null;yield return null;}
        [UnityTest] public IEnumerator FreeWorldHasNoPersistentPanelsOrPositionScrollbar()
        {
            Assert.That(Ui.BuildingOpen||Ui.PickerOpen||Ui.townMenu.activeSelf||root.Ui.ModalOpen,Is.False);
            Assert.That(Ui.launcher.gameObject.activeInHierarchy,Is.True);
            Assert.That(root.Ui.GetComponentsInChildren<Scrollbar>(),Is.Empty);
            foreach(string id in new[]{"detailTitle","questTitle","title","recovery"})Assert.That(root.Ui.Element<RectTransform>(id).gameObject.activeInHierarchy,Is.False,id);
            var dock=root.Ui.GetComponent<WorldNavigationBar>().Dock;
            Assert.That(dock.gameObject.activeInHierarchy,Is.True);
            Assert.That(dock.GetComponentsInChildren<Button>().Length,Is.EqualTo(5));
            Assert.That(dock.GetComponentsInChildren<Text>(),Is.Empty);
            Ui.launcher.onClick.Invoke();yield return null;Assert.That(Ui.townMenu.activeSelf,Is.True);
            root.Ui.Element<Button>("zone.Settlement").onClick.Invoke();Assert.That(Ui.townMenu.activeSelf,Is.False);Assert.That(root.CameraRig.CurrentDistrict.zone,Is.EqualTo(Zone.Settlement));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator MouseSwipeMovesEveryDistrictWithoutClickingABuilding()
        {
            foreach(Zone zone in Enum.GetValues(typeof(Zone)))
            {
                root.FocusZone(zone);root.CameraRig.ScrollTo(.5f);yield return Settle();
                var before=root.CameraRig.TargetPosition;var camera=root.CameraRig.RenderCamera;var range=root.CameraRig.CurrentDistrict;
                Vector2 screenDirection=(camera.WorldToScreenPoint(DistrictMap.ToWorld(range.end))-camera.WorldToScreenPoint(DistrictMap.ToWorld(range.start))).normalized;
                Vector2 start=new Vector2(Screen.width*.6f,Screen.height*.45f);
                yield return MouseAt(start,true);Assert.That(Ui.BuildingOpen,Is.False,"Press alone must not select");
                yield return MouseAt(start-screenDirection*180,true);yield return MouseAt(start-screenDirection*180,false);
                Assert.That(Vector3.Distance(before,root.CameraRig.TargetPosition),Is.GreaterThan(1),zone.ToString());Assert.That(Ui.BuildingOpen,Is.False,"Dragging must not dispatch a tap");
                Assert.That(root.CameraRig.DistrictFraction,Is.InRange(0f,1f));
            }
        }
        [UnityTest] public IEnumerator DiagonalTouchCanLeaveEveryDistrictAxis()
        {
            foreach(Zone zone in Enum.GetValues(typeof(Zone)))
            {
                root.CameraRig.EnterDistrict(zone);root.CameraRig.ScrollTo(.5f);yield return Settle();
                var before=DistrictMap.ToPlan(root.CameraRig.TargetPosition);
                var p=new Vector2(Screen.width*.4f,Screen.height*.55f);
                var end=p+new Vector2(Screen.width*.23f,-Screen.height*.17f);
                yield return Finger(1,p,TouchPhase.Began);yield return Finger(1,end,TouchPhase.Moved);yield return Finger(1,end,TouchPhase.Ended);
                var after=DistrictMap.ToPlan(root.CameraRig.TargetPosition);
                Assert.That(Mathf.Abs(after.x-before.x),Is.GreaterThan(4),zone+" horizontal movement");
                Assert.That(Mathf.Abs(after.y-before.y),Is.GreaterThan(4),zone+" vertical movement");
                Assert.That(Ui.BuildingOpen||Ui.townMenu.activeSelf,Is.False);
            }
        }
        [UnityTest] public IEnumerator TouchCrossesRiverAndReachesAllFourWorldEdgesWithoutMenu()
        {
            root.CameraRig.EnterDistrict(Zone.Farm);root.CameraRig.SetDiagnosticZoom(230);yield return Settle();
            Vector2 start=new Vector2(Screen.width*.25f,Screen.height*.5f),end=new Vector2(Screen.width*.75f,Screen.height*.5f);
            yield return Finger(1,start,TouchPhase.Began);yield return Finger(1,end,TouchPhase.Moved);yield return Finger(1,end,TouchPhase.Ended);
            Assert.That(DistrictMap.ToPlan(root.CameraRig.TargetPosition).x,Is.LessThan(140),"Swipe from farm across the river");
            foreach(var direction in new[]{new Vector2(1,1),new Vector2(-1,1),new Vector2(-1,-1),new Vector2(1,-1)})
            {
                start=new Vector2(Screen.width*(.5f-direction.x*.3f),Screen.height*(.5f-direction.y*.3f));
                end=new Vector2(Screen.width*(.5f+direction.x*.3f),Screen.height*(.5f+direction.y*.3f));
                for(int i=0;i<8;i++)
                {yield return Finger(1,start,TouchPhase.Began);yield return Finger(1,end,TouchPhase.Moved);yield return Finger(1,end,TouchPhase.Ended);}
                var plan=DistrictMap.ToPlan(root.CameraRig.TargetPosition);
                Assert.That(plan.x,Is.EqualTo(direction.x>0?-330:700).Within(.05));
                Assert.That(plan.y,Is.EqualTo(direction.y>0?-340:475).Within(.05));
                Assert.That(Ui.BuildingOpen||Ui.townMenu.activeSelf,Is.False);
            }
        }
        [UnityTest] public IEnumerator NavigationButtonsWorkByTouchAndPreserveLockedProgression()
        {
            root.Session.State.City.unlocked=Zone.Farm;root.Refresh();yield return Settle();
            foreach(Zone zone in Enum.GetValues(typeof(Zone)))
            {
                var button=root.Ui.Element<Button>("zone."+zone);var rect=button.GetComponent<RectTransform>();
                Vector2 p=rect.TransformPoint(rect.rect.center);
                yield return Finger(1,p,TouchPhase.Began);yield return Finger(1,p,TouchPhase.Ended);
                Assert.That(root.CameraRig.CurrentDistrict.zone,Is.EqualTo(zone));
                Assert.That(Vector2.Distance(DistrictMap.ToPlan(root.CameraRig.TargetPosition),root.CameraRig.CurrentDistrict.start),Is.LessThan(.01f));
                Assert.That(root.Session.State.City.unlocked,Is.EqualTo(Zone.Farm));
                Assert.That(Ui.townMenu.activeSelf||Ui.BuildingOpen,Is.False);
            }
            // The selected shortcut recentres even after freely panning in the same district.
            root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(450,180)));
            root.Ui.Element<Button>("zone.Industry").onClick.Invoke();
            Assert.That(Vector2.Distance(DistrictMap.ToPlan(root.CameraRig.TargetPosition),root.CameraRig.CurrentDistrict.start),Is.LessThan(.01f));
            var before=root.CameraRig.TargetPosition;
            var orders=root.Ui.Element<RectTransform>("ui.journal");Vector2 point=orders.TransformPoint(orders.rect.center);
            yield return Finger(1,point,TouchPhase.Began);yield return Finger(1,point,TouchPhase.Ended);
            Assert.That(root.Ui.Element<RectTransform>("journal").gameObject.activeInHierarchy,Is.True);
            Assert.That(root.CameraRig.TargetPosition,Is.EqualTo(before));
            Assert.That(root.Ui.GetComponent<WorldNavigationBar>().Dock.gameObject.activeSelf,Is.False);
            root.Ui.Close();yield return null;Assert.That(root.Ui.GetComponent<WorldNavigationBar>().Dock.gameObject.activeSelf,Is.True);
        }
        [UnityTest] public IEnumerator TouchStartingOnRoundNavigationCannotDragWorldOrClickOnReleaseElsewhere()
        {
            root.CameraRig.EnterDistrict(Zone.City);yield return Settle();var before=root.CameraRig.TargetPosition;
            var button=root.Ui.Element<RectTransform>("zone.Farm");Vector2 start=button.TransformPoint(button.rect.center);
            var end=new Vector2(Screen.width*.6f,Screen.height*.6f);
            Assert.That(root.CameraRig.IsUiPoint(start),Is.True);
            yield return Finger(1,start,TouchPhase.Began);yield return Finger(1,end,TouchPhase.Moved);yield return Finger(1,end,TouchPhase.Ended);
            Assert.That(root.CameraRig.TargetPosition,Is.EqualTo(before));Assert.That(Ui.BuildingOpen,Is.False);
            yield return MouseAt(start,true);yield return MouseAt(end,true);yield return MouseAt(end,false);
            Assert.That(root.CameraRig.TargetPosition,Is.EqualTo(before));Assert.That(Ui.BuildingOpen,Is.False);
        }
        [UnityTest] public IEnumerator MouseTapFramesBuildingOnRightAndCloseRestoresPreviousView()
        {
            var site=root.Site("farm_processing");root.CameraRig.ShowOverview();root.CameraRig.Focus(site.transform.position);root.CameraRig.SetDiagnosticZoom(45);yield return Settle();
            var center=site.VisualRoot.GetComponentInChildren<Weedtopia.Buildings.Presentation.BuildingSpriteVisual>().SpriteRenderer.bounds.center;
            Vector2 point=root.CameraRig.RenderCamera.WorldToScreenPoint(center);var before=root.CameraRig.TargetPosition;float zoom=root.CameraRig.DesiredZoom;
            yield return MouseAt(point,true);Assert.That(Ui.BuildingOpen,Is.False);yield return MouseAt(point,false);yield return Settle();
            Assert.That(Ui.BuildingOpen,Is.True);Assert.That(root.SelectedId,Is.EqualTo("farm_processing"));
            var framed=root.CameraRig.RenderCamera.WorldToViewportPoint(center);Assert.That(framed.x,Is.EqualTo(.68f).Within(.02));Assert.That(framed.y,Is.EqualTo(.5f).Within(.02));Assert.That(root.CameraRig.DesiredZoom,Is.LessThan(zoom));
            Ui.closeBuilding.onClick.Invoke();Assert.That(Ui.BuildingOpen,Is.False);Assert.That(root.CameraRig.TargetPosition,Is.EqualTo(before));Assert.That(root.CameraRig.DesiredZoom,Is.EqualTo(zoom));
            yield return Settle();point=root.CameraRig.RenderCamera.WorldToScreenPoint(center);
            yield return Finger(1,point,TouchPhase.Began);Assert.That(Ui.BuildingOpen,Is.False);
            yield return Finger(1,point,TouchPhase.Ended);Assert.That(Ui.BuildingOpen,Is.True);Assert.That(root.SelectedId,Is.EqualTo("farm_processing"));
            root.Focus("bakery_01");Assert.That(Ui.BuildingOpen,Is.False,"A quest focus must close the previous building context");Assert.That(root.CameraRig.IsInspecting,Is.False);
        }
        [UnityTest] public IEnumerator TouchSwipeAndPinchNeverDispatchAccidentalBuildingTaps()
        {
            root.FocusZone(Zone.Farm);root.CameraRig.ScrollTo(.5f);yield return Settle();var before=root.CameraRig.TargetPosition;
            var p=new Vector2(Screen.width*.6f,Screen.height*.5f);
            yield return Finger(1,p,TouchPhase.Began);yield return Finger(1,p-Vector2.right*200,TouchPhase.Moved);yield return Finger(1,p-Vector2.right*200,TouchPhase.Ended);
            Assert.That(Vector3.Distance(before,root.CameraRig.TargetPosition),Is.GreaterThan(1));Assert.That(Ui.BuildingOpen,Is.False);
            float initial=root.CameraRig.DesiredZoom;
            yield return Finger(1,p,TouchPhase.Began);yield return Finger(2,p+Vector2.right*100,TouchPhase.Began);yield return Finger(2,p+Vector2.right*200,TouchPhase.Moved);
            Assert.That(root.CameraRig.DesiredZoom,Is.LessThan(initial));
            yield return Finger(2,p+Vector2.right*200,TouchPhase.Ended);yield return Finger(1,p,TouchPhase.Ended);Assert.That(Ui.BuildingOpen,Is.False);
        }
        [UnityTest] public IEnumerator GesturesThatStartOnInspectorStayCapturedByUi()
        {
            Ui.OpenBuilding("farm_processing");yield return Settle();var before=root.CameraRig.TargetPosition;var rect=Ui.inspector.GetComponent<RectTransform>();var p=(Vector2)rect.TransformPoint(new Vector3(0,20,0));
            Assert.That(root.CameraRig.IsUiPoint(p),Is.True);
            yield return MouseAt(p,true);yield return MouseAt(p+Vector2.right*Screen.width*.4f,true);yield return MouseAt(p+Vector2.right*Screen.width*.4f,false);
            Assert.That(Ui.BuildingOpen,Is.True);Assert.That(root.CameraRig.TargetPosition,Is.EqualTo(before));
            yield return Finger(1,p,TouchPhase.Began);yield return Finger(1,p+Vector2.right*Screen.width*.4f,TouchPhase.Moved);yield return Finger(1,p+Vector2.right*Screen.width*.4f,TouchPhase.Ended);
            Assert.That(Ui.BuildingOpen,Is.True);Assert.That(root.CameraRig.TargetPosition,Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator EveryAssignmentPickerContainsOnlyCompatibleRolesAndButtonsAssignRemove()
        {
            foreach(var role in root.Definitions.roles)root.Session.Workers.Grant(role.id,4);
            foreach(var role in root.Definitions.roles)
            {
                var b=root.Session.State.buildings.FirstOrDefault(x=>root.Definitions.Building(x.family).managerRole==role.id||root.Definitions.Building(x.family).driverRole==role.id);
                if(b==null){root.Session.State.Find("settlement_02").family="park";b=root.Session.State.Find("settlement_02");}
                Ui.CloseBuilding();Ui.OpenBuilding(b.id);int slot=root.Definitions.Building(b.family).driverRole==role.id?1:0;Ui.OpenPicker(slot);yield return null;
                Assert.That(Ui.launcher.gameObject.activeInHierarchy,Is.False,"Global launcher must not overlap contextual close buttons");
                Assert.That(Ui.Candidates.Select(w=>w.role),Is.EquivalentTo(new[]{role.id}),b.id);
                var rows=Ui.pickerContent.GetComponentsInChildren<InteractionRow>();Assert.That(rows.Length,Is.EqualTo(1));rows[0].choose.onClick.Invoke();
                var worker=root.Session.State.workers.Single(w=>w.role==role.id);Assert.That(worker.buildingId,Is.EqualTo(b.id));Assert.That(worker.slot,Is.EqualTo(slot));
                if(slot==0){Assert.That(Ui.staffMark.Mark,Is.EqualTo(ActionMark.Shape.Cross));Ui.staffAction.onClick.Invoke();Assert.That(worker.buildingId,Is.Null);Assert.That(Ui.staffMark.Mark,Is.EqualTo(ActionMark.Shape.Plus));}
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FleetShowsUnlockedSlotsAndProtectsDriversInTransit()
        {
            var driver=root.Session.Workers.Grant("city_driver");root.Session.Workers.Assign(driver.id,"city_warehouse",1);
            root.Session.State.deliveries.Add(new DeliveryState{workerId=driver.id,route=root.Definitions.routes.First(r=>r.from=="city_warehouse"&&r.vehicle).id,totalSeconds=20,remainingSeconds=10,cargo=5});
            Ui.OpenBuilding("city_warehouse");Ui.OpenFleet();yield return null;
            var rows=Ui.pickerContent.GetComponentsInChildren<InteractionRow>();Assert.That(rows.Length,Is.EqualTo(root.Session.Workers.Slots(root.Session.State.Find("city_warehouse"))));
            Assert.That(rows[0].choose.interactable,Is.False);Assert.That(rows[0].remove.gameObject.activeSelf,Is.False);Assert.That(rows[0].body.text,Does.Contain("Unterwegs"));
            Ui.OpenPicker(1);var candidate=root.Session.Workers.Grant("city_driver");Ui.Assign(candidate.id);Assert.That(candidate.buildingId,Is.Null);Assert.That(driver.buildingId,Is.EqualTo("city_warehouse"));
            root.Session.State.deliveries.Clear();Ui.Assign(candidate.id);Assert.That(candidate.buildingId,Is.EqualTo("city_warehouse"));Assert.That(driver.buildingId,Is.Null);
            root.Session.State.Find("city_warehouse").level=5000;root.Refresh();Ui.OpenFleet();yield return null;Assert.That(Ui.pickerContent.GetComponentsInChildren<InteractionRow>().Length,Is.EqualTo(24));
        }
        [UnityTest] public IEnumerator UpgradePreviewMatchesActualPostUpgradeRateAndPreservesStateWhileReading()
        {
            var b=root.Session.State.Find("farm_processing");var worker=root.Session.Workers.Grant("processing",4);root.Session.Workers.Assign(worker.id,b.id,0);
            string before=JsonUtility.ToJson(root.Session.State);var metrics=BuildingInspectionMetrics.Read(root,b);
            Assert.That(JsonUtility.ToJson(root.Session.State),Is.EqualTo(before));var capacity=metrics.Single(x=>x.Label=="Pufferkapazität");var rate=metrics.Single(x=>x.Label=="Durchsatz / s");
            Ui.OpenBuilding(b.id);Ui.upgrade.onClick.Invoke();root.Session.Advance(5);root.Refresh();
            Assert.That(b.level,Is.EqualTo(11));Assert.That(root.Session.Simulation.Capacity(b).ToString(),Is.EqualTo(capacity.Next));
            Assert.That((root.Session.Simulation.Rate(b)*root.Session.Workers.Bonus(b,WorkerAttribute.CycleSpeed)).ToString(),Is.EqualTo(rate.Next));
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ImmediateTierSwapFramesTheCurrentLargestBuildingWithoutClipping()
        {
            var b=root.Session.State.Find("coffee_01");b.level=0;root.Refresh();Ui.OpenBuilding(b.id);yield return Settle();
            b.level=5000;root.Refresh();Ui.OpenBuilding(b.id);yield return Settle();
            Assert.That(root.Site(b.id).TryGetCurrentSpriteBounds(out var bounds),Is.True);
            var center=root.CameraRig.RenderCamera.WorldToViewportPoint(bounds.center);
            Assert.That(center.x,Is.EqualTo(.68f).Within(.02));Assert.That(center.y,Is.EqualTo(.5f).Within(.02));
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                var p=root.CameraRig.RenderCamera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));
                Assert.That(p.x,Is.InRange(.35f,.98f),"Complete current sprite stays to the right of the inspector");Assert.That(p.y,Is.InRange(.02f,.98f),"Roof and ground remain in frame");
            }
        }
    }
}

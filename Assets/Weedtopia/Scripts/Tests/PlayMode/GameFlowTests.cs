using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class GameFlowTests
    {
        private GameRoot root;
        private ManualClock clock;
        private string path;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/Bootstrap.unity");
            for(int i=0;i<200&&UnityEngine.Object.FindFirstObjectByType<GameRoot>()==null;i++)yield return new WaitForSecondsRealtime(.1f);
            root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();Assert.That(root,Is.Not.Null);root.enabled=false;
            path=Path.Combine(UnityEngine.Application.temporaryCachePath,"Weedtopia_PlayTest_"+Guid.NewGuid().ToString("N")+".json");
            clock=new ManualClock(new DateTime(2026,9,21,12,0,0,DateTimeKind.Utc));root.UseTestStore(path,clock);yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            // Only the individually owned test files are removed; never a player's persistent save.
            foreach(string p in new[]{path,path+".bak",path+".scores"})if(File.Exists(p))File.Delete(p);
            yield return null;
        }
        private void Start()
        {
            root.Ui.Element<InputField>("name").text="Spieltest";root.Ui.Element<Button>("ui.start").onClick.Invoke();
            Assert.That(root.Session,Is.Not.Null);root.Ui.Element<Button>("ui.skip").onClick.Invoke();
        }
        private void DriveUntil(Func<bool> condition,int limit=150)
        {
            for(int i=0;i<limit&&!condition();i++){TutorialDriver.Act(root.Session,clock.UtcNow);clock.UtcNow=clock.UtcNow.AddSeconds(1);root.Session.Advance(1);}
            root.Refresh();Assert.That(condition(),Is.True,root.Session.Progression.Current?.id);
        }
        [UnityTest] public IEnumerator AmbientTownLifeMovesPausesAndNeverChangesGameplayOrCreatesObjects()
        {
            Start();var life=UnityEngine.Object.FindFirstObjectByType<LivingTownPresentation>();Assert.That(life,Is.Not.Null);life.diagnosticFreeze=true;
            life.PresentAt(10);Assert.That(life.actors.Where(a=>a.zone>Zone.Farm).All(a=>!a.anchor.gameObject.activeSelf),Is.True);
            root.Session.State.City.unlocked=Zone.Industry;foreach(var b in root.Session.State.buildings)b.level=b.family=="town_hall"?1:10;
            string before=JsonUtility.ToJson(root.Session.State);int count=life.GetComponentsInChildren<Transform>(true).Length;
            life.PresentAt(12);var positions=life.actors.Select(a=>a.anchor.position).ToArray();var frames=life.actors.Select(a=>a.artwork.sprite).ToArray();
            life.PresentAt(19.2f);
            Assert.That(life.actors.Where((a,i)=>Vector3.Distance(a.anchor.position,positions[i])>.05f).Count(),Is.GreaterThan(30));
            Assert.That(life.actors.Where((a,i)=>a.artwork.sprite!=frames[i]).Count(),Is.GreaterThan(0));
            Assert.That(JsonUtility.ToJson(root.Session.State),Is.EqualTo(before));Assert.That(life.GetComponentsInChildren<Transform>(true).Length,Is.EqualTo(count));
            root.Session.Paused=true;life.diagnosticFreeze=false;float elapsed=life.Elapsed;yield return new WaitForSecondsRealtime(.1f);
            Assert.That(life.Elapsed,Is.EqualTo(elapsed));root.Session.Paused=false;yield return new WaitForSecondsRealtime(.1f);
            Assert.That(life.Elapsed,Is.GreaterThan(elapsed));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DirectionalVanMovesBetweenHudRefreshesAndFacesTheActualReturnDirection()
        {
            Start();var state=root.Session.State;state.City.unlocked=Zone.Industry;
            state.Find("butter_factory").level=state.Find("bakery_01").level=1;state.Find("butter_factory").butter=24;
            var baker=root.Session.Workers.Grant("baker");root.Session.Workers.Assign(baker.id,"bakery_01",0);
            root.Session.Advance(1);root.Refresh();yield return null;
            var trip=state.deliveries.Single(t=>t.workerId==baker.id);
            var van=UnityEngine.Object.FindObjectsByType<DeliveryView>(FindObjectsSortMode.None).Single(v=>v.GetComponentsInChildren<SpriteRenderer>().Any(x=>x.name=="TruckArtwork"));
            var motion=van.GetComponent<DirectionalSpriteAnimator>();Assert.That(motion,Is.Not.Null);
            Vector3 before=van.transform.position;
            root.Session.Advance(.05);yield return null; // Intentionally no root.Refresh().
            Assert.That(Vector3.Distance(before,van.transform.position),Is.GreaterThan(.001f));
            root.Session.Paused=true;Vector3 stopped=van.transform.position;var frame=motion.artwork.sprite;
            root.Session.Advance(.25);yield return null;
            Assert.That(van.transform.position,Is.EqualTo(stopped));Assert.That(motion.artwork.sprite,Is.EqualTo(frame));
            root.Session.Paused=false;trip.remainingSeconds=trip.totalSeconds/2;trip.fetching=true;root.Refresh();yield return null;
            int outbound=motion.CurrentDirection;trip.fetching=false;root.Refresh();yield return null;
            Assert.That(motion.CurrentDirection,Is.EqualTo((outbound+4)%8));
            Assert.That(motion.artwork.sprite,Is.EqualTo(motion.animationSet.views[motion.CurrentDirection].frames[motion.CurrentFrame]));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator WesternDirectionsReflectActualGeometryForBothProductionShaderFamilies()
        {
            Start();
            var life=UnityEngine.Object.FindFirstObjectByType<LivingTownPresentation>();life.diagnosticFreeze=true;
            var resident=life.actors.First(a=>a.animation.animationSet.name=="resident_a").animation;
            var van=GameObject.Find("Delivery_0").GetComponent<DirectionalSpriteAnimator>();
            var camera=root.CameraRig.RenderCamera;
            foreach(var motion in new[]{resident,van})
            foreach(int direction in new[]{0,3,4,5})
            {
                float angle=direction*Mathf.PI/4;
                motion.Present(DistrictMap.ToWorld(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))),.5f,true,camera);
                Assert.That(motion.CurrentDirection,Is.EqualTo(direction));
                float reflectedX=Vector3.Dot(motion.artwork.transform.TransformVector(Vector3.right),camera.transform.right);
                Assert.That(reflectedX*(direction==0?1:-1),Is.GreaterThan(0),motion.name+" direction "+direction);
            }
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator BootstrapAndStartButtonsCreateOneSessionWithExactResources()
        {
            Assert.That(UnityEngine.Object.FindObjectsByType<GameRoot>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Start();Assert.That(root.Session.State.playerName,Is.EqualTo("Spieltest"));Assert.That(root.Session.State.money,Is.EqualTo(new Amount(1000)));Assert.That(root.Session.State.grinders,Is.EqualTo(50));
            Assert.That(root.Session.Progression.Current.id,Is.EqualTo("farm_welcome"));yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator BuildingSelectionPanelUpgradeAndManagerHaveRealEffects()
        {
            Start();root.Select("farm_grow_01");root.Ui.Element<Button>("ui.upgrade").onClick.Invoke();root.Session.Advance(3);root.Refresh();
            Assert.That(root.Session.State.Find("farm_grow_01").level,Is.EqualTo(1));Assert.That(root.Ui.Element<Text>("detailBody").text,Does.Contain("Level 1"));Assert.That(root.Ui.Element<Text>("detailBody").text,Does.Not.Contain("T01_L001"));
            root.Session.Advance(5);Assert.That(root.Session.State.Find("farm_processing").raw,Is.EqualTo(Amount.Zero));
            var w=root.Session.Workers.Grant("grow");Assert.That(root.Session.Workers.Assign(w.id,"farm_grow_01",0),Is.EqualTo(Failure.None));Assert.That(root.Session.Workers.Automated(root.Session.State.Find("farm_grow_01")),Is.True);
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DistrictControlsReachEveryExpansionEndAndApprovedSpritesLoadOnDemand()
        {
            Start();root.Session.State.City.unlocked=Zone.Industry;root.Refresh();yield return null;
            Assert.That(root.UsesProductionBuildings,Is.True);
            Assert.That(root.Sites.All(x=>x.VisualRoot.GetComponentInChildren<Weedtopia.Buildings.Presentation.BuildingSpriteVisual>().SpriteRenderer.sprite!=null),Is.True);
            Assert.That(Weedtopia.Buildings.Presentation.BuildingSpriteCache.LoadedCount,Is.LessThanOrEqualTo(10));
            Assert.That(UnityEngine.Object.FindFirstObjectByType<DistrictNavigator>(),Is.Null,"Position scrollbar is archived");
            foreach(Zone zone in Enum.GetValues(typeof(Zone)))
            {
                root.FocusZone(zone);yield return null;var range=root.CameraRig.CurrentDistrict;
                Assert.That(range.zone,Is.EqualTo(zone));root.CameraRig.ScrollTo(1);yield return null;
                Assert.That(root.CameraRig.DistrictFraction,Is.EqualTo(1).Within(.001));
                Assert.That(Vector2.Distance(DistrictMap.ToPlan(root.CameraRig.TargetPosition),range.end),Is.LessThan(.001));
                root.CameraRig.ScrollTo(0);yield return null;Assert.That(root.CameraRig.DistrictFraction,Is.Zero.Within(.001));
                root.CameraRig.StepDistrict(1);yield return null;Assert.That(root.CameraRig.DistrictFraction,Is.GreaterThan(0));
            }
            root.CameraRig.ShowOverview();Assert.That(root.CameraRig.CurrentDistrict,Is.Null);
            root.Session.State.Find("coffee_01").level=5000;root.Select("coffee_01",true);root.Refresh();yield return null;
            var visual=root.Site("coffee_01").VisualRoot.GetComponentInChildren<Weedtopia.Buildings.Presentation.BuildingSpriteVisual>();
            Assert.That(visual.ResourcePath,Is.EqualTo("WeedtopiaBuildings/coffee_shop/T24_L5000"));Assert.That(visual.SpriteRenderer.sprite,Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FarmEndToEndUnlocksCityWithCompletedDelivery()
        {
            Start();DriveUntil(()=>root.Session.State.City.unlocked>=Zone.City);Assert.That(root.Session.State.City.stats.deliveries,Is.GreaterThan(0));Assert.That(root.Session.State.Find("city_warehouse").refined>0,Is.True);yield return null;
        }
        [UnityTest] public IEnumerator ExpandedBuildingsRemainClickableAfterTheirLargestVisualSwap()
        {
            Start();root.Session.State.City.unlocked=Zone.Industry;
            foreach(string id in new[]{"farm_grow_08","coffee_12","settlement_18","bakery_12"})
            foreach(int level in new[]{0,5000})
            {
                var state=root.Session.State.Find(id);if(state.family=="settlement_plot")state.family="residential_house";state.level=level;
                root.Refresh();yield return null;
                var visual=root.Site(id).VisualRoot.GetComponentInChildren<Weedtopia.Buildings.Presentation.BuildingSpriteVisual>();
                root.CameraRig.ShowOverview();root.CameraRig.Focus(visual.SpriteRenderer.bounds.center);root.CameraRig.SetDiagnosticZoom(28);
                float deadline=Time.realtimeSinceStartup+4;
                while(Vector3.Distance(root.CameraRig.transform.position,root.CameraRig.TargetPosition)>.005f&&Time.realtimeSinceStartup<deadline)yield return null;
                yield return null;Canvas.ForceUpdateCanvases();
                var screen=root.CameraRig.RenderCamera.WorldToScreenPoint(visual.SpriteRenderer.bounds.center);
                Assert.That(root.CameraRig.TrySelect(screen),Is.True,id+" level "+level);
                Assert.That(root.SelectedId,Is.EqualTo(id));
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CitySalesRathausSpinAndSettlementUnlockAreConnected()
        {
            Start();DriveUntil(()=>root.Session.State.City.unlocked>=Zone.Settlement);Assert.That(root.Session.State.City.stats.coffeeSold>0,Is.True);
            root.Ui.ShowTownHall();Assert.That(root.Ui.Element<Text>("scores").text,Does.Contain("Online-Rangliste noch nicht verbunden"));Assert.That(root.Session.State.City.stats.spins,Is.GreaterThan(0));yield return null;
        }
        [UnityTest] public IEnumerator SaveSceneReloadAndContinueKeepResidentialChoiceAndProgress()
        {
            Start();DriveUntil(()=>root.Session.State.City.unlocked>=Zone.Industry);root.Save();string before=JsonUtility.ToJson(root.Session.State);
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();root.enabled=false;root.UseTestStore(path,clock);Assert.That(root.Continue(),Is.True);
            Assert.That(JsonUtility.ToJson(root.Session.State),Is.EqualTo(before));Assert.That(root.Session.State.buildings.Any(x=>x.family=="residential_house"&&x.level>0),Is.True);
            Assert.That(root.Session.State.buildings.Any(x=>x.family=="park"&&x.level>0),Is.True);yield return null;
        }
        [UnityTest] public IEnumerator IndustrySellsButterProductsAndReachesFreeplay()
        {
            Start();DriveUntil(()=>root.Session.State.City.freeplay);Assert.That(root.Session.State.City.stats.butterProduced>0,Is.True);Assert.That(root.Session.State.City.stats.pastrySold>0,Is.True);
            Assert.That(root.Session.State.grinders,Is.GreaterThanOrEqualTo(50));Assert.That(root.Ui.Element<Text>("questTitle").text,Does.Contain("Freies Spiel"));yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator BakerVanDrivesBothDirectionsAndShowsButterOnlyAfterLoading()
        {
            Start();var state=root.Session.State;state.City.unlocked=Zone.Industry;
            state.Find("butter_factory").level=state.Find("bakery_01").level=1;state.Find("butter_factory").butter=24;
            var baker=root.Session.Workers.Grant("baker");root.Session.Workers.Assign(baker.id,"bakery_01",0);
            root.Session.Advance(1);root.Refresh();root.Ui.Interaction.OpenBuilding("bakery_01");yield return null;
            var trip=state.deliveries.Single(t=>t.workerId==baker.id);var route=root.Presentation.Route(trip.route);
            var van=UnityEngine.Object.FindObjectsByType<DeliveryView>(FindObjectsSortMode.None).Single(v=>v.GetComponentsInChildren<SpriteRenderer>().Any(x=>x.name=="TruckArtwork"));
            var cargo=van.GetComponentsInChildren<SpriteRenderer>(true).Single(x=>x.name=="Cargo");
            Assert.That(Vector3.Distance(van.transform.position,route.Position(1)+Vector3.up*.06f),Is.LessThan(.001));
            Assert.That(cargo.gameObject.activeSelf,Is.False);Assert.That(root.Ui.Interaction.status.text,Does.Contain("Zur Butterfabrik"));
            var actor=root.Presentation.characters.Single(a=>a.workplaceId=="bakery_01");
            Assert.That(actor.AwayForPickup,Is.True);Assert.That(actor.spriteRenderer.enabled,Is.False);
            Assert.That(root.Ui.Interaction.staffAction.interactable,Is.False);
            root.Session.Advance(trip.remainingSeconds);root.Refresh();yield return null;
            Assert.That(Vector3.Distance(van.transform.position,route.Position(0)+Vector3.up*.06f),Is.LessThan(.001));
            Assert.That(cargo.gameObject.activeSelf,Is.True);Assert.That(cargo.sprite,Is.EqualTo(root.Presentation.art.Get("butter")));
            Assert.That(root.Ui.Interaction.status.text,Does.Contain("Butter zur Bäckerei"));
            root.Save();Assert.That(File.Exists(path),Is.True);
            root.Session.Advance(trip.remainingSeconds);root.Refresh();yield return null;
            Assert.That(state.City.stats.pastrySold>0,Is.True);Assert.That(root.Ui.Interaction.staffAction.interactable,Is.True);LogAssert.NoUnexpectedReceived();
            Assert.That(actor.AwayForPickup,Is.False);Assert.That(actor.spriteRenderer.enabled,Is.True);
        }
        [UnityTest] public IEnumerator EveryBakeryHasAnAuthoredRoadAndTheCourierPoolCoversAllWorkers()
        {
            Start();var pickups=root.Definitions.routes.Where(r=>r.pickupByRecipient).ToArray();Assert.That(pickups.Length,Is.EqualTo(12));
            foreach(var definition in pickups)
            {
                var route=root.Presentation.Route(definition.id);Assert.That(route.conveyor,Is.False,definition.id);Assert.That(route.cargoMarkers,Is.Empty);
                Assert.That(route.GetComponent<MeshRenderer>().sharedMaterial.name,Is.EqualTo("MossyFlagstones"));
                Assert.That(route.GetComponent<MeshFilter>().sharedMesh,Is.Not.Null);
                Assert.That(Vector3.Distance(route.Position(0),root.Presentation.Route("city_butter").Position(1)),Is.LessThan(.001));
            }
            Assert.That(UnityEngine.Object.FindObjectsByType<DeliveryView>(FindObjectsSortMode.None).Length,Is.GreaterThanOrEqualTo(60));
            Assert.That(root.Presentation.Route("processing_farm").conveyor,Is.True,"Farm belts remain intact");yield return null;
        }
        [UnityTest] public IEnumerator UiRaycastsBlockWorldAndPauseStopsTime()
        {
            Start();root.Ui.ShowPanel("settings");yield return null;Canvas.ForceUpdateCanvases();
            Assert.That(root.CameraRig.IsUiPoint(new Vector2(Screen.width/2f,Screen.height/2f)),Is.True);Assert.That(root.CameraRig.TrySelect(new Vector2(Screen.width/2f,Screen.height/2f)),Is.False);
            root.Pause(true);long before=root.Session.State.simulationSeconds;root.Session.Advance(10);Assert.That(root.Session.State.simulationSeconds,Is.EqualTo(before));root.Ui.Close();root.Session.Advance(1);Assert.That(root.Session.State.simulationSeconds,Is.EqualTo(before+1));
        }
        [UnityTest] public IEnumerator WorkerInventoryButtonsAssignRemoveAndImproveManager()
        {
            Start();root.Select("farm_grow_01");root.Ui.Element<Button>("ui.upgrade").onClick.Invoke();root.Session.Advance(4);
            var worker=root.Session.Workers.Grant("grow");var view=root.Ui.Interaction;view.OpenBuilding("farm_grow_01");view.staffAction.onClick.Invoke();yield return null;
            var rows=view.pickerContent.GetComponentsInChildren<InteractionRow>();Assert.That(rows.Length,Is.EqualTo(1));rows[0].choose.onClick.Invoke();
            Assert.That(worker.buildingId,Is.EqualTo("farm_grow_01"));Assert.That(root.Session.Workers.Automated(root.Session.State.Find("farm_grow_01")),Is.True);
            view.train.onClick.Invoke();Assert.That(worker.level,Is.EqualTo(2));
            view.staffAction.onClick.Invoke();Assert.That(worker.buildingId,Is.Null);Assert.That(root.Session.Workers.Automated(root.Session.State.Find("farm_grow_01")),Is.False);
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PremiumPanelCancelThenConfirmSpendsOnlyAfterExplicitClick()
        {
            Start();root.Select("farm_grow_01");root.Session.State.money=0;
            root.Ui.Element<Button>("ui.upgrade").onClick.Invoke();Assert.That(root.Ui.ModalOpen,Is.True);Assert.That(root.Session.State.grinders,Is.EqualTo(50));
            root.Ui.Element<Button>("ui.cancel").onClick.Invoke();Assert.That(root.Session.State.grinders,Is.EqualTo(50));Assert.That(root.Session.State.Find("farm_grow_01").buildingSeconds,Is.Zero);
            root.Ui.Element<Button>("ui.upgrade").onClick.Invoke();root.Ui.Element<Button>("ui.confirm").onClick.Invoke();
            Assert.That(root.Session.State.grinders,Is.EqualTo(49));Assert.That(root.Session.State.Find("farm_grow_01").buildingSeconds,Is.GreaterThan(0));yield return null;
        }
        [UnityTest] public IEnumerator MaximumUiScaleKeepsContextualActionsInsideSafeArea()
        {
            Start();root.Session.State.uiScale=1.15f;root.Refresh();root.Ui.Interaction.OpenBuilding("farm_grow_01");yield return null;Canvas.ForceUpdateCanvases();
            var corners=new Vector3[4];
            foreach(var button in root.Ui.GetComponentsInChildren<Button>())
            {
                button.GetComponent<RectTransform>().GetWorldCorners(corners);
                Assert.That(corners.Min(x=>x.x),Is.GreaterThanOrEqualTo(-1),button.name);Assert.That(corners.Max(x=>x.x),Is.LessThanOrEqualTo(Screen.width+1),button.name);
                Assert.That(corners.Min(x=>x.y),Is.GreaterThanOrEqualTo(-1),button.name);Assert.That(corners.Max(x=>x.y),Is.LessThanOrEqualTo(Screen.height+1),button.name);
            }
        }
        [UnityTest] public IEnumerator CharacterRaycastsOpenTheMatchingAuthoredPortraitAndCanBeClosed()
        {
            Start();DriveUntil(()=>root.Session.State.City.freeplay);
            foreach(var actor in root.Presentation.characters)
            {
                // The baker cannot be clicked at home during an actual pickup trip.
                for(int i=0;i<120&&actor.AwayForPickup;i++)root.Session.Advance(1);
                Assert.That(actor.AwayForPickup,Is.False);root.Refresh();yield return null;
                root.Ui.Close();root.Focus(actor.workplaceId);root.CameraRig.Focus(actor.transform.position);
                float settleDeadline=Time.realtimeSinceStartup+4;
                while(Vector3.Distance(root.CameraRig.transform.position,root.CameraRig.TargetPosition)>.005f&&Time.realtimeSinceStartup<settleDeadline)yield return null;
                Assert.That(Vector3.Distance(root.CameraRig.transform.position,root.CameraRig.TargetPosition),Is.LessThan(.005f),"Camera must settle before the projected NPC click");
                yield return null;
                Physics.SyncTransforms();var collider=actor.GetComponent<Collider>();Assert.That(collider,Is.Not.Null,actor.speakerId);
                Vector2 point=root.CameraRig.RenderCamera.WorldToScreenPoint(collider.bounds.center);
                bool selected=root.CameraRig.TrySelect(point);
                var ray=root.CameraRig.RenderCamera.ScreenPointToRay(point);
                string diagnostic=selected?"":string.Join(",",Physics.RaycastAll(ray,2000).Select(x=>x.collider.name+":"+x.distance))+" ray="+ray+" bounds="+collider.bounds+" layer="+collider.gameObject.layer+" direct="+collider.Raycast(ray,out _,2000)+" bboxHit="+collider.bounds.IntersectRay(ray)+" distanceFromRay="+Vector3.Cross(ray.direction,collider.bounds.center-ray.origin).magnitude.ToString("R")+" localPhysics="+actor.gameObject.scene.GetPhysicsScene()+" defaultPhysics="+Physics.defaultPhysicsScene;
                Assert.That(selected,Is.True,actor.speakerId+" at "+point+"; screen "+Screen.width+"x"+Screen.height+"; covered by UI="+root.CameraRig.IsUiPoint(point)+"; modal="+root.Ui.ModalOpen+"; collider="+collider.enabled+"/"+collider.gameObject.activeInHierarchy+"; sprite="+actor.spriteRenderer.enabled+"; hits="+diagnostic);
                Assert.That(root.SelectedId,Is.EqualTo(actor.workplaceId));Assert.That(root.Ui.ModalOpen,Is.True);
                Assert.That(root.Ui.Element<Image>("conversationPortrait").sprite,Is.EqualTo(root.Presentation.art.Get("portrait_"+actor.speakerId)));
                Assert.That(root.Ui.Element<Text>("conversationTitle").text,Is.EqualTo(root.Definitions.Text("speaker."+actor.speakerId)));
                root.Ui.Element<Button>("ui.dialog_continue").onClick.Invoke();Assert.That(root.Ui.ModalOpen,Is.False);
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator WorkerRoleFiltersAndRarityCardsKeepAllTwelveRolesVisible()
        {
            Start();foreach(var role in root.Definitions.roles)root.Session.Workers.Grant(role.id,4);
            root.Ui.ShowWorkers();yield return null;
            for(int i=0;i<root.Definitions.roles.Length;i++)
            {
                root.Ui.Element<Dropdown>("filter").value=i+1;yield return null;
                var row=root.Ui.Element<RectTransform>("workerList").GetComponentsInChildren<Button>();Assert.That(row.Length,Is.EqualTo(1));row[0].onClick.Invoke();
                var role=root.Definitions.roles[i];Assert.That(root.Presentation.workerPortrait.sprite,Is.EqualTo(root.Presentation.art.Get("portrait_"+PresentationArt.CharacterForRole(role.id))));
                Assert.That(root.Presentation.workerRoleIcon.sprite,Is.EqualTo(root.Presentation.art.Get(PresentationArt.IconForRole(role.id))));
                Assert.That(root.Presentation.workerName.text,Does.Not.Contain("["));
                var text=root.Ui.Element<Text>("workerText");Canvas.ForceUpdateCanvases();
                float needed=text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text,text.GetGenerationSettings(text.rectTransform.rect.size))/text.pixelsPerUnit;
                Assert.That(needed,Is.LessThanOrEqualTo(text.rectTransform.rect.height),role.id+" full rarity detail");
                for(int rarity=0;rarity<5;rarity++){var worker=root.Session.State.workers.Single(x=>x.role==role.id);worker.rarity=rarity;root.Presentation.PaintWorker(worker);Assert.That(root.Presentation.workerRarity.color.a,Is.EqualTo(1));}
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}

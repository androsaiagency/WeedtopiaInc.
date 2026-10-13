using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Application;
using Weedtopia.Persistence;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Rendered play-mode evidence with an isolated save, actual scene travel and explicitly diagnostic growth fixtures.</summary>
    [InitializeOnLoad] public static class PurpleGardenEvidence
    {
        private const string Key="Weedtopia.PurpleGardenEvidence",Folder="Docs/PurpleGarden";
        private static GameRoot root;
        private static ManualClock clock;
        private static List<Func<bool>> steps;
        private static readonly List<string> shots=new List<string>();
        private static readonly List<CaptureDimensions> dimensions=new List<CaptureDimensions>();
        private static int index,frames;
        private static int expectedWidth=1920,expectedHeight=1080;
        private static string requested,pending,travel,longBodyResident;
        private static readonly List<string> plannedShots=new List<string>();
        private static readonly List<string> traveledCities=new List<string>();
        private static DateTime requestedUtc;
        private static Dictionary<string,string> previousCityProgress;
        private static Dictionary<string,string> previousCityBuildings;
        private static Dictionary<string,string> previousCityWorkers;
        private static SaveOfflineReceipt saveOffline;
        private static readonly string[] PreviousCities={"burning_hills","big_rock_city","seven_hills","little_montana","cassa_vadela","mark_wood_city","green_leaf_village"};
        private static double deadline,settle,resizeDeadline;
        static PurpleGardenEvidence(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
        public static void Begin()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Owned batch editor required");
            BurningHillsLayoutInstaller.RequireSafeEditor();
            SessionState.SetBool(Key+".hadPreference",PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey));
            SessionState.SetString(Key+".preference",PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey));
            Directory.CreateDirectory(Folder+"/Screenshots");
            ProductionUiEvidence.Reset();
            shots.Clear();dimensions.Clear();plannedShots.Clear();traveledCities.Clear();
            index=0;frames=0;deadline=0;steps=null;requested=null;pending=null;travel=null;saveOffline=null;
            previousCityProgress=null;previousCityBuildings=null;previousCityWorkers=null;
            SessionState.SetBool(Key,true);EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            EditorSceneManager.OpenScene(GameScaffold.GameScene);SetSize(1920,1080);EditorApplication.EnterPlaymode();
        }
        private static void SetSize(int width,int height)
        {
            // This shared helper uses GameViewSizes.currentGroup and SizeSelectionCallback;
            // selecting the Standalone group cannot resize an Android-target Game View.
            PhoneUiEvidence.FixedTestView(width,height);expectedWidth=width;expectedHeight=height;
        }
        private static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+600;
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Purple Garden Game View capture");
                if(steps==null)
                {
                    root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();if(root==null)return;
                    root.enabled=false;clock=new ManualClock(new DateTime(2026,10,9,12,0,0,DateTimeKind.Utc));
                    root.UseTestStore(Path.GetFullPath(Folder+"/capture_save.json"),clock);root.NewGame("Stadtchronik");Prepare();
                }
                if(++frames<12||EditorApplication.timeSinceStartup<settle)
                {if(root!=null&&!root.TravelLoading)root.Refresh();EditorApplication.QueuePlayerLoopUpdate();return;}
                if(pending!=null)
                {
                    if(!File.Exists(pending)||File.GetLastWriteTimeUtc(pending)<requestedUtc){EditorApplication.QueuePlayerLoopUpdate();return;}
                    var png=File.ReadAllBytes(pending);
                    if(png.Length<24){EditorApplication.QueuePlayerLoopUpdate();return;}
                    int pngWidth=(png[16]<<24)|(png[17]<<16)|(png[18]<<8)|png[19];
                    int pngHeight=(png[20]<<24)|(png[21]<<16)|(png[22]<<8)|png[23];
                    if(pngWidth!=expectedWidth||pngHeight!=expectedHeight)
                        throw new InvalidOperationException("Captured PNG has unexpected dimensions: "+pending+" "+pngWidth+"x"+pngHeight+", expected "+expectedWidth+"x"+expectedHeight);
                    dimensions.Add(new CaptureDimensions{path=pending,requestedWidth=expectedWidth,requestedHeight=expectedHeight,renderedWidth=Screen.width,renderedHeight=Screen.height,pngWidth=pngWidth,pngHeight=pngHeight});
                    shots.Add(pending);pending=null;index++;frames=0;return;
                }
                if(requested!=null)
                {
                    if(Screen.width!=expectedWidth||Screen.height!=expectedHeight)
                    {
                        if(EditorApplication.timeSinceStartup>resizeDeadline)
                            throw new TimeoutException("Game View did not reach requested "+expectedWidth+"x"+expectedHeight+"; rendered "+Screen.width+"x"+Screen.height);
                        EditorApplication.QueuePlayerLoopUpdate();return;
                    }
                    // A resolution change reflows the scroll content after the shot action.
                    // Validate the eighth row against the settled, actual viewport before capture.
                    if(Path.GetFileName(requested).StartsWith("18_city_travel_",StringComparison.Ordinal))
                        PurpleGardenTravelVisible();
                    pending=requested;requested=null;ProductionUiEvidence.Inspect(root.Ui,Path.GetFileName(pending));
                    requestedUtc=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(pending);frames=0;EditorApplication.QueuePlayerLoopUpdate();return;
                }
                if(index<steps.Count){if(!steps[index]())EditorApplication.QueuePlayerLoopUpdate();}
                else
                {
                    if(shots.Count!=plannedShots.Count)throw new InvalidOperationException("Capture count does not match planned evidence frames");
                    RequirePreviousCityPreservation();
                    if(saveOffline==null||!saveOffline.passed)throw new InvalidOperationException("Eight-city save/offline evidence did not run");
                    string fingerprint=ProductionVisualPreflight.Fingerprint();ProductionUiEvidence.Write(fingerprint,Folder+"/ui_overflow.json",new[]{"1920x1080","1366x768","2560x1080","1080x1920"});
                    File.WriteAllText(Folder+"/gameview_receipt.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),fingerprint=fingerprint,expectedScreenshots=plannedShots.Count,screenshots=shots.ToArray(),plannedCaptures=plannedShots.ToArray(),captureDimensions=dimensions.ToArray(),realSceneTravel=traveledCities.ToArray(),longBodyResident=longBodyResident,saveOffline=saveOffline,preservedPreviousCities=PreviousCities},true));
                    RestoreLanguage();SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);return;
                }
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch(Exception ex){RestoreLanguage();SessionState.SetBool(Key,false);File.WriteAllText(Folder+"/gameview_error.txt",ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        [Serializable] private sealed class Receipt
        {
            public string utc,fingerprint,longBodyResident;
            public string mode="actual_playmode_gameview";
            public string saveScope="Docs/PurpleGarden/capture_save.json and its isolated .scores file";
            public string fixtureMeaning="Population, levels and story counters are explicit diagnostic fixtures for visual coverage; they do not prove a played economy or device interaction.";
            public int expectedScreenshots;
            public bool userAcceptance=false,diagnosticGrowthFixtures=true,fullEconomyPlayed=false;
            public string[] screenshots,plannedCaptures,realSceneTravel;
            public CaptureDimensions[] captureDimensions;
            public SaveOfflineReceipt saveOffline;
            public string[] preservedPreviousCities;
        }
        [Serializable] private sealed class CaptureDimensions{public string path;public int requestedWidth,requestedHeight,renderedWidth,renderedHeight,pngWidth,pngHeight;}
        [Serializable] private sealed class SaveOfflineReceipt
        {
            public string savePath;
            public int expectedCities=8,loadedCities,offlineSeconds;
            public bool passed,previousQuestsPreserved,previousBuildingsPreserved,assignedWorkersPreserved,allCitiesProduced,choiceMemoryPreserved,nextOfflineReplayWasZero;
            public CityProduction[] production;
        }
        [Serializable] private sealed class CityProduction{public string city,beforeRaw,afterRaw;public bool increased;}
        [Serializable] private sealed class CityProgress
        {
            public string id;
            public Zone unlocked;
            public bool freeplay,charterAccepted;
            public long charterResidentMilestone;
            public int questIndex,storyResidentMilestone;
            public string[] talked,completedQuests;
            public CityProjectState project;
            public VipStoryState[] stories;
            public VipCollaborationState[] collaborations;
        }
        private static void RestoreLanguage()
        {
            if(SessionState.GetBool(Key+".hadPreference",false))PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey,SessionState.GetString(Key+".preference",""));
            else PlayerPrefs.DeleteKey(GameRoot.LanguagePreferenceKey);
            PlayerPrefs.Save();
        }
        private static void Capture(string name)
        {
            if(!plannedShots.Contains(name))throw new InvalidOperationException("Unplanned evidence capture "+name);
            requested=Path.GetFullPath(Folder+"/Screenshots/"+name+".png");
            frames=0;settle=EditorApplication.timeSinceStartup+2;resizeDeadline=EditorApplication.timeSinceStartup+12;
        }
        private static void Shot(string name,Action action)
        {
            plannedShots.Add(name);
            steps.Add(()=>{root.Ui.Rewards.DismissAll();action();root.Refresh();root.Ui.Notify("");Capture(name);return true;});
        }
        private static void CitiesTop()
        {
            root.Ui.ShowCities();SetCitiesScroll(1);
        }
        private static ModulePanelLayout SetCitiesScroll(float position)
        {
            root.Refresh();Canvas.ForceUpdateCanvases();
            var layout=root.Ui.Campaign.GetComponent<ModulePanelLayout>();
            if(layout?.scroll==null)throw new InvalidOperationException("Authored city campaign scroll is missing");
            layout.scroll.StopMovement();layout.scroll.verticalNormalizedPosition=position;Canvas.ForceUpdateCanvases();
            return layout;
        }
        private static CityTravelRow PurpleGardenTravelVisible()
        {
            var layout=SetCitiesScroll(0);
            var row=root.Ui.Campaign.travelRows.Single(r=>r!=null&&r.cityId=="purple_garden");
            if(!root.Ui.Campaign.gameObject.activeInHierarchy||!row.travel.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Purple Garden travel row is not active");
            bool expected=(root.Session.Campaign.Accessible("purple_garden")||root.Session.Campaign.Offered("purple_garden"))&&root.Session.State.activeCity!="purple_garden"&&!root.TravelLoading;
            if(row.travel.interactable!=expected)
                throw new InvalidOperationException("Purple Garden travel button has unexpected interactivity");
            foreach(var rect in new[]{row.progress.rectTransform,(RectTransform)row.travel.transform})
            {
                var corners=new Vector3[4];rect.GetWorldCorners(corners);
                foreach(var corner in corners)
                {
                    var local=layout.scroll.viewport.InverseTransformPoint(corner);var bounds=layout.scroll.viewport.rect;
                    if(local.x<bounds.xMin-1||local.x>bounds.xMax+1||local.y<bounds.yMin-1||local.y>bounds.yMax+1)
                        throw new InvalidOperationException("Purple Garden travel row is clipped after scrolling: "+rect.name);
                }
            }
            return row;
        }
        private static void CitiesBottom(string name,bool openOffer=false)
        {
            Shot(name,()=>
            {
                root.Ui.ShowCities();var row=PurpleGardenTravelVisible();
                if(openOffer)
                {
                    if(!row.travel.interactable||root.Session.Campaign.Accessible("purple_garden"))
                        throw new InvalidOperationException("Expected the enabled Purple Garden offer button before acceptance");
                    row.travel.onClick.Invoke();PurpleGardenTravelVisible();
                    if(root.Ui.Campaign.InvitationCityId!="purple_garden")throw new InvalidOperationException("City row did not show the Purple Garden offer");
                }
            });
        }
        private static void TravelShot(string city,string name,Action ready)
        {
            plannedShots.Add(name);
            steps.Add(()=>
            {
                if(travel==null)
                {
                    root.Ui.Rewards.DismissAll();root.Ui.ShowCities();root.Refresh();Canvas.ForceUpdateCanvases();
                    var row=city=="purple_garden"?PurpleGardenTravelVisible():root.Ui.Campaign.travelRows.Single(r=>r!=null&&r.cityId==city);
                    if(!row.travel.interactable||!root.Session.Campaign.Accessible(city))throw new InvalidOperationException("Authored travel button is unavailable for "+city);
                    row.travel.onClick.Invoke();
                    if(!root.TravelLoading)throw new InvalidOperationException("Authored city travel button did not begin a scene handoff for "+city);
                    travel=city;return false;
                }
                var candidate=UnityEngine.Object.FindFirstObjectByType<GameRoot>();
                if(candidate==null||candidate.SceneCityId!=city||candidate.Session==null)return false;
                root=candidate;root.enabled=false;travel=null;traveledCities.Add(city);RequirePreviousCityPreservation();ready();root.Refresh();root.Ui.Notify("");Capture(name);return true;
            });
        }
        private static void FreeplayFixture()
        {
            var city=root.Session.State.City;city.questIndex=root.Session.Progression.Quests.Length;
            city.completedQuests=root.Session.Progression.Quests.Select(q=>q.id).ToList();city.freeplay=true;city.unlocked=Zone.Industry;
        }
        private static void GrownCityFixture()
        {
            FreeplayFixture();var state=root.Session.State;state.money=1000000;
            var buildings=state.buildings.Where(b=>b.cityId==state.activeCity).ToArray();
            foreach(var b in buildings)
            {
                if(b.family=="settlement_plot")b.family=b.id.EndsWith("18",StringComparison.Ordinal)?"park":"residential_house";
                if(b.family!="town_hall")b.level=b.family=="residential_house"?5000:25;
                if(b.family!="residential_house")continue;
                var worker=root.Session.Workers.Grant("caretaker",4);worker.level=100;worker.rank=10;root.Session.Workers.Assign(worker.id,b.id,0);
            }
            Populate(state.activeCity,20000);
            root.Session.Stories.RefreshUnlocks();root.Refresh();
        }
        private static void Complete(string id)
        {
            var q=root.Session.Stories.Offer(id);if(q==null)throw new InvalidOperationException("Missing diagnostic offer "+id);
            if(root.Session.Stories.Accept(id,q.id,q.choices[0].id)!=Failure.None)throw new InvalidOperationException("Cannot accept "+q.id);
            PrepareGoals(q);
            if(root.Session.Stories.Claim(id,q.id)!=Failure.None)throw new InvalidOperationException("Cannot claim "+q.id);
        }

        private static void Prepare()
        {
            root.Session.CompleteIntro();root.Ui.Close();FreeplayFixture();steps=new List<Func<bool>>();
            root.SetLanguage("de");
            Populate("burning_hills",3000);root.Session.Campaign.RefreshMilestones();
            Require(root.Session.Campaign.Accept("big_rock_city"),"Accept existing second-city fixture");
            Populate("big_rock_city",2000);root.Session.Campaign.RefreshMilestones();
            Require(root.Session.Campaign.Accept("seven_hills"),"Accept existing third-city fixture");
            Populate("burning_hills",5000);Populate("big_rock_city",5000);Populate("seven_hills",5000);
            root.Session.Campaign.RefreshMilestones();
            Require(root.Session.Campaign.Accept("little_montana"),"Accept existing fourth-city fixture");
            Populate("burning_hills",15000);Populate("big_rock_city",15000);Populate("seven_hills",10000);Populate("little_montana",10000);
            root.Session.Campaign.RefreshMilestones();
            Require(root.Session.Campaign.Accept("cassa_vadela"),"Accept existing fifth-city fixture");
            Populate("burning_hills",20000);Populate("big_rock_city",20000);Populate("seven_hills",20000);Populate("little_montana",20000);Populate("cassa_vadela",20000);
            root.Session.Campaign.RefreshMilestones();
            Require(root.Session.Campaign.Accept("mark_wood_city"),"Accept existing sixth-city fixture");
            Populate("burning_hills",40000);Populate("big_rock_city",40000);Populate("seven_hills",40000);Populate("little_montana",40000);Populate("cassa_vadela",40000);Populate("mark_wood_city",50000);
            root.Session.Campaign.RefreshMilestones();
            Require(root.Session.Campaign.Accept("green_leaf_village"),"Accept existing seventh-city fixture");
            foreach(var city in PreviousCities.Take(6))Populate(city,70000);
            Populate("green_leaf_village",79999);root.Session.Campaign.RefreshMilestones();
            if(root.Session.Campaign.UnlockProgress("purple_garden")!=499999||root.Session.Campaign.Offered("purple_garden"))
                throw new InvalidOperationException("Expected the exact 499999 locked fixture");
            CitiesBottom("01_locked_499999");
            Shot("02_exact_500000_offer",()=>
            {
                Populate("green_leaf_village",80000);root.Session.Campaign.RefreshMilestones();
                if(root.Session.Campaign.UnlockProgress("purple_garden")!=500000||!root.Session.Campaign.Offered("purple_garden"))
                    throw new InvalidOperationException("Expected exactly 500000 and Purple Garden offered");
                root.Ui.ShowCities();PurpleGardenTravelVisible().travel.onClick.Invoke();SetCitiesScroll(1);
                if(root.Ui.Campaign.InvitationCityId!="purple_garden")throw new InvalidOperationException("Eighth authored row did not show invitation");
            });
            Shot("03_rocko_eighth_city_mandate",()=>
            {
                root.Ui.Close();root.Ui.SpeakToCharacter("rocko");SetCitiesScroll(1);
                if(root.Ui.Campaign.InvitationCityId!="purple_garden")throw new InvalidOperationException("Rocko did not open eighth-city mandate");
            });
            Shot("04_mandate_accepted",()=>
            {
                var beforeMoney=root.Session.State.money;int beforeCards=root.Session.State.cards;
                root.Ui.Campaign.accept.onClick.Invoke();
                if(!root.Session.Campaign.Accessible("purple_garden"))throw new InvalidOperationException("UI did not accept Purple Garden");
                if(root.Session.State.money!=beforeMoney+12500||root.Session.State.cards!=beforeCards+7)
                    throw new InvalidOperationException("Purple Garden bridge must grant exactly 12500 money and seven cards");
                if(root.Session.Campaign.Accept("purple_garden")!=Failure.NotReady||root.Session.State.money!=beforeMoney+12500||root.Session.State.cards!=beforeCards+7)
                    throw new InvalidOperationException("Purple Garden bridge was awarded more than once");
                PreparePreviousCityPreservation();root.Ui.Rewards.DismissAll();CitiesTop();
            });
            TravelShot("purple_garden","05_arrival_unbuilt",()=>
            {
                if(root.Session.Progression.Current?.id!="pg_arrival"||root.Session.State.Find("pg_farm_grow_01").level!=0)
                    throw new InvalidOperationException("Expected the untouched eighth-city arrival quest and unbuilt first garden");
                root.Ui.Close();root.Focus("pg_town_hall");root.CameraRig.SetDiagnosticZoom(35);
            });
            Shot("06_initial_quest",()=>
            {
                root.Ui.Experience.quests.Open("pg_arrival");
                if(root.Session.Progression.Current?.id!="pg_arrival")throw new InvalidOperationException("Unexpected initial Purple Garden quest");
            });
            LandmarkShot("07_water_court_before_growth","pg_joint_water_court",false,false);
            LandmarkShot("08_seed_archive_before_growth","pg_joint_seed_archive",false,false);
            LandmarkShot("09_night_bloom_before_growth","pg_joint_night_bloom",false,false);
            Shot("10_violet_glasshouses_diagnostic_growth",()=>{GrownCityFixture();root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Farm);root.CameraRig.SetDiagnosticZoom(46);});
            Shot("11_bloom_market_diagnostic_growth",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.City);root.CameraRig.SetDiagnosticZoom(40);});
            Shot("12_water_terraces_diagnostic_growth",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Settlement);root.CameraRig.SetDiagnosticZoom(45);});
            Shot("13_seed_archive_diagnostic_growth",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Industry);root.CameraRig.SetDiagnosticZoom(42);});
            LandmarkShot("13b_water_court_supplies","pg_joint_water_court",true,false);
            LandmarkShot("13c_seed_archive_supplies","pg_joint_seed_archive",true,false);
            LandmarkShot("13d_night_bloom_supplies","pg_joint_night_bloom",true,false);
            Shot("14_eight_permanent_residents",()=>
            {
                var cast=new[]{"pg_iris","pg_malik","pg_viola","pg_theo","pg_jun","pg_ada","pg_ravi","pg_selma"};
                if(!root.Session.Stories.People.Select(p=>p.id).OrderBy(id=>id).SequenceEqual(cast.OrderBy(id=>id))||root.Session.Stories.People.Any(p=>!root.Session.Stories.Unlocked(p.id)))
                    throw new InvalidOperationException("Expected the eight original permanent Purple Garden residents unlocked");
                root.Ui.ShowStories();root.Ui.Stories.peopleTab.onClick.Invoke();
            });
            Shot("14b_remaining_permanent_residents",()=>
            {
                root.Ui.ShowStories();root.Ui.Stories.peopleTab.onClick.Invoke();Canvas.ForceUpdateCanvases();
                root.Ui.Stories.rosterScroll.StopMovement();root.Ui.Stories.rosterScroll.verticalNormalizedPosition=0;
            });
            Shot("15_theo_third_chapter_choice",()=>{Complete("pg_theo");Complete("pg_theo");root.Ui.ShowStories("pg_theo");});
            Shot("16_theo_teaching_bench_choice_accepted",()=>
            {
                root.Ui.Stories.alternate.onClick.Invoke();
                if(root.Session.Stories.Active("pg_theo")?.choiceId!="teaching_bench")throw new InvalidOperationException("UI alternate did not choose teaching_bench");
            });
            Shot("17_theo_saved_choice_memory",()=>
            {
                var q=root.Session.Stories.ActiveDefinition("pg_theo");if(q==null)throw new InvalidOperationException("No active Theo choice");
                CompleteActive("pg_theo",q);RequireTheoMemory(3);root.Ui.Rewards.DismissAll();root.Ui.Close();root.StoryWorld.Focus("pg_theo");root.CameraRig.SetDiagnosticZoom(23);
                longBodyResident=root.Session.Stories.People.Where(p=>p.id!="pg_theo")
                    .OrderByDescending(p=>root.Definitions.Text(p.biographyKey).Length+root.Definitions.Text(root.Session.Stories.Offer(p.id).bodyKey).Length).First().id;
            });
            foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768),new Vector2Int(2560,1080),new Vector2Int(1080,1920)})
            {
                Shot("18_city_travel_"+resolution.x+"x"+resolution.y,()=>
                {
                    SetSize(resolution.x,resolution.y);root.Session.State.uiScale=resolution.x==1366?1.15f:1;
                    root.Ui.ShowCities();PurpleGardenTravelVisible();
                });
                Shot("19_long_story_body_"+resolution.x+"x"+resolution.y,()=>
                {
                    root.Ui.ShowStories(longBodyResident);
                    if(string.IsNullOrWhiteSpace(root.Ui.Stories.biography.text)||string.IsNullOrWhiteSpace(root.Ui.Stories.description.text))
                        throw new InvalidOperationException("Long story details did not render");
                });
            }
            foreach(var lang in new[]{"de","en","es","fr","ru"})
                Shot("19b_localized_story_"+lang,()=>{SetSize(1920,1080);root.Session.State.uiScale=1;root.SetLanguage(lang);root.Ui.ShowStories(longBodyResident);});
            Shot("19c_german_restored",()=>{root.SetLanguage("de");root.Ui.ShowStories(longBodyResident);});
            Shot("19d_portrait_permanent_residents_top",()=>
            {
                SetSize(1080,1920);root.Ui.ShowStories();root.Ui.Stories.peopleTab.onClick.Invoke();Canvas.ForceUpdateCanvases();
                root.Ui.Stories.rosterScroll.StopMovement();root.Ui.Stories.rosterScroll.verticalNormalizedPosition=1;
            });
            Shot("19e_portrait_permanent_residents_bottom",()=>
            {
                root.Ui.ShowStories();root.Ui.Stories.peopleTab.onClick.Invoke();Canvas.ForceUpdateCanvases();
                root.Ui.Stories.rosterScroll.StopMovement();root.Ui.Stories.rosterScroll.verticalNormalizedPosition=0;
            });
            Shot("20_three_community_projects_completed",()=>
            {
                SetSize(1920,1080);root.Session.State.uiScale=1;
                foreach(var person in root.Session.Stories.People)
                    while(root.Session.Stories.Person(person.id).completed.Count<person.chapters.Length)Complete(person.id);
                foreach(var project in new[]{"pg_joint_water_court","pg_joint_seed_archive","pg_joint_night_bloom"})Complete(project);
                RequireTheoMemory(4);
                if(root.StoryWorld.actors.Length!=8||root.StoryWorld.actors.Any(actor=>!actor.Visible))
                    throw new InvalidOperationException("Completing Purple Garden stories removed a permanent resident");
                root.Ui.ShowStories();root.Ui.Stories.togetherTab.onClick.Invoke();
            });
            LandmarkShot("21_water_court_restored","pg_joint_water_court",true,true);
            LandmarkShot("22_open_seed_archive_teaching_bench","pg_joint_seed_archive",true,true);
            LandmarkShot("23_night_bloom_restored","pg_joint_night_bloom",true,true);
            Shot("24_save_continue_offline_eight_cities",()=>
            {
                SaveAndContinueEightCities();
            });
            Shot("24b_eight_cities_after_offline_continue",()=>{root.Ui.Close();root.Ui.ShowCities();PurpleGardenTravelVisible();});
            foreach(var city in PreviousCities)
                TravelShot(city,"25_return_"+city,()=>
                {
                    root.Ui.Close();
                    var hall=root.Definitions.instances.Single(b=>b.cityId==city&&b.family=="town_hall");
                    root.Focus(hall.id);root.CameraRig.SetDiagnosticZoom(30);
                });
            TravelShot("purple_garden","26_roundtrip_theo_memory_preserved",()=>
            {
                RequireTheoMemory(4);
                foreach(var project in new[]{"pg_joint_water_court","pg_joint_seed_archive","pg_joint_night_bloom"})RequirePlaceStages(project,true,true);
                root.Ui.Close();root.StoryWorld.Focus("pg_theo");root.CameraRig.SetDiagnosticZoom(23);
            });
        }

        private static void LandmarkShot(string name,string project,bool grown,bool completed)
            => Shot(name,()=>{RequirePlaceStages(project,grown,completed);root.Ui.Close();root.CameraRig.Focus(Place(project).transform.position);root.CameraRig.SetDiagnosticZoom(36);});

        private static CityPlaceProgressVisual Place(string project)
            =>UnityEngine.Object.FindObjectsByType<CityPlaceProgressVisual>(FindObjectsSortMode.None).Single(p=>p.cityId=="purple_garden"&&p.collaborationId==project);

        private static void RequirePlaceStages(string project,bool grown,bool completed)
        {
            var place=Place(project);
            place.Refresh();
            if(place.keepGrowingPlaceAfterCompletion||place.closedPlace.activeSelf!=(!grown&&!completed)
                ||place.growingPlace.activeSelf!=(grown&&!completed)||place.completedPlace.activeSelf!=completed)
                throw new InvalidOperationException("Community place stages do not match the saved population/project state: "+project);
            if(completed&&project=="pg_joint_seed_archive")
                for(int index=0;index<place.choicePlaces.Length;index++)
                    if(place.choicePlaces[index].activeSelf!=(place.choiceValues[index]=="teaching_bench"))
                        throw new InvalidOperationException("Saved teaching-bench choice did not change the authored seed archive");
        }

        private static void RequireTheoMemory(int chapters)
        {
            var progress=root.Session.Stories.Person("pg_theo");
            if(progress.completed.Count!=chapters||progress.completed[2].choiceId!="teaching_bench")
                throw new InvalidOperationException("Saved Theo choice was not retained");
            root.StoryWorld.Refresh();
            var actor=root.StoryWorld.actors.Single(p=>p.vipId=="pg_theo");
            if(!actor.Visible)throw new InvalidOperationException("Completed stories removed Theo from town");
            var memories=actor.memories.Where(m=>m.chapter==2).ToArray();
            if(memories.Length!=2||memories.Any(memory=>memory.target==null))
                throw new InvalidOperationException("Theo's third chapter requires two authored choice-memory objects");
            foreach(var memory in memories)
                if(memory.target.activeSelf!=(memory.choiceId=="teaching_bench"))
                    throw new InvalidOperationException("Theo's authored memory objects did not match his saved choice");
        }
        private static string Progress(GameState state,string city)
        {
            var value=state.cities.Single(c=>c.id==city);
            return JsonUtility.ToJson(new CityProgress{id=value.id,unlocked=value.unlocked,freeplay=value.freeplay,charterAccepted=value.charterAccepted,
                charterResidentMilestone=value.charterResidentMilestone,questIndex=value.questIndex,storyResidentMilestone=value.storyResidentMilestone,
                talked=value.talked.ToArray(),completedQuests=value.completedQuests.ToArray(),project=value.project,
                stories=value.vipStories.ToArray(),collaborations=value.vipCollaborations.ToArray()});
        }
        private static string Buildings(GameState state,string city)
            =>string.Join(";",state.buildings.Where(b=>b.cityId==city).Select(b=>b.id+":"+b.family+":"+b.level));
        private static string Workers(GameState state,string city)
        {
            var buildings=new HashSet<string>(state.buildings.Where(b=>b.cityId==city).Select(b=>b.id));
            return string.Join(";",state.workers.Where(w=>buildings.Contains(w.buildingId)).Select(w=>JsonUtility.ToJson(w)));
        }
        private static void ProductionFixture(string city)
        {
            var state=root.Session.State;
            var grow=state.buildings.First(b=>b.cityId==city&&b.family=="cultivation_plot");
            grow.level=Math.Max(25,grow.level);grow.raw=0;grow.cycleProgress=0;
            if(!state.workers.Any(w=>w.buildingId==grow.id&&w.slot==0))
                state.workers.Add(new WorkerState{id="pg_offline_"+grow.id,role="grow",buildingId=grow.id,slot=0,
                    attributes=new List<WorkerAttribute>{root.Definitions.Role("grow").attributes[0]}});
            state.revision++;
        }
        private static void PreparePreviousCityPreservation()
        {
            foreach(var city in PreviousCities)ProductionFixture(city);
            var state=root.Session.State;string active=state.activeCity;
            // Prime each diagnostic source population through the existing story service before freezing its retained progress.
            try{foreach(var city in PreviousCities){state.activeCity=city;root.Session.Stories.RefreshUnlocks();}}
            finally{state.activeCity=active;}
            previousCityProgress=PreviousCities.ToDictionary(city=>city,city=>Progress(state,city));
            previousCityBuildings=PreviousCities.ToDictionary(city=>city,city=>Buildings(state,city));
            previousCityWorkers=PreviousCities.ToDictionary(city=>city,city=>Workers(state,city));
        }
        private static void RequirePreviousCityPreservation()
        {
            if(previousCityProgress==null)return;
            var state=root.Session.State;
            foreach(var city in PreviousCities)
            {
                if(Progress(state,city)!=previousCityProgress[city])throw new InvalidOperationException("Previous-city quests/story/charter changed: "+city);
                if(Buildings(state,city)!=previousCityBuildings[city])throw new InvalidOperationException("Previous-city building/family/level changed: "+city);
                if(Workers(state,city)!=previousCityWorkers[city])throw new InvalidOperationException("Previous-city worker assignments changed: "+city);
            }
        }
        private static void SaveAndContinueEightCities()
        {
            RequirePreviousCityPreservation();ProductionFixture("purple_garden");
            var before=root.Session.State;var beforeProgress=before.cities.ToDictionary(c=>c.id,c=>Progress(before,c.id));
            var beforeBuildings=before.cities.ToDictionary(c=>c.id,c=>Buildings(before,c.id));
            var beforeWorkers=before.cities.ToDictionary(c=>c.id,c=>Workers(before,c.id));
            var beforeProduction=before.cities.ToDictionary(c=>c.id,c=>c.stats.rawProduced);
            long seconds=before.simulationSeconds;root.Session.Save();
            var saved=new LocalSaveStore(root.SavePath,root.Definitions).Load();
            if(saved.State==null||saved.Recovered||saved.State.cities.Count!=8||saved.State.activeCity!="purple_garden")
                throw new InvalidOperationException("Fresh isolated eight-city save did not load");
            clock.UtcNow=clock.UtcNow.AddSeconds(64);
            if(!root.Continue()||root.TravelLoading||ReferenceEquals(root.Session.State,before))
                throw new InvalidOperationException("Purple Garden Continue did not activate the loaded isolated save");
            var after=root.Session.State;
            if(after.simulationSeconds-seconds!=64)throw new InvalidOperationException("Continue did not apply exactly 64 offline seconds");
            foreach(var city in after.cities)
                if(Progress(after,city.id)!=beforeProgress[city.id]||Buildings(after,city.id)!=beforeBuildings[city.id]||Workers(after,city.id)!=beforeWorkers[city.id])
                    throw new InvalidOperationException("Save/offline changed authored city progression or assignments: "+city.id);
            var production=after.cities.Select(c=>new CityProduction{city=c.id,beforeRaw=beforeProduction[c.id].ToString(),afterRaw=c.stats.rawProduced.ToString(),
                increased=c.stats.rawProduced>beforeProduction[c.id]}).ToArray();
            if(production.Any(c=>!c.increased))throw new InvalidOperationException("Offline simulation did not produce in all eight cities");
            RequireTheoMemory(4);RequirePreviousCityPreservation();
            if(root.Session.ApplyOffline().Seconds!=0)throw new InvalidOperationException("Offline continuation replayed an already applied interval");
            saveOffline=new SaveOfflineReceipt{savePath=root.SavePath,loadedCities=after.cities.Count,offlineSeconds=64,passed=true,
                previousQuestsPreserved=true,previousBuildingsPreserved=true,assignedWorkersPreserved=true,allCitiesProduced=true,
                choiceMemoryPreserved=true,nextOfflineReplayWasZero=true,production=production};
        }
        private static void Require(Failure failure,string action)
        {if(failure!=Failure.None)throw new InvalidOperationException(action+": "+failure);}
        private static void Populate(string city,int residents)
        {
            var game=root.Session;var plots=game.State.buildings.Where(b=>b.cityId==city&&(b.family=="settlement_plot"||b.family=="residential_house")).ToArray();
            foreach(var plot in plots){plot.level=0;game.State.workers.RemoveAll(w=>w.buildingId==plot.id);}
            var choices=new Dictionary<int,(int level,int workerLevel,int rank)>();
            for(int tier=1;tier<VisualTiers.Starts.Length;tier++)
                for(int level=1;level<=100;level++)for(int rank=1;rank<=10;rank++)
                {
                    int count=(int)Math.Floor(game.Definitions.Building("residential_house").ResidentCapacity(VisualTiers.Starts[tier])*(1+game.Definitions.workerAttributeStep*level*rank));
                    if(!choices.ContainsKey(count))choices[count]=(VisualTiers.Starts[tier],level,rank);
                }
            var parts=new List<int>();int remaining=residents;
            while(remaining>=5000){parts.Add(5000);remaining-=5000;}
            if(remaining>0)
            {
                if(choices.ContainsKey(remaining))parts.Add(remaining);
                else{int first=choices.Keys.First(n=>n<remaining&&choices.ContainsKey(remaining-n));parts.Add(first);parts.Add(remaining-first);}
            }
            for(int index=0;index<parts.Count;index++)
            {
                var configuration=choices[parts[index]];var building=plots[index];building.family="residential_house";building.level=configuration.level;
                game.State.workers.Add(new WorkerState{id="pg_capture_"+building.id,role="caretaker",level=configuration.workerLevel,rank=configuration.rank,
                    buildingId=building.id,slot=0,attributes=new List<WorkerAttribute>{WorkerAttribute.Residents}});
            }
            if(game.Simulation.Residents(city)!=residents)throw new InvalidOperationException("Population fixture mismatch for "+city);
            game.State.revision++;
        }
        private static void CompleteActive(string id,StoryQuest q)
        {
            PrepareGoals(q);root.Refresh();root.Ui.ShowStories(id);root.Ui.Stories.claim.onClick.Invoke();
            if(root.Session.Stories.ActiveDefinition(id)!=null)throw new InvalidOperationException("UI did not claim active "+q.id);
        }
        private static void PrepareGoals(StoryQuest q)
        {
            // Explicit diagnostic growth fixtures for visual coverage, not a played economy proof.
            var state=root.Session.State;state.City.stats.deliveries+=1000;state.City.stats.delivered+=100000;state.City.stats.coffeeSold+=100000;
            state.City.stats.rawProduced+=100000;state.City.stats.refinedProduced+=100000;state.City.stats.butterProduced+=100000;state.City.stats.pastrySold+=100000;
            foreach(var goal in q.goals.Where(g=>g.kind==StoryGoalKind.NpcVisited))for(int i=0;i<goal.count;i++)root.Session.Stories.Visit(goal.target);
            foreach(var goal in q.goals.Where(g=>g.kind==StoryGoalKind.Level))
            {
                var building=state.buildings.First(b=>b.cityId==state.activeCity&&(b.id==goal.target||b.family==goal.target));building.level=Math.Max(goal.count,building.level);
            }
            state.revision++;
        }
    }
}

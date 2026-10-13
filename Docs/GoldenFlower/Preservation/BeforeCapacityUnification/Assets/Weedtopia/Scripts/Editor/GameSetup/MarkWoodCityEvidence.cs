using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Application;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Rendered play-mode evidence with an isolated save, actual scene travel and explicitly diagnostic growth fixtures.</summary>
    [InitializeOnLoad] public static class MarkWoodCityEvidence
    {
        private const string Key="Weedtopia.MarkWoodCityEvidence",Folder="Docs/MarkWoodCity";
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
        private static double deadline,settle,resizeDeadline;
        static MarkWoodCityEvidence(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
        public static void Begin()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Owned batch editor required");
            BurningHillsLayoutInstaller.RequireSafeEditor();
            SessionState.SetBool(Key+".hadPreference",PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey));
            SessionState.SetString(Key+".preference",PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey));
            Directory.CreateDirectory(Folder+"/Screenshots");
            ProductionUiEvidence.Reset();
            shots.Clear();dimensions.Clear();plannedShots.Clear();traveledCities.Clear();
            index=0;frames=0;deadline=0;steps=null;requested=null;pending=null;travel=null;
            SessionState.SetBool(Key,true);
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
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Mark Wood City Game View capture");
                if(steps==null)
                {
                    root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();if(root==null)return;
                    root.enabled=false;clock=new ManualClock(new DateTime(2026,10,8,12,0,0,DateTimeKind.Utc));
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
                    // Validate the sixth row against the settled, actual viewport before capture.
                    if(Path.GetFileName(requested).StartsWith("18_city_travel_",StringComparison.Ordinal))
                        MarkWoodTravelVisible();
                    pending=requested;requested=null;ProductionUiEvidence.Inspect(root.Ui,Path.GetFileName(pending));
                    requestedUtc=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(pending);frames=0;EditorApplication.QueuePlayerLoopUpdate();return;
                }
                if(index<steps.Count){if(!steps[index]())EditorApplication.QueuePlayerLoopUpdate();}
                else
                {
                    if(shots.Count!=plannedShots.Count)throw new InvalidOperationException("Capture count does not match planned evidence frames");
                    string fingerprint=ProductionVisualPreflight.Fingerprint();ProductionUiEvidence.Write(fingerprint,Folder+"/ui_overflow.json",new[]{"1920x1080","1366x768","2560x1080","1080x1920"});
                    File.WriteAllText(Folder+"/gameview_receipt.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),fingerprint=fingerprint,expectedScreenshots=plannedShots.Count,screenshots=shots.ToArray(),plannedCaptures=plannedShots.ToArray(),captureDimensions=dimensions.ToArray(),realSceneTravel=traveledCities.ToArray(),longBodyResident=longBodyResident},true));
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
            public string saveScope="Docs/MarkWoodCity/capture_save.json and its isolated .scores file";
            public string fixtureMeaning="Population, levels and story counters are explicit diagnostic fixtures for visual coverage; they do not prove a played economy or device interaction.";
            public int expectedScreenshots;
            public bool userAcceptance=false,diagnosticGrowthFixtures=true,fullEconomyPlayed=false;
            public string[] screenshots,plannedCaptures,realSceneTravel;
            public CaptureDimensions[] captureDimensions;
        }
        [Serializable] private sealed class CaptureDimensions{public string path;public int requestedWidth,requestedHeight,renderedWidth,renderedHeight,pngWidth,pngHeight;}
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
        private static CityTravelRow MarkWoodTravelVisible()
        {
            var layout=SetCitiesScroll(0);
            var row=root.Ui.Campaign.travelRows.Single(r=>r!=null&&r.cityId=="mark_wood_city");
            if(!root.Ui.Campaign.gameObject.activeInHierarchy||!row.travel.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Mark Wood City travel row is not active");
            bool expected=(root.Session.Campaign.Accessible("mark_wood_city")||root.Session.Campaign.Offered("mark_wood_city"))&&root.Session.State.activeCity!="mark_wood_city"&&!root.TravelLoading;
            if(row.travel.interactable!=expected)
                throw new InvalidOperationException("Mark Wood City travel button has unexpected interactivity");
            foreach(var rect in new[]{row.progress.rectTransform,(RectTransform)row.travel.transform})
            {
                var corners=new Vector3[4];rect.GetWorldCorners(corners);
                foreach(var corner in corners)
                {
                    var local=layout.scroll.viewport.InverseTransformPoint(corner);var bounds=layout.scroll.viewport.rect;
                    if(local.x<bounds.xMin-1||local.x>bounds.xMax+1||local.y<bounds.yMin-1||local.y>bounds.yMax+1)
                        throw new InvalidOperationException("Mark Wood City travel row is clipped after scrolling: "+rect.name);
                }
            }
            return row;
        }
        private static void CitiesBottom(string name,bool openOffer=false)
        {
            Shot(name,()=>
            {
                root.Ui.ShowCities();var row=MarkWoodTravelVisible();
                if(openOffer)
                {
                    if(!row.travel.interactable||root.Session.Campaign.Accessible("mark_wood_city"))
                        throw new InvalidOperationException("Expected the enabled Mark Wood City offer button before acceptance");
                    row.travel.onClick.Invoke();MarkWoodTravelVisible();
                    if(root.Ui.Campaign.InvitationCityId!="mark_wood_city")throw new InvalidOperationException("City row did not show the Mark Wood City offer");
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
                    var row=city=="mark_wood_city"?MarkWoodTravelVisible():root.Ui.Campaign.travelRows.Single(r=>r!=null&&r.cityId==city);
                    if(!row.travel.interactable||!root.Session.Campaign.Accessible(city))throw new InvalidOperationException("Authored travel button is unavailable for "+city);
                    row.travel.onClick.Invoke();
                    if(!root.TravelLoading)throw new InvalidOperationException("Authored city travel button did not begin a scene handoff for "+city);
                    travel=city;return false;
                }
                var candidate=UnityEngine.Object.FindFirstObjectByType<GameRoot>();
                if(candidate==null||candidate.SceneCityId!=city||candidate.Session==null)return false;
                root=candidate;root.enabled=false;travel=null;traveledCities.Add(city);ready();root.Refresh();root.Ui.Notify("");Capture(name);return true;
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
            Populate(state.activeCity,16000);
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
            Populate("burning_hills",20000);Populate("big_rock_city",20000);Populate("seven_hills",20000);Populate("little_montana",20000);Populate("cassa_vadela",19999);
            root.Session.Campaign.RefreshMilestones();
            if(root.Session.Campaign.UnlockProgress("mark_wood_city")!=99999||root.Session.Campaign.Offered("mark_wood_city"))
                throw new InvalidOperationException("Expected the exact 99999 locked fixture");
            CitiesBottom("01_locked_99999");
            Shot("02_exact_100000_offer",()=>
            {
                Populate("cassa_vadela",20000);root.Session.Campaign.RefreshMilestones();
                if(root.Session.Campaign.UnlockProgress("mark_wood_city")!=100000||!root.Session.Campaign.Offered("mark_wood_city"))
                    throw new InvalidOperationException("Expected exactly 100000 and Mark Wood City offered");
                root.Ui.ShowCities();MarkWoodTravelVisible().travel.onClick.Invoke();SetCitiesScroll(1);
                if(root.Ui.Campaign.InvitationCityId!="mark_wood_city")throw new InvalidOperationException("Sixth authored row did not show invitation");
            });
            Shot("03_rocko_sixth_city_mandate",()=>
            {
                root.Ui.Close();root.Ui.SpeakToCharacter("rocko");SetCitiesScroll(1);
                if(root.Ui.Campaign.InvitationCityId!="mark_wood_city")throw new InvalidOperationException("Rocko did not open sixth-city mandate");
            });
            Shot("04_mandate_accepted",()=>
            {
                root.Ui.Campaign.accept.onClick.Invoke();
                if(!root.Session.Campaign.Accessible("mark_wood_city"))throw new InvalidOperationException("UI did not accept Mark Wood City");
                root.Ui.Rewards.DismissAll();CitiesTop();
            });
            TravelShot("mark_wood_city","05_arrival_unbuilt",()=>
            {
                if(root.Session.Progression.Current?.id!="mw_welcome"||root.Session.State.Find("mw_farm_grow_01").level!=0)
                    throw new InvalidOperationException("Expected the untouched sixth-city arrival quest and unbuilt first garden");
                root.Ui.Close();root.Focus("mw_town_hall");root.CameraRig.SetDiagnosticZoom(35);
            });
            Shot("06_initial_quest",()=>
            {
                root.Ui.Experience.quests.Open("mw_welcome");
                if(root.Session.Progression.Current?.id!="mw_welcome")throw new InvalidOperationException("Unexpected initial Mark Wood City quest");
            });
            LandmarkShot("07_river_market_before_growth","mw_joint_river_market",false,false,new Vector2(112,-90));
            LandmarkShot("08_saw_yards_before_growth","mw_joint_open_saw_yards",false,false,new Vector2(-173,100));
            LandmarkShot("09_forest_river_before_growth","mw_joint_forest_river_day",false,false,new Vector2(94,292));
            Shot("10_forest_gardens_diagnostic_growth",()=>{GrownCityFixture();root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Farm);root.CameraRig.SetDiagnosticZoom(46);});
            Shot("11_river_market_diagnostic_growth",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.City);root.CameraRig.SetDiagnosticZoom(40);});
            Shot("12_canopy_quarter_diagnostic_growth",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Settlement);root.CameraRig.SetDiagnosticZoom(45);});
            Shot("13_old_saw_yards_diagnostic_growth",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Industry);root.CameraRig.SetDiagnosticZoom(42);});
            Shot("14_eight_permanent_residents",()=>
            {
                if(root.Session.Stories.People.Count()!=8||root.Session.Stories.People.Any(p=>!root.Session.Stories.Unlocked(p.id)))
                    throw new InvalidOperationException("Expected all eight permanent Mark Wood City residents unlocked");
                root.Ui.ShowStories();root.Ui.Stories.peopleTab.onClick.Invoke();
            });
            Shot("15_bram_third_chapter_choice",()=>{Complete("mw_bram");Complete("mw_bram");root.Ui.ShowStories("mw_bram");});
            Shot("16_bram_learning_bench_choice_accepted",()=>
            {
                root.Ui.Stories.alternate.onClick.Invoke();
                if(root.Session.Stories.Active("mw_bram")?.choiceId!="learning_bench")throw new InvalidOperationException("UI alternate did not choose learning_bench");
            });
            Shot("17_bram_saved_choice_memory",()=>
            {
                var q=root.Session.Stories.ActiveDefinition("mw_bram");if(q==null)throw new InvalidOperationException("No active Bram choice");
                CompleteActive("mw_bram",q);RequireBramMemory(3);root.Ui.Rewards.DismissAll();root.Ui.Close();root.StoryWorld.Focus("mw_bram");root.CameraRig.SetDiagnosticZoom(23);
                longBodyResident=root.Session.Stories.People.Where(p=>p.id!="mw_bram")
                    .OrderByDescending(p=>root.Definitions.Text(p.biographyKey).Length+root.Definitions.Text(root.Session.Stories.Offer(p.id).bodyKey).Length).First().id;
            });
            foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768),new Vector2Int(2560,1080),new Vector2Int(1080,1920)})
            {
                Shot("18_city_travel_"+resolution.x+"x"+resolution.y,()=>
                {
                    SetSize(resolution.x,resolution.y);root.Session.State.uiScale=resolution.x==1366?1.15f:1;
                    root.Ui.ShowCities();MarkWoodTravelVisible();
                });
                Shot("19_long_story_body_"+resolution.x+"x"+resolution.y,()=>
                {
                    root.Ui.ShowStories(longBodyResident);
                    if(string.IsNullOrWhiteSpace(root.Ui.Stories.biography.text)||string.IsNullOrWhiteSpace(root.Ui.Stories.description.text))
                        throw new InvalidOperationException("Long story details did not render");
                });
            }
            Shot("20_three_community_projects_completed",()=>
            {
                SetSize(1920,1080);root.Session.State.uiScale=1;
                foreach(var person in root.Session.Stories.People)
                    while(root.Session.Stories.Person(person.id).completed.Count<person.chapters.Length)Complete(person.id);
                foreach(var project in new[]{"mw_joint_river_market","mw_joint_open_saw_yards","mw_joint_forest_river_day"})Complete(project);
                RequireBramMemory(4);root.Ui.ShowStories();root.Ui.Stories.togetherTab.onClick.Invoke();
            });
            LandmarkShot("21_river_market_restored","mw_joint_river_market",true,true,new Vector2(112,-90));
            LandmarkShot("22_open_saw_yards_learning_bench","mw_joint_open_saw_yards",true,true,new Vector2(-173,100));
            LandmarkShot("23_forest_river_day_restored","mw_joint_forest_river_day",true,true,new Vector2(94,292));
            TravelShot("burning_hills","24_return_burning_hills",()=>{root.Ui.Close();root.Focus("town_hall");root.CameraRig.SetDiagnosticZoom(30);});
            TravelShot("cassa_vadela","25_return_cassa_vadela",()=>{root.Ui.Close();root.Focus("cv_town_hall");root.CameraRig.SetDiagnosticZoom(30);});
            TravelShot("mark_wood_city","26_roundtrip_bram_memory_preserved",()=>
            {
                RequireBramMemory(4);
                foreach(var project in new[]{"mw_joint_river_market","mw_joint_open_saw_yards","mw_joint_forest_river_day"})RequirePlaceStages(project,true,true);
                root.Ui.Close();root.StoryWorld.Focus("mw_bram");root.CameraRig.SetDiagnosticZoom(23);
            });
        }

        private static void LandmarkShot(string name,string project,bool grown,bool completed,Vector2 position)
            => Shot(name,()=>{RequirePlaceStages(project,grown,completed);root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(position));root.CameraRig.SetDiagnosticZoom(36);});

        private static void RequirePlaceStages(string project,bool grown,bool completed)
        {
            var place=UnityEngine.Object.FindObjectsByType<CityPlaceProgressVisual>(FindObjectsSortMode.None).Single(p=>p.cityId=="mark_wood_city"&&p.collaborationId==project);
            place.Refresh();
            if(place.keepGrowingPlaceAfterCompletion||place.closedPlace.activeSelf!=(!grown&&!completed)
                ||place.growingPlace.activeSelf!=(grown&&!completed)||place.completedPlace.activeSelf!=completed)
                throw new InvalidOperationException("Community place stages do not match the saved population/project state: "+project);
            if(completed&&project=="mw_joint_open_saw_yards")
                for(int index=0;index<place.choicePlaces.Length;index++)
                    if(place.choicePlaces[index].activeSelf!=(place.choiceValues[index]=="learning_bench"))
                        throw new InvalidOperationException("Saved learning-bench choice did not change the authored saw-yard place");
        }

        private static void RequireBramMemory(int chapters)
        {
            var progress=root.Session.Stories.Person("mw_bram");
            if(progress.completed.Count!=chapters||progress.completed[2].choiceId!="learning_bench")
                throw new InvalidOperationException("Saved Bram choice was not retained");
            root.StoryWorld.Refresh();
            var actor=root.StoryWorld.actors.Single(p=>p.vipId=="mw_bram");
            if(!actor.Visible)throw new InvalidOperationException("Completed stories removed Bram from town");
            foreach(var memory in actor.memories.Where(m=>m.chapter==2))
                if(memory.target.activeSelf!=(memory.choiceId=="learning_bench"))
                    throw new InvalidOperationException("Bram's authored memory objects did not match his saved choice");
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
                    int count=(int)Math.Floor(game.Definitions.Building("residential_house").baseResidents*(1+tier)*(1+game.Definitions.workerAttributeStep*level*rank));
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
                game.State.workers.Add(new WorkerState{id="mw_capture_"+building.id,role="caretaker",level=configuration.workerLevel,rank=configuration.rank,
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

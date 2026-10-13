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
    [InitializeOnLoad] public static class CassaVadelaEvidence
    {
        private const string Key="Weedtopia.CassaVadelaEvidence",Folder="Docs/CassaVadela";
        private static GameRoot root;
        private static ManualClock clock;
        private static List<Func<bool>> steps;
        private static readonly List<string> shots=new List<string>();
        private static readonly List<CaptureDimensions> dimensions=new List<CaptureDimensions>();
        private static int index,frames;
        private static int expectedWidth=1920,expectedHeight=1080;
        private static string requested,pending,travel;
        private static DateTime requestedUtc;
        private static double deadline,settle,resizeDeadline;
        static CassaVadelaEvidence(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
        private static bool MemoryProbe => SessionState.GetBool(Key+".memoryProbe",false);
        public static void Begin() => BeginCapture(false);
        public static void BeginMemoryProbe() => BeginCapture(true);
        private static void BeginCapture(bool memoryProbe)
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Owned batch editor required");
            BurningHillsLayoutInstaller.RequireSafeEditor();
            SessionState.SetBool(Key+".hadPreference",PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey));
            SessionState.SetString(Key+".preference",PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey));
            SessionState.SetBool(Key+".memoryProbe",memoryProbe);
            Directory.CreateDirectory(Folder+(memoryProbe?"/MemoryProbeScreenshots":"/Screenshots"));SessionState.SetBool(Key,true);
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
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Cassa Vadela Game View capture");
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
                    pending=requested;requested=null;ProductionUiEvidence.Inspect(root.Ui,Path.GetFileName(pending));
                    requestedUtc=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(pending);frames=0;EditorApplication.QueuePlayerLoopUpdate();return;
                }
                if(index<steps.Count){if(!steps[index]())EditorApplication.QueuePlayerLoopUpdate();}
                else
                {
                    string fingerprint=ProductionVisualPreflight.Fingerprint();ProductionUiEvidence.Write(fingerprint,Folder+(MemoryProbe?"/ui_overflow_probe.json":"/ui_overflow.json"),MemoryProbe?new[]{"1920x1080"}:new[]{"1920x1080","1366x768","2560x1080","1080x1920"});
                    File.WriteAllText(Folder+(MemoryProbe?"/memory_probe_receipt.json":"/gameview_receipt.json"),JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),fingerprint=fingerprint,expectedScreenshots=MemoryProbe?8:80,screenshots=shots.ToArray(),captureDimensions=dimensions.ToArray()},true));
                    RestoreLanguage();SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);return;
                }
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch(Exception ex){RestoreLanguage();SessionState.SetBool(Key,false);File.WriteAllText(Folder+(MemoryProbe?"/memory_probe_error.txt":"/gameview_error.txt"),ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        [Serializable] private sealed class Receipt{public string utc,fingerprint;public string mode="actual_playmode_gameview";public string saveScope="Docs/CassaVadela/capture_save.json and its isolated .scores file";public int expectedScreenshots = 80;public bool userAcceptance=false,diagnosticGrowthFixtures=true;public string[] screenshots;public CaptureDimensions[] captureDimensions;}
        [Serializable] private sealed class CaptureDimensions{public string path;public int requestedWidth,requestedHeight,renderedWidth,renderedHeight,pngWidth,pngHeight;}
        private static void RestoreLanguage()
        {
            if(SessionState.GetBool(Key+".hadPreference",false))PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey,SessionState.GetString(Key+".preference",""));
            else PlayerPrefs.DeleteKey(GameRoot.LanguagePreferenceKey);
            PlayerPrefs.Save();
        }
        private static void Capture(string name)
        {
            // Run the same isolated public fixture actions, rendering only the eight memory frames.
            if(MemoryProbe&&!name.StartsWith("18e_memories_",StringComparison.Ordinal)){index++;frames=0;return;}
            requested=Path.GetFullPath(Folder+(MemoryProbe?"/MemoryProbeScreenshots/":"/Screenshots/")+name+".png");frames=0;settle=EditorApplication.timeSinceStartup+2;resizeDeadline=EditorApplication.timeSinceStartup+12;
        }
        private static void Shot(string name,Action action)
        {steps.Add(()=>{root.Ui.Rewards.DismissAll();action();root.Refresh();root.Ui.Notify("");Capture(name);return true;});}
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
        private static CityTravelRow CassaTravelVisible()
        {
            var layout=SetCitiesScroll(0);
            var row=root.Ui.Campaign.travelRows.Single(r=>r!=null&&r.cityId=="cassa_vadela");
            if(!root.Ui.Campaign.gameObject.activeInHierarchy||!row.travel.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Cassa Vadela travel row is not active");
            bool expected=(root.Session.Campaign.Accessible("cassa_vadela")||root.Session.Campaign.Offered("cassa_vadela"))&&root.Session.State.activeCity!="cassa_vadela"&&!root.TravelLoading;
            if(row.travel.interactable!=expected)
                throw new InvalidOperationException("Cassa Vadela travel button has unexpected interactivity");
            foreach(var rect in new[]{row.progress.rectTransform,(RectTransform)row.travel.transform})
            {
                var corners=new Vector3[4];rect.GetWorldCorners(corners);
                foreach(var corner in corners)
                {
                    var local=layout.scroll.viewport.InverseTransformPoint(corner);var bounds=layout.scroll.viewport.rect;
                    if(local.x<bounds.xMin-1||local.x>bounds.xMax+1||local.y<bounds.yMin-1||local.y>bounds.yMax+1)
                        throw new InvalidOperationException("Cassa Vadela travel row is clipped after scrolling: "+rect.name);
                }
            }
            return row;
        }
        private static void CitiesBottom(string name,bool openOffer=false)
        {
            Shot(name,()=>
            {
                root.Ui.ShowCities();var row=CassaTravelVisible();
                if(openOffer)
                {
                    if(!row.travel.interactable||root.Session.Campaign.Accessible("cassa_vadela"))
                        throw new InvalidOperationException("Expected the enabled Cassa Vadela offer button before acceptance");
                    row.travel.onClick.Invoke();CassaTravelVisible();
                    if(root.Ui.Campaign.InvitationCityId!="cassa_vadela")throw new InvalidOperationException("City row did not show the Cassa Vadela offer");
                }
            });
        }
        private static void TravelShot(string city,string name,Action ready)
        {
            steps.Add(()=>
            {
                if(travel==null)
                {
                    root.Ui.Rewards.DismissAll();root.Ui.ShowCities();root.Refresh();Canvas.ForceUpdateCanvases();
                    var row=city=="cassa_vadela"?CassaTravelVisible():root.Ui.Campaign.travelRows.Single(r=>r!=null&&r.cityId==city);
                    if(!row.travel.interactable||!root.Session.Campaign.Accessible(city))throw new InvalidOperationException("Authored travel button is unavailable for "+city);
                    row.travel.onClick.Invoke();
                    if(!root.TravelLoading)throw new InvalidOperationException("Authored city travel button did not begin a scene handoff for "+city);
                    travel=city;return false;
                }
                var candidate=UnityEngine.Object.FindFirstObjectByType<GameRoot>();
                if(candidate==null||candidate.SceneCityId!=city||candidate.Session==null)return false;
                root=candidate;root.enabled=false;travel=null;ready();root.Refresh();root.Ui.Notify("");Capture(name);return true;
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
            Populate(state.activeCity,12000);
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
            Populate("burning_hills",15000);Populate("big_rock_city",15000);Populate("seven_hills",10000);Populate("little_montana",9999);
            root.Session.Campaign.RefreshMilestones();
            if(root.Session.Campaign.UnlockProgress("cassa_vadela")!=49999||root.Session.Campaign.Offered("cassa_vadela"))
                throw new InvalidOperationException("Expected the exact 49999 locked fixture");
            CitiesBottom("01_locked_49999");
            Shot("02_exact_50000_offer",()=>
            {
                Populate("little_montana",10000);root.Session.Campaign.RefreshMilestones();
                if(root.Session.Campaign.UnlockProgress("cassa_vadela")!=50000)throw new InvalidOperationException("Expected exactly 50000");
                root.Ui.ShowCities();CassaTravelVisible().travel.onClick.Invoke();SetCitiesScroll(1);
                if(root.Ui.Campaign.InvitationCityId!="cassa_vadela")throw new InvalidOperationException("Fifth authored row did not show invitation");
            });
            Shot("03_rocko_fifth_city_mandate",()=>
            {
                root.Ui.Close();root.Ui.SpeakToCharacter("rocko");SetCitiesScroll(1);
                if(root.Ui.Campaign.InvitationCityId!="cassa_vadela")throw new InvalidOperationException("Rocko did not open fifth city mandate");
            });
            Shot("04_mandate_accepted",()=>
            {
                root.Ui.Campaign.accept.onClick.Invoke();
                if(!root.Session.Campaign.Accessible("cassa_vadela"))throw new InvalidOperationException("UI did not accept Cassa Vadela");
                root.Ui.Rewards.DismissAll();CitiesTop();
            });
            CitiesBottom("04b_fifth_city_travel");
            TravelShot("cassa_vadela","05_arrival",()=>root.Ui.Close());
            Shot("05a_arrival_quest_focus",()=>
            {
                if(root.Session.Progression.Current?.id!="cv_welcome")throw new InvalidOperationException("Unexpected arrival quest");
                root.Ui.Experience.quests.Open("cv_welcome");root.Ui.Experience.quests.previewAction.onClick.Invoke();
                var townHall=root.Site("cv_town_hall").transform.position;
                if(Vector2.Distance(DistrictMap.ToPlan(root.CameraRig.TargetPosition),DistrictMap.ToPlan(townHall))>.01f)
                    throw new InvalidOperationException("Arrival did not focus Cassa Vadela town hall");
            });
            Shot("05b_arrival_rocko_dialogue",()=>root.Ui.SpeakToCharacter("rocko"));
            Shot("05c_claim_arrival",()=>
            {
                root.Ui.Experience.quests.Open("cv_welcome");root.Ui.Experience.quests.previewAction.onClick.Invoke();
                if(root.Session.Progression.Current?.id!="cv_first_garden")throw new InvalidOperationException("UI did not claim arrival");
                root.Ui.Experience.quests.Open("cv_first_garden");
            });
            Shot("05d_first_garden_build",()=>
            {
                root.Ui.Experience.quests.previewAction.onClick.Invoke();root.Ui.Interaction.upgrade.onClick.Invoke();
                if(root.Session.State.Find("cv_farm_grow_01").buildingSeconds<=0)throw new InvalidOperationException("UI did not begin first garden");
            });
            Shot("05e_garden_simulated_and_claimed",()=>
            {
                root.Ui.Close();root.Session.Advance(root.Session.State.Find("cv_farm_grow_01").buildingSeconds);root.Refresh();
                root.Ui.Experience.quests.Open("cv_first_garden");root.Ui.Experience.quests.previewAction.onClick.Invoke();
                if(root.Session.Progression.Current?.id!="cv_first_harvest")throw new InvalidOperationException("Garden claim did not advance");
                root.Ui.Rewards.DismissAll();root.Ui.Experience.quests.Open("cv_first_harvest");
            });
            Shot("06_fountain_before_growth",()=>{RequireFountainStages(false,false);root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(143,-76)));root.CameraRig.SetDiagnosticZoom(34);});
            Shot("07_olive_terraces",()=>{GrownCityFixture();RequireFountainStages(true,false);root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Farm);root.CameraRig.SetDiagnosticZoom(43);});
            Shot("08_fountain_market",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.City);root.CameraRig.SetDiagnosticZoom(38);});
            Shot("09_sunny_slope",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Settlement);root.CameraRig.SetDiagnosticZoom(40);});
            Shot("10_ceramic_yard",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Industry);root.CameraRig.SetDiagnosticZoom(38);});
            Shot("11_ines_first_chapter",()=>root.Ui.ShowStories("cv_ines"));
            Shot("11b_ines_accepted",()=>root.Ui.Stories.accept.onClick.Invoke());
            Shot("11c_ines_saved_memory",()=>
            {
                var q=root.Session.Stories.ActiveDefinition("cv_ines");
                if(q==null)throw new InvalidOperationException("Ines was not accepted through the UI");
                CompleteActive("cv_ines",q);root.Ui.Rewards.DismissAll();root.Ui.Close();root.StoryWorld.Focus("cv_ines");root.CameraRig.SetDiagnosticZoom(17);
            });
            Shot("12_rafael_third_chapter_choice",()=>{Complete("cv_rafael");Complete("cv_rafael");root.Ui.ShowStories("cv_rafael");});
            Shot("12b_rafael_teach_choice_accepted",()=>
            {
                root.Ui.Stories.alternate.onClick.Invoke();
                if(root.Session.Stories.Active("cv_rafael")?.choiceId!="teach")throw new InvalidOperationException("UI alternate did not choose teach");
            });
            Shot("12c_rafael_choice_memory",()=>
            {
                var q=root.Session.Stories.ActiveDefinition("cv_rafael");if(q==null)throw new InvalidOperationException("No active Rafael choice");
                CompleteActive("cv_rafael",q);root.Ui.Rewards.DismissAll();root.Ui.Close();root.StoryWorld.Focus("cv_rafael");root.CameraRig.SetDiagnosticZoom(17);
            });
            foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768),new Vector2Int(2560,1080),new Vector2Int(1080,1920)})
            {
                Shot("13_campaign_"+resolution.x+"x"+resolution.y,()=>{SetSize(resolution.x,resolution.y);root.Session.State.uiScale=resolution.x==1366?1.15f:1;CitiesTop();});
                CitiesBottom("13b_travel_"+resolution.x+"x"+resolution.y);
                Shot("14_residents_"+resolution.x+"x"+resolution.y,()=>{root.Ui.ShowStories();root.Ui.Stories.peopleTab.onClick.Invoke();});
                Shot("14b_resident_details_"+resolution.x+"x"+resolution.y,()=>root.Ui.ShowStories("cv_ines"));
                Shot("14c_projects_"+resolution.x+"x"+resolution.y,()=>{root.Ui.ShowStories();root.Ui.Stories.togetherTab.onClick.Invoke();});
            }
            foreach(string person in new[]{"cv_ines","cv_mateo","cv_lucia","cv_rafael","cv_sofia","cv_elio","cv_amara","cv_nico"})
                Shot("15_city_"+person,()=>{SetSize(1920,1080);root.Session.State.uiScale=1;root.Ui.Close();root.StoryWorld.Focus(person);root.CameraRig.SetDiagnosticZoom(17);});
            foreach(string language in new[]{"de","en","es","fr","ru"})
            {
                Shot("16_campaign_"+language,()=>{root.SetLanguage(language);CitiesTop();});
                Shot("16b_story_"+language,()=>root.Ui.ShowStories("cv_rafael"));
            }
            Shot("17_terrace_walk_offered",()=>
            {
                root.SetLanguage("de");
                foreach(var person in root.Session.Stories.People)
                    while(root.Session.Stories.Person(person.id).completed.Count<person.chapters.Length)Complete(person.id);
                root.Ui.ShowStories("cv_joint_terrace_walk");
            });
            Shot("17b_terrace_walk_accepted",()=>root.Ui.Stories.accept.onClick.Invoke());
            Shot("17c_terrace_walk_completed",()=>
            {
                var q=root.Session.Stories.ActiveDefinition("cv_joint_terrace_walk");if(q==null)throw new InvalidOperationException("Trail project was not accepted");
                CompleteActive("cv_joint_terrace_walk",q);root.Ui.ShowStories("cv_joint_terrace_walk");
            });
            Shot("18_all_projects_completed",()=>
            {
                Complete("cv_joint_fountain_market");Complete("cv_joint_open_ceramic_yard");root.Refresh();root.Ui.ShowStories();root.Ui.Stories.togetherTab.onClick.Invoke();
            });
            Shot("18b_fountain_restored",()=>{RequireFountainStages(true,true);root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(143,-76)));root.CameraRig.SetDiagnosticZoom(34);});
            Shot("18c_rafael_learning_workshop",()=>{root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(-142,54)));root.CameraRig.SetDiagnosticZoom(23);});
            Shot("18d_terrace_walk_restored",()=>{root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(-40,324)));root.CameraRig.SetDiagnosticZoom(36);});
            foreach(var person in new[]{"cv_ines","cv_mateo","cv_lucia","cv_rafael","cv_sofia","cv_elio","cv_amara","cv_nico"})
                Shot("18e_memories_"+person,()=>
                {
                    SetSize(1920,1080);root.Ui.Close();
                    if(root.Session.Stories.Person(person).completed.Count!=4)throw new InvalidOperationException("Expected all four saved memories for "+person);
                    root.StoryWorld.Focus(person);
                    root.CameraRig.SetDiagnosticZoom(17);
                });
            TravelShot("burning_hills","19_return_burning_hills",()=>{root.Ui.Close();root.Focus("town_hall");root.CameraRig.SetDiagnosticZoom(25);});
            TravelShot("big_rock_city","20_return_big_rock_city",()=>{root.Ui.Close();root.Focus("br_town_hall");root.CameraRig.SetDiagnosticZoom(25);});
            TravelShot("seven_hills","21_return_seven_hills",()=>{root.Ui.Close();root.Focus("sh_town_hall");root.CameraRig.SetDiagnosticZoom(25);});
            TravelShot("little_montana","22_return_little_montana",()=>{root.Ui.Close();root.Focus("lm_town_hall");root.CameraRig.SetDiagnosticZoom(25);});
            TravelShot("cassa_vadela","23_return_cassa_vadela_choice_preserved",()=>
            {
                var rafael=root.Session.Stories.Person("cv_rafael");
                if(rafael.completed.Count!=4||rafael.completed[2].choiceId!="teach")throw new InvalidOperationException("Saved Rafael choice lost on travel");
                root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(-142,54)));root.CameraRig.SetDiagnosticZoom(23);
            });
        }
        private static void RequireFountainStages(bool grown,bool completed)
        {
            var place=UnityEngine.Object.FindObjectsByType<CityPlaceProgressVisual>(FindObjectsSortMode.None).Single(p=>p.collaborationId=="cv_joint_fountain_market");
            place.Refresh();
            if(place.keepGrowingPlaceAfterCompletion||place.closedPlace.activeSelf!=( !grown && !completed )
                ||place.growingPlace.activeSelf!=(grown&&!completed)||place.completedPlace.activeSelf!=completed)
                throw new InvalidOperationException("Fountain stages do not match the saved population/project state");
            var wet=place.completedPlace.GetComponentsInChildren<SpriteRenderer>(true).Single(renderer=>AssetDatabase.GetAssetPath(renderer.sprite)==CassaVadelaAuthoring.Folder+"/Art/fountain_square.png");
            if(wet.gameObject.activeInHierarchy!=completed)throw new InvalidOperationException("Restored fountain water must appear only after project completion");
            foreach(var stage in new[]{place.closedPlace,place.growingPlace})
            {
                var dry=stage.GetComponentsInChildren<SpriteRenderer>(true).Single(renderer=>AssetDatabase.GetAssetPath(renderer.sprite)==CassaVadelaAuthoring.Folder+"/Art/fountain_dry.png");
                if(dry.gameObject.activeInHierarchy!=stage.activeInHierarchy)throw new InvalidOperationException("Missing dry fountain in a pre-completion stage");
            }
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
                game.State.workers.Add(new WorkerState{id="cv_capture_"+building.id,role="caretaker",level=configuration.workerLevel,rank=configuration.rank,
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

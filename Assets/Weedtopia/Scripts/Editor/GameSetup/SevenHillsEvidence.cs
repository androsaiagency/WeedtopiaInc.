using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Rendered play-mode evidence with an isolated save, actual scene travel and explicitly diagnostic growth fixtures.</summary>
    [InitializeOnLoad] public static class SevenHillsEvidence
    {
        private const string Key="Weedtopia.SevenHillsEvidence",Folder="Docs/SevenHills";
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
        static SevenHillsEvidence(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
        public static void Begin()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Owned batch editor required");
            BurningHillsLayoutInstaller.RequireSafeEditor();
            SessionState.SetBool(Key+".hadPreference",PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey));
            SessionState.SetString(Key+".preference",PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey));
            Directory.CreateDirectory(Folder+"/Screenshots");SessionState.SetBool(Key,true);
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
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Seven Hills Game View capture");
                if(steps==null)
                {
                    root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();if(root==null)return;
                    root.enabled=false;clock=new ManualClock(new DateTime(2026,10,7,12,0,0,DateTimeKind.Utc));
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
                    string fingerprint=ProductionVisualPreflight.Fingerprint();ProductionUiEvidence.Write(fingerprint,Folder+"/ui_overflow.json");
                    File.WriteAllText(Folder+"/gameview_receipt.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),fingerprint=fingerprint,screenshots=shots.ToArray(),captureDimensions=dimensions.ToArray()},true));
                    RestoreLanguage();SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);return;
                }
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch(Exception ex){RestoreLanguage();SessionState.SetBool(Key,false);File.WriteAllText(Folder+"/gameview_error.txt",ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        [Serializable] private sealed class Receipt{public string utc,fingerprint;public string mode="actual_playmode_gameview";public bool userAcceptance=false,diagnosticGrowthFixtures=true;public string[] screenshots;public CaptureDimensions[] captureDimensions;}
        [Serializable] private sealed class CaptureDimensions{public string path;public int requestedWidth,requestedHeight,renderedWidth,renderedHeight,pngWidth,pngHeight;}
        private static void RestoreLanguage()
        {
            if(SessionState.GetBool(Key+".hadPreference",false))PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey,SessionState.GetString(Key+".preference",""));
            else PlayerPrefs.DeleteKey(GameRoot.LanguagePreferenceKey);
            PlayerPrefs.Save();
        }
        private static void Capture(string name)
        {requested=Path.GetFullPath(Folder+"/Screenshots/"+name+".png");frames=0;settle=EditorApplication.timeSinceStartup+2;resizeDeadline=EditorApplication.timeSinceStartup+12;}
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
        private static CityTravelRow SevenTravelVisible()
        {
            var layout=SetCitiesScroll(0);
            var row=root.Ui.Campaign.travelRows.Single(r=>r!=null&&r.cityId=="seven_hills");
            if(!root.Ui.Campaign.gameObject.activeInHierarchy||!row.travel.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Seven Hills travel row is not active");
            bool expected=(root.Session.Campaign.Accessible("seven_hills")||root.Session.Campaign.Offered("seven_hills"))&&root.Session.State.activeCity!="seven_hills"&&!root.TravelLoading;
            if(row.travel.interactable!=expected)
                throw new InvalidOperationException("Seven Hills travel button has unexpected interactivity");
            foreach(var rect in new[]{row.progress.rectTransform,(RectTransform)row.travel.transform})
            {
                var corners=new Vector3[4];rect.GetWorldCorners(corners);
                foreach(var corner in corners)
                {
                    var local=layout.scroll.viewport.InverseTransformPoint(corner);var bounds=layout.scroll.viewport.rect;
                    if(local.x<bounds.xMin-1||local.x>bounds.xMax+1||local.y<bounds.yMin-1||local.y>bounds.yMax+1)
                        throw new InvalidOperationException("Seven Hills travel row is clipped after scrolling: "+rect.name);
                }
            }
            return row;
        }
        private static void CitiesBottom(string name,bool openOffer=false)
        {
            Shot(name,()=>
            {
                root.Ui.ShowCities();var row=SevenTravelVisible();
                if(openOffer)
                {
                    if(!row.travel.interactable||root.Session.Campaign.Accessible("seven_hills"))
                        throw new InvalidOperationException("Expected the enabled Seven Hills offer button before acceptance");
                    row.travel.onClick.Invoke();SevenTravelVisible();
                    if(root.Ui.Campaign.InvitationCityId!="seven_hills")throw new InvalidOperationException("City row did not show the Seven Hills offer");
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
                    var row=city=="seven_hills"?SevenTravelVisible():root.Ui.Campaign.travelRows.Single(r=>r!=null&&r.cityId==city);
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
            var state=root.Session.State;root.SetLanguage("de");
            var sources=new[]{"burning_hills","big_rock_city"};
            foreach(var city in sources)
                foreach(var home in state.buildings.Where(b=>b.cityId==city&&b.family=="settlement_plot"))
                {home.family="residential_house";home.level=5000;}
            root.Session.Campaign.RefreshMilestones();
            if(root.Session.Campaign.Accept("big_rock_city")!=Failure.None)throw new InvalidOperationException("Could not accept first-city mandate fixture");
            var homes=state.buildings.Where(b=>b.cityId=="burning_hills"&&b.family=="residential_house").Take(2).ToArray();
            var firstWorker=root.Session.Workers.Grant("caretaker",4);firstWorker.level=79;firstWorker.rank=1;
            var secondWorker=root.Session.Workers.Grant("caretaker",4);secondWorker.level=1;secondWorker.rank=1;
            if(root.Session.Workers.Assign(firstWorker.id,homes[0].id,0)!=Failure.None||root.Session.Workers.Assign(secondWorker.id,homes[1].id,0)!=Failure.None)
                throw new InvalidOperationException("Could not assign population boundary fixture");
            if(root.Session.Campaign.UnlockProgress("seven_hills")!=4999)throw new InvalidOperationException("Expected 4999 combined fixture residents");
            Shot("01_seven_hills_below_threshold_4999",CitiesTop);
            TravelShot("big_rock_city","01b_seven_hills_below_threshold_in_big_rock_city",CitiesTop);
            Shot("02_seven_hills_exact_threshold_5000",()=>
            {
                // Travel retains the campaign state; reacquire its workers after the actual scene handoff.
                root.Session.State.workers.Single(w=>w.id==firstWorker.id).level=80;
                if(root.Session.Workers.Remove(secondWorker.id)!=Failure.None)throw new InvalidOperationException("Population boundary fixture adjustment failed");
                if(root.Session.Campaign.UnlockProgress("seven_hills")!=5000)throw new InvalidOperationException("Expected exactly 5000 combined residents");
                CitiesTop();
            });
            CitiesBottom("02b_seven_hills_offer_row_at_exact_threshold",true);
            Shot("03_rockos_third_city_government_request",()=>
            {
                root.Ui.Close();root.Ui.SpeakToCharacter("rocko");SetCitiesScroll(1);
                if(!root.Session.Campaign.Offered("seven_hills")||!root.Ui.Campaign.gameObject.activeInHierarchy||root.Ui.Campaign.InvitationCityId!="seven_hills")
                    throw new InvalidOperationException("Rocko's actual actor dialogue did not show the Seven Hills government request");
            });
            Shot("04_third_city_mandate_accepted",()=>{CitiesTop();root.Ui.Campaign.accept.onClick.Invoke();root.Ui.Rewards.DismissAll();SetCitiesScroll(1);});
            CitiesBottom("04b_seven_hills_travel_row_after_acceptance");
            TravelShot("seven_hills","05_seven_hills_arrival_new_main_quest",()=>root.Ui.Close());
            Shot("05a_authored_arrival_quest_focuses_rocko",()=>
            {
                if(root.Session.Progression.Current?.id!="sh_arrival")throw new InvalidOperationException("Unexpected initial Seven Hills quest");
                root.Ui.Experience.quests.Open("sh_arrival");root.Ui.Experience.quests.previewAction.onClick.Invoke();
                var townHall=root.Site("sh_town_hall").transform.position;
                var cameraTarget=root.CameraRig.TargetPosition;
                if(Vector2.Distance(DistrictMap.ToPlan(cameraTarget),DistrictMap.ToPlan(townHall))>.01f)
                    throw new InvalidOperationException("Authored arrival quest did not focus Rocko's town hall: camera target "+cameraTarget+", authored site "+townHall);
                if(root.Ui.Interaction.BuildingOpen)
                    throw new InvalidOperationException("Locked town-hall focus must not retain the previous building inspector: "+root.SelectedId);
            });
            Shot("05b_actual_rocko_arrival_dialogue",()=>root.Ui.SpeakToCharacter("rocko"));
            Shot("05c_actual_arrival_claim_unlocks_first_garden",()=>
            {
                root.Ui.Experience.quests.Open("sh_arrival");root.Ui.Experience.quests.previewAction.onClick.Invoke();
                if(root.Session.Progression.Current?.id!="sh_first_garden")throw new InvalidOperationException("Actual arrival claim did not advance the main quest");
                root.Ui.Experience.quests.Open("sh_first_garden");
            });
            Shot("05d_actual_first_terrace_garden_build_started",()=>
            {
                root.Ui.Experience.quests.previewAction.onClick.Invoke();root.Ui.Interaction.upgrade.onClick.Invoke();
                if(root.Session.State.Find("sh_farm_grow_01").buildingSeconds<=0)throw new InvalidOperationException("Actual building UI did not begin the first garden");
            });
            Shot("05e_first_garden_completed_through_simulation_and_claim",()=>
            {
                root.Ui.Close();root.Session.Advance(root.Session.State.Find("sh_farm_grow_01").buildingSeconds);root.Refresh();
                root.Ui.Experience.quests.Open("sh_first_garden");root.Ui.Experience.quests.previewAction.onClick.Invoke();
                if(root.Session.Progression.Current?.id!="sh_first_harvest")throw new InvalidOperationException("Actual garden build and claim did not advance to harvest");
                root.Ui.Rewards.DismissAll();root.Ui.Experience.quests.Open("sh_first_harvest");
            });
            Shot("05f_quellhuegel_abandoned_before_growth",()=>{root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(315,-12)));root.CameraRig.SetDiagnosticZoom(24);});
            Shot("06_seven_hills_terrassengaerten",()=>{GrownCityFixture();root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Farm);root.CameraRig.SetDiagnosticZoom(43);});
            Shot("07_seven_hills_altstadtbogen",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.City);root.CameraRig.SetDiagnosticZoom(38);});
            Shot("08_seven_hills_panoramaquartier",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Settlement);root.CameraRig.SetDiagnosticZoom(40);});
            Shot("09_seven_hills_werkhof",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Industry);root.CameraRig.SetDiagnosticZoom(38);});
            Shot("10_mila_first_plan",()=>root.Ui.ShowStories("sh_mila"));
            Shot("11_mila_accepted",()=>root.Ui.Stories.accept.onClick.Invoke());
            Shot("12_mila_memory_in_city",()=>
            {
                var q=root.Session.Stories.ActiveDefinition("sh_mila");
                if(q==null)throw new InvalidOperationException("No active Mila chapter");
                CompleteActive("sh_mila",q);
                root.Ui.Rewards.DismissAll();
                root.Ui.Close();root.StoryWorld.Focus("sh_mila");root.CameraRig.SetDiagnosticZoom(17);
                if(root.Ui.Rewards.IsOpen||root.Ui.ModalOpen||!root.StoryWorld.Actor("sh_mila").Visible)throw new InvalidOperationException("Mila's saved city memory is covered by a modal");
            });
            Shot("13_mila_third_chapter_choice",()=>{Complete("sh_mila");root.Ui.ShowStories("sh_mila");});
            Shot("13b_mila_chapter_choice_accepted",()=>root.Ui.Stories.alternate.onClick.Invoke());
            Shot("13c_mila_choice_memory_in_city",()=>
            {
                var q=root.Session.Stories.ActiveDefinition("sh_mila");if(q==null)throw new InvalidOperationException("Mila choice was not accepted");
                CompleteActive("sh_mila",q);root.Ui.Rewards.DismissAll();root.Ui.Close();root.StoryWorld.Focus("sh_mila");root.CameraRig.SetDiagnosticZoom(17);
                if(root.Ui.Rewards.IsOpen||root.Ui.ModalOpen||!root.StoryWorld.Actor("sh_mila").Visible)throw new InvalidOperationException("Mila's saved choice memory is covered by a modal");
            });
            Shot("14_city_shared_morning_courtyard",()=>root.Ui.ShowStories("sh_joint_morning_courtyard"));
            foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768),new Vector2Int(2560,1080),new Vector2Int(1080,1920)})
            {
                Shot("15_three_city_campaign_"+resolution.x+"x"+resolution.y,()=>{SetSize(resolution.x,resolution.y);root.Session.State.uiScale=resolution.x==1366?1.15f:1;CitiesTop();});
                CitiesBottom("15b_three_city_travel_"+resolution.x+"x"+resolution.y);
                Shot("16_seven_residents_"+resolution.x+"x"+resolution.y,()=>{root.Ui.ShowStories();root.Ui.Stories.peopleTab.onClick.Invoke();});
                Shot("16b_seven_resident_details_"+resolution.x+"x"+resolution.y,()=>root.Ui.ShowStories("sh_mila"));
                Shot("16c_three_projects_"+resolution.x+"x"+resolution.y,()=>{root.Ui.ShowStories();root.Ui.Stories.togetherTab.onClick.Invoke();});
            }
            foreach(string person in new[]{"sh_mila","sh_tariq","sh_juna","sh_emil","sh_safiya","sh_clara","sh_anton"})
                Shot("17_city_"+person,()=>{SetSize(1920,1080);root.Session.State.uiScale=1;root.Ui.Close();root.StoryWorld.Focus(person);root.CameraRig.SetDiagnosticZoom(17);});
            foreach(string language in new[]{"de","en","fr","es","ru"})
            {
                Shot("17b_campaign_"+language,()=>{root.SetLanguage(language);CitiesTop();});
                Shot("17c_story_"+language,()=>root.Ui.ShowStories("sh_mila"));
            }
            Shot("17d_joint_panorama_offered",()=>
            {
                root.SetLanguage("de");
                foreach(var person in root.Session.Stories.People)
                    while(root.Session.Stories.Person(person.id).completed.Count<person.chapters.Length)Complete(person.id);
                root.Ui.ShowStories("sh_joint_panorama_ring");
            });
            Shot("17e_joint_panorama_accepted",()=>root.Ui.Stories.accept.onClick.Invoke());
            Shot("17f_joint_panorama_completed",()=>
            {
                var q=root.Session.Stories.ActiveDefinition("sh_joint_panorama_ring");
                if(q==null)throw new InvalidOperationException("Panorama project was not accepted");
                CompleteActive("sh_joint_panorama_ring",q);root.Ui.ShowStories("sh_joint_panorama_ring");
            });
            Shot("17g_all_three_community_projects_completed",()=>
            {
                Complete("sh_joint_morning_courtyard");Complete("sh_joint_connection_terrace");
                root.Refresh();root.Ui.ShowStories();root.Ui.Stories.togetherTab.onClick.Invoke();
            });
            Shot("17h_quellhuegel_restored_after_project",()=>{root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(315,-12)));root.CameraRig.SetDiagnosticZoom(24);});
            Shot("17i_connection_public_terrace_saved_choice",()=>{root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(110,-76)));root.CameraRig.SetDiagnosticZoom(24);});
            Shot("17j_panorama_ring_restored",()=>{root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(110,324)));root.CameraRig.SetDiagnosticZoom(24);});
            TravelShot("burning_hills","18_return_burning_hills",()=>{root.Ui.Close();root.Focus("town_hall");root.CameraRig.SetDiagnosticZoom(25);});
            TravelShot("big_rock_city","19_return_big_rock_city",()=>{root.Ui.Close();root.Focus("br_town_hall");root.CameraRig.SetDiagnosticZoom(25);});
            TravelShot("seven_hills","20_return_seven_hills_memory_preserved",()=>root.Ui.ShowStories("sh_mila"));
        }
        private static void CompleteActive(string id,StoryQuest q)
        {
            PrepareGoals(q);
            root.Refresh();root.Ui.ShowStories(id);root.Ui.Stories.claim.onClick.Invoke();
            if(root.Session.Stories.ActiveDefinition(id)!=null)throw new InvalidOperationException("UI did not claim active "+q.id);
        }
        private static void PrepareGoals(StoryQuest q)
        {
            var state=root.Session.State;
            state.City.stats.deliveries+=1000;state.City.stats.delivered+=100000;state.City.stats.coffeeSold+=100000;
            state.City.stats.rawProduced+=100000;state.City.stats.refinedProduced+=100000;state.City.stats.butterProduced+=100000;state.City.stats.pastrySold+=100000;
            foreach(var goal in q.goals.Where(g=>g.kind==StoryGoalKind.NpcVisited))
                for(int i=0;i<goal.count;i++)root.Session.Stories.Visit(goal.target);
            foreach(var goal in q.goals.Where(g=>g.kind==StoryGoalKind.Level))
            {
                var building=state.buildings.First(b=>b.cityId==state.activeCity&&(b.id==goal.target||b.family==goal.target));
                building.level=Math.Max(goal.count,building.level);
            }
        }
    }
}

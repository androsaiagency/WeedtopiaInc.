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
    [InitializeOnLoad] public static class BigRockCityEvidence
    {
        private const string Key="Weedtopia.BigRockEvidence",Folder="Docs/BigRockCity";
        private static GameRoot root;
        private static ManualClock clock;
        private static List<Func<bool>> steps;
        private static readonly List<string> shots=new List<string>();
        private static int index,frames;
        private static string requested,pending,travel;
        private static DateTime requestedUtc;
        private static double deadline,settle;
        static BigRockCityEvidence(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
        public static void Begin()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Owned batch editor required");
            Directory.CreateDirectory(Folder+"/Screenshots");SessionState.SetBool(Key,true);
            EditorSceneManager.OpenScene(GameScaffold.GameScene);SetSize(1920,1080);EditorApplication.EnterPlaymode();
        }
        private static void SetSize(int width,int height)
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;var sizes=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=assembly.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizes);
            var instance=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
            var group=sizes.GetMethod("GetGroup").Invoke(instance,new object[]{0});
            var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new[]{Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"),1),(object)width,height,"Big Rock QA "+width+"x"+height});
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
            int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
            var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,count-1);view.Repaint();
        }
        private static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+600;
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Big Rock Game View capture");
                if(steps==null)
                {
                    root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();if(root==null)return;
                    root.enabled=false;clock=new ManualClock(new DateTime(2026,10,2,12,0,0,DateTimeKind.Utc));
                    root.UseTestStore(Path.GetFullPath(Folder+"/capture_save.json"),clock);root.NewGame("Stadtchronik");Prepare();
                }
                if(++frames<12||EditorApplication.timeSinceStartup<settle)
                {if(root!=null&&!root.TravelLoading)root.Refresh();EditorApplication.QueuePlayerLoopUpdate();return;}
                if(pending!=null)
                {
                    if(!File.Exists(pending)||File.GetLastWriteTimeUtc(pending)<requestedUtc){EditorApplication.QueuePlayerLoopUpdate();return;}
                    shots.Add(pending);pending=null;index++;frames=0;return;
                }
                if(requested!=null)
                {
                    pending=requested;requested=null;ProductionUiEvidence.Inspect(root.Ui,Path.GetFileName(pending));
                    requestedUtc=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(pending);frames=0;EditorApplication.QueuePlayerLoopUpdate();return;
                }
                if(index<steps.Count){if(!steps[index]())EditorApplication.QueuePlayerLoopUpdate();}
                else
                {
                    string fingerprint=ProductionVisualPreflight.Fingerprint();ProductionUiEvidence.Write(fingerprint,Folder+"/ui_overflow.json");
                    File.WriteAllText(Folder+"/gameview_receipt.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),fingerprint=fingerprint,screenshots=shots.ToArray()},true));
                    SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);return;
                }
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch(Exception ex){SessionState.SetBool(Key,false);File.WriteAllText(Folder+"/gameview_error.txt",ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        [Serializable] private sealed class Receipt{public string utc,fingerprint;public string mode="actual_playmode_gameview";public bool userAcceptance=false,diagnosticGrowthFixtures=true;public string[] screenshots;}
        private static void Capture(string name)
        {requested=Path.GetFullPath(Folder+"/Screenshots/"+name+".png");frames=0;settle=EditorApplication.timeSinceStartup+2;}
        private static void Shot(string name,Action action)
        {steps.Add(()=>{root.Ui.Rewards.DismissAll();action();root.Refresh();root.Ui.Notify("");Capture(name);return true;});}
        private static void TravelShot(string city,string name,Action ready)
        {
            steps.Add(()=>
            {
                if(travel==null){root.Ui.Rewards.DismissAll();root.Ui.Close();if(root.TravelToCity(city)!=Failure.None)throw new InvalidOperationException("Travel failed");travel=city;return false;}
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
            var state=root.Session.State;state.City.stats.deliveries+=100;state.City.stats.delivered+=10000;state.City.stats.coffeeSold+=10000;
            state.City.stats.rawProduced+=10000;state.City.stats.pastrySold+=10000;
            foreach(var goal in q.goals.Where(g=>g.kind==StoryGoalKind.NpcVisited))root.Session.Stories.Visit(goal.target);
            foreach(var goal in q.goals.Where(g=>g.kind==StoryGoalKind.Level))state.buildings.First(b=>b.cityId==state.activeCity&&(b.id==goal.target||b.family==goal.target)).level=Math.Max(goal.count,state.buildings.First(b=>b.cityId==state.activeCity&&(b.id==goal.target||b.family==goal.target)).level);
            if(root.Session.Stories.Claim(id,q.id)!=Failure.None)throw new InvalidOperationException("Cannot claim "+q.id);
        }
        private static void Prepare()
        {
            root.Session.CompleteIntro();root.Ui.Close();FreeplayFixture();steps=new List<Func<bool>>();
            var homes=root.Session.State.buildings.Where(b=>b.cityId=="burning_hills"&&b.family=="settlement_plot").Take(8).ToArray();
            foreach(var home in homes){home.family="residential_house";home.level=5000;}homes[7].level=4000;
            root.Session.State.City.storyResidentMilestone=999;
            Shot("01_burning_below_threshold",()=>root.Ui.ShowCities());
            Shot("02_rockos_government_request",()=>{homes[7].level=5000;root.Refresh();root.Ui.Close();root.Select("town_hall");root.Ui.SpeakToSelected();});
            Shot("03_rocko_visible_new_quest",()=>{root.Ui.Close();root.Focus("town_hall");root.CameraRig.SetDiagnosticZoom(19);});
            Shot("04_mandate_accepted",()=>{root.Ui.ShowCities();root.Ui.Campaign.accept.onClick.Invoke();root.Ui.Rewards.DismissAll();});
            TravelShot("big_rock_city","05_arrival_new_main_quest",()=>root.Ui.Close());
            Shot("06_big_rock_felsgaerten",()=>{GrownCityFixture();root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Farm);root.CameraRig.SetDiagnosticZoom(43);});
            Shot("07_big_rock_kupfergasse",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.City);root.CameraRig.SetDiagnosticZoom(38);});
            Shot("08_big_rock_felsstufen",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Settlement);root.CameraRig.SetDiagnosticZoom(40);});
            Shot("09_big_rock_werkviertel",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Industry);root.CameraRig.SetDiagnosticZoom(38);});
            Shot("10_berta_first_plan",()=>root.Ui.ShowStories("br_berta"));
            Shot("11_berta_accepted",()=>root.Ui.Stories.accept.onClick.Invoke());
            Shot("12_berta_memory_in_city",()=>
            {
                var q=root.Session.Stories.ActiveDefinition("br_berta");root.Session.State.City.stats.delivered+=10000;root.Session.State.City.stats.rawProduced+=10000;root.Session.State.City.stats.refinedProduced+=10000;root.Session.State.Find("br_farm_warehouse").level=25;
                if(root.Session.Stories.Claim("br_berta",q.id)!=Failure.None)throw new InvalidOperationException("Berta fixture did not complete");
                root.Ui.Close();root.StoryWorld.Focus("br_berta");root.CameraRig.SetDiagnosticZoom(17);
            });
            Shot("13_berta_third_chapter_choice",()=>{Complete("br_berta");root.Ui.ShowStories("br_berta");});
            Shot("14_city_shared_projects",()=>root.Ui.ShowStories("br_joint_open_books"));
            foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768),new Vector2Int(2560,1080),new Vector2Int(1080,1920)})
            {
                Shot("15_campaign_"+resolution.x+"x"+resolution.y,()=>{SetSize(resolution.x,resolution.y);root.Session.State.uiScale=resolution.x==1366?1.15f:1;root.Ui.ShowCities();});
                Shot("16_residents_"+resolution.x+"x"+resolution.y,()=>root.Ui.ShowStories("br_berta"));
            }
            foreach(string person in new[]{"br_berta","br_levin","br_samira","br_enno","br_lotte","br_ida"})
                Shot("17_city_"+person,()=>{SetSize(1920,1080);root.Session.State.uiScale=1;root.Ui.Close();root.StoryWorld.Focus(person);root.CameraRig.SetDiagnosticZoom(17);});
            TravelShot("burning_hills","18_return_burning_hills",()=>{root.Ui.Close();root.Focus("town_hall");root.CameraRig.SetDiagnosticZoom(25);});
            TravelShot("big_rock_city","19_return_big_rock_city_preserved",()=>root.Ui.ShowStories("br_berta"));
        }
    }
}

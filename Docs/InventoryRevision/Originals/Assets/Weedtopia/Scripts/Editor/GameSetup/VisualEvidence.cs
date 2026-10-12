using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit owned batch capture. Does nothing on normal editor starts or reloads.</summary>
    [InitializeOnLoad]
    public static class VisualEvidence
    {
        private const string Key="Weedtopia.ExplicitVisualEvidence";
        private static GameRoot root;
        private static ManualClock clock;
        private static int stage,frame;
        private static double deadline;
        private static string pending,requested;
        private static DateTime requestedUtc;
        private static readonly string Folder="Docs/Implementation/Validation/Screenshots";
        static VisualEvidence(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
        public static void Begin()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Capture requires an explicitly owned batch editor");
            Directory.CreateDirectory(Folder);SpriteReviewScene.Create();SessionState.SetBool(Key,true);SessionState.SetInt(Key+"stage",0);
            EditorSceneManager.OpenScene(GameScaffold.GameScene);SetSize(1920,1080);
            EditorApplication.EnterPlaymode();
        }
        private static void SetSize(int width,int height)
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;
            var sizes=assembly.GetType("UnityEditor.GameViewSizes");var single=assembly.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizes);
            object instance=single.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
            object group=sizes.GetMethod("GetGroup").Invoke(instance,new object[]{0});
            var sizeType=assembly.GetType("UnityEditor.GameViewSizeType");var size=assembly.GetType("UnityEditor.GameViewSize");
            object fixedSize=Activator.CreateInstance(size,new[]{Enum.ToObject(sizeType,1),(object)width,height,"Evidence "+width+"x"+height});
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{fixedSize});
            int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
            var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,count-1);view.Repaint();
        }
        private static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Visual capture timed out");
                if(root==null)
                {
                    root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();if(root==null)return;
                    root.enabled=false;clock=new ManualClock(new DateTime(2026,9,21,12,0,0,DateTimeKind.Utc));
                    root.UseTestStore(Path.GetFullPath("Docs/Implementation/Validation/capture_save.json"),clock);root.NewGame("Burning Hills QA");stage=0;frame=0;
                }
                if(++frame<30){EditorApplication.QueuePlayerLoopUpdate();return;}
                if(pending!=null)
                {
                    if(!File.Exists(pending)||File.GetLastWriteTimeUtc(pending)<requestedUtc){EditorApplication.QueuePlayerLoopUpdate();return;}
                    pending=null;stage++;frame=0;return;
                }
                if(requested!=null){pending=requested;requested=null;requestedUtc=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(pending);frame=0;EditorApplication.QueuePlayerLoopUpdate();return;}
                switch(stage)
                {
                    case 0:Capture("01_intro_1920x1080");break;
                    case 1:root.Session.CompleteIntro();root.Ui.Close();root.Session.Upgrades.BuyWithMoney("farm_grow_01");root.Session.Advance(12);root.Select("farm_grow_01",true);root.Refresh();Capture("02_manual_farm");break;
                    case 2:Drive(()=>root.Session.State.City.unlocked>=Zone.City);root.Select("farm_processing",true);root.Session.Advance(180);root.Refresh();Capture("03_farm_backlog");break;
                    case 3:Drive(()=>root.Session.State.City.unlocked>=Zone.Settlement);root.Select("coffee_01",true);root.Ui.ShowTownHall();root.Refresh();Capture("04_city_townhall");break;
                    case 4:root.Ui.ShowWorkers();root.Refresh();Capture("05_workers_spin");break;
                    case 5:root.Ui.Close();Drive(()=>root.Session.State.City.unlocked>=Zone.Industry);root.Select("settlement_01",true);root.Refresh();Capture("06_settlement");break;
                    case 6:Drive(()=>root.Session.State.City.freeplay);root.Select("butter_factory",true);root.Refresh();Capture("07_industry_freeplay");break;
                    case 7:SetSize(1366,768);root.Ui.Close();root.Select("bakery_01",true);root.Refresh();Capture("08_freeplay_1366x768");break;
                    case 8:SetSize(2560,1080);root.FocusZone(Zone.Farm);root.Refresh();Capture("09_freeplay_2560x1080");break;
                    case 9:SetSize(1366,768);root.Session.State.uiScale=1.15f;root.Refresh();Capture("10_ui_max_scale_1366x768");break;
                    case 10:
                        SetSize(1920,1080);
                        foreach(var obj in SceneManager.GetActiveScene().GetRootGameObjects())obj.SetActive(false);
                        EditorSceneManager.LoadSceneInPlayMode(SpriteReviewScene.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));
                        Capture("10_sprite_review_zoom26");break;
                    case 11:Camera.main.orthographicSize=38;Capture("11_sprite_review_zoom38");break;
                    case 12:
                        var camera=Camera.main;camera.orthographicSize=14;camera.transform.position=camera.transform.right*(-33)+Vector3.up*11-camera.transform.forward*90;
                        Capture("12_townhall_zoom14");break;
                    case 13:
                        var finalCamera=Camera.main;finalCamera.orthographicSize=14;finalCamera.transform.position=finalCamera.transform.right*33+Vector3.up*11-finalCamera.transform.forward*90;
                        Capture("13_coffee_final_zoom14");break;
                    default:
                        SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
                        File.WriteAllText("Docs/Implementation/Validation/visual_capture.json","{\"utc\":\""+DateTime.UtcNow.ToString("O")+"\",\"mode\":\"automated_playmode_gameview\",\"screenshots\":14,\"manualAcceptance\":false}");EditorApplication.Exit(0);break;
                }
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch(Exception ex){SessionState.SetBool(Key,false);File.WriteAllText("Docs/Implementation/Validation/visual_capture_error.txt",ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        private static void Drive(Func<bool> condition)
        {
            for(int i=0;i<TutorialDriver.MaxSimulationSeconds&&!condition();i++){TutorialDriver.Act(root.Session,clock.UtcNow);clock.UtcNow=clock.UtcNow.AddSeconds(1);root.Session.Advance(1);}
            if(!condition())throw new InvalidOperationException("Capture progression stalled at "+root.Session.Progression.Current?.id);
        }
        private static void Capture(string name)
        {
            // Give the actual Game View thirty player-loop updates to settle after each UI/camera change.
            requested=Path.GetFullPath(Path.Combine(Folder,name+".png"));frame=0;
        }
    }
}

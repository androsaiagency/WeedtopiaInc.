using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    [Serializable] public sealed class PolishCaptureReceipt
    {public string utc;public bool complete;public string[] screenshots,issues,resolutions;public float[] displayDensities;public bool physicalTouchVerified=false;}
    /// <summary>Owned test save and real Game View rendering. Never reads or overwrites the player's save.</summary>
    [InitializeOnLoad] public static class UiPolishEvidence
    {
        private const string Key="Weedtopia.UiPolishCapture",Folder="Docs/UiPolishRevision/Screenshots";
        private static GameRoot root;
        private static readonly List<string> screenshots=new List<string>(),issues=new List<string>(),resolutions=new List<string>();
        private static readonly List<(int width,int height,float density,string name,Action action)> steps=new List<(int,int,float,string,Action)>();
        private static readonly List<float> densities=new List<float>();
        private static int index,frames;
        private static string pending;
        private static DateTime requestUtc;
        private static double deadline;
        static UiPolishEvidence(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
        public static void Begin()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Owned batch editor required.");
            Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(GameScaffold.GameScene);SetSize(1920,1080);EditorApplication.EnterPlaymode();
        }
        private static void SetSize(int width,int height)
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;var sizes=assembly.GetType("UnityEditor.GameViewSizes");var singleton=assembly.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizes);
            var instance=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);var group=sizes.GetProperty("currentGroup",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(instance);
            var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new[]{Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"),1),(object)width,height,"Weedtopia UI QA "+width+"x"+height});
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
            var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            // Use Unity's selection callback, which updates the render target as well as the stored index.
            // Reference: Unity-Technologies/UnityCsReference, Editor/Mono/GameView/GameView.cs.
            view.GetType().GetMethod("SizeSelectionCallback",BindingFlags.Public|BindingFlags.Instance).Invoke(view,new object[]{count-1,null});view.Repaint();
        }
        private static void World()
        {
            root.NewGame("UI-Vorschau");root.Session.CompleteIntro();root.Ui.Close();root.Session.State.City.unlocked=Zone.Industry;root.Refresh();
        }
        private static void Project()
        {
            World();var s=root.Session.State;s.City.questIndex=root.Session.Progression.Quests.Length;s.City.completedQuests=root.Session.Progression.Quests.Select(q=>q.id).ToList();s.City.freeplay=true;
            s.City.project=new CityProjectState{planId="neighborhood",stepIndex=2,completedSteps=new List<string>{"harvest","logistics"},baselines=new Amount[]{0,0,1000}};
            foreach(string id in new[]{"coffee_01","coffee_03","coffee_04"}){s.Find(id).level=5;root.Session.Workers.Assign(root.Session.Workers.Grant("coffee").id,id,0);}
            s.Find("coffee_02").level=1;s.City.stats.coffeeSold=1100;root.Refresh();
        }
        private static void Prepare()
        {
            foreach(var size in new[]{(1920,1080,1f),(1280,720,1f),(844,390,1f),(390,844,1f),(2340,1080,2.75f)})
            {
                steps.Add((size.Item1,size.Item2,size.Item3,"start",()=>root.Ui.ShowStart()));
                steps.Add((size.Item1,size.Item2,size.Item3,"rocko_name",()=>root.NewGame("Neuling",true)));
                steps.Add((size.Item1,size.Item2,size.Item3,"rocko_intro",()=>{root.ChangeProfile("Kaya",PlayerProfile.DefaultAvatar);root.Ui.ShowIntro();}));
                steps.Add((size.Item1,size.Item2,size.Item3,"mila_explaining",()=>{World();root.Session.State.City.questIndex=1;root.Session.State.City.completedQuests.Add("farm_welcome");root.Ui.SpeakToSelected();}));
                steps.Add((size.Item1,size.Item2,size.Item3,"mila_pleased",()=>{root.Session.State.Find("farm_grow_01").level=1;root.Ui.SpeakToSelected();}));
                steps.Add((size.Item1,size.Item2,size.Item3,"nia_dialogue",()=>root.Ui.SpeakToCharacter("nia")));
                steps.Add((size.Item1,size.Item2,size.Item3,"jaro_dialogue",()=>root.Ui.SpeakToCharacter("jaro")));
                steps.Add((size.Item1,size.Item2,size.Item3,"active_quests",Project));
                steps.Add((size.Item1,size.Item2,size.Item3,"quest_preview",()=>root.Ui.Experience.quests.Open("project")));
                steps.Add((size.Item1,size.Item2,size.Item3,"coffeeshop_checklist",()=>root.Ui.Experience.quests.OpenDetails("project")));
                steps.Add((size.Item1,size.Item2,size.Item3,"worker_picker",()=>{root.Ui.Close();root.Session.State.Find("farm_grow_01").level=1;root.Session.Workers.Grant("grow");root.Ui.Interaction.OpenBuilding("farm_grow_01");root.Ui.Interaction.OpenPicker(0);root.Refresh();}));
                steps.Add((size.Item1,size.Item2,size.Item3,"settings",()=>root.Ui.Experience.ShowSettings()));
                steps.Add((size.Item1,size.Item2,size.Item3,"profile",()=>{root.Ui.Experience.ShowSettings();root.Ui.Experience.SelectSettingsTab(true);}));
                steps.Add((size.Item1,size.Item2,size.Item3,"new_game",()=>root.Ui.Experience.AskRestart()));
                steps.Add((size.Item1,size.Item2,size.Item3,"building_inspector",()=>{root.Ui.Close();root.Ui.Interaction.OpenBuilding("farm_grow_01");root.Refresh();}));
                steps.Add((size.Item1,size.Item2,size.Item3,"team",()=>root.Ui.ShowWorkers()));
                steps.Add((size.Item1,size.Item2,size.Item3,"journal",()=>root.Ui.ShowJournal()));
                steps.Add((size.Item1,size.Item2,size.Item3,"inventory",()=>root.Ui.ShowCollection()));
                steps.Add((size.Item1,size.Item2,size.Item3,"wheel",()=>root.Ui.ShowLuckyWheel()));
                steps.Add((size.Item1,size.Item2,size.Item3,"stories",()=>root.Ui.ShowStories()));
                steps.Add((size.Item1,size.Item2,size.Item3,"cities",()=>root.Ui.ShowCities()));
            }
        }
        private static void Inspect(string screen)
        {
            Canvas.ForceUpdateCanvases();float px=UiDisplayMetrics.Scale(root.Ui.GetComponent<Canvas>());
            var status=root.Ui.GetComponentInChildren<CityStatusBar>(true);
            if(status.Settings.gameObject.activeInHierarchy&&Overlaps(status.Settings,status.Resources))issues.Add(screen+": settings overlaps resources");
            var experience=root.Ui.Experience;
            var surfaces=new[]{root.Ui.Element<RectTransform>("start"),experience.intro.transform,experience.conversation.transform,experience.profileScroll.transform,experience.gameScroll.transform,experience.quests.heading.transform.parent,experience.quests.preview,experience.restartBody.transform.parent};
            foreach(var surface in surfaces.Where(s=>s.gameObject.activeInHierarchy))
            foreach(var label in surface.GetComponentsInChildren<Text>().Where(t=>t.enabled&&!string.IsNullOrWhiteSpace(t.text)))
            {
                float preferred=label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text,label.GetGenerationSettings(label.rectTransform.rect.size))/label.pixelsPerUnit;
                if(preferred>label.rectTransform.rect.height+2)issues.Add(screen+": text overflow "+label.name+" needs "+preferred+" has "+label.rectTransform.rect.height);
                if(label.text.Contains("[ui.")||label.text.Contains("[speaker."))issues.Add(screen+": unresolved text "+label.text);
                if(label.fontSize*px<15 && label.GetComponentInParent<Button>()==null)issues.Add(screen+": text below 15 pixels "+label.name);
            }
            var corners=new Vector3[4];foreach(var action in root.Ui.GetComponentsInChildren<Button>())
            {
                if(action.GetComponentInParent<ScrollRect>()!=null||action.name=="DialogueTap")continue;((RectTransform)action.transform).GetWorldCorners(corners);var area=Screen.safeArea;
                if(corners.Min(c=>c.x)<area.xMin-2||corners.Max(c=>c.x)>area.xMax+2||corners.Min(c=>c.y)<area.yMin-2||corners.Max(c=>c.y)>area.yMax+2)issues.Add(screen+": action outside safe area "+action.name);
            }
        }
        private static bool Overlaps(RectTransform a,RectTransform b)
        {
            var first=new Vector3[4];var second=new Vector3[4];a.GetWorldCorners(first);b.GetWorldCorners(second);
            return first.Min(v=>v.x)<second.Max(v=>v.x)&&first.Max(v=>v.x)>second.Min(v=>v.x)&&first.Min(v=>v.y)<second.Max(v=>v.y)&&first.Max(v=>v.y)>second.Min(v=>v.y);
        }
        private static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+900;if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("UI capture deadline.");
                if(root==null)
                {
                    root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();if(root==null)return;root.enabled=false;
                    root.UseTestStore(Path.GetFullPath("Docs/UiPolishRevision/capture_"+Guid.NewGuid().ToString("N")+".json"),new ManualClock(new DateTime(2026,10,3,12,0,0,DateTimeKind.Utc)));Prepare();
                }
                if(pending!=null)
                {
                    if(!File.Exists(pending)||File.GetLastWriteTimeUtc(pending)<requestUtc){EditorApplication.QueuePlayerLoopUpdate();return;}
                    screenshots.Add(pending);pending=null;index++;frames=0;
                }
                if(index>=steps.Count){Finish();return;}
                var step=steps[index];if(frames==0){UiDisplayMetrics.EvidenceDensity=step.density;SetSize(step.width,step.height);step.action();root.Refresh();root.Ui.Notify("");}
                if(++frames<14){EditorApplication.QueuePlayerLoopUpdate();return;}
                if(Screen.width!=step.width||Screen.height!=step.height){if(frames<300){EditorApplication.QueuePlayerLoopUpdate();return;}throw new InvalidOperationException("Actual "+Screen.width+"x"+Screen.height+" differs from requested "+step.width+"x"+step.height);}
                string name=step.width+"x"+step.height+"_"+step.name;Inspect(name);string size=Screen.width+"x"+Screen.height;if(!resolutions.Contains(size)){resolutions.Add(size);densities.Add(step.density);}
                pending=Folder+"/"+name+".png";requestUtc=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(pending);EditorApplication.QueuePlayerLoopUpdate();
            }
            catch(Exception ex){issues.Add(ex.ToString());Debug.LogError(ex);Finish();}
        }
        private static void Finish()
        {
            var receipt=new PolishCaptureReceipt{utc=DateTime.UtcNow.ToString("O"),complete=screenshots.Count==steps.Count&&issues.Count==0,screenshots=screenshots.ToArray(),issues=issues.Distinct().ToArray(),resolutions=resolutions.ToArray(),displayDensities=densities.ToArray()};
            UiDisplayMetrics.EvidenceDensity=0;
            File.WriteAllText("Docs/UiPolishRevision/capture_receipt.json",JsonUtility.ToJson(receipt,true));SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.Exit(receipt.complete?0:1);
        }
    }
}

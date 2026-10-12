using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Weedtopia.Application;
using Weedtopia.Content;
using Weedtopia.Domain;
using Weedtopia.Persistence;
using Weedtopia.Buildings.Presentation;

namespace Weedtopia.Unity
{
    public sealed class SystemClock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField] private GameContent content;
        [SerializeField] private GameVisualSet developmentVisuals;
        [SerializeField] private BuildingVisualCatalog productionCatalog;
        [SerializeField] private bool productionVisualsApproved;
        [SerializeField] private GameUi ui;
        [SerializeField] private IsometricCameraRig cameraRig;
        [SerializeField] private BuildingView[] sites;
        [SerializeField] private DeliveryView[] vehicles;
        [SerializeField] private GameFeedback feedback;
        [SerializeField] private ProductionPresentation presentation;
        [SerializeField] private string saveFileName="burning_hills_v2.json";
        private IClock clock;
        private LocalSaveStore store;
        private LocalLeaderboard leaderboard;
        private float repaintAfter;
        public GameSession Session {get;private set;}
        public GameDefinitions Definitions {get;private set;}
        public GameUi Ui=>ui;
        public ProductionPresentation Presentation=>presentation;
        public BuildingView[] Sites=>sites;
        public IsometricCameraRig CameraRig=>cameraRig;
        public string SelectedId {get;private set;}="farm_grow_01";
        public bool HasSave=>store!=null&&store.Exists;
        public IBuildingVisualProvider Visuals=>productionVisualsApproved?(IBuildingVisualProvider)productionCatalog:developmentVisuals;
        public bool UsesProductionBuildings => productionVisualsApproved;
        public string SavePath=>store?.Path;
        private void Awake()
        {
            if(content==null||(!productionVisualsApproved&&developmentVisuals==null)||productionCatalog==null||ui==null||cameraRig==null||sites==null||vehicles==null)throw new InvalidOperationException("Missing GameRoot authored reference");
            Definitions=content.Load();clock=new SystemClock();
            store=new LocalSaveStore(Path.Combine(UnityEngine.Application.persistentDataPath,saveFileName),Definitions);
            leaderboard=new LocalLeaderboard(Path.Combine(UnityEngine.Application.persistentDataPath,"local_leaderboard.json"));
            ui.Initialize(this);cameraRig.Initialize(this);cameraRig.EnterDistrict(Zone.Farm);
            foreach(var site in sites)site.Initialize(this);
            if(presentation!=null)presentation.Initialize(this);
            ui.ShowStart();
        }
        public void UseTestStore(string path,IClock testClock)
        {
            if(Session!=null)throw new InvalidOperationException("Set test storage before starting a session");
            clock=testClock;store=new LocalSaveStore(path,Definitions);leaderboard=new LocalLeaderboard(path+".scores");
        }
        public bool NewGame(string player)
        {
            try {Session=new GameSession(Definitions,GameState.Create(Definitions,player,clock.UtcNow),clock,store);Session.Save();SelectedId="farm_grow_01";Focus(SelectedId);ui.ShowIntro();Refresh();return true;}
            catch(ArgumentException){ui.Notify(Definitions.Text("name.invalid"));return false;}
            catch(IOException ex){ui.Notify(ex.Message);return false;}
        }
        public bool Continue()
        {
            var loaded=store.Load();if(loaded.State==null){ui.Notify(Definitions.Text(loaded.Message));return false;}
            Session=new GameSession(Definitions,loaded.State,clock,store);
            var offline=Session.ApplyOffline();SelectedId="farm_grow_01";Focus(SelectedId);Refresh();
            if(Session.State.introSeen)ui.ShowOffline(offline,loaded.Recovered);else ui.ShowIntro();return true;
        }
        private void Update()
        {
            if(Session==null)return;
            Session.Advance(Time.unscaledDeltaTime);
            if(Time.unscaledTime>=repaintAfter){Refresh();repaintAfter=Time.unscaledTime+.1f;}
        }
        public void Refresh()
        {
            if(Session==null)return;
            foreach(var site in sites)site.Refresh();
            for(int i=0;i<vehicles.Length;i++)vehicles[i].Present(Session.State.deliveries.Count>i?Session.State.deliveries[i]:null,this);
            ui.Refresh();if(presentation!=null)presentation.Refresh();AudioListener.volume=Session.State.volume;
        }
        public Failure Command(Func<Failure> command)
        {
            if(Session==null)return Failure.Invalid;
            try{var result=Session.Transact(command);if(feedback!=null)feedback.Play(result==Failure.None);ui.Notify(Definitions.Text("failure."+result));Refresh();return result;}
            catch(IOException ex){ui.Notify(ex.Message);return Failure.Invalid;}
        }
        public void Select(string id,bool focus=false)
        {
            if(Session==null)return;
            var b=Session.State.Find(id);var d=Definitions.Building(b.family);
            if(d.zone>Session.State.City.unlocked){ui.Notify(Definitions.Text("failure.Locked"));return;}
            SelectedId=id;if(focus)Focus(id);Refresh();
        }
        public void Focus(string id)
        {
            var site=sites.FirstOrDefault(x=>x.InstanceId==id);
            if(site!=null){cameraRig.EnterDistrict(Definitions.instances.Single(x=>x.id==id).zone);cameraRig.Focus(site.transform.position);}
        }
        public void FocusZone(Zone zone)
        {
            if(Session==null)return;
            if(Session.State.City.unlocked<zone){ui.Notify(Definitions.Text("failure.Locked"));return;}
            var site=sites.First(x=>Definitions.instances.First(i=>i.id==x.InstanceId).zone==zone);Select(site.InstanceId,true);
        }
        public BuildingView Site(string id)=>sites.Single(x=>x.InstanceId==id);
        public void Save() => SaveProgress(true);
        public void SaveSilently() => SaveProgress(false);
        private void SaveProgress(bool notify)
        {
            if(Session==null)return;
            try{Session.Save();RecordScore();if(notify)ui.Notify(Definitions.Text("ui.saved"));}catch(IOException ex){ui.Notify(ex.Message);}
        }
        public string Scores()
        {
            RecordScore();return string.Join("\n",leaderboard.Read().Select((x,i)=>(i+1)+". "+x.player+" · "+x.earned));
        }
        private void RecordScore()
        {
            if(Session==null)return;var s=Session.State;
            leaderboard.Record(new LeaderboardEntry{player=s.playerName,cityId=s.activeCity,earned=s.City.stats.earned,playSeconds=s.City.stats.playSeconds,freeplay=s.City.freeplay});
        }
        private void OnApplicationPause(bool paused){if(paused)SaveSilently();}
        private void OnApplicationFocus(bool focused){if(!focused)SaveSilently();}
        private void OnApplicationQuit(){SaveSilently();}
        public void Pause(bool value){if(Session==null)return;Session.Paused=value;if(value)SaveSilently();}
#if UNITY_EDITOR
        public void EditorConfigure(GameContent c,GameVisualSet v,BuildingVisualCatalog p,GameUi u,IsometricCameraRig camera,BuildingView[] b,DeliveryView[] pool)
        {content=c;developmentVisuals=v;productionCatalog=p;ui=u;cameraRig=camera;sites=b;vehicles=pool;}
#endif
    }
}

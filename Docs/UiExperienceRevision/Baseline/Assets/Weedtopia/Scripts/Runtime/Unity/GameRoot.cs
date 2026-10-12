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
        [SerializeField] private VipTownPresentation storyWorld;
        public VipTownPresentation StoryWorld => storyWorld;
        [SerializeField] private string saveFileName="burning_hills_v2.json";
        [SerializeField] private string sceneCityId="burning_hills";
        public string SceneCityId => sceneCityId;
        public bool TravelLoading {get;private set;}
        private IClock clock;
        private LocalSaveStore store;
        private LocalLeaderboard leaderboard;
        private string leaderboardPath;
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
            var handoff=CitySceneHandoff.Consume(sceneCityId);
            if(handoff!=null)clock=handoff.clock;
            store=new LocalSaveStore(Path.Combine(UnityEngine.Application.persistentDataPath,saveFileName),Definitions);
            if(handoff!=null)store=new LocalSaveStore(handoff.savePath,Definitions);
            leaderboardPath=handoff!=null?handoff.leaderboardPath:Path.Combine(UnityEngine.Application.persistentDataPath,"local_leaderboard.json");
            leaderboard=new LocalLeaderboard(leaderboardPath);
            ui.Initialize(this);cameraRig.Initialize(this);cameraRig.EnterDistrict(Zone.Farm);
            foreach(var site in sites)site.Initialize(this);
            if(presentation!=null)presentation.Initialize(this);
            if(storyWorld!=null)storyWorld.Initialize(this);
            if(handoff==null)ui.ShowStart();
            else
            {
                Session=new GameSession(Definitions,handoff.state,clock,store);
                SelectedId=Definitions.instances.First(i=>i.cityId==sceneCityId&&i.family=="cultivation_plot").id;
                Focus(SelectedId);ui.Close();Refresh();
                if(handoff.offline!=null)ui.ShowOffline(handoff.offline,handoff.recovered);
                else if(!Session.State.introSeen)ui.ShowIntro();
            }
        }
        public void UseTestStore(string path,IClock testClock)
        {
            if(Session!=null)throw new InvalidOperationException("Set test storage before starting a session");
            clock=testClock;store=new LocalSaveStore(path,Definitions);leaderboardPath=store.Path+".scores";leaderboard=new LocalLeaderboard(leaderboardPath);
        }
        public bool NewGame(string player)
        {
            try {ui.Rewards?.DismissAll();Session=new GameSession(Definitions,GameState.Create(Definitions,player,clock.UtcNow),clock,store);Session.Save();if(Session.State.activeCity!=sceneCityId)return LoadActiveCity(false);SelectedId="farm_grow_01";Focus(SelectedId);ui.ShowIntro();Refresh();return true;}
            catch(ArgumentException){ui.Notify(Definitions.Text("name.invalid"));return false;}
            catch(IOException ex){ui.Notify(ex.Message);return false;}
        }
        public bool Continue()
        {
            var loaded=store.Load();if(loaded.State==null){ui.Notify(Definitions.Text(loaded.Message));return false;}
            ui.Rewards?.DismissAll();Session=new GameSession(Definitions,loaded.State,clock,store);
            if(Session.State.activeCity!=sceneCityId)return LoadActiveCity(true,loaded.Recovered);
            var offline=Session.ApplyOffline();SelectedId=Definitions.instances.First(i=>i.cityId==sceneCityId&&i.family=="cultivation_plot").id;Focus(SelectedId);Refresh();
            if(Session.State.introSeen)ui.ShowOffline(offline,loaded.Recovered);else ui.ShowIntro();return true;
        }
        private void Update()
        {
            if(Session==null||TravelLoading)return;
            Session.Advance(Time.unscaledDeltaTime);
            if(Time.unscaledTime>=repaintAfter){Refresh();repaintAfter=Time.unscaledTime+.1f;}
        }
        public void Refresh()
        {
            if(Session==null)return;
            foreach(var site in sites)site.Refresh();
            var localDeliveries=Session.State.deliveries.Where(d=>Definitions.instances.First(b=>b.id==Definitions.routes.First(r=>r.id==d.route).from).cityId==sceneCityId).ToArray();
            for(int i=0;i<vehicles.Length;i++)vehicles[i].Present(localDeliveries.Length>i?localDeliveries[i]:null,this);
            ui.Refresh();if(presentation!=null)presentation.Refresh();AudioListener.volume=Session.State.volume;
            if(storyWorld!=null)storyWorld.Refresh();
        }
        public Failure Command(Func<Failure> command)
            => ExecuteCommand(command, false);
        public Failure StoryCommand(Func<Failure> command)
            => ExecuteCommand(command, true);
        public Failure CampaignCommand(Func<Failure> command)
        {
            if(Session==null||TravelLoading)return Failure.Invalid;
            long rewardBefore=Session.State.rewardSequence;
            try
            {
                var result=Session.TransactCampaign(command);
                if(Session.State.activeCity==sceneCityId)Refresh();
                ui.Notify(Definitions.Text("failure."+result));
                if(result==Failure.None)ui.Rewards?.EnqueueAfter(rewardBefore);
                return result;
            }
            catch(IOException ex){ui.Notify(ex.Message);return Failure.Invalid;}
        }
        public Failure TravelToCity(string id)
        {
            if(Session==null||TravelLoading)return Failure.Invalid;
            var destination=Definitions.cities.FirstOrDefault(c=>c.id==id);
            if(destination==null||!UnityEngine.Application.CanStreamedLevelBeLoaded(destination.scenePath))
            {ui.Notify(Definitions.Text("campaign.no_destination"));return Failure.Invalid;}
            var result=CampaignCommand(()=>Session.Campaign.Travel(id));
            if(result!=Failure.None)return result;
            return LoadActiveCity(false)?Failure.None:Failure.Invalid;
        }
        private bool LoadActiveCity(bool offline,bool recovered=false)
        {
            var destination=Definitions.cities.FirstOrDefault(c=>c.id==Session.State.activeCity);
            if(destination==null||!UnityEngine.Application.CanStreamedLevelBeLoaded(destination.scenePath))
            {ui.Notify(Definitions.Text("campaign.no_destination"));return false;}
            try
            {
                var summary=offline?Session.ApplyOffline():null;
                CitySceneHandoff.Prepare(Session.State,clock,store.Path,leaderboardPath,summary,recovered);
                TravelLoading=true;Session.Paused=true;ui.Notify(Definitions.Text("campaign.loading"));
                SceneManager.LoadSceneAsync(destination.scenePath,LoadSceneMode.Single);return true;
            }
            catch(IOException ex){CitySceneHandoff.Clear();TravelLoading=false;Session.Paused=false;ui.Notify(ex.Message);return false;}
        }
        private Failure ExecuteCommand(Func<Failure> command, bool story)
        {
            if(Session==null)return Failure.Invalid;
            long rewardBefore=Session.State.rewardSequence;
            try{var result=story?Session.TransactStory(command):Session.Transact(command);if(feedback!=null)feedback.Play(result==Failure.None);ui.Notify(Definitions.Text("failure."+result));Refresh();if(result==Failure.None)ui.Rewards?.EnqueueAfter(rewardBefore);return result;}
            catch(IOException ex){ui.Notify(ex.Message);return Failure.Invalid;}
        }
        public Failure SpinWheel(string token, out WheelSpinResult result)
        {
            result = null;
            if(Session == null)return Failure.Invalid;
            try
            {
                var failure = Session.SpinWheel(token, out result);
                if(failure != Failure.None)ui.Notify(Definitions.Text("failure."+failure));
                Refresh(); return failure;
            }
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

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private bool productionVisualsApproved = default;
        [SerializeField] private GameUi ui;
        [SerializeField] private IsometricCameraRig cameraRig;
        [SerializeField] private BuildingView[] sites;
        [SerializeField] private DeliveryView[] vehicles;
        [SerializeField] private GameFeedback feedback = default;
        [SerializeField] private ProductionPresentation presentation = default;
        [SerializeField] private VipTownPresentation storyWorld = default;
        public VipTownPresentation StoryWorld => storyWorld;
        [SerializeField] private string saveFileName="burning_hills_v2.json";
        [SerializeField] private string sceneCityId="burning_hills";
        public string SceneCityId => sceneCityId;
        public bool TravelLoading {get;private set;}
        public bool ContinueBusy {get;private set;}
        public bool ContinuationPending {get;private set;}
#if UNITY_WEBGL && !UNITY_EDITOR
        private const int OfflineBatchSeconds = Simulation.OfflineTickGroupSeconds;
        private const double OfflineFrameBudgetMilliseconds = 8;
#endif
        private bool initializingSceneHandoff;
        private bool SessionActionsBlocked => ContinueBusy || ContinuationPending || initializingSceneHandoff;
        public float ContinueProgress {get;private set;}
        private IEnumerator<OfflineProgress> pendingOffline = default;
        private OfflineSimulationOperation pendingOfflineWorker;
        private System.Diagnostics.Stopwatch continuationWatch, offlineSimulationWatch;
        private int continuationWorkers, continuationBuildings;
        private ICitySceneLoader sceneLoader=new UnityCitySceneLoader();
        private IClock clock;
        private LocalSaveStore store;
        private LocalLeaderboard leaderboard;
        private string leaderboardPath;
        private float repaintAfter;
        private GameSession storageStatusSession;
        private bool storageFailureNotified;
        public GameSession Session {get;private set;}
        public GameDefinitions Definitions {get;private set;}
        public LocalizationService Localization {get;private set;}
        public const string LanguagePreferenceKey="Weedtopia.Language";
        public GameUi Ui=>ui;
        public ProductionPresentation Presentation=>presentation;
        public BuildingView[] Sites=>sites;
        public IsometricCameraRig CameraRig=>cameraRig;
        public string SelectedId {get;private set;}="farm_grow_01";
        public bool HasSave=>store!=null&&store.Exists;
        public IBuildingVisualProvider Visuals=>productionVisualsApproved?(IBuildingVisualProvider)productionCatalog:developmentVisuals;
        public bool UsesProductionBuildings => productionVisualsApproved;
        private IShopPurchaseGateway ShopGateway() => UnityEngine.Application.isEditor || Debug.isDebugBuild ? (IShopPurchaseGateway)new TestShopPurchaseGateway(()=>Localization.Culture) : new UnavailableShopPurchaseGateway(()=>Localization.Culture);
        public string SavePath=>store?.Path;
        private void Awake()
        {
            if(content==null||(!productionVisualsApproved&&developmentVisuals==null)||productionCatalog==null||ui==null||cameraRig==null||sites==null||vehicles==null)throw new InvalidOperationException("Missing GameRoot authored reference");
            Definitions=content.Load();
            if(content.Languages==null)throw new InvalidOperationException("Missing authored language catalog. Run Weedtopia/Localization/Install or Repair.");
            Localization=Definitions.TextLookup as LocalizationService ?? content.Languages.Create();
            Definitions.TextLookup=Localization;
            Localization.Select(PlayerPrefs.GetString(LanguagePreferenceKey,content.Languages.fallback));
            GameText.Use(Localization);
            Localization.Changed+=RefreshLanguage;
            clock=new SystemClock();
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
                // The source root already durably committed the complete state. Initial panel
                // callbacks must not rewrite that save or replace its historical backup.
                initializingSceneHandoff=true;
                try
                {
                    Session=new GameSession(Definitions,handoff.state,clock,store,ShopGateway());
                    SelectedId=Definitions.instances.First(i=>i.cityId==sceneCityId&&i.family=="cultivation_plot").id;
                    Focus(SelectedId);ui.Close();Refresh();
                    if(handoff.offline!=null){Session.Paused=true;ui.ShowOffline(handoff.offline,handoff.recovered);}
                    else if(!Session.State.introSeen){Session.Paused=true;ui.ShowIntro();}
                }
                finally { initializingSceneHandoff=false; }
                RecordStartupReady(handoff.startupTimer,handoff.offline?.Seconds??0);
            }
        }
        public void UseTestStore(string path,IClock testClock)
        {
            if(Session!=null)throw new InvalidOperationException("Set test storage before starting a session");
            clock=testClock;store=new LocalSaveStore(path,Definitions);leaderboardPath=store.Path+".scores";leaderboard=new LocalLeaderboard(leaderboardPath);
        }
        public void UseTestSceneLoader(ICitySceneLoader loader)
        {
            if(ContinueBusy||TravelLoading)throw new InvalidOperationException("Replace scene loading before a continuation or travel begins.");
            sceneLoader=loader??throw new ArgumentNullException(nameof(loader));
        }
        public bool Play() => ContinuationPending || HasSave ? Continue() : NewGame("Neuling", true);
        public bool PlayResponsive()
        {
            if(ContinueBusy||TravelLoading)return false;
            return ContinuationPending||HasSave?ContinueResponsive():NewGame("Neuling",true);
        }
        public bool ContinueResponsive()
        {
            if(ContinueBusy||TravelLoading)return false;
            if(Session?.Shop.Purchasing==true)
            {ui.Notify(GameText.Get("presentation.gameroot.ein_kauf_wird_noch_verarbeitet_bitte_kur.02fd7903"));return false;}
            ContinuationPending=true;ContinueBusy=true;ContinueProgress=0;
            continuationWatch=System.Diagnostics.Stopwatch.StartNew();
            ui.ShowStart();ui.SetContinueProgress(0);
            StartCoroutine(ContinueInFrames());
            return true;
        }
        private IEnumerator ContinueInFrames()
        {
            try
            {
                // Draw the disabled authored Play button and progress before disk loading or simulation.
                yield return null;
                var loadWatch=System.Diagnostics.Stopwatch.StartNew();
                if(!TryLoadContinuation(out var next,out bool recovered))yield break;
                continuationWorkers=next.State.workers.Count;continuationBuildings=next.State.buildings.Count;
                LocalPlayerDiagnostics.RecordTiming(LocalPlayerDiagnostics.Operation.StartupLoad,loadWatch.Elapsed.TotalMilliseconds,0,continuationWorkers,continuationBuildings);
                OfflineSummary summary=null;
#if UNITY_WEBGL && !UNITY_EDITOR
                // Browser targets without a ThreadPool retain the bounded main-thread iterator.
                pendingOffline=next.ApplyOfflineIncrementally(OfflineBatchSeconds).GetEnumerator();
                var frameBudget=System.Diagnostics.Stopwatch.StartNew();
                while(TryAdvanceOffline(next,out var progress))
                {
                    ContinueProgress=(float)progress.Fraction;
                    if(progress.IsComplete){summary=progress.Summary;break;}
                    // Quick batches can share a frame; avoid a mandatory wait for every offline batch.
                    // Each batch remains bounded, and the initial progress always renders before simulation.
                    if(progress.CompletedSeconds==0||frameBudget.Elapsed.TotalMilliseconds>=OfflineFrameBudgetMilliseconds)
                    {
                        ui.SetContinueProgress(ContinueProgress);
                        yield return null;
                        frameBudget.Restart();
                    }
                }
                pendingOffline.Dispose();pendingOffline=null;
#else
                if(!TryStartOfflineWorker(next))yield break;
                yield return null;
                while(!TryFinishOfflineWorker(out summary))
                {
                    // The unpublished model belongs exclusively to the worker. Read atomic progress only.
                    float progress=(float)pendingOfflineWorker.Fraction;
                    if(progress!=ContinueProgress)
                    {
                        int previousPercent=Mathf.RoundToInt(ContinueProgress*100);
                        ContinueProgress=progress;
                        if(Mathf.RoundToInt(progress*100)!=previousPercent)ui.SetContinueProgress(progress);
                    }
                    yield return null;
                }
                pendingOfflineWorker.Dispose();pendingOfflineWorker=null;
#endif
                if(summary==null)yield break;
                ui.Notify("");
                if(ActivateContinuation(next,summary,recovered)&&!TravelLoading)RecordStartupReady(continuationWatch,summary.Seconds);
            }
            finally
            {
                pendingOffline?.Dispose();pendingOffline=null;
                pendingOfflineWorker?.Dispose();pendingOfflineWorker=null;
                ContinueBusy=false;ContinueProgress=0;
                if(ui!=null)ui.EndContinueProgress();
                if(!TravelLoading)continuationWatch?.Stop();
            }
        }
        private bool ActivateContinuation(GameSession next,OfflineSummary summary,bool recovered)
        {
            // A failed continuation retains the previous session without advancing or saving it.
            var previousSession=Session;string previousSelection=SelectedId;
            Session=next;
            SelectedId=Definitions.instances.First(i=>i.cityId==Session.State.activeCity&&i.family=="cultivation_plot").id;
            if(Session.State.activeCity!=sceneCityId)
            {
                if(!LoadActiveCity(false,recovered,summary))
                {Session=previousSession;SelectedId=previousSelection;return false;}
            }
            else
            {
                Focus(SelectedId);Refresh();
                if(Session.State.introSeen){Session.Paused=true;ui.ShowOffline(summary,recovered);}
                else ui.ShowIntro();
            }
            ContinuationPending=false;
            return true;
        }
        private bool TryLoadContinuation(out GameSession next,out bool recovered)
        {
            next=null;recovered=false;
            try
            {
                var loaded=store.Load();
                if(loaded.Error!=null)LocalPlayerDiagnostics.RecordException(LocalPlayerDiagnostics.Operation.Load,loaded.Error);
                if(loaded.State==null){ui.Notify(Definitions.Text(loaded.Message));return false;}
                if(loaded.State.activeCity!=sceneCityId)
                {
                    var destination=Definitions.cities.FirstOrDefault(c=>c.id==loaded.State.activeCity);
                    if(destination==null||!UnityEngine.Application.CanStreamedLevelBeLoaded(destination.scenePath))
                    {ui.Notify(Definitions.Text("campaign.no_destination"));return false;}
                }
                next=new GameSession(Definitions,loaded.State,clock,store,ShopGateway());
                recovered=loaded.Recovered;return true;
            }
            catch(Exception error) when(error is IOException||error is InvalidDataException||error is UnauthorizedAccessException)
            {NotifyStorageFailure(error,LocalPlayerDiagnostics.Operation.Load);return false;}
        }
        private bool TryAdvanceOffline(GameSession next,out OfflineProgress progress)
        {
            progress=null;
            try
            {
                if(!pendingOffline.MoveNext())return false;
                progress=pendingOffline.Current;return true;
            }
            catch(Exception error) when(error is IOException||error is UnauthorizedAccessException||ReferenceEquals(error,next.LastSaveError))
            {NotifyStorageFailure(error);return false;}
        }
        private bool TryStartOfflineWorker(GameSession next)
        {
            try { offlineSimulationWatch=System.Diagnostics.Stopwatch.StartNew();pendingOfflineWorker=next.StartOfflineSimulation();return true; }
            catch(Exception error) { NotifyContinuationFailure(error);return false; }
        }
        private bool TryFinishOfflineWorker(out OfflineSummary summary)
        {
            summary=null;
            if(!pendingOfflineWorker.SimulationFinished)return false;
            offlineSimulationWatch.Stop();
            LocalPlayerDiagnostics.RecordTiming(LocalPlayerDiagnostics.Operation.OfflineSimulation,offlineSimulationWatch.Elapsed.TotalMilliseconds,pendingOfflineWorker.TotalSeconds,continuationWorkers,continuationBuildings);
            var commitWatch=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                bool complete=pendingOfflineWorker.TryComplete(out summary);
                if(complete)LocalPlayerDiagnostics.RecordTiming(LocalPlayerDiagnostics.Operation.OfflineCommit,commitWatch.Elapsed.TotalMilliseconds,pendingOfflineWorker.TotalSeconds,continuationWorkers,continuationBuildings);
                return complete;
            }
            catch(Exception error)
            {
                // A failure is terminal for this attempt. Dispose never waits; the previous session stays blocked.
                NotifyContinuationFailure(error);return true;
            }
        }
        private void NotifyContinuationFailure(Exception error)
        {
            // Keep retry UI available even for a model failure. Detailed classification is recorded locally.
            if(error is IOException||error is UnauthorizedAccessException)NotifyStorageFailure(error,LocalPlayerDiagnostics.Operation.OfflineContinuation);
            else { LocalPlayerDiagnostics.RecordException(LocalPlayerDiagnostics.Operation.OfflineContinuation,error);Debug.LogError("Offline continuation failed. Retry remains available.");ui.Notify(Definitions.Text("save.error")); }
        }
        private void RecordStartupReady(System.Diagnostics.Stopwatch watch,int seconds)
        {
            if(watch==null)return;
            watch.Stop();
            LocalPlayerDiagnostics.RecordTiming(LocalPlayerDiagnostics.Operation.StartupReady,watch.Elapsed.TotalMilliseconds,seconds,Session.State.workers.Count,Session.State.buildings.Count);
        }
        public Failure ChangeProfile(string name,string avatar)
        {
            if(SessionActionsBlocked)return Failure.Busy;
            if(Session==null)return Failure.Invalid;
            try { var result=Session.UpdateProfile(name,avatar);Refresh();return result; }
            catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){NotifyStorageFailure(ex);return Failure.Invalid;}
        }
        public bool AdvanceIntro() => IntroAction(() => Session.NextIntro());
        public bool CompleteIntro() => IntroAction(() => Session.CompleteIntro());
        public bool RestartIntro() => IntroAction(() => Session.RestartIntro());
        private bool IntroAction(Action action)
        {
            if(Session==null||SessionActionsBlocked||TravelLoading)return false;
            try{action();return true;}
            catch(Exception error) when(error is IOException||error is UnauthorizedAccessException)
            {NotifyStorageFailure(error);return false;}
        }
        public bool RestartGame()
        {
            if(ContinueBusy||TravelLoading)return false;
            if(Session?.Shop.Purchasing==true){ui.Notify(GameText.Get("presentation.gameroot.ein_kauf_wird_noch_verarbeitet_bitte_kur.02fd7903"));return false;}
            try { if(!ContinuationPending)Session?.Save(); if(HasSave)store.BackupForRestart(); return NewGame("Neuling", true); }
            catch(Exception ex) when(ex is IOException||ex is InvalidDataException||ex is UnauthorizedAccessException) { NotifyStorageFailure(ex); return false; }
        }
        public bool NewGame(string player, bool askName = false)
        {
            if(ContinueBusy||TravelLoading)return false;
            if(Session?.Shop.Purchasing==true){ui.Notify(GameText.Get("presentation.gameroot.ein_kauf_wird_noch_verarbeitet_bitte_kur.02fd7903"));return false;}
            try {var state=GameState.Create(Definitions,player,clock.UtcNow);state.profileNamePending=askName;var next=new GameSession(Definitions,state,clock,store,ShopGateway());next.Save();ui.Rewards?.DismissAll();Session=next;ContinuationPending=false;if(askName)ui.Element<UnityEngine.UI.InputField>("name").SetTextWithoutNotify("");if(Session.State.activeCity!=sceneCityId)return LoadActiveCity(false);SelectedId="farm_grow_01";Focus(SelectedId);ui.ShowIntro();Refresh();return true;}
            catch(ArgumentException){ui.Notify(Definitions.Text("name.invalid"));return false;}
            catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){NotifyStorageFailure(ex);return false;}
        }
        public bool Continue()
        {
            if(ContinueBusy||TravelLoading)return false;
            if(Session?.Shop.Purchasing==true){ui.Notify(GameText.Get("presentation.gameroot.ein_kauf_wird_noch_verarbeitet_bitte_kur.02fd7903"));return false;}
            ContinuationPending=true;ui.ShowStart();
            if(!TryLoadContinuation(out var next,out bool recovered))return false;
            try
            {
                var summary=next.ApplyOffline();
                ui.Notify("");
                return ActivateContinuation(next,summary,recovered);
            }
            catch(Exception error) when(error is IOException||error is UnauthorizedAccessException||ReferenceEquals(error,next.LastSaveError))
            {NotifyStorageFailure(error);return false;}
        }
        private void Update()
        {
            if(Session==null||TravelLoading||SessionActionsBlocked)return;
            if(!ReferenceEquals(storageStatusSession,Session))
            {storageStatusSession=Session;storageFailureNotified=false;}
            Session.Advance(Time.unscaledDeltaTime);
            if(Session.LastSaveError==null)storageFailureNotified=false;
            else if(!storageFailureNotified)NotifyStorageFailure(Session.LastSaveError);
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
        public Failure StoryCommand(Func<Failure> command, bool notifySuccess = true)
            => ExecuteCommand(command, true, notifySuccess);
        public Failure CampaignCommand(Func<Failure> command)
        {
            if(SessionActionsBlocked)return Failure.Busy;
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
            catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){NotifyStorageFailure(ex);return Failure.Invalid;}
        }
        public Failure TravelToCity(string id)
        {
            if(Session?.Shop.Purchasing==true)return Failure.Busy;
            if(SessionActionsBlocked)return Failure.Busy;
            if(Session==null||TravelLoading)return Failure.Invalid;
            var destination=Definitions.cities.FirstOrDefault(c=>c.id==id);
            if(destination==null||!UnityEngine.Application.CanStreamedLevelBeLoaded(destination.scenePath))
            {ui.Notify(Definitions.Text("campaign.no_destination"));return Failure.Invalid;}
            var result=CampaignCommand(()=>Session.Campaign.Travel(id));
            if(result!=Failure.None)return result;
            return LoadActiveCity(false)?Failure.None:Failure.Invalid;
        }
        private bool LoadActiveCity(bool offline,bool recovered=false,OfflineSummary completedOffline=null)
        {
            var destination=Definitions.cities.FirstOrDefault(c=>c.id==Session.State.activeCity);
            if(destination==null||!UnityEngine.Application.CanStreamedLevelBeLoaded(destination.scenePath))
            {ContinuationPending=true;ui.ShowStart();ui.Notify(Definitions.Text("campaign.no_destination"));return false;}
            bool wasPaused=Session.Paused;
            try
            {
                var summary=completedOffline??(offline?Session.ApplyOffline():null);
                CitySceneHandoff.Prepare(Session.State,clock,store.Path,leaderboardPath,summary,recovered,ContinueBusy?continuationWatch:null);
                TravelLoading=true;Session.Paused=true;ui.Notify(Definitions.Text("campaign.loading"));
                if(!sceneLoader.Start(destination.scenePath))throw new InvalidOperationException("Scene loader did not return an active operation.");
                return true;
            }
            catch(Exception error)
            {
                CitySceneHandoff.Clear();TravelLoading=false;Session.Paused=wasPaused;
                ContinuationPending=true;ui.ShowStart();
                if(error is IOException||error is UnauthorizedAccessException)NotifyStorageFailure(error,LocalPlayerDiagnostics.Operation.Load);
                else
                {
                    LocalPlayerDiagnostics.RecordException(LocalPlayerDiagnostics.Operation.Load,error);
                    Debug.LogError("City scene load failed. Retry remains available.");
                    ui.Notify(Definitions.Text("campaign.no_destination"));
                }
                return false;
            }
        }
        private Failure ExecuteCommand(Func<Failure> command, bool story, bool notifySuccess = true)
        {
            if(SessionActionsBlocked)return Failure.Busy;
            if(Session==null)return Failure.Invalid;
            long rewardBefore=Session.State.rewardSequence;
            try
            {
                var result=story?Session.TransactStory(command):Session.Transact(command);
                if(feedback!=null)feedback.Play(result==Failure.None);
                ui.Notify(result!=Failure.None||notifySuccess?Definitions.Text("failure."+result):"");
                Refresh();if(result==Failure.None)ui.Rewards?.EnqueueAfter(rewardBefore);return result;
            }
            catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){NotifyStorageFailure(ex);return Failure.Invalid;}
        }
        public Failure SpinWheel(string token, out WheelSpinResult result)
        {
            result = null;
            if(SessionActionsBlocked)return Failure.Busy;
            if(Session == null)return Failure.Invalid;
            try
            {
                var failure = Session.SpinWheel(token, out result);
                if(failure != Failure.None)ui.Notify(Definitions.Text("failure."+failure));
                Refresh(); return failure;
            }
            catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){NotifyStorageFailure(ex);return Failure.Invalid;}
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
            if(Session==null||SessionActionsBlocked)return;
            try{Session.Save();RecordScore();if(notify)ui.Notify(Definitions.Text("ui.saved"));}catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){NotifyStorageFailure(ex);}
        }
        public string Scores()
        {
            RecordScore();return string.Join("\n",leaderboard.Read().Select((x,i)=>(i+1)+". "+x.player+" · "+x.earned));
        }
        private void RecordScore()
        {
            if(Session==null||SessionActionsBlocked)return;var s=Session.State;
            leaderboard.Record(new LeaderboardEntry{player=s.playerName,cityId=s.activeCity,earned=s.City.stats.earned,playSeconds=s.City.stats.playSeconds,freeplay=s.City.freeplay});
        }
        public bool SetLanguage(string code)
        {
            if(!Localization.Select(code))return false;
            PlayerPrefs.SetString(LanguagePreferenceKey,code);PlayerPrefs.Save();return true;
        }
        private void RefreshLanguage(){ui.RefreshLanguage();}
        private void OnDestroy()
        {
            pendingOffline?.Dispose();pendingOffline=null;
            pendingOfflineWorker?.Dispose();pendingOfflineWorker=null;
            if(Localization==null)return;
            Localization.Changed-=RefreshLanguage;GameText.Release(Localization);
        }
        private void NotifyStorageFailure(Exception error,LocalPlayerDiagnostics.Operation operation=LocalPlayerDiagnostics.Operation.Save)
        {
            storageStatusSession=Session;storageFailureNotified=true;
            LocalPlayerDiagnostics.RecordException(operation,error);
            Debug.LogWarning("Save operation failed: see local technical diagnostics.");
            ui.Notify(Definitions.Text("save.error"));
        }
        private void OnApplicationPause(bool paused){if(paused)SaveSilently();}
        private void OnApplicationFocus(bool focused){if(!focused)SaveSilently();}
        private void OnApplicationQuit(){SaveSilently();}
        public void Pause(bool value){if(Session==null||SessionActionsBlocked)return;Session.Paused=value;if(value)SaveSilently();}
#if UNITY_EDITOR
        public void EditorConfigure(GameContent c,GameVisualSet v,BuildingVisualCatalog p,GameUi u,IsometricCameraRig camera,BuildingView[] b,DeliveryView[] pool)
        {content=c;developmentVisuals=v;productionCatalog=p;ui=u;cameraRig=camera;sites=b;vehicles=pool;}
#endif
    }
}

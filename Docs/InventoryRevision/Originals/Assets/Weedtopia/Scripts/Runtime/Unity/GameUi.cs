using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Application;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    [Serializable] public sealed class UiBinding { public string id; public GameObject target; }
    public sealed class GameUi : MonoBehaviour
    {
        [SerializeField] private UiBinding[] bindings;
        [SerializeField] private GameObject[] panels;
        [SerializeField] private Button listRowPrefab;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private CanvasScaler scaler;
        [SerializeField] private BuildingInteractionUi interaction;
        [SerializeField] private WorldNavigationBar worldNavigation;
        [SerializeField] private CityStatusBar cityStatus;
        [SerializeField] private RewardPresentation rewards;
        [SerializeField] private RewardCollectionUi collection;
        [SerializeField] private RewardPreviewView rewardPreview;
        public RewardPresentation Rewards => rewards;
        public RewardCollectionUi Collection => collection;
        private float messageUntil;
        public BuildingInteractionUi Interaction => interaction;
        private GameRoot root;
        private readonly Dictionary<string,GameObject> map=new Dictionary<string,GameObject>();
        private readonly List<GameObject> rows=new List<GameObject>();
        private readonly List<Action> journalRefresh=new List<Action>();
        private string journalProjectKey;
        private string workerId,premiumToken;
        private bool initialized;
        public bool LegacyModalOpen=>(rewards != null && rewards.IsOpen) || panels.Any(x=>x.activeSelf);
        public bool ModalOpen=>LegacyModalOpen || (interaction != null && interaction.BlocksWorld);
        public T Element<T>(string id) where T:Component => map[id].GetComponent<T>();
        private Text Label(string id)=>Element<Text>(id);
        private Button Button(string id)=>Element<Button>(id);
        private void Click(string id,Action action)=>Button(id).onClick.AddListener(()=>action());
        private string T(string key)=>root.Definitions.Text(key);
        public void Initialize(GameRoot game)
        {
            if(initialized)throw new InvalidOperationException("UI already initialized");initialized=true;root=game;
            foreach(var b in bindings){if(b.target==null||map.ContainsKey(b.id))throw new InvalidOperationException("Invalid UI binding "+b.id);map.Add(b.id,b.target);}
            foreach(var pair in map)
            {
                if(pair.Key.StartsWith("ui.")){var text=pair.Value.GetComponentInChildren<Text>(true);if(text!=null)text.text=T(pair.Key);}
            }
            Label("title").text=T("game.title");Label("development").text=T("ui.dev");
            Label("rockoIdentity").text=T("ui.rocko_identity");
            Click("ui.start",()=>root.NewGame(Element<InputField>("name").text));
            Click("ui.new",()=>{Element<InputField>("name").text="";Element<InputField>("name").ActivateInputField();});Click("ui.continue",()=>root.Continue());
            Click("ui.next",()=>{root.Session.NextIntro();if(root.Session.State.introSeen)Close();else PaintIntro();});
            Click("ui.skip",()=>{root.Session.CompleteIntro();Close();});
            Click("ui.upgrade",Upgrade);Click("ui.finish",()=>Premium(true));
            Click("ui.harvest",()=>root.Command(()=>root.Session.Simulation.Harvest(root.SelectedId)));
            Click("ui.work",()=>root.Command(()=>root.Session.Simulation.Work(root.SelectedId)));
            Click("ui.talk",SpeakToSelected);
            if(map.ContainsKey("ui.dialog_continue"))Click("ui.dialog_continue",Close);
            Click("ui.claim",()=>{root.Command(()=>root.Session.Progression.Current != null ? root.Session.Progression.Claim() : root.Session.CityProject.Claim());if(map["journal"].activeSelf)ShowJournal();});
            Click("ui.focus",()=>{string focus=root.Session.Progression.Current?.focus ?? (root.Session.CityProject.Available ? root.Session.CityProject.Focus : null);if(focus!=null){Close();root.Focus(focus);}});
            Click("ui.workers",ShowWorkers);Click("ui.journal",ShowJournal);
            Click("ui.spin",()=>{var result=root.Command(()=>root.Session.Workers.Spin(out _));workerId=root.Session.State.workers.LastOrDefault()?.id;ShowWorkers();if(result==Failure.None&&root.Presentation!=null)root.Presentation.RevealWorker();});
            Click("ui.daily",()=>{root.Command(()=>root.Session.Daily.Claim(DateTime.UtcNow));ShowJournal();});
            Click("ui.house",()=>root.Command(()=>root.Session.ChoosePlot(root.SelectedId,"residential_house")));
            Click("ui.park",()=>root.Command(()=>root.Session.ChoosePlot(root.SelectedId,"park")));
            Click("ui.pause",()=>{root.Pause(true);ShowPanel("settings");});
            Click("ui.resume",Close);Click("ui.close",Close);Click("ui.cancel",()=>{root.Session.Upgrades.Cancel(premiumToken);premiumToken=null;Close();});
            Click("ui.confirm",()=>{root.Command(()=>root.Session.Upgrades.Confirm(premiumToken));premiumToken=null;Close();});
            Click("ui.restart_intro",()=>{root.Session.RestartIntro();ShowIntro();});
            Click("ui.reset",()=>{root.Save();root.Pause(true);ShowStart();});
            Click("ui.assign",()=>{root.Command(()=>root.Session.Workers.Assign(workerId,root.SelectedId,Element<Dropdown>("slot").value));ShowWorkers();});
            Click("ui.remove",()=>{root.Command(()=>root.Session.Workers.Remove(workerId));ShowWorkers();});
            Click("ui.worker_level",()=>{root.Command(()=>root.Session.Workers.Improve(workerId,false));PaintWorker();});
            Click("ui.worker_rank",()=>{root.Command(()=>root.Session.Workers.Improve(workerId,true));PaintWorker();});
            Click("recovery",()=>root.Command(()=>root.Session.RequestRecoveryWork()));Button("recovery").GetComponentInChildren<Text>().text=T("ui.recovery");
            foreach(Zone zone in Enum.GetValues(typeof(Zone)))
            {
                var captured=zone;
                // Navigation is available across the whole map. Building actions retain their progression checks.
                Click("zone."+zone,()=>{if(interaction!=null){interaction.townMenu.SetActive(false);interaction.CloseBuilding();}root.CameraRig.EnterDistrict(captured);});
                var caption=Button("zone."+zone).GetComponentInChildren<Text>();if(caption!=null)caption.text=T("zone."+zone);
            }
            var filter=Element<Dropdown>("filter");filter.ClearOptions();filter.AddOptions(new[]{T("ui.all_roles")}.Concat(root.Definitions.roles.Select(x=>T(x.nameKey))).ToList());filter.onValueChanged.AddListener(_=>BuildWorkerRows());
            if(interaction!=null)interaction.Initialize(root);
            if(worldNavigation!=null)worldNavigation.Initialize(root);
            if(cityStatus!=null)cityStatus.Initialize(root);
            if(rewards!=null)rewards.Initialize(root);
            if(collection!=null)collection.Initialize(root);
            if(rewardPreview!=null)rewardPreview.Initialize(root);
            if(map.ContainsKey("collectionJournal"))Click("collectionJournal",ShowCollection);
            if(map.ContainsKey("collectionTeam"))Click("collectionTeam",ShowCollection);
            Element<Slider>("volume").onValueChanged.AddListener(value=>{if(root.Session!=null)root.Session.State.volume=value;});
            Element<Slider>("scale").onValueChanged.AddListener(value=>{if(root.Session!=null)root.Session.State.uiScale=value;});
        }
        public void ShowStart(){rewards?.DismissAll();ShowPanel("start");Button("ui.continue").interactable=root.HasSave;}
        public void ShowIntro(){ShowPanel("intro");PaintIntro();}
        private void PaintIntro(){Label("introText").text=T("intro."+Mathf.Clamp(root.Session.State.introLine,0,2));}
        public void ShowPanel(string id)
        {
            if(interaction!=null){interaction.HideForModal();if(id=="start"||id=="intro")interaction.CloseBuilding();}
            foreach(var p in panels)p.SetActive(p==map[id]);
            map["modalShade"].SetActive(true);map["ui.close"].SetActive(id!="start"&&id!="intro"&&id!="premium");
            var closeRect=map["ui.close"].GetComponent<RectTransform>();var panelRect=map[id].GetComponent<RectTransform>();
            closeRect.SetAsLastSibling();
            closeRect.anchorMin=closeRect.anchorMax=new Vector2(.5f,.5f);
            FitModalPanels();rewards?.RefreshLayout();
            if(id=="settings"&&root.Session!=null){Element<Slider>("volume").SetValueWithoutNotify(root.Session.State.volume);Element<Slider>("scale").SetValueWithoutNotify(root.Session.State.uiScale);}
        }
        public void Close()
        {
            if(rewards!=null && rewards.IsOpen){rewards.Advance();return;}
            if(map["settings"].activeSelf)root.SaveSilently();
            if(premiumToken!=null){root.Session?.Upgrades.Cancel(premiumToken);premiumToken=null;}
            foreach(var p in panels)p.SetActive(false);map["modalShade"].SetActive(false);map["ui.close"].SetActive(false);
            root.Pause(false);if(root.Session!=null&&!root.Session.State.introSeen)ShowIntro();
        }
        public void Notify(string text){Label("message").text=text;messageUntil=Time.unscaledTime+4;Label("message").transform.SetAsLastSibling();}
        private void Update(){if(initialized&&Time.unscaledTime>messageUntil)Label("message").text="";}
        public void ShowOffline(OfflineSummary summary,bool recovered)
        {
            Label("offlineText").text=(recovered?T("ui.recovered")+"\n\n":"")+string.Format(T("ui.offline"),summary.Seconds,summary.Earned);ShowPanel("offline");root.Pause(true);
        }
        public void Refresh()
        {
            if(root.Session==null)return;
            var session=root.Session;var s=session.State;var b=s.Find(root.SelectedId);var d=root.Definitions.Building(b.family);
            Label("hud").text=string.Format(T("ui.hud"),s.money,s.grinders,s.cards,session.Simulation.Residents(s.activeCity));
            var q=session.Progression.Current;Label("questTitle").text=q==null?T("ui.freeplay"):T(q.titleKey);
            Label("questBody").text=q==null?T("ui.freeplay"):T("speaker."+q.speaker.ToLowerInvariant())+"\n"+T(q.bodyKey);
            Button("ui.claim").interactable=session.Progression.Ready;
            if(q==null&&session.CityProject.Available)
            {
                Label("questTitle").text=CityProjectText.Title(session);Label("questBody").text=CityProjectText.Body(session);
                Button("ui.claim").interactable=session.CityProject.Ready;
            }
            if(map["journal"].activeSelf)
            {
                if(journalProjectKey!=ProjectKey())ShowJournal();
                foreach(var refresh in journalRefresh)refresh();
            }
            Label("detailTitle").text=T(d.nameKey);
            Label("detailBody").text=string.Format(T("ui.detail"),b.level,"",b.Total,session.Simulation.Capacity(b),session.Simulation.Rate(b),session.Upgrades.Cost(b),BuildingInspectionMetrics.FormatDuration(b.buildingSeconds>0?b.buildingSeconds:session.Upgrades.Duration(b)),session.Workers.Slots(b),b.raw,b.refined,b.butter,b.pastry);
            Label("flow").text=T(session.Workers.Automated(b)?"ui.automated":"ui.manual")+"\n"+T(b.bottleneck??"bottleneck.none");
            Button("ui.upgrade").interactable=b.buildingSeconds==0&&b.level<5000&&d.kind!=BuildingKind.TownHall&&d.kind!=BuildingKind.Plot;
            Button("ui.finish").interactable=b.buildingSeconds>0;Button("ui.harvest").interactable=d.kind==BuildingKind.Grow&&b.raw>0;
            Button("ui.house").interactable=Button("ui.park").interactable=d.kind==BuildingKind.Plot;
            Button("recovery").interactable=!s.City.freeplay&&s.money<200&&s.simulationSeconds>=s.nextRecoveryWork;
            Button("ui.spin").interactable=s.City.unlocked>=Zone.City&&s.cards>0;
            if(map.TryGetValue("teamSpinHint",out var spinHint))spinHint.GetComponent<Text>().text=T(s.City.unlocked<Zone.City?"ui.spin_locked":s.cards<1?"ui.spin_empty":"ui.spin_ready");
            if(map["townhall"].activeSelf)PaintStats();
            scaler.referenceResolution=new Vector2(1920,1080)/s.uiScale;
            var area=Screen.safeArea;safeArea.anchorMin=new Vector2(area.x/Screen.width,area.y/Screen.height);safeArea.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
            FitModalPanels();
            if(interaction!=null)interaction.Refresh();
            rewardPreview?.Refresh();collection?.Refresh();rewards?.RefreshLayout();
        }
        private void FitModalPanels()
        {
            foreach(var panel in panels)
            {
                if(!panel.activeSelf)continue;
                var rect=panel.GetComponent<RectTransform>();
                float fit=Mathf.Min(1,Mathf.Max(0,safeArea.rect.width-32)/rect.sizeDelta.x,Mathf.Max(0,safeArea.rect.height-32)/rect.sizeDelta.y);
                rect.localScale=Vector3.one*fit;
                rect.anchoredPosition=new Vector2(0,rect.sizeDelta.y*(rect.pivot.y-.5f)*fit);
                var close=map["ui.close"].GetComponent<RectTransform>();
                close.localScale=Vector3.one*fit;
                close.anchoredPosition=new Vector2((rect.sizeDelta.x*.5f-49)*fit,(rect.sizeDelta.y*.5f-49)*fit);
            }
        }
        private void Upgrade()
        {
            var result=root.Command(()=>root.Session.Upgrades.BuyWithMoney(root.SelectedId));if(result==Failure.GrinderConfirmation)Premium(false);
        }
        private void Premium(bool finish)
        {
            var result=root.Session.Upgrades.Offer(root.SelectedId,finish,out var offer);
            if(result!=Failure.None){Notify(T("failure."+result));return;}
            premiumToken=offer.Token;Label("premiumText").text=string.Format(T("ui.premium"),offer.Grinders,T(root.Definitions.Building(root.Session.State.Find(offer.BuildingId).family).nameKey));ShowPanel("premium");
        }
        private static string Speaker(string id)=>id=="farm_grow_01"||id=="farm_grow_02"?"mila":id=="farm_processing"?"jaro":id=="farm_warehouse"?"nia":"rocko";
        public void SpeakToSelected()
        {
            string speaker=Speaker(root.SelectedId);root.Command(()=>root.Session.Talk(speaker));
            if(!map.ContainsKey("conversation"))return;
            string portrait=root.Presentation?.characters.FirstOrDefault(x=>x.workplaceId==root.SelectedId)?.speakerId??speaker;
            Element<Image>("conversationPortrait").sprite=root.Presentation.art.Get("portrait_"+portrait);
            Label("conversationTitle").text=T("speaker."+portrait);
            var quest=root.Session.Progression.Current;
            Label("conversationText").text=T(quest!=null&&quest.speaker.Equals(portrait,StringComparison.OrdinalIgnoreCase)?quest.bodyKey:"dialog."+portrait);ShowPanel("conversation");
        }
        private string BuildingName(string id)
        {var b=root.Session.State.Find(id);string name=T(root.Definitions.Building(b.family).nameKey);return id.EndsWith("_02")?name+" 2":id.EndsWith("_03")?name+" 3":id.EndsWith("_04")?name+" 4":name;}
        private void ClearRows(){foreach(var row in rows){row.SetActive(false);Destroy(row);}rows.Clear();journalRefresh.Clear();}
        private Button Row(Transform parent,string label,Action action)
        {
            var row=Instantiate(listRowPrefab,parent,false);rows.Add(row.gameObject);row.GetComponentInChildren<Text>().text=label;row.onClick.AddListener(()=>action());return row;
        }
        public void ShowCollection(){if(collection==null)return;ShowPanel("collection");collection.SelectTab(0);}
        public void ShowWorker(string id){workerId=id;Element<Dropdown>("filter").SetValueWithoutNotify(0);ShowWorkers();}
        public void ShowWorkers()
        {
            ShowPanel("workers");var b=root.Session.State.Find(root.SelectedId);var slots=Element<Dropdown>("slot");slots.ClearOptions();
            if(workerId==null)workerId=root.Session.State.workers.FirstOrDefault(x=>x.role==root.Definitions.Building(b.family).managerRole)?.id??root.Session.State.workers.FirstOrDefault()?.id;
            var values=new List<string>{T("ui.manager_slot")};for(int i=1;i<=root.Session.Workers.Slots(b);i++)values.Add(T("ui.driver_slot")+" "+i);
            slots.AddOptions(values);Label("assignmentTarget").text=BuildingName(b.id);
            BuildWorkerRows();PaintWorker();
        }
        private void BuildWorkerRows()
        {
            if(root.Session==null)return;ClearRows();int filter=Element<Dropdown>("filter").value;
            var candidates=root.Session.State.workers.Where(x=>filter==0||x.role==root.Definitions.roles[filter-1].id).ToArray();
            foreach(var worker in candidates){string id=worker.id;var row=Row(map["workerList"].transform,T(root.Definitions.Role(worker.role).nameKey)+" · "+T("rarity."+worker.rarity)+"\n"+(string.IsNullOrEmpty(worker.buildingId)?T("ui.unassigned"):T(root.Definitions.Building(root.Session.State.Find(worker.buildingId).family).nameKey)),()=>{workerId=id;PaintWorker();});if(root.Presentation!=null)root.Presentation.DecorateRow(row,"portrait_"+PresentationArt.CharacterForRole(worker.role));}
            if(candidates.Length==0)Label("workerText").text=T("ui.empty_workers");
        }
        private void PaintWorker()
        {
            var w=root.Session.State.workers.FirstOrDefault(x=>x.id==workerId);
            bool found=w!=null;
            if(root.Presentation!=null)root.Presentation.PaintWorker(w);
            foreach(string id in new[]{"ui.assign","ui.remove","ui.worker_level","ui.worker_rank"})Button(id).interactable=found;
            if(!found){Label("workerText").text=T("ui.select_worker");return;}
            Label("workerText").text=string.Format(T("ui.worker_detail"),T(root.Definitions.Role(w.role).nameKey),T("rarity."+w.rarity),w.level,w.rank,string.Join(", ",w.attributes.Select(x=>T("attribute."+(int)x))),string.IsNullOrEmpty(w.buildingId)?T("ui.unassigned"):BuildingName(w.buildingId)+" / "+T(w.slot==0?"ui.manager_slot":"ui.driver_slot"));
        }
        public void ShowTownHall()
        {
            if(root.Command(()=>root.Session.OpenTownHall())!=Failure.None)return;
            ShowPanel("townhall");PaintStats();Label("scores").text=root.Scores()+"\n\n"+T("online.disabled");
        }
        private void PaintStats()
        {
            var s=root.Session.State;var st=s.City.stats;
            Label("stats").text=T("speaker.rocko")+"\n\n"+string.Format(T("ui.stats"),st.earned,st.rawProduced,st.refinedProduced,st.coffeeSold,st.pastrySold,root.Session.Simulation.Residents(s.activeCity),st.deliveries,s.City.completedQuests.Count,st.vipCompleted,st.workersReceived,s.workers.Count(x=>!string.IsNullOrEmpty(x.buildingId)),s.buildings.Max(x=>x.level),st.playSeconds,T(s.City.freeplay?"ui.yes":"ui.no"),s.money,s.grinders);
        }
        public void ShowJournal()
        {
            ShowPanel("journal");ClearRows();var session=root.Session;
            journalProjectKey=ProjectKey();
            if(session.CityProject.Available)BuildProjectRows();
            foreach(var q in root.Definitions.quests){var captured=q;var row=Row(map["journalList"].transform,T(q.titleKey),()=>{Notify(T(captured.bodyKey));root.Focus(captured.focus);});if(root.Presentation!=null)root.Presentation.DecorateRow(row,session.State.City.completedQuests.Contains(q.id)?"confirm":"quest");}
            foreach(var vip in session.State.vip.Where(x=>!x.completed).ToArray())
            {string id=vip.id;var row=Row(map["journalList"].transform,string.Format(T("ui.vip"),T(vip.kind==ConditionKind.Delivered?"ui.deliveries":"ui.production"),vip.target,vip.reward),()=>{root.Command(()=>session.Progression.ClaimVip(id));ShowJournal();});if(root.Presentation!=null)root.Presentation.DecorateRow(row,"gift");}
        }
        private string ProjectKey()=>root.Session.State.activeCity+":"+root.Session.CityProject.Available+":"+root.Session.State.City.project?.planId+":"+root.Session.State.City.project?.stepIndex;
        private Button ProjectRow(string text,Action action=null)
        {
            var row=Row(map["journalList"].transform,text,action??(()=>{}));
            if(root.Presentation!=null)root.Presentation.DecorateRow(row,"quest");
            row.interactable=action!=null;return row;
        }
        private void BuildProjectRows()
        {
            var session=root.Session;var project=session.CityProject;
            ProjectRow(T(project.Definition.titleKey));
            if(project.Plan==null)
            {
                foreach(var plan in project.Definition.plans)
                {
                    string id=plan.id;
                    ProjectRow(T(plan.titleKey)+"\n"+T(plan.bodyKey));
                    ProjectRow(T(plan.rewardKey));
                    ProjectRow(string.Format(T("project.choose"),T(plan.titleKey)),()=>{root.Command(()=>project.ChoosePlan(id));ShowJournal();});
                }
                return;
            }
            ProjectRow(T(project.Plan.titleKey)+"\n"+T(project.Plan.rewardKey));
            if(project.Completed){ProjectRow(T("project.city_title_value")+" · "+T("project.completed"));return;}
            ProjectRow(T("project.counter_hint"));
            for(int i=0;i<project.Current.goals.Length;i++)
            {
                int index=i;var goal=project.Current.goals[i];
                var target=session.State.buildings.FirstOrDefault(b=>b.cityId==session.State.activeCity&&(b.id==goal.target||b.family==goal.target));
                string focus=target?.id??project.Current.focus;
                var row=ProjectRow(CityProjectText.Goal(session,index),()=>{Close();root.Focus(focus);});
                var label=row.GetComponentInChildren<Text>();journalRefresh.Add(()=>label.text=CityProjectText.Goal(session,index));
            }
            ProjectRow(CityProjectText.Reward(root.Definitions,project.Current));
            for(int i=0;i<project.Plan.steps.Length;i++)
            {
                var step=project.Plan.steps[i];
                string status=T(i<session.State.City.project.stepIndex?"project.completed":i==session.State.City.project.stepIndex?"project.active":"project.later");
                ProjectRow((i+1)+". "+T(step.titleKey)+" · "+status);
            }
        }
#if UNITY_EDITOR
        public void EditorConfigure(UiBinding[] b,GameObject[] p,Button row,RectTransform safe,CanvasScaler scale){bindings=b;panels=p;listRowPrefab=row;safeArea=safe;scaler=scale;}
#endif
    }
}

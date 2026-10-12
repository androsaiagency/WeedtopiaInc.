from pathlib import Path
root=Path(__file__).resolve().parents[2]
def edit(path,old,new):
 p=root/path;s=p.read_text(encoding='utf-8-sig');assert old in s,(path,old[:60]);p.write_text(s.replace(old,new,1),encoding='utf-8')
base='Assets/Weedtopia/Scripts/Runtime/'
edit(base+'Persistence/LocalSaveStore.cs','Require(s.vip.Select(x=>x.id)', 'RewardSaveValidator.Validate(s, d);\n            Require(s.vip.Select(x=>x.id)')
edit(base+'Unity/GameUi.cs','private float messageUntil;', '[SerializeField] private RewardPresentation rewards;\n        [SerializeField] private RewardCollectionUi collection;\n        [SerializeField] private RewardPreviewView rewardPreview;\n        public RewardPresentation Rewards => rewards;\n        public RewardCollectionUi Collection => collection;\n        private float messageUntil;')
edit(base+'Unity/GameUi.cs','public bool LegacyModalOpen=>panels.Any(x=>x.activeSelf);','public bool LegacyModalOpen=>(rewards != null && rewards.IsOpen) || panels.Any(x=>x.activeSelf);')
edit(base+'Unity/GameUi.cs','if(cityStatus!=null)cityStatus.Initialize(root);', 'if(cityStatus!=null)cityStatus.Initialize(root);\n            if(rewards!=null)rewards.Initialize(root);\n            if(collection!=null)collection.Initialize(root);\n            if(rewardPreview!=null)rewardPreview.Initialize(root);\n            if(map.ContainsKey("collectionJournal"))Click("collectionJournal",ShowCollection);\n            if(map.ContainsKey("collectionTeam"))Click("collectionTeam",ShowCollection);')
edit(base+'Unity/GameUi.cs','public void ShowStart(){ShowPanel', 'public void ShowStart(){rewards?.DismissAll();ShowPanel')
edit(base+'Unity/GameUi.cs','FitModalPanels();\n            if(id=="settings"', 'FitModalPanels();rewards?.RefreshLayout();\n            if(id=="settings"')
edit(base+'Unity/GameUi.cs','public void Close()\n        {','public void Close()\n        {\n            if(rewards!=null && rewards.IsOpen){rewards.Advance();return;}')
edit(base+'Unity/GameUi.cs','if(interaction!=null)interaction.Refresh();', 'if(interaction!=null)interaction.Refresh();\n            rewardPreview?.Refresh();collection?.Refresh();rewards?.RefreshLayout();')
edit(base+'Unity/GameUi.cs','public void ShowWorkers()','public void ShowCollection(){if(collection==null)return;ShowPanel("collection");collection.SelectTab(0);}\n        public void ShowWorker(string id){workerId=id;Element<Dropdown>("filter").SetValueWithoutNotify(0);ShowWorkers();}\n        public void ShowWorkers()')
edit(base+'Unity/GameRoot.cs','try {Session=new GameSession','try {ui.Rewards?.DismissAll();Session=new GameSession')
edit(base+'Unity/GameRoot.cs','Session=new GameSession(Definitions,loaded.State,clock,store);','ui.Rewards?.DismissAll();Session=new GameSession(Definitions,loaded.State,clock,store);')
edit(base+'Unity/GameRoot.cs','try{var result=Session.Transact(command);', 'long rewardBefore=Session.State.rewardSequence;\n            try{var result=Session.Transact(command);')
edit(base+'Unity/GameRoot.cs','Refresh();return result;}','Refresh();if(result==Failure.None)ui.Rewards?.EnqueueAfter(rewardBefore);return result;}')
print('Reward UI integrated.')

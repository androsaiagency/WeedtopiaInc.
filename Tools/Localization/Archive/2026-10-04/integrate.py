from pathlib import Path
import re,json
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'Docs/Localization'
entries=json.loads((OUT/'source_inventory.json').read_text(encoding='utf8'))
def edit(relative,old,new):
 p=ROOT/relative;s=p.read_text(encoding='utf-8-sig')
 if old not in s:raise RuntimeError('Missing edit in '+relative+': '+old[:100])
 p.write_text(s.replace(old,new),encoding='utf8')
def update(relative,change):
 p=ROOT/relative;s=p.read_text(encoding='utf-8-sig');p.write_text(change(s),encoding='utf8')
base='Assets/Weedtopia/Scripts/Runtime/'
p=base+'Unity/GameRoot.cs'
edit(p,'public GameDefinitions Definitions {get;private set;}','public GameDefinitions Definitions {get;private set;}\n        public LocalizationService Localization {get;private set;}\n        public const string LanguagePreferenceKey="Weedtopia.Language";')
edit(p,'Definitions=content.Load();clock=new SystemClock();','Definitions=content.Load();\n            if(content.Languages==null)throw new InvalidOperationException("Missing authored language catalog. Run Weedtopia/Localization/Install or Repair.");\n            Localization=content.Languages.Create();\n            Definitions.TextLookup=Localization;\n            Localization.Select(PlayerPrefs.GetString(LanguagePreferenceKey,content.Languages.fallback));\n            GameText.Use(Localization);\n            Localization.Changed+=RefreshLanguage;\n            clock=new SystemClock();')
edit(p,'private void OnApplicationPause(bool paused)', '''public bool SetLanguage(string code)
        {
            if(!Localization.Select(code))return false;
            PlayerPrefs.SetString(LanguagePreferenceKey,code);PlayerPrefs.Save();return true;
        }
        private void RefreshLanguage(){ui.RefreshLanguage();}
        private void OnDestroy()
        {
            if(Localization==null)return;
            Localization.Changed-=RefreshLanguage;GameText.Release(Localization);
        }
        private void OnApplicationPause(bool paused)''')
p=base+'Unity/UiExperience.cs'
edit(p,'public ScrollRect gameScroll, profileScroll;','public ScrollRect gameScroll, profileScroll;\n        public Dropdown language;\n        public Text languageLabel;')
edit(p,'root=value;quests.Initialize(root);','root=value;quests.Initialize(root);\n            RefreshLanguage();\n            language.onValueChanged.AddListener(index=>root.SetLanguage(root.Localization.Languages[index].Code));')
edit(p,'private void ChangeAvatar(int delta)','''public void RefreshLanguage()
        {
            language.ClearOptions();language.AddOptions(root.Localization.Languages.Select(l=>l.NativeName).ToList());
            language.SetValueWithoutNotify(root.Localization.Languages.ToList().FindIndex(l=>l.Code==root.Localization.Language));
            languageLabel.text=root.Definitions.Text("settings.language");
            gameTab.GetComponentInChildren<Text>().text=root.Definitions.Text("settings.game");
            profileTab.GetComponentInChildren<Text>().text=root.Definitions.Text("settings.profile");
            if(root.Session!=null&&intro.gameObject.activeSelf)PaintIntro();
            if(root.Session!=null)PaintAvatar();
            Fit();
        }
        private void ChangeAvatar(int delta)''')
edit(p,'float y=font;','''float y=font;
            Place(languageLabel.rectTransform,0,-y-font*.7f,w-20,font*1.4f);languageLabel.fontSize=Mathf.RoundToInt(font);y+=font*1.85f;
            Place((RectTransform)language.transform,0,-y-button*.5f,w-20,button);y+=button+font*.45f;
            foreach(var text in language.GetComponentsInChildren<Text>(true))text.fontSize=Mathf.RoundToInt(font);''')
update(p,lambda s:re.sub(r'avatarCaption.text="Avatar "\+\(avatarIndex\+1\)\+" / "\+PlayerProfile.Avatars.Length;', 'avatarCaption.text=GameText.Format("profile.avatar",avatarIndex+1,PlayerProfile.Avatars.Length);',s))
# Actual source has already had the Avatar prefix migrated.
update(p,lambda s:re.sub(r'avatarCaption.text=Weedtopia.Domain.GameText.Get\([^;]+;', 'avatarCaption.text=GameText.Format("profile.avatar",avatarIndex+1,PlayerProfile.Avatars.Length);',s))
p=base+'Unity/GameUi.cs'
edit(p,'public void ShowStart(){','''public void RefreshLanguage()
        {
            foreach(var pair in map)
                if(pair.Key.StartsWith("ui.")||pair.Key.StartsWith("zone."))
                {var text=pair.Value.GetComponentInChildren<Text>(true);if(text!=null)text.text=T(pair.Key);}
            Label("title").text=T("game.title");Label("development").text=T("ui.dev");
            var filter=Element<Dropdown>("filter");int selectedFilter=filter.value;
            filter.ClearOptions();filter.AddOptions(new[]{T("ui.all_roles")}.Concat(root.Definitions.roles.Select(x=>T(x.nameKey))).ToList());filter.SetValueWithoutNotify(selectedFilter);
            experience?.RefreshLanguage();
            stories?.RefreshLanguage();campaign?.RefreshLanguage();luckyWheel?.RefreshLanguage();collection?.Paint();
            if(root.Session==null)return;
            if(map["journal"].activeSelf)ShowJournal();
            if(map["workers"].activeSelf)ShowWorkers();
            Refresh();
        }
        public void ShowStart(){''')
# Explicit invalidation for state-cached displays.
p=base+'Unity/VipStoriesUi.cs'
edit(p,'public void Refresh() {','public void RefreshLanguage(){lastRevision=-1;lastSecond=-1;Refresh();}\n        public void Refresh() {')
p=base+'Unity/LuckyWheelUi.cs'
edit(p,'public void Refresh()','public void RefreshLanguage(){paintedDay=null;paintedSpins=-1;Refresh();}\n        public void Refresh()')
p=base+'Unity/CityCampaignUi.cs'
edit(p,'private string T(string key)', '''public void RefreshLanguage()
        {
            title.text=T("campaign.title");invitationTitle.text=T("campaign.takeover_title");
            invitation.text=T("campaign.takeover_body");shared.text=T("campaign.shared");
            accept.GetComponentInChildren<Text>().text=T("campaign.accept");Refresh();
        }
        private string T(string key)''')
# Whole grammatical templates for tutorial/progression objectives.
p=base+'Application/QuestGuidance.cs'
def guidance(s):
 s=re.sub(r'case ConditionKind.Talk: text=.*?;break;', 'case ConditionKind.Talk: text=GameText.Format("guidance.talk",defs.Text("speaker."+c.target).Split(\'·\')[0].Trim());break;',s)
 s=re.sub(r'case ConditionKind.Level: text=.*?;break;', 'case ConditionKind.Level: text=GameText.Format("guidance.level",target,c.count);break;',s)
 s=re.sub(r'case ConditionKind.Assigned: text=.*?;break;', 'case ConditionKind.Assigned: text=GameText.Format("guidance.assigned",target,c.count);break;',s)
 s=re.sub(r'case ConditionKind.Automated: text=.*?;break;', 'case ConditionKind.Automated: text=GameText.Format("guidance.automated",target);break;',s)
 s=re.sub(r'case ProjectGoalKind.Level:text=.*?;break;', 'case ProjectGoalKind.Level:text=GameText.Format("guidance.level",target,g.count);break;',s)
 s=re.sub(r'case ProjectGoalKind.Automated:text=.*?;break;', 'case ProjectGoalKind.Automated:text=GameText.Format("guidance.automated",target);break;',s)
 s=re.sub(r'case ProjectGoalKind.Residents:text=.*?;break;', 'case ProjectGoalKind.Residents:text=GameText.Format("guidance.residents",g.count);break;',s)
 s=re.sub(r'note=Weedtopia.Domain.GameText.Get\("presentation.questguidance.belohnung[^\n]+?,steps=', 'note=GameText.Format("guidance.reward",vip.reward),steps=',s)
 return s
update(p,guidance)
# Fix overlooked captions. No runtime-created static controls.
additional={' Geld · ':'format.money_separator','Fuhrpark · ':'inspection.fleet_prefix',' Fahrer':'inspection.drivers_suffix','Fuhrpark\n':'inspection.fleet_title','Ansprechen':'inspection.talk','Rang':'inspection.rank','Stufe':'inspection.level','Avatar auswählen':'profile.choose_avatar'}
for value,key in additional.items():entries[key]=value
for relative in [base+'Unity/BuildingInteractionUi.cs',base+'Unity/UiExperience.cs']:
 update(relative,lambda s:__import__('functools').reduce(lambda text,pair:text.replace(json.dumps(pair[0],ensure_ascii=False),'GameText.Get('+json.dumps(pair[1])+')'),additional.items(),s))
# Format cultures are passed at presentation boundaries; save data retains invariant formatting.
for p in (ROOT/base).rglob('*.cs'):
 if p.parts[-2] not in {'Unity','Application'}:continue
 s=p.read_text(encoding='utf-8-sig')
 if 'string.Format(' not in s:continue
 if p.name in {'ShopPurchaseGateway.cs'}:continue
 if 'GameText' in s or '.Text(' in s:
  s=s.replace('string.Format(', 'string.Format(Weedtopia.Domain.GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,')
  p.write_text(s,encoding='utf8')
(OUT/'source_inventory.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('Language selection and presentation refresh integrated')

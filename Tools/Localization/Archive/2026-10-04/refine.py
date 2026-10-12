from pathlib import Path
import re,json
R=Path(__file__).resolve().parents[2]; B=R/'Assets/Weedtopia/Scripts/Runtime'
p=B/'Unity/LuckyWheelUi.cs';s=p.read_text();p.write_text(s.replace('paintedDay=null','paintedDay=-1'),encoding='utf8')
p=B/'Domain/Definitions.cs';s=p.read_text();needle='public void Validate()';s=s.replace(needle,'public string Format(string key,params object[] arguments) => string.Format((TextLookup as LocalizationService)?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,Text(key),arguments);\n        '+needle);p.write_text(s,encoding='utf8')
p=B/'Application/QuestGuidance.cs';s=p.read_text();at=s.index('private static GuidanceStep ProjectStep');head,tail=s[:at],s[at:];head=head.replace('Weedtopia.Domain.GameText.Get(','defs.Text(').replace('GameText.Format(','defs.Format(');tail=tail.replace('Weedtopia.Domain.GameText.Get(','session.Definitions.Text(').replace('GameText.Format(','session.Definitions.Format(');p.write_text(head+tail,encoding='utf8')
# Add complete templates for inspector actions and fleet details.
p=B/'Unity/BuildingInteractionUi.cs';s=p.read_text()
s=re.sub(r'heading.text = BuildingName\(b.id\).*?;', 'heading.text = BuildingName(b.id) + "\\n" + (b.level == 0 ? T("inspection.build_site") : GameText.Format("format.level",b.level));',s)
s=re.sub(r'T\("rarity\." \+ manager.rarity\) \+ Weedtopia.Domain.GameText.Get\("presentation.buildinginteractionui.level.645d01b9"\).*?Traits\(manager\)', 'GameText.Format("format.worker",T("rarity."+manager.rarity),manager.level,manager.rank,Traits(manager))',s)
s=re.sub(r'fleetLabel.text = .*?;', 'fleetLabel.text = GameText.Format("inspection.fleet",root.Session.State.workers.Count(w=>w.buildingId==b.id&&w.slot>0),root.Session.Workers.Slots(b));',s)
s=s.replace('fleetMode + ":" + selectedSlot','root.Localization.Language + ":" + fleetMode + ":" + selectedSlot')
p.write_text(s,encoding='utf8')
p=B/'Unity/QuestTracker.cs';s=p.read_text();s=re.sub(r'dockHeading.text=.*?;', 'dockHeading.text=Weedtopia.Domain.GameText.Format("quest.active_count",active.Length);',s);p.write_text(s,encoding='utf8')
p=B/'Unity/ShopUi.cs';s=p.read_text();s=re.sub(r'balance.text = .*?;', 'balance.text = GameText.Format("shop.balance",root.Session.State.money,root.Session.State.grinders.ToString("N0",root.Localization.Culture));',s)
s=re.sub(r'Weedtopia.Domain.GameText.Get\("presentation.shopui.vip_aktiv_bis.[^"]+"\) \+ new DateTime\(vip.expiresUtcTicks, DateTimeKind.Utc\).ToLocalTime\(\).ToString\("dd.MM.yyyy HH:mm"\) \+ Weedtopia.Domain.GameText.Get\("presentation.shopui.boni[^\n]+?\)', 'GameText.Format("shop.vip_until",new DateTime(vip.expiresUtcTicks,DateTimeKind.Utc).ToLocalTime().ToString("g",root.Localization.Culture))',s)
s=s.replace('definition.value / 60 + Weedtopia.Domain.GameText.Get("presentation.shopui.min.','definition.value / 60 + Weedtopia.Domain.GameText.Get("presentation.shopui.min.')
p.write_text(s,encoding='utf8')
# UI literals still missed by the initial extraction: player-facing lines only, excluding object paths and IDs.
OUT=R/'Docs/Localization';entries=json.loads((OUT/'source_inventory.json').read_text());by_source={v:k for k,v in entries.items()}
tokens=re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"')
for name in ['BuildingInteractionUi','UiExperience','ShopUi','BuildingInspectionMetrics','QuestTracker','DistrictNavigator','ProductionPresentation']:
 p=B/'Unity'/(name+'.cs');s=p.read_text();changes=[]
 for m in tokens.finditer(s):
  if not m.group().startswith('"'):continue
  try:v=json.loads(m.group())
  except ValueError:continue
  line=s[s.rfind('\n',0,m.start())+1:s.find('\n',m.end())]
  if any(x in line for x in ['throw new ','Debug.Log','Tooltip(','GetComponents','StartsWith(','EndsWith(','transform.Find','Format(','Get(']):
   # GameText expressions can share the line with untranslated text; only exclude literals used as Get arguments.
   if re.search(r'(?:Text|Get|Format|Find)\($',s[max(0,m.start()-35):m.start()]) or any(x in line for x in ['throw new ','Debug.Log','Tooltip(']):continue
  if not re.search(r'[A-Za-zÄÖÜäöüß]{2}',v):continue
  if re.fullmatch(r'[a-zA-Z0-9_./:-]+',v) and v not in {'Ansprechen','Erhalten','Avatar','Level','Rang','Verfügbar','Automatisch'}:continue
  if v in {'N0','0.#','0.##','Burning Hills','WEEDTOPIA','X','mm:ss','[',']','Ready','Level'}:continue
  if not re.search(r'[ÄÖÜäöüß]|\s|[.!?]',v) and v not in {'Ansprechen','Erhalten','Avatar','Rang'}:continue
  key=by_source.get(v,'presentation.'+name.lower()+'.'+__import__('hashlib').sha256(v.encode()).hexdigest()[:12]);entries[key]=v;by_source[v]=key;changes.append((m.start(),m.end(),'Weedtopia.Domain.GameText.Get('+json.dumps(key)+')'))
 for a,b,value in reversed(changes):s=s[:a]+value+s[b:]
 p.write_text(s,encoding='utf8')
entries.update({'inspection.build_site':'Bauplatz','inspection.fleet':'Fuhrpark · {0} / {1} Fahrer'})
(OUT/'source_inventory.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('Formatting and missed labels repaired; keys',len(entries))

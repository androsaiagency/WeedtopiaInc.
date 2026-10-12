from pathlib import Path
import json,re,hashlib,datetime
R=Path(__file__).resolve().parents[2];O=R/'Docs/Localization';B=R/'.utmp/LocalizationBaseline'
folder=R/'Assets/Weedtopia/Game/Localization'
catalogs={p.stem:{x['key']:x['value'] for x in json.loads(p.read_text(encoding='utf8'))['entries']} for p in folder.glob('*.json')}
source=catalogs['de'];missing=[]
for p in (R/'Assets/Weedtopia/Scripts/Runtime').rglob('*.cs'):
 text=p.read_text(encoding='utf-8-sig')
 for key in re.findall(r'(?:GameText\.(?:Get|Format)|(?:Definitions|defs)\.(?:Text|Format)|\bT)\("([^"]+)"\s*[,)]',text):
  if key not in source:missing.append({'file':p.relative_to(R).as_posix(),'key':key})
content=json.loads((R/'Assets/Weedtopia/Game/Content/BurningHills.json').read_text(encoding='utf-8-sig'))
def walk(value):
 if isinstance(value,dict):
  for key,item in value.items():
   if key.endswith('Key') and isinstance(item,str) and item and item not in source:missing.append({'contentField':key,'key':item})
   walk(item)
 elif isinstance(value,list):
  for item in value:walk(item)
walk({k:v for k,v in content.items() if k!='texts'})
# All original game rules, transactions and authored content remain byte-identical except display adapters.
protected=[];changes=[]
excluded={'Definitions.cs','Amount.cs','QuestGuidance.cs','CityProjectText.cs','ShopPurchaseGateway.cs'}
for scope in ['Domain','Application','Persistence']:
 for original in (B/'Assets/Weedtopia/Scripts/Runtime'/scope).glob('*.cs'):
  if original.name in excluded:continue
  current=R/original.relative_to(B)
  (protected if original.read_bytes()==current.read_bytes() else changes).append(current.relative_to(R).as_posix())
original_content=B/'Assets/Weedtopia/Game/Content/BurningHills.json'
content_unchanged=original_content.read_bytes()==(R/'Assets/Weedtopia/Game/Content/BurningHills.json').read_bytes()
scenes={}
for original in (B/'Assets/Weedtopia/Game/Scenes').glob('*.unity'):
 current=R/original.relative_to(B)
 def ids(file):return set(re.findall(r'^--- !u!\d+ &(-?\d+)',file.read_text(encoding='utf-8-sig'),re.M))
 before,after=ids(original),ids(current)
 scenes[original.name]={'originalObjects':len(before),'removedOriginalObjects':len(before-after),'addedSerializedObjects':len(after-before)}
result={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'status':'passed' if not missing and not changes and content_unchanged and all(not s['removedOriginalObjects'] for s in scenes.values()) else 'failed','languages':{k:len(v) for k,v in catalogs.items()},'missingReferencedKeys':missing,'unchangedProtectedRuleFiles':len(protected),'changedProtectedRuleFiles':changes,'productiveContentByteIdentical':content_unchanged,'scenes':scenes}
(O/'preservation_and_coverage.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(json.dumps(result,ensure_ascii=False))
if result['status']!='passed':raise SystemExit(1)

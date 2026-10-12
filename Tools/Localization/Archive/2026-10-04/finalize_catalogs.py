from pathlib import Path
import json,re,argparse,datetime,hashlib,struct
R=Path(__file__).resolve().parents[2]; O=R/'Docs/Localization'; F=R/'Assets/Weedtopia/Game/Localization'
parser=argparse.ArgumentParser();parser.add_argument('--partial',action='store_true');args=parser.parse_args()
source=json.loads((O/'source_inventory.json').read_text(encoding='utf8'))
cache=json.loads((O/'local_translation_cache.json').read_text(encoding='utf8')) if (O/'local_translation_cache.json').exists() else {}
manual={}
for line in (O/'manual_overrides.txt').read_text(encoding='utf-8-sig').splitlines():
 if not line.strip():continue
 key,*values=line.split('|')
 if key not in source:raise ValueError('Unknown override '+key)
 manual[key]={lang:value.replace('\\n','\n') for lang,value in zip(['en','es','fr','ru'],values)}
for line in (O/"manual_source_overrides.txt").read_text(encoding="utf-8-sig").splitlines():
 if not line.strip():continue
 german,*values=line.split("|");german=german.replace("\\n","\n")
 for key,original in source.items():
  if original==german:manual[key]={lang:value.replace("\\n","\n") for lang,value in zip(["en","es","fr","ru"],values)}
ph=re.compile(r'\{\d+(?:,[^}:]+)?(?::[^}]+)?\}')
name_aliases=json.loads((O/'proper_names.json').read_text(encoding='utf8'))
def preserve_names(german,value):
 for name,aliases in name_aliases.items():
  if not re.search(r'\b'+re.escape(name)+r'\b',german):continue
  for alias in sorted(aliases,key=len,reverse=True):value=re.sub(r'\b'+re.escape(alias)+r'\b',name,value)
 if 'Grinder' in german:value=re.sub(r'Гриндер(?:ами|ах|ов|ом|ы|а|у|е)?', 'Grinder',value,flags=re.I)
 return value
missing=[];report={};locale_values={}
for lang in ['de','en','es','fr','ru']:
 entries=[]
 for key,german in sorted(source.items()):
  value=german if lang=='de' else manual.get(key,{}).get(lang,cache.get(german,{}).get(lang))
  # Proper city/game names and purely numeric/symbolic labels do not require translation.
  if value is None and (german in {'Weedtopia','Weedtopia Inc.','Burning Hills','Big Rock City','Play','WEEDTOPIA','Grinder','VIP'} or not re.search(r'[A-Za-zÄÖÜäöüß]',german)):value=german
  if value is None:missing.append((lang,key));continue
  if lang!='de':value=preserve_names(german,value)
  if sorted(ph.findall(value))!=sorted(ph.findall(german)):raise ValueError('Placeholder mismatch '+lang+' '+key)
  if value.count('\n')!=german.count('\n'):raise ValueError('Line break mismatch '+lang+' '+key)
  entries.append({'key':key,'value':value})
 report[lang]={'keys':len(entries),'missing':len(source)-len(entries)};locale_values[lang]=entries
if missing and not args.partial:raise ValueError('Untranslated keys: '+str(len(missing))+'; sample '+repr(missing[:8]))
F.mkdir(parents=True,exist_ok=True)
for lang,entries in locale_values.items():(F/(lang+'.json')).write_text(json.dumps({'language':lang,'entries':entries},ensure_ascii=False,indent=2)+'\n',encoding='utf8')
# SFNT cmap coverage without external packages.
def chars(font):
 data=font.read_bytes();tables={data[12+i*16:16+i*16].decode():struct.unpack_from('>II',data,20+i*16) for i in range(struct.unpack_from('>H',data,4)[0])}
 offset,size=tables['cmap'];cmap=data[offset:offset+size];covered=set()
 for i in range(struct.unpack_from('>H',cmap,2)[0]):
  platform,encoding,pos=struct.unpack_from('>HHI',cmap,4+i*8);fmt=struct.unpack_from('>H',cmap,pos)[0]
  if fmt==4:
   seg=struct.unpack_from('>H',cmap,pos+6)[0]//2;end=pos+14;start=end+2*seg+2;delta=start+2*seg;ranges=delta+2*seg
   for j in range(seg):
    a=struct.unpack_from('>H',cmap,start+2*j)[0];b=struct.unpack_from('>H',cmap,end+2*j)[0];d=struct.unpack_from('>h',cmap,delta+2*j)[0];ro=struct.unpack_from('>H',cmap,ranges+2*j)[0]
    for c in range(a,b+1):
     if c==65535:continue
     glyph=(c+d)&65535 if ro==0 else struct.unpack_from('>H',cmap,ranges+2*j+ro+2*(c-a))[0]
     if glyph:covered.add(c)
  elif fmt==12:
   for j in range(struct.unpack_from('>I',cmap,pos+12)[0]):
    a,b,_=struct.unpack_from('>III',cmap,pos+16+12*j);covered.update(range(a,b+1))
 return covered
needed={ord(c) for values in locale_values.values() for entry in values for c in entry['value'] if c.isalpha()}
font_report={}
for font in (R/'Assets/Weedtopia/Production/Fonts').glob('*.ttf'):
 absent=sorted(needed-chars(font));font_report[font.name]={'missingLetters':''.join(chr(c) for c in absent),'count':len(absent)}
result={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'status':'partial' if missing else 'passed','sourceKeys':len(source),'languages':report,'fonts':font_report,'translationOrigin':'local Ollama llama3:latest plus manually reviewed UI overrides','externalProjectTextTransfers':False,'nativeSpeakerReviewCompleted':False}
(O/'catalog_validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps(result,ensure_ascii=False))

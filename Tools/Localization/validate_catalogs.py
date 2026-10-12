from pathlib import Path
import json,re,datetime,struct,unicodedata
R=Path(__file__).resolve().parents[2];F=R/'Assets/Weedtopia/Game/Localization'
values={}
for file in F.glob('*.json'):
 document=json.loads(file.read_text(encoding='utf8'));entries=document['entries'];lang=document['language']
 assert lang==file.stem and len({e['key'] for e in entries})==len(entries),'Duplicate or invalid language'
 assert all(e['key'].strip() and e['value'].strip() for e in entries),'Empty key or text'
 values[lang]={e['key']:e['value'] for e in entries}
source=values['de'];ph=re.compile(r'\{\d+(?:,[^}:]+)?(?::[^}]+)?\}')
for lang,catalog in values.items():
 assert catalog.keys()==source.keys(),'Incomplete keys: '+lang
 for key,value in catalog.items():
  assert sorted(ph.findall(value))==sorted(ph.findall(source[key])),'Placeholder mismatch: '+lang+'/'+key
  assert value.count('\n')==source[key].count('\n'),'Line break mismatch: '+lang+'/'+key
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
     if ro and glyph:glyph=(glyph+d)&65535
     if glyph:covered.add(c)
  elif fmt==12:
   for j in range(struct.unpack_from('>I',cmap,pos+12)[0]):
    a,b,glyph=struct.unpack_from('>III',cmap,pos+16+12*j);covered.update(range(a+(glyph==0),b+1))
 return covered
text=''.join(value for catalog in values.values() for value in catalog.values())
text+=unicodedata.normalize('NFD',text)
needed={ord(c) for c in text if c.isprintable() and not c.isspace()}
fonts={}
font_folder=R/'Assets/Weedtopia/Production/Fonts'
if (font_folder/'WeedtopiaGrove').exists():font_folder=font_folder/'WeedtopiaGrove'
font_files=list(font_folder.glob('*.ttf'));assert font_files,'Missing shipped fonts'
for file in font_files:
 missing=sorted(needed-chars(file));fonts[file.name]={'missingCharacters':''.join(chr(c) for c in missing),'missingLetters':''.join(chr(c) for c in missing if chr(c).isalpha()),'count':len(missing),'testedCharacters':len(needed)}
 assert not missing,'Missing glyphs: '+file.name
result={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'status':'passed','sourceKeys':len(source),'languages':{k:{'keys':len(v),'missing':0} for k,v in values.items()},'fonts':fonts,'translationOrigin':'local Ollama llama3:latest plus manually reviewed UI overrides','externalProjectTextTransfers':False,'nativeSpeakerReviewCompleted':False}
(R/'Docs/Localization/catalog_validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(json.dumps(result,ensure_ascii=False))

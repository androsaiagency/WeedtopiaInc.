"""Inventory existing authored text without changing gameplay or serialised references."""
from pathlib import Path
import json,re,hashlib,shutil
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/Localization'
BASE=ROOT/'.utmp/LocalizationBaseline'
for folder in ['Scripts/Runtime','Game/Content','Game/Scenes','Production/Prefabs']:
    for p in (ROOT/'Assets/Weedtopia'/folder).rglob('*'):
        if p.is_file() and p.suffix in {'.cs','.json','.unity','.prefab','.asset'}:
            target=BASE/p.relative_to(ROOT);target.parent.mkdir(parents=True,exist_ok=True)
            if not target.exists():shutil.copy2(p,target)
content=json.loads((ROOT/'Assets/Weedtopia/Game/Content/BurningHills.json').read_text(encoding='utf-8-sig'))
entries={x['key']:x['de'] for x in content['texts']}
by_source={}
for key,text in entries.items():by_source.setdefault(text,key)
# C# tokens: ignore comments and preserve identifiers, formats, paths and diagnostic messages.
tokens=re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"')
single=set('Sofort Bauoptionen Einwohner Pufferkapazität Erntezyklus Fahrerplätze Lieferwagen Bauzeit Bauplatz Leitung Karten Auftrag Aufträge Geld Grinder Stunden Minuten Stufe Rang Level Profil Spiel Einstellungen Weiter Zurück Überspringen Abbrechen Kaufen BAUZEIT PAKET STADTVORRAT'.split())
runtime=[]
files=list((ROOT/'Assets/Weedtopia/Scripts/Runtime/Unity').glob('*.cs'))+[ROOT/'Assets/Weedtopia/Scripts/Runtime/Application/QuestGuidance.cs']
for p in files:
    source=p.read_text(encoding='utf-8-sig')
    for match in tokens.finditer(source):
        literal=match.group()
        if not literal.startswith('"'):continue
        try:value=json.loads(literal)
        except ValueError:continue
        line=source[source.rfind('\n',0,match.start())+1:source.find('\n',match.end())]
        if any(x in line for x in ['throw new ','Debug.Log','InvalidOperationException','ArgumentException']):continue
        if not (re.search(r'[ÄÖÜäöüß]',value) or re.search(r'[A-Za-z]{2,} [A-Za-z]{2,}',value) or value.strip() in single):continue
        if any(x in value for x in ['Weedtopia/','Assets/','UI already initialized','yyyy','dd.MM','[Serializable]']):continue
        key=by_source.get(value)
        if key is None:
            slug=re.sub(r'[^a-z0-9]+','_',value.lower()).strip('_')[:40]
            key='presentation.'+p.stem.lower()+'.'+slug+'.'+hashlib.sha256(value.encode()).hexdigest()[:8]
            entries[key]=value;by_source[value]=key
        runtime.append(dict(file=p.relative_to(ROOT).as_posix(),literal=literal,key=key,source=value))
# Unity's YAML Text components, including wrapped quoted strings. Installation uses this exact source alias.
authored=[]
for folder in ['Game/Scenes','Game/Prefabs','Production/Prefabs']:
    for p in (ROOT/'Assets/Weedtopia'/folder).rglob('*'):
        if p.suffix not in {'.prefab','.unity'} or 'Archive' in p.parts:continue
        source=p.read_text(encoding='utf-8-sig')
        for match in re.finditer(r'^  m_Text: (.*(?:\n(?!(?:---|  \w+:)).*)*)',source,re.M):
            raw=match.group(1).strip()
            if not raw:continue
            try:
                if raw.startswith('"'):value=json.loads(re.sub(r'\r?\n\s*',' ',raw))
                elif raw.startswith("'"):value=raw[1:-1].replace("''","'")
                else:value=re.sub(r'\r?\n\s*',' ',raw)
            except ValueError:continue
            if not re.search(r'[A-Za-zÄÖÜäöüß]{2}',value) or value in {'X','WEEDTOPIA'}:continue
            if value.startswith('ui.') or value.startswith('zone.'):continue
            key=by_source.get(value)
            if key is None:
                slug=re.sub(r'[^a-z0-9]+','_',value.lower()).strip('_')[:40]
                key='authored.'+slug+'.'+hashlib.sha256(value.encode()).hexdigest()[:8]
                entries[key]=value;by_source[value]=key
            authored.append(dict(file=p.relative_to(ROOT).as_posix(),key=key,source=value))
OUT.mkdir(parents=True,exist_ok=True)
for name,data in [('source_inventory',entries),('runtime_migration',runtime),('authored_inventory',authored)]:
    (OUT/(name+'.json')).write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps(dict(contentKeys=len(content['texts']),allKeys=len(entries),runtimeLiterals=len(runtime),runtimeFiles=len({x['file'] for x in runtime}),authoredLabels=len(authored),characters=sum(map(len,entries.values())))))

from pathlib import Path
import json,re,hashlib
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'Docs/Localization'
# Remove technical metadata and profiler counter names mistakenly selected by natural-language heuristics.
rows=json.loads((OUT/'runtime_migration.json').read_text())
rows=[r for r in rows if not r['file'].endswith(('BigRockCityStamp.cs','DistrictMap.cs','PresentationBenchmark.cs'))]
entries=json.loads((OUT/'source_inventory.json').read_text())
for row in rows:
    p=ROOT/row['file'];s=p.read_text(encoding='utf-8-sig');s=s.replace(row['literal'],'Weedtopia.Domain.GameText.Get('+json.dumps(row['key'])+')');p.write_text(s,encoding='utf8')
# Collect remaining presentation-only labels and snippets missed by the first inventory.
extra={'Gemeinschaft / s','Haus / Park','Ertrag / Ernte','Umsatz / s','Verkauf / s','Durchsatz / s','Butter / Backwaren','Karten / Tag','Grinder / Tag','Mit ',' sprechen',' ausbauen','Avatar ','2–24 Zeichen, bitte.','Min.','Std.',' Beschleuniger','Testkauf · kostenlos','Kaufen · ','Erhalten: ','Belohnung: ','VIP-PASS',' · Rang ',' / ',' · Level '}
by_source={v:k for k,v in entries.items()}
for relative in sorted({r['file'] for r in rows}):
    p=ROOT/relative;s=p.read_text()
    for value in extra-{' / '}:
        literal=json.dumps(value,ensure_ascii=False)
        if literal not in s:continue
        key=by_source.get(value,'presentation.'+p.stem.lower()+'.'+re.sub('[^a-z0-9]+','_',value.lower()).strip('_')+'.'+hashlib.sha256(value.encode()).hexdigest()[:8])
        entries[key]=value;by_source[value]=key
        s=s.replace(literal,'Weedtopia.Domain.GameText.Get('+json.dumps(key)+')')
    p.write_text(s,encoding='utf8')
# Key-based lookup stays isolated from domain/save data.
p=ROOT/'Assets/Weedtopia/Scripts/Runtime/Domain/Definitions.cs';s=p.read_text(encoding='utf-8-sig')
s=s.replace('public string Text(string key) => texts.FirstOrDefault(x => x.key == key)?.de ?? "[" + key + "]";','[NonSerialized] public ITextLookup TextLookup;\n        public string Text(string key) => TextLookup?.Get(key) ?? texts.FirstOrDefault(x => x.key == key)?.de ?? "[" + key + "]";')
p.write_text(s,encoding='utf8')
p=ROOT/'Assets/Weedtopia/Scripts/Runtime/Content/GameContent.cs';s=p.read_text(encoding='utf-8-sig')
s=s.replace('[SerializeField] private TextAsset definitions;','[SerializeField] private TextAsset definitions;\n        [SerializeField] private LanguageCatalog languageCatalog;\n        public LanguageCatalog Languages => languageCatalog;')
p.write_text(s,encoding='utf8')
# Semantic templates permit arbitrary word order instead of translating sentence fragments.
templates={
'settings.language':'Sprache','settings.game':'Spiel','settings.profile':'Profil',
'guidance.talk':'Mit {0} sprechen','guidance.level':'{0}: auf Stufe {1} ausbauen','guidance.assigned':'{0}: {1} passende Worker einsetzen','guidance.automated':'{0}: Leitung einsetzen (automatischer Betrieb)',
'guidance.residents':'Wohnhäuser: {0} Einwohner erreichen','guidance.reward':'Belohnung: {0} Geld',
'profile.avatar':'Avatar {0} / {1}',
'format.money':'{0} Geld','format.grinders':'{0} Grinder','format.cards':'{0} Karten','format.level':'Level {0}', 'format.worker':'{0} · Level {1} · Rang {2}\n{3}',
'shop.vip_until':'VIP aktiv bis {0} · Boni gelten für beide Städte.', 'shop.balance':'{0} Geld   ·   {1} Grinder',
'shop.accelerator':'{0} × {1} Beschleuniger', 'shop.duration_minutes':'{0} Min.', 'shop.duration_hours':'{0} Std.',
'shop.test_price':'Jetzt kostenlos testen · geplanter Preis {0}', 'shop.buy_price':'Kaufen · {0}', 'shop.received':'Erhalten: {0}',
'quest.active_count':'AKTIVE AUFGABEN · {0}', 'quest.next_step':'Nächster Schritt: {0}', 'quest.next_preview':'Als Nächstes:\n{0}',
'format.date':'{0:g}'
}
entries.update(templates)
(OUT/'source_inventory.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
(OUT/'runtime_migration.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
folder=ROOT/'Assets/Weedtopia/Game/Localization';folder.mkdir(parents=True,exist_ok=True)
for lang in ['de','en','es','fr','ru']:
    document={'language':lang,'entries':[{'key':k,'value':v} for k,v in sorted(entries.items())] if lang=='de' else []}
    (folder/(lang+'.json')).write_text(json.dumps(document,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('Migrated',len(rows),'presentation literals;',len(entries),'catalog keys')

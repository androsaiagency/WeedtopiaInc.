"""Author the approved shop catalog and retain a pre-change preservation baseline."""
import hashlib
import json
import pathlib
import shutil

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/ShopRevision'
BASE = OUT / 'Preservation'
BASE.mkdir(parents=True, exist_ok=True)
paths = ['Assets/Weedtopia/Game/Scenes/BurningHills.unity',
         'Assets/Weedtopia/Game/Scenes/BigRockCity.unity',
         'Assets/Weedtopia/Production/Prefabs/WorldNavigationDock.prefab',
         'Assets/Weedtopia/Game/Content/BurningHills.json']
for name in paths:
    target = BASE / (pathlib.Path(name).name + '.before')
    if not target.exists():
        shutil.copyfile(ROOT / name, target)
catalog = []
texts = []


def add(id, title, category, cents, icon, money=0, grinders=0, item=None, vip=0):
    key = 'shop.product.' + id
    catalog.append(dict(id='weedtopia.' + id, titleKey=key, category=category,
                        euroCents=cents, icon=icon, money=money, grinders=grinders,
                        items=[] if item is None else [dict(id=item, count=5)], vipDays=vip))
    texts.append(dict(key=key, de=title))


add('vip_30d', 'VIP · 30 Tage', 1, 499, 'star', vip=30)
add('starter', 'Start-Paket', 0, 299, 'gift', 10000, 200, 'build_5m')
add('development', 'Entwicklungs-Paket', 0, 499, 'gift', 50000, 500, 'build_15m')
for id, title, cents in [('5m', '5 × 5 Minuten', 99), ('15m', '5 × 15 Minuten', 199),
                         ('1h', '5 × 1 Stunde', 299), ('5h', '5 × 5 Stunden', 499)]:
    add('boost_' + id, title, 4, cents, 'time', item='build_' + id)
for value, cents in [(1000, 99), (5000, 199), (10000, 299), (100000, 499), (1000000, 999)]:
    add('money_' + str(value), f'{value:,}'.replace(',', '.') + ' Geld', 2, cents, 'money', money=value)
for value, cents in [(50, 99), (200, 199), (500, 299), (1000, 499), (10000, 999)]:
    add('grinders_' + str(value), f'{value:,}'.replace(',', '.') + ' Grinder', 3, cents, 'grinder', grinders=value)
content_path = ROOT / paths[-1]
data = json.loads(content_path.read_text(encoding='utf-8-sig'))
data['shopProducts'] = catalog
extra = texts + [dict(key='shop.vip_daily', de='VIP · tägliche Grinder'),
                 dict(key='shop.vip_status', de='VIP-Status'),
                 dict(key='reward.source.shop', de='Shop'),
                 dict(key='reward.source.vipmembership', de='VIP-Pass')]
known = {entry['key']: entry for entry in data['texts']}
for entry in extra:
    if entry['key'] in known:
        known[entry['key']]['de'] = entry['de']
    else:
        data['texts'].append(entry)
content_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
protected_path = BASE / 'protected.json'
if not protected_path.exists():
    protected = []
    for folder in ['Assets/Weedtopia/Art', 'Assets/Weedtopia/Production/Art', 'Assets/Weedtopia/Production/Audio']:
        for path in (ROOT / folder).rglob('*'):
            if path.is_file():
                protected.append(dict(path=path.relative_to(ROOT).as_posix(), sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    protected_path.write_text(json.dumps(protected, indent=2), encoding='utf-8')
(OUT / 'catalog.json').write_text(json.dumps(catalog, ensure_ascii=False, indent=2), encoding='utf-8')
print('Authored', len(catalog), 'products; original scenes, dock, content and artwork retained for comparison.')

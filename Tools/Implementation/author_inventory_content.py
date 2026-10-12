"""Idempotent item catalog/reward authoring. Does not change building economy or quest goals."""
from pathlib import Path
import json

root = Path(__file__).resolve().parents[2]
path = root / 'Assets/Weedtopia/Game/Content/BurningHills.json'
data = json.loads(path.read_text(encoding='utf-8-sig'))
texts = {t['key']: t['de'] for t in data['texts']}
items = []
def item(id, name, short, description, icon, category, effect, value, rarity=0):
    for suffix, text in [('name', name), ('short', short), ('description', description)]:
        texts['item.' + id + '.' + suffix] = text
    items.append(dict(id=id, nameKey=f'item.{id}.name', shortNameKey=f'item.{id}.short',
        descriptionKey=f'item.{id}.description', icon=icon, category=category, effect=effect, value=value, rarity=rarity))
for id, seconds, duration, rarity in [('build_1m',60,'1 Min.',0), ('build_5m',300,'5 Min.',0),
    ('build_15m',900,'15 Min.',1), ('build_30m',1800,'30 Min.',1), ('build_1h',3600,'1 Std.',2),
    ('build_5h',18000,'5 Std.',2), ('build_10h',36000,'10 Std.',3), ('build_24h',86400,'24 Std.',4)]:
    item(id, f'Baubeschleuniger · {duration}', f'Bauzeit\n{duration}',
        f'Verkürzt die Bauzeit einer gewählten Baustelle um bis zu {duration} Einmal verwendbar. Überschüssige Zeit verfällt.', 'time',1,0,seconds,rarity)
item('team_pack','Kartenpaket','Karten-\npaket','Enthält 3 Worker-Karten. Nutze sie im Team-Menü zum Rekrutieren oder für Rangaufstiege. Ab dem Stadtviertel verwendbar.','cards',0,1,3,1)
item('specialist_contract','Spezialistenvertrag','Spezialisten-\nvertrag','Wähle einen Beruf und erhalte einen seltenen Worker (Rare) mit zwei Eigenschaften. Ab dem Stadtviertel verwendbar.','person',0,2,1,1)
item('cash_crate','Geldkiste','Geldkiste','Enthält 500 Geld für deinen nächsten Ausbau. Du bestimmst, wann du die Kiste öffnest. Einmal verwendbar.','gift',2,3,500,1)
item('grinder_pouch','Grinder-Beutel','Grinder-\nBeutel','Enthält 10 Grinder. Sie werden beim Öffnen deinem vorhandenen Guthaben gutgeschrieben. Einmal verwendbar.','grinder',2,4,10,2)
data['items'] = items
def stacks(*pairs): return [dict(id=id, count=count) for id,count in pairs]
grants = {
 'first_crop':stacks(('build_1m',2)), 'grow_manager':stacks(('build_5m',1)),
 'processing_manager':stacks(('build_5m',1)), 'farm_delivery':stacks(('build_15m',1)),
 'city_sales':stacks(('team_pack',1),('cash_crate',1)),
 'townhall_spin':stacks(('specialist_contract',1),('grinder_pouch',1)),
 'settlement_build':stacks(('build_30m',1)), 'settlement_daily':stacks(('build_1h',1)),
 'industry_build':stacks(('build_5h',1)), 'industry_sell':stacks(('build_10h',1)),
 'freeplay':stacks(('build_24h',1))}
for q in data['quests']: q['itemRewards'] = grants.get(q['id'], [])
for plan in data['cityProject']['plans']:
    rewards = [stacks(('build_15m',2)),stacks(('team_pack',1)),stacks(('build_1h',1)),
        stacks(('specialist_contract',1)),stacks(('cash_crate',2),('grinder_pouch',1)),stacks(('build_24h',1))]
    assert len(plan['steps']) == len(rewards)
    for step, reward in zip(plan['steps'], rewards): step['itemRewards'] = reward
data['vipItemRewards'] = stacks(('build_5m',1))
texts.update({
 'reward.collection':'Inventar', 'reward.open_collection':'Inventar öffnen', 'reward.inventory':'Dein Inventar',
 'reward.tab.inventory':'Inventar', 'reward.inventory_hint':'Wähle ein Item und entdecke, was darin steckt.',
 'reward.source.inventory':'Inventar', 'reward.kind.Item':'Item',
 'inventory.all':'Alle', 'inventory.category.Workers':'Worker / Karten', 'inventory.category.Boosters':'Beschleuniger',
 'inventory.category.Supplies':'Vorräte', 'inventory.empty':'Hier ist noch Platz für deine Belohnungen.\nErfülle Quests und sammle neue Items!',
 'inventory.pick':'Dein nächster Vorteil wartet', 'inventory.pick_hint':'Klicke ein Item an, um es genauer anzuschauen.\n\nHier findest du Wirkung, Bestand und Verwendung.',
 'inventory.owned':'Im Besitz: {0}', 'inventory.worker_meta':'{0} · Level {1} · Rang {2}',
 'inventory.worker_info':'Ein Mitglied deines Teams. Die Eigenschaften verstärken das Gebäude, dem du diesen Worker zuweist.',
 'inventory.cards_info':'Verwende Worker-Karten, um ein neues Teammitglied zu rekrutieren oder einen Worker im Rang zu verbessern.',
 'inventory.manage':'Im Team ansehen', 'inventory.recruit':'Zum Rekrutieren', 'inventory.open':'1 × öffnen',
 'inventory.choose_worker':'Worker verpflichten', 'inventory.boost':'1 × anwenden', 'inventory.confirm':'Verbindlich verwenden',
 'inventory.cancel':'Zurück', 'inventory.confirm_hint':'Bestätige den Verbrauch von genau einem Item.',
 'inventory.no_build':'Keine laufende Baustelle. Starte zuerst einen Bau oder Ausbau.',
 'inventory.locked':'Verfügbar, sobald das Stadtviertel freigeschaltet ist.',
 'inventory.before_after':'Restzeit: {0} → {1}', 'inventory.waste':'Davon verfallen {0}.', 'inventory.ready':'Danach sofort fertig!',
 'inventory.applied':'Baubeschleuniger angewendet.', 'inventory.updated':'Die Baustelle hat sich verändert. Bitte erneut auswählen.',
 'inventory.contents':'Inhalt: {0} {1}', 'inventory.choose_build':'Baustelle auswählen', 'inventory.choose_role':'Beruf auswählen',
 'inventory.selected':'Ausgewählt', 'inventory.confirm_title':'Item verwenden?', 'inventory.back':'← Inventar',
 'inventory.unknown':'Nicht mehr im Inventar', 'inventory.no_filter':'In dieser Kategorie besitzt du noch keine Items.',
 'inventory.balance':'{0} Geld   ·   {1} Grinder',
 'inventory.split':'Aufteilen: {0} × {1}',
 'inventory.split_preview':'1 × {0} wird zu\n{1} × {2}\n\nDie gesamte Bauzeit bleibt erhalten.',
})
data['texts'] = [dict(key=k, de=v) for k,v in texts.items()]
path.write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(f'Authored {len(items)} items; all 8 durations rewarded, repeatable VIP grant; starting economy retained.')

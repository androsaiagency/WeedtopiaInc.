"""Idempotently author the optional second chapter, preserving the tutorial and balance."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
path = ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json'
data = json.loads(path.read_text(encoding='utf-8-sig'))
# This generator owns project.*; remove obsolete keys left by earlier chapter revisions.
texts = {t['key']: t['de'] for t in data['texts'] if not t['key'].startswith('project.')}

def text(key, value):
    key = 'project.' + key
    texts[key] = value
    return key

def goal(kind, count, label, target=''):
    return dict(kind=kind, count=count, target=target,
                textKey=text('goal.' + str(kind) + '.' + (target or str(count)), label))

def step(id, title, body, speaker, focus, goals, money, cards=0, workers=()):
    return dict(id=id, titleKey=text(id+'.title', title), bodyKey=text(id+'.body', body),
                speaker=speaker, focus=focus, goals=goals, moneyReward=money,
                cardsReward=cards, workerRewards=list(workers))

# ProjectGoalKind: Level=0, Automated=1, Residents=2, Refined=3, Coffee=4,
# Butter=5, Pastry=6, Delivered=7. Counters always measure new progress per step.
plans = []
for bakery in (False, True):
    id = 'gourmet' if bakery else 'neighborhood'
    venue = 'bakery_02' if bakery else 'coffee_02'
    role = 'baker' if bakery else 'coffee'
    steps = [
        step('harvest', 'Die Farm macht sich bereit',
             'Für unser erstes Stadtfest brauchen wir Nachschub. Stärke Anbau und Veredelung. Neue Blüten zählen ab jetzt.',
             'mila', 'farm_grow_01', [goal(0,5,'Erstes Anbaufeld · Level','farm_grow_01'),
                 goal(0,5,'Veredelung · Level','farm_processing'),goal(3,200,'Neue Blüten veredelt')], 1000, 1, ['grow']),
        step('logistics', 'Alles kommt an',
             'Setze deine neue Anbaukraft auf dem zweiten Feld ein. Erweitere beide Lager. Die nächste Belohnung ist die Leitung deines Feststands.',
             'nia', 'city_warehouse', [goal(0,5,'Farmlager · Level','farm_warehouse'),
                 goal(0,5,'Stadtlager · Level','city_warehouse'),goal(0,1,'Zweites Anbaufeld · Level','farm_grow_02'),
                 goal(1,1,'Zweites Anbaufeld mit Leitung','farm_grow_02'),goal(7,240,'Neu ausgelieferte Waren')], 2000, 1, [role]),
        step(id+'.venue', 'Die Genussmeile wächst' if bakery else 'Ein zweiter Treffpunkt',
             'Baue die zweite Bäckerei aus und setze deinen neuen Bäcker ein. Für das Verkaufsziel zählen alle Bäckereien.' if bakery else
             'Baue den zweiten Coffee-Shop aus und setze deine neue Verkaufskraft ein. Für das Verkaufsziel zählen alle Coffee-Shops.',
             'baker' if bakery else 'city_host', venue, [goal(0,5,'Zweite Bäckerei · Level' if bakery else 'Zweiter Coffee-Shop · Level',venue),
                 goal(1,1,'Feststand mit Leitung',venue),goal(6,120,'Neu verkaufte Backwaren') if bakery else goal(4,360,'Neu verkaufte Blüten')], 3000, 2),
        step('neighbors', 'Ein Fest für die Nachbarn',
             'Unsere Nachbarn feiern mit! Ein Haus und ein Park auf Level 5 reichen. Bestehende Gebäude zählen; neue Bauplätze sind nicht nötig.',
             'gardener', 'settlement_01', [goal(0,5,'Ein Wohnhaus · Level','residential_house'),
                 goal(0,5,'Ein Park · Level','park'),goal(2,15,'Einwohner')], 2500, 2),
        step(id+'.rehearsal', 'Die große Generalprobe',
             'Jetzt zählt die ganze Kette. Stärke die Industrie und versorge die Generalprobe mit frischer Butter und neuen Backwarenverkäufen.',
             'jaro', 'butter_factory' if bakery else 'supermarket', [goal(0,5 if bakery else 2,'Butterfabrik · Level','butter_factory'),
                 goal(0,2 if bakery else 5,'Supermarkt · Level','supermarket'),goal(0,2,'Erste Bäckerei · Level','bakery_01'),
                 goal(5,120,'Neue Butter produziert'),goal(6,80,'Neu verkaufte Backwaren')], 4000, 3),
        step(id+'.festival', 'Burning Hills feiert!',
             'Die Vorbereitungen stehen! Versorge das Fest mit neuen Verkäufen und Lieferungen. Dann erhältst du Stadttitel und Verkaufsbonus.',
             'rocko', 'town_hall', [goal(4,250 if bakery else 400,'Neu verkaufte Blüten'),
                 goal(6,180 if bakery else 100,'Neu verkaufte Backwaren'),goal(7,400,'Neu ausgelieferte Waren')], 5000, 5)
    ]
    plans.append(dict(id=id, titleKey=text(id+'.title','Genussmeile' if bakery else 'Nachbarschaftsfest'),
        bodyKey=text(id+'.body','Schwerpunkt: zweite Bäckerei und Backwaren.' if bakery else 'Schwerpunkt: zweiter Coffee-Shop und Verkauf.'),
        rewardKey=text(id+'.reward','Abschluss: dauerhaft +10 % Erlös in allen Bäckereien.' if bakery else 'Abschluss: dauerhaft +10 % Erlös in allen Coffee-Shops.'),
        bonusFamily='bakery' if bakery else 'coffee_shop', salesBonus=.1, steps=steps))

data['cityProject'] = dict(id='first_city_festival', cityId='burning_hills',
    titleKey=text('title','Kapitel 2 · Das erste Stadtfest'),
    invitationKey=text('invitation','Rocko lädt zum ersten Stadtfest! Wähle rechts einen Schwerpunkt für sechs Etappen. Die Wahl bleibt bestehen. Beide Wege stärken die Stadt.'),
    completedKey=text('finished','Burning Hills ist Feststadt! Dein Bonus gilt auch offline.'), plans=plans)

for key,value in {
    'step_title':'Stadtfest {0}/{1}\n{2}', 'finished_title':'Burning Hills · Feststadt',
    'goal_progress':'{0}\n{1} / {2}', 'step_reward':'Etappenbelohnung: {0} Geld · {1} Karten',
    'choose':'Verbindlich wählen: {0}', 'counter_hint':'Verkäufe, Produktion und Lieferungen zählen neu je Etappe.',
    'completed':'Erledigt', 'active':'Aktuell', 'later':'Danach',
    'city_title':'Stadttitel', 'city_title_value':'Feststadt', 'progress':'Stadtfest-Etappen'
}.items(): text(key,value)
data['texts'] = [dict(key=k,de=v) for k,v in texts.items()]
path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Authored city festival: two plans, six steps each; tutorial unchanged.')

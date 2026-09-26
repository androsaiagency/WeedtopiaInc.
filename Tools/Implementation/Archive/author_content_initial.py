"""Historical initial-content builder. Current authoritative content is Assets/Weedtopia/Game/Content/BurningHills.json."""
raise SystemExit("Archived one-off authoring script; use the authored JSON and explicit Unity scaffold menu.")
"""Explicit initial content authoring. Never overwrites an existing authored content asset."""
import json, pathlib
ROOT = pathlib.Path(__file__).resolve().parents[2]
DEST = ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json'
texts = {}
def text(key, de): texts[key] = de; return key
def curve(n, growth=1.008): return dict(basis=n, growth=growth)
families = [
    ('cultivation_plot','Anbauplatz',0,0,'grow','',80,16,4,4,1),
    ('farm_processing','Farm-Veredelung',1,0,'processing','',120,24,1,1,1),
    ('farm_warehouse','Farm-Lager',2,0,'farm_manager','farm_driver',140,40,8,1,1),
    ('city_warehouse','Stadt-Lager',2,1,'city_manager','city_driver',140,48,8,1,1),
    ('coffee_shop','Coffee-Shop',3,1,'coffee','',120,24,1,1,12),
    ('residential_house','Wohnhaus',4,2,'caretaker','',160,20,1,1,1),
    ('park','Park',5,2,'ranger','',120,20,1,1,1),
    ('butter_factory','Butterfabrik',6,3,'butter','',180,32,1,1,1),
    ('supermarket','Supermarkt',7,3,'market','',160,20,.1,1,1),
    ('bakery','Bäckerei',8,3,'baker','',180,24,1,1,35),
    ('town_hall','Rathaus · Rocko',9,1,'','',1,1,1,1,1),
    ('settlement_plot','Freier Bauplatz',10,2,'','',1,1,1,1,1),
]
buildings=[]
for id,name,kind,zone,manager,driver,cost,cap,rate,yield_,price in families:
    buildings.append(dict(id=id,nameKey=text('building.'+id,name),kind=kind,zone=zone,managerRole=manager,driverRole=driver,
        cost=curve(cost,1.012),capacity=curve(cap),rate=curve(rate),yield_=curve(yield_),price=curve(price,1.004),
        cycleSeconds=4,baseBuildSeconds=3,maxBuildSeconds=120,baseResidents=5,buildGrowth=1.001))
    buildings[-1]['yield'] = buildings[-1].pop('yield_')
instances=[]
def instance(id,family,zone,x,z): instances.append(dict(id=id,family=family,zone=zone,cityId='burning_hills',x=x,z=z))
instance('farm_grow_01','cultivation_plot',0,-36,6); instance('farm_grow_02','cultivation_plot',0,-36,-6)
instance('farm_processing','farm_processing',0,-24,0);instance('farm_warehouse','farm_warehouse',0,-12,0)
instance('city_warehouse','city_warehouse',1,4,0);instance('town_hall','town_hall',1,4,14)
instance('coffee_01','coffee_shop',1,16,6);instance('coffee_02','coffee_shop',1,16,-6)
for n,(x,z) in enumerate([(0,34),(14,34),(0,48),(14,48)]):instance('settlement_'+str(n+1).zfill(2),'settlement_plot',2,x,z)
instance('butter_factory','butter_factory',3,34,0);instance('supermarket','supermarket',3,34,14)
instance('bakery_01','bakery',3,48,0);instance('bakery_02','bakery',3,48,14)
routes=[]
def route(id,a,b,r,vehicle=False,time=6,cap=8):routes.append(dict(id=id,from_=a,to=b,resource=r,vehicle=vehicle,travelSeconds=time,capacity=cap))
route('grow1_processing','farm_grow_01','farm_processing',0);route('grow2_processing','farm_grow_02','farm_processing',0)
route('processing_farm','farm_processing','farm_warehouse',1);route('farm_city','farm_warehouse','city_warehouse',1,True,8)
route('city_coffee1','city_warehouse','coffee_01',1,True);route('city_coffee2','city_warehouse','coffee_02',1,True)
route('city_butter','city_warehouse','butter_factory',1,True,10)
route('butter_bakery1','butter_factory','bakery_01',2);route('butter_bakery2','butter_factory','bakery_02',2)
for r in routes:r['from']=r.pop('from_')
roles=[]
roledata=[('grow','Anbau-Manager',[0,1,2,3,9]),('processing','Verarbeitungsmanager',[0,1,2,3,9]),
('farm_manager','Farm-Lager-Manager',[1,2,3,9,0]),('farm_driver','Farm-Fahrer',[4,5,2,1,3]),
('city_manager','Stadt-Lager-Manager',[1,2,3,9,0]),('city_driver','Stadt-Lieferfahrer',[4,5,2,1,3]),
('coffee','Coffee-Shop-Manager',[1,2,3,6,9]),('caretaker','Hausmeister',[7,8,9,10,11]),
('ranger','Parkwächter',[8,9,12,13,14]),('butter','Butterfabrik-Manager',[0,1,2,3,9]),
('market','Supermarkt-Leiter',[1,2,6,8,9]),('baker','Bäckermeister',[1,2,3,6,9])]
for id,name,attrs in roledata:roles.append(dict(id=id,nameKey=text('role.'+id,name),attributes=attrs))
quests=[]
def cond(kind,target='',count=1):return dict(kind=kind,target=target,count=count)
def quest(id,title,body,speaker,focus,conditions,money=150,cards=0,workers=(),unlock=0):
    quests.append(dict(id=id,titleKey=text('quest.'+id+'.title',title),bodyKey=text('quest.'+id+'.body',body),speaker=speaker,focus=focus,
                       conditions=conditions,moneyReward=money,cardsReward=cards,workerRewards=list(workers),unlock=unlock))
quest('farm_welcome','Mila wartet am Anbauplatz','Sprich mit Mila am ersten Anbauplatz. Wir fangen klein an.','Mila','farm_grow_01',[cond(0,'mila')],money=0)
quest('first_crop','Ein Topf voller Hoffnung','Baue den ersten Anbauplatz auf Level 1 aus. Geld bezahlt den Ausbau, danach läuft die Bauzeit.','Mila','farm_grow_01',[cond(1,'farm_grow_01')])
quest('manual_harvest','Handarbeit','Warte auf die reife Ernte und sammle sie von Hand ein. Ohne Manager bleibt sie am Anbauplatz.','Mila','farm_grow_01',[cond(2)],workers=['grow'])
quest('grow_manager','Mila übernimmt','Weise den erhaltenen Anbau-Manager dem ersten Anbauplatz zu.','Mila','farm_grow_01',[cond(3,'farm_grow_01')])
quest('processing','Jaro veredelt','Sprich mit Jaro. Baue die Farm-Veredelung auf. Ein kleiner Durchlauf staut die Ernte auf dem Band.','Jaro','farm_processing',[cond(0,'jaro'),cond(1,'farm_processing')],workers=['processing'])
quest('processing_manager','Das Band läuft','Weise den Verarbeitungsmanager zu. Puffer behalten Material, auch wenn der nächste Schritt voll ist.','Jaro','farm_processing',[cond(3,'farm_processing')])
quest('farm_storage','Nias Logistik','Sprich mit Nia. Repariere das Farm-Lager. Die erste visuelle Stufe hat einen Fahrerplatz.','Nia','farm_warehouse',[cond(0,'nia'),cond(1,'farm_warehouse')],workers=['farm_manager','farm_driver'])
quest('farm_delivery','Burning Hills bekommt Nachschub','Besetze Manager und Fahrer im Farm-Lager. Eine echte Lieferung muss das Stadtdock erreichen.','Nia','farm_warehouse',[cond(3,'farm_warehouse',2),cond(4),cond(12,'farm')],unlock=1,cards=1)
quest('city_rebuild','Willkommen in der Stadt','Aktiviere das Stadt-Lager und den ersten Coffee-Shop. Rocko erwartet dich im Rathaus.','Rocko','city_warehouse',[cond(1,'city_warehouse'),cond(1,'coffee_01')],workers=['city_manager','city_driver','coffee'],unlock=1)
quest('city_sales','Die erste Kasse klingelt','Besetze Stadt-Manager, Lieferfahrer und Shop-Manager. Verkaufe mindestens eine veredelte Blüte.','Rocko','coffee_01',[cond(3,'city_warehouse',2),cond(3,'coffee_01'),cond(5)],unlock=1)
quest('townhall_spin','Ein neues Gesicht','Öffne das Rathaus und verwandle eine Worker-Karte mit einem Spin in einen Worker.','Rocko','town_hall',[cond(7),cond(6)],unlock=2,money=350)
quest('settlement_build','Ein Zuhause und ein grüner Ort','Wähle auf zwei getrennten Bauplätzen ein Wohnhaus und einen Park und baue beide auf. Der letzte freie Platz bleibt für den noch fehlenden Typ reserviert.','Rocko','settlement_01',[cond(1,'residential_house'),cond(1,'park')],workers=['caretaker','ranger'],unlock=2)
quest('settlement_daily','Die Nachbarschaft wächst','Besetze Hausmeister und Parkwächter. Hole die Tagesbelohnungen ab und erledige eine VIP-Quest. Neue Gebäude haben eine erste Tagesration.','Rocko','settlement_01',[cond(8),cond(9),cond(13)],unlock=3,money=600,workers=['butter','market','baker'])
quest('industry_build','Die nächste Produktionskette','Repariere Butterfabrik, Supermarkt und eine Bäckerei. Der Stadtfahrer beliefert die Butterfabrik im Wechsel mit den Shops.','Rocko','butter_factory',[cond(1,'butter_factory'),cond(1,'supermarket'),cond(1,'bakery_01')],unlock=3)
quest('industry_sell','Butter, Backwaren, Zukunft','Besetze die drei Spezialisten. Zwei Blüten werden zu einer Butter. Die Bäckerei verkauft fertige Backwaren.','Rocko','bakery_01',[cond(10),cond(11),cond(12,'industry')],unlock=3)
quest('freeplay','Burning Hills gehört euch','Hole den täglichen Grinderbonus im Supermarkt ab. Danach bestimmst du den weiteren Ausbau. Parks liefern weiterhin erreichbare VIP-Aufträge.','Rocko','supermarket',[cond(9,'supermarket'),cond(12,'industry')],unlock=3,money=500)
base_texts={
 'game.title':'BURNING HILLS','game.subtitle':'Wiederaufbau · Produktion · Gemeinschaft',
 'intro.0':'Rocko: Hey, aufwachen! Beide Seiten haben im THC-Krieg kapituliert. Jetzt bauen wir gemeinsam wieder auf.',
 'intro.1':'Rocko: Früher war ich bei der Mafia. Heute arbeite ich für die Regierung – als Bürgermeister von Burning Hills. Und du wirst mein Assistent.',
 'intro.2':'Rocko: Du bekommst 1.000 Geld und 50 goldene Grinder. Bring zuerst die Farm zum Laufen. Mila, Jaro und Nia helfen dir. Grinder brauchst du fürs Tutorial nicht.',
 'intro.done':'Willkommen, {0}. Die Farm wartet auf dich.',
 'name.invalid':'Bitte 2–24 Zeichen ohne Steuerzeichen oder spitze Klammern eingeben.',
 'bottleneck.harvest':'Erntepuffer voll · ernten oder Manager zuweisen', 'bottleneck.manager':'Handbetrieb · Arbeit starten oder Manager zuweisen',
 'bottleneck.output':'Abfluss blockiert · nachgelagertes Lager/Verarbeitung prüfen', 'bottleneck.throughput':'Durchlauf ausgelastet · Verarbeitung ausbauen',
 'bottleneck.input':'Wartet auf Material · vorgelagerte Kette prüfen', 'bottleneck.storage':'Lager voll · Lieferung wartet mit Fracht',
 'bottleneck.driver':'Fahrer fehlt · Fahrerplatz besetzen', 'bottleneck.none':'Produktion im Fluss',
 'failure.None':'Erledigt.', 'failure.Locked':'Zone noch gesperrt. Schließe den aktuellen Hauptauftrag ab.',
 'failure.Invalid':'Diese Aktion ist hier noch nicht möglich.', 'failure.Busy':'Gerade im Bau oder auf Lieferung. Bitte warten.',
 'failure.Money':'Es fehlt Geld. Produktion oder Auftragsbelohnung nutzen.', 'failure.GrinderConfirmation':'Geld reicht nicht. Eine Grinder-Ausgabe braucht deine Bestätigung.',
 'failure.Grinders':'Nicht genug Grinder.', 'failure.Incompatible':'Dieser Worker passt nicht zur Tätigkeit des Slots.',
 'failure.Slot':'Dieser Fahrerplatz wird erst mit dem nächsten visuellen Tier frei.', 'failure.Card':'Keine Worker-Karte verfügbar. Wohnhäuser liefern täglich Karten.',
 'failure.NotReady':'Noch nicht bereit. Die angezeigten Bedingungen zuerst erfüllen.', 'failure.StaleOffer':'Angebot abgelaufen oder bereits verwendet. Bitte neu öffnen.',
 'failure.ReservedPlot':'Dieser letzte freie Platz wird für den noch fehlenden Wohnhaus-/Parktyp gebraucht.',
 'online.disabled':'Online-Rangliste noch nicht verbunden', 'local.title':'Lokale Stadtstatistik',
 'ui.new':'Neues Spiel','ui.continue':'Weiterladen','ui.name':'Dein Name','ui.start':'Burning Hills starten','ui.next':'Weiter','ui.skip':'Intro überspringen',
 'ui.restart_intro':'Intro erneut ansehen','ui.save':'Speichern','ui.pause':'Pause / Einstellungen','ui.resume':'Weiterspielen','ui.close':'Schließen',
 'ui.upgrade':'Mit Geld ausbauen','ui.finish':'Bauzeit mit Grindern beenden','ui.harvest':'Ernte einsammeln','ui.work':'30 s Handbetrieb starten',
 'ui.talk':'Bewohner ansprechen','ui.claim':'Questbelohnung abholen','ui.focus':'Zum Questziel','ui.workers':'Worker','ui.journal':'Aufträge',
 'ui.townhall':'Rathaus','ui.spin':'1 Karte → Worker-Spin','ui.daily':'Tagesbelohnungen abholen',
 'ui.house':'Wohnhaus wählen','ui.park':'Park wählen','ui.confirm':'Grinder verbindlich ausgeben','ui.cancel':'Abbrechen',
 'ui.volume':'Lautstärke','ui.scale':'UI-Skalierung','ui.reset':'Neues Spiel vorbereiten','ui.filter':'Rollenfilter',
 'ui.assign':'Zuweisen','ui.remove':'Freistellen','ui.worker_level':'Worker-Level erhöhen','ui.worker_rank':'Worker-Rang erhöhen',
 'ui.freeplay':'Freies Spiel · Burning Hills wächst','ui.dev':'Entwicklungsvisuals · Gebäude-Art wartet auf visuelle Freigabe',
 'ui.saved':'Spielstand sicher gespeichert.','ui.no_save':'Noch kein gültiger Spielstand vorhanden.',
 'ui.recovered':'Die Hauptdatei war beschädigt. Der letzte gültige Backupstand wurde geladen.',
 'ui.offline':'Offline: {0} Sekunden simuliert · {1} Geld verdient. Maximal 8 Stunden; negative Zeit wird nicht belohnt.',
 'ui.premium':'{0} Grinder für {1}? Erst dieser Bestätigungsklick gibt Premiumwährung aus.',
 'ui.empty_workers':'Noch keine passenden Worker. Hauptquests geben garantierte Spezialisten; Karten gibt es in Wohnhäusern.',
 'ui.empty_vip':'Noch keine VIP-Aufträge. Ein gebauter Park stellt täglich neue Aufgaben bereit.',
 'ui.stats':'Verdient {0} · Blüten roh {1} / veredelt {2}\nCoffee-Verkäufe {3} · Backwaren {4}\nEinwohner {5} · Lieferungen {6}\nHauptquests {7} · VIP {8} · Worker erhalten/eingesetzt {9}/{10}\nHöchstes Level {11} · Spielzeit {12}s · Freeplay {13}',
 'ui.detail':'Level {0} · {1}\nPuffer {2} / {3} je Ressource\nDurchsatz {4}/s · Ausbau {5} Geld\nBauzeit {6}s · Fahrerplätze {7}',
 'ui.worker_detail':'{0} · {1}\nLevel {2} · Rang {3}\nAttribute: {4}\nZuweisung: {5}',
 'ui.hud':'Geld {0}     ◆ Grinder {1}     ▣ Karten {2}     Einwohner {3}',
 'ui.vip':'VIP: {0} → Ziel {1} · Belohnung {2} Geld',
 'zone.Farm':'Farm','zone.City':'Stadt','zone.Settlement':'Siedlung','zone.Industry':'Industrie',
 'rarity.0':'Normal','rarity.1':'Rare','rarity.2':'Super Rare','rarity.3':'Ultra Rare','rarity.4':'Mega Rare',
 'speaker.mila':'Mila · Anbau','speaker.jaro':'Jaro · Veredelung','speaker.nia':'Nia · Logistik','speaker.rocko':'Rocko · Bürgermeister',
}
texts.update(base_texts)
for i,a in enumerate(['Ertrag','Zyklusgeschwindigkeit','Durchsatz','Lagerkapazität','Transportkapazität','Fahrgeschwindigkeit','Verkaufspreis','Einwohnerbonus','Tagesbonus','Bauzeitbonus','Trainingsermäßigung','Rangermäßigung','VIP-Prämie','VIP-Zielbonus','Gemeinschaftseinkommen']):texts['attribute.'+str(i)]=a

data=dict(version=1,maxOfflineSeconds=28800,maxDailyCatchup=7,maxVip=12,rarityWeights=[60,25,10,4,1],workerAttributeStep=.05,
          workerLevelBaseCost=50,workerRankCards=2,moneyPerGrinder=100,secondsPerGrinder=60,buildings=buildings,instances=instances,
          routes=routes,roles=roles,quests=quests,texts=[dict(key=k,de=v) for k,v in texts.items()])
if DEST.exists(): raise RuntimeError('Existing authored content is protected: '+str(DEST))
DEST.parent.mkdir(parents=True,exist_ok=True)
DEST.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print('Authored content:',len(buildings),'families,',len(instances),'instances,',len(quests),'quests,',len(texts),'texts')

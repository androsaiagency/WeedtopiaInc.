"""Author Big Rock City's campaign and inhabitants without rewriting Burning Hills' content."""
import copy, json, pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
PATH = ROOT/'Assets/Weedtopia/Game/Content/BurningHills.json'
CITY = 'big_rock_city'
PREFIX = 'br_'

PEOPLE = [
    ('berta', 'Berta Fels', 'Vorarbeiterin mit langem Gedächtnis', 25, 'town_hall',
     'Berta hat die ersten Häuser am Felsen mitgebaut. Seit der Bürgermeister verschwunden ist, hält sie die Baustellen mit einem Bleistift und viel Geduld zusammen. Versprechen glaubt sie erst, wenn jemand mit anpackt.',
     'Ein gerader Balken ist mir lieber als eine große Rede.', [25, 600, 1500, 3500],
     [('Ein Plan mit Eselsohren', 'Berta breitet einen abgegriffenen Bauplan aus. Unter den schönen Fassaden fehlen einfache Dinge: Lagerplätze, Werkzeug und ein verlässlicher Arbeitsweg. Hilf ihr, die ersten Bausteine wirklich fertigzustellen.', 'farm_warehouse', 2, 0, 30, 'Bertas erster verlässlicher Bauplan'),
      ('Die Bank am Feierabend', 'Berta erzählt von ihrer Mutter, die jeden Abend an der unfertigen Promenade auf sie wartete. Sie möchte wieder einen Ort schaffen, an dem die Bauleute nach der Schicht sitzen können.', 'park', 10, 5, 40, 'Die Feierabendbank am Felsen'),
      ('Kein Haus ohne Nachbarn', 'Ein altes Musterhaus steht in Bertas Notizbuch allein auf einem leeren Platz. Heute will sie lieber ein Viertel bauen, in dem Menschen sich begegnen. Entscheide, was seinen Mittelpunkt bildet.', 'residential_house', 25, 4, 100, 'Bertas Nachbarschaftstafel'),
      ('Das Gerüst wird abgebaut', 'Berta findet ihren ersten Lehrlingsbrief wieder. Zum ersten Mal muss sie nicht mehr jeden Fehler selbst ausbügeln. Gemeinsam könnt ihr die Verantwortung an die nächste Generation übergeben.', 'farm_warehouse', 50, 7, 200, 'Bertas weitergereichter Zimmermannswinkel')]),
    ('levin', 'Levin Quitt', 'Buchhalter zwischen Belegen und Mut', 75, 'town_hall',
     'Levin war für die Stadtkasse zuständig, durfte die entscheidenden Bücher aber nie sehen. Zurück blieben leere Ordner und sein schlechter Ruf. Er möchte offenlegen, was geschehen ist, ohne die Menschen für den Diebstahl eines Einzelnen zahlen zu lassen.',
     'Diesmal kann jeder nachsehen, wohin das Geld geht.', [75, 750, 2000, 4000],
     [('Die leere Kasse', 'Levin öffnet den alten Tresor: darin liegen nur ein Theaterprogramm und unbezahlte Lieferscheine. Eine erste ordentliche Lieferung gibt ihm einen überprüfbaren Anfang für das neue Kassenbuch.', 'city_warehouse', 2, 9, 1, 'Das erste offene Kassenbuch'),
      ('Jeder Beleg hat ein Gesicht', 'Hinter einem zerknitterten Lieferschein steckt eine Bäckerei, die ihren Lohn nie erhalten hat. Levin sammelt die Namen der Betroffenen und möchte sehen, dass die Versorgung wieder ankommt.', 'bakery', 5, 6, 20, 'Die Wand der beglichenen Versprechen'),
      ('Licht im Rathaus', 'Levin hat herausgefunden, wie Scheinrechnungen das Startkapital verschwinden ließen. Er will die Unterlagen öffentlich erklären. Hilf ihm, die neuen Handelswege als nachvollziehbares Gegenbeispiel zu zeigen.', 'supermarket', 10, 8, 100, 'Levins öffentliches Haushaltsfenster'),
      ('Eine Zahl, auf die wir stolz sind', 'Levin schlägt die letzte Seite des alten Buches zu. Das neue beginnt nicht mit einem großen Guthaben, sondern mit einem Beschluss der Bewohner: Von nun an werden Entscheidungen gemeinsam erklärt.', 'city_warehouse', 50, 8, 300, 'Der gemeinsam unterzeichnete Stadtetat')]),
    ('samira', 'Samira Kupfer', 'Cafébetreiberin und Gastgeberin', 150, 'coffee_01',
     'Samiras Großvater eröffnete das Café am alten Steinbruch. Als die Stadtverwaltung zusammenbrach, stellte sie trotzdem jeden Morgen die Stühle hinaus. Für sie beginnt ein Neuanfang mit einer warmen Tasse und einem freien Platz.',
     'Setz dich. Die Stadt rennt uns schon nicht davon.', [150, 1000, 2500, 5000],
     [('Die Stühle bleiben draußen', 'Samira zeigt dir den leeren Platz vor ihrem Café. Sie möchte keinen feierlichen Fototermin, sondern wieder verlässliche Gäste. Bring die erste kleine Runde ins Rollen.', 'coffee_shop', 2, 5, 12, 'Samiras wieder geöffnete Kaffeeterrasse'),
      ('Ein Rezept auf der Rückseite', 'Auf einer alten Ansichtskarte entdeckt Samira das Gebäckrezept ihres Großvaters. Sie erinnert sich kaum an seine Stimme, aber noch genau an den Duft. Die Bäckerei kann daraus etwas Neues machen.', 'bakery', 10, 6, 35, 'Die Rezeptkarte vom Steinbruchcafé'),
      ('Ein Tisch für die ganze Straße', 'Die Nachbarn kennen inzwischen Samiras Bestellungen, aber noch nicht ihre Namen. Sie plant einen langen Tisch. Du bestimmst, ob Geschichten oder gemeinsames Essen den Abend eröffnen.', 'park', 25, 5, 100, 'Der lange Tisch der Kupfergasse'),
      ('Das zweite Schlüsselbund', 'Samira hat jeden Morgen allein aufgeschlossen. Nun möchte eine ehemalige Stammkundin das Café mitführen. Hilf ihnen, den Betrieb zuverlässig genug für einen freien Nachmittag zu machen.', 'coffee_shop', 50, 5, 250, 'Samiras zweites Schlüsselbund')]),
    ('enno', 'Enno Funke', 'Tüftler für die Dinge, die noch gehen', 300, 'butter_factory',
     'Enno reparierte früher die Maschinen des Steinbruchs. Sein bester Prototyp wurde nie bezahlt und landete im Schuppen. Er entdeckt, dass die alte Technik nicht weg muss: Mit anderen Aufgaben kann sie wieder nützlich sein.',
     'Wenn es klappert, ist meistens noch Leben drin.', [300, 1250, 3000, 6000],
     [('Der Motor im Schuppen', 'Enno zieht eine staubige Kiste ans Licht. Darin steckt ein kleiner Fördermotor. Er will ihn erst im Alltag testen, bevor jemand wieder ein großes Versprechen daraus macht.', 'farm_processing', 5, 4, 40, 'Ennos wieder laufender Fördermotor'),
      ('Ein Ersatzteil für alle', 'Jede Werkstatt hält dieselben seltenen Teile zurück. Enno schlägt ein gemeinsames Regal vor. Zuverlässige Warenwege zeigen, dass Teilen den Betrieb stärken kann.', 'butter_factory', 10, 8, 100, 'Das gemeinsame Ersatzteilregal'),
      ('Die Reparatursprechstunde', 'Enno richtet einen Tag ein, an dem jeder mit kaputten Dingen vorbeikommen darf. Soll die Werkstatt zuerst Erfahrungen sammeln oder Lehrlinge ausbilden? In beiden Fällen braucht sie Material und Zeit.', 'farm_processing', 25, 4, 180, 'Ennos offene Reparaturwerkstatt'),
      ('Eine Maschine mit Namen', 'Die alte Steinbruchmaschine hat nun einen neuen Zweck und viele helfende Hände. Enno möchte ihr keinen Firmennamen geben, sondern den Namen der Person, die als Erste an ihn glaubte.', 'butter_factory', 50, 7, 250, 'Die Maschine namens Hoffnung')]),
    ('lotte', 'Lotte Klang', 'Musikerin auf der stillen Bühne', 500, 'coffee_03',
     'Lotte spielte früher auf dem Steinbruchfest. Nach dem Verschwinden des Bürgermeisters blieben Bühne und Instrumentenkasse verschlossen. Sie will das Fest zurückbringen, aber diesmal sollen die Bewohner selbst darüber bestimmen.',
     'Ein leerer Platz klingt anders, wenn jemand zuhört.', [500, 1500, 3500, 7000],
     [('Ein Lied unter der Plane', 'Unter einer staubigen Plane findet Lotte ihre alte Bühne. Bevor sie ein großes Fest ankündigt, will sie einen kleinen Auftritt für die Menschen, die hier geblieben sind.', 'coffee_shop', 5, 5, 40, 'Lottes erster Auftritt nach der Stille'),
      ('Der Chor der Kupfergasse', 'Lotte hört beim Aufbau mehr Stimmen als Instrumente. Ein Nachbarschaftschor könnte aus dem Fest eine gemeinsame Geschichte machen. Sorge für die Versorgung seiner ersten Probe.', 'bakery', 10, 6, 60, 'Das Liederbuch der Kupfergasse'),
      ('Kein Eintritt für Erinnerungen', 'Eine Agentur bietet Lotte ein großes Konzert an. Sie erinnert sich an ein Fest, bei dem jeder einen Stuhl mitbrachte. Entscheide, welches Zeichen die offene Bühne setzen soll.', 'park', 25, 8, 180, 'Lottes offene Bühne'),
      ('Das Echo gehört uns', 'Die Felswand trägt den Klang inzwischen bis in die neuen Straßen. Lotte überlässt den letzten Auftritt jungen Musikern und hört zum ersten Mal selbst aus dem Publikum zu.', 'coffee_shop', 50, 5, 300, 'Der Staffelstab des Steinbruchfestes')]),
    ('ida', 'Ida Stein', 'Rangerin zwischen Fels und Garten', 800, 'settlement_03',
     'Ida wuchs am Rand des Steinbruchs auf. Sie kennt die Pflanzen, die in jeder Ritze überleben. Als die Ausbaupläne verschwanden, blieb eine zerrissene Landschaft zurück. Sie möchte zeigen, dass eine wachsende Stadt ihre Umgebung nicht aufgeben muss.',
     'Hier wächst mehr, als man auf den ersten Blick sieht.', [800, 1750, 4000, 8000],
     [('Grün in der Felsspalte', 'Ida führt dich zu einer unscheinbaren Pflanze im warmen Stein. Sie will aus der vergessenen Ecke einen zugänglichen Garten machen. Baue einen ersten Treffpunkt und hilf bei der Versorgung.', 'park', 5, 3, 80, 'Idas erster Garten im Stein'),
      ('Der Weg mit den kleinen Pausen', 'Der alte Steinbruchweg ist für viele Bewohner zu beschwerlich. Ida plant kurze Etappen mit Sitzplätzen und Schatten. Das neue Viertel soll von Anfang an dazugehören.', 'residential_house', 10, 9, 5, 'Die Pausenplätze am Felsenweg'),
      ('Was wir wachsen lassen', 'Ein alter Lageplan schlägt vor, die letzte grüne Senke zuzuschütten. Ida lädt die Stadt zu einem Rundgang ein. Entscheide, wie die Bewohner diesen Ort künftig gemeinsam nutzen.', 'park', 25, 8, 200, 'Der gemeinsam geschützte Felsgarten'),
      ('Die Stadt und der große Stein', 'Ida sieht Kinder auf dem Weg, den früher kaum jemand betrat. Nun kann sie ihre Kenntnisse weitergeben. Die nächste Gartenrunde soll ohne sie auskommen und trotzdem ihre Handschrift tragen.', 'park', 50, 3, 350, 'Idas weitergegebenes Gartenbuch')]),
]

def author():
    data = json.loads(PATH.read_text(encoding='utf-8'))
    if data['version'] >= 3: raise SystemExit('Big Rock City is already authored; no overwrite.')
    data['version'] = 3
    texts = {t['key'] for t in data['texts']}
    def text(key, value):
        if key in texts: raise ValueError('Existing text would be overwritten: '+key)
        texts.add(key); data['texts'].append({'key':key,'de':value}); return key
    text('city.big_rock_city', 'Big Rock City')
    text('campaign.title', 'Unsere Städte')
    text('campaign.open', 'Städte & Rockos Auftrag')
    text('campaign.takeover_title', 'Rocko: Eine Stadt braucht uns')
    text('campaign.takeover_body', 'Der Bürgermeister von Big Rock City ist mit dem Startkapital durchgebrannt. Die Regierung bittet uns, diese Stadt auch zu übernehmen, weil wir schon Großes in Burning Hills geleistet haben.\n\nDie Menschen dort sind geblieben. Gemeinsam bringen wir die Lieferwege wieder in Gang, öffnen die Bücher und geben der Stadt ihre Zukunft zurück.')
    text('campaign.locked', 'Rockos Auftrag erscheint ab {0} Einwohnern in Burning Hills. Bisher erreicht: {1}.')
    text('campaign.accept', 'Übernahmeauftrag annehmen')
    text('campaign.accepted', 'Übernommen · eigene Aufträge und Bewohnergeschichten')
    text('campaign.bridge', 'Die Regierung stellt einmalig {0} Geld und {1} Teamkarten für die ersten Reparaturen bereit.')
    text('campaign.travel', 'Nach {0} reisen')
    text('campaign.current', 'Du bist hier')
    text('campaign.progress', '{0} Einwohner · {1}/{2} Hauptaufträge')
    text('campaign.shared', 'Gemeinsame Kasse und Sammlung. Gebäude, Lieferwege und Geschichten gehören zur jeweiligen Stadt. Beide übernommenen Städte arbeiten auch während deiner Reise weiter.')
    text('campaign.notice', 'Rocko hat einen neuen Auftrag: Big Rock City braucht uns.')
    text('campaign.loading', 'Die Reise wird vorbereitet …')
    text('campaign.no_destination', 'Das Reiseziel ist nicht verfügbar. Dein Fortschritt bleibt gespeichert.')
    text('reward.source.citycharter', 'Regierungsauftrag')
    text('br.dialog.mila', 'Zwischen den warmen Felsen wächst unsere nächste Ernte. Die alte Farm bekommt endlich wieder verlässliche Abnehmer.')
    text('br.dialog.jaro', 'Die Maschinen sind noch da. Was uns gefehlt hat, waren ehrliche Lieferpläne und Menschen, die ihre Zusagen halten.')
    text('br.dialog.nia', 'Leere Lager bedeuten hier auch leere Versprechen. Lass uns die Regale und das Vertrauen zusammen wieder füllen.')
    text('br.dialog.rocko', 'Burning Hills hat gezeigt, was wir gemeinsam schaffen können. Big Rock City bekommt jetzt seine eigene Chance.')
    data['cities'] = [dict(id='burning_hills',nameKey='city.burning_hills',scenePath='Assets/Weedtopia/Game/Scenes/BurningHills.unity',introducedVersion=1),
        dict(id=CITY,nameKey='city.big_rock_city',scenePath='Assets/Weedtopia/Game/Scenes/BigRockCity.unity',sourceCity='burning_hills',introducedVersion=3,
             unlockResidents=1000,bridgeMoney=2500,bridgeCards=2,invitationTitleKey='campaign.takeover_title',invitationBodyKey='campaign.takeover_body')]
    originals = copy.deepcopy(data['instances'])
    ids = {i['id']:PREFIX+i['id'] for i in originals}
    for i in originals:
        i.update(id=ids[i['id']], cityId=CITY, introducedVersion=3)
        data['instances'].append(i)
    for route in copy.deepcopy(data['routes']):
        route.update(id=PREFIX+route['id'],from_=ids[route['from']],to=ids[route['to']])
        route['from']=route.pop('from_'); data['routes'].append(route)
    narrative = [
        ('Die Türen sind noch offen', 'Rocko: Der Bürgermeister ist verschwunden, aber die Menschen sind geblieben. Sprich mit Mila an der Farm. Wir beginnen mit dem, was hier noch vorhanden ist.'),
        ('Ernte zwischen warmen Felsen', 'Mila: Die Böden sind gut, nur der Betrieb stand still. Repariere das erste Anbaufeld. Ein funktionierender Anfang bedeutet mehr als ein weiteres Versprechen.'),
        ('Die erste Kiste Hoffnung', 'Mila: Diese Ernte gehört der Stadt, nicht einem verschlossenen Tresor. Hole die erste Kiste ab; danach hilft dir jemand dauerhaft auf dem Feld.'),
        ('Ein Team, das bleibt', 'Mila: Unser neuer Helfer will wissen, wo er gebraucht wird. Teile ihn dem Feld zu, damit wir auch während deiner Besuche in Burning Hills zuverlässig arbeiten.'),
        ('Staub aus der Werkhalle', 'Jaro: Die Verarbeitung wurde nie fertig eingerichtet. Sprich mit mir und setze die Anlage instand. Wir bringen die alte Technik wieder zum Laufen.'),
        ('Maschinen brauchen Menschen', 'Jaro: Eine reparierte Anlage reicht nicht. Gib dem Verarbeitungsteam einen festen Arbeitsplatz und einen Ablauf, auf den die Fahrer zählen können.'),
        ('Das Lager der gebrochenen Zusagen', 'Nia: Im Lager hängen noch unbezahlte Lieferscheine. Repariere die Halle. Dann ersetzen wir leere Zusagen durch Waren, die wirklich ankommen.'),
        ('Eine Lieferung statt einer Rede', 'Nia: Besetze Lagerleitung und Fahrdienst. Wenn die erste Lieferung eintrifft und die Farm automatisch arbeitet, öffnen wir die Wege in die Kupfergasse.'),
        ('Die Kupfergasse öffnet', 'Rocko: Samira hat die Stühle vor dem Café nie hereingeholt. Repariere Stadtlager und Café. Die Regierung kann fehlendes Startkapital ersetzen, aber Vertrauen müssen wir selbst verdienen.'),
        ('Eine Tasse und ein Gespräch', 'Rocko: Ordne Lager und Café ihre Teams zu und verkaufe die erste Tasse. Bald erzählen dir die Bewohner, wie sie die Stadt durch die schwierige Zeit getragen haben.'),
        ('Das Rathaus ohne Geheimtüren', 'Rocko: Im Rathaus sitzt nun niemand hinter verschlossenen Türen. Besuche es und wirb ein Teammitglied an. Die neuen Entscheidungen sollen für alle sichtbar sein.'),
        ('Häuser mit Nachbarschaft', 'Rocko: Berta möchte keine Kulisse für die nächste Wahl, sondern echte Nachbarschaften. Wähle auf den Bauplätzen mindestens ein Wohnhaus und einen Park und baue sie aus.'),
        ('Die Stadt zählt ihre Menschen', 'Rocko: Nimm die ersten Tageserträge mit, besetze Haus und Park und erledige einen Bewohnerauftrag aus den Tagesaufträgen. Danach gehen wir gemeinsam ins Werkviertel.'),
        ('Das Werkviertel bekommt eine Aufgabe', 'Rocko: Ennos alte Maschinen können mehr als stillstehen. Repariere Butterwerk, Markt und Bäckerei. Damit kann Big Rock City sich wieder selbst versorgen.'),
        ('Vom alten Steinbruch auf den Tisch', 'Rocko: Besetze die drei Betriebe und bringe die erste Butter und das erste Gebäck auf den Weg. Auf den warmen Felsstufen soll wieder Alltag einkehren.'),
        ('Ein neuer Anfang gehört allen', 'Rocko: Hole den ersten Tagesertrag des Marktes ab. Die Versorgung läuft nun zuverlässig. Die Bücher bleiben offen, und die Bewohner gestalten ihre nächsten Kapitel selbst.')]
    for source,(title,body) in zip(copy.deepcopy(data['quests']), narrative):
        old=source['id']; source.update(id=PREFIX+old,cityId=CITY,titleKey=text('br.quest.'+old+'.title',title),bodyKey=text('br.quest.'+old+'.body',body),focus=ids[source['focus']])
        for condition in source['conditions']:
            if condition['target'] in ids: condition['target']=ids[condition['target']]
        data['quests'].append(source)
    for pid,name,role,pop,focus,bio,greeting,thresholds,chapters in PEOPLE:
        pid=PREFIX+pid; base='br.story.'+pid
        person=dict(id=pid,cityId=CITY,residents=pop,repeatDelaySeconds=300,focus=ids[focus],
            nameKey=text(base+'.name',name),roleKey=text(base+'.role',role),biographyKey=text(base+'.biography',bio),greetingKey=text(base+'.greeting',greeting),chapters=[],recurring=[])
        def goal(kind,target,count,key):
            labels={0:'Frisch geerntete Rohware: {1}/{2}',3:'Frisch geerntete Rohware: {1}/{2}',4:'Neue verarbeitete Waren: {1}/{2}',5:'Neue Caféverkäufe: {1}/{2}',6:'Neu verkauftes Gebäck: {1}/{2}',7:'Neue Butterproduktion: {1}/{2}',8:'Neu ausgelieferte Waren: {1}/{2}',9:'Neue abgeschlossene Lieferungen: {1}/{2}'}
            runtime_kind=3 if kind==0 else 7 if kind==6 else 6 if kind==7 else kind
            return dict(kind=runtime_kind,target=target,count=count,textKey=text(key,labels.get(kind,'Fortschritt: {1}/{2}')))
        for index,(title,body,family,level,counter,count,memory) in enumerate(chapters):
            key=base+'.chapter.'+str(index+1); choice=index==2
            choices=[dict(id='community' if choice else 'begin',textKey=text(key+'.choice.0','Gemeinsam mit den Nachbarn' if choice else 'Ich packe mit an'),outcomeKey=text(key+'.outcome.0','Die Nachbarn übernehmen einen Teil der Planung. '+name.split()[0]+' merkt, dass die Stadt wieder zusammenarbeitet.'))]
            if choice: choices.append(dict(id='workshop',textKey=text(key+'.choice.1','Mit einer offenen Werkstatt beginnen'),outcomeKey=text(key+'.outcome.1','Eine offene Werkstatt gibt den Ideen einen festen Ort. '+name.split()[0]+' möchte die Erfahrungen an die nächsten Helfer weitergeben.')))
            person['chapters'].append(dict(id=pid+'_chapter_'+str(index+1),titleKey=text(key+'.title',title),bodyKey=text(key+'.body',body),completedKey=text(key+'.completed',name.split()[0]+': Wir sind einen Schritt weiter. Hier bleibt etwas, das wir zusammen geschafft haben.'),memoryKey=text(key+'.memory',memory),residents=thresholds[index],moneyReward=200+index*200,zone=1 if family not in ['butter_factory','bakery','supermarket'] else 3,
                goals=[dict(kind=0,target=family,count=level,textKey=text(key+'.building','{0} ausbauen: Stufe {1}/{2}')),goal(counter,'',count,key+'.counter')],choices=choices,prerequisites=[],itemRewards=[]))
        for index,(kind,count,label) in enumerate([(5,20,'Eine Runde für die Kupfergasse'),(8,30,'Was heute wirklich ankommen muss'),(3,40,'Ein kleiner Beitrag zum großen Alltag')]):
            key=base+'.repeat.'+str(index+1)
            person['recurring'].append(dict(id=pid+'_repeat_'+str(index+1),titleKey=text(key+'.title',label),bodyKey=text(key+'.body',name.split()[0]+': Die Stadt ist wieder gewachsen. Hilfst du mir bei einer neuen Runde für die Menschen, die jetzt hier leben?'),completedKey=text(key+'.completed','Diese Runde ist angekommen. '+name.split()[0]+' bleibt in der Stadt und meldet sich später wieder.'),memoryKey='',residents=pop,moneyReward=100,zone=1,goals=[goal(kind,'',count,key+'.goal')],choices=[dict(id='help',textKey=text(key+'.choice','Ich kümmere mich darum'),outcomeKey=text(key+'.outcome','Eine kleine Aufgabe macht den Alltag der Stadt verlässlicher.'))],prerequisites=[],itemRewards=[]))
        data['vipStories'].append(person)
    projects=[('open_books','Die Bücher liegen auf dem Tisch',1200,['br_berta','br_levin','br_samira'],'Levin öffnet die alten Akten, Berta zeigt die echten Baustellen und Samira stellt einen Tisch für alle auf. Gemeinsam erklären sie, was verschwunden ist und was bereits wieder funktioniert.',8,80,'Das erste öffentliche Stadtgespräch'),
        ('rock_garden','Grün zwischen großen Steinen',2500,['br_berta','br_enno','br_ida'],'Ida findet Schattenplätze, Berta baut die Sitzstufen und Enno richtet die Wasserversorgung ein. Ein ehemaliger Arbeitsort wird ein Garten, den die Bewohner selbst pflegen.',3,150,'Der gemeinsame Garten am großen Felsen'),
        ('echo_festival','Das Fest, dessen Echo bleibt',5000,['br_berta','br_levin','br_samira','br_enno','br_lotte','br_ida'],'Alle sechs Bewohner legen ihre Erfahrungen zusammen: offene Bücher, verlässliche Technik, gutes Essen, Musik und grüne Wege. Das neue Steinbruchfest gehört der Stadt und ihren Menschen.',6,160,'Das erste selbst gestaltete Steinbruchfest')]
    for pid,title,pop,participants,body,kind,count,memory in projects:
        key='br.joint.'+pid
        q=dict(id='br_joint_'+pid,titleKey=text(key+'.title',title),bodyKey=text(key+'.body',body),completedKey=text(key+'.completed','Big Rock City hat dieses Vorhaben gemeinsam getragen. Die Beteiligten bleiben mit ihren eigenen Geschichten Teil der Stadt.'),memoryKey=text(key+'.memory',memory),residents=pop,moneyReward=1200,zone=3,
            goals=[dict(kind=7 if kind==6 else kind,count=count,target='',textKey=text(key+'.goal','Gemeinsamer neuer Beitrag: {1}/{2}'))],choices=[dict(id='together',textKey=text(key+'.choice','Wir machen das gemeinsam'),outcomeKey=text(key+'.outcome','Die Bewohner planen und entscheiden gemeinsam.'))],prerequisites=[id+'_chapter_1' for id in participants],itemRewards=[])
        data['vipCollaborations'].append(dict(id='br_joint_'+pid,cityId=CITY,host=participants[0],participants=participants,quest=q))
    PATH.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('Big Rock City authored: 56 buildings, 35 routes, 16 main quests, 6 inhabitants, 24 chapters, 18 repeat variants, 3 projects.')

if __name__ == '__main__': author()

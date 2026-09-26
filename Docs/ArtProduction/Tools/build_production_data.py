"""Idempotent, local production-data authoring; never generates substitute images."""
from pathlib import Path
import csv
import hashlib
import json
import sys
from reviewed_geometry import production_height, dimension_note, image_dimension_note, family_dimension_note

ROOT = Path(__file__).resolve().parents[3]
ART = ROOT / 'Docs/ArtProduction'
LEVELS = [1,5,10,25,50,100,150,200,300,400,500,600,700,800,900,1000,1250,1500,1750,2000,2500,3000,4000,5000]
STYLES = ['Reclaimed_Green_Haven.png','Herbal_Atom_Age.png','Burning_Hills_Afterglow.png']
ERAS = [
    ('T00', 'Ruine', 'repairable ruin'),
    ('T01–T04','Survival-Reparatur','salvaged wood, patched corrugated metal, simple hand-built repairs'),
    ('T05–T08','Community-Aufbau','warm plaster, reclaimed wood, sage enamel, first permanent service modules'),
    ('T09–T12','Kooperative Industrie','sage enamel, cream plaster, brass pipes, organized industrial modules'),
    ('T13–T16','District Industry','matte concrete, olive enamel, larger integrated service modules'),
    ('T17–T20','Vertical Eco-Tech','oxidized copper, thick green glass, cream plaster, compact solar technology'),
    ('T21–T23','Metropolitan Flagship','cream plaster, copper, sage enamel, monumental but rounded modular forms'),
    ('T24','Burning-Hills-Ikone','cream plaster, copper and green glass, distinctive integrated roof crown'),
]

# Family DNA and maximum reserved parcel are stable across all visual levels.
FAMILIES = {
 'cultivation_plot': dict(name='Anbauplatz', subject='botanical cultivation module', size=(12,12,25),
    dna_de='Abgerundete Pflanzgefäße mit kräftigem Rand, grünem Pflanzenvolumen und frontseitigem Bewässerungskragen',
    dna='rounded thick-rim planters, readable green foliage masses, front irrigation collar',
    function='Erzeugt die Ernte; Abholung und Förderband bleiben separate spätere Systeme.',
    ports='Bewässerung vorn links; Ernteübergabe vorn rechts; keine eingebauten Förderbänder.',
    materials='terracotta, sage enamel, reclaimed wood, restrained thick green glass'),
 'farm_processing': dict(name='Farm-Veredelung',subject='harvest refinement building',size=(18,16,28),
    dna_de='Weiche Giebelhalle mit rundem Veredelungstank rechts und breitem, eingefasstem Ernteportal',
    dna='soft gabled hall, rounded refinement tank on local +X, broad harvest portal',
    function='Veredelt Ernte zwischen Anbau und Farm-Lager; Durchsatz wird über Tank- und Hallenvolumen sichtbar.',
    ports='Ernteeingang vorn links; veredelter Ausgang vorn rechts; Wartung hinten.',
    materials='warm plaster, sage enamel, aged corrugated metal, brass'),
 'farm_warehouse': dict(name='Farm-Lager',subject='rural dispatch warehouse',size=(22,18,26),
    dna_de='Breite Scheunengiebel mit tiefem Versandtor und seitlichem rundem Dispatch-Kopf',
    dna='broad barn gable, deep shipping door, rounded side dispatch head',
    function='Nimmt veredelte Ernte an und bündelt Versand zur Stadt; Dockvolumen deutet Fahrer-Kapazität an.',
    ports='Annahme vorn links; Versandbucht vorn rechts; reservierte Fahrzeugaufstellung außerhalb des Meshs.',
    materials='reclaimed wood, cream plaster, aged metal, sage enamel'),
 'city_warehouse': dict(name='Stadt-Lager',subject='urban distribution hub',size=(22,18,30),
    dna_de='Abgerundeter städtischer Block mit gestaffelter Attika und zweigeteilter Verteilfront',
    dna='rounded urban block, stepped parapet, bifurcated distribution front',
    function='Verteilt Farm-Ernte zu Coffee-Shops und Industrie; mehrere Ausgänge bleiben klar getrennt.',
    ports='Annahme vorn links; Shop-Ausgang vorn mittig; Industrie-Ausgang vorn rechts.',
    materials='cream plaster, matte concrete, olive enamel, oxidized copper'),
 'coffee_shop': dict(name='Coffee-Shop',subject='botanical retail coffee shop',size=(14,14,24),
    dna_de='Runde Eckfront mit tiefem Servicefenster unter weicher salbeigrüner Markise',
    dna='rounded corner facade, recessed service window, thick sage canopy',
    function='Verkaufsfront und zunehmende Kundenkapazität; keine Konsumszene.',
    ports='Kunden-/Servicefront mittig; Warenannahme vorn links; seitliche Ausgabe rechts.',
    materials='warm plaster, reclaimed wood, sage enamel, muted turquoise'),
 'residential_house': dict(name='Wohnhaus',subject='community residential building',size=(16,16,38),
    dna_de='Weiche Giebel, tiefe Loggia und dauerhaftes Community-Postmodul neben dem Eingang',
    dna='soft gables, deep loggia, integrated community mail niche beside entrance',
    function='Wachsende Wohnkapazität; Post-/Community-Nische deutet Worker-Karten an, ohne Gameplaywerte.',
    ports='Hauseingang vorn mittig; Community-Nische vorn rechts; Versorgung links.',
    materials='warm plaster, reclaimed wood, terracotta, oxidized copper'),
 'park': dict(name='Park',subject='bounded community park parcel',size=(16,16,18),
    dna_de='Niedrige ovale Einfassung mit zentralem Wegbogen und rundem Community-Pavillon hinten',
    dna='low oval parcel edging, central curved approach, round rear community pavilion',
    function='Erholung und VIP-/Community-Treffpunkt als erkennbare Park-Parzelle; keine Quest-UI.',
    ports='Zugang vorn mittig; Pflegezugang links; Community-Pavillon auf rückwärtiger Achse.',
    materials='reclaimed wood, warm stone, muted green foliage, oxidized copper'),
 'butter_factory': dict(name='Butterfabrik',subject='food-grade botanical butter factory',size=(20,18,28),
    dna_de='Abgerundete cremefarbene Mischhalle mit paarigen sauberen Tanks und dicker Rohrbrücke',
    dna='rounded cream mixing hall, paired clean tanks, thick integrated pipe bridge',
    function='Lebensmitteltaugliche Verarbeitung und Versorgung von Bäckereien; sauber und freundlich.',
    ports='Ernteeingang links; Butterausgabe zu Bäckereien rechts; separate rückwärtige Wartung.',
    materials='cream enamel, sage enamel, brass, restrained thick green glass'),
 'supermarket': dict(name='Supermarkt',subject='community supermarket',size=(22,18,25),
    dna_de='Breite weiche Verkaufshalle mit vorgezogener Eingangshaube und einem goldenen Zahnradmotiv',
    dna='broad soft retail hall, projecting entrance hood, one small gold gear motif',
    function='Versorgung und wachsende Verkaufsfläche; ein einziges goldenes Zahnrad deutet Grinder-Bonus an.',
    ports='Haupteingang mittig; Warenannahme links; Ausgabebucht rechts.',
    materials='warm plaster, sage enamel, matte concrete, mustard gold'),
 'bakery': dict(name='Bäckerei',subject='cooperative bakery',size=(16,16,27),
    dna_de='Terrakotta-Ofenbogen mit kurzem kräftigem Schornstein und kupfergedecktem Lieferfenster',
    dna='terracotta oven arch, short thick chimney, copper-capped delivery window',
    function='Backstube und Weiterverarbeitung der Butter; warme Ofen- und Kupferformen statt Tankdominanz.',
    ports='Zutatenannahme links; Lieferfenster rechts; Servicezugang mittig.',
    materials='terracotta, warm plaster, reclaimed wood, aged copper'),
 'town_hall': dict(name='Rathaus',subject='repaired Burning Hills town hall',size=(20,18,22),
    dna_de='Symmetrischer reparierter Bürgersaal mit rundem Zentralbau und Berg-/Flammenkrone',
    dna='symmetric repaired civic hall, rounded central volume, mountain-and-flame crown',
    function='Fester Arbeitsplatz Rockos und Zentrum für Stadtstatistiken; kein Charakter im Mesh.',
    ports='Bürgerzugang vorn mittig; Servicezugang links; rückwärtiger Verwaltungsflügel.',
    materials='warm plaster, sage enamel, matte stone, oxidized copper'),
}

# Every line is an authored large, visible addition. Non-cultivation families
# retain previous functional capacity by compactly integrating earlier modules.
PLANS = {
'farm_processing': '''Reparierte Ein-Tank-Halle|one repaired hall with one round refinement tank
Zweiter großer Veredelungstank|second large refinement tank
Überdachtes breites Ernteportal|wide roofed harvest intake portal
Rückwärtiger Werkstattflügel|rear workshop wing
Dauerhafte verputzte Seitenhalle|permanent plaster side hall
Dritter hoher Veredelungstank|third tall refinement tank
Breiter geschlossener Ausgabetrakt|wide enclosed output wing
Solardach über der Veredelungshalle|solar roof across refinement hall
Großer emaillierter Reaktordom|large enamel refinement dome
Zweiter Reaktordom auf gemeinsamem Sockel|second refinement dome on shared foundation
Kräftige integrierte Rohrbrücke|thick integrated pipe bridge
Zweigeschossige Durchsatzhalle|two-storey processing hall
Rückwärtige automatisierte Hallensektion|rear automated hall section
Hoher runder Filterturm|tall rounded filter tower
Großer Ausgabepuffer als Zylindertank|large cylindrical output buffer
Zweites vollständiges Hallenobergeschoss|second complete upper hall storey
Vertikaler Veredelungsschacht mit grünem Glas|vertical refinement shaft with thick green glass
Gekoppelter zweiter Vertikalschacht|coupled second vertical refinement shaft
Breites Kupfer-Solardach|broad copper-framed solar roof
Kompakter oberer Kreislauftankring|compact upper recirculation tank ring
Gestufte obere Veredelungsterrasse|stepped upper processing terrace
Hoher gemeinsamer Prozesskern|tall shared processing core
Zwei abgerundete obere Hallenflügel|two rounded upper hall wings
Burning-Hills-Krone aus drei verbundenen Tankkuppeln|landmark crown of three connected tank domes''',
'farm_warehouse': '''Reparierte Scheune mit einem Versandtor|repaired barn with one dispatch door
Zweites breites Versandtor|second broad dispatch door
Tiefe überdachte Ladebucht|deep covered loading bay
Rückwärtiger Garagenflügel ohne Fahrzeuge|rear garage wing without vehicles
Erhöhte verputzte Hauptlagerhalle mit breitem durchgehendem Dachmonitor|raise the plaster main storage hall and add a broad continuous clerestory roof monitor above its barn ridge
Zweiter überdachter Dockblock|second covered dock block
Breiter runder Dispatch-Aufsatz|broad rounded dispatch loft
Zusammenhängendes Solardach|continuous solar roof
Hohe emaillierte Lagersektion|tall enamel storage section
Seitlicher gestapelter Dockblock|side stacked dock block
Großer rückwärtiger Hochlagerkern|large rear high-bay storage core
Zweites Lagergeschoss|second storage storey
Breiter automatisierter Versandtrakt|broad automated dispatch wing
Zweiter rückwärtiger Hochlagerkern|second rear high-bay storage core
Runde zentrale Dispatch-Brücke|rounded central dispatch bridge
Drittes Lagergeschoss|third storage storey
Hoher kompakter Logistikturm|tall compact logistics tower
Gekoppelter zweiter Logistikturm|coupled second logistics tower
Kupfergedeckte gemeinsame Ladehalle|copper-roofed shared loading hall
Vertikales geschlossenes Sortiermodul|vertical enclosed sorting module
Gestuftes urbanes Hochlagerdach|stepped metropolitan high-bay roof
Breite zentrale Versandrotunde|broad central dispatch rotunda
Gekoppelte obere Dock-Galerie|connected upper dock gallery
Markante doppelte Scheunengiebelkrone|landmark twin barn-gable crown''',
'city_warehouse': '''Reparierter Stadtblock mit zweigeteilter Ausgabe|repaired urban block with two distribution exits
Dritte breite Verteilbucht|third broad distribution bay
Tiefe zusammenhängende Lade-Arkade|deep continuous loading arcade
Rückwärtige Sammelhalle|rear consolidation hall
Dauerhafter abgerundeter Verwaltungsflügel|permanent rounded administration wing
Zweiter klarer Industrie-Ausgabetrakt|second distinct industrial output wing
Runde zentrale Verteilwarte|rounded central distribution control loft
Breites städtisches Solardach|broad urban solar roof
Große emaillierte Sortierhalle|large enamel sorting hall
Zweiter geschlossener Sortierflügel|second enclosed sorting wing
Hoher rückwärtiger Stadtspeicher|tall rear city storage block
Zweites zusammenhängendes Verteilgeschoss|second continuous distribution storey
Große mehrteilige Ausgangsarkade|large multi-bay distribution arcade
Runder vertikaler Sortierkern|rounded vertical sorting core
Breiter oberer Speicherflügel|broad upper storage wing
Drittes Verteilgeschoss|third distribution storey
Kupfergefasster hoher Versorgungsturm|tall copper-framed supply tower
Zweiter gekoppelter Versorgungsturm|second coupled supply tower
Grüngläserne obere Sortierhalle|thick green-glass upper sorting hall
Solarterrasse über den Ausgangsportalen|solar terrace above distribution portals
Gestaffelte städtische Logistikkrone|stepped urban logistics crown
Hohe zentrale Dispatch-Rotunde|tall central dispatch rotunda
Zwei integrierte obere Verteilbrücken|two integrated upper distribution bridges
Dreiteilige Versorgungsspitze mit runder Zentralhaube|three-part supply crown with rounded central hood''',
'coffee_shop': '''Reparierter Laden mit tiefem Servicefenster|repaired shop with recessed service window
Breite zweite Serviceöffnung|broad second service opening
Überdachte fest verbundene Kundenveranda|roofed attached customer veranda
Rückwärtiger Verkaufsflügel|rear retail wing
Neuer hoher runder Ecksaal hinter der unveränderten Servicefront|add a tall rounded rear corner customer hall with a distinct curved roof above the original service front
Zusätzliche breite Fensterfront|additional broad service-window frontage
Fest integrierte Galerie über der Veranda|integrated gallery above veranda
Großes salbeigrünes Solarmarkisendach|large sage solar-canopy roof
Runder emaillierter Versorgungserker|rounded enamel supply bay
Zweiter durchgehender Verkaufsflügel|second continuous retail wing
Breite gestützte Kundengalerie|broad supported customer gallery
Vollständiges zweites Verkaufsgeschoss|complete second retail storey
Große rückwärtige Servicehalle|large rear service hall
Kupfergedeckte obere Eckloggia|copper-roofed upper corner loggia
Zweites tiefes Verandagalerieband|second deep veranda-gallery band
Drittes kompaktes Verkaufsgeschoss|third compact retail storey
Hoher begrünter Eckkern|tall planted corner core
Gekoppelter oberer Verkaufsflügel|coupled upper retail wing
Große dick verglaste Wintergartenloggia|large thick-glazed conservatory loggia
Gestaffelte solargedeckte Kundenterrasse|stepped solar-roofed customer terrace
Breite oberste Rundgalerie|broad top circular gallery
Hoher kupfergedeckter Servicepavillon|tall copper-roofed service pavilion
Zwei kompakte obere Eckflügel|two compact upper corner wings
Unverwechselbare doppelte Blätterdachkrone|distinctive twin botanical-canopy crown''',
'residential_house': '''Reparierte Notunterkunft mit Community-Nische|repaired shelter with community mail niche
Zusätzliches breites Wohnmodul|additional broad dwelling module
Seitliche Erweiterung der bestehenden Eingangsloggia um zwei breite Rundbogenfelder|extend the existing entrance loggia sideways with two broad rounded bays; keep the front door fixed
Rückwärtiger Familienflügel|rear family wing
Großes bewohnbares Dachgeschoss mit breiter zentraler Gaube|add a large inhabited attic with a broad central dormer; the first complete upper floor comes at T08
Zweiter gestaffelter Wohnflügel|second stepped residential wing
Durchgehende breite Gemeinschaftsloggia|continuous broad community loggia
Erstes vollständiges Obergeschoss|first complete upper storey
Runder Community-Erker|rounded community bay
Zweites vollständiges Obergeschoss|second complete upper storey
Große integrierte Gartengalerie|large integrated garden gallery
Drittes vollständiges Obergeschoss|third complete upper storey
Breiter rückwärtiger Wohnblock|broad rear residential block
Viertes gestaffeltes Obergeschoss|fourth stepped upper storey
Zwei breite begrünte Gemeinschaftsloggien|two broad planted community loggias
Fünftes kompaktes Obergeschoss|fifth compact upper storey
Hoher vertikaler Gartenkern|tall vertical garden core
Gekoppelter zweiter Wohnturm|coupled second residential tower
Großes Solardach über den Wohntürmen|large solar roof over residential towers
Dick verglaste gemeinsame Wintergartenhalle|thick-glazed shared conservatory hall
Gestufte obere Wohngalerie|stepped upper residential gallery
Dritter kompakter rückwärtiger Wohnturm|third compact rear residential tower
Breite gemeinsame begrünte Dachterrasse|broad shared planted roof terrace
Bergförmige kupferne Wohndachkrone|mountain-shaped copper residential roof crown''',
'park': '''Reparierter kleiner Rundpavillon mit einem Beet|repaired small round pavilion with one planting bed
Zusätzliche breite erhöhte Pflanzterrasse hinter dem Pavillon|add one broad raised rear planting terrace behind the retained pavilion
Neue seitliche Sitzarkade mit drei massiven Rundbögen außerhalb des bisherigen Pavillondachs|add an attached side seating arcade with three thick rounded arches beyond the original pavilion roof
Breiter rückwärtiger Community-Anbau mit eigenem zweitem Runddach|add a broad attached rear community wing with a separate second rounded roof
Dauerhafter Rundweg mit Pflanzeinfassung|permanent circular path with planted edging
Erste große kompakte Baumkrone über der rückwärtigen Pflanzterrasse|add the first large compact tree canopy rising above the rear planting terrace
Breite gestützte Pergola|broad supported pergola
Große ruhige Brunnenmulde ohne Fontäne|large calm fountain basin without water jets
Breite runde Gartenrotunde auf der hinteren erhöhten Terrasse|add a broad attached round garden rotunda on the raised rear terrace with a separate roof
Zweite vollständige Pergola-Arkade|second complete pergola arcade
Breite leicht erhöhte Gartenterrasse|broad slightly raised garden terrace
Zweigeschossiger Community-Pavillon|two-storey community pavilion
Großes rückwärtiges Gartenhaus|large rear garden house
Zusätzliche obere Pflanzterrasse|additional upper planted terrace
Breite grüngläserne Orangerie|broad thick green-glass orangery
Drittes gestuftes Pavillongeschoss|third stepped pavilion storey
Vertikaler Gartenkern hinter dem Zugang|vertical garden core behind entrance
Zweiter gekoppelter Gartenkern|second coupled garden core
Große Kupfer-Solarpergola|large copper solar pergola
Breite gestützte Gartengalerie|broad supported garden gallery
Oberste städtische Baumterrasse|top metropolitan tree terrace
Hoher runder VIP-/Community-Pavillon|tall round community pavilion
Zwei gekoppelte Orangerieflügel|two coupled orangery wings
Zentrale botanische Kuppelkrone über dem Park|central botanical dome crown over park''',
'butter_factory': '''Reparierte Mischhalle mit zwei Rundtanks|repaired mixing hall with two round tanks
Großer dritter lebensmitteltauglicher Tank|large third food-grade tank
Breite geschlossene Zutatenannahme|broad enclosed ingredient intake
Rückwärtige Rührwerkhalle|rear mixing-machine hall
Große gerundete Mischhallenhaube über der bisherigen Hauptanlage|add a large rounded cream mixing-hall roof hood above the retained main processing plant
Zweiter großer Mischkessel|second large mixing vessel
Breite geschlossene Wartungs- und Prozessbrücke über der hinteren Rührwerkhalle|add a broad enclosed process-and-maintenance bridge above the rear mixing hall, above the original tank pipework
Breites Solardach über der Mischhalle|broad solar roof above mixing hall
Großer runder Butter-Puffertank|large rounded butter buffer tank
Zweiter hoher Mischkessel|second tall mixing vessel
Breiter sauberer Verpackungstrakt|broad clean packing wing
Zweites vollständiges Prozessgeschoss|complete second processing storey
Rückwärtige kompakte Tankhalle|rear compact tank hall
Hoher runder Hygienekern|tall rounded hygiene core
Großer gemeinsamer Ausgabesammler|large shared output collector
Drittes kompaktes Prozessgeschoss|third compact processing storey
Vertikaler grüngläserner Mischkern|vertical thick green-glass mixing core
Zweiter gekoppelter Mischkern|second coupled mixing core
Große kupfergefasste Solardecke|large copper-framed solar canopy
Oberer geschlossener Tankring|upper enclosed tank ring
Breite städtische Verarbeitungsterrasse|broad metropolitan processing terrace
Hohe gemeinsame Ausgaberotunde|tall shared output rotunda
Zwei obere emaillierte Mischkuppeln|two upper enamel mixing domes
Landmark-Krone aus verbundenen Butterkesseln|landmark crown of connected butter vessels''',
'supermarket': '''Reparierte Verkaufshalle mit goldener Zahnradplakette|repaired retail hall with one gold gear plaque
Zweiter breiter Eingangsflügel|second broad entrance wing
Neue durchgehende seitliche Einkaufsarkade mit drei massiven Bögen|add a continuous attached side shopping arcade with three thick rounded arches beyond the entrance hood
Rückwärtiger Versorgungstrakt|rear supply wing
Hohe breite zentrale Lichtgewölbehalle über der bestehenden Verkaufsmitte|add a tall broad central clerestory-vault hall above the existing retail center
Zweite breite Verkaufshalle|second broad retail hall
Große runde Eingangshaube|large rounded entrance hood
Zusammenhängendes solargedecktes Hallendach|continuous solar-roofed hall roof
Runder emaillierter Kühlversorgungskern|rounded enamel cold-supply core
Breiter zusätzlicher Sortimentsflügel|broad additional retail-range wing
Großer rückwärtiger Warenpuffer|large rear goods buffer
Zweites vollständiges Verkaufsgeschoss|complete second retail storey
Breite gestützte Kundengalerie|broad supported customer gallery
Zweiter hoher Versorgungskern|second tall supply core
Großer geschlossener Abholtrakt|large enclosed collection wing
Drittes kompaktes Verkaufsgeschoss|third compact retail storey
Vertikales begrüntes Versorgungsatrium|vertical planted supply atrium
Zweiter gekoppelter oberer Marktflügel|second coupled upper market wing
Grüngläserner Dachmarkt|thick green-glass rooftop market
Großes kupfergefasstes Solardach|large copper-framed solar roof
Gestufte städtische Einkaufsrotunde|stepped metropolitan shopping rotunda
Breite gemeinsame Versorgungsgalerie|broad shared supply gallery
Zwei obere kompakte Markthallen|two compact upper market halls
Große runde Marktdachkrone mit einem Goldzahnrad|large rounded market roof crown with one gold gear''',
'bakery': '''Reparierte Backstube mit einem Terrakottaofen|repaired bakehouse with one terracotta oven
Zweiter breiter Ofenbogen|second broad oven arch
Breite baulich verbundene Versandloggia hinter dem bestehenden Lieferfenster|add a broad attached delivery loggia extending rearward from the existing copper-capped output window
Rückwärtiger Teigküchenflügel|rear dough-kitchen wing
Großes neues Terrakotta-Tonnengewölbe über der bisherigen Backhalle|add a large new terracotta barrel vault over the retained bakehouse hall
Zweiter kräftiger kurzer Schornstein|second thick short chimney
Breite geschlossene Zutatenannahme|broad enclosed ingredient intake
Großes Solardach über der Backstube|large solar roof above bakehouse
Runder kupferner Teigbereitungskern|rounded copper dough-preparation core
Zweite vollständige Ofenhalle|second complete oven hall
Breiter sauberer Versandflügel|broad clean dispatch wing
Zweites vollständiges Backgeschoss|complete second baking storey
Rückwärtige automatisierte Teigküche|rear automated dough kitchen
Großer zusammengefasster Kaminkörper|large unified chimney body
Breite obere Liefergalerie|broad upper delivery gallery
Drittes kompaktes Backgeschoss|third compact baking storey
Hoher kupferner Wärmekreislaufkern|tall copper heat-recovery core
Zweiter gekoppelter oberer Ofenflügel|second coupled upper oven wing
Dick verglaste obere Teigküchenhalle|thick-glazed upper dough-kitchen hall
Große solargedeckte Backterrasse|large solar-roofed baking terrace
Breite städtische Ofenrotunde|broad metropolitan oven rotunda
Zwei hohe verbundene Kupferhauben|two tall connected copper hoods
Gestufte obere Produktionsgalerie|stepped upper production gallery
Markante Terrakotta-Kupfer-Krone aus Ofenbögen|landmark terracotta-copper crown of oven arches'''
}

def era(t):
    index = 0 if t == 0 else (1 if t <= 4 else 2 if t <= 8 else 3 if t <= 12 else 4 if t <= 16 else 5 if t <= 20 else 6 if t <= 23 else 7)
    return ERAS[index]

def plan(family):
    if family == 'town_hall':
        return [('Repariertes Rathaus mit Berg-/Flammenkrone','repaired civic hall with mountain-and-flame crown')]
    if family == 'cultivation_plot':
        result=[]
        for de,en in [('robuster Topf','robust planter pot'),('kleine Anbaulage','small cultivation bed'),('große Anbaulage','large cultivation bed'),('Mega-Anbaulage','mega cultivation layer'),('gekoppeltes Giga-Gewächshaussegment','coupled giga greenhouse segment')]:
            for n in range(1,5):
                result.append((f'{n} × {de}',f'exactly {n} {en}' + ('s' if n>1 else '')))
        result += [('Ein kompakter vertikaler Grow-Turm','exactly one compact vertical grow tower'),('Zwei gekoppelte Grow-Türme','exactly two coupled grow towers'),('Drei gekoppelte Grow-Türme','exactly three coupled grow towers'),('Vier Grow-Türme mit zentralem Biokuppel-Kern','exactly four grow towers around one central biodome core')]
        return result
    result=[tuple(line.split('|')) for line in PLANS[family].splitlines()]
    assert len(result)==24
    return result

def write_new(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists():
        path.write_text(text, encoding='utf-8', newline='\n')

def image_prompt(family, f, tier, t, de, en):
    x,z,h=f['size']
    material=era(t)[2] if t>=0 else f['materials']
    if family=='cultivation_plot':
        progression='The exact number and category above completely replace the previous cultivation configuration; never add the previous pots or beds as extra modules. Keep one connected asset; coupled units use a compact 2 by 2 arrangement. Plants have chunky clustered foliage, no isolated thin leaves or floating parts.'
    else:
        history=plan(family)[:max(1,t)] if t>0 else []
        progression='Preserve the T01 family DNA and the previously accepted tier. Integrate previous functional capacity compactly upward and rearward, inside the reserved parcel. Current cumulative architectural milestones: ' + '; '.join(v[1] for v in history) + '. No separate freestanding buildings: all wings and modules are structurally attached.'
    if t==0:
        progression='Derive from the accepted T01 anchor: preserve recognizable original silhouette and front. Show broad repairable damage, broken roof/planters and an attached surviving foundation, without sharp loose debris, violence or a scenic ground slab.'
    return f'''Use case: stylized-concept
Asset type: one final Meshy image-to-3D building reference for Weedtopia Inc / Burning Hills.
Subject: {f['subject']}, {tier}. Exact visible state: {en}.
Family DNA: {f['dna']}. Function: {f['subject']}.
Progression: {progression}
{image_dimension_note(family,t)}
Scale: maximum reserved parcel {x}m local X by {z}m local Z, maximum family height {h}m. This reserved envelope is NOT a visible base. See asset_spec.de.md for this tier's approximate occupied dimensions; render the whole current object, not an empty full-size plot.
Front and camera: local +Y up, front facing +Z, front stays on the lower-left visible face of the image, local +X side on lower-right. Camera at local (+X,+Y,+Z), about 35 degrees elevated, near orthographic 3/4 front view. Never mirror. Ground-center pivot intent; future growth is upward and toward local -Z. The entry and delivery zones stay on +Z.
Materials: {f['materials']}; epoch treatment: {material}. Broad hand-painted color areas and large restrained wear patches. Rounded/beveled silhouettes, thick reconstructable parts, modular retro technology, one or at most two eventual material slots. Green glass must be thick and legible. At most one dominant botanical or grinder motif.
Reference input roles: Image 1 Reclaimed Green Haven gives warm rebuilt shapes and readability; image 2 Herbal Atom Age gives rounded enamel machines and copper/brass; image 3 Burning Hills Afterglow gives hopeful worldbuilding only, NOT its dusk lighting, environment or people. Additional inputs, when present: the family's accepted T01 geometry anchor, then its immediately preceding accepted tier. Preserve their family identity, front and proportions while making the specified large upgrade visible. For T00 use only the intact T01 as extra geometry reference. For T01 or T_FIXED only the three style inputs are supplied.
Composition: exactly one single complete freestanding asset, centered and grounded; square 1024x1024 or larger, generous 10 percent clear margin all around. Plain solid very light warm-gray/off-white background. Neutral even studio light; one soft contact shadow only. All geometry entirely visible.
Technical intent: stylized game-ready smart-topology, clean edges, minimal geometry; no floating parts. Attached architectural planting is allowed; no loose props. A park may contain its intrinsic bounded planted parcel, never a surrounding scenic diorama.
Exclude: people, characters, vehicles, conveyor belts, surrounding scene, landscape, city, unrelated buildings, loose props, diorama ground, text, readable signs, numbers, logos, watermarks, UI, weapons, gore, smoking, smoke, particles, thin antennae, tangled cables, clipped or cropped geometry, baked dramatic shadows, glows, fog or dusk color casts.
'''

def main():
    ART.mkdir(parents=True,exist_ok=True)
    old_path=ART/'BUILDING_ASSET_MANIFEST.json'
    old=json.loads(old_path.read_text(encoding='utf-8-sig')) if old_path.exists() else {}
    existing={s['building_id']+'/'+s['tier_id']:s for s in old.get('slots',[])}
    slots=[]
    for family,f in FAMILIES.items():
        family_dir=ART/'Buildings'/family
        states=plan(family)
        x,z,maxh=f['size']
        family_text=f'''# {f['name']} – verbindliche Familien-DNA

Funktion: {f['function']}

Silhouette: {f['dna_de']}. Materialprioritäten: {f['materials']}.

Reservierte maximale Stellfläche: **{x} × {z} m**, maximale Höhe **{maxh} m**. Ein Unity-Meter entspricht einer Unity-Einheit. Die reservierte Fläche wird nicht als Podest gerendert. Das frühe Visual nutzt nur einen Teil der Fläche; zunehmende Kapazität wächst bevorzugt nach hinten und oben. Die in den Slots genannten Maße sind Richtwerte für die Art-Produktion, keine Gameplaywerte.

Front bleibt lokal **+Z**, oben **+Y**, Pivot am Bodenmittelpunkt der reservierten Parzelle. Die Vorderkante bleibt bei z=+{z/2:g}; ein kleineres Visual darf seinen Körper nach hinten von dieser Frontkante entwickeln. Im Bild erscheint die +Z-Front links unten, die +X-Seite rechts unten. Endgültiger geometrischer Pivot und Anschlüsse werden am Wrapper manuell geprüft.

Anschlüsse: {f['ports']} InteractionAnchor am vorderen Hauptzugang, WorkerAnchor seitlich des Servicebereichs, DeliveryInputAnchor/DeliveryOutputAnchor passend zu den beschriebenen Ports, VfxAnchor über dem Funktionskern. ColliderRoot bleibt leer bis zur sichtbaren vereinfachten Collider-QA. Fahrzeuge, Förderbänder und Worker bleiben eigenständige spätere Prefabs.

T01 ist der verbindliche Formanker. T00 zeigt seine reparierbare Vorgeschichte; danach wird T02 aus T01 weiterentwickelt, nicht aus T00. Jede folgende Stufe nutzt T01 plus ihren direkten akzeptierten Vorgänger. Bei Gebäuden bleiben frühere Funktionsvolumen integriert und dürfen kompakt gestapelt werden; keine 24 unabhängigen Anbauten. Beim Anbau ersetzt jede Kategorie die vorherige vollständig und hält die vorgegebene exakte Modulzahl ein.

## Ausbaumerkmale

'''
        family_text += family_dimension_note(family) + '\n\n'
        if family=='town_hall':
            family_text+='Nur T_FIXED: '+states[0][0]+'. Keine Ruine, keine Upgrade-Level. Das Showcase zeigt dieses eine feste Rathaus in der Testumgebung; T01/T12/T24 existieren für diese Familie nicht.\n'
        else:
            family_text+='\n'.join(f'- T{i:02d}: {v[0]}.' for i,v in enumerate(states,1))+'\n'
        write_new(family_dir/'family_spec.de.md',family_text)
        tiers=[(-1,'T_FIXED',0,5000)] if family=='town_hall' else [(0,'T00_RUIN',0,0)]+[(i,f'T{i:02d}_L{level:03d}',level,LEVELS[i]-1 if i<24 else 5000) for i,level in enumerate(LEVELS,1)]
        for t,tier,minimum,maximum in tiers:
            if t==0:
                de='Zerstörte Töpfe mit reparierbarem Bewässerungsfundament' if family=='cultivation_plot' else 'Reparierbare Ruine der ursprünglichen '+f['name']+'-Grundform'
                en='broken planters, compacted soil and repairable attached irrigation foundation' if family=='cultivation_plot' else 'repairable damaged original structure with broad roof gaps and surviving front'
            else: de,en=states[0 if t<0 else t-1]
            folder=family_dir/tier
            relative=f'Docs/ArtProduction/Buildings/{family}/{tier}'
            actual_factor = 1 if t<0 else (.24 if t==0 else min(.3+.7*t/24,1))
            if family=='cultivation_plot':
                heights=[.6,1.8,1.8,1.8,1.8,2,2,2,2,3,3,3,3,5,5,5,5,8,8,8,8,12,16,20,25]
                height=heights[t]
                occupied_x=(1.2 if t==1 else 2.8 if t==2 else 3.8 if t<=4 else round(x*actual_factor,1))
                occupied_z=(1.2 if t in (1,2) else 2.8 if t<=4 else round(z*actual_factor,1))
            else:
                height=production_height(family,t,maxh)
                occupied_x=round(x*actual_factor,1);occupied_z=round(z*actual_factor,1)
            faces=6000 if t==0 else (15000 if t in (-1,24) else int(round((6000+7000*max(t,1)/24)/250)*250))
            if family in ('cultivation_plot','park') and 0<t<17: faces=max(6000,faces-1500)
            mat=f['materials']
            primary=f"{f['subject']}, {tier}: {en}. {f['dna']}. {mat}. Stylized cozy rebuilt retro-future, broad painted wear. game-ready, smart-topology, clean edges, minimal geometry, single complete asset, centered, grounded, front facing +Z, Y up, ground-center pivot, 1-2 materials, no floating parts. No people, scene, landscape, text, logos, vehicles, loose props, diorama ground, cropped geometry, smoke, conveyor belts."
            assert 0<len(primary)<=800,(family,tier,len(primary))
            texture=f"{mat}. Warm off-white, sage and olive color blocks with restrained terracotta, mustard and copper accents. Broad stylized hand-painted wear, sparse softened edge discoloration; no fine scratch noise. PBR base color has neutral albedo without baked lighting, shadows, glow or AO. Roughness mostly matte; metalness only on exposed metal. Subtle normal texture, restrained ambient occlusion map kept separate. No text, logos, labels or symbols."
            assert len(texture)<=800
            settings={'primary_workflow':'image_to_3d','fallback_workflow':'text_to_3d','model_type':'smart-topology','model_type_availability':'Select only if present; current web UI not inspected.','topology':'triangles','target_faces':faces,'target_faces_reason_de':f'{faces} Faces für die lesbare Silhouette dieser Stufe; '+('niedriges Budget für Ruine oder einfache Module.' if faces<=8000 else 'höheres Budget für gestapelte Hallen, Kuppeln und rekonstruierbare Anschlüsse.'),'texture_resolution':2048,'pbr':True,'rigging':False,'animation':False,'recommended_export':'FBX with complete texture folder','unity_scale':'1 unit = 1 meter','unity_up_axis':'+Y','unity_forward_axis':'+Z','pivot_intent':'ground center'}
            spec=f'''# {f['name']} / {tier}

- Levelband: {minimum}–{maximum}; {'fester, nicht ausbaubarer Slot' if t<0 else 'Tutorial-Ausgangszustand' if t==0 else era(t)[1]}.
- Funktion: {f['function']}
- Dauerhafte Silhouette: {f['dna_de']}.
- Sichtbare Ausprägung / große Veränderung: **{de}**.
- Unterschied zum Vorgänger: {'keine Vorgängerstufe; repariertes dauerhaftes Landmark' if t<0 else 'beschädigte Version des intakten T01-Ankers; keine grausame Zerstörung' if t==0 else 'intakter erster Formanker nach der Tutorial-Ruine' if t==1 else 'gegenüber '+states[t-2][0]+' wird '+de+' klar lesbar; keine reine Farbänderung'}.
- Ungefähres belegtes Volumen: {occupied_x:g} × {occupied_z:g} × {height:g} m (X × Z × Y); reserviertes Maximalfeld {x} × {z} m, Familienhöhenlimit {maxh} m.
- Front: +Z, oben +Y. Der Eingang bleibt an der Vorderkante des reservierten Feldes; Wachstum nach -Z und +Y. Pivot-Absicht: Bodenmittelpunkt des festen Bauplatzes. Der Wrapper richtet das importierte Modell und den tatsächlichen Bodenbezug aus; kein Umbau des FBX.
- Anschlussports: {f['ports']} Sie sind kräftige Anschlussbereiche; Förderbänder/Fahrzeuge werden nicht eingebaut.
- Materialien: {mat}. Stilisierte große Abnutzungsflächen; maximal ein dominantes Blatt-/Grindermotiv. Neutraler PBR-Grundfarbwert, kein Beleuchtungs-Glow.
- Geometriebudget: {faces} Dreiecke als manueller Zielwert; ein Material bevorzugt, maximal zwei für notwendiges Glas.
- Bildabnahme: vollständiges Einzelasset, 1024 × 1024 oder größer, helle Studio-Freistellung, Front links unten, keine Menschen/Texte/Fahrzeuge. Bildexistenz ist keine visuelle Freigabe; aktueller Status steht in PROGRESS.json und der hashgebundenen QA.

{'Beim Anbau gilt die exakte Anzahl dieser Stufe. Frühere Topf-/Lagenkategorien werden ersetzt, nicht zusätzlich behalten. Die Begrenzung beziehungsweise das Bewässerungsfundament gehört zur Anlage; keine dekorative Bodenscheibe.' if family=='cultivation_plot' else 'Frühere Funktionskapazität bleibt als kompakt integrierte Baukörper erhalten. Höhere Stufen dürfen Module neu stapeln und nach hinten verdichten; Kernsilhouette, Front und Anschlussbereich bleiben gleich.'}
'''
            spec += '\n' + dimension_note(family,t) + '\n'
            write_new(folder/'meshy_prompt_en.txt',primary)
            write_new(folder/'texture_prompt_en.txt',texture)
            write_new(folder/'image_prompt_en.txt',image_prompt(family,f,tier,t,de,en))
            write_new(folder/'asset_spec.de.md',spec)
            write_new(folder/'meshy_settings.json',json.dumps(settings,ensure_ascii=False,indent=2)+'\n')
            slot={'building_id':family,'building_name_de':f['name'],'tier_id':tier,'min_level':minimum,'max_level':maximum,'era':'Fixed Landmark' if t<0 else era(t)[1],'visual_name_de':de,'visible_delta_from_previous':de,'footprint_m_x':x,'footprint_m_z':z,'height_m':height,'target_faces':faces,'driver_slot_delta':1 if family in ('farm_warehouse','city_warehouse') and t>=1 else 0,'reference_image':relative+'/mesh_input_3q.png','meshy_prompt_file':relative+'/meshy_prompt_en.txt','texture_prompt_file':relative+'/texture_prompt_en.txt','image_prompt_file':relative+'/image_prompt_en.txt','asset_spec_file':relative+'/asset_spec.de.md','meshy_settings_file':relative+'/meshy_settings.json','unity_incoming_folder':f'Assets/Weedtopia/Art/Buildings/Incoming/{family}/{tier}','unity_prefab_target':f'Assets/Weedtopia/Art/Buildings/Prefabs/{family}/WT_{family}_{tier}.prefab','status':'pending_image'}
            # Existing rows and companion files are authoritative on continuation.
            slot=existing.get(family+'/'+tier,slot)
            slots.append(slot)
            (ROOT/slot['unity_incoming_folder']).mkdir(parents=True,exist_ok=True)
            (ROOT/slot['unity_prefab_target']).parent.mkdir(parents=True,exist_ok=True)
        showcase=f'''Use case: stylized-concept. Create exactly one wide 1536x1024 or larger family showcase for {f['name']} / {f['subject']}. {'Show the single fixed repaired town hall only; there are no upgrade tiers.' if family=='town_hall' else 'Show T01, T12 and T24 together from left to right, three fully separated complete versions of the same family; follow the attached accepted images precisely.'} Small readable isometric Burning Hills test environment at blue hour; warm windows, gold work lights, subtle green grow lights, atmospheric depth. Blend the rebuilt soft plaster/wood readability of Reclaimed Green Haven with Herbal Atom Age rounded enamel machinery and Burning Hills Afterglow lighting. Retain {f['dna']}. No text, labels, people, real brands, weapons, vehicles, floating parts or clipped buildings. This is an art-direction showcase, not an image-to-3D input. Reference order: image1 Reclaimed Green Haven, image2 Burning Hills Afterglow, then {'T_FIXED' if family=='town_hall' else 'T01, T12 and T24'} final accepted asset images.\n'''
        write_new(family_dir/'family_showcase_prompt_en.txt',showcase)
    manifest={'schemaVersion':1,'project':'Weedtopia Inc / Burning Hills','path_base':'project_root','axis_convention':{'up':'+Y','front':'+Z','pivot':'reserved parcel ground center'},'driver_slot_delta_interpretation':'farm_warehouse and city_warehouse: T00=0; T01=1 initial available driver slot; every T02..T24 visible milestone adds 1. This is presentation metadata, not an implemented economy.','fixed_town_hall_showcase':'One T_FIXED asset in a small environment; no invented T01/T12/T24.','slots':slots}
    # Atomic replace of manifest; matrices are recoverable from this authoritative source.
    tmp=old_path.with_suffix('.json.tmp');tmp.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');tmp.replace(old_path)
    columns=list(slots[0])
    matrix=ART/'BUILDING_TIER_MATRIX.csv';temp=matrix.with_suffix('.csv.tmp')
    with temp.open('w',encoding='utf-8',newline='') as stream:
        writer=csv.DictWriter(stream,fieldnames=columns);writer.writeheader();writer.writerows(slots)
    temp.replace(matrix)
    (ROOT/'Assets/Weedtopia/Data/Buildings').mkdir(parents=True,exist_ok=True)
    print(json.dumps({'slots':len(slots),'families':len(FAMILIES),'companion_files':len(slots)*5,'max_meshy_prompt_characters':max(len((ROOT/s['meshy_prompt_file']).read_text(encoding='utf-8')) for s in slots)},indent=2))


if __name__=='__main__':
    from ingest_generated_image import production_lock
    with production_lock():
        main()

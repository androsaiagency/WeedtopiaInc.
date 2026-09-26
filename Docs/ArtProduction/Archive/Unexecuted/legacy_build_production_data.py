"""Create the complete local building-art specification set; never generate images.

Run from any directory with Python 3.12. Existing texts, accepted images, slot status,
and Unity metadata are preserved. This tool does not contact any external service.
"""
from __future__ import annotations

import csv
import io
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "Docs/ArtProduction"
STYLES = [f"Docs/ArtProduction/References/ChosenStyle/{name}.png" for name in (
    "Reclaimed_Green_Haven", "Herbal_Atom_Age", "Burning_Hills_Afterglow")]
LEVELS = [(0, 0), (1, 4), (5, 9), (10, 24), (25, 49), (50, 99), (100, 149),
          (150, 199), (200, 299), (300, 399), (400, 499), (500, 599), (600, 699),
          (700, 799), (800, 899), (900, 999), (1000, 1249), (1250, 1499),
          (1500, 1749), (1750, 1999), (2000, 2499), (2500, 2999), (3000, 3999),
          (4000, 4999), (5000, 5000)]
ERAS = ["Tutorial-Ruine"] + ["Survival-Reparatur"]*4 + ["Community-Aufbau"]*4 + \
       ["Kooperative Industrie"]*4 + ["District Industry"]*4 + ["Vertical Eco-Tech"]*4 + \
       ["Metropolitan Flagship"]*3 + ["Burning-Hills-Ikone"]

# Every row describes a distinct, function-specific structural milestone. The
# preceding accepted image supplies continuity; the row supplies the large delta.
# Fields: German visual name | German visible change | English geometry | height m.
ROWS = {
"cultivation_plot": """
Zerstörtes Bewässerungsfundament|Zerbrochene Töpfe sitzen im verdichteten Anbaufundament; die gebogene Wasserarmatur bleibt erkennbar.|broken pots embedded in a compact irrigation foundation, bent thick water inlet|1.2
Ein robuster Topf|Ein einzelner großer Terrakottatopf mit kompakter botanischer Krone steht am reparierten Wasseranschluss.|exactly one robust terracotta planter with a compact leafy crown and a thick curved irrigation inlet|1.8
Zwei robuste Töpfe|Ein zweiter gleich großer Topf ergänzt den ersten nach hinten; beide Kronen bleiben einzeln lesbar.|exactly two robust planters in a compact front-to-back pair with two separated leafy crowns|1.9
Drei robuste Töpfe|Ein dritter Topf schließt eine kompakte Dreiecksanordnung hinter dem unveränderten Frontanschluss.|exactly three robust planters in a triangular arrangement, three separated leafy crowns|2
Vier robuste Töpfe|Ein vierter Topf vervollständigt ein klar zählbares 2-mal-2-Raster mit breitem Bewässerungsbügel.|exactly four robust planters in a two-by-two grid and one broad irrigation arch|2.1
Eine kleine Anbaulage|Die vier Einzeltöpfe werden zu genau einer zusammenhängenden niedrigen Anbaulage unter einem Rundbügel gebündelt.|exactly one small continuous raised grow bed under a broad rounded irrigation hoop|2.5
Zwei kleine Anbaulagen|Eine zweite niedrige Anbaulage mit eigenem Rundbügel verdoppelt die nach hinten gestaffelte Silhouette.|exactly two small raised grow beds with two broad rounded irrigation hoops|2.7
Drei kleine Anbaulagen|Eine dritte Anbaulage ergänzt die hintere Reihe; drei breite Bügel geben einen eindeutigen Rhythmus.|exactly three small raised grow beds with three clearly separated broad hoops|2.9
Vier kleine Anbaulagen|Die vierte kleine Anbaulage schließt das kompakte Raster; vier eigenständige Bügel sind sichtbar.|exactly four small raised grow beds in a compact two-by-two grid, four broad hoops|3.1
Eine große Anbaulage|Eine durchgehende große Anbaulage ersetzt das Kleinraster durch ein hohes gerundetes Technikportal.|exactly one large grow-bed module with a tall rounded utility portal and integrated grow-light hood|4
Zwei große Anbaulagen|Ein zweites großes Modul sitzt hinter dem ersten und bildet zwei parallele hohe Technikportale.|exactly two large grow-bed modules, each with a distinct tall rounded utility portal|4.4
Drei große Anbaulagen|Ein drittes großes Modul ergänzt die höhere hintere Staffel; drei Portale bleiben separat ablesbar.|exactly three large grow-bed modules stepped toward the rear, three broad utility portals|4.8
Vier große Anbaulagen|Ein viertes großes Modul vervollständigt den Block mit vier hohen, klar getrennten Portalen.|exactly four large grow-bed modules in a compact two-by-two block, four tall utility portals|5.2
Eine Mega-Anbaulage|Ein zusammenhängendes mehrstöckiges Mega-Modul ersetzt die Großlagen; breite Pflanzterrassen erzeugen eine neue Höhenklasse.|exactly one mega grow module with three broad stacked planting terraces and thick rounded supports|7
Zwei Mega-Anbaulagen|Ein zweites Mega-Modul schafft zwei getrennte Terrassenstapel am gemeinsamen Versorgungssockel.|exactly two coupled mega grow modules with separately readable stacks of broad planting terraces|8
Drei Mega-Anbaulagen|Ein drittes Mega-Modul steigt hinter den beiden bestehenden Modulen auf und erweitert die Terrassenkrone.|exactly three coupled mega grow modules, rear module taller, three distinct terraced crowns|9
Vier Mega-Anbaulagen|Das vierte Mega-Modul schließt die erhöhte Pflanzgruppe mit vier deutlich abgesetzten Kronen.|exactly four coupled mega grow modules in a compact block, four separated terraced crowns|10
Ein Giga-Gewächshauskomplex|Eine einzige hohe gerundete Glashalle fasst die Anbaulagen zu einem Giga-Komplex mit massiven Kupferrippen zusammen.|exactly one giga greenhouse complex with one rounded green-glass vault and thick copper ribs|12
Zwei Giga-Segmente|Ein zweites gekoppeltes Glashallensegment erzeugt zwei große grüne Gewölbe hinter dem festen Frontanschluss.|exactly two coupled giga greenhouse segments with two separate rounded green-glass vaults|13
Drei Giga-Segmente|Ein drittes Gewächshaussegment erhöht den hinteren Abschluss; drei Gewölbe bleiben klar erkennbar.|exactly three coupled giga greenhouse segments, three clearly separated rounded glass vaults|14
Vier Giga-Segmente|Ein viertes gekoppeltes Gewölbe komplettiert das kompakte Giga-Raster ohne die Parzellengrenze zu überschreiten.|exactly four coupled giga greenhouse segments in a compact block, four rounded green-glass vaults|15
Ein vertikaler Grow-Turm|Der Giga-Block wird zu genau einem kompakten vertikalen Grow-Turm mit breiten bepflanzten Ringgeschossen verdichtet.|exactly one compact vertical grow tower with broad planted ring floors and a rounded crown|19
Zwei gekoppelte Grow-Türme|Ein zweiter Turm ergänzt den ersten nach hinten; ein massiver geschlossener Verbindungskörper koppelt beide.|exactly two coupled vertical grow towers with a solid enclosed rear connector and two distinct crowns|21
Drei gekoppelte Grow-Türme|Ein dritter Turm bildet eine gestaffelte Dreiergruppe; jede bepflanzte Turmkrone ist sichtbar.|exactly three coupled vertical grow towers in a triangular group with three separated planted crowns|23
Vier Grow-Türme mit Biokuppel|Ein vierter Grow-Turm und eine zentrale gerundete Burning-Hills-Biokuppel schaffen die finale Fünfteil-Silhouette.|exactly four vertical grow towers around one central Burning Hills biodome, four tower crowns and one central dome|26
""",
"farm_processing": """
Offene Veredelungsruine|Das halbe Tonnendach ist eingebrochen; ein runder beschädigter Tank und die zwei breiten Ernteports lassen die Funktion erkennen.|collapsed barrel-roof workshop with one dented round tank and two thick harvest ports|2.8
Handveredelungshütte|Ein kleines repariertes Tonnendach und ein seitlicher Rundtank bilden die erste vollständige Veredelungseinheit.|small barrel-roof refining hut with one round side tank, left harvest inlet and right output port|3.5
Zweiter Veredelungstank|Ein zweiter großer Rundtank hinter dem ersten schafft einen deutlich zweigipfligen Tankabschluss.|add a second full-size round refining tank behind the first, keeping the barrel-roof core|4
Überdachte Ernteannahme|Ein breites gerundetes Annahmedach überspannt den linken Port und verbreitert die vordere Funktionssilhouette.|add a broad rounded harvest receiving canopy over the fixed left inlet|4.2
Hintere Trockenkammer|Eine hohe geschlossene Trockenkammer steigt am hinteren Dachende auf und macht den Betrieb tiefer.|add a tall enclosed rear drying chamber with a wide louvered crown|5
Gemauerte Veredelungshalle|Die Hütte erhält eine deutlich höhere verputzte Hallenschale mit durchgehendem Tonnendach und sichtbarem Tankfenster.|replace the hut shell with a taller plaster barrel-vault hall around the retained round tank core|5.5
Abgesetztes Tankpodest|Ein massives erhöhtes Tankpodest setzt die zwei Rundtanks über die bisherige Traufe.|raise the two round refining tanks on a broad solid rear service podium|6.2
Großer Filterturm|Ein dicker gerundeter Filterturm ergänzt die hintere linke Hallenecke als neue Funktionsspitze.|add one thick rounded filtration tower at the rear-left hall corner|7
Solare Veredelungsspange|Ein breites zusammenhängendes Solardach verbindet Halle und Tankpodest zu einer neuen gestuften Dachlinie.|add a broad integrated stepped solar roof connecting the hall and tank podium|7.3
Kooperative Doppelhalle|Eine zweite gerundete Hallensektion wächst nach hinten; zwei Tonnendächer sind aus der Distanz sichtbar.|add a second rear barrel-vault processing hall, two distinct rounded hall roofs|8
Dritter Hochleistungstank|Ein dritter großer emaillierter Tank ergänzt die hintere Tankreihe und überragt die neue Doppelhalle.|add a third large enamel refining tank rising above the twin hall roofs|8.7
Messing-Wärmetauscher|Ein breiter geschlossener Wärmetauscherblock mit zwei dicken Messingbögen sitzt über dem Ausgangsbereich.|add a broad enclosed heat-exchanger block with two thick brass pipe arches above the output bay|9
Hohe Kontrollkanzel|Eine große verglaste Kontrollkanzel krönt die Naht der zwei Hallen und schafft eine zentrale Dachspitze.|add a large rounded green-glass control cabin above the junction of the twin processing halls|10
Veredelungs-Obergeschoss|Ein vollwertiges Obergeschoss hebt das zentrale Tonnendach über die alte Halle; Ports und Eingangsfront bleiben unten.|add a complete upper processing floor under the raised central barrel roof|11.5
Doppelte Reaktorsilos|Zwei große vertikale Reaktorsilos ersetzen die niedrige hintere Tankgruppe und bilden eine klare Doppelsilhouette.|replace the rear low tank bank with two tall rounded refining silos|12.8
Hochliegende Transferhalle|Eine geschlossene breite Transferhalle verbindet die Silos oberhalb des Bestands und bildet einen massiven Querabschluss.|add a broad enclosed upper transfer hall joining the twin rear silos|13.5
Terrassiertes Wartungsdeck|Ein kräftiges gestuftes Wartungsdeck mit geschlossener Brüstung setzt die rückwärtige Technik eine Ebene höher.|add a solid stepped rear maintenance deck supporting the upper refining machinery|14
Kupferne Destillationshaube|Eine große rund gewölbte Kupferhaube überspannt das obere Prozesszentrum als neue dominante Dachform.|add a large rounded copper process canopy over the upper refining center|15.5
Vertikale Gewächsfilter|Ein hohes dickwandiges Grün-Glas-Filtermodul ergänzt die rückwärtige Fassade als breiter vertikaler Körper.|add one broad tall green-glass botanical filtration module against the rear facade|16.8
Zweite Prozesskrone|Ein zweiter hoher Prozessaufbau entsteht hinter der Kupferhaube und erzeugt eine gestaffelte Doppelkrone.|add a second tall rounded rear processing crown behind the main copper canopy|18
Solarer Hallenfächer|Ein großer massiver Solardachfächer legt sich über die obere Transferhalle und verändert den gesamten oberen Umriss.|add a broad solid fan-shaped solar canopy over the upper transfer hall|19
Metropolitaner Veredelungsturm|Ein kompakter Zentralbau wächst zu einem hohen Turm mit drei großflächigen Technikringen.|raise the center into a tall compact refining tower with three broad utility rings|21
Zweite Turmspange|Ein kräftiger rückwärtiger Turmflügel verbindet die obere Tanktechnik zu einem neuen H-förmigen Abschluss.|add a substantial rear tower wing with a broad enclosed upper bridge|22
Vierfach-Tankkrone|Vier große gerundete Tankköpfe prägen den höchsten Dachrand und machen die Endleistung lesbar.|add four large rounded tank heads as a distinct crown around the upper processing center|24
Burning-Hills-Veredelungskrone|Eine breite flammenförmig gestufte Kupfer-Prozesshaube überragt die vier Tankköpfe als einmaliges finales Wahrzeichen.|add one monumental stepped copper process crown shaped like a rounded mountain flame above four tank heads|26
""",
"farm_warehouse": """
Eingestürzte Versandscheune|Die breite Scheunenfront und ein verschüttetes Ladetor bleiben unter dem eingebrochenen Bogenfirst erkennbar.|collapsed broad barn warehouse with one blocked loading doorway and a broken rounded ridge|3
Reparierte Versandscheune|Eine kompakte Scheune mit breitem Bogenfirst und einem großen frontalen Versandtor nimmt den Betrieb auf.|compact repaired barn warehouse with a broad curved ridge and one large front dispatch doorway|4
Zweite Ladebucht|Eine zweite breite Ladebucht ergänzt die Scheunenfront seitlich innerhalb des reservierten Bauplatzes.|add a second broad loading bay beside the first on the fixed front facade|4.2
Überdachtes Versanddeck|Ein tiefes massives Versandvordach schützt beide Tore und bildet eine auffällige niedrige Frontspange.|add a deep solid dispatch canopy spanning the two front loading doorways|4.4
Rückwärtiger Garagenblock|Ein breiter kompakter Garagenblock wächst hinter der Scheune und hebt den rückwärtigen Dachabschluss an.|add a broad compact rear garage block with a higher rounded roof|5
Verputzte Lagerhalle|Die Scheune erhält eine höhere dauerhafte Hallenschale mit charakteristischem erhaltenem Bogenfirst.|raise the barn into a taller plaster storage hall retaining its broad curved ridge|5.8
Seitliche Hochregaltasche|Ein hoher geschlossener Lagerkörper sitzt hinter der linken Frontkante und zeigt mehr Stauraum über dem Dach.|add a tall enclosed rear-left storage pocket rising above the main barn roof|6.5
Versand-Leitkanzel|Eine große runde Dispatch-Kanzel steigt über dem festen Versandvordach auf und organisiert die Ladefront.|add a large rounded dispatch control cabin above the fixed loading canopy|7
Solarer Garagenriegel|Der hintere Garagenblock erhält ein breites geknicktes Solardach und eine deutlich höhere Attika.|add a broad bent solar roof and raised parapet over the rear garage block|7.3
Kooperative Doppelhalle|Eine zweite lange Lagerhalle wächst nach hinten und erzeugt zwei parallel ablesbare Bogenfirste.|add a second rear storage hall with a parallel broad curved ridge|8
Erhöhter Umschlagtrakt|Ein massiver Umschlagtrakt hebt den hinteren Warenfluss auf eine zweite sichtbare Ebene.|add a substantial elevated rear transfer block on solid enclosed supports|9
Doppeltes Dispatch-Portal|Zwei dicke emaillierte Torbügel rahmen die bestehende Front und bilden eine neue kräftige Eingangskontur.|add two thick enamel dispatch arches framing the retained front loading bays|9.2
Hohes Flottenmagazin|Ein geschlossener mehrstöckiger Garagenkörper steigt am hinteren Hallenende auf und zeigt organisierte Flottenkapazität.|add a tall enclosed multilevel rear fleet garage with broad recessed openings|10
Lager-Obergeschoss|Ein vollständiges Obergeschoss sitzt über der ersten Halle und verdoppelt deren Fassadenhöhe bei gleicher Front.|add a complete upper storage floor above the original barn hall|11.5
Zweiter Garagenturm|Ein zweiter gedrungener Garagenturm ergänzt das Flottenmagazin als klar getrennte hintere Doppelform.|add a second squat rear garage tower beside the existing fleet magazine|12.5
Geschlossene Versandbrücke|Eine breite geschlossene Logistikbrücke verbindet die beiden hinteren Garagentürme über dem Hallendach.|add a broad enclosed transfer bridge joining the twin rear garage towers|13
Hochliegende Sortierhalle|Eine neue hochliegende Sortierhalle sitzt zentral zwischen den Türmen und trägt ein breites rundes Dach.|add an elevated central sorting hall with a broad rounded roof between the garage towers|14
Begrüntes Logistikgeschoss|Ein terrassiertes Obergeschoss mit massiven integrierten Pflanztrögen tritt hinter der Front auf.|add a stepped upper logistics floor with thick integrated planted parapets|15
Kupferner Dispatch-Turm|Ein breiter kupferverkleideter Dispatch-Turm ergänzt die Rückseite als höchste Form über den Garagen.|add a broad copper-clad rear dispatch tower above the garage massing|16.5
Vertikales Lagermagazin|Ein hohes gerundetes Lagermagazin ragt hinter der Scheunenhalle auf und bündelt die Warenbewegung.|add a tall rounded vertical storage magazine behind the retained barn hall|18
Solarer Flottenschirm|Ein großer zusammenhängender Solarschirm überspannt die oberen Garagenkörper ohne freitragende dünne Stäbe.|add a large solid solar canopy over the upper fleet garage blocks|19
Metropolitanes Versandtor|Ein massiver mehrgeschossiger Rundbogen verbindet Lager und Dispatch-Turm und prägt eine weithin lesbare Torsilhouette.|add a monumental multilevel rounded dispatch arch linking storage and control tower|21
Zweites Hochmagazin|Ein zweites hohes Lagermagazin wächst an der Rückseite zu einer gestaffelten Doppelturmsilhouette.|add a second tall rear storage magazine forming a staggered twin-tower silhouette|22
Gestufte Logistikkrone|Ein breiter dreistufiger Dispatch-Aufbau verbindet die hohen Magazine zu einem kräftigen oberen Abschluss.|add a broad three-tier dispatch crown connecting the upper storage magazines|24
Burning-Hills-Farmterminal|Ein großer weich gerundeter Berg-Flammen-Bogen krönt die Scheunenfirst-DNA über den verbundenen Versandtürmen.|add one monumental rounded mountain-flame ridge crown above the connected farm dispatch towers|26
""",
"city_warehouse": """
Beschädigter Stadthof|Ein gebrochener gerundeter Eckbau und zwei verschlossene Verteilports lassen die frühere städtische Logistik erkennen.|damaged rounded corner depot with two blocked front distribution ports and a broken rear hall|3
Kompakter Stadtverteiler|Ein gerundeter Eckbau ordnet einen linken Shop-Ausgang und einen rechten Industrie-Ausgang an der festen Front.|compact rounded urban corner depot, left shop distribution port and right industry distribution port|4.2
Zweiter Verteilflügel|Ein kurzer zweiter Hallenflügel wächst hinter dem rechten Ausgang und erzeugt einen klaren L-Grundriss.|add a short rear distribution wing behind the right outlet, creating a clear L-shaped mass|4.8
Durchgehendes Dockvordach|Ein breites abgerundetes Vordach verbindet die zwei funktional getrennten Frontports zu einer Stadtlogistikfront.|add one broad rounded dock canopy spanning the two distinct front distribution outlets|5
Geschlossener Flottenhof|Ein hoher rückwärtiger Garagenriegel ergänzt den Eckbau und schließt die erkennbare urbane Hofform.|add a tall enclosed rear garage wing closing the compact urban depot courtyard|5.6
Städtische Verteilhalle|Eine dauerhafte verputzte Hallenschale hebt den L-Bau an; die gerundete Ecknase bleibt der Orientierungspunkt.|raise the L-shaped depot into a permanent plaster hall retaining its rounded corner nose|6.2
Erhöhter Shop-Trakt|Ein breiter Aufbau über dem linken Verteilflügel macht die Shopversorgung als eigenen Gebäudearm sichtbar.|add a broad upper block over the left shop distribution wing|7
Industrie-Sortierturm|Ein gedrungener gerundeter Sortierturm steigt hinter dem rechten Ausgang auf und differenziert die Industrieversorgung.|add a squat rounded sorting tower behind the right industry distribution outlet|7.8
Solarer Stadthofbogen|Ein kräftiges bogenförmiges Solardach überspannt die hintere Hofkante und verbindet die beiden Funktionsarme.|add a broad arched solar roof spanning the rear edge between the two distribution wings|8.2
Doppeltes Verteilgeschoss|Eine breite zweite Verteilebene wächst zentral nach hinten und hat zwei klar getrennte emaillierte Modulhauben.|add a broad rear upper distribution floor with two distinct rounded enamel module hoods|9
Hohes Flottendepot|Ein mehrstöckiges geschlossenes Garagendepot erhebt sich hinter dem Industriearm als neue vertikale Masse.|add a tall enclosed multilevel fleet depot behind the industry wing|10
Shop-Umschlagkanzel|Eine große runde Umschlagkanzel krönt den linken Verteilarm und balanciert die Flottenhöhe rechts.|add a large rounded transfer control cabin above the left shop distribution wing|10.5
Urbaner Sortierkern|Ein breiter zentraler Sortierkern mit gestapelten abgerundeten Geschossen wächst zwischen den Funktionsarmen.|add a broad central sorting core with stacked rounded floors between the distribution wings|11.5
Durchgehendes Lagergeschoss|Ein vollständiges zusätzliches Lagergeschoss verbindet beide Flügel hinter der unveränderten Erdgeschossfront.|add a complete upper storage floor connecting both rear distribution wings|13
Zweiter Flottenturm|Ein zweiter geschlossener Garagenturm ergänzt den ersten rückwärtig und vergrößert die obere Flottensilhouette.|add a second enclosed rear fleet tower beside the first|14
Erhöhte Verteilerbrücke|Eine breite geschlossene Verteilerbrücke koppelt die hinteren Türme über dem zentralen Sortierkern.|add a broad enclosed distribution bridge linking the rear fleet towers over the sorting core|14.8
Doppeltes Leitzentrum|Zwei große runde Leitkabinen wachsen auf der Verteilerbrücke und kennzeichnen getrennte Lieferzweige.|add two large rounded routing cabins on the upper distribution bridge|15.5
Begrünter Depotsockel|Ein massives terrassiertes Logistikgeschoss mit integrierten Pflanzbändern hebt den hinteren Depotaufbau an.|add a substantial stepped upper depot floor with integrated planted bands|16.5
Kupferner Verteilerkopf|Ein hoher kupferverkleideter Zentralaufbau mit breiter runder Haube wird zum neuen Orientierungspunkt.|add a tall copper-clad central routing head with a broad rounded hood|18
Vertikale Stadtspeicher|Zwei schlanke aber massive abgerundete Speicherblöcke wachsen hinter dem Leitzentrum nach oben.|add two solid rounded vertical storage blocks behind the routing center|19.5
Solarer Verteilerschirm|Ein breiter mehrstufiger Solarschirm überspannt die oberen Speicherblöcke und ordnet deren Silhouette.|add a broad stepped solar canopy over the upper urban storage blocks|20.5
Metropolitaner Logistikknoten|Ein monumentaler zentraler Rundturm ragt über die Lieferflügel und bündelt drei große geschlossene Verteilhauben.|raise a monumental rounded central routing tower with three large enclosed distribution hoods|23
Hochliegender Shop-Speicher|Ein weiterer hoher Speicherkörper ergänzt den linken oberen Flügel und erzeugt eine asymmetrische Skyline.|add a tall upper storage block over the left shop wing, creating an asymmetric skyline|24
Gestufte Routingkrone|Drei breite versetzte Routinggeschosse staffeln den zentralen Turmkopf und geben ihm eine klare Fernwirkung.|add three broad stepped routing floors to the central tower crown|25.5
Burning-Hills-Stadtterminal|Eine massive gerundete Berg-Flammen-Dachkrone fasst die drei Verteilhauben zum finalen Stadtlogistikwahrzeichen zusammen.|add one monumental rounded mountain-flame roof crown above the three urban routing hoods|28
""",
"coffee_shop": """
Verlassene Verkaufsruine|Ein gebogenes zerbrochenes Markisendach und das leere große Servicefenster erhalten die einladende Front als reparierbare Spur.|abandoned shop shell with a broken rounded awning and one large empty service window|2.5
Repariertes Servicehäuschen|Ein kleines warmes Häuschen mit tiefem Rundvordach und einem großen Servicefenster bildet den wiederkehrenden Kern.|small welcoming shop with a deep rounded awning and one oversized service window|3.2
Zweites Servicefenster|Ein zweites großes Servicefenster in einem kurzen seitlichen Verkaufsanbau verbreitert die erkennbare Bedienfront.|add a short side sales bay with a second oversized service window|3.5
Überdachte Empfangsnische|Eine breite baulich integrierte Empfangsnische mit massivem Runddach verlängert den Verkaufsbereich nach hinten.|add a broad integrated rear reception alcove under a solid rounded roof|3.8
Hohe Vorratskammer|Eine hohe geschlossene Vorratskammer wächst hinter der Verkaufsfront und hebt die rückwärtige Dachlinie.|add a tall enclosed rear stockroom above the low welcoming sales front|4.5
Verputztes Quartierscafé|Eine höhere dauerhafte Putzfassade mit einem großen gerundeten Eckfenster ersetzt die improvisierte Verkaufshülle.|raise the shop into a permanent plaster storefront with a large rounded corner window|5
Doppeltes Verkaufsdach|Ein zweites breites gebogenes Verkaufsdach ergänzt den hinteren Kundenraum und ergibt zwei sanfte Dachwellen.|add a second broad curved roof over the rear customer hall|5.6
Community-Oberstube|Eine volle obere Community-Stube sitzt über dem hinteren Verkaufsraum und erweitert die Nutzfläche sichtbar.|add a full upper community room above the rear sales hall|6.5
Solare Eingangsgalerie|Eine breite massive Eingangsgalerie mit integriertem Solardach vergrößert die feste Frontsilhouette.|add a broad solid entrance gallery with an integrated solar roof|6.8
Kooperative Verkaufshalle|Eine große gerundete hintere Verkaufshalle schafft deutlich mehr Innenkapazität bei erhaltenem Servicehäuschen vorne.|add a large rounded rear sales hall behind the retained front service-house core|7.5
Zweiter Servicepavillon|Ein baulich verbundener seitlicher Servicepavillon unter einer eigenen Rundhaube erweitert die Bedienkapazität.|add an attached side service pavilion under a separate rounded hood|8
Hochliegender Wintergarten|Ein breiter dickwandiger grüner Wintergarten wächst auf dem hinteren Dach und ist als große Glaskrone lesbar.|add a broad thick-framed green-glass winter garden above the rear sales hall|9
Rundes Quartiersfoyer|Ein hohes gerundetes Foyer verbindet die Servicepavillons und schafft eine neue mittige Frontspitze.|add a tall rounded central foyer joining the service pavilions|10
Zweites Kundengeschoss|Ein vollständiges neues Kundengeschoss über der hinteren Halle macht den Shop zu einem mehrstöckigen Quartiershaus.|add a complete upper customer floor above the rear sales hall|11
Gestaffelter Serviceflügel|Ein neuer nach hinten aufgestaffelter Verkaufsflügel erweitert das Volumen und besitzt ein eigenes breites Vordach.|add a stepped rear sales wing with its own broad solid awning|12
Große Loggia|Eine tiefe baulich geschlossene Loggia mit massiven Bögen prägt das neue obere Kundengeschoss.|add a deep upper loggia with thick rounded structural arches|12.5
Runder Gemeinschaftssaal|Ein großer runder Gemeinschaftssaal krönt die rückwärtige Mitte und überragt die Loggia.|add a large rounded community hall above the rear center|14
Begrüntes Cafégeschoss|Ein weiteres terrassiertes Kundengeschoss mit integrierten Pflanztrögen wächst hinter dem ursprünglichen Servicekern.|add a stepped upper customer floor with thick integrated planted parapets|15
Kupferne Wintergartenhaube|Eine breite gebogene Kupferhaube mit dicken Glasstreifen krönt den Gemeinschaftssaal als neues Dachmotiv.|add a broad curved copper roof with thick green-glass bands over the community hall|16
Vertikaler Community-Flügel|Ein kompakter hoher Community-Flügel erhebt sich hinten und trägt gestapelte große Rundfenster.|add a tall compact rear community wing with stacked oversized rounded windows|18
Solare Dachterrasse|Eine große integrierte Dachterrasse mit massivem gerundetem Solarschirm schafft einen neuen horizontalen oberen Abschluss.|add a broad integrated roof terrace under a solid rounded solar canopy|19
Metropolitanes Caféhaus|Ein hohes zentrales Caféhaus mit gestuften Rundgeschossen bündelt die Serviceflügel zu einem städtischen Wahrzeichen.|raise a tall central cafe house with broad stepped rounded floors above the sales wings|21
Zweiter Wintergartenturm|Ein zweiter hoher dickwandiger Wintergartenaufbau ergänzt die hintere Skyline als getrennte grüne Glaskrone.|add a second tall thick-framed green-glass winter garden crown at the rear|22
Dreifache Empfangskrone|Drei breite übereinander gestaffelte Empfangshauben steigern die markante obere Silhouette ohne zusätzliche Bodenausdehnung.|add three broad stepped reception canopies to the upper central silhouette|24
Burning-Hills-Cafékrone|Eine große warm kupferne, weich bergförmige Gewölbekrone über dem zentralen Gemeinschaftssaal markiert die finale Stufe.|add one grand warm-copper rounded mountain vault above the central community hall|26
""",
"residential_house": """
Beschädigte Wohnschale|Ein gerissener warmer Giebel und ein leeres Rundtürportal stehen über der beschädigten Wohnbasis.|damaged warm plaster gable shell with an empty rounded doorway and collapsed rear rooms|2.8
Repariertes Giebelhaus|Ein kleines weiches Giebelhaus mit großer Rundtür und fest eingebauter Community-Postnische bildet die Wohn-DNA.|small soft-gabled home with an oversized rounded door and an integrated community mail alcove|3.5
Zweites Wohnzimmermodul|Ein kurzer hinterer Wohnanbau mit eigenem Giebeldach erweitert die bewohnbare Kubatur deutlich.|add a short rear dwelling module with its own soft gabled roof|4
Breiter Seitenflügel|Ein breiter baulich angebundener Seitenflügel ergänzt die Hausform um einen zweiten klaren Wohnkörper.|add a broad attached side dwelling wing with a second clearly readable residential volume|4.3
Hohes Wohndach|Ein großes ausgebautes Dachgeschoss hebt den ursprünglichen Giebel und trägt ein breites integriertes Dachfenster.|add a large inhabited attic under a raised soft gable with one broad inset window|5.2
Verputztes Gemeinschaftshaus|Eine volle zweite Wohnebene über dem hinteren Anbau schafft ein dauerhaftes verputztes Gemeinschaftshaus.|add a complete second rear dwelling floor with warm permanent plaster walls|6
Tiefe Gemeinschaftsloggia|Eine breite integrierte Loggia mit massiven gerundeten Bögen erweitert die obere Wohnfassade.|add a broad integrated upper community loggia with thick rounded arches|6.5
Zweiter Wohngiebel|Ein zweiter hoher Wohngiebel wächst hinten und erzeugt zwei klar getrennte bewohnbare Dachkörper.|add a second tall rear residential gable, two distinct inhabited roof masses|7.5
Solarer Wohnriegel|Ein breiter neuer Wohnriegel mit geschlossenem Solardach verbindet die beiden Giebel nach hinten.|add a broad rear dwelling block with an integrated solid solar roof|8
Kooperativer Wohnhof|Ein hoher rückwärtiger Wohnflügel schließt die Hausgruppe zu einem kompakten begrünten Wohnhof.|add a tall rear residential wing closing a compact planted courtyard|9
Zusätzliches Loggiageschoss|Ein vollständiges neues Wohngeschoss mit breiten geschlossenen Loggien wächst über der Hofkante.|add a complete upper residential floor with broad solid-framed loggias|10
Community-Treppenhaus|Ein breiter gerundeter Treppenhausturm erhebt sich hinter der festen Eingangstür und organisiert die Wohnmasse.|add a broad rounded community stair tower behind the fixed entrance|11
Großer Gemeinschaftsgiebel|Ein breiter bewohnbarer Gemeinschaftsgiebel verbindet die zwei hinteren Wohnblöcke auf Dachhöhe.|add a broad inhabited community gable joining the two rear residential blocks|12
Gestaffelte Wohnterrassen|Ein neues vollwertiges Wohngeschoss mit zwei großen Rücksprüngen bildet großzügige integrierte Terrassen.|add a complete stepped residential floor with two broad integrated terraces|13.5
Zweiter Erschließungsturm|Ein zweiter massiver Treppenhausturm ergänzt den hinteren Wohnflügel und erzeugt eine klare Doppelspitze.|add a second solid rear stair tower beside the residential wing|14.5
Verbindendes Wohnhausband|Ein breites geschlossenes Wohnband verbindet die Erschließungstürme über dem Hof ohne dünne Stege.|add a broad enclosed inhabited bridge block joining the two stair towers|15.5
Oberer Familienflügel|Ein großer zusätzlicher Familienwohnflügel sitzt über der hinteren Hofkante und endet in einem weichen Giebel.|add a large upper family dwelling wing with a soft gable along the rear courtyard edge|17
Vertikales Gartenhaus|Ein kompaktes hohes Gartenwohnmodul mit massiven bepflanzten Geschossbändern wächst aus dem Hinterhaus.|add a tall compact garden residence with thick planted floor bands above the rear block|19
Großer Wohnwintergarten|Ein breiter dickwandiger grüner Gemeinschaftswintergarten ergänzt die obere Dachlandschaft.|add a broad thick-framed green-glass community winter garden at roof level|20
Zweiter Gartenturm|Ein zweiter gestaffelter Gartenwohnturm ergänzt den ersten und bildet eine klare begrünte Doppelsilhouette.|add a second stepped garden dwelling tower with broad integrated planted ledges|22
Solarer Wohndachbogen|Ein massiver gerundeter Solardachbogen verbindet die beiden Wohntürme als neuer großer oberer Abschluss.|add a broad solid rounded solar roof arch linking the two dwelling towers|23
Metropolitaner Wohnberg|Ein hoher zentraler Wohnkörper mit drei großen Terrassenstufen verdichtet die Gruppe zum begrünten Wohnberg.|raise a tall central residential mass with three broad planted terrace steps|26
Rückwärtiger Gemeinschaftsturm|Ein weiterer hoher Gemeinschaftswohnflügel wächst hinter dem Wohnberg und trägt eine weiche Giebelkrone.|add a tall rear community dwelling wing with a soft gabled crown|28
Dreifache Gartenkrone|Drei große bewohnte Dachpavillons mit integrierten Grünbändern machen die oberste Wohnebene weithin lesbar.|add three large inhabited roof pavilions with thick integrated planted bands|30
Burning-Hills-Wohngipfel|Ein breiter kupfergrüner Gemeinschaftsgiebel in weicher Berg-Flammen-Form krönt die Wohnterrassen als finale Skyline.|add one grand copper-green rounded mountain-flame community gable above the residential terraces|33
""",
"park": """
Zerstörte Parkparzelle|Die klar begrenzte Parzelle enthält verdichtete Pflanzfelder und einen beschädigten runden Community-Pavillonsockel.|bounded ruined park parcel with compacted planting beds and one broken round community pavilion base|1.2
Erster Gemeinschaftsgarten|Ein niedriger ovaler Pflanztrog und ein kleiner fest angebundener Rundpavillon bilden die erste Erholungsparzelle.|one bounded park asset with an oval integrated planted bed and a small attached round community pavilion|2.8
Zweites Pflanzband|Ein zweites großes eingebautes Pflanzband legt sich hinter das erste und vergrößert die grüne Massensilhouette.|add a second broad integrated rear planting band behind the first oval bed|3
Große Schattenscheibe|Ein massives gerundetes Schattendach erweitert den kleinen Community-Pavillon nach hinten.|add a large solid rounded shade roof extending the attached community pavilion rearward|3.4
Erhöhte Garteninsel|Eine breite integrierte bepflanzte Gartenterrasse hebt die hintere Parzellenhälfte um eine sichtbare Ebene an.|add one broad integrated raised garden terrace at the rear of the bounded parcel|3.8
Dauerhafter Parkpavillon|Ein größerer verputzter Rundpavillon mit massiver Kuppel ersetzt die improvisierte Community-Überdachung.|replace the small shelter with a larger plaster round community pavilion under a solid dome|4.3
Zweite Gartenterrasse|Eine zweite breite Pflanzterrasse staffelt die hintere Gartenzone klar sichtbar nach oben.|add a second broad integrated planted terrace stepped above the rear garden zone|4.8
Runder Wassergarten|Ein großes baulich eingefasstes rundes Wasserbecken mit kompakter Pflanzeninsel ergänzt die Gartenmitte.|add a large integrated round water garden basin with a compact central planted island|5
Solarer Pavillonbogen|Ein breiter massiver Solarbogen krönt den Community-Pavillon und gibt der Parkfront einen neuen oberen Umriss.|add a broad solid solar roof arch above the retained community pavilion|5.5
Kooperativer Wintergarten|Ein dickwandiger grüner Glaspavillon wächst hinter dem Rundpavillon als neuer großer Gartenkörper.|add a broad thick-framed green-glass winter garden behind the round community pavilion|6.5
Hohe Pflanzrotunde|Eine breite zylindrische Pflanzterrasse erhebt sich in der hinteren Parzellenmitte über die vorhandenen Beete.|add a broad tall cylindrical planted terrace at the rear center of the bounded parcel|7
Zweiter Community-Saal|Ein zweiter baulich verbundener Rundsaal ergänzt den Parkpavillon nach hinten und schafft eine doppelte Kuppelsilhouette.|add a second attached round community hall with a separate solid dome|7.5
Gestufte Gartenpromenade|Eine breite baulich integrierte Promenadenterrasse verbindet Wintergarten und Pflanzrotunde auf einer erhöhten Ebene.|add a broad solid raised garden promenade linking the winter garden and planted rotunda|8
Zweistöckiger Gartenpavillon|Ein vollständiges zweites Pavillongeschoss hebt die Community-Kuppel über den bisherigen Garten.|add a complete upper community pavilion floor beneath the raised dome|9
Doppelte Pflanzrotunde|Eine zweite hohe bepflanzte Rotunde ergänzt die hintere Gartenzone und bildet eine klare grüne Doppelform.|add a second tall planted rotunda at the rear of the bounded garden parcel|9.5
Geschlossener Gartenbogen|Ein massiver begrünter Gartenbogen verbindet die beiden Rotunden mit einem breiten bepflanzten Rücken.|add a thick solid planted garden arch joining the twin rear rotundas|10
Erhöhter VIP-Pavillon|Ein großer zusätzlicher runder Community-Pavillon sitzt auf der hinteren Gartenterrasse als gut lesbarer Treffpunkt.|add a large raised round community pavilion on the rear garden terrace|11
Vertikaler Terrassengarten|Ein kompakter hoher Gartenkörper mit drei großen bepflanzten Ringterrassen wächst hinter dem ursprünglichen Pavillon.|add a tall compact garden tower with three broad planted ring terraces at the rear|13
Grüne Gartenkuppel|Eine große dickwandige grüne Glaskuppel überspannt die obere Wintergartenzone als neue Hauptform.|add a large thick-ribbed green-glass dome over the upper winter garden zone|14
Zweiter Terrassengarten|Ein zweiter gestaffelter Gartenturm ergänzt den ersten hinten und schafft zwei eigenständige Pflanzenkronen.|add a second stepped garden tower with a separate broad planted crown|15
Solarer Parkschirm|Ein breiter massiver Solarschirm schützt die obere Community-Terrasse und verbindet die grünen Hochpunkte.|add a broad solid solar canopy over the upper community terrace between the garden towers|16
Metropolitaner Gartenberg|Ein zentraler Gartenkörper mit vier breiten bepflanzten Stufen bündelt die Parkanlage zu einem kompakten Landmark.|raise a central garden mass with four broad planted steps within the bounded parcel|18
Hochliegende Community-Rotunde|Eine große obere Community-Rotunde mit geschlossenem Runddach ergänzt die hintere Gartenkrone.|add a large upper community rotunda with a solid rounded roof at the rear crown|19
Dreifacher Biogarten|Drei große dickwandige Glashauben staffeln den oberen Garten und machen die botanische Vielfalt strukturell sichtbar.|add three large thick-framed green-glass garden hoods along the upper terraces|21
Burning-Hills-Hoffnungsgarten|Eine breite zentrale Biokuppel mit gerundeter Berg-Flammen-Rippe verbindet die drei Glashauben zu einem finalen Parkwahrzeichen.|add one broad central biodome with a thick rounded mountain-flame rib above the three garden hoods|23
""",
"butter_factory": """
Beschädigte Mischerei|Eine aufgebrochene Rundhalle mit einem eingedrückten sauberen Mischtank und zwei breiten Lebensmittelports bleibt reparierbar.|damaged round-roof food workshop with one dented sealed mixing tank and two broad food transfer ports|3
Kleine Butter-Mischerei|Eine kompakte Rundhalle mit einem bauchigen versiegelten Mischtank und breitem Bäckerei-Ausgang startet die Verarbeitung.|compact round-roof butter workshop with one bulbous sealed mixing tank and a broad bakery supply outlet|4
Zweiter Mischtank|Ein zweiter großer runder Mischtank hinter dem ersten verdoppelt die klar lesbare Mischsilhouette.|add a second large round sealed mixing tank behind the first|4.5
Breite Kühlkammer|Eine breite geschlossene Kühlkammer wächst hinten an die kleine Mischhalle und besitzt eine dicke gerundete Tür.|add a broad enclosed rear cold-storage chamber with a thick rounded door|5
Hochliegender Rührkopf|Ein großer geschlossener Rührkopf sitzt über der Mischhalle und schafft eine neue runde Dachspitze.|add one large enclosed rounded mixer drive head above the workshop roof|5.7
Hygienische Produktionshalle|Eine höhere dauerhafte cremefarbene Produktionshalle umschließt die Mischtanks und besitzt einen breiten grünen Sichtstreifen.|raise a permanent cream plaster production hall around the tanks with a broad green-glass inspection band|6.3
Großer Pasteurisiertank|Ein hoher runder Pasteurisiertank wächst hinter der Mischhalle und hebt die technische Silhouette an.|add a tall rounded sealed pasteurization tank behind the mixing hall|7
Doppelter Kühlblock|Ein zweiter großer geschlossener Kühlblock ergänzt die Rückseite als deutlich abgesetzte Hallenmasse.|add a second large enclosed rear cooling block beside the first|7.5
Solarer Hygienebogen|Ein breiter integrierter Solarbogen überspannt die obere Mischhalle und verbindet Tank- und Kühleinheit.|add a broad integrated solar roof arch over the mixing hall between tank and cooling blocks|8
Kooperative Doppel-Mischhalle|Eine zweite gerundete Mischhalle wächst nach hinten und zeigt zwei große geschlossene Rührkopfhauben.|add a second rear rounded mixing hall with a second large enclosed mixer hood|9
Dritter Großmischtank|Ein dritter großer emaillierter Mischtank ergänzt die hintere Tankgruppe und ist über den Hallendächern sichtbar.|add a third large enamel sealed mixing tank rising above the rear hall roofs|9.8
Breiter Wärmetauscher|Ein großer gerundeter Lebensmittel-Wärmetauscher mit zwei dicken Messingrohren ergänzt das obere Ausgangsmodul.|add a broad rounded food heat-exchanger block with two thick brass pipes above the output module|10.5
Hohe Hygienekanzel|Eine große dickwandige grüne Kontrollkanzel krönt die beiden Mischhallen als neuer zentraler Aufbau.|add a large thick-framed green-glass quality-control cabin above the twin mixing halls|11
Zweites Prozessgeschoss|Ein vollständiges zusätzliches Prozessgeschoss hebt die runden Rührköpfe deutlich über die ursprüngliche Halle.|add a complete upper food-processing floor beneath the raised round mixer heads|12.5
Gekoppelte Buttersilos|Zwei hohe bauchige Lebensmittelsilos ersetzen die niedrige hintere Tankgruppe und erzeugen eine klare Doppelsilhouette.|replace the rear low tank bank with two tall bulbous sealed food silos|14
Geschlossene Hygienebrücke|Eine breite geschlossene Prozessbrücke verbindet die beiden Silos als massiver oberer Querabschluss.|add a broad enclosed hygienic process bridge joining the twin food silos|14.5
Hochliegendes Kühllager|Ein großes gerundetes Kühllager wächst hinter der oberen Mischhalle und erweitert die Rückseitenhöhe.|add a large rounded upper cold-storage block behind the mixing hall|15.5
Kupferne Mischkrone|Eine breite kupferverkleidete Rührkopfkrone überspannt das obere Prozesszentrum und bewahrt die weiche Tank-DNA.|add a broad copper-clad rounded mixer crown above the upper processing center|17
Vertikale Kühlkaskade|Ein hoher geschlossener Kühlkörper mit drei großen gerundeten Kammersegmenten wächst an der Rückseite.|add a tall enclosed rear cooling tower with three broad rounded chamber segments|18.5
Zweiter Prozesskopf|Ein zweiter hoher gerundeter Prozesskopf ergänzt die Mischkrone hinten und bildet eine technische Doppelform.|add a second tall rounded rear process head behind the copper mixer crown|20
Solarer Lebensmittelschirm|Ein großer massiver Solarschirm verbindet die oberen Kühl- und Mischkörper ohne schwebende Einzelteile.|add a large solid solar canopy joining the upper cooling and mixing blocks|21
Metropolitane Buttermolkerei|Ein breiter vertikaler Mischkern mit drei großen bauchigen Prozessringen prägt die neue Landmark-Silhouette.|raise a broad vertical mixing core with three large bulbous sealed process rings|23
Zweites Hochkühllager|Ein weiterer hoher Kühlblock ergänzt die hintere Skyline und setzt einen abgestuften cremefarbenen Akzent.|add a second tall rear cold-storage block with a stepped cream-colored silhouette|24
Vierfache Mischkuppel|Vier große gerundete Mischhauben gruppieren sich um den oberen Prozesskern als unverwechselbare Funktionskrone.|add four large rounded sealed mixing domes around the upper process core|26
Burning-Hills-Butterkrone|Eine breite creme-kupferne Berg-Flammen-Prozesshaube überragt die vier Mischkuppeln als finales sauberes Lebensmittelwahrzeichen.|add one grand cream-and-copper rounded mountain-flame process hood above four mixing domes|28
""",
"supermarket": """
Eingestürzte Markthalle|Ein gebrochener flacher Rundbogenfirst und eine beschädigte breite Schaufensterfront zeigen den früheren Versorgungsort.|collapsed market hall with a broken low rounded ridge and a damaged broad shopfront|3
Kleine Versorgungshalle|Eine niedrige breite Markthalle mit gerundetem Eingangsbogen und einem kleinen integrierten goldenen Zahnradsymbol beginnt den Verkauf.|low broad market hall with a rounded entrance arch and one small integrated golden gear emblem|4
Zweite Verkaufshalle|Ein kurzer zweiter Hallenflügel wächst nach hinten und besitzt einen eigenen breiten Dachbogen.|add a short rear sales wing with its own broad rounded roof arch|4.5
Breites Abholfenster|Ein großer seitlicher Abholbereich mit breitem geschlossenem Vordach ergänzt die feste Verkaufsfront.|add a broad attached pickup bay with a solid wide canopy beside the fixed entrance|4.8
Hoher Vorratsriegel|Ein hoher geschlossener Vorratsriegel wächst hinter der Verkaufshalle und hebt den rückwärtigen Abschluss.|add a tall enclosed rear stockroom block above the low sales hall|5.5
Dauerhafter Quartiersmarkt|Eine höhere verputzte Markthalle mit großer gerundeter Fensterfront ersetzt die improvisierte Hülle.|raise the market into a taller permanent plaster hall with a broad rounded shopfront|6
Zweite Versorgungsbucht|Eine zweite breite baulich integrierte Versorgungsbucht ergänzt den hinteren Warenannahmebereich.|add a second broad integrated rear goods-receiving bay|6.5
Große Kuppelhalle|Eine breite flache Kuppel überspannt die zentrale Verkaufsfläche und macht deren größere Kapazität sichtbar.|add a broad shallow dome over the central sales hall|7.5
Solarer Marktbogen|Ein massiver gerundeter Solarbogen wächst über der hinteren Hallenkante und verbindet die Versorgungstrakte.|add a broad solid solar roof arch over the rear market hall edge|8
Kooperative Doppelhalle|Eine zweite große gerundete Verkaufshalle erweitert den Markt nach hinten zu zwei klar ablesbaren Hallendächern.|add a second large rounded rear sales hall, two distinct broad market roofs|9
Hohes Frischemagazin|Ein großer geschlossener Kühl- und Frischespeicher erhebt sich hinter dem Warenannahmebereich.|add a large tall enclosed rear fresh-food storage block above the receiving area|10
Breite Versorgungsgalerie|Eine geschlossene erhöhte Versorgungsgalerie verbindet die zwei Hallen und bildet einen kräftigen Querabschluss.|add a broad enclosed upper supply gallery joining the two market halls|10.5
Runde Marktkanzel|Eine große gerundete zentrale Marktkanzel sitzt oberhalb des Eingangs und trägt das einzige kleine Zahnradsymbol.|add a large rounded central market control cabin above the entrance, retaining one small golden gear emblem|11
Zweites Verkaufsgeschoss|Ein komplettes zweites Verkaufsgeschoss über der zentralen Halle macht die wachsende Kundenkapazität strukturell sichtbar.|add a complete second sales floor above the central market hall|12.5
Rückwärtiger Versorgungsturm|Ein breiter gerundeter Versorgungsturm wächst am hinteren Hallenabschluss über das zweite Geschoss hinaus.|add a broad rounded rear supply tower rising above the second sales floor|14
Zweites Hochmagazin|Ein zweiter großer geschlossener Vorratskörper ergänzt den Versorgungsturm seitlich zu einer abgestuften Rückseite.|add a second large upper stockroom block beside the rear supply tower|14.8
Oberes Quartiersforum|Ein großer runder Verkaufs- und Versorgungsraum krönt die obere Marktmitte mit breitem gerundetem Dach.|add a large rounded upper neighborhood market hall with a broad curved roof|16
Begrüntes Verkaufsgeschoss|Ein weiteres terrassiertes Verkaufsgeschoss mit integrierten Pflanzbändern wächst über der hinteren Marktzone.|add a stepped upper sales floor with thick integrated planted bands above the rear market|17.5
Kupferner Frischegarten|Ein breiter dickwandiger grüner Glaskörper unter einer Kupferhaube ergänzt die obere Versorgungszone.|add a broad thick-framed green-glass fresh-market hall under a copper hood at roof level|19
Vertikales Versorgungsmagazin|Ein hoher gerundeter geschlossener Speicher wächst hinter dem Frischegarten als neue vertikale Hauptform.|add a tall rounded enclosed supply magazine behind the upper fresh-market hall|21
Solarer Marktschirm|Ein breiter massiver Solarschirm spannt über dem Quartiersforum eine neue zusammenhängende Dachform.|add a broad solid solar canopy over the upper neighborhood market forum|22
Metropolitanes Versorgungshaus|Ein hoher zentraler Verkaufskörper mit drei breiten gestuften Rundgeschossen macht den Markt zum Stadtwahrzeichen.|raise a tall central market building with three broad stepped rounded sales floors|24
Zweiter Forumsturm|Ein zweiter hoher Marktflügel wächst hinten und trägt eine eigenständige breite gerundete Dachhaube.|add a second tall rear market wing with a separate broad rounded roof hood|25.5
Dreifache Versorgungskrone|Drei große gestaffelte Vorratshauben krönen die obere Marktzone und verdeutlichen die organisierte Versorgung.|add three large stepped stockroom hoods around the upper market center|27
Burning-Hills-Marktgipfel|Eine monumentale sanfte Berg-Flammen-Dachwelle krönt die Verkaufshallen; das kleine goldene Zahnrad bleibt der einzige Grinder-Akzent.|add one monumental rounded mountain-flame roof wave above the market halls, retaining the single small golden gear|29
""",
"bakery": """
Beschädigte Backstube|Ein gebrochener warmer Ofenbogen und ein kurzer beschädigter Terrakotta-Schornstein zeigen die reparierbare Backstuben-DNA.|damaged bakery shell with a broken broad oven arch and a short broken terracotta chimney|3
Kleine Ofenbackstube|Eine kompakte warme Backstube mit großem Ofenbogen, kurzem massivem Kamin und Lieferfenster startet die Produktion.|compact warm bakery with a broad oven arch, one short thick chimney and a delivery window|4
Zweiter Ofenblock|Ein zweiter großer baulich integrierter Ofenblock hinter dem ersten schafft zwei klar getrennte runde Ofenrücken.|add a second large integrated rear oven block, two distinct rounded oven backs|4.5
Breite Lieferloggia|Eine tiefe baulich integrierte Lieferloggia mit dickem Runddach erweitert das bestehende Ausgabe-Fenster.|add a deep integrated delivery loggia with a thick rounded canopy over the fixed output window|4.8
Hohe Teigkammer|Eine hohe geschlossene Teigkammer wächst am hinteren Ofenblock und hebt die hintere Dachlinie.|add a tall enclosed rear dough-preparation chamber above the oven blocks|5.5
Dauerhafte Backhalle|Eine höhere verputzte Backhalle mit großem Terrakotta-Tonnendach fasst die zwei Ofenblöcke zusammen.|raise a taller plaster bakehouse around the ovens under one broad terracotta barrel roof|6
Zweiter kurzer Kamin|Ein zweiter dicker kurzer Terrakotta-Kamin ergänzt den ersten als sichtbar getrennte Doppelsilhouette ohne Rauch.|add a second thick short terracotta chimney, two distinct smokeless chimney forms|6.8
Großes Gärhaus|Ein breites geschlossenes Gärhaus mit gerundetem Dach wächst hinter der Backhalle als eigener Produktionskörper.|add a broad enclosed rear proofing hall with a rounded roof|7.4
Solarer Ofenbogen|Ein großer massiver Solarbogen sitzt über dem Gärhaus und verbindet dessen Dach mit der Backhalle.|add a broad solid solar roof arch over the proofing hall and rear bakehouse edge|8
Kooperative Doppelbackhalle|Eine zweite gerundete Backhallensektion wächst nach hinten und trägt einen eigenen breiten Ofenrücken.|add a second rear rounded bakehouse hall with its own broad oven-back roof|9
Dritter Großofen|Ein dritter großer integrierter Ofenkörper ergänzt die obere hintere Produktion und hebt einen neuen runden Ofenrücken heraus.|add a third large integrated oven body with a distinct rounded rear roof mass|9.8
Kupferner Wärmerücklauf|Ein breiter geschlossener Wärmerückgewinnungsblock mit zwei dicken Kupferbögen verbindet die Ofenhalle oberhalb des Lieferbereichs.|add a broad enclosed heat-recovery block with two thick copper arches above the delivery bay|10.5
Hohe Backmeisterkanzel|Eine große gerundete Kontrollkanzel mit dickem grünem Glas krönt die beiden Backhallen als neuer Aufbau.|add a large rounded thick-framed green-glass baking control cabin above the twin halls|11
Zweites Backgeschoss|Ein komplettes zusätzliches Backgeschoss hebt das Terrakotta-Tonnendach über die ursprüngliche Hallenstruktur.|add a complete upper baking floor beneath the raised terracotta barrel roof|12.5
Doppeltes Gärsilo|Zwei hohe gerundete geschlossene Gärkammern ersetzen den niedrigen hinteren Gärraum und erzeugen eine neue Doppelform.|replace the low rear proofing hall with two tall rounded enclosed proofing chambers|14
Geschlossene Backbrücke|Eine breite geschlossene Produktionsbrücke verbindet die beiden Gärkammern oberhalb der Hauptbackhalle.|add a broad enclosed baking production bridge joining the twin proofing chambers|14.7
Hochliegendes Kühlschiff|Eine große gerundete Kühlhalle wächst hinter dem oberen Backgeschoss und erweitert die Produktionssilhouette.|add a large rounded upper cooling hall behind the baking floor|16
Kupferne Ofenkrone|Eine breite rund gewölbte Kupferhaube fasst die oberen Ofenkörper zusammen und erzeugt ein starkes neues Dachmotiv.|add a broad rounded copper oven canopy over the upper baking center|17.5
Vertikales Gärhaus|Ein hoher geschlossener Gärkörper mit drei großen runden Kammersegmenten erhebt sich hinter der Ofenkrone.|add a tall enclosed rear proofing tower with three broad rounded chamber segments|19
Zweiter Backturm|Ein zweiter hoher gerundeter Backaufbau mit dickem integriertem Kamin ergänzt die rückwärtige Skyline.|add a second tall rounded rear baking tower with one thick integrated smokeless chimney|20.5
Solarer Backhallenschirm|Ein großer massiver Solarschirm überspannt die oberen Produktionskörper und schließt die neue Dachlandschaft.|add a large solid solar canopy over the upper baking and cooling blocks|22
Metropolitane Ofenmanufaktur|Ein hoher zentraler Backkern mit drei großen Terrakotta-Ofenbögen bildet eine markante vertikale Landmark-Silhouette.|raise a tall central bakehouse core with three broad stacked terracotta oven arches|24
Zweites Hochkühlhaus|Ein weiterer hoher gerundeter Kühlkörper ergänzt die hintere Produktionsgruppe als abgestufter warmer Putzbau.|add a second tall rounded rear cooling block with warm plaster walls|25.5
Dreifache Kupferkuppel|Drei große kupferne Ofenhauben krönen die obere Backzone und verstärken die erkennbare Produktionsleistung.|add three large rounded copper oven domes around the upper baking center|27
Burning-Hills-Backkrone|Eine monumentale weiche Berg-Flammen-Ofenkrone aus Terrakotta und Kupfer überragt die drei Hauben als finales Backwahrzeichen.|add one monumental rounded mountain-flame oven crown in terracotta and copper above three oven domes|29
""",
"town_hall": """
Repariertes Rathaus von Burning Hills|Ein festes repariertes Rathaus mit gerundeter Freitreppe, breitem Ratsflügel und Berg-Flammen-Dachkrone bildet das dauerhafte Stadtzentrum.|one repaired civic town hall with a rounded entry stair, broad council wing and a monumental rounded mountain-flame roof crown|16
"""
}

# Family identity remains stable throughout all tiers. Meter values reserve the
# maximum placement area; tier-specific volumes occupy only a centered subset.
FAMILIES = {
"cultivation_plot": ("Anbauplatz", "Ernteanbau und Übergabe an ein später separates Förderband", "kompakte botanische Modulgruppe mit dickem gebogenem Bewässerungsanschluss vorne links", "compact botanical modules with a thick curved front-left irrigation inlet", 14, 14, "Terrakotta, wiederverwendetes Holz, Salbeiemaille; später Kupfer und dickes Grünglas", "terracotta, reclaimed wood, sage enamel, thick green glass, copper accents", "vorn links Wasser; vorn rechts Ernteausgang; mittig Pflegezugang", 6000),
"farm_processing": ("Farm-Veredelung", "Ernteannahme, Veredelung und Ausgabe an das spätere separate Lager-Förderband", "breites weiches Tonnendach, runde Tanks rechts und unveränderte breite Ernteports", "broad soft barrel roof, rounded right-side tanks and wide harvest ports", 18, 16, "Warmer Putz, Salbeiemaille, wiederverwendetes Holz; Messingrohre und grüne Glasstreifen", "warm plaster, sage enamel, reclaimed wood, brass pipes, thick green glass", "vorn links Ernteannahme; vorn rechts veredelte Ausgabe; Mitte Wartungszugang", 7500),
"farm_warehouse": ("Farm-Lager", "Annahme vom Farmgebäude, Lagerung und Versand zum Stadt-Lager", "ländliche breite Scheune mit weich gebogenem First und tiefem Versandvordach", "broad rural barn with a soft curved ridge and deep front dispatch canopy", 20, 18, "Recyceltes Holz, warmer Putz, olivfarbenes Wellblech und matte Betonsockel; Messingdetails", "reclaimed wood, warm plaster, olive corrugated metal, matte concrete, brass", "vorn links Förderbandannahme; vorn rechts Versanddock; Mitte Disposition", 8000),
"city_warehouse": ("Stadt-Lager", "Annahme vom Farm-Lager, Verteilung an Coffee-Shops und Industrie", "urbaner L-Bau mit gerundeter Ecknase und zwei klar getrennten Verteilflügeln", "urban L-shaped depot with a rounded corner nose and two distinct routing wings", 20, 18, "Warmer Putz, matte Betonformen, salbeigrüne Emaille, Messing; später Kupfer", "warm plaster, matte concrete, sage enamel, brass, copper accents", "vorn links Shopausgabe; vorn rechts Industrieausgabe; hinten mittig Lieferannahme", 8000),
"coffee_shop": ("Coffee-Shop", "Verkauf an Bewohner und architektonisch erkennbare Kundenkapazität", "einladendes Rundvordach, übergroßes Servicefenster und weiches Verkaufsdach", "welcoming rounded awning, oversized service window and soft sales roof", 14, 14, "Warmer Putz, Terrakotta, recyceltes Holz, gedämpftes Türkis; sparsam Messing und grünes Glas", "warm plaster, terracotta, reclaimed wood, muted turquoise, brass accents", "Frontmitte Service und Kundenzugang; vorn links Anlieferung; vorn rechts Ausgabeanker", 7000),
"residential_house": ("Wohnhaus", "Wohnkapazität; Community-Postnische deutet Worker-Karten ohne UI oder Zahlen an", "weicher warmer Giebel, große Rundtür und dauerhaft eingebaute Community-Postnische", "soft warm gable, oversized rounded door and integrated community mail alcove", 16, 16, "Warmer Putz, Terrakotta, recyceltes Holz, salbeigrüne Fenster; später Kupfer und Grünglas", "warm plaster, terracotta, reclaimed wood, sage window frames, copper accents", "Frontmitte Haustür; vorn links Serviceannahme; vorn rechts Community-Postanker", 7500),
"park": ("Park", "Erholung und Community-/VIP-Quests als Architektur eines wiederkehrenden Pavillons", "klar begrenzte ovale Pflanzterrassen und fest angebundener runder Community-Pavillon", "bounded oval planted terraces and an attached round community pavilion", 16, 16, "Matte Steinränder, warmer Putz, recyceltes Holz, große ruhige Grüntöne; später Kupferglas", "matte stone, warm plaster, reclaimed wood, broad muted foliage greens, copper", "Frontmitte Parkzugang; vorn links Pflegeanschluss; vorn rechts Community-Pavillonanker", 6000),
"butter_factory": ("Butterfabrik", "Saubere Lebensmittelverarbeitung und Versorgung der späteren separaten Bäckereien", "weiche Rundhalle mit bauchigen versiegelten Mischtanks und klaren Lebensmittelports", "soft round-roof hall with bulbous sealed mixing tanks and broad food ports", 18, 18, "Cremeputz, lebensmitteltauglich wirkende salbeigrüne Emaille, matter Beton und Messing", "cream plaster, sage enamel, matte concrete, clean brass, restrained copper", "vorn links Rohstoffannahme; vorn rechts Bäckereiversorgung; Mitte Hygienewartung", 8000),
"supermarket": ("Supermarkt", "Verkaufs- und Versorgungskapazität; ein kleines goldenes Zahnrad deutet den Grinder-Bonus an", "breite niedrige Markthalle mit Rundbogen-Eingang und genau einem kleinen goldenen Zahnrad", "broad low market hall with a rounded entrance arch and one small golden gear", 20, 18, "Warmer Putz, salbeigrüne Emaille, matter Beton; Senfgold nur am kleinen Grinder-Motiv", "warm plaster, sage enamel, matte concrete, one mustard-gold gear accent", "Frontmitte Kundeneingang; vorn links Warenannahme; vorn rechts Abholausgabe", 8000),
"bakery": ("Bäckerei", "Backproduktion, Teigvorbereitung und Lieferschnittstelle zur Stadt", "großer warmer Ofenbogen, Terrakotta-Tonnendach und kurze dicke rauchfreie Kamine", "broad warm oven arch, terracotta barrel roof and short thick smokeless chimneys", 16, 16, "Warmer Putz, Terrakotta, matte Kupferhauben und recyceltes Holz; zurückhaltendes Grünglas", "warm plaster, terracotta, matte copper oven hoods, reclaimed wood, green glass", "vorn links Rohstoffannahme; vorn rechts Lieferfenster; Mitte Backstubenzugang", 7500),
"town_hall": ("Rathaus", "Festes Stadtzentrum und Rockos Arbeitsplatz; Rats- und Statistikbereiche nur räumlich angedeutet", "breiter reparierter Ratsbau mit gerundeter Freitreppe und genau einer Berg-Flammen-Dachkrone", "broad repaired civic hall with a rounded entry stair and one mountain-flame roof crown", 20, 18, "Warmer reparierter Putz, matte Steinsockel, oxidiertes Kupfer und Salbeiemaille", "warm repaired plaster, matte stone, oxidized copper, sage enamel", "Frontmitte Haupteingang; vorn links Serviceannahme; vorn rechts Verwaltungsanschluss", 14000),
}

def write_new(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists():
        path.write_text(text.rstrip() + "\n", encoding="utf-8", newline="\n")

def tier_id(index: int) -> str:
    return "T00_RUIN" if index == 0 else f"T{index:02d}_L{LEVELS[index][0]:03d}"

def parse_rows(building: str) -> list[tuple[str, str, str, float]]:
    result = []
    for line in ROWS[building].strip().splitlines():
        name, delta, form, height = line.split("|")
        result.append((name, delta, form, float(height)))
    assert len(result) == (1 if building == "town_hall" else 25), building
    return result

def image_prompt(building: str, tier: str, index: int, form: str, height: float) -> str:
    f = FAMILIES[building]
    fixed = building == "town_hall"
    ref = ("Input images 1-3 are the chosen style references in this order: Reclaimed Green Haven "
           "for readable cozy reconstruction forms, Herbal Atom Age for rounded retro machines, "
           "Burning Hills Afterglow for hopeful worldbuilding only. Do not copy their scenery or lighting. ")
    if fixed:
        ref += "This is the only fixed town hall; establish its permanent family identity. "
    elif index == 1:
        ref += "This is the T01 family DNA anchor. Establish the permanent recognizable core described below. "
    elif index == 0:
        ref += "Input image 4 is the accepted T01 family DNA anchor. Derive a repairable ruined initial state from that exact core. "
    elif index == 2:
        ref += "Input image 4 is the accepted T01 family DNA anchor, which is also the previous accepted tier. Preserve its core. "
    else:
        ref += "Input image 4 is the accepted T01 family DNA anchor; input image 5 is the immediately previous accepted tier. Preserve identity and coherent accumulated structure. "
    geometry = f"Permanent family DNA: {f[3]}. Current required structural change and composition: {form}. "
    if building == "cultivation_plot":
        geometry += ("The stated count is exact. Each counted grow module must be individually readable; "
                     "replace the earlier scale class at a class transition instead of retaining obsolete extra pots or modules. ")
    if building == "park":
        geometry += ("The bounded structural park parcel, its integrated planting terraces and attached pavilion are ONE connected asset; "
                     "the parcel is the functional object itself, not a decorative diorama slab. No surrounding terrain or loose garden props. ")
    return (f"Create one final Meshy reconstruction reference image for Weedtopia / Burning Hills: {building}, {tier}. "
            + ref + geometry +
            f"Reserved maximum local footprint {f[4]} m X by {f[5]} m Z; current approximate height {height:g} m. "
            "Ground-center pivot at (0,0,0), Y up, building front facing +Z. View from front-right (+X,+Z) "
            "with a mildly elevated near-orthographic camera; always show the same front and right side, never mirror. "
            "Keep the fixed front door, logistics ports and connection area; added volume grows upward and toward rear -Z inside the same plot. "
            "Fit the entire asset centered with at least ten percent clear margin on all sides; square canvas at least 1024 by 1024. "
            f"Materials: {f[7]}. Warm hand-painted stylized cozy-dystopian reconstruction architecture; rounded or softly beveled substantial forms, "
            "slightly oversized functional details, broad clean color regions, large sparse stylized wear, readable isometric silhouette. "
            "One dominant botanical or grinder emblem at most; no real brands. Thick reconstructible glazing only. "
            "Use plain very light warm-gray off-white background, even neutral studio lighting and only a soft contact shadow. "
            "No dusk treatment, dramatic shadows, colored illumination, baked window glow or fog. "
            "Single complete freestanding game-ready asset, grounded, clean edges, smart-topology intent, minimal robust geometry, no floating parts. "
            "No people, vehicles, conveyors, workers, detached props, scene, surrounding landscape, decorative diorama ground, UI, text, logos, watermarks, "
            "cropping, cut-off geometry, smoke, particles, tangled cables, thin antennas, fragile rails, weapons or gore. "
            "Show the complete current tier only, not a comparison sheet or collage; no additional buildings. Do not change the specified family core.")

def meshy_prompt(building: str, tier: str, form: str) -> str:
    f = FAMILIES[building]
    subjects = {"cultivation_plot":"Grow asset", "farm_processing":"Harvest refinery", "farm_warehouse":"Farm dispatch warehouse",
                "city_warehouse":"Urban distribution depot", "coffee_shop":"Retail coffee shop", "residential_house":"Community residence",
                "park":"Bounded community park", "butter_factory":"Food-safe butter factory", "supermarket":"Neighborhood supermarket",
                "bakery":"Production bakery", "town_hall":"Fixed civic town hall"}
    text = (f"{subjects[building]} {tier}: {form}. "
            f"{f[7]}. Stylized cozy retro reconstruction; rounded robust forms. "
            "game-ready, smart-topology, clean edges, minimal geometry, single complete asset, centered, grounded, "
            "front facing +Z, Y up, ground-center pivot, no floating parts. "
            "No people, scene, landscape, text, logos, vehicles, loose props, diorama ground, cropped geometry, smoke, weapons or gore.")
    # The requested term is intentionally lowercase and validated literally.
    text = text.replace("Stylized", "stylized")
    assert 0 < len(text) <= 800, (building, tier, len(text))
    return text

def texture_prompt(building: str) -> str:
    return (f"Materials and PBR textures only: {FAMILIES[building][7]}. Broad hand-painted color regions; "
            "warm neutral base colors, sparse large stylized repaired patches and gentle edge wear. Matte plaster and concrete, "
            "soft wood grain, restrained enamel roughness, physically distinct metal roughness; muted thick green-glass tint only where present. "
            "2048 textures, coherent albedo, metallic and roughness maps; subtle normal detail, no displacement. "
            "No baked lights, shadows, glow, fog, photorealistic scratch noise, text, logos or material-painted extra parts. "
            "Preserve all geometry and UV intent; one opaque material atlas, optional second glass material.")

def main() -> None:
    DOCS.mkdir(parents=True, exist_ok=True)
    existing_path = DOCS / "BUILDING_ASSET_MANIFEST.json"
    existing = json.loads(existing_path.read_text(encoding="utf-8-sig")) if existing_path.exists() else {}
    previous_slots = {(s["building_id"], s["tier_id"]): s for s in existing.get("slots", [])}
    slots, family_records = [], []
    for building, f in FAMILIES.items():
        rows = parse_rows(building)
        fixed = building == "town_hall"
        family_folder = DOCS / "Buildings" / building
        family_records.append({"building_id": building, "building_name_de": f[0], "upgradeable": not fixed,
            "expected_slots": len(rows), "maximum_footprint_m_x": f[4], "maximum_footprint_m_z": f[5],
            "front_axis": "+Z", "up_axis": "+Y", "pivot_intent": "ground center",
            "family_spec_file": f"Docs/ArtProduction/Buildings/{building}/family_spec.de.md",
            "family_showcase": f"Docs/ArtProduction/Buildings/{building}/family_showcase.png",
            "family_showcase_prompt_file": f"Docs/ArtProduction/Buildings/{building}/family_showcase_prompt_en.txt"})
        table = []
        for i, (name, delta, form, height) in enumerate(rows):
            tier = "T_FIXED" if fixed else tier_id(i)
            min_level, max_level = (0, 5000) if fixed else LEVELS[i]
            era = "Repariertes festes Stadtzentrum" if fixed else ERAS[i]
            folder = family_folder / tier
            rel = f"Docs/ArtProduction/Buildings/{building}/{tier}"
            incoming = f"Assets/Weedtopia/Art/Buildings/Incoming/{building}/{tier}"
            prefab = f"Assets/Weedtopia/Art/Buildings/Prefabs/{building}/WT_{building}_{tier}.prefab"
            driver_delta = 1 if building in ("farm_warehouse", "city_warehouse") and i >= 1 else 0
            target = 14000 if fixed else min(15000, int(round((f[9] + max(0, i-4)*350)/500)*500))
            # The immutable grid envelope is authoritative; lower tiers use a
            # centered subset, leaving rear reserved space for later modules.
            portion = 0.80 if fixed else (0.32 if i < 2 else min(0.95, 0.32 + i*0.026))
            extent_x, extent_z = round(f[4]*portion, 1), round(f[5]*portion, 1)
            if building == "cultivation_plot" and i in (0, 1, 2, 3, 4):
                extent_x, extent_z = ((3,3),(1.4,1.4),(1.4,3),(3,3),(3,3))[i]
            refs = list(STYLES)
            if not fixed and i != 1:
                refs.append(f"Docs/ArtProduction/Buildings/{building}/T01_L001/mesh_input_3q.png")
                if i > 2:
                    refs.append(f"Docs/ArtProduction/Buildings/{building}/{tier_id(i-1)}/mesh_input_3q.png")
            slot = {"building_id": building, "building_name_de": f[0], "tier_id": tier,
                "min_level": min_level, "max_level": max_level, "era": era, "visual_name_de": name,
                "visible_delta_from_previous": delta, "visible_delta_en": form,
                "footprint_m_x": f[4], "footprint_m_z": f[5], "height_m": height,
                "current_extent_m_x": extent_x, "current_extent_m_z": extent_z,
                "target_faces": target, "driver_slot_delta": driver_delta,
                "reference_image": f"{rel}/mesh_input_3q.png", "meshy_prompt_file": f"{rel}/meshy_prompt_en.txt",
                "texture_prompt_file": f"{rel}/texture_prompt_en.txt", "image_prompt_file": f"{rel}/image_prompt_en.txt",
                "asset_spec_file": f"{rel}/asset_spec.de.md", "meshy_settings_file": f"{rel}/meshy_settings.json",
                "unity_incoming_folder": incoming, "unity_prefab_target": prefab, "status": "pending_image",
                "front_axis": "+Z", "up_axis": "+Y", "pivot_intent": "ground center",
                "generation_reference_images": refs,
                "generation_sequence_index": 0 if fixed or i == 1 else (1 if i == 0 else i)}
            previous = previous_slots.get((building, tier))
            if previous:
                # Preserve QA/status additions, and do not quietly alter accepted
                # assets when rerunning the specification scaffold.
                slot.update(previous)
            slots.append(slot)
            (ROOT / incoming).mkdir(parents=True, exist_ok=True)
            (ROOT / prefab).parent.mkdir(parents=True, exist_ok=True)
            write_new(folder / "meshy_prompt_en.txt", meshy_prompt(building, tier, form))
            write_new(folder / "texture_prompt_en.txt", texture_prompt(building))
            write_new(folder / "image_prompt_en.txt", image_prompt(building, tier, i, form, height))
            settings = {"primary_workflow": "image_to_3d", "fallback_workflow": "text_to_3d",
                "model_type": "smart-topology", "model_type_availability": "if exposed in the current manual UI; not verified online",
                "topology": "triangles", "target_faces": target,
                "target_faces_reason_de": f"{target} Dreiecke für {name}: große Funktionskörper und weiche Silhouette; keine Mikrodetails. Höhere Stufen priorisieren Kronen und Modultrennung.",
                "texture_resolution": 2048, "pbr": True, "rigging": False, "animation": False,
                "recommended_export": "FBX with complete texture folder", "unity_scale": "1 unit = 1 meter",
                "unity_up_axis": "+Y", "unity_forward_axis": "+Z", "pivot_intent": "ground center",
                "max_material_slots": 2, "preferred_material_slots": 1,
                "manual_orientation_review_required": True, "manual_collider_review_required": True,
                "external_execution_performed": False}
            write_new(folder / "meshy_settings.json", json.dumps(settings, indent=2, ensure_ascii=False))
            previous_label = "Keine Vorstufe; einziges unveränderliches Rathaus." if fixed else (
                "Tutorialzustand; beschädigte Ableitung derselben T01-Form-DNA." if i == 0 else (
                "Reparatur gegenüber T00; dieses Bild wird vor T00 als verbindlicher Familienkern erzeugt." if i == 1 else
                f"Vorherige akzeptierte Stufe: {tier_id(i-1)} ({rows[i-1][0]})."))
            spec = f"""# {f[0]} – {tier}: {name}

Produktionsstatus bei Anlage: Bild ausstehend. Die Bilddatei gilt erst nach tatsächlicher Generierung und QA als akzeptiert; siehe PROGRESS.json und Manifest.

## Funktion und Stufenänderung

{f[1]}. Levelband: {min_level}–{max_level}; Epoche: {era}.

{previous_label}

**Große sichtbare Änderung:** {delta}

Die Änderung muss bei gleicher isometrischer Kamera ohne UI klar sichtbar sein. Farbe, Schildchen oder Kleinteile sind kein Stufenersatz. Größere Funktionskörper bleiben fest mit dem Asset verbunden. {('Genau die im Stufennamen benannte Anzahl von Modulen verwenden; bei Wechsel der Größenklasse alte kleinere Module ersetzen, nicht zusätzlich zählen.' if building == 'cultivation_plot' else 'Die bestehende Front und der Familienkern bleiben erhalten; die definierte neue Hauptmasse wird nach hinten oder oben integriert.')}

## Silhouette, Material und reale Maße

Dauerhafte Familien-DNA: {f[2]}. Aktueller Formauftrag: {form}.

Materialpriorität: {f[6]}. Ruhige handbemalte Flächen, runde robuste Hauptformen und sparsame großflächige Reparaturspuren. Keine eingebrannte Dämmerung, Lichtflecken oder photorealistischen Kratzer.

Reservierte maximale Stellfläche: **{f[4]} × {f[5]} m** (X × Z), in allen Familien-Stufen unverändert. Grober aktueller Körperumfang: etwa **{extent_x:g} × {extent_z:g} × {height:g} m** (X × Z × Höhe); Maßhilfe für die visuelle Produktion, kein Gameplaywert. Die reale Hülle muss nach Meshy-Export anhand ihrer Bounds geprüft werden. Wachstum nach +Y und hinten −Z, nie in die vordere Anschlusszone oder über die Maximalgrenzen.

Lokales System: +Y oben, **Front +Z**, Kamera vorne rechts (+X,+Z), Pivot am Boden im Zentrum der reservierten Stellfläche (0,0,0). Der ursprüngliche Frontzugang bleibt auf derselben Achse. Kleinere Baukörper müssen nicht den ganzen Bauplatz ausfüllen; im Bild darf die Kamera näher heran, ohne Proportionen oder Ausrichtung zu ändern. Planerische Maximalgrenzen sind unsichtbar und kein Diorama-Sockel.

## Front, Ports und Wrapper

{f[8]}. Die freien Anschlusstransforms gehören dem Wrapper, nicht einem eingefrorenen Förderband- oder Fahrzeugmesh. Lieferanker liegen auf der festen Frontzone oder dem oben benannten hinteren Stadtlager-Eingang; der genaue finale Meshanschluss wird manuell im Prefab abgeglichen.

Wrapper-Root: `WT_{building}_{tier}`; Kinder: `VisualRoot`, `InteractionAnchor`, `WorkerAnchor`, `DeliveryInputAnchor`, `DeliveryOutputAnchor`, `VfxAnchor` sowie vorbereitete Collider-Root. Keine Personen, Worker, Fahrzeuge, Förderbänder oder frei platzierbare Props im Modell. Kein automatisch abgeleiteter beliebiger MeshCollider. Das Quell-FBX bleibt unangetastet; skalieren/drehen/verschieben ausschließlich im Wrapper gemäß gemessenen Bounds.

Annahmeordner: `{incoming}`

Wrapper-Ziel: `{prefab}`

## Budget und Abnahme

Empfehlung: {target} Dreiecke, 2048-PBR-Texturen, ein opakes Material; zweites Material nur für notwendiges dickes Glas. Keine dünnen Antennen, schwebenden Teile, Rauch, Text, Logos, Waffen oder Gore. {'Die Parkparzelle selbst ist ein einziges verbundenes Funktionsasset mit integrierten Pflanzterrassen; außerhalb keine Landschaft oder dekorative Bodenplatte.' if building == 'park' else 'Ein vollständiges freistehendes Asset auf neutralem hellem Hintergrund ohne separate Boden-Diorama-Platte.'}

Fahrer-Slot-Delta: {driver_delta}. {('T01 ist der initiale erste Fahrerplatz, T02–T24 jeweils ein weiterer visueller Meilenstein mit +1. Das ist eine Produktionsnotiz aus der zentral dokumentierten Interpretation; keine Wirtschaftssimulation.' if building in ('farm_warehouse','city_warehouse') else 'Für diese Familie keine Fahrer-Kapazität definiert; keine zusätzlichen Produktions-, Einwohner-, Quest- oder Währungszahlen erfunden.')}

Prüfen: vollständige Freistellung, Front und Kernsilhouette, große Änderung zur Vorstufe, keine Spiegelung, Bild mindestens 1024 × 1024, eindeutiger Hash, alle fünf Begleitdateien. Nach manuellem FBX-Import separat Maßstab, +Y/+Z, Materialien, Bounds, Anchors und vereinfachte Collider prüfen. Bild-QA ersetzt keine Unity- oder Mesh-Qualitätsabnahme.
"""
            write_new(folder / "asset_spec.de.md", spec)
            table.append(f"| {tier} | {min_level}–{max_level} | {name} | {height:g} | {delta} |")
        family_text = f"""# Gebäudefamilie {building} – {f[0]}

## Vertrag und Funktion

{f[1]}. {'Genau ein fester Slot T_FIXED für Level 0–5000; weder Ruine noch erfundene Ausbaustufen.' if fixed else '25 Zustände: T00_RUIN sowie T01 bis T24; innerhalb eines Levelbands bleibt das Visual identisch.'}

Permanente Form-DNA: **{f[2]}**. Die Front ist immer **+Z**, oben **+Y**; isometrische Front-Rechts-Ansicht aus (+X,+Z). Bodenpivot im Zentrum der unveränderten reservierten Stellfläche. Keine Spiegelung oder wandernde Eingänge.

## Bauplatz und Anschlüsse

Maximaler Bauplatz: **{f[4]} × {f[5]} m** (X × Z). Reservierte Grenzen: X von {-f[4]/2:g} bis {f[4]/2:g} m, Z von {-f[5]/2:g} bis {f[5]/2:g} m. Erweiterungen wachsen nach oben und zur Rückseite −Z; die feste vordere Anschlusszone bleibt zugänglich. Die Tier-Spezifikationen nennen den ungefähren tatsächlichen Körperumfang und die Höhe separat. Anker gehören zum Wrapper und werden beim realen Import kontrolliert.

Front-/Anschlusslogik: {f[8]}. Türen, Ports und Wartungszugänge sind groß genug für isometrische Erkennung. Worker, Fahrzeuge, Förderbänder und frei platzierbare Dekoration werden später als eigene Prefabs entwickelt. Keine losen Props oder statischen Laufzeitobjekte.

## Materialien und wiederkehrende Funktion

{f[6]}. Breite saubere Farbflächen, weiche Kanten, großflächige sparsame Wiederaufbauspuren, höchstens ein dominantes Blatt-/Grindermotiv. Funktionsmodule aus der folgenden Tabelle bleiben als große zusammenhängende Körper rekonstruierbar. Keine beleuchteten Base-Color-Flächen oder Afterglow-Schatten im Meshy-Eingabebild.

{'Die Parkparzelle ist selbst das Asset: feste Einfassung, verbundene Terrassen und angebundener Community-Pavillon. Pflanzenmassen bleiben breit und ruhig, kein dünner Blätterteppich. Kein Umfeld-Diorama.' if building == 'park' else 'Alle Stufen behalten Kernsilhouette und feste Front; höhere Volumen staffeln sich hinter dem ursprünglichen Funktionsbereich.'}

## Verbindliche Entwicklung

| Tier | Level | Sichtbarer Zustand | Höhe m | Große Änderung zur Vorstufe |
|---|---|---|---:|---|
{chr(10).join(table)}

## Bildfolge und Abnahme

{'T_FIXED einmal mit den drei ausgewählten Stilreferenzen erzeugen und prüfen.' if fixed else 'T01 zuerst als Familien-DNA erzeugen und akzeptieren. T00 daraus als beschädigten Zustand ableiten. Dann T02 bis T24 in Reihenfolge; alle Bilder verwenden die drei gewählten Stilreferenzen plus T01, ab T03 zusätzlich das unmittelbar vorherige akzeptierte Bild. Bei T02 ist T01 gleichzeitig Kern- und Vorstufenreferenz und wird nur einmal als Input angehängt.'}

Jeder Slot hat ein eigenes `mesh_input_3q.png` und einen vollständigen gespeicherten `image_prompt_en.txt`, dessen geordnete Referenzbilder im Manifest stehen. Ein Textprompt ist kein erzeugtes oder abgenommenes Bild. Mindestauflösung 1024 × 1024; quadratisch, neutraler Studiohintergrund und weicher Kontaktschatten, Asset vollständig im Bild. Niemals Referenzbild duplizieren, um eine Stufenzählung zu erfüllen.

{'Genau ein breites family_showcase.png zeigt dasselbe eine feste Rathaus in einer kleinen Burning-Hills-Testumgebung bei blauer Stunde. Keine T01/T12/T24 erfinden, keine drei Rathausvarianten.' if fixed else 'Genau ein breites family_showcase.png zeigt die akzeptierten T01, T12 und T24 gemeinsam in einer kleinen isometrischen Burning-Hills-Testumgebung bei blauer Stunde. Das Showcase ist nicht Meshy-Input.'}

Visuelle Mindeststichprobe: {'T_FIXED und Showcase' if fixed else 'T00, T01, T08, T16, T24 und Showcase'}, zusätzlich jedes neue Bild auf Front, Kern, Freistellung, große Stufenänderung und unerwünschte Menschen/Texte prüfen. Kein Material-, Bild- oder Source-Compile-Gate ersetzt eine nicht erfolgte Unity-Import-, Prefab- oder Collider-Abnahme.
"""
        write_new(family_folder / "family_spec.de.md", family_text)
        showcase = (f"Generate one wide family art-direction showcase for Weedtopia / Burning Hills, family {building}. "
            "Inputs 1-3 are Reclaimed Green Haven (base forms and readability), Herbal Atom Age (rounded retro machinery), "
            "Burning Hills Afterglow (warm blue-hour reconstruction atmosphere). "
            + ("Input 4 is the accepted T_FIXED town hall. Show exactly this ONE fixed repaired town hall in a small isometric test environment. "
               "There are no upgrades or ruined variants; do not invent T01, T12 or T24, and do not duplicate the town hall. " if fixed else
               "Inputs 4, 5, 6 are the accepted T01, T12 and T24 family assets, in that order. Show these exact three versions together "
               "in a small isometric test environment, separated and clearly readable from left to right, same front orientation. "
               "Do not merge them or redesign their silhouettes. ")
            + f"Family DNA: {f[3]}. A hopeful cozy-dystopian rebuilt district, rounded hand-painted warm plaster and reclaimed materials, "
            "blue-hour sky light with warm windows and golden work lights, restrained green grow-light accents, subtle atmospheric depth. "
            "Wide landscape composition, at least 1536 by 1024 if supported. Modest connecting paths and planting only; focus on asset silhouettes. "
            "No people, vehicles, text, UI, watermarks, real brands, weapons or gore. This is an atmospheric comparison showcase, not a Meshy input.")
        write_new(family_folder / "family_showcase_prompt_en.txt", showcase)
    assert len(slots) == 251
    manifest = {"schemaVersion": 1, "project": "Weedtopia Inc / Burning Hills", "path_base": "project_root",
        "coordinate_system": {"up": "+Y", "front": "+Z", "camera_side": "+X,+Z", "pivot": "ground center"},
        "expected_slot_count": 251, "expected_showcase_count": 11,
        "driver_slot_delta_interpretation": "Only farm_warehouse and city_warehouse: T00=0; T01=+1 initial slot; T02-T24=+1 each visible milestone. Production interpretation only, no gameplay implementation.",
        "town_hall_showcase_interpretation": "One fixed T_FIXED hall shown once in the test environment; no invented upgrade tiers.",
        "footprint_interpretation": "footprint_m_x/z are immutable maximum plot bounds; current_extent_m_x/z estimate the tier body. Grow upward and rearward inside the plot.",
        "generation_status_interpretation": "pending_image means specification only. Existence, generation provenance and visual acceptance must be checked before advancing status.",
        "building_families": family_records, "slots": slots}
    temp = existing_path.with_suffix(".json.tmp")
    temp.write_text(json.dumps(manifest, indent=2, ensure_ascii=False)+"\n", encoding="utf-8", newline="\n")
    temp.replace(existing_path)
    fields = ["building_id", "building_name_de", "tier_id", "min_level", "max_level", "era", "visual_name_de",
        "visible_delta_from_previous", "footprint_m_x", "footprint_m_z", "height_m", "target_faces", "driver_slot_delta",
        "reference_image", "meshy_prompt_file", "texture_prompt_file", "unity_incoming_folder", "unity_prefab_target", "status",
        "image_prompt_file", "asset_spec_file", "meshy_settings_file", "current_extent_m_x", "current_extent_m_z"]
    output = io.StringIO(newline="")
    writer = csv.DictWriter(output, fieldnames=fields, extrasaction="ignore", lineterminator="\n")
    writer.writeheader()
    writer.writerows(slots)
    (DOCS / "BUILDING_TIER_MATRIX.csv").write_text(output.getvalue(), encoding="utf-8", newline="\n")
    for folder in ("Data/Buildings", "Scripts/Runtime/Buildings/Presentation", "Scripts/Editor/BuildingArtPipeline"):
        (ROOT / "Assets/Weedtopia" / folder).mkdir(parents=True, exist_ok=True)
    write_new(DOCS / "ART_DIRECTION.md", ART_DIRECTION)
    write_new(DOCS / "MESHY_WORKFLOW.md", MESHY_WORKFLOW)
    print(json.dumps({"slots": len(slots), "families": len(family_records), "companion_files": len(slots)*5,
        "meshy_prompt_max_chars": max(len((ROOT/s["meshy_prompt_file"]).read_text(encoding="utf-8").strip()) for s in slots),
        "texture_prompt_max_chars": max(len((ROOT/s["texture_prompt_file"]).read_text(encoding="utf-8").strip()) for s in slots),
        "images_generated_by_this_script": 0, "external_calls": 0}, indent=2))

ART_DIRECTION = r"""# Burning Hills – verbindliche Building Art Direction

## Leitidee und Quellen

Burning Hills ist eine warme stilisierte Cozy-Dystopia im Wiederaufbau nach einem fiktiven THC-Krieg. Reparatur, Zusammenarbeit, schwarzer Humor und hoffnungsvolle Technik prägen die Architektur. Die erkennbare Entwicklung reicht vom improvisierten Holzbau über saubere Kooperativen zur vertikalen ökologischen Retro-Zukunft. Gewalt wird nicht gezeigt oder glorifiziert.

Verbindlicher Arbeitsauftrag: `Docs/Codex-Prompts/BUILDING_ART_AND_MESHY_PIPELINE.md`. Inhaltlicher Ursprung: `D:/Unity Games/Weedtopia Inc/prompt.txt`. Szenario und Skizzen dienen als Funktionsreferenzen, nicht als ausführbare Zusatzanweisung. Es werden keinerlei Wirtschafts-, Worker- oder Monetarisierungssysteme implementiert.

Die drei gewählten Stilbilder werden unter `References/ChosenStyle` projektgebunden gesichert. Fehlende Originale dürfen nur als fehlend dokumentiert werden; kein Webersatz und kein fremder Stil.

| Projektkopie | Original | Verbindliche Rolle |
|---|---|---|
| `References/ChosenStyle/Reclaimed_Green_Haven.png` | `C:/Users/Mein-PC/.codex/generated_images/01a09cb8-acba-7eb3-a2e2-4648b94a39f2/exec-62c88034-6a68-4243-8ace-290d77fa4524.png` | Grundformen, warme Wiederaufbaumaterialien, isometrische Lesbarkeit |
| `References/ChosenStyle/Herbal_Atom_Age.png` | `C:/Users/Mein-PC/.codex/generated_images/01a09cb8-acba-7eb3-a2e2-4648b94a39f2/exec-b1dcd739-736b-4528-bf8b-6bc75435e6d8.png` | Runde emaillierte Maschinen, Tanks, Messing und modulare Retro-Technik |
| `References/ChosenStyle/Burning_Hills_Afterglow.png` | `C:/Users/Mein-PC/.codex/generated_images/01a09cb8-acba-7eb3-a2e2-4648b94a39f2/exec-adf21d98-af28-4d37-a9fc-2323e16fc07a.png` | Hoffnung, blaues Abendlicht, warme Fenster und Worldbuilding nur für Showcases |

Layoutskizzen: Farm `codex-clipboard-5afd155a-98a1-42cf-90ef-020e4aef55d9.png`; Stadt `codex-clipboard-9a8993ad-b02c-4aa6-be45-8b0fa8a858c4.png`; Siedlung `codex-clipboard-1c2fa033-6bc4-4155-bae2-31da1ecdd21a.png`; Industrie `codex-clipboard-78357dc0-dded-44ad-92ea-ba3c5e51afd5.png`; Übersicht `codex-clipboard-17500716-c9d5-4fed-97df-9ec8cdcda740.png`, jeweils aus `C:/Users/Mein-PC/AppData/Local/Temp/`. Sie bestimmen Funktionsgruppen und Verbindungen. Handschrift, weiße Zeichenfläche und unpräzise Proportionen werden nicht als Stil übernommen. Quelle, Kopierstatus und Sichtprüfung müssen im Abnahmebericht belegt werden; diese Dokumentation behauptet keine ungesehene Bildabnahme.

## Form, Maßstab und feste Orientierung

Aus isometrischer Distanz erkennbare Hauptmassen haben Vorrang vor Kleindetails. Abgerundete oder weich abgeschrägte Kanten, übergroße Türen und Funktionsports, breite Tanks und dicke Rohre schaffen robuste rekonstruierbare Silhouetten. Dünne Antennen, freie Kabel, schwebende Elemente und fragile Zierteile entfallen.

Jede Familie erhält eine unveränderliche Form-DNA und eine maximale Stellfläche in ihrer `family_spec.de.md`. Das lokale System ist **+Y oben, +Z Front, −Z hinten**. Der Bodenpivot liegt im Zentrum der reservierten Stellfläche. Die Kamera schaut stets von vorne rechts (+X,+Z) leicht erhöht und nahe orthografisch. Der Servicezugang und Anschlussbereich bleiben fest; Ausbau wächst nach oben und hinten innerhalb des Bauplatzes. Die tatsächliche Körpergröße darf kleiner sein als die reservierte Fläche. Referenzbilder passen den Bildausschnitt an das Asset an, behalten aber Blickrichtung und Proportionen.

`footprint_m_x` und `footprint_m_z` bezeichnen immer den festen maximalen Bauplatz; `current_extent_m_x/z` sind ungefähre Produktionsmaße der jeweiligen Stufe. Alle Maßangaben sind Art- und Importhilfen, keine erfundenen Gameplayzahlen. Unity-Maßstab: 1 Unit = 1 Meter. Reale exportierte Bounds müssen geprüft werden.

## Materialien und Motive

Warmer handbemalter Putz, wiederverwendetes Holz, gealtertes Wellblech, matter Beton und oliv- oder salbeigrüne Emaille bilden die Basis. Terrakotta, Senfgold, Messing, oxidiertes Kupfer und gedämpftes Türkis setzen kontrollierte Akzente. Dickes leicht grünes Glas wird sparsam eingesetzt. Abnutzung besteht aus wenigen großen reparierten Flächen statt fotorealistischer Kratzernoise.

Cannabis ist über kompakte botanische Massen, Grow-Module und funktionale Blattrhythmik erkennbar. Höchstens ein dominantes Blatt- oder Grinder-Symbol pro Gebäude. Supermarkt: ein kleines goldenes Zahnrad. Rathaus und finale Landmark-Kronen nutzen weiche Berg-/Flammenformen. Keine realen Marken, lesbaren Produktnamen, Konsumszenen, Waffen oder Gore.

Base-Color-Texturen enthalten keine eingebrannten Lichtquellen, Schatten, Nebel oder Farbstiche. Ziel sind eine opake Materialatlasfläche und höchstens ein zweites Material für Glas; URP-kompatible PBR-Texturabsicht bei 2048 Pixeln. Geometriebudget je Slot begründet zwischen 6000 und 15000 Dreiecken.

## Vollständigkeit und Epochen

Zehn ausbaubare Familien × 25 Zustände = 250 Slots. Das unveränderliche Rathaus `town_hall/T_FIXED` ergänzt genau einen Slot, insgesamt 251. Die Matrix enthält die verbindlichen lückenlosen Levelbänder. T00 gilt genau für Level 0; T_FIXED deckt 0–5000 ab, ohne Ausbau zu implizieren.

| Stufen | Epoche | Struktureller Schwerpunkt |
|---|---|---|
| T00 | Tutorial-Ruine | Reparierbare Grundform und erhaltene Anschlussspuren |
| T01–T04 | Survival-Reparatur | Holz, Planen-/Wellblechcharakter, Handwerk, erste große Funktionsmodule |
| T05–T08 | Community-Aufbau | Dauerhafter Putz, geordnete Hallen/Beete/Wohnräume, erste große Solardächer |
| T09–T12 | Kooperative Industrie | Emaillierte Maschinen oder größere bürgerliche Nutzmodule, Messing und sichtbarer Durchsatz |
| T13–T16 | District Industry | Zusätzliche Geschosse, Hallen und große geordnete Versorgungskörper |
| T17–T20 | Vertical Eco-Tech | Höherer kompakter Ausbau, Kupfer, dickes Grünglas und integrierte Solardächer |
| T21–T23 | Metropolitan Flagship | Gestaffelte Stadtwahrzeichen und weithin lesbare Funktionskronen |
| T24 | Burning-Hills-Ikone | Einmalige finale Dachkrone innerhalb der Familien-DNA |

Jede der 251 Spezifikationen nennt eine konkrete große Formänderung. Ein Farbwechsel oder kleines Schild zählt nicht. Wohnhäuser, Shops und Parks verwenden funktionale zivile Nutzmodule anstelle austauschbarer Fabriktanks. Der Anbauplatz folgt exakt der im Auftrag festgelegten Folge: vier Topfstufen, vier kleine Lagen, vier große Lagen, vier Mega-Lagen, vier Giga-Gewächshauszustände und vier vertikale Turmzustände; T24 zeigt vier Türme um eine zentrale Biokuppel.

Fahrer-Interpretation: Nur Farm-Lager und Stadt-Lager besitzen `driver_slot_delta`. T00=0, T01=+1 als initial bereitgestellter Fahrerplatz, T02–T24 jeweils +1 pro sichtbarem Meilenstein. Das ursprüngliche Szenario nennt +1 an Ausbauschwellen; die initiale T01-Zuordnung ist die hier offen dokumentierte Produktionsinterpretation. Es werden keine 24 Fahrzeuge eingebaut und keine weiteren Gameplaywerte erfunden. Das bleibt eine Datenanmerkung, keine Kapazitätsimplementierung.

## Bildproduktion und QA

Jeder Slot benötigt sein eigenes quadratisches `mesh_input_3q.png`, mindestens 1024 × 1024 soweit vom Bildwerkzeug unterstützt. Ein vollständiges einzelnes Asset, ausreichender Rand, neutraler heller warmgrauer Hintergrund, gleichmäßiges Studio-Licht und weicher Kontaktschatten. Keine Umgebung, Personen, Fahrzeuge, lose Props, UI, Schrift, Wasserzeichen, Rauch oder angeschnittenen Teile. Der Park ist als begrenztes verbundenes Funktionsasset zulässig; er bekommt keine zusätzliche Umgebungs-Dioramaplatte.

Pro Familie zuerst T01 als Qualitätsanker erzeugen. Danach T00 aus T01 ableiten, dann T02–T24 streng in Reihenfolge. Referenzreihenfolge: drei Stilbilder, T01-DNA, bei T03–T24 zusätzlich unmittelbar vorheriges akzeptiertes Bild. Bei T02 erfüllt T01 beide Rollen. Der tatsächliche vollständige Toolprompt steht in `image_prompt_en.txt`; Änderungen bei einer Regeneration müssen dort nachvollziehbar gespeichert werden. Geordnete Eingabepfade stehen in `generation_reference_images` im Manifest. Vorhandene akzeptierte Bilder werden nicht neu erzeugt.

Genau elf breite `family_showcase.png`: zehn Vergleichsbilder mit T01/T12/T24 bei blauer Stunde und ein Showcase des **einen festen Rathauses**. Für das Rathaus werden keine fiktiven Ausbaustufen oder Mehrfachvarianten erfunden. Showcases dürfen Afterglow-Licht, Pfade und etwas Bepflanzung zeigen; sie sind keine Meshy-Eingaben.

Dateihash und Mindestauflösung sind technische Prüfungen. Visuell geprüft werden je ausbaubarer Familie mindestens T00, T01, T08, T16 und T24, dazu T_FIXED und alle elf Showcases. Jede Bildannahme prüft Motiv, Kernsilhouette, feste Front, Freistellung, große Formänderung und unerwünschte Texte/Personen. Status wird erst nach tatsächlichen Belegen weitergeschaltet.

## Lokale Produktions- und Importgrenze

`Tools/build_production_data.py` erzeugt nur lokale Textdaten, Spezifikationen, das Manifest, die Matrix und leere Annahmeordner. Es erzeugt keine Rasterbilder, Modelle oder `.meta`-Dateien, berührt keine vorhandenen Quellmodelle und ruft keinen externen Dienst auf. Wiederholung bewahrt bestehende Begleittexte und Slotzustände. `PROGRESS.json` und Bild-QA werden separat atomar fortgeschrieben.

Keine Bedienung von Meshy, keine API, Credits, Uploads oder Veröffentlichungen. Manuelle spätere Meshy-Schritte sind in `MESHY_WORKFLOW.md` beschrieben. Unity-Empfangsdaten und Editor-Intake bleiben unter `Assets/Weedtopia`; die Beispielszenen und Projekteinstellungen werden nicht verändert. Eine laufende Unity-Instanz wird weder ersetzt noch beendet; offene Import-/Serialisierungsgates werden ehrlich berichtet.
"""

MESHY_WORKFLOW = r"""# Manueller Meshy-Export und Unity Building Tier Intake

## Ausführungsstand und Produktionsgrenze

Diese Anleitung bereitet einen **später vom Nutzer ausgeführten** Arbeitsablauf vor. In dieser lokalen Produktion wird Meshy weder geöffnet noch per API angesprochen; keine Anmeldung, Generierung, Modell-Uploads oder Credits. Es wurden keine aktuellen Meshy-Menüs online geprüft. Die Begriffe in den Settings sind Empfehlungen aus dem Auftrag, keine Behauptung über eine verifizierte aktuelle Weboberfläche. Falls ein Feld anders heißt oder fehlt, manuell die gleiche Absicht wählen und die gefundene Entsprechung hier dokumentieren; nie ungefragt einen kostenpflichtigen Schritt ausführen.

Ein Slot ist erst bildseitig bereit, wenn sein Manifest-/Fortschrittsstatus und die QA-Belege ein tatsächlich akzeptiertes Bild bestätigen. `pending_image` bezeichnet eine vollständige Spezifikation mit noch ausstehendem Bild. Ein Textprompt oder leeres Verzeichnis ersetzt kein Eingabebild und kein importiertes Modell.

## 1. Eindeutigen Produktionsslot wählen

In `BUILDING_TIER_MATRIX.csv` nach Familie und Levelband suchen. Genau ein Slot pro Band; nicht 5000 Modelle erzeugen. `T00_RUIN` ist Level 0, T01–T24 bilden die lückenlosen Ausbaubänder ab. `town_hall/T_FIXED` ist das eine feste Rathaus.

Den zugehörigen Ordner `Docs/ArtProduction/Buildings/<building_id>/<tier_id>/` öffnen. Benötigt werden das akzeptierte `mesh_input_3q.png`, `meshy_prompt_en.txt`, `texture_prompt_en.txt`, `asset_spec.de.md` und `meshy_settings.json`. Das Showcase niemals als Meshy-Eingabe verwenden. Der gespeicherte Bildprompt und die im Manifest geordneten Referenzbilder dienen einer gezielten Regeneration, nicht einer externen Ausführung.

## 2. Modell manuell in Meshy erzeugen

1. Nutzer öffnet Meshy selbst und wählt Image-to-3D.
2. Genau das akzeptierte `mesh_input_3q.png` des Slots auswählen. Die private/externe Verarbeitung entscheidet und startet ausschließlich der Nutzer.
3. Den vollständigen `meshy_prompt_en.txt` kopieren. Er enthält die Ausschlüsse im Hauptprompt, auch falls ein Negativfeld fehlt.
4. Wenn nur Text-to-3D genutzt werden soll, denselben Hauptprompt als Fallback einsetzen.
5. Empfohlen: smart-topology, sofern angeboten; Dreiecke; slotspezifische 6000–15000 Ziel-Faces; 2048-PBR; kein Rigging, keine Animation. Ein Material, optional ein zweites für dickes Glas.
6. Falls eine separate Texturphase angeboten wird, `texture_prompt_en.txt` verwenden. Keine Geometrie neu erfinden oder zusätzliche Props hineintexturieren.
7. Das Modell von vorn, hinten, links, rechts und oben kontrollieren: vollständige Silhouette, keine offenen zufälligen Löcher, keine schwebenden Teile, keine eingebackenen Fahrzeuge/Worker/Förderbänder, keine Texte oder Logos. Große Stufenänderung und Familien-DNA müssen erhalten bleiben.
8. FBX **mit vollständigem Texturordner** exportieren. Modellname möglichst `WT_<building_id>_<tier_id>`; bei abweichendem Originalnamen im Intake die Familie/Tierstufe ausdrücklich prüfen. Dateien im Exportpaket nicht einzeln auseinanderreißen.

Die tatsächliche Face-Zahl, Materialzahl und Rekonstruktionsqualität hängen vom manuell akzeptierten Export ab. Die vorbereitete Empfehlung ist keine Meshabnahme.

## 3. Vollständigen Export lokal importieren

Den vollständigen exportierten Ordner nach `Assets/Weedtopia/Art/Buildings/Incoming/<building_id>/<tier_id>/` kopieren. Vorhandene Exporte nicht ungeprüft überschreiben; für eine neue Version einen eigenen Unterordner verwenden. Texturrelative Pfade erhalten. Unity regulär importieren lassen. Keine `.meta` von Hand erzeugen und keine Quellmodelle oder eingebetteten Materialien destruktiv umschreiben.

Maße aus `asset_spec.de.md` prüfen: 1 Unity Unit = 1 Meter, oben +Y, Front +Z, Bodenpivot im Zentrum des reservierten Bauplatzes. Die maximalen X/Z-Abmessungen müssen eingehalten werden. Der feste Eingangs-/Portbereich soll zwischen Stufen nicht wandern. Kleine frühe Stufen füllen den Bauplatz nur teilweise aus.

## 4. Wrapper über den lokalen Editor-Intake zuweisen

1. In Unity `Weedtopia/Art Pipeline/Create or Repair Building Intake Scaffold` ausführen, wenn die serialisierten Slots noch fehlen. Der Vorgang repariert ausschließlich fehlende Struktur und bewahrt bestehende Modellzuweisungen und manuelle Anpassungen.
2. `Weedtopia/Art Pipeline/Building Tier Intake` öffnen.
3. Familie/Tier oder Levelband filtern; Referenzbild, Prompt und eindeutigen Zielpfad prüfen.
4. Importiertes FBX-/Model-Prefab aus dem zugehörigen Incoming-Ordner in den vorgesehenen ObjectField ziehen.
5. Vor Erstellung den reinen Validierungsmodus verwenden. Fehlendes Modell, Namensabweichung, leere Materialien, unplausible Bounds und unbestätigte Ausrichtung bearbeiten.
6. Per explizitem Erstellen-/Aktualisieren-Klick den Wrapper speichern. Das Modell wird ausschließlich unter `VisualRoot` referenziert. Das Quell-FBX bleibt unverändert.
7. Front und Maßstab visuell in der Scene View kontrollieren; eventuell nötige Korrekturen am Wrapper-VisualRoot ausführen. Bestehende Child-Offsets und Anchor-Anpassungen beim späteren Update erhalten.
8. `InteractionAnchor`, `WorkerAnchor`, `DeliveryInputAnchor`, `DeliveryOutputAnchor`, `VfxAnchor` an die klaren Modellports anpassen. Die Collider-Root bleibt sichtbar; passende einfache Collider sind eine manuelle geometrische QA. Kein beliebiger automatischer MeshCollider.
9. Wrapper speichern und erneut rein validieren. Direkte Katalogreferenz muss auf `Assets/Weedtopia/Art/Buildings/Prefabs/<building_id>/WT_<building_id>_<tier_id>.prefab` zeigen.

Das Toolkit erstellt keine Economy-/Worker-Logik, Addressables oder statischen Laufzeitobjekte. Levelschwellen wählen ausschließlich Präsentationsdaten. Importierte Meshy-FBX und Texturen bleiben eigenständige unveränderte Quellassets.

## Fortsetzung und getrennte Abnahme

`PROGRESS.json` ist der fortsetzbare lokale Produktionsstand. Nur vollständig vorhandene, im Manifest registrierte und geprüfte Slots überspringen. Bei einer Bildtool-Unterbrechung den exakten nächsten Slot und fehlende Bilder festhalten, keine Platzhalter erzeugen.

Getrennte Gates: Produktionsdaten/Schema, echte Bildgenerierung, visuelle Bild-QA, Unity Source-Compile, Unity Import, EditMode-Tests, serialisierte Katalog-/Prefab-Abnahme und nach Meshy-Import manuelle Geometrie-/Material-/Collider-QA. Ein erfolgreicher Dateivalidator schließt offene Bild- oder Unity-Gates nicht.

Bei bereits laufendem Unity Editor oder `Temp/UnityLockfile` keine zweite Instanz starten, keine Prozesse beenden und keinen Lock entfernen. Fehlende Serialisierung über den beschriebenen Menüpunkt im vorhandenen Editor erledigen lassen; bis dahin bleibt dieses Gate offen.
"""

if __name__ == "__main__":
    main()

"""Explicit additive Great Kushville authoring; never runs on Unity import."""
import copy
import hashlib
import json
import pathlib
import string

from author_green_leaf_village import (ACTION, COUNTER_ACTION, COUNTER_KEYS,
                                      FAMILY_NAMES, LANGUAGES, TEXT as SHARED_TEXT, tr)
from author_golden_flower import REPEAT_INSTRUCTIONS

ROOT = pathlib.Path(__file__).resolve().parents[2]
PATH = ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json'
LOCALES = ROOT / 'Assets/Weedtopia/Game/Localization'
REPORT = ROOT / 'Docs/GreatKushville/content_receipt.json'
CITY, PREFIX, VERSION = 'great_kushville', 'gk_', 12
SOURCES = ['burning_hills', 'big_rock_city', 'seven_hills', 'little_montana',
           'cassa_vadela', 'mark_wood_city', 'green_leaf_village', 'purple_garden',
           'golden_flower', 'saint_mary_ville']


def t(value):
    """Five explicit translations in de/en/es/fr/ru order."""
    values = value.split('|')
    if len(values) != 5:
        raise ValueError('Expected five translations: ' + value)
    return tr(*values)


TEXT = {
    'city.great_kushville': tr(*(['Great Kushville'] * 5)),
    'campaign.great_kushville_title': t('Rocko: Die Stadt der verbundenen Wege|Rocko: The city of connected paths|Rocko: La ciudad de caminos unidos|Rocko : La ville des chemins réunis|Рокко: Город связанных путей'),
    'campaign.great_kushville_body': t('Fünf Millionen Einwohner leben in unseren zehn bisherigen Städten zusammen. Great Kushville wuchs um alte Kanäle, Terrassengärten und den Gründerbogen. Bei seinem schnellen Wachstum wurden Wasserrechte, Lieferzeiten und Nachbarschaften getrennt geplant. Die Gärten warten auf Wasser, der Markt auf Waren und die Höfe auf Begegnungen. Selma, Aurel, Daria, Leandro, Kira, Benno, Yuna und Oskar wollen ihre Stadt wieder verbinden. Mit 12.500 Geld und 7 Teamkarten beginnen wir einen Wasserpakt, einen gemeinsamen Umschlagplatz und einen Gründerweg, auf dem auch die Geschichten der Zugezogenen ihren Platz bekommen.|Five million residents live across our ten existing cities. Great Kushville grew around old canals, terrace gardens and the Founders’ Arch. During rapid growth, water rights, delivery schedules and neighbourhoods were planned separately. Gardens wait for water, the market for goods and courtyards for encounters. Selma, Aurel, Daria, Leandro, Kira, Benno, Yuna and Oskar want to reconnect their city. With 12,500 money and 7 team cards, we begin a water compact, a shared exchange and a founders’ walk that welcomes newcomers’ stories too.|Cinco millones de habitantes viven en nuestras diez ciudades. Great Kushville creció alrededor de canales antiguos, jardines en terrazas y el Arco Fundador. Su expansión separó los derechos de agua, los horarios de reparto y los barrios. Los jardines esperan agua, el mercado mercancías y los patios encuentros. Selma, Aurel, Daria, Leandro, Kira, Benno, Yuna y Oskar quieren conectar su ciudad. Con 12.500 de dinero y 7 cartas de equipo iniciamos un pacto del agua, un centro de intercambio y un paseo fundador que acoge las historias de recién llegados.|Cinq millions d’habitants vivent dans nos dix villes. Great Kushville a grandi autour de vieux canaux, de jardins en terrasses et de l’Arche fondatrice. Sa croissance a séparé droits d’eau, livraisons et quartiers. Les jardins attendent l’eau, le marché les marchandises et les cours les rencontres. Selma, Aurel, Daria, Leandro, Kira, Benno, Yuna et Oskar veulent relier leur ville. Avec 12 500 d’argent et 7 cartes d’équipe, créons un pacte de l’eau, un lieu d’échange et un parcours fondateur accueillant les récits des nouveaux venus.|В наших десяти городах живут пять миллионов жителей. Great Kushville вырос вокруг старых каналов, террасных садов и Арки основателей. Рост разделил водные права, графики поставок и кварталы. Сады ждут воды, рынок — товаров, дворы — встреч. Сельма, Аурель, Дария, Леандро, Кира, Бенно, Юна и Оскар хотят вновь соединить город. С 12 500 денег и 7 карточками команды начнём водный договор, общий обменный двор и путь основателей, где найдётся место историям новых жителей.'),
    'campaign.great_kushville_notice': t('Rocko: Great Kushville verbindet seine Viertel neu. Acht Bewohner öffnen ihre Wege und Geschichten.|Rocko: Great Kushville reconnects its districts. Eight residents open their paths and stories.|Rocko: Great Kushville une sus barrios. Ocho vecinos abren sus caminos e historias.|Rocko : Great Kushville relie ses quartiers. Huit habitants ouvrent leurs chemins et leurs récits.|Рокко: Great Kushville вновь соединяет кварталы. Восемь жителей откроют пути и истории.'),
    'campaign.great_kushville_locked': t('Ab {0} Einwohnern in allen zehn bisherigen Städten zusammen. Aktuell: {1}.|Available at {0} residents across all ten existing cities. Current total: {1}.|Disponible con {0} habitantes entre las diez ciudades anteriores. Total actual: {1}.|Disponible à {0} habitants dans les dix villes précédentes réunies. Total actuel : {1}.|Откроется при {0} жителях в десяти предыдущих городах вместе. Сейчас: {1}.'),
    'district.great_kushville.farm': t('Kushgärten|Kush Gardens|Jardines Kush|Jardins Kush|Кушские сады'),
    'district.great_kushville.city': t('Kanalmarkt|Canal Market|Mercado del Canal|Marché du canal|Канальный рынок'),
    'district.great_kushville.settlement': t('Terrassenhöfe|Terrace Courts|Patios en Terrazas|Cours en terrasses|Террасные дворы'),
    'district.great_kushville.industry': t('Gründerbogen|Founders’ Arch|Arco Fundador|Arche fondatrice|Арка основателей'),
    'gk.dialog.mila': t('Aurel kennt jede trockene Rinne. Selma hört ihm zu, bevor sie den gemeinsamen Wasserplan zeichnet.|Aurel knows every dry channel. Selma listens before drawing the shared water plan.|Aurel conoce cada acequia seca. Selma escucha antes de trazar el plan del agua.|Aurel connaît chaque rigole sèche. Selma l’écoute avant de dessiner le plan commun.|Аурель знает каждый сухой желоб. Сельма выслушает его перед составлением водного плана.'),
    'gk.dialog.jaro': t('Benno bewahrt den Gründerbogen, Leandro seine neue Backstube. Gemeinsam geben sie alten Räumen neue Aufgaben.|Benno preserves the Founders’ Arch and Leandro his new bakery. Together they give old spaces new roles.|Benno cuida el arco y Leandro su panadería. Juntos dan funciones nuevas a espacios antiguos.|Benno préserve l’arche et Leandro sa boulangerie. Ils donnent de nouveaux rôles aux lieux anciens.|Бенно хранит арку, Леандро — пекарню. Вместе дадут старым местам новые задачи.'),
    'gk.dialog.nia': t('Daria plant Übergaben mit Oskar. Verlässliche Lieferungen schaffen Zeit für die Nachbarn; unsere Fahrer nutzen die vorhandenen Routen.|Daria plans handovers with Oskar. Reliable deliveries make time for neighbours; our drivers use existing routes.|Daria organiza relevos con Oskar. Las entregas fiables dejan tiempo para vecinos; usamos las rutas existentes.|Daria organise les relais avec Oskar. Des livraisons fiables libèrent du temps ; nos chauffeurs suivent les trajets existants.|Дария планирует передачи с Оскаром. Надёжные доставки оставят время соседям; водители используют существующие маршруты.'),
    'gk.dialog.rocko': t('Kira verbindet die Terrassenhöfe, Yuna die Stimmen. Great Kushville wächst, wenn alte und neue Bewohner gemeinsam entscheiden.|Kira connects the terrace courts and Yuna the voices. Great Kushville grows when old and new residents decide together.|Kira une los patios y Yuna las voces. La ciudad crece cuando vecinos antiguos y nuevos deciden juntos.|Kira relie les cours et Yuna les voix. La ville grandit quand anciens et nouveaux habitants décident ensemble.|Кира соединяет дворы, Юна — голоса. Город растёт, когда старые и новые жители решают вместе.'),
    'gk.choice.help': t('Diesen Weg gemeinsam öffnen|Open this path together|Abrir juntos este camino|Ouvrir ce chemin ensemble|Открыть этот путь вместе'),
    'gk.outcome.help': t('Unsere Arbeit verbindet Menschen und Orte. Wissen und Verantwortung bleiben geteilt.|Our work connects people and places. Knowledge and responsibility remain shared.|Nuestro trabajo une personas y lugares. El saber y la responsabilidad siguen compartidos.|Notre travail relie personnes et lieux. Savoir et responsabilité restent partagés.|Наша работа соединяет людей и места. Знания и ответственность останутся общими.'),
    'gk.story.completed': t('Diese Erinnerung bleibt in meiner Stadt. Mein nächstes Kapitel wächst mit unseren Nachbarn.|This memory stays in my city. My next chapter grows with our neighbours.|Este recuerdo permanece en mi ciudad. Mi próximo capítulo crece con los vecinos.|Ce souvenir reste dans ma ville. Mon prochain chapitre grandit avec nos voisins.|Эта память останется в моём городе. Следующая глава растёт вместе с соседями.'),
    'gk.repeat.completed': t('Für heute verbunden. Nach einer Pause pflegen wir unsere Wege weiter.|Connected for today. After a break, we keep caring for our paths.|Conectado por hoy. Tras una pausa seguimos cuidando los caminos.|Reliés pour aujourd’hui. Après une pause, nous continuerons à soigner nos chemins.|На сегодня связь налажена. После перерыва продолжим заботиться о путях.'),
    'gk.joint.completed': t('Dieser gemeinsame Ort bleibt. Acht Bewohner teilen weiter Arbeit, Entscheidungen und Geschichten.|This shared place stays. Eight residents continue sharing work, choices and stories.|Este lugar común permanece. Ocho vecinos siguen compartiendo trabajo, decisiones e historias.|Ce lieu commun demeure. Huit habitants continuent de partager travail, choix et récits.|Общее место останется. Восемь жителей продолжат делить работу, решения и истории.'),
    'gk.place.water_compact': t('Der gemeinsame Wasserpakt|The shared water compact|El pacto común del agua|Le pacte commun de l’eau|Общий водный договор'),
    'gk.place.shared_exchange': t('Der gemeinsame Umschlagplatz|The shared exchange|El centro de intercambio común|Le lieu d’échange commun|Общий обменный двор'),
    'gk.place.founders_walk': t('Der Gründerweg der vielen Stimmen|The founders’ walk of many voices|El paseo fundador de muchas voces|Le parcours fondateur aux multiples voix|Путь основателей и многих голосов'),
    'gk.place.water_lab': t('Selmas offenes Wasserlabor|Selma’s open water laboratory|El laboratorio abierto de Selma|Le laboratoire d’eau ouvert de Selma|Открытая водная лаборатория Сельмы'),
    'gk.place.canal_workshop': t('Selmas Kanalwerkstatt|Selma’s canal workshop|El taller del canal de Selma|L’atelier du canal de Selma|Канальная мастерская Сельмы'),
    'gk.choice.selma.water_lab': t('Ein offenes Wasserlabor einrichten|Create an open water laboratory|Crear un laboratorio abierto del agua|Créer un laboratoire d’eau ouvert|Открыть водную лабораторию'),
    'gk.choice.selma.canal_workshop': t('Eine Kanalwerkstatt einrichten|Create a canal workshop|Crear un taller del canal|Créer un atelier du canal|Открыть канальную мастерскую'),
    'gk.choice.selma.water_lab.outcome': t('Selma öffnet ein Wasserlabor mit Messpult, Probenregal und Erklärtafel. Aurel und Yuna teilen die Lernrunden; die Stadt kann ihre Wasserentscheidungen selbst verstehen.|Selma opens a water laboratory with a measuring desk, sample shelf and explanation board. Aurel and Yuna share sessions so the city can understand its water decisions.|Selma abre un laboratorio con mesa de medición, muestras y panel. Aurel y Yuna comparten sesiones para entender las decisiones del agua.|Selma ouvre un laboratoire avec table de mesure, échantillons et panneau. Aurel et Yuna partagent les séances pour comprendre les décisions de l’eau.|Сельма откроет лабораторию со столом измерений, полкой образцов и поясняющей доской. Аурель и Юна разделят занятия, чтобы город понимал водные решения.'),
    'gk.choice.selma.canal_workshop.outcome': t('Selma öffnet eine Kanalwerkstatt mit Werkzeugwand, Schiebermodell und gemeinsamem Reparaturplan. Benno und Oskar teilen Dienste; Wartung gehört jetzt allen Vierteln.|Selma opens a canal workshop with a tool wall, sluice model and shared repair plan. Benno and Oskar share shifts; maintenance belongs to every district.|Selma abre un taller con herramientas, modelo de compuerta y plan común. Benno y Oskar comparten turnos; todos participan en el mantenimiento.|Selma ouvre un atelier avec outils, modèle de vanne et plan commun. Benno et Oskar partagent les permanences ; chaque quartier participe à l’entretien.|Сельма откроет мастерскую со стеной инструментов, моделью затвора и общим планом ремонта. Бенно и Оскар разделят дежурства; обслуживанием займутся все кварталы.'),
}
TEXT.update({key.replace('gl.goal.', 'gk.goal.', 1): value for key, value in SHARED_TEXT.items() if key.startswith('gl.goal.')})
PEOPLE = []


def person(id, name, residents, focus, thresholds, role, bio, greeting, chapters, repeat):
    PEOPLE.append(dict(id=id, name=name, residents=residents, focus=focus,
                       thresholds=thresholds, role=t(role), bio=t(bio),
                       greeting=t(greeting), chapters=chapters, repeat=t(repeat)))


def chapter(title, scene, family, level, counter, count):
    title, scene = t(title), t(scene)
    return dict(title=title, body={lang: scene[lang] + ' ' + ACTION[lang].format(FAMILY_NAMES[family][lang], level, COUNTER_ACTION[counter][lang].format(count)) for lang in LANGUAGES}, memory=title, family=family, level=level, counter=counter, count=count)


person('selma', 'Selma Wehr', 50, 'farm_warehouse', [50,1400,7000,14000],
       'Kanalingenieurin|Canal engineer|Ingeniera de canales|Ingénieure des canaux|Инженер каналов',
       'Selma übernahm das Kanalbuch ihrer Mutter. Sie unterschrieb Pläne, die jeden Bezirk einzeln versorgten und die Nachbarn trennten. Nun hört sie Aurel zu und erklärt Wasserentscheidungen öffentlich.|Selma inherited her mother’s canal ledger. She signed plans that supplied districts separately and divided neighbours. Now she listens to Aurel and explains water decisions publicly.|Selma heredó el registro de su madre. Firmó planes que separaron el suministro y a los vecinos. Ahora escucha a Aurel y explica las decisiones del agua.|Selma a hérité du registre de sa mère. Ses plans séparaient approvisionnement et voisins. Elle écoute désormais Aurel et explique les décisions de l’eau.|Сельма унаследовала книгу каналов матери. Подписанные ею планы разделили снабжение и соседей. Теперь она слушает Ауреля и открыто объясняет водные решения.',
       'Ein Wasserplan braucht die Stimmen aller am Kanal.|A water plan needs every voice along the canal.|Un plan del agua necesita todas las voces del canal.|Un plan de l’eau a besoin de toutes les voix du canal.|Водному плану нужны голоса всех у канала.', [
    chapter('Die Zeile ohne Nachbarn|The line without neighbours|La línea sin vecinos|La ligne sans voisins|Строка без соседей', 'Selma vergleicht ihren Plan mit Aurels Gartenbuch. Eine fehlende Verbindung erklärt die trockene Rinne.|Selma compares her plan with Aurel’s garden ledger. A missing connection explains the dry channel.|Selma compara su plano con el libro de Aurel. Falta la conexión de la acequia seca.|Selma compare son plan au registre d’Aurel. Un raccord manquant explique la rigole sèche.|Сельма сравнит план с книгой Ауреля. Пропущенная связь объяснит сухой желоб.', 'farm_warehouse',1,8,25),
    chapter('Wasser nach gemeinsamem Maß|Water by a shared measure|Agua con una medida común|L’eau selon une mesure commune|Вода по общей мере', 'Aurel und Kira zeichnen am Verteilplan mit. Er enthält auch Pflegezeiten und offene Fragen.|Aurel and Kira coauthor the distribution plan. It includes care times and open questions.|Aurel y Kira trazan el reparto. Incluye cuidados y preguntas abiertas.|Aurel et Kira dessinent la répartition. Elle comprend entretien et questions ouvertes.|Аурель и Кира составят распределение. В нём будут время ухода и открытые вопросы.', 'park',8,8,100),
    chapter('Ein Raum für Wasserwissen|A room for water knowledge|Un lugar para saber del agua|Un lieu pour comprendre l’eau|Место для водных знаний', 'Das alte Schieberhaus öffnet. Wähle Wasserlabor oder Kanalwerkstatt; der neue Zweck bleibt sichtbar.|The old sluice house opens. Choose a water laboratory or canal workshop; its new purpose stays visible.|Abre la casa de compuertas. Elige laboratorio o taller; su función queda visible.|La maison des vannes ouvre. Choisis laboratoire ou atelier ; son rôle restera visible.|Дом затворов откроется. Выбери лабораторию или мастерскую; назначение останется видимым.', 'farm_processing',15,4,180),
    chapter('Das Kanalbuch wird geteilt|The canal ledger is shared|El registro se comparte|Le registre se partage|Книга каналов становится общей', 'Oskar erhält einen zweiten Schlüssel, Yuna eine leere Buchseite. Künftige Änderungen werden gemeinsam festgehalten.|Oskar receives a second key and Yuna a blank page. Future changes will be recorded together.|Oskar recibe otra llave y Yuna una página vacía. Los cambios se registrarán juntos.|Oskar reçoit une seconde clé et Yuna une page vierge. Les changements seront consignés ensemble.|Оскар получит второй ключ, Юна — чистую страницу. Изменения запишут вместе.', 'supermarket',25,9,12)],
       'Selma prüft Wasserplan und Vorräte mit wechselnden Nachbarn.|Selma checks water plans and supplies with different neighbours.|Selma revisa planes y reservas con distintos vecinos.|Selma vérifie plans et réserves avec différents voisins.|Сельма проверяет водный план и запасы с разными соседями.')

person('aurel', 'Aurel Blatt', 100, 'farm_grow_01', [100,1600,7500,15000],
       'Gärtner der alten Kanäle|Gardener of the old canals|Jardinero de canales antiguos|Jardinier des anciens canaux|Садовник старых каналов',
       'Aurel kam als junger Gärtner. Sein Buch hielt Schatten, Wasser und Nachbarn fest, bis neue Grenzen die Beete austrockneten. Er möchte sein Wissen teilen und selbst wieder Zeit zum Sitzen haben.|Aurel arrived as a young gardener. His ledger recorded shade, water and neighbours until new boundaries dried the beds. He wants to share knowledge and find time to sit down himself.|Aurel llegó de joven jardinero. Anotó sombra, agua y vecinos hasta que las fronteras secaron sus bancales. Quiere compartir saber y tener tiempo para sentarse.|Aurel est arrivé jeune jardinier. Il notait ombre, eau et voisins avant que les limites assèchent les plates-bandes. Il veut transmettre et prendre le temps de s’asseoir.|Аурель приехал молодым садовником. Он записывал тень, воду и соседей, пока границы не иссушили грядки. Теперь поделится знаниями и найдёт время отдохнуть.',
       'Mein Gartenbuch hat Platz für fremde Handschriften.|My garden ledger has room for other handwriting.|En mi libro caben otras letras.|Mon registre accueille d’autres écritures.|В моей садовой книге есть место чужому почерку.', [
    chapter('Der trockene Schattenweg|The dry shaded path|El sendero seco de sombra|Le chemin d’ombre asséché|Сухая тенистая дорожка', 'Aurel zeigt Selma die Rinne, die auf keiner neuen Karte steht. Sein Buch liefert den ersten Hinweis.|Aurel shows Selma the channel missing from new maps. His ledger gives their first clue.|Aurel muestra la acequia ausente de los mapas. Su libro da la primera pista.|Aurel montre la rigole absente des cartes. Son registre offre le premier indice.|Аурель покажет желоб, которого нет на картах. Книга даст первую подсказку.', 'cultivation_plot',1,3,30),
    chapter('Eine Seite für Leandro|A page for Leandro|Una página para Leandro|Une page pour Leandro|Страница для Леандро', 'Leandro ergänzt Kräuter aus seiner Kindheit. Aurel entdeckt, wie neue Wurzeln den Garten bereichern.|Leandro adds herbs from his childhood. Aurel discovers how new roots enrich the garden.|Leandro añade hierbas de su infancia. Aurel descubre raíces que enriquecen el jardín.|Leandro ajoute les herbes de son enfance. Aurel découvre des racines qui enrichissent le jardin.|Леандро добавит травы детства. Аурель узнает, как новые корни обогащают сад.', 'cultivation_plot',8,3,100),
    chapter('Pflege mit freien Nachmittagen|Care with free afternoons|Cuidados con tardes libres|Des soins et des après-midi libres|Уход со свободными вечерами', 'Kira und Oskar teilen die Gartenrunden. Aurel setzt sich zum ersten Mal selbst auf die Bank.|Kira and Oskar share garden rounds. Aurel finally sits on the bench himself.|Kira y Oskar reparten rondas. Aurel se sienta por fin en el banco.|Kira et Oskar partagent les tournées. Aurel prend enfin place sur le banc.|Кира и Оскар разделят обходы. Аурель впервые сам сядет на скамью.', 'park',15,8,160),
    chapter('Das Gartenbuch hat viele Hände|The garden ledger has many hands|El libro tiene muchas manos|Le registre a plusieurs mains|У садовой книги много рук', 'Aurel stellt das Buch am Gründerweg aus. Die neue Seite trägt Nachbarnamen und eine offene Einladung.|Aurel displays the ledger on the founders’ walk. Its new page bears neighbours’ names and an invitation.|Aurel expone el libro en el paseo. La página lleva nombres de vecinos y una invitación.|Aurel expose le registre sur le parcours. La page porte les noms des voisins et une invitation.|Аурель выставит книгу на пути основателей. На странице — имена соседей и приглашение.', 'farm_warehouse',25,9,12)],
       'Aurel teilt Gartenrunden, Erntewissen und Pausen.|Aurel shares garden rounds, harvest knowledge and breaks.|Aurel comparte rondas, saber de cosecha y pausas.|Aurel partage tournées, savoir des récoltes et pauses.|Аурель делится обходами, знаниями об урожае и отдыхом.')

person('daria', 'Daria Markt', 180, 'coffee_01', [180,1800,8000,16000],
       'Koordinatorin des Kanalmarkts|Canal Market coordinator|Coordinadora del mercado|Coordinatrice du marché du canal|Координатор Канального рынка',
       'Daria erbte Schulden und Lieferzettel. Sie hielt den Markt mit Versprechen offen, die kein Fahrplan tragen konnte. Mit Oskar will sie verlässliche Übergaben und Platz für kleine Händler schaffen.|Daria inherited debts and delivery slips. She kept the market open with promises no timetable could support. With Oskar she wants reliable handovers and room for small traders.|Daria heredó deudas y albaranes. Sostuvo el mercado con promesas imposibles. Con Oskar busca relevos fiables y sitio para pequeños comerciantes.|Daria a hérité de dettes et de bons de livraison. Ses promesses maintenaient le marché sans horaires fiables. Avec Oskar, elle veut des relais et une place pour les petits vendeurs.|Дария унаследовала долги и накладные. Рынок держался обещаниями, которых не выдерживал график. С Оскаром она наладит передачи и место мелким торговцам.',
       'Ein ehrlicher Lieferzettel ist besser als eine große Ankündigung.|An honest delivery slip beats a grand announcement.|Un albarán sincero vale más que un gran anuncio.|Un bon de livraison honnête vaut mieux qu’une grande annonce.|Честная накладная лучше громкого объявления.', [
    chapter('Die Schublade voller Versprechen|The drawer of promises|El cajón de promesas|Le tiroir des promesses|Ящик обещаний', 'Daria legt die unbezahlten Zettel offen. Oskar sortiert Termine statt Schuldige; ein verlässlicher Marktplan entsteht.|Daria reveals the unpaid slips. Oskar sorts dates rather than culprits; a reliable market plan begins.|Daria muestra los impagados. Oskar ordena fechas sin buscar culpables; nace un plan fiable.|Daria expose les impayés. Oskar trie les dates sans chercher de coupables ; un plan fiable naît.|Дария покажет неоплаченные записки. Оскар разберёт даты, а не виновных; появится план.', 'coffee_shop',1,5,25),
    chapter('Kleine Stände, gemeinsame Wege|Small stalls, shared paths|Puestos pequeños, caminos comunes|Petits étals, chemins communs|Маленькие прилавки, общие пути', 'Leandro und neue Händler bekommen dieselben Lieferfenster. Der Plan hängt offen für alle.|Leandro and new traders get equal delivery windows. The schedule is visible to everyone.|Leandro y nuevos vendedores tienen los mismos turnos. El plan queda a la vista.|Leandro et les nouveaux vendeurs ont les mêmes créneaux. Le planning est visible pour tous.|Леандро и новые торговцы получат равные часы доставки. План увидят все.', 'city_warehouse',8,8,100),
    chapter('Ein freier Platz im Lieferplan|A free space in the schedule|Un hueco libre en el horario|Une place libre dans le planning|Свободное место в графике', 'Daria reserviert Zeit für Kira und gibt einen zweiten Standschlüssel weiter. Ihr Alltag bekommt Raum für Freundschaft.|Daria reserves time for Kira and shares a second stall key. Her everyday life gains room for friendship.|Daria reserva tiempo para Kira y comparte otra llave. Sus días dejan sitio a la amistad.|Daria réserve du temps pour Kira et partage une seconde clé. L’amitié trouve sa place au quotidien.|Дария оставит время Кире и поделится вторым ключом. В буднях появится место дружбе.', 'supermarket',15,9,8),
    chapter('Ein Markt mit lesbarer Zukunft|A market with a readable future|Un mercado con futuro legible|Un marché à l’avenir lisible|Рынок с понятным будущим', 'Daria und Yuna veröffentlichen die Regeln des Umschlagplatzes. Neue Händler brauchen keinen geheimen Kontakt mehr.|Daria and Yuna publish the exchange rules. New traders no longer need a secret contact.|Daria y Yuna publican las reglas. Los vendedores nuevos ya no necesitan contactos secretos.|Daria et Yuna publient les règles. Les nouveaux vendeurs n’ont plus besoin d’un contact caché.|Дария и Юна опубликуют правила. Новым торговцам не нужен тайный знакомый.', 'coffee_shop',25,5,220)],
       'Daria hält Übergaben und Marktregeln für alle verlässlich.|Daria keeps handovers and market rules reliable for everyone.|Daria asegura relevos y reglas para todos.|Daria assure des relais et des règles fiables pour tous.|Дария поддерживает надёжные передачи и правила для всех.')

person('leandro', 'Leandro Vale', 250, 'bakery_01', [250,2000,8500,17000],
       'Bäcker mit zwei Heimaten|Baker with two homes|Panadero con dos hogares|Boulanger aux deux foyers|Пекарь с двумя домами',
       'Leandro kam mit Rezeptheft und Koffer. Seine Backstube sollte verbinden, doch getrennte Lieferpläne ließen den Ofen kalt. Er möchte seine Wurzeln behalten und neue Nachbarn an denselben Tisch bringen.|Leandro arrived with recipes and a suitcase. His bakery was meant to connect people, but separate schedules left the oven cold. He wants to keep his roots and welcome neighbours at one table.|Leandro llegó con recetas y maleta. Su panadería debía unir, pero los repartos dejaron el horno frío. Quiere conservar raíces y reunir vecinos.|Leandro est arrivé avec recettes et valise. Sa boulangerie devait réunir, mais les livraisons ont laissé le four froid. Il veut garder ses racines et accueillir ses voisins.|Леандро приехал с рецептами и чемоданом. Пекарня должна была объединять, но раздельные поставки оставили печь холодной. Он сохранит корни и соберёт соседей.',
       'Ein Rezept darf sich ändern, ohne seine Herkunft zu verlieren.|A recipe can change without losing its roots.|Una receta puede cambiar sin perder su origen.|Une recette peut changer sans perdre ses racines.|Рецепт может меняться, сохраняя корни.', [
    chapter('Der kalte Ofen|The cold oven|El horno frío|Le four froid|Холодная печь', 'Benno hört die Geschichte von Leandros Rezept, bevor er den Herdstein repariert.|Benno hears the story behind Leandro’s recipe before repairing the hearthstone.|Benno escucha la historia de la receta antes de reparar el horno.|Benno écoute l’histoire de la recette avant de réparer la pierre du four.|Бенно выслушает историю рецепта, прежде чем чинить камень печи.', 'bakery',1,7,25),
    chapter('Zwei Handschriften im Rezeptheft|Two hands in the recipe book|Dos letras en el recetario|Deux écritures dans le carnet|Два почерка в рецептах', 'Aurel ergänzt Kräuter, Leandro ihre Zubereitung. Beide Namen stehen unter dem neuen Rezept.|Aurel adds herbs and Leandro their preparation. Both names sign the new recipe.|Aurel añade hierbas y Leandro la preparación. Ambos firman la receta.|Aurel ajoute des herbes et Leandro la préparation. Les deux noms signent la recette.|Аурель добавит травы, Леандро — способ приготовления. Под рецептом будут оба имени.', 'butter_factory',8,6,100),
    chapter('Der Tisch nach Ladenschluss|The table after closing|La mesa después del cierre|La table après la fermeture|Стол после закрытия', 'Daria sichert die Lieferzeit und Oskar die letzte Übergabe. Leandro empfängt selbst seine Nachbarn zum Abendbrot.|Daria secures delivery times and Oskar the last handover. Leandro welcomes his neighbours to supper himself.|Daria asegura el reparto y Oskar el relevo. Leandro recibe vecinos para cenar.|Daria assure la livraison et Oskar le relais. Leandro accueille ses voisins au dîner.|Дария обеспечит доставку, Оскар — передачу. Леандро сам встретит соседей за ужином.', 'bakery',15,7,180),
    chapter('Wurzeln am Gründerweg|Roots on the founders’ walk|Raíces en el paseo fundador|Des racines sur le parcours|Корни на пути основателей', 'Yuna erhält eine Rezeptabschrift. Leandros Ankommen gehört zur Stadtgeschichte; das Original bleibt in seiner Backstube.|Yuna receives a recipe copy. Leandro’s arrival joins city history; the original stays in his bakery.|Yuna recibe una copia. La llegada de Leandro es historia local; el original queda en la panadería.|Yuna reçoit une copie. L’arrivée de Leandro entre dans l’histoire ; l’original reste à la boulangerie.|Юна получит копию. Приезд Леандро войдёт в историю, оригинал останется в пекарне.', 'bakery',25,9,12)],
       'Leandro teilt Rezepte und Backtage mit alten und neuen Nachbarn.|Leandro shares recipes and baking days with old and new neighbours.|Leandro comparte recetas y hornadas con vecinos antiguos y nuevos.|Leandro partage recettes et fournées avec anciens et nouveaux voisins.|Леандро делится рецептами и выпечкой со старыми и новыми соседями.')

person('kira', 'Kira Stieg', 350, 'settlement_03', [350,2300,9000,18000],
       'Planerin der Terrassenhöfe|Terrace Courts planner|Planificadora de los patios|Urbaniste des cours en terrasses|Планировщик Террасных дворов',
       'Kira wuchs zwischen den Terrassenhöfen auf. Ihr erster großer Plan schuf Häuser, aber keine gemeinsamen Zugänge. Sie will nun mit den Bewohnern bauen und lernen, wann ein freier Platz wertvoller ist als ein weiteres Gebäude.|Kira grew up between the terrace courts. Her first major plan made houses without shared entrances. Now she wants to build with residents and learn when an open space matters more than another building.|Kira creció en los patios. Su primer plan creó casas sin accesos comunes. Ahora quiere construir con los vecinos y valorar el espacio libre.|Kira a grandi entre les cours. Son premier plan créait des maisons sans accès communs. Elle veut désormais bâtir avec les habitants et préserver les espaces libres.|Кира выросла между дворами. Первый план создал дома без общих входов. Теперь она строит с жителями и учится ценить свободное место.',
       'Ein guter Hof beginnt mit Zuhören.|A good courtyard begins with listening.|Un buen patio empieza escuchando.|Une belle cour commence par l’écoute.|Хороший двор начинается с умения слушать.', [
    chapter('Die Treppe ohne Treffpunkt|The stairs without a meeting place|La escalera sin encuentro|L’escalier sans rencontre|Лестница без места встреч', 'Kira geht die alte Treppe mit Daria ab. Neben dem Hauseingang bleibt ein Platz für den ersten Park frei.|Kira walks the old stairs with Daria. Space beside the house entrance stays open for the first park.|Kira recorre la escalera con Daria. Junto a la entrada queda espacio para el parque.|Kira parcourt l’escalier avec Daria. Près de l’entrée, une place reste libre pour le parc.|Кира пройдёт лестницу с Дарией. Рядом со входом останется место парку.', 'park',1,8,30),
    chapter('Ein Hofplan mit Namen|A courtyard plan with names|Un plano con nombres|Un plan de cour avec des noms|План двора с именами', 'Kira zeichnet Sitzplätze nach den Wünschen ihrer Nachbarn. Auch Leandros freie Abendstunde steht im Plan.|Kira draws seating from neighbours’ wishes. Leandro’s free evening hour joins the plan too.|Kira dibuja asientos según deseos vecinales. Incluye la hora libre de Leandro.|Kira dessine des sièges selon les souhaits des voisins. L’heure libre de Leandro entre aussi dans le plan.|Кира нарисует места по пожеланиям соседей. В плане будет свободный вечер Леандро.', 'park',8,8,120),
    chapter('Die freie Mitte bleibt|The open centre stays|El centro libre permanece|Le centre ouvert demeure|Свободный центр остаётся', 'Kira hält die Hofmitte frei und teilt die Pflege. Aurel erhält eine Bank im Schatten statt einer neuen Pflicht.|Kira keeps the courtyard centre open and shares care. Aurel gets a shaded bench rather than another duty.|Kira deja el centro libre y reparte cuidados. Aurel recibe un banco a la sombra.|Kira préserve le centre et partage l’entretien. Aurel reçoit un banc à l’ombre.|Кира сохранит свободный центр и разделит уход. Аурелю достанется скамья в тени.', 'park',15,3,180),
    chapter('Der zweite Schlüssel zum Hof|The second courtyard key|La segunda llave del patio|La seconde clé de la cour|Второй ключ от двора', 'Daria übernimmt eine Pflegerunde. Kira kann als Nachbarin im Hof sitzen, ohne jeden Plan allein zu tragen.|Daria takes a care round. Kira can sit as a neighbour without carrying every plan alone.|Daria toma una ronda de cuidados. Kira puede sentarse como vecina sin cargar con todo.|Daria assure une tournée. Kira peut s’asseoir comme voisine sans tout porter seule.|Дария возьмёт обход. Кира сможет сидеть как соседка, не неся все планы одна.', 'park',25,9,12)],
       'Kira hält die Höfe offen und teilt Pflege sowie freie Zeit.|Kira keeps courts open and shares care and free time.|Kira mantiene patios abiertos y comparte cuidados y tiempo.|Kira garde les cours ouvertes et partage entretien et temps libre.|Кира держит дворы открытыми, делит уход и свободное время.')

person('benno', 'Benno Bogen', 450, 'city_warehouse', [450,2500,9500,18500],
       'Maurer und Hüter des Gründerbogens|Mason and arch steward|Albañil y guardián del arco|Maçon et gardien de l’arche|Каменщик и хранитель арки',
       'Benno lernte sein Handwerk unter dem Gründerbogen. Er wehrte jede Veränderung ab, weil er den Verlust alter Spuren fürchtete. Leandros Geschichte zeigt ihm, dass Bewahren auch Platz für neue Namen schaffen kann.|Benno learned masonry beneath the Founders’ Arch. He resisted change for fear of losing old traces. Leandro’s story shows him that preservation can make room for new names.|Benno aprendió bajo el arco. Rechazó cambios por miedo a perder huellas antiguas. La historia de Leandro le enseña a conservar dejando espacio a nuevos nombres.|Benno a appris sous l’arche. Il refusait le changement par peur de perdre les traces anciennes. Leandro lui montre que préserver permet aussi de nouveaux noms.|Бенно учился под аркой. Он отвергал перемены, боясь потерять старые следы. История Леандро покажет, что сохранение оставляет место новым именам.',
       'Ein alter Stein kann eine neue Geschichte tragen.|An old stone can carry a new story.|Una piedra vieja puede llevar una historia nueva.|Une vieille pierre peut porter un nouveau récit.|Старый камень может хранить новую историю.', [
    chapter('Die Kerbe unter dem Bogen|The notch beneath the arch|La marca bajo el arco|L’entaille sous l’arche|Зарубка под аркой', 'Benno zeigt Yuna die älteste Handwerkermarke. Daneben findet sie eine unbeschriftete Fläche.|Benno shows Yuna the oldest mason’s mark. Beside it she finds an unmarked surface.|Benno muestra la marca más antigua. Yuna encuentra una superficie sin inscripción.|Benno montre la plus ancienne marque. Yuna trouve à côté une surface vierge.|Бенно покажет старейший знак мастера. Юна найдёт рядом чистую поверхность.', 'city_warehouse',1,8,40),
    chapter('Die offene Werkzeugkiste|The open toolbox|La caja abierta de herramientas|La boîte à outils ouverte|Открытый ящик инструментов', 'Benno erklärt Leandro die Reparatur statt sie allein zu erledigen. Die Werkzeugkiste bekommt einen gemeinsamen Platz.|Benno explains repairs to Leandro rather than doing them alone. The toolbox gains a shared place.|Benno explica la reparación a Leandro. La caja recibe un lugar común.|Benno explique la réparation à Leandro. La boîte trouve une place commune.|Бенно объяснит ремонт Леандро. У ящика появится общее место.', 'butter_factory',8,6,110),
    chapter('Ein neuer Name im alten Stein|A new name in old stone|Un nombre nuevo en piedra vieja|Un nouveau nom dans la pierre|Новое имя в старом камне', 'Mit Yuna ergänzt Benno die Geschichte der Zugezogenen. Alte Marken bleiben lesbar, neue erhalten eigenen Raum.|With Yuna, Benno adds newcomers’ history. Old marks stay readable and new ones gain their own space.|Benno y Yuna añaden historias nuevas. Las marcas antiguas se conservan y las nuevas tienen sitio.|Benno et Yuna ajoutent les nouveaux récits. Les anciennes marques restent lisibles, les nouvelles ont leur place.|Бенно и Юна добавят истории приезжих. Старые знаки останутся читаемыми, новые получат место.', 'city_warehouse',15,8,200),
    chapter('Bewahren mit vielen Händen|Preserving with many hands|Conservar con muchas manos|Préserver à plusieurs mains|Сохранение многими руками', 'Benno übergibt einen Werkzeugschlüssel an Oskar. Der Gründerweg gehört den Menschen, die ihn gemeinsam pflegen.|Benno gives Oskar a tool key. The founders’ walk belongs to those who care for it together.|Benno da a Oskar una llave. El paseo pertenece a quienes lo cuidan juntos.|Benno donne une clé à Oskar. Le parcours appartient à ceux qui l’entretiennent ensemble.|Бенно отдаст Оскару ключ. Путь принадлежит людям, которые ухаживают за ним вместе.', 'supermarket',25,9,12)],
       'Benno teilt Reparaturen, Werkzeuge und das Wissen um alte Spuren.|Benno shares repairs, tools and knowledge of old traces.|Benno comparte reparaciones, herramientas e historia.|Benno partage réparations, outils et connaissance des traces.|Бенно делится ремонтом, инструментами и знаниями о старых следах.')

person('yuna', 'Yuna Klang', 600, 'settlement_01', [600,2700,10000,19000],
       'Sammlerin der Stadtstimmen|Collector of city voices|Coleccionista de voces|Collectrice des voix de la ville|Хранительница городских голосов',
       'Yuna kehrte nach einem Studium zurück. Das Stadtarchiv enthielt Baupläne, aber kaum die Stimmen der Menschen. Sie sammelt Geschichten mit deren Zustimmung und lässt jede Person selbst bestimmen, was öffentlich bleiben darf.|Yuna returned after her studies. City archives held plans but few people’s voices. She collects stories with permission and lets each person decide what can stay public.|Yuna volvió tras estudiar. El archivo guardaba planos pero pocas voces. Recoge historias con permiso y deja decidir qué será público.|Yuna est revenue après ses études. Les archives gardaient des plans mais peu de voix. Elle recueille les récits avec accord et laisse chacun choisir ce qui restera public.|Юна вернулась после учёбы. В архиве были планы, но мало человеческих голосов. Она собирает истории с согласия людей, которые сами решают, что открыть всем.',
       'Deine Geschichte gehört zuerst dir.|Your story belongs to you first.|Tu historia te pertenece primero a ti.|Ton récit t’appartient avant tout.|Твоя история прежде всего принадлежит тебе.', [
    chapter('Das Archiv der leeren Stimmen|The archive of missing voices|El archivo sin voces|Les archives sans voix|Архив без голосов', 'Yuna hört Benno unter dem Bogen zu. Er wählt selbst den ersten Satz für ihre gemeinsame Stadtkarte.|Yuna listens to Benno beneath the arch. He chooses the first sentence for their shared city map.|Yuna escucha a Benno bajo el arco. Él elige la primera frase de la nueva carta.|Yuna écoute Benno sous l’arche. Il choisit la première phrase de leur carte commune.|Юна выслушает Бенно под аркой. Он сам выберет первую фразу общей карты.', 'park',1,8,40),
    chapter('Eine Abschrift, keine Wegnahme|A copy, not a loss|Una copia que conserva|Une copie qui préserve|Копия сохраняет оригинал', 'Leandro gibt eine Rezeptabschrift frei. Yuna hält seine Herkunft fest und lässt das Original in der Backstube.|Leandro shares a recipe copy. Yuna records its roots and leaves the original in the bakery.|Leandro comparte una copia. Yuna registra su origen y deja el original en la panadería.|Leandro partage une copie. Yuna note ses racines et laisse l’original à la boulangerie.|Леандро поделится копией. Юна запишет её корни, оригинал останется в пекарне.', 'coffee_shop',8,5,100),
    chapter('Die Karte kann antworten|The map can answer|El mapa puede responder|La carte peut répondre|Карта умеет отвечать', 'Neben alte Geschichten setzt Yuna offene Schreibflächen. Kira und Selma ergänzen Fragen statt endgültiger Antworten.|Yuna adds open writing spaces beside old stories. Kira and Selma contribute questions rather than final answers.|Yuna deja espacio junto a historias antiguas. Kira y Selma añaden preguntas abiertas.|Yuna laisse des espaces près des anciens récits. Kira et Selma ajoutent des questions ouvertes.|Юна оставит место рядом со старыми историями. Кира и Сельма добавят открытые вопросы.', 'park',15,8,180),
    chapter('Der Gründerweg gehört vielen|The founders’ walk belongs to many|El paseo pertenece a muchos|Le parcours appartient à tous|Путь принадлежит многим', 'Yuna hängt die Stimmenkarte am Gründerweg auf. Alle acht prüfen ihre Texte; neue Seiten bleiben für kommende Nachbarn frei.|Yuna displays the voice map on the founders’ walk. All eight review their words; blank pages await future neighbours.|Yuna expone el mapa de voces. Los ocho revisan sus textos; quedan páginas para nuevos vecinos.|Yuna affiche la carte des voix. Les huit relisent leurs textes ; des pages attendent les futurs voisins.|Юна выставит карту голосов. Все восемь проверят тексты; страницы останутся будущим соседям.', 'city_warehouse',25,9,12)],
       'Yuna hält Begegnungen fest und lässt neue Seiten für kommende Nachbarn offen.|Yuna records encounters and leaves new pages open for future neighbours.|Yuna guarda encuentros y páginas para próximos vecinos.|Yuna garde les rencontres et des pages pour les futurs voisins.|Юна записывает встречи и оставляет страницы будущим соседям.')

person('oskar', 'Oskar Takt', 700, 'butter_factory', [700,2900,10500,19500],
       'Planer der gemeinsamen Übergaben|Planner of shared handovers|Planificador de relevos comunes|Organisateur des relais communs|Планировщик общих передач',
       'Oskar fuhr jahrelang zwischen getrennten Bezirken. Immer engere Lieferfenster kosteten ihn Zeit mit Freunden. Er will verlässliche Routen mit Pausen planen und anderen beibringen, wie ein guter Tag mehr als pünktliche Waren enthält.|Oskar drove between separate districts for years. Tight delivery windows cost him time with friends. He wants reliable routes with breaks and to teach others that a good day means more than punctual goods.|Oskar condujo años entre barrios separados. Los plazos le quitaron tiempo con amigos. Quiere rutas fiables con pausas y días con algo más que mercancías puntuales.|Oskar a livré entre quartiers séparés durant des années. Les délais lui ont pris son temps avec ses amis. Il veut des trajets fiables avec pauses et des journées au-delà des colis.|Оскар годами возил товары между раздельными кварталами. Сроки отняли время у друзей. Теперь он спланирует надёжные маршруты с отдыхом и днями, в которых есть больше, чем товары.',
       'Ein guter Fahrplan lässt Zeit zum Bleiben.|A good timetable leaves time to stay.|Un buen horario deja tiempo para quedarse.|Un bon planning laisse le temps de rester.|Хороший график оставляет время побыть рядом.', [
    chapter('Die Runde ohne Pause|The round without a break|La ronda sin pausa|La tournée sans pause|Круг без отдыха', 'Oskar zeichnet seine Umwege für Daria auf. Die erste verlässliche Übergabe schafft eine echte Pause.|Oskar maps his detours for Daria. Their first reliable handover makes a real break.|Oskar dibuja sus rodeos para Daria. El primer relevo fiable deja una pausa.|Oskar dessine ses détours pour Daria. Le premier relais fiable offre une pause.|Оскар нарисует объезды для Дарии. Первая надёжная передача даст настоящий отдых.', 'butter_factory',1,9,3),
    chapter('Ein Fahrplan mit Nachbarn|A timetable with neighbours|Un horario con vecinos|Un planning avec des voisins|График с соседями', 'Daria teilt Übergaben, Kira vermerkt freie Hofstunden. Oskars Fahrplan enthält erstmals Verabredungen.|Daria shares handovers and Kira marks free courtyard hours. Oskar’s schedule includes meetings for the first time.|Daria comparte relevos y Kira horas libres. El horario de Oskar incluye encuentros.|Daria partage les relais, Kira les heures libres. Le planning d’Oskar comprend enfin des rencontres.|Дария разделит передачи, Кира отметит часы во дворе. В графике Оскара впервые появятся встречи.', 'supermarket',8,8,120),
    chapter('Die letzte Übergabe wird geteilt|The last handover is shared|El último relevo se comparte|Le dernier relais se partage|Последняя передача становится общей', 'Oskar schult wechselnde Fahrer. Leandro behält seinen Abend und Oskar bekommt einen freien Platz am Tisch.|Oskar trains different drivers. Leandro keeps his evening and Oskar gains a seat at the table.|Oskar forma a conductores. Leandro conserva su tarde y Oskar tiene sitio en la mesa.|Oskar forme plusieurs chauffeurs. Leandro garde sa soirée et Oskar une place à table.|Оскар обучит разных водителей. Леандро сохранит вечер, Оскар получит место за столом.', 'butter_factory',15,9,8),
    chapter('Zeit zum Ankommen|Time to arrive|Tiempo para llegar|Le temps d’arriver|Время быть рядом', 'Oskar hängt den geteilten Wochenplan neben Yunas Stimmenkarte. Seine Freunde erkennen gemeinsame freie Stunden.|Oskar hangs the shared weekly rota beside Yuna’s voice map. His friends find shared free hours.|Oskar cuelga el plan junto al mapa de Yuna. Sus amigos encuentran horas libres comunes.|Oskar affiche le planning près de la carte de Yuna. Ses amis trouvent des heures libres communes.|Оскар повесит недельный план у карты Юны. Друзья найдут общие свободные часы.', 'supermarket',25,9,12)],
       'Oskar hält Lieferwege verlässlich und schützt gemeinsame Pausen.|Oskar keeps deliveries reliable and protects shared breaks.|Oskar asegura entregas y protege pausas comunes.|Oskar assure les livraisons et protège les pauses communes.|Оскар поддерживает доставки и бережёт общий отдых.')

REPEATS = [
    (3,60,0,t('Die nächste Gartenrunde|The next garden round|La siguiente ronda del jardín|La prochaine tournée du jardin|Следующий обход сада')),
    (8,120,1,t('Verlässliche Übergaben|Reliable handovers|Relevos fiables|Des relais fiables|Надёжные передачи')),
    (7,80,3,t('Ein weiterer gemeinsamer Tisch|Another shared table|Otra mesa común|Une autre table commune|Ещё один общий стол')),
]

PROJECTS = [
    ('water_compact',2000,2,8,160,1,
     t('Wasser für alle vier Viertel|Water for all four districts|Agua para los cuatro barrios|De l’eau pour les quatre quartiers|Вода для четырёх кварталов'),
     t('Nach ihren ersten Kapiteln teilen alle acht ihre Wege. Selma und Aurel machen Wasserentscheidungen lesbar, Daria und Oskar planen Übergaben, Kira hält Treffpunkte offen, Leandro versorgt die Helfer, Benno pflegt den Ort und Yuna bewahrt die Absprachen.|After their first chapters all eight share their paths. Selma and Aurel explain water decisions, Daria and Oskar plan handovers, Kira keeps meeting spaces open, Leandro supplies helpers, Benno cares for the place and Yuna records agreements.|Tras sus primeras historias los ocho comparten caminos. Selma y Aurel explican el agua, Daria y Oskar organizan relevos, Kira abre encuentros, Leandro abastece, Benno cuida y Yuna guarda acuerdos.|Après leurs premiers chapitres, les huit partagent leurs chemins. Selma et Aurel expliquent l’eau, Daria et Oskar organisent les relais, Kira garde les rencontres ouvertes, Leandro ravitaille, Benno entretient et Yuna conserve les accords.|После первых глав все восемь объединят пути. Сельма и Аурель объяснят воду, Дария и Оскар спланируют передачи, Кира откроет встречи, Леандро снабдит помощников, Бенно займётся местом, Юна сохранит договорённости.')),
    ('shared_exchange',8000,1,9,18,2,
     t('Der Markt bekommt gemeinsame Zeiten|The market gains shared time|El mercado comparte horarios|Le marché partage son temps|Рынок получает общие часы'),
     t('Nach den zweiten Kapiteln öffnet der gemeinsame Umschlagplatz. Daria veröffentlicht faire Lieferfenster, Oskar teilt Fahrdienste. Wasser, Waren und freie Zeit werden gemeinsam geplant; neue Händler finden dieselben Regeln vor.|After the second chapters the shared exchange opens. Daria publishes fair delivery windows and Oskar shares driving shifts. Water, goods and free time are planned together; newcomers find equal rules.|Tras los segundos capítulos abre el intercambio. Daria publica turnos justos y Oskar comparte reparto. Agua, mercancías y tiempo libre se planifican juntos; los nuevos tienen las mismas reglas.|Après les deuxièmes chapitres, le lieu d’échange ouvre. Daria publie des créneaux équitables, Oskar partage les trajets. Eau, marchandises et temps libre se planifient ensemble ; les nouveaux ont les mêmes règles.|После вторых глав откроется общий обменный двор. Дария опубликует равные часы, Оскар разделит рейсы. Воду, товары и отдых спланируют вместе; новичкам достанутся те же правила.')),
    ('founders_walk',22000,3,7,280,4,
     t('Eine große Stadt mit vielen Anfängen|A great city with many beginnings|Una gran ciudad con muchos comienzos|Une grande ville aux multiples débuts|Большой город с множеством начал'),
     t('Alle acht schließen ihre vierten Kapitel ab. Benno und Yuna öffnen den Gründerweg mit alten Handwerkermarken, Leandros Rezept und freien Seiten für neue Nachbarn. Der Weg, der Wasserpakt und die geteilten Dienste bleiben im Alltag sichtbar.|All eight finish their fourth chapters. Benno and Yuna open the founders’ walk with old craft marks, Leandro’s recipe and blank pages for new neighbours. The walk, water compact and shared shifts remain visible in everyday life.|Los ocho terminan sus cuartos capítulos. Benno y Yuna abren el paseo con marcas antiguas, la receta de Leandro y páginas para vecinos nuevos. El paseo, el pacto y los turnos permanecen.|Les huit terminent leur quatrième chapitre. Benno et Yuna ouvrent le parcours avec marques anciennes, recette de Leandro et pages libres pour les nouveaux voisins. Parcours, pacte et permanences demeurent.|Все восемь завершат четвёртые главы. Бенно и Юна откроют путь со старыми знаками мастеров, рецептом Леандро и страницами для новых соседей. Путь, водный договор и общие дежурства останутся в буднях.')),
]

# Reuse the proven progression mechanics, with original city-specific narrative.
# Main quests intentionally point players towards personal/community quests;
# their completion is controlled by VipStoryService and its persisted graph.
MAIN_ROWS = [
    ('arrival','arrival',0,'Unter dem Gründerbogen|Beneath the Founders’ Arch|Bajo el Arco Fundador|Sous l’Arche fondatrice|Под Аркой основателей',
     'Rocko: Sprich mit mir am Rathaus. Acht Stimmen zeigen, wo die Stadt ihre Verbindungen verloren hat.|Rocko: Talk to me at the town hall. Eight voices show where the city lost its connections.|Rocko: Habla conmigo en el ayuntamiento. Ocho voces muestran las conexiones perdidas.|Rocko : Parle-moi à la mairie. Huit voix montrent les liens perdus.|Рокко: Поговори со мной у ратуши. Восемь голосов покажут потерянные связи.'),
    ('first_garden','dry_channel',0,'Aurels trockene Rinne|Aurel’s dry channel|La acequia seca de Aurel|La rigole sèche d’Aurel|Сухой желоб Ауреля',
     'Mila: Repariere das erste Feld. Aurel und Selma vergleichen Gartenbuch und Kanalplan.|Mila: Repair the first plot. Aurel and Selma compare garden ledger and canal plan.|Mila: Repara el primer campo. Aurel y Selma comparan jardín y canal.|Mila : Répare la première parcelle. Aurel et Selma comparent leurs registres.|Мила: Восстанови первое поле. Аурель и Сельма сравнят садовую книгу и канал.'),
    ('first_harvest','first_growth',0,'Die Gärten beginnen wieder|The gardens begin again|Los jardines vuelven a crecer|Les jardins repartent|Сады оживают',
     'Mila: Ernte erste Rohware. Aurel lässt im Gartenbuch Platz für neue Hände.|Mila: Harvest the first raw goods. Aurel leaves room in the ledger for new hands.|Mila: Cosecha materia prima. Aurel deja sitio para nuevas manos.|Mila : Récolte les premières matières. Aurel laisse de la place aux nouvelles mains.|Мила: Собери первое сырьё. Аурель оставит место новым рукам.'),
    ('processing','sluice_tools',0,'Werkzeuge für die Verbindung|Tools for the connection|Herramientas para unir|Des outils pour relier|Инструменты для связи',
     'Jaro: Besetze das Feld, sprich mit mir und repariere die Verarbeitung. Selma erklärt den fehlenden Anschluss.|Jaro: Staff the plot, talk to me and repair processing. Selma explains the missing connection.|Jaro: Asigna el campo, habla conmigo y repara procesamiento. Selma explica la conexión.|Jaro : Affecte la parcelle, parle-moi et répare la transformation. Selma explique le raccord.|Яро: Назначь поле, поговори со мной и восстанови переработку. Сельма объяснит пропуск.'),
    ('farm_store','shared_supplies',0,'Vorräte mit lesbarem Plan|Supplies with a clear plan|Reservas con plan claro|Des réserves au plan clair|Запасы с понятным планом',
     'Nia: Besetze die Verarbeitung, sprich mit mir und repariere das Farmlager. Daria und Oskar schreiben die erste gemeinsame Übergabe auf.|Nia: Staff processing, talk to me and repair the farm warehouse. Daria and Oskar record the first shared handover.|Nia: Asigna procesamiento, habla conmigo y repara almacén. Daria y Oskar anotan el relevo.|Nia : Affecte la transformation, parle-moi et répare l’entrepôt. Daria et Oskar notent le relais.|Ния: Назначь переработку, поговори со мной и восстанови склад. Дария и Оскар запишут передачу.'),
    ('farm_routes','first_handover',0,'Oskars erste Pause|Oskar’s first break|La primera pausa de Oskar|La première pause d’Oskar|Первый отдых Оскара',
     'Nia: Besetze Lagerleitung und Fahrer, schließe eine Lieferung ab und automatisiere die Farm. Der Kanalmarkt öffnet.|Nia: Staff warehouse manager and driver, complete a delivery and automate the farm. The Canal Market opens.|Nia: Asigna responsable y conductor, completa entrega y automatiza la granja. Abre el mercado.|Nia : Affecte responsable et chauffeur, livre puis automatise la ferme. Le marché ouvre.|Ния: Назначь управляющего и водителя, заверши доставку и автоматизируй ферму. Откроется рынок.'),
    ('market_reopen','honest_market',0,'Darias ehrlicher Anfang|Daria’s honest beginning|El comienzo sincero de Daria|Le début honnête de Daria|Честное начало Дарии',
     'Rocko: Repariere Stadtlager und ersten Coffeeshop. Daria legt die alten Lieferzettel offen.|Rocko: Repair the city warehouse and first coffee shop. Daria reveals the old delivery slips.|Rocko: Repara almacén y primer café. Daria muestra los albaranes.|Rocko : Répare entrepôt urbain et premier café. Daria expose les anciens bons.|Рокко: Восстанови городской склад и кофейню. Дария покажет старые накладные.'),
    ('first_meeting','open_windows',0,'Gleiche Lieferfenster|Equal delivery windows|Turnos de reparto iguales|Des créneaux de livraison égaux|Равные часы доставки',
     'Rocko: Besetze Stadtlager und Coffeeshop und verkaufe erste Cannabis-Blüten. Danach öffnen die Terrassenhöfe.|Rocko: Staff city warehouse and coffee shop and sell the first cannabis buds. Terrace Courts open next.|Rocko: Asigna almacén y café y vende flores de cannabis. Abren los patios.|Rocko : Affecte entrepôt et café puis vends des fleurs de cannabis. Les cours ouvrent.|Рокко: Назначь склад и кофейню, продай соцветия конопли. Откроются дворы.'),
    ('homes','courtyard_space',0,'Kiras freie Hofmitte|Kira’s open courtyard centre|El centro libre de Kira|Le centre ouvert de Kira|Свободный центр Киры',
     'Rocko: Errichte ein Wohnhaus und einen Park auf vorhandenen Bauplätzen. Kira hält Platz für Begegnungen frei.|Rocko: Build a house and park on existing plots. Kira keeps space for encounters.|Rocko: Construye casa y parque. Kira guarda sitio para encuentros.|Rocko : Construis maison et parc. Kira préserve une place pour les rencontres.|Рокко: Построй дом и парк. Кира оставит место встречам.'),
    ('everyday','neighbour_hours',0,'Stunden für die Nachbarn|Hours for neighbours|Horas para vecinos|Des heures pour les voisins|Часы для соседей',
     'Rocko: Besetze Haus und Park, hole Tagesertrag ab und erledige einen VIP-Auftrag samt Belohnung. Danach öffnet der Gründerbogen; persönliche Bewohnerkapitel warten im Bewohnerbereich.|Rocko: Staff house and park, collect daily income and complete a VIP task with its reward. Founders’ Arch opens next; personal chapters await in the resident panel.|Rocko: Asigna casa y parque, cobra ingreso y completa un encargo VIP con premio. Abre el arco; hay capítulos en el panel.|Rocko : Affecte maison et parc, collecte un revenu et termine une mission VIP avec récompense. L’arche ouvre ; les chapitres attendent dans le panneau.|Рокко: Назначь дом и парк, собери доход и выполни VIP-задание с наградой. Откроется арка; главы ждут в разделе жителей.'),
    ('workyard','warm_oven',0,'Wärme für Leandros Ofen|Warmth for Leandro’s oven|Calor para el horno de Leandro|De la chaleur pour le four de Leandro|Тепло для печи Леандро',
     'Rocko: Repariere Butterwerk, Supermarkt und erste Bäckerei. Benno und Leandro verbinden altes Handwerk mit neuen Rezepten.|Rocko: Repair butter factory, supermarket and first bakery. Benno and Leandro connect old craft with new recipes.|Rocko: Repara fábrica, supermercado y panadería. Benno y Leandro unen oficio y recetas.|Rocko : Répare beurrerie, supermarché et boulangerie. Benno et Leandro relient métier et recettes.|Рокко: Восстанови маслозавод, магазин и пекарню. Бенно и Леандро соединят ремесло и рецепты.'),
    ('local_supply','shared_supper',0,'Der Tisch nach der Lieferung|The table after delivery|La mesa tras la entrega|La table après la livraison|Стол после доставки',
     'Rocko: Automatisiere die drei Betriebe, produziere Butter und verkaufe Gebäck. Verlässliche Warenwege schaffen Zeit für ein gemeinsames Abendbrot.|Rocko: Automate the three businesses, produce butter and sell pastries. Reliable goods routes make time for a shared supper.|Rocko: Automatiza negocios, produce mantequilla y vende bollería. Las rutas fiables dejan tiempo para cenar juntos.|Rocko : Automatise les commerces, produis du beurre et vends des pâtisseries. Les trajets fiables libèrent du temps pour dîner ensemble.|Рокко: Автоматизируй предприятия, произведи масло и продай выпечку. Надёжные пути оставят время общему ужину.'),
    ('morning_courtyard','water_compact',2000,'2.000 Nachbarn, ein Wasserpakt|2,000 neighbours, one water compact|2.000 vecinos, un pacto del agua|2 000 voisins, un pacte de l’eau|2 000 соседей, один водный договор',
     'Rocko: Great Kushville zählt 2.000 eigene Einwohner. Nach den ersten Kapiteln aller acht ist der Wasserpakt im Bewohnerbereich bereit.|Rocko: Great Kushville has 2,000 local residents. After all eight first chapters the water compact awaits in the resident panel.|Rocko: La ciudad tiene 2.000 habitantes propios. Tras los ocho primeros capítulos espera el pacto en el panel.|Rocko : La ville compte 2 000 habitants. Après les huit premiers chapitres, le pacte attend dans le panneau.|Рокко: В городе 2 000 жителей. После восьми первых глав договор ждёт в разделе жителей.'),
    ('returning','shared_responsibility',4000,'Verantwortung bekommt viele Hände|Responsibility gains many hands|Muchas manos responsables|La responsabilité se partage|Ответственность в многих руках',
     'Rocko: 4.000 Einwohner tragen die Stadt. Die zweiten persönlichen Kapitel teilen Wissen und Dienste; alle acht bleiben hier.|Rocko: 4,000 residents sustain the city. The second personal chapters share knowledge and shifts; all eight stay here.|Rocko: 4.000 vecinos sostienen la ciudad. Los segundos capítulos comparten saber y turnos; los ocho permanecen.|Rocko : 4 000 habitants portent la ville. Les deuxièmes chapitres partagent savoir et permanences ; les huit restent.|Рокко: 4 000 жителей поддерживают город. Вторые главы разделят знания и дежурства; все восемь останутся.'),
    ('upper_lower','shared_exchange',8000,'Der offene Umschlagplatz|The open exchange|El centro de intercambio abierto|Le lieu d’échange ouvert|Открытый обменный двор',
     'Rocko: Great Kushville hat 8.000 Einwohner. Nach den zweiten Kapiteln aller acht öffnet das Gemeinschaftsprojekt den Umschlagplatz mit gleichen Regeln.|Rocko: Great Kushville has 8,000 residents. After all eight second chapters the community project opens an exchange with equal rules.|Rocko: Hay 8.000 habitantes. Tras los ocho segundos capítulos el proyecto abre intercambio con reglas iguales.|Rocko : Il y a 8 000 habitants. Après les huit deuxièmes chapitres, le projet ouvre un lieu aux règles égales.|Рокко: Здесь 8 000 жителей. После восьми вторых глав проект откроет двор с равными правилами.'),
    ('public_terrace','water_choice',12000,'Selmas neuer Raum|Selma’s new room|El lugar nuevo de Selma|Le nouveau lieu de Selma|Новое место Сельмы',
     'Rocko: 12.000 Einwohner leben hier. Selmas drittes Kapitel lässt dich Wasserlabor oder Kanalwerkstatt wählen. Die Entscheidung bleibt gespeichert und verändert den Ort.|Rocko: 12,000 residents live here. Selma’s third chapter lets you choose a water laboratory or canal workshop. The saved decision changes the place.|Rocko: Viven 12.000 habitantes. En el tercer capítulo de Selma eliges laboratorio o taller. La decisión guardada cambia el lugar.|Rocko : 12 000 habitants vivent ici. Au troisième chapitre de Selma, choisis laboratoire ou atelier. Le choix enregistré transforme le lieu.|Рокко: Здесь 12 000 жителей. В третьей главе Сельмы выбери лабораторию или мастерскую. Сохранённое решение изменит место.'),
    ('panorama','founders_walk',22000,'Viele Stimmen unter einem Bogen|Many voices beneath one arch|Muchas voces bajo un arco|Plusieurs voix sous une arche|Много голосов под одной аркой',
     'Rocko: Great Kushville hat 22.000 Einwohner. Nach den vierten Kapiteln aller acht ist das Gründerwegprojekt bereit. Alte Spuren und neue Wurzeln bekommen ihren eigenen Platz.|Rocko: Great Kushville has 22,000 residents. After all eight fourth chapters the founders’ walk project is ready. Old traces and new roots gain their own space.|Rocko: Hay 22.000 habitantes. Tras los ocho cuartos capítulos espera el paseo fundador. Huellas antiguas y raíces nuevas tienen sitio.|Rocko : Il y a 22 000 habitants. Après les huit quatrièmes chapitres, le parcours est prêt. Anciennes traces et nouvelles racines trouvent leur place.|Рокко: Здесь 22 000 жителей. После восьми четвёртых глав готов путь основателей. Старые следы и новые корни найдут место.'),
    ('home','connected_city',24000,'Die verbundene Stadt lebt weiter|The connected city lives on|La ciudad unida sigue viviendo|La ville reliée continue de vivre|Связанный город живёт дальше',
     'Rocko: 24.000 Einwohner tragen Great Kushville. Hole einen Tagesertrag des Supermarkts ab. Die Stadt ist frei ausbaubar; alle acht Bewohner, ihre Erinnerungen, Projekte und wiederkehrenden Aufgaben bleiben.|Rocko: 24,000 residents sustain Great Kushville. Collect the supermarket’s daily income. Develop the city freely; all eight residents, memories, projects and recurring tasks remain.|Rocko: 24.000 vecinos sostienen la ciudad. Cobra el ingreso del supermercado. Sigue creciendo; los ocho, sus recuerdos, proyectos y tareas permanecen.|Rocko : 24 000 habitants portent la ville. Collecte le revenu du supermarché. Développe-la librement ; les huit, leurs souvenirs, projets et missions demeurent.|Рокко: 24 000 жителей поддерживают город. Собери доход магазина. Развивай город; все восемь, их память, проекты и повторные дела останутся.'),
]

# The current quest is visible before its resident condition is fulfilled.
# Keep that milestone a target; never claim its population is already present.
# Only exact first-authored texts below may be migrated, including dialogues.
POPULATION_TARGET_REPAIRS = {
    'water_compact': (
        t('Great Kushville zählt 2.000 eigene Einwohner.|Great Kushville has 2,000 local residents.|La ciudad tiene 2.000 habitantes propios.|La ville compte 2 000 habitants.|В городе 2 000 жителей.'),
        t('Erreiche 2.000 eigene Einwohner in Great Kushville.|Reach 2,000 local residents in Great Kushville.|Alcanza 2.000 habitantes propios en Great Kushville.|Atteins 2 000 habitants à Great Kushville.|Достигни 2 000 жителей в Great Kushville.')),
    'shared_responsibility': (
        t('4.000 Einwohner tragen die Stadt.|4,000 residents sustain the city.|4.000 vecinos sostienen la ciudad.|4 000 habitants portent la ville.|4 000 жителей поддерживают город.'),
        t('Erreiche 4.000 Einwohner in Great Kushville.|Reach 4,000 residents in Great Kushville.|Alcanza 4.000 habitantes en Great Kushville.|Atteins 4 000 habitants à Great Kushville.|Достигни 4 000 жителей в Great Kushville.')),
    'shared_exchange': (
        t('Great Kushville hat 8.000 Einwohner.|Great Kushville has 8,000 residents.|Hay 8.000 habitantes.|Il y a 8 000 habitants.|Здесь 8 000 жителей.'),
        t('Erreiche 8.000 Einwohner in Great Kushville.|Reach 8,000 residents in Great Kushville.|Alcanza 8.000 habitantes en Great Kushville.|Atteins 8 000 habitants à Great Kushville.|Достигни 8 000 жителей в Great Kushville.')),
    'water_choice': (
        t('12.000 Einwohner leben hier.|12,000 residents live here.|Viven 12.000 habitantes.|12 000 habitants vivent ici.|Здесь 12 000 жителей.'),
        t('Erreiche 12.000 Einwohner in Great Kushville.|Reach 12,000 residents in Great Kushville.|Alcanza 12.000 habitantes en Great Kushville.|Atteins 12 000 habitants à Great Kushville.|Достигни 12 000 жителей в Great Kushville.')),
    'founders_walk': (
        t('Great Kushville hat 22.000 Einwohner.|Great Kushville has 22,000 residents.|Hay 22.000 habitantes.|Il y a 22 000 habitants.|Здесь 22 000 жителей.'),
        t('Erreiche 22.000 Einwohner in Great Kushville.|Reach 22,000 residents in Great Kushville.|Alcanza 22.000 habitantes en Great Kushville.|Atteins 22 000 habitants à Great Kushville.|Достигни 22 000 жителей в Great Kushville.')),
    'connected_city': (
        t('24.000 Einwohner tragen Great Kushville.|24,000 residents sustain Great Kushville.|24.000 vecinos sostienen la ciudad.|24 000 habitants portent la ville.|24 000 жителей поддерживают город.'),
        t('Erreiche 24.000 Einwohner in Great Kushville.|Reach 24,000 residents in Great Kushville.|Alcanza 24.000 habitantes en Great Kushville.|Atteins 24 000 habitants à Great Kushville.|Достигни 24 000 жителей в Great Kushville.')),
}


def population_target_body(qid, body):
    if qid not in POPULATION_TARGET_REPAIRS:
        return body
    previous, target = POPULATION_TARGET_REPAIRS[qid]
    for lang in LANGUAGES:
        if body[lang].count(previous[lang]) != 1:
            raise ValueError('Expected exact authored population phrase: ' + qid + '/' + lang)
    return {lang: value.replace(previous[lang], target[lang], 1) for lang, value in body.items()}


def correct_existing_population_targets(data, catalogs, added_texts):
    """Migrate only exact original Great text; reject independently edited values."""
    for qid, (previous, target) in POPULATION_TARGET_REPAIRS.items():
        for suffix in ('', '.dialogue'):
            key = 'gk.quest.' + qid + '.body' + suffix
            for lang in LANGUAGES:
                entries, field = (data['texts'], 'de') if lang == 'de' else (catalogs[lang]['entries'], 'value')
                # German has both a content fallback and a standalone catalog.
                sources = [(entries, field)] + ([(catalogs['de']['entries'], 'value')] if lang == 'de' else [])
                desired = added_texts[key][lang]
                original = desired.replace(target[lang], previous[lang], 1)
                for source, value_field in sources:
                    entry = next((e for e in source if e['key'] == key), None)
                    if entry is None:
                        continue
                    if entry[value_field] not in (original, desired):
                        raise ValueError('Edited Great Kushville population text: ' + key + '/' + lang)
                    entry[value_field] = desired


def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, ensure_ascii=False, separators=(',', ':')).encode()).hexdigest()


def fields(value):
    return sorted(f for _, f, _, _ in string.Formatter().parse(value) if f is not None)


def append_unique(target, entries, key):
    existing = {entry[key]: entry for entry in target}
    if len(existing) != len(target):
        raise ValueError('Duplicate existing ' + key)
    for entry in entries:
        identity = entry[key]
        if identity in existing:
            if existing[identity] != entry:
                raise ValueError('Existing authored content differs: ' + identity)
        else:
            target.append(entry)
            existing[identity] = entry


def author():
    data = json.loads(PATH.read_text(encoding='utf-8-sig'))
    catalogs = {lang: json.loads((LOCALES / (lang + '.json')).read_text(encoding='utf-8-sig')) for lang in LANGUAGES}
    before, before_catalogs = copy.deepcopy(data), copy.deepcopy(catalogs)
    if data['version'] < 11 or not set(SOURCES) <= {c['id'] for c in data['cities']}:
        raise ValueError('Great Kushville requires the ten-city catalog at version 11 or later.')
    added_texts = dict(TEXT)

    def text(key, translations):
        if set(translations) != set(LANGUAGES) or any(not value.strip() for value in translations.values()):
            raise ValueError('Missing translation: ' + key)
        if key in added_texts and added_texts[key] != translations:
            raise ValueError('Conflicting translation: ' + key)
        if any(fields(value) != fields(translations['de']) for value in translations.values()):
            raise ValueError('Placeholder mismatch: ' + key)
        added_texts[key] = translations
        return key

    original = [i for i in data['instances'] if i.get('cityId', 'burning_hills') == 'burning_hills']
    ids = {i['id']: PREFIX + i['id'] for i in original}
    instances = [dict(copy.deepcopy(i), id=ids[i['id']], cityId=CITY, introducedVersion=VERSION) for i in original]
    routes = [dict(copy.deepcopy(r), id=PREFIX + r['id'], **{'from': ids[r['from']], 'to': ids[r['to']]}) for r in data['routes'] if r['from'] in ids and r['to'] in ids]
    foundation = {q['id']: q for q in data['quests'] if q.get('cityId') == 'seven_hills'}
    ready = t('Alles bereit. Hole die Belohnung ab; dann öffnen wir den nächsten Weg.|Everything is ready. Claim the reward; then we open the next path.|Todo listo. Recoge el premio y abriremos el siguiente camino.|Tout est prêt. Réclame la récompense, puis ouvrons le prochain chemin.|Всё готово. Получи награду, затем откроем следующий путь.')
    quests = []
    for source, qid, residents, title, body in MAIN_ROWS:
        q = copy.deepcopy(foundation['sh_' + source])
        title, body = t(title), population_target_body(qid, t(body))
        q.update(id=PREFIX + qid, cityId=CITY, focus=q['focus'].replace('sh_', PREFIX, 1), titleKey=text('gk.quest.' + qid + '.title', title), bodyKey=text('gk.quest.' + qid + '.body', body))
        for condition in q['conditions']:
            if condition['target'].startswith('sh_'):
                condition['target'] = condition['target'].replace('sh_', PREFIX, 1)
            if condition['kind'] == 8 and residents:
                condition['count'] = residents
        text(q['bodyKey'] + '.dialogue', {lang: value.split(': ', 1)[-1] for lang, value in body.items()})
        text(q['bodyKey'] + '.dialogue_ready', ready)
        quests.append(q)

    def goal(kind, count, target=''):
        return dict(kind=kind, count=count, target=target, textKey='gk.goal.' + ('level' if kind == 0 else COUNTER_KEYS[kind]))

    stories = []
    for row in PEOPLE:
        pid, base = PREFIX + row['id'], 'gk.story.' + row['id']
        person = dict(id=pid, cityId=CITY, residents=row['residents'], focus=ids[row['focus']], repeatDelaySeconds=300, nameKey=text(base + '.name', tr(*([row['name']] * 5))), roleKey=text(base + '.role', row['role']), biographyKey=text(base + '.biography', row['bio']), greetingKey=text(base + '.greeting', row['greeting']), chapters=[], recurring=[])
        for index, c in enumerate(row['chapters']):
            key = base + '.chapter.' + str(index + 1)
            choices = [dict(id='help', textKey='gk.choice.help', outcomeKey='gk.outcome.help')]
            if row['id'] == 'selma' and index == 2:
                choices = [dict(id=choice, textKey='gk.choice.selma.' + choice, outcomeKey='gk.choice.selma.' + choice + '.outcome') for choice in ('water_lab', 'canal_workshop')]
            family = c['family']
            zone = 3 if family in ('butter_factory', 'supermarket', 'bakery') else 2 if family == 'park' else 0 if family in ('cultivation_plot', 'farm_warehouse', 'farm_processing') else 1
            person['chapters'].append(dict(id=pid + '_chapter_' + str(index + 1), titleKey=text(key + '.title', c['title']), bodyKey=text(key + '.body', c['body']), memoryKey=text(key + '.memory', c['memory']), completedKey='gk.story.completed', residents=row['thresholds'][index], moneyReward=400 + index * 250, zone=zone, goals=[goal(0, c['level'], family), goal(c['counter'], c['count'])], choices=choices, prerequisites=[], itemRewards=[]))
        for index, (kind, count, zone, label) in enumerate(REPEATS):
            key = base + '.repeat.' + str(index + 1)
            title = {lang: row['name'].split()[0] + ': ' + label[lang] for lang in LANGUAGES}
            body = {lang: row['repeat'][lang] + ' ' + REPEAT_INSTRUCTIONS[kind][lang] for lang in LANGUAGES}
            person['recurring'].append(dict(id=pid + '_repeat_' + str(index + 1), titleKey=text(key + '.title', title), bodyKey=text(key + '.body', body), completedKey='gk.repeat.completed', memoryKey='', residents=row['residents'], moneyReward=275, zone=zone, goals=[goal(kind, count)], choices=[dict(id='help', textKey='gk.choice.help', outcomeKey='gk.outcome.help')], prerequisites=[], itemRewards=[]))
        stories.append(person)
    projects = []
    for index, (joint_id, residents, zone, kind, count, chapter_round, title, body) in enumerate(PROJECTS):
        pid, key = 'gk_joint_' + joint_id, 'gk.joint.' + joint_id
        participants = [PREFIX + row['id'] for row in PEOPLE]
        prerequisites = [p + '_chapter_' + str(chapter_round) for p in participants]
        instruction = t('Seit Annahme: {0}.|Since acceptance: {0}.|Desde la aceptación: {0}.|Depuis acceptation : {0}.|После принятия: {0}.')
        body = {lang: body[lang] + ' ' + instruction[lang].format(COUNTER_ACTION[kind][lang].format(count)) for lang in LANGUAGES}
        q = dict(id=pid, titleKey=text(key + '.title', title), bodyKey=text(key + '.body', body), memoryKey=text(key + '.memory', title), completedKey='gk.joint.completed', residents=residents, moneyReward=2300, zone=zone, goals=[goal(kind, count)], choices=[dict(id='together', textKey='gk.choice.help', outcomeKey='gk.outcome.help')], prerequisites=prerequisites, itemRewards=[])
        projects.append(dict(id=pid, cityId=CITY, host=participants[index], participants=participants, quest=q))
    city = dict(id=CITY, nameKey='city.great_kushville', scenePath='Assets/Weedtopia/Game/Scenes/GreatKushville.unity', sourceCity='', unlockSources=SOURCES, introducedVersion=VERSION, unlockResidents=5000000, bridgeMoney=12500, bridgeCards=7, invitationTitleKey='campaign.great_kushville_title', invitationBodyKey='campaign.great_kushville_body', invitationNoticeKey='campaign.great_kushville_notice', invitationLockedKey='campaign.great_kushville_locked', dialoguePrefix='gk.dialog.')
    expected_prior, expected_prior_catalogs = copy.deepcopy(before), copy.deepcopy(before_catalogs)
    correct_existing_population_targets(data, catalogs, added_texts)
    correct_existing_population_targets(expected_prior, expected_prior_catalogs, added_texts)
    collections = [('cities',[city]),('instances',instances),('routes',routes),('quests',quests),('vipStories',stories),('vipCollaborations',projects)]
    for collection, entries in collections:
        append_unique(data[collection], entries, 'id')
    append_unique(data['texts'], [dict(key=k, de=v['de']) for k,v in added_texts.items()], 'key')
    for lang in LANGUAGES:
        append_unique(catalogs[lang]['entries'], [dict(key=k,value=v[lang]) for k,v in added_texts.items()], 'key')
    data['version'] = max(data['version'], VERSION)
    validate(data, catalogs, added_texts)
    additive = {name for name,_ in collections} | {'texts'}
    for name in additive:
        assert data[name][:len(before[name])] == expected_prior[name], 'Prior entries changed: ' + name
    for key in before.keys() - additive - {'version'}:
        assert before[key] == data[key], 'Prior field changed: ' + key
    for lang in LANGUAGES:
        assert catalogs[lang]['entries'][:len(before_catalogs[lang]['entries'])] == expected_prior_catalogs[lang]['entries'], 'Prior locale changed: ' + lang
    changed = data != before or catalogs != before_catalogs
    if data != before:
        PATH.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    for lang in LANGUAGES:
        if catalogs[lang] != before_catalogs[lang]:
            (LOCALES/(lang+'.json')).write_text(json.dumps(catalogs[lang],ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    receipt = dict(city=CITY,version=data['version'],changed=changed,instances=len(instances),routes=len(routes),mainQuests=len(quests),permanentResidents=len(stories),personalChapters=32,recurringVariants=24,communityProjects=len(projects),localizedKeys=len(added_texts),languages=list(LANGUAGES),oldContentDigest=digest(before),contentDigest=digest(data),priorContentPreserved=True,priorLocaleEntriesPreserved=True,allBuildingDefinitionsPreserved=True,placeholderParity=True,freshProgressGoals=True,prerequisiteGraphAcyclic=True,chapterRoundsBeforeProjects=True,savedPlaceChoice={'quest':'gk_selma_chapter_3','choices':['water_lab','canal_workshop']},residents=[dict(id=p['id'],appearsAt=p['residents'],chapters=[c['residents'] for c in p['chapters']]) for p in stories],projects=[dict(id=p['id'],residents=p['quest']['residents'],prerequisites=p['quest']['prerequisites']) for p in projects])
    receipt['authorizedPopulationTargetCorrection'] = dict(questIds=list(POPULATION_TARGET_REPAIRS), keys=12, languages=list(LANGUAGES), onlyExactFirstAuthoredGreatTexts=True, existingTenCitiesAndRequirementsPreserved=True)
    REPORT.parent.mkdir(parents=True,exist_ok=True)
    REPORT.write_text(json.dumps(receipt,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in receipt.items() if k not in ('residents','projects')},ensure_ascii=False))
    return receipt


def validate(data,catalogs,added_texts):
    city = next(c for c in data['cities'] if c['id']==CITY)
    assert city['unlockSources']==SOURCES and city['unlockResidents']==5000000
    assert (city['bridgeMoney'],city['bridgeCards'],city['introducedVersion'])==(12500,7,12)
    instances = [i for i in data['instances'] if i.get('cityId')==CITY]
    ids = {i['id'] for i in instances}
    assert len(instances)==56 and all(i['introducedVersion']==VERSION for i in instances)
    routes = [r for r in data['routes'] if r['from'] in ids]
    assert len(routes)==35 and all(r['to'] in ids and r['from']!=r['to'] for r in routes)
    quests = [q for q in data['quests'] if q.get('cityId')==CITY]
    families = {b['id'] for b in data['buildings']}
    assert len(quests)==18 and all(q['focus'] in ids for q in quests)
    assert all(not c['target'] or c['target'] in ids|families|{'mila','jaro','nia','rocko'} for q in quests for c in q['conditions'])
    people = [p for p in data['vipStories'] if p['cityId']==CITY]
    assert [p['id'] for p in people]==[PREFIX+p['id'] for p in PEOPLE]
    assert all(len(p['chapters'])==4 and len(p['recurring'])==3 and p['focus'] in ids and p['repeatDelaySeconds']==300 for p in people)
    projects = [p for p in data['vipCollaborations'] if p['cityId']==CITY]
    assert [p['id'] for p in projects]==['gk_joint_'+p[0] for p in PROJECTS]
    all_quests = [q for p in people for q in p['chapters']+p['recurring']]+[p['quest'] for p in projects]
    lookup = {q['id']:q for q in all_quests}
    assert len(lookup)==59
    visited,active=set(),set()
    def visit(qid):
        assert qid not in active, 'Cyclic prerequisite: '+qid
        if qid in visited:
            return
        active.add(qid)
        for prior in lookup[qid]['prerequisites']:
            assert prior in lookup, 'Unknown prerequisite: '+prior
            visit(prior)
        active.remove(qid)
        visited.add(qid)
    for qid in lookup:
        visit(qid)
    for p in people:
        thresholds=[q['residents'] for q in p['chapters']]
        assert thresholds[0]==p['residents'] and thresholds==sorted(set(thresholds)) and thresholds[-1]<22000
        assert all(not c['prerequisites'] for c in p['chapters'])
        assert all(not q['memoryKey'] for q in p['recurring'])
        for index,q in enumerate(p['chapters']):
            expected=['water_lab','canal_workshop'] if p['id']=='gk_selma' and index==2 else ['help']
            assert [c['id'] for c in q['choices']]==expected
            assert len({c['outcomeKey'] for c in q['choices']})==len(expected)
    for q in all_quests:
        assert any(g['kind'] in COUNTER_KEYS for g in q['goals']) and all(g['count']>0 for g in q['goals'])
        assert all(g['target'] in families|ids for g in q['goals'] if g['kind']==0)
    for index,p in enumerate(projects):
        expected=[v['id']+'_chapter_'+str(PROJECTS[index][5]) for v in people]
        assert p['quest']['prerequisites']==expected
        assert all(lookup[q]['residents']<=p['quest']['residents'] for q in expected)
        assert p['host'] in p['participants'] and set(p['participants'])=={v['id'] for v in people}
    texts={entry['key']:entry['de'] for entry in data['texts']}
    assert len(texts)==len(data['texts'])
    required=set(added_texts)
    repeat_keys={'gk.story.'+p['id']+'.repeat.'+str(i)+'.body' for p in PEOPLE for i in (1,2,3)}
    assert repeat_keys<=required
    assert all(not any(c.isdigit() for c in value) for key in repeat_keys for value in added_texts[key].values()), 'Recurring prose embeds unscaled amounts'
    for lang in LANGUAGES:
        entries={e['key']:e['value'] for e in catalogs[lang]['entries']}
        assert len(entries)==len(catalogs[lang]['entries']) and required<=entries.keys()
        assert all(entries[k].strip() and fields(entries[k])==fields(texts[k]) for k in required)
        for qid, (previous, target) in POPULATION_TARGET_REPAIRS.items():
            for suffix in ('', '.dialogue'):
                value = entries['gk.quest.' + qid + '.body' + suffix]
                assert target[lang] in value and previous[lang] not in value, 'Premature population claim: ' + qid + '/' + lang
    assert not any(other in value for translations in added_texts.values() for value in translations.values() for other in ('Saint Mary Ville','Golden Flower','Purple Garden','Seven Hills'))


if __name__=='__main__':
    author()

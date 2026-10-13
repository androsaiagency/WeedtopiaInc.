"""Explicit additive Great Sativadam authoring; never runs during Unity import."""
import copy
import json
import pathlib

from author_green_leaf_village import (ACTION, COUNTER_ACTION, COUNTER_KEYS,
                                      FAMILY_NAMES, LANGUAGES, TEXT as SHARED_TEXT, tr)
from author_golden_flower import REPEAT_INSTRUCTIONS
from author_great_kushville import append_unique, digest, fields
from mainrows_great_sativadam import MAIN_ROWS

ROOT = pathlib.Path(__file__).resolve().parents[2]
PATH = ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json'
LOCALES = ROOT / 'Assets/Weedtopia/Game/Localization'
REPORT = ROOT / 'Docs/GreatSativadam/content_receipt.json'
CITY, PREFIX, VERSION = 'great_sativadam', 'gs_', 14
SOURCES = ['burning_hills', 'big_rock_city', 'seven_hills', 'little_montana',
           'cassa_vadela', 'mark_wood_city', 'green_leaf_village', 'purple_garden',
           'golden_flower', 'saint_mary_ville', 'great_kushville', 'skunkfurt']


def t(value):
    """Five authored translations in de/en/es/fr/ru order."""
    values = value.split('|')
    if len(values) != 5:
        raise ValueError('Expected five translations: ' + value)
    return tr(*values)


REPEAT_INSTRUCTIONS = {
    **REPEAT_INSTRUCTIONS,
    9: t('Schließe neue Lieferungen seit Annahme ab. Das Auftragsziel zeigt die aktuelle Menge.|Complete new deliveries since acceptance. The quest goal shows the current amount.|Completa entregas nuevas desde la aceptación. El objetivo muestra la cantidad actual.|Termine de nouvelles livraisons depuis acceptation. L’objectif indique la quantité actuelle.|Заверши новые доставки после принятия. Цель задания показывает текущий объём.'),
}


TEXT = {
    'city.great_sativadam': tr(*(['Great Sativadam'] * 5)),
    'campaign.great_sativadam_title': t('Rocko: Wem gehört der Weg zum Wasser?|Rocko: Who has a place by the water?|Rocko: ¿Quién tiene acceso al agua?|Rocko : À qui appartient le chemin vers l’eau ?|Рокко: Кто имеет право на путь к воде?'),
    'campaign.great_sativadam_body': t('Zwanzig Millionen Menschen leben in unseren zwölf bisherigen Städten zusammen. Great Sativadam wurde aus einer Deichgemeinschaft zur Kanal- und Hafenstadt. Ihre ersten Wasserwege gehörten denen, die sie gemeinsam pflegten. Heute schließen starre Warenzeiten die Fähre aus, während alte Landrechte Hausbootgärten und gemeinsame Ufer bedrohen. Noor, Bram, Esra, Daan, Fenna und Joost suchen einen neuen Vertrag zwischen sicherer Arbeit, offenem Zugang und lebendigem Wasser. Mit 17.500 Geld und 9 Teamkarten schaffen wir einen Gezeitenhof, einen Saatgutkai und einen Vertrag der offenen Wasserwege.|Twenty million people live across our twelve existing cities. Great Sativadam grew from a dike community into a canal and port city. Its first waterways belonged to those who cared for them together. Today rigid cargo slots exclude the ferry, while old land claims threaten houseboat gardens and shared banks. Noor, Bram, Esra, Daan, Fenna and Joost seek a new agreement for safe work, open access and living water. With 17,500 money and 9 team cards, we create a tide court, a seed quay and an open waterways charter.|Veinte millones de personas viven en nuestras doce ciudades anteriores. Great Sativadam pasó de una comunidad de diques a una ciudad de canales y puerto. Sus primeros cauces pertenecían a quienes los cuidaban juntos. Hoy los horarios rígidos de carga excluyen al transbordador, y antiguos derechos sobre el suelo amenazan jardines flotantes y orillas comunes. Noor, Bram, Esra, Daan, Fenna y Joost buscan un acuerdo para trabajar con seguridad, acceder al agua y conservarla viva. Con 17.500 de dinero y 9 cartas de equipo creamos un patio de mareas, un muelle de semillas y un pacto de vías acuáticas abiertas.|Vingt millions de personnes vivent dans nos douze villes précédentes. Great Sativadam est passée d’une communauté de digues à une ville de canaux et de port. Ses premiers cours d’eau appartenaient à ceux qui les entretenaient ensemble. Aujourd’hui, les horaires rigides du fret excluent le bac ; d’anciens droits fonciers menacent jardins flottants et berges communes. Noor, Bram, Esra, Daan, Fenna et Joost cherchent un accord entre travail sûr, accès ouvert et eau vivante. Avec 17 500 d’argent et 9 cartes d’équipe, créons une cour des marées, un quai des semences et une charte des voies d’eau ouvertes.|В наших двенадцати предыдущих городах вместе живут двадцать миллионов человек. Great Sativadam вырос из общины строителей дамб в город каналов и портов. Первые водные пути принадлежали тем, кто вместе за ними ухаживал. Теперь жёсткие грузовые часы вытесняют паром, а старые земельные права угрожают садам на плавучих домах и общим берегам. Нур, Брам, Эсра, Даан, Фенна и Йост ищут соглашение о безопасной работе, свободном доступе и живой воде. С 17 500 денег и 9 карточками команды создадим двор приливов, семенной причал и договор открытых водных путей.'),
    'campaign.great_sativadam_notice': t('Rocko: Great Sativadam öffnet seine Schleuse. Sechs dauerhafte Nachbarn verbinden Deichgeschichte mit einem neuen Zugang zum Wasser.|Rocko: Great Sativadam opens its lock. Six permanent neighbours connect dike history with a new way to share the water.|Rocko: Great Sativadam abre su esclusa. Seis vecinos permanentes unen la historia de los diques con un nuevo acceso al agua.|Rocko : Great Sativadam ouvre son écluse. Six voisins permanents relient l’histoire des digues à un nouvel accès à l’eau.|Рокко: Great Sativadam открывает шлюз. Шесть постоянных соседей связывают историю дамб с новым доступом к воде.'),
    'campaign.great_sativadam_locked': t('Ab {0} Einwohnern in allen zwölf bisherigen Städten zusammen. Aktuell: {1}.|Available at {0} residents across all twelve previous cities. Current total: {1}.|Disponible con {0} habitantes entre las doce ciudades anteriores. Total actual: {1}.|Disponible à {0} habitants dans les douze villes précédentes réunies. Total actuel : {1}.|Откроется при {0} жителях во всех двенадцати предыдущих городах вместе. Сейчас: {1}.'),
    'district.great_sativadam.farm': t('Deichgärten|Dike Gardens|Jardines del Dique|Jardins des digues|Сады у дамбы'),
    'district.great_sativadam.city': t('Saatgutkai|Seed Quay|Muelle de Semillas|Quai des semences|Семенной причал'),
    'district.great_sativadam.settlement': t('Hausbootviertel|Houseboat Quarter|Barrio de Casas Flotantes|Quartier des maisons flottantes|Квартал плавучих домов'),
    'district.great_sativadam.industry': t('Werftbogen|Shipyard Bend|Recodo de los Astilleros|Courbe des chantiers|Изгиб верфи'),
    'gs.dialog.mila': t('Bram bewahrt Saatgut auf seinem Hausboot, Noor kennt die Wasserstände. In den Deichgärten beginnen Versorgung und gemeinsame Wasserpflege.|Bram keeps seeds on his houseboat; Noor knows the water levels. Dike Gardens are where supplies and shared water care begin.|Bram conserva semillas en su casa flotante y Noor conoce los niveles. En los jardines comienzan el abastecimiento y el cuidado común del agua.|Bram garde des graines sur sa péniche, Noor connaît les niveaux. Les jardins ouvrent la voie au ravitaillement et à l’entretien partagé de l’eau.|Брам хранит семена на плавучем доме, Нур знает уровни воды. В садах начинаются снабжение и общая забота о воде.'),
    'gs.dialog.jaro': t('Joost will zuverlässige Werkzeuge, Fenna verlässliche Zutaten. Die vorhandene Produktionskette gibt beiden Zeit, Verantwortung zu teilen.|Joost needs reliable tools and Fenna reliable ingredients. The existing production chain gives them time to share responsibility.|Joost necesita herramientas fiables y Fenna ingredientes seguros. La cadena de producción existente les permite compartir responsabilidades.|Joost veut des outils fiables, Fenna des ingrédients sûrs. La chaîne existante leur laisse le temps de partager les responsabilités.|Йосту нужны надёжные инструменты, Фенне — продукты. Существующая цепочка производства оставит время делиться ответственностью.'),
    'gs.dialog.nia': t('Esra kennt Menschen hinter den Fahrzeiten. Unsere Fahrer bleiben auf den vorhandenen Routen; ihre Übergaben helfen Noor, Menschen und Waren im Schleusenplan zu berücksichtigen.|Esra knows the people behind the timetables. Our drivers follow the existing routes; their handovers help Noor include people and goods in the lock schedule.|Esra conoce a las personas detrás de los horarios. Los conductores usan las rutas existentes; sus relevos ayudan a Noor a incluir viajeros y mercancías en el plan.|Esra connaît les gens derrière les horaires. Les chauffeurs suivent les trajets existants ; leurs relais aident Noor à intégrer personnes et marchandises au planning.|Эсра знает людей за расписаниями. Водители используют существующие маршруты; передачи помогут Нур учесть в плане шлюза людей и товары.'),
    'gs.dialog.rocko': t('Daan findet alte Rechte, aber auch alte Pflichten zur Pflege. Great Sativadam entscheidet gemeinsam, wie sein Wasserweg ein Zuhause bleiben kann.|Daan finds old rights, but also old duties of care. Great Sativadam decides together how its waterway can remain a home.|Daan encuentra derechos antiguos y deberes de cuidado. Great Sativadam decide en común cómo conservar su cauce como hogar.|Daan retrouve les anciens droits, mais aussi les devoirs d’entretien. Great Sativadam décide ensemble comment garder ses voies d’eau accueillantes.|Даан найдёт старые права и обязанности по уходу. Great Sativadam вместе решит, как сохранить водный путь домом.'),
    'gs.choice.help': t('Diesen Wasserweg gemeinsam pflegen|Care for this waterway together|Cuidar juntos este cauce|Entretenir cette voie d’eau ensemble|Вместе заботиться об этом водном пути'),
    'gs.outcome.help': t('Unser Beitrag bleibt am Ufer sichtbar. Zugang, Pflege und Verantwortung werden mit Namen und verlässlichen Absprachen geteilt.|Our contribution stays visible on the bank. Access, care and responsibility are shared through names and reliable agreements.|Nuestra aportación queda visible en la orilla. Compartimos acceso, cuidado y responsabilidad mediante nombres y acuerdos fiables.|Notre contribution reste visible sur la berge. Accès, entretien et responsabilité se partagent avec des noms et des accords fiables.|Наш вклад останется видимым на берегу. Доступ, уход и ответственность разделены поимённо и закреплены надёжными договорённостями.'),
    'gs.story.completed': t('Dieses Kapitel bleibt meine Erinnerung. Ich bleibe in Great Sativadam und übernehme mit meinen Nachbarn die nächste Pflege.|This chapter remains my memory. I stay in Great Sativadam and share the next care task with my neighbours.|Este capítulo queda en mi memoria. Me quedo en Great Sativadam y comparto el próximo cuidado con mis vecinos.|Ce chapitre reste dans ma mémoire. Je reste à Great Sativadam et partage le prochain entretien avec mes voisins.|Эта глава останется в моей памяти. Я остаюсь в Great Sativadam и разделю следующую заботу с соседями.'),
    'gs.repeat.completed': t('Für heute geschafft. Nach einer Pause wartet die nächste gemeinsame Arbeit am Wasser.|Done for today. After a break, another shared task by the water awaits.|Por hoy está hecho. Tras una pausa espera otra tarea común junto al agua.|C’est fait pour aujourd’hui. Après une pause, un autre travail partagé au bord de l’eau nous attend.|На сегодня готово. После перерыва ждёт следующая общая работа у воды.'),
    'gs.joint.completed': t('Dieser gemeinsame Ort bleibt offen. Sechs Geschichten verbinden sicheren Betrieb mit Zugang und Wasserpflege.|This shared place stays open. Six stories connect safe operation with access and care for the water.|Este lugar común sigue abierto. Seis historias unen un trabajo seguro con acceso y cuidado del agua.|Ce lieu commun reste ouvert. Six récits relient fonctionnement sûr, accès et entretien de l’eau.|Это общее место останется открытым. Шесть историй соединят безопасную работу, доступ и заботу о воде.'),
    'gs.place.tide_court': t('Gezeitenhof an der Schleuse|Tide court by the lock|Patio de mareas junto a la esclusa|Cour des marées près de l’écluse|Двор приливов у шлюза'),
    'gs.place.seed_quay': t('Offener Saatgutkai|Open seed quay|Muelle de semillas abierto|Quai des semences ouvert|Открытый семенной причал'),
    'gs.place.open_water_charter': t('Vertrag der offenen Wasserwege|Open waterways charter|Pacto de vías acuáticas abiertas|Charte des voies d’eau ouvertes|Договор открытых водных путей'),
    'gs.place.repair_wharf': t('Joosts gemeinschaftliche Reparaturwerft|Joost’s community repair wharf|El muelle de reparación comunitario de Joost|Le quai de réparation partagé de Joost|Общая ремонтная верфь Йоста'),
    'gs.place.learning_dock': t('Joosts offene Lehranlegestelle|Joost’s open learning dock|El embarcadero de aprendizaje abierto de Joost|L’embarcadère d’apprentissage ouvert de Joost|Открытый учебный причал Йоста'),
    'gs.choice.joost.repair_wharf': t('Die Werft für gemeinsame Reparaturen öffnen|Open the wharf for shared repairs|Abrir el muelle a reparaciones compartidas|Ouvrir le quai aux réparations partagées|Открыть верфь для общего ремонта'),
    'gs.choice.joost.learning_dock': t('Eine offene Lehranlegestelle einrichten|Create an open learning dock|Crear un embarcadero de aprendizaje abierto|Créer un embarcadère d’apprentissage ouvert|Создать открытый учебный причал'),
    'gs.choice.joost.repair_wharf.outcome': t('Joost legt Werkzeug und Wartungsbuch an die gemeinsame Werkbank. Esra koordiniert Liegezeiten, Noor Sicherheitsprüfungen; Nachbarn reparieren ihre Boote gemeinsam und halten den Uferzugang frei.|Joost puts tools and the maintenance ledger on a shared bench. Esra coordinates berthing, Noor safety checks; neighbours repair boats together and keep access to the bank open.|Joost coloca herramientas y libro de mantenimiento en un banco común. Esra coordina atraques, Noor las revisiones; los vecinos reparan juntos y mantienen libre el acceso.|Joost pose outils et carnet d’entretien sur un établi partagé. Esra coordonne les places, Noor les contrôles ; les voisins réparent ensemble et gardent la berge accessible.|Йост положит инструменты и журнал на общий верстак. Эсра согласует стоянки, Нур — проверки; соседи вместе отремонтируют лодки и сохранят доступ к берегу.'),
    'gs.choice.joost.learning_dock.outcome': t('Joost stellt einen Übungskahn und einen offenen Lehrplan an den Steg. Bram erklärt schwimmende Gärten, Esra sichere Übergaben; angehende Handwerker lernen mit echten Aufgaben und gemeinsam gepflegten Werkzeugen.|Joost brings a practice boat and an open lesson plan to the dock. Bram explains floating gardens, Esra safe handovers; new craftspeople learn through real tasks and shared tools.|Joost lleva una barca de práctica y un plan abierto al embarcadero. Bram enseña jardines flotantes, Esra relevos seguros; los aprendices trabajan con tareas reales y herramientas comunes.|Joost installe un bateau d’exercice et un programme ouvert au ponton. Bram explique les jardins flottants, Esra les relais sûrs ; les apprentis apprennent par des tâches réelles et des outils partagés.|Йост поставит учебную лодку и открытый план у причала. Брам объяснит плавучие сады, Эсра — безопасные передачи; ученики будут осваивать реальные задачи с общими инструментами.'),
}
TEXT.update({key.replace('gl.goal.', 'gs.goal.', 1): value for key, value in SHARED_TEXT.items() if key.startswith('gl.goal.')})
PEOPLE = []


def chapter(title, scene, family, level, counter, count):
    title, scene = t(title), t(scene)
    return dict(title=title, body={lang: scene[lang] + ' ' + ACTION[lang].format(FAMILY_NAMES[family][lang], level, COUNTER_ACTION[counter][lang].format(count)) for lang in LANGUAGES}, memory=title, family=family, level=level, counter=counter, count=count)


def person(id, name, residents, focus, thresholds, role, bio, greeting, chapters, repeat):
    PEOPLE.append(dict(id=id, name=name, residents=residents, focus=focus, thresholds=thresholds, role=t(role), bio=t(bio), greeting=t(greeting), chapters=chapters, repeat=t(repeat)))


person('noor', 'Noor van Dijk', 50, 'farm_warehouse', [50,2300,7100],
       'Schleuseningenieurin|Lock engineer|Ingeniera de esclusas|Ingénieure des écluses|Инженер шлюзов',
       'Noor stammt aus einer Deichbauerfamilie. Ihr strenger Schleusenplan sollte Unfälle verhindern und übersah Esras letzte Fähre. Brams trockengefallener Garten zeigt ihr, dass sichere Zahlen die Erfahrung der Menschen am Wasser brauchen.|Noor comes from dike builders. Her strict lock plan prevented accidents but missed Esra’s last ferry. Bram’s dry garden teaches her that safe calculations need the experience of people by the water.|Noor viene de constructores de diques. Su plan rígido evitaba accidentes, pero olvidaba el último ferry. El jardín seco de Bram le enseña que los cálculos necesitan la experiencia vecinal.|Noor vient de bâtisseurs de digues. Son planning strict prévenait les accidents mais oubliait le dernier bac. Le jardin asséché de Bram lui apprend que les calculs ont besoin de l’expérience des riverains.|Нур из семьи строителей дамб. Строгий план шлюза предотвращал аварии, но забывал последний паром. Высохший сад Брама научит её учитывать опыт жителей у воды.',
       'Ein Wasserstand hat auch eine Geschichte.|A water level has a story too.|Un nivel de agua también tiene historia.|Un niveau d’eau a aussi son histoire.|У уровня воды тоже есть история.', [
    chapter('Der Strich über der Fähre|The line across the ferry|La línea sobre el ferry|Le trait sur le bac|Черта поверх парома', 'Esra zeigt Noor die gestrichene letzte Fahrt. Noor ergänzt reale Übergaben, statt nur Waren zu zählen.|Esra shows the cancelled last crossing. Noor adds real handovers instead of counting only cargo.|Esra muestra la travesía suprimida. Noor añade relevos reales en vez de contar solo cargas.|Esra montre la traversée supprimée. Noor ajoute les relais réels au-delà du fret.|Эсра покажет отменённый рейс. Нур добавит реальные передачи, учитывая не только грузы.', 'farm_warehouse',1,8,25),
    chapter('Die Pegeltafel gehört allen|The level board belongs to everyone|La tabla de niveles es de todos|Le tableau des niveaux est à tous|Таблица уровней принадлежит всем', 'Noor erklärt Grenzwerte am Gezeitenhof. Fenna ergänzt Lieferzeiten, Bram Gartenruhe; gemeinsam planen sie Kontrollen.|Noor explains limits at the tide court. Fenna adds deliveries, Bram garden rest; they plan checks together.|Noor explica límites en el patio. Fenna añade entregas, Bram descansos; planifican revisiones juntos.|Noor explique les limites dans la cour. Fenna ajoute livraisons, Bram repos ; les contrôles sont communs.|Нур объяснит пределы во дворе. Фенна добавит поставки, Брам — покой сада; проверки станут общими.', 'park',7,8,90),
    chapter('Eine Schleuse mit geteiltem Wissen|A lock with shared knowledge|Una esclusa con saber compartido|Une écluse au savoir partagé|Шлюз с общими знаниями', 'Noor übergibt Joost ein lesbares Wartungsbuch und bildet Vertretungen aus. Sichere Entscheidungen hängen nicht mehr nur von ihr ab.|Noor gives Joost a clear maintenance book and trains relief staff. Safe decisions no longer depend on her alone.|Noor entrega un manual a Joost y forma suplentes. La seguridad ya no depende solo de ella.|Noor donne un carnet clair à Joost et forme des remplaçants. La sécurité ne dépend plus d’elle seule.|Нур передаст Йосту понятный журнал и обучит сменщиков. Безопасность больше не зависит лишь от неё.', 'farm_processing',15,4,160)],
       'Noor prüft Pegel mit Nachbarn und hält den gemeinsamen Schleusenplan lesbar.|Noor checks levels with neighbours and keeps the shared lock plan clear.|Noor revisa niveles con vecinos y mantiene claro el plan común.|Noor vérifie les niveaux avec les voisins et garde le plan lisible.|Нур проверяет уровни с соседями и сохраняет общий план понятным.')

person('bram', 'Bram de Vries', 150, 'coffee_01', [150,3000,8100],
       'Saatgut- und Hausbootgärtner|Seed and houseboat gardener|Jardinero de semillas y casas flotantes|Jardinier des semences et des péniches|Садовник семян и плавучих домов',
       'Bram erbte eine schwimmende Saatgutkiste, aber keinen eindeutigen Landtitel. Aus Räumungsangst versteckte er seine besten Sorten. Daan entdeckt alte Pflegepflichten am Ufer. Bram lernt zu teilen, ohne sein Zuhause aufzugeben.|Bram inherited a floating seed chest without clear land rights. Fear of eviction made him hide his best varieties. Daan discovers old duties of care. Bram learns to share while keeping his home.|Bram heredó semillas flotantes sin un título claro. Por miedo al desalojo ocultó sus mejores variedades. Daan descubre deberes antiguos. Bram aprende a compartir sin perder su hogar.|Bram a hérité d’un coffre de graines sans droits clairs. La peur d’être expulsé lui faisait cacher ses variétés. Daan retrouve les devoirs anciens. Bram apprend à partager sans perdre son foyer.|Брам унаследовал семена без ясного земельного права. Страх выселения заставлял его прятать лучшие сорта. Даан найдёт старые обязанности по уходу. Брам научится делиться, сохранив дом.',
       'Ein Garten braucht Wurzeln, auch auf einem Boot.|A garden needs roots, even on a boat.|Un jardín necesita raíces, incluso en un barco.|Un jardin a besoin de racines, même sur un bateau.|Саду нужны корни даже на лодке.', [
    chapter('Die Kiste unter dem Steg|The chest beneath the dock|El cofre bajo el embarcadero|Le coffre sous le ponton|Сундук под причалом', 'Fenna findet die versteckte Saatgutkiste. Bram erzählt von seiner Räumungsangst und teilt eine erste Sorte.|Fenna finds the hidden seed chest. Bram speaks of eviction fears and shares his first variety.|Fenna encuentra el cofre. Bram habla de su miedo al desalojo y comparte una primera variedad.|Fenna trouve le coffre. Bram raconte sa peur de l’expulsion et partage une première variété.|Фенна найдёт сундук. Брам расскажет о страхе выселения и поделится первым сортом.', 'cultivation_plot',1,3,35),
    chapter('Ein Samen kehrt zurück|A seed comes back|Una semilla regresa|Une graine revient|Семя возвращается', 'Esra bringt Samen aus einem Nachbargarten zurück. Bram öffnet sein Sortenbuch und vereinbart Pflege statt stiller Besitzansprüche.|Esra returns seeds from a neighbouring garden. Bram opens his variety book and agrees on care instead of silent ownership claims.|Esra devuelve semillas vecinales. Bram abre su catálogo y acuerda cuidados en vez de posesión silenciosa.|Esra rapporte des graines voisines. Bram ouvre son catalogue et choisit l’entretien plutôt que des droits tacites.|Эсра вернёт соседские семена. Брам откроет каталог и договорится об уходе вместо молчаливых притязаний.', 'coffee_shop',7,5,100),
    chapter('Der Garten hat mehr als einen Hüter|The garden has more than one keeper|El jardín tiene más de un guardián|Le jardin a plusieurs gardiens|У сада больше одного хранителя', 'Neue Hausbootnachbarn teilen die Pflege. Brams Liegeplatz bleibt geschützt; sein Gartenplan verbindet Zuhause und Verantwortung.|New houseboat neighbours share care. Bram’s berth stays protected; his garden plan connects home and responsibility.|Nuevos vecinos flotantes comparten cuidados. El atraque de Bram queda protegido; el plan une hogar y responsabilidad.|De nouveaux voisins partagent l’entretien. La place de Bram reste protégée ; le plan relie foyer et responsabilité.|Новые соседи разделят уход. Стоянка Брама останется защищённой; план соединит дом и ответственность.', 'farm_warehouse',15,3,180)],
       'Bram tauscht Saatgut und dokumentiert die Pflege der schwimmenden Gärten.|Bram exchanges seeds and records care for floating gardens.|Bram intercambia semillas y registra los cuidados de jardines flotantes.|Bram échange des graines et note l’entretien des jardins flottants.|Брам обменивается семенами и записывает уход за плавучими садами.')

person('esra', 'Esra Vermeer', 300, 'city_warehouse', [300,3600,9200],
       'Fährfahrerin|Ferry skipper|Patrona del transbordador|Conductrice du bac|Капитан парома',
       'Esra verbindet Hausboote, Gärten und Backstuben. Der Warenplan nennt ihre Passagiere Verzögerung. Heimliche Fahrten nach Dienstschluss erschöpfen sie. Sie will Zugang, der nicht von ihrer ständigen Ausnahme abhängt.|Esra connects houseboats, gardens and bakeries. The cargo plan calls her passengers a delay. Secret trips after her shift exhaust her. She wants access that does not depend on her endless exceptions.|Esra une casas flotantes, jardines y panaderías. El plan de carga llama retraso a sus pasajeros. Viajes secretos fuera de turno la agotan. Quiere acceso sin excepciones constantes.|Esra relie péniches, jardins et boulangeries. Le fret traite ses passagers comme un retard. Les trajets secrets après son service l’épuisent. Elle veut un accès sans exceptions permanentes.|Эсра связывает плавучие дома, сады и пекарни. Грузовой план считает пассажиров задержкой. Тайные рейсы после смены истощают её. Нужен доступ без постоянных исключений.',
       'Die letzte Fahrt darf kein heimliches Versprechen sein.|The last crossing must not be a secret promise.|La última travesía no debe ser una promesa secreta.|La dernière traversée ne doit pas être une promesse secrète.|Последний рейс не должен быть тайным обещанием.', [
    chapter('Menschen auf der Ladeliste|People on the cargo list|Personas en la lista de carga|Des gens sur la liste du fret|Люди в грузовом списке', 'Esra schreibt Passagiernamen neben Warenzeiten. Noor erkennt, dass eine gestrichene Fahrt echte Heimwege unterbricht.|Esra writes passenger names beside cargo slots. Noor sees that cancelling a crossing cuts real journeys home.|Esra escribe nombres junto a cargas. Noor comprende que suprimir una travesía corta caminos a casa.|Esra inscrit les noms près du fret. Noor voit que supprimer un trajet coupe de vrais retours au foyer.|Эсра запишет имена рядом с грузами. Нур увидит, что отмена прерывает настоящие пути домой.', 'city_warehouse',1,9,3),
    chapter('Der freie Sitz im Fahrplan|The free seat in the timetable|El asiento libre en el horario|La place libre dans les horaires|Свободное место в расписании', 'Fenna bündelt Lieferungen für ein verlässliches Fährfenster. Joost prüft den Steg; eine Pause steht erstmals im gemeinsamen Plan.|Fenna groups deliveries for a reliable ferry slot. Joost checks the dock; a break finally enters the shared plan.|Fenna agrupa entregas para una franja fiable. Joost revisa el embarcadero; el plan incluye una pausa.|Fenna regroupe les livraisons pour le bac. Joost vérifie le ponton ; une pause entre au planning.|Фенна объединит поставки для окна парома. Йост проверит причал; в плане впервые появится отдых.', 'supermarket',7,9,6),
    chapter('Eine Fähre ohne Heldenschicht|A ferry without a heroic shift|Un ferry sin turnos heroicos|Un bac sans service héroïque|Паром без героической смены', 'Esra bildet eine Vertretung aus. Der Zugang bleibt offen, während sie mit Bram am Ufer sitzt; Fürsorge wird gemeinsame Aufgabe.|Esra trains relief staff. Access stays open while she sits with Bram on the bank; care becomes shared work.|Esra forma una suplente. El acceso sigue abierto mientras descansa con Bram; cuidar es trabajo común.|Esra forme une remplaçante. L’accès reste ouvert pendant qu’elle s’assoit avec Bram ; le soin devient partagé.|Эсра обучит сменщицу. Доступ останется открытым, пока она отдыхает с Брамом; забота станет общей.', 'city_warehouse',15,9,10)],
       'Esra stimmt Fahrfenster mit Fahrgästen und Betrieben ab und teilt Dienste.|Esra agrees crossing slots with passengers and businesses and shares shifts.|Esra acuerda horarios con pasajeros y negocios y comparte turnos.|Esra ajuste les créneaux avec passagers et commerces et partage les services.|Эсра согласует окна с пассажирами и предприятиями и делится сменами.')

person('daan', 'Daan Koster', 500, 'settlement_02', [500,4400,10200],
       'Kartograf und Stadtarchivar|Cartographer and city archivist|Cartógrafo y archivero urbano|Cartographe et archiviste municipal|Картограф и городской архивист',
       'Daan zeichnet klare Grenzen und bewahrt Deichakten. Sein schöner Plan ließ schwimmende Gärten und mündliche Absprachen aus. Als ein Landanspruch die Karte als Beweis nutzt, beginnt er, Rechte und Pflegepflichten mit Betroffenen sichtbar zu machen.|Daan draws clear boundaries and keeps dike records. His elegant map omitted floating gardens and spoken agreements. When a land claim uses it as proof, he begins mapping rights and care duties with those affected.|Daan traza límites y conserva actas de diques. Su bello mapa omitía jardines flotantes y acuerdos verbales. Cuando un reclamo de tierras lo usa como prueba, muestra derechos y deberes junto a los afectados.|Daan trace des limites et garde les actes des digues. Sa belle carte omettait jardins flottants et accords oraux. Quand une revendication l’utilise comme preuve, il montre droits et devoirs avec les riverains.|Даан чертит границы и хранит акты дамб. Красивая карта забывала плавучие сады и устные соглашения. Когда её используют для земельного требования, он отмечает права и обязанности вместе с жителями.',
       'Eine leere Stelle auf der Karte ist kein leerer Ort.|A blank space on a map is not an empty place.|Un vacío en el mapa no es un lugar vacío.|Un blanc sur la carte n’est pas un lieu vide.|Пустое место на карте не означает пустое место в жизни.', [
    chapter('Die Insel, die nicht eingezeichnet war|The island missing from the map|La isla ausente del mapa|L’île absente de la carte|Остров, которого нет на карте', 'Bram führt Daan durch seinen Garten. Daan ergänzt Liegeplatz, Pflege und die Menschen, die sein Plan übersehen hat.|Bram guides Daan through his garden. Daan adds the berth, care and people his map overlooked.|Bram enseña su jardín. Daan añade atraque, cuidados y personas olvidadas por el plano.|Bram fait visiter son jardin. Daan ajoute place, entretien et habitants oubliés par la carte.|Брам проведёт Даана по саду. Тот добавит стоянку, уход и людей, которых карта не замечала.', 'park',1,8,40),
    chapter('Die Rückseite des Deichbriefs|The back of the dike deed|El reverso del acta del dique|Le verso de l’acte des digues|Оборотная сторона акта дамбы', 'Daan liest den Deichbrief mit Noor: Rechte verpflichten zu Pflege und freiem Durchgang. Er legt auch Widersprüche offen.|Daan reads the deed with Noor: rights require care and free passage. He publishes the contradictions too.|Daan lee el acta con Noor: los derechos exigen cuidado y paso libre. También publica contradicciones.|Daan lit l’acte avec Noor : les droits exigent entretien et passage libre. Il publie les contradictions aussi.|Даан прочтёт акт с Нур: права требуют ухода и свободного прохода. Противоречия тоже станут открытыми.', 'farm_warehouse',7,8,120),
    chapter('Eine Karte mit offenen Rändern|A map with open edges|Un mapa de bordes abiertos|Une carte aux bords ouverts|Карта с открытыми краями', 'Daan ergänzt überprüfbare Zugänge und gemeinsame Pflichten. Im Kanalarchiv bleibt Platz für die nächste Hausbootgeschichte und ihre Korrekturen.|Daan adds verifiable access and shared duties. The canal archive leaves room for the next houseboat story and its corrections.|Daan añade accesos verificables y deberes comunes. El archivo deja espacio para nuevas historias flotantes y correcciones.|Daan ajoute accès vérifiables et devoirs partagés. Les archives gardent une place pour les prochaines histoires et corrections.|Даан добавит проверяемый доступ и общие обязанности. Архив оставит место следующим историям и исправлениям.', 'supermarket',15,8,180)],
       'Daan hält Uferkarten öffentlich und ergänzt Erfahrungen aus den Hausbooten.|Daan keeps bank maps public and adds experiences from houseboats.|Daan publica mapas de orillas e incorpora experiencias de casas flotantes.|Daan garde les cartes publiques et ajoute les expériences des péniches.|Даан открывает береговые карты и добавляет опыт плавучих домов.')

person('fenna', 'Fenna Bakker', 900, 'bakery_01', [900,5200,11200],
       'Bäckerin des Morgenkais|Morning quay baker|Panadera del muelle matinal|Boulangère du quai du matin|Пекарь утреннего причала',
       'Fenna backt vor Sonnenaufgang für Werft und Fähre. Sie versprach jeder Schicht Brot, obwohl Warenfenster Zutaten verspäteten. Aus Pflichtgefühl verbarg sie ihre Müdigkeit. Jetzt will sie verlässliche Vorräte und einen Morgen, den sie selbst erlebt.|Fenna bakes before dawn for shipyard and ferry. She promised every shift bread despite delayed ingredients. Duty made her hide exhaustion. Now she wants reliable supplies and a morning she can enjoy herself.|Fenna hornea antes del alba para astillero y ferry. Prometía pan pese a ingredientes retrasados. Ocultaba su cansancio por deber. Ahora busca reservas fiables y una mañana propia.|Fenna cuit avant l’aube pour le chantier et le bac. Elle promettait du pain malgré les ingrédients retardés et cachait sa fatigue par devoir. Elle veut des provisions fiables et un matin à vivre elle-même.|Фенна печёт до рассвета для верфи и парома. Она обещала хлеб каждой смене, хотя продукты задерживались, и скрывала усталость. Теперь ей нужны надёжные запасы и собственное утро.',
       'Frisches Brot braucht auch einen ausgeruhten Menschen.|Fresh bread needs a rested person too.|El pan fresco también necesita a alguien descansado.|Le pain frais demande aussi quelqu’un de reposé.|Свежему хлебу нужен и отдохнувший человек.', [
    chapter('Das Mehl nach dem Morgen|Flour after the morning|La harina después de la mañana|La farine après le matin|Мука после утра', 'Esra bringt verspätete Zutaten. Fenna zeigt ihren wirklichen Backplan; beide benennen, was Warenzeiten verschweigen.|Esra brings late ingredients. Fenna shows her real baking plan; both name what cargo slots hide.|Esra trae ingredientes tarde. Fenna enseña el plan real; ambas nombran lo que ocultan los horarios.|Esra apporte les ingrédients en retard. Fenna montre le vrai planning ; elles nomment ce que cachent les horaires.|Эсра привезёт задержанные продукты. Фенна покажет настоящий план; обе назовут то, что скрывает график.', 'butter_factory',1,6,30),
    chapter('Das Rezept auf dem gemeinsamen Tisch|The recipe on the shared table|La receta en la mesa común|La recette sur la table commune|Рецепт на общем столе', 'Fenna teilt ihr Rezept. Bram notiert Zutaten, Joost übernimmt eine Übergabe; Backwissen und Vorräte erhalten mehrere Hüter.|Fenna shares her recipe. Bram lists ingredients, Joost takes a handover; knowledge and supplies gain several keepers.|Fenna comparte la receta. Bram anota ingredientes, Joost asume un relevo; saber y reservas tienen varios guardianes.|Fenna partage sa recette. Bram note les ingrédients, Joost assure un relais ; savoir et provisions trouvent plusieurs gardiens.|Фенна поделится рецептом. Брам запишет продукты, Йост возьмёт передачу; у знаний и запасов будет несколько хранителей.', 'bakery',7,7,110),
    chapter('Ein Morgen auf der anderen Seite|A morning on the other bank|Una mañana en la otra orilla|Un matin sur l’autre rive|Утро на другом берегу', 'Fenna nimmt mit geteilter Backschicht die Fähre als Fahrgast. Ihr freier Morgen steht im Ufervertrag; Versorgung läuft während ihrer Pause.|With shared baking shifts Fenna takes the ferry as a passenger. Her free morning is in the bank agreement; supplies continue during her break.|Con turnos compartidos Fenna viaja como pasajera. Su mañana libre figura en el pacto; el abastecimiento sigue durante su pausa.|Avec les services partagés Fenna prend le bac en passagère. Son matin libre figure dans l’accord ; les provisions continuent pendant sa pause.|Разделив выпечку по сменам, Фенна станет пассажиром парома. Её свободное утро войдёт в договор; снабжение продолжится во время отдыха.', 'bakery',15,7,180)],
       'Fenna plant Vorräte mit Fähre und Werft und hält die geteilten Backdienste verlässlich.|Fenna plans supplies with ferry and shipyard and keeps shared baking shifts reliable.|Fenna planifica reservas con ferry y astillero y sostiene turnos compartidos.|Fenna prévoit les provisions avec bac et chantier et veille aux services partagés.|Фенна планирует запасы с паромом и верфью и поддерживает общие смены выпечки.')

person('joost', 'Joost Meijer', 1500, 'farm_processing', [1500,5900,11900],
       'Werftmeister|Shipyard master|Maestro de astillero|Maître du chantier naval|Мастер верфи',
       'Joost hält die alte Werft mit Können und Erinnerungen zusammen. Sein Vater verlor ein Boot durch schlechte Wartung; seither vertraut Joost nur eigener Arbeit. Der Landstreit bedroht den Steg. Er muss Sicherheit weitergeben, damit die Werft ohne seine tägliche Kontrolle bleibt.|Joost holds the old yard together with skill and memories. His father lost a boat to poor care; since then he trusts only his own work. The land dispute threatens the dock. He must pass on safety so the yard survives without his daily control.|Joost sostiene el astillero con oficio y recuerdos. Su padre perdió un barco por mal mantenimiento; solo confía en su trabajo. El conflicto de tierras amenaza el muelle. Debe transmitir seguridad para que perdure sin su control diario.|Joost tient le chantier par son métier et ses souvenirs. Son père a perdu un bateau faute d’entretien ; il ne fait confiance qu’à lui-même. Le conflit foncier menace le ponton. Il doit transmettre son savoir-faire pour que le chantier reste sûr sans dépendre de sa surveillance quotidienne.|Йост держит верфь мастерством и памятью. Отец потерял лодку из-за плохого ухода; с тех пор он доверяет лишь себе. Земельный спор угрожает причалу. Нужно передать безопасность, чтобы верфь жила без его ежедневного контроля.',
       'Ein gutes Werkzeug soll mehr als eine Hand kennen.|A good tool should know more than one pair of hands.|Una buena herramienta está hecha para pasar de mano en mano.|Un bon outil doit connaître plusieurs mains.|Хороший инструмент должен знать больше одной пары рук.', [
    chapter('Der Schlüssel bleibt in meiner Tasche|The key stays in my pocket|La llave sigue en mi bolsillo|La clé reste dans ma poche|Ключ остаётся в моём кармане', 'Noor wartet an der verschlossenen Werkbank. Joost erzählt vom verlorenen Boot und lässt sie seine Prüfungen mitlesen.|Noor waits at the locked bench. Joost tells her of the lost boat and lets her read his checks.|Noor espera ante el banco cerrado. Joost cuenta lo del barco y deja que lea sus revisiones.|Noor attend devant l’établi fermé. Joost raconte le bateau perdu et partage ses contrôles.|Нур ждёт у закрытого верстака. Йост расскажет о потерянной лодке и откроет записи проверок.', 'farm_processing',1,4,35),
    chapter('Die zweite Hand am Werkzeug|The second hand on the tool|La segunda mano en la herramienta|La seconde main sur l’outil|Вторая рука на инструменте', 'Esra lernt sichere Reparaturen, Bram erklärt schwimmenden Anbau. Joost schreibt mit beiden einen Wartungsplan statt alles allein nachzubessern.|Esra learns safe repairs, Bram explains floating gardens. Joost writes a maintenance plan with them instead of redoing everything alone.|Esra aprende reparaciones, Bram cultivos flotantes. Joost crea un plan con ambos en vez de rehacerlo todo solo.|Esra apprend les réparations, Bram explique les jardins flottants. Joost rédige avec eux un plan plutôt que de tout reprendre seul.|Эсра освоит ремонт, Брам объяснит плавучие сады. Йост составит с ними план вместо исправления всего в одиночку.', 'farm_processing',7,4,120),
    chapter('Die Werft nach meiner Schicht|The wharf after my shift|El muelle después de mi turno|Le quai après mon service|Верфь после моей смены', 'Joost öffnet die Werkbank dauerhaft. Wähle Reparaturwerft oder Lehranlegestelle; beide teilen Wissen, Sicherheit und freien Uferzugang.|Joost opens his bench permanently. Choose a repair wharf or learning dock; both share knowledge, safety and open bank access.|Joost abre su banco permanentemente. Elige reparación o aprendizaje; ambos comparten saber, seguridad y acceso libre.|Joost ouvre définitivement son établi. Choisis réparation ou apprentissage ; les deux partagent savoir, sécurité et accès libre.|Йост навсегда откроет верстак. Выбери ремонтную верфь или учебный причал; оба сохранят знания, безопасность и доступ.', 'farm_processing',15,4,200)],
       'Joost teilt Wartungsarbeiten und Werkzeuge und nimmt sich Zeit zuzuhören.|Joost shares maintenance and tools and makes time to listen.|Joost comparte mantenimiento y herramientas y dedica tiempo a escuchar.|Joost partage entretien et outils et prend le temps d’écouter.|Йост разделяет уход и инструменты и находит время слушать.')

REPEATS = [
    (3,35,0,t('Vorräte für die Deichgärten|Supplies for Dike Gardens|Reservas para los jardines|Provisions pour les jardins|Запасы для садов у дамбы')),
    (9,3,1,t('Ein verlässliches Wasserfenster|A reliable water slot|Una franja de agua fiable|Un créneau d’eau fiable|Надёжное окно на воде')),
    (7,40,3,t('Brot für die gemeinsame Pflege|Bread for shared care|Pan para el cuidado común|Du pain pour l’entretien partagé|Хлеб для общей заботы')),
]

PROJECTS = [
    ('tide_court',2000,2,8,120,1,
     t('Ein Hof zwischen Ebbe und Flut|A court between ebb and flood|Un patio entre bajamar y pleamar|Une cour entre basse et haute mer|Двор между отливом и приливом'),
     t('Nach den ersten Kapiteln aller sechs öffnet der Gezeitenhof. Noor bringt Pegeltafel, Bram Wassergarten, Esra Fahrplan, Daan Deichakte, Fenna Tisch und Joost sichere Uferbank. Pflege und Zugang erhalten denselben Platz wie Waren.|After all six first chapters the tide court opens. Noor brings levels, Bram a water garden, Esra timetables, Daan the deed, Fenna a table and Joost a safe bench. Care and access get the same space as cargo.|Tras los primeros capítulos abre el patio. Noor aporta niveles, Bram jardín, Esra horarios, Daan acta, Fenna mesa y Joost banco seguro. Cuidado y acceso valen tanto como carga.|Après les premières histoires la cour ouvre. Noor apporte niveaux, Bram jardin, Esra horaires, Daan acte, Fenna table et Joost banc sûr. Entretien et accès comptent autant que le fret.|После первых глав откроется двор. Нур принесёт уровни, Брам — сад, Эсра — расписание, Даан — акт, Фенна — стол, Йост — скамью. Забота и доступ станут столь же важны, как грузы.')),
    ('seed_quay',6000,1,9,15,2,
     t('Ein Kai für Saatgut und Begegnungen|A quay for seeds and encounters|Un muelle de semillas y encuentros|Un quai pour graines et rencontres|Причал семян и встреч'),
     t('Nach den zweiten Kapiteln aller sechs öffnet der Saatgutkai. Brams Sortenbuch, Esras Fahrfenster und Fennas Vorräte treffen auf Noors Prüfplan, Daans Rechtekarte und Joosts offene Werkzeuge. Neue Hausbootgärten erhalten Raum mit verständlichen Pflegepflichten.|After all six second chapters Seed Quay opens. Bram’s seeds, Esra’s slots and Fenna’s supplies meet Noor’s checks, Daan’s rights map and Joost’s open tools. New houseboat gardens gain space with clear care duties.|Tras los segundos capítulos abre el muelle. Semillas de Bram, horarios de Esra y reservas de Fenna se unen a revisiones de Noor, derechos de Daan y herramientas de Joost. Hay espacio para jardines con deberes claros.|Après les deuxièmes histoires le quai ouvre. Graines de Bram, créneaux d’Esra et provisions de Fenna rejoignent contrôles de Noor, droits de Daan et outils de Joost. Les jardins trouvent une place aux devoirs clairs.|После вторых глав откроется причал. Семена Брама, окна Эсры и запасы Фенны соединятся с проверками Нур, правами Даана и инструментами Йоста. Новые сады получат место с ясными обязанностями.')),
    ('open_water_charter',12000,3,7,240,3,
     t('Der Wasserweg bleibt gemeinsam|The waterway stays shared|El cauce sigue siendo común|La voie d’eau reste partagée|Водный путь останется общим'),
     t('Nach den dritten Kapiteln aller sechs liegt im Kanalarchiv der Vertrag der offenen Wasserwege. Er schützt Hausbootplätze, sichere Schleusen, verlässliche Fähren, gemeinsame Pflege und persönliche Zeit. Joosts gewählte Werftform bleibt Teil des Vertrags; neue Nachbarn ergänzen seine Karte.|After all six third chapters the canal archive displays the open waterways charter. It protects houseboat berths, safe locks, reliable ferries, shared care and personal time. Joost’s chosen wharf stays in the agreement; new neighbours add to the map.|Tras los terceros capítulos el archivo expone el pacto. Protege atraques, esclusas, ferries, cuidado común y tiempo personal. La elección de Joost permanece; nuevos vecinos amplían el mapa.|Après les troisièmes histoires les archives présentent la charte. Elle protège places, écluses, bacs, entretien et temps personnel. Le choix de Joost demeure ; les nouveaux voisins complètent la carte.|После третьих глав архив представит договор. Он защитит дома, шлюзы, паромы, общую заботу и личное время. Выбор Йоста останется; новые соседи дополнят карту.')),
]

def extend_final_housing_tier(data):
    """Only the final, level-5000 bonus changes, so twenty million is reachable."""
    house = next(b for b in data['buildings'] if b['id'] == 'residential_house')
    bonuses = house['residentTierBonuses']
    if len(bonuses) != 25 or bonuses[:-1] != [0] * 24 or bonuses[24] not in (1000, 2000):
        raise ValueError('Independently edited residential final tier; inspect before authoring.')
    bonuses[24] = 2000


def author():
    data = json.loads(PATH.read_text(encoding='utf-8-sig'))
    catalogs = {lang: json.loads((LOCALES / (lang + '.json')).read_text(encoding='utf-8-sig')) for lang in LANGUAGES}
    before, before_catalogs = copy.deepcopy(data), copy.deepcopy(catalogs)
    if data['version'] < 13 or not set(SOURCES) <= {c['id'] for c in data['cities']}:
        raise ValueError('Great Sativadam requires the twelve-city catalog at version 13 or later.')
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
        title, body = t(title), t(body)
        q.update(id=PREFIX + qid, cityId=CITY, focus=q['focus'].replace('sh_', PREFIX, 1), titleKey=text('gs.quest.' + qid + '.title', title), bodyKey=text('gs.quest.' + qid + '.body', body))
        for condition in q['conditions']:
            if condition['target'].startswith('sh_'):
                condition['target'] = condition['target'].replace('sh_', PREFIX, 1)
            if condition['kind'] == 8 and residents:
                condition['count'] = residents
        text(q['bodyKey'] + '.dialogue', {lang: value.split(': ', 1)[-1] for lang, value in body.items()})
        text(q['bodyKey'] + '.dialogue_ready', ready)
        quests.append(q)

    def goal(kind, count, target=''):
        return dict(kind=kind, count=count, target=target, textKey='gs.goal.' + ('level' if kind == 0 else COUNTER_KEYS[kind]))

    stories = []
    for row in PEOPLE:
        pid, base = PREFIX + row['id'], 'gs.story.' + row['id']
        p = dict(id=pid, cityId=CITY, residents=row['residents'], focus=ids[row['focus']], repeatDelaySeconds=300, nameKey=text(base + '.name', tr(*([row['name']] * 5))), roleKey=text(base + '.role', row['role']), biographyKey=text(base + '.biography', row['bio']), greetingKey=text(base + '.greeting', row['greeting']), chapters=[], recurring=[])
        for index, c in enumerate(row['chapters']):
            key = base + '.chapter.' + str(index + 1)
            choices = [dict(id='help', textKey='gs.choice.help', outcomeKey='gs.outcome.help')]
            if row['id'] == 'joost' and index == 2:
                choices = [dict(id=choice, textKey='gs.choice.joost.' + choice, outcomeKey='gs.choice.joost.' + choice + '.outcome') for choice in ('repair_wharf', 'learning_dock')]
            family = c['family']
            zone = 3 if family in ('butter_factory', 'supermarket', 'bakery') else 2 if family == 'park' else 0 if family in ('cultivation_plot', 'farm_warehouse', 'farm_processing') else 1
            p['chapters'].append(dict(id=pid + '_chapter_' + str(index + 1), titleKey=text(key + '.title', c['title']), bodyKey=text(key + '.body', c['body']), memoryKey=text(key + '.memory', c['memory']), completedKey='gs.story.completed', residents=row['thresholds'][index], moneyReward=450 + index * 300, zone=zone, goals=[goal(0, c['level'], family), goal(c['counter'], c['count'])], choices=choices, prerequisites=[], itemRewards=[]))
        for index, (kind, count, zone, label) in enumerate(REPEATS):
            key = base + '.repeat.' + str(index + 1)
            title = {lang: row['name'].split()[0] + ': ' + label[lang] for lang in LANGUAGES}
            body = {lang: row['repeat'][lang] + ' ' + REPEAT_INSTRUCTIONS[kind][lang] for lang in LANGUAGES}
            p['recurring'].append(dict(id=pid + '_repeat_' + str(index + 1), titleKey=text(key + '.title', title), bodyKey=text(key + '.body', body), completedKey='gs.repeat.completed', memoryKey='', residents=row['residents'], moneyReward=300, zone=zone, goals=[goal(kind, count)], choices=[dict(id='help', textKey='gs.choice.help', outcomeKey='gs.outcome.help')], prerequisites=[], itemRewards=[]))
        stories.append(p)
    projects = []
    for index, (joint_id, residents, zone, kind, count, chapter_round, title, body) in enumerate(PROJECTS):
        pid, key = 'gs_joint_' + joint_id, 'gs.joint.' + joint_id
        participants = [PREFIX + row['id'] for row in PEOPLE]
        prerequisites = [p + '_chapter_' + str(chapter_round) for p in participants]
        instruction = t('Seit Annahme: {0}.|Since acceptance: {0}.|Desde la aceptación: {0}.|Depuis acceptation : {0}.|После принятия: {0}.')
        body = {lang: body[lang] + ' ' + instruction[lang].format(COUNTER_ACTION[kind][lang].format(count)) for lang in LANGUAGES}
        q = dict(id=pid, titleKey=text(key + '.title', title), bodyKey=text(key + '.body', body), memoryKey=text(key + '.memory', title), completedKey='gs.joint.completed', residents=residents, moneyReward=2500, zone=zone, goals=[goal(kind, count)], choices=[dict(id='together', textKey='gs.choice.help', outcomeKey='gs.outcome.help')], prerequisites=prerequisites, itemRewards=[])
        projects.append(dict(id=pid, cityId=CITY, host=participants[index], participants=participants, quest=q))
    city = dict(id=CITY, nameKey='city.great_sativadam', scenePath='Assets/Weedtopia/Game/Scenes/GreatSativadam.unity', sourceCity='', unlockSources=SOURCES, introducedVersion=VERSION, unlockResidents=20000000, bridgeMoney=17500, bridgeCards=9, invitationTitleKey='campaign.great_sativadam_title', invitationBodyKey='campaign.great_sativadam_body', invitationNoticeKey='campaign.great_sativadam_notice', invitationLockedKey='campaign.great_sativadam_locked', dialoguePrefix='gs.dialog.')
    expected_prior = copy.deepcopy(before)
    extend_final_housing_tier(data)
    extend_final_housing_tier(expected_prior)
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
        assert data[name][:len(before[name])] == before[name], 'Prior entries changed: ' + name
    for key in before.keys() - additive - {'version'}:
        assert expected_prior[key] == data[key], 'Prior field changed: ' + key
    for lang in LANGUAGES:
        assert catalogs[lang]['entries'][:len(before_catalogs[lang]['entries'])] == before_catalogs[lang]['entries'], 'Prior locale changed: ' + lang
    changed = data != before or catalogs != before_catalogs
    if data != before:
        PATH.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    for lang in LANGUAGES:
        if catalogs[lang] != before_catalogs[lang]:
            (LOCALES/(lang+'.json')).write_text(json.dumps(catalogs[lang],ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    receipt = dict(city=CITY,version=data['version'],changed=changed,instances=len(instances),routes=len(routes),mainQuests=len(quests),permanentResidents=len(stories),personalChapters=18,recurringVariants=18,communityProjects=len(projects),localizedKeys=len(added_texts),languages=list(LANGUAGES),oldContentDigest=digest(before),contentDigest=digest(data),priorContentPreservedExceptAuthorizedHousingTier=True,priorLocaleEntriesPreserved=True,allOtherBuildingDefinitionFieldsPreserved=True,placeholderParity=True,freshProgressGoals=True,prerequisiteGraphAcyclic=True,chapterRoundsBeforeProjects=True,savedPlaceChoice={'quest':'gs_joost_chapter_3','choices':['repair_wharf','learning_dock']},residents=[dict(id=p['id'],appearsAt=p['residents'],chapters=[c['residents'] for c in p['chapters']]) for p in stories],projects=[dict(id=p['id'],residents=p['quest']['residents'],prerequisites=p['quest']['prerequisites']) for p in projects])
    receipt['authorizedFinalHousingTier'] = dict(building='residential_house',index=24,level=5000,fromBonus=1000,toBonus=2000,previousLevelsPreserved=True,previousMaximumPerHouse=1125,maximumPerHouse=2125,maximumPreviousTwelveCities=22108500,required=20000000,allOtherEconomyCurvesPreserved=True)
    REPORT.parent.mkdir(parents=True,exist_ok=True)
    REPORT.write_text(json.dumps(receipt,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in receipt.items() if k not in ('residents','projects')},ensure_ascii=False))
    return receipt


def validate(data,catalogs,added_texts):
    city = next(c for c in data['cities'] if c['id']==CITY)
    assert city['unlockSources']==SOURCES and city['unlockResidents']==20000000
    assert (city['bridgeMoney'],city['bridgeCards'],city['introducedVersion'])==(17500,9,14)
    house = next(b for b in data['buildings'] if b['id']=='residential_house')
    assert house['residentTierBonuses']==[0]*24+[2000]
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
    assert all(len(p['chapters'])==3 and len(p['recurring'])==3 and p['focus'] in ids and p['repeatDelaySeconds']==300 for p in people)
    projects = [p for p in data['vipCollaborations'] if p['cityId']==CITY]
    assert [p['id'] for p in projects]==['gs_joint_'+p[0] for p in PROJECTS]
    all_quests = [q for p in people for q in p['chapters']+p['recurring']]+[p['quest'] for p in projects]
    lookup = {q['id']:q for q in all_quests}
    assert len(lookup)==39
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
        assert thresholds[0]==p['residents'] and thresholds==sorted(set(thresholds)) and thresholds[-1]<12000
        assert all(not c['prerequisites'] for c in p['chapters'])
        assert all(not q['memoryKey'] for q in p['recurring'])
        for index,q in enumerate(p['chapters']):
            expected=['repair_wharf','learning_dock'] if p['id']=='gs_joost' and index==2 else ['help']
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
    repeat_keys={'gs.story.'+p['id']+'.repeat.'+str(i)+'.body' for p in PEOPLE for i in (1,2,3)}
    assert repeat_keys<=required
    assert all(not any(c.isdigit() for c in value) for key in repeat_keys for value in added_texts[key].values()), 'Recurring prose embeds unscaled amounts'
    for lang in LANGUAGES:
        entries={e['key']:e['value'] for e in catalogs[lang]['entries']}
        assert len(entries)==len(catalogs[lang]['entries']) and required<=entries.keys()
        assert all(entries[k].strip() and fields(entries[k])==fields(texts[k]) for k in required)
    assert not any(other in value for translations in added_texts.values() for value in translations.values() for other in ('Great Kushville','Saint Mary Ville','Golden Flower','Purple Garden','Seven Hills','Skunkfurt'))


if __name__=='__main__':
    author()

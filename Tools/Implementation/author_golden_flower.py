"""Explicit additive Golden Flower content authoring; safe to run repeatedly."""
import copy
import hashlib
import json
import pathlib
import string

from author_green_leaf_village import (ACTION, COUNTER_ACTION, COUNTER_KEYS,
                                      FAMILY_NAMES, LANGUAGES, TEXT as SHARED_TEXT, tr)

ROOT = pathlib.Path(__file__).resolve().parents[2]
PATH = ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json'
LOCALES = ROOT / 'Assets/Weedtopia/Game/Localization'
REPORT = ROOT / 'Docs/GoldenFlower/content_receipt.json'
CITY, PREFIX, VERSION = 'golden_flower', 'gf_', 10
SOURCES = ['burning_hills', 'big_rock_city', 'seven_hills', 'little_montana',
           'cassa_vadela', 'mark_wood_city', 'green_leaf_village', 'purple_garden']

TEXT = {
    'city.golden_flower': tr(*(['Golden Flower'] * 5)),
    'campaign.golden_flower_title': tr('Rocko: Das Tor zum Sonnental', 'Rocko: The gate to Sun Valley', 'Rocko: La puerta del Valle del Sol', 'Rocko : La porte de la vallée du Soleil', 'Рокко: Ворота Солнечной долины'),
    'campaign.golden_flower_body': tr(
        'Eine Million Menschen leben in unseren acht Städten zusammen. Golden Flower entstand aus einer Gärtnergenossenschaft im Sonnental. Ein Sturm zerstörte das Gemeinschaftsglashaus und seine Wasseranlage; der Markt verlor Händler, das Goldblütenfest verstummte. Alma bewahrt die Saatgutkiste, Ida die Technik, Milan die Küche, Nora die Erinnerungen und Theo die Musik. Mit 12.500 Geld und 7 Teamkarten helfen wir ihnen, Garten, Glashaus, Markt und Fest gemeinsam zurückzugewinnen.',
        'One million people live across our eight cities. Golden Flower grew from a gardeners’ cooperative in Sun Valley. A storm damaged the shared glasshouse and water system; traders left the market and the Golden Bloom Festival fell silent. Alma kept the seed chest, Ida the machinery, Milan the kitchen, Nora the memories and Theo the music. With 12,500 money and 7 team cards, we help them restore the garden, glasshouse, market and festival together.',
        'Un millón de personas vive en nuestras ocho ciudades. Golden Flower nació de una cooperativa de jardineros en el Valle del Sol. Una tormenta dañó el invernadero común y el riego; el mercado perdió comerciantes y la fiesta de las flores doradas calló. Alma conservó las semillas, Ida la maquinaria, Milan la cocina, Nora los recuerdos y Theo la música. Con 12.500 de dinero y 7 cartas de equipo les ayudamos a recuperar juntos jardín, invernadero, mercado y fiesta.',
        'Un million de personnes vivent dans nos huit villes. Golden Flower est née d’une coopérative de jardiniers dans la vallée du Soleil. Une tempête a endommagé la serre commune et l’irrigation ; les commerçants sont partis et la fête des fleurs d’or s’est tue. Alma a gardé les graines, Ida les machines, Milan la cuisine, Nora les souvenirs et Theo la musique. Avec 12 500 d’argent et 7 cartes d’équipe, aidons-les à retrouver ensemble jardin, serre, marché et fête.',
        'В наших восьми городах живёт миллион человек. Golden Flower выросла из садоводческого кооператива в Солнечной долине. Буря повредила общую теплицу и полив; торговцы ушли с рынка, праздник золотых цветов затих. Альма сохранила семена, Ида — технику, Милан — кухню, Нора — воспоминания, Тео — музыку. С 12 500 денег и 7 карточками команды поможем им вместе вернуть сад, теплицу, рынок и праздник.'),
    'campaign.golden_flower_notice': tr('Rocko: Golden Flower öffnet das Tor zum Sonnental. Seine fünf Bewohner warten auf einen gemeinsamen Neuanfang.', 'Rocko: Golden Flower opens the gate to Sun Valley. Its five residents await a shared new beginning.', 'Rocko: Golden Flower abre la puerta del Valle del Sol. Sus cinco vecinos esperan un nuevo comienzo compartido.', 'Rocko : Golden Flower ouvre la vallée du Soleil. Ses cinq habitants attendent un nouveau départ commun.', 'Рокко: Golden Flower открывает ворота долины. Пять жителей ждут общего нового начала.'),
    'campaign.golden_flower_locked': tr('Ab {0} Einwohnern in allen bisherigen Städten zusammen. Aktuell: {1}.', 'Available at {0} residents across all existing cities. Current total: {1}.', 'Disponible con {0} habitantes entre todas las ciudades anteriores. Total actual: {1}.', 'Disponible à {0} habitants dans toutes les villes précédentes réunies. Total actuel : {1}.', 'Откроется при {0} жителях во всех предыдущих городах вместе. Сейчас: {1}.'),
    'district.golden_flower.farm': tr('Sonnenterrassen', 'Sun Terraces', 'Terrazas del Sol', 'Terrasses du Soleil', 'Солнечные террасы'),
    'district.golden_flower.city': tr('Goldblütenmarkt', 'Golden Bloom Market', 'Mercado de las Flores Doradas', 'Marché des fleurs d’or', 'Рынок золотых цветов'),
    'district.golden_flower.settlement': tr('Genossenschaftshöfe', 'Cooperative Courts', 'Patios de la Cooperativa', 'Cours de la coopérative', 'Дворы кооператива'),
    'district.golden_flower.industry': tr('Glashauswerkstätten', 'Glasshouse Workshops', 'Talleres del Invernadero', 'Ateliers de la serre', 'Тепличные мастерские'),
    'gf.dialog.mila': tr('Alma hat die letzte Saatgutkiste gerettet. Auf den Sonnenterrassen teilen wir Pflege und Wissen, damit der Garten wieder mehr als eine Hüterin hat.', 'Alma saved the last seed chest. On the Sun Terraces we share care and knowledge so the garden has more than one keeper again.', 'Alma salvó las últimas semillas. En las terrazas compartimos cuidados y saber para que el jardín vuelva a tener más de una guardiana.', 'Alma a sauvé les dernières graines. Sur les terrasses, partageons soins et savoir pour que le jardin retrouve plusieurs gardiens.', 'Альма спасла последний ящик семян. На террасах разделим заботу и знания, чтобы у сада снова было больше одного хранителя.'),
    'gf.dialog.jaro': tr('Ida repariert seit dem Sturm alles allein. Unsere Betriebe geben ihr eine verlässliche Grundlage für Wasseranlage und offene Reparaturwerkstatt.', 'Ida has repaired everything alone since the storm. Our businesses give her a reliable foundation for the water system and an open repair workshop.', 'Ida lo repara todo sola desde la tormenta. Nuestros negocios sostendrán el riego y un taller de reparación abierto.', 'Ida répare tout seule depuis la tempête. Nos commerces soutiendront l’irrigation et un atelier de réparation ouvert.', 'После бури Ида всё чинит одна. Наши предприятия станут опорой для полива и открытой ремонтной мастерской.'),
    'gf.dialog.nia': tr('Milan will verlässliche Lieferungen, keine leeren Festversprechen. Die vorhandenen Fahrwege verbinden Garten, Markt und Glashaus.', 'Milan wants reliable deliveries, not empty festival promises. Existing routes connect the garden, market and glasshouse.', 'Milan quiere entregas fiables. Las rutas existentes unen jardín, mercado e invernadero.', 'Milan veut des livraisons fiables. Les trajets existants relient jardin, marché et serre.', 'Милану нужны надёжные поставки. Существующие маршруты соединят сад, рынок и теплицу.'),
    'gf.dialog.rocko': tr('Nora schreibt diesmal auch über die Gebliebenen. Theo hört ihnen zu: Das neue Goldblütenfest soll ihre eigene Stadt feiern.', 'This time Nora writes about those who stayed. Theo listens: the new Golden Bloom Festival will celebrate their own city.', 'Nora cuenta también a quienes se quedaron. Theo escucha: la nueva fiesta celebrará su propia ciudad.', 'Nora raconte aussi ceux qui sont restés. Theo écoute : la nouvelle fête célébrera leur propre ville.', 'Теперь Нора пишет и об оставшихся. Тео слушает: новый праздник прославит их собственный город.'),
    'gf.choice.help': tr('Gemeinsam diesen Schritt gehen', 'Take this step together', 'Dar este paso juntos', 'Faire ce pas ensemble', 'Сделать этот шаг вместе'),
    'gf.outcome.help': tr('Unsere Arbeit bleibt Teil von Golden Flower. Wissen, Verantwortung und Freude werden geteilt.', 'Our work stays part of Golden Flower. Knowledge, responsibility and joy are shared.', 'Nuestro trabajo permanece en Golden Flower. Compartimos saber, responsabilidad y alegría.', 'Notre travail reste dans Golden Flower. Savoir, responsabilité et joie se partagent.', 'Наша работа останется частью Golden Flower. Знания, ответственность и радость будут общими.'),
    'gf.story.completed': tr('Diese Erinnerung bleibt. Ich gehöre weiter zu Golden Flower; meine Geschichte wächst mit unseren Nachbarn.', 'This memory stays. I remain part of Golden Flower; my story grows with our neighbours.', 'Este recuerdo permanece. Sigo en Golden Flower; mi historia crece con nuestros vecinos.', 'Ce souvenir demeure. Je reste à Golden Flower ; mon histoire grandit avec nos voisins.', 'Эта память останется. Я остаюсь в Golden Flower; моя история растёт вместе с соседями.'),
    'gf.repeat.completed': tr('Heute ist es geschafft. Nach einer Pause kümmern wir uns weiter um Garten, Markt und Nachbarschaft.', 'Done for today. After a break we keep caring for garden, market and neighbours.', 'Por hoy está hecho. Tras una pausa seguimos cuidando jardín, mercado y vecinos.', 'C’est fait pour aujourd’hui. Après une pause, nous continuerons à prendre soin du jardin, du marché et des voisins.', 'На сегодня готово. После перерыва продолжим заботиться о саде, рынке и соседях.'),
    'gf.joint.completed': tr('Unser gemeinsamer Ort bleibt sichtbar. Die fünf Bewohner und ihre Alltagsaufgaben bleiben Teil der Stadt.', 'Our shared place stays visible. The five residents and their everyday tasks remain part of the city.', 'Nuestro lugar común sigue visible. Los cinco vecinos y sus tareas diarias permanecen en la ciudad.', 'Notre lieu commun reste visible. Les cinq habitants et leurs missions quotidiennes restent dans la ville.', 'Общее место останется видимым. Пять жителей и их будничные дела остаются частью города.'),
}
TEXT.update({key.replace('gl.goal.', 'gf.goal.', 1): value for key, value in SHARED_TEXT.items() if key.startswith('gl.goal.')})


def chapter(title, scene, family, level, counter, count):
    return dict(title=title, body={lang: scene[lang] + ' ' + ACTION[lang].format(FAMILY_NAMES[family][lang], level, COUNTER_ACTION[counter][lang].format(count)) for lang in LANGUAGES}, memory=title, family=family, level=level, counter=counter, count=count)


PEOPLE = [
    dict(id='alma', name='Alma Sonnenberg', residents=50, focus='farm_warehouse', thresholds=[50, 1200, 5000],
        role=tr('Gärtnerin', 'Gardener', 'Jardinera', 'Jardinière', 'Садовница'),
        bio=tr('Alma rettete die Saatgutkiste der Gründer aus dem Sturm. Aus Angst vor Verlust hält sie jeden Schlüssel selbst. Ida zeigt ihr, wie Wissen und Technik zusammenleben; Nora bewahrt auch die Namen der anderen Gärtner.', 'Alma saved the founders’ seed chest from the storm. Fear of loss makes her keep every key. Ida shows her how knowledge and machinery can work together; Nora preserves the other gardeners’ names too.', 'Alma salvó las semillas de los fundadores. Por miedo guarda todas las llaves. Ida une saber y técnica; Nora conserva también los nombres de otros jardineros.', 'Alma a sauvé les graines des fondateurs. Par crainte, elle garde toutes les clés. Ida réunit savoir et technique ; Nora garde aussi les noms des autres jardiniers.', 'Альма спасла семена основателей. Страх потери заставляет её держать все ключи. Ида соединит знания и технику; Нора сохранит имена других садовников.'),
        greeting=tr('Vielleicht brauchen die alten Samen neue Hände.', 'Perhaps the old seeds need new hands.', 'Quizá las viejas semillas necesiten manos nuevas.', 'Les vieilles graines ont peut-être besoin de nouvelles mains.', 'Может, старым семенам нужны новые руки.'),
        chapters=[
            chapter(tr('Die letzte Saatgutkiste', 'The last seed chest', 'El último cofre de semillas', 'Le dernier coffre de graines', 'Последний ящик семян'), tr('Alma öffnet die Kiste mit Nora. Unter den goldenen Blütennamen stehen die Hände der Gründer; auch Ida darf den Inhalt prüfen.', 'Alma opens the chest with Nora. Beneath the golden flower names are the founders’ signatures; Ida may inspect it too.', 'Alma abre el cofre con Nora. Bajo los nombres de flores están las firmas de los fundadores; Ida también lo revisa.', 'Alma ouvre le coffre avec Nora. Sous les fleurs figurent les signatures des fondateurs ; Ida peut aussi regarder.', 'Альма откроет ящик с Норой. Под названиями цветов — подписи основателей; Ида тоже сможет посмотреть.'), 'farm_warehouse', 1, 3, 20),
            chapter(tr('Beete mit vielen Händen', 'Beds with many hands', 'Bancales con muchas manos', 'Des plates-bandes à plusieurs mains', 'Грядки для многих рук'), tr('Nach dem geöffneten Tor verteilt Alma Pflanzkästen. Ida setzt den Wasserverteiler instand; gemeinsam schreiben sie die Pflege an den Tisch.', 'After the gate opens, Alma shares planting boxes. Ida repairs the water distributor; together they write care instructions on the table.', 'Tras abrir la puerta, Alma reparte cajas de cultivo. Ida repara el riego; juntas anotan los cuidados en la mesa.', 'Après l’ouverture de la porte, Alma partage les bacs. Ida répare le distributeur d’eau ; elles notent ensemble les soins.', 'После открытия ворот Альма раздаст ящики для посадки. Ида починит распределитель воды; вместе запишут правила ухода.'), 'cultivation_plot', 8, 3, 100),
            chapter(tr('Die erste freie Gartenrunde', 'The first shared garden round', 'La primera ronda compartida', 'La première tournée partagée', 'Первый общий обход сада'), tr('Im reparierten Glashaus übernehmen Nachbarn Almas Gartenrunde. Sie gibt den Schlüssel weiter und kommt zu Milans Tisch ohne Arbeitsliste.', 'In the repaired glasshouse, neighbours take Alma’s garden round. She passes on the key and joins Milan’s table without a work list.', 'En el invernadero reparado, los vecinos hacen la ronda de Alma. Ella comparte la llave y se sienta con Milan sin tareas.', 'Dans la serre réparée, les voisins assurent la tournée d’Alma. Elle partage la clé et rejoint Milan sans liste de travail.', 'В восстановленной теплице соседи возьмут обход Альмы. Она передаст ключ и придёт к Милану без списка дел.'), 'park', 24, 8, 220),
        ],
        repeat=tr('Alma verteilt Gartenrunden und hält die Saatgutkästen zugänglich.', 'Alma shares garden rounds and keeps seed boxes accessible.', 'Alma comparte rondas y mantiene accesibles las semillas.', 'Alma partage les tournées et garde les graines accessibles.', 'Альма делит обходы сада и держит семена доступными.'),
        choices=[('garden_round', tr('Die Gartenrunde bewahren', 'Remember the garden round', 'Recordar la ronda del jardín', 'Garder la tournée du jardin', 'Сохранить обход сада')), ('shared_keys', tr('Die geteilten Schlüssel bewahren', 'Remember the shared keys', 'Recordar las llaves compartidas', 'Garder les clés partagées', 'Сохранить общие ключи'))]),
    dict(id='ida', name='Ida Kern', residents=120, focus='farm_processing', thresholds=[120, 1500, 6000],
        role=tr('Mechanikerin', 'Mechanic', 'Mecánica', 'Mécanicienne', 'Механик'),
        bio=tr('Ida hielt die Wasseranlage nach dem Sturm allein am Laufen. Sie misst ihren Wert an jeder reparierten Schraube und nimmt keine Hilfe an. Mit Alma lernt sie, Technik zu erklären; Milan erinnert sie daran, selbst einen Platz am Tisch zu haben.', 'Ida kept the water system alive alone after the storm. She measures her worth by every repaired screw and refuses help. With Alma she learns to explain the machinery; Milan reminds her she deserves a seat at the table too.', 'Ida mantuvo sola el riego tras la tormenta. Mide su valor por cada reparación. Alma le ayuda a enseñar la técnica; Milan le guarda un sitio en la mesa.', 'Ida a maintenu seule l’irrigation après la tempête. Elle mesure sa valeur à chaque réparation. Alma l’aide à expliquer les machines ; Milan lui garde une place.', 'После бури Ида одна поддерживала полив. Она мерит свою ценность ремонтом и не принимает помощь. С Альмой научится объяснять технику; Милан оставит ей место за столом.'),
        greeting=tr('Für alles, was heute noch nicht geht, machen wir einen Plan.', 'For everything that does not work yet, we make a plan.', 'Para todo lo que aún no funciona, hacemos un plan.', 'Pour tout ce qui ne fonctionne pas encore, faisons un plan.', 'Для всего, что пока не работает, составим план.'),
        chapters=[
            chapter(tr('Der klemmende Wasserverteiler', 'The jammed water distributor', 'El distribuidor atascado', 'Le distributeur d’eau bloqué', 'Заевший распределитель воды'), tr('Ida zeigt Alma das alte Ventil. Statt nachts allein zu schrauben, zeichnet sie die Reparatur so auf, dass andere sie verstehen.', 'Ida shows Alma the old valve. Instead of repairing alone at night, she records the repair so others can understand.', 'Ida enseña a Alma la válvula. En vez de reparar sola de noche, dibuja los pasos para todos.', 'Ida montre la vanne à Alma. Au lieu de réparer seule la nuit, elle dessine les étapes pour tous.', 'Ида покажет Альме старый клапан. Вместо одиночного ночного ремонта нарисует понятные всем шаги.'), 'farm_processing', 2, 4, 20),
            chapter(tr('Glas über gemeinsamen Beeten', 'Glass above shared beds', 'Cristal sobre bancales comunes', 'Du verre sur les plates-bandes communes', 'Стекло над общими грядками'), tr('Mit dem offenen Tor liefert Milan Vorräte. Ida und Alma verbinden die Glashausanlage; Nora notiert auch ihre misslungenen Versuche.', 'With the gate open, Milan brings supplies. Ida and Alma reconnect the glasshouse machinery; Nora records their failed attempts too.', 'Con la puerta abierta, Milan trae reservas. Ida y Alma conectan el invernadero; Nora anota también los intentos fallidos.', 'La porte ouverte, Milan apporte des réserves. Ida et Alma reconnectent la serre ; Nora note aussi les essais ratés.', 'После открытия ворот Милан принесёт припасы. Ида и Альма соединят технику теплицы; Нора запишет и неудачные попытки.'), 'farm_processing', 10, 4, 110),
            chapter(tr('Eine Werkstatt mit offener Tür', 'A workshop with an open door', 'Un taller de puerta abierta', 'Un atelier à la porte ouverte', 'Мастерская с открытой дверью'), tr('Ida öffnet ihren Werkstatthof. Theo lernt eine Reparatur, bevor er den Rhythmus in seine Musik nimmt. Ida kann endlich selbst Pause machen.', 'Ida opens her workshop yard. Theo learns a repair before using its rhythm in his music. Ida can finally take a break herself.', 'Ida abre el patio del taller. Theo aprende una reparación antes de usar su ritmo en la música. Ida puede descansar.', 'Ida ouvre sa cour. Theo apprend une réparation avant d’en tirer un rythme. Ida peut enfin prendre une pause.', 'Ида откроет двор мастерской. Тео научится ремонту, прежде чем взять его ритм в музыку. Ида наконец сможет отдохнуть.'), 'city_warehouse', 26, 9, 18),
        ],
        repeat=tr('Ida teilt Wartung und Reparaturwissen mit den Nachbarn.', 'Ida shares maintenance and repair knowledge with neighbours.', 'Ida comparte mantenimiento y saber de reparación.', 'Ida partage entretien et connaissances de réparation.', 'Ида делится обслуживанием и знаниями о ремонте.'),
        choices=[('repair_team', tr('Das Reparaturteam bewahren', 'Remember the repair team', 'Recordar el equipo de reparación', 'Garder l’équipe de réparation', 'Сохранить ремонтную команду')), ('teaching_bench', tr('Die Lernwerkbank bewahren', 'Remember the teaching bench', 'Recordar el banco de aprendizaje', 'Garder l’établi d’apprentissage', 'Сохранить учебный верстак'))]),
    dict(id='milan', name='Milan Sommer', residents=260, focus='bakery_01', thresholds=[260, 1900, 7000],
        role=tr('Marktkoch', 'Market cook', 'Cocinero del mercado', 'Cuisinier du marché', 'Повар рынка'),
        bio=tr('Milan hielt eine kleine Küche offen, als die Marktstände verschwanden. Er will verlässliche Versorgung, bei der kleine Händler mitentscheiden. Er gibt Theo sein erstes Publikum und bittet Nora, auch die Arbeit hinter jedem Rezept festzuhalten.', 'Milan kept a small kitchen open as market stalls disappeared. He wants reliable supplies with a voice for small traders. He gives Theo his first audience and asks Nora to record the work behind every recipe.', 'Milan mantuvo una cocina cuando cerraron los puestos. Quiere suministros fiables y voz para pequeños comerciantes. Da a Theo su primer público y pide a Nora contar el trabajo de cada receta.', 'Milan a gardé une cuisine quand les étals fermaient. Il veut un ravitaillement fiable et une voix pour les petits commerçants. Il offre à Theo son premier public et demande à Nora de raconter chaque recette.', 'Милан сохранил кухню, когда ларьки закрылись. Он хочет надёжных поставок и голоса для мелких торговцев. Даст Тео первых слушателей и попросит Нору записать труд за каждым рецептом.'),
        greeting=tr('Ein guter Markt beginnt mit einem freien Platz am Tisch.', 'A good market begins with an empty seat at the table.', 'Un buen mercado empieza con un sitio libre en la mesa.', 'Un bon marché commence par une place libre à table.', 'Хороший рынок начинается со свободного места за столом.'),
        chapters=[
            chapter(tr('Ein Topf für die Gebliebenen', 'A pot for those who stayed', 'Una olla para quienes se quedaron', 'Une marmite pour ceux qui sont restés', 'Кастрюля для оставшихся'), tr('Milan lädt alle fünf an seinen kleinen Tisch. Theo spielt leise; Alma bringt das alte Genossenschaftsschild statt einer Arbeitsliste.', 'Milan invites all five to his small table. Theo plays quietly; Alma brings the cooperative sign instead of a work list.', 'Milan invita a los cinco. Theo toca suave; Alma trae el letrero de la cooperativa en vez de tareas.', 'Milan invite les cinq à sa table. Theo joue doucement ; Alma apporte l’enseigne de la coopérative.', 'Милан пригласит всех пятерых к столу. Тео тихо сыграет; Альма принесёт вывеску кооператива вместо списка дел.'), 'bakery', 3, 7, 30),
            chapter(tr('Rezepte mit Namen', 'Recipes with names', 'Recetas con nombres', 'Des recettes avec des noms', 'Рецепты с именами'), tr('Nach dem geöffneten Tor sammelt Milan Rezepte mit Nora. Hinter jedem Gericht bleibt die Person sichtbar, die es geteilt hat.', 'After the gate opens, Milan collects recipes with Nora. Every dish keeps the name of the person who shared it.', 'Tras abrir la puerta, Milan reúne recetas con Nora. Cada plato conserva el nombre de quien lo compartió.', 'La porte ouverte, Milan recueille des recettes avec Nora. Chaque plat garde le nom de la personne qui l’a partagé.', 'После открытия ворот Милан соберёт рецепты с Норой. У каждого блюда останется имя поделившегося им человека.'), 'butter_factory', 12, 6, 120),
            chapter(tr('Ein Markt mit freien Plätzen', 'A market with open seats', 'Un mercado con sitios libres', 'Un marché avec des places libres', 'Рынок со свободными местами'), tr('Das Glashaus versorgt den Markt wieder. Milan verhandelt mit kleinen Händlern über gemeinsame Tische, statt ihre Stände mit Festaufträgen zu verdrängen.', 'The glasshouse supplies the market again. Milan works with small traders on shared tables so festival orders do not displace their stalls.', 'El invernadero abastece el mercado. Milan acuerda mesas comunes con pequeños vendedores sin desplazar sus puestos por la fiesta.', 'La serre ravitaille le marché. Milan organise des tables communes avec les petits vendeurs sans remplacer leurs étals par la fête.', 'Теплица снова снабжает рынок. Милан договорится с мелкими торговцами об общих столах, не вытесняя их праздничными заказами.'), 'bakery', 28, 7, 240),
        ],
        repeat=tr('Milan plant kleine Marktlieferungen und freie Plätze für die Nachbarn.', 'Milan plans small market deliveries and free seats for neighbours.', 'Milan prepara pequeñas entregas y sitios para los vecinos.', 'Milan prépare de petites livraisons et des places pour les voisins.', 'Милан планирует небольшие поставки и места для соседей.'),
        choices=[('recipe_board', tr('Die Rezepttafel bewahren', 'Remember the recipe board', 'Recordar la pizarra de recetas', 'Garder le tableau de recettes', 'Сохранить доску рецептов')), ('long_table', tr('Den langen Tisch bewahren', 'Remember the long table', 'Recordar la mesa larga', 'Garder la longue table', 'Сохранить длинный стол'))]),
    dict(id='nora', name='Nora Winter', residents=500, focus='city_warehouse', thresholds=[500, 2400, 8000],
        role=tr('Chronistin', 'Chronicler', 'Cronista', 'Chroniqueuse', 'Летописец'),
        bio=tr('Nora schrieb früher nur über Feste und Gründer. Nach dem Sturm merkt sie, dass die Gebliebenen in ihrem Stadtporträt fehlen. Sie hört Alma und Ida zu, bewahrt Milans Alltag und lässt Theo Widersprüche in das neue Lied aufnehmen.', 'Nora once wrote only about festivals and founders. After the storm she notices those who stayed are missing from her city portrait. She listens to Alma and Ida, records Milan’s daily work and lets Theo carry disagreements into the new song.', 'Nora escribía sobre fiestas y fundadores. Tras la tormenta descubre que omitió a quienes quedaron. Escucha a Alma e Ida, narra el día a día de Milan y deja a Theo cantar también desacuerdos.', 'Nora écrivait sur les fêtes et les fondateurs. Elle découvre qu’elle a oublié ceux qui sont restés. Elle écoute Alma et Ida, raconte Milan et laisse Theo chanter aussi les désaccords.', 'Нора писала лишь о праздниках и основателях. После бури заметила, что оставшихся нет в её книге. Выслушает Альму и Иду, запишет будни Милана и сохранит разногласия в песне Тео.'),
        greeting=tr('Diesmal soll jeder in unserer Geschichte vorkommen.', 'This time everyone belongs in our story.', 'Esta vez todos deben salir en nuestra historia.', 'Cette fois, chacun doit figurer dans notre histoire.', 'На этот раз в нашей истории найдётся место каждому.'),
        chapters=[
            chapter(tr('Die leere Seite nach dem Sturm', 'The empty page after the storm', 'La página vacía tras la tormenta', 'La page vide après la tempête', 'Пустая страница после бури'), tr('Nora liest ihr Stadtporträt vor. Ida fragt nach den nächtlichen Reparaturen. Nora lässt die leere Seite offen und hört erst zu.', 'Nora reads her city portrait aloud. Ida asks about the nightly repairs. Nora leaves the empty page open and listens first.', 'Nora lee su retrato. Ida pregunta por las reparaciones nocturnas. Nora deja la página abierta y escucha.', 'Nora lit son portrait. Ida demande où sont les réparations nocturnes. Nora laisse la page ouverte et écoute.', 'Нора прочитает портрет города. Ида спросит о ночном ремонте. Нора оставит страницу открытой и сначала послушает.'), 'city_warehouse', 4, 8, 50),
            chapter(tr('Ein Stadtalbum mit vielen Stimmen', 'A city album with many voices', 'Un álbum con muchas voces', 'Un album aux multiples voix', 'Городской альбом со многими голосами'), tr('Am offenen Tor sammelt Nora Erinnerungen. Almas Saatgutnamen, Idas Skizzen und Milans Rezepte erhalten gleich viel Platz.', 'At the open gate Nora gathers memories. Alma’s seed names, Ida’s drawings and Milan’s recipes receive equal space.', 'En la puerta abierta, Nora reúne recuerdos. Semillas de Alma, dibujos de Ida y recetas de Milan ocupan igual espacio.', 'À la porte ouverte, Nora recueille des souvenirs. Graines d’Alma, dessins d’Ida et recettes de Milan ont autant de place.', 'У открытых ворот Нора соберёт воспоминания. Семена Альмы, чертежи Иды и рецепты Милана получат равное место.'), 'city_warehouse', 14, 8, 140),
            chapter(tr('Die Ausstellung gehört den Nachbarn', 'The exhibition belongs to neighbours', 'La exposición pertenece a los vecinos', 'L’exposition appartient aux voisins', 'Выставка принадлежит соседям'), tr('Im wieder geöffneten Glashaus lesen Nachbarn ihre eigenen Einträge. Nora korrigiert ihr erstes Porträt öffentlich; auch verschiedene Erinnerungen dürfen nebeneinander bleiben.', 'In the reopened glasshouse neighbours read their own entries. Nora corrects her first portrait publicly; differing memories may remain side by side.', 'En el invernadero abierto, los vecinos leen sus textos. Nora corrige públicamente su retrato y conserva recuerdos diferentes juntos.', 'Dans la serre rouverte, les voisins lisent leurs textes. Nora corrige publiquement son portrait ; les souvenirs différents restent côte à côte.', 'В открытой теплице соседи прочитают свои записи. Нора публично исправит первый портрет; разные воспоминания останутся рядом.'), 'city_warehouse', 30, 8, 260),
        ],
        repeat=tr('Nora hört neue Erinnerungen an und ergänzt das Stadtalbum gemeinsam.', 'Nora listens to new memories and updates the city album together.', 'Nora escucha recuerdos nuevos y amplía el álbum con todos.', 'Nora écoute de nouveaux souvenirs et enrichit l’album avec tous.', 'Нора слушает новые воспоминания и вместе со всеми дополняет альбом.'),
        choices=[('living_album', tr('Das lebendige Stadtalbum bewahren', 'Remember the living city album', 'Recordar el álbum vivo', 'Garder l’album vivant', 'Сохранить живой альбом')), ('memory_wall', tr('Die Erinnerungswand bewahren', 'Remember the memory wall', 'Recordar el muro de recuerdos', 'Garder le mur des souvenirs', 'Сохранить стену памяти'))]),
    dict(id='theo', name='Theo Licht', residents=850, focus='coffee_01', thresholds=[850, 3000, 9000],
        role=tr('Musiker', 'Musician', 'Músico', 'Musicien', 'Музыкант'),
        bio=tr('Theo versucht, das alte Festlied fehlerlos nachzuspielen. Vor Publikum fehlt ihm Mut für seine eigene Melodie. Milan gibt ihm einen kleinen Anfang; Nora zeigt ihm andere Erinnerungen, Alma und Ida teilen Klänge aus Garten und Werkstatt.', 'Theo tries to play the old festival song perfectly. In public he lacks courage for his own melody. Milan gives him a small beginning; Nora shows other memories while Alma and Ida share sounds from garden and workshop.', 'Theo intenta tocar perfecto el viejo himno. Le falta valor para su melodía. Milan le da un pequeño comienzo; Nora comparte recuerdos y Alma e Ida sonidos del jardín y taller.', 'Theo veut jouer parfaitement l’ancien chant. Il n’ose pas sa propre mélodie. Milan lui offre un petit début ; Nora partage des souvenirs, Alma et Ida les sons du jardin et de l’atelier.', 'Тео пытается идеально сыграть старую песню. Ему не хватает смелости для своей мелодии. Милан даст первый шанс, Нора — воспоминания, Альма и Ида — звуки сада и мастерской.'),
        greeting=tr('Das Lied ist fast fertig. Jetzt muss ich es euch vorspielen.', 'The song is almost ready. Now I need to play it for you.', 'La canción casi está lista. Ahora debo tocarla para vosotros.', 'La chanson est presque prête. Il faut maintenant vous la jouer.', 'Песня почти готова. Теперь нужно сыграть её вам.'),
        chapters=[
            chapter(tr('Eine Melodie am kleinen Tisch', 'A melody at the small table', 'Una melodía en la mesa pequeña', 'Une mélodie à la petite table', 'Мелодия у маленького стола'), tr('Milan bittet Theo nicht um Perfektion. Zwischen zwei Schüsseln spielt er seine eigene Melodie erstmals für die fünf Nachbarn.', 'Milan does not ask Theo for perfection. Between two bowls he plays his own melody for the five neighbours for the first time.', 'Milan no pide perfección. Entre dos cuencos, Theo toca por primera vez su melodía a los cinco.', 'Milan ne demande pas la perfection. Entre deux bols, Theo joue sa mélodie aux cinq voisins pour la première fois.', 'Милан не ждёт совершенства. Между мисками Тео впервые сыграет свою мелодию пятерым соседям.'), 'coffee_shop', 5, 5, 60),
            chapter(tr('Die Probe mit offenen Ohren', 'The rehearsal with open ears', 'El ensayo con oídos abiertos', 'La répétition à l’écoute', 'Репетиция с открытым слухом'), tr('Nach dem geöffneten Tor hört Theo Idas Werkstattrhythmus und Almas Gartenlied. Nora liest eine unbequeme Erinnerung; auch sie bekommt einen Ton.', 'After the gate opens, Theo hears Ida’s workshop rhythm and Alma’s garden song. Nora reads an uncomfortable memory; it receives a note too.', 'Tras abrir la puerta, Theo oye el taller de Ida y el canto de Alma. Nora lee un recuerdo difícil; también tiene una nota.', 'La porte ouverte, Theo écoute l’atelier d’Ida et le chant d’Alma. Nora lit un souvenir difficile ; il reçoit aussi une note.', 'После открытия ворот Тео услышит ритм мастерской Иды и песню Альмы. Нора прочтёт трудное воспоминание; оно тоже получит ноту.'), 'park', 16, 5, 150),
            chapter(tr('Ein neues Lied für Golden Flower', 'A new song for Golden Flower', 'Una canción nueva para Golden Flower', 'Une nouvelle chanson pour Golden Flower', 'Новая песня для Golden Flower'), tr('Am restaurierten Glashaus singt Theo mit den Nachbarn. Er lässt Pausen für ihre Stimmen; das Goldblütenfest bekommt eine eigene Melodie statt einer Kopie.', 'At the restored glasshouse Theo sings with neighbours. He leaves pauses for their voices; the festival gains its own melody instead of a copy.', 'Junto al invernadero restaurado, Theo canta con todos. Deja pausas para sus voces; la fiesta tiene melodía propia.', 'Près de la serre restaurée, Theo chante avec tous. Il laisse des pauses pour leurs voix ; la fête trouve sa propre mélodie.', 'У восстановленной теплицы Тео споёт с соседями. Оставит паузы для их голосов; праздник получит собственную мелодию.'), 'park', 32, 9, 22),
        ],
        repeat=tr('Theo probt mit kleinen Gruppen und gibt neuen Stimmen Platz.', 'Theo rehearses with small groups and makes room for new voices.', 'Theo ensaya con grupos pequeños y deja sitio a voces nuevas.', 'Theo répète en petits groupes et accueille de nouvelles voix.', 'Тео репетирует с малыми группами и даёт место новым голосам.'),
        choices=[('shared_song', tr('Das gemeinsame Lied bewahren', 'Remember the shared song', 'Recordar la canción común', 'Garder le chant commun', 'Сохранить общую песню')), ('listening_circle', tr('Den Zuhörkreis bewahren', 'Remember the listening circle', 'Recordar el círculo de escucha', 'Garder le cercle d’écoute', 'Сохранить круг слушателей'))]),
]

REPEATS = [(3, 50, 0, tr('Eine neue Gartenrunde', 'A new garden round', 'Una ronda nueva del jardín', 'Une nouvelle tournée du jardin', 'Новый обход сада')),
           (8, 100, 1, tr('Vorräte für die Nachbarn', 'Supplies for neighbours', 'Reservas para los vecinos', 'Des réserves pour les voisins', 'Припасы для соседей')),
           (7, 65, 3, tr('Der nächste Markttag', 'The next market day', 'El próximo día de mercado', 'Le prochain jour de marché', 'Следующий рыночный день'))]

# Recurring targets scale when accepted. Their progress rows display the actual
# target; prose must never promise an unscaled base quantity.
REPEAT_INSTRUCTIONS = {
    3: tr('Produziere nach Annahme neue Rohware. Die für diesen Auftrag benötigte Menge steht im Fortschritt.',
          'Produce new raw goods after accepting. The amount required for this task is shown in the progress.',
          'Produce materias primas nuevas después de aceptar. La cantidad necesaria para este encargo aparece en el progreso.',
          'Produis de nouvelles matières premières après acceptation. La quantité requise pour cette mission est indiquée dans la progression.',
          'После принятия произведи новое сырьё. Нужное для задания количество указано в прогрессе.'),
    8: tr('Liefere nach Annahme neue Waren. Die für diesen Auftrag benötigte Menge steht im Fortschritt.',
          'Deliver new goods after accepting. The amount required for this task is shown in the progress.',
          'Entrega productos nuevos después de aceptar. La cantidad necesaria para este encargo aparece en el progreso.',
          'Livre de nouveaux produits après acceptation. La quantité requise pour cette mission est indiquée dans la progression.',
          'После принятия доставь новые товары. Нужное для задания количество указано в прогрессе.'),
    7: tr('Verkaufe nach Annahme neues Gebäck. Die für diesen Auftrag benötigte Menge steht im Fortschritt.',
          'Sell new pastries after accepting. The amount required for this task is shown in the progress.',
          'Vende bollería nueva después de aceptar. La cantidad necesaria para este encargo aparece en el progreso.',
          'Vends de nouvelles pâtisseries après acceptation. La quantité requise pour cette mission est indiquée dans la progression.',
          'После принятия продай новую выпечку. Нужное для задания количество указано в прогрессе.'),
}
RECURRING_BODY_KEYS = frozenset('gf.story.' + person['id'] + '.repeat.' + str(index) + '.body'
                               for person in PEOPLE for index in (1, 2, 3))

PROJECTS = [
    ('gate', 1000, 2, 8, 120, 1, tr('Das Tor zum Sonnental', 'The gate to Sun Valley', 'La puerta del Valle del Sol', 'La porte de la vallée du Soleil', 'Ворота Солнечной долины'), tr('Alle fünf erzählen, was nach dem Sturm blieb. Alma öffnet das Tor, Ida macht die Wasseranlage verlässlich, Milan stellt einen Tisch auf, Nora bewahrt ihre Namen und Theo spielt seine erste Melodie.', 'All five share what remained after the storm. Alma opens the gate, Ida makes the water system reliable, Milan sets a table, Nora keeps their names and Theo plays his first melody.', 'Los cinco cuentan qué quedó tras la tormenta. Alma abre la puerta, Ida asegura el agua, Milan pone una mesa, Nora guarda sus nombres y Theo toca su melodía.', 'Les cinq racontent ce qui reste après la tempête. Alma ouvre la porte, Ida assure l’eau, Milan dresse une table, Nora garde leurs noms et Theo joue sa mélodie.', 'Все пятеро расскажут, что осталось после бури. Альма откроет ворота, Ида обеспечит воду, Милан поставит стол, Нора запишет имена, Тео сыграет мелодию.')),
    ('greenhouse', 4000, 2, 9, 14, 2, tr('Unter Glas: Wasser für morgen', 'Under glass: water for tomorrow', 'Bajo cristal: agua para mañana', 'Sous le verre : de l’eau pour demain', 'Под стеклом: вода на завтра'), tr('Die persönlichen zweiten Kapitel führen zusammen. Alma und Ida erklären Pflege und Technik; Milan versorgt die Helfer, Nora stellt das Album aus und Theo probt mit offenen Ohren. Das Gemeinschaftsglashaus bleibt offen.', 'Their second personal chapters come together. Alma and Ida explain care and machinery; Milan supplies helpers, Nora displays the album and Theo rehearses with open ears. The shared glasshouse stays open.', 'Sus segundos capítulos se unen. Alma e Ida explican cuidados y técnica; Milan abastece, Nora muestra el álbum y Theo escucha en los ensayos. El invernadero común permanece abierto.', 'Leurs deuxièmes chapitres se rejoignent. Alma et Ida expliquent soins et machines ; Milan ravitaille, Nora expose l’album et Theo répète à l’écoute. La serre commune reste ouverte.', 'Вторые главы объединятся. Альма и Ида объяснят уход и технику; Милан накормит помощников, Нора выставит альбом, Тео послушает всех. Общая теплица останется открытой.')),
    ('market', 10000, 3, 7, 260, 3, tr('Ein Platz für alle', 'A place for everyone', 'Un sitio para todos', 'Une place pour tous', 'Место для всех'), tr('Nach den dritten persönlichen Kapiteln finden alle einen eigenen Platz. Milan eröffnet den Markt mit kleinen Händlern und gemeinsamen Tischen. Ida teilt Reparaturen, Alma Gartenrunden, Nora Erinnerungen und Theo das neue Lied.', 'After their third personal chapters everyone finds a place. Milan reopens the market with small traders and shared tables. Ida shares repairs, Alma garden rounds, Nora memories and Theo the new song.', 'Tras sus terceros capítulos todos encuentran sitio. Milan abre el mercado con pequeños comerciantes y mesas comunes. Ida comparte reparaciones, Alma rondas, Nora recuerdos y Theo la canción.', 'Après leurs troisièmes chapitres, chacun trouve sa place. Milan rouvre le marché avec de petits vendeurs et des tables communes. Ida partage les réparations, Alma les tournées, Nora les souvenirs et Theo le chant.', 'После третьих глав каждый найдёт своё место. Милан откроет рынок с мелкими торговцами и общими столами. Ида разделит ремонт, Альма обходы, Нора память, Тео новую песню.')),
    ('festival', 15000, 3, 9, 28, 3, tr('Golden Flower blüht', 'Golden Flower blooms', 'Golden Flower florece', 'Golden Flower fleurit', 'Golden Flower расцветает'), tr('Der wieder offene Markt trägt das neue Goldblütenfest. Alle fünf teilen Dienste und Entscheidungen. Das Lied, die Ausstellung, der Garten und die Werkstatt bleiben; nach dem Fest gehören Alltag und nächste Begegnungen weiter ihnen.', 'The reopened market supports the new Golden Bloom Festival. All five share duties and decisions. Song, exhibition, garden and workshop remain; everyday life and future meetings still belong to them after the festival.', 'El mercado abierto sostiene la fiesta de las flores doradas. Los cinco comparten tareas y decisiones. Canción, exposición, jardín y taller permanecen; después continúan el día a día y los encuentros.', 'Le marché rouvert porte la fête des fleurs d’or. Les cinq partagent tâches et décisions. Chant, exposition, jardin et atelier restent ; après la fête, le quotidien et les rencontres leur appartiennent toujours.', 'Открытый рынок поддержит новый праздник золотых цветов. Пятеро разделят дела и решения. Песня, выставка, сад и мастерская останутся; после праздника их ждут будни и новые встречи.')),
]

# Match the existing 18-step production tutorial without replacing its contracts.
MAIN_ROWS = [
    ('arrival', 'arrival', tr('Eine Einladung ins Sonnental', 'An invitation to Sun Valley', 'Una invitación al Valle del Sol', 'Une invitation dans la vallée du Soleil', 'Приглашение в Солнечную долину'), tr('Rocko: Sprich mit mir am Rathaus. Die fünf Bewohner von Golden Flower wollen ihre Genossenschaft gemeinsam neu beginnen.', 'Rocko: Talk to me at the town hall. Golden Flower’s five residents want a shared new beginning for their cooperative.', 'Rocko: Habla conmigo en el ayuntamiento. Los cinco vecinos quieren reiniciar juntos su cooperativa.', 'Rocko : Parle-moi à la mairie. Les cinq habitants veulent relancer ensemble leur coopérative.', 'Рокко: Поговори со мной у ратуши. Пять жителей хотят вместе возродить кооператив.')),
    ('first_garden', 'first_garden', tr('Das erste Beet am Glashaus', 'The first bed by the glasshouse', 'El primer bancal del invernadero', 'La première plate-bande près de la serre', 'Первая грядка у теплицы'), tr('Mila: Repariere das erste Feld. Alma bringt ihre geretteten Samen; Ida prüft den Wasseranschluss.', 'Mila: Repair the first plot. Alma brings her saved seeds; Ida checks its water connection.', 'Mila: Repara la primera parcela. Alma trae semillas e Ida revisa el agua.', 'Mila : Répare la première parcelle. Alma apporte les graines, Ida vérifie l’eau.', 'Мила: Восстанови первое поле. Альма принесёт семена, Ида проверит воду.')),
    ('first_harvest', 'first_harvest', tr('Die erste geteilte Ernte', 'The first shared harvest', 'La primera cosecha compartida', 'La première récolte partagée', 'Первый общий урожай'), tr('Mila: Ernte die erste Rohware. Alma teilt den Ertrag statt ihn einzuschließen; danach hilft ein neues Teammitglied.', 'Mila: Harvest the first raw goods. Alma shares the yield instead of locking it away; a new team member helps next.', 'Mila: Cosecha la primera materia prima. Alma comparte el resultado; después ayuda otro miembro.', 'Mila : Récolte les premières matières premières. Alma partage le résultat ; un équipier aidera ensuite.', 'Мила: Собери первое сырьё. Альма поделится урожаем; затем поможет новый работник.')),
    ('processing', 'processing', tr('Idas Werkbank wird gebraucht', 'Ida’s workbench is needed', 'Hace falta el banco de Ida', 'L’établi d’Ida reprend du service', 'Верстак Иды снова нужен'), tr('Jaro: Besetze das Feld, sprich mit mir und repariere die Verarbeitung. Ida erklärt den nächsten Arbeitsschritt.', 'Jaro: Staff the plot, talk to me and repair processing. Ida explains the next step.', 'Jaro: Asigna la parcela, habla conmigo y repara el procesamiento. Ida explica el paso.', 'Jaro : Affecte la parcelle, parle-moi et répare la transformation. Ida explique la suite.', 'Яро: Назначь работника, поговори со мной и восстанови переработку. Ида объяснит следующий шаг.')),
    ('farm_store', 'farm_store', tr('Vorräte am Gartentor', 'Supplies at the garden gate', 'Reservas en la puerta del jardín', 'Des réserves à la porte du jardin', 'Припасы у садовых ворот'), tr('Nia: Besetze die Verarbeitung, sprich mit mir und repariere das Farmlager. Alma und Nora beschriften die Vorräte gemeinsam.', 'Nia: Staff processing, talk to me and repair the farm warehouse. Alma and Nora label supplies together.', 'Nia: Asigna procesamiento, habla conmigo y repara el almacén. Alma y Nora etiquetan reservas.', 'Nia : Affecte la transformation, parle-moi et répare l’entrepôt. Alma et Nora étiquettent les réserves.', 'Ния: Назначь переработку, поговори со мной и восстанови склад. Альма и Нора подпишут припасы.')),
    ('farm_routes', 'farm_routes', tr('Verlässliche Wege ins Sonnental', 'Reliable routes into Sun Valley', 'Rutas fiables al Valle del Sol', 'Des trajets fiables dans la vallée', 'Надёжные пути в долину'), tr('Nia: Besetze Leitung und Fahrdienst des Farmlagers, schließe eine Lieferung ab und automatisiere die Farm. Danach öffnet der Goldblütenmarkt.', 'Nia: Staff the farm warehouse manager and driver, complete a delivery and automate the farm. The Golden Bloom Market opens next.', 'Nia: Asigna dirección y conductor, completa una entrega y automatiza la granja. Después abre el mercado.', 'Nia : Affecte responsable et chauffeur, termine une livraison et automatise la ferme. Le marché ouvre ensuite.', 'Ния: Назначь управляющего и водителя, заверши доставку и автоматизируй ферму. Затем откроется рынок.')),
    ('market_reopen', 'market_reopen', tr('Ein Stand bleibt offen', 'A stall stays open', 'Un puesto sigue abierto', 'Un étal reste ouvert', 'Ларёк остаётся открытым'), tr('Rocko: Repariere Stadtlager und ersten Coffeeshop. Milan zeigt den Platz, an dem er auch nach dem Sturm Nachbarn empfing.', 'Rocko: Repair the city warehouse and first coffee shop. Milan shows where he welcomed neighbours even after the storm.', 'Rocko: Repara almacén y primer café. Milan muestra dónde recibía vecinos tras la tormenta.', 'Rocko : Répare entrepôt et premier café. Milan montre où il accueillait les voisins après la tempête.', 'Рокко: Восстанови склад и первую кофейню. Милан покажет, где принимал соседей после бури.')),
    ('first_meeting', 'first_meeting', tr('Die erste Marktversorgung', 'The first market supplies', 'Los primeros suministros del mercado', 'Le premier ravitaillement du marché', 'Первые поставки рынку'), tr('Rocko: Besetze Stadtlager und Coffeeshop und verkaufe die ersten Cannabis-Blüten. Danach öffnen die Genossenschaftshöfe.', 'Rocko: Staff the city warehouse and coffee shop and sell the first cannabis buds. The Cooperative Courts open next.', 'Rocko: Asigna almacén y café y vende las primeras flores de cannabis. Después abren los patios.', 'Rocko : Affecte entrepôt et café, puis vends les premières fleurs de cannabis. Les cours ouvrent ensuite.', 'Рокко: Назначь склад и кофейню, продай первые соцветия конопли. Затем откроются дворы.')),
    ('homes', 'homes', tr('Nachbarn brauchen ein Zuhause', 'Neighbours need a home', 'Los vecinos necesitan un hogar', 'Les voisins ont besoin d’un foyer', 'Соседям нужен дом'), tr('Rocko: Errichte Wohnhaus und Park auf den vorhandenen Bauplätzen. Nora lässt Platz für die Geschichten der neuen Nachbarn.', 'Rocko: Build a house and park on existing plots. Nora makes space for new neighbours’ stories.', 'Rocko: Construye casa y parque. Nora deja sitio para las historias nuevas.', 'Rocko : Construis maison et parc. Nora accueille les nouvelles histoires.', 'Рокко: Построй дом и парк. Нора оставит место для историй новых соседей.')),
    ('everyday', 'everyday', tr('Alltag zwischen den Terrassen', 'Everyday life among the terraces', 'La vida entre las terrazas', 'Le quotidien entre les terrasses', 'Будни среди террас'), tr('Rocko: Besetze Haus und Park, hole einen Tagesertrag ab und erledige einen VIP-Auftrag samt Belohnung. Danach öffnen die Glashauswerkstätten. Persönliche Bewohnerkapitel warten zusätzlich.', 'Rocko: Staff house and park, collect daily income, complete a VIP task and claim its reward. The Glasshouse Workshops open next. Personal resident chapters also await.', 'Rocko: Asigna casa y parque, cobra ingreso y completa un encargo VIP con recompensa. Abren los talleres; las historias personales esperan también.', 'Rocko : Affecte maison et parc, collecte un revenu et termine une mission VIP avec récompense. Les ateliers ouvrent ; les histoires personnelles attendent aussi.', 'Рокко: Назначь дом и парк, собери доход и выполни VIP-задание с наградой. Откроются мастерские; личные главы ждут отдельно.')),
    ('workyard', 'workyard', tr('Wärme für Milans Küche', 'Warmth for Milan’s kitchen', 'Calor para la cocina de Milan', 'De la chaleur pour la cuisine de Milan', 'Тепло для кухни Милана'), tr('Rocko: Repariere Butterwerk, Supermarkt und erste Bäckerei. Milan und Ida bringen Küche und Werkstatt wieder zusammen.', 'Rocko: Repair the butter factory, supermarket and first bakery. Milan and Ida reconnect kitchen and workshop.', 'Rocko: Repara fábrica de mantequilla, supermercado y panadería. Milan e Ida unen cocina y taller.', 'Rocko : Répare beurrerie, supermarché et boulangerie. Milan et Ida réunissent cuisine et atelier.', 'Рокко: Восстанови маслозавод, магазин и пекарню. Милан и Ида соединят кухню и мастерскую.')),
    ('local_supply', 'local_supply', tr('Versorgung statt einzelner Festtage', 'Supplies beyond a single festival', 'Suministros más allá de la fiesta', 'Du ravitaillement au-delà de la fête', 'Поставки после праздника'), tr('Rocko: Automatisiere die drei Betriebe, produziere Butter und verkaufe Gebäck. Milan plant verlässliche Markttage, damit auch Helfer Zeit am Tisch haben.', 'Rocko: Automate the three businesses, produce butter and sell pastries. Milan plans reliable market days so helpers also have time at the table.', 'Rocko: Automatiza los tres negocios, produce mantequilla y vende bollería. Milan prepara días fiables para dar tiempo a los ayudantes.', 'Rocko : Automatise les trois commerces, produis du beurre et vends des pâtisseries. Milan organise des jours fiables pour laisser du temps aux aides.', 'Рокко: Автоматизируй три предприятия, произведи масло и продай выпечку. Милан устроит надёжные рыночные дни и отдых помощникам.')),
    ('morning_courtyard', 'sun_gate', tr('1.000 Nachbarn am Gartentor', '1,000 neighbours at the garden gate', '1.000 vecinos en la puerta', '1 000 voisins à la porte du jardin', '1 000 соседей у ворот'), tr('Rocko: Golden Flower selbst zählt 1.000 Einwohner. Nach den ersten Kapiteln aller fünf Bewohner öffnet ihr gemeinsames Projekt das Tor zum Sonnental.', 'Rocko: Golden Flower itself has 1,000 residents. After all five residents’ first chapters their shared project opens the gate to Sun Valley.', 'Rocko: Golden Flower tiene 1.000 habitantes propios. Tras los primeros capítulos de los cinco abre el proyecto de la puerta.', 'Rocko : Golden Flower compte 1 000 habitants. Après les premiers chapitres des cinq habitants, leur projet ouvre la porte.', 'Рокко: В самой Golden Flower 1 000 жителей. После первых глав пятерых общий проект откроет ворота.')),
    ('returning', 'water_for_tomorrow', tr('Ein Plan für Wasser und Glas', 'A plan for water and glass', 'Un plan para agua y cristal', 'Un plan pour l’eau et le verre', 'План для воды и стекла'), tr('Rocko: Golden Flower hat 3.000 Einwohner. Die zweiten persönlichen Kapitel bauen auf dem geöffneten Tor auf. Ida und Alma bereiten Wasser für morgen vor.', 'Rocko: Golden Flower has 3,000 residents. The second personal chapters build on the open gate. Ida and Alma prepare water for tomorrow.', 'Rocko: Aquí viven 3.000 personas. Los segundos capítulos siguen a la puerta abierta. Ida y Alma preparan agua para mañana.', 'Rocko : Ici vivent 3 000 habitants. Les deuxièmes chapitres suivent la porte ouverte. Ida et Alma préparent l’eau de demain.', 'Рокко: Здесь 3 000 жителей. Вторые главы продолжают открытие ворот. Ида и Альма готовят воду на завтра.')),
    ('upper_lower', 'under_glass', tr('Das Glashaus gehört allen', 'The glasshouse belongs to everyone', 'El invernadero es de todos', 'La serre appartient à tous', 'Теплица для всех'), tr('Rocko: Bei 4.000 Einwohnern und den zweiten Kapiteln aller fünf Bewohner wird das Gemeinschaftsglashaus geöffnet. Der Garten bekommt Pflege, Technik und gemeinsame Verantwortung.', 'Rocko: At 4,000 residents and after all five second chapters, the shared glasshouse opens. The garden gains care, machinery and shared responsibility.', 'Rocko: Con 4.000 habitantes y los cinco segundos capítulos abre el invernadero. El jardín recibe cuidados, técnica y responsabilidad común.', 'Rocko : À 4 000 habitants et après les cinq deuxièmes chapitres, la serre ouvre. Le jardin reçoit soins, technique et responsabilité commune.', 'Рокко: При 4 000 жителях и после пяти вторых глав откроется теплица. У сада будут уход, техника и общая забота.')),
    ('public_terrace', 'a_place_for_all', tr('Der Markt und seine Stimmen', 'The market and its voices', 'El mercado y sus voces', 'Le marché et ses voix', 'Рынок и его голоса'), tr('Rocko: Golden Flower hat 10.000 Einwohner. Nach den dritten persönlichen Kapiteln eröffnet Milan mit allen den Markt. Kleine Händler, Erinnerungen und Musik bleiben am selben Platz.', 'Rocko: Golden Flower has 10,000 residents. After the third personal chapters Milan reopens the market with everyone. Small traders, memories and music share the same place.', 'Rocko: Aquí viven 10.000 personas. Tras los terceros capítulos Milan abre el mercado con todos. Vendedores, recuerdos y música comparten sitio.', 'Rocko : Ici vivent 10 000 habitants. Après les troisièmes chapitres, Milan rouvre le marché avec tous. Vendeurs, souvenirs et musique partagent la place.', 'Рокко: Здесь 10 000 жителей. После третьих глав Милан со всеми откроет рынок. Торговцы, память и музыка будут рядом.')),
    ('panorama', 'golden_bloom', tr('Das neue Goldblütenfest', 'The new Golden Bloom Festival', 'La nueva fiesta de flores doradas', 'La nouvelle fête des fleurs d’or', 'Новый праздник золотых цветов'), tr('Rocko: 15.000 Einwohner tragen Golden Flower. Nach dem Marktprojekt feiern alle fünf gemeinsam. Das Fest öffnet einen bleibenden Ort; ihre Geschichten und Alltagsaufgaben bleiben erhalten.', 'Rocko: 15,000 residents sustain Golden Flower. After the market project all five celebrate together. The festival opens a lasting place; their stories and everyday tasks remain.', 'Rocko: 15.000 habitantes sostienen Golden Flower. Tras el mercado celebran los cinco. La fiesta deja un lugar permanente; historias y tareas continúan.', 'Rocko : 15 000 habitants portent Golden Flower. Après le marché, les cinq célèbrent ensemble. La fête crée un lieu durable ; histoires et missions restent.', 'Рокко: В Golden Flower 15 000 жителей. После рынка пятеро отпразднуют вместе. Праздник оставит постоянное место; истории и дела продолжатся.')),
    ('home', 'home', tr('Golden Flower lebt weiter', 'Golden Flower lives on', 'Golden Flower sigue viviendo', 'Golden Flower continue de vivre', 'Golden Flower продолжает жить'), tr('Rocko: Hole einen Tagesertrag des Supermarkts ab. Golden Flower ist frei ausbaubar. Alma, Ida, Milan, Nora und Theo bleiben, mit gespeicherten Erinnerungen und wiederkehrenden Aufgaben.', 'Rocko: Collect the supermarket’s daily income. Golden Flower can develop freely. Alma, Ida, Milan, Nora and Theo remain with saved memories and recurring tasks.', 'Rocko: Cobra el ingreso del supermercado. Golden Flower crece libremente. Los cinco permanecen con recuerdos guardados y tareas recurrentes.', 'Rocko : Collecte le revenu du supermarché. Golden Flower grandit librement. Les cinq restent avec leurs souvenirs enregistrés et missions récurrentes.', 'Рокко: Собери доход магазина. Golden Flower может свободно расти. Пятеро останутся с сохранённой памятью и повторяющимися заданиями.')),
]
MAIN_POPULATIONS = {'sun_gate': 1000, 'water_for_tomorrow': 3000, 'under_glass': 4000,
                    'a_place_for_all': 10000, 'golden_bloom': 15000, 'home': 15000}


def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, ensure_ascii=False, separators=(',', ':')).encode()).hexdigest()


def fields(value):
    return sorted(f for _, f, _, _ in string.Formatter().parse(value) if f is not None)


def append_unique(target, entries, key):
    existing = {e[key]: e for e in target}
    if len(existing) != len(target):
        raise ValueError('Duplicate existing ' + key)
    for entry in entries:
        identity = entry[key]
        if identity in existing:
            if existing[identity] != entry:
                if key == 'key' and identity in RECURRING_BODY_KEYS and existing[identity].keys() == entry.keys():
                    # Explicit repair is restricted to Golden Flower's fifteen
                    # recurring task descriptions, never shared or older texts.
                    existing[identity].clear()
                    existing[identity].update(entry)
                else:
                    raise ValueError('Existing authored content differs: ' + identity)
        else:
            target.append(entry)
            existing[identity] = entry


def author():
    data = json.loads(PATH.read_text(encoding='utf-8-sig'))
    catalogs = {lang: json.loads((LOCALES / (lang + '.json')).read_text(encoding='utf-8-sig')) for lang in LANGUAGES}
    before, before_catalogs = copy.deepcopy(data), copy.deepcopy(catalogs)
    if data['version'] < 9 or not set(SOURCES) <= {c['id'] for c in data['cities']}:
        raise ValueError('Golden Flower requires the eight-city catalog at version 9 or later.')
    added_texts = dict(TEXT)

    def text(key, translations):
        if set(translations) != set(LANGUAGES) or any(not v.strip() for v in translations.values()):
            raise ValueError('Missing translation: ' + key)
        if key in added_texts and added_texts[key] != translations:
            raise ValueError('Conflicting translation: ' + key)
        if any(fields(v) != fields(translations['de']) for v in translations.values()):
            raise ValueError('Placeholder mismatch: ' + key)
        added_texts[key] = translations
        return key

    original = [i for i in data['instances'] if i.get('cityId', 'burning_hills') == 'burning_hills']
    ids = {i['id']: PREFIX + i['id'] for i in original}
    instances = [dict(copy.deepcopy(i), id=ids[i['id']], cityId=CITY, introducedVersion=VERSION) for i in original]
    routes = [dict(copy.deepcopy(r), id=PREFIX + r['id'], **{'from': ids[r['from']], 'to': ids[r['to']]}) for r in data['routes'] if r['from'] in ids and r['to'] in ids]
    foundation = {q['id']: q for q in data['quests'] if q.get('cityId') == 'seven_hills'}
    quests = []
    ready = tr('Alles bereit. Hole die Belohnung ab; dann gehen wir gemeinsam weiter.', 'Everything is ready. Claim the reward; then we continue together.', 'Todo listo. Recoge la recompensa y seguiremos juntos.', 'Tout est prêt. Récupère la récompense, puis nous continuerons ensemble.', 'Всё готово. Забери награду, и продолжим вместе.')
    for source, qid, title, body in MAIN_ROWS:
        q = copy.deepcopy(foundation['sh_' + source])
        q.update(id=PREFIX + qid, cityId=CITY, focus=q['focus'].replace('sh_', PREFIX, 1), titleKey=text('gf.quest.' + qid + '.title', title), bodyKey=text('gf.quest.' + qid + '.body', body))
        for condition in q['conditions']:
            if condition['target'].startswith('sh_'):
                condition['target'] = condition['target'].replace('sh_', PREFIX, 1)
            if condition['kind'] == 8 and qid in MAIN_POPULATIONS:
                condition['count'] = MAIN_POPULATIONS[qid]
        text(q['bodyKey'] + '.dialogue', {lang: value.split(': ', 1)[-1] for lang, value in body.items()})
        text(q['bodyKey'] + '.dialogue_ready', ready)
        quests.append(q)

    def goal(kind, count, target=''):
        return dict(kind=kind, count=count, target=target, textKey='gf.goal.' + ('level' if kind == 0 else COUNTER_KEYS[kind]))

    stories = []
    for row in PEOPLE:
        pid, base = PREFIX + row['id'], 'gf.story.' + row['id']
        person = dict(id=pid, cityId=CITY, residents=row['residents'], focus=ids[row['focus']], repeatDelaySeconds=300, nameKey=text(base + '.name', tr(*([row['name']] * 5))), roleKey=text(base + '.role', row['role']), biographyKey=text(base + '.biography', row['bio']), greetingKey=text(base + '.greeting', row['greeting']), chapters=[], recurring=[])
        for index, c in enumerate(row['chapters']):
            key = base + '.chapter.' + str(index + 1)
            choices = [dict(id='help', textKey='gf.choice.help', outcomeKey='gf.outcome.help')]
            if index == 2:
                choices = []
                for choice_id, label in row['choices']:
                    choice_key = text('gf.choice.' + row['id'] + '.' + choice_id, label)
                    outcome = {lang: label[lang] + '. ' + TEXT['gf.outcome.help'][lang] for lang in LANGUAGES}
                    choices.append(dict(id=choice_id, textKey=choice_key, outcomeKey=text(choice_key + '.outcome', outcome)))
            family = c['family']
            zone = 3 if family in ('butter_factory', 'supermarket', 'bakery') else 2 if family in ('park', 'residential_house') else 0 if family in ('cultivation_plot', 'farm_warehouse', 'farm_processing') else 1
            # The shared story service advances each resident's chapters in order.
            # Prerequisites remain personal-chapter references, as required by its validator.
            prerequisites = []
            person['chapters'].append(dict(id=pid + '_chapter_' + str(index + 1), titleKey=text(key + '.title', c['title']), bodyKey=text(key + '.body', c['body']), memoryKey=text(key + '.memory', c['memory']), completedKey='gf.story.completed', residents=row['thresholds'][index], moneyReward=300 + index * 225, zone=zone, goals=[goal(0, c['level'], family), goal(c['counter'], c['count'])], choices=choices, prerequisites=prerequisites, itemRewards=[]))
        for index, (kind, count, zone, label) in enumerate(REPEATS):
            key = base + '.repeat.' + str(index + 1)
            title = {lang: row['name'].split()[0] + ': ' + label[lang] for lang in LANGUAGES}
            body = {lang: row['repeat'][lang] + ' ' + REPEAT_INSTRUCTIONS[kind][lang] for lang in LANGUAGES}
            person['recurring'].append(dict(id=pid + '_repeat_' + str(index + 1), titleKey=text(key + '.title', title), bodyKey=text(key + '.body', body), completedKey='gf.repeat.completed', memoryKey='', residents=row['residents'], moneyReward=225, zone=zone, goals=[goal(kind, count)], choices=[dict(id='help', textKey='gf.choice.help', outcomeKey='gf.outcome.help')], prerequisites=[], itemRewards=[]))
        stories.append(person)
    projects = []
    for index, (joint_id, residents, zone, kind, count, chapter_round, title, body) in enumerate(PROJECTS):
        pid, key = 'gf_joint_' + joint_id, 'gf.joint.' + joint_id
        participants = [PREFIX + row['id'] for row in PEOPLE]
        prerequisites = [p + '_chapter_' + str(chapter_round) for p in participants]
        instruction = tr('Seit Annahme: {0}.', 'Since acceptance: {0}.', 'Desde la aceptación: {0}.', 'Depuis acceptation : {0}.', 'После принятия: {0}.')
        body = {lang: body[lang] + ' ' + instruction[lang].format(COUNTER_ACTION[kind][lang].format(count)) for lang in LANGUAGES}
        q = dict(id=pid, titleKey=text(key + '.title', title), bodyKey=text(key + '.body', body), memoryKey=text(key + '.memory', title), completedKey='gf.joint.completed', residents=residents, moneyReward=1800, zone=zone, goals=[goal(kind, count)], choices=[dict(id='together', textKey='gf.choice.help', outcomeKey='gf.outcome.help')], prerequisites=prerequisites, itemRewards=[])
        projects.append(dict(id=pid, cityId=CITY, host=participants[index], participants=participants, quest=q))
    city = dict(id=CITY, nameKey='city.golden_flower', scenePath='Assets/Weedtopia/Game/Scenes/GoldenFlower.unity', sourceCity='', unlockSources=SOURCES, introducedVersion=VERSION, unlockResidents=1000000, bridgeMoney=12500, bridgeCards=7, invitationTitleKey='campaign.golden_flower_title', invitationBodyKey='campaign.golden_flower_body', invitationNoticeKey='campaign.golden_flower_notice', invitationLockedKey='campaign.golden_flower_locked', dialoguePrefix='gf.dialog.')
    collections = [('cities', [city]), ('instances', instances), ('routes', routes), ('quests', quests), ('vipStories', stories), ('vipCollaborations', projects)]
    for collection, entries in collections:
        append_unique(data[collection], entries, 'id')
    append_unique(data['texts'], [dict(key=k, de=v['de']) for k, v in added_texts.items()], 'key')
    for lang in LANGUAGES:
        append_unique(catalogs[lang]['entries'], [dict(key=k, value=v[lang]) for k, v in added_texts.items()], 'key')
    # With the mandatory park, the first eight cities otherwise cap at 867,000 residents. The final housing
    # tier alone supplies the capacity needed to reach the new one-million gate.
    # Earlier housing tiers and every other building field stay identical.
    # ChoosePlot reserves one settlement site for a park: 17 houses per source city
    # must reach the million without bypassing the actual player building rules.
    next(b for b in data['buildings'] if b['id'] == 'residential_house')['residentTierBonuses'] = [0] * 24 + [20]
    data['version'] = max(data['version'], VERSION)
    validate(data, catalogs, added_texts)
    additive_collections = {name for name, _ in collections} | {'texts'}
    for name in additive_collections:
        if name == 'texts':
            assert len(data[name]) >= len(before[name])
            for prior, current in zip(before[name], data[name]):
                if prior['key'] in RECURRING_BODY_KEYS:
                    assert prior.keys() == current.keys() and prior['key'] == current['key']
                else:
                    assert prior == current, 'Prior text changed: ' + prior['key']
        else:
            assert data[name][:len(before[name])] == before[name], 'Prior entries changed: ' + name
    for key in before.keys() - additive_collections - {'version', 'buildings'}:
        assert before[key] == data[key], 'Prior field changed: ' + key
    for prior, current in zip(before['buildings'], data['buildings']):
        expected = copy.deepcopy(prior)
        if expected['id'] == 'residential_house':
            expected['residentTierBonuses'] = [0] * 24 + [20]
        assert expected == current, 'Unexpected building change: ' + prior['id']
    for lang in LANGUAGES:
        assert len(catalogs[lang]['entries']) >= len(before_catalogs[lang]['entries'])
        for prior, current in zip(before_catalogs[lang]['entries'], catalogs[lang]['entries']):
            if prior['key'] in RECURRING_BODY_KEYS:
                assert prior.keys() == current.keys() and prior['key'] == current['key']
            else:
                assert prior == current, lang + ': prior locale text changed: ' + prior['key']
    changed = data != before or catalogs != before_catalogs
    if data != before:
        PATH.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    for lang in LANGUAGES:
        if catalogs[lang] != before_catalogs[lang]:
            (LOCALES / (lang + '.json')).write_text(json.dumps(catalogs[lang], ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    receipt = dict(city=CITY, version=data['version'], changed=changed, instances=len(instances), routes=len(routes), mainQuests=len(quests), permanentResidents=len(stories), personalChapters=sum(len(p['chapters']) for p in stories), recurringVariants=sum(len(p['recurring']) for p in stories), communityProjects=len(projects), localizedKeys=len(added_texts), languages=list(LANGUAGES), oldContentDigest=digest(before), contentDigest=digest(data), priorContentPreservedExceptFinalHousingTierBonus=True, authorizedBuildingDelta={'id': 'residential_house', 'residentTierBonuses': [0] * 24 + [20]}, priorLocaleEntriesPreserved=True, placeholderParity=True, freshProgressGoals=True, prerequisiteGraphAcyclic=True, chapterRoundsBeforeProjects=True, residents=[dict(id=p['id'], appearsAt=p['residents'], chapters=[c['residents'] for c in p['chapters']]) for p in stories], projects=[dict(id=p['id'], residents=p['quest']['residents'], prerequisites=p['quest']['prerequisites']) for p in projects])
    receipt['recurringDescriptionTargetsUseProgress'] = True
    receipt['authoredRecurringDescriptionUpdates'] = [current['key'] for prior, current in zip(before['texts'], data['texts'])
                                                    if prior != current and current['key'] in RECURRING_BODY_KEYS]
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(receipt, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(receipt, ensure_ascii=False, indent=2))
    return receipt


def validate(data, catalogs, added_texts):
    city = next(c for c in data['cities'] if c['id'] == CITY)
    assert city['unlockSources'] == SOURCES and city['unlockResidents'] == 1000000
    assert city['bridgeMoney'] == 12500 and city['bridgeCards'] == 7 and city['introducedVersion'] == VERSION
    instances = [i for i in data['instances'] if i.get('cityId') == CITY]
    original = [i for i in data['instances'] if i.get('cityId', 'burning_hills') == 'burning_hills']
    ids = {i['id'] for i in instances}
    assert len(instances) == 56 and [i['family'] for i in instances] == [i['family'] for i in original]
    assert all(i['introducedVersion'] == VERSION for i in instances)
    routes = [r for r in data['routes'] if r['from'] in ids]
    assert len(routes) == 35 and all(r['to'] in ids and r['from'] != r['to'] for r in routes)
    quests = [q for q in data['quests'] if q.get('cityId') == CITY]
    assert len(quests) == 18 and all(q['focus'] in ids for q in quests)
    families = {b['id'] for b in data['buildings']}
    assert all(not c['target'] or c['target'] in ids | families | {'mila', 'jaro', 'nia', 'rocko'} for q in quests for c in q['conditions'])
    people = [p for p in data['vipStories'] if p['cityId'] == CITY]
    assert [p['id'] for p in people] == [PREFIX + p['id'] for p in PEOPLE]
    assert all(len(p['chapters']) == 3 and len(p['recurring']) == 3 and p['focus'] in ids and p['repeatDelaySeconds'] == 300 for p in people)
    projects = [p for p in data['vipCollaborations'] if p['cityId'] == CITY]
    assert [p['id'] for p in projects] == ['gf_joint_' + p[0] for p in PROJECTS]
    all_quests = [q for p in people for q in p['chapters'] + p['recurring']] + [p['quest'] for p in projects]
    lookup = {q['id']: q for q in all_quests}
    visited, active = set(), set()

    def visit(qid):
        assert qid not in active, 'Cyclic prerequisite: ' + qid
        if qid in visited:
            return
        active.add(qid)
        for prior in lookup[qid]['prerequisites']:
            assert prior in lookup, 'Unknown prerequisite: ' + prior
            visit(prior)
        active.remove(qid)
        visited.add(qid)

    for qid in lookup:
        visit(qid)
    for p in people:
        assert p['chapters'][0]['residents'] == p['residents']
        assert p['chapters'][0]['residents'] < p['chapters'][1]['residents'] < p['chapters'][2]['residents'] < 10000
        assert all(not c['prerequisites'] for c in p['chapters'])
        assert len(p['chapters'][2]['choices']) == 2 and len({c['outcomeKey'] for c in p['chapters'][2]['choices']}) == 2
        assert all(not q['memoryKey'] for q in p['recurring'])
    for q in all_quests:
        assert any(g['kind'] in COUNTER_KEYS for g in q['goals']) and all(g['count'] > 0 for g in q['goals'])
        assert all(g['target'] in families | {'park', 'residential_house'} | ids for g in q['goals'] if g['kind'] == 0)
    for index, p in enumerate(projects):
        expected = [v['id'] + '_chapter_' + str(PROJECTS[index][5]) for v in people]
        assert p['quest']['prerequisites'] == expected
        assert p['host'] in p['participants'] and set(p['participants']) == {v['id'] for v in people}
    texts = {t['key']: t['de'] for t in data['texts']}
    assert len(texts) == len(data['texts'])
    required = set(added_texts)
    assert RECURRING_BODY_KEYS <= required
    assert all(not any(character.isdigit() for character in value) for key in RECURRING_BODY_KEYS
               for value in added_texts[key].values()), 'Recurring prose must not embed unscaled target amounts'
    for lang in LANGUAGES:
        entries = {e['key']: e['value'] for e in catalogs[lang]['entries']}
        assert len(entries) == len(catalogs[lang]['entries']) and required <= entries.keys()
        assert all(entries[k].strip() and fields(entries[k]) == fields(texts[k]) for k in required), lang
    assert not any(any(other in value for other in ('Purple Garden', 'Little Montana', 'Seven Hills')) for translations in added_texts.values() for value in translations.values())


if __name__ == '__main__':
    author()

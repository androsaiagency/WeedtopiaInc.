"""Explicit, additive and idempotent Seven Hills content authoring.

Run from any directory. Existing city content and translations are never overwritten.
The campaign uses the existing building, team, delivery and resident story contracts.
"""
import copy
import hashlib
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[2]
PATH = ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json'
LOCALES = ROOT / 'Assets/Weedtopia/Game/Localization'
CITY = 'seven_hills'
VERSION = 4
LANGUAGES = ('de', 'en', 'es', 'fr', 'ru')


def tr(de, en, es, fr, ru):
    return dict(zip(LANGUAGES, (de, en, es, fr, ru)))


TEXT = {
    'campaign.view_offer': tr('Auftrag ansehen', 'View mandate', 'Ver el encargo', 'Voir la mission', 'Посмотреть поручение'),
    'city.seven_hills': tr('Seven Hills', 'Seven Hills', 'Seven Hills', 'Seven Hills', 'Seven Hills'),
    'campaign.seven_hills_title': tr('Rocko: Sieben Hügel brauchen uns', 'Rocko: Seven hills need us', 'Rocko: Siete colinas nos necesitan', 'Rocko : Sept collines ont besoin de nous', 'Рокко: Семь холмов ждут нас'),
    'campaign.seven_hills_body': tr(
        'Die Regierung hat auch den Bürgermeister von Seven Hills entlassen. Sein Luxusprojekt „Seven Heights“ hat Bewohner verdrängt und öffentliche Wege gesperrt. 5.000 Menschen leben inzwischen in Burning Hills und Big Rock City zusammen. Jetzt können wir den Zurückgebliebenen helfen, ihre Nachbarschaften wieder zu verbinden. Sieben Hügel, ein Zuhause.',
        'The government has dismissed the mayor of Seven Hills too. His luxury project “Seven Heights” displaced residents and closed public paths. Burning Hills and Big Rock City now have 5,000 residents between them. We can help those who stayed reconnect their neighbourhoods. Seven hills, one home.',
        'El gobierno también ha destituido al alcalde de Seven Hills. Su proyecto de lujo «Seven Heights» desplazó a vecinos y cerró caminos públicos. Burning Hills y Big Rock City ya suman 5.000 habitantes. Podemos ayudar a quienes se quedaron a reconectar sus barrios. Siete colinas, un hogar.',
        'Le gouvernement a aussi destitué le maire de Seven Hills. Son projet de luxe « Seven Heights » a déplacé des habitants et fermé des chemins publics. Burning Hills et Big Rock City comptent désormais 5 000 habitants à elles deux. Aidons ceux qui sont restés à relier leurs quartiers. Sept collines, un foyer.',
        'Правительство уволило и мэра Seven Hills. Его роскошный проект «Seven Heights» вытеснил жителей и перекрыл общественные дорожки. В Burning Hills и Big Rock City вместе уже живут 5 000 человек. Поможем тем, кто остался, снова объединить свои кварталы. Семь холмов, один дом.'),
    'campaign.seven_hills_notice': tr('Rocko hat einen neuen Regierungsauftrag: Seven Hills braucht uns.', 'Rocko has a new government mandate: Seven Hills needs us.', 'Rocko tiene un nuevo encargo del gobierno: Seven Hills nos necesita.', 'Rocko a une nouvelle mission du gouvernement : Seven Hills a besoin de nous.', 'У Рокко новое поручение правительства: Seven Hills ждёт нашей помощи.'),
    'campaign.combined_locked': tr('Der Auftrag erscheint ab {0} Einwohnern in Burning Hills und Big Rock City zusammen. Aktuell: {1}.', 'The mandate becomes available at {0} residents across Burning Hills and Big Rock City. Current total: {1}.', 'El encargo aparece al sumar {0} habitantes en Burning Hills y Big Rock City. Total actual: {1}.', 'La mission devient disponible à {0} habitants au total dans Burning Hills et Big Rock City. Total actuel : {1}.', 'Поручение откроется при {0} жителях в Burning Hills и Big Rock City вместе. Сейчас: {1}.'),
    'district.seven_hills.farm': tr('Terrassengärten', 'Terrace Gardens', 'Jardines en Terrazas', 'Jardins en terrasses', 'Террасные сады'),
    'district.seven_hills.city': tr('Altstadtbogen', 'Old Town Crescent', 'Arco del Casco Antiguo', 'Arc du vieux quartier', 'Дуга старого города'),
    'district.seven_hills.settlement': tr('Panoramaquartier', 'Panorama Quarter', 'Barrio Panorámico', 'Quartier panoramique', 'Панорамный квартал'),
    'district.seven_hills.industry': tr('Werkhof', 'Works Yard', 'Patio de Talleres', 'Cour des ateliers', 'Производственный двор'),
    'sh.dialog.mila': tr('Die Gärten können wieder Menschen versorgen. Beginnen wir mit einer Ernte und einem verlässlichen Team.', 'The gardens can feed people again. Let us begin with a harvest and a reliable team.', 'Los jardines pueden volver a abastecer a los vecinos. Empecemos con una cosecha y un equipo fiable.', 'Les jardins peuvent à nouveau nourrir les habitants. Commençons par une récolte et une équipe fiable.', 'Сады снова могут снабжать жителей. Начнём с урожая и надёжной команды.'),
    'sh.dialog.jaro': tr('Hinter den Luxusfassaden stehen echte Maschinen. Mit Verarbeitung und festen Lieferwegen werden sie wieder nützlich.', 'There are real machines behind the luxury façades. Processing and dependable routes will make them useful again.', 'Detrás de las fachadas de lujo hay máquinas reales. Con procesamiento y rutas fiables volverán a ser útiles.', 'De vraies machines se cachent derrière les façades de luxe. La transformation et des livraisons fiables leur rendront leur utilité.', 'За роскошными фасадами стоят настоящие машины. Переработка и надёжные маршруты снова сделают их полезными.'),
    'sh.dialog.nia': tr('Diese Kisten gehören in die Nachbarschaften. Wir öffnen die Lager und bringen die Versorgung zwischen den Hügeln in Gang.', 'These crates belong in the neighbourhoods. Let us open the warehouses and get supplies moving between the hills.', 'Estas cajas deben llegar a los barrios. Abramos los almacenes y pongamos en marcha el suministro entre las colinas.', 'Ces caisses doivent arriver dans les quartiers. Ouvrons les entrepôts et relançons les livraisons entre les collines.', 'Эти ящики нужны кварталам. Откроем склады и наладим снабжение между холмами.'),
    'sh.dialog.rocko': tr('Seven Heights versprach sieben Luxusinseln. Wir bauen eine Stadt, in der Menschen wieder zusammenleben.', 'Seven Heights promised seven islands of luxury. We are building a city where people live together again.', 'Seven Heights prometía siete islas de lujo. Construimos una ciudad donde los vecinos vuelvan a convivir.', 'Seven Heights promettait sept îlots de luxe. Nous bâtissons une ville où les habitants vivent de nouveau ensemble.', 'Seven Heights обещал семь островков роскоши. Мы строим город, где люди снова живут вместе.'),
    'sh.goal.level': tr('{0} ausbauen: Stufe {1}/{2}', 'Upgrade {0}: level {1}/{2}', 'Mejorar {0}: nivel {1}/{2}', 'Améliorer {0} : niveau {1}/{2}', 'Улучшить {0}: уровень {1}/{2}'),
    'sh.goal.raw': tr('Neue Rohware seit Annahme: {1}/{2}', 'New raw goods since acceptance: {1}/{2}', 'Materia prima nueva desde la aceptación: {1}/{2}', 'Nouvelles matières premières depuis acceptation : {1}/{2}', 'Новое сырьё после принятия: {1}/{2}'),
    'sh.goal.refined': tr('Neue verarbeitete Ware seit Annahme: {1}/{2}', 'New refined goods since acceptance: {1}/{2}', 'Productos procesados nuevos desde la aceptación: {1}/{2}', 'Nouveaux produits transformés depuis acceptation : {1}/{2}', 'Новая переработанная продукция после принятия: {1}/{2}'),
    'sh.goal.coffee': tr('Neue Caféverkäufe seit Annahme: {1}/{2}', 'New café sales since acceptance: {1}/{2}', 'Ventas nuevas del café desde la aceptación: {1}/{2}', 'Nouvelles ventes au café depuis acceptation : {1}/{2}', 'Новые продажи в кафе после принятия: {1}/{2}'),
    'sh.goal.butter': tr('Neue Butterproduktion seit Annahme: {1}/{2}', 'New butter produced since acceptance: {1}/{2}', 'Mantequilla nueva desde la aceptación: {1}/{2}', 'Nouveau beurre produit depuis acceptation : {1}/{2}', 'Новое масло после принятия: {1}/{2}'),
    'sh.goal.pastry': tr('Neu verkauftes Gebäck seit Annahme: {1}/{2}', 'New pastry sales since acceptance: {1}/{2}', 'Bollería vendida desde la aceptación: {1}/{2}', 'Nouvelles pâtisseries vendues depuis acceptation : {1}/{2}', 'Новые продажи выпечки после принятия: {1}/{2}'),
    'sh.goal.delivered': tr('Neu gelieferte Waren seit Annahme: {1}/{2}', 'New goods delivered since acceptance: {1}/{2}', 'Productos entregados desde la aceptación: {1}/{2}', 'Nouveaux produits livrés depuis acceptation : {1}/{2}', 'Новые доставленные товары после принятия: {1}/{2}'),
    'sh.goal.deliveries': tr('Neue abgeschlossene Lieferungen seit Annahme: {1}/{2}', 'New completed deliveries since acceptance: {1}/{2}', 'Entregas completadas desde la aceptación: {1}/{2}', 'Nouvelles livraisons terminées depuis acceptation : {1}/{2}', 'Новые завершённые доставки после принятия: {1}/{2}'),
    'sh.choice.help': tr('Ich packe mit an', 'I will help', 'Voy a ayudar', 'Je vais aider', 'Я помогу'),
    'sh.outcome.help': tr('Gemeinsam geben wir diesem Ort wieder einen Alltag.', 'Together we give this place its everyday life back.', 'Juntos devolvemos la vida cotidiana a este lugar.', 'Ensemble, nous rendons son quotidien à ce lieu.', 'Вместе мы возвращаем этому месту повседневную жизнь.'),
    'sh.choice.share': tr('Wissen mit den Nachbarn teilen', 'Share knowledge with neighbours', 'Compartir conocimientos con los vecinos', 'Partager le savoir avec les voisins', 'Поделиться знаниями с соседями'),
    'sh.outcome.share': tr('Die Nachbarn übernehmen eigene Aufgaben und geben ihre Erfahrungen weiter.', 'Neighbours take on their own tasks and pass on what they know.', 'Los vecinos asumen tareas propias y comparten su experiencia.', 'Les voisins prennent leurs propres responsabilités et transmettent leur expérience.', 'Соседи берут на себя задачи и передают свой опыт.'),
    'sh.choice.meet': tr('Einen gemeinsamen Treffpunkt gestalten', 'Create a shared meeting place', 'Crear un lugar de encuentro común', 'Créer un lieu de rencontre commun', 'Создать общее место встреч'),
    'sh.outcome.meet': tr('Ein gemeinsam gestalteter Treffpunkt verbindet die persönlichen Geschichten.', 'A meeting place shaped together connects their personal stories.', 'Un lugar diseñado entre todos conecta sus historias personales.', 'Un lieu aménagé ensemble relie leurs histoires personnelles.', 'Общее место встреч связывает их личные истории.'),
    'sh.choice.quiet': tr('Ein ruhiger Nachbarschaftshof', 'A quiet neighbourhood courtyard', 'Un patio vecinal tranquilo', 'Une cour de quartier paisible', 'Тихий соседский двор'),
    'sh.outcome.quiet': tr('Sitzplätze, Pflanzen und Bilder schaffen einen ruhigen Ort für Gespräche.', 'Seats, plants and pictures create a quiet place to talk.', 'Asientos, plantas y fotos crean un lugar tranquilo para conversar.', 'Des sièges, des plantes et des photos créent un lieu calme pour discuter.', 'Скамейки, растения и фотографии создают тихое место для разговоров.'),
    'sh.choice.lively': tr('Eine lebendige Terrasse mit kleinen Ständen', 'A lively terrace with small stalls', 'Una terraza animada con pequeños puestos', 'Une terrasse animée avec de petits stands', 'Оживлённая терраса с небольшими прилавками'),
    'sh.outcome.lively': tr('Markisen, kleine Stände und Wegweiser machen die Terrasse zum belebten Treffpunkt.', 'Awnings, small stalls and signs make the terrace a lively meeting place.', 'Toldos, puestos y señales convierten la terraza en un encuentro animado.', 'Des auvents, des stands et des panneaux font de la terrasse un lieu animé.', 'Навесы, прилавки и указатели превращают террасу в оживлённое место встреч.'),
    'sh.repeat.completed': tr('Diese neue Runde ist angekommen. Die Bewohner bleiben und melden sich später wieder.', 'This new round has arrived. The residents stay and will ask again later.', 'Esta nueva ronda ha llegado. Los vecinos se quedan y volverán a pedir ayuda más adelante.', 'Cette nouvelle tournée est arrivée. Les habitants restent et reviendront plus tard.', 'Новая партия доставлена. Жители остаются и позже снова обратятся за помощью.'),
    'sh.joint.completed': tr('Die Nachbarn haben diesen Ort gemeinsam zurückgewonnen. Ihre Geschichten gehen hier weiter.', 'The neighbours have reclaimed this place together. Their stories continue here.', 'Los vecinos han recuperado juntos este lugar. Sus historias continúan aquí.', 'Les voisins ont reconquis ce lieu ensemble. Leurs histoires se poursuivent ici.', 'Соседи вместе вернули себе это место. Их истории продолжаются здесь.'),
}


def chapter(title, body, memory, family, level, counter, count):
    return dict(title=title, body=body, memory=memory, family=family, level=level, counter=counter, count=count)


PEOPLE = [
    dict(id='mila', name='Mila Morgen', residents=0, focus='settlement_01', thresholds=[0, 250, 1500, 5000],
         role=tr('Lehrerin mit einem Bücherwagen', 'Teacher with a book cart', 'Profesora con un carrito de libros', 'Enseignante au chariot de livres', 'Учительница с книжной тележкой'),
         bio=tr('Mila ist geblieben und hält im Wohnhof einen Lesetreff aufrecht. Sie hilft allen, lässt sich aber selten helfen.', 'Mila stayed and keeps a reading group going in the courtyard. She helps everyone but rarely accepts help.', 'Mila se quedó y mantiene un grupo de lectura en el patio. Ayuda a todos, pero apenas acepta ayuda.', 'Mila est restée et anime un cercle de lecture dans la cour. Elle aide tout le monde, mais accepte rarement de l’aide.', 'Мила осталась и ведёт встречи читателей во дворе. Она помогает всем, но редко принимает помощь.'),
         greeting=tr('Ein freier Stuhl und ein gutes Buch sind ein Anfang.', 'An empty chair and a good book are a start.', 'Una silla libre y un buen libro son un comienzo.', 'Une chaise libre et un bon livre, voilà un début.', 'Свободный стул и хорошая книга — уже начало.'),
         chapters=[
             chapter(tr('Der Bücherwagen bleibt', 'The book cart stays', 'El carrito de libros se queda', 'Le chariot de livres reste', 'Книжная тележка остаётся'), tr('Mila öffnet ihren Bücherwagen zwischen den Absperrungen. Eine frische Lieferung versorgt die Menschen, die geblieben sind.', 'Mila opens her book cart between the barriers. A fresh delivery supplies the people who stayed.', 'Mila abre su carrito entre las vallas. Una entrega nueva abastece a quienes se quedaron.', 'Mila ouvre son chariot entre les barrières. Une nouvelle livraison ravitaille ceux qui sont restés.', 'Мила открывает тележку среди ограждений. Свежая доставка снабдит тех, кто остался.'), tr('Milas offener Bücherwagen', 'Mila’s open book cart', 'El carrito abierto de Mila', 'Le chariot ouvert de Mila', 'Открытая книжная тележка Милы'), 'farm_warehouse', 1, 9, 1),
             chapter(tr('Licht im Morgenhof', 'Light in the Morning Courtyard', 'Luz en el Patio del Alba', 'Lumière dans la cour du matin', 'Свет в Утреннем дворе'), tr('Juna bringt Stoff, Safiya lädt die Nachbarn ein. Mila braucht einen bewohnten Hof und eine gemeinsame erste Runde.', 'Juna brings fabric and Safiya invites the neighbours. Mila needs an inhabited courtyard and a first shared round.', 'Juna trae telas y Safiya invita a los vecinos. Mila necesita un patio habitado y una primera ronda compartida.', 'Juna apporte du tissu et Safiya invite les voisins. Mila veut une cour habitée et une première tournée commune.', 'Юна приносит ткань, а Сафия зовёт соседей. Миле нужен жилой двор и первая общая встреча.'), tr('Die Lesebank im Morgenhof', 'The reading bench in the Morning Courtyard', 'El banco de lectura del Patio del Alba', 'Le banc de lecture de la cour du matin', 'Скамья для чтения в Утреннем дворе'), 'residential_house', 5, 5, 20),
             chapter(tr('Andere können das auch', 'Others can do it too', 'Otros también pueden hacerlo', 'Les autres savent aussi le faire', 'Другие тоже справятся'), tr('Mila ist erschöpft. Hilf ihr, den Lesetreff mit den Nachbarn zu teilen und auch selbst einen freien Nachmittag zu haben.', 'Mila is exhausted. Help her share the reading group with neighbours and take an afternoon off herself.', 'Mila está agotada. Ayúdala a compartir el grupo con los vecinos y a tener una tarde libre.', 'Mila est épuisée. Aidons-la à partager le cercle de lecture et à prendre un après-midi libre.', 'Мила устала. Помоги ей разделить заботы о встречах с соседями и освободить себе день.'), tr('Der gemeinsam betreute Lesetreff', 'The reading group run together', 'El grupo de lectura compartido', 'Le cercle de lecture partagé', 'Читательские встречи под общей заботой'), 'park', 10, 8, 80),
             chapter(tr('Eine Geschichte für morgen', 'A story for tomorrow', 'Una historia para mañana', 'Une histoire pour demain', 'История для завтрашнего дня'), tr('Andere Bewohner übernehmen nun eigene Treffen. Mila stellt den Bücherwagen am Panoramaring auf und hört einmal selbst zu.', 'Other residents now run their own meetings. Mila brings the book cart to the Panorama Ring and listens for once.', 'Otros vecinos organizan sus reuniones. Mila lleva el carrito al Anillo Panorámico y, por una vez, escucha.', 'D’autres habitants animent leurs rencontres. Mila installe son chariot sur l’anneau panoramique et prend le temps d’écouter.', 'Другие жители уже проводят свои встречи. Мила ставит тележку на Панорамном кольце и наконец слушает сама.'), tr('Milas weitergegebenes Lesebuch', 'Mila’s reading book passed on', 'El libro de lectura que Mila comparte', 'Le livre de lecture transmis par Mila', 'Переданная Милей книга для чтения'), 'residential_house', 25, 9, 12),
         ]),
    dict(id='tariq', name='Tariq Trepp', residents=50, focus='city_warehouse', thresholds=[50, 750, 2000, 5000],
         role=tr('Kurier der sieben Hügel', 'Courier of the seven hills', 'Mensajero de las siete colinas', 'Coursier des sept collines', 'Курьер семи холмов'),
         bio=tr('Tariq kennt jeden Umweg. Er möchte einen verlässlichen Lieferstützpunkt, glaubt aber noch, alles selbst retten zu müssen.', 'Tariq knows every detour. He wants a reliable delivery base but still thinks he has to rescue everything himself.', 'Tariq conoce todos los desvíos. Quiere una base de reparto fiable, pero cree que debe resolverlo todo solo.', 'Tariq connaît chaque détour. Il veut un relais fiable, mais pense encore devoir tout sauver seul.', 'Тарик знает каждый обход. Он мечтает о надёжном пункте доставки, но пока считает, что должен спасать всё сам.'),
         greeting=tr('Es gibt einen Weg. Wir müssen ihn nur wieder öffnen.', 'There is a way. We just have to open it again.', 'Hay un camino. Solo tenemos que volver a abrirlo.', 'Il existe un chemin. Il suffit de le rouvrir.', 'Путь есть. Нужно лишь снова его открыть.'),
         chapters=[
             chapter(tr('Die Karte voller Umwege', 'The map of detours', 'El mapa de los desvíos', 'La carte des détours', 'Карта обходов'), tr('Tariqs Karte zeigt gesperrte Durchgänge statt kurzer Wege. Neue Lieferungen helfen ihm, eine verlässliche Strecke zu markieren.', 'Tariq’s map shows closed passages instead of shortcuts. Fresh deliveries help him mark a dependable route.', 'El mapa de Tariq muestra pasos cerrados. Nuevas entregas le ayudan a marcar una ruta fiable.', 'La carte de Tariq montre des passages fermés. De nouvelles livraisons l’aident à tracer une route fiable.', 'На карте Тарика перекрытые проходы вместо коротких путей. Новые доставки помогут отметить надёжный маршрут.'), tr('Tariqs korrigierte Hügelkarte', 'Tariq’s corrected hill map', 'El mapa corregido de Tariq', 'La carte corrigée de Tariq', 'Исправленная карта холмов Тарика'), 'city_warehouse', 2, 9, 3),
             chapter(tr('Ein Platz für die Taschen', 'A place for the bags', 'Un sitio para las bolsas', 'Une place pour les sacoches', 'Место для сумок'), tr('Im Verbindungshügel findet Tariq einen alten Hof. Mit regelmäßig ankommenden Waren kann daraus ein Lieferstützpunkt werden.', 'Tariq finds an old yard on Connection Hill. Regular incoming goods can turn it into a delivery base.', 'Tariq encuentra un patio en la Colina de la Conexión. Las entregas regulares pueden convertirlo en una base.', 'Tariq trouve une ancienne cour sur la colline de liaison. Des arrivages réguliers peuvent en faire un relais.', 'На Соединительном холме Тарик находит старый двор. Регулярные поставки превратят его в пункт доставки.'), tr('Der Lieferstützpunkt am Verbindungshügel', 'The delivery base on Connection Hill', 'La base de reparto de la Colina de la Conexión', 'Le relais de la colline de liaison', 'Пункт доставки на Соединительном холме'), 'city_warehouse', 5, 8, 70),
             chapter(tr('Die zweite Tasche', 'The second bag', 'La segunda bolsa', 'La deuxième sacoche', 'Вторая сумка'), tr('Ein neuer Kollege möchte helfen. Tariq muss lernen, eine Route abzugeben und auf die Arbeit anderer zu vertrauen.', 'A new colleague wants to help. Tariq must learn to hand over a route and trust someone else’s work.', 'Un compañero quiere ayudar. Tariq debe ceder una ruta y confiar en el trabajo de otros.', 'Un collègue veut aider. Tariq doit confier une tournée et faire confiance au travail des autres.', 'Новый коллега хочет помочь. Тарику предстоит передать маршрут и довериться чужой работе.'), tr('Tariqs gemeinsam genutztes Taschenregal', 'Tariq’s shared bag shelf', 'La estantería de bolsas compartida de Tariq', 'L’étagère de sacoches partagée de Tariq', 'Общая полка для сумок Тарика'), 'farm_warehouse', 10, 9, 8),
             chapter(tr('Heute komme ich zu Fuß', 'Today I will walk', 'Hoy voy a pie', 'Aujourd’hui, je viens à pied', 'Сегодня я пойду пешком'), tr('Die Lieferungen laufen mit einem festen Team. Tariq legt seine alte Tasche ab und besucht die Nachbarn auf dem Panoramaring.', 'Deliveries now run with a steady team. Tariq puts down his old bag and visits neighbours on the Panorama Ring.', 'Un equipo estable lleva los repartos. Tariq deja su vieja bolsa y visita a los vecinos del Anillo Panorámico.', 'Une équipe stable assure les livraisons. Tariq pose sa vieille sacoche et visite les voisins de l’anneau panoramique.', 'Доставками занимается постоянная команда. Тарик откладывает старую сумку и идёт к соседям на Панорамном кольце.'), tr('Tariqs alte Tasche mit Nachbarsgrüßen', 'Tariq’s old bag with neighbours’ greetings', 'La vieja bolsa de Tariq con saludos vecinales', 'La vieille sacoche de Tariq et les mots des voisins', 'Старая сумка Тарика с приветами соседей'), 'city_warehouse', 25, 8, 160),
         ]),
    dict(id='juna', name='Juna Faden', residents=150, focus='coffee_02', thresholds=[150, 750, 2500, 5000],
         role=tr('Zurückkehrende Schneiderin', 'Returning tailor', 'Costurera que regresa', 'Couturière de retour', 'Вернувшаяся портниха'),
         bio=tr('Juna verlor ihr Atelier an Seven Heights. Sie möchte zurückkehren, braucht aber sichtbare Gründe, den neuen Versprechen zu glauben.', 'Juna lost her studio to Seven Heights. She wants to return but needs visible reasons to believe the new promises.', 'Juna perdió su taller por Seven Heights. Quiere volver, pero necesita motivos visibles para confiar en las promesas.', 'Juna a perdu son atelier à cause de Seven Heights. Elle veut revenir, mais doit voir des raisons de croire aux promesses.', 'Юна потеряла ателье из-за Seven Heights. Она хочет вернуться, но ей нужны зримые причины поверить новым обещаниям.'),
         greeting=tr('Ein Zuhause erkennt man an den kleinen eigenen Dingen.', 'A home is recognised by the little things made your own.', 'Un hogar se reconoce en los pequeños detalles propios.', 'On reconnaît un foyer à ses petits détails personnels.', 'Дом узнаётся по маленьким личным вещам.'),
         chapters=[
             chapter(tr('Ein Tisch vor der Tür', 'A table by the door', 'Una mesa ante la puerta', 'Une table devant la porte', 'Стол у двери'), tr('Juna kommt mit Nadel, Stoff und einem Klapptisch zurück. Ein geöffnetes Café und neue Verkäufe geben ihr einen ersten Arbeitsplatz.', 'Juna returns with a needle, fabric and a folding table. An open café and fresh sales give her a first place to work.', 'Juna vuelve con aguja, telas y una mesa. Un café abierto y nuevas ventas le dan su primer lugar de trabajo.', 'Juna revient avec du tissu et une table pliante. Un café ouvert et de nouvelles ventes lui offrent un premier atelier.', 'Юна возвращается с иглой, тканью и складным столом. Открытое кафе и новые продажи дадут ей первое рабочее место.'), tr('Junas erster Schneidertisch', 'Juna’s first tailoring table', 'La primera mesa de costura de Juna', 'La première table de couture de Juna', 'Первый швейный стол Юны'), 'coffee_shop', 2, 5, 15),
             chapter(tr('Die Markise von früher', 'The awning from before', 'El toldo de antes', 'L’auvent d’autrefois', 'Прежний навес'), tr('Ein Nachbar hat Junas alte Markise gerettet. Neue Lieferungen und bewohnte Häuser helfen ihr, das Atelier dauerhaft einzurichten.', 'A neighbour saved Juna’s old awning. Fresh deliveries and inhabited houses help her establish a lasting studio.', 'Un vecino salvó el viejo toldo de Juna. Nuevas entregas y casas habitadas ayudan a instalar su taller.', 'Un voisin a sauvé l’ancien auvent de Juna. Des livraisons et des maisons habitées l’aident à installer son atelier.', 'Сосед сохранил старый навес Юны. Новые поставки и жилые дома помогут ей обустроить постоянное ателье.'), tr('Junas wieder aufgehängte Markise', 'Juna’s awning hung again', 'El toldo recuperado de Juna', 'L’auvent de Juna à nouveau installé', 'Снова вывешенный навес Юны'), 'residential_house', 5, 8, 60),
             chapter(tr('Die Farben der Nachbarn', 'The neighbours’ colours', 'Los colores de los vecinos', 'Les couleurs des voisins', 'Цвета соседей'), tr('Juna näht Stoffschilder für Milas Hof und die Verbindungsterrasse. Entscheide, wie die Nachbarn ihre Ideen beitragen.', 'Juna makes fabric signs for Mila’s courtyard and the Connection Terrace. Choose how neighbours contribute their ideas.', 'Juna cose carteles para el patio de Mila y la Terraza de Conexión. Elige cómo aportan sus ideas los vecinos.', 'Juna coud des panneaux pour la cour de Mila et la terrasse de liaison. Choisis comment les voisins contribuent.', 'Юна шьёт вывески для двора Милы и Соединительной террасы. Выбери, как соседи внесут свои идеи.'), tr('Die Stoffschilder der sieben Hügel', 'The fabric signs of the seven hills', 'Los carteles de tela de las siete colinas', 'Les panneaux en tissu des sept collines', 'Тканевые вывески семи холмов'), 'coffee_shop', 10, 4, 100),
             chapter(tr('Ein Schlüssel, der bleibt', 'A key that stays', 'Una llave que se queda', 'Une clé qui reste', 'Ключ, который остаётся'), tr('Juna gibt ihrem Atelier einen eigenen Namen. Ihre Markisen begleiten den Panoramaring, und sie plant keine Abreise mehr.', 'Juna gives her studio its own name. Her awnings line the Panorama Ring, and she no longer plans to leave.', 'Juna da nombre a su taller. Sus toldos acompañan el Anillo Panorámico y ya no piensa marcharse.', 'Juna donne un nom à son atelier. Ses auvents bordent l’anneau panoramique et elle ne prévoit plus de partir.', 'Юна даёт ателье собственное имя. Её навесы украшают Панорамное кольцо, и она больше не собирается уезжать.'), tr('Junas eigener Atelierschlüssel', 'Juna’s own studio key', 'La llave del taller de Juna', 'La clé de l’atelier de Juna', 'Собственный ключ Юны от ателье'), 'coffee_shop', 25, 5, 160),
         ]),
    dict(id='emil', name='Emil Weitwinkel', residents=300, focus='settlement_03', thresholds=[300, 1000, 3000, 5000],
         role=tr('Fotograf mit neuen Blickwinkeln', 'Photographer finding new angles', 'Fotógrafo con nuevos enfoques', 'Photographe aux nouveaux regards', 'Фотограф с новым взглядом'),
         bio=tr('Emil fotografierte die glänzende Werbung für Seven Heights. Jetzt möchte er die Menschen zeigen, die darauf fehlten.', 'Emil shot the glossy Seven Heights adverts. Now he wants to show the people they left out.', 'Emil hizo los anuncios brillantes de Seven Heights. Ahora quiere mostrar a las personas que quedaron fuera.', 'Emil photographiait les publicités de Seven Heights. Il veut désormais montrer les habitants absents des images.', 'Эмиль снимал глянцевую рекламу Seven Heights. Теперь он хочет показать людей, которых на ней не было.'),
         greeting=tr('Diesmal soll jeder auf dem Bild einen Namen haben.', 'This time everyone in the picture should have a name.', 'Esta vez todos en la foto tendrán nombre.', 'Cette fois, chaque personne sur la photo aura un nom.', 'На этот раз у каждого на снимке будет имя.'),
         chapters=[
             chapter(tr('Die Menschen am Bildrand', 'The people at the edge', 'Las personas al margen', 'Les gens au bord de l’image', 'Люди на краю кадра'), tr('Tariq führt Emil zu Bewohnern abseits der Werbefassaden. Ein versorgtes Viertel ermöglicht die ersten ehrlichen Porträts.', 'Tariq takes Emil to residents away from the show façades. A supplied neighbourhood makes the first honest portraits possible.', 'Tariq lleva a Emil lejos de las fachadas publicitarias. Un barrio abastecido permite sus primeros retratos sinceros.', 'Tariq conduit Emil loin des façades publicitaires. Un quartier ravitaillé permet les premiers portraits sincères.', 'Тарик ведёт Эмиля к жителям вдали от рекламных фасадов. Снабжаемый квартал поможет сделать первые честные портреты.'), tr('Emils erste Nachbarporträts', 'Emil’s first neighbour portraits', 'Los primeros retratos vecinales de Emil', 'Les premiers portraits des voisins d’Emil', 'Первые портреты соседей Эмиля'), 'residential_house', 5, 8, 40),
             chapter(tr('Derselbe Ort, ein neuer Tag', 'Same place, a new day', 'El mismo lugar, otro día', 'Le même lieu, un jour nouveau', 'То же место, новый день'), tr('Emil fotografiert denselben Platz vor und nach dem Wiederaufbau. Neue Caféverkäufe zeigen Alltag statt einer leeren Kulisse.', 'Emil photographs the same square before and after rebuilding. Fresh café sales show daily life instead of an empty set.', 'Emil fotografía la plaza antes y después. Nuevas ventas del café muestran vida diaria en vez de un decorado vacío.', 'Emil photographie la place avant et après les travaux. Des ventes au café montrent la vie plutôt qu’un décor vide.', 'Эмиль снимает одну площадь до и после восстановления. Новые продажи в кафе показывают жизнь вместо пустой декорации.'), tr('Die Bilderreihe vom Wiederaufbau', 'The rebuilding photo series', 'La serie de fotos de la reconstrucción', 'La série de photos de la reconstruction', 'Серия фотографий восстановления'), 'coffee_shop', 10, 5, 70),
             chapter(tr('Ein Bild darf bleiben', 'One picture can stay', 'Una foto puede quedarse', 'Une photo peut rester', 'Снимок может остаться'), tr('Die öffentliche Terrasse braucht ein Bild, das den Nachbarn gehört. Emil bittet sie, gemeinsam den Ausstellungsort zu gestalten.', 'The public terrace needs a picture that belongs to its neighbours. Emil asks them to shape its display together.', 'La terraza necesita una foto de los vecinos. Emil les pide diseñar juntos el lugar de exposición.', 'La terrasse a besoin d’une photo des voisins. Emil leur propose d’aménager ensemble son exposition.', 'Общественной террасе нужен снимок, принадлежащий соседям. Эмиль просит вместе обустроить место для выставки.'), tr('Emils Nachbarbild auf der Aussichtsterrasse', 'Emil’s neighbour photo on the viewing terrace', 'La foto vecinal de Emil en el mirador', 'La photo des voisins d’Emil sur la terrasse', 'Фотография соседей Эмиля на смотровой террасе'), 'park', 10, 8, 120),
             chapter(tr('Anton kommt ins Bild', 'Anton steps into the picture', 'Anton entra en la foto', 'Anton entre dans l’image', 'Антон появляется в кадре'), tr('Anton verlässt sein Fenster für ein gemeinsames Foto im Pavillon. Emil zeigt eine Stadt, deren Bewohner sich wieder begegnen.', 'Anton leaves his window for a group photo in the pavilion. Emil shows a city whose residents meet again.', 'Anton deja su ventana para una foto en el pabellón. Emil muestra una ciudad donde los vecinos vuelven a encontrarse.', 'Anton quitte sa fenêtre pour une photo au pavillon. Emil montre une ville où les habitants se retrouvent.', 'Антон выходит от своего окна ради общего фото в павильоне. Эмиль показывает город, где жители снова встречаются.'), tr('Das gemeinsame Foto am Panoramaring', 'The group photo by the Panorama Ring', 'La foto de grupo del Anillo Panorámico', 'La photo commune de l’anneau panoramique', 'Общее фото у Панорамного кольца'), 'park', 25, 9, 12),
         ]),
    dict(id='safiya', name='Safiya Platz', residents=500, focus='town_hall', thresholds=[500, 1250, 3000, 5000],
         role=tr('Vermittlerin zwischen den Vierteln', 'Mediator between neighbourhoods', 'Mediadora entre los barrios', 'Médiatrice entre les quartiers', 'Посредница между кварталами'),
         bio=tr('Safiya kennt jeden Konflikt und trägt zu viele Aufgaben allein. Sie möchte die Viertel verbinden und auch einen eigenen Platz finden.', 'Safiya knows every conflict and carries too many tasks alone. She wants to connect the quarters and find her own place too.', 'Safiya conoce cada conflicto y carga con demasiado. Quiere unir los barrios y encontrar también su propio espacio.', 'Safiya connaît chaque conflit et porte trop de tâches seule. Elle veut relier les quartiers et trouver aussi sa place.', 'Сафия знает каждый конфликт и слишком многое делает одна. Она хочет объединить кварталы и найти место для себя.'),
         greeting=tr('Ich höre zu. Heute möchte ich auch etwas sagen.', 'I listen. Today I would also like to say something.', 'Escucho. Hoy también quiero decir algo.', 'J’écoute. Aujourd’hui, j’aimerais aussi parler.', 'Я слушаю. Сегодня я тоже хочу кое-что сказать.'),
         chapters=[
             chapter(tr('Der Tisch zwischen den Hügeln', 'The table between the hills', 'La mesa entre las colinas', 'La table entre les collines', 'Стол между холмами'), tr('Safiya lädt Menschen aus oberen und unteren Vierteln ein. Eine frisch versorgte Runde lässt sie erstmals gemeinsam reden.', 'Safiya invites people from the upper and lower quarters. A freshly supplied round lets them talk together for the first time.', 'Safiya invita a los barrios de arriba y abajo. Una ronda recién abastecida permite que hablen por primera vez.', 'Safiya invite les quartiers du haut et du bas. Une tournée bien approvisionnée les réunit enfin.', 'Сафия приглашает жителей верхних и нижних кварталов. Свежие угощения впервые помогут поговорить всем вместе.'), tr('Safiyas Tisch zwischen den Vierteln', 'Safiya’s table between the quarters', 'La mesa de Safiya entre barrios', 'La table de Safiya entre les quartiers', 'Стол Сафии между кварталами'), 'coffee_shop', 5, 5, 40),
             chapter(tr('Wer übernimmt morgen?', 'Who takes over tomorrow?', '¿Quién se encarga mañana?', 'Qui prend le relais demain ?', 'Кто возьмётся завтра?'), tr('Safiya organisiert jeden Termin selbst. Zuverlässige Lieferungen helfen ihr, Verantwortung an andere Nachbarn abzugeben.', 'Safiya organises every meeting herself. Reliable deliveries help her hand responsibility to other neighbours.', 'Safiya organiza cada encuentro sola. Las entregas fiables le ayudan a compartir la responsabilidad.', 'Safiya organise chaque rencontre seule. Des livraisons fiables l’aident à confier des responsabilités aux voisins.', 'Сафия сама организует каждую встречу. Надёжные доставки помогут передать ответственность другим соседям.'), tr('Der gemeinsame Nachbarschaftsplan', 'The shared neighbourhood plan', 'El plan vecinal compartido', 'Le planning partagé du quartier', 'Общий план соседских встреч'), 'city_warehouse', 10, 9, 6),
             chapter(tr('Ein Platz nach eigenen Wünschen', 'A place shaped by our wishes', 'Un lugar a nuestro gusto', 'Un lieu selon nos envies', 'Место по нашим желаниям'), tr('Die leere Luxusterrasse wird öffentlich. Safiya bittet um eine Entscheidung: ruhiger Hof oder lebendige Terrasse mit kleinen Ständen?', 'The empty luxury terrace becomes public. Safiya asks for a choice: a quiet courtyard or a lively terrace with small stalls?', 'La terraza de lujo pasa a ser pública. Safiya pregunta: ¿patio tranquilo o terraza animada con puestos?', 'La terrasse de luxe devient publique. Safiya propose un choix : cour paisible ou terrasse animée avec des stands ?', 'Пустая роскошная терраса становится общественной. Сафия просит выбрать: тихий двор или оживлённая терраса с прилавками?'), tr('Safiyas persönlich gestalteter Terrassenplatz', 'Safiya’s personally shaped terrace place', 'La terraza diseñada por Safiya y los vecinos', 'La terrasse aménagée avec Safiya', 'Терраса, обустроенная вместе с Сафией'), 'park', 10, 8, 100),
             chapter(tr('Auch mein Nachmittag', 'My afternoon too', 'También mi tarde', 'Mon après-midi aussi', 'И мой свободный день'), tr('Andere Bewohner übernehmen ihre Aufgaben. Safiya sitzt an ihrem eigenen grünen Rückzugsplatz, ohne auf den nächsten Termin zu warten.', 'Other residents take over her tasks. Safiya sits in her own green retreat without waiting for the next meeting.', 'Otros vecinos asumen sus tareas. Safiya descansa en su rincón verde sin esperar la siguiente reunión.', 'Les habitants reprennent ses tâches. Safiya profite de son refuge vert sans attendre la prochaine réunion.', 'Другие жители берут на себя её задачи. Сафия отдыхает в своём зелёном уголке, не ожидая следующей встречи.'), tr('Safiyas grüner Rückzugsplatz', 'Safiya’s green retreat', 'El rincón verde de Safiya', 'Le refuge vert de Safiya', 'Зелёный уголок Сафии'), 'park', 25, 7, 100),
         ]),
    dict(id='clara', name='Clara Steig', residents=800, focus='coffee_03', thresholds=[800, 1750, 3500, 5000],
         role=tr('Stadtführerin auf ehrlichen Wegen', 'City guide taking honest paths', 'Guía por caminos sinceros', 'Guide aux parcours sincères', 'Экскурсовод по честным маршрутам'),
         bio=tr('Clara führte Besucher nur durch Vorzeigeanlagen. Sie möchte eine ehrliche Route mit den Geschichten der Menschen entwickeln.', 'Clara only guided visitors through showcase developments. She wants an honest route shaped by people’s stories.', 'Clara solo enseñaba las zonas de escaparate. Quiere una ruta sincera con las historias de los vecinos.', 'Clara ne montrait que les quartiers de prestige. Elle veut un parcours sincère porté par les histoires des habitants.', 'Клара водила гостей только по образцовым площадкам. Теперь она хочет честный маршрут с историями жителей.'),
         greeting=tr('Die schönste Aussicht hat auch eine Geschichte.', 'The best view also has a story.', 'La mejor vista también tiene una historia.', 'Le plus beau panorama a aussi une histoire.', 'У самого красивого вида тоже есть история.'),
         chapters=[
             chapter(tr('Abseits des Prospekts', 'Beyond the brochure', 'Más allá del folleto', 'Au-delà de la brochure', 'За пределами буклета'), tr('Clara besucht mit Tariqs Hilfe die ausgelassenen Straßen. Neue Lieferungen zeigen, wo der Alltag tatsächlich stattfindet.', 'With Tariq’s help, Clara visits streets left out of the brochure. Fresh deliveries show where daily life really happens.', 'Con ayuda de Tariq, Clara visita las calles omitidas. Nuevas entregas muestran dónde sucede la vida diaria.', 'Avec Tariq, Clara visite les rues oubliées. De nouvelles livraisons montrent où se déroule vraiment la vie.', 'С помощью Тарика Клара посещает пропущенные улицы. Новые доставки покажут, где на самом деле кипит жизнь.'), tr('Claras ehrliche erste Stadtroute', 'Clara’s first honest city route', 'La primera ruta sincera de Clara', 'Le premier parcours sincère de Clara', 'Первый честный городской маршрут Клары'), 'city_warehouse', 5, 9, 4),
             chapter(tr('Ein Name an jedem Weg', 'A name along every path', 'Un nombre en cada camino', 'Un nom sur chaque chemin', 'Имя у каждой дорожки'), tr('Clara sammelt Geschichten statt Werbesprüche. Juna näht die ersten Wegweiser für eine gut versorgte Nachbarschaftsrunde.', 'Clara collects stories instead of slogans. Juna makes the first signs for a well-supplied neighbourhood walk.', 'Clara recoge historias en vez de lemas. Juna prepara señales para un paseo vecinal bien abastecido.', 'Clara recueille des histoires plutôt que des slogans. Juna prépare les panneaux d’une promenade bien ravitaillée.', 'Клара собирает истории вместо лозунгов. Юна делает первые указатели для прогулки по снабжаемым кварталам.'), tr('Die persönlichen Wegweiser der Hügel', 'The hills’ personal signposts', 'Las señales personales de las colinas', 'Les panneaux personnels des collines', 'Личные указатели холмов'), 'park', 10, 7, 70),
             chapter(tr('Erst zuhören, dann führen', 'Listen before guiding', 'Escuchar antes de guiar', 'Écouter avant de guider', 'Сначала выслушать, потом вести'), tr('Safiya bittet Clara, die Bewohner selbst erzählen zu lassen. Gemeinsam gestalten sie Haltepunkte mit Platz für persönliche Stimmen.', 'Safiya asks Clara to let residents tell their own stories. Together they create stops with room for personal voices.', 'Safiya pide que los vecinos hablen por sí mismos. Juntas crean paradas con espacio para sus voces.', 'Safiya demande à Clara de laisser parler les habitants. Ensemble, elles créent des haltes pour leurs voix.', 'Сафия просит Клару дать жителям рассказать самим. Вместе они создают остановки для личных историй.'), tr('Claras Runde der Nachbarstimmen', 'Clara’s walk of neighbours’ voices', 'La ruta de las voces vecinales de Clara', 'Le parcours des voix du quartier de Clara', 'Маршрут голосов соседей Клары'), 'coffee_shop', 10, 5, 120),
             chapter(tr('Eine Runde durch unser Zuhause', 'A walk through our home', 'Un paseo por nuestro hogar', 'Une promenade dans notre foyer', 'Прогулка по нашему дому'), tr('Claras Panoramarunde verbindet alle sieben Hügel. Ihre Schilder erinnern an die Projekte, die die Bewohner gemeinsam geschaffen haben.', 'Clara’s panorama walk connects all seven hills. Her signs remember the projects the residents created together.', 'El paseo de Clara conecta las siete colinas. Sus señales recuerdan los proyectos creados por los vecinos.', 'La promenade de Clara relie les sept collines. Ses panneaux rappellent les projets réalisés ensemble.', 'Панорамная прогулка Клары соединяет все семь холмов. Указатели напоминают об общих проектах жителей.'), tr('Claras vollständige Panoramarunde', 'Clara’s complete panorama walk', 'El paseo panorámico completo de Clara', 'La promenade panoramique complète de Clara', 'Полный панорамный маршрут Клары'), 'park', 25, 8, 170),
         ]),
    dict(id='anton', name='Anton Wolk', residents=1200, focus='settlement_18', thresholds=[1200, 2000, 4000, 5000],
         role=tr('Pensionierter Wetterbeobachter', 'Retired weather observer', 'Observador meteorológico jubilado', 'Observateur météo à la retraite', 'Наблюдатель погоды на пенсии'),
         bio=tr('Anton lebt am höchsten Aussichtspunkt. Seit der Krise sieht er die Stadt fast nur aus seinem Fenster und möchte wieder dazugehören.', 'Anton lives at the highest viewpoint. Since the crisis he mostly watches from his window and wants to belong again.', 'Anton vive en el mirador más alto. Desde la crisis observa por la ventana y quiere volver a formar parte de la ciudad.', 'Anton vit au plus haut belvédère. Depuis la crise, il regarde surtout par sa fenêtre et veut retrouver sa place.', 'Антон живёт у самой высокой смотровой площадки. После кризиса он смотрит из окна и хочет снова стать частью города.'),
         greeting=tr('Der Wind kommt zurück. Vielleicht sollte ich das auch.', 'The wind comes back. Perhaps I should too.', 'El viento vuelve. Quizá yo también debería.', 'Le vent revient. Peut-être devrais-je en faire autant.', 'Ветер возвращается. Возможно, мне тоже пора.'),
         chapters=[
             chapter(tr('Das Fenster am Wolkenhügel', 'The window on Cloud Hill', 'La ventana de la Colina de las Nubes', 'La fenêtre de la colline des nuages', 'Окно на Облачном холме'), tr('Emil besucht Anton mit einem Foto der neuen Nachbarschaft. Eine frische Lieferung gibt Anlass zum ersten Gespräch seit langer Zeit.', 'Emil visits Anton with a photo of the new neighbourhood. A fresh delivery starts their first conversation in a long time.', 'Emil visita a Anton con una foto del barrio. Una nueva entrega abre su primera conversación en mucho tiempo.', 'Emil rend visite à Anton avec une photo du quartier. Une nouvelle livraison lance leur première conversation depuis longtemps.', 'Эмиль приходит к Антону с фото нового квартала. Свежая доставка станет поводом для первого за долгое время разговора.'), tr('Antons wieder geöffnetes Fenster', 'Anton’s reopened window', 'La ventana reabierta de Anton', 'La fenêtre d’Anton rouverte', 'Снова открытое окно Антона'), 'residential_house', 5, 9, 4),
             chapter(tr('Der Pavillon wird geöffnet', 'The pavilion opens', 'El pabellón abre', 'Le pavillon ouvre', 'Павильон открывается'), tr('Anton findet den Schlüssel seines alten Pavillons. Ein gepflegter Park und verlässliche Versorgung machen den Aussichtspunkt zugänglich.', 'Anton finds the key to his old pavilion. A cared-for park and reliable supplies make the viewpoint accessible.', 'Anton encuentra la llave del pabellón. Un parque cuidado y el abastecimiento fiable hacen accesible el mirador.', 'Anton retrouve la clé du pavillon. Un parc entretenu et un ravitaillement fiable rendent le belvédère accessible.', 'Антон находит ключ от старого павильона. Ухоженный парк и надёжное снабжение откроют смотровую площадку.'), tr('Antons offener Aussichtspavillon', 'Anton’s open viewing pavilion', 'El pabellón abierto de Anton', 'Le pavillon ouvert d’Anton', 'Открытый смотровой павильон Антона'), 'park', 10, 8, 100),
             chapter(tr('Wolken lesen', 'Reading clouds', 'Leer las nubes', 'Lire les nuages', 'Читать облака'), tr('Jüngere Bewohner fragen Anton nach Wind und Wolken. Er entscheidet mit ihnen, wie seine Beobachtungen weitergegeben werden.', 'Younger residents ask Anton about wind and clouds. Together they decide how to pass on his observations.', 'Los jóvenes preguntan a Anton por el viento y las nubes. Juntos deciden cómo compartir sus observaciones.', 'Les plus jeunes interrogent Anton sur les nuages. Ils choisissent ensemble comment transmettre ses observations.', 'Молодые жители спрашивают Антона о ветре и облаках. Вместе они решают, как передать его наблюдения.'), tr('Antons geteiltes Wetterbuch', 'Anton’s shared weather book', 'El cuaderno meteorológico compartido de Anton', 'Le carnet météo partagé d’Anton', 'Общая книга погоды Антона'), 'park', 25, 7, 120),
             chapter(tr('Der Blick gehört allen', 'The view belongs to everyone', 'La vista es de todos', 'Le panorama appartient à tous', 'Вид принадлежит всем'), tr('Am Panoramaring erklärt Anton seine Windfahne. Sein Fernglas steht wieder draußen, und auf Emils Gruppenfoto ist er mittendrin.', 'On the Panorama Ring, Anton explains his weather vane. His telescope is outside again, and he stands in Emil’s group photo.', 'En el Anillo Panorámico, Anton explica su veleta. Su telescopio vuelve a estar fuera y él aparece en la foto de Emil.', 'Sur l’anneau panoramique, Anton explique sa girouette. Sa longue-vue est dehors et il figure sur la photo d’Emil.', 'На Панорамном кольце Антон объясняет свой флюгер. Телескоп снова стоит снаружи, а он сам — в центре общего фото Эмиля.'), tr('Antons Fernglas und Windfahne', 'Anton’s telescope and weather vane', 'El telescopio y la veleta de Anton', 'La longue-vue et la girouette d’Anton', 'Телескоп и флюгер Антона'), 'park', 25, 9, 15),
         ]),
]


def condition(kind, target='', count=1):
    # Main quests use the established ConditionKind values, including city-scoped counters.
    return dict(kind=kind, target=target, count=count)


def main(id, title, body, focus, conditions, unlock, money=150, workers=(), cards=0, items=(), speaker='Rocko'):
    return dict(id=id, title=title, body=body, focus=focus, conditions=conditions, unlock=unlock,
                moneyReward=money, workerRewards=list(workers), cardsReward=cards,
                itemRewards=[dict(id=id, count=count) for id, count in items], speaker=speaker)


MAIN = [
    main('arrival', tr('Die Stadt hinter den Absperrungen', 'The city behind the barriers', 'La ciudad tras las vallas', 'La ville derrière les barrières', 'Город за ограждениями'),
         tr('Rocko: Seven Heights hat die Nachbarschaften getrennt. Sprich mit mir am Verwaltungspunkt. Wir beginnen mit der Versorgung der Menschen, die geblieben sind.', 'Rocko: Seven Heights divided the neighbourhoods. Talk to me at the administration point. We start by supplying the people who stayed.', 'Rocko: Seven Heights dividió los barrios. Habla conmigo en la administración. Empezamos abasteciendo a quienes se quedaron.', 'Rocko : Seven Heights a divisé les quartiers. Parle-moi au point administratif. Commençons par ravitailler ceux qui sont restés.', 'Рокко: Seven Heights разделил кварталы. Поговори со мной у администрации. Начнём со снабжения тех, кто остался.'),
         'town_hall', [condition(0, 'rocko')], 0, money=0),
    main('first_garden', tr('Grün zwischen den Mauern', 'Green between the walls', 'Verde entre los muros', 'Du vert entre les murs', 'Зелень между стенами'),
         tr('Mila: Die Terrassengärten können wieder liefern. Baue das erste Anbaufeld auf Stufe 1. Ein funktionierender Anfang zählt.', 'Mila: The Terrace Gardens can supply us again. Restore the first growing plot to level 1. A working start matters.', 'Mila: Los jardines pueden abastecernos. Restaura el primer cultivo al nivel 1. Un inicio que funciona importa.', 'Mila : Les jardins peuvent nous ravitailler. Restaure la première culture au niveau 1. Un début concret compte.', 'Мила: Террасные сады снова могут нас снабжать. Восстанови первый участок до уровня 1. Начнём с дела.'),
         'farm_grow_01', [condition(1, 'farm_grow_01')], 0, items=[('build_1m', 2)], speaker='Mila'),
    main('first_harvest', tr('Die erste Kiste für die Nachbarn', 'The first crate for the neighbours', 'La primera caja para los vecinos', 'La première caisse pour les voisins', 'Первый ящик для соседей'),
         tr('Mila: Hole die erste Ernte von Hand ab. Danach bleibt ein Helfer dauerhaft beim Anbaufeld.', 'Mila: Collect the first harvest by hand. Afterwards, a helper stays at the growing plot.', 'Mila: Recoge a mano la primera cosecha. Después, un ayudante se queda en el cultivo.', 'Mila : Récolte la première fois à la main. Un équipier restera ensuite sur la culture.', 'Мила: Собери первый урожай вручную. После этого помощник останется на участке.'),
         'farm_grow_01', [condition(2)], 0, workers=['grow'], speaker='Mila'),
    main('processing', tr('Ein Team hinter den Fassaden', 'A team behind the façades', 'Un equipo tras las fachadas', 'Une équipe derrière les façades', 'Команда за фасадами'),
         tr('Jaro: Teile den Helfer dem Feld zu, sprich mit mir und repariere die Verarbeitung. Die Maschinen sollen wieder dem Alltag dienen.', 'Jaro: Assign the helper to the plot, talk to me and restore processing. The machines should serve everyday life again.', 'Jaro: Asigna al ayudante al cultivo, habla conmigo y repara el procesamiento. Las máquinas servirán a la vida diaria.', 'Jaro : Affecte l’équipier à la culture, parle-moi et répare la transformation. Les machines serviront de nouveau au quotidien.', 'Яро: Назначь помощника на участок, поговори со мной и восстанови переработку. Машины снова нужны городу.'),
         'farm_processing', [condition(3, 'farm_grow_01'), condition(0, 'jaro'), condition(1, 'farm_processing')], 0, workers=['processing'], items=[('build_5m', 1)], speaker='Jaro'),
    main('farm_store', tr('Die Lager öffnen', 'The warehouses open', 'Los almacenes abren', 'Les entrepôts ouvrent', 'Склады открываются'),
         tr('Nia: Besetze die Verarbeitung und repariere nach unserem Gespräch das Farmlager. Die Ware gehört in die Stadt.', 'Nia: Staff processing and restore the farm warehouse after our conversation. The goods belong in the city.', 'Nia: Asigna personal al procesamiento y repara el almacén tras hablar conmigo. La mercancía debe llegar a la ciudad.', 'Nia : Affecte une équipe à la transformation et répare l’entrepôt après notre échange. Les produits doivent rejoindre la ville.', 'Ния: Назначь команду на переработку и после разговора восстанови фермерский склад. Товары нужны городу.'),
         'farm_warehouse', [condition(3, 'farm_processing'), condition(0, 'nia'), condition(1, 'farm_warehouse')], 0, workers=['farm_manager', 'farm_driver'], speaker='Nia'),
    main('farm_routes', tr('Ein Weg durch die Absperrungen', 'A path through the barriers', 'Un camino entre las vallas', 'Un chemin à travers les barrières', 'Путь через ограждения'),
         tr('Nia: Besetze Leitung und Fahrdienst des Farmlagers. Eine abgeschlossene Lieferung und die automatische Farm öffnen den Altstadtbogen.', 'Nia: Staff the farm warehouse manager and driver. A completed delivery and an automated farm open the Old Town Crescent.', 'Nia: Asigna dirección y conductor al almacén. Una entrega y la granja automatizada abren el Arco del Casco Antiguo.', 'Nia : Affecte responsable et chauffeur à l’entrepôt. Une livraison et une ferme automatisée ouvrent le vieux quartier.', 'Ния: Назначь управляющего и водителя на склад. Завершённая доставка и автоматическая ферма откроют старый город.'),
         'farm_warehouse', [condition(3, 'farm_warehouse', 2), condition(4), condition(12, 'farm_grow_01'), condition(12, 'farm_processing'), condition(12, 'farm_warehouse')], 1, cards=1, items=[('build_15m', 1)], speaker='Nia'),
    main('market_reopen', tr('Stühle vor den Ladentüren', 'Chairs outside the shops', 'Sillas ante las tiendas', 'Des chaises devant les boutiques', 'Стулья перед магазинами'),
         tr('Rocko: Repariere das Stadtlager und das erste Café. Juna braucht einen Arbeitsplatz, die Nachbarn einen offenen Treffpunkt.', 'Rocko: Restore the city warehouse and first café. Juna needs a place to work; neighbours need an open meeting place.', 'Rocko: Repara el almacén urbano y el primer café. Juna necesita trabajar y los vecinos un lugar de encuentro.', 'Rocko : Répare l’entrepôt urbain et le premier café. Juna a besoin d’un atelier, les voisins d’un lieu ouvert.', 'Рокко: Восстанови городской склад и первое кафе. Юне нужно рабочее место, а соседям — место встреч.'),
         'city_warehouse', [condition(1, 'city_warehouse'), condition(1, 'coffee_01')], 1, workers=['city_manager', 'city_driver', 'coffee']),
    main('first_meeting', tr('Eine Tasse, ein Wiedersehen', 'A cup and a reunion', 'Una taza y un reencuentro', 'Une tasse et des retrouvailles', 'Чашка и новая встреча'),
         tr('Rocko: Besetze Stadtlager und Café und verkaufe die erste Tasse. Danach öffnen wir das Panoramaquartier für bewohnte Häuser.', 'Rocko: Staff the city warehouse and café and sell the first cup. Then we open the Panorama Quarter for inhabited homes.', 'Rocko: Asigna equipos al almacén y al café y vende la primera taza. Después abriremos el Barrio Panorámico para viviendas.', 'Rocko : Affecte les équipes à l’entrepôt et au café, puis vends une tasse. Nous ouvrirons le quartier panoramique aux logements.', 'Рокко: Назначь команды на склад и в кафе, продай первую чашку. Затем откроем Панорамный квартал для жилья.'),
         'coffee_01', [condition(3, 'city_warehouse', 2), condition(3, 'coffee_01'), condition(5)], 2, money=350, cards=1, items=[('team_pack', 1), ('cash_crate', 1)]),
    main('homes', tr('Hier wohnt noch jemand', 'Someone still lives here', 'Aquí sigue viviendo alguien', 'Quelqu’un vit encore ici', 'Здесь ещё кто-то живёт'),
         tr('Rocko: Errichte auf den Bauplätzen ein Wohnhaus und einen Park. Milas Bücherwagen soll einen belebten Hof bekommen.', 'Rocko: Build a house and a park on the plots. Mila’s book cart deserves a courtyard full of life.', 'Rocko: Construye una casa y un parque en los solares. El carrito de Mila merece un patio lleno de vida.', 'Rocko : Construis une maison et un parc sur les terrains. Le chariot de Mila mérite une cour vivante.', 'Рокко: Построй дом и парк на участках. Книжной тележке Милы нужен живой двор.'),
         'settlement_01', [condition(1, 'residential_house'), condition(1, 'park')], 2, workers=['caretaker', 'ranger'], items=[('build_30m', 1)]),
    main('everyday', tr('Der erste gemeinsame Alltag', 'Our first shared everyday life', 'La primera rutina compartida', 'Notre premier quotidien commun', 'Первые общие будни'),
         tr('Rocko: Besetze Haus und Park, hole einen Tagesertrag ab und erledige einen Bewohnerauftrag. Dann bekommt der Werkhof seine neue Aufgabe.', 'Rocko: Staff the house and park, collect daily income and complete a resident task. Then the Works Yard gains a new purpose.', 'Rocko: Asigna personal a casa y parque, cobra un ingreso diario y cumple un encargo vecinal. El patio de talleres tendrá un nuevo propósito.', 'Rocko : Affecte les équipes à la maison et au parc, collecte un revenu quotidien et aide un habitant. Les ateliers auront un nouveau rôle.', 'Рокко: Назначь работников в дом и парк, собери дневной доход и выполни задание жителя. Тогда оживёт производственный двор.'),
         'settlement_01', [condition(8), condition(9), condition(13), condition(3, 'residential_house'), condition(3, 'park')], 3, money=600, workers=['butter', 'market', 'baker'], items=[('build_1h', 1)]),
    main('workyard', tr('Der Verbindungshügel arbeitet', 'Connection Hill gets to work', 'La Colina de la Conexión trabaja', 'La colline de liaison se met au travail', 'Соединительный холм работает'),
         tr('Rocko: Repariere Butterwerk, Supermarkt und erste Bäckerei. Die bestehenden Produktionsketten versorgen bald alle Hügel.', 'Rocko: Restore the butter factory, supermarket and first bakery. The existing production chains will soon supply every hill.', 'Rocko: Repara la fábrica de mantequilla, el supermercado y la primera panadería. Las cadenas abastecerán las colinas.', 'Rocko : Répare la beurrerie, le supermarché et la première boulangerie. Les chaînes de production ravitailleront toutes les collines.', 'Рокко: Восстанови маслозавод, супермаркет и первую пекарню. Производственные цепочки снабдят все холмы.'),
         'butter_factory', [condition(1, 'butter_factory'), condition(1, 'supermarket'), condition(1, 'bakery_01')], 3, items=[('build_5h', 1)]),
    main('local_supply', tr('Vom Werkhof auf die Tische', 'From the Works Yard to the tables', 'Del taller a las mesas', 'Des ateliers aux tables', 'Из производственного двора на столы'),
         tr('Rocko: Automatisiere die drei Betriebe, produziere Butter und verkaufe Gebäck. Die Versorgung trägt jetzt den weiteren Wiederaufbau.', 'Rocko: Automate the three businesses, produce butter and sell pastries. Supplies now support the next stage of rebuilding.', 'Rocko: Automatiza los tres negocios, produce mantequilla y vende bollería. El suministro sostiene la reconstrucción.', 'Rocko : Automatise les trois commerces, produis du beurre et vends des pâtisseries. Le ravitaillement soutient la reconstruction.', 'Рокко: Автоматизируй три предприятия, произведи масло и продай выпечку. Снабжение поддержит дальнейшее восстановление.'),
         'bakery_01', [condition(12, 'butter_factory'), condition(12, 'supermarket'), condition(12, 'bakery_01'), condition(10), condition(11)], 3, money=500, items=[('build_10h', 1)]),
    main('morning_courtyard', tr('Leben im Morgenhof', 'Life in the Morning Courtyard', 'Vida en el Patio del Alba', 'La vie dans la cour du matin', 'Жизнь в Утреннем дворе'),
         tr('Rocko: 250 Einwohner machen aus Gebäuden eine Nachbarschaft. Baue Wohnraum aus und besuche Mila, Juna und Safiya bei ihren eigenen Geschichten.', 'Rocko: 250 residents turn buildings into a neighbourhood. Expand housing and visit Mila, Juna and Safiya for their own stories.', 'Rocko: 250 habitantes convierten edificios en un barrio. Amplía las viviendas y visita las historias de Mila, Juna y Safiya.', 'Rocko : 250 habitants transforment les bâtiments en quartier. Développe les logements et découvre les histoires de Mila, Juna et Safiya.', 'Рокко: 250 жителей превращают здания в квартал. Расширяй жильё и знакомься с историями Милы, Юны и Сафии.'),
         'settlement_01', [condition(8, count=250)], 3, money=500),
    main('returning', tr('Die Rückkehrer', 'The returnees', 'Quienes regresan', 'Ceux qui reviennent', 'Те, кто возвращается'),
         tr('Rocko: Bei 750 Einwohnern denken frühere Nachbarn an eine Rückkehr. Verlässliche Häuser und offene Betriebe geben ihnen Gründe zu bleiben.', 'Rocko: At 750 residents, former neighbours consider returning. Reliable homes and open businesses give them reasons to stay.', 'Rocko: Con 750 habitantes, antiguos vecinos piensan en volver. Casas fiables y negocios abiertos les dan razones para quedarse.', 'Rocko : À 750 habitants, d’anciens voisins envisagent de revenir. Des logements et des commerces leur donnent des raisons de rester.', 'Рокко: При 750 жителях прежние соседи задумываются о возвращении. Дома и открытые предприятия дадут им причины остаться.'),
         'coffee_02', [condition(8, count=750)], 3, money=600),
    main('upper_lower', tr('Oben und unten', 'Up above and down below', 'Arriba y abajo', 'En haut et en bas', 'Наверху и внизу'),
         tr('Rocko: 1.500 Einwohner brauchen gemeinsame Wege. Die Verbindungsterrasse bringt die Viertel zusammen; Tariq und Clara entwickeln ihre Routen weiter.', 'Rocko: 1,500 residents need shared paths. The Connection Terrace brings the quarters together; Tariq and Clara improve their routes.', 'Rocko: 1.500 habitantes necesitan caminos compartidos. La Terraza de Conexión une los barrios; Tariq y Clara mejoran sus rutas.', 'Rocko : 1 500 habitants ont besoin de chemins communs. La terrasse relie les quartiers ; Tariq et Clara améliorent leurs parcours.', 'Рокко: 1 500 жителям нужны общие пути. Соединительная терраса объединяет кварталы, а Тарик и Клара развивают маршруты.'),
         'city_warehouse', [condition(8, count=1500)], 3, money=700),
    main('public_terrace', tr('Das schönste leere Gebäude', 'The prettiest empty building', 'El edificio vacío más bonito', 'Le plus beau bâtiment vide', 'Самое красивое пустое здание'),
         tr('Rocko: Bei 3.000 Einwohnern wird die alte Luxusterrasse öffentlich. Safiyas drittes Kapitel entscheidet über einen ruhigen Hof oder kleine Stände.', 'Rocko: At 3,000 residents, the old luxury terrace becomes public. Safiya’s third chapter chooses a quiet courtyard or small stalls.', 'Rocko: Con 3.000 habitantes, la terraza de lujo pasa a ser pública. El tercer capítulo de Safiya elige patio tranquilo o puestos.', 'Rocko : À 3 000 habitants, l’ancienne terrasse devient publique. Le troisième chapitre de Safiya choisit cour paisible ou petits stands.', 'Рокко: При 3 000 жителях бывшая роскошная терраса становится общественной. В третьей главе Сафии выбирают тихий двор или прилавки.'),
         'settlement_03', [condition(8, count=3000)], 3, money=800),
    main('panorama', tr('Sieben Hügel, ein Zuhause', 'Seven hills, one home', 'Siete colinas, un hogar', 'Sept collines, un foyer', 'Семь холмов, один дом'),
         tr('Rocko: 5.000 Einwohner leben wieder auf den Hügeln. Alle sieben Bewohner können nun gemeinsam den Panoramaring gestalten; ihre ersten persönlichen Kapitel öffnen das Gemeinschaftsprojekt.', 'Rocko: 5,000 residents live on the hills again. All seven residents can now create the Panorama Ring together; their first personal chapters open the community project.', 'Rocko: 5.000 habitantes viven en las colinas. Los siete vecinos pueden crear el Anillo Panorámico; sus primeros capítulos abren el proyecto común.', 'Rocko : 5 000 habitants vivent sur les collines. Les sept voisins peuvent créer l’anneau panoramique ; leurs premiers chapitres ouvrent le projet commun.', 'Рокко: На холмах снова живут 5 000 человек. Семеро жителей могут вместе создать Панорамное кольцо; их первые главы откроют общий проект.'),
         'settlement_18', [condition(8, count=5000)], 3, money=1200),
    main('home', tr('Auch nach dem Fototermin', 'After the photo opportunity too', 'También después de la foto', 'Même après la séance photo', 'И после фотосъёмки'),
         tr('Rocko: Hole einen Tagesertrag des Supermarkts ab. Seven Hills ist jetzt dein frei ausbaubares Zuhause. Die persönlichen Kapitel und Alltagsaufträge bleiben bei ihren Bewohnern.', 'Rocko: Collect the supermarket’s daily income. Seven Hills is now your home to develop freely. Personal chapters and everyday tasks stay with their residents.', 'Rocko: Cobra el ingreso diario del supermercado. Seven Hills ya es tu hogar para desarrollar. Sus vecinos conservan capítulos y encargos cotidianos.', 'Rocko : Collecte le revenu quotidien du supermarché. Seven Hills est ton foyer à développer librement. Les habitants gardent leurs chapitres et missions.', 'Рокко: Собери дневной доход супермаркета. Seven Hills стал твоим домом для свободного развития. Личные главы и будничные задания остаются у жителей.'),
         'supermarket', [condition(8, count=5000), condition(9, 'supermarket'), condition(12, 'butter_factory'), condition(12, 'supermarket'), condition(12, 'bakery_01')], 3, money=1200, items=[('build_24h', 1)]),
]

REPEAT_CONTEXT = {
    'mila': tr('Mila: Im Morgenhof kommen neue Leser dazu. Hilf bei einer frischen Runde für unseren Lesetreff.', 'Mila: New readers are joining the Morning Courtyard. Help with a fresh round for our reading group.', 'Mila: Hay nuevos lectores en el patio. Ayuda con una ronda nueva para el grupo.', 'Mila : De nouveaux lecteurs arrivent dans la cour. Aide-nous pour une nouvelle tournée.', 'Мила: Во дворе появляются новые читатели. Помоги собрать новую партию для наших встреч.'),
    'tariq': tr('Tariq: Neue Nachbarn brauchen verlässliche Wege. Übernimm mit mir eine frische Runde zwischen den Hügeln.', 'Tariq: New neighbours need reliable routes. Join me for a fresh round between the hills.', 'Tariq: Los nuevos vecinos necesitan rutas fiables. Ayúdame con una ronda entre las colinas.', 'Tariq : Les nouveaux voisins ont besoin de routes fiables. Fais une nouvelle tournée entre les collines.', 'Тарик: Новым соседям нужны надёжные пути. Помоги с новой партией между холмами.'),
    'juna': tr('Juna: Die neue Ladenzeile bekommt eigene Farben. Eine frische Versorgungsrunde hilft uns beim gemeinsamen Arbeitstag.', 'Juna: The new shops are gaining their own colours. A fresh supply round helps our shared workday.', 'Juna: Las nuevas tiendas tendrán sus colores. Una ronda de suministros ayuda a nuestro día de trabajo.', 'Juna : Les boutiques prennent leurs couleurs. Une nouvelle tournée nous aide pour notre journée commune.', 'Юна: Новые магазины обретают свои цвета. Свежая партия припасов поможет нашему общему рабочему дню.'),
    'emil': tr('Emil: Heute fotografiere ich wieder denselben Platz. Hilf bei einer frischen Runde, damit lebendiger Alltag auf dem Bild ist.', 'Emil: I am photographing the same square again. Help with a fresh round so the picture shows daily life.', 'Emil: Hoy fotografío otra vez la plaza. Ayuda con una ronda para que la foto muestre vida diaria.', 'Emil : Je photographie à nouveau la place. Une nouvelle tournée fera vivre la scène.', 'Эмиль: Я снова снимаю ту же площадь. Помоги с новой партией, чтобы на снимке была повседневная жизнь.'),
    'safiya': tr('Safiya: Die Nachbarn organisieren das Treffen jetzt selbst. Wir unterstützen sie mit einer frischen Runde.', 'Safiya: The neighbours now organise the meeting themselves. We can support them with a fresh round.', 'Safiya: Los vecinos organizan el encuentro solos. Apoyémoslos con una ronda nueva.', 'Safiya : Les voisins organisent la rencontre eux-mêmes. Soutenons-les avec une nouvelle tournée.', 'Сафия: Соседи сами организуют встречу. Поддержим их новой партией припасов.'),
    'clara': tr('Clara: Meine nächste Runde folgt den Geschichten der Nachbarn. Hilf, die Haltepunkte frisch zu versorgen.', 'Clara: My next walk follows the neighbours’ stories. Help supply its stops with fresh goods.', 'Clara: Mi paseo sigue las historias vecinales. Ayuda a abastecer sus paradas.', 'Clara : Ma promenade suit les histoires des voisins. Aide à ravitailler les haltes.', 'Клара: Моя следующая прогулка пройдёт по историям соседей. Помоги снабдить остановки свежими товарами.'),
    'anton': tr('Anton: Im Pavillon gibt es heute eine neue Wolkenrunde. Hilf bei der Versorgung, bevor die Nachbarn eintreffen.', 'Anton: There is a new cloud-watching meeting in the pavilion today. Help supply it before the neighbours arrive.', 'Anton: Hoy observamos las nubes en el pabellón. Ayuda a abastecerlo antes de que lleguen los vecinos.', 'Anton : Nous observons les nuages au pavillon. Aide à le ravitailler avant l’arrivée des voisins.', 'Антон: Сегодня в павильоне снова наблюдают облака. Помоги с припасами до прихода соседей.'),
}

REPEATS = [
    (5, 20, 1, tr('Eine Runde für die Nachbarn', 'A round for the neighbours', 'Una ronda para los vecinos', 'Une tournée pour les voisins', 'Партия для соседей')),
    (8, 40, 1, tr('Was heute ankommen muss', 'What must arrive today', 'Lo que debe llegar hoy', 'Ce qui doit arriver aujourd’hui', 'Что должно прибыть сегодня')),
    (7, 25, 3, tr('Gebäck für den nächsten Treff', 'Pastries for the next meeting', 'Bollería para el próximo encuentro', 'Des pâtisseries pour la prochaine rencontre', 'Выпечка для следующей встречи')),
]

PROJECTS = [
    dict(id='morning_courtyard', residents=1250, participants=['mila', 'juna', 'safiya'], kind=5, count=80, zone=2,
         title=tr('Der Morgenhof gehört uns', 'The Morning Courtyard is ours', 'El Patio del Alba es nuestro', 'La cour du matin est à nous', 'Утренний двор принадлежит нам'),
         body=tr('Mila öffnet den Lesetreff, Juna näht die Stoffschilder und Safiya verteilt die Aufgaben. Frische Caféverkäufe tragen den ersten gemeinsamen Hoftag.', 'Mila opens the reading group, Juna makes fabric signs and Safiya shares the tasks. Fresh café sales support the first shared courtyard day.', 'Mila abre el grupo, Juna cose carteles y Safiya reparte tareas. Nuevas ventas del café apoyan el primer día compartido.', 'Mila ouvre le cercle, Juna coud les panneaux et Safiya répartit les tâches. Les ventes du café soutiennent la première journée commune.', 'Мила открывает встречи, Юна шьёт вывески, Сафия распределяет задачи. Новые продажи в кафе поддержат первый общий день во дворе.'),
         memory=tr('Der gemeinsam belebte Morgenhof', 'The Morning Courtyard brought to life together', 'El Patio del Alba que todos animaron', 'La cour du matin animée ensemble', 'Совместно оживлённый Утренний двор')),
    dict(id='connection_terrace', residents=3000, participants=['tariq', 'clara', 'safiya', 'emil'], kind=8, count=180, zone=2,
         title=tr('Die Verbindungsterrasse', 'The Connection Terrace', 'La Terraza de Conexión', 'La terrasse de liaison', 'Соединительная терраса'),
         body=tr('Tariq öffnet den Stützpunkt, Clara ergänzt Wegweiser, Emil zeigt die Nachbarn und Safiya lädt beide Stadtseiten ein. Frische Waren verbinden die Viertel.', 'Tariq opens the base, Clara adds signs, Emil shows the neighbours and Safiya invites both sides of town. Fresh goods connect the quarters.', 'Tariq abre la base, Clara añade señales, Emil muestra a los vecinos y Safiya invita a ambos lados. Los productos unen los barrios.', 'Tariq ouvre le relais, Clara ajoute des panneaux, Emil montre les voisins et Safiya invite les deux quartiers. Les produits les relient.', 'Тарик открывает пункт, Клара ставит указатели, Эмиль показывает соседей, Сафия зовёт обе стороны города. Свежие товары соединяют кварталы.'),
         memory=tr('Der Treffpunkt zwischen oberen und unteren Hügeln', 'The meeting place between upper and lower hills', 'El encuentro entre colinas altas y bajas', 'Le lieu de rencontre entre le haut et le bas', 'Место встреч верхних и нижних холмов')),
    dict(id='panorama_ring', residents=5000, participants=['mila', 'tariq', 'juna', 'emil', 'safiya', 'clara', 'anton'], kind=9, count=20, zone=3,
         title=tr('Der Panoramaring', 'The Panorama Ring', 'El Anillo Panorámico', 'L’anneau panoramique', 'Панорамное кольцо'),
         body=tr('Alle sieben Bewohner verbinden Höfe, Wege und Aussichtspunkte. Neue abgeschlossene Lieferungen tragen den ersten gemeinsamen Rundgang. Persönliche Erinnerungen werden Teil des Weges.', 'All seven residents connect courtyards, paths and viewpoints. New completed deliveries support the first shared walk. Personal memories become part of the route.', 'Los siete vecinos unen patios, caminos y miradores. Entregas nuevas sostienen el primer paseo. Los recuerdos forman parte del recorrido.', 'Les sept habitants relient cours, chemins et belvédères. De nouvelles livraisons portent la première promenade. Les souvenirs jalonnent le parcours.', 'Семеро жителей соединяют дворы, дорожки и площадки. Новые доставки поддержат первую общую прогулку. Личные воспоминания станут частью пути.'),
         memory=tr('Sieben Hügel, ein Zuhause', 'Seven hills, one home', 'Siete colinas, un hogar', 'Sept collines, un foyer', 'Семь холмов, один дом')),
]

COUNTER_KEYS = {3: 'raw', 4: 'refined', 5: 'coffee', 6: 'butter', 7: 'pastry', 8: 'delivered', 9: 'deliveries'}


def digest(value):
    return hashlib.sha256(json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode('utf-8')).hexdigest()


def author():
    data = json.loads(PATH.read_text(encoding='utf-8'))
    catalogs = {lang: json.loads((LOCALES / (lang + '.json')).read_text(encoding='utf-8')) for lang in LANGUAGES}
    before = copy.deepcopy(data)
    before_catalogs = copy.deepcopy(catalogs)
    added_texts = dict(TEXT)

    def text(key, value):
        if key in added_texts and added_texts[key] != value:
            raise ValueError('Conflicting Seven Hills translation: ' + key)
        added_texts[key] = value
        return key

    source = [i for i in data['instances'] if i.get('cityId', 'burning_hills') == 'burning_hills']
    ids = {i['id']: 'sh_' + i['id'] for i in source}
    instances = [dict(copy.deepcopy(i), id=ids[i['id']], cityId=CITY, introducedVersion=VERSION) for i in source]
    routes = [dict(copy.deepcopy(r), id='sh_' + r['id'], **{'from': ids[r['from']], 'to': ids[r['to']]})
              for r in data['routes'] if r['from'] in ids and r['to'] in ids]
    quests = []
    for row in MAIN:
        q = copy.deepcopy(row)
        q.update(id='sh_' + row['id'], cityId=CITY, focus=ids[row['focus']],
                 titleKey=text('sh.quest.' + row['id'] + '.title', q.pop('title')),
                 bodyKey=text('sh.quest.' + row['id'] + '.body', q.pop('body')))
        # The existing conversation presenter looks up these explicit spoken variants.
        text(q['bodyKey'] + '.dialogue', {lang: value.split(': ', 1)[-1] for lang, value in row['body'].items()})
        text(q['bodyKey'] + '.dialogue_ready', tr(
            'Alles bereit. Hole die Belohnung dieses Auftrags ab; danach gehen wir gemeinsam weiter.',
            'Everything is ready. Claim this task’s reward; then we will continue together.',
            'Todo está listo. Recoge la recompensa de este encargo; después seguiremos juntos.',
            'Tout est prêt. Récupère la récompense de cette mission ; nous poursuivrons ensemble.',
            'Всё готово. Забери награду за это задание, и мы продолжим вместе.'))
        for c in q['conditions']:
            if c['target'] in ids:
                c['target'] = ids[c['target']]
        quests.append(q)

    def goal(kind, count, target=''):
        return dict(kind=kind, count=count, target=target, textKey='sh.goal.' + ('level' if kind == 0 else COUNTER_KEYS[kind]))

    stories = []
    for row in PEOPLE:
        pid = 'sh_' + row['id']
        base = 'sh.story.' + row['id']
        p = dict(id=pid, cityId=CITY, residents=row['residents'], focus=ids[row['focus']], repeatDelaySeconds=300,
                 nameKey=text(base + '.name', tr(*([row['name']] * 5))), roleKey=text(base + '.role', row['role']),
                 biographyKey=text(base + '.biography', row['bio']), greetingKey=text(base + '.greeting', row['greeting']),
                 chapters=[], recurring=[])
        completed = text(base + '.completed', {
            lang: row['name'].split()[0] + ': ' + words for lang, words in tr(
                'Das haben wir gemeinsam geschafft. Dieser Ort bleibt Teil unserer Geschichte.',
                'We did this together. This place stays part of our story.',
                'Lo logramos juntos. Este lugar sigue siendo parte de nuestra historia.',
                'Nous l’avons fait ensemble. Ce lieu reste dans notre histoire.',
                'Мы сделали это вместе. Это место остаётся частью нашей истории.').items()})
        for index, chapter_row in enumerate(row['chapters']):
            key = base + '.chapter.' + str(index + 1)
            choices = [dict(id='begin', textKey='sh.choice.help', outcomeKey='sh.outcome.help')]
            if index == 2:
                values = ('quiet', 'lively') if row['id'] == 'safiya' else ('share', 'meet')
                choices = [dict(id=value, textKey='sh.choice.' + value, outcomeKey='sh.outcome.' + value) for value in values]
            zone = 3 if chapter_row['family'] in ('butter_factory', 'supermarket', 'bakery') else 2 if chapter_row['family'] in ('park', 'residential_house') else 0 if chapter_row['family'] in ('farm_warehouse', 'farm_processing') else 1
            p['chapters'].append(dict(id=pid + '_chapter_' + str(index + 1),
                titleKey=text(key + '.title', chapter_row['title']), bodyKey=text(key + '.body', chapter_row['body']),
                memoryKey=text(key + '.memory', chapter_row['memory']), completedKey=completed,
                residents=row['thresholds'][index], moneyReward=200 + index * 200, zone=zone,
                goals=[goal(0, chapter_row['level'], chapter_row['family']), goal(chapter_row['counter'], chapter_row['count'])],
                choices=choices, prerequisites=[], itemRewards=[]))
        repeat_body = text(base + '.repeat.body', REPEAT_CONTEXT[row['id']])
        for index, (kind, count, zone, label) in enumerate(REPEATS):
            key = base + '.repeat.' + str(index + 1)
            title = {lang: row['name'].split()[0] + ': ' + label[lang] for lang in LANGUAGES}
            p['recurring'].append(dict(id=pid + '_repeat_' + str(index + 1), titleKey=text(key + '.title', title),
                bodyKey=repeat_body, completedKey='sh.repeat.completed', memoryKey='', residents=row['residents'],
                moneyReward=150, zone=zone, goals=[goal(kind, count)],
                choices=[dict(id='help', textKey='sh.choice.help', outcomeKey='sh.outcome.help')], prerequisites=[], itemRewards=[]))
        stories.append(p)

    projects = []
    for row in PROJECTS:
        pid = 'sh_joint_' + row['id']
        key = 'sh.joint.' + row['id']
        participants = ['sh_' + id for id in row['participants']]
        quest = dict(id=pid, titleKey=text(key + '.title', row['title']), bodyKey=text(key + '.body', row['body']),
            memoryKey=text(key + '.memory', row['memory']), completedKey='sh.joint.completed', residents=row['residents'],
            moneyReward=1200, zone=row['zone'], goals=[goal(row['kind'], row['count'])],
            choices=[dict(id='together', textKey='sh.choice.help', outcomeKey='sh.outcome.help')],
            prerequisites=[id + '_chapter_1' for id in participants], itemRewards=[])
        projects.append(dict(id=pid, cityId=CITY, host=participants[0], participants=participants, quest=quest))

    city = dict(id=CITY, nameKey='city.seven_hills', scenePath='Assets/Weedtopia/Game/Scenes/SevenHills.unity',
        sourceCity='', unlockSources=['burning_hills', 'big_rock_city'], introducedVersion=VERSION,
        unlockResidents=5000, bridgeMoney=2500, bridgeCards=2,
        invitationTitleKey='campaign.seven_hills_title', invitationBodyKey='campaign.seven_hills_body')

    def append_unique(collection, proposed, identity):
        existing = {entry[identity]: entry for entry in collection}
        for entry in proposed:
            key = entry[identity]
            if key in existing:
                if existing[key] != entry:
                    raise ValueError('Refusing to overwrite existing content: ' + key)
            else:
                collection.append(entry)
                existing[key] = entry

    append_unique(data['cities'], [city], 'id')
    append_unique(data['instances'], instances, 'id')
    append_unique(data['routes'], routes, 'id')
    append_unique(data['quests'], quests, 'id')
    append_unique(data['vipStories'], stories, 'id')
    append_unique(data['vipCollaborations'], projects, 'id')
    append_unique(data['texts'], [dict(key=key, de=value['de']) for key, value in added_texts.items()], 'key')
    data['version'] = max(data['version'], VERSION)
    for lang in LANGUAGES:
        append_unique(catalogs[lang]['entries'], [dict(key=key, value=value[lang]) for key, value in added_texts.items()], 'key')
    validate(data, catalogs)
    # Assert preservation before the first productive write, including entry order.
    for collection in ('cities', 'instances', 'routes', 'quests', 'vipStories', 'vipCollaborations', 'texts'):
        assert data[collection][:len(before[collection])] == before[collection], collection
    for key in before.keys() - {'version', 'cities', 'instances', 'routes', 'quests', 'vipStories', 'vipCollaborations', 'texts'}:
        assert data[key] == before[key], key
    for lang in LANGUAGES:
        assert catalogs[lang]['entries'][:len(before_catalogs[lang]['entries'])] == before_catalogs[lang]['entries'], lang
    changed = data != before or catalogs != before_catalogs
    if data != before:
        PATH.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    for lang in LANGUAGES:
        if catalogs[lang] != before_catalogs[lang]:
            (LOCALES / (lang + '.json')).write_text(json.dumps(catalogs[lang], ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    receipt = dict(city=CITY, version=data['version'], changed=changed, instances=len(instances), routes=len(routes),
        mainQuests=len(quests), permanentResidents=len(stories), personalChapters=sum(len(p['chapters']) for p in stories),
        recurringVariants=sum(len(p['recurring']) for p in stories), communityProjects=len(projects),
        localizedKeys=len(added_texts), languages=list(LANGUAGES), oldContentDigest=digest(before), contentDigest=digest(data))
    print(json.dumps(receipt, ensure_ascii=False, indent=2))
    return receipt


def validate(data, catalogs):
    city = next(c for c in data['cities'] if c['id'] == CITY)
    assert city['unlockSources'] == ['burning_hills', 'big_rock_city'] and city['unlockResidents'] == 5000
    assert city['bridgeMoney'] == 2500 and city['bridgeCards'] == 2
    original = [i for i in data['instances'] if i.get('cityId', 'burning_hills') == 'burning_hills']
    instances = [i for i in data['instances'] if i.get('cityId') == CITY]
    instance_ids = {i['id'] for i in instances}
    assert len(instances) == 56 and sorted(i['family'] for i in instances) == sorted(i['family'] for i in original)
    routes = [r for r in data['routes'] if r['from'] in instance_ids]
    assert len(routes) == 35 and all(r['to'] in instance_ids and r['from'] != r['to'] for r in routes)
    quests = [q for q in data['quests'] if q.get('cityId') == CITY]
    assert len(quests) == 18 and all(q['focus'] in instance_ids for q in quests)
    people = [p for p in data['vipStories'] if p['cityId'] == CITY]
    assert [p['id'] for p in people] == ['sh_' + p['id'] for p in PEOPLE]
    assert [p['residents'] for p in people] == [0, 50, 150, 300, 500, 800, 1200]
    assert all(len(p['chapters']) == 4 and len(p['recurring']) == 3 for p in people)
    projects = [p for p in data['vipCollaborations'] if p['cityId'] == CITY]
    assert len(projects) == 3 and len(projects[-1]['participants']) == 7
    all_story = [q for p in people for q in p['chapters'] + p['recurring']] + [p['quest'] for p in projects]
    assert all(any(g['kind'] >= 2 for g in q['goals']) for q in all_story), 'A story needs fresh activity after acceptance'
    for p in people:
        thresholds = [q['residents'] for q in p['chapters']]
        assert thresholds == sorted(set(thresholds)) and thresholds[0] >= p['residents']
        assert p['focus'] in instance_ids
    for collection, key in (('cities', 'id'), ('instances', 'id'), ('routes', 'id'), ('quests', 'id'), ('vipStories', 'id'), ('vipCollaborations', 'id'), ('texts', 'key')):
        values = [v[key] for v in data[collection]]
        assert len(values) == len(set(values)), collection + ' has duplicate identities'
    texts = {t['key']: t['de'] for t in data['texts']}
    localized = {lang: {entry['key']: entry['value'] for entry in catalogs[lang]['entries']} for lang in LANGUAGES}
    def refs(value):
        if isinstance(value, dict):
            for key, item in value.items():
                if key.endswith('Key') and item:
                    yield item
                else:
                    yield from refs(item)
        elif isinstance(value, list):
            for item in value:
                yield from refs(item)
    referenced = set(refs([city, quests, people, projects])) | set(TEXT)
    referenced.update(t['key'] for t in data['texts'] if t['key'].startswith('sh.'))
    for key in referenced:
        assert key in texts and texts[key].strip(), key
        tokens = sorted(re.findall(r'\{\d+(?:,[^}:]+)?(?::[^}]+)?\}', texts[key]))
        for lang in LANGUAGES:
            assert key in localized[lang] and localized[lang][key].strip(), lang + ':' + key
            assert sorted(re.findall(r'\{\d+(?:,[^}:]+)?(?::[^}]+)?\}', localized[lang][key])) == tokens, lang + ':' + key
    return dict(valid=True, city=CITY, mainQuests=len(quests), personalChapters=28, recurringVariants=21,
                communityProjects=len(projects), referencedKeys=len(referenced), locales=len(LANGUAGES))


if __name__ == '__main__':
    author()

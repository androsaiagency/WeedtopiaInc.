"""Add separate NPC speech and Weedtopia menu copy without changing quest/save identities."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FILE = ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json"
content = json.loads(FILE.read_text(encoding="utf-8-sig"))
texts = {row["key"]: row for row in content["texts"]}

def put(key, value):
    if key in texts:
        texts[key]["de"] = value
    else:
        row = {"key": key, "de": value}
        content["texts"].append(row)
        texts[key] = row

for key, value in {
    "game.title": "WEEDTOPIA", "ui.start": "Play", "ui.reset": "Neues Spiel", "building.coffee_shop": "Coffeeshop",
    "ui.pause": "Einstellungen", "intro.name": "Hey, willkommen in Weedtopia! Ich bin Rocko. Wie darf ich dich nennen?",
    "intro.0": "Schön, dass du da bist, {0}. Der THC-Krieg ist vorbei. Jetzt geben wir unserer Stadt gemeinsam einen neuen Anfang.",
    "intro.1": "Früher war ich bei der Mafia. Heute bin ich Bürgermeister von Burning Hills. Ich kenne die Leute hier – und du bringst frische Ideen mit.",
    "intro.2": "Für unseren Start habe ich 1.000 Geld und 50 Grinder organisiert. Mila wartet auf der Farm. Sie zeigt dir die erste Ernte; Jaro und Nia helfen uns danach weiter.",
}.items():
    put(key, value)

speech = {
    "farm_welcome": "Hey! Ich bin Mila. Schön, dass du mit anpackst. Hier wachsen die ersten Pflanzen für unseren Neuanfang. Ich zeige dir gern, wie wir sie pflegen.",
    "first_crop": "Dieser Platz hat Potenzial. Wenn wir ihn wieder herrichten, kann hier bald die erste Ernte wachsen. Ich habe schon die Setzlinge bereit.",
    "manual_harvest": "Die Blüten sind reif! Ich liebe diesen Moment. Holen wir sie rein, dann haben die nächsten Pflanzen wieder Platz.",
    "grow_manager": "Ich bleibe hier und kümmere mich um den Anbau. Wenn du mich als Leitung einsetzt, halte ich die Pflanzen in Schuss, während du dich ums Dorf kümmerst.",
    "processing": "Jaro ist der Name. Diese Maschinen haben schon bessere Tage gesehen, aber wir bekommen sie wieder hin. Aus Milas Ernte machen wir hier veredelte Blüten.",
    "processing_manager": "Die Werkstatt läuft wieder! Ich übernehme die Verarbeitung. Regelmäßige Ernte und ein freier Puffer sind alles, was ich für einen guten Arbeitstag brauche.",
    "farm_storage": "Ich bin Nia. Wenn Waren pünktlich ankommen, merkt man kaum, wie viel Planung dahintersteckt. Unser Lager wird die Farm mit der Stadt verbinden.",
    "farm_delivery": "Unser Team steht bereit. Ich organisiere das Lager, die Fahrer bringen die Ware weiter. Sobald die Farm läuft, wird aus den einzelnen Werkstätten eine richtige Kooperative.",
    "city_rebuild": "In der Stadt ist lange nichts angekommen. Ein verlässliches Lager und unser erster Coffeeshop bringen wieder Leben in die Straßen.",
    "city_sales": "Hörst du das? Die Leute kommen wieder ins Viertel. Wenn das Lager liefert und der Laden besetzt ist, können wir ihnen endlich etwas anbieten.",
    "townhall_spin": "Wir brauchen mehr helfende Hände. Im Team kannst du mit einer Worker-Karte jemanden kennenlernen. Ich kümmere mich darum, dass jeder einen passenden Platz findet.",
    "settlement_build": "Eine Stadt ist mehr als ihre Werkstätten. Die Menschen brauchen ein Zuhause und einen grünen Platz zum Durchatmen. Darauf freue ich mich besonders.",
    "settlement_daily": "Unser Viertel wird lebendig. Die Nachbarn unterstützen uns mit Tagesboni, und im Park entstehen neue Kontakte. Lass uns gut für sie sorgen.",
    "industry_build": "Mit Butterfabrik, Supermarkt und Bäckerei können wir die nächste große Lücke schließen. Der Duft von frischem Gebäck wird die ganze Stadt anlocken.",
    "industry_sell": "Jetzt greift alles ineinander: Die Fabrik liefert Butter, der Bäcker holt sie ab und macht etwas Gutes daraus. Genau so habe ich mir unsere Stadt vorgestellt.",
    "freeplay": "Du hast aus den ersten Werkstätten eine funktionierende Stadt gemacht. Ich bin stolz auf unser Team. Bald kannst du deinen eigenen Weg wählen.",
}
ready_speech = {
    "farm_welcome": "Schön, dass wir uns kennengelernt haben. Ich glaube, wir werden ein gutes Team. Unser Neuanfang kann losgehen!",
    "first_crop": "Schau, unser erster Anbauplatz lebt wieder! Die Setzlinge haben ein Zuhause. Ich freue mich schon auf die erste Ernte.",
    "manual_harvest": "Die erste Ernte ist sicher im Lager. Sauber gemacht! Du hast ein gutes Händchen für die Pflanzen.",
    "grow_manager": "Ich habe hier übernommen. Die Pflanzen sind bei mir in guten Händen, und du kannst dich auf mich verlassen.",
    "processing": "Die Maschinen laufen wieder! Ich wusste, dass wir diese Werkstatt zusammen auf Vordermann bringen können.",
    "processing_manager": "Alles eingerichtet. Ich halte die Verarbeitung am Laufen – unser Team bekommt verlässlich gute Ware.",
    "farm_storage": "Das Lager ist bereit. Jetzt haben die Waren einen sicheren Platz und unser Team eine gemeinsame Drehscheibe.",
    "farm_delivery": "Die erste Lieferung ist angekommen! Ich bin stolz auf die Fahrer. So wird aus unserer Farm eine Kooperative.",
    "city_rebuild": "Der erste Treffpunkt steht! Endlich haben die Leute im Viertel wieder einen Ort, an dem sie zusammenkommen können.",
    "city_sales": "Unsere ersten Gäste sind versorgt. Es tut gut, die Straßen wieder so lebendig zu sehen.",
    "townhall_spin": "Unser Team wächst. Neue Gesichter, neue Ideen – genau das braucht die Stadt jetzt.",
    "settlement_build": "Die ersten neuen Zuhause und der Park sind da. Ich kann mir schon vorstellen, wie die Nachbarn hier zusammenleben.",
    "settlement_daily": "Die Nachbarn helfen uns, und wir helfen ihnen. Das fühlt sich endlich wieder nach einer Gemeinschaft an.",
    "industry_build": "Die Werkstätten stehen. Die Bäckerei allein duftet schon nach einem guten Morgen für unsere Stadt.",
    "industry_sell": "Unser Gebäck kommt bei den Leuten an! Vom Anbau bis zur Theke trägt jetzt jeder seinen Teil bei.",
    "freeplay": "Wir haben es geschafft. Die Stadt steht wieder auf eigenen Beinen. Danke, dass du an unser Team geglaubt hast.",
}
for quest in content["quests"]:
    identity = quest["id"].removeprefix("br_")
    put(quest["bodyKey"] + ".dialogue", speech[identity])
    put(quest["bodyKey"] + ".dialogue_ready", ready_speech[identity])

for key, value in {
    "project.goal.0.coffee_02": "Coffeeshop 2 auf Stufe 5 ausbauen",
    "project.goal.1.coffee_02": "Coffeeshop 2 mit einer Leitung besetzen",
    "project.neighborhood.venue.body": "Unser Treffpunkt ist Coffeeshop 2: Stufe 5 und eine eingesetzte Leitung. Dazu zählen 360 verkaufte Blüten seit Beginn dieses Abschnitts.",
    "project.goal.0.bakery_02": "Bäckerei 2 auf Stufe 5 ausbauen",
    "project.goal.1.bakery_02": "Bäckerei 2 mit einer Leitung besetzen",
}.items():
    put(key, value)

FILE.write_text(json.dumps(content, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print("Separate dialogue copy authored for", len(content["quests"]), "quests.")

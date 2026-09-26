"""Presentation-only localization. Gameplay definitions and IDs remain untouched."""
import json
from pathlib import Path
p=Path(__file__).resolve().parents[2]/'Assets/Weedtopia/Game/Content/BurningHills.json'
d=json.loads(p.read_text(encoding='utf-8-sig'))
copy={
 'dialog.rocko':'Sieh dich um: In jeder reparierten Werkstatt steckt ein neuer Anfang. Ich behalte im Rathaus den Überblick – du gibst Burning Hills seinen Rhythmus zurück.',
 'dialog.mila':'Die nächste Ernte wächst schon. Hol die Blüten rechtzeitig ab, dann bleibt Platz für frische Pflanzen. Mit einem Manager läuft der Anbau auch weiter, während du die Stadt erkundest.',
 'dialog.jaro':'Ein leeres Förderband hilft niemandem. Ich veredle die Ernte, solange im Zielpuffer Platz ist. Steht hier alles still, schau beim Lager und seinen Fahrern nach.',
 'dialog.nia':'Gute Logistik ist Teamarbeit. Ich halte das Lager am Laufen; unsere Fahrer bringen die Ware in die Stadt. Ein freier Fahrerplatz ist eine neue Verbindung für Burning Hills.'
}
for key,value in copy.items():
    entry=next((x for x in d['texts'] if x['key']==key),None)
    if entry: entry['de']=value
    else: d['texts'].append({'key':key,'de':value})
p.write_text(json.dumps(d,ensure_ascii=True,indent=2)+'\n',encoding='utf-8')

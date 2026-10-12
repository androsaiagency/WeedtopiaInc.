"""Export documentation from the authoritative game definitions without editing content."""
import datetime
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
data = json.loads((ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json').read_text(encoding='utf-8-sig'))
texts = {entry['key']: entry['de'] for entry in data['texts']}
conditions = ['Talk', 'Level', 'Harvest', 'Assigned', 'Delivered', 'Sold', 'Spin', 'TownHall',
              'Residents', 'Daily', 'Butter', 'PastrySold', 'Automated', 'VipCompleted']
zones = ['Farm', 'Stadt', 'Siedlung', 'Industrie']
lines = ['# Hauptquestfluss – Burning Hills', '',
         f'Direkt aus `Assets/Weedtopia/Game/Content/BurningHills.json` exportiert. Inhaltsversion {data["version"]}.', '',
         'Alle Bedingungen einer Quest müssen erfüllt sein. Danach wird die Belohnung ausdrücklich abgeholt. '
         'Die persistierte Reihenfolge und abgeschlossene Quest-ID verhindern doppelte Belohnungen. '
         'Farm, Stadt, Siedlung und Industrie werden durch die unten benannten Rewards freigeschaltet.', '',
         'Automated bezieht sich auf eine konkrete Kerninstanz. Zusätzliche optionale Gebäude erhöhen die '
         'Tutorialanforderung nicht. Assigned prüft kompatible Zuweisungen auf Instanz oder Gebäudefamilie. '
         'Harvest in Hauptquests zählt manuelle Ernteaktionen. VIP verwendet dagegen tatsächlich erzeugte Rohblüten '
         'beziehungsweise abgelieferte Mengen. Das sind bewusst unterschiedliche Fortschrittszähler.', '']
for index, quest in enumerate(data['quests'], 1):
    lines += [f"## {index}. {texts[quest['titleKey']]}", '',
              f"ID: `{quest['id']}`. Sprecher: {quest['speaker']}. Fokus: `{quest['focus']}`.", '',
              texts[quest['bodyKey']], '',
              '| Bedingung | Ziel | Mindestwert |', '| --- | --- | --- |']
    for condition in quest['conditions']:
        lines.append(f"| {conditions[condition['kind']]} | {condition['target'] or 'Stadtstatistik'} | {condition['count']} |")
    roles = ', '.join(texts['role.' + role] for role in quest['workerRewards']) or 'keine'
    lines += ['', f"Belohnung: {quest['moneyReward']} Geld, {quest['cardsReward']} Karten. Garantierte Worker: {roles}. "
                  f"Zone mindestens: {zones[quest['unlock']] }.", '']
lines += ['## Praktischer Bedienweg', '',
          '„Zum Questziel“ fokussiert die Kerninstanz. „Bewohner ansprechen“ erfüllt den passenden Dialog. '
          'Bauen/Ernte/Worker befinden sich im Gebäudepanel. Im Workerfenster eine Karte und den Manager- oder Fahrerplatz '
          'wählen und „Zuweisen“ drücken. Der Fahrer des Farm-Lagers gehört in Fahrerplatz 1, der Lager-Manager in Managerplatz 0. '
          'Analog funktioniert das Stadt-Lager. Tagesbelohnungen werden im Rathaus abgeholt; VIP-Belohnungen im Auftragsjournal.', '',
          'Nach dem letzten Abholen ist Freeplay aktiv. Kein zusätzlicher Grinderkauf ist notwendig. '
          'Der automatisierte Referenzpfad und ein Pfad mit früh errichteten optionalen Gebäuden bestehen aktuell. '
          'Die gemessene Simulationszeit steht im Validierungsbericht und ersetzt keine menschliche Spielzeitmessung.', '']
output = ROOT / 'Docs/Content/MAIN_QUEST_FLOW.md'
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text('\n'.join(lines), encoding='utf-8')
print(f'Exported {len(data["quests"])} quests to {output}')

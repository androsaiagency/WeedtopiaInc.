"""Add the authored wheel pool and German copy; preserve all existing content values."""
import json
from pathlib import Path

path = Path('Assets/Weedtopia/Game/Content/BurningHills.json')
data = json.loads(path.read_text(encoding='utf-8-sig'))
data['wheelRewards'] = [dict(id=i['id'], count=3 if i['id'] == 'build_1m' else 2 if i['id'] == 'build_5m' else 1) for i in data['items']]
copy = {
    'wheel.title': 'Glücksrad',
    'wheel.subtitle': 'Ein kleiner Dreh. Große Möglichkeiten.',
    'wheel.entry': 'Glücksrad',
    'wheel.entry_free': 'Glücksrad · Gratis',
    'wheel.reward_title': 'Dein Glücksrad-Gewinn',
    'reward.source.luckywheel': 'Glücksrad',
    'wheel.countdown': 'Neue Gewinne in {0}',
    'wheel.reset_hint': 'Täglich um 00:00 UTC · Gratis-Dreh inklusive',
    'wheel.remaining': '{0} von 10 Gewinnen übrig',
    'wheel.chance': 'Jedes Feld: {0} % Gewinnchance',
    'wheel.free': 'Kostenlos drehen',
    'wheel.paid': 'Drehen · {0} Grinder',
    'wheel.confirm': 'Für {0} Grinder drehen?',
    'wheel.confirm_button': '{0} Grinder einsetzen',
    'wheel.cancel': 'Abbrechen',
    'wheel.spinning': 'Dein Glück ist unterwegs …',
    'wheel.empty': 'Alle Gewinne eingesammelt!',
    'wheel.empty_hint': 'Morgen warten zehn neue Items auf dich.',
    'wheel.balance': 'Dein Guthaben: {0} Grinder',
    'wheel.prize_hint': 'Klicke auf ein Item, um es anzuschauen.',
    'wheel.costs': 'DREHKOSTEN',
    'wheel.costs_body': 'Gratis → 50 → 100 → 200 → 500\n→ 1.000 → 2.000 → 2.000 → 2.000 → 2.000',
    'wheel.inventory': 'Inventar öffnen',
    'wheel.details': '{0} · {1}\n\n{2}',
    'wheel.won': 'GEWONNEN!',
    'wheel.secured': '×{0} {1}\nDein Gewinn liegt im Inventar.',
    'wheel.insufficient': 'Benötigt: {0} Grinder',
    'wheel.short.specialist_contract': 'Vertrag',
    'wheel.short.team_pack': 'Kartenpaket',
    'wheel.short.grinder_pouch': 'Grinder',
}
texts = {t['key']: t for t in data['texts']}
for key, value in copy.items():
    if key in texts:
        texts[key]['de'] = value
        texts[key].pop('value', None)
    else:
        data['texts'].append(dict(key=key, de=value))
path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

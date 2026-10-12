from pathlib import Path
import json
root=Path(__file__).resolve().parents[2]
p=root/'Assets/Weedtopia/Game/Content/BurningHills.json'
data=json.loads(p.read_text(encoding='utf-8-sig'))
texts={
 'headline':'DAS HAST DU DIR VERDIENT!', 'welcome':'VERSTÄRKUNG FÜR DEIN TEAM!',
 'continue':'Weiter', 'open_collection':'Sammlung ansehen', 'collection':'Sammlung',
 'secured':'Bereits gutgeschrieben · bleibt auch nach dem Schließen erhalten',
 'preview':'DAS ERWARTET DICH', 'preview_empty':'Wähle ein Stadtprojekt und entdecke neue Belohnungen.',
 'preview_no_items':'Lerne deine Stadt kennen. Die ersten Gewinne warten in den nächsten Aufträgen.',
 'inventory':'DEINE SAMMLUNG', 'achievements':'DEINE AUSZEICHNUNGEN', 'history':'DEINE BELOHNUNGEN',
 'tab.inventory':'Bestand', 'tab.achievements':'Erfolge', 'tab.history':'Verlauf',
 'inventory_hint':'Deine verfügbaren Währungen und dein Team. Mitarbeiterkarten öffnen die Teamverwaltung.',
 'achievements_hint':'Deine Meilensteine bleiben erhalten. Freigeschaltete Verkaufsboni wirken automatisch.',
 'history_hint':'Zuletzt erhalten · Verlauf der letzten 100 Belohnungen, unabhängig vom aktuellen Bestand.',
 'no_achievements':'Dein erster Meilenstein wartet auf dich!\nSchließe die Anfangsquests ab und verdiene die Gründungsurkunde.',
 'no_history':'Deine nächsten Belohnungen erscheinen hier.\nBei älteren Spielständen beginnt der Verlauf ab diesem Update.',
 'kind.Money':'Geld', 'kind.Grinders':'Grinder', 'kind.Cards':'Mitarbeiterkarten', 'kind.Worker':'Neues Teammitglied',
 'kind.VipOrders':'VIP-Aufträge', 'title.founder':'Gründungsurkunde', 'title.festival':'Feststadt',
 'permanent':'Dauerhafte Auszeichnung', 'sales_bonus':'Dauerhafter Verkaufsbonus', 'unlocked':'Gebiet freigeschaltet',
 'available':'Aktuell verfügbar', 'credited':'Gutgeschrieben', 'assigned':'Im Einsatz', 'orders_hint':'Jetzt im Questbuch',
 'source.quest':'Quest abgeschlossen', 'source.cityproject':'Stadtprojekt abgeschlossen', 'source.vip':'VIP-Auftrag erfüllt',
 'source.daily':'Tagesbelohnungen', 'source.recruitment':'Teamverstärkung',
 'role.processing':'Verarbeitungs-\nmanager', 'role.coffee':'Coffee-Shop-\nManager',
 'role.processing.compact':'Verarbeitungs-\nmanager', 'role.coffee.compact':'Coffee-Shop\nManager',
 'role.grow.compact':'Anbau-\nManager', 'role.farm_manager.compact':'Farm-Lager\nManager',
 'role.farm_driver.compact':'Farm-Fahrer', 'role.city_manager.compact':'Stadt-Lager\nManager',
 'role.city_driver.compact':'Stadt-\nLieferfahrer', 'role.caretaker.compact':'Haus-\nverwaltung',
 'role.ranger.compact':'Park-\nbetreuung', 'role.butter.compact':'Butter-\nproduktion',
 'role.market.compact':'Supermarkt', 'role.baker.compact':'Bäckerei',
 'title.founder.compact':'Gründungs-\nurkunde', 'kind.Cards.compact':'Teamkarten',
}
data['texts']=[t for t in data['texts'] if not t['key'].startswith('reward.')]+[{'key':'reward.'+k,'de':v} for k,v in texts.items()]
p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Authored',len(texts),'reward text keys; all other content preserved.')

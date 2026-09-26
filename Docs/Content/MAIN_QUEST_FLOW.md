# Hauptquestfluss – Burning Hills

Direkt aus `Assets/Weedtopia/Game/Content/BurningHills.json` exportiert. Inhaltsversion 1.

Alle Bedingungen einer Quest müssen erfüllt sein. Danach wird die Belohnung ausdrücklich abgeholt. Die persistierte Reihenfolge und abgeschlossene Quest-ID verhindern doppelte Belohnungen. Farm, Stadt, Siedlung und Industrie werden durch die unten benannten Rewards freigeschaltet.

Automated bezieht sich auf eine konkrete Kerninstanz. Zusätzliche optionale Gebäude erhöhen die Tutorialanforderung nicht. Assigned prüft kompatible Zuweisungen auf Instanz oder Gebäudefamilie. Harvest in Hauptquests zählt manuelle Ernteaktionen. VIP verwendet dagegen tatsächlich erzeugte Rohblüten beziehungsweise abgelieferte Mengen. Das sind bewusst unterschiedliche Fortschrittszähler.

## 1. Mila wartet am Anbauplatz

ID: `farm_welcome`. Sprecher: Mila. Fokus: `farm_grow_01`.

Sprich mit Mila am ersten Anbauplatz. Wir fangen klein an.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Talk | mila | 1 |

Belohnung: 0 Geld, 0 Karten. Garantierte Worker: keine. Zone mindestens: Farm.

## 2. Ein Topf voller Hoffnung

ID: `first_crop`. Sprecher: Mila. Fokus: `farm_grow_01`.

Baue den ersten Anbauplatz auf Level 1 aus. Geld bezahlt den Ausbau, danach läuft die Bauzeit.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Level | farm_grow_01 | 1 |

Belohnung: 150 Geld, 0 Karten. Garantierte Worker: keine. Zone mindestens: Farm.

## 3. Handarbeit

ID: `manual_harvest`. Sprecher: Mila. Fokus: `farm_grow_01`.

Warte auf die reife Ernte und sammle sie von Hand ein. Ohne Manager bleibt sie am Anbauplatz.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Harvest | Stadtstatistik | 1 |

Belohnung: 150 Geld, 0 Karten. Garantierte Worker: Anbau-Manager. Zone mindestens: Farm.

## 4. Mila übernimmt

ID: `grow_manager`. Sprecher: Mila. Fokus: `farm_grow_01`.

Weise den erhaltenen Anbau-Manager dem ersten Anbauplatz zu.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Assigned | farm_grow_01 | 1 |

Belohnung: 150 Geld, 0 Karten. Garantierte Worker: keine. Zone mindestens: Farm.

## 5. Jaro veredelt

ID: `processing`. Sprecher: Jaro. Fokus: `farm_processing`.

Sprich mit Jaro. Baue die Farm-Veredelung auf. Ein kleiner Durchlauf staut die Ernte auf dem Band.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Talk | jaro | 1 |
| Level | farm_processing | 1 |

Belohnung: 150 Geld, 0 Karten. Garantierte Worker: Verarbeitungsmanager. Zone mindestens: Farm.

## 6. Das Band läuft

ID: `processing_manager`. Sprecher: Jaro. Fokus: `farm_processing`.

Weise den Verarbeitungsmanager zu. Puffer behalten Material, auch wenn der nächste Schritt voll ist.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Assigned | farm_processing | 1 |

Belohnung: 150 Geld, 0 Karten. Garantierte Worker: keine. Zone mindestens: Farm.

## 7. Nias Logistik

ID: `farm_storage`. Sprecher: Nia. Fokus: `farm_warehouse`.

Sprich mit Nia. Repariere das Farm-Lager. Die erste visuelle Stufe hat einen Fahrerplatz.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Talk | nia | 1 |
| Level | farm_warehouse | 1 |

Belohnung: 150 Geld, 0 Karten. Garantierte Worker: Farm-Lager-Manager, Farm-Fahrer. Zone mindestens: Farm.

## 8. Burning Hills bekommt Nachschub

ID: `farm_delivery`. Sprecher: Nia. Fokus: `farm_warehouse`.

Besetze Manager und Fahrer im Farm-Lager. Eine echte Lieferung muss das Stadtdock erreichen.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Assigned | farm_warehouse | 2 |
| Delivered | Stadtstatistik | 1 |
| Automated | farm_grow_01 | 1 |
| Automated | farm_processing | 1 |
| Automated | farm_warehouse | 1 |

Belohnung: 150 Geld, 1 Karten. Garantierte Worker: keine. Zone mindestens: Stadt.

## 9. Willkommen in der Stadt

ID: `city_rebuild`. Sprecher: Rocko. Fokus: `city_warehouse`.

Aktiviere das Stadt-Lager und den ersten Coffee-Shop. Rocko erwartet dich im Rathaus.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Level | city_warehouse | 1 |
| Level | coffee_01 | 1 |

Belohnung: 150 Geld, 0 Karten. Garantierte Worker: Stadt-Lager-Manager, Stadt-Lieferfahrer, Coffee-Shop-Manager. Zone mindestens: Stadt.

## 10. Die erste Kasse klingelt

ID: `city_sales`. Sprecher: Rocko. Fokus: `coffee_01`.

Besetze Stadt-Manager, Lieferfahrer und Shop-Manager. Verkaufe mindestens eine veredelte Blüte.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Assigned | city_warehouse | 2 |
| Assigned | coffee_01 | 1 |
| Sold | Stadtstatistik | 1 |

Belohnung: 150 Geld, 0 Karten. Garantierte Worker: keine. Zone mindestens: Stadt.

## 11. Ein neues Gesicht

ID: `townhall_spin`. Sprecher: Rocko. Fokus: `town_hall`.

Öffne das Rathaus und verwandle eine Worker-Karte mit einem Spin in einen Worker.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| TownHall | Stadtstatistik | 1 |
| Spin | Stadtstatistik | 1 |

Belohnung: 350 Geld, 0 Karten. Garantierte Worker: keine. Zone mindestens: Siedlung.

## 12. Ein Zuhause und ein grüner Ort

ID: `settlement_build`. Sprecher: Rocko. Fokus: `settlement_01`.

Wähle auf zwei getrennten Bauplätzen ein Wohnhaus und einen Park und baue beide auf. Der letzte freie Platz bleibt für den noch fehlenden Typ reserviert.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Level | residential_house | 1 |
| Level | park | 1 |

Belohnung: 150 Geld, 0 Karten. Garantierte Worker: Hausmeister, Parkwächter. Zone mindestens: Siedlung.

## 13. Die Nachbarschaft wächst

ID: `settlement_daily`. Sprecher: Rocko. Fokus: `settlement_01`.

Besetze Hausmeister und Parkwächter. Hole die Tagesbelohnungen ab und erledige eine VIP-Quest. Neue Gebäude haben eine erste Tagesration.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Residents | Stadtstatistik | 1 |
| Daily | Stadtstatistik | 1 |
| VipCompleted | Stadtstatistik | 1 |
| Assigned | residential_house | 1 |
| Assigned | park | 1 |

Belohnung: 600 Geld, 0 Karten. Garantierte Worker: Butterfabrik-Manager, Supermarkt-Leiter, Bäckermeister. Zone mindestens: Industrie.

## 14. Die nächste Produktionskette

ID: `industry_build`. Sprecher: Rocko. Fokus: `butter_factory`.

Repariere Butterfabrik, Supermarkt und eine Bäckerei. Der Stadtfahrer beliefert die Butterfabrik im Wechsel mit den Shops.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Level | butter_factory | 1 |
| Level | supermarket | 1 |
| Level | bakery_01 | 1 |

Belohnung: 150 Geld, 0 Karten. Garantierte Worker: keine. Zone mindestens: Industrie.

## 15. Butter, Backwaren, Zukunft

ID: `industry_sell`. Sprecher: Rocko. Fokus: `bakery_01`.

Besetze die drei Spezialisten. Zwei Blüten werden zu einer Butter. Die Bäckerei verkauft fertige Backwaren.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Butter | Stadtstatistik | 1 |
| PastrySold | Stadtstatistik | 1 |
| Automated | butter_factory | 1 |
| Automated | supermarket | 1 |
| Automated | bakery_01 | 1 |

Belohnung: 150 Geld, 0 Karten. Garantierte Worker: keine. Zone mindestens: Industrie.

## 16. Burning Hills gehört euch

ID: `freeplay`. Sprecher: Rocko. Fokus: `supermarket`.

Hole den täglichen Grinderbonus im Supermarkt ab. Danach bestimmst du den weiteren Ausbau. Parks liefern weiterhin erreichbare VIP-Aufträge.

| Bedingung | Ziel | Mindestwert |
| --- | --- | --- |
| Daily | supermarket | 1 |
| Automated | butter_factory | 1 |
| Automated | supermarket | 1 |
| Automated | bakery_01 | 1 |

Belohnung: 500 Geld, 0 Karten. Garantierte Worker: keine. Zone mindestens: Industrie.

## Praktischer Bedienweg

„Zum Questziel“ fokussiert die Kerninstanz. „Bewohner ansprechen“ erfüllt den passenden Dialog. Bauen/Ernte/Worker befinden sich im Gebäudepanel. Im Workerfenster eine Karte und den Manager- oder Fahrerplatz wählen und „Zuweisen“ drücken. Der Fahrer des Farm-Lagers gehört in Fahrerplatz 1, der Lager-Manager in Managerplatz 0. Analog funktioniert das Stadt-Lager. Tagesbelohnungen werden im Rathaus abgeholt; VIP-Belohnungen im Auftragsjournal.

Nach dem letzten Abholen ist Freeplay aktiv. Kein zusätzlicher Grinderkauf ist notwendig. Der automatisierte Referenzpfad und ein Pfad mit früh errichteten optionalen Gebäuden bestehen aktuell. Die gemessene Simulationszeit steht im Validierungsbericht und ersetzt keine menschliche Spielzeitmessung.

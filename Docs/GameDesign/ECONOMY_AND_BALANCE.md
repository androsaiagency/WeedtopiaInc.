# Wirtschaft und Balance

Inhaltsquelle: `Assets/Weedtopia/Game/Content/BurningHills.json`, Inhaltsversion 2, Balancing vom 30.09.2026. Werte liegen außerhalb von UI und Views. Die JSON-Datei wird über ein explizit referenziertes Content-Asset geladen und validiert.

Start: 1.000 Geld, 50 Grinder, keine Karten. Geld ist ein normalisierter, serialisierter Dezimalexponentwert mit zwölf signifikanten Stellen; Ganzzahlen für Grinder/Karten/Level/Slots bleiben exakt. Transformationsraten rechnen aggregiert in festen Sekundentakten. Rundung und relative Genauigkeit des skalierbaren Werts sind keine unbegrenzte Festkommapräzision.

Ausbaukosten für Ziellevel L: Basis × 1,08^min(L−1,24) × 1,012^max(0,L−25). Die optionalen Felder `cost.earlyLevels=24` und `cost.earlyGrowth=1.08` bilden die frühe Steigung ab; `cost.growth=1.012` gilt danach. Produktion, Kapazität und Durchsatz bleiben bei Basis × 1,008^(Level−1), Verkaufspreise bei Basis × 1,004^(Level−1). Geld wird wie bisher mit zwölf signifikanten Stellen berechnet; Anzeigen können gerundet sein.

| Gebäude | Erstaufbau Geld | Erstaufbau Sekunden | Maximale Umbauzeit Sekunden |
| --- | ---: | ---: | ---: |
| Anbauplatz | 100 | 30 | 120 |
| Farm-Veredelung | 150 | 60 | 240 |
| Farm-Lager | 175 | 90 | 360 |
| Stadt-Lager | 200 | 120 | 480 |
| Coffee-Shop | 250 | 120 | 480 |
| Wohnhaus | 1200 | 300 | 1200 |
| Park | 800 | 180 | 720 |
| Butterfabrik | 2500 | 480 | 1920 |
| Supermarkt | 2000 | 360 | 1440 |
| Bäckerei | 3500 | 600 | 2400 |

Die vollständige erste Verkaufskette kostet 875 Geld, also bleiben ohne Quest-Geldbelohnungen 125 Geld Reserve. Mitarbeiter werden weiter durch die unveränderten Anfangsquests vergeben. Weitere Exemplare einer Familie verwenden dieselben Werte. Das Rathaus ist bereits vorhanden und nicht ausbaubar; ein freier Bauplatz kostet nichts zusätzlich zur gewählten Bebauung.

Längere Baustellen entstehen nur bei den vorhandenen sichtbaren Tierwechseln. Für Ziellevel 1/5/10/25/50/100/150 gelten Zeitfaktoren 1/1,25/1,5/2/2,5/3/4; alle weiteren sichtbaren Tierwechsel behalten Faktor 4. `tierBuildMultipliers` definiert die Faktoren je Familie; sein letzter Wert gilt auch für spätere Tiers. Zwischenlevel werden beim bezahlten Befehl sofort abgeschlossen. Zeitwerte werden auf volle Sekunden aufgerundet; danach wirkt der vorhandene Worker-Bauzeitbonus mit erneuter Aufrundung. Der bisherige Level produziert während einer Baustelle weiter. Geld- und Premiumkauf verwenden dieselbe Ausbaulogik, Beschleunigung wird nur für aktive Baustellen angeboten.

Speicherkompatibilität: Inhalts-/Save-Version bleiben 2, weil keine Gebäude-IDs oder gespeicherten Zustandsfelder geändert werden. Bereits bezahlte Baustellen behalten ihre gespeicherte Restzeit und werden genau einmal beendet, auch wenn dieser Ziellevel nach der neuen Regel sofort wäre. Preise und Zeitkurve werden erst beim nächsten Kauf angewendet. Bisherige Spielstände werden nicht rückwirkend belastet oder zurückgesetzt.

Der Anbau produziert alle vier Sekunden eine Charge von vier rohen Blüten. Der frühe Verarbeiter schafft eine Blüte je Sekunde. Alle Puffer sind begrenzt. Je Ressourcentyp gibt es einen eigenen Puffer; ein voller Output hält Input zurück. Veredelung 1:1, Butter 2 Blüten:1 Butter, Backware 1 Butter:1 Backware. Verkauf entnimmt Ware und akkumuliert Verkauf/Geld getrennt. Ein Anbau-Manager gibt Ernte automatisch frei; ohne Manager wird manuell geerntet. Andere Tätigkeiten können 30 Sekunden manuell betrieben werden oder mit Manager dauerhaft laufen.

Lagerfahrerplätze entsprechen dem visuellen Tierindex: T00=0, T01=1, T02=2 bis T24=24. Der Managerplatz ist separat. Fahrten reservieren Zielkapazität; bereits transportierte Ware bleibt bei späterer Zielblockade im Fahrzeug. Entfernen/Tauschen eines fahrenden Workers wird abgelehnt. Stadtfahrer bedienen verfügbare Ziele zyklisch, damit die Butterfabrik nicht von Coffee-Shops verdrängt wird.

Worker haben fünf Seltenheiten mit 1/2/3/4/5 unterschiedlichen rollenrelevanten Attributen. Gewichte 60/25/10/4/1, eigener gespeicherter Xorshift32-Zustand. Startquests geben die nötigen Spezialisten garantiert. Spins verbrauchen eine Karte, keine Grinder. Attributbonus = 0,05 × Worker-Level × Worker-Rang. Kosten und Effektgrößen stehen in den Inhaltsdaten.

Premiumangebote sind kurzlebige, einmal verwendbare Bestätigungstoken. Ein Angebot verändert keine Währung. Abbrechen kostet nichts. Geldmangel bietet einen vollständigen Grinderkauf an (kein versteckter Teilabzug von Geld): aufgerundete Kosten / 100. Beschleunigung kostet aufgerundete verbleibende Bauzeit / 60. Veraltete und mehrfach bestätigte Angebote werden abgewiesen.

Tagesgrenze ist UTC, mit gespeicherter Höchstzeit gegen Uhr-Rückstellung. T01 liefert täglich eine Karte / VIP-Quest / Grinder; jedes visuelle Tier erhöht die Basismenge um eins. Offline-Simulation ist auf acht Stunden begrenzt und verwendet dieselbe Simulation. Fehlende Kalendertage werden höchstens sieben Tage berücksichtigt. Lokale Uhrdaten sind keine serverautorisierte Anti-Cheat-Garantie.

Die aktuelle Fortschrittssimulation steht in `Docs/Implementation/Validation/progression_simulation.json`. Sie erreicht alle 16 Hauptquests und Freeplay ohne Grinderkauf über öffentliche Spielerbefehle. Die Erstaufbau-Kalibrierung mit unveränderten Questbelohnungen benötigte je nach Worker-Zufallsfolge etwa 35–39 simulierte Minuten; aktuelle Testläufe stehen in den Validierungsdateien. Das ist ein Deadlock-/Regeltest mit schneller automatischer Bedienung, kein menschlicher Spielzeit- oder Retentionnachweis.

Optionales frühes Bauen des zweiten Anbauplatzes und der zweiten Bäckerei ist separat bis Freeplay getestet. Die Automationsbedingungen beziehen sich auf die garantierten Kerninstanzen. Bei versehentlichem Überausbau hilft vor Freeplay ein manueller Aufbauauftrag: unter 200 Geld gibt es 100 Geld, höchstens einmal pro 60 Simulationssekunden.

Ein UTC-Tagescheckpoint speichert das bisherige Anspruchstier. Nachholtage verwenden dieses alte Tier; nur der aktuelle Tag bekommt das aktuelle Gebäudetier. Ein Ausbau vervielfacht somit keine bereits verpassten Tage. Ein neues Haus/Park/Supermarkt kann am Bautag einmal abgeholt werden. VIP-Ziele verwenden zusätzliche produzierte Rohblüten oder abgelieferte Mengen. Es gibt höchstens zwölf offene Aufträge.

Worker-Level 1–100 und Rang 1–10 haben unabhängige Obergrenzen. Levelkauf kostet Basis 50 × Level × Rang / Hausbonus. Rang kostet zusätzlich zwei Karten. Eine seltenere Karte besitzt mehr unterschiedliche Attribute. Die Tabellenmatrix listet jeden zulässigen Attributpool.

Gesamtgeld verdient umfasst Produktions-/Verkaufseinnahmen, Aufbauhilfe sowie einmalig abgeholte Haupt- und VIP-Questbelohnungen. Das Startkapital zählt nicht als Verdienst; Ausgaben senken nur den aktuellen Kontostand. Tages-Grinder und Karten sind separate Ganzzahlressourcen.

Validierung der Umsetzung am 30.09.2026: 12 Assemblies erfolgreich kompiliert, 91/91 EditMode- und 42/42 PlayMode-Tests bestanden. Der Standardpfad benötigte 2.100 simulierte Sekunden (35 Minuten), erledigte 16 Quests und behielt 51 Grinder, ohne Aufbauhilfe. Mehrere Worker-Seeds, frühe optionale Gebäude, Sofortausbau, Bauzäune, Premiumbestätigung sowie bestehende/lange gespeicherte Baustellen wurden geprüft. Sammelbeleg: `Docs/BalanceRevision/validation.json`. Das ersetzt keinen manuellen Langzeit-Spieltest.

# Wirtschaft und Balance

Inhaltsquelle: `Assets/Weedtopia/Game/Content/BurningHills.json`, Inhaltsversion 1. Werte liegen außerhalb von UI und Views. Die JSON-Datei wird über ein explizit referenziertes Content-Asset geladen und validiert.

Start: 1.000 Geld, 50 Grinder, keine Karten. Geld ist ein normalisierter, serialisierter Dezimalexponentwert mit zwölf signifikanten Stellen; Ganzzahlen für Grinder/Karten/Level/Slots bleiben exakt. Transformationsraten rechnen aggregiert in festen Sekundentakten. Rundung und relative Genauigkeit des skalierbaren Werts sind keine unbegrenzte Festkommapräzision.

Ausbaukosten: Basis × 1,012^(Ziellevel−1). Produktion, Kapazität und Durchsatz: Basis × 1,008^(Level−1). Verkaufspreis: Basis × 1,004^(Level−1). Bauzeit: aufgerundete 3 × 1,001^(Ziellevel−1) Sekunden, bei 120 Sekunden gedeckelt. Kurven gelten monoton bis Level 5000. Der alte Level arbeitet während des Ausbaus weiter; bei Abschluss steigt das Level genau einmal.

Der Anbau produziert alle vier Sekunden eine Charge von vier rohen Blüten. Der frühe Verarbeiter schafft eine Blüte je Sekunde. Alle Puffer sind begrenzt. Je Ressourcentyp gibt es einen eigenen Puffer; ein voller Output hält Input zurück. Veredelung 1:1, Butter 2 Blüten:1 Butter, Backware 1 Butter:1 Backware. Verkauf entnimmt Ware und akkumuliert Verkauf/Geld getrennt. Ein Anbau-Manager gibt Ernte automatisch frei; ohne Manager wird manuell geerntet. Andere Tätigkeiten können 30 Sekunden manuell betrieben werden oder mit Manager dauerhaft laufen.

Lagerfahrerplätze entsprechen dem visuellen Tierindex: T00=0, T01=1, T02=2 bis T24=24. Der Managerplatz ist separat. Fahrten reservieren Zielkapazität; bereits transportierte Ware bleibt bei späterer Zielblockade im Fahrzeug. Entfernen/Tauschen eines fahrenden Workers wird abgelehnt. Stadtfahrer bedienen verfügbare Ziele zyklisch, damit die Butterfabrik nicht von Coffee-Shops verdrängt wird.

Worker haben fünf Seltenheiten mit 1/2/3/4/5 unterschiedlichen rollenrelevanten Attributen. Gewichte 60/25/10/4/1, eigener gespeicherter Xorshift32-Zustand. Startquests geben die nötigen Spezialisten garantiert. Spins verbrauchen eine Karte, keine Grinder. Attributbonus = 0,05 × Worker-Level × Worker-Rang. Kosten und Effektgrößen stehen in den Inhaltsdaten.

Premiumangebote sind kurzlebige, einmal verwendbare Bestätigungstoken. Ein Angebot verändert keine Währung. Abbrechen kostet nichts. Geldmangel bietet einen vollständigen Grinderkauf an (kein versteckter Teilabzug von Geld): aufgerundete Kosten / 100. Beschleunigung kostet aufgerundete verbleibende Bauzeit / 60. Veraltete und mehrfach bestätigte Angebote werden abgewiesen.

Tagesgrenze ist UTC, mit gespeicherter Höchstzeit gegen Uhr-Rückstellung. T01 liefert täglich eine Karte / VIP-Quest / Grinder; jedes visuelle Tier erhöht die Basismenge um eins. Offline-Simulation ist auf acht Stunden begrenzt und verwendet dieselbe Simulation. Fehlende Kalendertage werden höchstens sieben Tage berücksichtigt. Lokale Uhrdaten sind keine serverautorisierte Anti-Cheat-Garantie.

Die aktuelle Fortschrittssimulation steht in `Docs/Implementation/Validation/progression_simulation.json`. Sie erreicht alle 16 Hauptquests und Freeplay ohne Grinderkauf über öffentliche Spielerbefehle. Der Normalpfad benötigte zuletzt 72 simulierte Sekunden. Das ist ein Deadlock-/Regeltest mit schneller automatischer Bedienung, kein menschlicher Spielzeit- oder Retentionnachweis.

Optionales frühes Bauen des zweiten Anbauplatzes und der zweiten Bäckerei ist separat bis Freeplay getestet. Die Automationsbedingungen beziehen sich auf die garantierten Kerninstanzen. Bei versehentlichem Überausbau hilft vor Freeplay ein manueller Aufbauauftrag: unter 200 Geld gibt es 100 Geld, höchstens einmal pro 60 Simulationssekunden.

Ein UTC-Tagescheckpoint speichert das bisherige Anspruchstier. Nachholtage verwenden dieses alte Tier; nur der aktuelle Tag bekommt das aktuelle Gebäudetier. Ein Ausbau vervielfacht somit keine bereits verpassten Tage. Ein neues Haus/Park/Supermarkt kann am Bautag einmal abgeholt werden. VIP-Ziele verwenden zusätzliche produzierte Rohblüten oder abgelieferte Mengen. Es gibt höchstens zwölf offene Aufträge.

Worker-Level 1–100 und Rang 1–10 haben unabhängige Obergrenzen. Levelkauf kostet Basis 50 × Level × Rang / Hausbonus. Rang kostet zusätzlich zwei Karten. Eine seltenere Karte besitzt mehr unterschiedliche Attribute. Die Tabellenmatrix listet jeden zulässigen Attributpool.

Gesamtgeld verdient umfasst Produktions-/Verkaufseinnahmen, Aufbauhilfe sowie einmalig abgeholte Haupt- und VIP-Questbelohnungen. Das Startkapital zählt nicht als Verdienst; Ausgaben senken nur den aktuellen Kontostand. Tages-Grinder und Karten sind separate Ganzzahlressourcen.

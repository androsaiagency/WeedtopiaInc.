# Kapitel 2: Das erste Stadtfest

Nach den 16 Anfangsquests lädt Rocko in **Aufträge** zum ersten Stadtfest ein. Das optionale Kapitel verbindet alle vier Stadtbereiche. Freies Bauen, tägliche Belohnungen und VIP-Aufträge bleiben verfügbar. Das Kapitel setzt weder neue Bauplätze noch Zufallsarbeiter voraus.

## Entscheidung und Etappen

Vor Beginn wählt der Spieler verbindlich einen Schwerpunkt. Die Auswahl zeigt Ziel und dauerhafte Belohnung vor dem Start. **Nachbarschaftsfest** verlangt den zweiten Coffee-Shop und mehr Blütenverkäufe; **Genussmeile** verlangt die zweite Bäckerei und mehr Backwarenverkäufe. Die andere Produktionskette bleibt auf beiden Wegen wichtig.

| Etappe | Aufgabe | Belohnung |
|---|---|---|
| Die Farm macht sich bereit | Erstes Feld und Veredelung auf Level 5; 200 neue Blüten veredeln | 1.000 Geld, 1 Karte, Anbaukraft |
| Alles kommt an | Beide Lager auf Level 5; zweites Feld bauen und Anbaukraft einsetzen; 240 Waren neu ausliefern | 2.000 Geld, 1 Karte, garantierte Verkaufskraft bzw. Bäcker |
| Zweiter Treffpunkt / Genussmeile | Zweiten Coffee-Shop bzw. zweite Bäckerei auf Level 5 ausbauen und Leitung einsetzen; 360 Blüten bzw. 120 Backwaren neu verkaufen | 3.000 Geld, 2 Karten |
| Ein Fest für die Nachbarn | Ein Haus und ein Park auf Level 5; 15 Einwohner | 2.500 Geld, 2 Karten |
| Die große Generalprobe | Nachbarschaft: Supermarkt Level 5, Butterfabrik Level 2; Genussmeile: Butterfabrik Level 5, Supermarkt Level 2. Beide: erste Bäckerei Level 2, 120 neue Butter, 80 neue Backwarenverkäufe | 4.000 Geld, 3 Karten |
| Burning Hills feiert! | 400 / 250 Blüten, 100 / 180 Backwaren neu verkaufen; 400 Waren neu ausliefern | 5.000 Geld, 5 Karten, Stadttitel und dauerhafter Bonus |

Abschlussbonus: **10 % mehr Erlös in allen Coffee-Shops** beim Nachbarschaftsfest oder **10 % mehr Erlös in allen Bäckereien** bei der Genussmeile. Gilt online und offline, für vorhandene und spätere Betriebe der gewählten Familie. Der angezeigte Stückpreis berücksichtigt den Bonus. Der Titel **Feststadt** erscheint im Kapitelabschluss und in der Rathausinspektion. Die vorgeschriebenen Ausbaustufen verändern bereits die vorhandenen Gebäudeansichten; zusätzliche Festdekoration wurde nicht angelegt.

## Fortschritt und Spielstände

- Produktions-, Verkaufs- und Lieferzähler beginnen bei jeder Etappe neu. Historische Mengen erfüllen neue Aufträge nicht automatisch.
- Bestehende Gebäude und Einwohner zählen sofort. Bereits ausgebaute Städte müssen nichts abreißen oder doppelt bauen.
- Offline-Fortschritt zählt für die aktive Etappe. Belohnungen werden manuell abgeholt; es gibt keine automatische Kette von Abschlusszahlungen.
- Jede Belohnung wird genau einmal vergeben. Der dauerhafte Bonus wird aus dem gespeicherten Abschluss abgeleitet und beim Laden nicht erneut addiert.
- Die 16 Tutorialquests, deren IDs, Gebäude-Balance, Startgeld, Schema- und Inhaltsversion bleiben unverändert. Das neue optionale Spielstandfeld ist abwärtskompatibel mit vorhandenen Version-1/2-Inhalten beim Laden in dieser Fassung. Kapitelstände sind nicht für ältere Spielversionen vorgesehen.
- Keine Änderungen an Szenen, Prefabs, Benutzer-Spielständen oder produktiven Art-Referenzen nötig. Vorhandene Auftragsflächen und das ListRow-Prefab werden genutzt.

## Architektur

`CityProjectDefinition` enthält Pläne, Etappen, Ziele und Boni. `CityProjectService` verwaltet Auswahl, Baselines und Belohnungen unabhängig von Unity. `CityProjectText` liefert lokalisierte Anzeigetexte. `CityProjectRewards` wird direkt von der Verkaufssimulation und der Gebäudeinspektion verwendet. `SaveValidator` prüft Plan, Historie, Freischaltung und Zählerbaselines. Das wiederholbar ausführbare Authoring-Skript ist `Tools/Implementation/author_city_festival.py`.

## Heute Abend testen

1. Vorhandenen Spielstand laden. Nach den Anfangsquests **Aufträge** öffnen; bei einem neuen Spiel zuerst die 16 Anfangsquests abschließen.
2. Beide Schwerpunkte und ihre Belohnungen ansehen, dann einen verbindlich wählen.
3. Die Ziele rechts anklicken, um zum passenden Gebäude zu springen. Fertige Etappen über den vorhandenen Belohnungsbutton links abschließen.
4. Nach Etappe 2 die garantierte Leitung unter **Mein Team** dem zweiten Betrieb zuweisen.
5. Während einer Etappe schließen und erneut laden; Zielwerte, Schwerpunkt und Fortschritt müssen erhalten bleiben.

Automatisierte Ergebnisse werden separat in diesem Ordner dokumentiert. Menschliche Bewertung von Spaß, Tempo und Lesbarkeit bleibt offen.

## Simuliertes Tempo

Zwei feste Zufallsstarts pro Schwerpunkt wurden von einem Diagnose-Spieler ausschließlich über öffentliche Spielbefehle absolviert: Tutorial ab 1.000 Geld, anschließend Kapitelwahl, Ausbau, Mitarbeitereinsatz und Abholen der Belohnungen. Keine Ressourcenspritzen, kein Überspringen von Bauzeiten, keine Nothilfe und keine Grinderausgaben.

| Schwerpunkt | Kapitel, Seed 121 | Kapitel, Seed 420 |
|---|---:|---:|
| Nachbarschaftsfest | 82,6 Minuten | 86,4 Minuten |
| Genussmeile | 109,9 Minuten | 117,4 Minuten |

Die Anfangsquests kommen mit 35,0 bzw. 39,2 Minuten hinzu. Das sind simulierte Durchlaufzeiten, keine gemessenen menschlichen Spielzeiten. Der Diagnose-Spieler tätigt die geforderten Investitionen; zusätzliche Optimierung der Produktionskette bleibt dem Spieler offen. Die Tests prüfen zudem bestehende Spielstände ohne Kapitel-Feld, Baseline-Erhalt über Laden und Offline-Fortschritt, einmalige Belohnungen sowie die tatsächliche Anwendung des Familienbonus.

## Abschlussprüfung am 30.09.2026

- Quellkompilierung: 12 Assemblies erfolgreich; gespeicherte Quellhashes stimmen mit dem abschließenden Stand überein.
- Unity EditMode: **105/105 bestanden**, Lauf `20260930_125056_EditMode_CityFestivalFinal`.
- Unity PlayMode: **44/44 bestanden**, Lauf `20260930_125348_PlayMode_CityFestivalFinal`.
- Die neuen PlayMode-Prüfungen bedienen die vorhandene Auftragsoberfläche, wählen einen Plan, holen eine Etappenbelohnung ab, laden den Spielstand neu und prüfen die Textfelder aller Etappen beider Wege einschließlich Abschluss auf Überlauf.
- Alle 32 geschützten Szenen-, Prefab- und Metadateien unverändert; bisherige Definitionen, Texte, Quests und Gebäudebalance erhalten. Keine persönlichen Spielstände geändert.
- Zusammengefasster Nachweis: `validation.json`. Die vier `simulation_*.json` enthalten Etappenzeiten und Abschlusszustände.
- Keine menschliche Spielspaß- oder visuelle Abnahme behauptet. Game-View-Aufnahmen und neue Festdekoration sind nicht Bestandteil dieser Änderung.

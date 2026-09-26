# Codex-Masterauftrag: Weedtopia Inc. – Burning Hills als spielbares Unity-Spiel umsetzen

## Rolle und Arbeitsauftrag

Du arbeitest im bestehenden Unity-Projekt:

`D:\Unity Games\Weedtopia Inc\WeedtopiaInc`

Setze **Weedtopia Inc. / Burning Hills** als lokal vollständig spielbare, datengetriebene erste Produktversion um. Dies ist ein **Implementierungsauftrag**, kein Auftrag für eine bloße Analyse, ein Konzeptdokument oder einen unverbundenen Prototyp. Arbeite in überprüfbaren Meilensteinen, führe die jeweils passenden Tests aus, behebe gefundene Fehler und fahre fort, solange ein sicherer, sinnvoller Arbeitsschritt ohne neue Entscheidung des Benutzers möglich ist.

Das Ziel ist ein zusammenhängendes Spiel mit:

- isometrischer 3D-Spielwelt und 2D-/2.5D-Gebäudepräsentation,
- der vollständigen Burning-Hills-Einführung von Farm über Stadt und Siedlung bis zum Industriegebiet,
- funktionierender Produktions-, Logistik-, Ausbau-, Worker-, Quest-, Tagesbonus- und Speicherlogik,
- einer nach dem Tutorial frei spielbaren Idle-/Management-Schleife,
- einer Architektur, die später weitere Städte und einen globalen Highscore aufnehmen kann,
- in Unity sichtbar authorierten Szenen, Prefabs, Konfigurationen und Referenzen,
- automatischen EditMode- und PlayMode-Tests sowie nachvollziehbarer lokaler Abnahme.

Bleibe nicht bei Planung, Gerüsten, Interfaces oder TODOs stehen. Interfaces gelten nur dann als umgesetzt, wenn eine lokal nutzbare Implementierung, Unity-Integration, Tests und ein funktionsfähiger Benutzerfluss existieren. Ein externer Online-Highscore und weitere Städte sind bewusst spätere Ausbaustufen; deren Grenzen sind unten ausdrücklich definiert.

---

## 1. Verbindliche Quellen und Priorität

Lies zu Beginn vollständig:

1. `AGENTS.md` und alle weiteren im Projekt wirksamen Anweisungen.
2. Diesen Masterauftrag.
3. `D:\Unity Games\Weedtopia Inc\prompt.txt` als ursprüngliche Spielidee.
4. `Docs/ArtProduction/PROGRESS.json`.
5. `Docs/ArtProduction/ACCEPTANCE_REPORT.md`.
6. `Docs/ArtProduction/ART_DIRECTION.md`.
7. `Docs/ArtProduction/2D_BUILDING_ART_PIPELINE.md`.
8. `Docs/ArtProduction/BUILDING_ASSET_INDEX.md`.
9. `Docs/ArtProduction/BUILDING_ASSET_MANIFEST.json`.
10. `Docs/ArtProduction/BUILDING_TIER_MATRIX.csv`.
11. Die vorhandenen Runtime-, Editor- und Testquellen unter `Assets/Weedtopia`.
12. Die vorhandene Szene, Build Settings, Package-Konfiguration, Input Actions, Prefabs und ScriptableObjects.

Bei Widersprüchen gilt diese Reihenfolge:

1. aktuelle Benutzeranweisung,
2. `AGENTS.md`,
3. dieser Masterauftrag,
4. die aktive Entscheidung in `2D_BUILDING_ART_PIPELINE.md` und `PROGRESS.json`,
5. die ursprüngliche Spielidee,
6. ältere beziehungsweise archivierte Meshy-/FBX-Dokumente.

Die älteren Meshy-Unterlagen bleiben als Herkunft und Archiv erhalten. Sie definieren **nicht mehr** den aktiven visuellen Produktionsweg.

---

## 2. Verifizierter Ausgangsstand – vor jeder Änderung erneut prüfen

Der folgende Stand wurde am 21. September 2026 ermittelt. Verlasse dich nicht blind darauf, sondern bestätige ihn read-only, bevor du Dateien änderst:

- Unity-Version: `6000.3.9f1`.
- Renderpipeline: URP `17.3.0`.
- Vorhandene relevante Pakete: Input System `1.18.0`, uGUI `2.0.0`, Test Framework `1.6.0`, AI Navigation `2.0.10`.
- In den Build Settings befindet sich nur `Assets/Scenes/SampleScene.unity`.
- Diese Szene enthält lediglich Kamera, Directional Light und Global Volume; sie ist noch keine Spielszene.
- Im Projekt existieren 13 C#-Dateien, davon 11 Weedtopia-spezifische Dateien für Gebäudepräsentation und Art-Intake sowie zwei Unity-Tutorialdateien.
- Es gibt noch keine Gameplay-, Ökonomie-, Produktions-, Worker-, Quest-, Speicher-, UI-, Welt- oder Highscore-Implementierung.
- `BuildingVisualCatalog`, `IBuildingVisualProvider`, `BuildingVisualWrapper` und `BuildingSpriteVisual` bilden eine brauchbare Präsentationsbasis.
- Der Gebäudekatalog besitzt 251 Einträge: zehn ausbaubare Familien mit je 25 Zuständen sowie das feste Rathaus.
- 251 Gebäude-Referenzbilder und elf Showcases sind kunstseitig akzeptiert.
- 251 Wrapper-Prefabs mit `VisualRoot`, Interaktions-, Worker-, Liefer-, VFX- und Collider-Ankern existieren und sind im Katalog referenziert.
- Die aktive Lieferstrategie lautet `isometric_2d_sprite_with_optional_2_5d_layers`.
- Der frühere Meshy-/FBX-/Blender-Weg wurde nach sichtbaren Rekonstruktions- und Stilproblemen archiviert.
- Das transparente Rathaus-Sprite existiert, aber der isolierte 2D-Prototyp und seine Game-View-Abnahme sind laut `PROGRESS.json` noch offen.
- Der produktive Rathaus-Wrapper verweist weiterhin auf den älteren 3D-Versuch. Er darf nicht als freigegebener 2D-Produktionsstand behandelt werden.
- Frühere Berichte nennen 13 von 13 bestandene Art-Pipeline-EditMode-Tests und eine vollständige Katalog-/Wrapper-Serialisierung. Das ist historische Evidenz, kein Ersatz für einen aktuellen Lauf nach Änderungen.
- Beim letzten Check war ein Unity Editor geöffnet. Starte niemals ungeprüft eine zweite Instanz und beende niemals den Editor des Benutzers.
- Der Projektordner war beim letzten Check **kein Git-Repository**. Führe keine Git-Operationen aus und behaupte keinen Git-Status, solange nicht ein tatsächlicher Repository-Root gefunden wurde.

Erstelle zu Beginn eine knappe Ist-Zustandsnotiz in `Docs/Implementation/IMPLEMENTATION_STATUS.md`. Sie soll tatsächliche Befunde, bestehende Fremd-/Benutzeränderungen, offene manuelle Gates und die gewählte nächste Etappe enthalten. Halte sie während der Umsetzung aktuell.

---

## 3. Produktvision und verbindliche Atmosphäre

### 3.1 Welt und Ton

Die Welt liegt nach einem langen, absurden THC-Krieg in Trümmern. Beide Seiten haben kapituliert; Burning Hills ist Teil eines staatlich begleiteten Wiederaufbauprojekts. Der Ton ist eine Mischung aus:

- warmer, lesbarer, wiederverwendeter Solarpunk-/Cozy-Dystopia-Basis,
- modularer retro-industrieller Technik mit sichtbaren Förderbändern, Tanks, Leitungen und Logistikwegen,
- atmosphärischem Blue-Hour-/Afterglow-Licht,
- augenzwinkerndem 420-Thema ohne fotorealistischen Drogenkonsum und ohne düstere Brutalität,
- klarer Management-Lesbarkeit: Der Spieler muss Produktion, Stau, Lagerung, Transport und Ausbau visuell verstehen können.

Die gewählte Stilformel lautet verbindlich:

> Mockup 1 als Basis, Technik aus Mockup 4, Atmosphäre aus Mockup 5.

Das bedeutet für Unity:

- warme, handgemachte und reparierte Architektur,
- Pflanzen, Holz, Ziegel, recyceltes Metall und farbige Stoffelemente,
- modular erkennbare Maschinen und Logistik,
- blauer Abendhimmel mit warmen Fenstern und Arbeitslichtern,
- deutliche Silhouetten und gute Lesbarkeit auch bei weitem Kamera-Zoom.

### 3.2 Technische Interpretation des Looks

Das Spiel bleibt eine **dreidimensionale Unity-Welt** mit isometrischer Kamera, 3D-Koordinaten, Collidern, Wegpunkten, Lieferwegen, Figuren, VFX und Interaktion. Die akzeptierten Gebäudebilder werden als 2D-/2.5D-Sprites in dieser Welt verwendet. Reaktiviere Meshy oder die verworfene 3D-Rekonstruktionspipeline nicht, außer der Benutzer ändert diese Entscheidung ausdrücklich.

Atmosphäre, Fensterlicht, Nebel, Partikel und Tageszeit gehören in Unity. Sie dürfen nicht zerstörerisch in die neutralen Gebäudequellen eingebacken werden.

---

## 4. Vollständiger Spielumfang der ersten Produktversion

### 4.1 Einstieg

Der Spieler wird von **Rocko** geweckt:

- Rocko ist ein ehemaliger Mafioso, arbeitet nun für die Regierung, ist Mitte 30, trägt Rasta-Look und ist Bürgermeister von Burning Hills.
- Er erklärt knapp den THC-Krieg, die beiderseitige Kapitulation und das gemeinsame Wiederaufbauprojekt.
- Der Spieler gibt einen eigenen Namen ein. Der Name wird validiert, gespeichert und in Dialogen/Statistiken verwendet.
- Rocko ernennt den Spieler zu seinem Assistenten.
- Startressourcen: exakt **1.000 Geld** und **50 goldene Grinder**.
- Erste Aufgabe: Die Farm muss zuverlässig laufen, bevor der Spieler sie verlassen darf.

Der Einstieg muss überspringbar und erneut startbar sein, ohne einen fehlerhaften Speicherstand zu erzeugen. Dialoge werden datengetrieben authoriert und nicht als langer hart codierter Kontrollfluss in einem MonoBehaviour abgelegt.

### 4.2 Farm

Die Farm besitzt dauerhaft:

- genau ein Farm-Lager,
- genau ein Farm-Verarbeitungsgebäude,
- eine datengetrieben konfigurierbare Zahl an Anbauplätzen,
- drei Bewohner an den jeweiligen Ruinen als Tutorial-Questgeber.

Produktionsfluss:

`Anbauplatz -> Farm-Verarbeitung -> Farm-Lager -> Transport -> Stadt-Lager`

Verbindliche Regeln:

- Anbauplätze erzeugen rohe Blüten in Zyklen.
- Ohne Manager muss der Spieler reife Ernte manuell einsammeln beziehungsweise den Transfer auslösen.
- Ein zugewiesener Manager automatisiert Ernte und Übergabe an das Förderband.
- Das Farmgebäude veredelt rohe Blüten mit begrenztem Durchlauf und Puffer.
- Ist der Durchlauf kleiner als der Zufluss, entsteht sichtbar und rechnerisch ein Stau. Ressourcen verschwinden nicht.
- Das Farm-Lager besitzt Kapazität, einen Managerplatz und Fahrerplätze.
- Fahrer transportieren veredelte Blüten mit Kapazität und Fahrzeit in die Stadt.
- Die Zahl der Fahrerplätze steigt mit jeder visuellen Stufe um eins; die genaue Basis wird in den Tuningdaten dokumentiert.
- Manager und Fahrer beeinflussen nur für ihre Tätigkeit sinnvolle Werte.

Die Farm gilt erst dann als „laufend“, wenn ein automatisierter, testbarer End-to-End-Fluss bis zum Stadttransport existiert und die Farm-Tutorialquests abgeschlossen sind.

### 4.3 Stadt

Die Stadt besitzt:

- das feste, nicht ausbaubare Rathaus,
- genau ein Stadt-Lager,
- eine datengetrieben konfigurierbare Zahl an Coffee-Shops.

Produktionsfluss:

`Stadt-Lager -> Lieferfahrer -> Coffee-Shop -> Geld`

Zusätzlich liefert das Stadt-Lager Blüten an die Butterfabrik im Industriegebiet.

Verbindliche Regeln:

- Im Rathaus ist Rocko als fester Bürgermeister/Manager sichtbar zugeordnet.
- Das Rathaus zeigt Stadtstatistiken, Worker-Karten und die lokale Highscore-/Statistikansicht.
- Das Stadt-Lager hat Manager- und Fahrerplätze wie das Farm-Lager.
- Coffee-Shops wandeln gelieferte veredelte Blüten in Geld um.
- Durchsatz, Lagerkapazität, Lieferkapazität und Verkaufsrate müssen echte, sichtbare Engpässe bilden können.
- Nach den nötigen Stadtquests und der Automatisierung wird die Siedlung freigeschaltet.

### 4.4 Siedlung

Die Siedlung besitzt mehrere authorierte Bauplätze. Der Spieler entscheidet pro Bauplatz zwischen:

- Wohnhaus,
- Park.

Das Tutorial führt zwingend durch den Bau mindestens eines Wohnhauses und eines Parks.

Wohnhaus:

- erhöht die Einwohnerzahl,
- besitzt einen Hausmeister-Slot,
- erzeugt pro Kalendertag Worker-Karten,
- erhält an jedem visuellen Meilenstein eine höhere tägliche Kartenmenge.

Park:

- besitzt einen Parkwächter-Slot,
- erzeugt pro Kalendertag VIP-Quests,
- erhält an jedem visuellen Meilenstein eine höhere tägliche Questmenge,
- liefert im Freeplay weiterhin sinnvoll skalierte, nicht blockierende Entwicklungsquests.

Die Bauplatzwahl muss gespeichert werden. Eine bereits getroffene Wahl darf nicht durch Szenenneuladen oder visuelle Prefabwechsel verloren gehen.

### 4.5 Industriegebiet

Das Industriegebiet besitzt:

- genau eine Butterfabrik,
- genau einen Supermarkt,
- eine datengetrieben konfigurierbare Zahl an Bäckereien.

Produktionsfluss:

`Stadt-Lager -> Butterfabrik -> Cannabis-Butter -> Bäckerei -> Backwarenverkauf -> Geld`

Verbindliche Regeln:

- Die Butterfabrik besitzt begrenzten Durchlauf und einen spezialisierten Manager-Slot.
- Bäckereien verbrauchen Butter und erwirtschaften Geld; es wird für die erste Version keine weitere Pflichtwährung erfunden.
- Der Supermarkt erzeugt Geld in Abhängigkeit von der Einwohnerzahl.
- Der Supermarkt erzeugt zusätzlich täglich Grinder; die Menge wächst mit den visuellen Meilensteinen.
- Der Supermarkt besitzt einen spezialisierten Leiter-Slot.
- Nach Abschluss der Industrie-Tutorialquests wechselt das Spiel in Freeplay.

### 4.6 Freeplay und spätere Städte

Nach Farm, Stadt, Siedlung und Industrie darf der Spieler frei ausbauen. Parks liefern weitere VIP-Quests. Die erste Version enthält vollständig spielbar **Burning Hills**.

Weitere Städte werden noch nicht als leere Kopien erzeugt. Die Architektur muss jedoch `CityId`, getrennte Stadtzustände, stadtbezogene Definitionen und eine spätere Freischaltung mehrerer Städte unterstützen, ohne Kernsysteme umzuschreiben.

---

## 5. Ausbau- und visuelle Stufen

### 5.1 Levelbereiche

Alle ausbaubaren Gebäudefamilien verwenden exakt diese visuellen Bänder:

| Tier | Levelbereich |
|---|---:|
| `T00_RUIN` | 0 |
| `T01_L001` | 1–4 |
| `T02_L005` | 5–9 |
| `T03_L010` | 10–24 |
| `T04_L025` | 25–49 |
| `T05_L050` | 50–99 |
| `T06_L100` | 100–149 |
| `T07_L150` | 150–199 |
| `T08_L200` | 200–299 |
| `T09_L300` | 300–399 |
| `T10_L400` | 400–499 |
| `T11_L500` | 500–599 |
| `T12_L600` | 600–699 |
| `T13_L700` | 700–799 |
| `T14_L800` | 800–899 |
| `T15_L900` | 900–999 |
| `T16_L1000` | 1000–1249 |
| `T17_L1250` | 1250–1499 |
| `T18_L1500` | 1500–1749 |
| `T19_L1750` | 1750–1999 |
| `T20_L2000` | 2000–2499 |
| `T21_L2500` | 2500–2999 |
| `T22_L3000` | 3000–3999 |
| `T23_L4000` | 4000–4999 |
| `T24_L5000` | 5000 |

Das Rathaus verwendet ausschließlich `T_FIXED`.

### 5.2 Ausbauregeln

- Maximallevel der ersten Version: 5000; Definitionen müssen später erweiterbar sein.
- Geld ist die primäre Ausbauwährung.
- Reicht Geld nicht, darf ein Ausbau nur nach **expliziter Grinder-Bestätigung** angeboten werden.
- Grinder werden niemals still, automatisch oder durch einen Doppelklick ausgegeben.
- Reichen weder Geld noch bestätigte Grinder, bleibt der Ausbau gesperrt und erklärt verständlich, was fehlt.
- Bauzeiten sind datengetrieben. Grinder können nach expliziter Bestätigung beschleunigen oder abschließen.
- Ein Abbruch darf keine Währung duplizieren oder vernichten.
- Ausbaukosten, Bauzeit, Kapazität, Durchsatz und Ertrag folgen dokumentierten, monotonen Kurven.
- Die komplette Einführung muss mit vernünftigem Spieltempo und **ohne Ausgabe von Grindern** lösbar sein.
- Die Kurven liegen in ScriptableObjects beziehungsweise klar versionierten Inhaltsdaten; keine versteckten Magic Numbers in UI oder View-Code.

Da Geld- und Produktionswerte bei Level 5000 sehr groß werden können, implementiere einen serialisierbaren, getesteten skalierenden Zahlenwert für Idle-Ökonomie. Er muss Rechnen, Vergleichen, Clamping, deterministische Speicherung und lesbare Kurzformate wie `1.25K`, `3.4M` und darüber unterstützen. Premium-Grinder, Worker-Karten, Questmengen, Level und Slotzahlen bleiben exakte Ganzzahlen.

### 5.3 Tägliche Meilensteinbelohnungen

Treffe für die erste Version eine konsistente, datengetriebene Auslegung:

- Tier `T01` liefert eine Basismenge von eins pro Kalendertag.
- Jedes danach erreichte visuelle Tier erhöht diese tägliche Menge um eins.
- Wohnhäuser wenden die Menge auf Worker-Karten an.
- Parks wenden die Menge auf verfügbare VIP-Quests an.
- Supermärkte wenden die Menge auf Grinder an.
- Lager verwenden dieselbe visuelle Tierzählung für Fahrerplätze, sofern ihre Inhaltsdefinition keinen ausdrücklich dokumentierten anderen Basiswert setzt.

Die Vergabe erfolgt über einen testbaren Tageswechsel-Service, nicht durch `InvokeRepeating`. Verwende eine injizierbare Uhr, definiere eindeutig UTC oder lokale Spielzeit, verhindere Doppelabholung und behandle zurückgestellte Systemuhren defensiv. Ein echter Online-Zeitserver ist nicht Teil dieser Version.

---

## 6. Worker-System

Worker entstehen durch Startquests oder durch Worker-Karten. Karten werden im Rathaus über einen Zufalls-Spin in einen Worker umgewandelt.

### 6.1 Rollen

Unterstütze mindestens:

- Anbau-Manager,
- Farm-Verarbeitungsmanager,
- Farm-Lager-Manager,
- Farm-Fahrer,
- Stadt-Lager-Manager,
- Stadt-Lieferfahrer,
- Coffee-Shop-Manager,
- Hausmeister,
- Parkwächter,
- Butterfabrik-Manager,
- Supermarkt-Leiter,
- Bäckermeister.

Rollen und Eignungen werden datengetrieben modelliert. Ein Worker darf nur kompatiblen Slots zugewiesen werden. Umsetzen, Tauschen und Entfernen dürfen keine Duplikate oder verlorene Zuweisungen erzeugen.

### 6.2 Seltenheiten, Level, Stufe und Attribute

Seltenheiten:

1. Normal
2. Rare
3. Super Rare
4. Ultra Rare
5. Mega Rare

Jede höhere Seltenheit besitzt gegenüber der vorherigen genau ein zusätzliches, für ihre Rolle relevantes Attribut. Attribute stammen aus einer rollenabhängigen Auswahl, zum Beispiel:

- Ertrag,
- Zyklusgeschwindigkeit,
- Durchsatz,
- Lagerkapazität,
- Transportkapazität,
- Fahrgeschwindigkeit,
- Verkaufspreis,
- Einwohnerbonus,
- Tagesbonus,
- Bauzeitbonus.

Worker besitzen außerdem eigenes Level und eine Stufe/Rangentwicklung. Deren Effekte und Kosten sind getrennt von Gebäudestufen definiert.

Der Spin:

- verbraucht exakt eine Karte,
- verwendet einen injizierbaren, seedbaren Zufallsdienst,
- besitzt in Inhaltsdaten definierte Wahrscheinlichkeiten,
- ist deterministisch testbar,
- zeigt Ergebnis, Rolle, Seltenheit und Attribute klar,
- enthält keine Echtgeldkäufe und keine vorgetäuschte Online-Gacha-Logik.

---

## 7. Quest-, Dialog- und Freischaltungsfluss

Implementiere Quests als datengetriebene Definitionen plus serialisierten Laufzeitstatus. Bedingungen und Belohnungen müssen komponierbar sein. Keine Quest darf nur durch Stringvergleich in UI-Code funktionieren.

### 7.1 Minimaler Hauptpfad

Der Hauptpfad muss mindestens diese nachweisbare Reihenfolge abdecken:

1. Intro ansehen oder bewusst überspringen.
2. Spielernamen eingeben.
3. Von Rocko 1.000 Geld und 50 Grinder erhalten.
4. Ersten Farmbewohner ansprechen und einen Anbauplatz aus der Ruine aktivieren.
5. Eine Ernte manuell auslösen und einsammeln.
6. Einen Start-Worker erhalten und als passenden Manager zuweisen.
7. Farm-Verarbeitung aktivieren und einen realen Stau beziehungsweise dessen Vermeidung kennenlernen.
8. Farm-Lager aktivieren, Fahrer zuweisen und eine Lieferung zur Stadt abschließen.
9. Farm-End-to-End-Automatisierung nachweisen und Stadt freischalten.
10. Stadt-Lager und ersten Coffee-Shop aktivieren.
11. Blüten ausliefern und erstes Geld durch Verkauf verdienen.
12. Rathaus öffnen, Statistiken ansehen und einen Worker-Karten-Spin durchführen.
13. Siedlung freischalten.
14. Auf getrennten Bauplätzen ein Wohnhaus und einen Park errichten.
15. Einwohner-, Karten- und VIP-Quest-Funktion kennenlernen.
16. Industriegebiet freischalten.
17. Butterfabrik, Supermarkt und eine Bäckerei aktivieren.
18. Butter produzieren, in der Bäckerei verarbeiten und Geld verdienen.
19. Einwohnerabhängiges Supermarkt-Einkommen und täglichen Grinderbonus sichtbar machen.
20. Industrie automatisieren und Freeplay freischalten.

Questbelohnungen müssen den Hauptpfad ohne Premiumausgabe ermöglichen. Der Spieler darf nicht durch falsche Bauentscheidungen, volle Lager oder einen unpassenden Worker dauerhaft blockiert werden. Baue verständliche Hinweise und sichere Recovery-Wege ein.

### 7.2 VIP-Quests

Parks generieren nach den entsprechenden Freischaltungen wiederholbare VIP-Quests. Für die erste Version genügen datengetriebene Kombinationen aus sinnvollen Zielen, etwa:

- bestimmte Ressource produzieren oder verkaufen,
- Gebäudelevel erreichen,
- Engpass durch Ausbau lösen,
- Worker zuweisen oder verbessern,
- Einwohnerzahl erhöhen,
- Liefermenge abschließen.

Die Auswahl muss nur erreichbare Ziele berücksichtigen, Duplikate begrenzen, deterministisch testbar sein und skalierte, aber nicht exploitable Belohnungen besitzen.

---

## 8. Simulation, Ressourcen und Logistik

### 8.1 Domainmodell

Trenne die reine Simulation von Unity-Views. Die zentrale Spielökonomie muss in EditMode-Tests ohne Szene, Kamera oder MonoBehaviour laufen können.

Mindestens erforderliche Ressourcen:

- rohe Blüten,
- veredelte Blüten,
- Cannabis-Butter,
- verkaufsfertige Backwaren,
- Geld,
- Grinder,
- Worker-Karten,
- Einwohner.

Führe keine zusätzliche Pflichtwährung ein, sofern sie für die beschriebene Schleife nicht zwingend nötig ist.

### 8.2 Tick- und Offline-Simulation

- Verwende einen zentralen, injizierbaren Simulations-Takt statt vieler unabhängiger `Update`-Schleifen.
- Simulationsergebnisse dürfen nicht von der Render-Framerate abhängen.
- Ressourcenfluss wird aggregiert berechnet; erzeuge keine GameObjects pro Ressourceneinheit.
- Förderbandobjekte, Fahrzeuge und Pakete sind gepoolte, kosmetische Darstellungen des Domainzustands.
- Offline-Fortschritt verwendet dieselbe fachliche Logik oder eine mathematisch äquivalente, getestete Aggregation.
- Begrenze die maximale Offline-Dauer in Tuningdaten und zeige die Zusammenfassung beim Laden.
- Negative Zeitdifferenzen, Uhr-Rückstellung, sehr große Pausen und Zeitüberläufe dürfen keine Belohnungsexploits erzeugen.

### 8.3 Massenerhaltung und Engpässe

- Jede Transformation besitzt definierte Input-/Output-Verhältnisse.
- Begrenzte Kapazität oder Durchsatz erzeugt Backlog beziehungsweise stoppt Upstream-Produktion nachvollziehbar.
- Keine Ressource verschwindet wegen eines vollen Ziels.
- Keine Ressource wird bei Speichern, Laden, Szenenwechsel oder Offline-Simulation dupliziert.
- UI und Welt zeigen den aktuell stärksten Engpass verständlich an.

---

## 9. Technische Architektur

### 9.1 Schichten und Abhängigkeiten

Baue eine klare, austauschbare Struktur. Ein sinnvolles Ziel ist:

- `Weedtopia.Domain`: reine C#-Regeln und Zustände ohne UnityEngine-Abhängigkeit,
- `Weedtopia.Application`: Use Cases, Commands, Quest-/Progressionsorchestrierung und Ports,
- `Weedtopia.Content`: Unity-Definitionen und Mapping von ScriptableObjects in Domainwerte,
- `Weedtopia.Persistence`: Save DTOs, Versionierung, Migration und lokale Implementierung,
- `Weedtopia.Unity`: Bootstrap, Szenenbindung, Welt-Presenter, Input, Kamera, Zeitadapter,
- `Weedtopia.UI`: Menüs, HUD, Dialoge, Panels und ViewModels/Presenter,
- bestehendes `Weedtopia.Buildings.Presentation`: Katalog und Wrapper bleiben kompatibel,
- passende Editor- und Test-Assemblies.

Die konkreten Ordner und Assembly-Namen dürfen leicht abweichen, wenn die Abhängigkeitsrichtung gleich klar bleibt. Vermeide zyklische Referenzen, globale Service-Locator und unkontrollierte statische Singletons.

### 9.2 Komposition

- Verwende einen expliziten, in einer Bootstrap-Szene serialisierten Game-Installer/Composition Root.
- Abhängigkeiten werden über Konstruktoren in reinen C#-Klassen oder explizite `Initialize`-Methoden in Unity-Adaptern übergeben.
- Statische Szenenobjekte wie Kamera-Rig, Zonenwurzeln, Bauplätze, Canvas, EventSystem, Licht, Questgeber und feste Gebäude werden im Editor authoriert und sind in Hierarchie/Prefab sichtbar.
- Erzeuge statische Hierarchie nicht bei jedem Start per Code.
- Laufzeitinstanziierung ist für tatsächlich dynamische Dinge erlaubt: gewählte Bauplatzinhalte, aktive visuelle Gebäudestufe, gepoolte VFX/Transportdarstellung und dynamische UI-Listen.
- Wenn ein Aufbau wiederverwendbar ist, erstelle ein Prefab statt ihn mehrfach lose zu duplizieren.

### 9.3 IDs und Daten

- Nutze stabile, serialisierbare IDs, keine `GetInstanceID`-Werte und keine Szenennamen als Fachschlüssel.
- Erstelle Definitionen für Gebäude, Upgrade-Kurven, Ressourcen, Worker-Rollen, Attribute, Quests, Dialoge, Zonen und Balancing.
- Definitionen werden beim Start und in Editor-Tests auf doppelte IDs, ungültige Referenzen, nicht monotone Kurven und fehlende Inhalte geprüft.
- Harte Abhängigkeiten werden serialisiert und in Validierungstools sichtbar gemacht.
- Vermeide `Resources.Load` als unsichtbaren Standardmechanismus. Nutze direkte Katalogreferenzen beziehungsweise klar definierte Asset-Sammlungen.

### 9.4 Erweiterbarkeit

Kernsysteme dürfen nicht auf genau einen Anbauplatz, Coffee-Shop, Bauplatz oder eine Bäckerei hart codiert sein. Burning Hills bekommt konkrete authorierte Instanzen, die Systeme arbeiten jedoch über Definitionen und stabile Instanz-IDs.

Neue Städte sollen später durch neue Inhaltsdefinitionen, Szenen-/Prefab-Referenzen und Tuningdaten ergänzt werden können, nicht durch Kopieren der gesamten Spiellogik.

---

## 10. Szenen, Welt und Kamera

Erstelle mindestens:

- eine kleine Bootstrap-/Startszene,
- eine eigentliche Burning-Hills-Spielszene,
- bei echtem Nutzen optionale additive Zonenszenen für Farm, Stadt, Siedlung und Industrie.

Verwende keine Szenenaufteilung nur um der Aufteilung willen. Entscheidend sind stabile Referenzen, kurze Ladewege und authorierbare Hierarchie.

Die Spielwelt folgt den fünf Skizzen als räumlicher Leitidee:

- Farm: Förderbandkette zwischen Anbau, Verarbeitung und Lager,
- Stadt: Rathaus, Stadt-Lager und mehrere Coffee-Shop-Plätze,
- Siedlung: übersichtliches Raster aus wählbaren Bauplätzen,
- Industrie: Butterfabrik, Supermarkt und Bäckerei-Plätze,
- Gesamtübersicht: klare räumliche Verbindung der Zonen.

Kamera:

- isometrisch beziehungsweise orthografisch mit dem abgestimmten festen Gebäudeblickwinkel,
- weiches Schwenken per Maus, Tastatur und Touch,
- Zoom mit Rad, Tasten und Pinch,
- harte, authorierte Weltgrenzen,
- Fokusfahrt zu Questziel/Gebäude,
- kein Drehen, wenn dies die vorgerenderten Gebäudeansichten entwertet,
- Eingaben ausschließlich über das neue Input System.

Interaktion:

- Gebäude/Bauplätze mit Pointer oder Touch auswählen,
- UI-Klicks dürfen nicht gleichzeitig die Welt treffen,
- Auswahlumriss beziehungsweise klarer Auswahlzustand,
- Kamera und Auswahl bleiben auch bei Pause, Dialogen und Panels konsistent.

---

## 11. Gebäudevisuals und vorhandene Kunstpipeline

### 11.1 Bestehende Verträge erhalten

Erhalte:

- `BuildingVisualCatalog` als visuellen Lookup nach `buildingId` und Level,
- alle 251 Levelband-Zuordnungen,
- die Wrapper-Namen und stabilen IDs,
- `VisualRoot`, `InteractionAnchor`, `WorkerAnchor`, `DeliveryInputAnchor`, `DeliveryOutputAnchor`, `VfxAnchor` und `ColliderRoot`,
- manuell gesetzte Offsets, Zuordnungen und Quellassets,
- alle akzeptierten Bilder, Prompts, FBX-, Blender- und Diagnoseartefakte.

Gameplay kennt keine konkreten Sprite- oder FBX-Dateien. Es fragt eine Präsentationsschnittstelle nach dem aktuellen visuellen Prefab. Ein Levelwechsel aktualisiert ausschließlich die View und verändert den Domainzustand nicht erneut.

### 11.2 Offenes 2D-Gate

Vor einer Massenumstellung der 251 Produktions-Wrapper:

1. aktuellen Unity-Zustand prüfen,
2. isolierten Rathaus-2D-Prototyp erzeugen beziehungsweise reparieren,
3. Sprite-Import, Pivot, Pixels-per-Unit, Maßstab, Sorting, Collider und Kamerawinkel prüfen,
4. ihn in einer kleinen tatsächlichen Game-View-Testszene bei mehreren Zoomstufen zeigen,
5. einen frühen, mittleren und finalen Zustand einer weiteren Gebäudefamilie als Stichprobe vorbereiten,
6. Game-View-Nachweise erzeugen,
7. die ausdrückliche visuelle Freigabe des Benutzers abwarten, bevor produktive Wrapper/Katalogreferenzen massenhaft auf Sprites umgestellt werden.

Dieses visuelle Gate blockiert **nicht** die Entwicklung der fachlichen Spielsysteme. Verwende bis zur Freigabe vorhandene Wrapper oder klar erkennbare Entwicklungs-Placeholder über dieselbe Präsentationsschnittstelle. Ändere nicht heimlich den produktiven Kunstbestand.

Nach Freigabe:

- implementiere eine idempotente, referenzerhaltende Konvertierung aller akzeptierten Bilder,
- stelle die Wrapper auf authorierte Sprite-Visuals um,
- erhalte Anker und Colliders,
- validiere alle 251 Zustände,
- prüfe Speicherverbrauch, Zoomschärfe, Pivot, Sorting und Game View,
- aktualisiere `PROGRESS.json` nur mit tatsächlich bestandener Evidenz.

### 11.3 Performance

- Kein Laden aller 251 Texturen in eine Szene, wenn nur wenige Stufen sichtbar sind.
- Prüfe Importgrößen, Kompression, Mipmaps, Sprite-Mesh und Materialanzahl für die Zielplattform.
- Nutze Pooling für wiederkehrende Transport-/VFX-Objekte.
- Erzeuge keine individuellen Materialien pro Gebäudeinstanz, wenn MaterialPropertyBlock oder gemeinsame Materialien genügen.
- Profiler-Aussagen nur nach tatsächlicher Messung.

---

## 12. Benutzeroberfläche und Bedienung

Verwende uGUI/TextMeshPro oder eine gleichwertig wartbare Unity-UI-Lösung. Vermische nicht mehrere UI-Systeme ohne triftigen Grund.

Mindestens erforderlich:

- Start/Weiter/Neues Spiel,
- Namenseingabe,
- Intro- und Questdialoge mit Rocko und Bewohnern,
- HUD für Geld, Grinder, aktuelle Quest und Engpass,
- Navigation/Fokus zu Farm, Stadt, Siedlung und Industrie,
- Gebäude-Detailpanel mit Level, Tier, Produktion, Kapazität, Durchsatz, Upgrade-Kosten und Bauzeit,
- ausdrücklicher Grinder-Bestätigungsdialog,
- Worker-Inventar, Filter, Detailansicht und Slot-Zuweisung,
- Rathausansicht mit Statistiken und Worker-Spin,
- Bauplatz-Auswahl Wohnhaus/Park,
- Questjournal einschließlich VIP-Quests,
- Offline-Ertragsdialog,
- Pause/Einstellungen mit Audio-, Text-/UI-Skalierungs- und Speicheroptionen,
- verständliche Fehler-, Leer- und Gesperrtzustände.

UX-Regeln:

- UI-Texte nicht in Gameplay-Code hart codieren; liefere deutsche Inhalte und eine erweiterbare Lokalisierungsstruktur.
- Lesbare Schriftgrößen, ausreichender Kontrast, Safe Areas und Landscape-Auflösungen berücksichtigen.
- Geld und Grinder visuell eindeutig unterscheiden.
- Premium-Ausgabe braucht immer eine eindeutige Bestätigung.
- Gesperrte Aktionen erklären Ursache und nächsten sinnvollen Schritt.
- Produktionsstau und freier Durchsatz müssen ohne Tabellenstudium erkennbar sein.
- Keine Fake-Buttons, toten Menüpunkte oder behaupteten Online-Funktionen.

Prüfe mindestens 1920×1080 und 1366×768 sowie einen repräsentativen breiteren Landscape-Aspekt. Touch-Bedienung ist gesondert zu testen und darf nicht allein aus Maus-Simulation als bestanden gelten.

---

## 13. Speichern, Laden und Wiederherstellung

Implementiere lokale, versionierte Spielstände mit:

- Spielername,
- Ressourcen und Währungen,
- Zeitstempeln,
- Stadt-/Zonenfreischaltung,
- Gebäudeinstanzen, Level, Bauzustand und gewählte Bauplatztypen,
- Produktionspuffern und Lieferungen,
- Workern, Karten, Attributen und Zuweisungen,
- Quest- und Dialogfortschritt,
- Tagesbelohnungsstatus,
- Statistiken,
- Inhalts-/Save-Schema-Version.

Anforderungen:

- atomisches Schreiben über temporäre Datei und sichere Ersetzung,
- Backup des letzten validen Stands,
- Validierung vor Übernahme,
- verständliche Recovery bei beschädigter Hauptdatei,
- Migration mindestens von einer künstlichen älteren Testversion zur aktuellen Version,
- kein Speichern von direkten Unity-Objektreferenzen in Domain-Saves,
- keine Abhängigkeit von Szene-Lade-Reihenfolge,
- manuelles Speichern plus Autosave bei wichtigen Transaktionen und beim Verlassen/Pausieren,
- kein häufiges synchrones Schreiben pro Simulations-Tick.

Ein neues Spiel, Weiterladen, beschädigter Save, veralteter Save und mehrfaches Szenenneuladen müssen in Tests abgedeckt sein.

---

## 14. Statistiken und Highscore-Grenze

Das Rathaus zeigt lokale, korrekt berechnete Statistiken, mindestens:

- Gesamtgeld verdient,
- aktuell verfügbares Geld und Grinder,
- gesamte rohe/veredelte Produktion,
- gesamte Verkäufe über Coffee-Shops und Bäckereien,
- Einwohnerzahl,
- abgeschlossene Lieferungen,
- abgeschlossene Haupt- und VIP-Quests,
- erhaltene und eingesetzte Worker,
- höchstes Gebäudelevel,
- Spielzeit und Freeplay-Status.

Bereite eine austauschbare Highscore-Schnittstelle vor, beispielsweise `ILeaderboardService`, und liefere:

- eine funktionsfähige lokale Statistik-/Bestenlistenimplementierung für Entwicklung und Tests,
- einen klaren deaktivierten Online-Adapter, der ehrlich „Online-Rangliste noch nicht verbunden“ meldet,
- dokumentierte DTOs und Trust-Boundaries für eine spätere serverautorisierte Variante.

Nicht Teil dieses Auftrags ohne gesonderte Freigabe und Zugangsdaten:

- Supabase/Firebase/PlayFab oder ein anderer echter Backendanbieter,
- Produktivdatenbank,
- Authentifizierung,
- Deployment,
- globale Ranglistenbehauptung,
- Secrets oder Tokens.

Führe keine Netzwerkaufrufe zu erfundenen Endpunkten aus. Ein globaler Highscore darf erst als fertig gemeldet werden, wenn Backend, Authentifizierung, serverseitige Validierung, Missbrauchsschutz und reale End-to-End-Tests separat umgesetzt wurden.

---

## 15. Editor-Werkzeuge und explizite Installation

Erstelle ein explizites, idempotentes Unity-Menü, zum Beispiel:

`Weedtopia/Setup/Create or Repair Game Scaffold`

Es darf:

- Ordner, Datenassets, Prefabs und Szenengerüste anlegen,
- fehlende Komponenten und Referenzen reparieren,
- Build Settings aktualisieren,
- Inhaltsdefinitionen validieren,
- einen nachvollziehbaren Bericht schreiben.

Es darf nicht:

- bei Domain Reload automatisch fremde Szenen verändern,
- manuelle Referenzen, Offsets oder Inhalte überschreiben,
- vorhandene Assets blind löschen,
- bei jedem Editorstart ungefragt laufen,
- den offenen Editor beenden oder eine zweite Instanz starten.

Der zweite Lauf ohne fachliche Änderung muss idempotent sein. Teste dies über Asset-/Referenzinventar und geeignete Hashes; unvermeidliche Unity-Metadatenänderungen sind getrennt zu erklären.

---

## 16. Umsetzungsreihenfolge

Arbeite in diesen Meilensteinen. Jeder Meilenstein endet mit relevanten Tests, aktualisierter Statusdatei und einem lokal spielbaren oder fachlich abgeschlossenen Zwischenstand.

### M0 – Audit und sichere Basis

- Projekt, Anweisungen, Lockfile/Prozesse, Assets, Szenen, Assemblies und aktuelle Berichte prüfen.
- Fremd-/Benutzeränderungen dokumentieren und erhalten.
- keine zweite Unity-Instanz starten.
- Zielordner, Assembly-Abhängigkeiten, ID-Schema und Installationsweg festlegen.
- bestehende Art-Pipeline-Tests unverändert weiter bestehen lassen.

### M1 – Reine Domain-Simulation

- skalierbarer Zahlenwert,
- Ressourcen, Speicher, Konverter, Produzenten, Transporte und Engpässe,
- Gebäudelevel und visuelle Tierauflösung,
- Worker, Rollen, Attribute, Seltenheiten und Zuweisungen,
- Upgrade-, Bauzeit- und Premiumbestätigungsregeln,
- deterministische Uhr und Zufallsquelle,
- umfassende EditMode-Tests.

### M2 – Progression, Quests und Balancing

- Hauptquestkette,
- Zonenfreischaltung,
- VIP-Quests,
- Tagesbelohnungen,
- belastbare Tuningdaten,
- automatisierter Fortschrittssimulator, der den Hauptpfad ohne Grinder-Ausgabe und ohne Deadlock erreicht.

### M3 – Persistenz und Offline-Fortschritt

- versioniertes Save-Schema,
- atomisches Schreiben, Backup, Recovery und Migration,
- Offline-Simulation,
- EditMode- und PlayMode-Tests.

### M4 – Unity-Bootstrap, Welt, Kamera und Input

- authorierte Szenen und Prefabs,
- Composition Root,
- Kamera-Rig und Steuerung,
- Zonen, Bauplätze, Questgeber und Interaktionsfluss,
- vorhandenen Gebäudekatalog anbinden,
- Scene-/Prefab-Validierung.

### M5 – Farm als vollständiger vertikaler Slice

- Intro, Name, Rocko, Startressourcen,
- Farmquests,
- manuelle Ernte, Managerautomation, Verarbeitung, Lager, Fahrer und Stadttransport,
- UI, Speichern/Laden und audiovisuelle Rückmeldung für diesen Fluss,
- PlayMode-End-to-End-Test.

### M6 – Stadt und Rathaus

- Stadt-Lager, Coffee-Shops, Geldfluss,
- Stadtquests,
- Rathausstatistiken,
- Worker-Inventar und Spin,
- lokale Bestenliste/Online-deaktiviert-Zustand.

### M7 – Siedlung

- Bauplatzwahl,
- Wohnhäuser, Parks, Einwohner, Karten und VIP-Quests,
- Tageswechsel und Save-Roundtrip.

### M8 – Industrie und Freeplay

- Butterfabrik, Supermarkt und Bäckereien,
- vollständige Industriekette,
- Grindertagesbonus,
- Abschlussquests und Freeplay,
- weitere-Städte-Erweiterungspunkt ohne leeren Fake-Content.

### M9 – Gebäude-2D-/2.5D-Gate und visueller Ausbau

- Prototypen und Game-View-Nachweis vorbereiten,
- Benutzerfreigabe für produktive Massenumstellung einholen,
- nach Freigabe idempotent auf alle 251 Slots anwenden,
- alle Katalog-, Import-, Pivot-, Sorting-, Collider- und Speicherprüfungen ausführen.

M9 darf teilweise parallel zur fachlichen Umsetzung vorbereitet werden, die produktive Massenänderung bleibt jedoch freigabepflichtig.

### M10 – Politur und vollständige lokale Abnahme

- Onboarding, Fehlermeldungen, UI-Skalierung, Audio-Hooks, VFX, Auswahlfeedback und Performance,
- alle EditMode-/PlayMode-Tests,
- frischer Installations-/Repair-Lauf plus Idempotenzlauf,
- kompletter manueller Hauptpfad in Game View,
- Save/Load/Offline-/Tageswechsel- und Recovery-Checks,
- ehrlicher Abschlussbericht mit offenen externen/manuellen Gates.

---

## 17. Verbindliche Tests

### 17.1 EditMode

Mindestens abdecken:

- alle Levelgrenzen von 0 bis 5000 und die 251 Katalogzuordnungen,
- monotone Upgrade-, Ertrags-, Kapazitäts- und Zeitkurven,
- skalierbare Zahl: Rechnen, Vergleichen, Rundung, Format und Serialisierung,
- Geldkauf, fehlendes Geld, Grinderangebot, Bestätigung, Abbruch und unzureichende Grinder,
- Produktion und Massenerhaltung bei freien und vollen Puffern,
- definierte Engpässe in Verarbeitung, Lager und Transport,
- manuelle Ernte versus Managerautomation,
- Fahrerplätze und Lieferzeiten,
- kompatible und inkompatible Worker-Zuweisung,
- Seltenheit und exakt zusätzliche relevante Attribute,
- deterministischer Worker-Spin,
- Questbedingungen, Reihenfolge, Wiederaufnahme und Belohnung genau einmal,
- Bauplatzwahl und Zonenfreischaltung,
- Tagesbelohnung einmal pro Tag, mehrere Tage, Uhr-Rückstellung und Grenzzeit,
- VIP-Questgenerierung nur mit erreichbaren Zielen,
- Save-Roundtrip, Migration, beschädigte Datei und Backup-Recovery,
- Offline-Fortschritt und maximale Offline-Dauer,
- Statistikakkumulation ohne Doppelzählung,
- Definitionen ohne doppelte IDs oder fehlende Referenzen,
- Fortschrittssimulation bis Freeplay ohne Premiumzwang oder Deadlock,
- bestehende Art-Pipeline-Tests.

### 17.2 PlayMode

Mindestens abdecken:

- Bootstrap lädt genau eine konsistente Spielsession,
- Neues Spiel: Name, Startressourcen und erste Quest,
- Farm-End-to-End-Fluss bis Stadtfreischaltung,
- Gebäudeauswahl, Panel und Upgrade,
- Managerzuweisung schaltet Automatik korrekt,
- Save, Szenenneuladen und Continue erhalten Zustand,
- Stadtverkauf erzeugt Geld,
- Wohnhaus/Park-Wahl bleibt erhalten,
- Industriekette erreicht Verkauf und Freeplay,
- UI blockiert World-Clicks,
- Pausieren/Weiterlaufen beeinflusst Simulation korrekt,
- kein Missing-Script, keine fehlende Pflichtreferenz und keine unbehandelte Exception.

### 17.3 Editor-/Assetvalidierung

Mindestens abdecken:

- alle erforderlichen Szenen in Build Settings,
- genau ein EventSystem im aktiven UI-Kontext,
- gültige Bootstrap-/Installer-Referenzen,
- stabile Gebäude- und Instanz-IDs,
- alle Wrapper-Anker,
- Katalog deckt alle definierten Levelbereiche ohne Lücke/Überlappung ab,
- keine produktive Wrapper-Änderung vor visuellem Gate,
- nach Freigabe: 251 gültige Sprite-/Wrapper-Zuordnungen,
- zweiter Scaffold-Lauf zerstört keine authorierten Daten.

---

## 18. Abnahme und Beweisführung

Ein Compilerlauf allein ist keine Abnahme. Trenne die Nachweise klar:

1. C#-Quellkompilierung,
2. Unity-Import und Domain Reload,
3. EditMode-Tests,
4. PlayMode-Tests,
5. Szenen-/Prefab-/Assetvalidierung,
6. tatsächliche Game-View-Ausführung,
7. manuelle visuelle Gebäudeabnahme,
8. Touch-/Zielgerätetest, falls ausgeführt,
9. Online-/Backendtest, falls später separat beauftragt.

Wenn bereits ein Unity Editor offen ist:

- keine zweite Instanz starten,
- den Benutzereditor nicht beenden,
- vorhandene file-basierte Request-/Result-Werkzeuge verwenden, falls sie zuverlässig vorhanden sind,
- ein fehlendes Resultat, ein Lockfile oder ein Prozessstatus nicht als „bestanden“ werten.

Erzeuge maschinenlesbare Resultate unter `Docs/Implementation/Validation/` und einen verständlichen Bericht unter `Docs/Implementation/VALIDATION_REPORT.md`. Nenne pro Gate:

- Zeitpunkt,
- genaue Unity-/Projektversion,
- ausgeführten Test,
- Ergebnis und Zähler,
- Pfad zur Roh-Evidenz,
- offene Einschränkungen.

Erzeuge für die visuelle Abnahme Screenshots von:

- Intro/Rocko,
- Farm im manuellen und automatisierten Zustand,
- sichtbarem Produktionsstau,
- Stadt/Rathaus/Coffee-Shop,
- Siedlung mit Wohnhaus und Park,
- Industriefluss,
- Gebäude-Detailpanel,
- Worker-Spin,
- Freeplay-Übersicht,
- 2D-Prototypen bei mehreren Zoomstufen.

Screenshots beweisen Darstellung, nicht automatisch Logik, Touch oder Performance.

---

## 19. Dokumentation und Lieferartefakte

Lege beziehungsweise pflege:

- `Docs/Implementation/IMPLEMENTATION_STATUS.md`
- `Docs/GameDesign/WEEDTOPIA_GDD.md`
- `Docs/GameDesign/ECONOMY_AND_BALANCE.md`
- `Docs/Architecture/GAMEPLAY_ARCHITECTURE.md`
- `Docs/Architecture/SAVE_SCHEMA.md`
- `Docs/Content/MAIN_QUEST_FLOW.md`
- `Docs/Content/BUILDING_AND_WORKER_MATRIX.csv`
- `Docs/Implementation/VALIDATION_REPORT.md`
- `Docs/Implementation/Validation/` für maschinenlesbare Resultate

Dokumentation ersetzt keine Implementierung. Sie soll getroffene Annahmen, Formeln, Erweiterungspunkte und echte Prüfergebnisse festhalten.

Der Abschlussbericht muss enthalten:

- was tatsächlich spielbar ist,
- welche Szenen und Systeme hinzugekommen sind,
- welche Tests aktuell bestanden sind,
- welche visuellen Freigaben noch offen sind,
- welche Plattform-/Touch-/Audio-/Performance-Gates nicht ausgeführt wurden,
- dass der globale Highscore ohne Backend noch nicht produktiv ist,
- welche wenigen, konkreten nächsten manuellen Schritte der Benutzer ausführen muss.

---

## 20. Sicherheits- und Erhaltungsregeln

- Bestehende Benutzerarbeit, manuelle Prefabzuweisungen, Offsets, Quelldateien und akzeptierte Kunst erhalten.
- Keine destruktiven Resets, Bereinigungen oder Massenlöschungen.
- Keine Commits, Pushes, Deployments oder Produktivserviceänderungen ohne ausdrücklichen Auftrag.
- Keine Secrets, Tokens oder Zugangsdaten erzeugen oder speichern.
- Keine neuen kostenpflichtigen Services oder Assets voraussetzen.
- Keine automatische Paketinstallation, wenn eine vorhandene Unity-Funktion genügt; notwendige neue Abhängigkeiten vorher begründen.
- Keine statischen Weltobjekte im Laufzeitcode zusammenbauen, wenn sie als Szene oder Prefab authoriert sein sollten.
- Keine parallele zweite Unity-Instanz bei vorhandenem Lock/Editor.
- Alte Meshy-/FBX-/Blender-Quellen nicht löschen; sie sind Archiv und Diagnosematerial.
- Unbenutzte neue Assets nicht lose im Projekt verteilen; in klar benannte Quarantäne-/Archivordner verschieben und dokumentieren.
- Keine Erfolgsmeldung aufgrund von Dateiexistenz allein.

---

## 21. Entscheidungen, die du selbst treffen sollst

Frage nicht bei jeder Kleinigkeit nach. Triff konservative, reversible und dokumentierte Entscheidungen zu:

- konkreten Startwerten für Ertrag, Kapazität, Durchsatz und Bauzeiten,
- Formeln und Rundung,
- Anzahl der authorierten Burning-Hills-Instanzen innerhalb der Skizzenlogik,
- konkreten deutschen Dialogformulierungen,
- UI-Layoutdetails,
- VFX-/Audio-Placeholdern,
- internen Klassen- und Ordnernamen,
- kleinen Recovery- und Komfortfunktionen.

Dabei gelten:

- Tutorial ohne Grinderverbrauch abschließbar,
- keine Sackgasse,
- Werte datengetrieben,
- Formeln dokumentiert,
- Änderungen durch Tests abgesichert,
- keine Abweichung von Kernfantasie oder Ressourcenfluss.

Halte nur dann an und frage gezielt nach, wenn:

- die produktive Massenumstellung auf 2D-Sprites die ausdrückliche visuelle Freigabe benötigt,
- eine externe Plattform, ein Backend oder Zugangsdaten notwendig würden,
- eine destruktive oder nicht reversible Entscheidung nötig wäre,
- der tatsächliche Projektstand dem Auftrag fundamental widerspricht,
- ein bereits geöffneter Unity Editor eine notwendige Prüfung dauerhaft blockiert und kein sicherer file-basierter Weg existiert.

Arbeite in allen anderen Fällen weiter.

---

## 22. Definition of Done

Die erste Burning-Hills-Version ist erst fertig, wenn alle folgenden Punkte wahr und belegt sind:

- Der Spieler kann ein neues Spiel starten, einen Namen wählen und die korrekten Startressourcen erhalten.
- Der gesamte Hauptpfad Farm -> Stadt -> Siedlung -> Industrie -> Freeplay ist in Game View spielbar.
- Jede Zone besitzt ihre beschriebenen Produktions-, Ausbau-, Worker- und Questfunktionen.
- Manuelle Arbeit und Managerautomation unterscheiden sich fachlich und sichtbar.
- Lager, Durchsatz, Fahrer und Lieferzeiten bilden echte Engpässe ohne Ressourcenverlust.
- Geld, Grinder, Ausbau, Bauzeit und explizite Premiumbestätigung funktionieren korrekt.
- Alle Gebäudelevel 0–5000 lösen zuverlässig das richtige visuelle Tier auf.
- Worker-Karten, Spin, Seltenheiten, Attribute, Level und Zuweisungen funktionieren.
- Wohnhäuser, Parks und Supermarkt vergeben Tagesbelohnungen ohne Doppelabholung.
- Spielstände sind versioniert, wiederherstellbar und über Szenen-/Programmneustart stabil.
- Offline-Fortschritt ist begrenzt, nachvollziehbar und gegen Uhr-Rückstellung abgesichert.
- Rathausstatistiken sind korrekt; Online-Highscore wird ehrlich als noch nicht verbunden dargestellt.
- Szenen, Prefabs und feste Referenzen sind im Editor sichtbar authoriert.
- Der Installer ist explizit, idempotent und erhaltend.
- Alle aktuellen EditMode-, PlayMode- und Assetvalidierungen bestehen oder verbleibende externe/manuelle Gates sind ausdrücklich als offen dokumentiert.
- Es gibt keine unbehandelte Exception, kein Missing Script und keine fehlende Pflichtreferenz im geprüften Hauptpfad.
- Die visuelle Produktion respektiert das 2D-Freigabegate und behauptet keine Freigabe, die nicht erteilt wurde.
- `VALIDATION_REPORT.md` nennt ehrlich alle nicht geprüften Bereiche.

Beginne jetzt mit M0, prüfe den realen Zustand, schütze vorhandene Arbeit und implementiere anschließend den nächsten noch nicht erfüllten Meilenstein. Fahre nach jedem bestandenen Meilenstein mit dem nächsten fort. Beende die Arbeit nicht mit einem Plan, solange sichere Implementierung, Prüfung oder Fehlerbehebung möglich ist.

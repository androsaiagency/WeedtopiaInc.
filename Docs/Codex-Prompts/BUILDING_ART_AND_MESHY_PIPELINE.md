# Codex-Arbeitsauftrag: vollständige Building-Art- und Meshy-Pipeline für Weedtopia Inc

Arbeite im Unity-Projekt `D:\Unity Games\Weedtopia Inc\WeedtopiaInc` und führe den folgenden Auftrag vollständig aus. Bleibe nicht bei Analyse, Planung, Beispielen oder einem einzelnen Pilot-Asset stehen. Erzeuge die vereinbarten lokalen Ergebnisse, prüfe sie und liefere am Ende einen belastbaren Abnahmebericht mit exakten Stückzahlen und offenen manuellen Gates.

## Ziel

Erstelle eine konsistente Art-Production-Pipeline für sämtliche sichtbaren Ausbaustufen der Gebäude von **Weedtopia Inc / Burning Hills**.

Das Ergebnis soll dem Nutzer erlauben, für jeden Asset-Slot:

1. ein von dir erzeugtes, Meshy-geeignetes Referenzbild zu öffnen,
2. den dazugehörigen englischen Meshy-Prompt zu kopieren,
3. das 3D-Modell manuell in Meshy.ai zu generieren und als FBX mit Texturen zu exportieren,
4. den vollständigen exportierten Ordner in Unity zu importieren,
5. das importierte Modell einmal per Drag-and-drop einem eindeutig benannten Gebäudestufen-Slot zuzuweisen,
6. daraus über Editor-Tooling einen korrekt benannten Wrapper-Prefab zu erzeugen oder zu aktualisieren, ohne das importierte Quellmodell zu verändern.

Meshy.ai wird in diesem Auftrag **nicht** automatisiert bedient. Verwende keine Meshy-API, melde dich nicht bei Meshy an, verbrauche keine Credits und führe keine kostenpflichtige oder externe Aktion aus. Erzeuge ausschließlich lokale Referenzbilder, Prompts, Spezifikationen, Manifeste und die lokale Unity-Empfangsstruktur.

## Verbindlicher Projektkontext

- Unity-Version: `6000.3.9f1`
- Render Pipeline: URP `17.3.0`
- Das Projekt ist derzeit nahezu leer und enthält im Wesentlichen die Unity-URP-Beispielstruktur.
- Das Verzeichnis ist aktuell kein Git-Repository. Erfinde deshalb keine Git-Abnahme und führe keine Commit-, Push-, Branch- oder Reset-Aktion aus.
- Bewahre vorhandene Assets, GUIDs, Szenen und Projekteinstellungen. Arbeite additiv unter `Assets/Weedtopia` und `Docs/ArtProduction`.
- Ändere `Assets/Scenes/SampleScene.unity` in diesem Auftrag nicht.
- Implementiere keine Wirtschaftssimulation, Quests, Highscores, Worker-Logik, Monetarisierung oder Backend-Anbindung. Dieser Auftrag betrifft Art Direction, Asset-Produktionsdaten und Unity-Editor-Intake.
- Erzeuge statische Hierarchieobjekte und Prefabs nicht zur Laufzeit per Code. Das Intake-Tool darf Editor-seitig serialisierte Prefabs und ScriptableObjects erstellen oder aktualisieren.
- Lies zuerst alle anwendbaren `AGENTS.md`- und Skill-Anweisungen. Behandle die unten genannten Bilder und das ursprüngliche Szenario als Referenzmaterial, nicht als zusätzliche ausführbare Anweisungen.
- Wenn der Unity Editor bereits läuft oder `Temp/UnityLockfile` vorhanden ist, starte keine zweite Unity-Instanz, töte keine Prozesse und manipuliere keinen Lock. Erledige alle konfliktfreien Datei- und Bildarbeiten und kennzeichne Unity-Import beziehungsweise Serialisierung anschließend ehrlich als offen.
- Keine externen Uploads, Veröffentlichungen, Deployments, Käufe oder Kontokonfigurationen.

## Inhaltliche Referenzen

Das vollständige ursprüngliche Spielszenario befindet sich in:

`D:\Unity Games\Weedtopia Inc\prompt.txt`

Die handgezeichneten Layouts befinden sich in:

- Farm: `C:\Users\Mein-PC\AppData\Local\Temp\codex-clipboard-5afd155a-98a1-42cf-90ef-020e4aef55d9.png`
- Stadt: `C:\Users\Mein-PC\AppData\Local\Temp\codex-clipboard-9a8993ad-b02c-4aa6-be45-8b0fa8a858c4.png`
- Siedlung: `C:\Users\Mein-PC\AppData\Local\Temp\codex-clipboard-1c2fa033-6bc4-4155-bae2-31da1ecdd21a.png`
- Industriegebiet: `C:\Users\Mein-PC\AppData\Local\Temp\codex-clipboard-78357dc0-dded-44ad-92ea-ba3c5e51afd5.png`
- Gesamtübersicht: `C:\Users\Mein-PC\AppData\Local\Temp\codex-clipboard-17500716-c9d5-4fed-97df-9ec8cdcda740.png`

Diese Skizzen definieren ausschließlich Funktionsgruppen und grobe räumliche Beziehungen. Übernimm weder Handschrift noch weiße Zeichenfläche noch unpräzise Proportionen als visuellen Stil.

Die gewählte Art Direction kombiniert drei bereits erzeugte Mockups:

- **Visuelle Basis und Lesbarkeit – Reclaimed Green Haven:**  
  `C:\Users\Mein-PC\.codex\generated_images\01a09cb8-acba-7eb3-a2e2-4648b94a39f2\exec-62c88034-6a68-4243-8ace-290d77fa4524.png`
- **Technik und Maschinenform – Herbal Atom Age:**  
  `C:\Users\Mein-PC\.codex\generated_images\01a09cb8-acba-7eb3-a2e2-4648b94a39f2\exec-b1dcd739-736b-4528-bf8b-6bc75435e6d8.png`
- **Atmosphäre und Worldbuilding – Burning Hills Afterglow:**  
  `C:\Users\Mein-PC\.codex\generated_images\01a09cb8-acba-7eb3-a2e2-4648b94a39f2\exec-adf21d98-af28-4d37-a9fc-2323e16fc07a.png`

Falls eine Referenz nicht mehr vorhanden ist, suche nicht im Internet nach einem Ersatz und erfinde keinen fremden Stil. Verwende die folgende schriftliche Art Direction als verbindliche Quelle und dokumentiere die fehlende Bildreferenz.

## Verbindliche Art Direction

### Leitidee

Burning Hills ist eine warme, stilisierte **Cozy-Dystopia im Wiederaufbau**. Die Welt trägt sichtbare Spuren eines fiktiven THC-Kriegs, vermittelt aber Hoffnung, Zusammenarbeit, schwarzen Humor und stetigen technischen Fortschritt. Die Architektur beginnt improvisiert und beschädigt und entwickelt sich über handwerkliche Kooperativen zu einer vertikalen, ökologischen Retro-Zukunft.

### Formensprache

- Klare, bereits aus isometrischer Distanz erkennbare Silhouetten.
- Runde oder weich abgeschrägte Hauptformen; keine fotorealistisch scharfen Kanten.
- Leicht überzeichnete Proportionen: Türen, Dächer, Tanks, Förderschnittstellen und Funktionsmodule dürfen für die Lesbarkeit größer sein als real.
- Gebäude besitzen pro Familie eine dauerhafte visuelle DNA, die in jeder Stufe wiedererkennbar bleibt.
- Höhere Stufen wachsen bevorzugt nach oben und nach hinten innerhalb eines reservierten maximalen Bauplatzes. Der Pivot, die Frontausrichtung und der Anschlussbereich bleiben konstant.
- Technik besteht aus modularen, runden Retro-Maschinen: emaillierte Gehäuse, dicke Rohre, Messing- oder Kupferdetails, abgerundete Tanks, grün getöntes Gewächshausglas, sichtbare aber aufgeräumte Wartungsbereiche.
- Keine dünnen, fragilen, schwebenden oder geometrisch zweifelhaften Teile, die Meshy schlecht rekonstruieren würde.

### Materialien

- Hauptmaterialien: warmer handbemalter Putz, wiederverwendetes Holz, gealtertes Wellblech, matter Beton, oliv- oder salbeigrün emailliertes Metall.
- Akzente: Terrakotta, Senfgold, Messing, oxidiertes Kupfer, gedämpftes Türkis.
- Glas: sparsam, dick, leicht grün getönt, vor allem bei Gewächshäusern und Veredelungstanks.
- Abnutzung ist großflächig und stilisiert. Keine kleinteiligen fotorealistischen Kratzerteppiche oder Schmutz-Noise.
- Beleuchtung und Dämmerungsatmosphäre werden später in Unity erzeugt. Backe keine dramatischen Schatten, Fenster-Glows, Nebel oder Farbstiche in Geometrie beziehungsweise Base-Color-Texturen ein.

### 420-Motivik

- Die Cannabis-Thematik ist klar erkennbar, aber nicht das einzige Gestaltungsmittel.
- Verwende botanische Formen, Grow-Lights, Blatt-Rhythmik, goldene Grinder-inspirierte Zahnräder und Burning-Hills-Symbole aus Berg- und Flammenformen.
- Pro Gebäude höchstens ein dominantes Blatt- oder Grinder-Symbol; weitere Motive nur klein und funktional.
- Keine realen Marken, Logos, lesbaren Produktnamen oder fotorealistischen Konsumszenen.
- Keine Waffen, kein Gore und keine glorifizierte Gewaltdarstellung.

### Atmosphäre

- Die Meshy-Eingabebilder selbst verwenden neutrale, gleichmäßige Studio-Beleuchtung auf einfachem Hintergrund.
- Separate Familien-Showcases dürfen die Afterglow-Stimmung zeigen: blaue Stunde, warme Fenster, goldene Arbeitsleuchten, dezente grüne Grow-Lights, leichte atmosphärische Tiefe.
- Die Umgebung bleibt erwachsen und satirisch, Figuren werden respektvoll und nicht als Karikatur dargestellt. Einzelne Meshy-Gebäudereferenzen enthalten jedoch keine Personen.

### Zielplattform und technische Bildsprache

- Ausgelegt für ein isometrisches 3D-Aufbau-/Idle-Spiel auf PC, mit skalierbarer URP-Qualität für spätere schwächere Geräte.
- Lesbarkeit und Silhouette haben Vorrang vor Kleindetails.
- Ein Mesh-Asset soll möglichst wenige Material-Slots benötigen, idealerweise einen und höchstens zwei, sofern transparente Glasflächen eine Trennung erfordern.
- Keine lesbaren Texte als Teil der Textur. Beschriftungen und UI kommen später separat in Unity.

## Gebäudefamilien und Umfang

Erzeuge die vollständige Pipeline für diese zehn ausbaubaren Familien:

1. `cultivation_plot` – Anbauplatz
2. `farm_processing` – Farmgebäude zur Veredelung
3. `farm_warehouse` – Farm-Lager und Versand in die Stadt
4. `city_warehouse` – Stadt-Lager und Verteilung an Shops/Industrie
5. `coffee_shop` – Verkauf und Geldumwandlung
6. `residential_house` – Wohnhaus, Einwohner und Worker-Karten
7. `park` – Park, Erholung und VIP-Quests
8. `butter_factory` – Butterfabrik und Versorgung der Bäckereien
9. `supermarket` – Supermarkt, Einwohnerumsatz und Grinder-Bonus
10. `bakery` – Bäckerei und Weiterverarbeitung

Erzeuge zusätzlich genau einen festen, nicht ausbaubaren Slot:

11. `town_hall/T_FIXED` – Rathaus von Burning Hills, fester Arbeitsplatz von Rocko und Zentrum für Stadtstatistiken

Für jede ausbaubare Familie existieren 25 Asset-Zustände:

- `T00_RUIN` – zerstörter beziehungsweise unbebauter Tutorial-Ausgangszustand
- `T01_L001` – Level 1–4
- `T02_L005` – Level 5–9
- `T03_L010` – Level 10–24
- `T04_L025` – Level 25–49
- `T05_L050` – Level 50–99
- `T06_L100` – Level 100–149
- `T07_L150` – Level 150–199
- `T08_L200` – Level 200–299
- `T09_L300` – Level 300–399
- `T10_L400` – Level 400–499
- `T11_L500` – Level 500–599
- `T12_L600` – Level 600–699
- `T13_L700` – Level 700–799
- `T14_L800` – Level 800–899
- `T15_L900` – Level 900–999
- `T16_L1000` – Level 1000–1249
- `T17_L1250` – Level 1250–1499
- `T18_L1500` – Level 1500–1749
- `T19_L1750` – Level 1750–1999
- `T20_L2000` – Level 2000–2499
- `T21_L2500` – Level 2500–2999
- `T22_L3000` – Level 3000–3999
- `T23_L4000` – Level 4000–4999
- `T24_L5000` – Level 5000, vorläufiges Maximum

Damit müssen am Ende **250 ausbaubare Asset-Slots plus ein Rathaus-Slot = 251 Slots** vollständig im Manifest vorhanden sein. Erzeuge nicht 5.000 Modelle. Innerhalb eines Levelbands wird dasselbe Visual verwendet.

## Globale Fortschrittsgrammatik

Jede Familie behält ihre Grundidentität, entwickelt sich aber in diesen Epochen:

- `T00`: glaubwürdige, aber nicht grausame Ruine beziehungsweise zerstörter Platz; klare rekonstruierbare Grundform
- `T01–T04`: Survival-Reparatur – Holz, Planen, Wellblech, Handwerk, wenige einfache Solarmodule
- `T05–T08`: Community-Aufbau – sauberer Putz, erste dauerhafte Technik, kleine Anbauten, bessere Wege und Versorgung
- `T09–T12`: Kooperative Industrie – emaillierte Maschinen, Messingrohre, größere Funktionsmodule, klar sichtbare Durchsatzsteigerung
- `T13–T16`: District Industry – zusätzliche Etage oder Hallensektion, automatisierte Anschlüsse, größere Tanks und strukturierte Wartungsflächen
- `T17–T20`: Vertical Eco-Tech – vertikale Erweiterung, Kupfer, grünes Glas, Solardächer, kompakte Automation
- `T21–T23`: Metropolitan Flagship – städtisches Wahrzeichen, starke Silhouette, hochwertige modulare Technik und sichtbare Logistikleistung
- `T24`: Burning-Hills-Ikone – eindeutig finales Landmark-Visual mit unverwechselbarer Dachkrone oder Funktionssilhouette, ohne den Stil zu verlassen

Jede aufeinanderfolgende Stufe muss im isometrischen Kamerabild ohne UI unterscheidbar sein. Füge je Stufe mindestens eine große, aus der Entfernung erkennbare Veränderung hinzu: ein neuer Gebäudeflügel, ein zusätzlicher Tank, ein Gewächshaussegment, eine Etage, ein Ladebereich, ein Solardach, ein Turm, ein Pavillon, ein markanter Schornstein oder ein vergleichbares Funktionsmodul. Verlasse dich nicht auf kleine Schilder oder reine Farbänderungen.

## Besondere Entwicklung des Anbauplatzes

Die Anbauplätze folgen zusätzlich dieser verbindlichen Lesart:

- `T01`: 1 robuster Topf
- `T02`: 2 Töpfe
- `T03`: 3 Töpfe
- `T04`: 4 Töpfe
- `T05`: 1 kleine Anbaulage
- `T06`: 2 kleine Anbaulagen
- `T07`: 3 kleine Anbaulagen
- `T08`: 4 kleine Anbaulagen
- `T09`: 1 große Anbaulage
- `T10`: 2 große Anbaulagen
- `T11`: 3 große Anbaulagen
- `T12`: 4 große Anbaulagen
- `T13`: 1 Mega-Anbaulage
- `T14`: 2 Mega-Anbaulagen
- `T15`: 3 Mega-Anbaulagen
- `T16`: 4 Mega-Anbaulagen
- `T17`: 1 Giga-Gewächshauskomplex
- `T18`: 2 gekoppelte Giga-Segmente
- `T19`: 3 gekoppelte Giga-Segmente
- `T20`: 4 gekoppelte Giga-Segmente
- `T21`: 1 kompakter vertikaler Grow-Turm
- `T22`: 2 gekoppelte Grow-Türme
- `T23`: 3 gekoppelte Grow-Türme
- `T24`: 4 Grow-Türme mit zentralem Burning-Hills-Biokuppel-Kern

`T00` zeigt zerstörte Töpfe, verdichteten Boden und ein reparierbares Bewässerungsfundament. Halte auch die großen Stufen innerhalb eines vorher festgelegten maximalen Bauplatzes; skaliere vor allem vertikal und über kompakte gekoppelte Module.

## Funktionsmerkmale je Familie

Definiere vor der Bildgenerierung für jede Familie eine dauerhafte Silhouette, einen Frontbereich, eine maximale Stellfläche, eine ungefähre reale Größe in Metern, Materialprioritäten und wiederkehrende Funktionsmodule.

- `farm_processing`: Eingangsport für Ernte, Veredelungstanks, Ausgang zum Förderband; steigender Durchsatz muss durch zusätzliche runde Tanks, Leitungen und Hallensektionen sichtbar werden.
- `farm_warehouse`: Annahme vom Farmgebäude, Lagerung und Versand; jeder visuelle Meilenstein erhöht gameplayseitig die Fahrer-Kapazität um 1. Zeige zunehmende Lade- und Flottenkapazität durch Tore, Docks, kompakte Park-/Garagenmodule und Dispatch-Technik, ohne 24 Fahrzeuge in das Gebäudemesh einzubacken.
- `city_warehouse`: städtisches Pendant mit mehreren klaren Ausgängen zu Coffee-Shops und Industrie; ebenfalls +1 Fahrer-Kapazität pro Meilenstein. Optisch urbaner und organisierter als das Farm-Lager.
- `coffee_shop`: einladende Verkaufsfront, Kasse/Servicefenster und zunehmende Kundenkapazität; keine Konsumszene und keine lesbare Markenbeschriftung.
- `residential_house`: wachsende Einwohnerkapazität; an visuellen Meilensteinen erkennbarer Ausbau von Notunterkunft zu vertikalem, begrüntem Wohnkomplex. Worker-Karten werden nur über ein abstrahiertes Post-/Community-Modul angedeutet.
- `park`: von zerstörter Brache zu hochwertigem, begrüntem Stadtpark; Questfunktion durch einen kleinen wiederkehrenden Community-/VIP-Pavillon andeuten. Das Asset bleibt als klar begrenzte Park-Parzelle Meshy-tauglich.
- `butter_factory`: lebensmitteltauglich wirkende Retro-Verarbeitung mit Tanks, Mischern, Rohrsystemen und Ausgängen zu Bäckereien; keine ekelerregenden oder toxischen Assoziationen.
- `supermarket`: zunehmende Verkaufs- und Versorgungskapazität; Grinder-Bonus durch ein dezentes goldenes Zahnrad-/Grinder-Motiv, nicht durch Text.
- `bakery`: Ofen, Backstube, Lieferfenster und wachsender Produktionsbereich; warme Terrakotta- und Kupferakzente unterscheiden sie von der Butterfabrik.
- `town_hall`: nicht zerstört und nicht ausbaubar; repariertes, glaubwürdiges Rathaus als zentrales Wahrzeichen mit Burning-Hills-Berg-/Flammenkrone, Statistikbereich im Inneren nur durch Architektur angedeutet. Keine Figur in das Gebäudemesh integrieren.

Förderbänder, Fahrzeuge, Worker und frei platzierbare Dekorationen sind eigenständige spätere Prefab-Familien. Backe sie nicht als dauerhaft untrennbare Teile in Gebäudemodelle ein. Die Gebäude dürfen klar erkennbare Anschlussports, Ladebuchten und Ankerbereiche dafür besitzen.

## Bildproduktionsregeln

Verwende den vorhandenen kostenlosen `imagegen`-Skill beziehungsweise die eingebaute Bildgenerierung. Verwende nicht Python oder handgezeichnete Platzhalter als Ersatz für die verlangten finalen Rasterbilder. Erzeuge pro angefordertem Asset ein eigenes Bild; eine Kontaktübersicht ersetzt kein Meshy-Eingabebild.

### Pflichtbild pro Slot

Erzeuge für jeden der 251 Slots:

`mesh_input_3q.png`

Anforderungen:

- quadratisch und mindestens 1024 × 1024 Pixel, sofern das verfügbare Bildwerkzeug dies unterstützt
- genau ein vollständiges, freistehendes Asset
- nahe-orthografische 3/4-Frontansicht von leicht erhöhtem Kamerapunkt
- Front zeigt in allen Bildern konsistent in dieselbe lokale Richtung
- gesamtes Objekt vollständig sichtbar, mit ausreichend Rand
- einfarbiger sehr heller warmgrauer oder off-white Hintergrund
- gleichmäßige neutrale Studio-Beleuchtung und nur ein weicher Kontaktschatten
- kein Boden-Diorama, keine Landschaft, keine Stadt, keine Personen, keine Fahrzeuge, keine UI, kein Text, kein Wasserzeichen
- keine angeschnittenen oder außerhalb des Bildes liegenden Teile
- keine schwebenden Partikel, Rauchfahnen, Kabelsalate oder dünnen Antennen
- transparente beziehungsweise gläserne Bereiche müssen als klare, dicke, rekonstruierbare Formen lesbar bleiben
- keine dramatische Dämmerungsbeleuchtung im Meshy-Eingabebild

### Konsistenz innerhalb einer Familie

- Erzeuge zuerst `T01`, definiere daraus den unverwechselbaren Kern und verwende dieses Bild zusammen mit den drei Style-Referenzen als Referenz für alle folgenden Stufen derselben Familie.
- Erzeuge `T00` aus derselben Form-DNA als beschädigten Ausgangszustand.
- Erzeuge danach `T02` bis `T24` in Reihenfolge. Verwende bei jeder neuen Stufe zusätzlich das unmittelbar vorherige akzeptierte Stufenbild als Geometrie- und Proportionsreferenz.
- Bewahre Frontseite, Pivot-Logik, Maximalgrundfläche, Hauptmaterialien und Kernsilhouette. Ändere nur die in der Spezifikation definierte sichtbare Weiterentwicklung.
- Akzeptiere keine Ausgabe, die die Gebäudefamilie neu erfindet, spiegelt, die Front wechselt, das Asset abschneidet, mehrere Gebäude zeigt oder eine Szene statt eines Einzelassets erzeugt. Regeneriere gezielt.
- Nutze niemals dasselbe Bild für zwei Slots. Prüfe Dateihashes auf versehentliche Duplikate.

### Familien-Showcase

Erzeuge zusätzlich pro Gebäudefamilie genau ein breites `family_showcase.png`. Es zeigt `T01`, `T12` und `T24` gemeinsam in einer kleinen, isometrisch lesbaren Burning-Hills-Testumgebung bei blauer Stunde. Dieses Bild dient nur der Art-Direction-Kontrolle und wird nicht an Meshy übergeben. Es muss die warme Beleuchtung aus „Afterglow“, die Grundformen aus „Reclaimed Green Haven“ und die Technik aus „Herbal Atom Age“ sichtbar verbinden.

## Meshy-Prompt-Regeln

Erzeuge pro Slot genau einen primären englischen Prompt:

`meshy_prompt_en.txt`

Der Prompt muss:

- maximal 800 Zeichen einschließlich Leerzeichen enthalten,
- das wichtigste Objekt und die Stufe zuerst nennen,
- der Reihenfolge **subject/function → proportions/form → materials → art style → technical constraints → exclusions** folgen,
- auch ohne Bild als brauchbarer Text-to-3D-Prompt funktionieren,
- mit dem zugehörigen Bild als eindeutige Geometrie-/Texturergänzung funktionieren,
- die konkrete sichtbare Veränderung dieser Stufe nennen,
- Begriffe wie `stylized`, `game-ready`, `smart-topology`, `clean edges`, `minimal geometry`, `single complete asset`, `centered`, `grounded`, `front facing +Z`, `no floating parts` enthalten,
- Personen, Szene, Landschaft, Text, Logos, Fahrzeuge, lose Props, Diorama-Boden und abgeschnittene Geometrie im selben Hauptprompt ausschließen, damit die Ausschlüsse auch dann gelten, wenn Meshys separates Negativfeld nicht verfügbar oder wirkungslos ist,
- keine Künstlernamen, Film-, Spiele-, Studio- oder Markenstile nachahmen,
- keine widersprüchlichen Material-, Kamera- oder Topologieangaben enthalten.

Erzeuge zusätzlich:

- `texture_prompt_en.txt` – höchstens 800 Zeichen, ausschließlich Materialien, Farbflächen, stilisierte Abnutzung und PBR-Texturabsicht; keine Geometrieänderungen
- `image_prompt_en.txt` – der tatsächlich verwendete vollständige Prompt für `mesh_input_3q.png`
- `asset_spec.de.md` – deutsche Beschreibung von Funktion, Silhouette, Ausbaumerkmalen, ungefähren Maßen, Front, Pivot, maximaler Stellfläche, Anschlussports und Unterschieden zur vorherigen Stufe
- `meshy_settings.json` – empfohlene manuelle Einstellungen, ohne eine externe Aufgabe auszuführen

Empfohlene Einstellungen in `meshy_settings.json`:

- `primary_workflow`: `image_to_3d`
- `fallback_workflow`: `text_to_3d`
- `model_type`: `smart-topology` sofern in der aktuellen Meshy-Oberfläche verfügbar
- `topology`: `triangles`
- `target_faces`: je nach Größe und Silhouette begründet zwischen 6000 und 15000; kleinere Anbauplätze/Parks eher unten, Landmark-Gebäude eher oben
- `texture_resolution`: `2048`
- `pbr`: `true`
- `rigging`: `false`
- `animation`: `false`
- `recommended_export`: `FBX with complete texture folder`
- `unity_scale`: `1 unit = 1 meter`
- `unity_up_axis`: `+Y`
- `unity_forward_axis`: `+Z`
- `pivot_intent`: `ground center`

Wenn sich die aktuelle Meshy-Weboberfläche von diesen Bezeichnungen unterscheidet, ändere keine Grundanforderung, sondern dokumentiere die aktuell verfügbare Entsprechung in `Docs/ArtProduction/MESHY_WORKFLOW.md`. Nutze weiterhin keine API und verbrauche keine Credits.

## Dateistruktur

Lege die projektgebundenen Referenzbilder und Produktionsdaten außerhalb des Unity-`Assets`-Imports ab:

```text
Docs/ArtProduction/
  ART_DIRECTION.md
  BUILDING_TIER_MATRIX.csv
  BUILDING_ASSET_MANIFEST.json
  MESHY_WORKFLOW.md
  PROGRESS.json
  References/
    ChosenStyle/
  Buildings/
    <building_id>/
      family_showcase.png
      family_spec.de.md
      T00_RUIN/
        mesh_input_3q.png
        image_prompt_en.txt
        meshy_prompt_en.txt
        texture_prompt_en.txt
        meshy_settings.json
        asset_spec.de.md
      ...
      T24_L5000/
        ...
    town_hall/
      T_FIXED/
        ...
```

Kopiere die drei ausgewählten Stilreferenzen nachvollziehbar nach `Docs/ArtProduction/References/ChosenStyle`, ohne die Originale zu löschen oder zu überschreiben. Vermerke Herkunft und Rolle in `ART_DIRECTION.md`.

Lege die Unity-Empfangsstruktur unter diesen Pfaden an:

```text
Assets/Weedtopia/
  Art/Buildings/Incoming/<building_id>/<tier_id>/
  Art/Buildings/Prefabs/<building_id>/
  Data/Buildings/
  Scripts/Runtime/Buildings/Presentation/
  Scripts/Editor/BuildingArtPipeline/
```

Erzeuge keine `.meta`-Dateien von Hand, wenn Unity verfügbar ist und sie regulär importieren kann. Bewahre vorhandene Metadaten. Lege keine generierten Bild-Artefakte nur im globalen Codex-Ausgabeordner ab; die final akzeptierten Bilder müssen in `Docs/ArtProduction` kopiert werden.

## Manifest und Matrix

`BUILDING_TIER_MATRIX.csv` enthält mindestens:

- `building_id`
- `building_name_de`
- `tier_id`
- `min_level`
- `max_level`
- `era`
- `visual_name_de`
- `visible_delta_from_previous`
- `footprint_m_x`
- `footprint_m_z`
- `height_m`
- `target_faces`
- `driver_slot_delta`
- `reference_image`
- `meshy_prompt_file`
- `texture_prompt_file`
- `unity_incoming_folder`
- `unity_prefab_target`
- `status`

`BUILDING_ASSET_MANIFEST.json` ist die maschinenlesbare Quelle derselben Daten. Definiere ein klares `schemaVersion`. Relative Pfade sind relativ zum Projektroot und verwenden `/`. Jeder der 251 Slots kommt genau einmal vor. Führe keine Gameplaywerte ein, die nicht aus dem Szenario ableitbar sind. `driver_slot_delta` ist nur bei `farm_warehouse` und `city_warehouse` ab `T01` beziehungsweise pro sichtbarem Meilenstein gemäß Szenario gesetzt; dokumentiere die genaue Interpretation zentral.

`PROGRESS.json` muss nach jeder vollständig akzeptierten Familie atomar aktualisiert werden und mindestens erwartete, erzeugte, validierte und fehlgeschlagene Slot-Zahlen sowie den letzten abgeschlossenen Slot enthalten. So muss der Auftrag nach einer Unterbrechung idempotent fortsetzbar sein, ohne fertige Bilder neu zu erzeugen.

## Unity-Editor-Intake

Implementiere eine kleine, saubere und erweiterbare Präsentations-/Editor-Struktur. Verwende Namespaces unter `Weedtopia.Buildings.Presentation` und `Weedtopia.Editor.BuildingArtPipeline`.

### Runtime-Daten

- Ein ScriptableObject-Katalog hält pro `building_id` die levelbasierte Zuordnung auf Wrapper-Prefabs.
- Die Datenstruktur trennt Levelschwellen, visuelle Präsentation und spätere Gameplaylogik.
- Keine riesigen `switch`-Blöcke und keine hart kodierten Ressourcenpfade zur Laufzeit.
- Ein Gebäude-Slot kann später über eine kleine Schnittstelle das passende Visual für ein Level anfordern, ohne Wirtschaftsdaten zu kennen.
- Verwende direkte serialisierte Prefab-Referenzen. Füge in diesem Auftrag kein Addressables-Paket hinzu.

### Wrapper-Prefab

Jeder der 251 Slots erhält einen eindeutig benannten Ziel-Prefab beziehungsweise einen vorbereiteten serialisierten Slot. Die Root-Hierarchie lautet mindestens:

```text
WT_<BuildingId>_<TierId>
  VisualRoot
  InteractionAnchor
  WorkerAnchor
  DeliveryInputAnchor
  DeliveryOutputAnchor
  VfxAnchor
```

- Das importierte Meshy-Modell wird ausschließlich unter `VisualRoot` referenziert.
- Verändere das importierte FBX und seine eingebetteten Daten nicht destruktiv.
- Transformkorrekturen erfolgen am `VisualRoot`-Kind beziehungsweise in dokumentierten Importsettings, nicht auf jeder Szeneninstanz.
- Erzeuge keine willkürlichen MeshCollider. Bereite nur eine klare Collider-Root vor; konkrete vereinfachte Collider bleiben ein sichtbarer manueller QA-Schritt, sofern sie nicht mit einfachen Box-Collidern zuverlässig ableitbar sind.
- Förderband- und Fahrzeuganschlüsse sind leere serialisierte Transforms, keine zur Laufzeit erzeugten Deko-Objekte.

### EditorWindow

Erzeuge ein EditorWindow unter:

`Weedtopia/Art Pipeline/Building Tier Intake`

Es soll:

- alle 251 Manifest-Slots filtern und durchsuchen können,
- Gebäudefamilie, Tier, Levelband, Referenzbild, Meshy-Prompt und Zielpfad anzeigen,
- einen ObjectField-/Drag-and-drop-Slot für das importierte FBX-/Model-Prefab bieten,
- auf expliziten Klick einen Wrapper-Prefab idempotent erstellen oder aktualisieren,
- vorhandene manuelle Child-Offsets und Anchor-Anpassungen beim Update bewahren,
- niemals Quellmodelle löschen, verschieben oder überschreiben,
- fehlende Modelle, falsche Namenszuordnung, leere Materialien, unplausible Bounds, falsche Ausrichtung und fehlende Katalogreferenzen melden,
- einen reinen Validierungsmodus besitzen, der nichts verändert,
- keinen externen Dienst aufrufen.

Erzeuge zusätzlich den Menüpunkt:

`Weedtopia/Art Pipeline/Create or Repair Building Intake Scaffold`

Der Menüpunkt erstellt beziehungsweise repariert nur fehlende Projektordner, Katalogeinträge und serialisierte leere Slots. Er bleibt idempotent und überschreibt keine zugewiesenen Modelle oder manuellen Prefab-Anpassungen.

## Arbeitsreihenfolge

1. Inventarisiere den aktuellen Projektzustand, aktive Unity-Prozesse/Lockfile und anwendbare Anweisungen.
2. Lies das Spielszenario und die acht Referenzbilder vollständig.
3. Schreibe `ART_DIRECTION.md`, die Familien-Spezifikationen, Epochenlogik und die vollständige Tiermatrix, bevor du Bilder erzeugst.
4. Implementiere Manifest-Schema, Fortschrittsdatei und Unity-Intake so, dass Zielpfade und IDs feststehen.
5. Erzeuge einen kleinen internen Qualitätsanker für jede Familie (`T01`) und prüfe ihn gegen die Art Direction. Diese Qualitätsanker sind bereits Teil der geforderten 251 Slots, keine zusätzlichen Ersatzbilder.
6. Erzeuge danach Familie für Familie alle weiteren Pflichtbilder und Promptdateien. Überspringe bei einer Fortsetzung nur Slots, die vollständig vorhanden, im Manifest registriert und validiert sind.
7. Erzeuge die elf Familien-Showcases.
8. Führe automatisierte Vollständigkeits-, Schema-, Zeichenlängen-, Bildgrößen-, Dateihash- und Pfadprüfungen aus.
9. Prüfe die Unity-C#-Kompilierung und Editor-Tests, sofern dies ohne Konflikt mit einer laufenden Unity-Instanz möglich ist.
10. Erzeuge den Abnahmebericht.

Die Arbeit darf wegen ihres Umfangs in lokalen Batches erfolgen. Ein Batch-Ende ist jedoch kein Abschluss. Setze anhand von `PROGRESS.json` fort, bis alle 251 Slots vollständig sind oder ein echter externer Blocker vorliegt. Erzeuge keine generischen Platzhalterbilder, um Zählungen künstlich zu erfüllen.

## Automatische Prüfungen

Erzeuge EditMode-Tests oder geeignete lokale Validatoren für mindestens:

- exakt 251 eindeutige Manifest-Slots
- exakt 25 Slots pro ausbaubarer Familie
- exakt 1 `T_FIXED` für `town_hall`
- korrekte, lückenlose Levelbänder und `T00`-Zuordnung
- vorhandenes `mesh_input_3q.png` für jeden Slot
- Mindestauflösung jedes Pflichtbilds
- keine identischen Bildhashes zwischen zwei Slots
- vorhandene fünf Begleitdateien pro Slot (`image_prompt_en.txt`, `meshy_prompt_en.txt`, `texture_prompt_en.txt`, `meshy_settings.json`, `asset_spec.de.md`)
- Meshy-Hauptprompt nicht leer und höchstens 800 Zeichen
- Texturprompt nicht leer und höchstens 800 Zeichen
- alle Manifestpfade relativ, normalisiert und existent
- `target_faces` innerhalb des festgelegten Budgets
- eindeutige Unity-Zielpfade
- keine doppelte `building_id/tier_id`-Kombination
- idempotentes erneutes Ausführen des Scaffold-Generators
- vorhandene Katalogeinträge für alle 251 Slots
- keine C#-Compilerfehler in den neu erstellten Assemblies

Für die Bildinhalte führe zusätzlich eine visuelle Stichprobe jedes `T00`, `T01`, `T08`, `T16` und `T24` sowie aller elf Showcases durch. Prüfe Motiv, Freistellung, Frontorientierung, Stiltreue, Silhouette, Abgrenzung zur vorherigen Stufe und fehlende unerwünschte Texte beziehungsweise Personen. Bildabmessungen und Dateiexistenz allein sind keine visuelle Abnahme.

## Abnahmekriterien

Der Auftrag ist erst vollständig, wenn:

- `Docs/ArtProduction` die verbindliche Art Direction und Produktionsdokumentation enthält,
- alle 251 Slots mit einem final akzeptierten Meshy-Eingabebild, Bildprompt, Meshy-Prompt, Texturprompt, Settings und deutscher Asset-Spezifikation vorhanden sind,
- alle elf Familien-Showcases vorhanden sind,
- Matrix, JSON-Manifest und Dateien exakt übereinstimmen,
- die Unity-Empfangsstruktur und der Editor-Intake vorhanden sind,
- der Nutzer jedes spätere FBX über einen klaren Drag-and-drop-Slot zuordnen kann,
- der Scaffold-Generator keine bestehenden Zuweisungen oder manuellen Anpassungen überschreibt,
- die automatisierten Prüfungen erfolgreich sind,
- Unity-Compiler- und Teststatus getrennt und ehrlich berichtet werden,
- kein Meshy-Aufruf, kein Creditverbrauch, kein Upload und keine Veröffentlichung stattgefunden hat.

Wenn ein Toollimit die Bildgenerierung unterbricht, markiere fehlende Slots in `PROGRESS.json`, bewahre alle bereits geprüften Ergebnisse und melde den exakten wiederaufnehmbaren nächsten Slot. Nenne den Gesamtauftrag in diesem Fall nicht abgeschlossen.

## Abschlussbericht

Berichte am Ende knapp, aber beweisbar:

- angelegte beziehungsweise geänderte Dateien und Hauptpfade
- erwartete und tatsächlich vollständige Slot-Zahl
- Bildzahl einschließlich Showcases
- Prompt- und Spezifikationszahl
- Ergebnis aller Validatoren
- Unity-Source-Compile, Unity-Import, EditMode-Tests und manuelle visuelle QA als getrennte Gates
- welche Schritte der Nutzer danach in Meshy und Unity exakt ausführt
- offene Risiken oder manuelle Prüfungen
- ausdrückliche Bestätigung, dass weder Meshy noch ein anderer externer Dienst bedient und keine Credits verbraucht wurden

Melde keinen Erfolg nur aufgrund erzeugter Dateien. Erfolg setzt die oben definierten Vollständigkeits- und Qualitätsprüfungen voraus.

# Burning Hills – Building Art Direction

Verbindliche Produktionsgrundlage für Weedtopia Inc, Unity 6000.3.9f1 / URP 17.3.0. Stil und Architektur dienen einem isometrischen Aufbau-/Idle-Spiel. Die Dateien in diesem Ordner sind Produktionsdaten; weder Wirtschaft noch Worker-Logik sind implementiert.

## Leitidee und Formensprache

Eine warme Cozy-Dystopia im Wiederaufbau nach einem fiktiven THC-Krieg: Hoffnung, Kooperation, erwachsene Satire und sichtbarer technischer Fortschritt. Reparierte, improvisierte Anfänge entwickeln sich zu handwerklichen Kooperativen und einer vertikalen ökologischen Retro-Zukunft. Keine Gewaltverherrlichung, Waffen, Gore oder entwürdigenden Figuren. In den einzelnen Gebäudereferenzen gibt es überhaupt keine Personen.

Klare Silhouetten mit runden oder weich abgeschrägten Hauptformen sind bereits aus isometrischer Distanz lesbar. Türen, Dächer, Tanks und Schnittstellen dürfen überzeichnet sein. Jede Familie hält ihren in `family_spec.de.md` definierten Kern, ihre Front und ihren Anschlussbereich über alle Stufen. Wachstum erfolgt nach oben und hinten innerhalb einer festen reservierten Maximalfläche. Die frühe Stufe füllt diese Fläche nicht künstlich auf. Die reservierte Parzelle ist keine sichtbare Dioramaplatte.

Technik besteht aus dicken, sauber integrierten Rohren, runden emaillierten Gehäusen, robusten Tanks, Messing-/Kupferdetails und sparsamem dickem grünem Glas. Keine fragilen Antennen, Kabelknäuel, schwebenden Bauteile oder dünnen, unverbundenen Blätter. Funktionsmodule bilden einen zusammenhängenden Gebäudekörper. Förderbänder, Fahrzeuge, Worker und frei platzierbare Dekoration gehören zu späteren eigenen Prefab-Familien.

## Material- und Farbprioritäten

Warmer handbemalter Putz, wiederverwendetes Holz, gealtertes Wellblech, matter Beton und oliv-/salbeigrünes Emaille. Akzente: Terrakotta, Senfgold, Messing, oxidiertes Kupfer und gedämpftes Türkis. Glas bleibt dick, sparsam und geometrisch nachvollziehbar. Abnutzung besteht aus großen ruhigen Flächen; keine fotorealistischen Kratzerteppiche oder Schmutz-Noise.

PBR-Base-Color enthält weder dramatische Schatten noch Fenster-Glow, Nebel, Dämmerungsfarbstich oder eingebrannte Umgebungsokklusion. Roughness überwiegend matt; Metallwerte nur für tatsächlich freiliegendes Metall. Ein Material pro Asset bevorzugt, höchstens zwei bei notwendiger Glastrennung.

Cannabis ist als botanische Funktion und Rhythmik sichtbar, aber nicht als einziges Motiv. Pro Gebäude höchstens ein dominantes Blatt- oder Grinder-Symbol. Goldene Zahnräder und Berg-/Flammenformen verweisen auf Burning Hills. Keine lesbaren Marken, Logos oder Produktschriften; spätere Beschriftungen gehören nach Unity.

## Epochen und Slotvertrag

| Tiers | Epoche | Große lesbare Entwicklung |
|---|---|---|
| T00 | Ruine | Reparierbare beschädigte Grundform; Anbau mit kaputten Töpfen, verdichtetem Boden und Bewässerungsfundament |
| T01–T04 | Survival-Reparatur | Holz, Planen, Wellblech, robuste erste Funktionsmodule |
| T05–T08 | Community-Aufbau | Dauerhafter Putz, Versorgung, erste Solardächer, klarere Wege |
| T09–T12 | Kooperative Industrie | Emaillierte Maschinen, Messingrohre, größere Durchsatzmodule |
| T13–T16 | District Industry | Hallensektionen, Etagen, automatische Anschlüsse und strukturierte Wartung |
| T17–T20 | Vertical Eco-Tech | Vertikale Verdichtung, Kupfer, grünes Glas und Solartechnik |
| T21–T23 | Metropolitan Flagship | Städtisches Wahrzeichen und starke modulare Kapazitätssilhouette |
| T24 | Burning-Hills-Ikone | Unverwechselbare integrierte Krone oder finale Funktionssilhouette |

Jede Stufe hat eine große funktionale Geometrieänderung, keine reine Farbänderung. Alle 24 intakten Ausbauzustände plus T00 stehen je Familie in der Matrix. Die Levelgrenzen sind 1, 5, 10, 25, 50, 100, 150, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1250, 1500, 1750, 2000, 2500, 3000, 4000, 5000. T00 gilt nur für Level 0; T24 vorläufig nur für Level 5000. Es werden 250 ausbaubare Slots plus genau ein `town_hall/T_FIXED` geführt. Das feste Rathaus verwendet Band 0–5000 als Präsentationszuordnung ohne Ausbau.

Anbau folgt exakt 1–4 Töpfen, 1–4 kleinen Lagen, 1–4 großen Lagen, 1–4 Mega-Lagen, 1–4 gekoppelten Giga-Segmenten und 1–4 Grow-Türmen. T24 besitzt zusätzlich den zentralen Biokuppel-Kern. Ein Kategorienwechsel ersetzt die frühere Konfiguration; alte Töpfe werden nicht zusätzlich zwischen neuen Gewächshäusern gesammelt.

`driver_slot_delta`: nur beide Lager verwenden T01=1 als initialen Fahrerplatz, danach T02–T24 jeweils +1 pro sichtbarem Meilenstein. T00 und alle anderen Familien verwenden 0. Diese Interpretation ist zentral dokumentierte Produktionsmetadaten, keine zusätzlich implementierte Wirtschaft oder Kapazitätsbalance. Wohnkapazität, Worker-Karten, VIP-Quests und Grinder-Bonus werden nur funktional angedeutet, ohne erfundene Zahlenwerte.

## Koordinaten und visuelle Abnahme

Einheit: 1 Unity-Einheit = 1 m. Oben +Y, Front +Z, feste maximal reservierte Grundfläche X/Z. Die Vorderkante bleibt für Anschlüsse konstant. Pivot-Absicht ist der Bodenmittelpunkt des reservierten Bauplatzes; der Wrapper korrigiert Position/Rotation/Skalierung des importierten Visuals. Anker sind sichtbare serialisierte Transforms. Die Spezifikation nennt ungefähre belegte Maße pro Stufe und das feste Reservierungsmaß separat.

Pflichtbild: quadratisch, mindestens 1024 × 1024, genau ein vollständig sichtbares freistehendes Asset, nahe-orthografische 3/4-Frontansicht mit etwa 35° Höhe. Front auf der linken unteren sichtbaren Seite; +X-Seite rechts unten. Heller einfarbiger warmgrauer Hintergrund, neutrale gleichmäßige Studiobeleuchtung, nur weicher Kontaktschatten, großzügiger Rand. Keine Landschaft, Personen, Fahrzeuge, UI, lesbaren Texte, Logos oder Rauch. Der Park darf seine konstruktiv begrenzte Park-Parzelle enthalten; keine zusätzliche Umgebung.

Reihenfolge: zuerst alle zehn T01-Qualitätsanker und das feste Rathaus. Danach jede Familie T00 aus T01, anschließend T02–T24 in Reihenfolge. Jede Folgegeneration nutzt die drei Stilreferenzen, den akzeptierten T01-Anker und den direkten akzeptierten Vorgänger. Keine Kontaktübersicht ersetzt ein Einzelbild. Kein Bild darf für zwei Slots wiederverwendet werden.

Zusätzlich elf breite Showcases: bei zehn Familien T01/T12/T24 zusammen in kleiner isometrischer Testumgebung bei blauer Stunde, warmen Fenstern, goldenen Arbeitsleuchten und dezentem grünen Grow-Light. Das Rathaus ist ausdrücklich nicht ausbaubar; sein elftes Showcase zeigt das eine feste Rathaus in derselben Atmosphäre. Das ist die einzige notwendige Auslegung des sonst widersprüchlichen T01/T12/T24-Showcase-Satzes.

Jedes Bild wird visuell geprüft und erst mit hashgebundener QA freigegeben. Verpflichtende dokumentierte Kontrollstichprobe: alle T00, T01, T08, T16, T24 sowie T_FIXED und alle elf Showcases. Prüfkriterien: Motiv, Freistellung, vollständige Geometrie, gleiche Front, DNA, große Stufendifferenz, keine unerwünschten Personen/Texte. Hash, Dateigröße und Auflösung allein belegen keine Bildqualität.

## Referenzherkunft

Alle folgenden acht Originale waren beim Start vorhanden und wurden einzeln visuell gelesen. Sie bleiben unverändert. Die fünf Skizzen definieren ausschließlich Funktionsgruppen und grobe räumliche Beziehungen; weder Handschrift noch Zeichenfläche oder unpräzise Proportionen werden als Stil übernommen.

| Lokale Kopie | Rolle | Originaldatei |
|---|---|---|
| References/ChosenStyle/Reclaimed_Green_Haven.png | Visuelle Basis, warmer Wiederaufbau und Lesbarkeit | C:/Users/Mein-PC/.codex/generated_images/01a09cb8-acba-7eb3-a2e2-4648b94a39f2/exec-62c88034-6a68-4243-8ace-290d77fa4524.png |
| References/ChosenStyle/Herbal_Atom_Age.png | Runde Retro-Technik und Maschinenformen | C:/Users/Mein-PC/.codex/generated_images/01a09cb8-acba-7eb3-a2e2-4648b94a39f2/exec-b1dcd739-736b-4528-bf8b-6bc75435e6d8.png |
| References/ChosenStyle/Burning_Hills_Afterglow.png | Hoffnung, warme Lichter, Worldbuilding; Atmosphäre nur für Showcases | C:/Users/Mein-PC/.codex/generated_images/01a09cb8-acba-7eb3-a2e2-4648b94a39f2/exec-adf21d98-af28-4d37-a9fc-2323e16fc07a.png |
| References/Layouts/Farm.png | Anbau → Veredelung → Farm-Lager | C:/Users/Mein-PC/AppData/Local/Temp/codex-clipboard-5afd155a-98a1-42cf-90ef-020e4aef55d9.png |
| References/Layouts/City.png | Rathaus, Stadt-Lager und Shops | C:/Users/Mein-PC/AppData/Local/Temp/codex-clipboard-9a8993ad-b02c-4aa6-be45-8b0fa8a858c4.png |
| References/Layouts/Residential.png | Austauschbare Wohnhaus-/Park-Parzellen | C:/Users/Mein-PC/AppData/Local/Temp/codex-clipboard-1c2fa033-6bc4-4155-bae2-31da1ecdd21a.png |
| References/Layouts/Industry.png | Butterfabrik, Supermarkt und Bäckereien | C:/Users/Mein-PC/AppData/Local/Temp/codex-clipboard-78357dc0-dded-44ad-92ea-ba3c5e51afd5.png |
| References/Layouts/Overview.png | Farm, Stadt, Siedlung, Industriegebiet | C:/Users/Mein-PC/AppData/Local/Temp/codex-clipboard-17500716-c9d5-4fed-97df-9ec8cdcda740.png |

Ursprüngliches vollständig gelesenes Spielszenario: `D:/Unity Games/Weedtopia Inc/prompt.txt`. Verbindlicher Arbeitsauftrag: `Docs/Codex-Prompts/BUILDING_ART_AND_MESHY_PIPELINE.md`. Keine fehlende Referenz wurde durch Internetmaterial ersetzt.

## Quelle und Fortschritt

`BUILDING_ASSET_MANIFEST.json` mit `schemaVersion: 1` ist die maschinenlesbare Quelle; relative Pfade beziehen sich auf den Projektroot und verwenden `/`. Die CSV projiziert dieselben Felder. `Tools/build_production_data.py` legt fehlende Begleitdateien idempotent an, bewahrt vorhandene Zeilen und akzeptierte Bilder und führt keine Bildgenerierung aus. `Tools/validate_pipeline.py` prüft Metadaten, Dateien und Bildabnahmen und schreibt `PROGRESS.json` atomar. Vorbereitete Bildprompts gelten erst nach protokolliertem Generatoraufruf als tatsächlich verwendet.

Unity-Source-Compile, Unity-Import, serialisierte Katalog-/Wrapper-Erzeugung, EditMode-Ausführung, Bild-QA und spätere manuelle FBX-Abnahme sind getrennte Gates. Ein laufender Unity-Prozess wird weder beendet noch durch eine zweite Instanz ergänzt. Keine handgeschriebenen `.meta`-Dateien.

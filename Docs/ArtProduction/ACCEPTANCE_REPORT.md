# Abnahmebericht – Building Art und Unity-Empfang

> **Produktionswechsel am 21. September 2026:** Die 251 akzeptierten isometrischen Einzelbilder bleiben der verbindliche Art-Stand. Der Meshy-/FBX-/Blender-Weg ist für Weedtopia nach der visuellen Nutzerprüfung archiviert. Die aktive Umsetzung verwendet isometrische 2D-Sprites mit optionalen 2.5D-Ebenen unter den vorhandenen `VisualRoot`-Objekten. Der isolierte Rathaus-Prototyp ist vorbereitet, verändert weder Produktions-Wrapper noch Katalog und wartet noch auf Unity-Import, Game-View-Prüfung und ausdrückliche visuelle Freigabe. Der aktuelle maschinenlesbare Stand steht in [PROGRESS.json](PROGRESS.json) und [2d_visual_qa.json](Buildings/town_hall/T_FIXED/2d_visual_qa.json).

**Abgeschlossen am 14. September 2026.** Alle **251 Einzelbilder und elf Familien-Showcases** sind vollständig akzeptiert. Die vollständige lokale Produktionsprüfung, die 62 vorgeschriebenen visuellen Stichproben und die tatsächliche Unity-Abnahme sind bestanden. Der lokale Arbeitsauftrag ist vollständig ausgeführt. Die spätere manuelle Erzeugung und Prüfung echter Meshy-FBX-Modelle bleibt entsprechend dem Auftrag außerhalb dieser Abnahme.

Der Auftrag umfasst zehn ausbaubare Familien mit jeweils 25 Zuständen und genau ein festes Rathaus, insgesamt **251 Slots**. Es wurden ausschließlich lokale Produktionsdaten, die ausdrücklich zugelassene integrierte Bildgenerierung und lokale Unity-Werkzeuge verwendet. Meshy wurde weder geöffnet noch automatisiert bedient; keine Meshy-API, Anmeldung, Modell-Uploads, Credit-Ausgaben, Veröffentlichungen oder Deployments wurden ausgeführt. Ein kostenpflichtiger Bild-CLI-/API-Fallback wurde nicht verwendet.

## Produktionspaket und Stückzahlen

Der verbindliche aktuelle Fortschritt steht in [PROGRESS.json](PROGRESS.json). Der [Asset-Index](BUILDING_ASSET_INDEX.md) bietet direkte Links zu allen Slots. [Manifest](BUILDING_ASSET_MANIFEST.json) und [Tiermatrix](BUILDING_TIER_MATRIX.csv) führen dieselben 251 Identitäten, Levelbänder, Maße, Face-Budgets, Funktionsunterschiede und lokalen Zielpfade.

| Bestandteil | Soll | Nachgewiesener Umfang |
|---|---:|---|
| Manifest- und Matrix-Slots | 251 | 251 eindeutige Slots: zehnmal 25 plus einmal T_FIXED |
| Final akzeptierte Einzelbilder | 251 | 251 vollständig vorhanden und aktuell hashgebunden akzeptiert |
| Familien-Showcases | 11 | 11 vollständig vorhanden und aktuell hashgebunden akzeptiert |
| Tatsächliche Bildprompts für Einzelbilder | 251 | 251 Dateien; ausgeführte Requests mit Referenzhashes separat protokolliert |
| Meshy-Hauptprompts | 251 | 251, je 538–659 Zeichen einschließlich Leerzeichen |
| Texturprompts | 251 | 251, je 489–505 Zeichen einschließlich Leerzeichen |
| Deutsche Asset-Spezifikationen | 251 | 251 |
| Meshy-Settings | 251 | 251 |
| Deutsche Familien-Spezifikationen | 11 | 11 |
| Showcase-Bildprompts | 11 | 11 |
| Unity-Incoming-Slots | 251 | 251 vorbereitete Tierordner |
| Serialisierte Unity-Wrapper | 251 | 251 tatsächliche Prefabs mit regulären Unity-Metadaten |
| Direkte Katalogreferenzen | 251 | 251 in einem ScriptableObject-Katalog |

Die fünf Begleitdateien je Slot ergeben **1.255 Dateien**. Die Bildproduktion umfasst **262 finale Rasterbilder**; verworfene Versuche, Referenzmaterial und Testfixtures zählen nicht dazu. Alle 251 Einzelbilder sind echte PNGs mit **1.254 × 1.254 Pixeln** und jeweils unterschiedlichem SHA-256. Die Showcases verwenden ein breites Format; ihre vollständigen Maße und Hashes stehen in der [finalen Bestandsaufnahme](Validation/artifact_inventory.json). Jeder finale Bildpfad liegt unter `Docs/ArtProduction/Buildings`, außerhalb des Unity-Assets-Imports.

Die Face-Ziele liegen zwischen **6.000 und 15.000 Dreiecken**. Die Settings fordern 2.048-Pixel-PBR-Texturen, statische Triangle-Geometrie ohne Rigging/Animation und möglichst ein, höchstens zwei Materialien. Dies sind Produktionsvorgaben für den späteren manuellen 3D-Schritt; Referenzbilder garantieren weder diese tatsächliche Polygonzahl noch korrekte FBX-Topologie.

## Art Direction, Referenzkette und Sichtprüfung

[ART_DIRECTION.md](ART_DIRECTION.md) dokumentiert das gewählte Zusammenspiel aus Reclaimed Green Haven, Herbal Atom Age und Burning Hills Afterglow samt Herkunft der drei kopierten Stilbilder und der fünf Layoutskizzen. Neutrale Einzelassets und atmosphärische Showcases sind als unterschiedliche Ausgabezwecke definiert.

Jede neue Stufe verwendet den akzeptierten T01-Anker und die drei Stilreferenzen; ab T02 kommt zwingend der unmittelbar vorherige akzeptierte Zustand hinzu. T00 wird aus T01 abgeleitet. Die vorbereiteten Requests, tatsächlich verwendeten Prompts, Originalausgaben sowie Bild- und Referenzhashes sind unter `Validation/ImageRequests` und [visual_acceptance.json](Validation/visual_acceptance.json) nachvollziehbar. Ein Status im Manifest allein erteilt keine Freigabe. Geänderte Referenzen, fehlende Vorgänger, doppelte Bildhashes oder veränderte Prompts machen eine Abnahme ungültig.

Die zehn ausbaubaren Familien zeigen in ihren Showcases die tatsächlich akzeptierten Stufen T01, T12 und T24. Das Rathaus besitzt gemäß Auftrag genau einen festen Zustand. Sein elftes Showcase zeigt daher dieses feste Rathaus in der Testumgebung; es wurden keine Rathaus-Ausbaustufen erfunden. Die dreistufigen Showcases verwenden die drei tatsächlichen Tierbilder sowie Reclaimed Green Haven und Afterglow als fünf Referenzeingaben; die akzeptierten Tierbilder tragen bereits die definierte Retro-Technik.

Jedes akzeptierte Bild wurde vor der Aufnahme tatsächlich gesichtet. Zusätzlich sind alle geforderten Stichproben T00, T01, T08, T16 und T24 der zehn Familien, das feste Rathaus und alle elf Showcases durch aktuelle Sichtprüfungen nachgewiesen. Belege: [frühere Stichprobe](Validation/independent_visual_review.json), [abschließende unabhängige Sichtprüfung](Validation/final_independent_visual_review.json) und [Rathausprüfung](Validation/town_hall_visual_review.json). Der [Coverage-Bericht](Validation/visual_coverage_report.json) bestätigt **62 von 62 Pflichtbildern** mit aktuellen, passenden Bildhashes; es fehlen keine Stichproben. Die Beobachtungen stammen aus visuellen Agentenprüfungen und behaupten keine persönliche menschliche Mesh-Abnahme.

Bei der Anbau-Endfolge wurde eine frühere falsche Referenzsubstitution erkannt. T20–T24 wurden ausdrücklich zurückgezogen und mit der vorgeschriebenen direkten Vorgängerkette neu erzeugt. Die Butterfabrik wurde wegen sichtbar leuchtender Eingangsbauteile mit allen 25 Einzelbildern und dem Showcase vollständig korrigiert. Der [Korrekturbeleg](Validation/butter_neutral_correction_complete.json) dokumentiert die neuen Hashes und 26 archivierte Originale. Verworfene Bilder, tatsächliche Prompts und Requests bleiben unter `Validation/Rejected` archiviert; sie werden weder als finale Bilder gezählt noch durch neue Dateien verdeckt. Der frühe Butter-T01 hatte nur eine Direktabnahme; sein Rücknahme-Request wurde aus verifizierten ursprünglichen QA-Hashes rekonstruiert und ausdrücklich als historische Rekonstruktion markiert. Alle neuen Generierungen sind vollständig protokolliert. Die endgültige Freigabe verwendet ausschließlich die aktuelle korrigierte Kette.

## Automatisierte und Unity-Prüfungen

| Gate | Ergebnis | Beleg |
|---|---|---|
| Lokale Metadaten und vollständige Art-Prüfung | Beide Modi bestanden: 251 vollständige Slots, elf Showcases, null Fehler, null offene Artefakte | [Metadatenprüfung](Validation/metadata_validation.json), [vollständige Produktionsprüfung](Validation/unity_preflight_production_validation.json) |
| Python-Regressionen | 25 Tests bestanden; Test-/Validatorhashes entsprechen den aktuellen Quellen | [validation_cache_tests.json](Validation/validation_cache_tests.json), [test_evidence_hash_check.json](Validation/test_evidence_hash_check.json) |
| Abdeckung der 62 visuellen Pflichtstichproben | 62/62 aktuelle Bildhashes und bestandene Sichtprüfungen belegt; keine offenen Art-Befunde | [visual_coverage_report.json](Validation/visual_coverage_report.json) |
| Source-Kompilierung | Acht C#-Dateien in drei Assemblies bestanden, aktuelle Quellhashes bestätigt | [source_compile_report.json](Validation/SourceCompile/source_compile_report.json) |
| Tatsächlicher Unity-Import | Unity 6000.3.9f1 importiert und serialisiert 251 Wrapper plus Katalog | [unity_scaffold.log](Validation/unity_scaffold.log) |
| Tatsächliche EditMode-Tests | 13/13 bestanden, 0 fehlgeschlagen, 0 übersprungen, Exitcode 0 | [unity_editmode_results.xml](Validation/unity_editmode_results.xml), [Teststatus](Validation/unity_editmode_status.json) |
| Scaffold-Idempotenz | Zweiter vollständiger 251-Slot-Lauf im isolierten Test unverändert | `FullScaffoldSecondRunLeavesAll251PrefabsAndCatalogByteIdentical` in der EditMode-XML |
| Erhalt der Produktions-Prefabs | Alle 504 Prefab-/Katalogdateien einschließlich Metadaten nach Tests und finalem Lauf unverändert | [unity_final_preservation.json](Validation/unity_final_preservation.json) |
| Finale reine Unity-Produktionsvalidierung | Bestanden: Exitcode 0, 251 Wrapper, 251 direkte Katalogreferenzen, 251 Referenzbilder, null Fehler | [unity_finalvalidate_status.json](Validation/unity_finalvalidate_status.json), [Intake-Bericht](Validation/unity_intake_report.json) |
| Echter Meshy-FBX-Import und Material-/Collider-QA | Späterer manueller Nutzerschritt; kein Fremdmodell wurde erzeugt oder importiert | [MESHY_WORKFLOW.md](MESHY_WORKFLOW.md) |

Der vollständige lokale Validator prüft alle 251 IDs und lückenlosen Levelbänder, CSV-/JSON-Übereinstimmung, normalisierte projektinterne Pfade, sämtliche fünf Begleitdateien, Promptlängen, Face-Budgets, eindeutige Zielpfade, vollständiges PNG-Decoding, Mindestmaße, doppelte Bildhashes, aktuelle visuelle Abnahmen, echte Produktionsprovenienz und sämtliche vorgeschriebenen Bildreferenzen. Fehlende Bilder dürfen im vollständigen Modus nicht als Erfolg erscheinen. Der reine Datenmodus ist als separate Teilprüfung gekennzeichnet.

Die echte Unity-Abnahme wird ausführlich in [UNITY_ACCEPTANCE.de.md](Validation/UNITY_ACCEPTANCE.de.md) beschrieben. Die 13 Testfälle prüfen unter anderem den vollständigen Scaffold-Lauf, Levelgrenzen, ungültige Pfade/Identitäten/Budgets, direkte Runtime-Zuordnung sowie den Modellwechsel mit Erhalt manueller Child-, VisualRoot- und Anchor-Offsets. Unveränderte Eigenschaften übernehmen die Defaults des Ersatzmodells; fehlt ein manuell bearbeitetes Child im Ersatz, bricht das Update vor dem Speichern ab. Der reine Validierungsmodus verändert keine Assets.

Die Modellwechseltests verwenden durch Unity erzeugte lokale Test-Prefabs. Sie ersetzen keine Prüfung eines echten Meshy-FBX und keine tatsächliche Bedienprüfung des sichtbaren EditorWindow per Drag-and-drop. Game View, PlayMode-Darstellung, Zielgeräte und Performance späterer Fremdmodelle wurden nicht als bestanden ausgegeben.

Die finale Unity-Validierung enthält exakt **251 erwartete Hinweise auf noch leere Modellslots** und keine unerwarteten Warnungen. Diese Slots sind die geforderte Empfangsstruktur für die spätere manuelle FBX-Erzeugung. Der finale Lauf hat ausschließlich gelesen und den expliziten Prüfbericht exportiert. Unity wurde regulär beendet; danach waren weder ein Unity-Prozess noch ein Unity-Lock vorhanden.

## Projekterhalt

Alle 60 ursprünglichen Projektdateien stimmen nach den kontrollierten Unity-Läufen mit dem Hashstand unmittelbar vor diesen Läufen überein. `Assets/Scenes/SampleScene.unity` blieb byteidentisch. Gegenüber der frühen Sitzungsbaseline bestand bereits vor dem eigenen Unity-Start eine Änderung an `Assets/Settings/Mobile_RPAsset.asset`; ihre Herkunft ist nicht festgestellt. Sie wurde unverändert erhalten und nicht auf die alte Version zurückgesetzt. Der erneute [historische Baselinevergleich](Validation/preservation_report.json) meldet daher ehrlich diese eine Abweichung und keinen pauschalen Erfolg für die alte Baseline. Der [frische kontrollierte Erhaltungscheck](Validation/unity_final_preservation.json) bestätigt hingegen unveränderte 60 Originaldateien und 504 Prefab-/Katalogdateien gegenüber ihren dokumentierten Vorher-Ständen. Weitere Belege: [ursprüngliche Baseline](Validation/original_project_hashes.json) und [Stand vor Unity](Validation/unity_originals_before.json).

Die neuen Produktdateien liegen unter `Docs/ArtProduction` und `Assets/Weedtopia`. Unity-Metadaten wurden regulär durch Unity erstellt. Es wurden keine statischen Szenenobjekte zur Laufzeit erzeugt, keine Quellmodelle überschrieben und keine Gameplay-, Backend-, Addressables- oder Wirtschaftssimulationsfunktionen ergänzt. Das Projekt ist kein Git-Repository; es gab deshalb weder erfundene Git-Abnahme noch Commit-, Push-, Branch- oder Reset-Aktionen.

## Nächste manuelle Schritte

1. Im [Asset-Index](BUILDING_ASSET_INDEX.md) eine freigegebene Familie/Stufe wählen und deren Referenzbild, deutsche Spezifikation und englischen Meshy-Prompt öffnen.
2. Später selbst in Meshy die Bild-/3D-Erzeugung mit den slotbezogenen Settings ausführen. Die aktuelle Meshy-Oberfläche und deren Kosten wurden in diesem Auftrag nicht geöffnet oder live verifiziert; `smart-topology` ist entsprechend seiner tatsächlichen Verfügbarkeit zu verwenden.
3. Das Modell als FBX zusammen mit dem vollständigen Texturordner exportieren und unter `Assets/Weedtopia/Art/Buildings/Incoming/<building_id>/<tier_id>/` ablegen.
4. In Unity **Weedtopia → Art Pipeline → Building Tier Intake** öffnen, die Stufe wählen, den importierten Model-Prefab in das ObjectField ziehen und **Wrapper mit diesem Modell erstellen / aktualisieren** anklicken.
5. Am Wrapper die Ausrichtung `+Z`, die Hochachse `+Y`, den Metermaßstab, Bodenpivot, maximale Stellfläche/Höhe, URP-Materialien und Glas prüfen. Korrekturen erfolgen am `VisualRoot`; Anchor- und Child-Anpassungen werden beim späteren Update bewahrt. Vereinfachte Collider werden sichtbar unter `ColliderRoot` eingerichtet und geprüft.
6. Den reinen Validierungsmodus ausführen. Fehlende Modelle werden bis zur Zuweisung als erwartete Warnungen gemeldet. Diese Warnungen sind keine automatische Freigabe der späteren Geometrie.

Die vollständige Schrittfolge und die wiederholbaren Prüfkommandos stehen in [MESHY_WORKFLOW.md](MESHY_WORKFLOW.md) und [Tools/README.md](Tools/README.md). Enge, unbeschnittene Bildränder, im Einzelblick verdeckte Rückseiten und die tatsächliche Rekonstruierbarkeit dichter Pflanzen-/Glasbereiche bleiben bei der 3D-Erzeugung zu prüfen; Referenzmaßangaben sind Gestaltungsziele, keine vermessenen Mesh-Bounds.

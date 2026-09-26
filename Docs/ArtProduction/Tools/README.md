# Lokale Produktionsprüfung und Wiederaufnahme

Python 3.11+; für vollständiges PNG-Decoding wird Pillow benötigt. Auf diesem Rechner stehen Python 3.12 und Pillow 10.2 zur Verfügung. Das Werkzeug führt keine Netzwerkaufrufe, Bildgenerierung, Unity-Starts oder Änderungen an Quellmodellen aus.

Alle Befehle werden im Unity-Projektroot ausgeführt. Die Python-Datei findet den Root auch unabhängig vom aktuellen Arbeitsverzeichnis. Für ein anderes Projekt ist `--root <Projektroot>` vor dem Unterbefehl möglich.

```powershell
python Docs/ArtProduction/Tools/validate_pipeline.py validate --data-only --report Docs/ArtProduction/Validation/metadata_validation.json
python Docs/ArtProduction/Tools/validate_pipeline.py validate
python Docs/ArtProduction/Tools/validate_pipeline.py sync-progress
python Docs/ArtProduction/Tools/test_validate_pipeline.py
python Docs/ArtProduction/Tools/validate_visual_coverage.py
```

`--data-only` prüft vollständige Produktionsdaten, Budget, exakte CSV/JSON-Übereinstimmung, Pfadgrenzen und alle bereits vorhandenen Bilder. Fehlende Bilder, Showcases, visuelle Abnahmen und noch nicht durch Unity erzeugte Prefabs bleiben als `pending` sichtbar. Ein Erfolg dieses Modus bedeutet ausschließlich valide Metadaten. Der vollständige Modus schlägt auch bei jedem offenen Pflichtartefakt fehl. Exitcode 0 bedeutet Erfolg im ausgewählten Modus, 1 einen dokumentierten Prüfungsfehler oder eine offene Vollständigkeitsanforderung und 2 einen nicht ausführbaren Befehl.

Die PNG-Prüfung kontrolliert den Dateityp, prüft den kompletten Datenstrom, decodiert sämtliche Pixel, prüft Quadrat/Mindestauflösung beziehungsweise breites Showcaseformat und berechnet SHA-256. Sie ersetzt keine visuelle Qualitätsprüfung.

Ein Bild wird erst nach einer tatsächlichen Sichtprüfung akzeptiert. Die finale PNG muss bereits unter dem Manifestpfad liegen. `image_prompt_en.txt` muss exakt den tatsächlich für die Generierung verwendeten vollständigen Prompt enthalten. Die originale Werkzeugausgabe wird als unveränderte Quelle angegeben. Jede bei der Generierung verwendete Referenz wird wiederholt mit `--reference` übergeben. Visuelle QA-Notizen beschreiben konkret Motiv, vollständige Freistellung, Frontorientierung, Stil, Stufenänderung, Text-/Personenfreiheit und etwaige Auffälligkeiten.

```powershell
python Docs/ArtProduction/Tools/validate_pipeline.py accept --slot cultivation_plot/T01_L001 --notes "Konkrete Ergebnisse der tatsächlichen visuellen Prüfung" --source-image "C:/Pfad/zur/originalen/Tool-Ausgabe.png" --prompt-file Docs/ArtProduction/Buildings/cultivation_plot/T01_L001/image_prompt_en.txt --reference Docs/ArtProduction/References/ChosenStyle/Reclaimed_Green_Haven.png
```

Die im Beispiel genannte Referenz muss unter ihrem tatsächlichen Dateinamen übergeben werden; alle vorhandenen ausgewählten Style-Referenzen sind zu protokollieren. Für T00 und T02–T24 ist zusätzlich der akzeptierte T01-Anker derselben Familie erforderlich. T02–T24 benötigen außerdem die unmittelbar vorherige akzeptierte Stufe. T00 ist kein Geometrievorgänger für T02. Ein erneut ausgeführtes `accept` verändert einen bereits aktuellen Abnahmestempel nicht. Ein nachträglich geändertes, bereits akzeptiertes Bild wird abgelehnt, bis die Änderung ausdrücklich mit QA-Notiz zurückgewiesen und anschließend neu geprüft wurde. Der Validator erzeugt und überschreibt selbst keinerlei Bilder.

```powershell
python Docs/ArtProduction/Tools/validate_pipeline.py reject --slot cultivation_plot/T01_L001 --notes "Konkreter visueller Ablehnungsgrund"
python Docs/ArtProduction/Tools/validate_pipeline.py accept --showcase cultivation_plot --notes "T01/T12/T24 und Afterglow-Konsistenz visuell geprüft" --source-image "C:/Pfad/zur/originalen/Showcase-Ausgabe.png" --prompt-file Docs/ArtProduction/Buildings/cultivation_plot/family_showcase_prompt_en.txt --reference Docs/ArtProduction/Buildings/cultivation_plot/T01_L001/mesh_input_3q.png
python Docs/ArtProduction/Tools/validate_pipeline.py set-blocker --reason "Exakter beobachteter externer Bildwerkzeug-Blocker"
python Docs/ArtProduction/Tools/validate_pipeline.py block --code image_tool_unavailable --message "Exakte beobachtete Werkzeugfehlermeldung" --slot cultivation_plot/T01_L001
python Docs/ArtProduction/Tools/validate_pipeline.py block --code unity_editor_active --message "Laufender Editor verhindert konfliktfreien separaten Import-/Testlauf"
python Docs/ArtProduction/Tools/validate_pipeline.py clear-blockers
```

`Validation/visual_acceptance.json` bindet Abnahmen an Bild-, tatsächlichen Prompt- und Referenzhashes. Der ursprüngliche temporäre Werkzeugpfad darf später verschwinden; der protokollierte ursprüngliche Hash muss weiterhin dem finalen Bild entsprechen. Die lokalen Referenzbilder müssen erhalten bleiben. Manifeststatus allein akzeptiert kein Bild.

`PROGRESS.json` wird mit temporärer Datei im selben Ordner, Flush, fsync und atomarem Replace gespeichert. Fehlende Bilder werden als `pending`, niemals allein wegen ihrer Abwesenheit als `failed` gezählt. Reihenfolge: zehn T01-Anker, festes Rathaus, anschließend je Familie T00 und T02–T24, zuletzt elf Showcases. `last_completed_slot` benennt den letzten vollständigen Slot in dieser deterministischen Reihenfolge; `next_slot` den ersten noch nicht vollständigen. Jede Abnahme aktualisiert den Stand, somit auch jede vollständige Familie. `art_complete` ist ausdrücklich keine Behauptung über Unity-Import, EditMode-Tests oder spätere FBX-Qualität. Diese Gates bleiben getrennt dokumentiert.

Bei `sync-progress`, `accept` und `reject` werden die berechneten Slotstatus auch in Manifest und CSV projiziert: `pending_image`, `image_pending_review`, `accepted` oder `failed`. Jede Datei wird atomar geschrieben. Ein Prozessabbruch genau zwischen Manifest- und CSV-Ersetzung kann eine erkennbare Statusabweichung hinterlassen; erneutes `sync-progress` repariert diese Projektion, ohne andere Metadatenabweichungen zu überdecken. `block` ersetzt einen vorhandenen Eintrag mit gleichem Code und Slot, sodass wiederholte beobachtete Blocker keine Duplikate erzeugen.

Die Regressionstests erzeugen kleine synthetische Fixtures ausschließlich in einem temporären Testverzeichnis. Diese Bilder sind Testdaten für Größen-, Duplikat- und Manipulationsprüfungen und werden niemals als Produktionsreferenzen verwendet.

`validate_visual_coverage.py` prüft ergänzend die protokollierten Sichtprüfungen der exakt 62 Pflichtstichproben: zehnmal T00/T01/T08/T16/T24, das feste Rathaus und elf Showcases. Es akzeptiert nur einen als bestanden protokollierten Review mit dem aktuellen Bildhash und schreibt `Validation/visual_coverage_report.json`. Das Werkzeug führt selbst keine Bildsichtprüfung aus und verändert keine Freigaben oder Fortschrittsdaten. Historische, inzwischen ersetzte Bilder schließen die aktuelle Stichprobe nicht.

## Geschützte Wiederaufnahme und Referenzkette

`prepare_image_job.py --slot <Familie/Tier>` und `ingest_generated_image.py` verwenden dieselbe Windows-Dateisperre. Vor dem Vorbereiten des nächsten Requests muss ein laufender Ingest-Prozess vollständig beendet sein. Falls `exec_command` eine Session-ID liefert, sein endgültiges Prozessende abwarten; ein leerer Zwischenausgabestand ist noch kein JSON-Request.

Die Referenzkette ist zwingend: drei Stilbilder plus T01 und unmittelbarer akzeptierter Vorgänger. T00 verwendet T01, T02 benötigt T01 nur einmal. Ein früheres Layoutbild darf den direkten Vorgänger niemals ersetzen. Die optionale Angabe `--predecessor-first` ändert ausschließlich die Reihenfolge zu Vorgänger, T01, drei Stilbilder; der tatsächlich verwendete Prompt muss die Rollen dieser Reihenfolge korrekt beschreiben. Gespeicherte historische Layout-Override-Metadaten schaffen keine Ausnahme und werden bei aktueller QA nicht als Ersatz gewertet.

Die Aufnahme prüft vor jeder Akzeptanz die aktuelle SHA-256 aller Referenzen gegen den vorbereiteten Request. Eine nach unterbrochener Aufnahme bereits vorhandene Zieldatei ist nur bei exakt identischem Hash wiederaufnehmbar. Abweichende Dateien und bereits akzeptierte Bilder werden geschützt.

Eine bestehende Abnahme lässt sich ausschließlich ausdrücklich zurückziehen, ohne ein Bild zu überschreiben:

```powershell
python Docs/ArtProduction/Tools/ingest_generated_image.py --slot city_warehouse/T15_L900 --source Docs/ArtProduction/Buildings/city_warehouse/T15_L900/mesh_input_3q.png --reject --reopen --notes "Konkreter Grund fuer die Zuruecknahme"
```

`--reopen` ist nur zusammen mit `--reject` zulässig. Das aktuelle Bild, der aktuelle Prompt, der ursprüngliche vorbereitete Prompt und der Request werden unter `Validation/Rejected` archiviert; die frühere Abnahme bleibt in der Historie. Erst danach darf ein neuer Request vorbereitet werden. Dadurch werden auch fehlerhafte frühere Referenzketten ehrlich zurückgezogen.

Showcase-Requests entstehen mit `prepare_showcase_job.py --family <Familie>` erst nach vollständiger Familienabnahme. Zehn ausbaubare Familien verwenden die drei tatsächlichen Bilder T01, T12 und T24 sowie Reclaimed Green Haven und Afterglow, insgesamt fünf Eingaben. Ihre bereits akzeptierten Modelle tragen die Retro-Technik. Das Rathaus verwendet sein einzelnes festes Tier plus dieselben zwei Stilreferenzen. Die normale Aufnahme unterstützt dafür `ingest_generated_image.py --showcase <Familie> --source <Originalausgabe> --notes <Sichtpruefung>`.

Each validation call creates a fresh in-memory SHA/PNG memo keyed by resolved path, file identity, size, and nanosecond modification/change timestamps. Repeated unchanged references reuse that call's actual-byte result; the next validation performs fresh hashing and full PNG decoding. No cache persists acceptance between calls. Files changed while reading fail validation.

## Reguläre Unity-Abnahme

Der lokale Runner prüft vor jedem Start, dass kein Unity-Prozess und kein `Temp/UnityLockfile` vorhanden sind. Er verändert keinen Lock und beendet keinen fremden Prozess. Unity läuft verborgen im Batchmodus mit seiner regulären lokalen Cache-/Lizenzumgebung; die restriktive Dateisandbox dieses Rechners verhinderte den Editorstart vor Erzeugung einer Logdatei.

```powershell
& .\Docs\ArtProduction\Tools\run_unity_acceptance.ps1 -Mode EditMode
& .\Docs\ArtProduction\Tools\run_unity_acceptance.ps1 -Mode FinalValidate
```

`EditMode` lässt den Unity Test Runner regulär selbst beenden und prüft die frisch erzeugte XML-Datei. `FinalValidate` führt zuerst den vollständigen lokalen Produktionsvalidator aus. Erst bei vollständigem Erfolg startet es Unity mit der reinen `ValidateAndWriteReport`-Methode; es erstellt und repariert keine Assets. Jeder Modus schreibt sein eigenes Log und einen Statusbericht unter `Validation`.

Der erste Scaffold-Lauf hat alle 251 Wrapper und Katalogreferenzen angelegt. Sein abschließender Produktionscheck war wegen damals noch fehlender Parkbilder unvollständig; dieser historische Bericht liegt separat unter `Validation/unity_intake_initial_report.json`. Das ist kein Compiler- oder Strukturfehler. Die späteren EditMode-Tests bestanden mit 13/13 Testfällen; `unity_editmode_results.xml` ist der tatsächliche Unity-Nachweis. Die abschließende Produktions-/Intake-Abnahme bleibt ein separates Gate.

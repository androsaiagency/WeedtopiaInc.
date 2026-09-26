# Ausgangsinventar am 14.09.2026

- Projekt: `D:/Unity Games/Weedtopia Inc/WeedtopiaInc`.
- Unity laut `ProjectSettings/ProjectVersion.txt`: `6000.3.9f1 (7a9955a4f2fa)`.
- URP laut `Packages/manifest.json`: `17.3.0`; Test Framework bereits vorhanden: `1.6.0`.
- Kein `.git` im Projektroot; keine Git-Aktion durchgeführt.
- Vor Beginn kein `Docs/ArtProduction/PROGRESS.json` und kein `Assets/Weedtopia`.
- Keine weiteren `AGENTS.md` in den geprüften Vorfahren beziehungsweise unter Assets, Docs und Packages. Die im Auftrag mitgegebenen AGENTS-Anweisungen gelten.
- `Get-Process Unity`: laufender Prozess PID `45660`, Pfad `D:/Unity Games/6000.3.9f1/Editor/Unity.exe`. Fenstertitel leer. Prozesszuordnung zum Projekt nicht belegt.
- `Temp/UnityLockfile` im Projekt bei Erstprüfung nicht vorhanden. Der laufende Unity-Prozess reicht gemäß Arbeitsauftrag trotzdem aus, um keine zweite Instanz zu starten.
- Die CIM-Abfrage der Prozess-Kommandozeile wurde vom System mit `Zugriff verweigert` beantwortet. Die alternative reine Prozessabfrage war erfolgreich. Kein Prozess beendet, kein Lock verändert.
- `original_project_hashes.json` erfasst 60 vorhandene Dateien unter Assets, Packages und ProjectSettings, ohne den neuen Bereich Assets/Weedtopia. Es dient der abschließenden Integritätskontrolle.
- Ursprünglicher SHA256 von `Assets/Scenes/SampleScene.unity`: `07FCE8B04804DDCCB3026DB5B43D8DC41BA138E49D50D36A5C24877081EACDCA`.
- Das gesamte Szenario aus `D:/Unity Games/Weedtopia Inc/prompt.txt` wurde gelesen.
- Alle fünf Layoutskizzen und drei ausgewählten Stilbilder waren vorhanden und wurden einzeln visuell betrachtet. Ihre Originale wurden unverändert belassen; Kopien liegen unter `References/Layouts` und `References/ChosenStyle`.
- Die Skizzen bestätigen die Farm-Kette Anbau → Veredelung → Lager, die zentrale Stadt-Verteilung, austauschbare Siedlungsparzellen und das Industriegebiet mit Butterfabrik, Supermarkt und Bäckereien. Handschrift, weiße Zeichenfläche und Skizzenproportionen werden nicht als Art Direction übernommen.
- Die Stilbilder liefern warme reparierte Baustoffe, runde emaillierte Technik und hoffnungsvolle Dämmerungsatmosphäre. Ihre Personen, Fahrzeuge, Landschaft, Förderbänder und dramatische Beleuchtung werden nicht in einzelne Meshy-Eingabebilder übernommen.

## Prüfgrenzen

Eine separate Roslyn-Kompilierung gegen die lokal installierten Unity-Assemblies kann C#-Fehler erkennen, belegt aber weder Unity-Import noch ScriptableObject-/Prefab-Serialisierung noch ausgeführte EditMode-Tests. Diese Gates bleiben ausdrücklich getrennt.

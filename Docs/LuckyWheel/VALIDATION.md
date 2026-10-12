# Abschlussprüfung – tägliches Glücksrad

Der implementierte Stand ist automatisch geprüft. Persönliche Spiel- und Grafikabnahme bleibt offen.

| Prüfung | Ergebnis |
|---|---|
| Source-Compiler | 12/12 Assemblies, alle kompilierten Quelldateien entsprechen dem aktuellen Stand |
| Vollständige Spielregel-Tests | 169/169 bestanden, einschließlich 16 Glücksrad-Fällen |
| Vollständige PlayMode-Tests | 56/56 bestanden |
| Betroffene UI nach visuellen Korrekturen | 12/12 bestanden (Glücksrad und Belohnungs-/Inventar-Präsentation) |
| Asset-Referenzen und wiederholter Installer | 2/2 bestanden; 26 bestehende Dateien bytegleich |
| Echte Game View | 12 Aufnahmen, 1920×1080, 1366×768 mit UI-Skalierung 1,15, 2560×1080 und 1080×1920 |
| Layout-Prüfung | 0 Textüberläufe, 0 Aktionen außerhalb des sicheren Bildschirmbereichs |
| Bewahrung | Alle 9.167 vorherigen Szenenobjekt-IDs erhalten, 10 zusätzliche Szenenobjekte; vorhandene Inhaltswerte und Inventar-Prefab unverändert |

Der erste vollständige EditMode-Lauf fand die Besonderheit von Unitys Serialisierung optionaler Objekte sowie einen zu frühen Zeitstempel beim Initialisieren des Rads. Beide Fehler wurden korrigiert; der gesamte EditMode-Lauf wurde danach erfolgreich wiederholt. Anschließende Änderungen betrafen ausschließlich UI-Geometrie, Icon-Clipping, Seltenheitsfarbe und Editor-Beschriftungen. Die betroffenen PlayMode-Tests und die Asset-Prüfung wurden erneut ausgeführt.

Der Gratis-Dreh und die Erneuerung verwenden 00:00 UTC. Das Rad enthält täglich genau zehn verschiedene Item-Gewinne mit gleichen Chancen. Neun optionale zusätzliche Drehs kosten zusammen 9.850 Grinder. Save-Tests prüfen tatsächliches Laden und Speichern, Einzelvergabe, Kostenfolge, Überläufe, Tageswechsel, Uhr-Rückstellung und Rücknahme bei einem Speicherfehler. Pointer-Tests prüfen die obersten Raycast-Treffer auf Radfeldern, Dreh-/Abbruch-/Inventar-Buttons und beiden Einstiegspunkten.

Die Aufnahmen entstanden in einer separaten Diagnose-Speicherdatei unter `Docs/LuckyWheel/capture_save.json`. Nur für das Bild des geleerten Rads wurde dort Testguthaben gesetzt. Benutzer-Spielstände und Produktionskunst wurden dafür nicht geändert. Der gemeinsame Belohnungskarten-Prefab erhielt einen zusätzlichen Icon-Clipbereich; seine GUID, alle bisherigen Objekte und die Original-Sprites bleiben erhalten.

Siehe `completion_receipt.json` für genaue Run-IDs und den Fingerprint, `gameview_receipt.json` für die Bilddateien, `ui_overflow.json` für die Layoutprüfung und `Originals/` für die Ausgangsdateien. Die Original-Testlogs und XML-Ergebnisse liegen unter `Docs/Implementation/Validation`.

# Validierung – Inventar

Stand: 30.09.2026. Unity 6000.3.9f1. Alle Unity-Prüfungen liefen nacheinander in eigenen, versteckten Batch-Prozessen mit Projekt-Lock-Prüfung. Es wurde kein fremder Editor beendet und kein persönlicher Spielstand verwendet.

## Bestandene Prüfungen

| Prüfung | Ergebnis | Nachweis |
| --- | --- | --- |
| Quellcode | 12 Assemblies kompiliert; vorhandene CS0649-Warnungen | `source_compile_receipt.json` |
| Spielregeln / Speichern, vollständige Game-EditMode-Suite | **153/153 bestanden** | `../Implementation/Validation/20260930_224037_EditMode_InventoryFinal.xml` |
| Inventar / Belohnungen, gezielter PlayMode-Nachtest | **8/8 bestanden** | `../Implementation/Validation/20260930_224931_PlayMode_InventoryUiFinal.xml` |
| Gesamte PlayMode-Suite vor letzter Anzeige-Korrektur | 51/52 bestanden; der einzige Fehler ist im obigen Nachtest behoben | `../Implementation/Validation/20260930_224438_PlayMode_InventoryFinal.xml` |
| Grafik- und Referenz-Preflight | **1/1 bestanden**; unabhängige Release-Abnahmen bleiben offen | `../Implementation/Validation/20260930_225048_EditMode_InventoryAssetFinal.xml` |
| Installer wiederholen | **20 Dateien bytegleich**, keine Änderungen | `idempotence_receipt.json` |
| Spieldaten erhalten | Bestehende Wirtschaft, Weltdefinitionen, Ziele und Belohnungen unverändert | `content_preservation.json` |
| Szene erhalten | 9.167 Objekte vor und nach der Änderung; kein Objekt hinzugefügt/entfernt; nur 3 bestehende Prefab-Instanzen mit geänderten Overrides | `scene_preservation.json` |

Der umfassende erste EditMode-Lauf umfasste außerdem 14 Art-Pipeline- und 36 Asset-Tests. 193/195 bestanden. Ein neuer Speichertest hatte eine außerhalb der echten Bauzeitgrenzen liegende Test-Fixture; diese wurde korrigiert. Der Grafik-Preflight erkannte fehlende Sprite-Referenzen auf absichtlich einfarbigen UI-Flächen; diese besitzen nun eine authored `UiSurface.png`. Beide betroffenen Prüfungen wurden anschließend erfolgreich wiederholt.

Der Fehler im ersten PlayMode-Gesamtlauf betraf das sofortige Befüllen der Guthabenanzeige beim Öffnen. Die Anzeige wird jetzt sowohl beim Öffnen als auch während der laufenden Aktualisierung befüllt. Alle acht Tests der betroffenen Inventar-/Belohnungs-UI wurden nach dieser Änderung erneut ausgeführt und bestanden. Die übrigen 44 PlayMode-Tests wurden nach dieser reinen Anzeige-Korrektur nicht noch einmal ausgeführt.

## Prüfinhalte

- Alle acht Zeitwerte, exakt reduzierte Restzeit, Abschluss ohne negative Restzeit und Erstbau-Tagesanspruch.
- Kein Fortschritt anderer Baustellen, keine zusätzliche simulierte Weltzeit oder kostenlose Ressourcenproduktion durch Beschleuniger.
- Einmalige Bestätigungen; veraltete, abgebrochene und bereits verbrauchte Angebote belasten keine weiteren Items.
- Verlustfreies Aufteilen aller sieben größeren Stückelungen; Schutz gegen Bestandsüberlauf.
- Tatsächliche Inhalte von Kartenpaketen, Geldkisten und Grinder-Beuteln; Worker-Berufswahl mit zwei gültigen Eigenschaften.
- Quest- und VIP-Vergabe, Belohnungsvorschau und gespeicherte Belege.
- Save-Roundtrip nach Verbrauch und Aufteilen; alte Saves ohne Inventarfeld; Zurückweisung beschädigter Bestände.
- Kategorien, read-only Item-Inspektion, Übergang in die Teamverwaltung, Abbruch durch Navigation und korrektes Fortsetzen der Simulation.
- EventSystem-Raycast-Prüfungen für Inventar-Slots, Kategorien, Aufteilen, Erfolge, Verlauf, Schließen und Belohnungsbuttons.

## Game View

Die Aufnahmen entstehen in der echten Szene mit den produktiven Prefabs. Ein separater diagnostischer Spielstand durchläuft die Hauptquests über die vorhandenen öffentlichen Spielbefehle. Die dargestellten Items stammen aus diesen Questbelohnungen; die gezeigte Baustelle wurde regulär mit verdientem Geld begonnen.

**13 finale Game-View-Aufnahmen erstellt; 0 gemeldete Text- oder Bedienflächen-Überläufe.** Die Ergebnisse liegen in `gameview_receipt.json`, `ui_overflow.json` und `Screenshots/`. Die dokumentierten Auflösungen sind 1920×1080, 1366×768 mit UI-Skalierung 1,15, 2560×1080 und 1080×1920. Raster, Detailansichten, Berufswahl, Aufteilen, kleine Fenster und Hochformat wurden zusätzlich bildlich geprüft.

Finaler Capture-Lauf: `../Implementation/Validation/20260930_225355_InventoryCapture_Final.json`. Fingerprint: `3db6f4bff6a120c2152f48365648d77a97e173b7b68a7c8e9410f77561cc1b31`.

Eine persönliche Spiel-/Gestaltungsabnahme durch den Nutzer und physische Touch-Tests wurden dadurch nicht ersetzt. Diese Änderung enthält keine neue Audio-Generierung oder Audio-Abnahme.

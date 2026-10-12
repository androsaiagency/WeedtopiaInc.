# Weedtopia – zweite UI-Revision

In Burning Hills und Big Rock City installiert. Unity 6000.3.9f1, 03.10.2026.

## Ergebnis

- Eigenes bildschirmfüllendes Hauptmenü mit gemalter Weedtopia-Stadt, transparentem Weedtopia-Schriftzug und einem einzigen großen Play-Button. Hintergrund verwendet Cover, Logo erhält sein Seitenverhältnis, Play ist eine vektorbasiert gerundete Kapsel. Die leere Spielwelt ist im Startmenü nicht sichtbar.
- Play lädt den vorhandenen Spielstand oder beginnt ohne Spielstand ein neues Spiel. Name mit Rocko, Profil/Avatar und bestätigter Neustart über Einstellungen bleiben erhalten.
- Aktive Aufgaben erscheinen links oben als runde Buttons mit Symbol und Nummer/Erledigt-Markierung. Ein Klick öffnet die kompakte Vorschau mit dem nächsten konkreten Schritt. „Auftrag öffnen“ führt zur vollständigen Checkliste. Zielnavigation und Belohnung verwenden die bestehenden Questdienste.
- Petrol-/Jade-Fenster mit zurückhaltenden hellen Rändern ersetzen die verzogenen goldenen Ornamente. Inhalt erhält einen eigenen maskierten Bereich; Überschrift und Schließen haben reservierten Platz. Buttons haben innere Textabstände. Team, Inventar, Stadtgeschichten, Städteübersicht und Glücksrad verwenden responsive oder scrollbare Inhalte für schmale Ansichten.
- Weltklicks auf Standard-NPCs übergeben direkt die angeklickte Sprecher-ID. Der vorher ausgewählte oder gesperrte Arbeitsplatz entscheidet nicht mehr über den Gesprächspartner. Gesichter links oberhalb der Box schauen nach rechts.
- Normaler Dialog und Einführung werden durch einen Klick auf die gesamte Bildschirmfläche weitergeschaltet und nach dem letzten Abschnitt geschlossen. Weiter- und X-Buttons sind dort ausgeblendet. Die Namenseingabe behält ihre notwendige Bestätigung.

## Prüfungen

| Prüfung | Bestanden | Nachweis |
|---|---:|---|
| PlayMode | 80 / 80 | [XML](../Implementation/Validation/20261003_121302_PlayMode_UiPolishFinalRegression.xml) |
| Assets | 47 / 47 | [XML](../Implementation/Validation/20261003_121854_EditMode_UiPolishAssetsFinal.xml) |

Die PlayMode-Tests umfassen echte Welt-Raycasts auf die Standard-NPCs in beiden Städten, korrekte Identität trotz gesperrter Arbeitsplätze, Klicks an Bildschirmrändern zum Weiterführen/Beenden, runde Aufgabenbuttons mit Vorschau, Play/Save/Profile/Neustart, erreichbare Schließen-Buttons und gescrollte Inventaraktionen. Die komplette Asset-Suite enthält die erneute Installer-Ausführung samt Bytevergleich und die Prüfung aller bestehenden UI-Bindungen.

- [Gesamtreceipt](validation_summary.json) verbindet die aktuellen abgeschlossenen Testläufe und ihre XML-/Logpfade.
- [Galerie](ReviewGallery.html): 105 Game Views in 1920×1080, 1280×720, 844×390, 390×844 und 2340×1080. [Layout-Receipt](capture_receipt.json) enthält die tatsächlich gerenderten Auflösungen und gemeldeten Probleme.
- [Erhaltung](preservation.json): 293 Welt-/Actor-/Lieferprefabs bytegleich. In jeder Stadt 1.480 Datensätze außerhalb der Canvas-Hierarchie ohne Änderung. Gameplay-Content bleibt gegenüber dem Start dieser zweiten Revision bytegleich.
- [Wiederholbarkeit](idempotence.json): Szenen, gemeinsame UI- und Modul-Prefabs, Titelbilder und Importmetadaten bleiben bei erneutem explizitem Installer-Lauf bytegleich.
- [Art-Prompts und Provenienz](ArtPrompts.md): Hintergrund und Logo wurden mit dem eingebauten ImageGen-Werkzeug erzeugt und unverändert übernommen. Fünf neue UI-Surfaces sind originale deterministische Zeichnungsprimitive. Vorhandene Weltkunst wurde nicht ersetzt.

## Aufbau

`TitleScreen.prefab`, `QuestPreview.prefab`, `QuestShortcut.prefab` und `DialogueStage.prefab` sind gemeinsame authored Prefabs. Die Rahmen/Scrollflächen der vorhandenen Module liegen in deren ursprünglichen Source-Prefabs. Feste Controls werden nicht zur Laufzeit erzeugt. Nur bestehende dynamische Listen bleiben dynamisch.

`ModalFrame` schützt Header und Ecken, `TeamPanelLayout` stellt Roster und Details nebeneinander beziehungsweise untereinander, `ModulePanelLayout` setzt vorhandene Modul-Controls lesbar in scrollbaren Flächen. `CapsuleButtonGraphic` zeichnet den Titel-Play-Button mit einer an die tatsächliche Höhe angepassten Rundung. `QuestTracker` führt von der Vorschau in die Details. `DialoguePanel` besitzt die authored Bildschirm-Klickfläche und Porträtausrichtung.

Explizite, bereits ausgeführte Installation: `Weedtopia > Presentation > Install Polished UI`. Sie verweigert Play Mode/ungespeicherte Szenen, aktualisiert Source-Prefabs vor den Szenen und prüft bestehende Bindungen vor dem Speichern. Erneutes Installieren ist getestet. Alte Menüobjekte bleiben deaktiviert unter `ArchivedUiBeforeExperience`. Der Task-Snapshot liegt in `Baseline`.

## Abnahmegrenzen

Die Aufnahmen und automatischen Trefferprüfungen sind echte Editor-Prüfungen, ersetzen aber keinen Test auf einem physischen Android-Gerät. Bildschirmtastatur, konkrete Displayaussparungen, Touch-Gefühl und Geräteperformance sind noch offen. Kein APK für diese Revision gebaut. Deine visuelle Freigabe ist nicht durch Tests oder Screenshots vorweggenommen.

# Weedtopia – Startmenü, Dialogführung, Profil und Questführung

In Burning Hills und Big Rock City installiert. Datum: 03.10.2026. Unity: 6000.3.9f1.

## Sichtbares Ergebnis

- Das Startmenü zeigt WEEDTOPIA, das vorhandene Weedtopia-Artwork und einen großen Play-Button. Play lädt den vorhandenen Spielstand; ohne Spielstand beginnt der neue Namensdialog mit Rocko.
- Rocko fragt nach einem Namen mit 2–24 Zeichen, bevor das Tutorial fortgesetzt werden kann. Bestehende Spielstände behalten ihren Namen und bekommen bei Bedarf den Standardavatar.
- Einstellungen enthält die Tabs Spiel und Profil. Im Profil werden Name und einer von acht Avataren gemeinsam gespeichert, ohne Stadtfortschritt zurückzusetzen.
- Neues Spiel steht im Tab Spiel. Der Bestätigungsdialog kann abgebrochen werden. Vor dem Neustart wird der bisherige Stand als eigene `*.before_restart_*.json`-Datei gesichert, die spätere Autosaves nicht überschreiben.
- NPC-Gespräche verwenden eine gemeinsame untere Dialogbox. Das Porträt sitzt links darüber. Acht vorhandene Begrüßungsporträts sowie je acht erklärende und erfreute Varianten sind eingebunden. 32 Tutorial-Quests haben eigene Gesprächs- und Erfolgstexte in natürlicher NPC-Sprache; Handlungsanweisungen stehen in der Questliste.
- Aktive Tutorial-, Stadtprojekt-, angenommene Story- und VIP-Aufgaben sind oben links als Buttons erreichbar. Die Details nennen jeden notwendigen Schritt mit aktuellem Fortschritt, erledigte Punkte, den nächsten Schritt und die Belohnungsabholung. Ein Zielpunkt öffnet das passende Gebäude beziehungsweise den passenden Auftrag.
- Das Worker-Fenster hat einen eigenen großen Schließen-Button unterhalb der globalen Einstellungen. Während der Auswahl wird der darunterliegende Inspektor ausgeblendet. Globale Menüs schließen den Inspektor und die Worker-Auswahl und bringen danach die Navigation wieder zurück.

## Der unklare Coffeeshop-Schritt

Der bestehende Stadtfest-Plan verlangt in diesem Abschnitt **Coffeeshop 2 (`coffee_02`)** auf **Stufe 5**, eine eingesetzte **Leitung** für den automatischen Betrieb sowie **360 verkaufte Blüten seit Abschnittsbeginn**. Andere besetzte Coffeeshops ersetzen die konkrete Gebäudevoraussetzung nicht. Die Liste liest dieselben Bedingungen und Zähler wie die bestehende Belohnungsprüfung.

Das Aufnahmebeispiel simuliert vier Coffeeshops, davon drei auf Stufe 5 und besetzt. Coffeeshop 2 ist Stufe 1 und unbesetzt. Die neue Liste zeigt `1 / 5`, `0 / 1` und `100 / 360`. Dieses Beispiel greift nicht auf deinen persönlichen Spielstand zu.

## Prüfnachweise

| Prüfung | Bestanden | Nachweis |
|---|---:|---|
| EditMode | 222 / 222 | [XML](../Implementation/Validation/20261003_085126_EditMode_UiExperienceFinalRegression.xml) |
| PlayMode | 74 / 74 | [XML](../Implementation/Validation/20261003_090233_PlayMode_UiExperienceFinalRegression.xml) |
| Finale UI-Bedienung | 6 / 6 | [XML](../Implementation/Validation/20261003_091129_PlayMode_UiExperienceFinalLayout.xml) |
| Assets | 13 / 13 | [XML](../Implementation/Validation/20261003_092852_EditMode_UiExperienceAssetsFinal.xml) |

- [Gesamtreceipt](validation_summary.json): aktuelle abgeschlossene Läufe mit Log- und XML-Pfaden.
- [Aufnahmegalerie](ReviewGallery.html): 50 echte Game Views in 1920×1080, 1280×720, 844×390, 390×844 und 2340×1080. Das letzte Format verwendet simulierte Android-Displaydichte 2,75, um die Mindestgrößen auf einem hochauflösenden Handy zu prüfen.
- [Layout-Receipt](capture_receipt.json): tatsächliche Renderauflösungen geprüft; keine gemeldeten Textüberläufe oder Aktionen außerhalb der Safe Area.
- [Erhaltung](preservation.json): 1.480 Datensätze außerhalb der UI pro Szene unverändert verglichen. Lediglich die erlaubte Referenz des Intro-Porträts wurde auf das gemeinsame Dialog-Prefab umgebunden. Gameplay-Daten und Questvoraussetzungen unverändert; nur Textinhalte geändert.
- [Wiederholbarkeit](idempotence.json): erneuter expliziter UI-Installer lässt Szenen, gemeinsame Prefabs und Atlas-Metadaten bytegleich.

PlayMode prüft unter anderem neuen Start/Namenseingabe, Fortsetzen, Profilwechsel samt Save-Roundtrip, Abbrechen/Bestätigen des Neustarts, passende Mila-Pose samt Erfolgstext, Raycast-Erreichbarkeit der Schließen-Buttons, Worker-Auswahl/Einstellungen und Abbrechen der Premium-Bestätigung. EditMode prüft alte Spielstände, fehlgeschlagenes Profil-Speichern mit Rollback, dauerhaftes Neustart-Archiv und den Mehr-Coffeeshop-Fall.

Die vollständige Asset-Suite erreichte zuerst 45 von 46 bestandene Tests. Die einzige fehlgeschlagene Prüfung meldete acht Scroll-Hintergründe ohne Sprite in den beiden Szenen. Diese wurden im expliziten Installer mit dem vorhandenen Weedtopia-Panel-Sprite verbunden. Anschließend bestanden alle oben aufgeführten gezielten Asset- und Produktionsprüfungen, einschließlich derselben zuvor fehlgeschlagenen Preflight-Prüfung und der Installer-Wiederholung. Der Gesamtlauf wurde nach dieser begrenzten Sprite-Korrektur nicht erneut ausgeführt; der originale Lauf und die konkrete Korrektur sind im Gesamtreceipt enthalten. Die unabhängigen bisherigen Release-Abnahmegates bleiben offen.

## Architektur und Erhaltung

Statische Oberflächen sind in den Szenen und den wiederverwendbaren Prefabs `DialogueStage.prefab` und `QuestRow.prefab` authored. Laufzeitobjekte sind auf die dynamischen, gepoolten Listenzeilen beschränkt. `QuestGuidance` bildet vorhandene Application-Dienste auf lesbare Anweisungen ab und hält keinen zweiten Questzustand. `PlayerProfile` teilt Validierungsregeln zwischen Tutorial, Einstellungen und Persistence. Der explizite Installer befindet sich unter `Weedtopia > Presentation > Install UI Experience`; die Installation ist bereits erfolgt.

Vorherige UI-Objekte liegen deaktiviert unter `ArchivedUiBeforeExperience`. Ein Task-Start-Snapshot der betroffenen Dateien liegt in `Baseline`; der erste Porträtatlas wurde vor der Bildüberarbeitung in `ArtSource` gesichert. Vorhandene Weltobjekte, Gebäude, Kataloge, Worker-/Liefer-/Questlogik und Autosave bleiben erhalten.

Der Porträtatlas wurde mit dem eingebauten ImageGen-Werkzeug erstellt und anschließend für saubere Zellabstände überarbeitet. Der Import verwendet tatsächliche Quelldimensionen und eine kleine Schutzkante; GUID und Sprite-Referenzen bleiben stabil. [Bildprovenienz und Prompt](PortraitPrompt.md).

`UiDisplayMetrics` rechnet Canvas-Einheiten mit der gemeldeten mobilen Displaydichte um. Neue Dialog-/Aufgabentexte verwenden mindestens 18 Display-Einheiten, Schließen/Navigation mindestens 48 und Play mindestens 64 Einheiten Höhe. Damit werden hochauflösende Telefone beim Layout berücksichtigt. Bei nicht gemeldeter DPI wird Dichte 1 verwendet; die tatsächliche Geräteprüfung bleibt offen. Grundlagen: [Unity Screen.dpi](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/screen/dpi), [Android-Displaydichte](https://developer.android.com/training/multiscreen/screendensities) und [Android-Trefferflächen](https://developer.android.com/guide/topics/ui/accessibility/views/apps-views).

## Offene Abnahmegrenzen

Kein APK für diese Revision gebaut und kein physisches Android-Gerät bedient. Android-Bildschirmtastatur, Aussparungen eines konkreten Geräts, echte Touch-Bedienung und Geräteperformance sind noch nicht geprüft. Die Game Views und Raycast-Tests ersetzen diese Prüfungen nicht. Die visuelle Freigabe des Designs und der neuen Mimik erfolgt durch dich.

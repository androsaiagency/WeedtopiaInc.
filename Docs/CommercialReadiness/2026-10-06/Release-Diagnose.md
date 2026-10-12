# Diagnose eines veröffentlichten Players

Stand: 6. Oktober 2026. Dieses Dokument beschreibt die implementierten lokalen Diagnosewege. Ein erfolgreicher Compilerlauf, Editor-Test oder Diagnose-Build erteilt keine Geräte-, Store- oder visuelle Freigabe.

## Den richtigen Build aufbewahren

Der explizite Menüpunkt `Weedtopia/Build/Production Android Release` erstellt Android-Builds mit vollständigen nativen Symbolen im ZIP-Format und IL2CPP-Informationen einschließlich Methode, Datei und Zeile. Fehler und Exceptions erhalten vollständige Stacktraces. Nach dem Build werden die zuvor gespeicherten Einstellungen und die laufenden Editor-Stacktrace-Einstellungen wiederhergestellt. Die bestehenden Produktionsfreigaben gelten auch für diesen Menüpunkt.

Für technische Verifikation ist eine ausdrücklich angeforderte `AndroidTestBuild.Request` mit `releaseDiagnostics: true` verfügbar. Sie verwendet `BuildOptions.None`, erzeugt `WeedtopiaInc-Android-ReleaseDiagnostic.apk` und hat keine Produktionsfreigabe. Der normale Android-Testpfad bleibt ein Development-Build. Ein Diagnose-APK ersetzt keine Store-Signatur und keinen Store-Test.

Ein neu gestarteter Batch-Editor erhält seinen ausdrücklich erstellten Auftrag als beständige JSON-Datei unter `.utmp`, etwa `.utmp/AndroidBuildRequests/<frische-ID>.json`. Unity kann `Temp` beim Prozessstart leeren. Deshalb wird die Auftragsdatei vor dem Start dort nicht als einziger Beleg abgelegt. Der Auftrag enthält eine neue ID aus Buchstaben, Ziffern und Bindestrichen sowie `refreshAssets: true`, `applySelectedGameIcon: false`, `refreshOnly: false` und `releaseDiagnostics: true`.

Der CLI-Einstieg wird ohne `-quit` aufgerufen; er beendet ausschließlich seinen eigenen Batch-Prozess nach Abschluss und Wiederherstellung:

```powershell
& "D:/Unity Games/6000.3.9f1/Editor/Unity.exe" -batchmode -force-d3d11 -projectPath "D:/Unity Games/Weedtopia Inc/WeedtopiaInc" -executeMethod Weedtopia.Editor.GameSetup.AndroidTestBuild.BeginOwnedBatch -weedtopiaAndroidBuildRequest "D:/Unity Games/Weedtopia Inc/WeedtopiaInc/.utmp/AndroidBuildRequests/<frische-ID>.json" -logFile "<absoluter-Pfad-zum-eigenen-Buildlog>"
```

Der Bootstrap akzeptiert nur den ausdrücklich angegebenen JSON-Pfad innerhalb der projektinternen `.utmp`-Grenze, weist umgeleitete Dateien beziehungsweise Verzeichnisse zurück und prüft Prozess- und Auftragsbesitz vor dem Kopieren. Er erhält die Originalbytes. Bytegleiche Temp-Aufträge bleiben unverändert; abweichende Aufträge werden auch bei gleicher ID nicht überschrieben. Ohne das zusätzliche Argument gilt weiterhin der vorhandene Temp-Auftragsvertrag für den bereits geöffneten Editor. Vor einem Batch-Start bestehende Unity-Prozesse und laufende Aufträge schützen; jeder Wiederholungsversuch erhält eine neue Build-ID und einen neuen Ausgabeordner.

Der aktuelle Diagnoseweg arbeitet lokal. In `ProjectSettings/UnityConnectSettings.asset` ist `InsightsSettings.m_EngineDiagnosticsEnabled` deshalb auf `0` gesetzt. Diese projektbezogene Einstellung deaktiviert Unitys Cloud-Diagnosedatensammlung; die vorhandene Cloud-Projektidentität und die übrigen Serviceeinstellungen sind erhalten. Sie verändert weder das eigene lokale Journal noch die Full-Symbol-Erzeugung und -Archivierung. Der gebaute Player besitzt dadurch keine aktivierte Unity-Cloud-Diagnosekollektion. Wird dieser zusätzliche Provider später eingesetzt, müssen Anmeldung, Einwilligungs-/Supportablauf und ein echter nativer Crash mit Symbolzuordnung gesondert geprüft werden. [Unity: EngineDiagnosticsSettings.enabled](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EngineDiagnostics.EngineDiagnosticsSettings-enabled.html)

Beim anschließenden nativen Build hat Unity zusätzlich den obersten `UnityConnectSettings.m_Enabled`-Wert von `1` auf `0` serialisiert. Diese einzelne Normalisierung ist im Abschlussbeleg ausdrücklich ausgewiesen; alle übrigen Bytes und individuellen Serviceeinstellungen stimmen mit der Launchsicherung überein. Die Cloud-Projekt-ID wurde erhalten. `ProjectSettings.asset` und `EditorBuildSettings.asset` sind nach dem Build vollständig bytegleich zu ihren Vorher-Sicherungen.

Die unveränderten Upstream-Tests des eingebetteten Render-Core-Pakets liegen reversibel unter `Packages/com.unity.render-pipelines.core/Tests~`, einschließlich aller Original-GUIDs und der archivierten Ordner-Meta. Dadurch werden absichtlich ungültige Test-Stripper nicht mehr in der Produktionssuche aktiviert. Die eigenen Weedtopia-Tests bleiben aktiv. Wer später ausdrücklich die Unity-/URP-Upstreamtests ausführt, stellt das Testverzeichnis samt Ordner-Meta vorher wieder her. [Unity: ausgeblendete Ordner beim Import](https://docs.unity3d.com/6000.3/Documentation/Manual/SpecialFolders.html#HiddenAssets)

Für jeden erfolgreichen Android-Build archiviert `NativeSymbolArtifacts` die zugehörige Symbol-ZIP unter dem jeweiligen Build-Verzeichnis. Das Manifest hält APK- und Symbol-SHA256, Build-GUID, Version, Versioncode, ABI, Backend und Buildoptionen fest. Die ELF-Build-IDs von `libmain`, `libunity` und gegebenenfalls `libil2cpp` müssen zwischen APK und Symbolarchiv übereinstimmen. Ein fehlendes, mehrdeutiges oder unpassendes Symbolarchiv macht den technischen Buildauftrag fehlerhaft. APK, Manifest und Symbolarchiv gemeinsam aufbewahren; Symbole gehören nicht in das installierte APK.

Die unabhängige Prüfung ist dauerhaft unter `Tools/CommercialReadiness/audit_android_release.py` verfügbar. Nach einem abgeschlossenen Diagnose-Build wird sie aus dem Projektverzeichnis ausgeführt:

```powershell
python Tools/CommercialReadiness/audit_android_release.py "Builds/Android/<Build-ID>/WeedtopiaInc-Android-ReleaseDiagnostic.apk" --output "Docs/CommercialReadiness/<Datum>/ReleaseApkAudit/<Build-ID>" --textures
```

Die Platzhalter werden durch den tatsächlich gebauten Auftrag ersetzt. Der Prüfer liest das Paket, recomputiert CRC und Größe jedes ZIP-Eintrags und überprüft Signatur, Ausrichtung, Manifest, Symbolbibliotheken und die 16 dokumentierten Vorher-/Nachher-Einstellungspaare. Mit `--textures` untersucht er zusätzlich die 251 produktiven Gebäudetexturen im tatsächlichen APK. Er ändert weder Paket noch Quellen, installiert keine App und lädt nichts hoch. Selbsttests verwenden synthetische Pakete und ersetzen den echten Paketnachweis nicht. Details stehen in `Tools/CommercialReadiness/README.md`.

## Lokales Journal

Der Player legt unter `Application.persistentDataPath/WeedtopiaDiagnostics` die Dateien `player-events.ndjson` und gegebenenfalls `player-events.ndjson.previous` an. Jeder Datensatz ist eine JSON-Zeile. Das aktuelle Journal und die vorherige Datei sind jeweils auf höchstens 256 KiB begrenzt; eine Ausführung schreibt höchstens 256 Ereignisse. Das Unity-Editor-Spiel erzeugt über diesen Dienst kein persönliches Journal.

Erfasst werden UTC, Build-GUID, Unity-/Spielversion, Plattform, Ereignisart, ein begrenzter Exception-Code und höchstens 16 bereinigte Methodennamen. Originale Logtexte, Dateipfade, Methodenargumente, Namen, Spielstände, Zugangsdaten und Gerätekennungen werden nicht übernommen. Es gibt keinen automatischen Versand. Ein begrenztes `Lifecycle/PlayerStarted`-Ereignis markiert den Start jeder Player-Ausführung und zählt zum Ereignislimit.

In beiden Städten ist die Supportoberfläche ausdrücklich in den vorhandenen Szenen eingerichtet: Über das runde Einstellungssymbol oben rechts im Startmenü erreicht man unter „Spiel“ die Aktion „Diagnose kopieren“. Sie bleibt auch ohne geladene Session und nach einer fehlgeschlagenen Fortsetzung erreichbar. Der Export kopiert höchstens 256 KiB der jüngsten vollständigen Ereignisse aus aktuellem Journal und vorheriger Datei in die Zwischenablage. Leere Daten und fehlgeschlagenes Kopieren erhalten eigene lokalisierte Rückmeldungen. Der Spieler entscheidet anschließend selbst, ob und an wen er diese Daten weitergibt. Die Szene verwendet bestehende geteilte Prefabs; `Weedtopia/Release/Install or Repair Support Export` ist die explizite, wiederholbare Installationsaktion. Native Tests prüfen diesen Ablauf mit einer injizierten Zwischenablage, ohne die persönliche Windows-Zwischenablage zu überschreiben.

Speicher- und Ladevorgänge sowie Offline-Fortsetzung melden ihre Fehlerklasse an dieses Journal. Auch eine erfolgreich wiederhergestellte Sicherung kann die technische Ursache des ungültigen Hauptspielstands melden. Die Ursache wird nur im Speicher transportiert und nicht in den Spielstand geschrieben. Der Benutzer sieht die bestehende lokalisierte Meldung und erhält bei fehlgeschlagener Fortsetzung erneut die Start-/Fortsetzen-Oberfläche.

Nach einer fehlgeschlagenen Fortsetzung bleibt der vorherige aktive Zustand gesperrt, bis Fortsetzen erfolgreich ist oder der Spieler den vorhandenen „Neues Spiel“-Dialog bestätigt. Vor diesem bestätigten Neustart archiviert die Anwendung lesbare Originalbytes auch dann, wenn sich daraus kein gültiges Modell mehr laden lässt. Sind Primärdatei und Backup beide ungültig, werden beide archiviert. Ein fehlgeschlagener Archivierungsvorgang verhindert den Neustart. Die Archive enthalten persönliche Spielstände und gehören deshalb ausdrücklich nicht zum normalen Diagnoseexport.

## Langsamen Start eingrenzen

Vier technische Zeitmessungen werden protokolliert:

| Operation | Gemessener Abschnitt | Praktische Interpretation |
|---|---|---|
| `StartupLoad` | Hauptdatei/Sicherungsdatei lesen, deserialisieren, validieren und die private Session erstellen | Hoher Wert spricht für Datei-/JSON-/Validierungsaufwand. |
| `OfflineSimulation` | Hintergrundsimulation ab Start bis zur beobachteten Fertigstellung | Hoher Wert betrifft die vollständige Nachrechnung einschließlich Threadplanung. |
| `OfflineCommit` | Abschluss auf dem Session-Besitzerthread einschließlich endgültiger Speicherung | Hoher Wert betrifft Abschlussdienste oder Schreib-/Validierungsaufwand. |
| `StartupReady` | Fortsetzung bis zur aufgebauten Oberfläche, einschließlich Zielstadtszenenwechsel | Hoher Gesamtwert bei kleinen vorherigen Phasen spricht für zusätzlichen Szenen-/UI-Aufwand. |

Zusätzlich enthält die Zeile die beantragten Offline-Sekunden sowie die Anzahl von Gebäuden und Mitarbeitern. Die Zeiten sind Stoppuhr-Wandzeiten. `StartupReady` beweist den UI-Aufbau; es misst weder das erste fertig gerenderte GPU-Bild noch den gesamten Prozessstart. Die erste Anwendungsszene, Betriebssystem-Caches, Assetimport, Geräte-Touch und Geräte-Audio bleiben eigene Messungen. Bei einem abgebrochenen Simulationsauftrag kann die angegebene Offline-Dauer die angeforderte und noch nicht vollständig ausgeführte Dauer sein. Der WebGL-Pfad verwendet weiterhin die bestehende stufenweise Hauptthread-Fortsetzung und besitzt nicht alle separaten Hintergrundphasen.

## Einen Fehlerfall reproduzierbar übergeben

1. Plattform, Spielversion, Zeitpunkt und konkrete Bedienfolge festhalten; vorhandenen Spielstand außerhalb der laufenden Anwendung sichern. Eine Diagnosekopie nur nach ausdrücklicher Zustimmung des Spielers verwenden.
2. Im Startmenü oder Spiel die Einstellungen öffnen, „Spiel“ wählen und „Diagnose kopieren“ betätigen. Die kopierten bereinigten Ereignisse zusammen mit der Bedienfolge übergeben. Den Originalspielstand nicht als normale Logdatei behandeln.
3. Anhand der Build-GUID und des Buildmanifests genau das passende APK und Symbolarchiv auswählen. Ein Symbolarchiv einer anderen Version ist unbrauchbar, auch wenn die Dateinamen ähnlich sind.
4. Bei einem nativen Android-Absturz den verfügbaren Crashbericht beziehungsweise Tombstone mit dem passenden ABI und den passenden ELF-Build-IDs symbolisieren. Ein tatsächlicher nativer Absturz und seine Symbolisierung müssen auf dem Zielgerät gesondert geprüft werden.
5. Bei fehlgeschlagener Offline-Fortsetzung prüfen, ob der Fehler in `Load`, `OfflineContinuation` oder `Save` liegt. Anschließend die gleiche historische Zeit und denselben Spielstand in einer isolierten Kopie reproduzieren. Die öffentliche Session darf vor erfolgreichem Abschluss keine teilweise nachgerechneten Zustände übernehmen.

Ein Betriebssystemabbruch, Stromverlust oder nativer Prozessabsturz kann vor dem nächsten Journalereignis eintreten. Das lokale Journal ersetzt deshalb keine nativen Crashberichte. Telemetrie-, Store-Crash- und Supportkonto-Integrationen sowie eine Einwilligungsoberfläche sind noch separate Produktaufgaben.

## Abnahmegrenzen

Die hier implementierten Bausteine sind lokale technische Diagnose und reproduzierbare Buildzuordnung. Für die kommerzielle Freigabe fehlen weiterhin echte Zielgerätmessungen mit kleinen und großen historischen Spielständen, ein kontrollierter nativer Crash samt Symbolisierung, eine vom Nutzer bestätigte visuelle Abnahme, produktive Store-/Kaufprüfung und die finalen Eigentümer-, Versions- und Signaturdaten. Diese Daten werden durch technische Diagnoseaufträge nicht automatisch festgelegt.

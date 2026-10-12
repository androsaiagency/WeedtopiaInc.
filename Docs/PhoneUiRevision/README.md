# Smartphone-Gebäudedetails und Stadtgeschichten

Diese Revision beantwortet die Smartphone-Screenshots vom 04.10.2026. Sie erweitert die bestehende Oberfläche in Burning Hills und Big Rock City.

## Verhalten

- Das Gebäudefenster wird nicht mehr als Ganzes verkleinert. Schrift und Bedienflächen berücksichtigen die Pixeldichte des Displays.
- Bei geöffneten Gebäudedetails oder der Worker-Auswahl sind Stadtanzeige, Ressourcen, Einstellungen und untere Navigation ausgeblendet. Nach dem Schließen sind die Bedienelemente wieder erreichbar.
- Auf schmalen bzw. kurzen Displays nutzt das Fenster den sicheren Bildschirmbereich. Breite Handyansichten ordnen Gebäudewerte und Team nebeneinander an; schmale Ansichten ordnen sie untereinander an.
- Die Gebäudeaktionen stehen außerhalb des scrollbaren Inhalts. Der Upgrade-Button bleibt erreichbar. Das goldene X behält eine große Trefferfläche, erhält aber einen kleineren sichtbaren Durchmesser.
- Ein Klick auf einen Bewohner in den Stadtgeschichten wechselt direkt in dessen Angebots-/Auftragsansicht. „Zur Übersicht“ führt zurück zur Liste. Auftragsziele stehen vor der längeren Geschichte; Annahme bzw. Abschluss bleiben sichtbar.
- Ein bloßer Besuch löst keine missverständliche Erfolgsmeldung „Erledigt“ mehr aus. Besuche, Entscheidungen, Ziele und Belohnungen werden weiterhin durch die vorhandenen Story- und Speicherdienste verarbeitet.

## Autorisierte Übernahme und Erhalt

Der ausdrückliche Installer `Weedtopia/Presentation/Install Phone Building and Story UI` ergänzt vorhandene Controls und gemeinsame Prefabs. Er erzeugt keine statischen UI-Objekte beim Spielstart. World- und Gameplay-Referenzen bleiben erhalten. Unsichere Übernahme bei Play Mode oder ungespeicherten Szenen wird abgewiesen.

Die Übernahme und Prüfungen erfolgen über Dateiaufträge im bereits geöffneten Unity-Editor. Es wird kein weiterer Editor für dasselbe Projekt gestartet und keine fremde Sitzung beendet. Der Prüfablauf verwendet einen eigenen Spielstand und ein eigenes Leaderboard. Der originale Game-View-Modus wird danach wieder ausgewählt.

## Nachweise

Die jeweils letzten Ergebnisse sind maßgeblich:

- `authoring_receipt.json`: installierte Szenen und Story-Prefabs.
- `capture_receipt.json`: echte Game-View-Bilder, Layoutgrenzen und Pointer-Treffer. Die Bilder liegen unter `Screenshots/`.
- `Tests/PlayMode.xml`: vollständiger erster bzw. zweiter Regressionstest; ältere Fehlversuche sind zusätzlich separat gespeichert.
- `Tests/PlayModeFocused.xml`: erneuter Test der betroffenen Gebäude-, Menü-, Story- und Smartphone-Testgruppen.
- `Tests/EditMode.xml`: Domain- und Asset-Regressionen.
- `Tests/AssetRecheck.xml`: erneute Prüfung der korrigierten transparenten Scrollfläche und der bestehenden Presentation-Gates.
- `Tests/combined-result.json`: 86 PlayMode- und 280 EditMode-Testfälle, jeweils mit dem letzten Ergebnis und seiner Herkunft. Die ursprünglichen Fehlversuche bleiben dokumentiert; es wird kein einzelner vollständig erfolgreicher Gesamtlauf behauptet.
- `Preservation/result.json`: Erhalt der ursprünglichen Szenenobjekte und Prüfsummen geschützter Daten.
- `summary.json`: zusammengeführtes Endergebnis mit Datum, gültigen Nachweisen und Android-Testversion.

Die Game-View-Prüfung verwendet 1920×1080, 844×390, 2340×1080 und 1080×2340 sowie die UI-Skalierungen 1,0 und 1,2. Die großen Handyauflösungen werden mit Displaydichte 2,75 geprüft. Die Regressionstests verwenden eine feste Game-View-Auflösung, um die Größe des Desktopfensters als Einfluss auszuschließen.

Die echte Bedienung und Lesbarkeit auf einem physischen Smartphone sind durch diese Desktopprüfungen nicht bestätigt. Die neue APK ist eine Development-Testversion.

## Abschließende Desktopprüfung vom 04.10.2026

- 12 Source-Assemblies erfolgreich kompiliert.
- Alle 86 unterschiedlichen PlayMode-Fälle bestanden in der Kombination aus Gesamtlauf und gezielter Wiederholung von 40 betroffenen Fällen.
- Alle 280 unterschiedlichen Logik-/Asset-Fälle bestanden in der Kombination aus Gesamtlauf und gezielter Wiederholung von 10 Presentation-Fällen.
- Letzte Game-View-Prüfung: 80 Bilder und 600 erfolgreiche Layout-/Pointer-/Ablaufprüfungen, einschließlich der letzten Hochformatkorrektur.
- Alle 19.117 vor der Revision vorhandenen Szenenobjekte bleiben erhalten. 104 geschützte Dateien für Inhalte, Domain, Application, Persistenz, Audio und Icon haben weiterhin ihre ursprünglichen Prüfsummen.

Die Android-Version liegt nach erfolgreichem Build unter `Builds/Android/20261004-phone-ui-fixes-02/WeedtopiaInc-Android-Test.apk`. Build-Ergebnis und APK-Prüfungen stehen im selben Ordner. Das maßgebliche Endergebnis ist `summary.json`.

Der erste Android-Build (`20261004-phone-ui-fixes-01`) scheiterte mit `Java heap space` während `compressDebugAssets`. Der Test-Build verwendet jetzt mindestens 8192 MB Java-Heap über Unitys [AndroidExternalToolsSettings](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/android/androidexternaltoolssettings). Die ursprüngliche globale Einstellung wird im `finally`-Block wiederhergestellt. Ein regulärer Android-Postprocessor begrenzt Gradle auf zwei Worker und deaktiviert parallele Projekte; er übernimmt die übrigen generierten Properties unverändert. Der fehlgeschlagene erste Build bleibt als Nachweis erhalten.

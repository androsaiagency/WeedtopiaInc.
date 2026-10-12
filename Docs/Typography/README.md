# Weedtopia Grove

Eigene Weedtopia-Schriftfamilie mit zwei Schnitten: **Regular** für Fließtext und **Bold** für Überschriften, Schaltflächen und Weltbeschriftungen. Weiche Enden und Ecken, neu gezeichnete einstöckige `a`/`g`, dazu passende kyrillische `а` und samenförmige Punkte bei `i`/`j`. Für die Benutzeroberfläche bleibt die Lesbarkeit wichtiger als dekorative Details.

## Dateien und Herkunft

Die fertigen, vollständig eingebetteten TrueType-Schriften liegen in `Assets/Weedtopia/Production/Fonts/WeedtopiaGrove/`:

- `WeedtopiaGrove-Regular.ttf`
- `WeedtopiaGrove-Bold.ttf`
- `OFL.txt`

Weedtopia Grove ist eine gestaltete Ableitung der bereits im Projekt vorhandenen Lato Regular/Bold von Łukasz Dziedzic. Die Originaldateien werden unverändert als Quelle behalten. Die neue Familie trägt einen eigenen Namen; Copyright und die **SIL Open Font License 1.1** sind sowohl als Datei als auch in den Schriftmetadaten enthalten. Die Schrift darf mit dem Spiel gebündelt werden. Die Schriftdateien selbst bleiben unter der OFL; diese bestimmt nicht die Lizenz des Spiels. Siehe auch [offizielle OFL-Hinweise zu Änderungen und Weitergabe](https://openfontlicense.org/ofl-faq/).

Die Konturen werden tatsächlich verändert. Alle geeigneten Ecken und Enden erhalten quadratische Rundungen; `a`, `g` und kyrillisches `а` werden neu konstruiert. Akzentkomponenten behalten ihre Verknüpfung mit den Grundzeichen. Häkchen `✓`/`✔`, UI-Zeichen `▣`/`◆` und Rubel `₽` wurden ergänzt. Alte Raster-Hinweise werden entfernt, weil sie nicht zu den geänderten Konturen passen. Originale Zeichenbreiten, Zeilenmetriken sowie die Kerning- und OpenType-Tabellen bleiben erhalten.

## Sprachumfang

Beide Schnitte enthalten **3.027 Glyphen für 2.169 Unicode-Zeichen**. Deutsch, Englisch, Spanisch, Französisch und Russisch werden einschließlich Groß-/Kleinbuchstaben, Umlauten, `ß`/`ẞ`, Akzenten, `Œ`/`œ`, `Ñ`/`ñ`, `Ё`/`ё`, `Й`/`й`, Ziffern, Währungen und den vorhandenen UI-Symbolen geprüft. Auch zerlegte Akzente (Unicode NFD) aus diesen Sprachen werden geprüft.

Das ist keine Zusage für alle Schriftsysteme der Welt. Bei weiteren Sprachen wird deren tatsächlicher Textbestand gegen beide Fonts geprüft. Zusätzliche lateinische und kyrillische Zeichen sind bereits enthalten; Arabisch, CJK usw. benötigen eine eigene gestalterische Erweiterung bzw. explizit zugeordnete passende Font-Assets.

## Unity-Einbindung

`PresentationArt.asset` und `LanguageCatalog.asset` referenzieren die beiden Schnitte. Produktive Szenen und wiederverwendbare Text-Prefabs verwenden dieselben Fonts. Die Sprache bleibt in den Einstellungen umschaltbar; jede Sprache benutzt dieselbe Familie. Kein Font-Download, keine Betriebssystem-Schrift und keine Laufzeit-Erzeugung fester UI-Objekte.

Expliziter Editor-Befehl: **Weedtopia → Typography → Install or Repair**. Er ersetzt ausschließlich die bekannten Lato-Schriftreferenzen und zugehörigen Font-Materialreferenzen in vorhandenen Assets. Manuell zugewiesene andere Fonts bleiben erhalten. Review-Assets behalten ihre Ausgangsschriften. Szenenobjekte, IDs, Layoutwerte und sämtliche Texte werden dabei unverändert erhalten. **Validate Characters** prüft die aktuellen Übersetzungsdateien mit den von Unity importierten Schriften. Es gibt keine automatische Installation beim Import oder beim Spielstart.

## Erweiterung und reproduzierbarer Build

Die Gestaltung ist unter `Tools/Typography/design.json` parametrisiert; der vollständige Vektor-Build liegt in `Tools/Typography/build_fonts.py`. Benötigte kostenlose Werkzeuge sind in `requirements.txt` versioniert. Sie können projektlokal unter `.utmp/FontTooling` bereitgestellt werden. Bei Änderungen werden die Originalschriften als Ausgangspunkt benutzt, nicht die bereits generierten Fonts. `prepare_import.py` legt nur fehlende `.meta`-Dateien an und erhält vorhandene GUIDs.

```powershell
python -X utf8 Tools/Typography/build_fonts.py
python -X utf8 Tools/Typography/prepare_import.py
python -X utf8 Tools/Typography/validate_fonts.py
python -X utf8 Tools/Typography/render_specimen.py
```

`validate_fonts.py` prüft jedes druckbare Zeichen sämtlicher Sprachdateien, die vollständigen fünf Alphabete und deren zerlegte Akzente. Es prüft zusätzlich interne Familien-/Schnittnamen, Lizenzmetadaten, offene Einbettung, unveränderte Breiten/Zeilenmetriken, Kerning/Shaping sowie bytegleiche Neugenerierung. Danach prüfen die Unity-Assettests echte Rasterisierung bei 16/32/64 Pixeln, Szenen, TextMesh-Materialien, Prefabs und wiederholte Installation.

Auch der bestehende Sprachdatei-Check `Tools/Localization/validate_catalogs.py` prüft jetzt sämtliche druckbaren Zeichen und deren zerlegte Akzente gegen beide produktiven Weedtopia-Grove-Schnitte. Neue Texte mit bislang fehlenden Symbolen fallen dadurch schon bei der normalen Übersetzungsprüfung auf. Die ursprünglichen Lato-Dateien sind weiterhin die Build-Quelle und keine Ausweichschriften für fehlende Produktionszeichen.

Die Game-View-Aufnahmen werden über `Tools/Typography/Run-Typography.ps1 -Mode Capture` mit isoliertem Spielstand erzeugt. Bestehende Spracheinstellungen und die projektspezifischen Editor-/Render-Einstellungen werden anschließend wiederhergestellt. Der Ablauf startet nur dann einen eigenen versteckten Unity-Prozess, wenn kein Editor und keine Sperrdatei vorhanden sind.

## Nachweise

- `WeedtopiaGrove-Specimen.png`: echte TTF-Probe mit fünf Sprachen und kleinen UI-Größen.
- `Outline-Comparison.png`: Lato-Ausgangspunkt und neue Konturen.
- `font_build.json`, `font_validation.json`: Font-Build und Zeichendeckung.
- `unity_glyph_validation.json`, `idempotence.json`: Import/Rasterisierung und wiederholte Anwendung.
- `preservation.json`: Abgleich mit dem vor der Schriftmigration gesicherten Projektstand.
- `Screenshots/`, `gameview_receipt.json`, `ui_overflow.json`: tatsächliche Game View in Quer- und Hochformat.

Game-View-Prüfungen bestätigen keine physische Gerätebedienung und ersetzen keine persönliche Gestaltungsabnahme.

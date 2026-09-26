# Ressourcen-Umwandlung: negativer Restbestand behoben

Datum: 26.09.2026

## Ursache

`Amount` rechnet mit zwölf signifikanten Stellen. Bei der Butterproduktion wurde zuerst `available / 2` als Produktionsmenge berechnet und danach `amount * 2` vom Eingangslager abgezogen. Dieser Rundungsweg kann den ursprünglichen Bestand überschreiten.

Reproduzierbares Beispiel: Aus `6.98234169929` Eingang werden gerundet `3.49117084965` Ausgang; zurückgerechnet ergibt das `6.98234169930` Verbrauch. `BuildingState.Set` lehnt den dadurch negativen Rest zu Recht ab. Vier neue Unity-Testfälle mit unterschiedlichen Größenordnungen reproduzierten vor der Korrektur exakt den gemeldeten Stacktrace.

## Korrektur

- `Simulation.Convert` bestimmt zuerst die zulässige Ausgangsmenge aus Rate und freiem Lagerplatz. Der tatsächliche Verbrauch wird anschließend in Eingangseinheiten auf den vorhandenen Bestand begrenzt. Die Ausgangsmenge wird aus diesem Verbrauch berechnet und ebenfalls durch Rate und Lagerplatz begrenzt.
- Der negative Rundungsrest wird nicht nachträglich versteckt. Die strenge Prüfung in `BuildingState.Set` bleibt erhalten; ungültige Zahlen werden jetzt vor dem Größenvergleich geprüft, damit beispielsweise NaN zuverlässig als ungültiger Wert abgewiesen wird.
- `Amount`, Speicherformat, Spieldefinitionen, Szenen und Prefabs wurden nicht geändert. Ein Zurücksetzen oder Migrieren des Spielstands ist nicht erforderlich.

## Prüfung

Unity 6000.3.9f1, vollständige Assembly `Weedtopia.Game.EditModeTests`: **55/55 bestanden, 0 fehlgeschlagen, 0 übersprungen**.

Beleg: `Docs/Implementation/Validation/20260926_105247_EditMode_ResourceConversionFinal.xml` und gleichnamige JSON-/Logdateien.

Die 13 neuen Regressionstestfälle decken ab:

- Exaktes Leeren gebrochener Butter-Eingangsbestände bei Exponenten 0, 9, 400 und 2000.
- 6.000 deterministische gebrochene Eingangsbestände über Verarbeitung, Butterherstellung und Bäckerei.
- Durchsatzlimit, fast volles, volles sowie bereits überfülltes Ausgangslager.
- Ressourcenerhaltung innerhalb der vorhandenen Zahlengenauigkeit über mehrere Produktionstakte.
- Weiterhin abgewiesene negative und ungültige Bestände.
- Speichern, Laden sowie dieselbe Fortsetzung über Live-Frames und Offline-Fortschritt mit den echten Spieldefinitionen und einem isolierten Testspielstand.

Die vorherige fehlerhafte Implementierung und ihr Testprotokoll sind in `Originals/` beziehungsweise `Docs/Implementation/Validation/20260926_104617_EditMode_ResourceConversionBefore.xml` dokumentiert. Der bestehende Nutzer-Spielstand wurde nicht verändert. Ein manuell bedienter Play-Mode-Durchlauf war nicht Bestandteil dieser Prüfung.

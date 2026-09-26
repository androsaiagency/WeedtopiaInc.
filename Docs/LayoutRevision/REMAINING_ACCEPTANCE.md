# Noch offene Abnahme

Die Freigabe der vier Gebäudeprototypen und die Wahl der lokalen pixelbewahrenden Freistellung liegen vor. Beide Entscheidungen sind umgesetzt; sie werden nicht erneut angefordert. Die folgende Abnahme betrifft das gesamte Spiel und dessen Veröffentlichung.

| Gate | Konkreter nächster Schritt | Vorbereitete Grundlage |
| --- | --- | --- |
| Zielplattform und Leistungsziel | Primäre Plattform, Mindestgerät, Auflösung und gewünschte Bildrate festlegen; danach dort messen. | Lokaler Windows-Diagnosebuild mit Farm, Gesamtübersicht und Tierwechsel. Seine Messwerte sind kein Nachweis für andere Geräte oder physisches Touch. |
| Menschlicher Spieldurchlauf | Vom Intro bis Freeplay spielen und insbesondere Bezirksschieben, entfernte Farm, Fahrerwartezeit, Gebäudeauswahl und Lesbarkeit beurteilen. | Aktuelle Szenen, automatisierte Hauptpfadtests und die [Galerie mit 73 Game-View-Aufnahmen](comparison.html). |
| Audio-Abhören | Musik im Spiel mehrfach über die Loopgrenze hören und Bestätigung, fehlende Mittel, Lieferung und Upgrade auf Lautsprechern/Kopfhörern beurteilen. | Originalmusik und vier Cues unter `Assets/Audio/Approved/BurningHills`; technische PCM- und Importnachweise liegen vor. Der Ordnername ist keine Hörfreigabe. |
| Audio-Lizenzfreigabe | Kommerzielle Nutzungsberechtigung und gegebenenfalls nötige Registrierung für den tatsächlichen Herausgeber nachweisen. | [Dokumentierte Quellen und Modell-/Lizenzverweise](../ProductionFinish/PROVENANCE_AND_LICENSES.md), Originalquellen und Generierungsparameter sind erhalten. |

## Spielprüfung

1. `Assets/Weedtopia/Game/Scenes/Bootstrap.unity` starten oder den gebauten Windows-Player normal öffnen. Der normale Start benutzt den vorhandenen Speicherpfad. Für einen vollständigen Erstlauf zuerst den bisherigen Spielstand sichern und anschließend bewusst ein neues Spiel beginnen; „Starten“ speichert den neuen Stand im selben Spielstand-Slot.
2. Rockos Einführung und den Farm-Aufbau durchspielen, Automatisierung, Lagerstau und Lieferung in die Stadt beobachten.
3. Farm nach rechts, Shops nach unten, Siedlung nach oben und Bäckereien nach links durchschieben. Mausrad bewegt entlang der Bezirksrichtung; Strg+Mausrad zoomt. Übersicht und Schieberegler stehen zusätzlich bereit.
4. Fahrer vom Stadt-Lager zu Coffee Shops und Butterfabrik beobachten; Siedlung, Industrie, Workerzuweisung und Ausbau bedienen.
5. Speichern und laden, Fortschritt prüfen und bis Freeplay weiterspielen. Schrift, Bedienflächen und Audio auf dem gewählten Zielgerät beurteilen.

Automatische Tests, Agenten-Sichtprüfung und eine erfolgreiche Build-Erstellung ersetzen diese Gates nicht. Es werden keine angenommenen oder vorab ausgefüllten Freigabedateien erzeugt. Der Production-Preflight lässt `productionReady` offen, bis die entsprechenden aktuellen Nachweise vorhanden sind.

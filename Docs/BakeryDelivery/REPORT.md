# Bäckereien: Straßen und Butterabholung

Stand: 2026-09-25T06:35:31.676467+00:00. Änderung integriert und technisch geprüft; keine neue Gesamtspiel-Freigabe.

Alle zwölf Bäckereien sind über gepflasterte Straßen an den Vorplatz der Butterfabrik angeschlossen. Die bisherigen Bäckerei-Förderbänder und ihre Frachtmarkierungen sind nicht mehr sichtbar. Die übrigen Transportverbindungen, insbesondere die Farm-Förderbänder und Lagerfahrer, behalten ihre Funktion.

Der zugewiesene Bäcker fährt mit seinem Lieferwagen leer zur Butterfabrik, lädt dort Butter ein und bringt sie zur eigenen Bäckerei zurück. Die Abholung beginnt automatisch bei höchstens einem Viertel Buttervorrat, sobald Butter verfügbar ist. Ladekapazität und Fahrzeit berücksichtigen Gebäudestufe und passende Worker-Boni. Unterwegs pausiert seine Backarbeit; die Bäckerfigur erscheint erst nach der Rückkehr wieder am Arbeitsplatz. Der Worker kann während einer aktiven Fahrt nicht entfernt oder umverteilt werden. Das Gebäudemenü zeigt Fahrtstatus, Transportkapazität und Dauer je Strecke.

Butter wird bei Abfahrt reserviert und erst beim tatsächlichen Eintreffen in der Fabrik verladen. Reservierungen verhindern doppelt verplante Ware bei mehreren Bäckern. Ein voller Zielpuffer vernichtet keine Ladung: Der zurückgekehrte Bäcker kann Vorräte verarbeiten und damit Platz schaffen. Fahrtphase, Reservierung und Ladung werden gespeichert; Nachladen und Offline-Fortschritt verwenden denselben Simulationsablauf. Die additive Erweiterung behält die bestehende Speicherformat-Version bei.

Die Straßen liegen als zwölf gespeicherte Mesh-Assets in `Assets/Weedtopia/Production/BakeryDelivery/Meshes`. Sie verwenden das vorhandene natürliche Straßenmaterial. Der vorhandene Fahrzeug-Pool wurde um zwölf Instanzen desselben Lieferwagen-Prefabs auf 60 erweitert. Die explizite Editor-Aktion lautet `Weedtopia/Production/Apply Bakery Pickup Roads`; es gibt keine automatische Reparatur beim Projektstart oder Import.

## Aktuelle Prüfungen

- **EditMode 75/75 bestanden**, [XML](../Implementation/Validation/20260925_082830_EditMode_BakeryFinal.xml). Neun neue Testfälle prüfen u. a. Hin-/Rückfahrt, tatsächlichen Ladezeitpunkt, fehlenden Bäcker, knappen Fabrikbestand, konkurrierende Abholer, volle Zielpuffer, leere Rückfahrt, Speichern und Offline-Fortschritt.
- **PlayMode 25/25 bestanden**, [XML](../Implementation/Validation/20260925_082711_PlayMode_BakeryFinal.xml). Zwei zusätzliche Integrationstests prüfen die sichtbaren Fahrtphasen, Bäckeranwesenheit, Straßenzuordnung, gespeicherten Fahrzeug-Pool und erhaltene Farmbänder.
- **12 Assemblies kompiliert**, [Quellprüfsummen](source_compile.json) mit dem aktuellen Dateistand abgeglichen.
- **11 echte Unity-Game-View-Aufnahmen**, [Aufnahmebeleg](gameview_receipt.json) und [dokumentierte Sichtprüfung](visual_review.json). Straßenübersicht, westliche Erweiterung, komplette Abholfahrt und Gebäudemenü bei 1920×1080, 1366×768/115 % sowie 2560×1080. [UI-Überlaufprüfung](ui_overflow.json): keine gemeldeten Überläufe in diesen Ansichten.
- **Windows-Development-Build erstellt**, [Buildbeleg](development_build.json). Der separate D3D11-Diagnoseplayer hat drei bestehende Spielszenarien tatsächlich gerendert; [Laufbeleg](player_run.json), [Messdaten](player_profile.json). Das ist ein Desktop-Diagnoselauf, keine Abnahme auf einem Zielgerät und kein manuelles Durchspielen.
- **Erhaltung geprüft:** [Vergleich](preservation.json) aller 2549 ursprünglichen Dateien. Kein ursprüngliches Asset und kein ursprünglicher Szenenblock entfernt. Alle 251 zentrierten Gebäude-Prefabs, Kunstquellen, Kataloge, vorhandenen Fahrzeugreferenzen, Bauplatzpositionen und sonstigen Inhalts-/Balancingdaten erhalten. Geändert wurden die zwölf Bäckereirouten, passende Texte, die erforderliche Logik und deren Tests.

Der erste gezielte Domain-Testlauf hatte einen Fehlschlag: Die vollständige Tutorial-Speicherprobe endete bisher nach 120 simulierten Sekunden. Mit der Hin- und Rückfahrt erreicht sie das Ende nach 126 Sekunden. Das Testbudget wurde auf die bereits im separaten Progressionstest verwendeten 1800 Sekunden angepasst; die Abschluss- und Speicherprüfungen bleiben bestehen. Dieser erste Lauf wird nicht als bestanden gezählt. Maßgeblich sind die oben verlinkten vollständigen Wiederholungsläufe.

![Rückfahrt mit Butter](Screenshots/07_return_status.png)

Fingerprint für Game View, Build und Player: `715487e3a738d9219dd56d672961807d1e38c074d1f48a89d29319f956b68203`.

Kein Benutzereditor wurde beendet oder parallel gestartet. Physische Touchbedienung sowie `target_profile`, `human_playthrough`, `audio_listening` und `audio_license` bleiben offene Abnahmen. Frühere Berichte belegen jeweils ihren damaligen Stand; dieser Bericht enthält die aktuellen Nachweise für die Bäckereiänderung und die Regressionstests.

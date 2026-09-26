# Burning Hills: freie Spielansicht und Gebäudemenüs

Stand: 2026-09-24T23:19:38.224081+00:00. Die neue Bedienung ist in der vorhandenen Szene integriert. Eine endgültige Gesamtspiel- und Geräteabnahme bleibt offen.

## Bedienung

- Im Spiel bleibt nur der kleine Menüknopf sichtbar. Questkarte, Ressourcenleiste, feste Gebäudeanzeige, Navigationsleiste und Positionsregler sind nicht mehr dauerhaft eingeblendet.
- Linke Maustaste halten und ziehen oder mit einem Finger wischen: den jeweiligen Stadtteil bewegen. Die vier Richtungen und ihre Grenzen bleiben erhalten. Mausrad und Zwei-Finger-Geste zoomen.
- Kurzer Klick oder Touch-Tipp: Gebäudemenü links, das bestehende Gebäude vergrößert rechts. Eine Bewegung über die Gestenschwelle wird nicht als Klick verarbeitet. Ein auf UI begonnener Zug bleibt der UI zugeordnet.
- Das Gebäude wird anhand seiner aktuellen Spriteinstanz eingerahmt. Schließen kehrt zur vorherigen Kameraposition und Zoomstufe zurück. Eine neue Weltgeste bzw. ein gezielter Kamerasprung beendet den bisherigen Gebäudekontext.
- Das Menü zeigt Name, Level, Workerportrait, Rolle, Seltenheit, Level/Rang und tatsächliche Attribute. Grün **+** öffnet die passende Auswahl rechts; Rot **X** entfernt die Besetzung. Weiterbildung und Rangaufstieg zeigen ihre Kosten.
- Gebäudespezifische Kennzahlen zeigen aktuelle Werte und tatsächliche Verbesserungen beim nächsten Level mit grünen Pfeilen. Werte, die sich nicht ändern, erhalten keinen Verbesserungspfeil. Bau, Ernte, Verarbeitung, Gespräche, Wohnhaus/Park-Wahl und Premiumbestätigung verwenden die bestehenden Spielbefehle.
- Über **Menü** bleiben Stadtteile, Übersicht, Ressourcen, aktuelle Quest und Belohnung, Aufträge, Rathaus, Team, Speichern und Einstellungen erreichbar. Hinweise verschwinden nach vier Sekunden.

## Lager und Fahrer

Der obere Workerplatz ist die Leitung. **Fuhrpark** öffnet rechts die einzeln besetzbaren Fahrerplätze. Ihre Anzahl folgt weiterhin den vorhandenen Ausbaustufen bis zu 24 Plätzen. Ein freier Platz bietet nur Farm-Fahrer bzw. Stadt-Lieferfahrer des passenden Lagertyps an. Die Auswahl zeigt den bisherigen Einsatzort; Versetzen und kompatibles Tauschen verwenden die vorhandene atomare Zuweisungslogik. Bereits zugewiesene Kandidaten und Fahrer unterwegs sind nicht neu auswählbar. Eine laufende Lieferung verhindert Entfernen und Wechsel. Die vorhandenen Versorgungsrouten und Warenbestände wurden nicht geändert.

## Technische Integration und Erhaltung

Die festen Panels sind gespeicherte Hierarchieobjekte in `BurningHillsUI/SafeArea`. Die frühere feste Oberfläche liegt unter `BurningHillsUI/ArchivedFixedInterface` inaktiv vor. Wiederholte Worker- und Fuhrparkkarten verwenden `Assets/Weedtopia/Production/Prefabs/InteractionRow.prefab`; nur diese datenabhängigen Einträge werden zur Laufzeit aus einem Prefab-Pool angezeigt. Sämtliche Panels, Schriften und Porträts verwenden die vorhandene Kunst. Plus, Kreuz und Verbesserungspfeile sind skalierbare Vektorgrafiken.

`BuildingInteractionUi` steuert den Kontext, `WorldPointerGesture` trennt Tippen und Ziehen, `BuildingInspectionMetrics` projiziert die vorhandenen Simulationsformeln ohne den Spielstand zu ändern. `BuildingInteractionAuthoring.Apply` ist ein expliziter, wiederholbar sicherer Installer; es gibt keinen automatischen Szenenneuaufbau beim Import oder Spielstart.

Der Bestandsvergleich umfasst 2531 ursprüngliche Dateien. Keine fehlt. Alle bisherigen Kunstquellen, 251 Gebäude-Wrapper, Katalogzuordnungen, Landschafts-Prefabinstanzen, Domain- und Anwendungssysteme bleiben unverändert. Von bestehenden Dateien wurden nur die Spielszene, drei Unity-Präsentationsskripte und die an die neue Bedienung angepassten Spielfluss-Tests geändert. Alle 7091 ursprünglichen serialisierten Szenenblöcke bleiben vorhanden; an geschützten Gameplay-Komponenten und Prefabinstanzen gibt es keine Änderungen. Zwei UI-Komponenten erhielten die neuen Referenzen.

Vor jedem Unity-Start wurden Prozess und Lock geprüft. Kein Benutzereditor wurde beendet oder parallel gestartet. Ein ausschließlich vom eigenen fehlgeschlagenen Compilerlauf hinterlassener Lock wurde erst nach Prozessprüfung, Zeitabgleich und exklusivem Dateizugriff archiviert; [Nachweis](stale_owned_lock.json). Testspielstände liegen in eigenen Diagnose-/Testpfaden.

## Aktuelle Nachweise

| Prüfung | Ergebnis | Beleg |
| --- | --- | --- |
| EditMode | 66/66 bestanden | [XML](../Implementation/Validation/20260925_011317_EditMode_WindowVisibilityFinal.xml), [Lauf](../Implementation/Validation/20260925_011317_EditMode_WindowVisibilityFinal.json) |
| PlayMode | 23/23 bestanden | [XML](../Implementation/Validation/20260925_010923_PlayMode_WindowVisibilityFinal.xml), [Lauf](../Implementation/Validation/20260925_010923_PlayMode_WindowVisibilityFinal.json) |
| Source-Compiler | 12 Assemblies, aktuelle Quellenprüfsummen | [Receipt](../Implementation/Validation/SourceCompile/result.json) |
| Tatsächliche Game View | 47 Aufnahmen des Play Mode | [Receipt](gameview_receipt.json), [Bildprüfung](visual_review.json), [Galerie](review.html) |
| UI-Überläufe | Keine Befunde bei 1920×1080, 1366×768 / 115 %, 2560×1080 | [Receipt](../ProductionFinish/Evidence/ui_overflow.json) |
| Erhaltung | Keine fehlenden Assets oder unerwarteten Änderungen | [SHA-256-Vergleich](preservation.json) |
| Windows-Testbuild | Development-Build erfolgreich; separater D3D11-Diagnoselauf mit gerenderten Szenarien | [Build](../ProductionFinish/Evidence/development_build.json), [Player](../ProductionFinish/Evidence/player_run.json) |
| Produktions-Preflight | Nur unabhängige Abnahme-Gates offen | [Receipt](../ProductionFinish/Evidence/production_preflight.json) |

Die neun ergänzten PlayMode-Tests prüfen die freie Ansicht, Maus-Wischen in allen vier Bezirken, Maus-/Touch-Tipp und Kamerarückkehr, Touch-Wischen und Pinch ohne Fehlklick, UI-Gestenerfassung, alle zwölf kompatiblen Rollen, Fahrerplätze und Schutz laufender Lieferungen, Upgrade-Projektionen sowie die vollständige Rahmung nach einem sofortigen Wechsel zur höchsten Gebäudestufe. Die 14 bestehenden Spielfluss-Tests wurden erneut ausgeführt; veraltete Erwartungen an den Positionsregler wurden auf die neue Oberfläche angepasst.

Frühe fehlerhafte Läufe sind in den datierten Logs erhalten. Sie zählen nicht als bestanden. Die Bildprüfung führte zu Korrekturen an UI-Mark-Renderern, Kostenbeschriftung, Bauplatztext und der Kamerarahmung neuer Gebäudestufen.

Fingerprint: `2a3c5b56d1acc467c2b16b8979b782a3df768a8105475bf628dcf7a240ae9785`.

## Offene Abnahme

Die Maus-/Touch-Ereignisse wurden über virtuelle Geräte im Unity Input System geprüft. Dies ersetzt keine Bedienung auf einem physischen Touchgerät. Die aktuelle Game-View-Bildprüfung ist Agentenprüfung, keine persönliche visuelle Freigabe des Benutzers.

Offene Gesamtspiel-Gates: `target_profile, human_playthrough, audio_listening, audio_license`. Zielgeräteprofil, persönlicher Durchspieltest, Audiohörprobe und Audio-Lizenzabnahme sind weiterhin separat erforderlich. Der Development-Testbuild ist keine freigegebene Veröffentlichung. Die bereits erteilte Gebäudeart-Freigabe bleibt gültig.

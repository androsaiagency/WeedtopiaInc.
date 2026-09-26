<!-- LAYOUT_REVISION_CURRENT -->
# Aktueller Stand nach Benutzerfreigabe

Die vier Gebäudeprototypen und die lokale Freistellung sind ausdrücklich freigegeben; 251 produktive Sprites und die Skizzen-Anordnung wurden integriert. Maßgeblich ist [Docs/LayoutRevision/REPORT.md](../LayoutRevision/REPORT.md).

Die folgenden Abschnitte dokumentieren den historischen Stand vor dieser Integration. Ihre damaligen offenen Gebäude-Gates und Testzahlen gelten nicht als aktueller Status.

---

# Burning Hills – lokaler Validierungsbericht

Stand: 2026-09-21T21:14:18.724265+00:00. Unity 6000.3.9f1 (7a9955a4f2fa), Windows, URP 17.3.0, Input System 1.18.0, uGUI 2.0.0, Test Framework 1.6.0. Inhaltsversion 1, Save-Schema 2. Kein Git-Repository festgestellt; keine Git-, Commit- oder Deploymentbehauptung.

## Aktuelle Gates

| Gate | Ergebnis und Zeitpunkt UTC | Roh-Evidenz | Grenze |
| --- | --- | --- | --- |
| Quellkompilierung | 12 Assemblies erfolgreich, 2026-09-21T21:11:43.070518+00:00 | `Validation/SourceCompile/result.json` und Assemblylogs | Roslyn gegen installierte Unity-Referenzen; vier CS0649-Warnungen für serialisierte Felder, keine Compilerfehler |
| Unity-Import / Domain Reload | Erfolgreich in aktuellen Test-/Captureläufen | `20260921_231101_EditMode_FinalText.log`, `20260921_231220_PlayMode_FinalText.log` unter Validation | Gilt für dieses Unity-Projekt/diese Editorversion |
| EditMode | 50/50, 0 fehlgeschlagen, 0 übersprungen; 2026-09-21T21:12:06.0566516Z | `Validation/20260921_231101_EditMode_FinalText.xml` und `.json` | 32 Gameplay-/Save-/Progressiontests, 14 bestehende Arttests, 4 Assettests |
| PlayMode | 10/10, 0 fehlgeschlagen, 0 übersprungen; 2026-09-21T21:12:46.7925908Z | `Validation/20260921_231220_PlayMode_FinalText.xml` und `.json` | Echte Szenen/UI-Befehle, teilweise deterministische Command-Automation; kein menschlicher Hauptpfad |
| Szenen / Assets | 16 Instanzen, 1 EventSystem, 0 Missing Scripts, 0 Fehler; 2026-09-21T21:11:48.8361382Z | `Validation/game_asset_validation.json` | Bootstrap/BurningHills in Build Settings; Pflichtreferenzen, IDs, Katalog und gesperrtes Produktionsgate geprüft |
| Explizite Installation / Idempotenz | Initialer Installationslauf plus aktuelle Reparatur, 110 Gameplaydateien und 513 Dateien im Produktions-Prefabordner bytegleich; 2026-09-21T21:11:58.9498195Z | `Validation/game_idempotence.json`, `Validation/20260921_221937_Install_M4.json`, aktueller Assettest | Zweiter Lauf überschreibt keine authorierten Daten; unklare bestehende Szene wird geschützt |
| Reale Game View | 14 PNGs, 2026-09-21T21:13:47.7704755Z | `Validation/20260921_231318_Capture_FinalText.log`, `Validation/screenshot_manifest.json`, `Validation/Screenshots/` | Automatisierter PlayMode mit ScreenCapture, Bilder agentseitig geprüft; kein manueller Durchlauf |
| 2D-Prototypen | Vier isolierte Prefabs, Alpha, Bodenpivot, Anker, Mipmaps, ein Collider je Prefab und Sortingkomponente geprüft | `Validation/sprite_prototype_validation.json` | Ausdrückliche Nutzerfreigabe ausstehend; 251 Produktions-Wrapper unverändert |
| Originalbestand | 3171 Ausgangsdateien, 0 fehlend, vier begründete Änderungen, 251 Produktions-Wrapper bytegleich | `Validation/m0_baseline.json`, `Validation/preservation_final.json` | Hashvergleich des vorhandenen Bestands, kein Ersatz für Git-Historie |

`Validation/evidence_manifest.json` bindet aktuelle C#-/asmdef-/Contentdateien per SHA256, bestätigt passende Quellkompilierung und dass die Quellen vor den letzten Teststarts vorlagen. Frühere fehlerhafte Läufe bleiben als Diagnosehistorie vorhanden und werden nicht als aktuelles Ergebnis verwendet.

## Was ausgeführt wurde

EditMode prüft skalierbare Zahlen, alle 5.001 Level je Katalogfamilie, monotone Kurven, 251 Katalogbänder, Massenbilanz bei vollen Puffern, verzögerte/blockierte Fracht, Fahrerbindung, manuelle/automatische Arbeit, Workerrollen/Seltenheiten/Level/Rang, deterministischen Spin, Geld-/Grindertransaktionen, doppelte Belohnungen, Tagesgrenzen/Rückstellung/Nachholen, VIP-Erreichbarkeit, Bauplatzreservierung, Save-/Backup-/Migration-/Offlineverhalten, Referenzfehler und Fortschritt bis Freeplay. Bestehende Arttests wurden nicht umgeschrieben.

PlayMode lädt Bootstrap/BurningHills, startet über die tatsächlichen Namens-/Start-/Skip-Schaltflächen und prüft Farm, Stadtverkauf, Rathaus, Wohnhaus-/Park-Roundtrip nach Szenenneuladen, Industrieverkäufe und Freeplay. Zusätzliche echte UI-Buttonpfade prüfen Worker zuweisen/freistellen/verbessern und Premiumabbruch/-bestätigung. UI-Raycasts verhindern World-Clicks; Pause hält die Simulation an. Die größte angebotene UI-Skalierung lässt Aktionen oberhalb der Navigation und alle Navigationsknöpfe innerhalb des Bildes. Kein unerwarteter Logfehler im geprüften PlayMode-Pfad.

Der öffentliche Command-Fortschrittssimulator erreichte 16 Quests in 72 simulierten Sekunden, Freeplay=True, 51 Grinder verbleibend und 3.34315e3 Geld. Kein Premiumkauf und keine direkte Injektion von Geld, Questfortschritt oder Freischaltungen. Ein gesonderter Pfad mit früh gebautem zweitem Anbauplatz und zweiter Bäckerei erreicht ebenfalls Freeplay. Die 72 Sekunden sind kein menschlicher Tutorialzeitwert.

## Sichtprüfung und Prototypen

Aufnahmen: Intro/Rocko-Text; manuelle Farm; automatisierte Farm mit Engpass; Stadt/Rathaus; Worker nach Spin; Wohnhaus/Park; Industrie/Freeplay; 1366×768 und 2560×1080; größte UI-Skalierung 1,15 bei 1366×768; isolierte Gebäude bei Orthogröße 26 und 38 sowie Nahansichten bei 14.

Erkannte und reparierte Darstellungsfehler: zu kleine Gebäudenamen, mehrdeutige summierte Pufferanzeige, abgeschnittener Aufbauhilfe-Text und Überlappung mit Navigation auf Breitbild. Die letzte Variante verwendet getrennte Puffer, kompakteres Detailpanel, horizontale Navigation mit flexiblen Breiten und am unteren Rand verankerte Aufbauhilfe. Panels sind deckend. Deutsche Texte wurden nach einem Windows-Encodingfehler korrigiert und durch einen Unicode-Codepoint-Test abgesichert. Kurze Engpassmeldungen passen in den geprueften Statusbereich. Die letzten Aufnahmen zeigen diese Korrekturen.

Die drei neuen Coffee-Shop-Cutouts liegen in `Assets/Weedtopia/Game/VisualReview/Sprites/`; das vorhandene Rathausbild bleibt seine unveränderte Quelle. Alle vier Texturen haben 1254×1254 Pixel. Gemessen wurden je 8,384,968 Bytes importierter Texturspeicher inklusive Mips, zusammen etwa 32 MiB. Das ist eine Editor-Speichermessung der vier Prototypen, kein GPU-/FPS-/Mobilbudget für alle 251 Slots. Vor Massenintegration sind bedarfsgesteuertes Laden, plattformspezifische Kompression und Zielgeräteprofiling nötig. Prompts/Quellen: `Validation/sprite_cutout_prompts.json`.

## Offene Abnahmen

- **M9: ausdrückliche visuelle Nutzerfreigabe** für Rathaus und frühe/mittlere/finale Coffee-Shop-Stufe. Bis dahin keine Massenumstellung. `productionVisualsApproved` bleibt false. Produktive 251-Sprite-Abdeckung kann deshalb noch nicht bestanden sein.
- Vollständiger menschlich bedienter Hauptpfad in Game View einschließlich Kamera-/Mausgefühl, lesbarer Dialoge, Save/Load/Recovery und Tageswechsel. Automatische Tests und Bilder ersetzen diese Prüfung nicht.
- Physisches Touchgerät, Android/iOS/WebGL-Builds, Browser, Zielgeräte-Audio und tatsächliches Anhören der Entwicklungsclips wurden nicht geprüft.
- Profiling unter Zielbedingungen, Overdraw, Framezeit/GC, Langzeitsimulation und finales Balancing sind offen. Keine FPS-Zahl behauptet.
- Finale NPC-/Weltgestaltung, animiertes Onboarding, fertiges VFX-/Audio-Polish folgen der gestalterischen Abnahme. Die lokale Spielwelt verwendet erkennbare Entwicklungs-Prefabs.
- Online-/globale Rangliste ist ausdrücklich deaktiviert. Kein Backend, keine Authentifizierung, keine Cloud-/Produktivtests oder Deployments.

## Wiederholung und nächste Bedienung

Projekt mit Unity 6000.3.9f1 öffnen, `Assets/Weedtopia/Game/Scenes/Bootstrap.unity` laden und Play drücken. Namen eingeben, Intro ansehen/überspringen und die angezeigte Hauptquest bedienen. Einzelne Gebäude im World View auswählen. Rathaus enthält Spin/Tagesabholung; das Journal enthält VIP-Rewards. Produktionsart separat über `Assets/Weedtopia/Game/VisualReview/Building2DReview.unity` begutachten.

Explizites Authoring: `Weedtopia > Setup > Create or Repair Game Scaffold`; Prüfung: `Weedtopia > Setup > Validate Game Assets`. Vorher eigene schmutzige Szenen speichern. `Tools/Implementation/Run-UnityValidation.ps1 -Mode EditMode` beziehungsweise `-Mode PlayMode` starten nur ohne laufenden Unity-Prozess und ohne Projektlock. Ein bereits geöffneter Benutzereditor wird niemals beendet. Das alte eigene verwaiste Lock wurde einmal nach bestätigtem Prozessende/exklusiver Öffnung archiviert; dieser Vorgang ist keine automatische Lock-Löschfunktion.

Die erste Produktversion wird wegen dieser offenen Gates ausdrücklich noch nicht als vollständig abgenommen oder veröffentlicht bezeichnet.

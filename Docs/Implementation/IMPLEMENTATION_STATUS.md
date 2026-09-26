<!-- LAYOUT_REVISION_CURRENT -->
# Aktueller Stand nach Benutzerfreigabe

Die vier Gebäudeprototypen und die lokale Freistellung sind ausdrücklich freigegeben; 251 produktive Sprites und die Skizzen-Anordnung wurden integriert. Maßgeblich ist [Docs/LayoutRevision/REPORT.md](../LayoutRevision/REPORT.md).

Die folgenden Abschnitte dokumentieren den historischen Stand vor dieser Integration. Ihre damaligen offenen Gebäude-Gates und Testzahlen gelten nicht als aktueller Status.

---

# Burning Hills – Implementierungsstand

> Aktueller Nachtrag 2026-09-22: Die neue Welt-, Figuren-, UI- und Audio-Praesentation ist im bestehenden Spiel integriert. Aktuelle Ergebnisse und offene Release-Gates stehen in [Production Finish](../ProductionFinish/STATUS.md) und dessen [Pruefbericht](../ProductionFinish/VALIDATION_REPORT.md). Die folgende M0-M10-Tabelle bleibt als historischer Bericht erhalten. Die ausdrueckliche Gebaeudefreigabe und die produktive 251er-Umstellung sind weiterhin offen.

Stand: 2026-09-21T21:14:18.724265+00:00. Masterauftrag vollständig gelesen; M0 vor Änderungen neu gegen Projekt, Prozesse, Lock, Quellbestände, Szenen, Pakete und bestehende Artberichte geprüft. Ergebnis und Ausführung sind in `VALIDATION_REPORT.md` mit Rohbelegen getrennt dokumentiert.

| Meilenstein | Stand |
| --- | --- |
| M0 Audit / Erhaltung | Aktuell durchgeführt. 3.171 vorhandene Dateien gehasht; 251 Produktions-Wrapper unverändert, nichts entfernt. |
| M1 Domain | Implementiert und EditMode-geprüft: skalierbare Zahlen, Stufen, Puffer/Umwandlung, Fahrer, Worker, Ausbau/Premium. |
| M2 Progression | 16 datengetriebene Hauptquests, Zonen, VIP, Tag, Recovery; Referenz- und optionaler Ausbaupfad erreichen Freeplay ohne Premium. |
| M3 Persistenz | Save-Schema 2, Atomik, Backup, Recovery, Migration, acht Stunden Offline, Uhrenrückstellung, Save/Szenenreload/Continue geprüft. |
| M4 Unity-Integration | Bootstrap und BurningHills authoriert; 16 Instanzen, Kamera/Input, uGUI, EventSystem, Fahrzeuge; Pflichtreferenzen/Idempotenz geprüft. |
| M5 Farm | Einstieg, manuelle Ernte, Manager, Veredelung, Lager/Fahrer und Stadtfreischaltung integriert; PlayMode und Game View ausgeführt. |
| M6 Stadt / Rathaus | Coffee-Verkauf, Statistik, Worker/Spin, lokale Bestenliste; Online-deaktiviert-Zustand integriert und geprüft. |
| M7 Siedlung | Dauerhafte Platzwahl, Häuser/Parks, Einwohner/Karten/VIP, Hausmeister/Parkmanager, Tage und Roundtrip geprüft. |
| M8 Industrie / Freeplay | Butter, Supermarkt, Bäckerei, Verkäufe, tägliche Grinder und Abschluss integriert und geprüft. |
| M9 Visuelle Produktion | Vier isolierte 2D-Prototypen und echte Game-View-Nachweise vorbereitet. **Nutzerfreigabe offen. 251er-Massenintegration nicht ausgeführt.** |
| M10 Politur / Abnahme | Automatische Prüfungen, UI-Korrekturen, Audiohooks und Game-View-Aufnahmen ausgeführt. Manueller Gesamtdurchlauf, finales Art-/Audio-/VFX-Polish, Touch, Zielgeräte und Performance offen. |

Aktuell: 50/50 EditMode, 10/10 PlayMode, 12 Quellassemblies, null Missing Scripts/Pflichtreferenzfehler. Rohdateien: `Validation/20260921_231101_EditMode_FinalText.xml`, `Validation/20260921_231220_PlayMode_FinalText.xml`, `Validation/evidence_manifest.json`. Screenshots: `Validation/Screenshots/`.

Beim Audit bestanden 13 vorhandene C#-Dateien und nur SampleScene als Startszene. Jetzt existieren separate Domain-, Application-, Persistence-, Content-, Unity-, Editor- und Testassemblies; authorierte Gameplayassets liegen unter `Assets/Weedtopia/Game`. Keine neue Paketinstallation, kein Commit/Push/Deployment und kein beendeter Benutzereditor.

Vier bestehende Dateien sind begründet geändert: Sprite-Prototyp-Utility (Importerreihenfolge/Erhalt vorhandener GUIDs), Metadaten des vorhandenen Rathaus-Sprites (korrekter Spriteimport), EditorBuildSettings (zwei Spielszenen ergänzt) und ArtProduction/PROGRESS.json (aktuelle Evidenz statt überholtem Editorblocker). Vorhandene Bilder, Modelle, manuelle Wrapperzuweisungen, Offsets und Projekteinstellungen außerhalb der Buildliste sind hashgleich.

Die produktive Artfreigabe fehlt. Der aktuelle Gameplaypfad ist eine funktionierende lokale Version mit Entwicklungsdarstellung und kein vollständig freigegebenes Endprodukt. Der frühere offene-Editor-Blocker ist durch frische sichere eigene Läufe aufgelöst. Historische fehlgeschlagene Versuche werden im Ergebnis nicht mitgezählt.

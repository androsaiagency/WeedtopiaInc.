# Gebäude mittig auf den Bauplätzen

Stand: 2026-09-25T05:46:23.717251+00:00.

Alle 251 produktiven Gebäudevarianten sind nach unten versetzt. Ihre Grundfläche liegt nun mittig auf dem Bauplatz; zuvor lag die vordere untere Spritekante auf dessen Mittelpunkt. Der Versatz beträgt 22 Prozent der sichtbaren Spritebreite und berücksichtigt damit die unterschiedliche Größe der Gebäudestufen. Transparente Bildränder werden nicht mitgemessen.

Die Korrektur ist als lokale Position von `ProductionSpriteVisual` im jeweiligen Prefab gespeichert. `footprintInset` dokumentiert den bereits angewendeten Anteil, damit ein erneuter Aufruf keinen weiteren Versatz addiert und zusätzliche manuelle Offsets erhalten bleiben. Auch die bestehende Importfunktion kennt die neue Grundausrichtung. Es gibt keine zusätzliche Laufzeitverschiebung oder Änderung der Bilddateien.

Der [Datei- und Prefabvergleich](preservation.json) prüft alle 2549 ursprünglichen Dateien. An jedem Gebäude wurden exakt zwei serialisierte Blöcke geändert: die lokale Spriteposition und der gespeicherte Versatz im Spritebaustein. Alle anderen Prefabblöcke, die Szene, Bauplätze, Katalogzuordnungen, Quellbilder, Liefer-/Workeranker und Gameplaydateien bleiben unverändert.

Aktuelle Nachweise:

- **PlayMode 23/23 bestanden** nach der Prefabkorrektur; [XML](../Implementation/Validation/20260925_073757_PlayMode_ParcelCenter.xml). Anschließend wurde ausschließlich der Editor-Aufnahmeablauf um zwei Auflösungen ergänzt; Laufzeitcode und Prefabs blieben unverändert.
- **EditMode 66/66 bestanden**; [XML](../Implementation/Validation/20260925_074155_EditMode_ParcelCenterFinal.xml).
- **12 Assemblies kompiliert**, aktuelle Quellenprüfsummen geprüft; [Compiler](source_compile.json).
- **16 tatsächliche Game-View-Aufnahmen**: alle elf Familien, Stadtübersicht, Coffee-Shop-Ruine und Level 5000 sowie Lager bei 1366×768/115 % und 2560×1080. [Aufnahmelauf](gameview_receipt.json), [Bildprüfung](visual_review.json).
- Keine gemeldeten UI-Überläufe in den erfassten Ansichten der drei Auflösungen. Die umfassende Bedienungsprüfung des vorherigen Auftrags bleibt separat dokumentiert.

![Stadtansicht nach der Korrektur](Screenshots/city_overview.png)

Fingerprint: `02112b30f7991e8845135811b3657d909c600b85b900a2f527125a352e69ce99`.

Die Spielszene verwendet die geänderten Prefabs direkt. Der frühere Windows-Diagnosebuild wurde für diese reine Platzierungskorrektur nicht neu erstellt. Physische Touchbedienung und die Gesamtspiel-/Audio-/Zielgeräteabnahmen sind weiterhin offen. Kein Benutzereditor wurde beendet oder parallel gestartet.

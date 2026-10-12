# Prüfstand – Questbelohnungen und Sammlung

Unity 6000.3.9f1, lokales Projekt, 30.09.2026. Alle Läufe benutzten einen eigenen Hintergrund-Editor; die Spieltests und Aufnahmen verwenden eigene Diagnosespeicherstände.

| Prüfung | Ergebnis | Nachweis |
| --- | --- | --- |
| Domain-, Wirtschafts-, Quest- und Speichertests | 119 von 119 bestanden | `../Implementation/Validation/20260930_210332_EditMode_QuestRewards.xml` |
| Gesamte PlayMode-Suite nach Integration | 47 von 47 bestanden | `../Implementation/Validation/20260930_210733_PlayMode_QuestRewards.xml` |
| Gezielte PlayMode-Nachprüfung nach Layoutkorrekturen | 4 von 4 bestanden | `../Implementation/Validation/20260930_211938_PlayMode_QuestRewardsFinal.xml` |
| Quellcode-Kompilierung | Alle 12 Assemblies erfolgreich | `../Implementation/Validation/SourceCompile/result.json` |
| Installation und erforderliche Prefab-Referenzen | Erfolgreich, einschließlich CanvasRenderer der Lichtstrahlen | `authoring_receipt.json` |
| Finale echte Game-View-Aufnahmen | Zehn aktuelle Ansichten erfolgreich gerendert | `gameview_receipt.json` und `Screenshots/` |
| Text- und Aktionsgrenzen | Bestanden, 0 Probleme: 1920×1080, 1366×768 bei 115 % UI-Skalierung, 2560×1080 und 1080×1920 | `ui_overflow.json` |
| Erhalt bestehender Prefabs | 30 geprüfte Dateien unverändert | `preservation-after.json` |
| Erhalt bestehender Szenenobjekte | Keine alten Objekte entfernt; geänderte alte Objekte gehören zur UI | `scene-preservation.json` |

Die gezielte UI-Nachprüfung deckt gespeicherte Vergabe, ausbleibende Doppelvergaben, Laden ohne erneute Enthüllung, Neustart ohne alte Belohnungen, aktuelle Bestände, Wechsel zur Teamverwaltung und den tatsächlichen obersten Pointer-Treffer der neuen Buttons ab. Die anschließenden Änderungen betreffen nur das Rendering des Strahlenkranzes; sie werden im finalen Game View und durch die Referenzprüfung kontrolliert.

Für die Aufnahmen durchläuft der Diagnosespieler die Anfangsquests und das Nachbarschaftsfest mit öffentlichen Spielbefehlen. Die Stadt-Quest wird über `GameRoot.Command` eingelöst. Die finale Stadtfestbelohnung wird nach dem automatisierten Durchlauf aus dem gerade gespeicherten Beleg angezeigt. Die Aufnahmen erzeugen keine zusätzlichen Belohnungen und verändern keinen produktiven Spielstand.

Die visuelle Kontrolle fand an den tatsächlichen Aufnahmen statt. Dabei wurden unnatürliche Wortumbrüche, ein verdeckter Schließen-Button und die fehlende Renderer-Komponente der Strahlengrafik entdeckt und behoben. Ein automatischer Texttest allein hatte diese visuellen bzw. Eingabeprobleme nicht erkannt.

Nicht beansprucht: persönliche Art-Abnahme, physischer Touch-Test, Hörtest am Zielgerät, WebGL- oder Release-Build-Abnahme. `userAcceptance` bleibt ausdrücklich `false`.

Der maschinenlesbare Abschlussbeleg ist `completion_receipt.json`. Die bestehenden Wirtschaftsdaten und alle zuvor vorhandenen Texte wurden mit der Sicherung vor dieser Änderung verglichen und sind identisch. Nach den Prüfungen ist kein eigener Unity-Prozess mehr aktiv und keine Projektsperre zurückgeblieben.

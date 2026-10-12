# Rathaus: ursprüngliches 2D-Sprite wiederhergestellt

Entscheidung des Nutzers vom 30.09.2026: Weedtopia bleibt bei einer festen isometrischen Kamera und dem bestehenden 2D/2.5D-Gebäudestil. Eine freie Kameraführung ist auch künftig nicht geplant. Das technisch erfolgreiche 3D-Rathaus-Experiment ist beendet und archiviert.

## Produktiver Stand

- Bestehendes Prefab: `Assets/Weedtopia/Art/Buildings/Prefabs/town_hall/WT_town_hall_T_FIXED.prefab`
- Unveränderte GUID: `fb592e607e1cdf74b93684f1cc98441b`
- Reaktiviertes Original: `ProductionSpriteVisual`, Resource `WeedtopiaBuildings/town_hall/T_FIXED`
- Originalbild: `Assets/Weedtopia/Production/Resources/WeedtopiaBuildings/town_hall/T_FIXED.png`
- Alle 251 Gebäudestufen verwenden wieder Sprites; keine aktive 3D-Rathaus-Ausnahme.
- Sprite-Objekt, Transform, SpriteRenderer, BuildingSpriteVisual und BoxCollider entsprechen exakt der Sicherung vor der 3D-Integration. Größe, manuelle Offsets, Pivot und Klickfläche wurden nicht neu berechnet.
- Katalogidentität und Gameplay-Anker bleiben erhalten. Szene, andere Gebäude und Spiellogik wurden nicht umgebaut.

## Erhaltenes 3D-Experiment

- Modell, LODs und aufgehellte Materialien: `Assets/Weedtopia/Art/Buildings/Archive/TownHall3D20260928/Model`
- Testszene, Quellen, Vergleichsmaterialien: `Assets/Weedtopia/Art/Buildings/Archive/TownHall3D20260928/Review`
- Die Verschiebung erfolgte über Unity AssetDatabase mit erhaltenen GUIDs. 55 verschobene Dateien sind bytegleich zur Sicherung vor der Rückstellung.
- Highpoly-GLB, Blender-Arbeiten, Rebakes, Bilder und bisherige Prüfberichte bleiben im übergeordneten Experimentordner erhalten. Alte Berichte beschreiben den damaligen Stand; ihre früheren Assetpfade sind durch die Archivpfade ersetzt.
- Das produktive Rathaus hat keine Modell- oder Archivabhängigkeit mehr.
- Die frühere Installationsfreigabe liegt unverändert in `Backup/3d_installation_approval.json`. `../CityIntegration/approval.json` erlaubt keine erneute 3D-Installation. Dafür ist eine neue ausdrückliche Entscheidung erforderlich.

## Nachweise

- `restoration.json`: ursprüngliches Sprite aktiv, Gameplay-Anker und Sprite-Einstellungen erhalten, keine Mesh-Abhängigkeiten.
- `preservation_before.json` / `preservation_after.json`: 747 geprüfte Dateien, darunter andere Gebäude, Szene, Spielassets, Projekteinstellungen und archivierte Dateien; unverändert. Sprite-Komponenten zusätzlich gegen den Stand vor der 3D-Integration verglichen.
- EditMode: `Docs/Implementation/Validation/20260930_053517_EditMode_TownHall2DRestore.json`, 4/4 bestanden. Enthält die erneute Ausführung der Rückstellung ohne Änderung der Prefab-Bytes, Archivprüfung und Produktions-Preflight.
- PlayMode: `Docs/Implementation/Validation/20260930_053707_PlayMode_TownHall2DRestore.json`, 19/19 bestanden, einschließlich Maus-/Touch-Auswahl, Statistik und Rocko-Dialog.
- Fünf aktuelle Aufnahmen aus dem tatsächlichen PlayMode Game View (1920×1080) wurden geprüft: Stadtumgebung, Detail, Auswahl, Statistik und Dialog. Siehe `validation.json`, `gameview_receipt.json` und `Screenshots`. Die Detailaufnahme ist stark gezoomt; Stadt- und Auswahlansicht zeigen das vollständige Rathaus.
- Die Sichtprüfung durch den Agenten ersetzt keine neue Nutzerabnahme; die ausdrücklich beauftragte Rückstellung des Original-Sprites ist abgeschlossen.

Die unabhängigen Release-Abnahmen bleiben offen; diese Rückstellung ist keine allgemeine Release-Freigabe.

## Wiederholbare lokale Ausführung

Aus dem Projektverzeichnis und nur ohne vorhandenen Unity-Editor oder Lock:

```powershell
& Docs/ArtProduction/Buildings/town_hall/T_FIXED/Meshy_Highpoly_2026-09-28/run_restore_2d.ps1 -Mode Apply
& Docs/ArtProduction/Buildings/town_hall/T_FIXED/Meshy_Highpoly_2026-09-28/run_restore_2d.ps1 -Mode Capture
```

Die Wiederherstellung ist explizit und idempotent; sie läuft nicht automatisch beim Import oder Editorstart. Die Aufnahmen verwenden einen isolierten Diagnose-Spielstand.

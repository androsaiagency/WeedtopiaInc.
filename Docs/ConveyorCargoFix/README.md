# Vollständige Erntekisten auf dem Förderband

Die untere Hälfte des kameraseitigen Kisten-Sprites schnitt die deckende Förderbandfläche. Dadurch wurden die vordere Holzkante und die Metallecke im Tiefentest verdeckt; die PNG-Datei selbst ist vollständig.

Die `Artwork`-Position im gemeinsamen `HarvestTray.prefab` ist um 2,2 Welteinheiten entlang der Kamerablickachse zur Kamera versetzt. In der orthografischen Spielkamera bleibt dadurch die Bildschirmposition gleich. Die gesamte Sprite-Fläche liegt jetzt oberhalb von Band und Übergabetellern, einschließlich der leichten Bewegung in der Höhe. Der Tiefentest bleibt aktiv. Die explizite Förderband-Installation verwendet denselben Versatz beim erstmaligen Erstellen des Prefabs.

## Nachweis vom 28.09.2026

- Sechs tatsächliche PlayMode-Game-View-Aufnahmen: jeweils vorher/nachher auf der senkrechten Zuleitung, am Übergabeteller und am schrägen Gebäudeanschluss. Das letzte Bildpaar trägt den Dateinamen `horizontal_*`.
- Der Vorherzustand wird ausschließlich im eigenen Diagnose-Spielstand durch Zurücksetzen des Grafikversatzes reproduziert. Die Nachherbilder verwenden den gespeicherten Prefab-Versatz. Keine Szene wird dabei gespeichert.
- `geometry_receipt.json` bestätigt in allen drei Nachheransichten positiven Abstand zwischen der vollständigen Sprite-Geometrie und der höchsten Bandfläche. Die Abweichung der Bildschirmposition beträgt weniger als 0,001 Pixel.
- Unity 6000.3.9f1 hat kompiliert und die Aufnahme erfolgreich beendet. Bericht: `Docs/Implementation/Validation/20260928_204921_ConveyorCargoCapture_CargoClippingFix.json`. Es wurde für diese reine Prefab-Korrektur keine zusätzliche Testsuite ausgeführt.
- `BurningHills.unity` ist bytegleich zum Stand vor dieser Korrektur: SHA-256 `2d81f593f8881b4b22c9a82865e5ce6e5f97e74eceef3f26d6d7e66e36c3348a`.

Reproduktion: `Tools/Implementation/Run-UnityValidation.ps1 -Mode ConveyorCargoCapture -Label CargoClippingFix`.

![Vollständige Erntekiste](Screenshots/vertical_after.png)

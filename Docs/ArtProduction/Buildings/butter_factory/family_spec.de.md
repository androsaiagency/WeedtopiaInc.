# Butterfabrik – verbindliche Familien-DNA

Funktion: Lebensmitteltaugliche Verarbeitung und Versorgung von Bäckereien; sauber und freundlich.

Silhouette: Abgerundete cremefarbene Mischhalle mit paarigen sauberen Tanks und dicker Rohrbrücke. Materialprioritäten: cream enamel, sage enamel, brass, restrained thick green glass.

Reservierte maximale Stellfläche: **20 × 18 m**, maximale Höhe **28 m**. Ein Unity-Meter entspricht einer Unity-Einheit. Die reservierte Fläche wird nicht als Podest gerendert. Das frühe Visual nutzt nur einen Teil der Fläche; zunehmende Kapazität wächst bevorzugt nach hinten und oben. Die in den Slots genannten Maße sind Richtwerte für die Art-Produktion, keine Gameplaywerte.

Front bleibt lokal **+Z**, oben **+Y**, Pivot am Bodenmittelpunkt der reservierten Parzelle. Die Vorderkante bleibt bei z=+9; ein kleineres Visual darf seinen Körper nach hinten von dieser Frontkante entwickeln. Im Bild erscheint die +Z-Front links unten, die +X-Seite rechts unten. Endgültiger geometrischer Pivot und Anschlüsse werden am Wrapper manuell geprüft.

Anschlüsse: Ernteeingang links; Butterausgabe zu Bäckereien rechts; separate rückwärtige Wartung. InteractionAnchor am vorderen Hauptzugang, WorkerAnchor seitlich des Servicebereichs, DeliveryInputAnchor/DeliveryOutputAnchor passend zu den beschriebenen Ports, VfxAnchor über dem Funktionskern. ColliderRoot bleibt leer bis zur sichtbaren vereinfachten Collider-QA. Fahrzeuge, Förderbänder und Worker bleiben eigenständige spätere Prefabs.

T01 ist der verbindliche Formanker. T00 zeigt seine reparierbare Vorgeschichte; danach wird T02 aus T01 weiterentwickelt, nicht aus T00. Jede folgende Stufe nutzt T01 plus ihren direkten akzeptierten Vorgänger. Bei Gebäuden bleiben frühere Funktionsvolumen integriert und dürfen kompakt gestapelt werden; keine 24 unabhängigen Anbauten. Beim Anbau ersetzt jede Kategorie die vorherige vollständig und hält die vorgegebene exakte Modulzahl ein.

## Ausbaumerkmale

- T01: Reparierte Mischhalle mit zwei Rundtanks.
- T02: Großer dritter lebensmitteltauglicher Tank.
- T03: Breite geschlossene Zutatenannahme.
- T04: Rückwärtige Rührwerkhalle.
- T05: Große gerundete Mischhallenhaube über der bisherigen Hauptanlage.
- T06: Zweiter großer Mischkessel.
- T07: Breite geschlossene Wartungs- und Prozessbrücke über der hinteren Rührwerkhalle.
- T08: Breites Solardach über der Mischhalle.
- T09: Großer runder Butter-Puffertank.
- T10: Zweiter hoher Mischkessel.
- T11: Breiter sauberer Verpackungstrakt.
- T12: Zweites vollständiges Prozessgeschoss.
- T13: Rückwärtige kompakte Tankhalle.
- T14: Hoher runder Hygienekern.
- T15: Großer gemeinsamer Ausgabesammler.
- T16: Drittes kompaktes Prozessgeschoss.
- T17: Vertikaler grüngläserner Mischkern.
- T18: Zweiter gekoppelter Mischkern.
- T19: Große kupfergefasste Solardecke.
- T20: Oberer geschlossener Tankring.
- T21: Breite städtische Verarbeitungsterrasse.
- T22: Hohe gemeinsame Ausgaberotunde.
- T23: Zwei obere emaillierte Mischkuppeln.
- T24: Landmark-Krone aus verbundenen Butterkesseln.

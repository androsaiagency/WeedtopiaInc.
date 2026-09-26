# Bäckerei – verbindliche Familien-DNA

Funktion: Backstube und Weiterverarbeitung der Butter; warme Ofen- und Kupferformen statt Tankdominanz.

Silhouette: Terrakotta-Ofenbogen mit kurzem kräftigem Schornstein und kupfergedecktem Lieferfenster. Materialprioritäten: terracotta, warm plaster, reclaimed wood, aged copper.

Reservierte maximale Stellfläche: **16 × 16 m**, maximale Höhe **27 m**. Ein Unity-Meter entspricht einer Unity-Einheit. Die reservierte Fläche wird nicht als Podest gerendert. Das frühe Visual nutzt nur einen Teil der Fläche; zunehmende Kapazität wächst bevorzugt nach hinten und oben. Die in den Slots genannten Maße sind Richtwerte für die Art-Produktion, keine Gameplaywerte.

Front bleibt lokal **+Z**, oben **+Y**, Pivot am Bodenmittelpunkt der reservierten Parzelle. Die Vorderkante bleibt bei z=+8; ein kleineres Visual darf seinen Körper nach hinten von dieser Frontkante entwickeln. Im Bild erscheint die +Z-Front links unten, die +X-Seite rechts unten. Endgültiger geometrischer Pivot und Anschlüsse werden am Wrapper manuell geprüft.

Anschlüsse: Zutatenannahme links; Lieferfenster rechts; Servicezugang mittig. InteractionAnchor am vorderen Hauptzugang, WorkerAnchor seitlich des Servicebereichs, DeliveryInputAnchor/DeliveryOutputAnchor passend zu den beschriebenen Ports, VfxAnchor über dem Funktionskern. ColliderRoot bleibt leer bis zur sichtbaren vereinfachten Collider-QA. Fahrzeuge, Förderbänder und Worker bleiben eigenständige spätere Prefabs.

T01 ist der verbindliche Formanker. T00 zeigt seine reparierbare Vorgeschichte; danach wird T02 aus T01 weiterentwickelt, nicht aus T00. Jede folgende Stufe nutzt T01 plus ihren direkten akzeptierten Vorgänger. Bei Gebäuden bleiben frühere Funktionsvolumen integriert und dürfen kompakt gestapelt werden; keine 24 unabhängigen Anbauten. Beim Anbau ersetzt jede Kategorie die vorherige vollständig und hält die vorgegebene exakte Modulzahl ein.

## Ausbaumerkmale

- T01: Reparierte Backstube mit einem Terrakottaofen.
- T02: Zweiter breiter Ofenbogen.
- T03: Breite baulich verbundene Versandloggia hinter dem bestehenden Lieferfenster.
- T04: Rückwärtiger Teigküchenflügel.
- T05: Großes neues Terrakotta-Tonnengewölbe über der bisherigen Backhalle.
- T06: Zweiter kräftiger kurzer Schornstein.
- T07: Breite geschlossene Zutatenannahme.
- T08: Großes Solardach über der Backstube.
- T09: Runder kupferner Teigbereitungskern.
- T10: Zweite vollständige Ofenhalle.
- T11: Breiter sauberer Versandflügel.
- T12: Zweites vollständiges Backgeschoss.
- T13: Rückwärtige automatisierte Teigküche.
- T14: Großer zusammengefasster Kaminkörper.
- T15: Breite obere Liefergalerie.
- T16: Drittes kompaktes Backgeschoss.
- T17: Hoher kupferner Wärmekreislaufkern.
- T18: Zweiter gekoppelter oberer Ofenflügel.
- T19: Dick verglaste obere Teigküchenhalle.
- T20: Große solargedeckte Backterrasse.
- T21: Breite städtische Ofenrotunde.
- T22: Zwei hohe verbundene Kupferhauben.
- T23: Gestufte obere Produktionsgalerie.
- T24: Markante Terrakotta-Kupfer-Krone aus Ofenbögen.

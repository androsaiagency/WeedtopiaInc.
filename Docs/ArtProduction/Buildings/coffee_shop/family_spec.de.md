# Coffee-Shop – verbindliche Familien-DNA

Funktion: Verkaufsfront und zunehmende Kundenkapazität; keine Konsumszene.

Silhouette: Runde Eckfront mit tiefem Servicefenster unter weicher salbeigrüner Markise. Materialprioritäten: warm plaster, reclaimed wood, sage enamel, muted turquoise.

Reservierte maximale Stellfläche: **14 × 14 m**, maximale Höhe **24 m**. Ein Unity-Meter entspricht einer Unity-Einheit. Die reservierte Fläche wird nicht als Podest gerendert. Das frühe Visual nutzt nur einen Teil der Fläche; zunehmende Kapazität wächst bevorzugt nach hinten und oben. Die in den Slots genannten Maße sind Richtwerte für die Art-Produktion, keine Gameplaywerte.

Front bleibt lokal **+Z**, oben **+Y**, Pivot am Bodenmittelpunkt der reservierten Parzelle. Die Vorderkante bleibt bei z=+7; ein kleineres Visual darf seinen Körper nach hinten von dieser Frontkante entwickeln. Im Bild erscheint die +Z-Front links unten, die +X-Seite rechts unten. Endgültiger geometrischer Pivot und Anschlüsse werden am Wrapper manuell geprüft.

Anschlüsse: Kunden-/Servicefront mittig; Warenannahme vorn links; seitliche Ausgabe rechts. InteractionAnchor am vorderen Hauptzugang, WorkerAnchor seitlich des Servicebereichs, DeliveryInputAnchor/DeliveryOutputAnchor passend zu den beschriebenen Ports, VfxAnchor über dem Funktionskern. ColliderRoot bleibt leer bis zur sichtbaren vereinfachten Collider-QA. Fahrzeuge, Förderbänder und Worker bleiben eigenständige spätere Prefabs.

T01 ist der verbindliche Formanker. T00 zeigt seine reparierbare Vorgeschichte; danach wird T02 aus T01 weiterentwickelt, nicht aus T00. Jede folgende Stufe nutzt T01 plus ihren direkten akzeptierten Vorgänger. Bei Gebäuden bleiben frühere Funktionsvolumen integriert und dürfen kompakt gestapelt werden; keine 24 unabhängigen Anbauten. Beim Anbau ersetzt jede Kategorie die vorherige vollständig und hält die vorgegebene exakte Modulzahl ein.

## Ausbaumerkmale

- T01: Reparierter Laden mit tiefem Servicefenster.
- T02: Breite zweite Serviceöffnung.
- T03: Überdachte fest verbundene Kundenveranda.
- T04: Rückwärtiger Verkaufsflügel.
- T05: Neuer hoher runder Ecksaal hinter der unveränderten Servicefront.
- T06: Zusätzliche breite Fensterfront.
- T07: Fest integrierte Galerie über der Veranda.
- T08: Großes salbeigrünes Solarmarkisendach.
- T09: Runder emaillierter Versorgungserker.
- T10: Zweiter durchgehender Verkaufsflügel.
- T11: Breite gestützte Kundengalerie.
- T12: Vollständiges zweites Verkaufsgeschoss.
- T13: Große rückwärtige Servicehalle.
- T14: Kupfergedeckte obere Eckloggia.
- T15: Zweites tiefes Verandagalerieband.
- T16: Drittes kompaktes Verkaufsgeschoss.
- T17: Hoher begrünter Eckkern.
- T18: Gekoppelter oberer Verkaufsflügel.
- T19: Große dick verglaste Wintergartenloggia.
- T20: Gestaffelte solargedeckte Kundenterrasse.
- T21: Breite oberste Rundgalerie.
- T22: Hoher kupfergedeckter Servicepavillon.
- T23: Zwei kompakte obere Eckflügel.
- T24: Unverwechselbare doppelte Blätterdachkrone.

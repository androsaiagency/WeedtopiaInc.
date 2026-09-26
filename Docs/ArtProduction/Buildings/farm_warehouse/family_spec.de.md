# Farm-Lager – verbindliche Familien-DNA

Funktion: Nimmt veredelte Ernte an und bündelt Versand zur Stadt; Dockvolumen deutet Fahrer-Kapazität an.

Silhouette: Breite Scheunengiebel mit tiefem Versandtor und seitlichem rundem Dispatch-Kopf. Materialprioritäten: reclaimed wood, cream plaster, aged metal, sage enamel.

Reservierte maximale Stellfläche: **22 × 18 m**, maximale Höhe **26 m**. Ein Unity-Meter entspricht einer Unity-Einheit. Die reservierte Fläche wird nicht als Podest gerendert. Das frühe Visual nutzt nur einen Teil der Fläche; zunehmende Kapazität wächst bevorzugt nach hinten und oben. Die in den Slots genannten Maße sind Richtwerte für die Art-Produktion, keine Gameplaywerte.

Front bleibt lokal **+Z**, oben **+Y**, Pivot am Bodenmittelpunkt der reservierten Parzelle. Die Vorderkante bleibt bei z=+9; ein kleineres Visual darf seinen Körper nach hinten von dieser Frontkante entwickeln. Im Bild erscheint die +Z-Front links unten, die +X-Seite rechts unten. Endgültiger geometrischer Pivot und Anschlüsse werden am Wrapper manuell geprüft.

Anschlüsse: Annahme vorn links; Versandbucht vorn rechts; reservierte Fahrzeugaufstellung außerhalb des Meshs. InteractionAnchor am vorderen Hauptzugang, WorkerAnchor seitlich des Servicebereichs, DeliveryInputAnchor/DeliveryOutputAnchor passend zu den beschriebenen Ports, VfxAnchor über dem Funktionskern. ColliderRoot bleibt leer bis zur sichtbaren vereinfachten Collider-QA. Fahrzeuge, Förderbänder und Worker bleiben eigenständige spätere Prefabs.

T01 ist der verbindliche Formanker. T00 zeigt seine reparierbare Vorgeschichte; danach wird T02 aus T01 weiterentwickelt, nicht aus T00. Jede folgende Stufe nutzt T01 plus ihren direkten akzeptierten Vorgänger. Bei Gebäuden bleiben frühere Funktionsvolumen integriert und dürfen kompakt gestapelt werden; keine 24 unabhängigen Anbauten. Beim Anbau ersetzt jede Kategorie die vorherige vollständig und hält die vorgegebene exakte Modulzahl ein.

## Ausbaumerkmale

- T01: Reparierte Scheune mit einem Versandtor.
- T02: Zweites breites Versandtor.
- T03: Tiefe überdachte Ladebucht.
- T04: Rückwärtiger Garagenflügel ohne Fahrzeuge.
- T05: Erhöhte verputzte Hauptlagerhalle mit breitem durchgehendem Dachmonitor.
- T06: Zweiter überdachter Dockblock.
- T07: Breiter runder Dispatch-Aufsatz.
- T08: Zusammenhängendes Solardach.
- T09: Hohe emaillierte Lagersektion.
- T10: Seitlicher gestapelter Dockblock.
- T11: Großer rückwärtiger Hochlagerkern.
- T12: Zweites Lagergeschoss.
- T13: Breiter automatisierter Versandtrakt.
- T14: Zweiter rückwärtiger Hochlagerkern.
- T15: Runde zentrale Dispatch-Brücke.
- T16: Drittes Lagergeschoss.
- T17: Hoher kompakter Logistikturm.
- T18: Gekoppelter zweiter Logistikturm.
- T19: Kupfergedeckte gemeinsame Ladehalle.
- T20: Vertikales geschlossenes Sortiermodul.
- T21: Gestuftes urbanes Hochlagerdach.
- T22: Breite zentrale Versandrotunde.
- T23: Gekoppelte obere Dock-Galerie.
- T24: Markante doppelte Scheunengiebelkrone.

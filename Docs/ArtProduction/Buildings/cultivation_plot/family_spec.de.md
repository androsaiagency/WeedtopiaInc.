# Anbauplatz – verbindliche Familien-DNA

Funktion: Erzeugt die Ernte; Abholung und Förderband bleiben separate spätere Systeme.

Silhouette: Abgerundete Pflanzgefäße mit kräftigem Rand, grünem Pflanzenvolumen und frontseitigem Bewässerungskragen. Materialprioritäten: terracotta, sage enamel, reclaimed wood, restrained thick green glass.

Reservierte maximale Stellfläche: **12 × 12 m**, maximale Höhe **25 m**. Ein Unity-Meter entspricht einer Unity-Einheit. Die reservierte Fläche wird nicht als Podest gerendert. Das frühe Visual nutzt nur einen Teil der Fläche; zunehmende Kapazität wächst bevorzugt nach hinten und oben. Die in den Slots genannten Maße sind Richtwerte für die Art-Produktion, keine Gameplaywerte.

Front bleibt lokal **+Z**, oben **+Y**, Pivot am Bodenmittelpunkt der reservierten Parzelle. Die Vorderkante bleibt bei z=+6; ein kleineres Visual darf seinen Körper nach hinten von dieser Frontkante entwickeln. Im Bild erscheint die +Z-Front links unten, die +X-Seite rechts unten. Endgültiger geometrischer Pivot und Anschlüsse werden am Wrapper manuell geprüft.

Anschlüsse: Bewässerung vorn links; Ernteübergabe vorn rechts; keine eingebauten Förderbänder. InteractionAnchor am vorderen Hauptzugang, WorkerAnchor seitlich des Servicebereichs, DeliveryInputAnchor/DeliveryOutputAnchor passend zu den beschriebenen Ports, VfxAnchor über dem Funktionskern. ColliderRoot bleibt leer bis zur sichtbaren vereinfachten Collider-QA. Fahrzeuge, Förderbänder und Worker bleiben eigenständige spätere Prefabs.

T01 ist der verbindliche Formanker. T00 zeigt seine reparierbare Vorgeschichte; danach wird T02 aus T01 weiterentwickelt, nicht aus T00. Jede folgende Stufe nutzt T01 plus ihren direkten akzeptierten Vorgänger. Bei Gebäuden bleiben frühere Funktionsvolumen integriert und dürfen kompakt gestapelt werden; keine 24 unabhängigen Anbauten. Beim Anbau ersetzt jede Kategorie die vorherige vollständig und hält die vorgegebene exakte Modulzahl ein.

## Ausbaumerkmale

- T01: 1 × robuster Topf.
- T02: 2 × robuster Topf.
- T03: 3 × robuster Topf.
- T04: 4 × robuster Topf.
- T05: 1 × kleine Anbaulage.
- T06: 2 × kleine Anbaulage.
- T07: 3 × kleine Anbaulage.
- T08: 4 × kleine Anbaulage.
- T09: 1 × große Anbaulage.
- T10: 2 × große Anbaulage.
- T11: 3 × große Anbaulage.
- T12: 4 × große Anbaulage.
- T13: 1 × Mega-Anbaulage.
- T14: 2 × Mega-Anbaulage.
- T15: 3 × Mega-Anbaulage.
- T16: 4 × Mega-Anbaulage.
- T17: 1 × gekoppeltes Giga-Gewächshaussegment.
- T18: 2 × gekoppeltes Giga-Gewächshaussegment.
- T19: 3 × gekoppeltes Giga-Gewächshaussegment.
- T20: 4 × gekoppeltes Giga-Gewächshaussegment.
- T21: Ein kompakter vertikaler Grow-Turm.
- T22: Zwei gekoppelte Grow-Türme.
- T23: Drei gekoppelte Grow-Türme.
- T24: Vier Grow-Türme mit zentralem Biokuppel-Kern.

Maßtrennung T01: Der Terrakottatopf hat etwa 0,65 m Außendurchmesser; 1,2 × 1,2 m bezeichnet die Pflanzenkronenhülle einschließlich Blattüberstand. Die Gesamthöhe inklusive Pflanze beträgt etwa 1,8 m. Der gespeicherte tatsächlich verwendete Bildprompt bleibt unverändert.

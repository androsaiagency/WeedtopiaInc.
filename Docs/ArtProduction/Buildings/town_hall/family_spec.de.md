# Rathaus – verbindliche Familien-DNA

Funktion: Fester Arbeitsplatz Rockos und Zentrum für Stadtstatistiken; kein Charakter im Mesh.

Silhouette: Symmetrischer reparierter Bürgersaal mit rundem Zentralbau und Berg-/Flammenkrone. Materialprioritäten: warm plaster, sage enamel, matte stone, oxidized copper.

Reservierte maximale Stellfläche: **20 × 18 m**, maximale Höhe **22 m**. Ein Unity-Meter entspricht einer Unity-Einheit. Die reservierte Fläche wird nicht als Podest gerendert. Das frühe Visual nutzt nur einen Teil der Fläche; zunehmende Kapazität wächst bevorzugt nach hinten und oben. Die in den Slots genannten Maße sind Richtwerte für die Art-Produktion, keine Gameplaywerte.

Front bleibt lokal **+Z**, oben **+Y**, Pivot am Bodenmittelpunkt der reservierten Parzelle. Die Vorderkante bleibt bei z=+9; ein kleineres Visual darf seinen Körper nach hinten von dieser Frontkante entwickeln. Im Bild erscheint die +Z-Front links unten, die +X-Seite rechts unten. Endgültiger geometrischer Pivot und Anschlüsse werden am Wrapper manuell geprüft.

Anschlüsse: Bürgerzugang vorn mittig; Servicezugang links; rückwärtiger Verwaltungsflügel. InteractionAnchor am vorderen Hauptzugang, WorkerAnchor seitlich des Servicebereichs, DeliveryInputAnchor/DeliveryOutputAnchor passend zu den beschriebenen Ports, VfxAnchor über dem Funktionskern. ColliderRoot bleibt leer bis zur sichtbaren vereinfachten Collider-QA. Fahrzeuge, Förderbänder und Worker bleiben eigenständige spätere Prefabs.

T01 ist der verbindliche Formanker. T00 zeigt seine reparierbare Vorgeschichte; danach wird T02 aus T01 weiterentwickelt, nicht aus T00. Jede folgende Stufe nutzt T01 plus ihren direkten akzeptierten Vorgänger. Bei Gebäuden bleiben frühere Funktionsvolumen integriert und dürfen kompakt gestapelt werden; keine 24 unabhängigen Anbauten. Beim Anbau ersetzt jede Kategorie die vorherige vollständig und hält die vorgegebene exakte Modulzahl ein.

## Ausbaumerkmale

Nur T_FIXED: Repariertes Rathaus mit Berg-/Flammenkrone. Keine Ruine, keine Upgrade-Level. Das Showcase zeigt dieses eine feste Rathaus in der Testumgebung; T01/T12/T24 existieren für diese Familie nicht.

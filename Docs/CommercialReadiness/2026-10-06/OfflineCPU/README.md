Der abschließende Rechenstand ist `Simulation.optimized.cs` mit SHA-256 `60a04cbefcd9e543fbd7e32fa545d159c69bcb6e1dce41c10e2c290148eede60`. Die produktive `Amount.cs` bleibt unverändert bei `1e2d5bcce8465a7c1942504f2670144b83e85b1c109a5ab4cb67df7b6c19bd8d`. `legacy32-integration-manifest.json` pinnt den gezielten Abschluss-Patch gegen den zuvor integrierten Simulation-Stand `2734bf74...`; der eingefrorene Leistungs-Kontrollstand in `manifest.json` ist ausdrücklich das frühere `b26a6b64...` und wird nicht überschrieben.

Die exklusive Offline-Simulation bereitet Gebäude, Worker und Routen einmal vor. Lieferungsindizes bewahren die ursprüngliche Listenreihenfolge aller Reservierungssummen; Geld und Mengen werden nicht umgeordnet oder angenähert. Exakte Kapazitätskurven werden innerhalb des vorbereiteten Plans wiederverwendet. Öffentliche positive `Advance`-Aufrufe bereiten ihren Plan wie zuvor immer neu vor. Die Offline-Arbeit verwendet dieselben numerischen 32-Sekunden-Gruppen wie der vorherige tatsächliche UI-Start; Fortschritt und Abbruchprüfung erfolgen nach höchstens 128 Sekunden. Die gemeinsame Konstante ist `Simulation.OfflineTickGroupSeconds`.

Bei nichtbinären Restwerten waren die alten 32er- und 128er-Aufrufgruppen bereits verschieden: beispielsweise 0,1 wurde zu `0.10000000000000142` beziehungsweise `0.09999999999999432`. `boundary-before-fix.log` bewahrt diesen Befund, `boundary-after-fix.log` zeigt den nun vollständigen Feldgleichstand des Offline-Pfads mit Legacy32 für alle fünf geprüften Restwerte. Der Session-Komfortpfad `ApplyOffline()` und der WebGL-Pfad müssen die gemeinsame 32er-Konstante verwenden; explizite Iterator-Chunkparameter behalten ihre bestehende Semantik.

`validation.json` enthält sämtliche echten Messwerte, Quell- und Beleg-Hashes, Fallnamen und den getrennten Bezug zu historischen nativen Ergebnissen. Die finale Achtstunden-Gegenmessung ergibt:

| Laufzeit und Modell | Kontrolle | Kandidat | Weniger CPU-Zeit |
|---|---:|---:|---:|
| Gebündeltes Mono, 112 Gebäude / 118 Worker | 14,767 s | 11,993 s | 18,8 % |
| Gebündeltes Mono, zusätzlich 10.000 Inventar-Worker | 16,566 s | 12,077 s | 27,1 % |
| .NET, 112 Gebäude / 118 Worker | 2,614 s | 1,823 s | 30,3 % |
| .NET, zusätzlich 10.000 Inventar-Worker | 3,126 s | 1,767 s | 43,5 % |

Die maßgebliche isolierte Laufzeit für das Unity-CPU-Verhalten ist das gebündelte Mono. .NET-Werte sind durch JIT/Tiering beeinflussbar. Alle vier Gegenmessungen haben 28.800 Sekunden ausgeführt und jedes öffentliche Modellfeld einschließlich Float-/Doublebits sowie Feld-JSON innerhalb der jeweiligen Laufzeit exakt verglichen. Die Standard-Leistungsmodelle sind synthetische Domain-Modelle mit allen Enum-Attributen und keine durch SaveValidator zertifizierten vollständigen Spielstände. Der Inventarfall beschränkt die Attribute auf gültige Rollenattribute und ergänzt gültig geformte unzugewiesene Worker; auch für dessen gesamten synthetischen Graphen wird keine SaveValidator-Abnahme behauptet.

`regressions-legacy32-final-dotnet.log` und `regressions-legacy32-final-mono.log` enthalten jeweils 23 bestandene Fälle: 17 Offline-Domain-Fälle und sechs Zahlen-Grenzfälle. Die Domain-Fälle vergleichen alle Modellfelder für fünf Restwerte, Konstruktion, gewöhnliche und zwei präzise fraktionale UTC-Grenzen von VIP-Laufzeiten, Abbruch, ungültige Dauer, rekursive Offline-Aufrufe und Planbereinigung. Zusätzlich bestehen je Laufzeit 1.000.968 Konstruktorvergleiche und 280.000 Rechen-/Serialisierungsvergleiche mit exakten Bits und Exception-Arten/Parametern.

Der optionale Integer-Math.Pow-Cache in `Amount.cs` ist isoliert vollständig geprüft, bringt auf Mono nach den Transportänderungen jedoch nur ungefähr 1–2 % zusätzlich. Seine Übernahme wird nicht empfohlen. Historische Messungen dieses und früherer Kandidaten bleiben erhalten; `SourceSnapshots` bewahrt die vorherigen Hashstände und Abschluss-Belege. Die aktuelle Empfehlung enthält ausschließlich die Simulation-Änderung und die unveränderte produktive Amount-Implementierung.

Reproduktion außerhalb Unity:

```powershell
python .utmp/OfflineCpu20261006/compile.py
python .utmp/OfflineCpu20261006/compile_final_regressions.py
dotnet .utmp/OfflineCpu20261006/bin/final/net9.0/RegressionFinal.dll
& 'D:/Unity Games/6000.3.9f1/Editor/Data/MonoBleedingEdge/bin/mono.exe' .utmp/OfflineCpu20261006/RegressionFinalMono.exe
```

Die Benchmark-Gegenläufe in `run_final_benchmarks.py` werden nacheinander ausgeführt und bereits vorhandene bestandene Belege werden bewusst nicht neu gemessen. Diese Messung fand außerhalb des Unity Editors statt. Sie ersetzt weder native EditMode-/PlayMode-Tests noch den echten `GameRoot.ContinueResponsive`-Pfad, Kaltstart, GPU-/Gerätetiming oder menschliche Abnahme. Die historischen nativen 32,721 s Modellzeit und 50,314 s tatsächlicher ContinueResponsive-Start vom 2026-10-05 beziehen sich auf andere Hashstände. Der Parent führt die nativen Abschlussprüfungen für den produktiven Freeze aus.

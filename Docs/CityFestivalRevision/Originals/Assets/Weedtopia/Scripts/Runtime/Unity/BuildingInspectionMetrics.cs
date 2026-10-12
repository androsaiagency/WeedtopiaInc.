using System;
using System.Collections.Generic;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    public readonly struct InspectionMetric
    {
        public readonly string Label, Current, Next;
        public InspectionMetric(string label, string current, string next = null) { Label = label; Current = current; Next = next == current ? null : next; }
    }

    /// <summary>Read-only projection of the simulation formulas; never advances or edits a saved building.</summary>
    public static class BuildingInspectionMetrics
    {
        public static string FormatDuration(int seconds)
        {
            if (seconds <= 0) return "Sofort";
            if (seconds < 60) return seconds + " s";
            return seconds / 60 + " min" + (seconds % 60 == 0 ? "" : " " + seconds % 60 + " s");
        }
        public static List<InspectionMetric> Read(GameRoot root, BuildingState b)
        {
            var d = root.Definitions.Building(b.family); var workers = root.Session.Workers;
            int next = Math.Min(5000, b.level + 1); bool preview = next != b.level && b.buildingSeconds == 0;
            var rows = new List<InspectionMetric>();
            void Value(string label, Func<int, string> calculate) => rows.Add(new InspectionMetric(label, b.level == 0 ? "—" : calculate(b.level), preview ? calculate(next) : null));
            string Curve(Curve curve, int level, double bonus = 1) => (curve.At(level) * bonus).ToString();
            double Bonus(WorkerAttribute attr) => workers.Bonus(b, attr);
            if (d.kind == BuildingKind.Plot) { rows.Add(new InspectionMetric("Bauoptionen", "Haus / Park")); return rows; }
            if (d.kind == BuildingKind.TownHall) { rows.Add(new InspectionMetric("Einwohner", root.Session.Simulation.Residents(b.cityId).ToString())); rows.Add(new InspectionMetric("Aufgaben erledigt", root.Session.State.City.completedQuests.Count.ToString())); return rows; }
            if (d.kind == BuildingKind.House) Value("Einwohner", l => Math.Floor(d.baseResidents * (1 + VisualTiers.Index(l)) * Bonus(WorkerAttribute.Residents)).ToString());
            else if (d.kind == BuildingKind.Park) rows.Add(new InspectionMetric("Gemeinschaft / s", (new Amount(root.Session.Simulation.Residents(b.cityId)) * (Bonus(WorkerAttribute.CommunityIncome) - 1)).ToString()));
            else
            {
                Value("Pufferkapazität", l => Curve(d.capacity, l, Bonus(WorkerAttribute.Storage)));
                if (d.kind == BuildingKind.Grow)
                {
                    Value("Ertrag / Ernte", l => Curve(d.yield, l, Bonus(WorkerAttribute.Yield)));
                    rows.Add(new InspectionMetric("Erntezyklus", Math.Max(1, (int)Math.Ceiling(d.cycleSeconds / Bonus(WorkerAttribute.CycleSpeed))) + " s"));
                }
                else if (d.kind == BuildingKind.Market)
                    Value("Umsatz / s", l => Curve(d.rate, l, root.Session.Simulation.Residents(b.cityId) * Bonus(WorkerAttribute.Price) * Bonus(WorkerAttribute.Throughput) * Bonus(WorkerAttribute.CycleSpeed)));
                else Value(d.kind == BuildingKind.Shop ? "Verkauf / s" : "Durchsatz / s", l => Curve(d.rate, l, Bonus(WorkerAttribute.Yield) * Bonus(WorkerAttribute.Throughput) * Bonus(WorkerAttribute.CycleSpeed)));
                switch (d.kind)
                {
                    case BuildingKind.Grow: rows.Add(new InspectionMetric("Ernte im Puffer", b.raw.ToString())); break;
                    case BuildingKind.Processing: rows.Add(new InspectionMetric("Roh / Blüten", b.raw + " / " + b.refined)); break;
                    case BuildingKind.Butter: rows.Add(new InspectionMetric("Blüten / Butter", b.refined + " / " + b.butter)); break;
                    case BuildingKind.Bakery: rows.Add(new InspectionMetric("Butter / Backwaren", b.butter + " / " + b.pastry)); break;
                    case BuildingKind.Shop: case BuildingKind.Warehouse: rows.Add(new InspectionMetric("Blüten auf Lager", b.refined.ToString())); break;
                }
                if (d.kind == BuildingKind.Shop || d.kind == BuildingKind.Bakery) Value("Preis / Stück", l => Curve(d.price, l, Bonus(WorkerAttribute.Price)));
                if (d.kind == BuildingKind.Warehouse) Value("Fahrerplätze", l => VisualTiers.Index(l).ToString());
                if(d.kind == BuildingKind.Bakery)
                {
                    var route = root.Definitions.routes.FirstOrDefault(r => r.to == b.id && r.pickupByRecipient);
                    var baker = root.Session.State.workers.FirstOrDefault(w => w.buildingId == b.id && w.slot == 0);
                    if(route != null)
                    {
                        Value("Lieferwagen", l => (new Amount(route.capacity) * d.capacity.At(l) / d.capacity.At(1) * Bonus(WorkerAttribute.Storage) * Bonus(WorkerAttribute.Cargo)).ToString());
                        rows.Add(new InspectionMetric("Fahrt je Strecke", (baker == null ? route.travelSeconds : root.Session.Simulation.PickupTravelSeconds(route,baker)) + " s"));
                    }
                }
            }
            if (d.kind == BuildingKind.House || d.kind == BuildingKind.Park || d.kind == BuildingKind.Market)
                Value(d.kind == BuildingKind.House ? "Karten / Tag" : d.kind == BuildingKind.Market ? "Grinder / Tag" : "VIP-Aufträge / Tag", l => (VisualTiers.Index(l) + (int)Math.Floor(Bonus(WorkerAttribute.DailyBonus) - 1)).ToString());
            if (d.kind == BuildingKind.Park) Value("VIP-Prämie", l => (new Amount(40 * Math.Max(1, VisualTiers.Index(l))) * Bonus(WorkerAttribute.VipReward)).ToString());
            if(b.level<5000) rows.Add(new InspectionMetric(b.buildingSeconds > 0 ? "Restliche Bauzeit" : "Ausbau dauert",
                FormatDuration(b.buildingSeconds > 0 ? b.buildingSeconds : root.Session.Upgrades.Duration(b))));
            return rows;
        }
    }
}

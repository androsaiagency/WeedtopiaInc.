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
            if (seconds <= 0) return GameText.Get("presentation.buildinginspectionmetrics.sofort.02f195c0");
            if (seconds < 60) return seconds + " s";
            return seconds / 60 + GameText.Get("presentation.buildinginspectionmetrics.2e75737a6b58") + (seconds % 60 == 0 ? "" : " " + seconds % 60 + " s");
        }
        public static List<InspectionMetric> Read(GameRoot root, BuildingState b)
        {
            var d = root.Definitions.Building(b.family); var workers = root.Session.Workers;
            int next = Math.Min(5000, b.level + 1); bool preview = next != b.level && b.buildingSeconds == 0;
            var rows = new List<InspectionMetric>();
            void Value(string label, Func<int, string> calculate) => rows.Add(new InspectionMetric(label, b.level == 0 ? "—" : calculate(b.level), preview ? calculate(next) : null));
            string Curve(Curve curve, int level, double bonus = 1) => (curve.At(level) * bonus).ToString();
            double Bonus(WorkerAttribute attr) => workers.Bonus(b, attr);
            if (d.kind == BuildingKind.Plot) { rows.Add(new InspectionMetric(GameText.Get("presentation.buildinginspectionmetrics.bauoptionen.92021ca2"), GameText.Get("presentation.buildinginspectionmetrics.haus_park.e15deade"))); return rows; }
            if (d.kind == BuildingKind.TownHall)
            {
                rows.Add(new InspectionMetric(GameText.Get("project.goal.2.15"), root.Session.Simulation.Residents(b.cityId).ToString()));
                rows.Add(new InspectionMetric(GameText.Get("presentation.buildinginspectionmetrics.aufgaben_erledigt.1b01d8ab"), root.Session.State.City.completedQuests.Count.ToString()));
                var project = root.Session.CityProject;
                if (project.Completed) rows.Add(new InspectionMetric(root.Definitions.Text("project.city_title"), root.Definitions.Text("project.city_title_value")));
                if (project.Plan != null) rows.Add(new InspectionMetric(root.Definitions.Text("project.progress"), root.Session.State.City.project.stepIndex + " / " + project.Plan.steps.Length));
                return rows;
            }
            if (d.kind == BuildingKind.House) Value(GameText.Get("project.goal.2.15"), l => Math.Floor(d.baseResidents * (1 + VisualTiers.Index(l)) * Bonus(WorkerAttribute.Residents)).ToString());
            else if (d.kind == BuildingKind.Park) rows.Add(new InspectionMetric(GameText.Get("presentation.buildinginspectionmetrics.gemeinschaft_s.b8019700"), (new Amount(root.Session.Simulation.Residents(b.cityId)) * (Bonus(WorkerAttribute.CommunityIncome) - 1)).ToString()));
            else
            {
                Value(GameText.Get("presentation.buildinginspectionmetrics.pufferkapazit_t.7c39d7e4"), l => Curve(d.capacity, l, Bonus(WorkerAttribute.Storage)));
                if (d.kind == BuildingKind.Grow)
                {
                    Value(GameText.Get("presentation.buildinginspectionmetrics.ertrag_ernte.e46758e3"), l => Curve(d.yield, l, Bonus(WorkerAttribute.Yield)));
                    rows.Add(new InspectionMetric(GameText.Get("presentation.buildinginspectionmetrics.erntezyklus.53c3cf0d"), Math.Max(1, (int)Math.Ceiling(d.cycleSeconds / Bonus(WorkerAttribute.CycleSpeed))) + " s"));
                }
                else if (d.kind == BuildingKind.Market)
                    Value(GameText.Get("presentation.buildinginspectionmetrics.umsatz_s.22cb2d47"), l => Curve(d.rate, l, root.Session.Simulation.Residents(b.cityId) * Bonus(WorkerAttribute.Price) * Bonus(WorkerAttribute.Throughput) * Bonus(WorkerAttribute.CycleSpeed)));
                else Value(d.kind == BuildingKind.Shop ? GameText.Get("presentation.buildinginspectionmetrics.verkauf_s.ea54090f") : GameText.Get("presentation.buildinginspectionmetrics.durchsatz_s.03000ead"), l => Curve(d.rate, l, Bonus(WorkerAttribute.Yield) * Bonus(WorkerAttribute.Throughput) * Bonus(WorkerAttribute.CycleSpeed)));
                switch (d.kind)
                {
                    case BuildingKind.Grow: rows.Add(new InspectionMetric(GameText.Get("presentation.buildinginspectionmetrics.ernte_im_puffer.4dba81e5"), b.raw.ToString())); break;
                    case BuildingKind.Processing: rows.Add(new InspectionMetric(GameText.Get("presentation.buildinginspectionmetrics.roh_bl_ten.3a7bcbc7"), b.raw + " / " + b.refined)); break;
                    case BuildingKind.Butter: rows.Add(new InspectionMetric(GameText.Get("presentation.buildinginspectionmetrics.bl_ten_butter.7756d83d"), b.refined + " / " + b.butter)); break;
                    case BuildingKind.Bakery: rows.Add(new InspectionMetric(GameText.Get("presentation.buildinginspectionmetrics.butter_backwaren.04ac2cb5"), b.butter + " / " + b.pastry)); break;
                    case BuildingKind.Shop: case BuildingKind.Warehouse: rows.Add(new InspectionMetric(GameText.Get("presentation.buildinginspectionmetrics.bl_ten_auf_lager.d03a1e1a"), b.refined.ToString())); break;
                }
                if (d.kind == BuildingKind.Shop || d.kind == BuildingKind.Bakery)
                    Value(GameText.Get("presentation.buildinginspectionmetrics.preis_st_ck.6d4ae43e"), l => Curve(d.price, l, Bonus(WorkerAttribute.Price) * CityProjectRewards.SalesMultiplier(root.Definitions, root.Session.State.cities.Single(c => c.id == b.cityId), b.family)));
                if (d.kind == BuildingKind.Warehouse) Value(GameText.Get("presentation.buildinginspectionmetrics.fahrerpl_tze.96b81623"), l => VisualTiers.Index(l).ToString());
                if(d.kind == BuildingKind.Bakery)
                {
                    var route = root.Definitions.routes.FirstOrDefault(r => r.to == b.id && r.pickupByRecipient);
                    var baker = root.Session.State.workers.FirstOrDefault(w => w.buildingId == b.id && w.slot == 0);
                    if(route != null)
                    {
                        Value(GameText.Get("presentation.buildinginspectionmetrics.lieferwagen.5d60082a"), l => (new Amount(route.capacity) * d.capacity.At(l) / d.capacity.At(1) * Bonus(WorkerAttribute.Storage) * Bonus(WorkerAttribute.Cargo)).ToString());
                        rows.Add(new InspectionMetric(GameText.Get("presentation.buildinginspectionmetrics.fahrt_je_strecke.77513de1"), (baker == null ? route.travelSeconds : root.Session.Simulation.PickupTravelSeconds(route,baker)) + " s"));
                    }
                }
            }
            if (d.kind == BuildingKind.House || d.kind == BuildingKind.Park || d.kind == BuildingKind.Market)
                Value(d.kind == BuildingKind.House ? GameText.Get("presentation.buildinginspectionmetrics.karten_tag.6223b323") : d.kind == BuildingKind.Market ? GameText.Get("presentation.buildinginspectionmetrics.grinder_tag.c8912970") : GameText.Get("presentation.buildinginspectionmetrics.vip_auftr_ge_tag.df6f396f"), l => (VisualTiers.Index(l) + (int)Math.Floor(Bonus(WorkerAttribute.DailyBonus) - 1)).ToString());
            if (d.kind == BuildingKind.Park) Value(GameText.Get("attribute.12"), l => (new Amount(40 * Math.Max(1, VisualTiers.Index(l))) * Bonus(WorkerAttribute.VipReward)).ToString());
            if(b.level<5000) rows.Add(new InspectionMetric(b.buildingSeconds > 0 ? GameText.Get("presentation.buildinginspectionmetrics.restliche_bauzeit.39874d48") : GameText.Get("presentation.buildinginspectionmetrics.ausbau_dauert.c09bbd41"),
                FormatDuration(b.buildingSeconds > 0 ? b.buildingSeconds : root.Session.Upgrades.Duration(b))));
            return rows;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Weedtopia.Domain
{
    /// <summary>Fixed one-second deterministic pipeline. No visual objects or wall clock calls.</summary>
    public sealed class ReferenceSimulation
    {
        private readonly GameDefinitions defs;
        private readonly GameState state;
        private readonly Dictionary<string, RouteDefinition> routes;
        private readonly RouteDefinition[] pickupRoutes;
        private readonly HashSet<string> pickupRecipients;
        private readonly Dictionary<string, string> routeCities;
        private readonly HashSet<string> runningCities = new HashSet<string>();
        private long? evaluationUtcTicks;
        private long EvaluationTime => evaluationUtcTicks ?? state.highWaterUtcTicks;
        public WorkerService Workers { get; }
        public ReferenceSimulation(GameDefinitions defs, GameState state)
        {
            this.defs = defs; this.state = state; Workers = new WorkerService(defs, state);
            routes = defs.routes.ToDictionary(r => r.id); pickupRoutes = defs.routes.Where(r => r.pickupByRecipient).ToArray();
            pickupRecipients = new HashSet<string>(pickupRoutes.Select(r => r.to));
            var instances = defs.instances.ToDictionary(i => i.id);
            routeCities = defs.routes.ToDictionary(r => r.id, r => instances[r.from].cityId);
        }
        private bool Collecting(BuildingState b) => pickupRecipients.Contains(b.id) && state.deliveries.Any(t =>
            routes[t.route].pickupByRecipient && routes[t.route].to == b.id && (t.fetching || t.remainingSeconds > 0));
        public Amount Capacity(BuildingState b) => defs.Building(b.family).capacity.At(Math.Max(1, b.level)) * Workers.Bonus(b, WorkerAttribute.Storage);
        public Amount Rate(BuildingState b) => defs.Building(b.family).rate.At(b.level) * Workers.Bonus(b, WorkerAttribute.Throughput) * Workers.Bonus(b, WorkerAttribute.Yield) * ShopRules.ProductionMultiplier(state, EvaluationTime);
        public int Residents(string city) => state.buildings.Where(x => x.cityId == city && x.level > 0 && defs.Building(x.family).kind == BuildingKind.House)
            .Sum(x => (int)Math.Floor(defs.Building(x.family).baseResidents * (1 + VisualTiers.Index(x.level)) * Workers.Bonus(x, WorkerAttribute.Residents)));
        public Failure Work(string id)
        {
            var b = state.buildings.FirstOrDefault(x => x.id == id);
            if (b == null || b.level < 1 || b.cityId != state.activeCity) return Failure.Invalid;
            if (defs.Building(b.family).zone > state.City.unlocked) return Failure.Locked;
            if (Collecting(b)) return Failure.Busy;
            b.workSeconds = 30; state.revision++; return Failure.None;
        }
        public Failure Harvest(string id)
        {
            var b = state.buildings.FirstOrDefault(x => x.id == id);
            if (b == null || b.cityId != state.activeCity || defs.Building(b.family).kind != BuildingKind.Grow || b.raw <= 0) return Failure.NotReady;
            // A manual harvest releases crop to the existing belt; if the target is blocked it stays here.
            b.workSeconds = 30; state.cities.Single(x => x.id == b.cityId).stats.manualHarvests++; state.revision++;
            return Failure.None;
        }
        public void Advance(double seconds, long? startUtcTicks = null)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > 604800) throw new ArgumentOutOfRangeException(nameof(seconds));
            double previousRemainder = state.tickRemainder;
            state.tickRemainder += seconds;
            int ticks = (int)Math.Floor(state.tickRemainder + 1e-9);
            state.tickRemainder = Math.Max(0, state.tickRemainder - ticks);
            try
            {
                for (int t = 0; t < ticks; t++)
                {
                    evaluationUtcTicks = startUtcTicks.HasValue ? Math.Max(0, startUtcTicks.Value + (long)((t - previousRemainder) * TimeSpan.TicksPerSecond)) : (long?)null;
                    Tick();
                }
            }
            finally { evaluationUtcTicks = null; }
        }
        private void Tick()
        {
            state.simulationSeconds++;
            runningCities.Clear();
            foreach (var city in state.cities.Where(c => CityCampaignRules.Accessible(defs, state, c.id)))
            { runningCities.Add(city.id); city.stats.playSeconds++; }
            foreach (var b in state.buildings.Where(b => runningCities.Contains(b.cityId)))
            {
                b.bottleneck = null;
                ConstructionProgress.Reduce(state, b, 1);
            }
            ReceiveDeliveries();
            // Consume downstream before refilling. Input cannot cross multiple processing steps in one tick.
            foreach (var b in state.buildings.Where(x => runningCities.Contains(x.cityId)).OrderByDescending(x => defs.Building(x.family).zone).ThenBy(x => x.id)) Produce(b);
            foreach (var route in defs.routes.Where(x => !x.vehicle && runningCities.Contains(routeCities[x.id]))) Belt(route);
            DispatchVehicles();
            DispatchPickups();
            foreach (var b in state.buildings.Where(x => runningCities.Contains(x.cityId))) if (b.workSeconds > 0) b.workSeconds--;
        }
        private bool Running(BuildingState b) => Workers.Automated(b) || b.workSeconds > 0;
        private void Produce(BuildingState b)
        {
            var d = defs.Building(b.family);
            var city = state.cities.Single(x => x.id == b.cityId);
            if (b.level < 1 || d.zone > city.unlocked) return;
            var stats = city.stats;
            if (d.kind == BuildingKind.Grow)
            {
                int cycle = Math.Max(1, (int)Math.Ceiling(d.cycleSeconds / Workers.Bonus(b, WorkerAttribute.CycleSpeed) / ShopRules.ProductionMultiplier(state, EvaluationTime)));
                b.cycleProgress = Math.Min(cycle, b.cycleProgress + 1);
                if (b.cycleProgress >= cycle)
                {
                    Amount batch = d.yield.At(b.level) * Workers.Bonus(b, WorkerAttribute.Yield);
                    if (Capacity(b) - b.Total >= batch) { b.raw += batch; stats.rawProduced += batch; b.cycleProgress = 0; }
                    else b.bottleneck = "bottleneck.harvest";
                }
                return;
            }
            if (!Running(b)) { b.bottleneck = "bottleneck.manager"; return; }
            if (Collecting(b))
            { b.bottleneck = "bottleneck.pickup"; return; }
            Amount rate = Rate(b) * Workers.Bonus(b, WorkerAttribute.CycleSpeed);
            switch (d.kind)
            {
                case BuildingKind.Processing: stats.refinedProduced += Convert(b, Resource.Raw, Resource.Refined, rate, 1); break;
                case BuildingKind.Butter: stats.butterProduced += Convert(b, Resource.Refined, Resource.Butter, rate, 2); break;
                case BuildingKind.Bakery:
                    stats.pastryProduced += Convert(b, Resource.Butter, Resource.Pastry, rate, 1);
                    Amount sold = Sell(b, Resource.Pastry, rate); stats.pastrySold += sold; break;
                case BuildingKind.Shop: stats.coffeeSold += Sell(b, Resource.Refined, rate); break;
                case BuildingKind.Market: Earn(city, d.rate.At(b.level) * Residents(b.cityId) * Workers.Bonus(b, WorkerAttribute.Price) * Workers.Bonus(b, WorkerAttribute.Throughput) * Workers.Bonus(b, WorkerAttribute.CycleSpeed)); break;
                case BuildingKind.Park: Earn(city, new Amount(Residents(b.cityId)) * (Workers.Bonus(b, WorkerAttribute.CommunityIncome) - 1)); break;
            }
        }
        private Amount Convert(BuildingState b, Resource input, Resource output, Amount rate, int ratio)
        {
            // Capacity applies per resource buffer. Conversion never discards input or output.
            Amount available = b.Get(input);
            Amount outputLimit = Amount.Min(rate, Amount.Max(0, Capacity(b) - b.Get(output)));
            // Amount rounds to 12 significant digits: (available / ratio) * ratio can exceed
            // available. Bound consumption in input units before deriving the rounded output.
            Amount consumed = Amount.Min(available, outputLimit * ratio);
            Amount amount = Amount.Min(outputLimit, consumed / ratio);
            b.Set(input, available - consumed); b.Set(output, b.Get(output) + amount);
            if (b.Get(output) >= Capacity(b)) b.bottleneck = "bottleneck.output";
            else if (b.Get(input) >= rate * ratio) b.bottleneck = "bottleneck.throughput";
            else if (amount == 0) b.bottleneck = "bottleneck.input";
            return amount;
        }
        private Amount Sell(BuildingState b, Resource resource, Amount rate)
        {
            Amount amount = Amount.Min(b.Get(resource), rate); b.Set(resource, b.Get(resource) - amount);
            var d = defs.Building(b.family);
            var city = state.cities.Single(x => x.id == b.cityId);
            Earn(city, amount * d.price.At(b.level) * Workers.Bonus(b, WorkerAttribute.Price) * CityProjectRewards.SalesMultiplier(defs, city, b.family)
                * (resource == Resource.Refined ? ShopRules.FlowerPriceMultiplier(state, EvaluationTime) : 1));
            if (amount == 0) b.bottleneck = "bottleneck.input";
            return amount;
        }
        private void Earn(CityState city, Amount money) { state.money += money; city.stats.earned += money; }
        private void Belt(RouteDefinition route)
        {
            var from = state.Find(route.from); var to = state.Find(route.to);
            if (from.level < 1 || to.level < 1 || !Running(from)) return;
            Amount moved = Amount.Min(from.Get(route.resource), Amount.Min(Rate(from) * Workers.Bonus(from, WorkerAttribute.CycleSpeed), Amount.Max(0, Capacity(to) - to.Get(route.resource))));
            from.Set(route.resource, from.Get(route.resource) - moved); to.Set(route.resource, to.Get(route.resource) + moved);
            if (moved > 0) ConveyorTransferred?.Invoke(route.id);
            if (from.Get(route.resource) > 0 && moved == 0) from.bottleneck = "bottleneck.output";
        }
        /// <summary>Presentation-only notification after a completed transfer; never serialized into the save.</summary>
        public event System.Action<string> ConveyorTransferred;
        private void ReceiveDeliveries()
        {
            for (int i = state.deliveries.Count - 1; i >= 0; i--)
            {
                var delivery = state.deliveries[i]; if (delivery.remainingSeconds > 0) delivery.remainingSeconds--;
                if (delivery.remainingSeconds > 0) continue;
                var route = routes[delivery.route]; var to = state.Find(route.to);
                if (delivery.fetching)
                {
                    var from = state.Find(route.from); var worker = state.workers.Single(w => w.id == delivery.workerId);
                    Amount available = Amount.Max(0, from.Get(route.resource) - PickupReserved(route.from, route.resource, delivery));
                    Amount room = Amount.Max(0, Capacity(to) - to.Get(route.resource) - IncomingReserved(route.to, route.resource, delivery));
                    delivery.cargo = Amount.Min(available, Amount.Min(PickupCapacity(route, to, worker), room));
                    from.Set(route.resource, from.Get(route.resource) - delivery.cargo);
                    delivery.reservedCargo = 0; delivery.fetching = false;
                    delivery.remainingSeconds = delivery.totalSeconds;
                    continue;
                }
                bool carriedCargo = delivery.cargo > 0;
                Amount unloaded = Amount.Min(delivery.cargo, Amount.Max(0, Capacity(to) - to.Get(route.resource)));
                to.Set(route.resource, to.Get(route.resource) + unloaded); delivery.cargo -= unloaded;
                var stats = state.cities.Single(x => x.id == to.cityId).stats; stats.delivered += unloaded;
                if (delivery.cargo == 0) { if(carriedCargo)stats.deliveries++; state.deliveries.RemoveAt(i); }
                else to.bottleneck = "bottleneck.storage";
            }
        }
        private void DispatchVehicles()
        {
            foreach (var from in state.buildings.Where(x => runningCities.Contains(x.cityId) && x.level > 0 && !string.IsNullOrEmpty(defs.Building(x.family).driverRole)))
            {
                if (!Running(from)) continue;
                var routes = defs.routes.Where(x => x.from == from.id && x.vehicle && !x.pickupByRecipient &&
                    (state.Find(x.to).level > 0 || defs.Building(state.Find(x.to).family).kind == BuildingKind.Warehouse)).ToArray();
                if (routes.Length == 0) continue;
                foreach (var w in state.workers.Where(x => x.buildingId == from.id && x.slot > 0).OrderBy(x => x.slot))
                {
                    if (state.deliveries.Any(x => x.workerId == w.id)) continue;
                    for (int n = 0; n < routes.Length; n++)
                    {
                        var route = routes[(from.routeCursor + n) % routes.Length]; var to = state.Find(route.to);
                        Amount reserved = IncomingReserved(to.id, route.resource);
                        Amount room = Amount.Max(0, Capacity(to) - to.Get(route.resource) - reserved);
                        Amount capacity = new Amount(route.capacity) * defs.Building(from.family).capacity.At(from.level) / defs.Building(from.family).capacity.At(1) * (1 + Workers.Effect(w, WorkerAttribute.Cargo)) * (1 + Workers.Effect(w, WorkerAttribute.Storage));
                        Amount amount = Amount.Min(from.Get(route.resource), Amount.Min(capacity, room));
                        if (amount <= 0) continue;
                        int time = Math.Max(1, (int)Math.Ceiling(route.travelSeconds / (1 + Workers.Effect(w, WorkerAttribute.TravelSpeed)) / (1 + Workers.Effect(w, WorkerAttribute.CycleSpeed))));
                        amount = Amount.Min(amount, Rate(from) * Workers.Bonus(from, WorkerAttribute.CycleSpeed) * (1 + Workers.Effect(w, WorkerAttribute.Throughput)));
                        from.Set(route.resource, from.Get(route.resource) - amount);
                        state.deliveries.Add(new DeliveryState { route = route.id, workerId = w.id, cargo = amount, remainingSeconds = time, totalSeconds = time });
                        from.routeCursor = (from.routeCursor + n + 1) % routes.Length; break;
                    }
                }
                if (from.Total > 0 && !state.workers.Any(x => x.buildingId == from.id && x.slot > 0)) from.bottleneck = "bottleneck.driver";
            }
        }
        private Amount IncomingReserved(string recipient, Resource resource, DeliveryState except = null) =>
            state.deliveries.Where(t => t != except && routes[t.route].to == recipient && routes[t.route].resource == resource)
                .Aggregate(Amount.Zero, (a, t) => a + (t.fetching ? t.reservedCargo : t.cargo));
        private Amount PickupReserved(string source, Resource resource, DeliveryState except = null) =>
            state.deliveries.Where(t => t != except && t.fetching && routes[t.route].from == source && routes[t.route].resource == resource)
                .Aggregate(Amount.Zero, (a, t) => a + t.reservedCargo);
        public Amount PickupCapacity(RouteDefinition route, BuildingState recipient, WorkerState worker) =>
            new Amount(route.capacity) * defs.Building(recipient.family).capacity.At(Math.Max(1, recipient.level)) / defs.Building(recipient.family).capacity.At(1)
            * (1 + Workers.Effect(worker, WorkerAttribute.Cargo)) * (1 + Workers.Effect(worker, WorkerAttribute.Storage));
        public int PickupTravelSeconds(RouteDefinition route, WorkerState worker) =>
            Math.Max(1, (int)Math.Ceiling(route.travelSeconds / (1 + Workers.Effect(worker, WorkerAttribute.TravelSpeed)) / (1 + Workers.Effect(worker, WorkerAttribute.CycleSpeed))));
        private void DispatchPickups()
        {
            foreach (var route in pickupRoutes.Where(x => runningCities.Contains(routeCities[x.id])))
            {
                var from = state.Find(route.from); var to = state.Find(route.to);
                if (from.level < 1 || to.level < 1 || defs.Building(to.family).zone > state.cities.Single(c => c.id == to.cityId).unlocked) continue;
                var worker = state.workers.FirstOrDefault(w => w.buildingId == to.id && w.slot == 0);
                if (worker == null || state.deliveries.Any(t => t.workerId == worker.id)) continue;
                // Use the existing stock first; do not send a van for every unit consumed.
                if (to.Get(route.resource) > Capacity(to) * .25) continue;
                Amount available = Amount.Max(0, from.Get(route.resource) - PickupReserved(from.id, route.resource));
                Amount room = Amount.Max(0, Capacity(to) - to.Get(route.resource) - IncomingReserved(to.id, route.resource));
                Amount reserve = Amount.Min(available, Amount.Min(PickupCapacity(route, to, worker), room));
                if (reserve <= 0) continue;
                int time = PickupTravelSeconds(route, worker);
                state.deliveries.Add(new DeliveryState { route = route.id, workerId = worker.id, fetching = true,
                    reservedCargo = reserve, cargo = 0, remainingSeconds = time, totalSeconds = time });
                to.bottleneck = "bottleneck.pickup";
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Weedtopia.Domain
{
    /// <summary>Fixed one-second deterministic pipeline. No visual objects or wall clock calls.</summary>
    public sealed class Simulation
    {
        public const int OfflineTickGroupSeconds = 32;
        private readonly GameDefinitions defs;
        private readonly GameState state;
        private readonly Dictionary<string, RouteDefinition> routes;
        private readonly RouteDefinition[] pickupRoutes;
        private readonly HashSet<string> pickupRecipients;
        private readonly Dictionary<string, string> routeCities;
        private readonly HashSet<string> runningCities = new HashSet<string>();
        private long? evaluationUtcTicks;
        // Rebuilt for each batch, so purchases, assignments and loaded saves cannot leave stale values.
        private Dictionary<string, BuildingState> batchBuildings;
        private Dictionary<string, CityState> batchCities;
        private Dictionary<string, BuildingDefinition> batchDefinitions;
        private Dictionary<string, BuildingPlan> batchPlans;
        private Dictionary<string, WorkerState> batchWorkers;
        private BuildingState[] productionOrder;
        private RouteDefinition[] beltRoutes;
        private Dictionary<string, RouteDefinition[]> vehicleRoutes;
        private readonly Dictionary<string, RouteDefinition[]> eligibleVehicleRoutes = new Dictionary<string, RouteDefinition[]>();
        private Dictionary<string, WorkerState> batchManagers;
        private Dictionary<string, int> deliveryCounts;
        private Dictionary<ReservationKey, List<DeliveryState>> incomingDeliveries, pickupDeliveries;
        private readonly struct ReservationKey : IEquatable<ReservationKey>
        {
            private readonly string building;
            private readonly Resource resource;
            public ReservationKey(string building, Resource resource) { this.building = building; this.resource = resource; }
            public bool Equals(ReservationKey other) => building == other.building && resource == other.resource;
            public override bool Equals(object value) => value is ReservationKey other && Equals(other);
            public override int GetHashCode() => (building == null ? 0 : building.GetHashCode()) ^ (int)resource;
        }
        private void PrepareDeliveryIndexes()
        {
            deliveryCounts = new Dictionary<string, int>();
            incomingDeliveries = new Dictionary<ReservationKey, List<DeliveryState>>();
            pickupDeliveries = new Dictionary<ReservationKey, List<DeliveryState>>();
            // List order stays identical to the save's delivery order; Amount addition is never reassociated.
            foreach (var delivery in state.deliveries) TrackDelivery(delivery, routes[delivery.route]);
        }
        private static void AddIndexed(Dictionary<ReservationKey, List<DeliveryState>> index, ReservationKey key, DeliveryState delivery)
        {
            if (!index.TryGetValue(key, out var entries)) index.Add(key, entries = new List<DeliveryState>());
            entries.Add(delivery);
        }
        private void TrackDelivery(DeliveryState delivery, RouteDefinition route)
        {
            if (delivery.workerId != null)
            { deliveryCounts.TryGetValue(delivery.workerId, out int count); deliveryCounts[delivery.workerId] = count + 1; }
            AddIndexed(incomingDeliveries, new ReservationKey(route.to, route.resource), delivery);
            AddIndexed(pickupDeliveries, new ReservationKey(route.from, route.resource), delivery);
        }
        private void RemoveDeliveryIndexes(DeliveryState delivery, RouteDefinition route)
        {
            if (delivery.workerId != null)
            {
                int count = deliveryCounts[delivery.workerId];
                if (count == 1) deliveryCounts.Remove(delivery.workerId); else deliveryCounts[delivery.workerId] = count - 1;
            }
            incomingDeliveries[new ReservationKey(route.to, route.resource)].Remove(delivery);
            pickupDeliveries[new ReservationKey(route.from, route.resource)].Remove(delivery);
        }
        private void AddDelivery(DeliveryState delivery)
        { state.deliveries.Add(delivery); TrackDelivery(delivery, routes[delivery.route]); }
        private bool HasDelivery(string worker) => worker == null ? state.deliveries.Any(delivery => delivery.workerId == null) : deliveryCounts.ContainsKey(worker);
        private readonly Dictionary<string, int> residentCounts = new Dictionary<string, int>();
        private sealed class BuildingPlan
        {
            public int Level;
            public double[] Bonuses;
            public bool Automated;
            public WorkerState[] Drivers;
            public Amount Capacity, BaseRate, Yield, Price, LevelCapacity, FirstCapacity;
            public Amount ScaledRate, VipRate, ProductionRate, PassiveIncome;
            public double ProductionMultiplier = double.NaN;
            public int GrowCycle, PassiveResidents = -1;
        }
        private long EvaluationTime => evaluationUtcTicks ?? state.highWaterUtcTicks;
        public WorkerService Workers { get; }
        public Simulation(GameDefinitions defs, GameState state)
        {
            this.defs = defs; this.state = state; Workers = new WorkerService(defs, state);
            routes = defs.routes.ToDictionary(r => r.id); pickupRoutes = defs.routes.Where(r => r.pickupByRecipient).ToArray();
            pickupRecipients = new HashSet<string>(pickupRoutes.Select(r => r.to));
            var instances = defs.instances.ToDictionary(i => i.id);
            routeCities = defs.routes.ToDictionary(r => r.id, r => instances[r.from].cityId);
        }
        private bool Collecting(BuildingState b) => pickupRecipients.Contains(b.id) && state.deliveries.Any(t =>
            routes[t.route].pickupByRecipient && routes[t.route].to == b.id && (t.fetching || t.remainingSeconds > 0));
        private BuildingDefinition Definition(BuildingState b) => batchDefinitions == null ? defs.Building(b.family) : batchDefinitions[b.family];
        private BuildingState Find(string id) => batchBuildings == null ? state.Find(id) : batchBuildings[id];
        private CityState City(string id) => batchCities == null ? state.cities.Single(x => x.id == id) : batchCities[id];
        private double Bonus(BuildingState b, WorkerAttribute attribute) => batchPlans == null ? Workers.Bonus(b, attribute) : Plan(b).Bonuses[(int)attribute];
        private BuildingPlan Plan(BuildingState b)
        {
            var plan = batchPlans[b.id];
            if (plan.Level != b.level) RefreshPlan(b, plan);
            return plan;
        }
        private void RefreshPlan(BuildingState b, BuildingPlan plan)
        {
            var d = Definition(b);
            plan.Level = b.level;
            plan.LevelCapacity = d.capacity.At(Math.Max(1, b.level));
            plan.FirstCapacity = d.capacity.At(1);
            plan.Capacity = plan.LevelCapacity * plan.Bonuses[(int)WorkerAttribute.Storage];
            plan.BaseRate = d.rate.At(b.level);
            plan.ScaledRate = plan.BaseRate * plan.Bonuses[(int)WorkerAttribute.Throughput] * plan.Bonuses[(int)WorkerAttribute.Yield];
            plan.Yield = d.yield.At(b.level) * plan.Bonuses[(int)WorkerAttribute.Yield];
            plan.Price = d.price.At(b.level);
            plan.ProductionMultiplier = double.NaN; plan.PassiveResidents = -1;
        }
        private BuildingPlan ProductionPlan(BuildingState b)
        {
            var plan = Plan(b);
            double multiplier = ShopRules.ProductionMultiplier(state, EvaluationTime);
            if (plan.ProductionMultiplier != multiplier)
            {
                plan.ProductionMultiplier = multiplier;
                plan.VipRate = plan.ScaledRate * multiplier;
                plan.ProductionRate = plan.VipRate * plan.Bonuses[(int)WorkerAttribute.CycleSpeed];
                plan.GrowCycle = Math.Max(1, (int)Math.Ceiling(Definition(b).cycleSeconds / plan.Bonuses[(int)WorkerAttribute.CycleSpeed] / multiplier));
            }
            return plan;
        }
        private Amount ProductionRate(BuildingState b) => ProductionPlan(b).ProductionRate;
        private Amount PassiveIncome(BuildingState b)
        {
            var plan = Plan(b); int residents = Residents(b.cityId);
            if (plan.PassiveResidents != residents)
            {
                plan.PassiveResidents = residents;
                plan.PassiveIncome = Definition(b).kind == BuildingKind.Market
                    ? plan.BaseRate * residents * Bonus(b, WorkerAttribute.Price) * Bonus(b, WorkerAttribute.Throughput) * Bonus(b, WorkerAttribute.CycleSpeed)
                    : new Amount(residents) * (Bonus(b, WorkerAttribute.CommunityIncome) - 1);
            }
            return plan.PassiveIncome;
        }
        private void PrepareBatch()
        {
            batchBuildings = state.buildings.ToDictionary(b => b.id);
            batchCities = state.cities.ToDictionary(c => c.id);
            batchDefinitions = defs.buildings.ToDictionary(d => d.id);
            batchWorkers = state.workers.ToDictionary(w => w.id);
            batchManagers = state.workers.Where(w => w.slot == 0 && !string.IsNullOrEmpty(w.buildingId)).GroupBy(w => w.buildingId).ToDictionary(g => g.Key, g => g.First());
            batchPlans = new Dictionary<string, BuildingPlan>();
            var assignments = state.workers.ToLookup(w => w.buildingId ?? "");
            foreach (var b in state.buildings)
            {
                var assigned = assignments[b.id].ToArray();
                var managers = assigned.Where(w => w.slot == 0).ToArray();
                var plan = new BuildingPlan { Bonuses = new double[Enum.GetValues(typeof(WorkerAttribute)).Length],
                    Automated = managers.Length > 0, Drivers = assigned.Where(w => w.slot > 0).OrderBy(w => w.slot).ToArray() };
                for (int i = 0; i < plan.Bonuses.Length; i++) plan.Bonuses[i] = 1 + managers.Sum(w => Workers.Effect(w, (WorkerAttribute)i));
                batchPlans.Add(b.id, plan);
                RefreshPlan(b, plan);
            }
            productionOrder = state.buildings.OrderByDescending(b => Definition(b).zone).ThenBy(b => b.id).ToArray();
            beltRoutes = defs.routes.Where(r => !r.vehicle).ToArray();
            vehicleRoutes = defs.routes.Where(r => r.vehicle && !r.pickupByRecipient).GroupBy(r => r.from).ToDictionary(g => g.Key, g => g.ToArray());
            residentCounts.Clear();
            eligibleVehicleRoutes.Clear();
            PrepareDeliveryIndexes();
        }
        private RouteDefinition[] EligibleVehicleRoutes(BuildingState from)
        {
            if (eligibleVehicleRoutes.TryGetValue(from.id, out var eligible)) return eligible;
            if (!vehicleRoutes.TryGetValue(from.id, out var candidates)) candidates = Array.Empty<RouteDefinition>();
            eligible = candidates.Where(r => Find(r.to).level > 0 || Definition(Find(r.to)).kind == BuildingKind.Warehouse).ToArray();
            eligibleVehicleRoutes[from.id] = eligible;
            return eligible;
        }
        private void ClearBatch()
        {
            batchBuildings = null; batchCities = null; batchDefinitions = null; batchPlans = null; batchWorkers = null;
            productionOrder = null; beltRoutes = null; vehicleRoutes = null; batchManagers = null; residentCounts.Clear(); eligibleVehicleRoutes.Clear();
            deliveryCounts = null; incomingDeliveries = null; pickupDeliveries = null;
        }
        public Amount Capacity(BuildingState b) => batchPlans == null ? Definition(b).capacity.At(Math.Max(1, b.level)) * Bonus(b, WorkerAttribute.Storage) : Plan(b).Capacity;
        public Amount Rate(BuildingState b) => batchPlans == null
            ? Definition(b).rate.At(b.level) * Bonus(b, WorkerAttribute.Throughput) * Bonus(b, WorkerAttribute.Yield) * ShopRules.ProductionMultiplier(state, EvaluationTime)
            : ProductionPlan(b).VipRate;
        public int Residents(string city)
        {
            if (batchPlans != null && residentCounts.TryGetValue(city, out int cached)) return cached;
            int count = state.buildings.Where(x => x.cityId == city && x.level > 0 && Definition(x).kind == BuildingKind.House)
                .Sum(x => (int)Math.Floor(Definition(x).ResidentCapacity(x.level) * Bonus(x, WorkerAttribute.Residents)));
            if (batchPlans != null) residentCounts[city] = count;
            return count;
        }
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
        public void Advance(double seconds, long? startUtcTicks = null) => AdvanceCore(seconds, startUtcTicks, false);

        /// <summary>Exclusive private offline model work. Retains a plan for this call only; callbacks report completed segments and must not mutate the model.</summary>
        public void AdvanceOffline(int seconds, long startUtcTicks, CancellationToken cancellation, Action<int> completed)
        {
            if (seconds < 0 || seconds > 604800) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (batchPlans != null) throw new InvalidOperationException("An advance batch is already active.");
            cancellation.ThrowIfCancellationRequested();
            try
            {
                int simulated = 0;
                while (simulated < seconds)
                {
                    cancellation.ThrowIfCancellationRequested();
                    int reportAt = Math.Min(simulated + 128, seconds);
                    // Preserve the previous ContinueResponsive 32-second remainder/UTC rounding exactly.
                    // The prepared plan stays alive; these numerical groups do not yield or rebuild it.
                    while (simulated < reportAt)
                    {
                        int segment = Math.Min(OfflineTickGroupSeconds, reportAt - simulated);
                        AdvanceCore(segment, startUtcTicks + simulated * TimeSpan.TicksPerSecond, true);
                        simulated += segment;
                    }
                    completed?.Invoke(simulated);
                }
            }
            finally { evaluationUtcTicks = null; ClearBatch(); }
        }

        private void AdvanceCore(double seconds, long? startUtcTicks, bool retainPreparedBatch)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > 604800) throw new ArgumentOutOfRangeException(nameof(seconds));
            double previousRemainder = state.tickRemainder;
            state.tickRemainder += seconds;
            int ticks = (int)Math.Floor(state.tickRemainder + 1e-9);
            state.tickRemainder = Math.Max(0, state.tickRemainder - ticks);
            try
            {
                if (ticks > 0 && (!retainPreparedBatch || batchPlans == null)) PrepareBatch();
                for (int t = 0; t < ticks; t++)
                {
                    evaluationUtcTicks = startUtcTicks.HasValue ? Math.Max(0, startUtcTicks.Value + (long)((t - previousRemainder) * TimeSpan.TicksPerSecond)) : (long?)null;
                    Tick();
                }
            }
            finally { evaluationUtcTicks = null; if (!retainPreparedBatch) ClearBatch(); }
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
                int level = b.level;
                ConstructionProgress.Reduce(state, b, 1);
                if (level != b.level) { RefreshPlan(b, batchPlans[b.id]); residentCounts.Remove(b.cityId); eligibleVehicleRoutes.Clear(); }
            }
            ReceiveDeliveries();
            // Consume downstream before refilling. Input cannot cross multiple processing steps in one tick.
            foreach (var b in productionOrder) if (runningCities.Contains(b.cityId)) Produce(b);
            foreach (var route in beltRoutes) if (runningCities.Contains(routeCities[route.id])) Belt(route);
            DispatchVehicles();
            DispatchPickups();
            foreach (var b in state.buildings.Where(x => runningCities.Contains(x.cityId))) if (b.workSeconds > 0) b.workSeconds--;
        }
        private bool Running(BuildingState b) => (batchPlans == null ? Workers.Automated(b) : Plan(b).Automated) || b.workSeconds > 0;
        private void Produce(BuildingState b)
        {
            var d = Definition(b);
            var city = City(b.cityId);
            if (b.level < 1 || d.zone > city.unlocked) return;
            var stats = city.stats;
            if (d.kind == BuildingKind.Grow)
            {
                int cycle = ProductionPlan(b).GrowCycle;
                b.cycleProgress = Math.Min(cycle, b.cycleProgress + 1);
                if (b.cycleProgress >= cycle)
                {
                    Amount batch = Plan(b).Yield;
                    if (Capacity(b) - b.Total >= batch) { b.raw += batch; stats.rawProduced += batch; b.cycleProgress = 0; }
                    else b.bottleneck = "bottleneck.harvest";
                }
                return;
            }
            if (!Running(b)) { b.bottleneck = "bottleneck.manager"; return; }
            if (Collecting(b))
            { b.bottleneck = "bottleneck.pickup"; return; }
            Amount rate = ProductionRate(b);
            switch (d.kind)
            {
                case BuildingKind.Processing: stats.refinedProduced += Convert(b, Resource.Raw, Resource.Refined, rate, 1); break;
                case BuildingKind.Butter: stats.butterProduced += Convert(b, Resource.Refined, Resource.Butter, rate, 2); break;
                case BuildingKind.Bakery:
                    stats.pastryProduced += Convert(b, Resource.Butter, Resource.Pastry, rate, 1);
                    Amount sold = Sell(b, Resource.Pastry, rate); stats.pastrySold += sold; break;
                case BuildingKind.Shop: stats.coffeeSold += Sell(b, Resource.Refined, rate); break;
                case BuildingKind.Market: Earn(city, PassiveIncome(b)); break;
                case BuildingKind.Park: Earn(city, PassiveIncome(b)); break;
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
            var d = Definition(b);
            var city = City(b.cityId);
            Earn(city, amount * Plan(b).Price * Bonus(b, WorkerAttribute.Price) * CityProjectRewards.SalesMultiplier(defs, city, b.family)
                * (resource == Resource.Refined ? ShopRules.FlowerPriceMultiplier(state, EvaluationTime) : 1));
            if (amount == 0) b.bottleneck = "bottleneck.input";
            return amount;
        }
        private void Earn(CityState city, Amount money) { state.money += money; city.stats.earned += money; }
        private void Belt(RouteDefinition route)
        {
            var from = Find(route.from); var to = Find(route.to);
            if (from.level < 1 || to.level < 1 || !Running(from)) return;
            Amount moved = Amount.Min(from.Get(route.resource), Amount.Min(ProductionRate(from), Amount.Max(0, Capacity(to) - to.Get(route.resource))));
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
                var route = routes[delivery.route]; var to = Find(route.to);
                if (delivery.fetching)
                {
                    var from = Find(route.from); var worker = batchWorkers[delivery.workerId];
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
                var stats = City(to.cityId).stats; stats.delivered += unloaded;
                if (delivery.cargo == 0) { if(carriedCargo)stats.deliveries++; state.deliveries.RemoveAt(i); RemoveDeliveryIndexes(delivery, route); }
                else to.bottleneck = "bottleneck.storage";
            }
        }
        private void DispatchVehicles()
        {
            foreach (var from in state.buildings.Where(x => runningCities.Contains(x.cityId) && x.level > 0 && !string.IsNullOrEmpty(Definition(x).driverRole)))
            {
                if (!Running(from)) continue;
                var routes = EligibleVehicleRoutes(from);
                if (routes.Length == 0) continue;
                foreach (var w in Plan(from).Drivers)
                {
                    if (HasDelivery(w.id)) continue;
                    for (int n = 0; n < routes.Length; n++)
                    {
                        var route = routes[(from.routeCursor + n) % routes.Length]; var to = Find(route.to);
                        Amount reserved = IncomingReserved(to.id, route.resource);
                        Amount room = Amount.Max(0, Capacity(to) - to.Get(route.resource) - reserved);
                        Amount capacity = new Amount(route.capacity) * Plan(from).LevelCapacity / Plan(from).FirstCapacity * (1 + Workers.Effect(w, WorkerAttribute.Cargo)) * (1 + Workers.Effect(w, WorkerAttribute.Storage));
                        Amount amount = Amount.Min(from.Get(route.resource), Amount.Min(capacity, room));
                        if (amount <= 0) continue;
                        int time = Math.Max(1, (int)Math.Ceiling(route.travelSeconds / (1 + Workers.Effect(w, WorkerAttribute.TravelSpeed)) / (1 + Workers.Effect(w, WorkerAttribute.CycleSpeed))));
                        amount = Amount.Min(amount, ProductionRate(from) * (1 + Workers.Effect(w, WorkerAttribute.Throughput)));
                        from.Set(route.resource, from.Get(route.resource) - amount);
                        AddDelivery(new DeliveryState { route = route.id, workerId = w.id, cargo = amount, remainingSeconds = time, totalSeconds = time });
                        from.routeCursor = (from.routeCursor + n + 1) % routes.Length; break;
                    }
                }
                if (from.Total > 0 && Plan(from).Drivers.Length == 0) from.bottleneck = "bottleneck.driver";
            }
        }
        private Amount IncomingReserved(string recipient, Resource resource, DeliveryState except = null)
        {
            Amount reserved = Amount.Zero;
            if (!incomingDeliveries.TryGetValue(new ReservationKey(recipient, resource), out var entries)) return reserved;
            foreach (var t in entries)
            {
                var route = routes[t.route];
                if (t != except && route.to == recipient && route.resource == resource) reserved += t.fetching ? t.reservedCargo : t.cargo;
            }
            return reserved;
        }
        private Amount PickupReserved(string source, Resource resource, DeliveryState except = null)
        {
            Amount reserved = Amount.Zero;
            if (!pickupDeliveries.TryGetValue(new ReservationKey(source, resource), out var entries)) return reserved;
            foreach (var t in entries)
            {
                var route = routes[t.route];
                if (t != except && t.fetching && route.from == source && route.resource == resource) reserved += t.reservedCargo;
            }
            return reserved;
        }
        public Amount PickupCapacity(RouteDefinition route, BuildingState recipient, WorkerState worker) =>
            new Amount(route.capacity) * (batchPlans == null ? Definition(recipient).capacity.At(Math.Max(1, recipient.level)) : Plan(recipient).LevelCapacity)
            / (batchPlans == null ? Definition(recipient).capacity.At(1) : Plan(recipient).FirstCapacity)
            * (1 + Workers.Effect(worker, WorkerAttribute.Cargo)) * (1 + Workers.Effect(worker, WorkerAttribute.Storage));
        public int PickupTravelSeconds(RouteDefinition route, WorkerState worker) =>
            Math.Max(1, (int)Math.Ceiling(route.travelSeconds / (1 + Workers.Effect(worker, WorkerAttribute.TravelSpeed)) / (1 + Workers.Effect(worker, WorkerAttribute.CycleSpeed))));
        private void DispatchPickups()
        {
            foreach (var route in pickupRoutes.Where(x => runningCities.Contains(routeCities[x.id])))
            {
                var from = Find(route.from); var to = Find(route.to);
                if (from.level < 1 || to.level < 1 || Definition(to).zone > City(to.cityId).unlocked) continue;
                var worker = batchManagers.TryGetValue(to.id, out var manager) ? manager : null;
                if (worker == null || HasDelivery(worker.id)) continue;
                // Use the existing stock first; do not send a van for every unit consumed.
                if (to.Get(route.resource) > Capacity(to) * .25) continue;
                Amount available = Amount.Max(0, from.Get(route.resource) - PickupReserved(from.id, route.resource));
                Amount room = Amount.Max(0, Capacity(to) - to.Get(route.resource) - IncomingReserved(to.id, route.resource));
                Amount reserve = Amount.Min(available, Amount.Min(PickupCapacity(route, to, worker), room));
                if (reserve <= 0) continue;
                int time = PickupTravelSeconds(route, worker);
                AddDelivery(new DeliveryState { route = route.id, workerId = worker.id, fetching = true,
                    reservedCargo = reserve, cargo = 0, remainingSeconds = time, totalSeconds = time });
                to.bottleneck = "bottleneck.pickup";
            }
        }
    }
}

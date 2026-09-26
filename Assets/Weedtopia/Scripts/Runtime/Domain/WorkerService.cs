using System;
using System.Linq;

namespace Weedtopia.Domain
{
    public sealed class WorkerService
    {
        private readonly GameDefinitions defs;
        private readonly GameState state;
        public WorkerService(GameDefinitions defs, GameState state) { this.defs = defs; this.state = state; }
        public WorkerState Grant(string role, int rarity = 0)
        {
            if (rarity < 0 || rarity > 4) throw new ArgumentOutOfRangeException(nameof(rarity));
            var pool = defs.Role(role).attributes.ToList();
            var random = new SeededRandom(state.randomState);
            var worker = new WorkerState { id = "worker_" + (state.workers.Count + 1).ToString("D6"), role = role, rarity = rarity };
            for (int i = 0; i <= rarity; i++) { int at = random.Next(pool.Count); worker.attributes.Add(pool[at]); pool.RemoveAt(at); }
            state.randomState = random.State;
            state.workers.Add(worker); state.City.stats.workersReceived++; state.revision++;
            return worker;
        }
        public Failure Spin(out WorkerState worker)
        {
            worker = null;
            if (state.City.unlocked < Zone.City) return Failure.Locked;
            if (state.cards < 1) return Failure.Card;
            var random = new SeededRandom(state.randomState);
            int roll = random.Next(defs.rarityWeights.Sum()), rarity = 0;
            while (roll >= defs.rarityWeights[rarity]) roll -= defs.rarityWeights[rarity++];
            string role = defs.roles[random.Next(defs.roles.Length)].id;
            state.randomState = random.State; state.cards--;
            worker = Grant(role, rarity); state.City.stats.spins++;
            return Failure.None;
        }
        public int Slots(BuildingState building) => string.IsNullOrEmpty(defs.Building(building.family).driverRole) ? 0 : VisualTiers.Index(building.level);
        public string SlotRole(BuildingState b, int slot) => slot == 0 ? defs.Building(b.family).managerRole : defs.Building(b.family).driverRole;
        public Failure Assign(string workerId, string buildingId, int slot)
        {
            var worker = state.workers.FirstOrDefault(x => x.id == workerId);
            var building = state.buildings.FirstOrDefault(x => x.id == buildingId);
            if (worker == null || building == null || building.cityId != state.activeCity || building.level < 1) return Failure.Invalid;
            if (slot < 0 || slot > Slots(building)) return Failure.Slot;
            if (SlotRole(building, slot) != worker.role) return Failure.Incompatible;
            if (state.deliveries.Any(x => x.workerId == workerId)) return Failure.Busy;
            var other = state.workers.FirstOrDefault(x => x.buildingId == buildingId && x.slot == slot && x != worker);
            if (other != null && state.deliveries.Any(x => x.workerId == other.id)) return Failure.Busy;
            // A compatible swap is atomic; otherwise the displaced worker returns to inventory.
            string oldBuilding = worker.buildingId; int oldSlot = worker.slot;
            worker.buildingId = buildingId; worker.slot = slot;
            if (other != null)
            {
                var old = state.buildings.FirstOrDefault(x => x.id == oldBuilding);
                bool swap = old != null && SlotRole(old, oldSlot) == other.role;
                other.buildingId = swap ? oldBuilding : null; other.slot = swap ? oldSlot : -1;
            }
            state.revision++; return Failure.None;
        }
        public Failure Remove(string workerId)
        {
            var worker = state.workers.FirstOrDefault(x => x.id == workerId);
            if (worker == null) return Failure.Invalid;
            if (state.deliveries.Any(x => x.workerId == workerId)) return Failure.Busy;
            worker.buildingId = null; worker.slot = -1; state.revision++; return Failure.None;
        }
        public Failure Improve(string workerId, bool rank)
        {
            var w = state.workers.FirstOrDefault(x => x.id == workerId);
            if (w == null || (rank ? w.rank >= 10 : w.level >= 100)) return Failure.Invalid;
            var home = state.buildings.FirstOrDefault(x => x.id == w.buildingId);
            double discount = home == null ? 1 : Bonus(home, rank ? WorkerAttribute.RankDiscount : WorkerAttribute.TrainingDiscount);
            Amount cost = new Amount(defs.workerLevelBaseCost) * w.level * w.rank / discount;
            if (state.money < cost) return Failure.Money;
            if (rank && state.cards < defs.workerRankCards) return Failure.Card;
            state.money -= cost;
            if (rank) { state.cards -= defs.workerRankCards; w.rank++; } else w.level++;
            state.revision++; return Failure.None;
        }
        public bool Automated(BuildingState b) => state.workers.Any(x => x.buildingId == b.id && x.slot == 0);
        public double Bonus(BuildingState b, WorkerAttribute attribute) =>
            1 + state.workers.Where(x => x.buildingId == b.id && x.slot == 0).Sum(x => Effect(x, attribute));
        public double Effect(WorkerState w, WorkerAttribute a) => w.attributes.Contains(a) ? defs.workerAttributeStep * w.level * w.rank : 0;
    }
}

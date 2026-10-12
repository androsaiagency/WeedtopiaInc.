using System;
using System.Collections.Generic;
using System.Linq;

namespace Weedtopia.Domain
{
    public enum Resource { Raw, Refined, Butter, Pastry }
    public enum Zone { Farm, City, Settlement, Industry }
    public enum BuildingKind { Grow, Processing, Warehouse, Shop, House, Park, Butter, Market, Bakery, TownHall, Plot }
    public enum WorkerAttribute { Yield, CycleSpeed, Throughput, Storage, Cargo, TravelSpeed, Price, Residents, DailyBonus, BuildSpeed, TrainingDiscount, RankDiscount, VipReward, VipTarget, CommunityIncome }
    public enum ConditionKind { Talk, Level, Harvest, Assigned, Delivered, Sold, Spin, TownHall, Residents, Daily, Butter, PastrySold, Automated, VipCompleted }
    public enum Failure { None, Locked, Invalid, Busy, Money, GrinderConfirmation, Grinders, Incompatible, Slot, Card, NotReady, StaleOffer, ReservedPlot }

    public interface IClock { DateTime UtcNow { get; } }
    public sealed class ManualClock : IClock
    {
        public DateTime UtcNow { get; set; }
        public ManualClock(DateTime time) { UtcNow = time.ToUniversalTime(); }
    }
    public interface IRandomSource { uint State { get; } int Next(int exclusiveMax); }
    public sealed class SeededRandom : IRandomSource
    {
        public uint State { get; private set; }
        public SeededRandom(uint seed) { State = seed == 0 ? 1u : seed; }
        public int Next(int exclusiveMax)
        {
            if (exclusiveMax <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            uint x = State; x ^= x << 13; x ^= x >> 17; x ^= x << 5; State = x;
            return (int)(x % (uint)exclusiveMax);
        }
    }
    public static class VisualTiers
    {
        public static readonly int[] Starts = { 0, 1, 5, 10, 25, 50, 100, 150, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1250, 1500, 1750, 2000, 2500, 3000, 4000, 5000 };
        public static int Index(int level)
        {
            if (level < 0 || level > 5000) throw new ArgumentOutOfRangeException(nameof(level));
            int i = Array.BinarySearch(Starts, level);
            return i >= 0 ? i : ~i - 1;
        }
        public static string Id(int level, bool fixedBuilding = false)
        {
            int tier = Index(level);
            return fixedBuilding ? "T_FIXED" : tier == 0 ? "T00_RUIN" : "T" + tier.ToString("00") + "_L" + Starts[tier].ToString("000");
        }
    }
    [Serializable] public sealed class Curve
    {
        public double basis = 1, growth = 1;
        public Amount At(int level) => basis == 0 ? Amount.Zero : Amount.FromLog10(Math.Log10(basis) + Math.Max(0, level - 1) * Math.Log10(growth));
    }
    [Serializable] public sealed class BuildingDefinition
    {
        public string id, nameKey, managerRole, driverRole;
        public BuildingKind kind;
        public Zone zone;
        public Curve cost = new Curve(), capacity = new Curve(), rate = new Curve(), yield = new Curve(), price = new Curve();
        public int cycleSeconds = 4, baseBuildSeconds = 3, maxBuildSeconds = 120, baseResidents = 5;
        public double buildGrowth = 1.001;
        public int BuildSeconds(int level) => Math.Min(maxBuildSeconds, (int)Math.Ceiling(baseBuildSeconds * Math.Pow(buildGrowth, Math.Max(0, level - 1))));
    }
    [Serializable] public sealed class InstanceDefinition
    {
        public string id, family, cityId = "burning_hills";
        public Zone zone;
        public float x, z;
        public int introducedVersion = 1;
    }
    [Serializable] public sealed class RouteDefinition
    {
        public string id, from, to;
        public Resource resource;
        public bool vehicle;
        public bool pickupByRecipient;
        public int travelSeconds = 5;
        public double capacity = 8;
    }
    [Serializable] public sealed class RoleDefinition
    {
        public string id, nameKey;
        public WorkerAttribute[] attributes;
    }
    [Serializable] public sealed class QuestCondition
    {
        public ConditionKind kind;
        public string target;
        public int count = 1;
    }
    [Serializable] public sealed class QuestDefinition
    {
        public string id, titleKey, bodyKey, speaker, focus;
        public QuestCondition[] conditions;
        public int moneyReward = 100, cardsReward;
        public string[] workerRewards = Array.Empty<string>();
        public Zone unlock;
    }
    [Serializable] public sealed class TextEntry { public string key, de; }
    [Serializable] public sealed class GameDefinitions
    {
        public int version = 1, maxOfflineSeconds = 28800, maxDailyCatchup = 7, maxVip = 12;
        public int[] rarityWeights = { 60, 25, 10, 4, 1 };
        public double workerAttributeStep = 0.05;
        public int workerLevelBaseCost = 50, workerRankCards = 2, moneyPerGrinder = 100, secondsPerGrinder = 60;
        public BuildingDefinition[] buildings;
        public InstanceDefinition[] instances;
        public RouteDefinition[] routes;
        public RoleDefinition[] roles;
        public QuestDefinition[] quests;
        public TextEntry[] texts;
        public BuildingDefinition Building(string id) => buildings.Single(x => x.id == id);
        public RoleDefinition Role(string id) => roles.Single(x => x.id == id);
        public string Text(string key) => texts.FirstOrDefault(x => x.key == key)?.de ?? "[" + key + "]";
        public void Validate()
        {
            if (version < 1 || maxOfflineSeconds < 0 || maxOfflineSeconds > 604800 || maxDailyCatchup < 1 || maxVip < 1 ||
                moneyPerGrinder < 1 || secondsPerGrinder < 1 || rarityWeights == null || rarityWeights.Length != 5 || rarityWeights.Any(x => x <= 0))
                throw new InvalidOperationException("Invalid tuning");
            Unique(buildings.Select(x => x.id)); Unique(instances.Select(x => x.id)); Unique(roles.Select(x => x.id));
            Unique(routes.Select(x => x.id)); Unique(quests.Select(x => x.id)); Unique(texts.Select(x => x.key));
            foreach (var b in buildings)
            {
                if (!string.IsNullOrEmpty(b.managerRole)) Role(b.managerRole);
                if (!string.IsNullOrEmpty(b.driverRole)) Role(b.driverRole);
                foreach (var c in new[] { b.cost, b.capacity, b.rate, b.yield, b.price })
                    if (c == null || c.basis <= 0 || c.growth < 1 || double.IsNaN(c.growth) || double.IsInfinity(c.growth)) throw new InvalidOperationException("Invalid curve " + b.id);
                if (b.cycleSeconds < 1 || b.baseBuildSeconds < 1 || b.buildGrowth < 1 || b.maxBuildSeconds < b.baseBuildSeconds) throw new InvalidOperationException("Invalid times");
            }
            foreach (var i in instances) { Building(i.family); if (string.IsNullOrWhiteSpace(i.cityId)) throw new InvalidOperationException("City ID required"); }
            foreach (var r in routes)
            {
                var a = instances.Single(x => x.id == r.from); var b = instances.Single(x => x.id == r.to);
                if (a.cityId != b.cityId || r.from == r.to || r.capacity <= 0 || r.travelSeconds < 1) throw new InvalidOperationException("Invalid route");
                if (r.pickupByRecipient && (!r.vehicle || string.IsNullOrEmpty(Building(b.family).managerRole)))
                    throw new InvalidOperationException("Pickup route needs a recipient worker and vehicle");
            }
            foreach (var role in roles) if (role.attributes == null || role.attributes.Length != 5 || role.attributes.Distinct().Count() != 5 || role.attributes.Any(x=>!Enum.IsDefined(typeof(WorkerAttribute),x))) throw new InvalidOperationException("Five distinct valid role attributes required");
            foreach (var q in quests)
            {
                if (q.conditions == null || q.conditions.Length == 0 || q.moneyReward < 0 || q.cardsReward < 0) throw new InvalidOperationException("Invalid quest");
                foreach (var role in q.workerRewards) Role(role);
                if(!instances.Any(x=>x.id==q.focus))throw new InvalidOperationException("Unknown quest focus "+q.focus);
                foreach (var c in q.conditions)
                {
                    if (c.count < 1 || !Enum.IsDefined(typeof(ConditionKind), c.kind)) throw new InvalidOperationException("Invalid condition");
                    if(c.kind==ConditionKind.Automated&&!instances.Any(x=>x.id==c.target))throw new InvalidOperationException("Unknown automation target "+c.target);
                    if((c.kind==ConditionKind.Level||c.kind==ConditionKind.Assigned)&&!instances.Any(x=>x.id==c.target)&&!buildings.Any(x=>x.id==c.target))throw new InvalidOperationException("Unknown condition target "+c.target);
                }
                if (Text(q.titleKey).StartsWith("[") || Text(q.bodyKey).StartsWith("[")) throw new InvalidOperationException("Missing quest text");
            }
        }
        private static void Unique(IEnumerable<string> ids)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids) if (string.IsNullOrWhiteSpace(id) || !set.Add(id)) throw new InvalidOperationException("Missing/duplicate ID " + id);
        }
    }
}

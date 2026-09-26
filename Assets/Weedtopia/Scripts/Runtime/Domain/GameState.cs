using System;
using System.Collections.Generic;
using System.Linq;

namespace Weedtopia.Domain
{
    [Serializable] public sealed class BuildingState
    {
        public string id, family, cityId;
        public int level, buildingSeconds, workSeconds, cycleProgress, routeCursor, dailyTier, dailyClaims;
        public long lastDailyDay;
        public Amount raw, refined, butter, pastry;
        public string bottleneck;
        public Amount Get(Resource resource) => resource == Resource.Raw ? raw : resource == Resource.Refined ? refined : resource == Resource.Butter ? butter : pastry;
        public void Set(Resource resource, Amount value)
        {
            if (!value.IsValid || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            switch (resource) { case Resource.Raw: raw = value; break; case Resource.Refined: refined = value; break; case Resource.Butter: butter = value; break; default: pastry = value; break; }
        }
        public Amount Total => raw + refined + butter + pastry;
    }
    [Serializable] public sealed class WorkerState
    {
        public string id, role, buildingId;
        public int rarity, level = 1, rank = 1, slot = -1;
        public List<WorkerAttribute> attributes = new List<WorkerAttribute>();
    }
    [Serializable] public sealed class DeliveryState
    {
        public string route, workerId;
        public Amount cargo;
        public int remainingSeconds, totalSeconds;
        // Existing source-driver saves default to a loaded delivery. Pickup trips have two legs.
        public bool fetching;
        public Amount reservedCargo;
    }
    [Serializable] public sealed class VipState
    {
        public string id, cityId;
        public ConditionKind kind;
        public Amount target, reward;
        public bool completed;
    }
    [Serializable] public sealed class CityState
    {
        public string id;
        public Zone unlocked;
        public bool freeplay;
        public int questIndex;
        public List<string> talked = new List<string>();
        public List<string> completedQuests = new List<string>();
        public Statistics stats = new Statistics();
    }
    [Serializable] public sealed class Statistics
    {
        public Amount earned, rawProduced, refinedProduced, butterProduced, pastryProduced, coffeeSold, pastrySold, delivered;
        public long playSeconds;
        public int manualHarvests, deliveries, spins, townHallVisits, dailyClaims, vipCompleted, workersReceived;
    }
    [Serializable] public sealed class GameState
    {
        public int schemaVersion = 2, contentVersion = 1;
        public string playerName, activeCity = "burning_hills";
        public bool introSeen;
        public int introLine;
        public Amount money = 1000;
        public int grinders = 50, cards;
        public uint randomState = 420;
        public long lastUtcTicks, highWaterUtcTicks, simulationSeconds;
        public double tickRemainder;
        public long revision, nextRecoveryWork;
        public float volume = 0.7f, uiScale = 1;
        public List<CityState> cities = new List<CityState>();
        public List<BuildingState> buildings = new List<BuildingState>();
        public List<WorkerState> workers = new List<WorkerState>();
        public List<DeliveryState> deliveries = new List<DeliveryState>();
        public List<VipState> vip = new List<VipState>();
        public CityState City => cities.Single(x => x.id == activeCity);
        public BuildingState Find(string id) => buildings.Single(x => x.id == id);
        public static GameState Create(GameDefinitions defs, string name, DateTime utc, uint seed = 420)
        {
            name = (name ?? "").Trim();
            if (name.Length < 2 || name.Length > 24 || name.Any(c => char.IsControl(c) || c == '<' || c == '>')) throw new ArgumentException("name.invalid");
            defs.Validate();
            var state = new GameState { playerName = name, contentVersion = defs.version, lastUtcTicks = utc.Ticks, highWaterUtcTicks = utc.Ticks, randomState = seed == 0 ? 1 : seed };
            foreach (string city in defs.instances.Select(x => x.cityId).Distinct()) state.cities.Add(new CityState { id = city });
            foreach (var i in defs.instances) state.buildings.Add(new BuildingState { id = i.id, family = i.family, cityId = i.cityId,
                level = defs.Building(i.family).kind == BuildingKind.TownHall ? 1 : 0, lastDailyDay = utc.Date.Ticks / TimeSpan.TicksPerDay - 1 });
            return state;
        }
    }
}

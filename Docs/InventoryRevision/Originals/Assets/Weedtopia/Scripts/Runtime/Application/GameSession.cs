using System;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    public interface ISaveStore { void Save(GameState state); SaveLoadResult Load(); bool Exists { get; } }
    public sealed class SaveLoadResult { public GameState State; public bool Recovered; public string Message; }
    public sealed class OfflineSummary { public int Seconds; public Amount Earned; public bool ClockWentBack; }

    /// <summary>Use cases and transaction boundary. UI can be replaced without changing rules.</summary>
    public sealed class GameSession
    {
        public GameDefinitions Definitions { get; }
        public GameState State { get; }
        public Simulation Simulation { get; }
        public WorkerService Workers => Simulation.Workers;
        public UpgradeService Upgrades { get; }
        public ProgressionService Progression { get; }
        public DailyService Daily { get; }
        public CityProjectService CityProject { get; }
        public bool Paused { get; set; }
        private readonly IClock clock;
        private readonly ISaveStore save;
        private double autoSaveSeconds;
        public GameSession(GameDefinitions defs, GameState state, IClock clock, ISaveStore save = null)
        {
            defs.Validate(); Definitions = defs; State = state; this.clock = clock; this.save = save;
            Simulation = new Simulation(defs, state); Upgrades = new UpgradeService(defs, state, Workers);
            Progression = new ProgressionService(defs, state, Simulation); Daily = new DailyService(defs, state, Simulation);
            CityProject = new CityProjectService(defs, state, Simulation);
        }
        public void Advance(double seconds)
        {
            if (Paused || !State.introSeen) return;
            StampTime(); Simulation.Advance(seconds); autoSaveSeconds += seconds;
            if (autoSaveSeconds >= 30) Save();
        }
        private void StampTime()
        {
            long ticks = clock.UtcNow.Ticks;
            State.highWaterUtcTicks = Math.Max(State.highWaterUtcTicks, ticks);
            State.lastUtcTicks = State.highWaterUtcTicks;
        }
        public void Save() { StampTime(); save?.Save(State); autoSaveSeconds = 0; }
        public Failure Transact(Func<Failure> command)
        {
            Failure result = command(); if (result == Failure.None) Save(); return result;
        }
        public void CompleteIntro() { State.introSeen = true; State.introLine = 3; State.revision++; Save(); }
        public void RestartIntro() { State.introSeen = false; State.introLine = 0; State.revision++; Save(); }
        public void NextIntro() { State.introLine++; if (State.introLine >= 3) CompleteIntro(); else Save(); }
        public OfflineSummary ApplyOffline()
        {
            long now = clock.UtcNow.Ticks;
            long previous = Math.Max(State.lastUtcTicks, State.highWaterUtcTicks);
            var result = new OfflineSummary { ClockWentBack = now < previous };
            if (now > previous && State.introSeen)
            {
                result.Seconds = (int)Math.Min(Definitions.maxOfflineSeconds, (now - previous) / TimeSpan.TicksPerSecond);
                Amount earned = State.City.stats.earned;
                StampTime(); Simulation.Advance(result.Seconds);
                result.Earned = State.City.stats.earned - earned;
            }
            Save(); return result;
        }
        public Failure ChoosePlot(string id, string family)
        {
            if (State.City.unlocked < Zone.Settlement) return Failure.Locked;
            if (family != "residential_house" && family != "park") return Failure.Invalid;
            var plot = State.buildings.FirstOrDefault(x => x.id == id && x.cityId == State.activeCity);
            if (plot == null || Definitions.Building(plot.family).kind != BuildingKind.Plot) return Failure.Invalid;
            var remaining = State.buildings.Where(x => x.cityId == State.activeCity && Definitions.Building(x.family).kind == BuildingKind.Plot).ToArray();
            string missing = family == "park" ? "residential_house" : "park";
            if (remaining.Length == 1 && !State.buildings.Any(x => x.cityId == State.activeCity && x.family == missing)) return Failure.ReservedPlot;
            plot.family = family; State.revision++; return Failure.None;
        }
        public Failure Talk(string speaker)
        {
            if (!new[] { "mila", "jaro", "nia", "rocko" }.Contains(speaker)) return Failure.Invalid;
            if (!State.City.talked.Contains(speaker)) { State.City.talked.Add(speaker); State.revision++; }
            return Failure.None;
        }
        public Failure OpenTownHall()
        {
            if (State.City.unlocked < Zone.City) return Failure.Locked;
            State.City.stats.townHallVisits++; State.revision++; return Failure.None;
        }
        public Failure RequestRecoveryWork()
        {
            // Repeatable manual rebuilding work keeps optional overspending from deadlocking the tutorial.
            if (State.City.freeplay || State.money >= 200 || State.simulationSeconds < State.nextRecoveryWork) return Failure.NotReady;
            State.money += 100; State.City.stats.earned += 100; State.nextRecoveryWork = State.simulationSeconds + 60; State.revision++;
            return Failure.None;
        }
    }
}

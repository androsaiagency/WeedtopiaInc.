using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    public interface ISaveStore { void Save(GameState state); SaveLoadResult Load(); bool Exists { get; } }
    public sealed class SaveLoadResult
    {
        public GameState State;
        public bool Recovered;
        public string Message;
        // A transient diagnostic cause. Never serialized; callers may classify it without exposing raw text.
        public Exception Error;
    }
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
        public InventoryService Inventory { get; }
        public LuckyWheelService Wheel { get; }
        public VipStoryService Stories { get; }
        public CityCampaignService Campaign { get; }
        public ShopService Shop { get; }
        public bool Paused { get; set; }
        private int offlineState;
        public bool IsApplyingOffline
        { get => Volatile.Read(ref offlineState) != 0; private set => Volatile.Write(ref offlineState, value ? 1 : 0); }
        private readonly int ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        private readonly IClock clock;
        private readonly ISaveStore save;
        private const double AutoSaveInterval = 30;
        private const double MaximumSaveRetryInterval = 300;
        private double autoSaveSeconds;
        private bool deferPersistence;
        public Exception LastSaveError { get; private set; }
        public int ConsecutiveSaveFailures { get; private set; }
        public double SaveRetrySeconds { get; private set; }
        public GameSession(GameDefinitions defs, GameState state, IClock clock, ISaveStore save = null, IShopPurchaseGateway shopGateway = null)
        {
            defs.Validate(); Definitions = defs; State = state; this.clock = clock; this.save = save;
            Simulation = new Simulation(defs, state); Upgrades = new UpgradeService(defs, state, Workers);
            Progression = new ProgressionService(defs, state, Simulation); Daily = new DailyService(defs, state, Simulation);
            CityProject = new CityProjectService(defs, state, Simulation);
            Inventory = new InventoryService(defs, state, Workers);
            Wheel = new LuckyWheelService(defs, state, clock);
            Wheel.RefreshDay();
            Stories = new VipStoryService(defs, state, Simulation);
            Campaign = new CityCampaignService(defs, state, Simulation);
            Campaign.RefreshMilestones();
            Stories.RefreshUnlocks();
            Shop = new ShopService(defs, state, clock, Save, shopGateway);
        }
        public void Advance(double seconds)
        {
            EnsureOfflineIdle();
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > 604800)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            SaveRetrySeconds = Math.Max(0, SaveRetrySeconds - seconds);
            if (SaveRetrySeconds < 1e-9) SaveRetrySeconds = 0;
            // A failing VIP grant also writes a save. Do not retry that grant every frame.
            if (SaveRetrySeconds == 0) TryAutomaticPersistence(() => Shop.RefreshVip());
            if (!Paused && State.introSeen)
            {
                StampTime(); Simulation.Advance(seconds, Math.Max(0, State.highWaterUtcTicks - (long)(seconds * TimeSpan.TicksPerSecond))); autoSaveSeconds += seconds;
                Campaign.RefreshMilestones();
                Stories.RefreshUnlocks();
            }
            if (SaveRetrySeconds == 0 && (autoSaveSeconds >= AutoSaveInterval || LastSaveError != null))
                TryAutomaticPersistence(Save);
        }
        private void TryAutomaticPersistence(Action action)
        {
            try { action(); }
            // Only exceptions raised by this session's save boundary are recoverable here.
            // Simulation and command defects must remain visible to the caller.
            catch (Exception error) when (ReferenceEquals(error, LastSaveError)) { }
        }
        private void StampTime()
        {
            long ticks = clock.UtcNow.Ticks;
            State.highWaterUtcTicks = Math.Max(State.highWaterUtcTicks, ticks);
            State.lastUtcTicks = State.highWaterUtcTicks;
        }
        public void Save()
        {
            EnsureOfflineIdle();
            if (deferPersistence) return;
            long previousLast = State.lastUtcTicks, previousHigh = State.highWaterUtcTicks;
            StampTime();
            try { save?.Save(State); }
            catch (Exception error)
            {
                State.lastUtcTicks = previousLast; State.highWaterUtcTicks = previousHigh;
                LastSaveError = error;
                ConsecutiveSaveFailures = Math.Min(ConsecutiveSaveFailures + 1, 30);
                SaveRetrySeconds = Math.Min(MaximumSaveRetryInterval,
                    AutoSaveInterval * Math.Pow(2, Math.Min(ConsecutiveSaveFailures - 1, 4)));
                throw;
            }
            autoSaveSeconds = 0;
            LastSaveError = null; ConsecutiveSaveFailures = 0; SaveRetrySeconds = 0;
        }
        public Failure Transact(Func<Failure> command)
        {
            if (IsApplyingOffline) return Failure.Busy;
            if (command == null) throw new ArgumentNullException(nameof(command));
            var snapshot = new GameStateTransactionSnapshot(State);
            try
            {
                Failure result = command();
                if (result == Failure.None) { Campaign.RefreshMilestones(); Save(); }
                else snapshot.Restore();
                return result;
            }
            catch { snapshot.Restore(); throw; }
        }
        public Failure TransactStory(Func<Failure> command)
        {
            if (IsApplyingOffline) return Failure.Busy;
            var snapshot = new VipStoryTransactionSnapshot(State);
            try { var result = command(); if (result == Failure.None) Save(); return result; }
            catch { snapshot.Restore(State); throw; }
        }
        public Failure TransactCampaign(Func<Failure> command)
        {
            if (IsApplyingOffline) return Failure.Busy;
            string active = State.activeCity;
            int cards = State.cards;
            var cities = State.cities.Select(c => new { City = c, c.charterAccepted, c.charterResidentMilestone, c.storyResidentMilestone, c.stats.earned }).ToArray();
            var snapshots = State.cities.Select(c => new VipStoryTransactionSnapshot(State, c)).ToArray();
            try
            {
                var result = command();
                if (result == Failure.None) { Stories.RefreshUnlocks(); Save(); }
                return result;
            }
            catch
            {
                State.activeCity = active; State.cards = cards;
                foreach (var snapshot in snapshots) snapshot.Restore(State);
                foreach (var c in cities) { c.City.charterAccepted = c.charterAccepted; c.City.charterResidentMilestone = c.charterResidentMilestone; c.City.storyResidentMilestone = c.storyResidentMilestone; c.City.stats.earned = c.earned; }
                throw;
            }
        }
        public void RefreshWheel()
        {
            EnsureOfflineIdle();
            if (!Wheel.NeedsRefresh) return;
            var snapshot = new WheelTransactionSnapshot(State);
            if (!Wheel.RefreshDay()) return;
            try { Save(); } catch { snapshot.Restore(State); throw; }
        }
        public Failure SpinWheel(string token, out WheelSpinResult result)
        {
            if (IsApplyingOffline) { result = null; return Failure.Busy; }
            var snapshot = new WheelTransactionSnapshot(State);
            var failure = Wheel.Confirm(token, out result);
            if (failure != Failure.None) return failure;
            try { Save(); }
            catch { snapshot.Restore(State); result = null; throw; }
            return Failure.None;
        }
        public Failure UpdateProfile(string name, string avatar)
        {
            if (IsApplyingOffline) return Failure.Busy;
            if (!PlayerProfile.ValidName(name) || !PlayerProfile.ValidAvatar(avatar)) return Failure.Invalid;
            string previousName = State.playerName, previousAvatar = State.playerAvatar;
            bool pending = State.profileNamePending; long revision = State.revision;
            State.playerName = name.Trim(); State.playerAvatar = avatar; State.profileNamePending = false; State.revision++;
            try { Save(); return Failure.None; }
            catch { State.playerName = previousName; State.playerAvatar = previousAvatar; State.profileNamePending = pending; State.revision = revision; throw; }
        }
        public void CompleteIntro()
        {
            EnsureOfflineIdle();
            Transact(() => { CompleteIntroCore(); return Failure.None; });
        }
        private void CompleteIntroCore()
        {
            if(State.profileNamePending)throw new InvalidOperationException("Set the profile name with Rocko first.");
            State.introSeen = true; State.introLine = 3; State.revision++; Stories.RefreshUnlocks();
        }
        public void RestartIntro()
        {
            EnsureOfflineIdle();
            Transact(() => { State.introSeen = false; State.introLine = 0; State.revision++; return Failure.None; });
        }
        public void NextIntro()
        {
            EnsureOfflineIdle();
            Transact(() => { State.introLine++; if (State.introLine >= 3) CompleteIntroCore(); return Failure.None; });
        }
        public OfflineSummary ApplyOffline() => ApplyOfflineIncrementally(Weedtopia.Domain.Simulation.OfflineTickGroupSeconds).Last().Summary;

        /// <summary>
        /// Enumerate on the main thread, yielding a rendered frame between batches.
        /// Disposal or failure restores the pre-load model; only the complete result is saved.
        /// </summary>
        public IEnumerable<OfflineProgress> ApplyOfflineIncrementally(int batchSeconds = 128)
        {
            if (batchSeconds < 1 || batchSeconds > 128) throw new ArgumentOutOfRangeException(nameof(batchSeconds));
            EnsureOfflineIdle();
            var snapshot = new GameStateTransactionSnapshot(State);
            bool completed = false;
            IsApplyingOffline = true;
            try
            {
                long now = clock.UtcNow.Ticks;
                long previous = Math.Max(State.lastUtcTicks, State.highWaterUtcTicks);
                var summary = new OfflineSummary { ClockWentBack = now < previous };
                if (now > previous && State.introSeen)
                    summary.Seconds = (int)Math.Min(Definitions.maxOfflineSeconds, (now - previous) / TimeSpan.TicksPerSecond);
                Amount before = State.money;
                yield return new OfflineProgress(0, summary.Seconds);
                int simulated = 0;
                while (simulated < summary.Seconds)
                {
                    int batch = Math.Min(batchSeconds, summary.Seconds - simulated);
                    Simulation.Advance(batch, previous + simulated * TimeSpan.TicksPerSecond);
                    simulated += batch;
                    if (simulated < summary.Seconds) yield return new OfflineProgress(simulated, summary.Seconds);
                }
                CommitOffline(summary, before);
                completed = true;
                yield return new OfflineProgress(summary.Seconds, summary.Seconds, summary);
            }
            finally
            {
                IsApplyingOffline = false;
                if (!completed) snapshot.Restore();
            }
        }
        /// <summary>
        /// Start only for an exclusively owned, unpublished continuation session. Do not read its model
        /// or call its domain services until completion. The operation exposes atomic progress only.
        /// </summary>
        public OfflineSimulationOperation StartOfflineSimulation()
        {
            if (Thread.CurrentThread.ManagedThreadId != ownerThreadId)
                throw new InvalidOperationException("Start offline simulation on the session's creating thread.");
            EnsureOfflineIdle();
            var snapshot = new GameStateTransactionSnapshot(State);
            IsApplyingOffline = true;
            try
            {
                long now = clock.UtcNow.Ticks;
                long previous = Math.Max(State.lastUtcTicks, State.highWaterUtcTicks);
                var summary = new OfflineSummary { ClockWentBack = now < previous };
                if (now > previous && State.introSeen)
                    summary.Seconds = (int)Math.Min(Definitions.maxOfflineSeconds, (now - previous) / TimeSpan.TicksPerSecond);
                Amount before = State.money;
                return new OfflineSimulationOperation(summary.Seconds,
                    (cancellation, progress) => Simulation.AdvanceOffline(summary.Seconds, previous, cancellation, progress),
                    () => { CommitOffline(summary, before); return summary; },
                    () => { try { snapshot.Restore(); } finally { IsApplyingOffline = false; } });
            }
            catch { try { snapshot.Restore(); } finally { IsApplyingOffline = false; } throw; }
        }

        private void CommitOffline(OfflineSummary summary, Amount before)
        {
            if (summary.Seconds > 0) { StampTime(); summary.Earned = State.money - before; }
            // No worker remains. The owning thread performs all derived unlocks and the one final save.
            IsApplyingOffline = false;
            deferPersistence = true;
            try { Stories.RefreshUnlocks(); Campaign.RefreshMilestones(); Shop.RefreshVip(); }
            finally { deferPersistence = false; }
            Save();
        }
        private void EnsureOfflineIdle()
        {
            if (IsApplyingOffline) throw new InvalidOperationException("Offline progress must finish before another action or save.");
        }
        public Failure ChoosePlot(string id, string family)
        {
            if (IsApplyingOffline) return Failure.Busy;
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
            if (IsApplyingOffline) return Failure.Busy;
            if (!new[] { "mila", "jaro", "nia", "rocko" }.Contains(speaker)) return Failure.Invalid;
            if (!State.City.talked.Contains(speaker)) { State.City.talked.Add(speaker); State.revision++; }
            return Failure.None;
        }
        public Failure OpenTownHall()
        {
            if (IsApplyingOffline) return Failure.Busy;
            if (State.City.unlocked < Zone.City) return Failure.Locked;
            State.City.stats.townHallVisits++; State.revision++; return Failure.None;
        }
        public Failure RequestRecoveryWork()
        {
            if (IsApplyingOffline) return Failure.Busy;
            // Repeatable manual rebuilding work keeps optional overspending from deadlocking the tutorial.
            if (State.City.freeplay || State.money >= 200 || State.simulationSeconds < State.nextRecoveryWork) return Failure.NotReady;
            State.money += 100; State.City.stats.earned += 100; State.nextRecoveryWork = State.simulationSeconds + 60; State.revision++;
            return Failure.None;
        }
    }
}

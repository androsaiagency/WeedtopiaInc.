using System;
using System.Collections.Generic;
using System.Linq;

namespace Weedtopia.Domain
{
    public sealed class PremiumOffer
    {
        public string Token { get; internal set; }
        public string BuildingId { get; internal set; }
        public int Grinders { get; internal set; }
        public bool FinishConstruction { get; internal set; }
        internal long revision;
        internal int level;
    }
    public sealed class UpgradeService
    {
        private readonly GameDefinitions defs;
        private readonly GameState state;
        private readonly WorkerService workers;
        private readonly Dictionary<string, PremiumOffer> offers = new Dictionary<string, PremiumOffer>();
        public UpgradeService(GameDefinitions defs, GameState state, WorkerService workers) { this.defs = defs; this.state = state; this.workers = workers; }
        public Amount Cost(BuildingState b) => defs.Building(b.family).cost.At(b.level + 1);
        public int Duration(BuildingState b)
        {
            if (b.level >= 5000) return 0;
            return (int)Math.Ceiling(defs.Building(b.family).BuildSeconds(b.level + 1) /
                workers.Bonus(b, WorkerAttribute.BuildSpeed));
        }
        private Failure CanUpgrade(BuildingState b)
        {
            if (b == null || b.cityId != state.activeCity) return Failure.Invalid;
            var d = defs.Building(b.family);
            if (d.kind == BuildingKind.TownHall || d.kind == BuildingKind.Plot || b.level >= 5000) return Failure.Invalid;
            if (d.zone > state.City.unlocked) return Failure.Locked;
            return b.buildingSeconds > 0 ? Failure.Busy : Failure.None;
        }
        public Failure BuyWithMoney(string id)
        {
            var b = state.buildings.FirstOrDefault(x => x.id == id);
            Failure check = CanUpgrade(b); if (check != Failure.None) return check;
            if (state.money < Cost(b)) return Failure.GrinderConfirmation;
            state.money -= Cost(b); Begin(b); return Failure.None;
        }
        private void Begin(BuildingState b)
        {
            b.buildingSeconds = Duration(b);
            if (b.buildingSeconds == 0) b.level++;
            state.revision++;
        }
        public Failure Offer(string id, bool finish, out PremiumOffer offer)
        {
            offer = null; var b = state.buildings.FirstOrDefault(x => x.id == id);
            if (b == null || b.cityId != state.activeCity) return Failure.Invalid;
            if (finish ? b.buildingSeconds <= 0 : CanUpgrade(b) != Failure.None) return Failure.Invalid;
            if (!finish && state.money >= Cost(b)) return Failure.Invalid;
            Amount units = finish ? new Amount(Math.Ceiling((double)b.buildingSeconds / defs.secondsPerGrinder)) : Cost(b) / defs.moneyPerGrinder;
            if (units > int.MaxValue) return Failure.Grinders;
            int cost = Math.Max(1, (int)Math.Ceiling(units.ToDouble()));
            offer = new PremiumOffer { Token = Guid.NewGuid().ToString("N"), BuildingId = id, Grinders = cost,
                FinishConstruction = finish, revision = state.revision, level = b.level };
            offers.Clear(); offers.Add(offer.Token, offer); return Failure.None;
        }
        public void Cancel(string token) { if (token != null) offers.Remove(token); }
        public Failure Confirm(string token)
        {
            if (token == null || !offers.TryGetValue(token, out PremiumOffer offer)) return Failure.StaleOffer;
            offers.Remove(token); // One-use capability; UI double clicks cannot charge twice.
            var b = state.Find(offer.BuildingId);
            if (offer.revision != state.revision || offer.level != b.level || (offer.FinishConstruction && b.buildingSeconds <= 0)) return Failure.StaleOffer;
            if (state.grinders < offer.Grinders) return Failure.Grinders;
            state.grinders -= offer.Grinders;
            if (offer.FinishConstruction)
            {
                if(b.level==0){b.lastDailyDay=state.highWaterUtcTicks/TimeSpan.TicksPerDay-1;b.dailyTier=1;}
                b.buildingSeconds = 0; b.level++; state.revision++;
            }
            else Begin(b);
            return Failure.None;
        }
    }
}

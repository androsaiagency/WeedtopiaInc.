using System;
using System.Collections.Generic;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    public sealed class WheelOffer
    {
        public string Token { get; internal set; }
        public int Grinders { get; internal set; }
        internal long day;
        internal int spins;
    }
    public sealed class WheelSpinResult
    {
        public int Index { get; internal set; }
        public ItemStack Prize { get; internal set; }
        public int Cost { get; internal set; }
        public long ReceiptSequence { get; internal set; }
    }
    /// <summary>Persisted UTC daily board; uniform draws without replacement. No Unity dependency.</summary>
    public sealed class LuckyWheelService
    {
        private readonly GameDefinitions defs;
        private readonly GameState state;
        private readonly IClock clock;
        private WheelOffer pending;
        public bool Available => defs.wheelRewards != null && defs.wheelRewards.Length >= LuckyWheelRules.Size;
        private long Now => Math.Max(clock.UtcNow.Ticks, state.highWaterUtcTicks);
        public bool NeedsRefresh => Available && (state.luckyWheel == null || !state.luckyWheel.initialized || state.luckyWheel.day < Now / TimeSpan.TicksPerDay);
        public TimeSpan UntilReset => TimeSpan.FromTicks(Math.Max(0, Math.Min(DateTime.MaxValue.Ticks,
            (Now / TimeSpan.TicksPerDay + 1) * TimeSpan.TicksPerDay) - Now));
        public WheelPrize[] Remaining => state.luckyWheel?.prizes.Where(p => !p.claimed).ToArray() ?? Array.Empty<WheelPrize>();
        public int NextCost => LuckyWheelRules.Cost(state.luckyWheel?.spins ?? 0);
        public LuckyWheelService(GameDefinitions defs, GameState state, IClock clock)
        { this.defs = defs; this.state = state; this.clock = clock; }
        public bool RefreshDay()
        {
            if (!Available) return false;
            long day = Now / TimeSpan.TicksPerDay;
            if (state.luckyWheel != null && state.luckyWheel.initialized && state.luckyWheel.day >= day) return false;
            var random = new SeededRandom(state.luckyWheel != null && state.luckyWheel.initialized ? state.luckyWheel.randomState : (state.randomState ^ (uint)day ^ 0x71A9F35Bu));
            var pool = defs.wheelRewards.ToList();
            var board = new LuckyWheelState { initialized = true, day = day };
            for (int i = 0; i < LuckyWheelRules.Size; i++)
            {
                int index = random.Next(pool.Count); var item = pool[index]; pool.RemoveAt(index);
                board.prizes.Add(new WheelPrize { id = item.id, count = item.count });
            }
            board.randomState = random.State;
            state.luckyWheel = board; state.revision++;
            pending = null;
            return true;
        }
        public Failure Offer(out WheelOffer offer)
        {
            offer = null; pending = null; RefreshDay();
            if (!Available || Remaining.Length == 0) return Failure.NotReady;
            if (state.grinders < NextCost) return Failure.Grinders;
            if (!CanGrant()) return Failure.Invalid;
            offer = new WheelOffer { Token = Guid.NewGuid().ToString("N"), Grinders = NextCost,
                day = state.luckyWheel.day, spins = state.luckyWheel.spins };
            pending = offer; return Failure.None;
        }
        public Failure Confirm(string token, out WheelSpinResult result)
        {
            result = null; var offer = pending; pending = null;
            if (offer == null || token != offer.Token) return Failure.StaleOffer;
            if (RefreshDay() || state.luckyWheel.day != offer.day || state.luckyWheel.spins != offer.spins || NextCost != offer.Grinders)
                return Failure.StaleOffer;
            if (Remaining.Length == 0) return Failure.NotReady;
            if (state.grinders < offer.Grinders) return Failure.Grinders;
            if (!CanGrant()) return Failure.Invalid;
            var choices = Remaining; var random = new SeededRandom(state.luckyWheel.randomState);
            int index = random.Next(choices.Length); var prize = choices[index];
            var items = new List<RewardItem>();
            state.grinders -= offer.Grinders;
            InventoryItems.Grant(state, new[] { new ItemStack { id = prize.id, count = prize.count } }, items);
            items[0].rarity = defs.items.Single(d => d.id == prize.id).rarity;
            prize.claimed = true; state.luckyWheel.spins++; state.luckyWheel.randomState = random.State; state.revision++;
            state.highWaterUtcTicks = Now;
            RewardHistory.Record(state, RewardSource.LuckyWheel, "wheel_" + offer.day + "_" + state.luckyWheel.spins,
                "wheel.reward_title", items);
            result = new WheelSpinResult { Index = index, Prize = new ItemStack { id = prize.id, count = prize.count },
                Cost = offer.Grinders, ReceiptSequence = state.rewardSequence };
            return Failure.None;
        }
        private bool CanGrant() => state.rewardSequence < long.MaxValue - 1 && Remaining.All(p =>
            InventoryItems.Count(state, p.id) <= int.MaxValue - p.count);
        public void Cancel() => pending = null;
    }
    /// <summary>Restore this feature's mutations if the atomic save fails, including RNG and receipts.</summary>
    internal sealed class WheelTransactionSnapshot
    {
        private readonly int grinders;
        private readonly uint random;
        private readonly long revision, sequence, highWater, last;
        private readonly LuckyWheelState wheel;
        private readonly List<ItemStack> inventory;
        private readonly List<RewardReceipt> history;
        public WheelTransactionSnapshot(GameState s)
        {
            grinders = s.grinders; random = s.randomState; revision = s.revision; sequence = s.rewardSequence;
            highWater = s.highWaterUtcTicks; last = s.lastUtcTicks;
            wheel = s.luckyWheel == null ? null : new LuckyWheelState { initialized = s.luckyWheel.initialized, day = s.luckyWheel.day, spins = s.luckyWheel.spins, randomState = s.luckyWheel.randomState,
                prizes = s.luckyWheel.prizes.Select(p => new WheelPrize { id = p.id, count = p.count, claimed = p.claimed }).ToList() };
            inventory = s.inventory?.Select(p => new ItemStack { id = p.id, count = p.count }).ToList();
            history = s.rewardHistory?.ToList();
        }
        public void Restore(GameState s)
        {
            s.grinders = grinders; s.randomState = random; s.revision = revision; s.rewardSequence = sequence;
            s.highWaterUtcTicks = highWater; s.lastUtcTicks = last;
            s.luckyWheel = wheel; s.inventory = inventory; s.rewardHistory = history;
        }
    }
}

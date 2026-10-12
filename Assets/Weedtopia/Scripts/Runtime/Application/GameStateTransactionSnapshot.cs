using System;
using System.Collections.Generic;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    /// <summary>
    /// Captures the mutable save graph before a command. Rollback keeps existing objects,
    /// lists and arrays alive, so services and presentation references remain valid.
    /// Explicit copies avoid reflection and runtime serialization on mobile platforms.
    /// </summary>
    internal sealed class GameStateTransactionSnapshot
    {
        private readonly List<Action> restore = new List<Action>();

        public GameStateTransactionSnapshot(GameState state)
        {
            Remember(state, Copy);
            RememberList(state.buildings, b => Remember(b, Copy));
            RememberList(state.workers, w => { Remember(w, Copy); RememberList(w.attributes); });
            RememberList(state.deliveries, d => Remember(d, Copy));
            RememberList(state.vip, v => Remember(v, Copy));
            RememberList(state.inventory, i => Remember(i, Copy));
            RememberList(state.rewardHistory, r =>
            {
                Remember(r, Copy);
                RememberList(r.items, i => Remember(i, Copy));
            });
            RememberList(state.cities, CaptureCity);
            if (state.luckyWheel != null)
            {
                Remember(state.luckyWheel, Copy);
                RememberList(state.luckyWheel.prizes, p => Remember(p, Copy));
            }
            if (state.shop != null)
            {
                Remember(state.shop, Copy);
                RememberList(state.shop.purchases, p => Remember(p, Copy));
                Remember(state.shop.vipMembership, Copy);
            }
        }

        public void Restore()
        {
            foreach (var action in restore) action();
        }

        private void CaptureCity(CityState city)
        {
            Remember(city, Copy);
            Remember(city.stats, Copy);
            RememberList(city.talked);
            RememberList(city.completedQuests);
            if (city.project != null)
            {
                Remember(city.project, Copy);
                RememberList(city.project.completedSteps);
                RememberArray(city.project.baselines);
            }
            RememberList(city.vipStories, s =>
            {
                Remember(s, Copy);
                RememberList(s.completed, c => Remember(c, Copy));
                CaptureActiveStory(s.active);
            });
            RememberList(city.vipCollaborations, c =>
            {
                Remember(c, Copy);
                Remember(c.completed, Copy);
                CaptureActiveStory(c.active);
            });
        }

        private void CaptureActiveStory(ActiveStoryQuest quest)
        {
            if (quest == null) return;
            Remember(quest, Copy);
            RememberArray(quest.baselines);
            RememberArray(quest.targets);
        }

        private void Remember<T>(T original, Action<T, T> copy) where T : class, new()
        {
            if (original == null) return;
            var values = new T();
            copy(original, values);
            restore.Add(() => copy(values, original));
        }

        private void RememberList<T>(List<T> original, Action<T> capture = null)
        {
            if (original == null) return;
            var items = original.ToArray();
            restore.Add(() => { original.Clear(); original.AddRange(items); });
            if (capture != null) foreach (var item in items) capture(item);
        }

        private void RememberArray<T>(T[] original)
        {
            if (original == null) return;
            var values = (T[])original.Clone();
            restore.Add(() => Array.Copy(values, original, values.Length));
        }

        private static void Copy(GameState a, GameState b)
        {
            b.schemaVersion = a.schemaVersion; b.contentVersion = a.contentVersion;
            b.playerName = a.playerName; b.playerAvatar = a.playerAvatar; b.activeCity = a.activeCity;
            b.profileNamePending = a.profileNamePending; b.introSeen = a.introSeen; b.introLine = a.introLine;
            b.money = a.money; b.grinders = a.grinders; b.cards = a.cards; b.randomState = a.randomState;
            b.lastUtcTicks = a.lastUtcTicks; b.highWaterUtcTicks = a.highWaterUtcTicks;
            b.simulationSeconds = a.simulationSeconds; b.tickRemainder = a.tickRemainder;
            b.revision = a.revision; b.nextRecoveryWork = a.nextRecoveryWork;
            b.inventory = a.inventory; b.luckyWheel = a.luckyWheel; b.shop = a.shop;
            b.rewardSequence = a.rewardSequence; b.rewardHistory = a.rewardHistory;
            b.volume = a.volume; b.uiScale = a.uiScale;
            b.cities = a.cities; b.buildings = a.buildings; b.workers = a.workers;
            b.deliveries = a.deliveries; b.vip = a.vip;
        }
        private static void Copy(BuildingState a, BuildingState b)
        {
            b.id = a.id; b.family = a.family; b.cityId = a.cityId; b.level = a.level;
            b.buildingSeconds = a.buildingSeconds; b.workSeconds = a.workSeconds;
            b.cycleProgress = a.cycleProgress; b.routeCursor = a.routeCursor;
            b.dailyTier = a.dailyTier; b.dailyClaims = a.dailyClaims; b.lastDailyDay = a.lastDailyDay;
            b.raw = a.raw; b.refined = a.refined; b.butter = a.butter; b.pastry = a.pastry;
            b.bottleneck = a.bottleneck;
        }
        private static void Copy(WorkerState a, WorkerState b)
        {
            b.id = a.id; b.role = a.role; b.buildingId = a.buildingId;
            b.rarity = a.rarity; b.level = a.level; b.rank = a.rank; b.slot = a.slot;
            b.attributes = a.attributes;
        }
        private static void Copy(DeliveryState a, DeliveryState b)
        {
            b.route = a.route; b.workerId = a.workerId; b.cargo = a.cargo;
            b.remainingSeconds = a.remainingSeconds; b.totalSeconds = a.totalSeconds;
            b.fetching = a.fetching; b.reservedCargo = a.reservedCargo;
        }
        private static void Copy(VipState a, VipState b)
        {
            b.id = a.id; b.cityId = a.cityId; b.kind = a.kind;
            b.target = a.target; b.reward = a.reward; b.completed = a.completed;
        }
        private static void Copy(CityState a, CityState b)
        {
            b.id = a.id; b.unlocked = a.unlocked; b.freeplay = a.freeplay;
            b.charterAccepted = a.charterAccepted; b.charterResidentMilestone = a.charterResidentMilestone; b.questIndex = a.questIndex;
            b.talked = a.talked; b.completedQuests = a.completedQuests; b.stats = a.stats;
            b.project = a.project; b.storyResidentMilestone = a.storyResidentMilestone;
            b.vipStories = a.vipStories; b.vipCollaborations = a.vipCollaborations;
        }
        private static void Copy(Statistics a, Statistics b)
        {
            b.earned = a.earned; b.rawProduced = a.rawProduced; b.refinedProduced = a.refinedProduced;
            b.butterProduced = a.butterProduced; b.pastryProduced = a.pastryProduced;
            b.coffeeSold = a.coffeeSold; b.pastrySold = a.pastrySold; b.delivered = a.delivered;
            b.playSeconds = a.playSeconds; b.manualHarvests = a.manualHarvests;
            b.deliveries = a.deliveries; b.spins = a.spins; b.townHallVisits = a.townHallVisits;
            b.dailyClaims = a.dailyClaims; b.vipCompleted = a.vipCompleted; b.workersReceived = a.workersReceived;
        }
        private static void Copy(CityProjectState a, CityProjectState b)
        {
            b.planId = a.planId; b.stepIndex = a.stepIndex;
            b.completedSteps = a.completedSteps; b.baselines = a.baselines;
        }
        private static void Copy(VipStoryState a, VipStoryState b)
        {
            b.id = a.id; b.visits = a.visits; b.nextRecurringSecond = a.nextRecurringSecond;
            b.recurringCompleted = a.recurringCompleted; b.lastRecurringId = a.lastRecurringId;
            b.lastRecurringChoice = a.lastRecurringChoice; b.lastWasRecurring = a.lastWasRecurring;
            b.completed = a.completed; b.active = a.active;
        }
        private static void Copy(VipCollaborationState a, VipCollaborationState b)
        { b.id = a.id; b.completed = a.completed; b.active = a.active; }
        private static void Copy(StoryCompletion a, StoryCompletion b)
        { b.questId = a.questId; b.choiceId = a.choiceId; }
        private static void Copy(ActiveStoryQuest a, ActiveStoryQuest b)
        {
            b.questId = a.questId; b.choiceId = a.choiceId; b.scale = a.scale;
            b.acceptedSecond = a.acceptedSecond; b.baselines = a.baselines; b.targets = a.targets;
        }
        private static void Copy(ItemStack a, ItemStack b)
        { b.id = a.id; b.count = a.count; }
        private static void Copy(RewardReceipt a, RewardReceipt b)
        {
            b.sequence = a.sequence; b.simulationSeconds = a.simulationSeconds;
            b.source = a.source; b.sourceId = a.sourceId; b.titleKey = a.titleKey;
            b.cityId = a.cityId; b.items = a.items;
        }
        private static void Copy(RewardItem a, RewardItem b)
        {
            b.kind = a.kind; b.key = a.key; b.reference = a.reference;
            b.quantity = a.quantity; b.rarity = a.rarity;
        }
        private static void Copy(LuckyWheelState a, LuckyWheelState b)
        {
            b.initialized = a.initialized; b.day = a.day; b.spins = a.spins;
            b.randomState = a.randomState; b.prizes = a.prizes;
        }
        private static void Copy(WheelPrize a, WheelPrize b)
        { b.id = a.id; b.count = a.count; b.claimed = a.claimed; }
        private static void Copy(ShopState a, ShopState b)
        { b.purchases = a.purchases; b.vipMembership = a.vipMembership; }
        private static void Copy(ShopPurchaseReceipt a, ShopPurchaseReceipt b)
        { b.transactionId = a.transactionId; b.productId = a.productId; b.channel = a.channel; b.utcTicks = a.utcTicks; }
        private static void Copy(VipMembershipState a, VipMembershipState b)
        { b.startedUtcTicks = a.startedUtcTicks; b.expiresUtcTicks = a.expiresUtcTicks; b.rewardedDays = a.rewardedDays; }
    }
}

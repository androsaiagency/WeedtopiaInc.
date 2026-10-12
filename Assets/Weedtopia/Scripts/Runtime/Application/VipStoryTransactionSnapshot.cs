using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    /// <summary>A failed disk write restores the complete scope of a story command, including its reward receipt.</summary>
    internal sealed class VipStoryTransactionSnapshot
    {
        private readonly Amount money, earned;
        private readonly long revision, rewardSequence, lastUtcTicks, highWaterUtcTicks;
        private readonly int milestone;
        private readonly CityState city;
        private readonly ItemStack[] inventory;
        private readonly RewardReceipt[] receipts;
        private readonly VipStoryState[] people;
        private readonly VipCollaborationState[] collaborations;
        public VipStoryTransactionSnapshot(GameState state, CityState target = null)
        {
            city = target ?? state.City;
            money = state.money; earned = city.stats.earned; revision = state.revision; rewardSequence = state.rewardSequence;
            milestone = city.storyResidentMilestone;
            lastUtcTicks = state.lastUtcTicks; highWaterUtcTicks = state.highWaterUtcTicks;
            inventory = state.inventory?.Select(i => new ItemStack { id = i.id, count = i.count }).ToArray();
            receipts = state.rewardHistory?.ToArray();
            people = city.vipStories?.Select(p => new VipStoryState { id = p.id, visits = p.visits, nextRecurringSecond = p.nextRecurringSecond,
                recurringCompleted = p.recurringCompleted, lastRecurringId = p.lastRecurringId, lastRecurringChoice = p.lastRecurringChoice,
                lastWasRecurring = p.lastWasRecurring,
                completed = p.completed.ToList(), active = Clone(p.active) }).ToArray();
            collaborations = city.vipCollaborations?.Select(p => new VipCollaborationState { id = p.id, completed = p.completed, active = Clone(p.active) }).ToArray();
        }
        private static ActiveStoryQuest Clone(ActiveStoryQuest a) => a == null ? null : new ActiveStoryQuest
        { questId = a.questId, choiceId = a.choiceId, scale = a.scale, acceptedSecond = a.acceptedSecond, baselines = a.baselines.ToArray(), targets = a.targets.ToArray() };
        public void Restore(GameState state)
        {
            state.money = money; city.stats.earned = earned; state.revision = revision; state.rewardSequence = rewardSequence;
            state.lastUtcTicks = lastUtcTicks; state.highWaterUtcTicks = highWaterUtcTicks;
            city.storyResidentMilestone = milestone; state.inventory = inventory?.ToList(); state.rewardHistory = receipts?.ToList();
            city.vipStories = people?.ToList(); city.vipCollaborations = collaborations?.ToList();
        }
    }
}

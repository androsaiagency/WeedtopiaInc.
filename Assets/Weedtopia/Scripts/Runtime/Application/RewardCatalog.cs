using System.Collections.Generic;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    /// <summary>Queries live inventory and future rewards without spending currency or advancing RNG.</summary>
    public static class RewardCatalog
    {
        public static List<RewardItem> Preview(GameSession session)
        {
            var quest = session.Progression.Current;
            var step = session.CityProject.Current;
            var rewards = RewardHistory.Currency(quest?.moneyReward ?? step?.moneyReward ?? 0, quest?.cardsReward ?? step?.cardsReward ?? 0);
            foreach (var role in quest?.workerRewards ?? step?.workerRewards ?? System.Array.Empty<string>())
                rewards.Add(RewardItem.Of(RewardKind.Worker, 1, role));
            foreach (var stack in quest?.itemRewards ?? step?.itemRewards ?? System.Array.Empty<ItemStack>())
                rewards.Add(RewardItem.Of(RewardKind.Item, stack.count, stack.id));
            if (quest != null)
            {
                if (quest.unlock > session.State.City.unlocked) rewards.Add(RewardItem.Of(RewardKind.District, 1, quest.unlock.ToString()));
                if (session.State.City.questIndex == session.Progression.Quests.Length - 1) rewards.Add(RewardItem.Of(RewardKind.Title, 1, "founder"));
            }
            if (step != null && session.State.City.project.stepIndex == session.CityProject.Plan.steps.Length - 1)
            {
                rewards.Add(RewardItem.Of(RewardKind.Title, 1, "festival"));
                rewards.Add(RewardItem.Of(RewardKind.SalesBonus, new Amount(session.CityProject.Plan.salesBonus * 100), session.CityProject.Plan.bonusFamily));
            }
            return rewards;
        }
        public static List<RewardItem> Inventory(GameState state)
        {
            var result = new List<RewardItem>();
            if (state.cards > 0) result.Add(RewardItem.Of(RewardKind.Cards, state.cards));
            result.AddRange((state.inventory ?? new List<ItemStack>()).Where(s => s.count > 0).Select(s => RewardItem.Of(RewardKind.Item, s.count, s.id)));
            result.AddRange(state.workers.OrderByDescending(w => w.rarity).ThenBy(w => w.id).Select(RewardItem.ForWorker));
            return result;
        }
        public static List<RewardItem> Achievements(GameSession session)
        {
            var result = new List<RewardItem>();
            if (session.State.City.freeplay) result.Add(RewardItem.Of(RewardKind.Title, 1, "founder"));
            if (session.CityProject.Completed)
            {
                result.Add(RewardItem.Of(RewardKind.Title, 1, "festival"));
                result.Add(RewardItem.Of(RewardKind.SalesBonus, new Amount(session.CityProject.Plan.salesBonus * 100), session.CityProject.Plan.bonusFamily));
            }
            return result;
        }
    }
}

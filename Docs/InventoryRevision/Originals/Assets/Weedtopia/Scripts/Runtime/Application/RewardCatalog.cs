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
            if (quest != null)
            {
                if (quest.unlock > session.State.City.unlocked) rewards.Add(RewardItem.Of(RewardKind.District, 1, quest.unlock.ToString()));
                if (session.State.City.questIndex == session.Definitions.quests.Length - 1) rewards.Add(RewardItem.Of(RewardKind.Title, 1, "founder"));
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
            var result = new List<RewardItem> { RewardItem.Of(RewardKind.Money, state.money),
                RewardItem.Of(RewardKind.Grinders, state.grinders), RewardItem.Of(RewardKind.Cards, state.cards) };
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

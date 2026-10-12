using System;
using System.Collections.Generic;
using System.Linq;

namespace Weedtopia.Domain
{
    public enum StoryGoalKind { Level, Residents, NpcVisited, RawProduced, RefinedProduced, CoffeeSold, ButterProduced, PastrySold, DeliveredGoods, Deliveries }

    [Serializable] public sealed class StoryGoal
    {
        public StoryGoalKind kind;
        public string target, textKey;
        public int count = 1;
        public bool IsCounter => kind >= StoryGoalKind.NpcVisited;
    }

    [Serializable] public sealed class StoryChoice
    {
        public string id, textKey, outcomeKey;
    }

    [Serializable] public sealed class StoryQuest
    {
        public string id, titleKey, bodyKey, completedKey, memoryKey;
        public int residents, moneyReward;
        public Zone zone = Zone.City;
        public StoryGoal[] goals = Array.Empty<StoryGoal>();
        public StoryChoice[] choices = Array.Empty<StoryChoice>();
        public string[] prerequisites = Array.Empty<string>();
        public ItemStack[] itemRewards = Array.Empty<ItemStack>();
    }

    [Serializable] public sealed class VipStoryDefinition
    {
        public string id, cityId, nameKey, roleKey, biographyKey, greetingKey, focus;
        public int residents, repeatDelaySeconds = 300;
        public StoryQuest[] chapters = Array.Empty<StoryQuest>(), recurring = Array.Empty<StoryQuest>();
    }

    [Serializable] public sealed class VipCollaborationDefinition
    {
        public string id, cityId, host;
        public string[] participants = Array.Empty<string>();
        public StoryQuest quest;
    }

    [Serializable] public sealed class StoryCompletion
    {
        public string questId, choiceId;
    }

    [Serializable] public sealed class ActiveStoryQuest
    {
        public string questId, choiceId;
        public Amount[] baselines = Array.Empty<Amount>();
        public int[] targets = Array.Empty<int>();
        public int scale = 1;
        public long acceptedSecond;
    }

    [Serializable] public sealed class VipStoryState
    {
        public string id;
        public long visits, nextRecurringSecond;
        public int recurringCompleted;
        public string lastRecurringId, lastRecurringChoice;
        public bool lastWasRecurring;
        public List<StoryCompletion> completed = new List<StoryCompletion>();
        public ActiveStoryQuest active;
    }

    [Serializable] public sealed class VipCollaborationState
    {
        public string id;
        public StoryCompletion completed;
        public ActiveStoryQuest active;
    }

    public static class VipStoryRules
    {
        public const int ActiveLimit = 3;
        public static int RecurringScale(int residents) => Math.Min(10, 1 + Math.Max(0, residents) / 1000);
        public static int GoalTarget(StoryGoal goal, bool recurring, int scale) =>
            checked(goal.count * (recurring && goal.IsCounter && goal.kind != StoryGoalKind.NpcVisited ? scale : 1));
        public static IEnumerable<StoryQuest> Quests(GameDefinitions defs) =>
            (defs.vipStories ?? Array.Empty<VipStoryDefinition>()).SelectMany(v => v.chapters.Concat(v.recurring))
            .Concat((defs.vipCollaborations ?? Array.Empty<VipCollaborationDefinition>()).Select(c => c.quest));

        public static Amount Value(GameDefinitions defs, GameState state, Simulation simulation, StoryGoal goal, string cityId)
        {
            var city = state.cities.Single(c => c.id == cityId);
            switch (goal.kind)
            {
                case StoryGoalKind.Level:
                    return state.buildings.Where(b => b.cityId == cityId && (b.id == goal.target || b.family == goal.target)).Select(b => b.level).DefaultIfEmpty(0).Max();
                case StoryGoalKind.Residents: return simulation.Residents(cityId);
                case StoryGoalKind.NpcVisited: return new Amount(city.vipStories?.FirstOrDefault(v => v.id == goal.target)?.visits ?? 0);
                case StoryGoalKind.RawProduced: return city.stats.rawProduced;
                case StoryGoalKind.RefinedProduced: return city.stats.refinedProduced;
                case StoryGoalKind.CoffeeSold: return city.stats.coffeeSold;
                case StoryGoalKind.ButterProduced: return city.stats.butterProduced;
                case StoryGoalKind.PastrySold: return city.stats.pastrySold;
                case StoryGoalKind.DeliveredGoods: return city.stats.delivered;
                case StoryGoalKind.Deliveries: return city.stats.deliveries;
                default: throw new ArgumentOutOfRangeException(nameof(goal.kind));
            }
        }

        public static void ValidateDefinitions(GameDefinitions defs)
        {
            var people = defs.vipStories ?? Array.Empty<VipStoryDefinition>();
            var joint = defs.vipCollaborations ?? Array.Empty<VipCollaborationDefinition>();
            void Require(bool condition, string detail) { if (!condition) throw new InvalidOperationException("Invalid VIP story: " + detail); }
            void Text(string key) { Require(!string.IsNullOrWhiteSpace(key) && defs.texts.Any(t => t.key == key && !string.IsNullOrWhiteSpace(t.de)), "text " + key); }
            Require(people.All(v => v != null && !string.IsNullOrWhiteSpace(v.id)) && people.Select(v => v.id).Distinct().Count() == people.Length, "people");
            Require(joint.All(v => v != null && !string.IsNullOrWhiteSpace(v.id) && v.quest != null) && joint.Select(v => v.id).Distinct().Count() == joint.Length, "collaborations");
            Require(!people.Select(p => p.id).Intersect(joint.Select(c => c.id)).Any(), "identity collision");
            var all = Quests(defs).ToArray();
            Require(all.All(q => q != null && !string.IsNullOrWhiteSpace(q.id)) && all.Select(q => q.id).Distinct().Count() == all.Length, "quest IDs");
            var chapters = people.SelectMany(p => p.chapters).ToArray();
            foreach (var person in people)
            {
                Require(person.residents >= 0 && person.repeatDelaySeconds >= 60 && person.repeatDelaySeconds <= 86400, person.id);
                Require(defs.instances.Any(i => i.cityId == person.cityId && i.id == person.focus), "focus " + person.id);
                foreach (string key in new[] { person.nameKey, person.roleKey, person.biographyKey, person.greetingKey }) Text(key);
                Require(person.chapters != null && person.chapters.Length > 0 && person.chapters.Length <= 32 && person.recurring != null && person.recurring.Length > 0, "chapter pool " + person.id);
                int previous = person.residents - 1;
                foreach (var q in person.chapters) { Require(q.residents > previous && !string.IsNullOrEmpty(q.memoryKey), "chapter order " + person.id); previous = q.residents; }
                foreach (var q in person.chapters.Concat(person.recurring))
                {
                    Require(q.residents >= person.residents, "early quest " + q.id);
                    ValidateQuest(q, person.cityId, person.recurring.Contains(q));
                }
            }
            foreach (var collaboration in joint)
            {
                Require(collaboration.participants != null && collaboration.participants.Length >= 2 && collaboration.participants.Distinct().Count() == collaboration.participants.Length, "participants");
                Require(collaboration.participants.Contains(collaboration.host) && collaboration.participants.All(id => people.Any(p => p.id == id && p.cityId == collaboration.cityId && p.residents <= collaboration.quest.residents)), "host/participants");
                Require(!string.IsNullOrEmpty(collaboration.quest.memoryKey), "joint memory");
                ValidateQuest(collaboration.quest, collaboration.cityId, false);
            }
            void ValidateQuest(StoryQuest q, string city, bool recurring)
            {
                Require(q.residents >= 0 && q.residents <= 1000000 && Enum.IsDefined(typeof(Zone), q.zone) && q.moneyReward >= 0 && q.moneyReward <= 1000000, q.id);
                foreach (string key in new[] { q.titleKey, q.bodyKey, q.completedKey }) Text(key);
                if (!string.IsNullOrEmpty(q.memoryKey)) Text(q.memoryKey);
                Require(!recurring || string.IsNullOrEmpty(q.memoryKey), "repeatable memory " + q.id);
                Require(q.choices != null && q.choices.Length >= 1 && q.choices.Length <= 2 && q.choices.All(c => c != null && !string.IsNullOrWhiteSpace(c.id)) && q.choices.Select(c => c.id).Distinct().Count() == q.choices.Length, "choices " + q.id);
                foreach (var choice in q.choices) { Text(choice.textKey); Text(choice.outcomeKey); }
                Require(q.prerequisites != null && q.prerequisites.Distinct().Count() == q.prerequisites.Length && q.prerequisites.All(id => chapters.Any(c => c.id == id && c.residents <= q.residents && id != q.id)), "prerequisites " + q.id);
                InventoryItems.ValidateGrants(defs, q.itemRewards);
                Require(q.goals != null && q.goals.Length > 0 && q.goals.Length <= 6 && q.goals.All(g => g != null), "goals " + q.id);
                Require(q.goals.Any(g => g.IsCounter), "fresh activity " + q.id);
                foreach (var goal in q.goals)
                {
                    Require(Enum.IsDefined(typeof(StoryGoalKind), goal.kind) && goal.count > 0 && goal.count <= 1000000, "goal " + q.id); Text(goal.textKey);
                    if (goal.kind == StoryGoalKind.NpcVisited) Require(goal.count == 1 && people.Any(p => p.id == goal.target && p.cityId == city && p.residents <= q.residents), "visit " + q.id);
                    if (goal.kind == StoryGoalKind.Level) Require(goal.count <= 5000 && defs.instances.Any(i => i.cityId == city && (i.id == goal.target || i.family == goal.target || (i.family == "settlement_plot" && (goal.target == "park" || goal.target == "residential_house")))), "building " + q.id);
                }
            }
        }
    }
}

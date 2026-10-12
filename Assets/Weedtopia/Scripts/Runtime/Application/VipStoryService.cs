using System;
using System.Collections.Generic;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    /// <summary>Persistent inhabitants and optional quests. Counters are captured only when an offer is accepted.</summary>
    public sealed class VipStoryService
    {
        private readonly GameDefinitions defs;
        private readonly GameState state;
        private readonly Simulation simulation;
        public VipStoryService(GameDefinitions definitions, GameState game, Simulation sim)
        { defs = definitions; state = game; simulation = sim; }
        public IEnumerable<VipStoryDefinition> People => (defs.vipStories ?? Array.Empty<VipStoryDefinition>()).Where(p => p.cityId == state.activeCity);
        public IEnumerable<VipCollaborationDefinition> Collaborations => (defs.vipCollaborations ?? Array.Empty<VipCollaborationDefinition>()).Where(p => p.cityId == state.activeCity);
        public VipStoryState Person(string id) => state.City.vipStories?.FirstOrDefault(p => p.id == id);
        public VipCollaborationState Joint(string id) => state.City.vipCollaborations?.FirstOrDefault(p => p.id == id);
        public bool Unlocked(string id) => Person(id) != null;
        public int ActiveCount => (state.City.vipStories?.Count(p => p.active != null) ?? 0) + (state.City.vipCollaborations?.Count(p => p.active != null) ?? 0);
        public bool HasCompleted(string questId) => state.City.vipStories?.Any(p => p.completed.Any(c => c.questId == questId)) == true;
        public ActiveStoryQuest Active(string id) => Person(id)?.active ?? Joint(id)?.active;
        public StoryQuest Definition(string questId) => VipStoryRules.Quests(defs).FirstOrDefault(q => q.id == questId);
        public StoryQuest ActiveDefinition(string id) => Definition(Active(id)?.questId);
        public void RefreshUnlocks()
        {
            if (!state.introSeen) return;
            int residents = simulation.Residents(state.activeCity);
            bool changed = false;
            if (residents > state.City.storyResidentMilestone) { state.City.storyResidentMilestone = residents; changed = true; }
            foreach (var person in People.Where(p => p.residents <= state.City.storyResidentMilestone))
            {
                if (Unlocked(person.id)) continue;
                state.City.vipStories ??= new List<VipStoryState>();
                state.City.vipStories.Add(new VipStoryState { id = person.id }); changed = true;
            }
            if (changed) state.revision++;
        }
        public Failure Visit(string id)
        {
            if (!state.introSeen || Person(id) == null) return Failure.Locked;
            var person = Person(id);
            if (person.visits >= long.MaxValue - 1) return Failure.Invalid;
            person.visits++; state.revision++; return Failure.None;
        }
        private bool Eligible(StoryQuest quest) => quest != null && state.City.storyResidentMilestone >= quest.residents &&
            state.City.unlocked >= quest.zone && quest.prerequisites.All(HasCompleted);
        public StoryQuest NextChapter(string id)
        {
            var person = Person(id); var definition = People.FirstOrDefault(p => p.id == id);
            return person != null && definition != null && person.completed.Count < definition.chapters.Length ? definition.chapters[person.completed.Count] : null;
        }
        public StoryQuest Offer(string id)
        {
            if (!state.introSeen || Active(id) != null) return null;
            var definition = People.FirstOrDefault(p => p.id == id); var person = Person(id);
            if (definition != null && person != null)
            {
                var next = NextChapter(id);
                if (Eligible(next)) return next;
                if (person.completed.Count == 0 || state.simulationSeconds < person.nextRecurringSecond) return null;
                var pool = definition.recurring.Where(Eligible).ToArray();
                return pool.Length > 0 ? pool[person.recurringCompleted % pool.Length] : null;
            }
            var joint = Collaborations.FirstOrDefault(j => j.id == id);
            return joint != null && Joint(id)?.completed == null && joint.participants.All(Unlocked) && Eligible(joint.quest) ? joint.quest : null;
        }
        public Failure Accept(string id, string questId, string choiceId)
        {
            var offer = Offer(id);
            if (offer == null || offer.id != questId) return Failure.StaleOffer;
            if (ActiveCount >= VipStoryRules.ActiveLimit) return Failure.Busy;
            if (!offer.choices.Any(c => c.id == choiceId)) return Failure.Invalid;
            bool recurring = People.Any(p => p.id == id && p.recurring.Contains(offer));
            int scale = recurring ? VipStoryRules.RecurringScale(state.City.storyResidentMilestone) : 1;
            var active = new ActiveStoryQuest { questId = questId, choiceId = choiceId, scale = scale, acceptedSecond = state.simulationSeconds,
                targets = offer.goals.Select(g => VipStoryRules.GoalTarget(g, recurring, scale)).ToArray(),
                baselines = offer.goals.Select(g => g.IsCounter ? VipStoryRules.Value(defs, state, simulation, g, state.activeCity) : Amount.Zero).ToArray() };
            if (Person(id) != null) Person(id).active = active;
            else
            {
                state.City.vipCollaborations ??= new List<VipCollaborationState>();
                var joint = Joint(id);
                if (joint == null) { joint = new VipCollaborationState { id = id }; state.City.vipCollaborations.Add(joint); }
                joint.active = active;
            }
            state.revision++; return Failure.None;
        }
        public Amount Progress(string id, int index)
        {
            var active = Active(id); var quest = ActiveDefinition(id);
            if (active == null || quest == null || index < 0 || index >= quest.goals.Length) return Amount.Zero;
            return Amount.Min(active.targets[index], Amount.Max(0, VipStoryRules.Value(defs, state, simulation, quest.goals[index], state.activeCity) - active.baselines[index]));
        }
        public bool Ready(string id) => state.introSeen && ActiveDefinition(id) is StoryQuest q && q.goals.Select((g, i) => Progress(id, i) >= Active(id).targets[i]).All(x => x);
        public Failure Claim(string id, string questId)
        {
            var active = Active(id); var quest = ActiveDefinition(id);
            if (active == null || quest?.id != questId || !Ready(id)) return Failure.NotReady;
            var rewards = RewardHistory.Currency(checked(quest.moneyReward * active.scale));
            foreach (var grant in quest.itemRewards ?? Array.Empty<ItemStack>())
                if (InventoryItems.Count(state, grant.id) > int.MaxValue - grant.count) return Failure.Invalid;
            InventoryItems.Grant(state, quest.itemRewards, rewards);
            state.money += checked(quest.moneyReward * active.scale); state.City.stats.earned += checked(quest.moneyReward * active.scale);
            var completion = new StoryCompletion { questId = quest.id, choiceId = active.choiceId };
            var person = Person(id);
            if (person != null)
            {
                var definition = People.Single(p => p.id == id);
                if (definition.chapters.Contains(quest)) { person.completed.Add(completion); person.lastWasRecurring = false; }
                else
                {
                    person.recurringCompleted = checked(person.recurringCompleted + 1);
                    person.lastRecurringId = quest.id; person.lastRecurringChoice = active.choiceId;
                    person.lastWasRecurring = true;
                    person.nextRecurringSecond = checked(state.simulationSeconds + definition.repeatDelaySeconds);
                }
                person.active = null;
            }
            else { var joint = Joint(id); joint.completed = completion; joint.active = null; }
            if (!string.IsNullOrEmpty(quest.memoryKey)) rewards.Add(RewardItem.Of(RewardKind.StoryMemory, 1, quest.memoryKey));
            RewardHistory.Record(state, RewardSource.VipStory, quest.id, quest.titleKey, rewards);
            state.revision++; return Failure.None;
        }
        public StoryCompletion LastCompletion(string id)
        {
            var person = Person(id);
            if (person == null) return Joint(id)?.completed;
            if (person.lastWasRecurring) return new StoryCompletion { questId = person.lastRecurringId, choiceId = person.lastRecurringChoice };
            return person.completed.LastOrDefault();
        }
    }
}

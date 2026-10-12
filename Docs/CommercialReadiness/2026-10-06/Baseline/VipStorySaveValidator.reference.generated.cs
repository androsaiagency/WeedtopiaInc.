using System;
using System.IO;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Persistence.Reference
{
    internal static class VipStorySaveValidator
    {
        // JsonUtility writes null inline classes as empty objects. Restore only the
        // exact empty representation; partially populated objects remain invalid.
        public static void RestoreSerializedNulls(GameState state)
        {
            bool Empty(ActiveStoryQuest active) => active != null &&
                string.IsNullOrEmpty(active.questId) && string.IsNullOrEmpty(active.choiceId) &&
                (active.baselines == null || active.baselines.Length == 0) &&
                (active.targets == null || active.targets.Length == 0) &&
                active.acceptedSecond == 0 && (active.scale == 0 || active.scale == 1);
            foreach (var city in state.cities ?? new System.Collections.Generic.List<CityState>())
            {
                if (city == null) continue;
                foreach (var person in city.vipStories ?? new System.Collections.Generic.List<VipStoryState>())
                    if (person != null && Empty(person.active)) person.active = null;
                foreach (var joint in city.vipCollaborations ?? new System.Collections.Generic.List<VipCollaborationState>())
                {
                    if (joint == null) continue;
                    if (Empty(joint.active)) joint.active = null;
                    if (joint.completed != null && string.IsNullOrEmpty(joint.completed.questId) && string.IsNullOrEmpty(joint.completed.choiceId)) joint.completed = null;
                }
            }
        }

        public static void Validate(GameState state, GameDefinitions defs)
        {
            var sim = new Simulation(defs, state);
            void Require(bool condition) { if (!condition) throw new InvalidDataException("Invalid VIP story progress"); }
            foreach (var city in state.cities)
            {
                Require(city.storyResidentMilestone >= 0 && city.storyResidentMilestone <= 1000000);
                var people = city.vipStories ?? new System.Collections.Generic.List<VipStoryState>();
                var joint = city.vipCollaborations ?? new System.Collections.Generic.List<VipCollaborationState>();
                Require(people.All(p => p != null) && people.Select(p => p.id).Distinct().Count() == people.Count);
                Require(joint.All(p => p != null) && joint.Select(p => p.id).Distinct().Count() == joint.Count);
                Require(people.Count(p => p.active != null) + joint.Count(p => p.active != null) <= VipStoryRules.ActiveLimit);
                foreach (var progress in people)
                {
                    var person = (defs.vipStories ?? Array.Empty<VipStoryDefinition>()).FirstOrDefault(p => p.id == progress.id && p.cityId == city.id);
                    Require(person != null && person.residents <= city.storyResidentMilestone);
                    Require(progress.visits >= 0 && progress.visits < long.MaxValue && progress.recurringCompleted >= 0 && progress.recurringCompleted < int.MaxValue);
                    Require(progress.completed != null && progress.completed.Count <= person.chapters.Length && progress.completed.All(c => c != null));
                    Require(progress.completed.Select(c => c.questId).SequenceEqual(person.chapters.Take(progress.completed.Count).Select(q => q.id)));
                    foreach (var completion in progress.completed) ValidateCompletion(completion, person.chapters.Single(q => q.id == completion.questId));
                    Require(progress.nextRecurringSecond >= 0 && progress.nextRecurringSecond <= state.simulationSeconds + person.repeatDelaySeconds);
                    if (progress.recurringCompleted == 0)
                        Require(progress.nextRecurringSecond == 0 && !progress.lastWasRecurring && string.IsNullOrEmpty(progress.lastRecurringId) && string.IsNullOrEmpty(progress.lastRecurringChoice));
                    else
                    {
                        Require(progress.completed.Count > 0);
                        var last = person.recurring.FirstOrDefault(q => q.id == progress.lastRecurringId);
                        Require(last != null);
                        ValidateCompletion(new StoryCompletion { questId = progress.lastRecurringId, choiceId = progress.lastRecurringChoice }, last);
                    }
                    if (progress.active != null)
                    {
                        var quest = person.chapters.Concat(person.recurring).FirstOrDefault(q => q.id == progress.active.questId);
                        Require(quest != null);
                        bool recurring = person.recurring.Contains(quest);
                        Require(recurring ? progress.completed.Count > 0 && progress.active.acceptedSecond >= progress.nextRecurringSecond :
                            progress.completed.Count < person.chapters.Length && quest == person.chapters[progress.completed.Count]);
                        ValidateActive(progress.active, quest, recurring);
                    }
                }
                foreach (var progress in joint)
                {
                    var definition = (defs.vipCollaborations ?? Array.Empty<VipCollaborationDefinition>()).FirstOrDefault(p => p.id == progress.id && p.cityId == city.id);
                    Require(definition != null && definition.participants.All(id => people.Any(p => p.id == id)));
                    Require((progress.completed == null) != (progress.active == null));
                    if (progress.completed != null) ValidateCompletion(progress.completed, definition.quest);
                    if (progress.active != null) ValidateActive(progress.active, definition.quest, false);
                }
                void ValidateCompletion(StoryCompletion completion, StoryQuest quest)
                {
                    Require(completion.questId == quest.id && quest.residents <= city.storyResidentMilestone && quest.choices.Any(c => c.id == completion.choiceId));
                }
                void ValidateActive(ActiveStoryQuest active, StoryQuest quest, bool recurring)
                {
                    Require(quest.residents <= city.storyResidentMilestone && quest.choices.Any(c => c.id == active.choiceId));
                    Require(active.acceptedSecond >= 0 && active.acceptedSecond <= state.simulationSeconds);
                    Require(active.scale >= 1 && active.scale <= 10 && (recurring || active.scale == 1));
                    Require(active.baselines != null && active.targets != null && active.baselines.Length == quest.goals.Length && active.targets.Length == quest.goals.Length);
                    Require(quest.prerequisites.All(id => people.Any(p => p.completed.Any(c => c.questId == id))));
                    for (int i = 0; i < quest.goals.Length; i++)
                    {
                        var goal = quest.goals[i]; var baseline = active.baselines[i];
                        Require(baseline.IsValid && baseline >= 0 && (!goal.IsCounter ? baseline == 0 : baseline <= VipStoryRules.Value(defs, state, sim, goal, city.id)));
                        Require(active.targets[i] == VipStoryRules.GoalTarget(goal, recurring, active.scale));
                    }
                }
            }
        }
    }
}

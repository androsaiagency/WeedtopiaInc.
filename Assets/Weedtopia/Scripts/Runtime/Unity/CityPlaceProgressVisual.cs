using System;
using System.Linq;
using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Shows serialized place stages and saved story choices without constructing scene objects.</summary>
    public sealed class CityPlaceProgressVisual : MonoBehaviour
    {
        public GameRoot root;
        public string cityId, collaborationId, choiceResidentId;
        public int residents, choiceChapter;
        public int mainQuestIndex = -1;
        public GameObject closedPlace, growingPlace, completedPlace;
        public bool keepGrowingPlaceAfterCompletion = true;
        public string[] choiceValues = Array.Empty<string>();
        public GameObject[] choicePlaces = Array.Empty<GameObject>();
        private long revision = long.MinValue;
        private object session;

        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            var state = root?.Session?.State;
            if (state == null || state.activeCity != cityId) return;
            if (ReferenceEquals(session, root.Session) && revision == state.revision) return;
            session = root.Session; revision = state.revision;
            bool grown = state.City.storyResidentMilestone >= residents;
            bool completed = (mainQuestIndex >= 0 && state.City.questIndex >= mainQuestIndex) ||
                (!string.IsNullOrEmpty(collaborationId) && state.City.vipCollaborations.Any(c => c.id == collaborationId && c.completed != null));
            Set(closedPlace, !grown && !completed);
            Set(growingPlace, (grown && !completed) || (completed && keepGrowingPlaceAfterCompletion));
            Set(completedPlace, completed);
            var person = state.City.vipStories.FirstOrDefault(p => p.id == choiceResidentId);
            string choice = choiceChapter >= 0 && person != null && person.completed.Count > choiceChapter ? person.completed[choiceChapter].choiceId : null;
            for (int index = 0; index < (choicePlaces?.Length ?? 0); index++)
                Set(choicePlaces[index], completed && index < (choiceValues?.Length ?? 0) &&
                    !string.IsNullOrEmpty(choice) && string.Equals(choice, choiceValues[index], StringComparison.Ordinal));
        }
        private static void Set(GameObject target, bool visible)
        { if (target != null && target.activeSelf != visible) target.SetActive(visible); }
    }
}

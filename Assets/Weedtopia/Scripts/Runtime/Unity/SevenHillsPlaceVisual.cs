using System;
using System.Linq;
using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Shows authored place stages from saved resident/project progress; never creates scene objects.</summary>
    public sealed class SevenHillsPlaceVisual : MonoBehaviour
    {
        public GameRoot root;
        public int residents;
        public string collaborationId;
        public GameObject closedPlace, growingPlace, completedPlace, quietPlace, livelyPlace;
        private long revision = long.MinValue;
        private object session;
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            var state = root?.Session?.State;
            if (state == null || state.activeCity != "seven_hills") return;
            if (ReferenceEquals(session, root.Session) && revision == state.revision) return;
            session = root.Session; revision = state.revision;
            bool grown = state.City.storyResidentMilestone >= residents;
            bool completed = state.City.vipCollaborations.Any(c => c.id == collaborationId && c.completed != null);
            Set(closedPlace, !grown && !completed); Set(growingPlace, grown || completed); Set(completedPlace, completed);
            // Safiya's saved public-terrace choice belongs to her third personal chapter.
            var safiya = state.City.vipStories.FirstOrDefault(s => s.id == "sh_safiya");
            string choice = safiya != null && safiya.completed.Count > 2 ? safiya.completed[2].choiceId : null;
            Set(quietPlace, completed && string.Equals(choice, "quiet", StringComparison.Ordinal));
            Set(livelyPlace, completed && string.Equals(choice, "lively", StringComparison.Ordinal));
        }
        private static void Set(GameObject target, bool visible)
        { if (target != null && target.activeSelf != visible) target.SetActive(visible); }
    }
}

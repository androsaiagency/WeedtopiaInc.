using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    /// <summary>Localized read-only chapter presentation shared by the existing orders UI.</summary>
    public static class CityProjectText
    {
        public static string Title(GameSession session)
        {
            var project = session.CityProject; var defs = session.Definitions;
            if (project.Completed) return defs.Text("project.finished_title");
            if (project.Current == null) return defs.Text(project.Definition.titleKey);
            return string.Format(defs.Text("project.step_title"), session.State.City.project.stepIndex + 1,
                project.Plan.steps.Length, defs.Text(project.Current.titleKey));
        }

        public static string Body(GameSession session)
        {
            var project = session.CityProject; var defs = session.Definitions;
            if (project.Completed) return defs.Text(project.Definition.completedKey) + "\n" + defs.Text(project.Plan.rewardKey);
            if (project.Current == null) return defs.Text(project.Definition.invitationKey);
            return defs.Text("speaker." + project.Current.speaker) + "\n" + defs.Text(project.Current.bodyKey);
        }

        public static string Goal(GameSession session, int index)
        {
            var project = session.CityProject; var goal = project.Current.goals[index];
            return string.Format(session.Definitions.Text("project.goal_progress"), session.Definitions.Text(goal.textKey), project.Progress(index), goal.count);
        }

        public static string Reward(GameDefinitions defs, ProjectStep step)
        {
            string reward = string.Format(defs.Text("project.step_reward"), step.moneyReward, step.cardsReward);
            if (step.workerRewards.Length > 0)
                reward += "\n" + string.Join(", ", step.workerRewards.Select(r => defs.Text(defs.Role(r).nameKey)));
            return reward;
        }
    }
}

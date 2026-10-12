using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    /// <summary>Uses only serialized panels, rows, and buttons. No fixed controls are created at runtime.</summary>
    public sealed class VipStoriesUi : MonoBehaviour
    {
        public VipVisualCatalog visuals;
        public VipStoryRosterRow[] rows;
        public Image portrait;
        public Text heading, subtitle, milestone, nameLabel, role, biography, greeting, questTitle, description, reward, memories, hint;
        public Button peopleTab, togetherTab, accept, alternate, claim, speak, focus, remember;
        public Text acceptLabel, alternateLabel;
        public Button[] goals;
        public Text[] goalLabels;
        public ScrollRect rosterScroll, detailScroll;
        private GameRoot root;
        private string selected;
        private bool joint;
        private long lastRevision = -1, lastSecond = -1;
        public string SelectedId => selected;
        public bool JointTab => joint;
        public bool ShowingDetails { get; private set; }
        private string T(string key) => root.Definitions.Text(key);
        public void Initialize(GameRoot value)
        {
            root = value;
            var layout = GetComponent<VipStoryPanelLayout>();
            if (layout != null) layout.back.onClick.AddListener(BackToRoster);
            RefreshStaticLabels();
            peopleTab.onClick.AddListener(() => Tab(false)); togetherTab.onClick.AddListener(() => Tab(true));
            foreach (var row in rows) { var captured = row; row.button.onClick.AddListener(() => Select(captured.id, captured.collaboration, true)); }
            accept.onClick.AddListener(() => Accept(0)); alternate.onClick.AddListener(() => Accept(1));
            claim.onClick.AddListener(() => { var quest = root.Session.Stories.ActiveDefinition(selected); if (quest != null) root.StoryCommand(() => root.Session.Stories.Claim(selected, quest.id)); Paint(true); });
            speak.onClick.AddListener(() => { string id = Host(); if (id != null) root.StoryCommand(() => root.Session.Stories.Visit(id), false); Paint(true); });
            focus.onClick.AddListener(() => { string id = Host(); root.Ui.Close(); root.StoryWorld?.Focus(id); });
            remember.onClick.AddListener(() => { Canvas.ForceUpdateCanvases(); detailScroll.verticalNormalizedPosition = 0; });
            for (int i = 0; i < goals.Length; i++)
            {
                int index = i; goals[i].onClick.AddListener(() => FollowGoal(index));
            }
        }
        public void Open(string id = null)
        {
            bool explicitSelection = id != null;
            bool isJoint = id != null && root.Session.Stories.Collaborations.Any(c => c.id == id);
            if (id == null) id = selected ?? root.Session.Stories.People.FirstOrDefault(p => root.Session.Stories.Unlocked(p.id))?.id ?? root.Session.Stories.People.FirstOrDefault()?.id;
            if (id != null) Select(id, isJoint || (id == selected && joint), explicitSelection);
            else Paint(true);
            ShowingDetails = explicitSelection;
            FitRoute();
        }
        private void Tab(bool value)
        {
            joint = value; string id = value ? root.Session.Stories.Collaborations.FirstOrDefault()?.id : root.Session.Stories.People.FirstOrDefault()?.id;
            if (id != null) Select(id, value, false); else Paint(true);
            rosterScroll.verticalNormalizedPosition = 1;
            BackToRoster();
        }
        public void Select(string id, bool collaboration = false, bool visit = true)
        {
            selected = id; joint = collaboration;
            ShowingDetails = visit;
            if (visit && !joint && root.Session.Stories.Unlocked(id)) root.StoryCommand(() => root.Session.Stories.Visit(id), false);
            Paint(true); FitRoute(); Canvas.ForceUpdateCanvases(); detailScroll.verticalNormalizedPosition = 1;
        }
        private void BackToRoster()
        {
            ShowingDetails = false; FitRoute(); Canvas.ForceUpdateCanvases();
            rosterScroll.StopMovement(); rosterScroll.verticalNormalizedPosition = 1;
        }
        private void FitRoute()
        {
            var layout = GetComponent<VipStoryPanelLayout>();
            if (layout != null) layout.Fit(UiDisplayMetrics.Scale(GetComponentInParent<Canvas>()));
        }
        private string Host() => joint ? root.Session.Stories.Collaborations.FirstOrDefault(c => c.id == selected)?.host : selected;
        private void Accept(int index)
        {
            var offer = root.Session.Stories.Offer(selected);
            if (offer == null || index >= offer.choices.Length) return;
            root.StoryCommand(() => root.Session.Stories.Accept(selected, offer.id, offer.choices[index].id)); Paint(true);
        }
        private void FollowGoal(int index)
        {
            var quest = root.Session.Stories.ActiveDefinition(selected) ?? root.Session.Stories.Offer(selected);
            if (quest == null || index >= quest.goals.Length) return;
            var goal = quest.goals[index];
            if (goal.kind == StoryGoalKind.NpcVisited) { Select(goal.target, false, true); return; }
            if (goal.kind == StoryGoalKind.Level)
            {
                var building = root.Session.State.buildings.FirstOrDefault(b => b.cityId == root.Session.State.activeCity && (b.id == goal.target || b.family == goal.target));
                if (building != null) { root.Ui.Close(); root.Focus(building.id); }
            }
        }
        private void RefreshStaticLabels()
        {
            heading.text = T("story.title"); subtitle.text = T("story.subtitle");
            peopleTab.GetComponentInChildren<Text>(true).text = T("story.people"); togetherTab.GetComponentInChildren<Text>(true).text = T("story.together");
            claim.GetComponentInChildren<Text>(true).text = T("story.claim"); speak.GetComponentInChildren<Text>(true).text = T("story.speak");
            focus.GetComponentInChildren<Text>(true).text = T("story.focus"); remember.GetComponentInChildren<Text>(true).text = T("story.memories");
        }
        public void RefreshLanguage()
        {
            RefreshStaticLabels(); lastRevision = -1; lastSecond = -1; Refresh();
        }
        public void Refresh() { if (isActiveAndEnabled) Paint(false); }
        private string Status(string id, bool collaboration)
        {
            var stories = root.Session.Stories;
            if (stories.Ready(id)) return T("story.ready");
            if (stories.Active(id) != null) return T("story.active");
            if (stories.Offer(id) != null) return T("story.available");
            if (collaboration)
            {
                if (stories.Joint(id)?.completed != null) return T("story.complete");
                var project = stories.Collaborations.Single(c => c.id == id);
                if (root.Session.State.City.storyResidentMilestone < project.quest.residents)
                    return string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture, T("story.locked"), project.quest.residents);
                if (!project.participants.All(stories.Unlocked) || !project.quest.prerequisites.All(stories.HasCompleted))
                    return T(RequirementsKey(project));
                return T("story.later_zone");
            }
            var person = stories.People.Single(p => p.id == id);
            if (!stories.Unlocked(id)) return string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("story.locked"), person.residents);
            var next = stories.NextChapter(id);
            return next != null ? string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("story.next"), next.residents) : T("story.recurring");
        }
        private void Paint(bool force)
        {
            if (root?.Session == null || selected == null) return;
            var session = root.Session; var stories = session.Stories; var state = session.State;
            if (!force && lastRevision == state.revision && lastSecond == state.simulationSeconds) return;
            lastRevision = state.revision; lastSecond = state.simulationSeconds;
            milestone.text = string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("story.milestone"), state.City.storyResidentMilestone, stories.ActiveCount, VipStoryRules.ActiveLimit);
            foreach (var row in rows)
            {
                row.gameObject.SetActive(row.collaboration == joint);
                if (!row.gameObject.activeSelf) continue;
                string host = row.collaboration ? stories.Collaborations.Single(c => c.id == row.id).host : row.id;
                var person = stories.People.Single(p => p.id == host);
                row.portrait.sprite = visuals.Get(host).portrait;
                row.caption.text = row.collaboration ? T(stories.Collaborations.Single(c => c.id == row.id).quest.titleKey) : T(person.nameKey);
                row.status.text = Status(row.id, row.collaboration);
                row.accent.color = row.id == selected ? new Color(1, .79f, .37f) : new Color(.19f, .36f, .34f);
                row.portrait.color = stories.Unlocked(host) ? Color.white : new Color(.45f, .51f, .49f, .6f);
            }
            string hostId = Host(); var definition = stories.People.FirstOrDefault(p => p.id == hostId);
            if (definition == null) return;
            bool unlocked = stories.Unlocked(hostId);
            portrait.sprite = visuals.Get(hostId).body; portrait.color = unlocked ? Color.white : new Color(.5f, .56f, .53f, .6f);
            var collaboration = joint ? stories.Collaborations.Single(c => c.id == selected) : null;
            nameLabel.text = joint ? T(collaboration.quest.titleKey) : T(definition.nameKey);
            role.text = joint ? string.Join(" · ", collaboration.participants.Select(id => T(stories.People.Single(p => p.id == id).nameKey).Replace("Dr. ", "").Split(' ')[0])) : T(definition.roleKey);
            biography.text = joint ? T(collaboration.quest.bodyKey) : T(definition.biographyKey);
            greeting.text = T(definition.greetingKey);
            var active = stories.Active(selected); var quest = stories.ActiveDefinition(selected) ?? stories.Offer(selected);
            bool offer = active == null && quest != null;
            bool recurring = quest != null && definition.recurring.Contains(quest);
            int scale = active?.scale ?? (recurring ? VipStoryRules.RecurringScale(state.City.storyResidentMilestone) : 1);
            accept.gameObject.SetActive(offer); alternate.gameObject.SetActive(offer && quest.choices.Length > 1);
            claim.gameObject.SetActive(active != null); claim.interactable = stories.Ready(selected);
            speak.interactable = focus.interactable = unlocked;
            if (offer)
            {
                acceptLabel.text = T(quest.choices[0].textKey); if (quest.choices.Length > 1) alternateLabel.text = T(quest.choices[1].textKey);
                accept.interactable = alternate.interactable = stories.ActiveCount < VipStoryRules.ActiveLimit;
            }
            questTitle.text = quest != null ? T(quest.titleKey) : Status(selected, joint);
            description.text = quest != null ? T(quest.bodyKey) : joint ? Requirements(collaboration) : T("story.fresh");
            if (active != null) description.text += "\n\n" + string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("story.chosen"), T(quest.choices.Single(c => c.id == active.choiceId).textKey));
            for (int i = 0; i < goals.Length; i++)
            {
                bool show = quest != null && i < quest.goals.Length; goals[i].gameObject.SetActive(show); if (!show) continue;
                var goal = quest.goals[i]; string target = goal.kind == StoryGoalKind.NpcVisited ? T(stories.People.Single(p => p.id == goal.target).nameKey) :
                    goal.kind == StoryGoalKind.Level ? T(root.Definitions.Building(root.Definitions.instances.FirstOrDefault(b => b.id == goal.target)?.family ?? goal.target).nameKey) : "";
                Amount progress=active!=null?stories.Progress(selected,i):goal.IsCounter?Amount.Zero:
                    Amount.Min(goal.count,VipStoryRules.Value(root.Definitions,state,root.Session.Simulation,goal,state.activeCity));
                goalLabels[i].text = string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T(goal.textKey), target, progress, active?.targets[i] ?? VipStoryRules.GoalTarget(goal, recurring, scale));
                goals[i].interactable = goal.kind == StoryGoalKind.Level || goal.kind == StoryGoalKind.NpcVisited;
            }
            reward.gameObject.SetActive(quest != null);
            if (quest != null) reward.text = T("story.rewards") + ": +" + quest.moneyReward * scale + GameText.Get("presentation.buildinginteractionui.geld.c29cffbd") +
                (string.IsNullOrEmpty(quest.memoryKey) ? "" : "\n" + T(quest.memoryKey)) + string.Concat(quest.itemRewards.Select(item => "\n+" + item.count + " " + T(root.Definitions.items.Single(d => d.id == item.id).nameKey)));
            var memoryText = new StringBuilder(T("story.memories") + "\n");
            var completed = joint ? (stories.Joint(selected)?.completed == null ? Array.Empty<StoryCompletion>() : new[] { stories.Joint(selected).completed }) : stories.Person(selected)?.completed.ToArray() ?? Array.Empty<StoryCompletion>();
            foreach (var completion in completed)
            {
                var q = stories.Definition(completion.questId);
                memoryText.Append("\n").Append(T(q.memoryKey)).Append("\n").Append(T(q.choices.Single(c => c.id == completion.choiceId).outcomeKey)).Append("\n");
            }
            var last = stories.LastCompletion(selected);
            if (last != null)
            {
                var q = stories.Definition(last.questId);
                memoryText.Append("\n").Append(T("story.past")).Append("\n").Append(T(q.completedKey));
            }
            if (completed.Length == 0 && last == null) memoryText.Append(T("story.none"));
            memories.text = memoryText.ToString();
            hint.text = offer && stories.ActiveCount >= VipStoryRules.ActiveLimit ? T("story.capacity") : active != null ? T("story.fresh") : "";
            if (!joint && quest == null && unlocked)
            {
                var progress = stories.Person(selected); long wait = progress.nextRecurringSecond - state.simulationSeconds;
                if (wait > 0) hint.text = string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("story.waiting"), TimeSpan.FromSeconds(wait).ToString(@"mm\:ss"));
                else if (stories.NextChapter(selected) is StoryQuest next && state.City.storyResidentMilestone >= next.residents) hint.text = T("story.later_zone");
            }
            FitRoute();
        }
        private string Requirements(VipCollaborationDefinition definition)
        {
            var stories = root.Session.Stories;
            var result = new StringBuilder(T(RequirementsKey(definition)));
            foreach (string id in definition.quest.prerequisites)
                result.Append("\n").Append(stories.HasCompleted(id) ? "✓ " : "○ ").Append(T(stories.Definition(id).titleKey));
            return result.ToString();
        }
        private static string RequirementsKey(VipCollaborationDefinition definition) =>
            definition.id == "joint_ten_voices" ? "story.joint_final_hint" : "story.requirements";
    }
}

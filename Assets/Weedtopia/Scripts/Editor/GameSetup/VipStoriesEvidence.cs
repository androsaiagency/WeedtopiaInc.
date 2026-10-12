using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    public static class VipStoriesEvidence
    {
        public static List<Action> Prepare(GameRoot root, ManualClock clock, Action<string> capture, Action<int, int> size)
        {
            var steps = new List<Action>(); root.Session.CompleteIntro(); root.Ui.Close();
            var state = root.Session.State;
            state.City.questIndex = root.Session.Progression.Quests.Length; state.City.completedQuests = root.Session.Progression.Quests.Select(q => q.id).ToList();
            state.City.freeplay = true; state.City.unlocked = Zone.Industry;
            var plots = state.buildings.Where(b => b.cityId==state.activeCity && b.family == "settlement_plot").ToArray();
            for (int i = 0; i < plots.Length; i++) { plots[i].family = i == plots.Length - 1 ? "park" : "residential_house"; plots[i].level = i < 2 ? 25 : 0; }
            root.Session.Stories.RefreshUnlocks(); root.Refresh();
            void Shot(string name, Action action) { steps.Add(() => { root.Ui.Rewards.DismissAll(); action(); root.Refresh(); root.Ui.Notify(""); capture(name); }); }
            void Complete(string id)
            {
                var quest = root.Session.Stories.Offer(id); root.Session.Stories.Accept(id, quest.id, quest.choices[0].id);
                state.City.stats.deliveries += 100; state.City.stats.delivered += 10000; state.City.stats.coffeeSold += 10000;
                state.City.stats.rawProduced += 10000; state.City.stats.pastrySold += 10000;
                foreach (var goal in quest.goals.Where(g => g.kind == StoryGoalKind.NpcVisited)) root.Session.Stories.Visit(goal.target);
                foreach (var goal in quest.goals.Where(g => g.kind == StoryGoalKind.Level))
                    state.buildings.First(b => b.id == goal.target || b.family == goal.target).level = goal.count;
                if (root.Session.Stories.Claim(id, quest.id) != Failure.None) throw new InvalidOperationException("Diagnostic quest did not complete: " + quest.id);
            }
            Shot("01_first_vip_in_city", () => { root.Ui.Close(); root.StoryWorld.Focus("toni"); root.CameraRig.SetDiagnosticZoom(17); });
            Shot("02_first_story_50_residents", () => root.Ui.ShowStories("toni"));
            Shot("03_active_first_tour", () => root.Ui.Stories.accept.onClick.Invoke());
            Shot("04_first_tour_memory", () =>
            {
                state.City.stats.deliveries += 3; root.Session.Stories.Claim("toni", "toni_chapter_1"); root.Ui.ShowStories("toni"); root.Ui.Stories.remember.onClick.Invoke();
            });
            Shot("05_ten_vips_growing_city", () =>
            {
                foreach (var building in state.buildings.Where(b=>b.cityId==state.activeCity))
                {
                    if (building.family != "town_hall") building.level = 25;
                    if (building.family != "residential_house") continue;
                    building.level = 5000; var worker = root.Session.Workers.Grant("caretaker", 4); worker.level = 100; worker.rank = 10;
                    root.Session.Workers.Assign(worker.id, building.id, 0);
                }
                root.Session.Stories.RefreshUnlocks(); root.Ui.ShowStories("toni");
            });
            Shot("06_junos_personal_choice", () => { Complete("juno"); root.Ui.ShowStories("juno"); });
            Shot("07_shared_projects", () => { root.Ui.ShowStories("joint_lantern_evening"); });
            Shot("08_first_keepsake_in_world", () => { root.Ui.Close(); root.StoryWorld.Focus("toni"); root.CameraRig.SetDiagnosticZoom(17); });
            foreach (var resolution in new[] { new Vector2Int(1366, 768), new Vector2Int(2560, 1080), new Vector2Int(1080, 1920) })
                Shot("09_stories_" + resolution.x + "x" + resolution.y, () => { size(resolution.x, resolution.y); state.uiScale = resolution.x == 1366 ? 1.15f : 1; root.Ui.ShowStories("juno"); });
            Shot("10_new_alltagsstory", () => { size(1920, 1080); state.uiScale = 1; while (root.Session.Stories.NextChapter("toni") != null) Complete("toni"); root.Ui.ShowStories("toni"); });
            foreach (var person in root.Definitions.vipStories.Where(p=>p.cityId==state.activeCity))
                Shot("11_city_" + person.id, () => { root.Ui.Close(); root.StoryWorld.Focus(person.id); root.CameraRig.SetDiagnosticZoom(17); });
            return steps;
        }
    }
}

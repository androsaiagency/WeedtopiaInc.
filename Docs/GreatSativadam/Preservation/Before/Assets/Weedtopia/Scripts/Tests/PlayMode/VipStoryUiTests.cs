using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class VipStoryUiTests
    {
        private GameRoot root;
        private string path;
        private WorkerState caretaker;
        [UnitySetUp] public IEnumerator Setup()
        {
            caretaker = null;
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/Bootstrap.unity");
            for (int i = 0; i < 200 && Object.FindFirstObjectByType<GameRoot>() == null; i++) yield return new WaitForSecondsRealtime(.1f);
            root = Object.FindFirstObjectByType<GameRoot>(); root.enabled = false;
            path = Path.Combine(UnityEngine.Application.temporaryCachePath, "vip_ui_" + Guid.NewGuid() + ".json");
            root.UseTestStore(path, new ManualClock(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc)));
            Assert.That(root.NewGame("VIP UI"), Is.True); root.Session.CompleteIntro(); root.Ui.Close(); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            root.Ui.Rewards.DismissAll(); root.Ui.Close();
            foreach (string file in new[] { path, path + ".bak", path + ".scores" }) if (File.Exists(file)) File.Delete(file);
            yield return null;
        }
        private void Residents(int rank = 1)
        {
            root.Session.State.City.unlocked = Zone.Industry;
            var house = root.Session.State.Find("settlement_01"); house.family = "residential_house"; house.level = 1;
            if (caretaker == null) { caretaker = root.Session.Workers.Grant("caretaker", 4); caretaker.level = 80; Assert.That(root.Session.Workers.Assign(caretaker.id, house.id, 0), Is.EqualTo(Failure.None)); }
            caretaker.rank = rank; root.Session.Stories.RefreshUnlocks(); root.Refresh();
        }
        private void Hit(Button button)
        {
            UiScrollTestUtility.Reveal(button);
            Canvas.ForceUpdateCanvases(); var rect = (RectTransform)button.transform;
            var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), button.name); Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.EqualTo(button), button.name);
        }
        [UnityTest] public IEnumerator PopulationUnlocksOnlyTheFirstActorAndNewGameHidesAllActorsAgain()
        {
            Assert.That(root.StoryWorld.actors.Length, Is.EqualTo(10)); Assert.That(root.StoryWorld.actors.All(a => !a.Visible && !a.selectionBox.enabled), Is.True);
            Residents(); yield return null;
            Assert.That(root.Session.Simulation.Residents(root.Session.State.activeCity), Is.EqualTo(50));
            Assert.That(root.StoryWorld.actors.Count(a => a.Visible), Is.EqualTo(1)); Assert.That(root.StoryWorld.Actor("toni").Visible, Is.True);
            root.NewGame("Neu VIP"); root.Session.CompleteIntro(); root.Ui.Close(); root.Refresh(); yield return null;
            Assert.That(root.StoryWorld.actors.All(a => !a.Visible && !a.selectionBox.enabled), Is.True); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ClickingTheActualActorOpensItsOwnStoryAndStoresTheVisit()
        {
            Residents(); var actor = root.StoryWorld.Actor("toni"); root.StoryWorld.Focus("toni"); root.CameraRig.SetDiagnosticZoom(17);
            for (int i = 0; i < 40; i++) yield return null;
            var point = (Vector2)root.CameraRig.RenderCamera.WorldToScreenPoint(actor.spriteRenderer.bounds.center);
            Assert.That(root.CameraRig.TrySelect(point), Is.True); yield return null;
            Assert.That(root.Ui.Stories.gameObject.activeInHierarchy, Is.True); Assert.That(root.Ui.Stories.SelectedId, Is.EqualTo("toni"));
            Assert.That(root.Session.Stories.Person("toni").visits, Is.GreaterThan(0)); Assert.That(root.Ui.Stories.nameLabel.text, Does.Contain("Toni")); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AcceptedQuestClaimsOnceAndItsWorldMemoryAndProgressSurviveContinue()
        {
            Residents(); root.Ui.ShowStories("toni"); var ui = root.Ui.Stories; ui.accept.onClick.Invoke(); yield return null;
            Assert.That(root.Session.Stories.Active("toni").questId, Is.EqualTo("toni_chapter_1")); Assert.That(ui.claim.interactable, Is.False);
            root.Session.State.City.stats.deliveries += 3; root.Session.State.revision++; root.Refresh(); Assert.That(ui.claim.interactable, Is.True);
            ui.claim.onClick.Invoke(); yield return null;
            Assert.That(root.Ui.Rewards.IsOpen, Is.True); Assert.That(root.Session.Stories.Person("toni").completed.Count, Is.EqualTo(1));
            Assert.That(root.StoryWorld.Actor("toni").memories.Any(m => m.chapter == 0 && m.target.activeSelf), Is.True);
            long sequence = root.Session.State.rewardSequence; root.Ui.Rewards.DismissAll(); Assert.That(root.Continue(), Is.True); root.Ui.Close(); root.Ui.ShowStories("toni"); yield return null;
            Assert.That(root.Session.State.rewardSequence, Is.EqualTo(sequence)); Assert.That(root.Session.Stories.Person("toni").completed.Count, Is.EqualTo(1));
            Assert.That(ui.memories.text, Does.Contain("Dankeskarte")); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ChapterChoicesAreSelectableAndSavedWithTheChosenResponse()
        {
            Residents(6); root.Ui.ShowStories("toni"); var ui = root.Ui.Stories; ui.accept.onClick.Invoke();
            root.Session.State.City.stats.deliveries += 3; root.Session.State.revision++; root.Refresh(); ui.claim.onClick.Invoke(); root.Ui.Rewards.DismissAll(); root.Ui.ShowStories("toni"); yield return null;
            Assert.That(ui.alternate.gameObject.activeSelf, Is.True); Hit(ui.accept); Hit(ui.alternate);
            ui.alternate.onClick.Invoke(); Assert.That(root.Session.Stories.Active("toni").choiceId, Is.EqualTo("honest"));
            Assert.That(ui.description.text, Does.Contain("späteren Termin")); Assert.That(File.Exists(path), Is.True); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LockedRosterAndSharedProjectsRemainReadableAndCannotBeAccepted()
        {
            Residents(); root.Ui.ShowStories("valentina"); var ui = root.Ui.Stories; yield return null;
            Assert.That(ui.questTitle.text, Does.Contain("3000")); Assert.That(ui.accept.gameObject.activeSelf, Is.False); Assert.That(ui.focus.interactable, Is.False);
            ui.togetherTab.onClick.Invoke(); yield return null;
            Assert.That(ui.JointTab, Is.True); Assert.That(ui.rows.Count(r => r.gameObject.activeSelf), Is.EqualTo(5)); Assert.That(ui.accept.gameObject.activeSelf, Is.False);
            Assert.That(ui.description.text, Does.Contain("ersten Kapitel")); Hit(ui.peopleTab); Hit(ui.togetherTab); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RecurringOfferPreviewsTheSameScaledTargetsAndRewardThatAcceptanceStores()
        {
            Residents(); root.Session.State.City.storyResidentMilestone = 10000; root.Session.Stories.RefreshUnlocks();
            var person = root.Session.Stories.Person("toni"); var definition = root.Session.Stories.People.Single(p => p.id == "toni");
            foreach (var chapter in definition.chapters) person.completed.Add(new StoryCompletion { questId = chapter.id, choiceId = chapter.choices[0].id });
            root.Ui.ShowStories("toni"); var ui = root.Ui.Stories; yield return null;
            var offer = root.Session.Stories.Offer("toni"); int target = VipStoryRules.GoalTarget(offer.goals[0], true, 10);
            Assert.That(ui.goalLabels[0].text, Does.Contain("0/" + target));
            Assert.That(ui.reward.text, Does.Contain("+" + offer.moneyReward * 10 + " Geld"));
            ui.accept.onClick.Invoke(); Assert.That(root.Session.Stories.Active("toni").targets[0], Is.EqualTo(target));
            Assert.That(ui.goalLabels[0].text, Does.Contain("0/" + target)); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator StoryPanelRefreshesAllAuthoredLabelsAcrossFiveLanguageChangesWithoutRepeatingVisits()
        {
            bool hadPreference = PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey);
            string previousPreference = PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey);
            string previousLanguage = root.Localization.Language;
            try
            {
                Assert.That(root.SetLanguage("de"), Is.True);
                Residents(); root.Ui.ShowStories("toni"); var ui = root.Ui.Stories;
                var person = root.Session.Stories.People.Single(p => p.id == "toni");
                var offer = root.Session.Stories.Offer("toni"); Assert.That(offer, Is.Not.Null);
                var labels = new[] { ui.heading, ui.subtitle,
                    ui.peopleTab.GetComponentInChildren<Text>(true), ui.togetherTab.GetComponentInChildren<Text>(true),
                    ui.claim.GetComponentInChildren<Text>(true), ui.speak.GetComponentInChildren<Text>(true),
                    ui.focus.GetComponentInChildren<Text>(true), ui.remember.GetComponentInChildren<Text>(true) };
                var keys = new[] { "story.title", "story.subtitle", "story.people", "story.together",
                    "story.claim", "story.speak", "story.focus", "story.memories" };
                foreach (string code in new[] { "en", "es", "fr", "ru", "de" })
                {
                    Assert.That(root.SetLanguage(code), Is.True); yield return null;
                    Assert.That(root.Localization.Language, Is.EqualTo(code));
                    for (int i = 0; i < labels.Length; i++)
                        Assert.That(labels[i].text, Is.EqualTo(root.Definitions.Text(keys[i])), code + ": " + keys[i]);
                    Assert.That(ui.nameLabel.text, Is.EqualTo(root.Definitions.Text(person.nameKey)), code);
                    Assert.That(ui.biography.text, Is.EqualTo(root.Definitions.Text(person.biographyKey)), code);
                    Assert.That(ui.questTitle.text, Is.EqualTo(root.Definitions.Text(offer.titleKey)), code);
                    Assert.That(ui.acceptLabel.text, Is.EqualTo(root.Definitions.Text(offer.choices[0].textKey)), code);
                    Assert.That(ui.claim.gameObject.activeSelf, Is.False, "Inactive action labels must also refresh.");
                }
                long visits = root.Session.Stories.Person("toni").visits;
                ui.speak.onClick.Invoke();
                Assert.That(root.Session.Stories.Person("toni").visits, Is.EqualTo(visits + 1),
                    "Language changes must not register additional action listeners.");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                root.SetLanguage(previousLanguage);
                if (hadPreference) PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey, previousPreference);
                else PlayerPrefs.DeleteKey(GameRoot.LanguagePreferenceKey);
                PlayerPrefs.Save();
            }
        }
        [UnityTest] public IEnumerator StoryControlsReceivePointerHitsAndUseTheAuthoredStaticRows()
        {
            Residents(); root.Ui.ShowStories("toni"); var ui = root.Ui.Stories; yield return null;
            Hit(ui.accept); Hit(ui.speak); Hit(ui.focus); Hit(ui.remember);
            Hit(ui.GetComponent<VipStoryPanelLayout>().back); ui.GetComponent<VipStoryPanelLayout>().back.onClick.Invoke();
            yield return null; // Let the re-enabled roster rebuild its authored row layout before raycasting.
            Hit(ui.rows.First(r => r.id == "toni").button); Hit(root.Ui.Element<Button>("ui.close"));
            int count = ui.GetComponentsInChildren<VipStoryRosterRow>(true).Length;
            for (int i = 0; i < 5; i++) { ui.togetherTab.onClick.Invoke(); ui.peopleTab.onClick.Invoke(); }
            Assert.That(ui.GetComponentsInChildren<VipStoryRosterRow>(true).Length, Is.EqualTo(count)); Assert.That(count, Is.EqualTo(15));
            root.Ui.ShowJournal(); yield return null;
            Assert.That(root.Ui.GetComponentsInChildren<Text>(false).Any(t => t.text.Contains("Stadtgeschichten")), Is.True); LogAssert.NoUnexpectedReceived();
            var entry = root.Ui.Element<Button>("storyJournal"); Hit(entry);
            root.Ui.ShowJournal(); yield return null; Assert.That(root.Ui.Element<Button>("storyJournal"), Is.SameAs(entry));
            entry.onClick.Invoke(); Assert.That(ui.gameObject.activeInHierarchy, Is.True);
        }
    }
}

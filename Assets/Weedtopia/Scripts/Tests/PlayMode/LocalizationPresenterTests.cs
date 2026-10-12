using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class LocalizationPresenterTests
    {
        private GameRoot root;
        private string folder, previousPreference;
        private bool hadPreference;
        [UnitySetUp] public IEnumerator Setup()
        {
            hadPreference = PlayerPrefs.HasKey(GameRoot.LanguagePreferenceKey); previousPreference = PlayerPrefs.GetString(GameRoot.LanguagePreferenceKey);
            PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey, "de");
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");
            root = UnityEngine.Object.FindFirstObjectByType<GameRoot>(); root.enabled = false;
            folder = Path.Combine(UnityEngine.Application.temporaryCachePath, "LocalizationPresenters_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            root.UseTestStore(Path.Combine(folder, "save.json"), new ManualClock(new DateTime(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc)));
            root.NewGame("Sprachwechsel"); root.Session.CompleteIntro(); root.Ui.Close(); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (root != null) { root.Ui.Rewards.DismissAll(); root.Ui.Close(); root.enabled = false; }
            if (hadPreference) PlayerPrefs.SetString(GameRoot.LanguagePreferenceKey, previousPreference); else PlayerPrefs.DeleteKey(GameRoot.LanguagePreferenceKey);
            PlayerPrefs.Save(); if (Directory.Exists(folder)) Directory.Delete(folder, true); yield return null;
        }
        private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        [UnityTest] public IEnumerator RewardTranslationPreservesCurrentReceiptQueuePageAndRevealProgress()
        {
            var state = root.Session.State; long sequence = state.rewardSequence;
            var firstItems = Enumerable.Range(0, root.Ui.Rewards.cards.Length + 1).Select(i => RewardItem.Of(RewardKind.Money, i + 1)).ToArray();
            RewardHistory.Record(state, RewardSource.Daily, "language-first", "ui.daily", firstItems);
            RewardHistory.Record(state, RewardSource.Daily, "language-second", "ui.daily", new[] { RewardItem.Of(RewardKind.Cards, 3) });
            var rewards = root.Ui.Rewards; rewards.EnqueueAfter(sequence); yield return null;
            var current = rewards.Current; object started = Field(rewards, "started"), offset = Field(rewards, "itemOffset"), revealed = Field(rewards, "revealed");
            int queued = ((Queue<RewardReceipt>)Field(rewards, "pending")).Count;
            rewards.cards[0].Reveal(.4f); string stateBefore = JsonUtility.ToJson(state);
            foreach (var language in root.Localization.Languages)
            {
                Assert.That(root.SetLanguage(language.Code), Is.True);
                Assert.That(rewards.Current, Is.SameAs(current)); Assert.That(Field(rewards, "started"), Is.EqualTo(started));
                Assert.That(Field(rewards, "itemOffset"), Is.EqualTo(offset)); Assert.That(Field(rewards, "revealed"), Is.EqualTo(revealed));
                Assert.That(((Queue<RewardReceipt>)Field(rewards, "pending")).Count, Is.EqualTo(queued));
                Assert.That(rewards.cards[0].reveal.alpha, Is.EqualTo(.4f));
                Assert.That(rewards.headline.text, Is.EqualTo(root.Definitions.Text("reward.headline")));
                Assert.That(rewards.source.text, Is.EqualTo(root.Definitions.Text("ui.daily")));
                Assert.That(rewards.caption.text, Is.EqualTo(root.Definitions.Text("reward.secured")));
                Assert.That(rewards.continueText.text, Is.EqualTo(root.Definitions.Text("reward.continue")));
                Assert.That(rewards.collectionText.text, Is.EqualTo(root.Definitions.Text("reward.open_collection")));
                Assert.That(rewards.counter.text, Does.StartWith(RewardText.Source(root.Definitions, current)));
                Assert.That(rewards.cards[0].title.text, Is.EqualTo(RewardText.Name(root.Definitions, firstItems[0])));
                Assert.That(rewards.cards[0].detail.text, Is.EqualTo(RewardText.Detail(root, firstItems[0], false)));
                Assert.That(JsonUtility.ToJson(state), Is.EqualTo(stateBefore));
            }
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator WheelTranslationKeepsPaidOfferAndSelectedPrizeUntilOneExplicitConfirmation()
        {
            root.Ui.ShowLuckyWheel(); var wheel = root.Ui.LuckyWheel; wheel.Spin(); root.Ui.Close(); root.Ui.ShowLuckyWheel();
            var selected = wheel.slots[4].Prize; wheel.Inspect(selected); wheel.Spin(); yield return null;
            Assert.That(wheel.Confirming, Is.True); var offer = Field(wheel, "offer");
            string state = JsonUtility.ToJson(root.Session.State); var pose = wheel.disc.localRotation;
            int retainedCallbackHits = 0, expectedCallbackHits = 0;
            wheel.slots[4].button.onClick.AddListener(() => retainedCallbackHits++);
            foreach (var language in root.Localization.Languages)
            {
                Assert.That(root.SetLanguage(language.Code), Is.True);
                Assert.That(wheel.Confirming, Is.True); Assert.That(Field(wheel, "offer"), Is.SameAs(offer));
                Assert.That(wheel.disc.localRotation, Is.EqualTo(pose));
                wheel.slots[4].button.onClick.Invoke(); Assert.That(retainedCallbackHits, Is.EqualTo(++expectedCallbackHits), "Language updates preserve external slot listeners.");
                Assert.That(wheel.itemName.text, Is.EqualTo(root.Definitions.Text(root.Definitions.items.Single(i => i.id == selected.id).nameKey)));
                Assert.That(wheel.status.text, Is.EqualTo(root.Localization.Format("wheel.confirm", 50)));
                Assert.That(wheel.spinLabel.text, Is.EqualTo(root.Localization.Format("wheel.confirm_button", 50)));
                Assert.That(JsonUtility.ToJson(root.Session.State), Is.EqualTo(state));
            }
            wheel.Spin(); Assert.That(wheel.Spinning, Is.True); Assert.That(root.Session.State.grinders, Is.Zero);
            Assert.That(root.Session.State.luckyWheel.spins, Is.EqualTo(2));
            root.SetLanguage("de"); Assert.That(wheel.Spinning, Is.True);
            Assert.That(wheel.status.text, Is.EqualTo(root.Definitions.Text("wheel.spinning")));
            Assert.That(wheel.spinLabel.text, Is.EqualTo(root.Definitions.Text("wheel.spinning")));
            root.Ui.Close(); yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator OpenConversationRetranslatesItsCurrentPageWithoutRepeatingTalkOrChangingProgress()
        {
            root.Definitions.TextLookup = new DialogueLookup(root.Definitions.TextLookup, root.Localization);
            root.SetLanguage("en"); root.Ui.SpeakToCharacter("nia"); root.Ui.AdvanceConversation(); yield return null;
            Assert.That(root.Ui.Experience.conversation.body.text, Is.EqualTo("Second page."));
            string state = JsonUtility.ToJson(root.Session.State);
            root.SetLanguage("de");
            Assert.That(root.Ui.Experience.conversation.body.text, Is.EqualTo("Zweite Seite."));
            Assert.That(root.Ui.Experience.conversation.speaker.text, Is.EqualTo(root.Definitions.Text("speaker.nia")));
            Assert.That(JsonUtility.ToJson(root.Session.State), Is.EqualTo(state));
            root.Ui.AdvanceConversation(); Assert.That(root.Ui.ModalOpen, Is.False);
            yield return null; LogAssert.NoUnexpectedReceived();
        }
        private sealed class DialogueLookup : ITextLookup
        {
            private readonly ITextLookup original;
            private readonly LocalizationService localization;
            public DialogueLookup(ITextLookup original, LocalizationService localization) { this.original = original; this.localization = localization; }
            public string Get(string key) => key == "dialog.nia" ? localization.Language == "de" ? "Erste Seite.\n\nZweite Seite." : "First page.\n\nSecond page." : original.Get(key);
        }
    }
}
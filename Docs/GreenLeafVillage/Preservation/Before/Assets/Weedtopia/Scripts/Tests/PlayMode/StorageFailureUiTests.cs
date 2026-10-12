using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class StorageFailureUiTests
    {
        private GameRoot root;
        private string path;

        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/Bootstrap.unity");
            for (int i = 0; i < 200 && UnityEngine.Object.FindFirstObjectByType<GameRoot>() == null; i++)
                yield return new WaitForSecondsRealtime(.1f);
            root = UnityEngine.Object.FindFirstObjectByType<GameRoot>();
            Assert.That(root, Is.Not.Null); root.enabled = false;
            path = Path.Combine(UnityEngine.Application.temporaryCachePath, "Weedtopia_StorageUi_" + Guid.NewGuid().ToString("N") + ".json");
            var clock = new ManualClock(new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc));
            root.UseTestStore(path, clock);
            Assert.That(root.NewGame("Speichermeldung"), Is.True);
            root.Session.CompleteIntro();
            root.Ui.Close();
            yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var file in new[] { path, path + ".bak", path + ".scores" })
                if (File.Exists(file)) File.Delete(file);
            yield return null;
        }

        [UnityTest] public IEnumerator AutosaveErrorAppearsOnceUntilRecoveryAndCanBeReportedAgain()
        {
            var store = new FailingStore { Fail = true };
            var session = new GameSession(root.Definitions, root.Session.State,
                new ManualClock(new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc)), store);
            typeof(GameRoot).GetField("<Session>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(root, session);
            var update = typeof(GameRoot).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            session.Advance(30);
            LogAssert.Expect(LogType.Warning, "Save operation failed: see local technical diagnostics.");
            update.Invoke(root, null);
            Assert.That(root.Ui.Element<Text>("message").text, Is.EqualTo(root.Definitions.Text("save.error")));
            root.Ui.Notify("Andere Spielmeldung");
            for (int i = 0; i < 5; i++) update.Invoke(root, null);
            Assert.That(root.Ui.Element<Text>("message").text, Is.EqualTo("Andere Spielmeldung"));
            store.Fail = false; session.Save(); update.Invoke(root, null);
            store.Fail = true; Assert.Throws<IOException>(() => session.Save());
            LogAssert.Expect(LogType.Warning, "Save operation failed: see local technical diagnostics.");
            update.Invoke(root, null);
            Assert.That(root.Ui.Element<Text>("message").text, Is.EqualTo(root.Definitions.Text("save.error")));
            store.Fail = false;
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator DirectUnauthorizedSaveIsLocalizedAndRestoresTheFailedCommand()
        {
            var error = new UnauthorizedAccessException("Simulated UI access denied");
            var store = new FailingStore { Fail = true, Error = error };
            var session = new GameSession(root.Definitions, root.Session.State,
                new ManualClock(new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc)), store);
            typeof(GameRoot).GetField("<Session>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(root, session);
            string before = JsonUtility.ToJson(session.State);
            LogAssert.Expect(LogType.Warning, "Save operation failed: see local technical diagnostics.");
            Assert.That(root.Command(() => session.Upgrades.BuyWithMoney("farm_grow_01")), Is.EqualTo(Failure.Invalid));
            Assert.That(JsonUtility.ToJson(session.State), Is.EqualTo(before));
            Assert.That(session.LastSaveError, Is.SameAs(error));
            Assert.That(root.Ui.Element<Text>("message").text, Is.EqualTo(root.Definitions.Text("save.error")));
            store.Fail = false;
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator FailedIntroButtonsStayOnTheirPageAndKeepTheIntroOpenUntilSaved()
        {
            var store = new FailingStore { Fail = true };
            var session = new GameSession(root.Definitions, root.Session.State,
                new ManualClock(new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc)), store);
            typeof(GameRoot).GetField("<Session>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(root, session);
            session.State.introSeen = false; session.State.introLine = 2;
            root.Ui.ShowIntro(); string before = JsonUtility.ToJson(session.State);
            foreach (var error in new Exception[] { new IOException("Intro UI disk failure"), new UnauthorizedAccessException("Intro UI access denied") })
            {
                store.Error = error;
                LogAssert.Expect(LogType.Warning, "Save operation failed: see local technical diagnostics.");
                root.Ui.Element<Button>("ui.next").onClick.Invoke();
                Assert.That(JsonUtility.ToJson(session.State), Is.EqualTo(before));
                Assert.That(root.Ui.Element<RectTransform>("intro").gameObject.activeSelf, Is.True);
                Assert.That(root.Ui.Element<Text>("message").text, Is.EqualTo(root.Definitions.Text("save.error")));
                LogAssert.Expect(LogType.Warning, "Save operation failed: see local technical diagnostics.");
                root.Ui.Element<Button>("ui.skip").onClick.Invoke();
                Assert.That(JsonUtility.ToJson(session.State), Is.EqualTo(before));
                Assert.That(root.Ui.Element<RectTransform>("intro").gameObject.activeSelf, Is.True);
            }
            store.Fail = false;
            root.Ui.Element<Button>("ui.skip").onClick.Invoke();
            Assert.That(session.State.introSeen, Is.True);
            Assert.That(root.Ui.Element<RectTransform>("intro").gameObject.activeSelf, Is.False);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private sealed class FailingStore : ISaveStore
        {
            public bool Fail;
            public Exception Error;
            public bool Exists => false;
            public SaveLoadResult Load() => new SaveLoadResult();
            public void Save(GameState state) { if (Fail) throw Error ?? new IOException("Simulated UI disk failure"); }
        }
    }
}

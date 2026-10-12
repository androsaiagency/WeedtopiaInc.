using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class ResponsiveStartupUiTests
    {
        private GameRoot root;
        private ManualClock clock;
        private string directory, path;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");
            root = Object.FindFirstObjectByType<GameRoot>(); root.enabled = false;
            directory = Path.Combine(UnityEngine.Application.temporaryCachePath, "Weedtopia_ResponsiveStart_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); path = Path.Combine(directory, "save.json");
            clock = new ManualClock(new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc));
            root.UseTestStore(path, clock); Assert.That(root.NewGame("Responsiver Start"), Is.True);
            root.Session.CompleteIntro(); root.Ui.Close();
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (root != null) { root.StopAllCoroutines(); root.enabled = false; }
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            yield return null;
        }
        private IEnumerator Finish()
        {
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (root != null && root.ContinueBusy && timeout.Elapsed.TotalSeconds < 10) yield return null;
            Assert.That(root == null || !root.ContinueBusy, Is.True, "Small continuation did not finish within ten seconds");
        }

        [UnityTest] public IEnumerator StartShowsProgressBeforeWorkAndSuppressesDuplicateClicksAndPauseSaves()
        {
            root.Session.State.Find("farm_grow_01").level = 1; root.SaveSilently();
            var previous = root.Session; string diskBefore = File.ReadAllText(path);
            clock.UtcNow = clock.UtcNow.AddSeconds(500); root.Ui.ShowStart();
            var play = root.Ui.Element<Button>("ui.start"); play.onClick.Invoke();
            Assert.That(root.ContinueBusy, Is.True); Assert.That(root.Session, Is.SameAs(previous));
            Assert.That(play.interactable, Is.False); Assert.That(play.GetComponentInChildren<Text>().text, Does.Contain("0"));
            Assert.That(root.PlayResponsive(), Is.False); Assert.That(root.ContinueResponsive(), Is.False);
            root.Session.State.money = 2345; // A focus callback must not overwrite the loaded disk snapshot with this old session.
            typeof(GameRoot).GetMethod("OnApplicationPause", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(root, new object[] { true });
            Assert.That(File.ReadAllText(path), Is.EqualTo(diskBefore));
            yield return null;
            Assert.That(root.ContinueBusy, Is.True); Assert.That(root.Session, Is.SameAs(previous));
            Assert.That(File.ReadAllText(path), Is.EqualTo(diskBefore));
            string loadingCaption = play.GetComponentInChildren<Text>().text;
            root.Ui.Experience.Fit(); Assert.That(play.GetComponentInChildren<Text>().text, Is.EqualTo(loadingCaption));
            yield return Finish();
            Assert.That(root.Session, Is.Not.SameAs(previous)); Assert.That(root.Session.State.simulationSeconds, Is.EqualTo(500));
            Assert.That(root.Session.State.money, Is.EqualTo(new Amount(1000))); Assert.That(play.interactable, Is.True);
            Assert.That(root.Ui.Element<RectTransform>("offline").gameObject.activeSelf, Is.True);
            Assert.That(root.Session.Paused, Is.True); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator FailedFinalWritePreservesOldSessionAndUnlocksPlayForRetry()
        {
            root.SaveSilently(); var previous = root.Session; string diskBefore = File.ReadAllText(path);
            clock.UtcNow = clock.UtcNow.AddSeconds(257);
            Directory.CreateDirectory(path + ".tmp");
            LogAssert.Expect(LogType.Warning, new Regex("^Save operation failed:"));
            Assert.That(root.ContinueResponsive(), Is.True); yield return Finish();
            Assert.That(root.Session, Is.SameAs(previous)); Assert.That(File.ReadAllText(path), Is.EqualTo(diskBefore));
            Assert.That(root.Ui.Element<Button>("ui.start").interactable, Is.True);
            Assert.That(root.Ui.Element<Text>("message").text, Is.EqualTo(root.Definitions.Text("save.error")));
            Directory.Delete(path + ".tmp");
            Assert.That(root.ContinueResponsive(), Is.True); yield return Finish();
            Assert.That(root.Session, Is.Not.SameAs(previous)); Assert.That(root.Session.State.simulationSeconds, Is.EqualTo(257));
            Assert.That(root.ContinuationPending, Is.False);
            Assert.That(root.Session.ApplyOffline().Seconds, Is.Zero); LogAssert.NoUnexpectedReceived();
        }

        private void InvokeRoot(string method, params object[] arguments)
            => typeof(GameRoot).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(root, arguments);

        [UnityTest] public IEnumerator FailedContinueBlocksRetainedSessionUpdateFocusWritesAndCommandsUntilRetry()
        {
            var previous=root.Session;
            previous.State.shop=new ShopState { vipMembership=new VipMembershipState
                { startedUtcTicks=clock.UtcNow.Ticks,expiresUtcTicks=clock.UtcNow.AddDays(3).Ticks } };
            root.SaveSilently();string diskBefore=File.ReadAllText(path),stateBefore=JsonUtility.ToJson(previous.State);
            int grindersBefore=previous.State.grinders;bool pausedBefore=previous.Paused;
            clock.UtcNow=clock.UtcNow.AddSeconds(257);Directory.CreateDirectory(path+".tmp");
            LogAssert.Expect(LogType.Warning,new Regex("^Save operation failed:"));
            Assert.That(root.ContinueResponsive(),Is.True);yield return Finish();
            Assert.That(root.Session,Is.SameAs(previous));Assert.That(root.ContinuationPending,Is.True);
            Assert.That(root.ContinueBusy,Is.False);
            // Restore writable storage before exercising the real callbacks: any leaked write now succeeds.
            Directory.Delete(path+".tmp");
            InvokeRoot("Update");root.SaveSilently();root.Save();root.Pause(true);
            InvokeRoot("OnApplicationFocus",false);InvokeRoot("OnApplicationPause",true);InvokeRoot("OnApplicationQuit");
            bool invoked=false;
            Func<Failure> mutation=()=>{invoked=true;previous.State.money=new Amount(1);return Failure.None;};
            Assert.That(root.Command(mutation),Is.EqualTo(Failure.Busy));
            Assert.That(root.StoryCommand(mutation),Is.EqualTo(Failure.Busy));
            Assert.That(root.CampaignCommand(mutation),Is.EqualTo(Failure.Busy));
            Assert.That(root.ChangeProfile("Ueberschrieben",PlayerProfile.Avatars[0]),Is.EqualTo(Failure.Busy));
            Assert.That(root.TravelToCity("big_rock_city"),Is.EqualTo(Failure.Busy));
            Assert.That(root.SpinWheel("pending-continuation",out var spin),Is.EqualTo(Failure.Busy));Assert.That(spin,Is.Null);
            Assert.That(root.AdvanceIntro(),Is.False);Assert.That(root.CompleteIntro(),Is.False);Assert.That(root.RestartIntro(),Is.False);
            Assert.That(invoked,Is.False);Assert.That(previous.Paused,Is.EqualTo(pausedBefore));
            Assert.That(JsonUtility.ToJson(previous.State),Is.EqualTo(stateBefore));Assert.That(File.ReadAllText(path),Is.EqualTo(diskBefore));
            Assert.That(root.Ui.Element<Button>("ui.start").interactable,Is.True);
            Assert.That(root.ContinueResponsive(),Is.True);yield return Finish();
            Assert.That(root.ContinuationPending,Is.False);Assert.That(root.Session,Is.Not.SameAs(previous));
            Assert.That(root.Session.State.simulationSeconds,Is.EqualTo(257));
            Assert.That(root.Session.State.grinders,Is.EqualTo(grindersBefore+ShopRules.DailyGrinders));
            Assert.That(root.Session.State.shop.vipMembership.rewardedDays,Is.EqualTo(1));
            Assert.That(root.Session.State.rewardHistory.Count(r=>r.source==RewardSource.VipMembership),Is.EqualTo(1));
            Assert.That(root.Session.ApplyOffline().Seconds,Is.Zero);
            root.Ui.Close();Assert.That(root.Command(()=>Failure.None),Is.EqualTo(Failure.None));LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator FailedContinueCanStartExplicitNewGameWithoutAdvancingRetainedSession()
        {
            root.SaveSilently();var previous=root.Session;string stateBefore=JsonUtility.ToJson(previous.State);
            clock.UtcNow=clock.UtcNow.AddSeconds(257);Directory.CreateDirectory(path+".tmp");
            LogAssert.Expect(LogType.Warning,new Regex("^Save operation failed:"));
            Assert.That(root.ContinueResponsive(),Is.True);yield return Finish();Directory.Delete(path+".tmp");
            Assert.That(root.ContinuationPending,Is.True);Assert.That(root.NewGame("Neues Spiel",true),Is.True);
            Assert.That(root.ContinuationPending,Is.False);Assert.That(root.Session,Is.Not.SameAs(previous));
            Assert.That(root.Session.State.playerName,Is.EqualTo("Neues Spiel"));Assert.That(root.Session.State.profileNamePending,Is.True);
            Assert.That(JsonUtility.ToJson(previous.State),Is.EqualTo(stateBefore));LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator RestartAfterFailedContinueArchivesOriginalSaveBeforeStartingNewGame()
        {
            root.SaveSilently();var previous=root.Session;string diskBefore=File.ReadAllText(path),stateBefore=JsonUtility.ToJson(previous.State);
            clock.UtcNow=clock.UtcNow.AddSeconds(257);Directory.CreateDirectory(path+".tmp");
            LogAssert.Expect(LogType.Warning,new Regex("^Save operation failed:"));
            Assert.That(root.ContinueResponsive(),Is.True);yield return Finish();Directory.Delete(path+".tmp");
            Assert.That(root.RestartGame(),Is.True);Assert.That(root.ContinuationPending,Is.False);
            var archives=Directory.GetFiles(directory,"save.json.before_restart_*.json");Assert.That(archives.Length,Is.EqualTo(1));
            Assert.That(File.ReadAllText(archives[0]),Is.EqualTo(diskBefore));
            Assert.That(JsonUtility.ToJson(previous.State),Is.EqualTo(stateBefore));LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator FailedSynchronousContinueRetainsSessionUntilSuccessfulSynchronousRetry()
        {
            root.SaveSilently();var previous=root.Session;string diskBefore=File.ReadAllText(path),stateBefore=JsonUtility.ToJson(previous.State);
            clock.UtcNow=clock.UtcNow.AddSeconds(257);Directory.CreateDirectory(path+".tmp");
            LogAssert.Expect(LogType.Warning,new Regex("^Save operation failed:"));
            Assert.That(root.Continue(),Is.False);Assert.That(root.Session,Is.SameAs(previous));
            Assert.That(root.ContinuationPending,Is.True);Assert.That(root.ContinueBusy,Is.False);Directory.Delete(path+".tmp");
            InvokeRoot("Update");InvokeRoot("OnApplicationFocus",false);
            Assert.That(JsonUtility.ToJson(previous.State),Is.EqualTo(stateBefore));Assert.That(File.ReadAllText(path),Is.EqualTo(diskBefore));
            Assert.That(root.Continue(),Is.True);Assert.That(root.ContinuationPending,Is.False);
            Assert.That(root.Session.State.simulationSeconds,Is.EqualTo(257));Assert.That(root.Session.ApplyOffline().Seconds,Is.Zero);
            LogAssert.NoUnexpectedReceived();yield return null;
        }

        [UnityTest] public IEnumerator PlayAfterFailedContinueRetriesMissingSaveUntilExplicitNewGame()
        {
            root.SaveSilently();var previous=root.Session;string stateBefore=JsonUtility.ToJson(previous.State);
            clock.UtcNow=clock.UtcNow.AddSeconds(257);Directory.CreateDirectory(path+".tmp");
            LogAssert.Expect(LogType.Warning,new Regex("^Save operation failed:"));
            Assert.That(root.ContinueResponsive(),Is.True);yield return Finish();Directory.Delete(path+".tmp");
            // Simulate missing media only within this fixture's private temporary save directory.
            File.Delete(path);File.Delete(path+".bak");Assert.That(root.HasSave,Is.False);
            Assert.That(root.Play(),Is.False);Assert.That(root.ContinuationPending,Is.True);
            Assert.That(root.PlayResponsive(),Is.True);yield return Finish();
            Assert.That(root.Session,Is.SameAs(previous));Assert.That(root.ContinuationPending,Is.True);
            Assert.That(JsonUtility.ToJson(previous.State),Is.EqualTo(stateBefore));Assert.That(File.Exists(path),Is.False);
            Assert.That(root.NewGame("Bewusst neues Spiel",true),Is.True);Assert.That(root.ContinuationPending,Is.False);
            Assert.That(root.Session,Is.Not.SameAs(previous));LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator ResponsiveContinueOpensSavedCityWithOneCompletedOfflineSummary()
        {
            root.Session.State.City.storyResidentMilestone = 1000;
            root.Session.State.cities.Single(c => c.id == "big_rock_city").charterAccepted = true;
            root.Session.State.activeCity = "big_rock_city";
            root.Session.Stories.RefreshUnlocks(); root.SaveSilently();
            clock.UtcNow = clock.UtcNow.AddSeconds(257);
            Assert.That(root.ContinueResponsive(), Is.True);
            for (int i = 0; i < 200; i++)
            {
                var candidate = Object.FindFirstObjectByType<GameRoot>();
                if (candidate != null && candidate.SceneCityId == "big_rock_city" && candidate.Session != null)
                { root = candidate; root.enabled = false; break; }
                yield return new WaitForSecondsRealtime(.05f);
            }
            Assert.That(root.SceneCityId, Is.EqualTo("big_rock_city")); Assert.That(root.SavePath, Is.EqualTo(Path.GetFullPath(path)));
            Assert.That(root.Session.State.simulationSeconds, Is.EqualTo(257));
            Assert.That(root.Ui.Element<RectTransform>("offline").gameObject.activeSelf, Is.True);
            Assert.That(root.Ui.Element<Text>("offlineText").text, Does.Contain("257"));
            Assert.That(root.Session.ApplyOffline().Seconds, Is.Zero); LogAssert.NoUnexpectedReceived();
        }
    }
}

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Persistence;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class OfflineWorkerLifecycleUiTests
    {
        private GameRoot root;
        private ManualClock clock;
        private string directory, path;

        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");
            root = Object.FindFirstObjectByType<GameRoot>(); root.enabled = false;
            directory = Path.Combine(UnityEngine.Application.temporaryCachePath,
                "Weedtopia_OfflineWorkerLifecycle_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); path = Path.Combine(directory, "save.json");
            clock = new ManualClock(new DateTime(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc));
            root.UseTestStore(path, clock);
            Assert.That(root.NewGame("Worker Lifecycle"), Is.True);
            Assert.That(root.CompleteIntro(), Is.True);
            root.Ui.Close();
            yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (root != null) Object.Destroy(root.gameObject);
            yield return null;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [UnityTest] public IEnumerator DestroyingRootBeforeFirstOfflineFrameCannotAdvanceOrPersist()
        {
            root.SaveSilently(); var previous = root.Session;
            string diskBefore = File.ReadAllText(path), stateBefore = JsonUtility.ToJson(previous.State);
            clock.UtcNow = clock.UtcNow.AddHours(8);
            Assert.That(root.ContinueResponsive(), Is.True);
            Assert.That(root.ContinueBusy, Is.True);
            Object.Destroy(root.gameObject);
            yield return null;
            Assert.That(root == null, Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo(diskBefore));
            Assert.That(JsonUtility.ToJson(previous.State), Is.EqualTo(stateBefore));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator DestroyingRootWithOwnedWorkerCancelsPrivateModelWithoutSaveOrHandoff()
        {
            root.Session.State.Find("farm_grow_01").level = 1;
            root.SaveSilently(); var previous = root.Session;
            string diskBefore = File.ReadAllText(path), previousBefore = JsonUtility.ToJson(previous.State);
            clock.UtcNow = clock.UtcNow.AddHours(8);
            var store = new LocalSaveStore(path, root.Definitions);
            var next = new GameSession(root.Definitions, store.Load().State, clock, store);
            string nextBefore = JsonUtility.ToJson(next.State); var buildings = next.State.buildings;
            var firstBuilding = buildings[0];
            Assert.That(root.ContinueResponsive(), Is.True);
            // Attach the real worker through the same Root boundary before its coroutine can resume.
            // This controls the lifecycle boundary even if a fast worker finishes within one frame.
            var start = typeof(GameRoot).GetMethod("TryStartOfflineWorker", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(start, Is.Not.Null);
            Assert.That((bool)start.Invoke(root, new object[] { next }), Is.True);
            var field = typeof(GameRoot).GetField("pendingOfflineWorker", BindingFlags.NonPublic | BindingFlags.Instance);
            var operation = (OfflineSimulationOperation)field.GetValue(root);
            Assert.That(operation, Is.Not.Null);
            Assert.That(next.IsApplyingOffline, Is.True);
            Object.Destroy(root.gameObject);
            yield return null;
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (next.IsApplyingOffline && timeout.Elapsed.TotalSeconds < 10) yield return null;
            Assert.That(root == null, Is.True);
            Assert.That(next.IsApplyingOffline, Is.False, "Destroyed Root must abandon and restore its unpublished model.");
            Assert.That(operation.SimulationFinished, Is.True);
            Assert.That(JsonUtility.ToJson(next.State), Is.EqualTo(nextBefore));
            Assert.That(next.State.buildings, Is.SameAs(buildings)); Assert.That(buildings[0], Is.SameAs(firstBuilding));
            Assert.That(JsonUtility.ToJson(previous.State), Is.EqualTo(previousBefore));
            Assert.That(File.ReadAllText(path), Is.EqualTo(diskBefore));
            Assert.Throws<ObjectDisposedException>(() => operation.TryComplete(out _));
            LogAssert.NoUnexpectedReceived();
        }
    }
}

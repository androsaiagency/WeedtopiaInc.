using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
    /// <summary>Native editor ContinueResponsive measurement on an owned temporary save; no target-device acceptance.</summary>
    public sealed class ResponsiveStartupBenchmarkTests
    {
        private const int OfflineSeconds = 28800;
        private const string FirstScene = "Assets/Weedtopia/Game/Scenes/BurningHills.unity";
        private const string TargetCity = "big_rock_city";
        private const string Evidence = "Docs/SevenHills/Benchmarks";
        private static readonly string[] CurrentCities = { "burning_hills", "big_rock_city", "seven_hills" };
        private const int BuildingsPerCity = 56, AssignedWorkersPerCity = 59;
        private const string TestSource = "Assets/Weedtopia/Scripts/Tests/PlayMode/ResponsiveStartupBenchmarkTests.cs";
        private static readonly DateTime SavedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        private GameRoot root;
        private string temporaryDirectory;
        private string savePath;
        private string baselinePath;
        private string projectRoot;

        [Serializable] private sealed class Report
        {
            public int schemaVersion = 1;
            public string utc, status, scope, frameMeasurement, offlinePhaseMeasurement, fixture, fixtureAdaptations;
            public string unity, runtime, platform, processor, operatingSystem, loadedUnityAssembly, loadedDomainAssembly;
            public string rootSourceSha256, sessionSourceSha256, simulationSourceSha256, testSourceSha256, contentSha256;
            public string unityAssemblySha256, domainAssemblySha256, inputStateSha256, inputSaveSha256;
            public string responsiveStateSha256, synchronousStateSha256, persistedStateSha256;
            public string sourceScene, destinationCity, destinationScene, detail, screenshot, screenshotSha256, screenshotDetail;
            public int seconds, buildings, workers, cities, framesElapsed, observedFrames, offlinePhaseObservedFrames, generation0Collections, generation1Collections, generation2Collections;
            public double responsiveMilliseconds, synchronousMilliseconds, maximumObservedFrameMilliseconds, p95ObservedFrameMilliseconds;
            public double maximumOfflinePhaseFrameMilliseconds, p95OfflinePhaseFrameMilliseconds;
            public long responsiveExecutedTicks, synchronousExecutedTicks;
            public bool completed, exactStateMatch, exactPersistedStateMatch, screenshotAttempted, screenshotExists, batchMode, targetAccepted, releaseAccepted, deviceAccepted, humanAccepted;
            public double[] observedFrameMilliseconds, offlinePhaseFrameMilliseconds;
        }

        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.That(UnityEngine.Application.isEditor, Is.True, "This diagnostic records the existing native Unity editor only.");
            yield return SceneManager.LoadSceneAsync(FirstScene);
            root = Object.FindFirstObjectByType<GameRoot>();
            Assert.That(root, Is.Not.Null); root.enabled = false;
            Assert.That(root.Session, Is.Null, "Benchmark must start before any session or personal-save continuation.");
            projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            temporaryDirectory = Path.Combine(UnityEngine.Application.temporaryCachePath, "Weedtopia_ResponsiveBenchmark_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
            savePath = Path.Combine(temporaryDirectory, "save.json");
            baselinePath = Path.Combine(temporaryDirectory, "synchronous.json");
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (root != null) { root.StopAllCoroutines(); root.enabled = false; }
            // Remove only this test's handoff; a failed owned scene transition must not leak into a later fixture.
            typeof(GameRoot).Assembly.GetType("Weedtopia.Unity.CitySceneHandoff", true)
                .GetMethod("Clear", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null, null);
            // Destroy the owned session before deleting its save directory. The fresh menu root has no active session.
            yield return SceneManager.LoadSceneAsync(FirstScene);
            root = Object.FindFirstObjectByType<GameRoot>();
            if (root != null) root.enabled = false;
            if (!string.IsNullOrEmpty(temporaryDirectory) && Directory.Exists(temporaryDirectory))
                Directory.Delete(temporaryDirectory, true);
        }

        [UnityTest, Category("Performance"), Timeout(360000)]
        public IEnumerator EightHourResponsiveContinueMatchesSynchronousStateAndRecordsFrameLatency()
            => Benchmark(0, "");

        [UnityTest, Category("Performance"), Timeout(360000)]
        public IEnumerator TenThousandInventoryWorkersContinueAndPersistAnExactEightHourState()
            => Benchmark(10000, ".large-inventory");

        private IEnumerator Benchmark(int inventoryWorkers, string suffix)
        {
            var definitions = root.Definitions;
            var prepared = Prepare(definitions);
            var inventoryRole = definitions.roles[0];
            for(int index=0;index<inventoryWorkers;index++)
                prepared.workers.Add(new WorkerState { id="inventory_bench_"+index, role=inventoryRole.id,
                    attributes={inventoryRole.attributes[0]} });
            var clock = new ManualClock(SavedAt.AddSeconds(OfflineSeconds));
            var store = new LocalSaveStore(savePath, definitions);
            var report = new Report
            {
                utc = DateTime.UtcNow.ToString("O"), status = "preparing",
                scope = "Current four-city catalog native Unity editor GameRoot.ContinueResponsive after the initial BurningHills scene is loaded: real temporary LocalSaveStore read/write, productive eight-hour simulation in the established three owned cities while Little Montana remains locked, private background simulation, owner-thread commit, target-city handoff and authored offline panel. This 224-catalog-building/168-productive-building/177-assigned-worker workload differs from the historical three-city-catalog and 112-building/118-worker two-city benchmarks; its timing is not a direct before/after comparison. Initial editor/process boot, initial scene load, texture import and physical target devices are excluded. This is an observed editor path; it does not certify GPU or device frame time, cold OS caches or human acceptance.",
                frameMeasurement = "Stopwatch wall-clock intervals between successive UnityTest coroutine observations, beginning immediately before ContinueResponsive. Includes the work and waits observed across those editor/player-loop frames. P95 uses nearest-rank over observed intervals. No GPU profiler or device frame measurement; there is no timing pass/fail threshold.",
                offlinePhaseMeasurement = "Interior observed frame intervals for which the original source GameRoot reports ContinueBusy, no TravelLoading, and partial progress strictly between zero and one at both observation boundaries. Excludes initial file loading and mixed terminal commit/scene-handoff intervals; target-scene loading stays in the whole-path statistics. No GPU or device frame-time claim.",
                fixture = "Established three-city synthetic test: Burning Hills, Big Rock City and Seven Hills each have 56 building instances at level 500 and 59 assigned workers at level 4/rank 2/rarity 4, for 168 buildings and 177 assigned workers; optional 10000 valid unassigned inventory workers. The original indexed plot-selection rule produces 18 residential houses per city in this fixture. Resource buffers are 50/20/10/5, 12-second construction is assigned on every seventh indexed instance, tick remainder is 0.375, and the saved active city is Big Rock City.",
                fixtureAdaptations = "Uses GameState.Create for current schema/content/profile fields and includes Little Montana's 56 untouched locked instances. The established three city charters are owned; the Burning Hills personal milestone satisfies the 1000-resident Big Rock charter, and Seven Hills stores its valid historical 5000 simultaneous source-resident charter milestone, retained after later population decline. Workers use each role's five allowed attributes. The model-only benchmark's all-enum attributes do not pass productive SaveValidator, so they cannot be written to a real LocalSaveStore. Initial high-water and last-UTC anchors both equal SavedAt; no simulation warmup tick is added. The synchronous comparison runs after the measured responsive path to avoid warming that path first.",
                unity = UnityEngine.Application.unityVersion,
                runtime = Type.GetType("Mono.Runtime") != null ? "Unity Editor Mono " + Environment.Version : Environment.Version.ToString(),
                platform = UnityEngine.Application.platform.ToString(), processor = SystemInfo.processorType,
                batchMode = UnityEngine.Application.isBatchMode,
                operatingSystem = SystemInfo.operatingSystem,
                loadedUnityAssembly = typeof(GameRoot).Assembly.Location, loadedDomainAssembly = typeof(Simulation).Assembly.Location,
                rootSourceSha256 = HashProjectFile("Assets/Weedtopia/Scripts/Runtime/Unity/GameRoot.cs"),
                sessionSourceSha256 = HashProjectFile("Assets/Weedtopia/Scripts/Runtime/Application/GameSession.cs"),
                simulationSourceSha256 = HashProjectFile("Assets/Weedtopia/Scripts/Runtime/Domain/Simulation.cs"),
                testSourceSha256 = HashProjectFile(TestSource), contentSha256 = HashProjectFile("Assets/Weedtopia/Game/Content/BurningHills.json"),
                unityAssemblySha256 = HashFile(typeof(GameRoot).Assembly.Location), domainAssemblySha256 = HashFile(typeof(Simulation).Assembly.Location),
                inputStateSha256 = HashText(JsonUtility.ToJson(prepared)),
                seconds = OfflineSeconds, buildings = prepared.buildings.Count, workers = prepared.workers.Count, cities = prepared.cities.Count,
                sourceScene = FirstScene, destinationCity = TargetCity,
                targetAccepted = false, releaseAccepted = false, deviceAccepted = false, humanAccepted = false
            };
            var frameIntervals = new List<double>();
            var offlineIntervals = new List<double>();
            try
            {
                CollectionAssert.AreEquivalent(definitions.cities.Select(city => city.id), prepared.cities.Select(city => city.id));
                CollectionAssert.AreEquivalent(CurrentCities, prepared.cities.Where(city => CityCampaignRules.Accessible(definitions, prepared, city.id)).Select(city => city.id));
                Assert.That(prepared.buildings.Count, Is.EqualTo(definitions.instances.Length));
                var assignments = new Dictionary<(string building, int slot), string>();
                var workerRules = new WorkerService(definitions, prepared);
                foreach (var building in prepared.buildings.Where(building => CurrentCities.Contains(building.cityId)))
                {
                    var definition = definitions.Building(building.family);
                    if (!string.IsNullOrEmpty(definition.managerRole)) assignments.Add((building.id, 0), definition.managerRole);
                    if (!string.IsNullOrEmpty(definition.driverRole))
                        for (int slot = 1; slot <= Math.Min(2, workerRules.Slots(building)); slot++) assignments.Add((building.id, slot), definition.driverRole);
                }
                Assert.That(assignments.Count, Is.EqualTo(CurrentCities.Length * AssignedWorkersPerCity));
                Assert.That(prepared.workers.Count, Is.EqualTo(assignments.Count + inventoryWorkers));
                foreach (var assignment in assignments)
                {
                    var workers = prepared.workers.Where(worker => worker.buildingId == assignment.Key.building && worker.slot == assignment.Key.slot).ToArray();
                    Assert.That(workers.Length, Is.EqualTo(1), assignment.Key.ToString());
                    Assert.That(workers[0].role, Is.EqualTo(assignment.Value), assignment.Key.ToString());
                }
                foreach (string city in CurrentCities)
                {
                    var citySites = prepared.buildings.Where(building => building.cityId == city).Select(building => building.id).ToHashSet();
                    Assert.That(citySites.Count, Is.EqualTo(BuildingsPerCity), city);
                    Assert.That(prepared.workers.Count(worker => !string.IsNullOrEmpty(worker.buildingId) && citySites.Contains(worker.buildingId)), Is.EqualTo(AssignedWorkersPerCity), city);
                }
                // Write the valid established fixture through the productive save envelope without GameSession.Save stamping it to now.
                store.Save(prepared); File.Copy(savePath, baselinePath, false);
                report.inputSaveSha256 = HashFile(savePath);
                root.UseTestStore(savePath, clock); root.Ui.ShowStart();
                Assert.That(root.HasSave, Is.True); Assert.That(root.Session, Is.Null);
                int startFrame = Time.frameCount;
                int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
                report.status = "running_responsive";
                var sourceRoot = root;
                var watch = Stopwatch.StartNew(); double previousMilliseconds = 0;
                Assert.That(root.ContinueResponsive(), Is.True);
                while (watch.Elapsed.TotalSeconds < 240)
                {
                    bool wasOfflinePhase = IsInteriorOfflinePhase(sourceRoot);
                    yield return null;
                    double elapsed = watch.Elapsed.TotalMilliseconds;
                    double interval = elapsed - previousMilliseconds;
                    frameIntervals.Add(interval); previousMilliseconds = elapsed;
                    if (wasOfflinePhase && IsInteriorOfflinePhase(sourceRoot)) offlineIntervals.Add(interval);
                    var candidate = Object.FindFirstObjectByType<GameRoot>();
                    if (candidate == null || candidate.SavePath != Path.GetFullPath(savePath)) continue;
                    root = candidate;
                    if (root.Session != null && root.SceneCityId == TargetCity && !root.ContinueBusy && !root.ContinuationPending
                        && !root.TravelLoading && root.Ui.Element<RectTransform>("offline").gameObject.activeSelf)
                    {
                        root.enabled = false; report.completed = true; break;
                    }
                    // A failed continuation exposes the authored retry button; do not turn that into an endless timing wait.
                    if (!root.ContinueBusy && root.ContinuationPending && !root.TravelLoading) break;
                }
                watch.Stop();
                report.responsiveMilliseconds = watch.Elapsed.TotalMilliseconds;
                report.framesElapsed = Time.frameCount - startFrame; report.observedFrames = frameIntervals.Count;
                report.generation0Collections = GC.CollectionCount(0) - gen0;
                report.generation1Collections = GC.CollectionCount(1) - gen1;
                report.generation2Collections = GC.CollectionCount(2) - gen2;
                report.observedFrameMilliseconds = frameIntervals.ToArray();
                report.offlinePhaseFrameMilliseconds = offlineIntervals.ToArray();
                report.offlinePhaseObservedFrames = offlineIntervals.Count;
                report.maximumObservedFrameMilliseconds = frameIntervals.Count == 0 ? 0 : frameIntervals.Max();
                report.p95ObservedFrameMilliseconds = P95(frameIntervals);
                report.maximumOfflinePhaseFrameMilliseconds = offlineIntervals.Count == 0 ? 0 : offlineIntervals.Max();
                report.p95OfflinePhaseFrameMilliseconds = P95(offlineIntervals);
                report.status = report.completed ? "comparing_synchronous" : "failed";
                Assert.That(report.completed, Is.True, "Real ContinueResponsive did not reach the saved city's authored offline panel.");
                Assert.That(root.Session.Paused, Is.True); Assert.That(root.SavePath, Is.EqualTo(Path.GetFullPath(savePath)));
                report.destinationScene = SceneManager.GetActiveScene().path;
                string responsiveJson = JsonUtility.ToJson(root.Session.State);
                report.responsiveStateSha256 = HashText(responsiveJson);
                report.responsiveExecutedTicks = root.Session.State.simulationSeconds - prepared.simulationSeconds;
                if (!UnityEngine.Application.isBatchMode && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    report.screenshot = Evidence + "/NativeResponsiveStartupBenchmark"+suffix+".offline.png";
                    string screenshotPath = Path.Combine(projectRoot, report.screenshot);
                    Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath));
                    if (File.Exists(screenshotPath)) File.Delete(screenshotPath);
                    report.screenshotAttempted = true; ScreenCapture.CaptureScreenshot(screenshotPath);
                    for (int frame = 0; frame < 3 && !File.Exists(screenshotPath); frame++) yield return null;
                    report.screenshotExists = File.Exists(screenshotPath);
                    if (report.screenshotExists) report.screenshotSha256 = HashFile(screenshotPath);
                    report.screenshotDetail = report.screenshotExists
                        ? "Native ScreenCapture of the completed authored offline panel; screenshot presence is not human or device acceptance. Capture waits are excluded from startup timing."
                        : "Native screenshot requested but no file appeared within three observed frames; no capture acceptance is claimed.";
                }
                else report.screenshotDetail = "Owned batch editor or null graphics device; no Game View screenshot attempted or claimed.";
                var baselineStore = new LocalSaveStore(baselinePath, definitions);
                var loaded = baselineStore.Load(); Assert.That(loaded.State, Is.Not.Null);
                var synchronous = new GameSession(definitions, loaded.State, clock, baselineStore);
                var baselineWatch = Stopwatch.StartNew(); var summary = synchronous.ApplyOffline(); baselineWatch.Stop();
                report.synchronousMilliseconds = baselineWatch.Elapsed.TotalMilliseconds;
                report.synchronousExecutedTicks = synchronous.State.simulationSeconds - prepared.simulationSeconds;
                string synchronousJson = JsonUtility.ToJson(synchronous.State);
                report.synchronousStateSha256 = HashText(synchronousJson);
                report.exactStateMatch = string.Equals(responsiveJson, synchronousJson, StringComparison.Ordinal);
                var persisted = store.Load(); Assert.That(persisted.State, Is.Not.Null);
                string persistedJson = JsonUtility.ToJson(persisted.State);
                report.persistedStateSha256 = HashText(persistedJson);
                report.exactPersistedStateMatch = string.Equals(responsiveJson, persistedJson, StringComparison.Ordinal);
                Assert.That(HashFile(savePath+".bak"),Is.EqualTo(report.inputSaveSha256),
                    "The scene handoff must retain the historical backup; its initial panel may not perform a second save.");
                if (!report.exactStateMatch || !report.exactPersistedStateMatch)
                {
                    WriteEvidence("NativeResponsiveStartupBenchmark"+suffix+".responsive.json", responsiveJson);
                    WriteEvidence("NativeResponsiveStartupBenchmark"+suffix+".synchronous.json", synchronousJson);
                    WriteEvidence("NativeResponsiveStartupBenchmark"+suffix+".persisted.json", persistedJson);
                }
                Assert.That(summary.Seconds, Is.EqualTo(OfflineSeconds));
                Assert.That(report.responsiveExecutedTicks, Is.EqualTo(OfflineSeconds));
                Assert.That(report.synchronousExecutedTicks, Is.EqualTo(OfflineSeconds));
                Assert.That(report.exactStateMatch, Is.True, "All serialized fields must equal the synchronous GameSession.ApplyOffline result.");
                Assert.That(report.exactPersistedStateMatch, Is.True, "Successful responsive continuation must persist exactly its active state.");
                Assert.That(root.Session.ApplyOffline().Seconds, Is.Zero, "The successful city handoff must not leave a second offline interval.");
                TestContext.Progress.WriteLine("Native ContinueResponsive eight-hour startup: " + report.responsiveMilliseconds.ToString("F1")
                    + " ms, " + report.framesElapsed + " frames; observed p95 " + report.p95ObservedFrameMilliseconds.ToString("F1")
                    + " ms, maximum " + report.maximumObservedFrameMilliseconds.ToString("F1") + " ms; complete state matches synchronous path.");
                TestContext.Progress.WriteLine("Interior offline frames: " + report.offlinePhaseObservedFrames + "; observed p95 "
                    + report.p95OfflinePhaseFrameMilliseconds.ToString("F1") + " ms, maximum " + report.maximumOfflinePhaseFrameMilliseconds.ToString("F1") + " ms.");
                LogAssert.NoUnexpectedReceived();
                report.status = "passed";
            }
            finally
            {
                if (report.status != "passed") { report.detail = "Benchmark did not pass; inspect its original NUnit result for the failure."; report.status = "failed"; }
                WriteEvidence("NativeResponsiveStartupBenchmark"+suffix+".json", JsonUtility.ToJson(report, true));
            }
        }

        private static bool IsInteriorOfflinePhase(GameRoot source)
            => source != null && source.ContinueBusy && !source.TravelLoading && source.ContinueProgress > 0 && source.ContinueProgress < 1;
        private static double P95(List<double> intervals)
        {
            var ordered = intervals.OrderBy(value => value).ToArray();
            return ordered.Length == 0 ? 0 : ordered[Math.Max(0, (int)Math.Ceiling(ordered.Length * .95) - 1)];
        }

        private static GameState Prepare(GameDefinitions definitions)
        {
            var state = GameState.Create(definitions, "Start-Benchmark", SavedAt);
            state.introSeen = true; state.tickRemainder = .375; state.activeCity = TargetCity;
            foreach (var city in state.cities.Where(city => CurrentCities.Contains(city.id)))
            {
                city.charterAccepted = true; city.unlocked = Zone.Industry; city.storyResidentMilestone = 1000;
                var destination = definitions.cities.Single(definition => definition.id == city.id);
                if (CityCampaignRules.Combined(destination)) city.charterResidentMilestone = destination.unlockResidents;
            }
            int index = 0;
            foreach (var building in state.buildings.Where(building => CurrentCities.Contains(building.cityId)))
            {
                var definition = definitions.Building(building.family); building.level = 500;
                if (definition.kind == BuildingKind.Plot)
                { building.family = index++ % 2 == 0 ? "residential_house" : "park"; definition = definitions.Building(building.family); }
                building.buildingSeconds = index++ % 7 == 0 ? 12 : 0; building.workSeconds = 3; building.lastDailyDay = 0;
                building.raw = 50; building.refined = 20; building.butter = 10; building.pastry = 5;
                if (!string.IsNullOrEmpty(definition.managerRole)) AddWorker(definitions, state, building, definition.managerRole, 0);
                if (!string.IsNullOrEmpty(definition.driverRole))
                    for (int slot = 1; slot <= 2; slot++) AddWorker(definitions, state, building, definition.driverRole, slot);
            }
            return state;
        }
        private static void AddWorker(GameDefinitions definitions, GameState state, BuildingState building, string role, int slot)
        {
            var worker = new WorkerState { id = "bench_" + state.workers.Count, role = role, buildingId = building.id, slot = slot,
                level = 4, rank = 2, rarity = 4 };
            worker.attributes.AddRange(definitions.Role(role).attributes); state.workers.Add(worker);
        }
        private void WriteEvidence(string filename, string contents)
        {
            string folder = Path.Combine(projectRoot, Evidence); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, filename), contents);
        }
        private string HashProjectFile(string path) => HashFile(Path.Combine(projectRoot, path));
        private static string HashFile(string path)
        {
            using (var hash = SHA256.Create()) using (var stream = File.OpenRead(path)) return Hex(hash.ComputeHash(stream));
        }
        private static string HashText(string value)
        {
            using (var hash = SHA256.Create()) return Hex(hash.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
        private static string Hex(byte[] value) => BitConverter.ToString(value).Replace("-", "").ToLowerInvariant();
    }
}

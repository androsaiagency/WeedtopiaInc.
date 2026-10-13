using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Weedtopia.Content;
using Weedtopia.Domain;
using Weedtopia.Editor.GameSetup;

namespace Weedtopia.Tests.Assets
{
    /// <summary>Actual Unity Editor/Mono model benchmark. No UI startup, player save or target-device acceptance.</summary>
    public sealed class NativeOfflineModelBenchmarkTests
    {
        private const string Folder = "Docs/CommercialReadiness/2026-10-06";
        private const string BaselineFolder = "Docs/CommercialReadiness/2026-10-05";
        private const string OriginalSource = BaselineFolder + "/Baseline/Simulation.cs";
        private const string OptimizedSource = "Assets/Weedtopia/Scripts/Runtime/Domain/Simulation.cs";
        private const int Seconds = 28800;
        [Serializable] private sealed class CompileReceipt
        {
            public string status = default, originalSourceSha256 = default, referenceAssembly = default, referenceAssemblySha256 = default;
        }
        [Serializable] private sealed class Measurement
        {
            public double milliseconds;
            public int generation0Collections, generation1Collections, generation2Collections;
            public long executedTicks;
        }
        [Serializable] private sealed class Report
        {
            public string utc, scope, unity, runtime, platform, processor, loadedDomainAssembly;
            public string originalSourceSha256, optimizedSourceSha256, referenceAssemblySha256, domainAssemblySha256, contentSha256;
            public string initialStateSha256, originalStateSha256, optimizedStateSha256;
            public int seconds, warmupSeconds, buildings, workers, cities;
            public double speedup;
            public bool exactStateMatch, targetAccepted, releaseAccepted, humanAccepted;
            public Measurement original, optimized;
        }
        [Test, Category("Performance"), Timeout(900000)]
        public void EightHourEstablishedTwoCityModelMatchesOriginalInNativeEditor()
        {
            var compile = JsonUtility.FromJson<CompileReceipt>(File.ReadAllText(Folder + "/Baseline/reference_compile.json"));
            Assert.That(compile.status, Is.EqualTo("passed"));
            string referencePath = Path.GetFullPath(compile.referenceAssembly);
            string baselinePrefix = Path.GetFullPath(Folder + "/Baseline").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            Assert.That(referencePath.StartsWith(baselinePrefix,
                StringComparison.OrdinalIgnoreCase), Is.True, "Reference must belong to this benchmark's baseline folder");
            Assert.That(File.Exists(referencePath), Is.True,
                "Compile the isolated original model first: python Tools/Implementation/compile_offline_reference.py");
            Assert.That(HashFile(OriginalSource), Is.EqualTo(compile.originalSourceSha256), "Reference source changed after compilation");
            Assert.That(HashFile(referencePath), Is.EqualTo(compile.referenceAssemblySha256), "Reference DLL changed after compilation");
            var content = AssetDatabase.LoadAssetAtPath<GameContent>(GameScaffold.ContentPath);
            // This performance regression compares the established two-city model to its original source.
            // Later destinations must not silently increase the measured workload or change its 112/118 baseline.
            var definitions = content.Load();
            var historicalCities = new[] { "burning_hills", "big_rock_city" };
            definitions.instances = definitions.instances.Where(i => historicalCities.Contains(i.cityId)).ToArray();
            var historicalSites = definitions.instances.Select(i => i.id).ToHashSet();
            definitions.routes = definitions.routes.Where(r => historicalSites.Contains(r.from) && historicalSites.Contains(r.to)).ToArray();
            definitions.cities = definitions.cities.Where(c => historicalCities.Contains(c.id)).ToArray();
            definitions.quests = definitions.quests.Where(q => historicalCities.Contains(string.IsNullOrEmpty(q.cityId) ? "burning_hills" : q.cityId)).ToArray();
            definitions.vipStories = definitions.vipStories.Where(v => historicalCities.Contains(v.cityId)).ToArray();
            definitions.vipCollaborations = definitions.vipCollaborations.Where(c => historicalCities.Contains(c.cityId)).ToArray();
            var prepared = Prepare(definitions);
            Assert.That(prepared.buildings.Count, Is.EqualTo(112));
            Assert.That(prepared.workers.Count, Is.EqualTo(118));
            Assert.That(prepared.cities.Count, Is.EqualTo(2));
            string initialJson = JsonUtility.ToJson(prepared);
            var originalState = JsonUtility.FromJson<GameState>(initialJson);
            var optimizedState = JsonUtility.FromJson<GameState>(initialJson);
            Assert.That(JsonUtility.ToJson(originalState), Is.EqualTo(JsonUtility.ToJson(optimizedState)), "Different benchmark starts");
            var referenceAssembly = Assembly.LoadFrom(referencePath);
            var type = referenceAssembly.GetType("Weedtopia.Domain.ReferenceSimulation", true);
            object reference = Activator.CreateInstance(type, definitions, originalState);
            var originalAdvance = (Action<double, long?>)Delegate.CreateDelegate(typeof(Action<double, long?>), reference,
                type.GetMethod("Advance", new[] { typeof(double), typeof(long?) }));
            var optimized = new Simulation(definitions, optimizedState);
            Action<double, long?> optimizedAdvance = optimized.Advance;
            // Both methods warm up one real tick on identical clones before the measured eight-hour advance.
            originalAdvance(1, originalState.highWaterUtcTicks);
            optimizedAdvance(1, optimizedState.highWaterUtcTicks);
            Assert.That(JsonUtility.ToJson(originalState), Is.EqualTo(JsonUtility.ToJson(optimizedState)), "Warmup changed results");
            var originalMeasurement = Measure(originalAdvance, originalState);
            var optimizedMeasurement = Measure(optimizedAdvance, optimizedState);
            string originalJson = JsonUtility.ToJson(originalState);
            string optimizedJson = JsonUtility.ToJson(optimizedState);
            var report = new Report
            {
                utc = DateTime.UtcNow.ToString("O"),
                scope = "Native Unity Editor model-only benchmark: saved original Simulation versus productive optimized Simulation, sharing current Domain helpers; synthetic established city fixture. Does not measure scene loading, texture import, player startup, personal progression or physical target devices.",
                unity = UnityEngine.Application.unityVersion,
                runtime = Type.GetType("Mono.Runtime") != null ? "Unity Editor Mono " + Environment.Version : Environment.Version.ToString(),
                platform = UnityEngine.Application.platform.ToString(), processor = SystemInfo.processorType,
                loadedDomainAssembly = typeof(Simulation).Assembly.Location,
                originalSourceSha256 = HashFile(OriginalSource), optimizedSourceSha256 = HashFile(OptimizedSource),
                referenceAssemblySha256 = HashFile(referencePath), domainAssemblySha256 = HashFile(typeof(Simulation).Assembly.Location),
                contentSha256 = HashFile(AssetDatabase.GetAssetPath(content.DefinitionsAsset)),
                initialStateSha256 = HashText(initialJson), originalStateSha256 = HashText(originalJson), optimizedStateSha256 = HashText(optimizedJson),
                seconds = Seconds, warmupSeconds = 1, buildings = prepared.buildings.Count, workers = prepared.workers.Count, cities = prepared.cities.Count,
                original = originalMeasurement, optimized = optimizedMeasurement,
                speedup = originalMeasurement.milliseconds / Math.Max(.000001, optimizedMeasurement.milliseconds),
                exactStateMatch = string.Equals(originalJson, optimizedJson, StringComparison.Ordinal),
                targetAccepted = false, releaseAccepted = false, humanAccepted = false
            };
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "/NativeModelOfflineBenchmark.json", JsonUtility.ToJson(report, true));
            if (!report.exactStateMatch)
            {
                File.WriteAllText(Folder + "/NativeModelOfflineBenchmark.original.json", originalJson);
                File.WriteAllText(Folder + "/NativeModelOfflineBenchmark.optimized.json", optimizedJson);
            }
            TestContext.Progress.WriteLine("Native Unity eight-hour model: original " + report.original.milliseconds.ToString("F1")
                + " ms, optimized " + report.optimized.milliseconds.ToString("F1") + " ms; exact state " + report.exactStateMatch);
            Assert.That(originalMeasurement.executedTicks, Is.EqualTo(Seconds));
            Assert.That(optimizedMeasurement.executedTicks, Is.EqualTo(Seconds));
            Assert.That(report.exactStateMatch, Is.True, "All serialized state fields must exactly match the original model");
        }
        private static Measurement Measure(Action<double, long?> advance, GameState state)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            long tickBefore = state.simulationSeconds;
            var watch = Stopwatch.StartNew();
            advance(Seconds, state.highWaterUtcTicks + TimeSpan.TicksPerSecond);
            watch.Stop();
            return new Measurement
            {
                milliseconds = watch.Elapsed.TotalMilliseconds, executedTicks = state.simulationSeconds - tickBefore,
                generation0Collections = GC.CollectionCount(0) - gen0,
                generation1Collections = GC.CollectionCount(1) - gen1,
                generation2Collections = GC.CollectionCount(2) - gen2
            };
        }
        private static GameState Prepare(GameDefinitions definitions)
        {
            // Keep this fixture identical to OfflineBenchmark.Prepare(defs, true) in measure_offline_simulation.ps1.
            var state = new GameState { playerName = "Benchmark", introSeen = true, tickRemainder = .375,
                highWaterUtcTicks = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc).Ticks };
            foreach (string id in definitions.instances.Select(instance => instance.cityId).Distinct())
                state.cities.Add(new CityState { id = id, charterAccepted = true, unlocked = Zone.Industry });
            int index = 0;
            foreach (var instance in definitions.instances)
            {
                var definition = definitions.Building(instance.family);
                var building = new BuildingState { id = instance.id, family = instance.family, cityId = instance.cityId, level = 500 };
                if (definition.kind == BuildingKind.Plot)
                {
                    building.family = index++ % 2 == 0 ? "residential_house" : "park";
                    definition = definitions.Building(building.family);
                }
                building.buildingSeconds = index++ % 7 == 0 ? 12 : 0;
                building.workSeconds = 3;
                building.raw = 50; building.refined = 20; building.butter = 10; building.pastry = 5;
                if (!string.IsNullOrEmpty(definition.managerRole)) AddWorker(state, building, definition.managerRole, 0);
                if (!string.IsNullOrEmpty(definition.driverRole))
                    for (int slot = 1; slot <= 2; slot++) AddWorker(state, building, definition.driverRole, slot);
                state.buildings.Add(building);
            }
            return state;
        }
        private static void AddWorker(GameState state, BuildingState building, string role, int slot)
        {
            var worker = new WorkerState { id = "bench_" + state.workers.Count, role = role, buildingId = building.id, slot = slot,
                level = 4, rank = 2, rarity = 4 };
            worker.attributes.AddRange((WorkerAttribute[])Enum.GetValues(typeof(WorkerAttribute)));
            state.workers.Add(worker);
        }
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

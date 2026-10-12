using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Weedtopia.Content;
using Weedtopia.Domain;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Persistence;

namespace Weedtopia.Tests.Assets
{
    public sealed class LargeSavePersistenceTests
    {
        private const string Baseline = "Docs/CommercialReadiness/2026-10-06/Baseline";
        private const string Evidence = "Docs/CommercialReadiness/2026-10-06";
        [Serializable] private sealed class ReferenceReceipt
        { public string status = default, referenceAssembly = default, referenceAssemblySha256 = default; }
        [Serializable] private sealed class Report
        {
            public string utc, scope, unity, status, stateSha256, validatorSourceSha256, referenceAssemblySha256;
            public int workers, receipts, repetitions;
            public long saveBytes;
            public double originalValidationMilliseconds, indexedValidationMilliseconds, saveMilliseconds, loadMilliseconds;
            public bool exactSerializedStateMatch, deviceAccepted;
        }

        private static Action<GameState, GameDefinitions> HistoricalValidator()
        {
            var receipt = JsonUtility.FromJson<ReferenceReceipt>(File.ReadAllText(Baseline + "/save_reference_compile.json"));
            Assert.That(receipt.status, Is.EqualTo("passed"));
            string path = Path.GetFullPath(receipt.referenceAssembly);
            Assert.That(path.StartsWith(Path.GetFullPath(Baseline) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Hash(path), Is.EqualTo(receipt.referenceAssemblySha256));
            var type = Assembly.LoadFrom(path).GetType("Weedtopia.Persistence.Reference.SaveValidator", true);
            return (Action<GameState, GameDefinitions>)Delegate.CreateDelegate(typeof(Action<GameState, GameDefinitions>), type.GetMethod("Validate"));
        }
        private static GameDefinitions Definitions() => AssetDatabase.LoadAssetAtPath<GameContent>(GameScaffold.ContentPath).Load();
        private static GameState Prepare(GameDefinitions definitions, int count)
        {
            var state = GameState.Create(definitions, "Großer Teststand", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
            var role = definitions.roles[0];
            for (int index = 0; index < count; index++)
                state.workers.Add(new WorkerState { id = "large_worker_" + index, role = role.id, attributes = { role.attributes[0] } });
            for (int index = Math.Max(0, count - RewardHistory.Capacity); index < count; index++)
                RewardHistory.Record(state, RewardSource.Recruitment, state.workers[index].id, "reward.source.recruitment", new[] { RewardItem.ForWorker(state.workers[index]) });
            return state;
        }

        [TestCase(1000), TestCase(10000), Category("Performance")]
        public void LargeWorkerInventoryAndBoundedReceiptsRoundTripWithoutChangingValidation(int count)
        {
            var definitions = Definitions(); var state = Prepare(definitions, count);
            var reference = HistoricalValidator(); reference(state, definitions); SaveValidator.Validate(state, definitions);
            var report = new Report
            {
                utc = DateTime.UtcNow.ToString("O"), scope = "Native Unity editor validation and real temporary atomic save/load of large unassigned-worker inventories with full bounded reward history. Eight repetitions, warmed validators, no timing threshold; excludes offline simulation, scene loading, cold caches and devices.",
                unity = UnityEngine.Application.unityVersion, status = "running", workers = count, receipts = state.rewardHistory.Count, repetitions = 8,
                validatorSourceSha256 = Hash("Assets/Weedtopia/Scripts/Runtime/Persistence/LocalSaveStore.cs"),
                referenceAssemblySha256 = Hash(Assembly.GetAssembly(reference.Method.DeclaringType).Location), deviceAccepted = false
            };
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < report.repetitions; i++) reference(state, definitions);
            report.originalValidationMilliseconds = watch.Elapsed.TotalMilliseconds;
            watch.Restart(); for (int i = 0; i < report.repetitions; i++) SaveValidator.Validate(state, definitions);
            report.indexedValidationMilliseconds = watch.Elapsed.TotalMilliseconds;
            string directory = Path.Combine(UnityEngine.Application.temporaryCachePath, "Weedtopia_LargeSave_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string before = JsonUtility.ToJson(state);
                var store = new LocalSaveStore(Path.Combine(directory, "save.json"), definitions);
                watch.Restart(); store.Save(state); report.saveMilliseconds = watch.Elapsed.TotalMilliseconds;
                report.saveBytes = new FileInfo(store.Path).Length;
                watch.Restart(); var loaded = store.Load(); report.loadMilliseconds = watch.Elapsed.TotalMilliseconds;
                Assert.That(loaded.State, Is.Not.Null); Assert.That(loaded.Recovered, Is.False);
                report.exactSerializedStateMatch = before == JsonUtility.ToJson(loaded.State);
                Assert.That(report.exactSerializedStateMatch, Is.True);
                Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before), "Validation/save may not mutate the active model.");
                report.stateSha256 = LocalSaveStore.Hash(before); report.status = "passed";
            }
            finally
            {
                Directory.Delete(directory, true);
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/NativeLargeSavePersistence-" + count + ".json", JsonUtility.ToJson(report, true));
            }
        }

        [Test]
        public void CorruptPrimaryRecoversItsExactBackupAndPreservesAClassifiableTransientCause()
        {
            var definitions=Definitions();var state=Prepare(definitions,1000);
            string expected=JsonUtility.ToJson(state);
            string directory=Path.Combine(UnityEngine.Application.temporaryCachePath,"Weedtopia_LoadCause_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var store=new LocalSaveStore(Path.Combine(directory,"save.json"),definitions);
                store.Save(state);state.money+=1;store.Save(state);
                const string corrupt="{\"format\":0,\"payload\":\"invalid\",\"sha256\":\"invalid\"}";
                File.WriteAllText(store.Path,corrupt);
                var recovered=store.Load();
                Assert.That(recovered.Recovered,Is.True);Assert.That(recovered.Error,Is.TypeOf<InvalidDataException>());
                Assert.That(recovered.Message,Is.EqualTo("ui.recovered"));
                Assert.That(JsonUtility.ToJson(recovered.State),Is.EqualTo(expected));
                Assert.That(File.ReadAllText(store.Path),Is.EqualTo(corrupt));
                File.Delete(store.Path+".bak");
                var invalid=store.Load();Assert.That(invalid.State,Is.Null);Assert.That(invalid.Error,Is.TypeOf<InvalidDataException>());
                Assert.That(invalid.Message,Is.EqualTo("save.invalid"));
            }
            finally { Directory.Delete(directory,true); }
        }

        [Test]
        public void IndexedValidationRetainsHistoricalRejectionAndNullIdentifierSemantics()
        {
            var definitions = Definitions(); var state = Prepare(definitions, 1000); var reference = HistoricalValidator();
            string source = JsonUtility.ToJson(state);
            Action<GameState>[] invalid =
            {
                s => s.workers.Add(s.workers[0]),
                s => s.workers[999].role = "missing_role",
                s => s.workers[999].buildingId = "missing_building",
                s => s.workers[999].attributes.Clear(),
                s => s.rewardHistory.Last().items[0].reference = "missing_worker",
                s => s.rewardHistory.Last().items[0].rarity = 4
            };
            foreach (var mutation in invalid)
            {
                var candidate = JsonUtility.FromJson<GameState>(source); mutation(candidate);
                Assert.Throws<InvalidDataException>(() => reference(candidate, definitions));
                Assert.Throws<InvalidDataException>(() => SaveValidator.Validate(candidate, definitions));
            }
            // Historical validation accepts one null ID on an unassigned worker. The index must
            // retain that behavior; strengthening the save schema requires a separate migration.
            state.workers[0].id = null; reference(state, definitions); SaveValidator.Validate(state, definitions);
        }
        private static string Hash(string path)
        { using (var hash = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
    }
}

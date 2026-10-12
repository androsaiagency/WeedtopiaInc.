using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Persistence;

namespace Weedtopia.Tests
{
    public sealed class RestartRecoveryTests
    {
        private string folder;
        private GameDefinitions definitions;
        private LocalSaveStore store;
        private DateTime start;
        [SetUp] public void Setup()
        {
            folder=Path.GetFullPath("Temp/RestartRecovery_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            definitions=ProgressionTests.Definitions();start=new DateTime(2026,10,6,8,0,0,DateTimeKind.Utc);
            store=new LocalSaveStore(Path.Combine(folder,"save.json"),definitions);
        }
        [TearDown] public void Cleanup(){if(Directory.Exists(folder))Directory.Delete(folder,true);}
        private void AssertBytes(byte[] expected,string file)=>Assert.That(expected.SequenceEqual(File.ReadAllBytes(file)),Is.True,file);
        private string ChecksumInvalid()
        {
            string payload=JsonUtility.ToJson(GameState.Create(definitions,"Previous owner",start));
            return JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256="invalid"});
        }
        private void SaveFreshAndAutosave()
        {
            var fresh=GameState.Create(definitions,"New owner",start);store.Save(fresh);fresh.money=999;store.Save(fresh);
            Assert.That(store.Load().State.playerName,Is.EqualTo("New owner"));
            Assert.That(store.Load().State.money,Is.EqualTo(new Amount(999)));
        }
        [Test] public void ChecksumInvalidPrimaryCanBeArchivedBeforeAnExplicitFreshGame()
        {
            File.WriteAllText(store.Path,ChecksumInvalid());byte[] original=File.ReadAllBytes(store.Path);
            var loaded=store.Load();Assert.That(loaded.State,Is.Null);Assert.That(loaded.Error,Is.TypeOf<InvalidDataException>());
            string archive=store.BackupForRestart();AssertBytes(original,archive);AssertBytes(original,store.Path);
            SaveFreshAndAutosave();AssertBytes(original,archive);
            Assert.That(Directory.GetFiles(folder,"save.json.corrupt_*").Length,Is.EqualTo(1));
        }
        [Test] public void InvalidPrimaryAndBackupBothSurviveLaterAutosavesAsOriginalBytes()
        {
            File.WriteAllText(store.Path,"{invalid primary");File.WriteAllText(store.Path+".bak",ChecksumInvalid());
            byte[] primary=File.ReadAllBytes(store.Path),backup=File.ReadAllBytes(store.Path+".bak");
            Assert.That(store.Load().State,Is.Null);
            string archive=store.BackupForRestart();AssertBytes(primary,archive);AssertBytes(backup,archive+".bak");
            AssertBytes(primary,store.Path);AssertBytes(backup,store.Path+".bak");
            SaveFreshAndAutosave();AssertBytes(primary,archive);AssertBytes(backup,archive+".bak");
        }
        [Test] public void InvalidBackupWithoutPrimaryIsArchivedBeforeAFreshGame()
        {
            File.WriteAllText(store.Path+".bak",ChecksumInvalid());byte[] original=File.ReadAllBytes(store.Path+".bak");
            Assert.That(store.Load().State,Is.Null);string archive=store.BackupForRestart();
            AssertBytes(original,archive);Assert.That(File.Exists(store.Path),Is.False);
            SaveFreshAndAutosave();AssertBytes(original,archive);
        }
        [Test] public void ValidRecoveredBackupKeepsTheExistingRestartArchiveContract()
        {
            var previous=GameState.Create(definitions,"Previous owner",start);store.Save(previous);previous.money=900;store.Save(previous);
            byte[] validBackup=File.ReadAllBytes(store.Path+".bak");File.WriteAllText(store.Path,ChecksumInvalid());
            byte[] invalidPrimary=File.ReadAllBytes(store.Path);var loaded=store.Load();
            Assert.That(loaded.State,Is.Not.Null);Assert.That(loaded.Recovered,Is.True);Assert.That(loaded.Error,Is.TypeOf<InvalidDataException>());
            string archive=store.BackupForRestart();AssertBytes(validBackup,archive);
            Assert.That(store.Decode(File.ReadAllText(archive)).money,Is.EqualTo(new Amount(1000)));
            SaveFreshAndAutosave();AssertBytes(validBackup,archive);
            string corrupt=Directory.GetFiles(folder,"save.json.corrupt_*").Single();AssertBytes(invalidPrimary,corrupt);
        }
        [Test] public void MissingFilesCannotProduceAFictitiousArchive()
        {
            Assert.Throws<InvalidDataException>(()=>store.BackupForRestart());Assert.That(Directory.GetFiles(folder),Is.Empty);
        }
        [Test] public void ExclusiveSourceLockPreventsRestartArchiveAndPreservesTheSave()
        {
            File.WriteAllText(store.Path,ChecksumInvalid());byte[] original=File.ReadAllBytes(store.Path);
            using(var locked=new FileStream(store.Path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                Assert.Throws<IOException>(()=>store.BackupForRestart());
            AssertBytes(original,store.Path);Assert.That(Directory.GetFiles(folder,"*.before_restart_*").Length,Is.Zero);
        }
    }
}

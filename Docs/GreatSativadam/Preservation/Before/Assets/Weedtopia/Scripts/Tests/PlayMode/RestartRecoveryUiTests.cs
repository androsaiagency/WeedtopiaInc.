using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object=UnityEngine.Object;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class RestartRecoveryUiTests
    {
        private GameRoot root;
        private string folder,path;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");
            root=Object.FindFirstObjectByType<GameRoot>();root.enabled=false;
            folder=Path.Combine(UnityEngine.Application.temporaryCachePath,"RestartRecoveryUi_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);path=Path.Combine(folder,"save.json");
            root.UseTestStore(path,new ManualClock(new DateTime(2026,10,6,8,0,0,DateTimeKind.Utc)));
            root.Ui.ShowStart();yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(root!=null)Object.Destroy(root.gameObject);yield return null;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        [UnityTest] public IEnumerator FailedInitialContinueAllowsConfirmedResetWithOriginalArchive()
        {yield return ConfirmedRecovery(false);}
        [UnityTest] public IEnumerator FailedContinueRetainsOldSessionUntilConfirmedResetAndArchivesBothInvalidFiles()
        {yield return ConfirmedRecovery(true);}
        private IEnumerator ConfirmedRecovery(bool previousSession)
        {
            if(previousSession)Assert.That(root.NewGame("Previous owner"),Is.True);
            var previous=root.Session;string previousState=previous==null?null:JsonUtility.ToJson(previous.State);
            File.WriteAllText(path,"{invalid primary");byte[] primary=File.ReadAllBytes(path);
            byte[] backup=null;
            if(previousSession){File.WriteAllText(path+".bak","{invalid backup");backup=File.ReadAllBytes(path+".bak");}
            Assert.That(root.ContinueResponsive(),Is.True);var timeout=System.Diagnostics.Stopwatch.StartNew();
            while(root.ContinueBusy&&timeout.Elapsed.TotalSeconds<10)yield return null;
            Assert.That(root.ContinueBusy,Is.False);Assert.That(root.ContinuationPending,Is.True);Assert.That(root.Session,Is.SameAs(previous));
            var view=root.Ui.Experience;view.startSettings.onClick.Invoke();yield return null;
            var reset=root.Ui.Element<Button>("ui.reset");Assert.That(reset.interactable,Is.True);reset.onClick.Invoke();yield return null;
            Assert.That(root.Ui.Element<RectTransform>("newGameConfirm").gameObject.activeSelf,Is.True);
            view.confirmRestart.onClick.Invoke();yield return null;
            Assert.That(root.ContinuationPending,Is.False);Assert.That(root.Session,Is.Not.Null.And.Not.SameAs(previous));
            Assert.That(root.Session.State.profileNamePending,Is.True);Assert.That(root.Ui.Element<RectTransform>("intro").gameObject.activeSelf,Is.True);
            if(previous!=null)Assert.That(JsonUtility.ToJson(previous.State),Is.EqualTo(previousState));
            string archive=Directory.GetFiles(folder,"save.json.before_restart_*.json").Single();
            Assert.That(primary.SequenceEqual(File.ReadAllBytes(archive)),Is.True);
            if(backup!=null)Assert.That(backup.SequenceEqual(File.ReadAllBytes(archive+".bak")),Is.True);
            var loaded=new Weedtopia.Persistence.LocalSaveStore(path,root.Definitions).Load();Assert.That(loaded.State,Is.Not.Null);
            Assert.That(loaded.State.profileNamePending,Is.True);Assert.That(loaded.Recovered,Is.False);
            LogAssert.NoUnexpectedReceived();
        }
    }
}

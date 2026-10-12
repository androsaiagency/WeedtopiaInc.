using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object=UnityEngine.Object;

namespace Weedtopia.Tests.PlayMode
{
    public sealed class SupportDiagnosticsUiTests
    {
        private GameRoot root;
        private string folder,path;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Weedtopia/Game/Scenes/BurningHills.unity");
            root=Object.FindFirstObjectByType<GameRoot>();root.enabled=false;
            folder=Path.Combine(UnityEngine.Application.temporaryCachePath,"SupportUi_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);path=Path.Combine(folder,"save.json");
            root.UseTestStore(path,new ManualClock(new DateTime(2026,10,6,8,0,0,DateTimeKind.Utc)));
            root.Ui.ShowStart();yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(root!=null)Object.Destroy(root.gameObject);yield return null;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        private void InjectCopier(Func<DiagnosticsCopyResult> copy)
            => typeof(UiExperience).GetField("diagnosticsCopy",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(root.Ui.Experience,copy);

        [UnityTest] public IEnumerator AuthoredStartSettingsExportsThroughFakeClipboardWithoutSessionOrSave()
        {
            var view=root.Ui.Experience;Assert.That(root.Session,Is.Null);
            Assert.That(view.startSettings,Is.Not.Null);Assert.That(view.copyDiagnostics,Is.Not.Null);
            Canvas.ForceUpdateCanvases();var rect=(RectTransform)view.startSettings.transform;
            var data=new PointerEventData(EventSystem.current){position=rect.TransformPoint(rect.rect.center)};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(view.startSettings));
            view.startSettings.onClick.Invoke();yield return null;
            Assert.That(root.Ui.Element<RectTransform>("settings").gameObject.activeSelf,Is.True);
            Assert.That(view.profileTab.interactable,Is.False);Assert.That(view.profileScroll.gameObject.activeSelf,Is.False);
            Assert.That(root.Ui.Element<Slider>("volume").interactable,Is.False);Assert.That(root.Ui.Element<Slider>("scale").interactable,Is.False);
            Assert.That(root.Ui.Element<Button>("ui.restart_intro").interactable,Is.False);
            string snapshot=JsonUtility.ToJson(LocalPlayerDiagnostics.CreateTimingEvent(LocalPlayerDiagnostics.Operation.StartupLoad,1,0,0,0))+"\n",written=null;
            InjectCopier(()=>PlayerDiagnosticsClipboard.Copy(()=>snapshot,s=>written=s));view.copyDiagnostics.onClick.Invoke();
            Assert.That(written,Is.EqualTo(snapshot));Assert.That(root.Ui.Element<Text>("message").text,Is.EqualTo(root.Definitions.Text("settings.diagnostics.copied")));
            Assert.That(root.Session,Is.Null);Assert.That(File.Exists(path),Is.False);
            root.Ui.Close();Assert.That(root.Ui.Element<RectTransform>("start").gameObject.activeSelf,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FailedInitialContinueStillExposesSupportAndLocalizedEmptyOrFailureFeedback()
        {
            File.WriteAllText(path,"{invalid");string disk=File.ReadAllText(path);
            Assert.That(root.ContinueResponsive(),Is.True);
            var timeout=System.Diagnostics.Stopwatch.StartNew();
            while(root.ContinueBusy&&timeout.Elapsed.TotalSeconds<10)yield return null;
            Assert.That(root.ContinueBusy,Is.False);Assert.That(root.ContinuationPending,Is.True);Assert.That(root.Session,Is.Null);
            var view=root.Ui.Experience;view.startSettings.onClick.Invoke();yield return null;
            Assert.That(root.Ui.Element<RectTransform>("settings").gameObject.activeSelf,Is.True);
            bool wrote=false;InjectCopier(()=>PlayerDiagnosticsClipboard.Copy(()=>"",_=>wrote=true));
            view.copyDiagnostics.onClick.Invoke();Assert.That(wrote,Is.False);
            Assert.That(root.Ui.Element<Text>("message").text,Is.EqualTo(root.Definitions.Text("settings.diagnostics.empty")));
            InjectCopier(()=>PlayerDiagnosticsClipboard.Copy(()=>"{}\n",_=>throw new IOException("Owned fake clipboard failure")));
            view.copyDiagnostics.onClick.Invoke();
            Assert.That(root.Ui.Element<Text>("message").text,Is.EqualTo(root.Definitions.Text("settings.diagnostics.failed")));
            Assert.That(File.ReadAllText(path),Is.EqualTo(disk));Assert.That(root.Session,Is.Null);
            root.Ui.Close();Assert.That(root.Ui.Element<RectTransform>("start").gameObject.activeSelf,Is.True);
            Assert.That(root.Ui.Element<Button>("ui.start").interactable,Is.True);LogAssert.NoUnexpectedReceived();
        }
    }
}

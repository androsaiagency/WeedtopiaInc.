using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class SupportDiagnosticsTests
    {
        [Test] public void LaunchEventHasOnlyFixedLifecycleCodeAndTechnicalBuildContext()
        {
            var record=LocalPlayerDiagnostics.CreateLaunchEvent();
            Assert.That(record.type,Is.EqualTo("Lifecycle"));Assert.That(record.code,Is.EqualTo("PlayerStarted"));
            Assert.That(record.operation,Is.Empty);Assert.That(record.frames,Is.Empty);
            Assert.That(DateTime.TryParse(record.utc,out _),Is.True);
            Assert.That(record.elapsedMs,Is.EqualTo(-1));Assert.That(record.offlineSeconds,Is.EqualTo(-1));
            Assert.That(record.workers,Is.EqualTo(-1));Assert.That(record.buildings,Is.EqualTo(-1));
            foreach(string token in new[]{record.buildGuid,record.unity,record.version,record.platform})
                Assert.That(token,Does.Not.Contain("\\").And.Not.Contain("/"));
        }
        [TestCase(null),TestCase(""),TestCase(" \n\t")]
        public void EmptySnapshotLeavesTheExistingClipboardUntouched(string snapshot)
        {
            bool wrote=false;
            Assert.That(PlayerDiagnosticsClipboard.Copy(()=>snapshot,_=>wrote=true),Is.EqualTo(DiagnosticsCopyResult.Empty));
            Assert.That(wrote,Is.False);
        }
        [Test] public void ReadAndClipboardFailureReturnFailureWithoutExposingPrivateExceptionText()
        {
            bool wrote=false;
            Assert.That(PlayerDiagnosticsClipboard.Copy(()=>throw new IOException("Private save path"),_=>wrote=true),Is.EqualTo(DiagnosticsCopyResult.Failed));
            Assert.That(wrote,Is.False);
            Assert.That(PlayerDiagnosticsClipboard.Copy(()=>"{}\n",_=>throw new InvalidOperationException("Private clipboard detail")),Is.EqualTo(DiagnosticsCopyResult.Failed));
        }
        [Test] public void ClipboardByteQuotaCountsUtf8AndAcceptsTheExactBoundary()
        {
            string written=null;
            Assert.That(PlayerDiagnosticsClipboard.Copy(()=>new string('ä',LocalPlayerDiagnostics.MaximumFileBytes/2+1),s=>written=s),Is.EqualTo(DiagnosticsCopyResult.Failed));
            Assert.That(written,Is.Null);
            var exact=new string('x',LocalPlayerDiagnostics.MaximumFileBytes);
            Assert.That(PlayerDiagnosticsClipboard.Copy(()=>exact,s=>written=s),Is.EqualTo(DiagnosticsCopyResult.Copied));
            Assert.That(written,Is.SameAs(exact));
        }
        [Test] public void InjectedClipboardReceivesOnlyTheRedactedTechnicalSnapshot()
        {
            var record=LocalPlayerDiagnostics.CreateEvent(LogType.Exception,"IOException: Alice private-save.json token=secret",
                "at Weedtopia.Unity.GameRoot.Continue (Alice private-save.json)\n");
            string snapshot=JsonUtility.ToJson(record)+"\n",written=null;
            Assert.That(snapshot,Does.Not.Contain("Alice").And.Not.Contain("private-save").And.Not.Contain("secret"));
            Assert.That(PlayerDiagnosticsClipboard.Copy(()=>snapshot,s=>written=s),Is.EqualTo(DiagnosticsCopyResult.Copied));
            Assert.That(written,Is.EqualTo(snapshot));
        }
        [Test] public void RotatedSnapshotKeepsNewestCompleteEventsWithinTheUtf8Quota()
        {
            string folder=Path.Combine(UnityEngine.Application.temporaryCachePath,"SupportSnapshot_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);string path=Path.Combine(folder,"events.ndjson");
            try
            {
                string Event(string code)=>JsonUtility.ToJson(new LocalPlayerDiagnostics.TechnicalEvent {code=code,frames=new[]{new string('ä',160)}})+"\n";
                string Fill(string prefix)
                {
                    var builder=new StringBuilder();int bytes=0;
                    for(int i=0;;i++)
                    {
                        string line=Event(prefix+i);int size=Encoding.UTF8.GetByteCount(line);
                        if(bytes+size>=LocalPlayerDiagnostics.MaximumFileBytes-1000)break;
                        builder.Append(line);bytes+=size;
                    }
                    return builder.ToString();
                }
                string previous=Fill("old_"),current=Fill("new_");
                File.WriteAllText(path+".previous",previous,new UTF8Encoding(false));
                File.WriteAllText(path,current+"{incomplete",new UTF8Encoding(false));
                string snapshot=LocalPlayerDiagnostics.ReadSnapshotFiles(path);
                Assert.That(Encoding.UTF8.GetByteCount(snapshot),Is.LessThanOrEqualTo(LocalPlayerDiagnostics.MaximumFileBytes));
                Assert.That(snapshot,Does.Contain("old_").And.Contain("new_").And.Not.Contain("{incomplete"));
                Assert.That(snapshot,Does.Not.Contain("\"code\":\"old_0\""));
                Assert.That(snapshot.EndsWith("\n",StringComparison.Ordinal),Is.True);
                foreach(string line in snapshot.Split('\n').Where(l=>l.Length>0))
                    Assert.That(JsonUtility.FromJson<LocalPlayerDiagnostics.TechnicalEvent>(line),Is.Not.Null);
                Assert.That(snapshot.EndsWith(current.Substring(current.LastIndexOf('\n',current.Length-2)+1),StringComparison.Ordinal),Is.True);
            }
            finally {Directory.Delete(folder,true);}
        }
        [Test] public void OversizedCurrentFileCannotSuppressTheValidPreviousJournal()
        {
            string folder=Path.Combine(UnityEngine.Application.temporaryCachePath,"SupportQuota_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);string path=Path.Combine(folder,"events.ndjson");
            try
            {
                string eventLine=JsonUtility.ToJson(LocalPlayerDiagnostics.CreateTimingEvent(LocalPlayerDiagnostics.Operation.StartupLoad,1,0,0,0))+"\n";
                File.WriteAllText(path+".previous",eventLine,new UTF8Encoding(false));
                File.WriteAllText(path,new string('x',LocalPlayerDiagnostics.MaximumFileBytes+1),new UTF8Encoding(false));
                Assert.That(LocalPlayerDiagnostics.ReadSnapshotFiles(path),Is.EqualTo(eventLine));
                File.Delete(path+".previous");Assert.That(LocalPlayerDiagnostics.ReadSnapshotFiles(path),Is.Empty);
            }
            finally {Directory.Delete(folder,true);}
        }
    }
}

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class LuckyWheelAssetTests
    {
        [Test] public void InstallerPreservesInstalledPrefabsContentSceneAndAllTheirGuids()
        {
            var files=Directory.GetFiles(LuckyWheelAuthoring.Folder,"*",SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(RewardPresentationAuthoring.Folder,"*",SearchOption.AllDirectories))
                .Concat(new[]{GameScaffold.GameScene,GameScaffold.GameScene+".meta","Assets/Weedtopia/Game/Content/BurningHills.json"}).ToArray();
            var before=files.ToDictionary(p=>p,File.ReadAllBytes);LuckyWheelAuthoring.Apply();
            foreach(var pair in before)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(pair.Key),pair.Key);
            var prefab=AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(LuckyWheelAuthoring.PanelPath);
            var ui=prefab.GetComponent<LuckyWheelUi>();Assert.That(ui.slots.Length,Is.EqualTo(10));
            Assert.That(ui.slots.All(s=>s!=null&&s.button!=null&&s.icon!=null&&s.caption!=null),Is.True);
            File.WriteAllText("Docs/LuckyWheel/idempotence_receipt.json","{\"status\":\"passed\",\"preservedFiles\":"+files.Length+",\"authoredSlots\":10}");
        }
    }
}

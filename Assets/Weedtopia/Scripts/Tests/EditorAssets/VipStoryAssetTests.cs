using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class VipStoryAssetTests
    {
        [Test] public void InstallerIsIdempotentAndPreservesAllExistingBindingsAndNewAssetGuids()
        {
            var paths = Directory.GetFiles(VipStoriesAuthoring.Folder, "*", SearchOption.AllDirectories)
                .Concat(new[] { GameScaffold.GameScene, GameScaffold.GameScene + ".meta", "Assets/Weedtopia/Game/Content/BurningHills.json" }).ToArray();
            var before = paths.ToDictionary(p => p, File.ReadAllBytes); VipStoriesAuthoring.Apply(); VipStoriesAuthoring.RefinePresentation();
            foreach (var pair in before) CollectionAssert.AreEqual(pair.Value, File.ReadAllBytes(pair.Key), pair.Key);
            File.WriteAllText("Docs/VipStories/idempotence_receipt.json", "{\"status\":\"passed\",\"preservedFiles\":" + paths.Length + "}");
        }
        [Test] public void AllTenDistinctSpritesFifteenRowsAndFortyMemoriesAreAuthored()
        {
            var art = AssetDatabase.LoadAssetAtPath<VipVisualCatalog>(VipStoriesAuthoring.CatalogPath);
            Assert.That(art.entries.Length, Is.EqualTo(10)); Assert.That(art.entries.All(e => e.body != null && e.portrait != null), Is.True);
            Assert.That(art.entries.Select(e => e.body.texture).Distinct().Count(), Is.EqualTo(10));
            var world = AssetDatabase.LoadAssetAtPath<GameObject>(VipStoriesAuthoring.WorldPath).GetComponent<VipTownPresentation>();
            Assert.That(world.actors.Length, Is.EqualTo(10)); Assert.That(world.actors.Select(a => a.vipId).Distinct().Count(), Is.EqualTo(10));
            Assert.That(world.actors.All(a => !a.visibleRoot.activeSelf && !a.selectionBox.enabled && a.memories.Select(m => m.chapter).Distinct().Count() == 4), Is.True);
            Assert.That(world.actors.All(a=>a.marker.transform.parent==a.artwork),Is.True);
            var ui = AssetDatabase.LoadAssetAtPath<GameObject>(VipStoriesAuthoring.PanelPath).GetComponent<VipStoriesUi>();
            Assert.That(ui.rows.Length, Is.EqualTo(15)); Assert.That(ui.goals.Length, Is.EqualTo(6));
            Assert.That(ui.detailScroll.content != null && ui.rosterScroll.content != null, Is.True);
            Assert.That(new[] {ui.questTitle,ui.description,ui.reward,ui.memories}.All(t=>t.GetComponent<VipStoryParagraph>()?.label==t),Is.True);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(VipStoriesAuthoring.JournalEntryPath).GetComponent<UnityEngine.UI.Button>(),Is.Not.Null);
        }
    }
}

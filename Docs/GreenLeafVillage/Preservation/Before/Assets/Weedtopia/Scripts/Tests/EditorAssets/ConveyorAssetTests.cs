using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class ConveyorAssetTests
    {
        [Test] public void AuthoredNetworkUsesWideReusableSpritesAndSharesEveryOverlappingSpan()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var root=Object.FindFirstObjectByType<GameRoot>();var net=root.Presentation.conveyors;
                Assert.That(net.routes.Length,Is.EqualTo(9));Assert.That(net.sections.Count(s=>!s.junction),Is.EqualTo(18));
                Assert.That(net.sections.Count(s=>s.junction),Is.EqualTo(20));
                Assert.That(net.sections.Where(s=>!s.junction).Select(s=>s.transform.position).Distinct().Count(),Is.EqualTo(18));
                Assert.That(net.sections.Any(s=>s.owners.Length==8),Is.True);
                foreach(var section in net.sections)
                {
                    Assert.That(PrefabUtility.IsPartOfPrefabInstance(section),Is.True);
                    Assert.That(ShaderUtil.ShaderHasError(section.surface.sharedMaterial.shader),Is.False);
                    Assert.That(section.owners,Is.Not.Empty);
                    if(!section.junction)Assert.That(section.transform.localScale.z,Is.EqualTo(6));
                }
                foreach(var route in net.routes)
                {
                    Assert.That(route.belt.enabled,Is.False);
                    Assert.That(route.cargoMarkers.Length,Is.EqualTo(3));
                    Assert.That(route.conveyorPath.First(),Is.EqualTo(route.points.First()));
                    Assert.That(route.conveyorPath.Last(),Is.EqualTo(route.points.Last()));
                    foreach(var cargo in route.cargoMarkers)Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(cargo),Is.EqualTo(ConveyorAuthoring.CargoPrefab));
                }
                Assert.That(net.GetComponentsInChildren<Collider>(true),Is.Empty);
                foreach(var name in new[]{"ConveyorStraight","ConveyorJunction","HarvestTray"})
                {
                    var importer=(TextureImporter)AssetImporter.GetAtPath(ConveyorAuthoring.Folder+"/Art/"+name+".png");
                    Assert.That(importer.DoesSourceTextureHaveAlpha(),Is.True);Assert.That(importer.mipmapEnabled,Is.True);
                }
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        [Test] public void ReapplyingConveyorAuthoringPreservesTheScene()
        {
            var before=File.ReadAllBytes(GameScaffold.GameScene);ConveyorAuthoring.Apply();
            Assert.That(File.ReadAllBytes(GameScaffold.GameScene),Is.EqualTo(before));
        }
    }
}

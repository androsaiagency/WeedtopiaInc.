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
    public sealed class ConstructionFenceAssetTests
    {
        [Test] public void EverySiteInheritsAnInactiveReusableEnclosureWithoutBlockingSelection()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var root = Object.FindFirstObjectByType<GameRoot>();
                foreach (var site in root.Sites)
                {
                    var fence = site.ConstructionFence; Assert.That(fence, Is.Not.Null, site.InstanceId);
                    Assert.That(fence.gameObject.activeSelf, Is.False);
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(fence), Is.EqualTo(ConstructionFenceAuthoring.FencePrefab));
                    Assert.That(fence.GetComponentsInChildren<Collider>(true), Is.Empty);
                    Assert.That(fence.Sides.Length, Is.EqualTo(4)); Assert.That(fence.Posts.Length, Is.EqualTo(4));
                    Assert.That(fence.Sides.All(s => s.drawMode == SpriteDrawMode.Tiled && s.sprite != null), Is.True);
                    Assert.That(fence.Posts.All(s => s.sprite != null), Is.True);
                }
                foreach (var name in new[] { "FencePanel", "WarningPost" })
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(ConstructionFenceAuthoring.Folder + "/Art/" + name + ".png");
                    Assert.That(importer.DoesSourceTextureHaveAlpha(), Is.True); Assert.That(importer.mipmapEnabled, Is.True);
                }
            }
            finally { if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        [Test] public void ReinstallationPreservesTheSceneAndAuthoredPrefab()
        {
            byte[] scene = File.ReadAllBytes(GameScaffold.GameScene), prefab = File.ReadAllBytes(ConstructionFenceAuthoring.SitePrefab);
            ConstructionFenceAuthoring.Apply();
            Assert.That(File.ReadAllBytes(GameScaffold.GameScene).SequenceEqual(scene), Is.True);
            Assert.That(File.ReadAllBytes(ConstructionFenceAuthoring.SitePrefab).SequenceEqual(prefab), Is.True);
        }
    }
}

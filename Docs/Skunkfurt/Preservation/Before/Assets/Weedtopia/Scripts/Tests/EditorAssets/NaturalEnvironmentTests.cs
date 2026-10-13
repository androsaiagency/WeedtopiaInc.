using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class NaturalEnvironmentTests
    {
        [Test] public void NaturalLandscapeIsSerializedWithReusableArtAndCannotInterceptBuildingInput()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var landscape=Object.FindFirstObjectByType<NaturalEnvironmentStamp>();Assert.That(landscape,Is.Not.Null);
                Assert.That(landscape.authoredPlants,Is.GreaterThan(500));
                Assert.That(landscape.GetComponentsInChildren<Collider>(),Is.Empty,"Decorative planting must not intercept gameplay picks.");
                var artwork=landscape.GetComponentsInChildren<SpriteRenderer>();Assert.That(artwork.Length,Is.GreaterThan(500));
                foreach(var sprite in artwork)
                {
                    Assert.That(sprite.sprite,Is.Not.Null,sprite.name);Assert.That(PrefabUtility.IsPartOfPrefabInstance(sprite),Is.True,sprite.name);
                    Assert.That(sprite.sharedMaterial,Is.Not.Null,sprite.name);Assert.That(ShaderUtil.ShaderHasError(sprite.sharedMaterial.shader),Is.False,sprite.name);
                }
                Assert.That(landscape.transform.Find("FlowingTurquoiseRiver"),Is.Not.Null);
                Assert.That(landscape.transform.Find("WoodlandPond"),Is.Not.Null);
                Assert.That(GameObject.Find("BurningHills_Districts").transform.Find("ContinuousCountryside").gameObject.activeSelf,Is.False);
                var manifest=JsonUtility.FromJson<NaturalEnvironmentAuthoring.ArtManifest>(File.ReadAllText(NaturalEnvironmentAuthoring.Folder+"/art_manifest.json"));
                Assert.That(manifest.sprites.Select(x=>x.id).Distinct().Count(),Is.EqualTo(16));
                foreach(var entry in manifest.sprites)
                {
                    var sprite=AssetDatabase.LoadAllAssetsAtPath(NaturalEnvironmentAuthoring.Folder+"/Art/"+entry.file).OfType<Sprite>().Single(x=>x.name==entry.id);
                    Assert.That(sprite.rect.width,Is.GreaterThan(100));Assert.That(sprite.pivot.y,Is.Zero);
                }
            }
            finally {if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        [Test] public void ReapplyingNaturalLandscapePreservesAuthoredSceneBytes()
        {
            byte[] before;using(var sha=SHA256.Create())before=sha.ComputeHash(File.ReadAllBytes(GameScaffold.GameScene));
            NaturalEnvironmentAuthoring.Apply();
            using(var sha=SHA256.Create())Assert.That(sha.ComputeHash(File.ReadAllBytes(GameScaffold.GameScene)),Is.EqualTo(before));
        }
    }
}

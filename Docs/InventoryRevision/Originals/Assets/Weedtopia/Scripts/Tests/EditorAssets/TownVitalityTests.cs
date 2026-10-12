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
    public sealed class TownVitalityTests
    {
        [Test] public void NewTownDetailsAreReusableDoNotInterceptInputAndLeaveBuildingParcelsClear()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var stamp=Object.FindFirstObjectByType<TownVitalityStamp>();
                Assert.That(stamp,Is.Not.Null);
                Assert.That(stamp.GetComponentsInChildren<Collider>(true),Is.Empty);
                var plan=JsonUtility.FromJson<BurningHillsLayoutInstaller.LayoutPlan>(File.ReadAllText(BurningHillsLayoutInstaller.Folder+"/BurningHillsLayout.json"));
                var art=stamp.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(r=>AssetDatabase.GetAssetPath(r.sprite).StartsWith(TownVitalityAuthoring.Folder+"/Art/")).ToArray();
                Assert.That(art.Select(r=>r.sprite).Distinct().Count(),Is.EqualTo(16));
                Assert.That(art.Length,Is.GreaterThan(100));
                foreach(var renderer in art)
                {
                    Assert.That(PrefabUtility.IsPartOfPrefabInstance(renderer),Is.True);
                    Assert.That(ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader),Is.False);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(renderer.sprite));
                    Assert.That(importer.alphaIsTransparency,Is.True);
                    Assert.That(importer.mipmapEnabled,Is.True);
                    var point=DistrictMap.ToPlan(renderer.transform.parent.position);
                    Assert.That(plan.sites.Any(s=>Mathf.Abs(s.u-point.x)<12&&Mathf.Abs(s.v-point.y)<17),Is.False,renderer.transform.parent.name+" occupies a building parcel");
                }
                Assert.That(stamp.preservedStreetTrees.Length,Is.EqualTo(20));
                Assert.That(stamp.preservedStreetTrees.All(t=>t!=null&&!t.activeSelf),Is.True);
                Assert.That(stamp.preservedStreetTrees.All(PrefabUtility.IsPartOfPrefabInstance),Is.True);
                var life=Object.FindFirstObjectByType<LivingTownPresentation>();
                var added=life.actors.Where(a=>a.anchor.IsChildOf(stamp.transform)).ToArray();
                Assert.That(added.Length,Is.EqualTo(34));
                Assert.That(added.All(a=>a.animation!=null&&a.path.Length>1),Is.True);
                Assert.That(added.Select(a=>a.anchor).Distinct().Count(),Is.EqualTo(added.Length));
            }
            finally {if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }

        [Test] public void ExplicitAuthoringIsIdempotentAndKeepsExistingSceneBytes()
        {
            byte[] before;
            using(var sha=SHA256.Create())before=sha.ComputeHash(File.ReadAllBytes(GameScaffold.GameScene));
            TownVitalityAuthoring.Apply();
            using(var sha=SHA256.Create())Assert.That(sha.ComputeHash(File.ReadAllBytes(GameScaffold.GameScene)),Is.EqualTo(before));
        }
    }
}


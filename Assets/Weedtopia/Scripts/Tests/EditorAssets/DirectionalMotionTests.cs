using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Unity;
using Weedtopia.Editor.GameSetup;

namespace Weedtopia.Tests.Assets
{
    public sealed class DirectionalMotionTests
    {
        [Test] public void EveryMovingInstanceHasCompleteViewsAndTheVanMatchesHumanScale()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var life=Object.FindFirstObjectByType<LivingTownPresentation>();
                Assert.That(life.actors.All(a=>a.animation!=null),Is.True);
                var motions=Object.FindObjectsByType<DirectionalSpriteAnimator>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                var vitality=Object.FindFirstObjectByType<TownVitalityStamp>();
                var corners=Object.FindFirstObjectByType<WorldCornersStamp>();
                Assert.That(motions.Length,Is.EqualTo(110+(vitality!=null?vitality.residents:0)+(corners!=null?corners.residents+corners.wildlife:0)));
                foreach(var motion in motions)
                {
                    Assert.That(PrefabUtility.IsPartOfPrefabInstance(motion),Is.True);
                    Assert.That(motion.animationSet.views.Length,Is.EqualTo(8));
                    foreach(var view in motion.animationSet.views)
                    {
                        Assert.That(view.frames.Length,Is.EqualTo(6));Assert.That(view.frames.All(f=>f!=null),Is.True);
                        Assert.That(view.frames.Distinct().Count(),Is.EqualTo(6));Assert.That(view.scale,Is.GreaterThan(0));
                    }
                    Assert.That(motion.GetComponentsInChildren<Collider>(true),Is.Empty,"Moving decoration cannot intercept building input");
                }
                var van=AssetDatabase.LoadAssetAtPath<DirectionalSpriteSet>(DirectionalMotionAuthoring.Folder+"/Animations/van.asset");
                float width=van.views[0].frames[0].rect.width/100*van.views[0].scale;
                Assert.That(width,Is.InRange(8.5f,10.5f),"Van length should be about twice an adult's height");
                foreach(var vehicle in Object.FindObjectsByType<DeliveryView>(FindObjectsSortMode.None))
                    Assert.That(new SerializedObject(vehicle).FindProperty("pickupVanScale").floatValue,Is.EqualTo(1),"All adult-sized vans share the same scale");
            }
            finally {if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }

        [Test] public void GroundDirectionDoesNotConfuseCameraForeshorteningOrReturnTrips()
        {
            var go=new GameObject("Direction test camera",typeof(Camera));
            try
            {
                var camera=go.GetComponent<Camera>();camera.transform.rotation=Quaternion.Euler(35.264f,45,0);
                for(int direction=0;direction<8;direction++)
                {
                    float angle=direction*Mathf.PI/4;
                    var heading=DistrictMap.ToWorld(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)));
                    Assert.That(DirectionalSpriteSet.Sector(DirectionalSpriteSet.GroundHeading(heading,camera)),Is.EqualTo(direction));
                    Assert.That(DirectionalSpriteSet.Sector(DirectionalSpriteSet.GroundHeading(-heading,camera)),Is.EqualTo((direction+4)%8));
                }
                Assert.That(DirectionalSpriteSet.Sector(Vector2.zero,5),Is.EqualTo(5));
            }
            finally {Object.DestroyImmediate(go);}
        }

        [Test] public void ReapplyingDirectionalAuthoringPreservesSceneAndPrefabBytes()
        {
            var paths=Directory.GetFiles(DirectionalMotionAuthoring.Folder+"/Animations","*.asset").Concat(Directory.GetFiles(LivingHillsAuthoring.Folder+"/Prefabs","*.prefab"))
                .Concat(new[]{GameScaffold.GameScene,ProductionArtImporter.Root+"/Prefabs/DeliveryTruck.prefab"}).ToArray();
            string Hash(string path){using(var sha=SHA256.Create())return System.Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));}
            var before=paths.ToDictionary(p=>p,Hash);DirectionalMotionAuthoring.Apply();
            foreach(var path in paths)Assert.That(Hash(path),Is.EqualTo(before[path]),path);
        }
    }
}

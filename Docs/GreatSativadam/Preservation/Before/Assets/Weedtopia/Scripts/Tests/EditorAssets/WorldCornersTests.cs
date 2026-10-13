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
    public sealed class WorldCornersTests
    {
        [Test] public void DestinationsConnectToTownWithoutBlockingBuildingParcelsOrInput()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var stamp = Object.FindFirstObjectByType<WorldCornersStamp>();
                Assert.That(stamp, Is.Not.Null);
                Assert.That(stamp.GetComponentsInChildren<Collider>(true), Is.Empty);
                var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(BurningHillsLayoutInstaller.MapPath);
                var bounds = map.OverviewBounds(Mathf.Sin(35.264f*Mathf.Deg2Rad));
                var plan = JsonUtility.FromJson<BurningHillsLayoutInstaller.LayoutPlan>(File.ReadAllText(BurningHillsLayoutInstaller.Folder+"/BurningHillsLayout.json"));
                foreach(var renderer in stamp.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    Assert.That(PrefabUtility.IsPartOfPrefabInstance(renderer), Is.True, renderer.name);
                    Assert.That(renderer.sprite, Is.Not.Null);
                    Assert.That(ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader), Is.False);
                    var point = DistrictMap.ToPlan(renderer.transform.parent.position);
                    Assert.That(bounds.Contains(point), Is.True, renderer.transform.parent.name);
                    Assert.That(plan.sites.Any(s=>Mathf.Abs(s.u-point.x)<12&&Mathf.Abs(s.v-point.y)<17), Is.False,
                        renderer.transform.parent.name+" occupies a productive parcel");
                }
                // Known attachment points on the existing village road, farm arrival, pond walk and park.
                var townConnections = new[] { new Vector2(-38,306), new Vector2(232,-75), new Vector2(267,205), new Vector2(-143,-176) };
                var connected = stamp.walks.Where(w=>townConnections.Any(p=>Near(p,w.points))).ToList();
                bool changed;
                do
                {
                    changed=false;
                    foreach(var walk in stamp.walks.Where(w=>!connected.Contains(w)).ToArray())
                        if(connected.Any(other=>walk.points.Any(p=>Near(p,other.points)))) { connected.Add(walk); changed=true; }
                } while(changed);
                Assert.That(connected.Count, Is.EqualTo(stamp.walks.Length), "A countryside path is disconnected from the village.");
                foreach(var point in stamp.destinationEntrances)
                    Assert.That(connected.Any(w=>Near(point,w.points)), Is.True, "Destination has no walkable approach: "+point);
                foreach(var walk in stamp.walks)
                    for(int i=1;i<walk.points.Length;i++) for(int sample=0;sample<=30;sample++)
                    {
                        var p=Vector2.Lerp(walk.points[i-1],walk.points[i],sample/30f);
                        Assert.That(bounds.Contains(p),Is.True,walk.name);
                        Assert.That(plan.sites.Any(s=>Mathf.Abs(s.u-p.x)<12&&Mathf.Abs(s.v-p.y)<14),Is.False,walk.name+" crosses a productive parcel");
                    }
                var life = Object.FindFirstObjectByType<LivingTownPresentation>();
                var added=life.actors.Where(a=>a.anchor.IsChildOf(stamp.transform)).ToArray();
                Assert.That(added.Count(a=>!a.airborne),Is.EqualTo(stamp.residents));
                Assert.That(added.Count(a=>a.airborne),Is.EqualTo(stamp.wildlife));
                foreach(var actor in added.Where(a=>!a.airborne)) foreach(var point in actor.path)
                    Assert.That(stamp.walks.Any(w=>Near(DistrictMap.ToPlan(point),w.points)),Is.True,actor.anchor.name+" has left the authored path");
            }
            finally { if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        private static bool Near(Vector2 p,Vector2[] points)
        {
            for(int i=1;i<points.Length;i++)
            {
                var d=points[i]-points[i-1];
                var nearest=points[i-1]+d*Mathf.Clamp01(Vector2.Dot(p-points[i-1],d)/Mathf.Max(.0001f,d.sqrMagnitude));
                if(Vector2.Distance(p,nearest)<2) return true;
            }
            return false;
        }
        [Test] public void ReapplyingCornersPreservesTheSavedScene()
        {
            byte[] before;
            using(var sha=SHA256.Create()) before=sha.ComputeHash(File.ReadAllBytes(GameScaffold.GameScene));
            WorldCornersAuthoring.Apply();
            using(var sha=SHA256.Create()) Assert.That(sha.ComputeHash(File.ReadAllBytes(GameScaffold.GameScene)), Is.EqualTo(before));
        }
    }
}

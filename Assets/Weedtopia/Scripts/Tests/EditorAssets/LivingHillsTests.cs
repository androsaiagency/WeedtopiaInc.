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
    public sealed class LivingHillsTests
    {
        [Test] public void TownLifeIsAuthoredReusableAndKeepsPedestriansOutsideBuildingParcels()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);var life=Object.FindFirstObjectByType<LivingTownPresentation>();
                Assert.That(life,Is.Not.Null);Assert.That(life.root,Is.Not.Null);Assert.That(life.authoredLandmarks,Is.EqualTo(29));
                var vitality=Object.FindFirstObjectByType<TownVitalityStamp>();
                var corners=Object.FindFirstObjectByType<WorldCornersStamp>();
                Assert.That(life.actors.Count(a=>!a.airborne),Is.EqualTo(26+(vitality!=null?vitality.residents:0)+(corners!=null?corners.residents:0)));Assert.That(life.actors.Count(a=>a.airborne),Is.EqualTo(24+(corners!=null?corners.wildlife:0)));
                Assert.That(life.GetComponentsInChildren<Collider>(true),Is.Empty,"Decoration cannot intercept input.");
                var plan=JsonUtility.FromJson<BurningHillsLayoutInstaller.LayoutPlan>(File.ReadAllText(BurningHillsLayoutInstaller.Folder+"/BurningHillsLayout.json"));
                foreach(var renderer in life.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    Assert.That(PrefabUtility.IsPartOfPrefabInstance(renderer),Is.True,renderer.name);Assert.That(renderer.sprite,Is.Not.Null);
                    Assert.That(ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader),Is.False);
                    if(renderer.sprite.name=="street_cafe")foreach(var speaker in life.root.Presentation.characters)
                        Assert.That(Projected(renderer,life.root.CameraRig.RenderCamera).Overlaps(Projected(speaker.spriteRenderer,life.root.CameraRig.RenderCamera)),Is.False,"Cafe furniture obscures "+speaker.speakerId);
                }
                foreach(var actor in life.actors)
                {
                    Assert.That(actor.frames.Length,Is.EqualTo(2));Assert.That(actor.frames.All(f=>f!=null),Is.True);Assert.That(actor.path.Length,Is.GreaterThan(1));
                    if(actor.airborne)continue;
                    for(int segment=1;segment<actor.path.Length;segment++)for(int sample=0;sample<=20;sample++)
                    {
                        var point=DistrictMap.ToPlan(Vector3.Lerp(actor.path[segment-1],actor.path[segment],sample/20f));
                        Assert.That(plan.sites.Any(s=>Mathf.Abs(s.u-point.x)<14&&Mathf.Abs(s.v-point.y)<14),Is.False,actor.anchor.name+" enters a parcel");
                    }
                }
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static Rect Projected(SpriteRenderer renderer,Camera camera)
        {
            var bounds=renderer.sprite.bounds;var min=Vector2.one*float.PositiveInfinity;var max=Vector2.one*float.NegativeInfinity;
            foreach(float x in new[]{bounds.min.x,bounds.max.x})foreach(float y in new[]{bounds.min.y,bounds.max.y})
            {
                var world=renderer.transform.TransformPoint(new Vector3(x,y,0));var point=new Vector2(Vector3.Dot(world,camera.transform.right),Vector3.Dot(world,camera.transform.up));
                min=Vector2.Min(min,point);max=Vector2.Max(max,point);
            }
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        [Test] public void ApplyingLivingHillsAgainPreservesTheEntireScene()
        {
            byte[] before;using(var sha=SHA256.Create())before=sha.ComputeHash(File.ReadAllBytes(GameScaffold.GameScene));
            LivingHillsAuthoring.Apply();
            using(var sha=SHA256.Create())Assert.That(sha.ComputeHash(File.ReadAllBytes(GameScaffold.GameScene)),Is.EqualTo(before));
        }
    }
}

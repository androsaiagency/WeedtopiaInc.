using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Buildings.Presentation;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class BigRockCityAssetTests
    {
        [Test] public void BothScenesHaveOnlyLocalSitesRoutesAndCompleteAuthoredCampaignBindings()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var entry in new[]{(GameScaffold.GameScene,"burning_hills"),(BigRockCityAuthoring.ScenePath,"big_rock_city")})
                {
                    var scene=EditorSceneManager.OpenScene(entry.Item1);var root=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameRoot>(true)).Single();
                    var definitions=AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
                    Assert.That(root.SceneCityId,Is.EqualTo(entry.Item2));
                    CollectionAssert.AreEquivalent(definitions.instances.Where(i=>i.cityId==entry.Item2).Select(i=>i.id),root.Sites.Select(s=>s.InstanceId));
                    Assert.That(root.Presentation.routes.Length,Is.EqualTo(35));
                    Assert.That(root.Presentation.routes.All(r=>definitions.instances.Single(i=>i.id==definitions.routes.Single(d=>d.id==r.routeId).from).cityId==entry.Item2),Is.True);
                    var ui=root.Ui.Campaign;Assert.That(ui,Is.Not.Null);
                    Assert.That(new UnityEngine.Object[]{ui.accept,ui.burningTravel,ui.rockTravel,ui.headerEntry,ui.journalEntry,ui.invitation,ui.offerBadge}.All(o=>o!=null),Is.True);
                    CollectionAssert.AreEquivalent(definitions.cities.Select(city=>city.id),ui.travelRows.Select(row=>row.cityId));
                    Assert.That(ui.travelRows.All(row=>row.progress!=null&&row.travel!=null),Is.True);
                    Assert.That(root.Presentation.characters.Single(c=>c.speakerId=="rocko").GetComponentInChildren<CityMandateActor>(true).root,Is.SameAs(root));
                    if(entry.Item2!="big_rock_city")continue;
                    Assert.That(root.StoryWorld.actors.Length,Is.EqualTo(6));Assert.That(root.Ui.Stories.rows.Length,Is.EqualTo(9));
                    Assert.That(root.StoryWorld.actors.Select(a=>a.spriteRenderer.sprite.texture).Distinct().Count(),Is.EqualTo(6));
                    Assert.That(root.StoryWorld.actors.All(a=>a.memories.Select(m=>m.chapter).Distinct().Count()==4),Is.True);
                    var stamp=UnityEngine.Object.FindFirstObjectByType<BigRockCityStamp>();Assert.That(stamp.landmarks,Is.EqualTo(5));
                    var signs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="BigRockWelcomeSign").ToArray();
                    Assert.That(signs.Length,Is.EqualTo(2));
                    foreach(var sign in signs)Assert.That(AssetDatabase.GetAssetPath(sign.GetComponentInChildren<SpriteRenderer>().sprite),Is.EqualTo(BigRockCityAuthoring.Folder+"/Art/town_sign.png"));
                }
            }
            finally{Restore(setup);}
        }
        [Test] public void All251CityVariantsPreserveTierCoverageCollidersOffsetsAndGameplayAnchors()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);var root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();
                var source=(BuildingVisualCatalog)new SerializedObject(root).FindProperty("productionCatalog").objectReferenceValue;
                var target=AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(BigRockCityAuthoring.CatalogPath);
                Assert.That(target.Families.Sum(f=>f.Bands.Count),Is.EqualTo(251));
                foreach(var family in source.Families)
                    foreach(var band in family.Bands)
                    {
                        Assert.That(target.TryGetVisual(family.BuildingId,band.MinLevel,out var prefab),Is.True);
                        Assert.That(target.TryGetVisual(family.BuildingId,band.MaxLevel,out var end),Is.True);Assert.That(end,Is.SameAs(prefab));
                        var old=band.WrapperPrefab.GetComponent<BuildingVisualWrapper>();var current=prefab.GetComponent<BuildingVisualWrapper>();
                        Assert.That(current.BuildingId,Is.EqualTo(old.BuildingId));Assert.That(current.TierId,Is.EqualTo(old.TierId));
                        var a=new[]{old.InteractionAnchor,old.WorkerAnchor,old.DeliveryInputAnchor,old.DeliveryOutputAnchor,old.VfxAnchor,old.ColliderRoot};
                        var b=new[]{current.InteractionAnchor,current.WorkerAnchor,current.DeliveryInputAnchor,current.DeliveryOutputAnchor,current.VfxAnchor,current.ColliderRoot};
                        for(int i=0;i<a.Length;i++){Assert.That(b[i],Is.Not.Null);Assert.That(b[i].localPosition,Is.EqualTo(a[i].localPosition));Assert.That(b[i].localRotation,Is.EqualTo(a[i].localRotation));}
                        CollectionAssert.AreEqual(band.WrapperPrefab.GetComponentsInChildren<BoxCollider>(true).Select(c=>c.center+"/"+c.size+"/"+c.enabled),prefab.GetComponentsInChildren<BoxCollider>(true).Select(c=>c.center+"/"+c.size+"/"+c.enabled));
                        var oldSprite=band.WrapperPrefab.GetComponentInChildren<BuildingSpriteVisual>(true);var newSprite=prefab.GetComponentInChildren<BuildingSpriteVisual>(true);
                        Assert.That(newSprite.ResourcePath,Is.EqualTo(oldSprite.ResourcePath));Assert.That(newSprite.transform.localPosition,Is.EqualTo(oldSprite.transform.localPosition));
                        Assert.That(newSprite.transform.localScale,Is.EqualTo(oldSprite.transform.localScale));Assert.That(newSprite.FootprintInset,Is.EqualTo(oldSprite.FootprintInset));
                        Assert.That(newSprite.SpriteRenderer.sharedMaterial.shader.name,Is.EqualTo("Weedtopia/Big Rock/Building Palette"));
                        Assert.That(newSprite.SpriteRenderer.sharedMaterial,Is.Not.SameAs(oldSprite.SpriteRenderer.sharedMaterial));
                    }
            }
            finally{Restore(setup);}
        }
        private static void Restore(SceneSetup[] setup)
        {
            if(setup.Any(s=>s.isLoaded)&&setup.Count(s=>s.isActive)==1)EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
        [Test] public void ExplicitInstallerPreservesExistingSceneAndAssetBytesOnARepeatRun()
        {
            var paths=Directory.GetFiles(BigRockCityAuthoring.Folder,"*",SearchOption.AllDirectories).Concat(new[]{GameScaffold.GameScene,GameScaffold.GameScene+".meta",BigRockCityAuthoring.ScenePath,BigRockCityAuthoring.ScenePath+".meta","Assets/Weedtopia/Game/Content/BurningHills.json","ProjectSettings/EditorBuildSettings.asset"}).ToArray();
            var before=paths.ToDictionary(p=>p,File.ReadAllBytes);BigRockCityAuthoring.Apply();
            foreach(var pair in before)Assert.That(pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)),Is.True,pair.Key);
            File.WriteAllText("Docs/BigRockCity/idempotence_receipt.json","{\"status\":\"passed\",\"preservedFiles\":"+paths.Length+"}");
        }
    }
}

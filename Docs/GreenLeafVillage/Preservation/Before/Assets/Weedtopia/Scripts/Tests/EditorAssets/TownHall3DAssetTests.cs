using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Weedtopia.Buildings.Presentation;
using Weedtopia.Editor.GameSetup;

namespace Weedtopia.Tests.Assets
{
    public sealed class TownHall3DAssetTests
    {
        [Test] public void ArchivedTownHallRetainsThreeBoundedLodsAndOnly2KTextures()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(TownHall3DIntegration.ModelPath);
            Assert.That(prefab,Is.Not.Null,"Archived 3D experiment must remain recoverable");
            var lod=prefab.GetComponent<LODGroup>();var levels=lod.GetLODs();
            Assert.That(levels.Length,Is.EqualTo(3));int[] expected={50000,20000,12000};
            for(int i=0;i<levels.Length;i++)
            {
                Assert.That(levels[i].renderers.Sum(r=>(int)r.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0)/3),Is.EqualTo(expected[i]));
                foreach(var r in levels[i].renderers)foreach(var m in r.sharedMaterials)
                    foreach(var property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"})
                    {
                        var texture=m.GetTexture(property);Assert.That(texture,Is.Not.Null);
                        Assert.That(texture.width,Is.EqualTo(2048));Assert.That(texture.height,Is.EqualTo(2048));
                    }
            }
            Assert.That(lod.GetComponentsInChildren<MeshCollider>(),Is.Empty);
            Assert.That(lod.GetComponentsInChildren<BoxCollider>().Length,Is.EqualTo(2));
            var dependencies=AssetDatabase.GetDependencies(TownHall3DIntegration.ModelPath,true);
            Assert.That(dependencies.Any(p=>p.Contains("/Incoming/")||p.Contains("_4K.")),Is.False,"Retired meshes and 4K review maps must not be production dependencies");
        }
        [Test] public void RestoredOriginalSpritePreservesIdentityAndHasNoMeshDependency()
        {
            string[] paths={TownHall3DIntegration.WrapperPath,TownHall3DIntegration.WrapperPath+".meta",TownHall3DIntegration.ModelPath,TownHall3DIntegration.ModelPath+".meta"};
            var before=paths.ToDictionary(p=>p,File.ReadAllBytes);
            var wrapper=AssetDatabase.LoadAssetAtPath<GameObject>(TownHall3DIntegration.WrapperPath).GetComponent<BuildingVisualWrapper>();
            string anchors=ProductionBuildingIntegration.AnchorState(wrapper);
            TownHall2DRestore.Apply();
            foreach(var pair in before)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(pair.Key),pair.Key);
            wrapper=AssetDatabase.LoadAssetAtPath<GameObject>(TownHall3DIntegration.WrapperPath).GetComponent<BuildingVisualWrapper>();
            Assert.That(ProductionBuildingIntegration.AnchorState(wrapper),Is.EqualTo(anchors));
            Assert.That(wrapper.BuildingId,Is.EqualTo("town_hall"));Assert.That(wrapper.TierId,Is.EqualTo("T_FIXED"));
            Assert.That(AssetDatabase.AssetPathToGUID(TownHall3DIntegration.WrapperPath),Is.EqualTo("fb592e607e1cdf74b93684f1cc98441b"));
            Assert.That(wrapper.SourceModel,Is.Null);Assert.That(wrapper.ManagedModel,Is.Null);
            var sprites=wrapper.GetComponentsInChildren<BuildingSpriteVisual>();
            Assert.That(sprites.Length,Is.EqualTo(1));Assert.That(sprites[0].ResourcePath,Is.EqualTo(TownHall2DRestore.OriginalResource));
            Assert.That(wrapper.GetComponentsInChildren<LODGroup>(true),Is.Empty);
            Assert.That(wrapper.GetComponentsInChildren<MeshRenderer>(true),Is.Empty);
            Assert.That(AssetDatabase.GetDependencies(TownHall3DIntegration.WrapperPath,true).Any(p=>p.Contains("/Archive/")||p.EndsWith(".fbx")),Is.False);
        }
    }
}

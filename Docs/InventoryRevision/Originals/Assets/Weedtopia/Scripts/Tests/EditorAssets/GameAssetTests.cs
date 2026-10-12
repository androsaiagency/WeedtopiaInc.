using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Buildings.Presentation;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Editor.BuildingArtPipeline;

namespace Weedtopia.Tests.Assets
{
    public sealed class GameAssetTests
    {
        [Serializable] private sealed class SpriteMetric {public string path;public int width,height,colliders;public long textureBytes;public float worldHeight;}
        [Serializable] private sealed class SpriteReceipt {public string utc;public bool productionApproval=false;public SpriteMetric[] prototypes;}
        [Test] public void SceneReferencesAndBuildConfigurationValidate(){GameScaffold.ValidateBatch();}
        [Test] public void ProjectAssetsUseCanonicalRoots()
        {
            Assert.That(AssetDatabase.IsValidFolder("Assets/Audio"),Is.False,"Audio belongs below the game-owned root");
            Assert.That(AssetDatabase.IsValidFolder("Assets/Scenes"),Is.False,"Production scenes belong in Game/Scenes");
            Assert.That(AssetDatabase.IsValidFolder("Assets/TutorialInfo"),Is.False,"Unity template material belongs in the archive");
            Assert.That(AssetDatabase.IsValidFolder("Assets/Weedtopia/Game/Audio/Approved"),Is.True);
            Assert.That(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Weedtopia/Game/Audio/Approved/BurningHills/afterglow_loop_60s.wav"),Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Weedtopia/Game/Archive/UnityTemplate/SampleScene.unity"),Is.Not.Null);
        }
        [Test] public void EveryLevelMapsToExistingCatalogWithNoGaps()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(BuildingManifestReader.CatalogPath);int bands=0;
            foreach(var family in catalog.Families)
            {
                bands+=family.Bands.Count;
                for(int level=0;level<=5000;level++)
                {Assert.That(catalog.TryGetVisual(family.BuildingId,level,out var prefab),Is.True);Assert.That(prefab.GetComponent<BuildingVisualWrapper>().TierId,Is.EqualTo(VisualTiers.Id(level,family.BuildingId=="town_hall")));}
            }
            Assert.That(bands,Is.EqualTo(251));
        }
        [Test] public void SecondGameScaffoldPreservesEveryAuthoredAssetByte()
        {
            var before=Directory.GetFiles(GameScaffold.Root,"*",SearchOption.AllDirectories).ToDictionary(p=>p,File.ReadAllBytes);
            var production=Directory.GetFiles("Assets/Weedtopia/Art/Buildings/Prefabs","*",SearchOption.AllDirectories).ToDictionary(p=>p,File.ReadAllBytes);
            GameScaffold.CreateOrRepair();
            foreach(var pair in before)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(pair.Key),pair.Key);
            foreach(var pair in production)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(pair.Key),pair.Key);
            File.WriteAllText("Docs/Implementation/Validation/game_idempotence.json","{\"utc\":\""+DateTime.UtcNow.ToString("O")+"\",\"passed\":true,\"gameFiles\":"+before.Count+",\"productionFiles\":"+production.Count+"}");
        }
        [Test] public void IsolatedSpritePrototypesHaveAlphaGroundPivotAnchorsAndBoundedTextures()
        {
            var paths=Directory.GetFiles(SpriteReviewScene.Folder+"/Prefabs","*.prefab");Assert.That(paths.Length,Is.EqualTo(4));
            var metrics=new System.Collections.Generic.List<SpriteMetric>();
            foreach(string path in paths)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var wrapper=prefab.GetComponent<BuildingVisualWrapper>();
                foreach(var anchor in new[]{wrapper.VisualRoot,wrapper.InteractionAnchor,wrapper.WorkerAnchor,wrapper.DeliveryInputAnchor,wrapper.DeliveryOutputAnchor,wrapper.VfxAnchor,wrapper.ColliderRoot})Assert.That(anchor,Is.Not.Null,path);
                var visual=prefab.GetComponentInChildren<BuildingSpriteVisual>();var sprite=visual.SpriteRenderer.sprite;var texture=sprite.texture;
                Assert.That(sprite.pivot.y,Is.EqualTo(0).Within(.01));Assert.That(sprite.pivot.x,Is.EqualTo(sprite.rect.width*.5f).Within(.01));
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));Assert.That(importer.textureType,Is.EqualTo(TextureImporterType.Sprite));Assert.That(importer.DoesSourceTextureHaveAlpha(),Is.True);Assert.That(importer.mipmapEnabled,Is.True);Assert.That(importer.isReadable,Is.False);
                Assert.That(Math.Max(texture.width,texture.height),Is.LessThanOrEqualTo(2048));Assert.That(prefab.GetComponentsInChildren<Collider>().Length,Is.EqualTo(1));
                Assert.That(prefab.GetComponentInChildren<Weedtopia.Unity.ReviewGroundSorting>(),Is.Not.Null);
                metrics.Add(new SpriteMetric{path=path,width=texture.width,height=texture.height,textureBytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture),colliders=1,worldHeight=visual.AuthoredWorldHeight});
            }
            File.WriteAllText("Docs/Implementation/Validation/sprite_prototype_validation.json",JsonUtility.ToJson(new SpriteReceipt{utc=DateTime.UtcNow.ToString("O"),prototypes=metrics.ToArray()},true));
        }
    }
}

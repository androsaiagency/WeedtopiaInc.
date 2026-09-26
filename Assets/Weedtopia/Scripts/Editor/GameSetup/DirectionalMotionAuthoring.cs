using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Unity;
using Weedtopia.Editor.BuildingArtPipeline;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit migration of moving artwork only; scene layout and logistics remain authored in place.</summary>
    public static class DirectionalMotionAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/DirectionalMotion";
        [Serializable] private sealed class Entry
        {
            public string key, file, name;
            public int row, frame, x, y, width, height;
            public float pivotX, pivotY, scale;
        }
        [Serializable] private sealed class Manifest { public Entry[] sprites; }
        private static readonly string[] Keys = { "resident_a", "resident_b", "resident_c", "cyclist", "van", "swallow", "butterfly" };
        private static readonly int[] Rows = { 2, 3, 4, 3, 2, 1, 0, 1 };

        [MenuItem("Weedtopia/Production/Install Directional Motion")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                BuildingIntakeService.EnsureAssetFolder(Folder + "/Animations");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder + "/art_manifest.json"));
                var sprites = new Dictionary<string, Sprite>();
                foreach (var group in manifest.sprites.GroupBy(e => e.file))
                {
                    string path = Folder + "/Art/" + group.Key;
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
                    importer.spritePixelsPerUnit = 100; importer.alphaIsTransparency = true; importer.mipmapEnabled = true;
                    importer.filterMode = FilterMode.Trilinear; importer.wrapMode = TextureWrapMode.Clamp;
                    importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 4096;
#pragma warning disable 618
                    importer.spritesheet = group.Select(e => new SpriteMetaData { name=e.name, rect=new Rect(e.x,e.y,e.width,e.height), alignment=9, pivot=new Vector2(e.pivotX,e.pivotY) }).ToArray();
#pragma warning restore 618
                    importer.SaveAndReimport();
                    foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()) sprites.Add(sprite.name,sprite);
                }
                var sets = new Dictionary<string, DirectionalSpriteSet>();
                foreach (string key in Keys)
                {
                    string path = Folder + "/Animations/" + key + ".asset";
                    var set = AssetDatabase.LoadAssetAtPath<DirectionalSpriteSet>(path);
                    if (set == null) { set=ScriptableObject.CreateInstance<DirectionalSpriteSet>();AssetDatabase.CreateAsset(set,path); }
                    set.name=key;
                    set.aerial = key == "swallow" || key == "butterfly";
                    set.distancePerCycle = key == "van" ? 3.6f : key == "cyclist" ? 5.5f : key == "swallow" ? 2.6f : key == "butterfly" ? .4f : 3.1f;
                    set.restingFrame = key == "van" ? 0 : 2;
                    set.views = new DirectionalSpriteSet.View[8];
                    for (int direction=0;direction<8;direction++)
                    {
                        int row=set.aerial?0:Rows[direction];
                        var frames=manifest.sprites.Where(e=>e.key==key&&e.row==row).OrderBy(e=>e.frame).ToArray();
                        if(frames.Length!=6)throw new InvalidOperationException("Incomplete animation: "+key+"/"+direction);
                        set.views[direction]=new DirectionalSpriteSet.View { frames=frames.Select(e=>sprites[e.name]).ToArray(),scale=frames[0].scale,mirror=!set.aerial&&direction>=3&&direction<=5 };
                    }
                    EditorUtility.SetDirty(set);sets.Add(key,set);
                }
                foreach(string key in Keys.Where(k=>k!="van"))
                    ConfigurePrefab(LivingHillsAuthoring.Folder+"/Prefabs/Living_"+key+"_0.prefab",sets[key],false);
                ConfigurePrefab(ProductionArtImporter.Root+"/Prefabs/DeliveryTruck.prefab",sets["van"],true);
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var life=Object.FindFirstObjectByType<LivingTownPresentation>();
                foreach(var actor in life.actors)
                {
                    actor.animation=actor.anchor.GetComponent<DirectionalSpriteAnimator>();
                    if(actor.animation==null)throw new InvalidOperationException("Missing authored actor animation: "+actor.anchor.name);
                    actor.bob=0;
                }
                life.revision=2;EditorUtility.SetDirty(life);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Docs/DirectionalMotion");
                File.WriteAllText("Docs/DirectionalMotion/integration.json","{\"utc\":\""+DateTime.UtcNow.ToString("O")+"\",\"actorCount\":"+life.actors.Length+",\"vehicleCount\":"+Object.FindObjectsByType<DeliveryView>(FindObjectsSortMode.None).Length+",\"animationSets\":7,\"directions\":8,\"framesPerCycle\":6}");
            }
            finally { if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static void ConfigurePrefab(string path,DirectionalSpriteSet set,bool vehicle)
        {
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var animator=prefab.GetComponent<DirectionalSpriteAnimator>()??prefab.AddComponent<DirectionalSpriteAnimator>();
                animator.animationSet=set;
                animator.artwork=prefab.GetComponentsInChildren<SpriteRenderer>(true).Single(s=>!vehicle||s.name=="TruckArtwork");
                animator.artwork.sprite=set.views[7].frames[2];animator.artwork.flipX=false;
                animator.artwork.transform.localScale=Vector3.one*set.views[7].scale;
                if(vehicle)
                {
                    var contact=prefab.transform.Find("Body/SoftGroundContact");
                    if(contact==null)
                    {
                        contact=new GameObject("SoftGroundContact",typeof(MeshFilter),typeof(MeshRenderer)).transform;
                        contact.SetParent(prefab.transform.Find("Body"),false);
                    }
                    contact.localScale=new Vector3(2.1f,1,4.2f);animator.groundContact=contact;
                    contact.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(NaturalEnvironmentAuthoring.Folder+"/Meshes/CanopyContact.asset");
                    var shadow=contact.GetComponent<MeshRenderer>();
                    shadow.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(NaturalEnvironmentAuthoring.Folder+"/Materials/CanopyShadow.mat");
                    shadow.sortingOrder=-32010;shadow.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    var fields=new SerializedObject(prefab.GetComponent<DeliveryView>());
                    fields.FindProperty("directionalAnimation").objectReferenceValue=animator;
                    fields.FindProperty("pickupVanScale").floatValue=1;
                    fields.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
    }
}

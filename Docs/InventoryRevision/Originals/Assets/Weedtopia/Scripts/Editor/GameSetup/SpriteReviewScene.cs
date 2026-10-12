using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Weedtopia.Buildings.Presentation;
using Weedtopia.Editor.BuildingArtPipeline;
using Weedtopia.Unity;
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    public static class SpriteReviewScene
    {
        public const string Folder="Assets/Weedtopia/Game/VisualReview",ScenePath=Folder+"/Building2DReview.unity";
        [MenuItem("Weedtopia/Art Pipeline/2D Buildings/Create Isolated Review Scene")]
        public static void Create()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before explicit review setup");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene changes before explicit review setup");
            BuildingIntakeService.EnsureAssetFolder(Folder);BuildingIntakeService.EnsureAssetFolder(Folder+"/Prefabs");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var town=CreateSpritePrefab("town_hall","T_FIXED",BuildingSpritePrototypeUtility.SourcePath,22);
                var early=CreateSpritePrefab("coffee_shop","T01_L001",Folder+"/Sprites/coffee_shop_T01_L001.png",8);
                var middle=CreateSpritePrefab("coffee_shop","T12_L600",Folder+"/Sprites/coffee_shop_T12_L600.png",14);
                var final=CreateSpritePrefab("coffee_shop","T24_L5000",Folder+"/Sprites/coffee_shop_T24_L5000.png",22);
                if(File.Exists(ScenePath))return;
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var obj=new GameObject("ReviewCamera",typeof(Camera));obj.tag="MainCamera";var camera=obj.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=26;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.065f,.12f,.16f);camera.farClipPlane=300;
                obj.transform.rotation=Quaternion.Euler(35.264f,45,0);obj.transform.position=new Vector3(0,7,0)-obj.transform.forward*90;
                var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Neutral3DGround";ground.transform.localScale=Vector3.one*12;
                var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(.09f,.18f,.19f));AssetDatabase.CreateAsset(material,Folder+"/ReviewGround.mat");ground.GetComponent<Renderer>().sharedMaterial=material;
                GameObject[] prefabs={town,early,middle,final};string[] labels={"Rathaus · T_FIXED","Coffee-Shop · T01","Coffee-Shop · T12","Coffee-Shop · T24"};
                for(int i=0;i<4;i++)
                {
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);instance.transform.position=obj.transform.right*((i-1.5f)*22);
                    var text=new GameObject(labels[i],typeof(TextMesh));text.transform.position=instance.transform.position+Vector3.up*.3f-obj.transform.forward*1.5f;text.transform.rotation=obj.transform.rotation;
                    var mesh=text.GetComponent<TextMesh>();mesh.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");mesh.fontSize=48;mesh.characterSize=.25f;mesh.anchor=TextAnchor.UpperCenter;mesh.alignment=TextAlignment.Center;mesh.text=labels[i];mesh.GetComponent<MeshRenderer>().sharedMaterial=mesh.font.material;
                }
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static GameObject CreateSpritePrefab(string family,string tier,string path,float height)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new FileNotFoundException(path);
            if(family!="town_hall")
            {
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;importer.SetTextureSettings(settings);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=64;importer.spritePivot=new Vector2(.5f,0);importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            string target=Folder+"/Prefabs/Review_"+family+"_"+tier+".prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(target);if(existing!=null)return existing;
            var root=new GameObject("Review_"+family+"_"+tier);
            try
            {
                Transform Child(string name){var obj=new GameObject(name);obj.transform.SetParent(root.transform,false);return obj.transform;}
                var visual=Child("VisualRoot");var interaction=Child("InteractionAnchor");var worker=Child("WorkerAnchor");var input=Child("DeliveryInputAnchor");var output=Child("DeliveryOutputAnchor");var vfx=Child("VfxAnchor");var colliders=Child("ColliderRoot");
                var spriteObject=new GameObject("SpriteVisual",typeof(SpriteRenderer),typeof(BuildingSpriteVisual),typeof(ReviewGroundSorting));spriteObject.transform.SetParent(visual,false);
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite==null)throw new InvalidOperationException("Missing imported sprite "+path);
                var renderer=spriteObject.GetComponent<SpriteRenderer>();renderer.sprite=sprite;spriteObject.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);spriteObject.GetComponent<BuildingSpriteVisual>().EditorConfigure(renderer,true,height);
                var box=colliders.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,.5f,0);box.size=new Vector3(height*.7f,1,height*.55f);
                root.AddComponent<BuildingVisualWrapper>().EditorSetStructure(family,tier,visual,interaction,worker,input,output,vfx,colliders);
                return PrefabUtility.SaveAsPrefabAsset(root,target);
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}

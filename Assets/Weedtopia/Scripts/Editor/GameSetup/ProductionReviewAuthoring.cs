using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Unity;
using Weedtopia.Editor.BuildingArtPipeline;

namespace Weedtopia.Editor.GameSetup
{
    public static class ProductionReviewAuthoring
    {
        public const string ScenePath="Assets/Weedtopia/Production/Review/BuildingApproval.unity";
        public static void Create()
        {
            if(File.Exists(ScenePath))return;
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save authored scene first");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                BuildingIntakeService.EnsureAssetFolder("Assets/Weedtopia/Production/Review");
                var scene=EditorSceneManager.OpenScene(SpriteReviewScene.ScenePath);var art=AssetDatabase.LoadAssetAtPath<PresentationArt>(ProductionArtImporter.ArtPath);
                var ground=GameObject.Find("Neutral3DGround");ground.transform.localScale=Vector3.one;ground.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(ProductionArtImporter.Root+"/Meshes/ContinuousMeadow.asset");ground.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(ProductionArtImporter.Root+"/Materials/Meadow.mat");
                foreach(var text in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)){text.font=art.headingFont;text.GetComponent<MeshRenderer>().sharedMaterial=art.headingFont.material;text.color=new Color(.99f,.96f,.85f);}
                var camera=Camera.main;camera.backgroundColor=new Color(.18f,.26f,.29f);
                for(int i=0;i<4;i++)
                {
                    var position=camera.transform.right*((i-1.5f)*22);
                    foreach(var pair in new[]{("Prop_lantern",new Vector3(-6,0,3)),("Prop_flowers",new Vector3(6,0,3)),("Character_rocko",new Vector3(-7,0,-5))})
                    {var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ProductionArtImporter.Root+"/Prefabs/"+pair.Item1+".prefab");var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab);obj.transform.position=position+pair.Item2;foreach(var sr in obj.GetComponentsInChildren<SpriteRenderer>())sr.sortingOrder=1500;}
                }
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}

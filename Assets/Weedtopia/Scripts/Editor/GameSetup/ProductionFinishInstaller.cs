using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    public static class ProductionFinishInstaller
    {
        [MenuItem("Weedtopia/Production/Apply Authored Presentation")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before authoring");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene edits before authoring");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);
                // A district migration archives the previous environment, including its installation stamp.
                // Check inactive roots before any importer or prefab authoring can run.
                if(UnityEngine.Object.FindFirstObjectByType<ProductionAuthoringStamp>(FindObjectsInactive.Include)!=null)return;
                var art=ProductionArtImporter.Import();
                var root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();var presentation=root.gameObject.AddComponent<ProductionPresentation>();presentation.art=art;
                var serialized=new SerializedObject(root);serialized.FindProperty("presentation").objectReferenceValue=presentation;serialized.ApplyModifiedPropertiesWithoutUndo();
                ProductionWorldAuthoring.Author(root,presentation,art);ProductionUiAuthoring.Author(root.Ui,presentation,art);
                var music=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Approved/BurningHills/afterglow_loop_60s.wav");
                if(music!=null){var obj=new GameObject("AfterglowMusic",typeof(AudioSource));obj.transform.SetParent(root.transform,false);var audio=obj.GetComponent<AudioSource>();audio.clip=music;audio.loop=true;audio.playOnAwake=true;audio.volume=.2f;presentation.ambience=audio;}
                // Preserve SampleScene on disk, remove only the confirmed stock sample from build configuration.
                EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(x=>x.path!="Assets/Scenes/SampleScene.unity").ToArray();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Docs/ProductionFinish/Evidence");File.WriteAllText("Docs/ProductionFinish/Evidence/install.json","{\"utc\":\""+DateTime.UtcNow.ToString("O")+"\",\"implemented\":true,\"userBuildingApproval\":false,\"productionWrappersConverted\":0,\"releaseReady\":false}");
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}

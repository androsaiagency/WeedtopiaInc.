using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    public static class ProductionAudioAuthoring
    {
        [MenuItem("Weedtopia/Production/Bind Original Audio")]
        public static void Bind()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save authored scene first");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);const string folder="Assets/Weedtopia/Game/Audio/Approved/BurningHills/";
                foreach(string name in new[]{"confirm","unavailable","upgrade","delivery","afterglow_loop_60s"})
                {
                    var importer=(AudioImporter)AssetImporter.GetAtPath(folder+name+".wav");var settings=importer.defaultSampleSettings;settings.loadType=name.StartsWith("afterglow")?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.85f;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
                }
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);var root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();var feedback=new SerializedObject(root.GetComponent<GameFeedback>());
                feedback.FindProperty("success").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"confirm.wav");feedback.FindProperty("error").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"unavailable.wav");feedback.ApplyModifiedPropertiesWithoutUndo();
                var p=root.Presentation;var child=root.transform.Find("PresentationEvents");if(child==null){var obj=new GameObject("PresentationEvents",typeof(AudioSource));obj.transform.SetParent(root.transform,false);child=obj.transform;}
                p.eventAudio=child.GetComponent<AudioSource>();p.eventAudio.playOnAwake=false;p.eventAudio.volume=.4f;p.upgradeSound=AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"upgrade.wav");p.deliverySound=AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"delivery.wav");
                if(p.ambience==null){var obj=new GameObject("AfterglowMusic",typeof(AudioSource));obj.transform.SetParent(root.transform,false);p.ambience=obj.GetComponent<AudioSource>();}
                p.ambience.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"afterglow_loop_60s.wav");p.ambience.loop=true;p.ambience.playOnAwake=true;p.ambience.volume=.2f;
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}

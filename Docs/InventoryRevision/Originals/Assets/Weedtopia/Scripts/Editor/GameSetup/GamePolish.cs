using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    public static class GamePolish
    {
        public static void ApplyResponsiveLayout(GameUi ui)
        {
            var serialized=new SerializedObject(ui);var bindings=serialized.FindProperty("bindings");
            GameObject Find(string id)
            {
                for(int i=0;i<bindings.arraySize;i++){var entry=bindings.GetArrayElementAtIndex(i);if(entry.FindPropertyRelative("id").stringValue==id)return (GameObject)entry.FindPropertyRelative("target").objectReferenceValue;}
                throw new InvalidOperationException("Missing binding "+id);
            }
            ui.GetComponent<CanvasScaler>().matchWidthOrHeight=1;
            var detail=Find("detailBody").GetComponent<RectTransform>();detail.anchoredPosition=new Vector2(0,-160);detail.sizeDelta=new Vector2(324,212);
            var panel=detail.parent.GetComponent<RectTransform>();panel.anchoredPosition=new Vector2(-24,-118);panel.sizeDelta=new Vector2(360,675);
            var flow=Find("flow").GetComponent<RectTransform>();flow.anchoredPosition=new Vector2(0,-288);flow.sizeDelta=new Vector2(324,55);
            string[] actions={"ui.upgrade","ui.finish","ui.harvest","ui.work","ui.workers","ui.talk","ui.house","ui.park"};
            for(int i=0;i<actions.Length;i++){var rect=Find(actions[i]).GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(0,-343-i*42);rect.sizeDelta=new Vector2(318,38);}
            var recovery=Find("recovery").GetComponent<RectTransform>();recovery.anchorMin=recovery.anchorMax=Vector2.zero;recovery.pivot=Vector2.zero;recovery.anchoredPosition=new Vector2(24,122);
            var message=Find("message").GetComponent<RectTransform>();message.anchorMin=message.anchorMax=new Vector2(.5f,0);message.anchoredPosition=new Vector2(0,150);message.sizeDelta=new Vector2(800,46);
            var nav=Find("zone.Farm").transform.parent.GetComponent<RectTransform>();nav.anchorMin=Vector2.zero;nav.anchorMax=new Vector2(1,0);nav.pivot=new Vector2(.5f,0);nav.anchoredPosition=Vector2.zero;nav.sizeDelta=new Vector2(0,104);
            var layout=nav.GetComponent<HorizontalLayoutGroup>();if(layout==null)layout=nav.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding=new RectOffset(24,24,20,20);layout.spacing=16;layout.childControlWidth=true;layout.childForceExpandWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=true;
            foreach(Transform child in nav){var element=child.GetComponent<LayoutElement>();if(element==null)element=child.gameObject.AddComponent<LayoutElement>();element.minWidth=0;element.preferredWidth=1;element.flexibleWidth=1;}
            var top=Find("title").transform.parent.GetComponent<RectTransform>();top.anchorMin=new Vector2(0,1);top.anchorMax=Vector2.one;top.pivot=new Vector2(.5f,1);top.anchoredPosition=Vector2.zero;top.sizeDelta=new Vector2(0,106);
            var title=Find("title").GetComponent<RectTransform>();title.anchorMin=title.anchorMax=new Vector2(0,1);title.pivot=new Vector2(0,.5f);title.anchoredPosition=new Vector2(24,-28);
            var hud=Find("hud").GetComponent<RectTransform>();hud.anchorMin=new Vector2(0,1);hud.anchorMax=Vector2.one;hud.offsetMin=new Vector2(450,-55);hud.offsetMax=new Vector2(-24,-9);
            var development=Find("development").GetComponent<RectTransform>();development.anchorMin=new Vector2(0,1);development.anchorMax=Vector2.one;development.offsetMin=new Vector2(24,-91);development.offsetMax=new Vector2(-24,-61);
            foreach(var image in ui.GetComponentsInChildren<Image>(true))if(image.gameObject.name=="TopBar"||image.gameObject.name=="Navigation"||image.gameObject.name=="QuestCard"||image.gameObject.name=="BuildingDetails"||new[]{"workers","townhall","start","intro","settings","premium","offline","journal"}.Contains(image.gameObject.name)){var color=image.color;color.a=1;image.color=color;}
        }
        [MenuItem("Weedtopia/Setup/Apply Reviewed Readability Fixes")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before explicit polish");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene changes before explicit polish");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);var ui=UnityEngine.Object.FindFirstObjectByType<GameUi>(FindObjectsInactive.Include);
                if(UnityEngine.Object.FindFirstObjectByType<ProductionAuthoringStamp>()!=null){Debug.Log("Authored production layout preserved; use the Production correction menu.");return;}
                var so=new SerializedObject(ui);var bindings=so.FindProperty("bindings");
                GameObject Find(string id)
                {
                    for(int i=0;i<bindings.arraySize;i++){var item=bindings.GetArrayElementAtIndex(i);if(item.FindPropertyRelative("id").stringValue==id)return (GameObject)item.FindPropertyRelative("target").objectReferenceValue;}
                    throw new InvalidOperationException("Missing binding "+id);
                }
                var detail=Find("detailBody").GetComponent<RectTransform>();detail.anchoredPosition=new Vector2(0,-166);detail.sizeDelta=new Vector2(324,212);
                var flow=Find("flow").GetComponent<RectTransform>();flow.anchoredPosition=new Vector2(0,-316);flow.sizeDelta=new Vector2(324,95);Find("flow").GetComponent<Text>().fontSize=20;
                string[] actions={"ui.upgrade","ui.finish","ui.harvest","ui.work","ui.workers","ui.talk","ui.house","ui.park"};for(int i=0;i<actions.Length;i++)Find(actions[i]).GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-389-i*48);
                detail.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(360,790);
                ApplyResponsiveLayout(ui);
                var root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();var feedback=root.GetComponent<GameFeedback>();if(feedback==null)feedback=root.gameObject.AddComponent<GameFeedback>();
                var audio=root.GetComponent<AudioSource>();if(audio==null)audio=root.gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;audio.volume=.25f;
                feedback.EditorConfigure(audio,AssetDatabase.LoadAssetAtPath<AudioClip>(GameScaffold.Root+"/Audio/development_accept.wav"),AssetDatabase.LoadAssetAtPath<AudioClip>(GameScaffold.Root+"/Audio/development_blocked.wav"));
                var game=new SerializedObject(root);if(game.FindProperty("feedback").objectReferenceValue==null){game.FindProperty("feedback").objectReferenceValue=feedback;game.ApplyModifiedPropertiesWithoutUndo();}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                string prefabPath=GameScaffold.Prefabs+"/BuildingSite.prefab";var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
                try{prefab.GetComponentInChildren<TextMesh>(true).characterSize=.3f;PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
                AssetDatabase.SaveAssets();
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}

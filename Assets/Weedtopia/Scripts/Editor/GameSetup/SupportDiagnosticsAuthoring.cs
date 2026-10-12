using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit, additive installation using existing shared button prefabs. Never runs on import or at runtime.</summary>
    public static class SupportDiagnosticsAuthoring
    {
        public const string CopyButtonPrefab = "Assets/Weedtopia/Production/Prefabs/UiExperience/StoryBackButton.prefab";
        public const string SettingsButtonPrefab = "Assets/Weedtopia/Production/Prefabs/WorldNavigationButton.prefab";
        [MenuItem("Weedtopia/Release/Install or Repair Support Export")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            if(BuildPipeline.isBuildingPlayer||EditorApplication.isCompiling||EditorApplication.isUpdating)
                throw new InvalidOperationException("Wait for the current build, compilation or import to finish.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene changes first.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string path in new[]{GameScaffold.GameScene,"Assets/Weedtopia/Game/Scenes/BigRockCity.unity"})
                {
                    var scene=EditorSceneManager.OpenScene(path);
                    var root=Object.FindFirstObjectByType<GameRoot>();
                    if(root==null||root.Ui.Experience==null)throw new InvalidOperationException("Missing authored UI experience in "+path);
                    if(!Install(root))continue;
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();
            }
            finally { if(!UnityEngine.Application.isBatchMode)EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static bool Install(GameRoot root)
        {
            var view=root.Ui.Experience;bool changed=false;
            var start=Bound<RectTransform>(root.Ui,"start");
            var copy=EnsureButton(view.copyDiagnostics,view.gamePage,"CopyDiagnostics",CopyButtonPrefab,ref changed);
            var settings=EnsureButton(view.startSettings,start,"StartSettings",SettingsButtonPrefab,ref changed);
            if(view.copyDiagnostics!=copy||view.startSettings!=settings)
            {
                view.copyDiagnostics=copy;view.startSettings=settings;
                PrefabUtility.RecordPrefabInstancePropertyModifications(view);
                EditorUtility.SetDirty(view);changed=true;
            }
            var caption=copy.GetComponentInChildren<Text>(true);
            if(caption==null)throw new InvalidOperationException("Missing shared export button caption.");
            var localized=caption.GetComponent<LocalizedLabel>();
            if(localized==null){localized=caption.gameObject.AddComponent<LocalizedLabel>();changed=true;}
            if(localized.Key!="settings.diagnostics.copy")
            {localized.EditorConfigure("settings.diagnostics.copy");EditorUtility.SetDirty(localized);changed=true;}
            if(changed)
            {
                caption.text="Diagnose kopieren";EditorUtility.SetDirty(caption);
                var copyRect=(RectTransform)copy.transform;var reset=Bound<RectTransform>(root.Ui,"ui.reset");
                copyRect.anchorMin=copyRect.anchorMax=new Vector2(.5f,1);copyRect.pivot=new Vector2(.5f,.5f);
                copyRect.sizeDelta=reset.sizeDelta;
                copyRect.anchoredPosition=reset.anchoredPosition-new Vector2(0,reset.sizeDelta.y+30);
                view.gamePage.sizeDelta=new Vector2(view.gamePage.sizeDelta.x,
                    Mathf.Max(view.gamePage.sizeDelta.y,-copyRect.anchoredPosition.y+copyRect.sizeDelta.y*.5f+30));
                var rect=(RectTransform)settings.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;
                rect.anchoredPosition=new Vector2(-24,-24);rect.sizeDelta=new Vector2(80,80);
                var icon=rect.Find("Icon").GetComponent<Image>();icon.sprite=root.Presentation.art.Get("settings");
                icon.rectTransform.sizeDelta=new Vector2(48,48);
                PrefabUtility.RecordPrefabInstancePropertyModifications(copyRect);
                PrefabUtility.RecordPrefabInstancePropertyModifications(caption);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                PrefabUtility.RecordPrefabInstancePropertyModifications(icon);
                PrefabUtility.RecordPrefabInstancePropertyModifications(icon.rectTransform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(view.gamePage);
                EditorUtility.SetDirty(view.gamePage);EditorUtility.SetDirty(view);
            }
            return changed;
        }

        private static Button EnsureButton(Button assigned,Transform parent,string name,string prefabPath,ref bool changed)
        {
            if(assigned!=null)
            {
                if(assigned.transform.parent!=parent)throw new InvalidOperationException("Preserve the assigned support control; repair its parent explicitly: "+name);
                return assigned;
            }
            var existing=parent.Find(name);
            if(existing!=null)
            {
                var button=existing.GetComponent<Button>();
                if(button==null)throw new InvalidOperationException("Existing support object is not a button: "+name);
                changed=true;return button;
            }
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if(prefab==null)throw new InvalidOperationException("Missing shared support button prefab: "+prefabPath);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);instance.name=name;instance.SetActive(true);
            changed=true;return instance.GetComponent<Button>();
        }

        private static T Bound<T>(GameUi ui,string id) where T:Component
        {
            var entries=new SerializedObject(ui).FindProperty("bindings");
            for(int i=0;i<entries.arraySize;i++)
            {
                var entry=entries.GetArrayElementAtIndex(i);
                if(entry.FindPropertyRelative("id").stringValue==id)
                    return ((GameObject)entry.FindPropertyRelative("target").objectReferenceValue).GetComponent<T>();
            }
            throw new InvalidOperationException("Missing authored UI binding: "+id);
        }
    }
}

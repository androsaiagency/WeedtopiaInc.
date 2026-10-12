using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Unity;
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit scene migration: keeps the original controls and all world references intact.</summary>
    public static class WorldNavigationAuthoring
    {
        public const string ButtonPath="Assets/Weedtopia/Production/Prefabs/WorldNavigationButton.prefab";
        public const string DockPath="Assets/Weedtopia/Production/Prefabs/WorldNavigationDock.prefab";
        private static readonly string[] Ids={"zone.Farm","zone.City","zone.Settlement","zone.Industry","ui.journal"};
        private static readonly string[] Names={"Farm","City","Settlement","Industry","Orders"};
        private static readonly string[] Icons={"farm","city","settlement","industry","quest"};
        [MenuItem("Weedtopia/Presentation/Install World Navigation")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode before authoring.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene changes first.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var root=Object.FindFirstObjectByType<GameRoot>();
                EnsurePrefabs(root.Presentation.art);
                if(root.Ui.GetComponent<WorldNavigationBar>()!=null){Validate(root);return;}
                var so=new SerializedObject(root.Ui);var bindings=so.FindProperty("bindings");
                var map=new Dictionary<string,GameObject>();
                for(int i=0;i<bindings.arraySize;i++)
                {var e=bindings.GetArrayElementAtIndex(i);map.Add(e.FindPropertyRelative("id").stringValue,(GameObject)e.FindPropertyRelative("target").objectReferenceValue);}
                var safe=(RectTransform)so.FindProperty("safeArea").objectReferenceValue;
                var dock=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DockPath),safe);
                var archive=Rect("ArchivedNavigationButtons",root.Ui.transform,Vector2.zero);
                archive.gameObject.SetActive(false);
                for(int i=0;i<Ids.Length;i++)
                {
                    map[Ids[i]].transform.SetParent(archive,false);
                    var replacement=dock.transform.Find(Names[i]).gameObject;
                    for(int j=0;j<bindings.arraySize;j++)
                    {var e=bindings.GetArrayElementAtIndex(j);if(e.FindPropertyRelative("id").stringValue==Ids[i])e.FindPropertyRelative("target").objectReferenceValue=replacement;}
                }
                var navigation=root.Ui.gameObject.AddComponent<WorldNavigationBar>();
                navigation.EditorConfigure((RectTransform)dock.transform);
                so.FindProperty("worldNavigation").objectReferenceValue=navigation;so.ApplyModifiedPropertiesWithoutUndo();
                // Compact the remaining menu actions into three rows next to the existing quest card.
                Place(map["ui.townhall"],new Vector2(70,-252));Place(root.Ui.Interaction.inventory.gameObject,new Vector2(384,-252));
                Place(root.Ui.Interaction.overview.gameObject,new Vector2(70,-330));Place(map["ui.save"],new Vector2(384,-330));
                Place(map["ui.pause"],new Vector2(70,-408));
                var toast=map["message"].GetComponent<RectTransform>();
                toast.anchorMin=toast.anchorMax=new Vector2(.5f,0);toast.anchoredPosition=new Vector2(0,158);
                map["message"].GetComponent<Text>().raycastTarget=false;
                dock.transform.SetAsLastSibling();map["modalShade"].transform.SetAsLastSibling();
                dock.SetActive(false);
                var districtMap=AssetDatabase.LoadAssetAtPath<DistrictMap>("Assets/Weedtopia/Production/Layout/DistrictMap.asset");
                districtMap.overviewAspect=16f/9f;
                EditorUtility.SetDirty(districtMap);EditorUtility.SetDirty(root.Ui);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Validate(root);
            }
            finally {if(!UnityEngine.Application.isBatchMode)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static void EnsurePrefabs(PresentationArt art)
        {
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
            if(existing!=null)
            {
                var contents=PrefabUtility.LoadPrefabContents(ButtonPath);
                // Persist required components even when Unity has already supplied one during import.
                try {if(contents.GetComponent<CanvasRenderer>()==null)contents.AddComponent<CanvasRenderer>();PrefabUtility.SaveAsPrefabAsset(contents,ButtonPath);}
                finally {PrefabUtility.UnloadPrefabContents(contents);}
            }
            if(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath)==null)
            {
                var rect=Rect("WorldNavigationButton",null,new Vector2(96,96));
                var skin=rect.gameObject.AddComponent<RoundNavigationGraphic>();
                var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=skin;
                var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,1,.85f);
                colors.pressedColor=new Color(.65f,.86f,.73f);colors.selectedColor=Color.white;colors.fadeDuration=.08f;button.colors=colors;
                button.navigation=new Navigation{mode=Navigation.Mode.None};
                var icon=Rect("Icon",rect,new Vector2(62,62)).gameObject.AddComponent<Image>();
                icon.sprite=art.Get("farm");icon.preserveAspect=true;icon.raycastTarget=false;
                PrefabUtility.SaveAsPrefabAsset(rect.gameObject,ButtonPath);Object.DestroyImmediate(rect.gameObject);
            }
            if(AssetDatabase.LoadAssetAtPath<GameObject>(DockPath)==null)
            {
                var dock=Rect("WorldNavigationDock",null,new Vector2(560,104));
                dock.anchorMin=dock.anchorMax=new Vector2(.5f,0);dock.pivot=new Vector2(.5f,0);dock.anchoredPosition=new Vector2(0,20);
                for(int i=0;i<Ids.Length;i++)
                {
                    var button=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath),dock);
                    button.name=Names[i];button.GetComponent<RectTransform>().anchoredPosition=new Vector2((i-2)*112,0);
                    button.GetComponentInChildren<Image>().sprite=art.Get(Icons[i]);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(button.GetComponentInChildren<Image>());
                }
                PrefabUtility.SaveAsPrefabAsset(dock.gameObject,DockPath);Object.DestroyImmediate(dock.gameObject);
            }
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 size)
        {
            var obj=new GameObject(name,typeof(RectTransform));var rect=obj.GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;return rect;
        }
        private static void Place(GameObject obj,Vector2 point) {obj.GetComponent<RectTransform>().anchoredPosition=point;}
        private static void Validate(GameRoot root)
        {
            var navigation=root.Ui.GetComponent<WorldNavigationBar>();
            var buttons=navigation.Dock.GetComponentsInChildren<Button>(true);
            int expected=root.Ui.GetComponentInChildren<CityStatusBar>(true)!=null?7:5;
            if(buttons.Length!=expected||buttons.Any(b=>b.GetComponent<RoundNavigationGraphic>()==null||b.GetComponent<CanvasRenderer>()==null||b.GetComponentsInChildren<Text>(true).Length>0))
                throw new InvalidOperationException("World navigation must contain "+expected+" round icon-only controls.");
            Directory.CreateDirectory("Docs/WorldNavigationRevision");
            File.WriteAllText("Docs/WorldNavigationRevision/authoring_receipt.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),buttons=buttons.Length,buildings=root.Sites.Length,routes=root.Presentation.routes.Length},true));
        }
        [Serializable] private sealed class Receipt {public string utc;public int buttons,buildings,routes;}
    }
}
